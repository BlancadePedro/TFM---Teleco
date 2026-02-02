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

        [Header("Dynamic Gestures (J, Z)")]
        [Tooltip("Componente DynamicGestureRecognizer para gestos dinámicos")]
        [SerializeField] private ASL.DynamicGestures.DynamicGestureRecognizer dynamicGestureRecognizer;

        [Header("Feedback UI")]
        [Tooltip("Panel que muestra feedback durante la práctica")]
        [SerializeField] private GameObject feedbackPanel;

        [Tooltip("Texto del feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        // NOTA: Recording status text eliminado - ya no es necesario
        // [Tooltip("Texto que muestra el estado de grabación (RECORDING, WAITING, etc.)")]
        // [SerializeField] private TextMeshProUGUI recordingStatusText;

        [Header("Navigation")]
        [Tooltip("Botón para ir al siguiente signo")]
        [SerializeField] private Button nextSignButton;

        [Tooltip("Botón para ir al signo anterior")]
        [SerializeField] private Button previousSignButton;

        [Header("Additional Categories")]
        [Tooltip("Categoría Digits para cargar poses numéricas (1, 2, 3, etc.)")]
        [SerializeField] private CategoryData digitsCategory;

        private CategoryData currentCategory;
        private int currentSignIndex = 0;
        private bool isPracticing = false;
        private bool isWaitingForDynamicGesture = false;
        private bool isShowingSuccessMessage = false; // Flag para evitar sobrescribir mensaje de éxito

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

            // Configura listeners para gestos dinámicos
            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.OnGestureStarted += OnDynamicGestureStarted;
                dynamicGestureRecognizer.OnGestureCompleted += OnDynamicGestureCompleted;
                dynamicGestureRecognizer.OnGestureFailed += OnDynamicGestureFailed;
            }
            else
            {
                Debug.LogWarning("[LearningController] DynamicGestureRecognizer no asignado - los gestos J y Z no funcionarán.");
            }

            // Oculta el panel de feedback al inicio
            if (feedbackPanel != null)
                feedbackPanel.SetActive(false);

            // Desactiva el reconocimiento de gestos al inicio
            SetRecognitionEnabled(false);

            // Carga el primer signo
            LoadSign(currentSignIndex);
        }

        void OnDestroy()
        {
            // Desuscribirse de eventos dinámicos
            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.OnGestureStarted -= OnDynamicGestureStarted;
                dynamicGestureRecognizer.OnGestureCompleted -= OnDynamicGestureCompleted;
                dynamicGestureRecognizer.OnGestureFailed -= OnDynamicGestureFailed;
            }
        }

        // NOTA: Update() y UpdateRecordingStatus() eliminados - ya no son necesarios sin el recording status text

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
                Debug.Log($"[LearningController] Cargando gesto dinámico: '{sign.signName}'");

                // Para gestos dinámicos, configurar la pose INICIAL que necesitan
                SignData initialPose = GetInitialPoseForDynamicGesture(sign);

                if (initialPose != null)
                {
                    Debug.Log($"[LearningController] Configurando pose inicial '{initialPose.signName}' para gesto dinámico '{sign.signName}'");

                    if (rightHandRecognizer != null)
                        rightHandRecognizer.TargetSign = initialPose;

                    if (leftHandRecognizer != null)
                        leftHandRecognizer.TargetSign = initialPose;
                }
                else
                {
                    Debug.LogError($"[LearningController] No se encontró pose inicial para gesto dinámico '{sign.signName}'");
                }
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
                // Gesto dinámico: activar DynamicGestureRecognizer
                Debug.Log($"[LearningController] Reconocimiento de gesto dinámico '{currentSign.signName}': {enabled}");

                if (dynamicGestureRecognizer != null)
                {
                    dynamicGestureRecognizer.SetEnabled(enabled);
                }

                // IMPORTANTE: También activar GestureRecognizer para detectar pose inicial (I para J, Z para Z)
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

                return;
            }

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

        /// <summary>
        /// Callback cuando un gesto es detectado.
        /// </summary>
        private void OnGestureDetected(SignData sign)
        {
            // No sobrescribir si estamos mostrando mensaje de éxito
            if (isShowingSuccessMessage)
                return;

            SignData currentSign = GameManager.Instance != null ? GameManager.Instance.CurrentSign : null;

            if (currentSign == null)
                return;

            // CASO 1: El signo actual es DINÁMICO (requiere movimiento)
            if (currentSign.requiresMovement)
            {
                // Mostrar confirmación de pose inicial para que el usuario sepa que puede empezar a moverse
                UpdateFeedbackText("Pose inicial reconocida, haz el gesto.");
                return;
            }
            // CASO 2: El signo actual es ESTÁTICO (NO requiere movimiento)
            else if (!currentSign.requiresMovement && currentSign.signName == sign.signName)
            {
                // Gesto estático normal - solo si coincide EXACTAMENTE con el signo actual
                UpdateFeedbackText($"Correct! Sign '{sign.signName}' detected.");
            }
            // Si no coincide con ningún caso, no mostrar nada
        }

        /// <summary>
        /// Callback cuando un gesto termina.
        /// </summary>
        private void OnGestureEnded(SignData sign)
        {
            // No sobrescribir si estamos mostrando mensaje de éxito
            if (!isShowingSuccessMessage)
            {
                UpdateFeedbackText("Make the sign to practice...");
            }
        }

        /// <summary>
        /// Callback cuando se inicia un gesto dinámico.
        /// </summary>
        private void OnDynamicGestureStarted(string gestureName)
        {
            Debug.Log($"[LearningController] Gesto dinámico INICIADO: {gestureName}");

            // No sobrescribir si estamos mostrando mensaje de éxito
            if (!isShowingSuccessMessage)
            {
                UpdateFeedbackText($"GESTO '{gestureName}' INICIADO!\nSigue moviendo para completar...");
            }
        }

        /// <summary>
        /// Callback cuando se completa un gesto dinámico.
        /// </summary>
        private void OnDynamicGestureCompleted(string gestureName)
        {
            Debug.Log($"[LearningController] Gesto dinámico COMPLETADO: {gestureName}");
            UpdateFeedbackText($"✓ ¡PERFECTO!\nGesto '{gestureName}' completado correctamente!");

            // Marcar que estamos mostrando mensaje de éxito
            isShowingSuccessMessage = true;

            // Limpiar el mensaje después de 3 segundos
            CancelInvoke(nameof(ClearSuccessMessage));
            Invoke(nameof(ClearSuccessMessage), 3f);
        }

        /// <summary>
        /// Callback cuando falla un gesto dinámico.
        /// </summary>
        private void OnDynamicGestureFailed(string gestureName, string reason)
        {
            Debug.Log($"[LearningController] Gesto dinámico FALLADO: {gestureName} - Razón: {reason}");

            // No sobrescribir si estamos mostrando mensaje de éxito
            if (!isShowingSuccessMessage)
            {
                UpdateFeedbackText($"Intenta de nuevo '{gestureName}'. {reason}");
            }
        }

        /// <summary>
        /// Actualiza el texto del feedback.
        /// </summary>
        private void UpdateFeedbackText(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                Debug.Log($"[LearningController] FEEDBACK UI ACTUALIZADO: '{message}' (isShowingSuccess={isShowingSuccessMessage})");
            }
            else
            {
                Debug.LogWarning("[LearningController] feedbackText es NULL! No se puede actualizar UI");
            }
        }

        /// <summary>
        /// Limpia el mensaje de éxito después de 3 segundos.
        /// </summary>
        private void ClearSuccessMessage()
        {
            isShowingSuccessMessage = false;
            UpdateFeedbackText("Make the sign to practice...");
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

        /// <summary>
        /// Obtiene la pose inicial necesaria para un gesto dinámico.
        /// </summary>
        private SignData GetInitialPoseForDynamicGesture(SignData dynamicSign)
        {
            if (dynamicSign == null || currentCategory == null)
                return null;

            // Mapeo de gestos dinámicos a sus poses iniciales
            switch (dynamicSign.signName)
            {
                case "White":
                    // White empieza con mano abierta "5"
                    return FindSignByName("5");

                case "Orange":
                    // Orange empieza con forma "O"
                    return FindSignByName("O");

                case "J":
                    // J usa su propio SignData (Sign_J con ASL_Letter_J_Shape)
                    // NO usar Sign_I para evitar confusión
                    return dynamicSign;

                case "Z":
                    // Z usa su propio SignData (Sign_Z con ASL_Letter_Z_Shape)
                    // NO usar Sign_1 para evitar confusión
                    return dynamicSign;

                case "Yes":
                case "No":
                case "Hello":
                case "Bye":
                case "Please":
                case "Thank You":
                case "Good":
                case "Bad":
                    // Los gestos de Basic Communication usan su propio SignData con requiresMovement
                    // No necesitan mapeo a pose inicial diferente
                    return dynamicSign;

                default:
                    Debug.LogWarning($"[LearningController] No se conoce la pose inicial para el gesto dinámico '{dynamicSign.signName}'");
                    return dynamicSign; // Fallback al signo original
            }
        }

        /// <summary>
        /// Busca un SignData por nombre en categoría actual y en categoría Digits.
        /// </summary>
        private SignData FindSignByName(string signName)
        {
            // Primero buscar en la categoría actual
            if (currentCategory != null && currentCategory.signs != null)
            {
                foreach (var sign in currentCategory.signs)
                {
                    if (sign != null && sign.signName == signName)
                    {
                        return sign;
                    }
                }
            }

            // Si no se encuentra, buscar en la categoría Digits (para poses numéricas como "1")
            if (digitsCategory != null && digitsCategory.signs != null)
            {
                foreach (var sign in digitsCategory.signs)
                {
                    if (sign != null && sign.signName == signName)
                    {
                        Debug.Log($"[LearningController] Signo '{signName}' encontrado en categoría Digits");
                        return sign;
                    }
                }
            }

            // Fallback: buscar en todos los SignData cargados (útil para Alphabet - caso 'O')
            var allSigns = Resources.FindObjectsOfTypeAll<SignData>();
            foreach (var sign in allSigns)
            {
                if (sign != null && sign.signName == signName)
                {
                    Debug.Log($"[LearningController] Signo '{signName}' encontrado vía Resources.");
                    return sign;
                }
            }

            Debug.LogError($"[LearningController] No se encontró el signo '{signName}' ni en categoría actual ni en Digits. Asegúrate de asignar 'Digits Category' o que el signo esté en Resources.");
            return null;
        }

    }
}
