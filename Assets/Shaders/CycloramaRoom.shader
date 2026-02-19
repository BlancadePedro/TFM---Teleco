Shader "ASL_LearnVR/CycloramaRoom"
{
    Properties
    {
        _FloorColor      ("Floor Color",      Color) = (0.92, 0.92, 0.91, 1)
        _WallColor       ("Wall Color",       Color) = (0.96, 0.96, 0.95, 1)
        _CeilingColor    ("Ceiling Color",    Color) = (0.98, 0.98, 0.97, 1)
        _FloorDarkColor  ("Floor Dark Edge",  Color) = (0.72, 0.72, 0.71, 1)
        
        // Curvatura en la union suelo-pared (el "sweep" del ciclorama)
        _SweepBlend      ("Sweep Blend Zone", Range(0.01, 1.5)) = 0.55
        _SweepSoftness   ("Sweep Softness",   Range(0.01, 1.0)) = 0.40
        
        // Degradado radial en el suelo (punto focal central)
        _FloorVignette   ("Floor Vignette Radius", Float) = 2.8
        _FloorVignetteSoft("Floor Vignette Soft", Range(0.1, 2.0)) = 1.2
        
        // Altura de referencia
        _FloorY          ("Floor Y",  Float) = 0.0
        _CeilingY        ("Ceiling Y",Float) = 3.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" }
        LOD 100
        Cull Front          // vemos el interior del cubo
        ZWrite On

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            // ---- propiedades ----
            float4 _FloorColor;
            float4 _WallColor;
            float4 _CeilingColor;
            float4 _FloorDarkColor;
            float  _SweepBlend;
            float  _SweepSoftness;
            float  _FloorVignette;
            float  _FloorVignetteSoft;
            float  _FloorY;
            float  _CeilingY;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 wp = i.worldPos;

                // Altura normalizada 0 (suelo) → 1 (techo)
                float height01 = saturate((wp.y - _FloorY) / max(0.001, _CeilingY - _FloorY));

                // ── SUELO ──────────────────────────────────────────────────
                // Degradado radial: centro claro (foco de la acción), bordes más grises
                float distXZ  = length(wp.xz);
                float vigT    = smoothstep(0, _FloorVignette, distXZ);
                float4 floorC = lerp(_FloorColor, _FloorDarkColor, pow(vigT, _FloorVignetteSoft));

                // ── PARED ──────────────────────────────────────────────────
                // Las paredes son neutras, ligeramente más oscuras que el techo
                float4 wallC = lerp(_WallColor, _CeilingColor, height01);

                // ── SWEEP (transición curvada suelo → pared) ──────────────
                // El sweep es la zona de fusión fotográfica sin esquina visible.
                // Se define como una banda alrededor de height01 ≈ 0.
                float sweepT = smoothstep(0, _SweepBlend, height01 + _SweepSoftness * 0.5);
                // sweepT = 0 → estamos en el suelo; = 1 → estamos en la pared/techo

                float4 col = lerp(floorC, wallC, sweepT);

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
