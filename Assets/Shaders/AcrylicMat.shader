Shader "ASL_LearnVR/AcrylicMat"
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Mantel de cristal acrílico para la mano guía.
    //  Aspecto: translúcido blanco, borde índigo fino, micro-noise, sombra suave.
    //  Aplicar a un Quad o Plane plano sobre la mesa.
    // ─────────────────────────────────────────────────────────────────────────
    Properties
    {
        _BaseColor      ("Base Color",       Color)   = (1, 1, 1, 0.18)
        _NoiseScale     ("Noise Scale",      Float)   = 14.0
        _NoiseStrength  ("Noise Strength",   Range(0, 0.05)) = 0.018

        _BorderColor    ("Border Color",     Color)   = (0.29, 0.44, 0.83, 0.65)
        _BorderWidth    ("Border Width",     Range(0.005, 0.08)) = 0.022
        _BorderGlow     ("Border Glow",      Range(0, 0.06)) = 0.016
        _GlowIntensity  ("Glow Intensity",   Range(0, 1))    = 0.35

        _Radius         ("Corner Radius",    Range(0, 0.25)) = 0.10
        _Feather        ("Edge Feather",     Range(0.002, 0.03)) = 0.006

        // Opción: mostrar etiqueta de texto embebida (no usada directamente en shader,
        // se gestiona desde GuideHandMatController.cs)
        _LabelAlpha     ("Label Alpha",      Range(0, 1))    = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+1"   // encima de la mesa
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _BaseColor;
            float  _NoiseScale;
            float  _NoiseStrength;
            float4 _BorderColor;
            float  _BorderWidth;
            float  _BorderGlow;
            float  _GlowIntensity;
            float  _Radius;
            float  _Feather;
            float  _LabelAlpha;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos    : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float smoothNoise(float2 uv, float scale)
            {
                float2 i = floor(uv * scale);
                float2 f = frac(uv * scale);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash21(i), b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1)), d = hash21(i + float2(1,1));
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            float roundedRectSDF(float2 uv, float r)
            {
                float2 d = abs(uv - 0.5) - (0.5 - r);
                return length(max(d, 0.0)) - r;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float sdf        = roundedRectSDF(i.uv, _Radius);
                float shapeMask  = 1.0 - smoothstep(-_Feather, _Feather, sdf);
                if (shapeMask < 0.001) discard;

                // Base con micro-noise
                float4 col = _BaseColor;
                float  n   = smoothNoise(i.uv, _NoiseScale) * 2.0 - 1.0;
                col.rgb    = saturate(col.rgb + n * _NoiseStrength);

                // Borde
                float innerSDF   = -(sdf + _BorderWidth);
                float borderMask = smoothstep(-_Feather, _Feather, innerSDF)
                                 * (1.0 - smoothstep(-_Feather, 0, sdf));
                float glowMask   = (1.0 - smoothstep(0, _BorderGlow, -sdf)) * (1.0 - shapeMask);

                fixed4 result  = col;
                result.a      *= shapeMask;
                result         = lerp(result, _BorderColor, borderMask * _BorderColor.a);
                result.rgb     = lerp(result.rgb, _BorderColor.rgb, glowMask * _GlowIntensity);
                result.a       = max(result.a, glowMask * _BorderColor.a * 0.4);

                return result;
            }
            ENDCG
        }
    }
}
