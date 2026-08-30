Shader "Custom/DamageInstanced"
{
    Properties
    {
        _MainTex ("Digit Atlas 1 (e.g. 加速)", 2D) = "white" {}
        _MainTex2 ("Digit Atlas 2 (e.g. 击退)", 2D) = "white" {}
        _Gravity ("Gravity", Float) = 15.0
        _LifeTime ("Life Time", Float) = 1.0
        _BaseScale ("Base Scale", Float) = 1.0
    }
    SubShader
    {
        // 彻底稳定在 Overlay 队列，由单独的 DamageCamera 负责渲染
        Tags { "Queue"="Overlay+100" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Blend SrcAlpha OneMinusSrcAlpha 
        ZWrite Off
        Cull Off
        ZTest Always 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            // 必须与 C# 端的结构体字节完全对齐
            struct DamageDigitData
            {
                float3 startPos;
                float2 velocity;
                float startTime;
                uint digit;
                float4 color;
                float scaleMultiplier;
                uint texIndex; // 新增：用于区分使用哪张图集 (0: _MainTex, 1: _MainTex2)
            };

            StructuredBuffer<DamageDigitData> _DigitDataBuffer;

            sampler2D _MainTex;
            sampler2D _MainTex2;
            float _Gravity;
            float _LifeTime;
            float _BaseScale;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                uint texIndex : TEXCOORD1; // 传递给片元着色器
            };

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                
                DamageDigitData data = _DigitDataBuffer[instanceID];
                float timeAlive = _Time.y - data.startTime;

                // 生命期结束，直接缩放到0（配合 CPU 端的回收逻辑）
                if (timeAlive > _LifeTime) 
                {
                    o.pos = float4(0, 0, 0, 0);
                    return o;
                }

                // 1. 物理动画：计算抛物线
                float3 currentPos = data.startPos;
                currentPos.x += data.velocity.x * timeAlive;
                currentPos.y += (data.velocity.y * timeAlive) - (0.5 * _Gravity * timeAlive * timeAlive);

                // 2. 动画效果：先放大后缩小，透明度渐隐
                float normalizedTime = saturate(timeAlive / _LifeTime);
                
                // 将缩放直接应用于模型顶点（结合材质面板的 Base Scale 和来自 C# 的实例 scaleMultiplier）
                float scale = _BaseScale * data.scaleMultiplier * (1.0 - pow(normalizedTime, 3.0));
                float alpha = 1.0 - pow(normalizedTime, 2.0);
                
                // 将本地 Quad 顶点放大，并加上世界坐标
                float3 worldPos = currentPos + (v.vertex.xyz * scale);
                
                // 正常的投影转换
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));

                // 3. UV 切割 (将默认的 0~1 压缩到 0~0.1，并根据数字偏移)
                float uvWidth = 0.1; 
                o.uv.x = (v.texcoord.x * uvWidth) + (data.digit * uvWidth);
                o.uv.y = v.texcoord.y;

                o.color = data.color;
                o.color.a *= alpha;
                
                o.texIndex = data.texIndex; // 传递图集索引

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col;
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
