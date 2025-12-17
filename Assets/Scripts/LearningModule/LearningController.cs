using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL_LearnVR.Core;
using ASL_LearnVR.Data;
using ASL_LearnVR.Gestures;

namespace ASL_LearnVR.LearningModule
{
    /// <summary>
    /// Controla el módulo de aprendizaje donde el usuario aprende signos individuales.
    /// Permite repetir el gesto con ghost hands y practicar con feedback en tiempo real.
    /// </summary>
    public class LearningController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Texto que muestra el nombre del signo actual")]
        [SerializeField] private TextMeshProUGUI signNameText;

        [Tooltip("Texto que muestra la descripción del signo")]
        [SerializeField] private TextMeshProUGUI signDescriptionText;

        [Tooltip("Botón 'Repetir' que muestra las ghost hands")]
        [SerializeField] private Button repeatButton;

        [Tooltip("Botón 'Practicar' que activa el feedback en tiempo real")]
        [SerializeField] private Button practiceButton;

        [Tooltip("Botón para ir al modo autoevaluación")]
        [SerializeField] private Button selfAssessmentButton;

        [Tooltip("Botón para volver a la selección de nivel")]
        [SerializeField] private Button backButton;

        [Header("Components")]
        [Tooltip("Componente GhostHandPlayer")]
        [SerializeField] private GhostHandPlayer ghostHandPlayer;

        [Tooltip("Componente GestureRecognizer para la mano derecha")]
        [SerializeField] private GestureRecognizer rightHandRecognizer;

        [Tooltip("Componente GestureRecognizer para la mano izquierda")]
        [SerializeField] private GestureRecognizer leftHandRecognizer;

        [Tooltip("Componente DynamicGestureRecognizer para gestos dinámicos (J, Z)")]
        [SerializeField] private DynamicGestureRecognizer dynamicGestureRecognizer;

        [Header("Feedback UI")]
        [Tooltip("Panel que muestra feedback durante la práctica")]
        [SerializeField] private GameObject feedbackPanel;

