Shader "Custom/UGUI/ImageTint"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint Color", Color) = (1,1,1,1)
        _Black("Black Point", Color) = (0,0,0,0)
        
        // UGUI 必要属性
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha  // Spine 使用的预乘Alpha混合
        ColorMask[_ColorMask]

        Pass
        {
            Name "Default"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _Black;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                // Spine 的逻辑：将预乘 Alpha 版本的 _Color 与顶点颜色结合
                // _Color.rgb * _Color.a 表示预乘 Alpha
                OUT.color = v.color * float4(_Color.rgb * _Color.a, _Color.a);
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 采样纹理
                fixed4 texColor = tex2D(_MainTex, IN.texcoord);
                
                // 确保纹理也是预乘 Alpha（如果原始纹理不是，这里会转换）
                texColor.rgb *= texColor.a;
                
                // Spine 的双色着色公式：
                // (texColor * vertexColor) + (1 - texColor.rgb) * _Black.rgb * texColor.a * _Color.a * vertexColor.a
                fixed4 finalColor;
                
                // 第一部分：基础着色
                finalColor = texColor * IN.color;
                
                // 第二部分：黑色点调整（仅对非黑色的区域）
                fixed3 blackAdjustment = (1.0 - texColor.rgb) * _Black.rgb;
                blackAdjustment *= texColor.a * _Color.a * IN.color.a;
                
                finalColor.rgb += blackAdjustment;
                finalColor.a = texColor.a * IN.color.a;
                
                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalColor.a - 0.001);
                #endif
                
                return finalColor;
            }
            ENDCG
        }
    }
}