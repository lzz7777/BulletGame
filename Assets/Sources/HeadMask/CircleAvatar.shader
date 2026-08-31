Shader "UI/CircleAvatar_Final"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Smoothness ("Edge Smoothness", Range(0.001, 0.1)) = 0.01 
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil { /* 保持默认 */ }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0; // 图集 UV，用于采样颜色
                float2 uv1      : TEXCOORD1; // 【新增】C# 传进来的本地 UV，用于画圆！
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1; 
                float2 localUV  : TEXCOORD2; // 传递给片段着色器的本地 UV
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Smoothness;
            float4 _ClipRect; 

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex; 
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                
                OUT.texcoord = IN.texcoord; 
                // 接收 C# 传过来的 0~1 完美 UV
                OUT.localUV = IN.uv1; 
                OUT.color = IN.color * _Color;
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. 用图集 UV 正常采样图像颜色
                half4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // 2. 【核心解法】用 C# 传来的本地 UV (永远是 0~1) 来计算距离！
                float dist = distance(IN.localUV, float2(0.5, 0.5));
                
                // 3. 画圆并抗锯齿
                color.a *= smoothstep(0.5, 0.5 - _Smoothness, dist);
                
                // 4. RectMask2D 裁剪支持
                #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                clip(color.a - 0.001);
                
                return color;
            }
            ENDCG
        }
    }
}