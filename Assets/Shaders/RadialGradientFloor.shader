Shader "ASL_LearnVR/RadialGradientFloor"
{
    Properties
    {
        _CenterColor ("Center Color", Color) = (0.165, 0.165, 0.165, 1)
        _EdgeColor ("Edge Color", Color) = (0.62, 0.62, 0.62, 1)
        _Radius ("Gradient Radius", Float) = 3.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.1
        _CenterPos ("Center Position (XZ)", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            float4 _CenterColor;
            float4 _EdgeColor;
            float _Radius;
            float _Smoothness;
            float4 _CenterPos;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Distancia radial desde el centro en el plano XZ
                float2 delta = i.worldPos.xz - _CenterPos.xz;
                float dist = length(delta);

                // Gradiente suave con smoothstep
                float t = smoothstep(0, _Radius, dist);

                // Color base del degradado
                fixed4 color = lerp(_CenterColor, _EdgeColor, t);

                // Iluminacion basica (Lambert) para que reciba sombras
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndotl = max(0.0, dot(normal, lightDir));
                float3 lighting = _LightColor0.rgb * ndotl + UNITY_LIGHTMODEL_AMBIENT.rgb;

                color.rgb *= lighting;

                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }

        // Shadow caster pass para que proyecte sombras si es necesario
        Pass
        {
            Tags { "LightMode"="ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
