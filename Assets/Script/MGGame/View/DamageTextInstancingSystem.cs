using System.Runtime.InteropServices;
using UnityEngine;

// ==========================================
// 【核心思想】：面向数据设计 (DOD) 与 GPU 驱动
// CPU 不再负责计算每个飘字的位移和缩放，只负责“发号施令”（生成基础数据）。
// 所有繁重的动画、物理抛物线计算，全部交给 GPU (Shader) 并行处理。
// ==========================================

// 严格对齐的数据结构体 (总计 52 Bytes)
// 【注意】：这个结构体的字段顺序和类型，必须和 Shader 中的 DamageDigitData 完全一致！
// CPU 就是通过这个结构体把参数打包，一次性塞给显卡的。
public struct DamageDigitData
{
    public Vector3 startPos;      // 12 bytes - 飘字出生的世界坐标起点
    public Vector2 velocity;      // 8 bytes  - 初速度 (X控制左右抛物线，Y控制向上弹跳力度)
    public float startTime;       // 4 bytes  - 出生时间 (Time.time)，GPU 用它算存活了多久
    public uint digit;            // 4 bytes  - 具体的数字 (0-9)，告诉 Shader 采样贴图的哪个区域
    public Color color;           // 16 bytes - 飘字的颜色 (支持暴击爆红等)
    public float scaleMultiplier; // 4 bytes  - 缩放系数，传递给 Shader 放大/缩小 Quad 用的
    public uint texIndex;         // 4 bytes  - 图集索引，告诉 Shader 用哪张图集 (0: 默认, 1: 击退等特殊图集)
}

public class DamageTextInstancingSystem : MonoBehaviour
{
    public static DamageTextInstancingSystem Instance;

    [Header("渲染资源")]
    public Mesh quadMesh;         // Unity 默认的 Quad 网格（一个正方形面片，只有4个顶点，极度省性能）
    public Material instancedMat; // 挂载了 DamageInstanced.shader 的材质球
    public Camera damageCamera;   // 专用飘字相机；为空时回退到 Camera.main (主相机)
    
    [Range(0, 31)]
    public int damageLayer = 0;   // 仅用于相机剔除(Culling)，决定这个飘字在哪一层渲染

    private const int MAX_DIGITS = 10000; // 同屏最大支持的单个【数字字符】数量 (注意：是字符不是飘字串，比如"123"算3个)
    
    // 【ComputeBuffer】：CPU 与 GPU 之间的高速通道
    private ComputeBuffer dataBuffer; // 用于存放所有飘字数据的 Buffer
    private ComputeBuffer argsBuffer; // 用于存放 IndirectArguments (间接渲染参数) 的 Buffer
    
    // args 数组是给 DrawMeshInstancedIndirect 用的特殊参数：
    // args[0]: 网格的索引数量 (Quad 是 6)
    // args[1]: 实例数量 (当前要渲染多少个字符)
    // 后三个填 0 即可
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    // 【内存池优化】：原生数组，内存绝对连续，避免了 List 的开销，对 CPU Cache 极度友好
    private DamageDigitData[] digitDataArray = new DamageDigitData[MAX_DIGITS];
    
    // 临时数组，用于 0 GC 拆解数字（比如把 123 拆成 [3, 2, 1]），最大支持 8 位数伤害
    private readonly int[] tempDigits = new int[8]; 
    private int activeCount = 0; // 当前已经生成的总字符数 (一直累加)

    [Header("外观设置")]
    [Tooltip("全局缩放比例 (控制飘字的基础大小)")]
    public float globalScale = 0.5f;
    [Tooltip("字间距")]
    public float charSpacing = 0.05f;

    // 字宽微调字典（因为美术切图里，数字 '1' 比较瘦，占位小；其他数字正常，占位大）
    // 数组下标 0-9 对应数字 0-9 的宽度
    private readonly float[] digitWidths = { 0.4f, 0.25f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f };

    void Awake()
    {
        Instance = this;

        // 设置材质的渲染队列，3000 是透明半透明队列 (Transparent)，保证飘字能正确混合背景
        if (instancedMat != null)
        {
            instancedMat.renderQueue = 3000;
        }

        // 初始化 ComputeBuffer
        // 参数1：数量 (10000)，参数2：单个数据大小 (52 字节)
        dataBuffer = new ComputeBuffer(MAX_DIGITS, Marshal.SizeOf(typeof(DamageDigitData)));
        // 初始化参数 Buffer，ComputeBufferType.IndirectArguments 表示它是用来发渲染指令的
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        // 获取 Quad 网格的索引数量，固定为 6
        args[0] = quadMesh != null ? quadMesh.GetIndexCount(0) : 6;
        argsBuffer.SetData(args);

        // 把数据 Buffer 绑定到材质球上，这样 Shader 里就能拿到 _DigitDataBuffer 了
        instancedMat.SetBuffer("_DigitDataBuffer", dataBuffer);
    }

