using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL_LearnVR.Core;
using ASL_LearnVR.Data;
using ASL_LearnVR.Gestures;
using ASL_LearnVR.Feedback;

namespace ASL_LearnVR.LearningModule
{
    /// <summary>
    /// Controls the learning module where the user learns individual signs.
    /// Allows repeating the gesture with ghost hands and practicing with real-time feedback.
    /// </summary>
    public partial class LearningController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text showing the current sign name")]
        [SerializeField] private TextMeshProUGUI signNameText;

        [Tooltip("Text showing the sign description")]
        [SerializeField] private TextMeshProUGUI signDescriptionText;

        [Tooltip("Button 'Repeat' that shows the ghost hands")]
        [SerializeField] private Button repeatButton;

        [Tooltip("Button 'Practice' that enables real-time feedback")]
        [SerializeField] private Button practiceButton;

        [Tooltip("Button to go to self-assessment mode")]
        [SerializeField] private Button selfAssessmentButton;

        [Tooltip("Button to return to level selection")]
        [SerializeField] private Button backButton;

        [Header("Components")]
        [Tooltip("GhostHandPlayer component")]
        [SerializeField] private GhostHandPlayer ghostHandPlayer;

        [Tooltip("GestureRecognizer component for the right hand")]
        [SerializeField] private GestureRecognizer rightHandRecognizer;

        [Tooltip("GestureRecognizer component for the left hand")]
        [SerializeField] private GestureRecognizer leftHandRecognizer;

        [Header("Dynamic Gestures (J, Z)")]
        [Tooltip("DynamicGestureRecognizer component for dynamic gestures")]
        [SerializeField] private ASL.DynamicGestures.DynamicGestureRecognizer dynamicGestureRecognizer;

        [Header("Feedback UI")]
        [Tooltip("Panel showing feedback during practice")]
        [SerializeField] private GameObject feedbackPanel;

        [Tooltip("Feedback text")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Navigation")]
        [Tooltip("Button to go to the next sign")]
        [SerializeField] private Button nextSignButton;

        [Tooltip("Button to go to the previous sign")]
        [SerializeField] private Button previousSignButton;

        [Header("Additional Categories")]
        [Tooltip("Digits category for loading numeric poses (1, 2, 3, etc.)")]
        [SerializeField] private CategoryData digitsCategory;

        [Header("Month Sequence Support")]
        [Tooltip("Component managing month sequence practice (3 letters)")]
        [SerializeField] private MonthPracticeController monthPracticeController;

        [Header("Pedagogical Feedback System")]
        [Tooltip("Pedagogical feedback system (visual + textual + audio)")]
        [SerializeField] private FeedbackSystem feedbackSystem;

        private CategoryData currentCategory;
        private int currentSignIndex = 0;
        private bool isPracticing = false;
        private bool isWaitingForDynamicGesture = false;
        private bool isShowingSuccessMessage = false; // Flag to avoid overwriting success message

        void Start()
        {
            // Get the current category from GameManager
            if (GameManager.Instance != null && GameManager.Instance.CurrentCategory != null)
            {
                currentCategory = GameManager.Instance.CurrentCategory;
            }
            else
            {
                Debug.LogError("LearningController: No category selected in GameManager.");
                return;
            }

            // Configure buttons
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

            // Configure listeners for dynamic gestures
            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.OnGestureStarted   += OnDynamicGestureStarted;
                dynamicGestureRecognizer.OnGestureCompleted += OnDynamicGestureCompleted;
                dynamicGestureRecognizer.OnGestureFailed    += OnDynamicGestureFailed;
            }
            else
            {
                Debug.LogWarning("[LearningController] DynamicGestureRecognizer not assigned - J and Z gestures will not work.");
            }

            // Hide feedback panel on start
            if (feedbackPanel != null)
                feedbackPanel.SetActive(false);

            // Disable gesture recognition on start
            SetRecognitionEnabled(false);

            // If MonthPracticeController exists, configure its recognizers now
            if (monthPracticeController != null)
                monthPracticeController.SetRecognizers(rightHandRecognizer, leftHandRecognizer);

            // Load first sign
            LoadSign(currentSignIndex);
        }

