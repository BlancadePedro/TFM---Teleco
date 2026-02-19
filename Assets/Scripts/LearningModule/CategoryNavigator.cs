using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL_LearnVR.Core;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.LearningModule
{
    /// <summary>
    /// Navegador de categorías para la Escena 3 (Learning Module).
    ///
    /// Permite moverse entre categorías de forma secuencial usando flechas
    /// en los paneles laterales: ← [Categoría actual] →
    ///
    /// Las categorías se navegan en el orden definido en availableCategories[].
    /// Por defecto: Alfabeto → Dígitos → Comunicación Básica → (vuelve al inicio)
    ///
    /// SETUP:
    ///   1. Añade este componente al GameObject que gestiona la escena 3.
    ///   2. Arrastra el LearningController existente.
    ///   3. Arrastra los botones de flecha (prevCategoryButton, nextCategoryButton).
    ///   4. Arrastra el texto que muestra la categoría actual (categoryLabel).
    ///   5. En availableCategories[], arrastra los ScriptableObjects CategoryData
    ///      en el orden deseado (el que ya está en GameManager se usa como punto de partida).
    ///
    /// CÓMO FUNCIONA:
    ///   El navegador NO recarga la escena. Llama a LearningController.SwitchCategory()
    ///   (método que añadimos con un partial/extension via este mismo componente) para
    ///   cambiar la categoría en caliente: resetea el índice de signo a 0 y recarga
    ///   la pose guía.
    /// </summary>
    [AddComponentMenu("ASL_LearnVR/Learning/Category Navigator")]
    public class CategoryNavigator : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────
        [Header("Categories (en orden de navegación)")]
        [Tooltip("Arrastra los CategoryData ScriptableObjects en el orden que quieras navegar")]
        [SerializeField] private List<CategoryData> availableCategories = new List<CategoryData>();

        [Header("UI — Flecha izquierda (panel lateral izquierdo)")]
        [SerializeField] private Button prevCategoryButton;
        [Tooltip("Icono/texto del botón anterior (opcional, para animar)")]
        [SerializeField] private TextMeshProUGUI prevArrowLabel;   // "←"

        [Header("UI — Etiqueta central")]
        [Tooltip("Texto que muestra la categoría actual y la posición")]
        [SerializeField] private TextMeshProUGUI categoryLabel;    // "Alfabeto  1/3"
        [SerializeField] private TextMeshProUGUI categorySubtitle; // "26 signos"

        [Header("UI — Flecha derecha (panel lateral derecho)")]
        [SerializeField] private Button nextCategoryButton;
        [SerializeField] private TextMeshProUGUI nextArrowLabel;   // "→"

        [Header("Progress Bar")]
        [Tooltip("Image con ImageType=Filled para la barra de progreso de categoría")]
        [SerializeField] private Image   progressBarFill;
        [SerializeField] private TextMeshProUGUI progressLabel;    // "12 / 26"

        [Header("LearningController")]
        [Tooltip("Arrastra el LearningController de la escena")]
        [SerializeField] private LearningController learningController;

        [Header("Transition")]
        [SerializeField] private float arrowPulseDuration = 0.15f;
        [SerializeField] private Color arrowHoverColor    = new Color(0.29f, 0.44f, 0.83f); // índigo
        [SerializeField] private Color arrowIdleColor     = new Color(0.34f, 0.34f, 0.38f); // gris

        // ─── Runtime ─────────────────────────────────────────────────────
        private int _currentCategoryIndex = 0;

        // ─────────────────────────────────────────────────────────────────
        void Start()
        {
            // Determinar índice inicial según lo que GameManager tenga cargado
            if (GameManager.Instance?.CurrentCategory != null)
            {
                int found = availableCategories.IndexOf(GameManager.Instance.CurrentCategory);
                if (found >= 0) _currentCategoryIndex = found;
            }

            // Listeners
            if (prevCategoryButton != null)
                prevCategoryButton.onClick.AddListener(GoToPreviousCategory);

            if (nextCategoryButton != null)
                nextCategoryButton.onClick.AddListener(GoToNextCategory);

            // Labels de flechas
            if (prevArrowLabel != null) prevArrowLabel.text = "‹";
            if (nextArrowLabel != null) nextArrowLabel.text = "›";

            RefreshUI();
        }

        void OnDestroy()
        {
            if (prevCategoryButton != null) prevCategoryButton.onClick.RemoveListener(GoToPreviousCategory);
            if (nextCategoryButton != null) nextCategoryButton.onClick.RemoveListener(GoToNextCategory);
        }

        // ─── Navegación ───────────────────────────────────────────────────
        public void GoToNextCategory()
        {
            if (availableCategories.Count == 0) return;
            _currentCategoryIndex = (_currentCategoryIndex + 1) % availableCategories.Count;
            ApplyCategory();
            StartCoroutine(PulseArrow(nextArrowLabel));
        }

        public void GoToPreviousCategory()
        {
            if (availableCategories.Count == 0) return;
            _currentCategoryIndex = (_currentCategoryIndex - 1 + availableCategories.Count) % availableCategories.Count;
            ApplyCategory();
            StartCoroutine(PulseArrow(prevArrowLabel));
        }

        private void ApplyCategory()
        {
            if (_currentCategoryIndex < 0 || _currentCategoryIndex >= availableCategories.Count) return;

            CategoryData cat = availableCategories[_currentCategoryIndex];

            // Actualizar GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.CurrentCategory = cat;

            // Decirle al LearningController que cambie de categoría
            if (learningController != null)
                learningController.SwitchToCategory(cat);

            RefreshUI();
        }

        // ─── UI ───────────────────────────────────────────────────────────
        private void RefreshUI()
        {
            if (availableCategories.Count == 0) return;

            CategoryData cat  = availableCategories[_currentCategoryIndex];
            int          total = availableCategories.Count;

            // Etiqueta principal
            if (categoryLabel != null)
                categoryLabel.text = $"{cat.categoryName}";

            if (categorySubtitle != null)
                categorySubtitle.text = $"{_currentCategoryIndex + 1} / {total}  ·  {cat.signs.Count} signos";

            // Flechas — siempre activas (navegación circular)
            if (prevCategoryButton != null) prevCategoryButton.interactable = true;
            if (nextCategoryButton != null) nextCategoryButton.interactable = true;

            // Si solo hay 1 categoría, ocultar flechas
            bool showArrows = total > 1;
            if (prevCategoryButton != null) prevCategoryButton.gameObject.SetActive(showArrows);
            if (nextCategoryButton != null) nextCategoryButton.gameObject.SetActive(showArrows);

            UpdateProgress(0, cat.signs.Count);
        }

        /// <summary>
        /// Actualiza la barra de progreso dentro de la categoría actual.
        /// Llama desde LearningController cuando cambia el índice del signo.
        /// </summary>
        /// <param name="completedInCategory">Signos completados en esta categoría</param>
        /// <param name="totalInCategory">Total de signos en esta categoría</param>
        public void UpdateProgress(int completedInCategory, int totalInCategory)
        {
            if (totalInCategory <= 0) return;

            float pct = (float)completedInCategory / totalInCategory;

            if (progressBarFill != null)
                progressBarFill.fillAmount = pct;

            if (progressLabel != null)
                progressLabel.text = $"{completedInCategory} / {totalInCategory}";
        }

        // ─── Animación de flechas ─────────────────────────────────────────
        private IEnumerator PulseArrow(TextMeshProUGUI arrow)
        {
            if (arrow == null) yield break;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / arrowPulseDuration;
                arrow.color = Color.Lerp(arrowHoverColor, arrowIdleColor, t);
                // Escala: crece y vuelve
                float scale = Mathf.Lerp(1.3f, 1f, t);
                arrow.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            arrow.color                   = arrowIdleColor;
            arrow.transform.localScale    = Vector3.one;
        }

        // ─── API pública ──────────────────────────────────────────────────
        public CategoryData CurrentCategory =>
            _currentCategoryIndex < availableCategories.Count
                ? availableCategories[_currentCategoryIndex]
                : null;

        public int CurrentIndex  => _currentCategoryIndex;
        public int TotalCategories => availableCategories.Count;
    }
}