    /// <summary>
    /// 对外接口：生成多位伤害数字
    /// </summary>
    public void SpawnDamage(int damageValue, Vector3 centerPos, Color color, uint texIndex = 0)
    {
        if (damageValue <= 0) return;
        
        // ==========================================
        // 第一步：0 GC 提取数字 (这是核心优化之一)
        // 传统做法 damageValue.ToString() 会产生大量字符串 GC。
        // 这里通过 %10 和 /10 纯数学计算，把每一位扣出来存进 tempDigits 数组。
        // ==========================================
        int digitCount = 0;
        int tempVal = damageValue;

        // 比如伤害是 123：
        // 第一次循环：tempDigits[0] = 3, tempVal = 12
        // 第二次循环：tempDigits[1] = 2, tempVal = 1
        // 第三次循环：tempDigits[2] = 1, tempVal = 0
        while (tempVal > 0 && digitCount < 8)
        {
            tempDigits[digitCount] = tempVal % 10;
            tempVal /= 10;
            digitCount++;
        }

        // ==========================================
        // 第二步：计算整串数字的总宽度，以便让数字在起点位置居中
        // ==========================================
        float totalWidth = 0f;
        for (int i = 0; i < digitCount; i++)
        {
            totalWidth += digitWidths[tempDigits[i]] * globalScale; // 累加每个字的宽度
            if (i > 0) totalWidth += charSpacing * globalScale;     // 累加字间距
        }

        // 计算这串数字最左边的起始 X 坐标
        float currentX = centerPos.x - (totalWidth * 0.5f); 
        
        // 随机一个抛物线初速度：X 决定往左还是往右飘，Y 决定往上蹦多高
        Vector2 randomVelocity = new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(3.0f, 6.0f)) * Mathf.Sqrt(globalScale);

        // ==========================================
        // 第三步：逐个字符生成实例数据 (因为 tempDigits 里是反的，所以倒序遍历)
        // 比如 123，tempDigits 是 [3, 2, 1]，倒序遍历出来的就是 1 -> 2 -> 3
        // ==========================================
        for (int i = digitCount - 1; i >= 0; i--)
        {
            int digit = tempDigits[i];
            float charWidth = digitWidths[digit] * globalScale;

            // 计算当前这个字符的中心坐标
            Vector3 charPos = new Vector3(currentX + charWidth * 0.5f, centerPos.y, centerPos.z);

            // 把这个字符的数据塞进大数组里
            AddDigitInstance(charPos, randomVelocity, (uint)digit, color, globalScale, texIndex);

            // 累加 X 坐标，游标往右移，准备排版下一个字
            currentX += charWidth + (charSpacing * globalScale);
        }
    }

    private void AddDigitInstance(Vector3 pos, Vector2 vel, uint digit, Color color, float scale, uint texIndex)
    {
        // 【环形缓冲区 (Ring Buffer) 思想】
        // 当 activeCount 超过 10000 时，取模操作会让 index 变回 0。
        // 直接覆盖最老的数据！这样就不需要复杂的对象池出池/入池逻辑了。
        int index = activeCount % MAX_DIGITS;

        digitDataArray[index] = new DamageDigitData
        {
            startPos = pos,
            velocity = vel,
            startTime = Time.time, // 记录当前时间，Shader 里拿这个时间计算动画进度
            digit = digit,
            color = color,
            scaleMultiplier = scale,
            texIndex = texIndex
        };
        activeCount++;
    }

    void Update()
    {
        if (activeCount == 0 || quadMesh == null || instancedMat == null)
        {
            return;
        }

        // 一次最多渲染 10000 个，如果没有满 10000，就按实际数量渲染
        int renderCount = Mathf.Min(activeCount, MAX_DIGITS);

        // 【提交数据】：把 CPU 数组里的数据一把推送到显卡的 Buffer 里
        dataBuffer.SetData(digitDataArray, 0, 0, renderCount);

        // 更新渲染参数：告诉显卡这次要画多少个 Quad
        args[1] = (uint)renderCount;
        argsBuffer.SetData(args);

        // ==========================================
        // 【暴力剔除破解】：绕过视锥体剔除
        // 因为 CPU 不计算飘字位移了，CPU 眼里飘字永远在起点。
        // 如果相机移动了，CPU 会误以为飘字在屏幕外，从而把它剔除不画了 (数字突然消失)。
        // 解决办法：造一个大到离谱的包围盒 (Bounds) 跟着相机，骗过 CPU 强制提交渲染，
        // 真正的屏幕内外剔除交给显卡 (GPU Clip Space) 去做。
        // ==========================================
        Camera targetCamera = damageCamera != null ? damageCamera : Camera.main;
        Vector3 boundsCenter = targetCamera != null ? targetCamera.transform.position : Vector3.zero;
        Bounds renderBounds = new Bounds(boundsCenter, new Vector3(10000, 10000, 10000));
        
        // 发起 GPU Instancing 绘制调用！1 个 DrawCall 画出成千上万个飘字！
        Graphics.DrawMeshInstancedIndirect(
            quadMesh,
            0,
            instancedMat,
            renderBounds,
            argsBuffer,
            0,
            null,
            UnityEngine.Rendering.ShadowCastingMode.Off, // 飘字不需要投影
            false, // 飘字不接收阴影
            damageLayer,
            targetCamera);
    }

    void OnDestroy()
    {
        // 记得释放显存！如果不释放会导致严重的内存泄漏
        dataBuffer?.Release();
        argsBuffer?.Release();
    }
}