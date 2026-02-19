using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL_LearnVR.Core;
using ASL_LearnVR.Data;
using ASL_LearnVR.Gestures;
using ASL.DynamicGestures;

namespace ASL_LearnVR.SelfAssessment
{
    /// <summary>
    /// Controls self-assessment mode where the user practices all signs in a category.
    /// Shows a grid of tiles that light up when a gesture is correctly detected.
    /// </summary>
    public class SelfAssessmentController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text showing the category title")]
        [SerializeField] private TextMeshProUGUI categoryTitleText;

        [Tooltip("Grid container for sign tiles")]
        [SerializeField] private Transform gridContainer;

        [Tooltip("Sign tile prefab")]
        [SerializeField] private GameObject signTilePrefab;

        [Tooltip("Button to return to the learning module")]
        [SerializeField] private Button backButton;

        [Header("Components")]
        [Tooltip("MultiGestureRecognizer component that detects all signs")]
        [SerializeField] private MultiGestureRecognizer multiGestureRecognizer;

        [Tooltip("(Optional) Dynamic gesture manager for self-assessment")]
        [SerializeField] private DynamicGesturePracticeManager dynamicGesturePracticeManager;

        [Header("Progress")]
        [Tooltip("Text showing progress")]
        [SerializeField] private TextMeshProUGUI progressText;

        [Tooltip("Progress bar (Slider or Image with fill)")]
        [SerializeField] private Slider progressBar;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        private CategoryData currentCategory;
        private Dictionary<SignData, SignTileController> signTiles = new Dictionary<SignData, SignTileController>();
        private HashSet<SignData> completedSigns = new HashSet<SignData>();
        private SignData lastActiveSign = null;

        void Start()
        {
            // Get the current category from GameManager
            if (GameManager.Instance != null && GameManager.Instance.CurrentCategory != null)
            {
                currentCategory = GameManager.Instance.CurrentCategory;
            }
            else
            {
                Debug.LogError("SelfAssessmentController: No category selected in GameManager.");
                return;
            }

            // Update the title
            if (categoryTitleText != null)
                categoryTitleText.text = $"Self-Assessment: {currentCategory.categoryName}";

            // Generate the sign tiles grid
            GenerateSignTiles();

            // Configure the back button
            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClicked);

            // IMPORTANT: Clear CurrentSign so DynamicGestureRecognizer
            // enters Scene 4 mode (self-assessment: all gestures active)
            GameManager.Instance.CurrentSign = null;

            // Start static gesture recognition
            StartRecognition();

            // Auto-find DynamicGesturePracticeManager if not assigned
            if (dynamicGesturePracticeManager == null)
                dynamicGesturePracticeManager = FindObjectOfType<DynamicGesturePracticeManager>();

            if (dynamicGesturePracticeManager != null)
            {
                // Subscribe to completed dynamic gesture event
                dynamicGesturePracticeManager.OnDynamicGestureCompletedSignal  += OnDynamicGestureCompleted;

                // Subscribe to visual recognition event (instant feedback)
                dynamicGesturePracticeManager.OnDynamicGestureRecognizedSignal += OnDynamicGestureRecognized;

                if (showDebugLogs)
                    Debug.Log("SelfAssessmentController: DynamicGesturePracticeManager found and subscribed.");
            }
            else if (showDebugLogs)
            {
                Debug.Log("SelfAssessmentController: No DynamicGesturePracticeManager found.");
            }

            // AUTO-CONFIGURE: Filter DynamicGestureRecognizer for only the gestures in this category
            AutoConfigureDynamicGestures();

