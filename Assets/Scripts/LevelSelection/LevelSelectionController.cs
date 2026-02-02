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
    /// Usa los paneles visuales existentes en la escena.
    /// </summary>
    public class LevelSelectionController : MonoBehaviour
    {
        [Header("Available Levels")]
        [Tooltip("Lista de niveles disponibles")]
        [SerializeField] private List<LevelData> levels = new List<LevelData>();

        [Header("UI References - Level Panels")]
        [Tooltip("Panel visual para el nivel Basic")]
        [SerializeField] private GameObject basicPanel;

        [Tooltip("Panel visual para el nivel Intermediate")]
        [SerializeField] private GameObject intermediatePanel;

        [Tooltip("Panel visual para el nivel Advanced")]
        [SerializeField] private GameObject advancedPanel;

        [Tooltip("Contenedor de categorías dentro de cada panel (opcional, se busca automáticamente)")]
        [SerializeField] private Transform categoryButtonsContainer;

        [Tooltip("Prefab del botón de categoría")]
        [SerializeField] private GameObject categoryButtonPrefab;

        [Header("UI References - Text")]
        [Tooltip("Texto que muestra el título del nivel seleccionado")]
        [SerializeField] private TextMeshProUGUI selectedLevelText;

        [Tooltip("Texto de encabezado que aparece en el contenedor de categorías (opcional, se crea automáticamente si no existe)")]
        [SerializeField] private TextMeshProUGUI categoryHeaderText;

        [Header("Back Button")]
        [Tooltip("Botón para volver al menú principal")]
        [SerializeField] private Button backButton;

        private LevelData selectedLevel;
        private List<GameObject> levelPanels = new List<GameObject>();

        void Start()
        {
            // Mapea los paneles por índice
            InitializeLevelPanels();

            // Configura los botones dentro de cada panel
            SetupLevelPanelButtons();

            // Oculta las categorías al inicio (solo se muestran al hacer click en un nivel)
            HideAllCategoryContainers();

            // Configura el botón de volver
            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClicked);
        }

        /// <summary>
        /// Inicializa la lista de paneles de nivel por índice.
        /// Los paneles se mapean directamente a los LevelData en el mismo orden.
        /// </summary>
        private void InitializeLevelPanels()
        {
            // Agrega los paneles en el orden: Basic (0), Intermediate (1), Advanced (2)
            levelPanels.Add(basicPanel);
            levelPanels.Add(intermediatePanel);
            levelPanels.Add(advancedPanel);

            Debug.Log($"LevelSelectionController: {levelPanels.Count} paneles configurados.");
            Debug.Log($"LevelSelectionController: {levels.Count} niveles configurados.");

            for (int i = 0; i < levelPanels.Count; i++)
            {
                if (levelPanels[i] != null)
                    Debug.Log($"LevelSelectionController: Panel[{i}]: {levelPanels[i].name}");
                else
                    Debug.LogWarning($"LevelSelectionController: Panel[{i}] es null!");
            }

            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] != null)
                    Debug.Log($"LevelSelectionController: Level[{i}]: {levels[i].name} (levelName='{levels[i].levelName}')");
            }
        }

        /// <summary>
        /// Configura los botones de cada panel para que al hacer click muestren las categorías.
        /// Mapea por índice: levels[i] -> levelPanels[i]
        /// </summary>
        private void SetupLevelPanelButtons()
        {
            for (int i = 0; i < levelPanels.Count; i++)
            {
                GameObject panel = levelPanels[i];

                if (panel == null)
                {
                    Debug.LogWarning($"LevelSelectionController: Panel[{i}] es null, saltando.");
                    continue;
                }

                // Si hay un nivel correspondiente para este panel
                if (i < levels.Count && levels[i] != null && levels[i].IsValid())
                {
                    LevelData level = levels[i];

                    // Busca el botón con el nombre "Button" dentro de la jerarquía del panel
                    Transform buttonTransform = panel.transform.Find("Background/Title/Top/Body/Button");

                    if (buttonTransform == null)
                    {
                        // Intenta buscar solo por "Button" en cualquier parte
                        buttonTransform = FindChildRecursive(panel.transform, "Button");
                    }

                    Button panelButton = null;
                    if (buttonTransform != null)
                    {
                        panelButton = buttonTransform.GetComponent<Button>();
                    }

                    if (panelButton == null)
                    {
                        // Último recurso: busca CUALQUIER botón en el panel
                        Button[] allButtons = panel.GetComponentsInChildren<Button>();
                        if (allButtons.Length > 0)
                        {
                            panelButton = allButtons[0];
                            Debug.LogWarning($"LevelSelectionController: Usando el primer botón encontrado en panel[{i}] '{panel.name}': {panelButton.gameObject.name}");
                        }
                    }

                    if (panelButton != null)
                    {
                        LevelData levelCopy = level;
                        int panelIndex = i; // Captura el índice para el callback
                        panelButton.onClick.AddListener(() => OnLevelButtonClicked(levelCopy, panelIndex));
                        Debug.Log($"LevelSelectionController: Listener añadido al botón '{panelButton.gameObject.name}' del panel[{i}] '{panel.name}' para nivel '{level.name}'.");
                    }
                    else
                    {
                        Debug.LogError($"LevelSelectionController: NO se encontró NINGÚN botón en el panel[{i}] '{panel.name}'!");
                    }
                }
                else
                {
                    // Panel sin nivel configurado -> marcar como "Próximamente"
                    MarkPanelAsUnavailable(panel, i);
                }
            }
        }

        /// <summary>
        /// Busca un hijo recursivamente por nombre.
        /// </summary>
        private Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Marca un panel específico como no disponible con texto "Próximamente".
        /// </summary>
        private void MarkPanelAsUnavailable(GameObject panel, int index)
        {
            if (panel == null)
                return;

            Debug.Log($"LevelSelectionController: Marcando panel[{index}] '{panel.name}' como 'Próximamente'.");

            // Desactiva el botón
            Button button = panel.GetComponentInChildren<Button>();
            if (button != null)
                button.interactable = false;

            // Muestra "Próximamente"
            TextMeshProUGUI text = panel.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = "Próximamente";
        }

        /// <summary>
        /// Oculta todos los contenedores de categorías dentro de los paneles.
        /// </summary>
        private void HideAllCategoryContainers()
        {
            foreach (var panel in levelPanels)
            {
                if (panel != null)
                {
                    // Busca el contenedor de categorías (suponiendo que tiene un tag o nombre específico)
                    Transform categoryContainer = FindCategoryContainer(panel.transform);
                    if (categoryContainer != null)
                        categoryContainer.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Callback cuando se hace clic en un botón de nivel.
        /// </summary>
        private void OnLevelButtonClicked(LevelData level, int panelIndex)
        {
            selectedLevel = level;

            // Guarda el nivel seleccionado en el GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.CurrentLevel = level;

            // Actualiza el texto del nivel seleccionado
            if (selectedLevelText != null)
            {
                string displayName = string.IsNullOrEmpty(level.levelName) ? level.name : level.levelName;
                selectedLevelText.text = $"Level: {displayName}";
            }

            // Busca el panel correspondiente usando el índice
            if (panelIndex >= 0 && panelIndex < levelPanels.Count)
            {
                GameObject panel = levelPanels[panelIndex];
                if (panel != null)
                {
                    GenerateCategoryButtons(level, panel);
                }
                else
                {
                    Debug.LogError($"LevelSelectionController: Panel[{panelIndex}] es null.");
                }
            }
            else
            {
                Debug.LogError($"LevelSelectionController: Índice de panel inválido: {panelIndex}.");
            }
        }

        /// <summary>
        /// Busca el contenedor de categorías dentro de un panel.
        /// Si no existe, crea uno automáticamente DENTRO del panel de contenido.
        /// </summary>
        private Transform FindCategoryContainer(Transform panelTransform)
        {
            // Busca por nombre común
            Transform container = panelTransform.Find("CategoryContainer");
            if (container == null)
                container = panelTransform.Find("Categories");
            if (container == null)
                container = panelTransform.Find("Content/CategoryContainer");

            // Busca dentro de Background/Title/Top/Body si existe esa estructura
            if (container == null)
            {
                Transform bodyTransform = panelTransform.Find("Background/Title/Top/Body");
                if (bodyTransform != null)
                {
                    container = bodyTransform.Find("CategoryContainer");
                }
            }

            // Si no existe ninguno, busca el primer VerticalLayoutGroup o HorizontalLayoutGroup
            if (container == null)
            {
                foreach (Transform child in panelTransform)
                {
                    if (child.GetComponent<UnityEngine.UI.LayoutGroup>() != null)
                    {
                        container = child;
                        break;
                    }
                }
            }

            // Si TODAVÍA no existe, CRÉALO dentro del Body si existe, o directamente en el panel
            if (container == null)
            {
                Debug.Log($"LevelSelectionController: Creando contenedor de categorías en panel '{panelTransform.name}'.");

                // Buscar el parent ideal: Background/Title/Top/Body
                Transform parentForContainer = panelTransform;
                Transform bodyTransform = panelTransform.Find("Background/Title/Top/Body");

                if (bodyTransform != null)
                {
                    parentForContainer = bodyTransform;
                    Debug.Log($"LevelSelectionController: Usando Body como parent para CategoryContainer.");
                }
                else
                {
                    Debug.LogWarning($"LevelSelectionController: No se encontró Body en '{panelTransform.name}', usando el panel directamente.");
                }

                GameObject containerObj = new GameObject("CategoryContainer");
                containerObj.transform.SetParent(parentForContainer, false);

                // Añade un VerticalLayoutGroup para organizar los botones
                var layoutGroup = containerObj.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                layoutGroup.spacing = 10f;
                layoutGroup.padding = new RectOffset(15, 15, 15, 15); // Padding interno
                layoutGroup.childAlignment = TextAnchor.UpperCenter;
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandWidth = true; // FUERZA que los botones se expandan al ancho
                layoutGroup.childForceExpandHeight = false;

                // Añade ContentSizeFitter para ajustar el tamaño automáticamente
                var sizeFitter = containerObj.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                sizeFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                sizeFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained; // NO restringe el ancho, usa el del parent

                // Configurar el RectTransform
                RectTransform rectTransform = containerObj.GetComponent<RectTransform>();
                if (rectTransform == null)
                    rectTransform = containerObj.AddComponent<RectTransform>();

                // Posicionar usando anchors que se expandan con el parent
                // Esto hará que el contenedor se ajuste al espacio disponible en el Body
                rectTransform.anchorMin = new Vector2(0f, 0f);
                rectTransform.anchorMax = new Vector2(1f, 0f);
                rectTransform.pivot = new Vector2(0.5f, 0f); // Pivote abajo para crecer hacia arriba
                rectTransform.anchoredPosition = new Vector2(0f, 20f); // 20 píxeles desde el bottom
                rectTransform.sizeDelta = new Vector2(0f, 150f); // Alto para los botones, ancho ajustado automáticamente

                container = containerObj.transform;
            }

            return container;
        }

        /// <summary>
        /// Genera dinámicamente los botones de categoría para el nivel seleccionado dentro del panel.
        /// </summary>
        private void GenerateCategoryButtons(LevelData level, GameObject panel)
        {
            if (categoryButtonPrefab == null)
            {
                Debug.LogError("LevelSelectionController: Falta referencia al prefab de botón de categoría.");
                return;
            }

            // Busca el contenedor de categorías dentro del panel
            Transform container = FindCategoryContainer(panel.transform);

            if (container == null)
            {
                Debug.LogWarning($"LevelSelectionController: No se encontró contenedor de categorías en el panel '{level.levelName}'.");
                return;
            }

            // Oculta primero todos los contenedores
            HideAllCategoryContainers();

            // Activa el contenedor de este panel
            container.gameObject.SetActive(true);

            // Limpia los botones anteriores (excepto el header si existe)
            foreach (Transform child in container)
            {
                if (child.name != "CategoryHeader")
                    Destroy(child.gameObject);
            }

            // Crea o actualiza el texto de encabezado
            CreateOrUpdateCategoryHeader(container, level);

            // Genera nuevos botones
            foreach (var category in level.categories)
            {
                if (category == null || !category.IsValid())
                {
                    Debug.LogWarning($"LevelSelectionController: Categoría inválida encontrada en nivel '{level.levelName}'.");
                    continue;
                }

                GameObject buttonObj = Instantiate(categoryButtonPrefab, container);
                Button button = buttonObj.GetComponent<Button>();

                // Asegura que el botón tenga un LayoutElement con ancho mínimo
                var layoutElement = buttonObj.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layoutElement == null)
                    layoutElement = buttonObj.AddComponent<UnityEngine.UI.LayoutElement>();

                // Configura el tamaño mínimo del botón
                layoutElement.minWidth = 250f; // Ancho mínimo
                layoutElement.preferredWidth = 350f; // Ancho preferido
                layoutElement.minHeight = 60f; // Alto mínimo
                layoutElement.preferredHeight = 70f; // Alto preferido
                layoutElement.flexibleWidth = 1f; // Permite que se expanda si hay espacio

                // Busca TODOS los TextMeshProUGUI en el botón
                TextMeshProUGUI[] textComponents = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();

                // El primer texto es el nombre de la categoría
                if (textComponents.Length > 0)
                    textComponents[0].text = category.categoryName;

                // El segundo texto (si existe) es el contador de signos
                if (textComponents.Length > 1)
                {
                    int signCount = category.signs != null ? category.signs.Count : 0;
                    textComponents[1].text = $"{signCount} signos";
                }

                if (button != null)
                {
                    CategoryData categoryCopy = category;
                    button.onClick.AddListener(() => OnCategoryButtonClicked(categoryCopy));
                }
            }
        }

        /// <summary>
        /// Crea o actualiza el texto de encabezado del contenedor de categorías.
        /// </summary>
        private void CreateOrUpdateCategoryHeader(Transform container, LevelData level)
        {
            // Busca si ya existe un header
            Transform headerTransform = container.Find("CategoryHeader");
            TextMeshProUGUI headerText = null;

            if (headerTransform == null)
            {
                // Crea el header
                GameObject headerObj = new GameObject("CategoryHeader");
                headerObj.transform.SetParent(container, false);
                headerObj.transform.SetAsFirstSibling(); // Colócalo al principio

                headerText = headerObj.AddComponent<TextMeshProUGUI>();

                // Configuración del texto
                headerText.fontSize = 24;
                headerText.alignment = TextAlignmentOptions.Center;
                headerText.color = Color.white;
                headerText.fontStyle = FontStyles.Bold;

                // Configuración del RectTransform
                RectTransform headerRect = headerObj.GetComponent<RectTransform>();
                headerRect.sizeDelta = new Vector2(300f, 60f);

                // Añade un LayoutElement para controlar el tamaño
                var layoutElement = headerObj.AddComponent<UnityEngine.UI.LayoutElement>();
                layoutElement.minHeight = 60f;
                layoutElement.preferredHeight = 60f;
            }
            else
            {
                headerText = headerTransform.GetComponent<TextMeshProUGUI>();
            }

            // Actualiza el texto
            if (headerText != null)
            {
                string levelName = string.IsNullOrEmpty(level.levelName) ? level.name : level.levelName;
                headerText.text = $"Has seleccionado: {levelName.ToUpper()}\n\nElige una categoría para comenzar:";
            }

            // Guarda la referencia si el campo está disponible
            if (categoryHeaderText == null)
                categoryHeaderText = headerText;
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
