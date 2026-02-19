using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ASL_LearnVR.UI
{
    /// <summary>
    /// Barra de progreso visual estilizada — signos conseguidos vs. total.
    /// Diseño: [XX%][████████░░░░░] N / Total
    ///
    /// Compatible con Escena 3 (Learning) y Escena 4 (Self Assessment).
    ///
    /// ── ESCENA 4 ──────────────────────────────────────────────────────────
    /// El SelfAssessmentController ya tiene un Slider en [progressBar].
    /// OPCIÓN A (recomendada, sin tocar código existente):
    ///   1. Crea el prefab visual (ver SETUP más abajo).
    ///   2. Añade este componente al root del prefab.
    ///   3. En el Inspector de SelfAssessmentController, DEJA progressBar vacío.
    ///   4. Añade el componente ProgressBarBridge (abajo en este archivo)
    ///      al mismo GameObject que SelfAssessmentController.
    ///   5. Arrastra este componente a ProgressBarBridge.targetBar.
    ///
    /// OPCIÓN B (si prefieres reemplazar el Slider directamente):
    ///   Arrastra este componente en el slot [progressBar] del SelfAssessmentController
    ///   → NO funciona porque espera un Slider. Usa la Opción A.
    ///
    /// ── ESCENA 3 ──────────────────────────────────────────────────────────
    /// Usa CategoryProgressBar.cs (ya generado) que llama a SetProgress() aquí.
    /// O usa este componente directamente y llama SetProgress() desde CategoryNavigator.
    ///
    /// ── SETUP DEL PREFAB ─────────────────────────────────────────────────
    ///
    ///   [ProgressBar_Root]  (RectTransform, anchors stretch horizontal)
    ///   ├── [PercentBadge]  (Image redondeada, color índigo)  ← percentLabel
    ///   │     └── Text      (TMP "46%", blanco, bold 18pt)
    ///   ├── [Track]         (Image, color gris claro #E8E8E8, rounded corners)
    ///   │     └── [Fill]    (Image, Type=Filled, Horizontal, color índigo→menta)
    ///   └── [CountLabel]    (TMP "12 / 26", gris oscuro, alineado derecha)
    ///
    ///   Tamaños sugeridos (World Space Canvas a 1m de distancia):
    ///     ProgressBar_Root: 500 × 36 px
    ///     PercentBadge:      72 × 36 px
    ///     Track:            390 × 28 px  (margen 4px arriba/abajo)
    ///     Fill:             100% height del Track
    ///     CountLabel:        80 × 36 px
    /// </summary>
    [AddComponentMenu("ASL_LearnVR/UI/Session Progress Bar")]
    public class SessionProgressBar : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────
        [Header("UI References")]
        [Tooltip("Texto del porcentaje en el badge izquierdo  (ej: '46%')")]
        [SerializeField] private TextMeshProUGUI percentLabel;

        [Tooltip("Image con Type=Filled, FillMethod=Horizontal — la barra en sí")]
        [SerializeField] private Image fillImage;

        [Tooltip("Texto 'N / Total' a la derecha de la barra")]
        [SerializeField] private TextMeshProUGUI countLabel;

        [Tooltip("Image del badge del porcentaje (para cambiar color cuando completa)")]
        [SerializeField] private Image percentBadge;

        [Header("Colors")]
        [SerializeField] private Color colorProgress  = new Color(0.29f, 0.44f, 0.83f, 1f); // índigo #4A6FD4
        [SerializeField] private Color colorComplete  = new Color(0.28f, 0.79f, 0.62f, 1f); // menta  #48C99E
        [SerializeField] private Color colorTrack     = new Color(0.91f, 0.91f, 0.91f, 0.6f);
        [Tooltip("Umbral fill para cambiar a color 'completado' (0–1)")]
        [SerializeField] [Range(0.9f, 1f)] private float completeThreshold = 1.0f;

        [Header("Animation")]
        [SerializeField] private float fillLerpSpeed = 5f;
        [SerializeField] private bool  animateOnSet  = true;

        [Header("Track Background")]
        [SerializeField] private Image trackImage;

        // ─── Runtime ─────────────────────────────────────────────────────
        private float _targetFill   = 0f;
        private float _currentFill  = 0f;
        private int   _completed    = 0;
        private int   _total        = 0;
        private bool  _initialized  = false;

        // ─────────────────────────────────────────────────────────────────
        void Awake()
        {
            if (fillImage != null)
            {
                fillImage.type       = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillAmount = 0f;
                fillImage.color      = colorProgress;
            }
            if (trackImage != null) trackImage.color = colorTrack;

            UpdateLabels(0, 0);
            _initialized = true;
        }

        void Update()
        {
            if (!_initialized || fillImage == null) return;

            // Animar fill suavemente
            _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * fillLerpSpeed);
            fillImage.fillAmount = _currentFill;

            // Color dinámico: índigo → menta según progreso
            float t          = Mathf.InverseLerp(0f, completeThreshold, _currentFill);
            Color barColor   = Color.Lerp(colorProgress, colorComplete, t);
            fillImage.color  = barColor;

            if (percentBadge != null) percentBadge.color = barColor;
        }

        // ─── API pública ──────────────────────────────────────────────────

        /// <summary>
        /// Actualiza la barra. completedSigns = cuántos ha hecho bien; total = todos los de la categoría.
        /// </summary>
        public void SetProgress(int completedSigns, int total)
        {
            if (total <= 0) return;

            _completed   = completedSigns;
            _total       = total;
            _targetFill  = (float)completedSigns / total;

            UpdateLabels(completedSigns, total);

            // Si no estamos en Update aún (llamada desde Awake externo), aplicar directo
            if (!animateOnSet || !gameObject.activeInHierarchy)
            {
                _currentFill = _targetFill;
                if (fillImage != null) fillImage.fillAmount = _currentFill;
            }
        }

        /// <summary>Resetea a 0 con animación.</summary>
        public void Reset()
        {
            _targetFill  = 0f;
            _completed   = 0;
            UpdateLabels(0, _total);
        }

        /// <summary>Flash de "correcto" — incrementa en 1 y hace un breve destello.</summary>
        public void IncrementAndFlash()
        {
            SetProgress(_completed + 1, _total);
            if (gameObject.activeInHierarchy)
                StartCoroutine(FlashFill());
        }

        // ─── Helpers ─────────────────────────────────────────────────────
        private void UpdateLabels(int done, int total)
        {
            if (percentLabel != null)
            {
                int pct = total > 0 ? Mathf.RoundToInt((float)done / total * 100f) : 0;
                percentLabel.text = $"{pct}%";
            }

            if (countLabel != null)
                countLabel.text = total > 0 ? $"{done} / {total}" : "— / —";
        }

        private IEnumerator FlashFill()
        {
            // Breve destello blanco en el fill
            Color original = fillImage.color;
            Color flash    = Color.white;
            float t        = 0f;
            float dur      = 0.18f;

            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                fillImage.color = Color.Lerp(flash, original, t);
                yield return null;
            }
            fillImage.color = original;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  BRIDGE — conecta SelfAssessmentController con SessionProgressBar
    //  sin tocar el código existente de SelfAssessmentController
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Puente ligero que observa el estado de SelfAssessmentController
    /// y alimenta a SessionProgressBar cada vez que cambia el progreso.
    ///
    /// SETUP:
    ///   1. Añade este componente al mismo GameObject que SelfAssessmentController.
    ///   2. Arrastra el SessionProgressBar al campo targetBar.
    ///   3. Listo. El bridge llama a targetBar.SetProgress() en cada frame
    ///      detectando cambios en completed/total.
    ///
    /// El polling es muy barato (2 int comparisons por frame).
    /// </summary>
    [AddComponentMenu("ASL_LearnVR/UI/Progress Bar Bridge")]
    public class ProgressBarBridge : MonoBehaviour
    {
        [SerializeField] private SessionProgressBar targetBar;

        // ─── Runtime ─────────────────────────────────────────────────────
        private ASL_LearnVR.SelfAssessment.SelfAssessmentController _controller;
        private int _lastCompleted = -1;
        private int _lastTotal     = -1;

        void Start()
        {
            _controller = GetComponent<ASL_LearnVR.SelfAssessment.SelfAssessmentController>();
            if (_controller == null)
                Debug.LogWarning("[ProgressBarBridge] No se encontró SelfAssessmentController en el mismo GameObject.");
        }

        void Update()
        {
            if (_controller == null || targetBar == null) return;

            int done  = _controller.CompletedCount;
            int total = _controller.TotalCount;

            if (done != _lastCompleted || total != _lastTotal)
            {
                _lastCompleted = done;
                _lastTotal     = total;
                targetBar.SetProgress(done, total);
            }
        }
    }
}
