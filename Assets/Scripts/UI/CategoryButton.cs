using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ASL_LearnVR.UI
{
    /// <summary>
    /// Botón de categoría/nivel con microinteracciones completas:
    ///   - Escala 1.02 en hover, 0.98 en click
    ///   - Intensificación de alpha del borde en hover
    ///   - Indicador activo (punto azul índigo) cuando está seleccionado
    ///   - Integración con PanelInteractionController para el borde reactivo
    ///
    /// Estructura esperada del prefab:
    ///   [Button] (este componente + Image con material FrostedGlassPanel)
    ///   ├── Icon (Image, opcional)
    ///   ├── Label (TextMeshProUGUI)
    ///   ├── Subtitle (TextMeshProUGUI, opcional)
    ///   ├── ActiveIndicator (Image, punto circular azul índigo)
    ///   └── ProgressBar (Image, fill, opcional)
    /// </summary>
    [AddComponentMenu("ASL_LearnVR/UI/Category Button")]
    [RequireComponent(typeof(Button))]
    public class CategoryButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        // ─── Inspector ────────────────────────────────────────────────────
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Image           activeIndicator;    // punto azul índigo
        [SerializeField] private Image           progressBar;        // fill image
        [SerializeField] private Image           iconImage;

        [Header("State")]
        [SerializeField] private bool isActive = false;
        [SerializeField] [Range(0f,1f)] private float progress = 0f;

        [Header("Scale Animation")]
        [SerializeField] private float hoverScale    = 1.02f;
        [SerializeField] private float clickScale    = 0.98f;
        [SerializeField] private float scaleDuration = 0.10f;

        [Header("Colors")]
        [SerializeField] private Color labelIdleColor   = new Color(0.11f, 0.11f, 0.13f); // #1A1A22
        [SerializeField] private Color labelHoverColor  = new Color(0.05f, 0.05f, 0.08f); // casi negro
        [SerializeField] private Color indicatorColor   = new Color(0.29f, 0.44f, 0.83f); // #4A6FD4
        [SerializeField] private Color progressColor    = new Color(0.29f, 0.44f, 0.83f, 0.7f);

        // ─── Runtime ─────────────────────────────────────────────────────
        private PanelInteractionController _panel;
        private Button                     _button;
        private Vector3                    _baseScale;
        private Coroutine                  _scaleCoroutine;
        private bool                       _isPointerOver;

        // ─────────────────────────────────────────────────────────────────
        void Awake()
        {
            _button    = GetComponent<Button>();
            _panel     = GetComponent<PanelInteractionController>();
            _baseScale = transform.localScale;
        }

        void Start()
        {
            ApplyInitialState();
        }

        // ─── Pointer events ───────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData e)
        {
            _isPointerOver = true;
            _panel?.SetState(PanelInteractionController.BorderState.Hover);
            ScaleTo(hoverScale);
            SetLabelColor(labelHoverColor);
        }

        public void OnPointerExit(PointerEventData e)
        {
            _isPointerOver = false;
            _panel?.SetState(PanelInteractionController.BorderState.Idle);
            ScaleTo(1f);
            SetLabelColor(labelIdleColor);
        }

        public void OnPointerDown(PointerEventData e)
        {
            _panel?.SetState(PanelInteractionController.BorderState.Click);
            ScaleTo(clickScale);
        }

        public void OnPointerUp(PointerEventData e)
        {
            ScaleTo(_isPointerOver ? hoverScale : 1f);
            _panel?.SetState(_isPointerOver
                ? PanelInteractionController.BorderState.Hover
                : PanelInteractionController.BorderState.Idle);
        }

        // ─── API pública ──────────────────────────────────────────────────
        public void SetLabel(string label, string subtitle = "")
        {
            if (labelText    != null) labelText.text    = label;
            if (subtitleText != null) subtitleText.text = subtitle;
        }

        public void SetActive(bool active)
        {
            isActive = active;
            if (activeIndicator != null)
            {
                activeIndicator.gameObject.SetActive(active);
                activeIndicator.color = indicatorColor;
            }
        }

        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            if (progressBar != null)
            {
                progressBar.fillAmount = progress;
                progressBar.color = progressColor;
                progressBar.gameObject.SetActive(progress > 0f);
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────
        private void ApplyInitialState()
        {
            SetLabelColor(labelIdleColor);
            SetActive(isActive);
            SetProgress(progress);

            if (activeIndicator != null)
                activeIndicator.color = indicatorColor;
        }

        private void SetLabelColor(Color c)
        {
            if (labelText    != null) labelText.color    = c;
            if (subtitleText != null) subtitleText.color = new Color(c.r, c.g, c.b, 0.65f);
        }

        private void ScaleTo(float target)
        {
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(ScaleRoutine(_baseScale * target));
        }

        private IEnumerator ScaleRoutine(Vector3 target)
        {
            Vector3 start = transform.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / scaleDuration;
                transform.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            transform.localScale = target;
        }
    }
}
