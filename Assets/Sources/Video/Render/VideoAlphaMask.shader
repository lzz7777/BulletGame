Shader "UI/Video/VideoAlphaMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "white" {}
        _Alpha ("Alpha", Range(0,1)) = 1
        _MaskStrength ("Mask Strength", Range(0,1)) = 1
        _InvertMask ("Invert Mask", Float) = 0
        _MaskBias ("Mask Bias", Range(-1,1)) = 0
        _MaskGamma ("Mask Gamma", Range(0.1,4)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _MaskTex;
            float4 _MaskTex_ST;
            float _Alpha;
            float _MaskStrength;
            float _InvertMask;
            float _MaskBias;
            float _MaskGamma;
            float4 _ClipRect;

            struct appdata_t { float4 vertex: POSITION; float2 texcoord: TEXCOORD0; float4 color: COLOR; };
            struct v2f { float4 vertex: SV_POSITION; float2 uvMain: TEXCOORD0; float2 uvMask: TEXCOORD1; float4 color: COLOR; float4 worldPosition: TEXCOORD2; };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uvMain = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.uvMask = TRANSFORM_TEX(v.texcoord, _MaskTex);
                o.color = v.color;
                o.worldPosition = v.vertex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uvMain) * i.color;
                fixed4 m = tex2D(_MaskTex, i.uvMask);
                float maskA = saturate(m.a);
                maskA = (_InvertMask > 0.5) ? (1.0 - maskA) : maskA;
                maskA = saturate(maskA + _MaskBias);
                maskA = pow(maskA, _MaskGamma);
                maskA = lerp(1.0, maskA, _MaskStrength);
                float clipA = UnityGet2DClipping(i.worldPosition, _ClipRect);
                col.a = col.a * maskA * _Alpha * clipA;
                return col;
            }
            ENDCG
        }
    }
}
