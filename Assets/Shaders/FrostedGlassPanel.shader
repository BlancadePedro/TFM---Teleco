Shader "ASL_LearnVR/FrostedGlassPanel"
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Frosted Glass Panel  —  ASL_LearnVR design system
    //
    //  Material base: blanco translúcido (alpha 0.10–0.14) + noise micro-textura
    //  Borde: color reactivo (idle / hover / correct / pending)
    //  Compatibilidad: Built-in RP + URP (Forward)
    // ─────────────────────────────────────────────────────────────────────────
    Properties
    {
        // ── Panel base ──────────────────────────────────────
        _PanelColor     ("Panel Color",         Color)   = (1, 1, 1, 0.12)
        _NoiseScale     ("Noise Scale",         Float)   = 18.0
        _NoiseStrength  ("Noise Strength",      Range(0, 0.06)) = 0.025

        // ── Borde ───────────────────────────────────────────
        _BorderColor    ("Border Color",        Color)   = (0.29, 0.435, 0.83, 0.5)
        _BorderWidth    ("Border Width (UV)",   Range(0.001, 0.06)) = 0.012
        _BorderGlow     ("Border Glow Spread",  Range(0, 0.08)) = 0.022
        _GlowIntensity  ("Glow Intensity",      Range(0, 1.5))  = 0.45

        // ── Degradado vertical interno ───────────────────────
        _TopAlphaBoost  ("Top Alpha Boost",     Range(0, 0.15)) = 0.04

        // ── Radius (esquinas redondeadas, UV-space) ──────────
        _Radius         ("Corner Radius",       Range(0, 0.15)) = 0.06
        _Feather        ("Edge Feather",        Range(0.002, 0.04)) = 0.008

        // ── Animación: pulso del borde ───────────────────────
        _PulseSpeed     ("Pulse Speed",         Float)   = 1.2
        _PulseAmplitude ("Pulse Amplitude",     Range(0, 0.4)) = 0.0   // 0 = sin pulso
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "IgnoreProjector"= "True"
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

            // ── inputs ──────────────────────────────────────────
            float4 _PanelColor;
            float  _NoiseScale;
            float  _NoiseStrength;
            float4 _BorderColor;
            float  _BorderWidth;
            float  _BorderGlow;
            float  _GlowIntensity;
            float  _TopAlphaBoost;
            float  _Radius;
            float  _Feather;
            float  _PulseSpeed;
            float  _PulseAmplitude;

            struct appdata
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.texcoord;
                return o;
            }

            // ── Helpers ─────────────────────────────────────────

            // Hash 2D → float  (sin(dot) trick, rápido en GPU)
            float hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            // Smooth noise 2D
            float smoothNoise(float2 uv, float scale)
            {
                float2 i = floor(uv * scale);
                float2 f = frac(uv * scale);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));

                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            // SDF de rectángulo redondeado centrado en 0.5
            // Devuelve distancia: negativa = interior, positiva = exterior
            float roundedRectSDF(float2 uv, float r)
            {
                float2 d = abs(uv - 0.5) - (0.5 - r);
                return length(max(d, 0.0)) - r;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // ── Shape mask (esquinas redondeadas) ────────────
                float sdf      = roundedRectSDF(uv, _Radius);
                float panelMask = 1.0 - smoothstep(-_Feather, _Feather, sdf);
                if (panelMask < 0.001) discard;

                // ── Color base del panel ─────────────────────────
                float4 col = _PanelColor;

                // Micro-noise de textura
                float noise = smoothNoise(uv, _NoiseScale) * 2.0 - 1.0; // -1..1
                col.rgb += noise * _NoiseStrength;
                col.rgb  = saturate(col.rgb);

                // Degradado vertical sutil: parte superior ligeramente más opaca/clara
                col.a += uv.y * _TopAlphaBoost;

                // ── Borde ────────────────────────────────────────
                // sdf negativa = interior; 0 = borde exterior; positiva = fuera
                // Zona de borde: sdf ∈ (-_BorderWidth, 0)
                float innerSDF   = -(sdf + _BorderWidth);            // >0 en zona de borde
                float borderMask = smoothstep(-_Feather, _Feather, innerSDF)
                                 * (1.0 - smoothstep(-_Feather, 0, sdf)); // recortar por exterior

                // Glow exterior (difuminado fuera del panel)
                float glowSDF  = -sdf;                                // > 0 fuera
                float glowMask = (1.0 - smoothstep(0, _BorderGlow, glowSDF)) * (1.0 - panelMask);

                // Pulso animado en el borde
                float pulse    = 1.0 + _PulseAmplitude * sin(_Time.y * _PulseSpeed * 6.2831);
                float4 bColor  = _BorderColor;
                bColor.a      *= pulse;

                // Glow: mismo color, alpha reducido
                float4 glowColor = bColor;
                glowColor.a     *= _GlowIntensity;

                // ── Mezcla final ─────────────────────────────────
                // 1. Panel base
                fixed4 result = col;
                result.a     *= panelMask;

                // 2. Borde (over)
                result = lerp(result, bColor, borderMask * bColor.a);

                // 3. Glow exterior (add-blend simulado en alpha)
                result.rgb = lerp(result.rgb, glowColor.rgb, glowMask * glowColor.a);
                result.a   = max(result.a, glowMask * glowColor.a * 0.6);

                return result;
            }
            ENDCG
        }
    }
}
