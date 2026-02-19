using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ASL_LearnVR.UI
{
    /// <summary>
    /// Barra de progreso de categoría para la Escena 3.
    ///
    /// Muestra: [████████░░░░░░░]  12 / 26
    ///
    /// SETUP:
    ///   Estructura del prefab (World Space Canvas o Screen Space):
    ///
    ///   ProgressBar_Root
    ///   ├── Background   (Image, color gris neutro, alpha 0.25)
    ///   ├── Fill         (Image, ImageType=Filled, FillMethod=Horizontal, color índigo)
    ///   └── Label        (TextMeshProUGUI, "12 / 26", alineado a la derecha)
    ///
    ///   Arrastra Fill en fillImage y Label en progressLabel.
    ///   Llama a SetProgress(currentIndex, total) desde LearningController o CategoryNavigator.
    ///
    /// La barra anima suavemente hacia el nuevo valor con lerp.
    /// El color transiciona de índigo → verde menta al completar la categoría.
    /// </summary>
    [AddComponentMenu("ASL_LearnVR/UI/Category Progress Bar")]
    public class CategoryProgressBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image            fillImage;
        [SerializeField] private TextMeshProUGUI  progressLabel;     // "12 / 26"
        [SerializeField] private TextMeshProUGUI  percentLabel;      // "46%" (opcional)
        [SerializeField] private TextMeshProUGUI  categoryNameLabel; // nombre categoría (opcional)

        [Header("Colors")]
        [SerializeField] private Color colorStart    = new Color(0.29f, 0.44f, 0.83f); // índigo
        [SerializeField] private Color colorComplete = new Color(0.28f, 0.79f, 0.62f); // verde menta
        [Tooltip("Umbral para considerar 'completado' y cambiar a verde (0–1)")]
        [SerializeField] [Range(0.8f, 1f)] private float completeThreshold = 1.0f;

        [Header("Animation")]
        [SerializeField] private float fillSpeed = 4f;  // lerp speed

        // ─── Runtime ─────────────────────────────────────────────────────
        private float _targetFill   = 0f;
        private float _currentFill  = 0f;
        private int   _currentCount = 0;
        private int   _totalCount   = 0;

        // ─────────────────────────────────────────────────────────────────
        void Awake()
        {
            if (fillImage != null)
            {
                fillImage.type       = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillAmount = 0f;
                fillImage.color      = colorStart;
            }
            UpdateLabels();
        }

        void Update()
        {
            if (fillImage == null) return;

            // Animar fill
            _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * fillSpeed);
            fillImage.fillAmount = _currentFill;

            // Color: lerp entre índigo y verde según progreso
            float colorT     = Mathf.InverseLerp(0f, completeThreshold, _currentFill);
            fillImage.color  = Color.Lerp(colorStart, colorComplete, colorT);
        }

        // ─── API pública ──────────────────────────────────────────────────

        /// <summary>
        /// Actualiza la barra con el signo actual y el total.
        /// currentIndex es 0-based; se muestra como currentIndex+1.
        /// </summary>
        public void SetProgress(int currentIndex, int total)
        {
            if (total <= 0) return;

            _currentCount = Mathf.Clamp(currentIndex + 1, 0, total); // 1-based para display
            _totalCount   = total;
            _targetFill   = (float)currentIndex / total;

            UpdateLabels();
        }

        /// <summary>
        /// Actualiza el nombre de la categoría en el label opcional.
        /// </summary>
        public void SetCategoryName(string name)
        {
            if (categoryNameLabel != null)
                categoryNameLabel.text = name;
        }

        /// <summary>
        /// Resetea la barra a 0 (animación desde cero).
        /// </summary>
        public void Reset()
        {
            _targetFill   = 0f;
            _currentFill  = 0f;
            _currentCount = 0;
            if (fillImage != null) fillImage.fillAmount = 0f;
            UpdateLabels();
        }

        // ─── Helpers ─────────────────────────────────────────────────────
        private void UpdateLabels()
        {
            if (progressLabel != null)
            {
                progressLabel.text = _totalCount > 0
                    ? $"{_currentCount} / {_totalCount}"
                    : "— / —";
            }

            if (percentLabel != null && _totalCount > 0)
            {
                int pct = Mathf.RoundToInt(_targetFill * 100f);
                percentLabel.text = $"{pct}%";
            }
        }
    }
}
