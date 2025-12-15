using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL_LearnVR.Core;

namespace ASL_LearnVR.MainMenu
{
    /// <summary>
    /// Controla el menú principal (escena LearningAppVR).
    /// Gestiona los botones y el estado del tracking de manos.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Header("UI References - Panel Left")]
        [Tooltip("Texto que muestra el estado del tracking de manos")]
        [SerializeField] private TextMeshProUGUI handStatusText;

        [Tooltip("Botón para salir de la aplicación")]
        [SerializeField] private Button exitButton;

        [Header("UI References - Panel Front")]
        [Tooltip("Botón para acceder al módulo de aprendizaje")]
        [SerializeField] private Button learningModuleButton;

        [Tooltip("Botón para acceder al módulo de traducción")]
        [SerializeField] private Button translationModuleButton;

        [Header("Components")]
        [Tooltip("Referencia al HandTrackingStatus")]
        [SerializeField] private HandTrackingStatus handTrackingStatus;

        [Header("Popup")]
        [Tooltip("Panel popup que muestra 'Módulo en desarrollo'")]
        [SerializeField] private GameObject translationPopup;

        [Tooltip("Botón para cerrar el popup")]
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

            // Limpia la sesión al volver al menú principal
            if (GameManager.Instance != null)
                GameManager.Instance.ClearSession();
        }

        void Update()
        {
            // Actualiza el texto del estado de tracking
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
        /// Callback cuando se hace clic en el botón del módulo de aprendizaje.
        /// </summary>
        private void OnLearningModuleButtonClicked()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadLevelSelection();
            }
            else
            {
                Debug.LogError("MenuController: SceneLoader.Instance es null.");
            }
        }

        /// <summary>
        /// Callback cuando se hace clic en el botón del módulo de traducción.
        /// </summary>
        private void OnTranslationModuleButtonClicked()
        {
            ShowTranslationPopup();
        }

        /// <summary>
        /// Muestra el popup indicando que el módulo de traducción está en desarrollo.
        /// </summary>
        private void ShowTranslationPopup()
        {
            if (translationPopup != null)
                translationPopup.SetActive(true);
        }

        /// <summary>
        /// Cierra el popup del módulo de traducción.
        /// </summary>
        private void CloseTranslationPopup()
        {
            if (translationPopup != null)
                translationPopup.SetActive(false);
        }

        /// <summary>
        /// Callback cuando se hace clic en el botón de salir.
        /// </summary>
        private void OnExitButtonClicked()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.QuitApplication();
            }
            else
            {
                // Fallback si no hay SceneLoader
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        void OnDestroy()
        {
            // Limpia los listeners
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
