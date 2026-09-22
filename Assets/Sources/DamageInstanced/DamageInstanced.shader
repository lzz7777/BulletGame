Shader "Custom/DamageInstanced"
{
    Properties
    {
        _MainTex ("Digit Atlas 1 (e.g. 加速)", 2D) = "white" {}
        _MainTex2 ("Digit Atlas 2 (e.g. 击退)", 2D) = "white" {}
        _Gravity ("Gravity", Float) = 15.0       // 重力加速度，控制飘字下坠的快慢
        _LifeTime ("Life Time", Float) = 1.0     // 飘字存活时间，决定动画播放周期
        _BaseScale ("Base Scale", Float) = 1.0   // 材质面板上的基础缩放
    }
    SubShader
    {
        // 彻底稳定在 Overlay 队列，由单独的 DamageCamera 负责渲染，永远显示在最上层
        Tags { "Queue"="Overlay+100" "RenderType"="Transparent" "IgnoreProjector"="True" }

        // 正常的半透明混合模式 (SrcAlpha * 源颜色 + (1-SrcAlpha) * 目标颜色)
        Blend SrcAlpha OneMinusSrcAlpha 
        ZWrite Off    // 关闭深度写入，因为是半透明物体，写深度会导致互相遮挡黑边
        Cull Off      // 关闭背面剔除，无论正反面都画
        ZTest Always  // 关闭深度测试，无视遮挡，永远画在最前面

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing // 开启 GPU Instancing 变体支持
            #include "UnityCG.cginc"

            // ==========================================
            // 必须与 C# 端的 DamageDigitData 结构体字节完全对齐！
            // 显卡就是通过这个结构体，拿到 C# 传过来的每一个飘字的参数
            // ==========================================
            struct DamageDigitData
            {
                float3 startPos;       // 起点
                float2 velocity;       // 初速度 (x:水平漂移, y:垂直跳跃)
                float startTime;       // 创建时间
                uint digit;            // 是数字几 (0-9)
                float4 color;          // 颜色
                float scaleMultiplier; // 缩放倍率
                uint texIndex;         // 图集编号 (0: _MainTex, 1: _MainTex2)
            };

            // StructuredBuffer 相当于显存里的一个大数组，存储了上万个飘字的数据
            StructuredBuffer<DamageDigitData> _DigitDataBuffer;

            sampler2D _MainTex;
            sampler2D _MainTex2;
            float _Gravity;
            float _LifeTime;
            float _BaseScale;

            // 顶点着色器传给片元着色器的数据结构
            struct v2f
            {
                float4 pos : SV_POSITION; // 最终屏幕上的裁剪空间坐标
                float2 uv : TEXCOORD0;    // 采样贴图用的 UV 坐标
                float4 color : COLOR;     // 顶点颜色
                uint texIndex : TEXCOORD1;// 传递给片元着色器的图集编号
            };

            // ==========================================
            // Vertex Shader (顶点着色器)：这里取代了 CPU 的 Update 计算
            // ==========================================
            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                
                // 根据 InstanceID，从 Buffer 中拿到当前这个飘字的数据
                DamageDigitData data = _DigitDataBuffer[instanceID];
                
                // 计算当前飘字已经存活了多长时间
                float timeAlive = _Time.y - data.startTime;

                // 【生命周期控制】
                // 如果存活时间超过了设定的寿命，直接把顶点缩放到原点(0,0,0,0)
                // 这样显卡就不会画它了，完美配合 CPU 端的 Ring Buffer 覆盖逻辑
                if (timeAlive > _LifeTime) 
                {
                    o.pos = float4(0, 0, 0, 0);
                    return o;
                }

                // ==========================================
                // 1. 物理动画：计算抛物线轨迹
                // ==========================================
                float3 currentPos = data.startPos;
                // X轴：匀速直线运动 (位移 = 速度 * 时间)
                currentPos.x += data.velocity.x * timeAlive;
                // Y轴：上抛下坠运动 (位移 = 初速度 * 时间 - 0.5 * 重力 * 时间的平方)
                currentPos.y += (data.velocity.y * timeAlive) - (0.5 * _Gravity * timeAlive * timeAlive);

                // ==========================================
                // 2. 缩放与透明度动画
                // ==========================================
                // 将存活时间映射到 0~1 的进度值 (saturate 保证不会超过 1)
                float normalizedTime = saturate(timeAlive / _LifeTime);
                
                // 缩放曲线：随着时间流逝，慢慢缩小直到 0 (1 - t^3 是一种缓动曲线)
                float scale = _BaseScale * data.scaleMultiplier * (1.0 - pow(normalizedTime, 3.0));
                
                // 透明度曲线：随着时间流逝，渐渐变透明 (1 - t^2)
                float alpha = 1.0 - pow(normalizedTime, 2.0);
                
                // 把模型原本的顶点放大，再加上计算出来的抛物线世界坐标
                float3 worldPos = currentPos + (v.vertex.xyz * scale);
                
                // 将世界坐标转换到屏幕裁剪空间
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));

                // ==========================================
                // 3. UV 切割 (非常巧妙的排版逻辑)
                // 假设 0-9 横向排成一排，每个数字占 1/10 (即 0.1)
                // ==========================================
                float uvWidth = 0.1; 
                // 原本的 0~1 UV 被压缩到 0~0.1，然后根据具体数字加上偏移
                // 比如 digit 是 3，那起点就是 0.3，UV 范围就是 0.3 ~ 0.4
                o.uv.x = (v.texcoord.x * uvWidth) + (data.digit * uvWidth);
                o.uv.y = v.texcoord.y;

                // 应用颜色和透明度
                o.color = data.color;
                o.color.a *= alpha;
                
                // 传递图集索引
                o.texIndex = data.texIndex;

                return o;
            }

            // ==========================================
            // Fragment Shader (片元/像素着色器)
            // ==========================================
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col;
                // 根据 CPU 传过来的 texIndex，决定采样哪一张贴图
                if (i.texIndex == 0)
                {
                    col = tex2D(_MainTex, i.uv) * i.color;
                }
                else
                {
                    col = tex2D(_MainTex2, i.uv) * i.color;
                }
                return col;
            }
            ENDCG
        }
    }
}