            // Update progress
            UpdateProgress();
        }

        /// <summary>
        /// Dynamically generates grid tiles for each sign in the category.
        /// </summary>
        private void GenerateSignTiles()
        {
            if (gridContainer == null || signTilePrefab == null)
            {
                Debug.LogError("SelfAssessmentController: Missing references for generating tiles.");
                return;
            }

            SetupGridLayout();

            foreach (var sign in currentCategory.signs)
            {
                if (sign == null) continue;

                GameObject tileObj = Instantiate(signTilePrefab, gridContainer);
                SignTileController tileController = tileObj.GetComponent<SignTileController>();

                if (tileController != null)
                {
                    tileController.Initialize(sign);
                    signTiles[sign] = tileController;
                }
            }
        }

        /// <summary>
        /// Configures the GridLayoutGroup to fill the panel.
        /// Fills left-to-right, top-to-bottom, with incomplete rows centred.
        /// </summary>
        private void SetupGridLayout()
        {
            GridLayoutGroup gridLayout = gridContainer.GetComponent<GridLayoutGroup>();

            if (gridLayout == null)
                gridLayout = gridContainer.gameObject.AddComponent<GridLayoutGroup>();

            RectTransform containerRect = gridContainer.GetComponent<RectTransform>();

            gridLayout.cellSize  = new Vector2(100, 120);
            gridLayout.spacing   = new Vector2(15, 15);
            gridLayout.padding   = new RectOffset(20, 20, 20, 20);

            // Flexible constraint so columns adapt to panel width
            gridLayout.constraint      = GridLayoutGroup.Constraint.Flexible;
            gridLayout.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis       = GridLayoutGroup.Axis.Horizontal;

            // Centre incomplete rows
            gridLayout.childAlignment  = TextAnchor.UpperCenter;

            Debug.Log($"SelfAssessmentController: GridLayout configured — panel width: {containerRect.rect.width}");
        }

        /// <summary>
        /// Starts gesture recognition for all signs in the category.
        /// </summary>
        private void StartRecognition()
        {
            if (multiGestureRecognizer != null)
            {
                multiGestureRecognizer.SetTargetSigns(currentCategory.signs);

                multiGestureRecognizer.onGestureDetected.AddListener(OnGestureDetected);
                multiGestureRecognizer.onGestureRecognized.AddListener(OnGestureRecognized);
                multiGestureRecognizer.onGestureLost.AddListener(OnGestureLost);

                if (showDebugLogs)
                    Debug.Log($"SelfAssessmentController: MultiGestureRecognizer configured with {currentCategory.signs.Count} signs.");
            }
            else
            {
                Debug.LogError("SelfAssessmentController: MultiGestureRecognizer is not assigned.");
            }
        }

        /// <summary>
        /// Callback when a DYNAMIC gesture is detected and confirmed.
        /// Called by DynamicGesturePracticeManager.
        /// </summary>
        private void OnDynamicGestureCompleted(SignData sign)
        {
            if (showDebugLogs)
                Debug.Log($"=== OnDynamicGestureCompleted: '{sign?.signName}' ===");

            if (!currentCategory.signs.Contains(sign))
            {
                Debug.LogWarning($"Dynamic gesture '{sign?.signName}' is not in the current category.");
                return;
            }

            if (!completedSigns.Contains(sign))
            {
                completedSigns.Add(sign);

                if (signTiles.ContainsKey(sign))
                    signTiles[sign].SetCompleted(true);

                UpdateProgress();

                if (showDebugLogs)
                    Debug.Log($"SelfAssessmentController: Dynamic gesture '{sign.signName}' completed!");
            }
        }

        /// <summary>
        /// Callback when a DYNAMIC gesture is visually recognised (instant feedback).
        /// Lights up the tile temporarily.
        /// </summary>
        private void OnDynamicGestureRecognized(string gestureName)
        {
            if (showDebugLogs)
                Debug.Log($"=== OnDynamicGestureRecognized: '{gestureName}' ===");

            SignData matchingSign = currentCategory.signs.Find(s =>
                s != null && s.signName.Equals(gestureName, System.StringComparison.OrdinalIgnoreCase));

            if (matchingSign == null)
            {
                Debug.LogWarning($"    -> Dynamic gesture '{gestureName}' not found in category.");
                return;
            }

            // Turn off the previous tile if different
            if (lastActiveSign != null && lastActiveSign != matchingSign && signTiles.ContainsKey(lastActiveSign))
            {
                if (showDebugLogs)
                    Debug.Log($"    -> Hiding previous tile: '{lastActiveSign.signName}'");

                if (!completedSigns.Contains(lastActiveSign))
                    signTiles[lastActiveSign].HideRecognitionFeedback();
            }

            // Light up the current tile if not already completed
            if (signTiles.ContainsKey(matchingSign))
            {
                if (showDebugLogs)
                    Debug.Log($"    -> Showing dynamic tile: '{matchingSign.signName}'");

                if (!completedSigns.Contains(matchingSign))
                    signTiles[matchingSign].ShowRecognitionFeedback();
            }

            lastActiveSign = matchingSign;
        }

        /// <summary>
        /// Callback when a gesture is detected and confirmed (after hold time).
        /// </summary>
        private void OnGestureDetected(SignData sign)
        {
            if (!currentCategory.signs.Contains(sign)) return;

            if (!completedSigns.Contains(sign))
            {
                completedSigns.Add(sign);

                if (signTiles.ContainsKey(sign))
                    signTiles[sign].SetCompleted(true);

                UpdateProgress();

                if (showDebugLogs)
                    Debug.Log($"SelfAssessmentController: Sign '{sign.signName}' completed!");
            }
        }

        /// <summary>
        /// Callback when a gesture is recognised (instant visual feedback).
        /// </summary>
        private void OnGestureRecognized(SignData sign)
        {
            Debug.Log($"=== OnGestureRecognized: '{sign?.signName}' ===");

            if (!currentCategory.signs.Contains(sign))
            {
                Debug.LogWarning($"    -> Sign '{sign?.signName}' is not in the current category.");
                return;
            }

            // Turn off previous tile
            if (lastActiveSign != null && lastActiveSign != sign && signTiles.ContainsKey(lastActiveSign))
            {
                Debug.Log($"    -> Hiding previous tile: '{lastActiveSign.signName}'");
                if (!completedSigns.Contains(lastActiveSign))
                    signTiles[lastActiveSign].HideRecognitionFeedback();
            }

            // Light up current tile
            if (signTiles.ContainsKey(sign))
            {
                Debug.Log($"    -> Showing tile: '{sign.signName}'");
                if (!completedSigns.Contains(sign))
                    signTiles[sign].ShowRecognitionFeedback();
                else
                    Debug.Log($"    -> Tile '{sign.signName}' already completed.");
            }
            else
            {
                Debug.LogError($"    -> No tile exists for '{sign.signName}' in the dictionary.");
            }

            lastActiveSign = sign;
        }

        /// <summary>
        /// Callback when a gesture is lost.
        /// </summary>
        private void OnGestureLost(SignData sign)
        {
            if (!currentCategory.signs.Contains(sign)) return;

            if (signTiles.ContainsKey(sign))
                if (!completedSigns.Contains(sign))
                    signTiles[sign].HideRecognitionFeedback();

            if (lastActiveSign == sign)
                lastActiveSign = null;
        }

        /// <summary>
        /// Updates the progress text and bar.
        /// </summary>
        private void UpdateProgress()
        {
            int total     = currentCategory.GetSignCount();
            int completed = completedSigns.Count;

            if (progressText != null)
                progressText.text = $"Progress: {completed}/{total} ({(total > 0 ? (completed * 100f / total) : 0f):0.#}%)";

            if (progressBar != null)
            {
                progressBar.maxValue = Mathf.Max(1, total);
                progressBar.value    = completed;
            }
        }

        /// <summary>
        /// Auto-configures the DynamicGestureRecognizer to only recognise
        /// dynamic gestures belonging to the current category.
        /// </summary>
        private void AutoConfigureDynamicGestures()
        {
            var dynamicRecognizer = FindObjectOfType<DynamicGestureRecognizer>();
            if (dynamicRecognizer == null)
            {
                if (showDebugLogs)
                    Debug.Log("SelfAssessmentController: No DynamicGestureRecognizer found.");
                return;
            }

            var dynamicSignNames = new HashSet<string>();
            foreach (var sign in currentCategory.signs)
                if (sign != null && sign.requiresMovement)
                    dynamicSignNames.Add(sign.signName);

            if (dynamicSignNames.Count > 0)
            {
                dynamicRecognizer.FilterGesturesByNames(dynamicSignNames);
                dynamicRecognizer.SetEnabled(true);

                if (showDebugLogs)
                    Debug.Log($"SelfAssessmentController: DynamicGestureRecognizer filtered to {dynamicSignNames.Count} gestures.");
            }
            else
            {
                // No dynamic gestures in this category — disable completely
                dynamicRecognizer.SetEnabled(false);

                if (showDebugLogs)
                    Debug.Log($"SelfAssessmentController: Category '{currentCategory.categoryName}' has no dynamic gestures — DynamicGestureRecognizer DISABLED.");
            }
        }

        /// <summary>
        /// Callback when the Back button is clicked.
        /// </summary>
        private void OnBackButtonClicked()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadLearningModule();
            else
                Debug.LogError("SelfAssessmentController: SceneLoader.Instance is null.");
        }

        void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackButtonClicked);

            if (multiGestureRecognizer != null)
            {
                multiGestureRecognizer.onGestureDetected.RemoveListener(OnGestureDetected);
                multiGestureRecognizer.onGestureRecognized.RemoveListener(OnGestureRecognized);
                multiGestureRecognizer.onGestureLost.RemoveListener(OnGestureLost);
            }

            if (dynamicGesturePracticeManager != null)
            {
                dynamicGesturePracticeManager.OnDynamicGestureCompletedSignal  -= OnDynamicGestureCompleted;
                dynamicGesturePracticeManager.OnDynamicGestureRecognizedSignal -= OnDynamicGestureRecognized;
            }

            // Restore full dynamic gesture list when leaving the scene
            var dynamicRecognizer = FindObjectOfType<DynamicGestureRecognizer>();
            if (dynamicRecognizer != null)
                dynamicRecognizer.RestoreAllGestures();
        }

        /// <summary>Number of signs correctly completed in this session.</summary>
        public int CompletedCount => completedSigns?.Count ?? 0;

        /// <summary>Total number of signs in the current category.</summary>
        public int TotalCount => currentCategory?.GetSignCount() ?? 0;
    }
}
