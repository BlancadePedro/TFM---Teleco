Shader "UI/Rounded"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Radius ("Radius", Range(0,0.5)) = 0.08
        _Feather ("Feather", Range(0,0.2)) = 0.01
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Radius;
            float _Feather;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // return smooth alpha mask for rounded rect in UV space
            float roundedMask(float2 uv)
            {
                // uv in 0..1
                float2 p = uv;
                float2 b = float2(_Radius, _Radius);

                // distance to nearest edge in normalized coords
                float2 d = max(abs(p - 0.5) - (0.5 - b), 0.0);
                float dist = length(d) - _Radius;
                return saturate(1.0 - smoothstep(-_Feather, _Feather, dist));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * i.color;
                float mask = roundedMask(i.uv);
                tex.a *= mask;
                return tex;
            }
            ENDCG
        }
    }
}
