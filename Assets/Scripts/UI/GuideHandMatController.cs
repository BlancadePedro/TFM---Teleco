using UnityEngine;

namespace ASL_LearnVR.UI
{
    /// <summary>
    /// Controla el mantel de cristal acrílico donde descansa la mano guía.
    ///
    /// Coloca este script en el GameObject del mantel (Quad/Plane con material AcrylicMat).
    /// El mantel hace un pulso muy suave en el borde cuando la guía está activa,
    /// y se desvanece a alpha mínimo cuando la mano guía se oculta.
    ///
    /// SETUP:
    ///   1. Crea un Quad 3D (o Plane muy plano): escala (0.35, 1, 0.25) aprox.
    ///   2. Rótalo 90° en X para que quede horizontal sobre la mesa.
    ///   3. Colócalo encima de la mesa, en el punto donde aparece la mano guía.
    ///   4. Asígnale el material con shader ASL_LearnVR/AcrylicMat.
    ///   5. Añade este script y arrastra el Renderer.
    ///   6. (Opcional) Arrastra el GuideHandPoseApplier para auto-detectar estado.
    /// </summary>
    [AddComponentMenu("ASL_LearnVR/UI/Guide Hand Mat Controller")]
    public class GuideHandMatController : MonoBehaviour
    {
        [Header("Renderer")]
        [SerializeField] private Renderer matRenderer;

        [Header("Idle State")]
        [Tooltip("Alpha del panel cuando no hay mano guía activa")]
        [SerializeField] [Range(0f, 0.4f)] private float idleAlpha = 0.10f;

        [Header("Active State")]
        [Tooltip("Alpha del panel cuando la mano guía está visible")]
        [SerializeField] [Range(0.1f, 0.5f)] private float activeAlpha = 0.22f;

        [Tooltip("Pulso sutil del borde cuando la mano guía está activa")]
        [SerializeField] private bool enableActivePulse = true;
        [SerializeField] [Range(0.5f, 3f)] private float pulseFrequency = 1.1f;
        [SerializeField] [Range(0f, 0.25f)] private float pulseAmplitude = 0.12f;

        [Header("Transition")]
        [SerializeField] private float transitionSpeed = 3f;

        [Header("Label (opcional)")]
        [Tooltip("TextMeshPro WorldSpace sobre el mantel (ej: 'MANO GUÍA')")]
        [SerializeField] private TMPro.TextMeshPro labelText;
        [SerializeField] private string labelContent = "MANO GUÍA";
        [SerializeField] [Range(0f, 1f)] private float labelAlphaActive = 0.55f;
        [SerializeField] [Range(0f, 1f)] private float labelAlphaIdle   = 0.20f;

        // ─── Runtime ─────────────────────────────────────────────────────
        private Material _mat;
        private float    _targetAlpha;
        private float    _currentAlpha;
        private bool     _isActive = false;

        private static readonly int _BaseColorID   = Shader.PropertyToID("_BaseColor");
        private static readonly int _BorderColorID = Shader.PropertyToID("_BorderColor");

        // ─────────────────────────────────────────────────────────────────
        void Awake()
        {
            if (matRenderer == null) matRenderer = GetComponent<Renderer>();
            if (matRenderer != null)
            {
                _mat = matRenderer.material; // instancia propia
                _currentAlpha = idleAlpha;
                ApplyAlpha(_currentAlpha, 0f);
            }

            if (labelText != null)
            {
                labelText.text = labelContent;
                SetLabelAlpha(labelAlphaIdle);
            }
        }

        void Update()
        {
            if (_mat == null) return;

            // Calcular alpha objetivo
            float baseAlpha   = _isActive ? activeAlpha : idleAlpha;
            float pulseOffset = 0f;

            if (_isActive && enableActivePulse)
                pulseOffset = pulseAmplitude * Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f);

            _targetAlpha = baseAlpha + pulseOffset;

            // Lerp suave
            _currentAlpha = Mathf.Lerp(_currentAlpha, baseAlpha, Time.deltaTime * transitionSpeed);
            float displayAlpha = _currentAlpha + (_isActive && enableActivePulse
                ? pulseAmplitude * Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) * 0.5f
                : 0f);

            ApplyAlpha(displayAlpha, _isActive ? pulseOffset * 0.5f : 0f);

            // Label alpha
            if (labelText != null)
            {
                float targetLabelAlpha = _isActive ? labelAlphaActive : labelAlphaIdle;
                Color lc = labelText.color;
                lc.a = Mathf.Lerp(lc.a, targetLabelAlpha, Time.deltaTime * transitionSpeed);
                labelText.color = lc;
            }
        }

        // ─── API pública ──────────────────────────────────────────────────

        /// <summary>Activa el estado "mano guía visible" con pulso de borde</summary>
        public void SetActive(bool active)
        {
            _isActive = active;
        }

        /// <summary>Activa directamente</summary>
        public void Show() => SetActive(true);

        /// <summary>Desactiva (alpha mínimo)</summary>
        public void Hide() => SetActive(false);

        // ─── Helpers ─────────────────────────────────────────────────────
        private void ApplyAlpha(float baseAlpha, float borderBoost)
        {
            if (_mat == null) return;

            // Panel base
            Color bc = _mat.GetColor(_BaseColorID);
            bc.a     = Mathf.Clamp01(baseAlpha);
            _mat.SetColor(_BaseColorID, bc);

            // Borde — alpha un poco mayor que el panel
            Color brc = _mat.GetColor(_BorderColorID);
            brc.a     = Mathf.Clamp01(baseAlpha * 2.8f + borderBoost);
            _mat.SetColor(_BorderColorID, brc);
        }

        private void SetLabelAlpha(float a)
        {
            if (labelText == null) return;
            Color c = labelText.color;
            c.a = a;
            labelText.color = c;
        }
    }
}
