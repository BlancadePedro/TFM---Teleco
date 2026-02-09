using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Panel UI en World Space para mostrar feedback textual al usuario.
    /// Muestra 1-2 mensajes de corrección priorizados según severidad.
    /// </summary>
    public class FeedbackUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Texto principal del mensaje de feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Tooltip("Texto secundario para mensaje adicional (opcional)")]
        [SerializeField] private TextMeshProUGUI secondaryText;

        [Tooltip("Icono de estado (check/warning/error)")]
        [SerializeField] private Image statusIcon;

        [Tooltip("Panel contenedor del feedback")]
        [SerializeField] private GameObject feedbackPanel;

        [Tooltip("Panel de éxito (se muestra brevemente al completar gesto)")]
        [SerializeField] private GameObject successPanel;

        [Header("Icons")]
        [Tooltip("Icono para estado correcto")]
        [SerializeField] private Sprite iconSuccess;

        [Tooltip("Icono para advertencia/ajuste menor")]
        [SerializeField] private Sprite iconWarning;

        [Tooltip("Icono para error mayor")]
        [SerializeField] private Sprite iconError;

        [Tooltip("Icono para estado en progreso")]
        [SerializeField] private Sprite iconProgress;

        [Header("Colors")]
        [Tooltip("Color para estado correcto")]
        [SerializeField] private Color colorSuccess = new Color(0.2f, 0.8f, 0.2f);

        [Tooltip("Color para advertencia")]
        [SerializeField] private Color colorWarning = new Color(1f, 0.3f, 0.3f);

        [Tooltip("Color para error")]
        [SerializeField] private Color colorError = new Color(1f, 0.3f, 0.3f);

        [Tooltip("Color para estado neutral/en progreso")]
        [SerializeField] private Color colorNeutral = Color.white;

        [Header("Animation")]
        [Tooltip("Duración del fade in/out")]
        [SerializeField] private float fadeDuration = 0.2f;

        [Tooltip("Duración que se muestra el panel de éxito")]
        [SerializeField] private float successDisplayDuration = 2f;

        // Estado actual
        private FeedbackState currentState = FeedbackState.Inactive;
        private CanvasGroup canvasGroup;
        private float successHideTime;

        void Awake()
        {
            // Usar el propio GameObject como panel por defecto si no se asignÃ³
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
            // Solo ocultar el panel de éxito al inicio; el panel principal se controla desde FeedbackSystem.SetActive
            if (successPanel != null)
                successPanel.SetActive(false);
        }

        void Update()
        {
            // Auto-ocultar panel de éxito después del tiempo configurado
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
        /// Actualiza el feedback basándose en el resultado del análisis de gesto estático.
        /// </summary>
        public void UpdateFromStaticResult(StaticGestureResult result)
        {
            // Asegurar que el panel estÃ© visible cuando recibimos resultados
            if (feedbackPanel != null && !feedbackPanel.activeSelf)
                feedbackPanel.SetActive(true);

            if (result == null)
            {
                SetWaitingState();
                return;
            }

            // Solo mostrar éxito cuando el recognizer confirma el match global
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
                // Sin color naranja: los ajustes menores tambiÃ©n se muestran en rojo
                SetErrorState(result.summaryMessage);
            }
            else
            {
                // Sin errores pero tampoco match global: usar mensaje generado (ej. "Hand not tracked")
                SetWaitingState(string.IsNullOrEmpty(result.summaryMessage)
                    ? "Haz el signo para practicar..."
                    : result.summaryMessage);
            }
        }

        /// <summary>
        /// Actualiza el feedback basándose en el resultado del análisis de gesto dinámico.
        /// </summary>
        public void UpdateFromDynamicResult(DynamicGestureResult result)
        {
            if (result == null)
                return;

            if (result.isSuccess)
            {
                ShowSuccessMessage($"¡'{result.gestureName}' completado!");
            }
            else
            {
                SetErrorState(result.troubleshootingMessage);
            }
        }

        /// <summary>
        /// Muestra progreso de un gesto dinámico.
        /// </summary>
        public void ShowDynamicProgress(string gestureName, float progress)
        {
            SetProgressState($"'{gestureName}' en progreso... {Mathf.RoundToInt(progress * 100)}%");
        }

        /// <summary>
        /// Establece estado de espera (neutral).
        /// </summary>
        public void SetWaitingState(string message = "Haz el signo para practicar...")
        {
            currentState = FeedbackState.Waiting;
            SetText(message);
            SetStatusIcon(iconProgress, colorNeutral);
        }

        /// <summary>
        /// Establece estado de éxito.
        /// </summary>
        public void SetSuccessState(string message = "¡Perfecto! Posición correcta.")
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
        /// Establece estado de progreso (gesto dinámico).
        /// </summary>
        public void SetProgressState(string message)
        {
            currentState = FeedbackState.InProgress;
            SetText(message);
            SetStatusIcon(iconProgress, colorNeutral);
        }

        /// <summary>
        /// Muestra mensaje de éxito temporal.
        /// </summary>
        public void ShowSuccessMessage(string message)
        {
            if (successPanel != null)
            {
                // Activar panel de éxito
                successPanel.SetActive(true);
                successHideTime = Time.time + successDisplayDuration;

                // Buscar texto en el panel de éxito si existe
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
        /// Muestra mensaje de error específico de dedo.
        /// </summary>
        public void ShowFingerErrors(FingerError[] errors, int maxDisplay = 2)
        {
            if (errors == null || errors.Length == 0)
            {
                SetSuccessState();
                return;
            }

            string message = FeedbackMessages.GenerateSummary(errors, maxDisplay);

            // Determinar severidad más alta
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
        /// Estado actual del feedback.
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
