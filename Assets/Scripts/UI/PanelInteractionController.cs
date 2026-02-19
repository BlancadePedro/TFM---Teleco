using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ASL_LearnVR.UI
{
    /// <summary>
    /// Gestiona microinteracciones y estados de borde del panel de vidrio esmerilado.
    ///
    /// Estados de borde:
    ///   Idle    → azul índigo  #4A6FD4  alpha 0.50
    ///   Hover   → azul índigo  #4A6FD4  alpha 0.75  + escala 1.02
    ///   Click   → azul índigo  #4A6FD4  alpha 0.90  + escala 0.98
    ///   Pending → ámbar        #F5A623  alpha 0.70  + pulso lento
    ///   Correct → verde menta  #48C99E  alpha 0.90  + flash
    ///
    /// Adjuntar a cualquier GameObject que tenga un Renderer con el material
    /// "ASL_LearnVR/FrostedGlassPanel".  Para los botones de UI, se puede
    /// adjuntar a la Image que actúa como background del botón.
    /// </summary>
    [AddComponentMenu("ASL_LearnVR/UI/Panel Interaction Controller")]
    public class PanelInteractionController : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        // ─────────────────────────────────────────────────────────────────
        //  Inspector
        // ─────────────────────────────────────────────────────────────────
        [Header("Material Reference")]
        [Tooltip("Deja vacío para buscarlo automáticamente en este GameObject")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Image    targetImage;     // alternativa para UI Canvas

        [Header("Hover / Click")]
        [SerializeField] private float hoverScale    = 1.02f;
        [SerializeField] private float clickScale    = 0.98f;
        [SerializeField] private float scaleDuration = 0.12f;

        [Header("Border Colors")]
        [SerializeField] private Color idleColor    = new Color(0.290f, 0.435f, 0.831f, 0.50f); // #4A6FD4
        [SerializeField] private Color hoverColor   = new Color(0.290f, 0.435f, 0.831f, 0.75f);
        [SerializeField] private Color clickColor   = new Color(0.290f, 0.435f, 0.831f, 0.90f);
        [SerializeField] private Color pendingColor = new Color(0.961f, 0.651f, 0.137f, 0.70f); // #F5A623
        [SerializeField] private Color correctColor = new Color(0.282f, 0.788f, 0.620f, 0.90f); // #48C99E

        [Header("Border Animation")]
        [SerializeField] private float colorTransitionSpeed = 6f;   // lerp por frame
        [SerializeField] private float correctFlashDuration = 0.6f;
        [SerializeField] private float pendingPulseSpeed    = 1.4f;  // Hz

        [Header("Panel Base Alpha")]
        [SerializeField] private float idlePanelAlpha   = 0.12f;
        [SerializeField] private float hoverPanelAlpha  = 0.16f;

        // ─────────────────────────────────────────────────────────────────
        //  Runtime state
        // ─────────────────────────────────────────────────────────────────
        public enum BorderState { Idle, Hover, Click, Pending, Correct }

        private BorderState   _state          = BorderState.Idle;
        private Material      _mat;
        private Vector3       _baseScale;
        private Color         _currentBorder;
        private float         _currentPanelAlpha;
        private bool          _isPointerOver   = false;
        private Coroutine     _scaleCoroutine;
        private Coroutine     _flashCoroutine;

        // Shader property IDs (cacheados para rendimiento)
        private static readonly int _BorderColorID   = Shader.PropertyToID("_BorderColor");
        private static readonly int _PanelColorID    = Shader.PropertyToID("_PanelColor");
        private static readonly int _PulseSpeedID    = Shader.PropertyToID("_PulseSpeed");
        private static readonly int _PulseAmplitudeID= Shader.PropertyToID("_PulseAmplitude");

        // ─────────────────────────────────────────────────────────────────
        //  Init
        // ─────────────────────────────────────────────────────────────────
        void Awake()
        {
            _baseScale = transform.localScale;

            // Buscar material
            if (targetRenderer != null)
            {
                _mat = targetRenderer.material;           // instancia propia
            }
            else if (targetImage != null)
            {
                _mat = targetImage.material;
            }
            else
            {
                var r = GetComponent<Renderer>();
                if (r != null) { _mat = r.material; targetRenderer = r; }
                var img = GetComponent<Image>();
                if (img != null) { _mat = img.material; targetImage = img; }
            }

            if (_mat == null)
                Debug.LogWarning($"[PanelInteractionController] No se encontró material en {gameObject.name}");

            _currentBorder     = idleColor;
            _currentPanelAlpha = idlePanelAlpha;
        }

        void Update()
        {
            UpdateBorderColor();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Pointer events (XR Ray + legacy UI pointer)
        // ─────────────────────────────────────────────────────────────────
        public void OnPointerEnter(PointerEventData e)
        {
            _isPointerOver = true;
            if (_state == BorderState.Idle)
                SetState(BorderState.Hover);
        }

        public void OnPointerExit(PointerEventData e)
        {
            _isPointerOver = false;
            if (_state == BorderState.Hover || _state == BorderState.Click)
                SetState(BorderState.Idle);
        }

        public void OnPointerDown(PointerEventData e)
        {
            SetState(BorderState.Click);
            StartScaleTo(clickScale);
        }

        public void OnPointerUp(PointerEventData e)
        {
            StartScaleTo(_isPointerOver ? hoverScale : 1f);
            SetState(_isPointerOver ? BorderState.Hover : BorderState.Idle);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Estado público (llámalo desde FeedbackUI / GestureRecognizer)
        // ─────────────────────────────────────────────────────────────────
        public void SetState(BorderState newState)
        {
            if (_state == newState) return;
            _state = newState;

            // Detener flash anterior si existía
            if (_flashCoroutine != null) { StopCoroutine(_flashCoroutine); _flashCoroutine = null; }

            switch (_state)
            {
                case BorderState.Idle:
                    SetPulse(0f, 0f);
                    _currentPanelAlpha = idlePanelAlpha;
                    break;

                case BorderState.Hover:
                    SetPulse(0f, 0f);
                    StartScaleTo(hoverScale);
                    _currentPanelAlpha = hoverPanelAlpha;
                    break;

                case BorderState.Click:
                    SetPulse(0f, 0f);
                    _currentPanelAlpha = hoverPanelAlpha;
                    break;

                case BorderState.Pending:
                    SetPulse(pendingPulseSpeed, 0.25f);
                    _currentPanelAlpha = idlePanelAlpha;
                    break;

                case BorderState.Correct:
                    SetPulse(0f, 0f);
                    _flashCoroutine = StartCoroutine(CorrectFlash());
                    _currentPanelAlpha = hoverPanelAlpha;
                    break;
            }
        }

        // ── Shortcuts públicos ──────────────────────────────────────────
        public void SetPending() => SetState(BorderState.Pending);
        public void SetCorrect() => SetState(BorderState.Correct);
        public void SetIdle()    => SetState(BorderState.Idle);

        // ─────────────────────────────────────────────────────────────────
        //  Animación de borde
        // ─────────────────────────────────────────────────────────────────
        private void UpdateBorderColor()
        {
            if (_mat == null) return;

            Color target = GetTargetBorderColor();
            _currentBorder = Color.Lerp(_currentBorder, target, Time.deltaTime * colorTransitionSpeed);
            _mat.SetColor(_BorderColorID, _currentBorder);

            // Actualizar alpha del panel base
            Color panelCol = _mat.GetColor(_PanelColorID);
            panelCol.a = Mathf.Lerp(panelCol.a, _currentPanelAlpha, Time.deltaTime * colorTransitionSpeed);
            _mat.SetColor(_PanelColorID, panelCol);
        }

        private Color GetTargetBorderColor()
        {
            return _state switch
            {
                BorderState.Hover   => hoverColor,
                BorderState.Click   => clickColor,
                BorderState.Pending => pendingColor,
                BorderState.Correct => correctColor,
                _                   => idleColor,
            };
        }

        private void SetPulse(float speed, float amplitude)
        {
            if (_mat == null) return;
            _mat.SetFloat(_PulseSpeedID, speed);
            _mat.SetFloat(_PulseAmplitudeID, amplitude);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Escala
        // ─────────────────────────────────────────────────────────────────
        private void StartScaleTo(float targetScale)
        {
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(ScaleTo(_baseScale * targetScale));
        }

        private IEnumerator ScaleTo(Vector3 target)
        {
            Vector3 start = transform.localScale;
            float   t     = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / scaleDuration;
                transform.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.localScale = target;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Flash de "correcto"
        // ─────────────────────────────────────────────────────────────────
        private IEnumerator CorrectFlash()
        {
            // Breve destello: aumenta alpha del borde inmediatamente
            if (_mat != null) _mat.SetColor(_BorderColorID, correctColor);
            yield return new WaitForSeconds(correctFlashDuration);
            // Volver a idle después del feedback
            SetState(BorderState.Idle);
        }
    }
}
