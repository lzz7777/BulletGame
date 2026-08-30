using System.Runtime.InteropServices;
using UnityEngine;

// 严格对齐的数据结构体 (52 Bytes - 新增了 4 bytes 的 texIndex)
public struct DamageDigitData
{
    public Vector3 startPos; // 12 bytes
    public Vector2 velocity; // 8 bytes
    public float startTime; // 4 bytes
    public uint digit; // 4 bytes
    public Color color; // 16 bytes
    public float scaleMultiplier; // 4 bytes, 传递给 Shader 缩放 Quad
    public uint texIndex; // 4 bytes, 用于告诉 Shader 用哪张图集 (0 或 1)
}

public class DamageTextInstancingSystem : MonoBehaviour
{
    public static DamageTextInstancingSystem Instance;

    public Mesh quadMesh; // Unity 默认的 Quad 网格
    public Material instancedMat; // 挂载上方 Shader 的材质
    public Camera damageCamera; // 专用飘字相机；为空时回退到 Camera.main
    [Range(0, 31)]
    public int damageLayer = 0; // 仅用于相机剔除，不影响 Sorting Layer

    private const int MAX_DIGITS = 10000;
    private ComputeBuffer dataBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    private DamageDigitData[] digitDataArray = new DamageDigitData[MAX_DIGITS];
    private readonly int[] tempDigits = new int[8];
    private int activeCount = 0;

    [Header("外观设置")]
    [Tooltip("全局缩放比例 (控制飘字的基础大小)")]
    public float globalScale = 0.5f;
    [Tooltip("字间距")]
    public float charSpacing = 0.05f;

    // 字宽微调字典（根据您的图片，'1' 较瘦，其他正常）
    // 顺序对应 0-9
    private readonly float[] digitWidths = { 0.4f, 0.25f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f, 0.4f };

    void Awake()
    {
        Instance = this;

        // 单独相机决定绘制阶段，材质保留普通透明队列即可。
        if (instancedMat != null)
        {
            instancedMat.renderQueue = 3000;
        }

        // 初始化 ComputeBuffers
        dataBuffer = new ComputeBuffer(MAX_DIGITS, Marshal.SizeOf(typeof(DamageDigitData)));
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        args[0] = quadMesh != null ? quadMesh.GetIndexCount(0) : 6;
        argsBuffer.SetData(args);

        instancedMat.SetBuffer("_DigitDataBuffer", dataBuffer);
    }

    /// <summary>
    /// 对外接口：生成多位伤害数字
    /// </summary>
    /// <param name="damageValue">数值</param>
    /// <param name="centerPos">世界坐标位置</param>
    /// <param name="color">颜色</param>
    /// <param name="texIndex">图集索引 (0: 默认图集, 1: 第二张图集如加速/击退)</param>
    public void SpawnDamage(int damageValue, Vector3 centerPos, Color color, uint texIndex = 0)
    {
        if (damageValue <= 0) return;
        
        // 1. 0 GC 提取数字 (从低位到高位，最大支持 8 位数)
        int digitCount = 0;
        int tempVal = damageValue;

        while (tempVal > 0 && digitCount < 8)
        {
            tempDigits[digitCount] = tempVal % 10;
            tempVal /= 10;
            digitCount++;
        }

        // 2. 计算总宽度，以便让整个数字串居中
        float totalWidth = 0f;
        for (int i = 0; i < digitCount; i++)
        {
            totalWidth += digitWidths[tempDigits[i]] * globalScale;
            if (i > 0) totalWidth += charSpacing * globalScale;
        }

        // 3. 生成每个字符的实例（从高位到低位排版，所以反向遍历）
        float currentX = centerPos.x - (totalWidth * 0.5f); // 居中起始点
        
        // 【速度调整】：配合全局缩放，让抛物线高度也适当缩放
        Vector2 randomVelocity = new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(3.0f, 6.0f)) * Mathf.Sqrt(globalScale);

        for (int i = digitCount - 1; i >= 0; i--)
        {
            int digit = tempDigits[i];
            float charWidth = digitWidths[digit] * globalScale;

            // 当前字符的中心坐标
            Vector3 charPos = new Vector3(currentX + charWidth * 0.5f, centerPos.y, centerPos.z);

            AddDigitInstance(charPos, randomVelocity, (uint)digit, color, globalScale, texIndex);

            // 累加 X 坐标，准备排版下一个字
            currentX += charWidth + (charSpacing * globalScale);
        }
    }

    private void AddDigitInstance(Vector3 pos, Vector2 vel, uint digit, Color color, float scale, uint texIndex)
    {
        // 简单环形覆盖，防止越界
        int index = activeCount % MAX_DIGITS;

        digitDataArray[index] = new DamageDigitData
        {
            startPos = pos,
            velocity = vel,
            startTime = Time.time,
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

        int renderCount = Mathf.Min(activeCount, MAX_DIGITS);

        // 推送数据到 GPU
        dataBuffer.SetData(digitDataArray, 0, 0, renderCount);

        // 更新绘制参数
        args[1] = (uint)renderCount;
        argsBuffer.SetData(args);

        // 动态跟随主相机位置，防止被相机的视锥体 (Frustum) 错误剔除
        Camera targetCamera = damageCamera != null ? damageCamera : Camera.main;
        Vector3 boundsCenter = targetCamera != null ? targetCamera.transform.position : Vector3.zero;
        Bounds renderBounds = new Bounds(boundsCenter, new Vector3(10000, 10000, 10000));
        
        Graphics.DrawMeshInstancedIndirect(
            quadMesh,
            0,
            instancedMat,
            renderBounds,
            argsBuffer,
            0,
            null,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false,
            damageLayer,
            targetCamera);
    }

    void OnDestroy()
    {
        dataBuffer?.Release();
        argsBuffer?.Release();
    }
}
