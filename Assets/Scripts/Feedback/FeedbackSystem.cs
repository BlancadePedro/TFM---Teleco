using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using ASL_LearnVR.Data;
using ASL_LearnVR.Gestures;
using ASL.DynamicGestures;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Main pedagogical feedback system.
    /// Orchestrates visual, UI and audio feedback to provide explainable guidance to the user.
    ///
    /// Responsibilities:
    /// - Coordinate HandPoseAnalyzer, FeedbackUI, FeedbackAudio and FingerIndicatorVisualizer
    /// - Apply debounce (200ms) to avoid flickering
    /// - Listen to GestureRecognizer and DynamicGestureRecognizer events
    /// - Enable/disable feedback depending on practice mode
    /// </summary>
    public class FeedbackSystem : MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("Hand pose analyzer")]
        [SerializeField] private HandPoseAnalyzer handPoseAnalyzer;

        [Tooltip("Textual feedback UI (optional if using Direct Text)")]
        [SerializeField] private FeedbackUI feedbackUI;

        [Tooltip("Feedback audio")]
        [SerializeField] private FeedbackAudio feedbackAudio;

        [Tooltip("Finger indicator visualizer (fingertip dots)")]
        [SerializeField] private FingerIndicatorVisualizer fingerIndicatorVisualizer;

        [Tooltip("Finger overlay renderer (capsules along the fingers)")]
        [SerializeField] private XRFingerOverlayRenderer fingerOverlayRenderer;

        [Header("Direct Text Output (Alternative to FeedbackUI)")]
        [Tooltip("If assigned, feedback is written directly here (e.g. LearningController feedbackText)")]
        [SerializeField] private TextMeshProUGUI directFeedbackText;

        [Tooltip("Use direct text instead of FeedbackUI")]
        [SerializeField] private bool useDirectText = true;

        [Header("Gesture Recognizers")]
        [Tooltip("GestureRecognizer for right hand")]
        [SerializeField] private GestureRecognizer rightHandRecognizer;

        [Tooltip("GestureRecognizer for left hand (optional)")]
        [SerializeField] private GestureRecognizer leftHandRecognizer;

        [Tooltip("DynamicGestureRecognizer for motion-based gestures")]
        [SerializeField] private DynamicGestureRecognizer dynamicGestureRecognizer;

        [Header("Sampling Settings")]
        [Tooltip("Analysis interval in seconds (debounce)")]
        [SerializeField] private float analysisInterval = 0.2f;

        [Tooltip("Time success feedback is shown before resuming analysis (seconds)")]
        [SerializeField] private float successDisplayDuration = 3f;

        [Header("Dynamic Message Latch")]
        [Tooltip("Minimum hold time for error messages (seconds)")]
        [SerializeField] private float errorMessageHoldMin = 1.0f;

        [Tooltip("Maximum hold time for error messages (seconds)")]
        [SerializeField] private float errorMessageHoldMax = 1.3f;

        [Tooltip("Hold time for dynamic success message (seconds)")]
        [SerializeField] private float dynamicSuccessHoldDuration = 3f;

        [Header("Feedback Stability")]
        [Tooltip("Minimum time an error must persist before being shown")]
        [SerializeField] private float messageEnterDelay = 0.25f;

        [Tooltip("Minimum corrected time before hiding a message")]
        [SerializeField] private float messageExitDelay = 0.45f;

        [Header("Current Sign")]
        [Tooltip("Currently practiced sign")]
        [SerializeField] private SignData currentSign;

        [Header("Events")]
        [Tooltip("Invoked when feedback state changes")]
        public UnityEvent<FeedbackState> onFeedbackStateChanged;

        [Tooltip("Invoked when a gesture is successfully detected")]
        public UnityEvent<SignData> onGestureSuccess;

        [Tooltip("Invoked when the feedback message changes")]
        public UnityEvent<string> onFeedbackMessageChanged;

        // Internal state
        private bool isActive = false;
        private float lastAnalysisTime = 0f;
        private float successEndTime = 0f;
        private FeedbackState currentState = FeedbackState.Inactive;
        private StaticGestureResult lastResult;

        // Dynamic gesture cache
        private DynamicGestureResult lastDynamicResult;
        private bool directTextValidated = false;

        // Dynamic feedback analyzer
        private DynamicGestureFeedbackAnalyzer dynamicFeedbackAnalyzer;

        // Message stability (anti-flicker)
        private readonly Dictionary<string, MessageWindow> messageWindows = new();
        private List<string> lastStableMessages = new();

        // Dynamic gesture hints (J, Z, etc.)
        private readonly Dictionary<string, string> dynamicHints = new()
        {
            { "J", "Dibuja una J con el menique: mueve hacia abajo y curva a la izquierda" },
            { "Z", "Dibuja una Z con el indice: derecha, diagonal abajo, y derecha" }
        };

        private readonly Dictionary<string, DynamicGestureDefinition> gestureDefinitionCache = new();

        // === MESSAGE LATCH (dynamic gestures) ===
        private float messageLatchUntil = 0f;
        private bool lastDynamicMessageWasError = false;
        private bool pendingResetToIdle = false;

        // === Overlay control based on dynamic phase ===
        private bool isDynamicGestureActive = false;

        void Start()
        {
            ValidateComponents();
            EnsureDirectTextMode();

            dynamicFeedbackAnalyzer = new DynamicGestureFeedbackAnalyzer();
            dynamicFeedbackAnalyzer.OnPhaseChanged += OnDynamicPhaseChanged;
            dynamicFeedbackAnalyzer.OnFeedbackMessage += OnDynamicFeedbackMessage;

            SetActive(false);
        }

        void OnEnable()
        {
            SubscribeToEvents();
        }

        void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        void Update()
        {
            if (!isActive)
                return;

            // === MESSAGE LATCH LOGIC ===
            // Check if the latch expired and needs to reset to Idle
            if (pendingResetToIdle && Time.time >= messageLatchUntil)
            {
                HandleLatchExpired();
            }

            // If showing success, wait before re-analyzing
            if (currentState == FeedbackState.Success && Time.time < successEndTime)
                return;

            // Debounce: analizar solo cada analysisInterval segundos
            if (Time.time - lastAnalysisTime < analysisInterval)
                return;

            // Run analysis for static gestures
            if (currentSign != null && !currentSign.requiresMovement)
            {
                AnalyzeCurrentPose();
            }

            lastAnalysisTime = Time.time;
        }

        /// <summary>
        /// Se llama cuando expira el message latch.
        /// Decide si volver a Idle o continuar evaluando.
        /// </summary>
        private void HandleLatchExpired()
        {
            pendingResetToIdle = false;

            // If last message was SUCCESS => return to Idle
            if (!lastDynamicMessageWasError)
            {
                ForceDynamicIdle();
                return;
            }

            // If last message was ERROR => check if initial pose is still valid
            bool startPoseStillValid = CheckStartPoseStillValid();

            if (!startPoseStillValid)
            {
                // Initial pose lost => return to Idle to reposition
                ForceDynamicIdle();
            }
            // If pose is still valid, stay in InProgress (no reset)
        }

        /// <summary>
        /// Verifica si la initial pose del dynamic gesture sigue siendo valid.
        /// </summary>
        private bool CheckStartPoseStillValid()
        {
            if (dynamicGestureRecognizer == null)
                return false;

            // DynamicGestureRecognizer has a method to verify the initial pose
            // If it doesn't exist, assume still valid if gesture is in progress
            return dynamicGestureRecognizer.IsStartPoseValid;
        }

        /// <summary>
        /// Strength la vuelta a Idle para dynamic gestures.
        /// Activa el overlay y resetea el analizador.
        /// </summary>
        private void ForceDynamicIdle()
        {
            isDynamicGestureActive = false;

            // Enable overlay (feedback static visual)
            SetOverlayVisible(true);

            // Resetear analizador de feedback dynamic
            dynamicFeedbackAnalyzer?.Reset();

            // Notificar Idle
            if (currentSign != null)
            {
                dynamicFeedbackAnalyzer?.NotifyIdle(currentSign.signName);
                string message = dynamicFeedbackAnalyzer?.CurrentMessage ?? $"Place your hand for '{currentSign.signName}'";
                UpdateFeedbackMessage(message);
            }

            SetState(FeedbackState.Waiting);

            Debug.Log("[FeedbackSystem] Forced to Idle - overlay ON");
        }

        /// <summary>
        /// Activates or deactivates the visual overlay based on the current phase.
        /// </summary>
        private void SetOverlayVisible(bool visible)
        {
            fingerIndicatorVisualizer?.SetVisible(visible);
            fingerOverlayRenderer?.SetVisible(visible);
        }

        /// <summary>
        /// Validates that all required components are assigned.
        /// </summary>
        private void ValidateComponents()
        {
            if (handPoseAnalyzer == null)
                Debug.LogWarning("[FeedbackSystem] HandPoseAnalyzer not assigned - per-finger error analysis will not work");

            if (feedbackUI == null)
                Debug.LogWarning("[FeedbackSystem] FeedbackUI not assigned - textual feedback will not be shown");

            if (feedbackAudio == null)
                Debug.LogWarning("[FeedbackSystem] FeedbackAudio not assigned - there will be no feedback audio");

            if (fingerIndicatorVisualizer == null)
                Debug.LogWarning("[FeedbackSystem] FingerIndicatorVisualizer not assigned - no habra indicadores visuales por dedo (bolas)");

            if (fingerOverlayRenderer == null)
                Debug.LogWarning("[FeedbackSystem] XRFingerOverlayRenderer not assigned - there will be no color overlays on the fingers (capsulas)");
        }

        /// <summary>
        /// Suscribirse a eventos de los recognizers.
        /// </summary>
        private void SubscribeToEvents()
        {
            // Gestures statics
            if (rightHandRecognizer != null)
            {
                rightHandRecognizer.onGestureDetected.AddListener(OnStaticGestureDetected);
                rightHandRecognizer.onGestureEnded.AddListener(OnStaticGestureEnded);
            }

            if (leftHandRecognizer != null)
            {
                leftHandRecognizer.onGestureDetected.AddListener(OnStaticGestureDetected);
                leftHandRecognizer.onGestureEnded.AddListener(OnStaticGestureEnded);
            }

            // Gestures dynamics
            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.OnGestureStarted += OnDynamicGestureStarted;
                dynamicGestureRecognizer.OnGestureProgress += OnDynamicGestureProgress;
                dynamicGestureRecognizer.OnGestureCompletedStructured += OnDynamicGestureCompletedStructured;
                dynamicGestureRecognizer.OnGestureFailedStructured += OnDynamicGestureFailedStructured;
                // Nuevos eventos para feedback por fases
                dynamicGestureRecognizer.OnInitialPoseDetected += OnInitialPoseDetected;
                dynamicGestureRecognizer.OnGestureProgressWithMetrics += OnDynamicGestureProgressWithMetrics;
                dynamicGestureRecognizer.OnGestureNearCompletion += OnDynamicGestureNearCompletion;
            }
        }

        /// <summary>
        /// Unsubscribe from events.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (rightHandRecognizer != null)
            {
                rightHandRecognizer.onGestureDetected.RemoveListener(OnStaticGestureDetected);
                rightHandRecognizer.onGestureEnded.RemoveListener(OnStaticGestureEnded);
            }

            if (leftHandRecognizer != null)
            {
                leftHandRecognizer.onGestureDetected.RemoveListener(OnStaticGestureDetected);
                leftHandRecognizer.onGestureEnded.RemoveListener(OnStaticGestureEnded);
            }

            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.OnGestureStarted -= OnDynamicGestureStarted;
                dynamicGestureRecognizer.OnGestureProgress -= OnDynamicGestureProgress;
                dynamicGestureRecognizer.OnGestureCompletedStructured -= OnDynamicGestureCompletedStructured;
                dynamicGestureRecognizer.OnGestureFailedStructured -= OnDynamicGestureFailedStructured;
                // Desuscribir nuevos eventos
                dynamicGestureRecognizer.OnInitialPoseDetected -= OnInitialPoseDetected;
                dynamicGestureRecognizer.OnGestureProgressWithMetrics -= OnDynamicGestureProgressWithMetrics;
                dynamicGestureRecognizer.OnGestureNearCompletion -= OnDynamicGestureNearCompletion;
            }
        }

        #region Public API

        /// <summary>
        /// Activa o desactiva el sistema de feedback.
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            EnsureDirectTextMode();
            ClearMessageWindows();

            if (feedbackUI != null)
                feedbackUI.SetVisible(active);

            if (fingerIndicatorVisualizer != null)
                fingerIndicatorVisualizer.SetVisible(active);

            if (fingerOverlayRenderer != null)
                fingerOverlayRenderer.SetVisible(active);

            if (active)
            {
                SetState(FeedbackState.Waiting);
                UpdateFeedbackMessage($"Practice '{currentSign?.signName ?? "sign"}'...");
                if (feedbackUI != null)
                {
                    feedbackUI.SetWaitingState($"Practice '{currentSign?.signName ?? "sign"}'...");
                }
                feedbackAudio?.PlayStartPractice();
            }
            else
            {
                SetState(FeedbackState.Inactive);
            }

            Debug.Log($"[FeedbackSystem] Feedback system: {(active ? "ENABLED" : "DISABLED")}");
        }

        /// <summary>
        /// Establece el current sign a practicar.
        /// IMPORTANT: Make sure the GestureRecognizer also has the same sign configured.
        /// </summary>
        public void SetCurrentSign(SignData sign)
        {
            currentSign = sign;
            ClearMessageWindows();

            // Sync with the recognizers if they are assigned
            // (This is a safety check - LearningController should have already done it)
            if (sign != null && !sign.requiresMovement)
            {
                if (rightHandRecognizer != null && rightHandRecognizer.TargetSign?.signName != sign.signName)
                {
                    rightHandRecognizer.TargetSign = sign;
                    Debug.Log($"[FeedbackSystem] Synchronized rightHandRecognizer with sign: {sign.signName}");
                }
                if (leftHandRecognizer != null && leftHandRecognizer.TargetSign?.signName != sign.signName)
                {
                    leftHandRecognizer.TargetSign = sign;
                    Debug.Log($"[FeedbackSystem] Synchronized leftHandRecognizer with sign: {sign.signName}");
                }
            }

            EnsureDirectTextMode();

            // Para dynamic gestures, mostrar mensaje de Fase 0 (Idle)
            string initialMessage;
            if (sign != null && sign.requiresMovement)
            {
                // Fase 0: Esperando initial pose for dynamic gesture
                dynamicFeedbackAnalyzer?.NotifyIdle(sign.signName);
                initialMessage = dynamicFeedbackAnalyzer?.CurrentMessage ?? $"Place your hand for '{sign.signName}'";
            }
            else
            {
                // Static gesture: mensaje normal
                initialMessage = $"Make the sign '{sign?.signName ?? "sign"}'...";
            }

            UpdateFeedbackMessage(initialMessage);
            if (feedbackUI != null)
            {
                feedbackUI.SetWaitingState(initialMessage);
            }

            // Reset estado al cambiar de signo
            SetState(FeedbackState.Waiting);
            if (fingerIndicatorVisualizer != null)
            {
                fingerIndicatorVisualizer.HideAll();
            }
            if (fingerOverlayRenderer != null)
            {
                fingerOverlayRenderer.ClearAllStatuses();
            }

            Debug.Log($"[FeedbackSystem] Sign configured: {sign?.signName ?? "none"}" +
                     (sign?.requiresMovement == true ? " (dynamic - Phase 0: Idle)" : " (static)"));
        }

        /// <summary>
        /// Assigns the TextMeshProUGUI where feedback will be shown directly.
        /// Useful for reusing the existing feedbackText from LearningController.
        /// </summary>
        public void SetDirectFeedbackText(TextMeshProUGUI text)
        {
            directFeedbackText = text;
            // Si recibimos un texto en runtime, forzamos modo directo y marcamos validado
            if (text != null)
            {
                useDirectText = true;
                directTextValidated = true;
            }
            else
            {
                useDirectText = false;
                directTextValidated = false;
                EnsureDirectTextMode();
            }
        }

        /// <summary>
        /// True if the system is active.
        /// </summary>
        public bool IsActive => isActive;

        /// <summary>
        /// State actual del feedback.
        /// </summary>
        public FeedbackState CurrentState => currentState;

        /// <summary>
        /// Last static analysis result.
        /// </summary>
        public StaticGestureResult LastStaticResult => lastResult;

        /// <summary>
        /// Last dynamic analysis result.
        /// </summary>
        public DynamicGestureResult LastDynamicResult => lastDynamicResult;

        #endregion

        #region Analysis

        /// <summary>
        /// Verifies whether the GestureRecognizer is actively detecting the current sign.
        /// IMPORTANT: Only returns true if the recognizer is configured with the SAME sign as currentSign.
        /// </summary>
        private bool IsGestureCurrentlyDetected()
        {
            if (currentSign == null)
                return false;

            // Verificar con el recognizer de mano derecha
            if (rightHandRecognizer != null && rightHandRecognizer.isActiveAndEnabled)
            {
                // IMPORTANTE: El recognizer DEBE tener el mismo signo que estamos practicando
                if (rightHandRecognizer.TargetSign == null ||
                    rightHandRecognizer.TargetSign.signName != currentSign.signName)
                {
                    Debug.LogWarning($"[FeedbackSystem] Recognizer tiene '{rightHandRecognizer.TargetSign?.signName}' pero deberia tener '{currentSign.signName}'");
                    return false;
                }

                if (rightHandRecognizer.IsPerformed)
                {
                    return true;
                }
            }

            // Verificar con el recognizer de mano izquierda
            if (leftHandRecognizer != null && leftHandRecognizer.isActiveAndEnabled)
            {
                // IMPORTANTE: El recognizer DEBE tener el mismo signo que estamos practicando
                if (leftHandRecognizer.TargetSign == null ||
                    leftHandRecognizer.TargetSign.signName != currentSign.signName)
                {
                    return false;
                }

                if (leftHandRecognizer.IsPerformed)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Analiza la pose actual y actualiza el feedback.
        /// </summary>
        private void AnalyzeCurrentPose()
        {
            if (currentSign == null)
            {
                UpdateFeedbackMessage("No sign selected...");
                return;
            }

            // Get detection state from GestureRecognizer
            bool isDetectedByRecognizer = IsGestureCurrentlyDetected();

            // Si no hay analyzer, mostrar mensaje basado en el recognizer
            if (handPoseAnalyzer == null)
            {
                if (isDetectedByRecognizer)
                {
                    SetState(FeedbackState.Success);
                    UpdateFeedbackMessage($"Correct! '{currentSign.signName}' detected");
                    if (fingerIndicatorVisualizer != null)
                        fingerIndicatorVisualizer.ShowHandCorrect(true);
                    // Mostrar todos los dedos en verde (correct)
                    fingerOverlayRenderer?.SetAllStatuses(
                        FingerOverlayStatus.Correct,
                        FingerOverlayStatus.Correct,
                        FingerOverlayStatus.Correct,
                        FingerOverlayStatus.Correct,
                        FingerOverlayStatus.Correct
                    );
                }
                else
                {
                    SetState(FeedbackState.Waiting);
                    UpdateFeedbackMessage($"Make the sign '{currentSign.signName}'...");
                    if (fingerIndicatorVisualizer != null)
                        fingerIndicatorVisualizer.HideAll();
                    fingerOverlayRenderer?.ClearAllStatuses();
                }
                return;
            }

            // Analizar pose con errores detallados por dedo, pasando el estado del recognizer
            lastResult = handPoseAnalyzer.Analyze(currentSign, isDetectedByRecognizer, useGlobalMatch: true);

            // Generate and show specific message
            string message = GenerateDetailedFeedbackMessage(lastResult);
            lastResult.summaryMessage = message;

            // Actualizar indicadores visuales basados en el resultado
            fingerIndicatorVisualizer?.UpdateFromResult(lastResult);
            fingerOverlayRenderer?.UpdateFromResult(lastResult);

            // Update UI with FeedbackUI if available
            bool shouldUseUI = feedbackUI != null && (!useDirectText || directFeedbackText == null);
            if (shouldUseUI)
                feedbackUI.UpdateFromStaticResult(lastResult);

            UpdateFeedbackMessage(message);

            // Actualizar estado basado en el resultado
            if (lastResult.isMatchGlobal)
            {
                SetState(FeedbackState.Success);
                successEndTime = Time.time + successDisplayDuration;
            }
            else if (lastResult.majorErrorCount > 0)
            {
                SetState(FeedbackState.ShowingErrors);
            }
            else if (lastResult.minorErrorCount > 0)
            {
                SetState(FeedbackState.PartialMatch);
            }
            else
            {
                SetState(FeedbackState.Waiting);
            }
        }

        /// <summary>
        /// Generates a detailed feedback message based on the analysis result.
        /// </summary>
        private string GenerateDetailedFeedbackMessage(StaticGestureResult result)
        {
            if (result == null)
                return $"Make the sign '{currentSign?.signName ?? "sign"}'...";

            // Confirmed success: clear message windows and reinforce positive
            if (result.isMatchGlobal)
            {
                ClearMessageWindows();
                return $"Correct! '{currentSign?.signName}' detected";
            }

            var fingerStates = BuildFingerStates(result);
            var candidates = BuildCandidateMessages(result, fingerStates);
            var stableMessages = ApplyMessageHysteresis(candidates);

            if (stableMessages.Count > 0)
                return string.Join("\n", stableMessages);

            // Fallback si no hay mensajes estables pero hay resumen previo
            if (!string.IsNullOrEmpty(result.summaryMessage))
                return result.summaryMessage;

            return $"Adjust your hand for '{currentSign?.signName ?? "sign"}'...";
        }

        /// <summary>
        /// Construye los estados por dedo (Correct/Casi/Incorrect) a partir del resultado.
        /// </summary>
        private FingerStateSnapshot[] BuildFingerStates(StaticGestureResult result)
        {
            var states = new FingerStateSnapshot[5];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new FingerStateSnapshot
                {
                    finger = (Finger)i,
                    severity = Severity.None,
                    errorType = FingerErrorType.None,
                    message = string.Empty,
                    expectedValue = 0f
                };
            }

            if (result?.perFingerErrors == null)
                return states;

            foreach (var error in result.perFingerErrors)
            {
                if (error.severity == Severity.None)
                    continue;

                int index = (int)error.finger;
                if (index < 0 || index >= states.Length)
                    continue;

                states[index] = new FingerStateSnapshot
                {
                    finger = error.finger,
                    severity = error.severity,
                    errorType = error.errorType,
                    message = error.correctionMessage,
                    expectedValue = error.expectedValue
                };
            }

            return states;
        }

        /// <summary>
        /// Generates message candidates grouped by action and prioritized.
        /// Uses the THREE-STATE philosophy: Extended, Curled, Closed.
        /// </summary>
        private List<string> BuildCandidateMessages(StaticGestureResult result, FingerStateSnapshot[] states)
        {
            var candidates = new List<MessageCandidate>();

            // === Semantic action lists (three states) ===
            var needCurve = new List<Finger>();      // From extended to curled (without closing fist)
            var needFist = new List<Finger>();       // From curled/extended to closed fist
            var needRelease = new List<Finger>();    // From fist to curled (closed too much)
            var needExtend = new List<Finger>();     // De curvado/cerrado a extendido

            // === Lists legacy (compatibilidad) ===
            var needCurl = new List<Finger>();       // Generic: close (legacy)
            var needSpreadNarrow = new List<Finger>();
            var needSpreadWide = new List<Finger>();

            foreach (var state in states)
            {
                if (state.severity == Severity.None)
                    continue;

                switch (state.errorType)
                {
                    // === Three-state semantic errors (PRIORITY) ===
                    case FingerErrorType.NeedsCurve:
                        needCurve.Add(state.finger);
                        break;

                    case FingerErrorType.NeedsFist:
                        needFist.Add(state.finger);
                        break;

                    case FingerErrorType.TooMuchCurl:
                        needRelease.Add(state.finger);
                        break;

                    case FingerErrorType.NeedsExtend:
                        needExtend.Add(state.finger);
                        break;

                    // === Errores legacy ===
                    case FingerErrorType.TooExtended:
                        // Determinar si debe curvar o cerrar basandose en expectedValue
                        if (state.expectedValue >= 0.25f && state.expectedValue <= 0.75f)
                            needCurve.Add(state.finger); // Objetivo es curvado
                        else if (state.expectedValue > 0.75f)
                            needFist.Add(state.finger);  // Objetivo es cerrado
                        else
                            needCurl.Add(state.finger);  // Fallback legacy
                        break;

                    case FingerErrorType.TooCurled:
                        needExtend.Add(state.finger);
                        break;

                    case FingerErrorType.SpreadTooNarrow:
                        needSpreadNarrow.Add(state.finger);
                        break;

                    case FingerErrorType.SpreadTooWide:
                        needSpreadWide.Add(state.finger);
                        break;

                    default:
                        string fallback = !string.IsNullOrEmpty(state.message)
                            ? state.message
                            : FeedbackMessages.GetCorrectionMessage(state.finger, state.errorType);
                        candidates.Add(new MessageCandidate
                        {
                            text = fallback,
                            severityWeight = state.severity == Severity.Major ? 3 : 2,
                            affectedCount = 1,
                            order = 20
                        });
                        break;
                }
            }

            // === Anadir candidatos en orden de prioridad semantica ===
            // CURVA: Prioridad alta - forma controlada sin cerrar
            AddActionCandidate(candidates, needCurve, "Curva", -2, states);

            // SUELTA: Prioridad alta - cerro demasiado, debe relajar
            AddActionCandidate(candidates, needRelease, "Suelta", -1, states);

            // CIERRA: Para formar puno completo
            AddActionCandidate(candidates, needFist, "Cierra", 0, states);

            // EXTIENDE: Abrir dedo
            AddActionCandidate(candidates, needExtend, "Extiende", 1, states);

            // Legacy: cerrar generico
            AddActionCandidate(candidates, needCurl, "Flexiona", 2, states);

            // Spread
            AddActionCandidate(candidates, needSpreadWide, "Junta", 3, states);
            AddActionCandidate(candidates, needSpreadNarrow, "Separa", 4, states);

            int incorrectCount = CountSeverity(states, Severity.Major);
            int almostCount = CountSeverity(states, Severity.Minor);
            int totalIssues = incorrectCount + almostCount;

            // Mensaje de estabilidad cuando solo faltan pequenos ajustes
            if (incorrectCount == 0 && totalIssues > 0)
            {
                candidates.Add(new MessageCandidate
                {
                    text = "Almost there: hold the gesture steady for 0.5 s",
                    severityWeight = 1,
                    affectedCount = totalIssues,
                    order = 40
                });
            }

            // Progress global: cuantos dedos faltan
            if (totalIssues > 0)
            {
                string progress = totalIssues == 1
                    ? "Only 1 adjustment left"
                    : $"You still need to adjust {totalIssues} fingers";

                candidates.Add(new MessageCandidate
                {
                    text = progress,
                    severityWeight = incorrectCount > 0 ? 2 : 1,
                    affectedCount = totalIssues,
                    order = 50
                });
            }
            else
            {
                candidates.Add(new MessageCandidate
                {
                    text = $"Repeat the gesture '{currentSign?.signName ?? "sign"}' without moving for a moment",
                    severityWeight = 1,
                    affectedCount = 1,
                    order = 60
                });
            }

            candidates.Sort((a, b) =>
            {
                int severityCompare = b.severityWeight.CompareTo(a.severityWeight);
                if (severityCompare != 0) return severityCompare;

                int countCompare = b.affectedCount.CompareTo(a.affectedCount);
                if (countCompare != 0) return countCompare;

                return a.order.CompareTo(b.order);
            });

            var uniqueMessages = new List<string>();
            foreach (var candidate in candidates)
            {
                if (uniqueMessages.Contains(candidate.text))
                    continue;

                uniqueMessages.Add(candidate.text);
                if (uniqueMessages.Count >= 3)
                    break;
            }

            return uniqueMessages;
        }

        /// <summary>
        /// Aplica histeresis a los mensajes para evitar parpadeos.
        /// </summary>
        private List<string> ApplyMessageHysteresis(List<string> candidates)
        {
            float now = Time.time;
            var stable = new List<string>();

            // Asegurar que also procesamos mensajes que estaban actives en el frame anterior
            var orderedMessages = new List<string>(candidates);
            foreach (var previous in messageWindows.Keys)
            {
                if (!orderedMessages.Contains(previous))
                    orderedMessages.Add(previous);
            }

            foreach (var message in orderedMessages)
            {
                bool seenNow = candidates.Contains(message);

                if (!messageWindows.TryGetValue(message, out var window))
                {
                    if (!seenNow)
                        continue;

                    window = new MessageWindow { firstSeen = now, lastSeen = now, isActive = false };
                    messageWindows[message] = window;
                }

                if (seenNow)
                {
                    window.lastSeen = now;
                    if (!window.isActive && now - window.firstSeen >= messageEnterDelay)
                    {
                        window.isActive = true;
                    }
                }

                if (!seenNow && window.isActive && now - window.lastSeen >= messageExitDelay)
                {
                    window.isActive = false;
                }

                if (window.isActive)
                {
                    stable.Add(message);
                }

                if (!window.isActive && !seenNow && now - window.lastSeen >= messageExitDelay)
                {
                    messageWindows.Remove(message);
                }
            }

            if (stable.Count == 0 && lastStableMessages.Count > 0)
            {
                stable.AddRange(lastStableMessages);
            }
            else if (stable.Count > 0)
            {
                lastStableMessages = stable;
            }

            return stable;
        }

        /// <summary>
        /// Limpia el estado de histeresis (util al cambiar de signo o tras exito).
        /// </summary>
        private void ClearMessageWindows()
        {
            messageWindows.Clear();
            lastStableMessages.Clear();
        }

        private void AddActionCandidate(List<MessageCandidate> list, List<Finger> fingers, string prefix, int order, FingerStateSnapshot[] states)
        {
            if (fingers == null || fingers.Count == 0)
                return;

            bool hasMajor = HasMajorSeverity(fingers, states);

            // Check if any finger has a specific custom message from the constraint profile.
            // If so, use those messages directly instead of the generic "Action: finger" format.
            var fingersWithCustom = new List<Finger>();
            var fingersWithoutCustom = new List<Finger>();

            foreach (var finger in fingers)
            {
                int idx = (int)finger;
                if (idx >= 0 && idx < states.Length && !string.IsNullOrEmpty(states[idx].message))
                {
                    fingersWithCustom.Add(finger);
                }
                else
                {
                    fingersWithoutCustom.Add(finger);
                }
            }

            // Add custom messages as individual candidates (higher priority)
            foreach (var finger in fingersWithCustom)
            {
                int idx = (int)finger;
                list.Add(new MessageCandidate
                {
                    text = states[idx].message,
                    severityWeight = states[idx].severity == Severity.Major ? 3 : 2,
                    affectedCount = 1,
                    order = order
                });
            }

            // Add remaining fingers without custom messages using generic format
            if (fingersWithoutCustom.Count > 0)
            {
                string message = $"{prefix}: {BuildFingerList(fingersWithoutCustom)}";
                list.Add(new MessageCandidate
                {
                    text = message,
                    severityWeight = hasMajor ? 3 : 2,
                    affectedCount = fingersWithoutCustom.Count,
                    order = order
                });
            }
        }

        private bool HasMajorSeverity(List<Finger> fingers, FingerStateSnapshot[] states)
        {
            foreach (var finger in fingers)
            {
                int index = (int)finger;
                if (index >= 0 && index < states.Length && states[index].severity == Severity.Major)
                    return true;
            }
            return false;
        }

        private int CountSeverity(FingerStateSnapshot[] states, Severity severity)
        {
            int count = 0;
            foreach (var state in states)
            {
                if (state.severity == severity)
                    count++;
            }
            return count;
        }

        private string BuildFingerList(List<Finger> fingers)
        {
            if (fingers == null || fingers.Count == 0)
                return string.Empty;

            var labels = new List<string>();
            foreach (var finger in fingers)
            {
                labels.Add(GetFingerLabel(finger));
            }

            if (labels.Count == 1)
                return labels[0];

            if (labels.Count == 2)
                return $"{labels[0]} y {labels[1]}";

            string leading = string.Join(", ", labels.GetRange(0, labels.Count - 1));
            return $"{leading} y {labels[labels.Count - 1]}";
        }

        private string GetFingerLabel(Finger finger)
        {
            return finger switch
            {
                Finger.Thumb => "pulgar",
                Finger.Index => "indice",
                Finger.Middle => "medio",
                Finger.Ring => "anular",
                Finger.Pinky => "menique",
                _ => "dedo"
            };
        }

        /// <summary>
        /// Obtiene un hint predefinido para dynamic gestures (J, Z, etc.).
        /// </summary>
        private string GetDynamicHint(string gestureName)
        {
            if (string.IsNullOrEmpty(gestureName))
                return string.Empty;

            var upper = gestureName.ToUpper();

            if (dynamicHints.TryGetValue(upper, out var hint))
                return hint;

            // Si el nombre viene con sufijos/prefijos (ej. "J_Right" o "J_Move"), detectar por prefijo
            foreach (var kvp in dynamicHints)
            {
                if (upper.StartsWith(kvp.Key))
                    return kvp.Value;
            }

            return string.Empty;
        }

        private struct FingerStateSnapshot
        {
            public Finger finger;
            public Severity severity;
            public FingerErrorType errorType;
            public string message;
            public float expectedValue;
        }

        private class MessageCandidate
        {
            public string text;
            public int severityWeight;
            public int affectedCount;
            public int order;
        }

        private class MessageWindow
        {
            public float firstSeen;
            public float lastSeen;
            public bool isActive;
        }

        /// <summary>
        /// Establece el estado actual del feedback.
        /// </summary>
        private void SetState(FeedbackState newState)
        {
            if (currentState == newState)
                return;

            currentState = newState;
            onFeedbackStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Actualiza el mensaje de feedback en el texto directo o en FeedbackUI.
        /// </summary>
        private void UpdateFeedbackMessage(string message)
        {
            // Prioridad 1: Text directo (ej: feedbackText de LearningController)
            if (useDirectText && directFeedbackText != null)
            {
                directFeedbackText.text = message;
                Debug.Log($"[FeedbackSystem] Mensaje updated (directo): '{message}'");
            }

            // Siempre sincronizar el panel de UI si existe, para que also muestre el feedback (static o dynamic)
            if (feedbackUI != null)
            {
                switch (currentState)
                {
                    case FeedbackState.Waiting:
                        feedbackUI.SetWaitingState(message);
                        break;
                    case FeedbackState.InProgress:
                        feedbackUI.SetProgressState(message);
                        break;
                    case FeedbackState.ShowingErrors:
                        feedbackUI.SetErrorState(message);
                        break;
                    case FeedbackState.PartialMatch:
                        feedbackUI.SetWarningState(message);
                        break;
                    case FeedbackState.Success:
                        feedbackUI.SetSuccessState(message);
                        break;
                    default:
                        feedbackUI.SetWaitingState(message);
                        break;
                }
                Debug.Log($"[FeedbackSystem] Message updated (UI): '{message}' [State: {currentState}]");
            }
            else if (!(useDirectText && directFeedbackText != null))
            {
                // Solo avisar si no hay ni texto directo ni UI
                Debug.LogWarning($"[FeedbackSystem] No destination for the message: '{message}'");
            }

            // Siempre emitir evento para integracion externa
            onFeedbackMessageChanged?.Invoke(message);
        }

        #endregion

        #region Static Gesture Callbacks

        /// <summary>
        /// Callback cuando un gesto static es detected por el GestureRecognizer.
        /// Este callback solo maneja el audio y el evento externo.
        /// La actualizacion visual se hace en AnalyzeCurrentPose() que ya verifica IsGestureCurrentlyDetected().
        /// </summary>
        private void OnStaticGestureDetected(SignData sign)
        {
            if (!isActive || currentSign == null)
                return;

            // IMPORTANTE: Ignorar si estamos en cooldown de exito
            if (currentState == FeedbackState.Success && Time.time < successEndTime)
            {
                return; // Silenciosamente ignorar - el usuario esta viendo el mensaje de exito
            }

            // IMPORTANTE: Solo procesar si es EXACTAMENTE el signo que estamos practicando
            if (sign == null || sign.signName != currentSign.signName)
            {
                Debug.Log($"[FeedbackSystem] Ignoring detection of '{sign?.signName}' - currently practicing '{currentSign.signName}'");
                return;
            }

            // Reproducir audio de exito (solo una vez cuando se confirma)
            feedbackAudio?.PlaySuccess();

            // Emitir evento para que otros sistemas puedan reaccionar (usar currentSign, no sign)
            onGestureSuccess?.Invoke(currentSign);

            // Forzar feedback visual inmediato en exito para evitar parpadeos rojo/verde
            var successResult = StaticGestureResult.CreateSuccess();
            successResult.summaryMessage = $"Correct! '{currentSign.signName}' detected";
            lastResult = successResult;

            fingerIndicatorVisualizer?.UpdateFromResult(successResult);
            fingerOverlayRenderer?.UpdateFromResult(successResult);
            SetState(FeedbackState.Success);
            successEndTime = Time.time + successDisplayDuration;
            UpdateFeedbackMessage(successResult.summaryMessage);

            // Forzar un analisis inmediato para actualizar el feedback visual
            // Esto evita el delay del analysisInterval
            lastAnalysisTime = 0f; // Reset para permitir analisis inmediato

            Debug.Log($"[FeedbackSystem] Gesture CORRECT detected: '{currentSign.signName}'");
        }

        /// <summary>
        /// Callback cuando un gesto static termina.
        /// </summary>
        private void OnStaticGestureEnded(SignData sign)
        {
            if (!isActive)
                return;

            // Back a estado waiting despues del exito
            if (currentState == FeedbackState.Success && Time.time > successEndTime)
            {
                SetState(FeedbackState.Waiting);
                UpdateFeedbackMessage($"Make the sign '{currentSign?.signName ?? "sign"}'...");

                if (feedbackUI != null)
                    feedbackUI.SetWaitingState();
            }
        }

        #endregion

        #region Dynamic Gesture Callbacks

        /// <summary>
        /// Callback cuando un dynamic gesture inicia.
        /// Fase 1: StartDetected - La initial pose fue detectada.
        /// REGLA: Al entrar en dynamic => overlay OFF
        /// </summary>
        private void OnDynamicGestureStarted(string gestureName)
        {
            if (!isActive)
                return;

            // IMPORTANTE: Ignorar si estamos en cooldown de exito o en message latch
            if (currentState == FeedbackState.Success && Time.time < successEndTime)
            {
                Debug.Log($"[FeedbackSystem] Ignoring start of '{gestureName}' - in success cooldown");
                return;
            }

            if (Time.time < messageLatchUntil)
            {
                Debug.Log($"[FeedbackSystem] Ignoring start of '{gestureName}' - message latch active");
                return;
            }

            // === OVERLAY OFF al entrar en dynamic gesture ===
            isDynamicGestureActive = true;
            SetOverlayVisible(false);

            // Fase 1: Notificar al analizador que la initial pose fue detectada
            dynamicFeedbackAnalyzer?.NotifyStartDetected(gestureName);

            SetState(FeedbackState.InProgress);

            // El mensaje viene del analizador
            string message = dynamicFeedbackAnalyzer?.CurrentMessage ?? $"Good! Now start the movement";
            string hint = GetDynamicHint(gestureName);
            if (!string.IsNullOrEmpty(hint))
            {
                message = hint;
            }
            UpdateFeedbackMessage(message);

            if (feedbackUI != null)
                feedbackUI.SetProgressState(message);

            Debug.Log($"[FeedbackSystem] Phase 1 - StartDetected: {gestureName} (overlay OFF)");
        }

        /// <summary>
        /// Callback de progreso de dynamic gesture (evento basico).
        /// NOTA: Este es un FALLBACK. La logica principal esta en OnDynamicGestureProgressWithMetrics
        /// que recibe la definicion del gesto directamente.
        /// </summary>
        private void OnDynamicGestureProgress(string gestureName, float progress)
        {
            // Este callback es un fallback minimo. La logica principal esta en OnDynamicGestureProgressWithMetrics.
            // Solo actualizamos el mensaje con el hint si no estamos en latch y no hay otro mensaje en curso.
            if (!isActive)
                return;

            bool canEmitMessage = Time.time >= messageLatchUntil;

            if (canEmitMessage)
            {
                // Mostrar hint con porcentaje como mensaje de progreso basico
                string hint = GetDynamicHint(gestureName);
                if (!string.IsNullOrEmpty(hint))
                {
                    string message = $"{hint} ({Mathf.RoundToInt(progress * 100)}%)";
                    UpdateFeedbackMessage(message);
                }
            }
        }

        /// <summary>
        /// Callback cuando un dynamic gesture se completa exitosamente (evento estructurado).
        /// Fase 4: Completed - Gesture correct.
        /// REGLA: Mensaje fijo 3s => luego volver a Idle.
        /// </summary>
        private void OnDynamicGestureCompletedStructured(DynamicGestureResult result)
        {
            if (!isActive || result == null)
                return;

            lastDynamicResult = result;

            // Fase 4: Notificar exito al analizador
            dynamicFeedbackAnalyzer?.NotifyCompleted(result.gestureName, result.metrics);

            SetState(FeedbackState.Success);
            successEndTime = Time.time + dynamicSuccessHoldDuration;

            // === MESSAGE LATCH de 3s para exito ===
            messageLatchUntil = Time.time + dynamicSuccessHoldDuration;
            lastDynamicMessageWasError = false;
            pendingResetToIdle = true; // Al expirar => volver a Idle

            // Reproducir audio de exito
            feedbackAudio?.PlaySuccess();

            // Clear and definitive success message: "Movement recognized!"
            string message = "Movement recognized!";
            UpdateFeedbackMessage(message);

            if (feedbackUI != null)
                feedbackUI.UpdateFromDynamicResult(result);

            // El overlay sigue OFF durante el hold de exito (no mostramos dedos, solo exito global)

            Debug.Log($"[FeedbackSystem] Phase 4 - Completed: {result.gestureName} (hold 3s, then Idle)");
        }

        /// <summary>
        /// Callback cuando un dynamic gesture falla (evento estructurado).
        /// Fase 5: Failed - Explicacion del fallo.
        /// REGLA: Mensaje fijo 1-1.3s => luego volver a Idle (overlay ON).
        /// </summary>
        private void OnDynamicGestureFailedStructured(DynamicGestureResult result)
        {
            if (!isActive || result == null)
                return;

            lastDynamicResult = result;

            // Fase 5: Notificar fallo al analizador con contexto
            dynamicFeedbackAnalyzer?.NotifyFailed(
                result.gestureName,
                result.failureReason,
                result.failedPhase,
                result.metrics
            );

            SetState(FeedbackState.ShowingErrors);

            // === MESSAGE LATCH de 1-1.3s para fallo ===
            float holdDuration = UnityEngine.Random.Range(errorMessageHoldMin, errorMessageHoldMax);
            messageLatchUntil = Time.time + holdDuration;
            lastDynamicMessageWasError = true;
            pendingResetToIdle = true; // Al expirar => volver a Idle

            // Reproducir audio de error (si esta habilitado)
            feedbackAudio?.PlayError();

            // Mensaje de fallo que explica el por que e invita a reintentar
            // Try to get expected direction from gesture definition for specific feedback
            Vector3 expectedDir = Vector3.zero;
            var gestureDef = GetActiveGestureDefinition(result.gestureName);
            if (gestureDef != null)
                expectedDir = gestureDef.primaryDirection;

            string message = dynamicFeedbackAnalyzer?.CurrentMessage ??
                FeedbackMessages.GetFailedMessage(result.failureReason, result.failedPhase, result.metrics, result.gestureName, expectedDir);

            UpdateFeedbackMessage(message);

            if (feedbackUI != null)
                feedbackUI.UpdateFromDynamicResult(result);

            // El overlay sigue OFF durante el hold de fallo

            Debug.Log($"[FeedbackSystem] Phase 5 - Failed: {result.gestureName} - {result.failureReason} (hold {holdDuration:F2}s, then Idle)");

            // NO reseteamos el analizador aqui - lo hacemos cuando expira el latch en HandleLatchExpired
        }

        /// <summary>
        /// Callback cuando cambia la fase del feedback dynamic.
        /// REGLA: Idle => overlay ON, cualquier otra fase => overlay OFF
        /// </summary>
        private void OnDynamicPhaseChanged(DynamicFeedbackPhase phase, string message)
        {
            Debug.Log($"[FeedbackSystem] Cambio de fase dinamica: {phase} - {message}");

            // === LOGICA DE OVERLAY segun fase ===
            bool shouldShowOverlay = (phase == DynamicFeedbackPhase.Idle);
            isDynamicGestureActive = !shouldShowOverlay;
            SetOverlayVisible(shouldShowOverlay);
        }

        /// <summary>
        /// Callback cuando hay un nuevo mensaje de feedback dynamic.
        /// </summary>
        private void OnDynamicFeedbackMessage(string message)
        {
            // El mensaje ya se actualiza en los callbacks principales
            // Este callback es para extensibilidad futura
        }

        /// <summary>
        /// Intenta obtener la definicion del gesto active por nombre.
        /// </summary>
        private ASL.DynamicGestures.DynamicGestureDefinition GetActiveGestureDefinition(string gestureName)
        {
            if (string.IsNullOrEmpty(gestureName))
                return null;

            if (gestureDefinitionCache.TryGetValue(gestureName, out var cached) && cached != null)
                return cached;

            DynamicGestureDefinition found = null;

            if (dynamicGestureRecognizer != null)
            {
                var fieldInfo = typeof(ASL.DynamicGestures.DynamicGestureRecognizer)
                    .GetField("gestureDefinitions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var definitions = fieldInfo?.GetValue(dynamicGestureRecognizer) as List<DynamicGestureDefinition>;
                if (definitions != null)
                {
                    for (int i = 0; i < definitions.Count; i++)
                    {
                        var def = definitions[i];
                        if (def != null && def.gestureName == gestureName)
                        {
                            found = def;
                            break;
                        }
                    }
                }
            }

            if (found == null)
            {
                var loadedDefs = Resources.FindObjectsOfTypeAll<DynamicGestureDefinition>();
                for (int i = 0; i < loadedDefs.Length; i++)
                {
                    var def = loadedDefs[i];
                    if (def != null && def.gestureName == gestureName)
                    {
                        found = def;
                        break;
                    }
                }
            }

            if (found != null)
            {
                gestureDefinitionCache[gestureName] = found;
            }

            return found;
        }

        /// <summary>
        /// Callback cuando la initial pose del dynamic gesture es detectada.
        /// Fase 1: El usuario ha colocado la mano correctamente, puede empezar a moverse.
        /// </summary>
        private void OnInitialPoseDetected(string gestureName)
        {
            if (!isActive)
                return;

            // El analizador ya fue notificado en OnDynamicGestureStarted
            // Este callback es para acciones adicionales si se necesitan
            Debug.Log($"[FeedbackSystem] Pose inicial detectada para '{gestureName}'");
        }

        /// <summary>
        /// Callback de progreso con metricas detalladas.
        /// Este es el callback PRINCIPAL para feedback durante el movimiento.
        /// Recibe la definicion del gesto directamente del recognizer.
        /// </summary>
        private void OnDynamicGestureProgressWithMetrics(
            string gestureName,
            float progress,
            DynamicMetrics metrics,
            ASL.DynamicGestures.DynamicGestureDefinition gestureDefinition)
        {
            if (!isActive)
                return;

            // === LOG de metricas para depuracion (cada 0.5s para no saturar) ===
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[FeedbackSystem] Metricas: vel={metrics.averageSpeed:F3}m/s, dist={metrics.totalDistance:F3}m, " +
                          $"rot={metrics.totalRotation:F1}°, dirChanges={metrics.directionChanges}, " +
                          $"progress={Mathf.RoundToInt(progress * 100)}%, defGesture={(gestureDefinition != null ? gestureDefinition.gestureName : "NULL")}");
            }

            // === MESSAGE LATCH: si estamos en hold, seguir analizando pero no emitir mensajes ===
            bool canEmitMessage = Time.time >= messageLatchUntil;

            // Analizar progreso con metricas completas y definicion del gesto
            dynamicFeedbackAnalyzer?.AnalyzeProgress(gestureName, progress, metrics, gestureDefinition);

            // Obtener el resultado del analizador
            var result = dynamicFeedbackAnalyzer?.GetCurrentResult();
            DynamicMovementIssue issue = result?.issue ?? DynamicMovementIssue.None;
            string message = result?.message ?? "Sigue el movimiento";

            // Si no hay issue, mostrar el hint con porcentaje
            if (issue == DynamicMovementIssue.None)
            {
                string hint = GetDynamicHint(gestureName);
                if (!string.IsNullOrEmpty(hint))
                {
                    message = $"{hint} ({Mathf.RoundToInt(progress * 100)}%)";
                }
            }

            // === EMITIR MENSAJE solo si no estamos en latch ===
            if (canEmitMessage)
            {
                // Si hay un problema detected => activar latch de 1s
                if (issue != DynamicMovementIssue.None)
                {
                    float holdDuration = UnityEngine.Random.Range(errorMessageHoldMin, errorMessageHoldMax);
                    messageLatchUntil = Time.time + holdDuration;
                    lastDynamicMessageWasError = true;
                    pendingResetToIdle = false; // NO volver a Idle, seguir evaluando

                    UpdateFeedbackMessage(message);
                    Debug.Log($"[FeedbackSystem] Issue detected: {issue} - '{message}' (hold {holdDuration:F1}s, progreso continua)");
                }
                else
                {
                    UpdateFeedbackMessage(message);
                }
            }
        }

        /// <summary>
        /// Callback cuando el gesto esta cerca de completarse.
        /// Fase 3: Mensaje de animo para que el usuario termine el movimiento.
        /// </summary>
        private void OnDynamicGestureNearCompletion(string gestureName, float progress)
        {
            if (!isActive)
                return;

            // El analizador ya maneja esto en AnalyzeProgress,
            // pero podemos anadir feedback visual adicional aqui
            Debug.Log($"[FeedbackSystem] Fase 3 - NearCompletion: {gestureName} ({Mathf.RoundToInt(progress * 100)}%)");

            // Optional: feedback visual de "casi completed"
            if (feedbackUI != null)
            {
                feedbackUI.ShowDynamicProgress(gestureName, progress);
            }
        }

        /// <summary>
        /// Parsea el string de razon a enum FailureReason.
        /// Normaliza tildes para matching robusto.
        /// </summary>
        private FailureReason ParseFailureReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                return FailureReason.Unknown;

            // Normalizar: minusculas y quitar tildes
            string lowerReason = RemoveAccents(reason.ToLower());

            if (lowerReason.Contains("pose") && (lowerReason.Contains("perdida") || lowerReason.Contains("lost")))
                return FailureReason.PoseLost;

            if (lowerReason.Contains("velocidad") || lowerReason.Contains("speed"))
            {
                if (lowerReason.Contains("baja") || lowerReason.Contains("low") || lowerReason.Contains("lento"))
                    return FailureReason.SpeedTooLow;
                if (lowerReason.Contains("alta") || lowerReason.Contains("high") || lowerReason.Contains("rapido"))
                    return FailureReason.SpeedTooHigh;
            }

            if (lowerReason.Contains("distancia") || lowerReason.Contains("distance") || lowerReason.Contains("corto"))
                return FailureReason.DistanceTooShort;

            if (lowerReason.Contains("direccion") || lowerReason.Contains("direction"))
            {
                if (lowerReason.Contains("cambios") || lowerReason.Contains("changes") || lowerReason.Contains("insuficientes"))
                    return FailureReason.DirectionChangesInsufficient;
                return FailureReason.DirectionWrong;
            }

            if (lowerReason.Contains("rotacion") || lowerReason.Contains("rotation") || lowerReason.Contains("giro"))
                return FailureReason.RotationInsufficient;

            if (lowerReason.Contains("circular") || lowerReason.Contains("circulo"))
                return FailureReason.NotCircular;

            if (lowerReason.Contains("timeout") || lowerReason.Contains("tiempo") || lowerReason.Contains("excedido"))
                return FailureReason.Timeout;

            if (lowerReason.Contains("tracking") || lowerReason.Contains("visible"))
                return FailureReason.TrackingLost;

            if (lowerReason.Contains("zona") || lowerReason.Contains("espacial") || lowerReason.Contains("fuera"))
                return FailureReason.OutOfZone;

            if (lowerReason.Contains("final") || lowerReason.Contains("end") || lowerReason.Contains("requisitos"))
                return FailureReason.EndPoseMismatch;

            return FailureReason.Unknown;
        }

        /// <summary>
        /// Elimina tildes y diacriticos para matching robusto.
        /// </summary>
        private string RemoveAccents(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace('á', 'a').Replace('à', 'a').Replace('ä', 'a').Replace('â', 'a')
                .Replace('é', 'e').Replace('è', 'e').Replace('ë', 'e').Replace('ê', 'e')
                .Replace('í', 'i').Replace('ì', 'i').Replace('ï', 'i').Replace('î', 'i')
                .Replace('ó', 'o').Replace('ò', 'o').Replace('ö', 'o').Replace('ô', 'o')
                .Replace('ú', 'u').Replace('ù', 'u').Replace('ü', 'u').Replace('û', 'u')
                .Replace('ñ', 'n');
        }

        /// <summary>
        /// Determina la fase donde ocurrio el fallo.
        /// </summary>
        private GesturePhase DetermineFailedPhase(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                return GesturePhase.Move;

            string lowerReason = reason.ToLower();

            if (lowerReason.Contains("inicial") || lowerReason.Contains("start") ||
                lowerReason.Contains("inicio"))
                return GesturePhase.Start;

            if (lowerReason.Contains("final") || lowerReason.Contains("end") ||
                lowerReason.Contains("completar"))
                return GesturePhase.End;

            return GesturePhase.Move;
        }

        /// <summary>
        /// Garantiza que useDirectText solo este active cuando haya un Text assigned; si no, cae al FeedbackPanel de escena.
        /// </summary>
        private void EnsureDirectTextMode()
        {
            if (directTextValidated)
                return;

            if (useDirectText && directFeedbackText == null)
            {
                if (feedbackUI != null)
                {
                    Debug.LogWarning("[FeedbackSystem] useDirectText esta active pero no hay Text assigned. Se usara el FeedbackPanel configured en escena.");
                    useDirectText = false;
                }
            }

            directTextValidated = true;
        }

        #endregion
    }
}
