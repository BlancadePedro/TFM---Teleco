using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL_LearnVR.Core;

namespace ASL_LearnVR.MainMenu
{
    /// <summary>
    /// Controls the main menu (LearningAppVR scene).
    /// Manages buttons and hand tracking state.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Header("UI References - Left Panel")]
        [Tooltip("Text showing hand tracking status")]
        [SerializeField] private TextMeshProUGUI handStatusText;

        [Tooltip("Button to exit the application")]
        [SerializeField] private Button exitButton;

        [Header("UI References - Front Panel")]
        [Tooltip("Button to access the learning module")]
        [SerializeField] private Button learningModuleButton;

        [Tooltip("Button to access the translation module")]
        [SerializeField] private Button translationModuleButton;

        [Header("Components")]
        [Tooltip("Reference to HandTrackingStatus")]
        [SerializeField] private HandTrackingStatus handTrackingStatus;

        [Header("Popup")]
        [Tooltip("Popup panel showing 'Module in development'")]
        [SerializeField] private GameObject translationPopup;

        [Tooltip("Button to close the popup")]
        [SerializeField] private Button closePopupButton;

        void Start()
        {
            // Configura los botones
            if (learningModuleButton != null)
                learningModuleButton.onClick.AddListener(OnLearningModuleButtonClicked);

            if (translationModuleButton != null)
                translationModuleButton.onClick.AddListener(OnTranslationModuleButtonClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitButtonClicked);

            if (closePopupButton != null)
                closePopupButton.onClick.AddListener(CloseTranslationPopup);

            // Oculta el popup al inicio
            if (translationPopup != null)
                translationPopup.SetActive(false);

            // Clears the session when returning to the main menu
            if (GameManager.Instance != null)
                GameManager.Instance.ClearSession();
        }

        void Update()
        {
            // Updates the tracking status text
            UpdateHandStatusText();
        }

        /// <summary>
        /// Actualiza el texto que muestra el estado del tracking de manos.
        /// </summary>
        private void UpdateHandStatusText()
        {
            if (handStatusText == null || handTrackingStatus == null)
                return;

            handStatusText.text = handTrackingStatus.GetStatusDescription();
        }

        /// <summary>
        /// Callback when button is clicked del modulo de aprendizaje.
        /// </summary>
        private void OnLearningModuleButtonClicked()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadLevelSelection();
            }
            else
            {
                Debug.LogError("MenuController: SceneLoader.Instance is null.");
            }
        }

        /// <summary>
        /// Callback when button is clicked del modulo de traduccion.
        /// </summary>
        private void OnTranslationModuleButtonClicked()
        {
            ShowTranslationPopup();
        }

        /// <summary>
        /// Shows the popup indicating the translation module is in development.
        /// </summary>
        private void ShowTranslationPopup()
        {
            if (translationPopup != null)
                translationPopup.SetActive(true);
        }

        /// <summary>
        /// Closes the translation module popup.
        /// </summary>
        private void CloseTranslationPopup()
        {
            if (translationPopup != null)
                translationPopup.SetActive(false);
        }

        /// <summary>
        /// Callback when button is clicked de salir.
        /// </summary>
        private void OnExitButtonClicked()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.QuitApplication();
            }
            else
            {
                // Fallback if SceneLoader is not available
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        void OnDestroy()
        {
            // Clears the listeners
            if (learningModuleButton != null)
                learningModuleButton.onClick.RemoveListener(OnLearningModuleButtonClicked);

            if (translationModuleButton != null)
                translationModuleButton.onClick.RemoveListener(OnTranslationModuleButtonClicked);

            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitButtonClicked);

            if (closePopupButton != null)
                closePopupButton.onClick.RemoveListener(CloseTranslationPopup);
        }
    }
}
