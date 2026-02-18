Shader "ASL_LearnVR/WallGradient"
{
    Properties
    {
        _BottomColor ("Bottom Color", Color) = (0.62, 0.62, 0.62, 1)
        _TopColor ("Top Color", Color) = (0.96, 0.96, 0.94, 1)
        _GradientHeight ("Gradient Height", Float) = 4.0
        _GroundLevel ("Ground Level (Y)", Float) = 0.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        // Cull Front para esfera invertida (ver el interior)
        Cull Front

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            float4 _BottomColor;
            float4 _TopColor;
            float _GradientHeight;
            float _GroundLevel;
            float _Smoothness;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Degradado vertical basado en altura world-space
                float height = i.worldPos.y - _GroundLevel;
                float t = saturate(height / _GradientHeight);

                // Suavizar la transicion
                t = smoothstep(0, 1, t);

                fixed4 color = lerp(_BottomColor, _TopColor, t);

                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
    FallBack Off
}