        void OnDestroy()
        {
            // Unsubscribe from dynamic gesture events
            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.OnGestureStarted   -= OnDynamicGestureStarted;
                dynamicGestureRecognizer.OnGestureCompleted -= OnDynamicGestureCompleted;
                dynamicGestureRecognizer.OnGestureFailed    -= OnDynamicGestureFailed;
            }
        }

        /// <summary>
        /// Loads a sign by index.
        /// </summary>
        private void LoadSign(int index)
        {
            if (currentCategory == null || currentCategory.signs == null || currentCategory.signs.Count == 0)
            {
                Debug.LogError("LearningController: No signs in the category.");
                return;
            }

            // Ensure index is within bounds
            currentSignIndex = Mathf.Clamp(index, 0, currentCategory.signs.Count - 1);

            SignData sign = currentCategory.signs[currentSignIndex];

            // Save current sign in GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.CurrentSign = sign;

            // Update UI
            if (signNameText != null)
                signNameText.text = sign.signName;

            if (signDescriptionText != null)
                signDescriptionText.text = sign.description;

            // Configure recognizers with the new sign
            if (sign.requiresMovement)
            {
                // Dynamic gesture: use DynamicGestureRecognizer
                Debug.Log($"[LearningController] Loading dynamic gesture: '{sign.signName}'");

                // For dynamic gestures, configure the initial pose they need
                SignData initialPose = GetInitialPoseForDynamicGesture(sign);

                if (initialPose != null)
                {
                    Debug.Log($"[LearningController] Configuring initial pose '{initialPose.signName}' for dynamic gesture '{sign.signName}'");

                    if (rightHandRecognizer != null)
                        rightHandRecognizer.TargetSign = initialPose;

                    if (leftHandRecognizer != null)
                        leftHandRecognizer.TargetSign = initialPose;
                }
                else
                {
                    Debug.LogError($"[LearningController] Initial pose not found for dynamic gesture '{sign.signName}'");
                }
            }
            else
            {
                // Static gesture: use standard GestureRecognizer
                if (rightHandRecognizer != null)
                    rightHandRecognizer.TargetSign = sign;

                if (leftHandRecognizer != null)
                    leftHandRecognizer.TargetSign = sign;
            }

            // IMPORTANT: If already practicing, also update FeedbackSystem
            if (isPracticing && feedbackSystem != null)
            {
                feedbackSystem.SetCurrentSign(sign);
                Debug.Log($"[LearningController] FeedbackSystem updated with sign: '{sign.signName}'");
            }

            // Update navigation buttons
            UpdateNavigationButtons();

            // If there's a MonthPracticeController, manage its state according to the new sign
            if (monthPracticeController != null)
            {
                if (sign is ASL_LearnVR.Data.MonthSequenceData monthSequence && isPracticing)
                {
                    monthPracticeController.SetRecognizers(rightHandRecognizer, leftHandRecognizer);
                    monthPracticeController.StartPractice(monthSequence);
                }
                else
                {
                    monthPracticeController.OnSignChanged();
                }
            }

            // AUTOMATICALLY show the gesture with guide hands when loading a new sign
            // Only if NOT in practice mode (hands are hidden during practice)
            if (ghostHandPlayer != null)
            {
                if (isPracticing)
                {
                    // In practice mode: keep guide hands hidden but update the sign
                    // so the correct sign shows when practice stops
                    ghostHandPlayer.SetCurrentSign(sign);
                    ghostHandPlayer.SetVisibilityImmediate(false);
                    Debug.Log($"[LearningController] Sign '{sign.signName}' loaded - guide hands hidden (practice mode)");
                }
                else
                {
                    Debug.Log($"[LearningController] Automatically showing gesture '{sign.signName}' with guide hands");
                    ghostHandPlayer.PlaySign(sign);
                }
            }
        }

        /// <summary>
        /// Updates navigation button states.
        /// </summary>
        private void UpdateNavigationButtons()
        {
            if (previousSignButton != null)
                previousSignButton.interactable = currentSignIndex > 0;

            if (nextSignButton != null)
                nextSignButton.interactable = currentSignIndex < currentCategory.signs.Count - 1;
        }

        /// <summary>
        /// Callback when the Repeat button is clicked.
        /// </summary>
        private void OnRepeatButtonClicked()
        {
            Debug.Log($"[LearningController] REPEAT clicked. ghostHandPlayer={ghostHandPlayer != null}, " +
                $"CurrentSign={GameManager.Instance?.CurrentSign?.signName ?? "NULL"}");

            if (ghostHandPlayer != null && GameManager.Instance != null && GameManager.Instance.CurrentSign != null)
            {
                Debug.Log($"[LearningController] Calling PlaySign with sign: '{GameManager.Instance.CurrentSign.signName}'");
                ghostHandPlayer.PlaySign(GameManager.Instance.CurrentSign);
            }
            else
            {
                Debug.LogError($"[LearningController] Cannot play sign. " +
                    $"ghostHandPlayer={ghostHandPlayer != null}, GameManager={GameManager.Instance != null}, " +
                    $"CurrentSign={GameManager.Instance?.CurrentSign != null}");
            }
        }

        /// <summary>
        /// Callback when the Practice button is clicked.
        /// </summary>
        private void OnPracticeButtonClicked()
        {
            isPracticing = !isPracticing;

            SignData currentSign = GameManager.Instance != null ? GameManager.Instance.CurrentSign : null;

            // If the current item is a MonthSequenceData, delegate to MonthPracticeController
            if (currentSign is MonthSequenceData monthSequence && monthPracticeController != null)
            {
                if (isPracticing)
                {
                    Debug.Log($"[LearningController] ====== STARTING MONTH PRACTICE: {monthSequence.signName} ======");

                    if (ghostHandPlayer != null)
                        ghostHandPlayer.FadeOut();

                    if (feedbackPanel != null)
                        feedbackPanel.SetActive(true);

                    // Hide feedback text (MonthTilesUI will show its own UI)
                    if (feedbackText != null)
                        feedbackText.gameObject.SetActive(false);

                    monthPracticeController.SetRecognizers(rightHandRecognizer, leftHandRecognizer);
                    SetRecognitionEnabled(true);
                    monthPracticeController.StartPractice(monthSequence);

                    if (practiceButton != null)
                    {
                        var buttonText = practiceButton.GetComponentInChildren<TextMeshProUGUI>();
                        if (buttonText != null) buttonText.text = "Stop";
                    }
                }
                else
                {
                    Debug.Log("[LearningController] ====== STOPPING MONTH PRACTICE ======");

                    monthPracticeController.StopPractice();
                    SetRecognitionEnabled(false);

                    if (feedbackPanel != null)
                        feedbackPanel.SetActive(false);

                    // Restore feedbackText visibility for normal practice
                    if (feedbackText != null)
                        feedbackText.gameObject.SetActive(true);

                    if (ghostHandPlayer != null)
                        ghostHandPlayer.FadeIn();

                    if (practiceButton != null)
                    {
                        var buttonText = practiceButton.GetComponentInChildren<TextMeshProUGUI>();
                        if (buttonText != null) buttonText.text = "Practice";
                    }
                }

                return;
            }

            // Default behaviour for individual signs
            if (isPracticing)
            {
                if (ghostHandPlayer != null)
                    ghostHandPlayer.FadeOut();

                if (feedbackPanel != null)
                    feedbackPanel.SetActive(true);

                if (feedbackText != null && !feedbackText.gameObject.activeSelf)
                    feedbackText.gameObject.SetActive(true);

                SetRecognitionEnabled(true);

                // Enable pedagogical feedback system
                if (feedbackSystem != null)
                {
                    feedbackSystem.SetDirectFeedbackText(feedbackText);
                    feedbackSystem.SetCurrentSign(currentSign);
                    feedbackSystem.SetActive(true);
                    Debug.Log("[LearningController] FeedbackSystem ENABLED with direct feedbackText");
                }
                else
                {
                    UpdateFeedbackText("Make the sign to practice...");
                }

                if (practiceButton != null)
                {
                    var buttonText = practiceButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null) buttonText.text = "Stop Practice";
                }
            }
            else
            {
                if (feedbackPanel != null)
                    feedbackPanel.SetActive(false);

                SetRecognitionEnabled(false);

                // Disable pedagogical feedback system
                if (feedbackSystem != null)
                {
                    feedbackSystem.SetActive(false);
                    Debug.Log("[LearningController] FeedbackSystem DISABLED");
                }

                if (ghostHandPlayer != null)
                    ghostHandPlayer.FadeIn();

                if (practiceButton != null)
                {
                    var buttonText = practiceButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null) buttonText.text = "Practice";
                }
            }
        }

        /// <summary>
        /// Enables or disables gesture recognition.
        /// </summary>
        private void SetRecognitionEnabled(bool enabled)
        {
            SignData currentSign = GameManager.Instance != null ? GameManager.Instance.CurrentSign : null;

            if (currentSign != null && currentSign.requiresMovement)
            {
                // Dynamic gesture: enable DynamicGestureRecognizer
                Debug.Log($"[LearningController] Recognition of dynamic gesture '{currentSign.signName}': {enabled}");

                if (dynamicGestureRecognizer != null)
                    dynamicGestureRecognizer.SetEnabled(enabled);

                // IMPORTANT: Also enable GestureRecognizer to detect initial pose
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

            // Static gesture: use standard GestureRecognizer
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
        /// Callback when a gesture is detected.
        /// </summary>
        private void OnGestureDetected(SignData sign)
        {
            // Do not overwrite if showing success message
            if (isShowingSuccessMessage) return;

            SignData currentSign = GameManager.Instance != null ? GameManager.Instance.CurrentSign : null;
            if (currentSign == null) return;

            // If current sign is a month sequence, delegate to MonthPracticeController
            if (currentSign is MonthSequenceData && isPracticing && monthPracticeController != null)
            {
                monthPracticeController.ProcessDetection(sign);
                return;
            }

            // If FeedbackSystem is active, let it handle static gesture messages
            if (feedbackSystem != null && feedbackSystem.IsActive && !currentSign.requiresMovement)
                return;

            if (currentSign.requiresMovement)
            {
                // Show initial pose confirmation so user knows they can start moving
                if (feedbackSystem == null || !feedbackSystem.IsActive)
                    UpdateFeedbackText("Initial pose detected — now move!");
                return;
            }
            else if (!currentSign.requiresMovement && currentSign.signName == sign.signName)
            {
                if (feedbackSystem == null || !feedbackSystem.IsActive)
                    UpdateFeedbackText($"Correct! Sign '{sign.signName}' detected.");
            }
        }

        /// <summary>
        /// Callback when a gesture ends.
        /// </summary>
        private void OnGestureEnded(SignData sign)
        {
            SignData currentSign = GameManager.Instance?.CurrentSign;
            if (currentSign is MonthSequenceData) return;

            if (feedbackSystem != null && feedbackSystem.IsActive) return;

            if (!isShowingSuccessMessage)
                UpdateFeedbackText("Make the sign to practice...");
        }

        /// <summary>
        /// Callback when a dynamic gesture starts.
        /// </summary>
        private void OnDynamicGestureStarted(string gestureName)
        {
            Debug.Log($"[LearningController] Dynamic gesture STARTED: {gestureName}");

            if (feedbackSystem != null && feedbackSystem.IsActive) return;

            if (!isShowingSuccessMessage)
                UpdateFeedbackText($"'{gestureName}' started. Keep moving!");
        }

        /// <summary>
        /// Callback when a dynamic gesture is completed.
        /// </summary>
        private void OnDynamicGestureCompleted(string gestureName)
        {
            Debug.Log($"[LearningController] Dynamic gesture COMPLETED: {gestureName}");

            isShowingSuccessMessage = true;
            CancelInvoke(nameof(ClearSuccessMessage));
            Invoke(nameof(ClearSuccessMessage), 3f);

            if (feedbackSystem != null && feedbackSystem.IsActive) return;

            UpdateFeedbackText($"Perfect! '{gestureName}' completed.");
        }

        /// <summary>
        /// Callback when a dynamic gesture fails.
        /// </summary>
        private void OnDynamicGestureFailed(string gestureName, string reason)
        {
            Debug.Log($"[LearningController] Dynamic gesture FAILED: {gestureName} - Reason: {reason}");

            if (feedbackSystem != null && feedbackSystem.IsActive) return;

            if (!isShowingSuccessMessage)
                UpdateFeedbackText($"Try again: '{gestureName}'. {reason}");
        }

        /// <summary>
        /// Updates the feedback text.
        /// </summary>
        private void UpdateFeedbackText(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                Debug.Log($"[LearningController] FEEDBACK UI: '{message}' (isShowingSuccess={isShowingSuccessMessage})");
            }
            else
            {
                Debug.LogWarning("[LearningController] feedbackText is NULL — cannot update UI");
            }
        }

        /// <summary>
        /// Clears the success message after 3 seconds.
        /// </summary>
        private void ClearSuccessMessage()
        {
            isShowingSuccessMessage = false;
            UpdateFeedbackText("Make the sign to practice...");
        }

        /// <summary>
        /// Callback when Next Sign button is clicked.
        /// </summary>
        private void OnNextSignButtonClicked() => LoadSign(currentSignIndex + 1);

        /// <summary>
        /// Callback when Previous Sign button is clicked.
        /// </summary>
        private void OnPreviousSignButtonClicked() => LoadSign(currentSignIndex - 1);

        /// <summary>
        /// Callback when Self-Assessment button is clicked.
        /// </summary>
        private void OnSelfAssessmentButtonClicked()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadSelfAssessmentMode();
            else
                Debug.LogError("LearningController: SceneLoader.Instance is null.");
        }

        /// <summary>
        /// Callback when Back button is clicked.
        /// </summary>
        private void OnBackButtonClicked()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadLevelSelection();
            else
                Debug.LogError("LearningController: SceneLoader.Instance is null.");
        }

        /// <summary>
        /// Returns the initial pose required for a dynamic gesture.
        /// </summary>
        private SignData GetInitialPoseForDynamicGesture(SignData dynamicSign)
        {
            if (dynamicSign == null || currentCategory == null) return null;

            switch (dynamicSign.signName)
            {
                case "White":      return FindSignByName("5");
                case "Orange":     return FindSignByName("O");
                case "J":          return dynamicSign;
                case "Z":          return dynamicSign;
                case "Bye":        return FindSignByName("5");
                case "Brown":      return FindSignByName("B");
                case "Yes":
                case "No":
                case "Hello":
                case "Please":
                case "Thank You":
                case "Good":
                case "Bad":        return dynamicSign;
                default:
                    Debug.LogWarning($"[LearningController] Unknown initial pose for dynamic gesture '{dynamicSign.signName}'");
                    return dynamicSign;
            }
        }

        /// <summary>
        /// Finds a SignData by name in the current category, Digits category, or Resources.
        /// </summary>
        private SignData FindSignByName(string signName)
        {
            if (currentCategory?.signs != null)
                foreach (var s in currentCategory.signs)
                    if (s != null && s.signName == signName) return s;

            if (digitsCategory?.signs != null)
                foreach (var s in digitsCategory.signs)
                    if (s != null && s.signName == signName)
                    {
                        Debug.Log($"[LearningController] Sign '{signName}' found in Digits category");
                        return s;
                    }

            var allSigns = Resources.FindObjectsOfTypeAll<SignData>();
            foreach (var s in allSigns)
                if (s != null && s.signName == signName)
                {
                    Debug.Log($"[LearningController] Sign '{signName}' found via Resources.");
                    return s;
                }

            Debug.LogError($"[LearningController] Sign '{signName}' not found in current category, Digits, or Resources.");
            return null;
        }

        /// <summary>
        /// Switches the active category at runtime without reloading the scene.
        /// Called from CategoryNavigator when the user presses the arrows.
        /// Resets the sign index to 0 and reloads the guide pose.
        /// If practice mode was active it is stopped automatically.
        /// </summary>
        public void SwitchToCategory(CategoryData newCategory)
        {
            if (newCategory == null)
            {
                Debug.LogError("[LearningController] SwitchToCategory: newCategory is null.");
                return;
            }

            if (isPracticing) StopPracticeMode();

            currentCategory  = newCategory;
            currentSignIndex = 0;

            if (GameManager.Instance != null)
                GameManager.Instance.CurrentCategory = newCategory;

            Debug.Log($"[LearningController] Category changed to: {newCategory.categoryName} ({newCategory.signs.Count} signs)");

            LoadSign(0);
        }

        /// <summary>
        /// Stops practice mode cleanly (internal helper).
        /// </summary>
        private void StopPracticeMode()
        {
            isPracticing = false;
            SetRecognitionEnabled(false);

            if (feedbackPanel != null) feedbackPanel.SetActive(false);
            if (ghostHandPlayer != null) ghostHandPlayer.SetVisibilityImmediate(true);

            if (practiceButton != null)
            {
                var tmp = practiceButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null) tmp.text = "Practice";
            }
        }

        /// <summary>
        /// Exposes the current sign index so CategoryNavigator can update the progress bar.
        /// </summary>
        public int CurrentSignIndex => currentSignIndex;

        /// <summary>
        /// Exposes the total number of signs in the current category.
        /// </summary>
        public int TotalSignsInCategory => currentCategory?.signs?.Count ?? 0;
    }
}