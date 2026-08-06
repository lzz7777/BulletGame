Shader"UGC/Composure"
{
    Properties
    {
        _BaseTex ("BaseTex", 2D) = "white" {}
        _Tex1 ("_Tex1", 2D) = "white" {}
        _GammaCorrect("GammaCorrect", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma multi_compile _ FlipY
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _BaseTex;
            float4 _BaseTex_ST;
            sampler2D _Tex1;
            sampler2D _Tex2;
            sampler2D _Tex3;
            int _TexCount1;
            int _TexCount2;
            int _TexCount3;
            
            float _GammaCorrect;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BaseTex);
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                #if FlipY
                    uv.y = 1 - uv.y;
                #endif 
                float4 backColor = tex2D(_BaseTex, uv);
                float4 tex1Color = tex2D(_Tex1, uv);
                float4 tex2Color = tex2D(_Tex2, uv);
                float4 tex3Color = tex2D(_Tex3, uv);
                float a = clamp(tex1Color.a - 1 + _TexCount1, 0, 1);
                float4 col = lerp(backColor, tex1Color, a);
                
                a = clamp(tex2Color.a - 1 + _TexCount2, 0, 1);
                col = lerp(col, tex2Color, a);
                a = clamp(tex3Color.a - 1 + _TexCount3, 0, 1);
                col = lerp(col, tex3Color, a);
                
                col.xyz = pow(col.xyz, _GammaCorrect);
                col.a = 1;
                return col;
            }
            ENDHLSL
        }
    }
}