        [Tooltip("Texto del feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Tooltip("Texto que muestra el estado de grabación (RECORDING, WAITING, etc.)")]
        [SerializeField] private TextMeshProUGUI recordingStatusText;

        [Header("Navigation")]
        [Tooltip("Botón para ir al siguiente signo")]
        [SerializeField] private Button nextSignButton;

        [Tooltip("Botón para ir al signo anterior")]
        [SerializeField] private Button previousSignButton;

        private CategoryData currentCategory;
        private int currentSignIndex = 0;
        private bool isPracticing = false;
        private bool isWaitingForDynamicGesture = false;

        void Start()
        {
            // Obtiene la categoría actual del GameManager
            if (GameManager.Instance != null && GameManager.Instance.CurrentCategory != null)
            {
                currentCategory = GameManager.Instance.CurrentCategory;
            }
            else
            {
                Debug.LogError("LearningController: No hay categoría seleccionada en GameManager.");
                return;
            }

            // Configura los botones
            if (repeatButton != null)
                repeatButton.onClick.AddListener(OnRepeatButtonClicked);

            if (practiceButton != null)
                practiceButton.onClick.AddListener(OnPracticeButtonClicked);

            if (selfAssessmentButton != null)
                selfAssessmentButton.onClick.AddListener(OnSelfAssessmentButtonClicked);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClicked);

            if (nextSignButton != null)
                nextSignButton.onClick.AddListener(OnNextSignButtonClicked);

            if (previousSignButton != null)
                previousSignButton.onClick.AddListener(OnPreviousSignButtonClicked);

            // Oculta el panel de feedback al inicio
            if (feedbackPanel != null)
                feedbackPanel.SetActive(false);

            // Desactiva el reconocimiento de gestos al inicio
            SetRecognitionEnabled(false);

            // Carga el primer signo
            LoadSign(currentSignIndex);
        }

        void Update()
        {
            // Actualiza el estado visual si está practicando
            if (isPracticing)
            {
                UpdateRecordingStatus();
            }
        }

        /// <summary>
        /// Actualiza el texto de estado de grabación.
        /// </summary>
        private void UpdateRecordingStatus()
        {
            if (recordingStatusText == null)
                return;

            SignData currentSign = GameManager.Instance != null ? GameManager.Instance.CurrentSign : null;

            if (currentSign != null && currentSign.requiresMovement)
            {
                // Gesto dinámico
                if (dynamicGestureRecognizer != null && dynamicGestureRecognizer.IsRecording)
                {
                    recordingStatusText.text = "RECORDING MOVEMENT...";
                    recordingStatusText.color = Color.red;
                }
                else if (isWaitingForDynamicGesture)
                {
                    recordingStatusText.text = "WAITING... (Make the gesture)";
                    recordingStatusText.color = Color.yellow;
                }
                else
                {
                    recordingStatusText.text = "READY";
                    recordingStatusText.color = Color.green;
                }
            }
            else
            {
                // Gesto estático
                if (rightHandRecognizer != null && rightHandRecognizer.IsDetected)
                {
                    recordingStatusText.text = "DETECTED!";
                    recordingStatusText.color = Color.green;
                }
                else
                {
                    recordingStatusText.text = "WATCHING... (Make the sign)";
                    recordingStatusText.color = Color.cyan;
                }
            }
        }

        /// <summary>
        /// Carga un signo por índice.
        /// </summary>
        private void LoadSign(int index)
        {
            if (currentCategory == null || currentCategory.signs == null || currentCategory.signs.Count == 0)
            {
                Debug.LogError("LearningController: No hay signos en la categoría.");
                return;
            }

            // Asegura que el índice esté dentro de los límites
            currentSignIndex = Mathf.Clamp(index, 0, currentCategory.signs.Count - 1);

            SignData sign = currentCategory.signs[currentSignIndex];

            // Guarda el signo actual en el GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.CurrentSign = sign;

            // Actualiza la UI
            if (signNameText != null)
                signNameText.text = sign.signName;

            if (signDescriptionText != null)
                signDescriptionText.text = sign.description;

            // Configura los recognizers con el nuevo signo
            if (sign.requiresMovement)
            {
                // Gesto dinámico: usa DynamicGestureRecognizer
                if (dynamicGestureRecognizer != null)
                    dynamicGestureRecognizer.SetTargetSign(sign);
            }
            else
            {
                // Gesto estático: usa GestureRecognizer normal
                if (rightHandRecognizer != null)
                    rightHandRecognizer.TargetSign = sign;

                if (leftHandRecognizer != null)
                    leftHandRecognizer.TargetSign = sign;
            }

            // Actualiza los botones de navegación
            UpdateNavigationButtons();
        }

        /// <summary>
        /// Actualiza el estado de los botones de navegación.
        /// </summary>
        private void UpdateNavigationButtons()
        {
            if (previousSignButton != null)
                previousSignButton.interactable = currentSignIndex > 0;

            if (nextSignButton != null)
                nextSignButton.interactable = currentSignIndex < currentCategory.signs.Count - 1;
        }

        /// <summary>
        /// Callback cuando se hace clic en el botón "Repetir".
        /// </summary>
        private void OnRepeatButtonClicked()
        {
            if (ghostHandPlayer != null && GameManager.Instance != null && GameManager.Instance.CurrentSign != null)
            {
                ghostHandPlayer.PlaySign(GameManager.Instance.CurrentSign);
            }
            else
            {
                Debug.LogError("LearningController: No se puede reproducir el signo.");
            }
        }

        /// <summary>
        /// Callback cuando se hace clic en el botón "Practicar".
        /// </summary>
        private void OnPracticeButtonClicked()
        {
            isPracticing = !isPracticing;

            if (isPracticing)
            {
                // Activa el feedback y el reconocimiento de gestos
                if (feedbackPanel != null)
                    feedbackPanel.SetActive(true);

                SetRecognitionEnabled(true);

                if (practiceButton != null)
                {
                    var buttonText = practiceButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                        buttonText.text = "Stop Practice";
                }

                UpdateFeedbackText("Make the sign to practice...");
            }
            else
            {
                // Desactiva el feedback y el reconocimiento
                if (feedbackPanel != null)
                    feedbackPanel.SetActive(false);

                SetRecognitionEnabled(false);

                if (practiceButton != null)
                {
                    var buttonText = practiceButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                        buttonText.text = "Practice";
                }
            }
        }

        /// <summary>
        /// Activa o desactiva el reconocimiento de gestos.
        /// </summary>
        private void SetRecognitionEnabled(bool enabled)
        {
            SignData currentSign = GameManager.Instance != null ? GameManager.Instance.CurrentSign : null;

            if (currentSign != null && currentSign.requiresMovement)
            {
                // Gesto dinámico: usa DynamicGestureRecognizer
                if (dynamicGestureRecognizer != null)
                {
                    if (enabled)
                    {
                        Debug.Log($"[LearningController] ACTIVANDO reconocimiento dinámico para '{currentSign.signName}'");
                        dynamicGestureRecognizer.onDynamicGestureDetected.AddListener(OnDynamicGestureDetected);
                        dynamicGestureRecognizer.onDynamicGestureFailed.AddListener(OnDynamicGestureFailed);
                        isWaitingForDynamicGesture = true;
                        dynamicGestureRecognizer.StartRecording();
                        Debug.Log($"[LearningController] GRABACION INICIADA - Haz el gesto '{currentSign.signName}' AHORA!");
                    }
                    else
                    {
                        Debug.Log($"[LearningController] DESACTIVANDO reconocimiento dinámico");
                        dynamicGestureRecognizer.onDynamicGestureDetected.RemoveListener(OnDynamicGestureDetected);
                        dynamicGestureRecognizer.onDynamicGestureFailed.RemoveListener(OnDynamicGestureFailed);
                        isWaitingForDynamicGesture = false;
                        if (dynamicGestureRecognizer.IsRecording)
                            dynamicGestureRecognizer.StopRecording();
                    }
                }
            }
            else
            {
                // Gesto estático: usa GestureRecognizer normal
                if (rightHandRecognizer != null)
                {
                    rightHandRecognizer.SetDetectionEnabled(enabled);

                    if (enabled)
                    {
                        rightHandRecognizer.onGestureDetected.AddListener(OnGestureDetected);
                        rightHandRecognizer.onGestureEnded.AddListener(OnGestureEnded);
                    }
                    else
                    {
                        rightHandRecognizer.onGestureDetected.RemoveListener(OnGestureDetected);
                        rightHandRecognizer.onGestureEnded.RemoveListener(OnGestureEnded);
                    }
                }

                if (leftHandRecognizer != null)
                {
                    leftHandRecognizer.SetDetectionEnabled(enabled);

                    if (enabled)
                    {
                        leftHandRecognizer.onGestureDetected.AddListener(OnGestureDetected);
                        leftHandRecognizer.onGestureEnded.AddListener(OnGestureEnded);
                    }
                    else
                    {
                        leftHandRecognizer.onGestureDetected.RemoveListener(OnGestureDetected);
                        leftHandRecognizer.onGestureEnded.RemoveListener(OnGestureEnded);
                    }
                }
            }
        }

        /// <summary>
        /// Callback cuando un gesto es detectado.
        /// </summary>
        private void OnGestureDetected(SignData sign)
        {
            UpdateFeedbackText($"Correct! Sign '{sign.signName}' detected.");
        }

        /// <summary>
        /// Callback cuando un gesto termina.
        /// </summary>
        private void OnGestureEnded(SignData sign)
        {
            UpdateFeedbackText("Make the sign to practice...");
        }

        /// <summary>
        /// Callback cuando un gesto dinámico es detectado correctamente.
        /// </summary>
        private void OnDynamicGestureDetected(SignData sign)
        {
            UpdateFeedbackText($"PERFECTO! Gesto dinamico '{sign.signName}' correcto!");

            // Detiene la grabación y permite volver a intentar
            if (dynamicGestureRecognizer != null && dynamicGestureRecognizer.IsRecording)
            {
                Invoke(nameof(RestartDynamicRecording), 2f);
            }
        }

        /// <summary>
        /// Callback cuando un gesto dinámico falla.
        /// </summary>
        private void OnDynamicGestureFailed(SignData sign)
        {
            UpdateFeedbackText($"Intenta de nuevo. El movimiento no coincide con '{sign.signName}'.");

            // Permite volver a intentar
            if (dynamicGestureRecognizer != null)
            {
                Invoke(nameof(RestartDynamicRecording), 1.5f);
            }
        }

        /// <summary>
        /// Reinicia la grabación de gestos dinámicos.
        /// </summary>
        private void RestartDynamicRecording()
        {
            if (dynamicGestureRecognizer != null && isPracticing)
            {
                UpdateFeedbackText("Make the dynamic gesture...");
                dynamicGestureRecognizer.StartRecording();
            }
        }

        /// <summary>
        /// Actualiza el texto del feedback.
        /// </summary>
        private void UpdateFeedbackText(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message;
        }

        /// <summary>
        /// Callback cuando se hace clic en el botón "Siguiente Signo".
        /// </summary>
        private void OnNextSignButtonClicked()
        {
            LoadSign(currentSignIndex + 1);
        }

        /// <summary>
        /// Callback cuando se hace clic en el botón "Signo Anterior".
        /// </summary>
        private void OnPreviousSignButtonClicked()
        {
            LoadSign(currentSignIndex - 1);
        }

        /// <summary>
        /// Callback cuando se hace clic en el botón "Autoevaluación".
        /// </summary>
        private void OnSelfAssessmentButtonClicked()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadSelfAssessmentMode();
            }
            else
            {
                Debug.LogError("LearningController: SceneLoader.Instance es null.");
            }
        }

        /// <summary>
        /// Callback cuando se hace clic en el botón "Volver".
        /// </summary>
        private void OnBackButtonClicked()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadLevelSelection();
            }
            else
            {
                Debug.LogError("LearningController: SceneLoader.Instance es null.");
            }
        }

        void OnDestroy()
        {
            // Limpia los listeners
            if (repeatButton != null)
                repeatButton.onClick.RemoveListener(OnRepeatButtonClicked);

            if (practiceButton != null)
                practiceButton.onClick.RemoveListener(OnPracticeButtonClicked);

            if (selfAssessmentButton != null)
                selfAssessmentButton.onClick.RemoveListener(OnSelfAssessmentButtonClicked);

            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackButtonClicked);

            if (nextSignButton != null)
                nextSignButton.onClick.RemoveListener(OnNextSignButtonClicked);

            if (previousSignButton != null)
                previousSignButton.onClick.RemoveListener(OnPreviousSignButtonClicked);

            SetRecognitionEnabled(false);
        }
    }
}
