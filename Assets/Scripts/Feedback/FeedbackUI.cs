using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// World-space UI panel to show textual feedback to the user.
    /// Shows one or two correction messages prioritized by severity.
    /// </summary>
    public class FeedbackUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Main feedback message text")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Tooltip("Secondary text for optional extra message")]
        [SerializeField] private TextMeshProUGUI secondaryText;

        [Tooltip("Status icon (check/warning/error)")]
        [SerializeField] private Image statusIcon;

        [Tooltip("Feedback container panel")]
        [SerializeField] private GameObject feedbackPanel;

        [Tooltip("Success panel (briefly shown when the gesture completes)")]
        [SerializeField] private GameObject successPanel;

        [Header("Icons")]
        [Tooltip("Icon for correct state")]
        [SerializeField] private Sprite iconSuccess;

        [Tooltip("Icon for warning/minor adjustment")]
        [SerializeField] private Sprite iconWarning;

        [Tooltip("Icon for major error")]
        [SerializeField] private Sprite iconError;

        [Tooltip("Icon for in-progress state")]
        [SerializeField] private Sprite iconProgress;

        [Header("Colors")]
        [Tooltip("Color for correct state")]
        [SerializeField] private Color colorSuccess = new Color(0.2f, 0.8f, 0.2f);

        [Tooltip("Warning color")]
        [SerializeField] private Color colorWarning = new Color(1f, 0.3f, 0.3f);

        [Tooltip("Error color")]
        [SerializeField] private Color colorError = new Color(1f, 0.3f, 0.3f);

        [Tooltip("Color for neutral/in-progress state")]
        [SerializeField] private Color colorNeutral = Color.white;

        [Header("Animation")]
        [Tooltip("Fade in/out duration")]
        [SerializeField] private float fadeDuration = 0.2f;

        [Tooltip("Duration the success panel is shown")]
        [SerializeField] private float successDisplayDuration = 2f;

        // State actual
        private FeedbackState currentState = FeedbackState.Inactive;
        private CanvasGroup canvasGroup;
        private float successHideTime;

        void Awake()
        {
            // Use own GameObject as default panel if not assigned
            if (feedbackPanel == null)
                feedbackPanel = gameObject;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null && feedbackPanel != null)
            {
                canvasGroup = feedbackPanel.GetComponent<CanvasGroup>();
            }
        }

        void Start()
        {
            // Solo ocultar el panel de exito al inicio; el panel principal se controla desde FeedbackSystem.SetActive
            if (successPanel != null)
                successPanel.SetActive(false);
        }

        void Update()
        {
            // Auto-ocultar panel de exito despues del tiempo configured
            if (successPanel != null && successPanel.activeSelf &&
                Time.time > successHideTime)
            {
                successPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Muestra u oculta el panel de feedback.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (feedbackPanel != null)
                feedbackPanel.SetActive(visible);

            currentState = visible ? FeedbackState.Waiting : FeedbackState.Inactive;
        }

        /// <summary>
        /// Actualiza el feedback basandose en el resultado del analisis de gesto static.
        /// </summary>
        public void UpdateFromStaticResult(StaticGestureResult result)
        {
            // Ensure panel is visible when results are received
            if (feedbackPanel != null && !feedbackPanel.activeSelf)
                feedbackPanel.SetActive(true);

            if (result == null)
            {
                SetWaitingState();
                return;
            }

            // Solo mostrar exito cuando el recognizer confirma el match global
            if (result.isMatchGlobal)
            {
                SetSuccessState(result.summaryMessage);
            }
            else if (result.majorErrorCount > 0)
            {
                SetErrorState(result.summaryMessage);
            }
            else if (result.minorErrorCount > 0)
            {
                // No orange color: minor adjustments also shown in red
                SetErrorState(result.summaryMessage);
            }
            else
            {
                // Sin errores pero tampoco match global: usar mensaje generated (ej. "Hand not tracked")
                SetWaitingState(string.IsNullOrEmpty(result.summaryMessage)
                    ? "Make the sign to practice..."
                    : result.summaryMessage);
            }
        }

        /// <summary>
        /// Actualiza el feedback basandose en el resultado del analisis de dynamic gesture.
        /// </summary>
        public void UpdateFromDynamicResult(DynamicGestureResult result)
        {
            if (result == null)
                return;

            if (result.isSuccess)
            {
                ShowSuccessMessage($"'{result.gestureName}' completed!");
            }
            else
            {
                SetErrorState(result.troubleshootingMessage);
            }
        }

        /// <summary>
        /// Muestra progreso de un dynamic gesture.
        /// </summary>
        public void ShowDynamicProgress(string gestureName, float progress)
        {
            SetProgressState($"'{gestureName}' in progress... {Mathf.RoundToInt(progress * 100)}%");
        }

        /// <summary>
        /// Establece estado de espera (neutral).
        /// </summary>
        public void SetWaitingState(string message = "Make the sign to practice...")
        {
            currentState = FeedbackState.Waiting;
            SetText(message);
            SetStatusIcon(iconProgress, colorNeutral);
        }

        /// <summary>
        /// Establece estado de exito.
        /// </summary>
        public void SetSuccessState(string message = "Perfect! Correct position.")
        {
            currentState = FeedbackState.Success;
            SetText(message);
            SetStatusIcon(iconSuccess, colorSuccess);
        }

        /// <summary>
        /// Establece estado de advertencia (errores menores).
        /// </summary>
        public void SetWarningState(string message)
        {
            // Se usa el mismo tratamiento visual que error (rojo) para simplificar feedback
            currentState = FeedbackState.ShowingErrors;
            SetText(message);
            SetStatusIcon(iconError != null ? iconError : iconWarning, colorError);
        }

        /// <summary>
        /// Establece estado de error (errores mayores).
        /// </summary>
        public void SetErrorState(string message)
        {
            currentState = FeedbackState.ShowingErrors;
            SetText(message);
            SetStatusIcon(iconError, colorError);
        }

        /// <summary>
        /// Establece estado de progreso (dynamic gesture).
        /// </summary>
        public void SetProgressState(string message)
        {
            currentState = FeedbackState.InProgress;
            SetText(message);
            SetStatusIcon(iconProgress, colorNeutral);
        }

        /// <summary>
        /// Muestra mensaje de exito temporal.
        /// </summary>
        public void ShowSuccessMessage(string message)
        {
            if (successPanel != null)
            {
                // Enable panel de exito
                successPanel.SetActive(true);
                successHideTime = Time.time + successDisplayDuration;

                // Buscar texto en el panel de exito si existe
                var successText = successPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (successText != null)
                    successText.text = message;
            }
            else
            {
                // Fallback: usar panel principal
                SetSuccessState(message);
            }
        }

        /// <summary>
        /// Muestra mensaje de error especifico de dedo.
        /// </summary>
        public void ShowFingerErrors(FingerError[] errors, int maxDisplay = 2)
        {
            if (errors == null || errors.Length == 0)
            {
                SetSuccessState();
                return;
            }

            string message = FeedbackMessages.GenerateSummary(errors, maxDisplay);

            // Determinar severidad mas alta
            bool hasMajor = false;
            foreach (var e in errors)
            {
                if (e.severity == Severity.Major)
                {
                    hasMajor = true;
                    break;
                }
            }

            if (hasMajor)
                SetErrorState(message);
            else
                SetWarningState(message);
        }

        /// <summary>
        /// Establece el texto principal.
        /// </summary>
        private void SetText(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message;
        }

        /// <summary>
        /// Establece el texto secundario.
        /// </summary>
        public void SetSecondaryText(string message)
        {
            if (secondaryText != null)
            {
                secondaryText.gameObject.SetActive(!string.IsNullOrEmpty(message));
                secondaryText.text = message;
            }
        }

        /// <summary>
        /// Establece el icono y color de estado.
        /// </summary>
        private void SetStatusIcon(Sprite icon, Color color)
        {
            if (statusIcon != null)
            {
                if (icon != null)
                {
                    statusIcon.sprite = icon;
                    statusIcon.gameObject.SetActive(true);
                }
                else
                {
                    statusIcon.gameObject.SetActive(false);
                }

                statusIcon.color = color;
            }

            // NOTA: No cambiar el color del texto - siempre mantenerlo negro para legibilidad
            // El color solo se aplica al icono de estado
        }

        /// <summary>
        /// State actual del feedback.
        /// </summary>
        public FeedbackState CurrentState => currentState;

        /// <summary>
        /// Limpia el feedback y lo oculta.
        /// </summary>
        public void Clear()
        {
            SetText("");
            SetSecondaryText("");
            SetVisible(false);

            if (successPanel != null)
                successPanel.SetActive(false);
        }
    }
}
