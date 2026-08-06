Shader "UI/Video/VideoAlphaMaskGray"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaskTex ("Mask (Black=Opaque, White=Transparent)", 2D) = "white" {}
        _Alpha ("Alpha", Range(0,1)) = 1
        _MaskStrength ("Mask Strength", Range(0,1)) = 1
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
                fixed3 m = tex2D(_MaskTex, i.uvMask).rgb;
                float lum = dot(m, float3(0.2126, 0.7152, 0.0722));
                float maskFactor = 1.0 - lum;
                maskFactor = lerp(1.0, maskFactor, _MaskStrength);
                float clipA = UnityGet2DClipping(i.worldPosition, _ClipRect);
                col.a = col.a * maskFactor * _Alpha * clipA;
                return col;
            }
            ENDCG
        }
    }
}
