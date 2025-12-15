using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL_LearnVR.Core;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.LevelSelection
{
    /// <summary>
    /// Controla la escena de selección de nivel y categoría.
    /// Permite al usuario elegir un nivel (Básico/Intermedio/Avanzado) y luego una categoría.
    /// </summary>
    public class LevelSelectionController : MonoBehaviour
    {
        [Header("Available Levels")]
        [Tooltip("Lista de niveles disponibles")]
        [SerializeField] private List<LevelData> levels = new List<LevelData>();

        [Header("UI References - Level Selection")]
        [Tooltip("Contenedor de los botones de nivel")]
        [SerializeField] private Transform levelButtonsContainer;

        [Tooltip("Prefab del botón de nivel")]
        [SerializeField] private GameObject levelButtonPrefab;

        [Header("UI References - Category Selection")]
        [Tooltip("Contenedor de los botones de categoría")]
        [SerializeField] private Transform categoryButtonsContainer;

        [Tooltip("Prefab del botón de categoría")]
        [SerializeField] private GameObject categoryButtonPrefab;

        [Tooltip("Panel que contiene los botones de categoría (se oculta hasta que se seleccione un nivel)")]
        [SerializeField] private GameObject categoryPanel;

        [Header("UI References - Text")]
        [Tooltip("Texto que muestra el título del nivel seleccionado")]
        [SerializeField] private TextMeshProUGUI selectedLevelText;

        [Header("Back Button")]
        [Tooltip("Botón para volver al menú principal")]
        [SerializeField] private Button backButton;

        private LevelData selectedLevel;

        void Start()
        {
            // Oculta el panel de categorías al inicio
            if (categoryPanel != null)
                categoryPanel.SetActive(false);

            // Genera los botones de nivel
            GenerateLevelButtons();

            // Configura el botón de volver
            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClicked);
        }

        /// <summary>
        /// Genera dinámicamente los botones de nivel.
        /// </summary>
        private void GenerateLevelButtons()
        {
            if (levelButtonsContainer == null || levelButtonPrefab == null)
            {
                Debug.LogError("LevelSelectionController: Faltan referencias para generar botones de nivel.");
                return;
            }

            foreach (var level in levels)
            {
                if (level == null || !level.IsValid())
                {
                    Debug.LogWarning($"LevelSelectionController: Nivel inválido encontrado.");
                    continue;
                }

                GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonsContainer);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                if (buttonText != null)
                    buttonText.text = level.levelName;

                if (button != null)
                {
                    LevelData levelCopy = level; // Captura el nivel en el closure
                    button.onClick.AddListener(() => OnLevelButtonClicked(levelCopy));
                }
            }
        }

        /// <summary>
        /// Callback cuando se hace clic en un botón de nivel.
        /// </summary>
        private void OnLevelButtonClicked(LevelData level)
        {
            selectedLevel = level;

            // Guarda el nivel seleccionado en el GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.CurrentLevel = level;

            // Actualiza el texto del nivel seleccionado
            if (selectedLevelText != null)
                selectedLevelText.text = $"Level: {level.levelName}";

            // Genera los botones de categoría
            GenerateCategoryButtons(level);

            // Muestra el panel de categorías
            if (categoryPanel != null)
                categoryPanel.SetActive(true);
        }

        /// <summary>
        /// Genera dinámicamente los botones de categoría para el nivel seleccionado.
        /// </summary>
        private void GenerateCategoryButtons(LevelData level)
        {
            if (categoryButtonsContainer == null || categoryButtonPrefab == null)
            {
                Debug.LogError("LevelSelectionController: Faltan referencias para generar botones de categoría.");
                return;
            }

            // Limpia los botones anteriores
            foreach (Transform child in categoryButtonsContainer)
            {
                Destroy(child.gameObject);
            }

            // Genera nuevos botones
            foreach (var category in level.categories)
            {
                if (category == null || !category.IsValid())
                {
                    Debug.LogWarning($"LevelSelectionController: Categoría inválida encontrada en nivel '{level.levelName}'.");
                    continue;
                }

                GameObject buttonObj = Instantiate(categoryButtonPrefab, categoryButtonsContainer);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                if (buttonText != null)
                    buttonText.text = category.categoryName;

                if (button != null)
                {
                    CategoryData categoryCopy = category; // Captura la categoría en el closure
                    button.onClick.AddListener(() => OnCategoryButtonClicked(categoryCopy));
                }
            }
        }

        /// <summary>
        /// Callback cuando se hace clic en un botón de categoría.
        /// </summary>
        private void OnCategoryButtonClicked(CategoryData category)
        {
            // Guarda la categoría seleccionada en el GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.CurrentCategory = category;

            // Carga la escena del módulo de aprendizaje
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadLearningModule();
            }
            else
            {
                Debug.LogError("LevelSelectionController: SceneLoader.Instance es null.");
            }
        }

        /// <summary>
        /// Callback cuando se hace clic en el botón de volver.
        /// </summary>
        private void OnBackButtonClicked()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadMainMenu();
            }
            else
            {
                Debug.LogError("LevelSelectionController: SceneLoader.Instance es null.");
            }
        }

        void OnDestroy()
        {
            // Limpia el listener del botón de volver
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackButtonClicked);
        }
    }
}
