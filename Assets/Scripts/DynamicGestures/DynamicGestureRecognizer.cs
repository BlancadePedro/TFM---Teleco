using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Management;
using ASL_LearnVR.Data;
using ASL_LearnVR.Feedback;

namespace ASL.DynamicGestures
{
    /// <summary>
    /// Dynamic gesture recognizer based on static pose sequences + movement.
    /// Optimized for Meta Quest 3 with conservative parameters and a robust state machine.
    /// </summary>
    public class DynamicGestureRecognizer : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("List de definiciones de dynamic gestures a reconocer")]
        [SerializeField] private List<DynamicGestureDefinition> gestureDefinitions = new List<DynamicGestureDefinition>();

        [Tooltip("Adapter for detecting static poses (StaticPoseAdapter or SingleGestureAdapter)")]
        [SerializeField] private MonoBehaviour poseAdapterComponent;

        [Tooltip("Component XRHandTrackingEvents para verificar tracking (Right Hand Controller)")]
        [SerializeField] private UnityEngine.XR.Hands.XRHandTrackingEvents handTrackingEvents;

        [Header("Suavizado")]
        [Tooltip("Position smoothing factor (0.5-0.7 recommended for Quest 3)")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float positionSmoothingFactor = 0.7f;

        [Header("Debug")]
        [Tooltip("Enable detailed logs and Gizmo visualization")]
        [SerializeField] private bool debugMode = false;

        // Public events (original API - maintain compatibility)
        public System.Action<string> OnGestureStarted;
        public System.Action<string, float> OnGestureProgress; // nombre, progreso 0-1
        public System.Action<string> OnGestureCompleted;
        public System.Action<string, string> OnGestureFailed; // name, reason
        public System.Action<bool> OnPendingConfirmationChanged; // true = entered pending, false = exited pending

        // Events estructurados (nuevos - para FeedbackSystem)
        /// <summary>
        /// Event con resultado estructurado al completar un gesto.
        /// Includes detailed movement metrics.
        /// </summary>
        public System.Action<DynamicGestureResult> OnGestureCompletedStructured;

        /// <summary>
        /// Event con resultado estructurado al fallar un gesto.
        /// Includes failure reason, phase, and metrics.
        /// </summary>
        public System.Action<DynamicGestureResult> OnGestureFailedStructured;

        // Events para feedback por fases (nuevos)
        /// <summary>
        /// Event cuando la initial pose es detectada correctamente.
        /// Se emite ANTES de que comience el movimiento (Fase 1: StartDetected).
        /// </summary>
        public System.Action<string> OnInitialPoseDetected;

        /// <summary>
        /// Progress event with detailed metrics.
        /// Includes name, progress, metrics, and gesture definition for feedback analysis.
        /// </summary>
        public System.Action<string, float, DynamicMetrics, DynamicGestureDefinition> OnGestureProgressWithMetrics;

        /// <summary>
        /// Event when gesture is close to completion (>80%).
        /// Usado para feedback de "casi completed" (Fase 3: NearCompletion).
        /// </summary>
        public System.Action<string, float> OnGestureNearCompletion;

        // State interno
        private bool isEnabled = true; // Activado por defecto
        private GestureState currentState = GestureState.Idle;
        private DynamicGestureDefinition activeGesture = null;
        private MovementTracker movementTracker;
        private float gestureStartTime = 0f;
        private List<DynamicGestureDefinition> allGestureDefinitions = null; // List completa sin filtrar

        // Cooldown after success (so the user can see the message)
        private float successCooldownEndTime = 0f;
        private const float SUCCESS_COOLDOWN_DURATION = 1f; // 1s (previously 2s) - allows consecutive gestures faster in Scene 4

        /// <summary>
        /// True if the recognizer is in cooldown after a success.
        /// </summary>
        public bool IsInSuccessCooldown => Time.time < successCooldownEndTime;

        /// <summary>
        /// True if the initial pose of the active gesture is still valid.
        /// Usado por FeedbackSystem para decidir si volver a Idle tras un error.
        /// </summary>
        public bool IsStartPoseValid
        {
            get
            {
                // Si no estamos en progreso, no hay pose que validar
                if (currentState != GestureState.InProgress || activeGesture == null)
                    return false;

                if (poseAdapter == null)
                    return false;

                string currentPose = poseAdapter.GetCurrentPoseName();

                // Si no hay pose detectada, no es valid
                if (string.IsNullOrEmpty(currentPose))
                    return false;

                // Check if the current pose can still start the active gesture
                return activeGesture.CanStartWithPose(currentPose) || CanStartWithPoseData(activeGesture);
            }
        }

        // Pending confirmation state
        private List<DynamicGestureDefinition> pendingGestures = new List<DynamicGestureDefinition>();
        // Alternative gestures when disambiguation couldn't resolve (compound gestures with same Start pose)
        private List<DynamicGestureDefinition> alternativeGestures = new List<DynamicGestureDefinition>();
        private float pendingStartTime = 0f;
        private const float PENDING_CONFIRMATION_TIMEOUT = 0.4f; // 0.4s for Scene 3 (1 specific gesture)
        private const float PENDING_CONFIRMATION_TIMEOUT_SCENE4 = 1.2f; // 1.2s for Scene 4 (disambiguation between multiple gestures)

        // Tracking de mano
        private Vector3 smoothedHandPosition = Vector3.zero;
        private Quaternion smoothedHandRotation = Quaternion.identity;
        private Vector3 lastHandPosition = Vector3.zero;
        private Quaternion lastHandRotation = Quaternion.identity;
        private float trackingLostTime = 0f;
        private const float TRACKING_LOSS_TOLERANCE = 0.5f; // 0.5s (previously 0.2s) - more tolerant of momentary losses on Quest 3

        // Cooldown after PendingConfirmation times out
        private float pendingTimeoutCooldownEnd = 0f;
        private const float PENDING_TIMEOUT_COOLDOWN = 0.6f;

        // Detection and prevention of PENDING loop
        private int consecutivePendingEntries = 0;
        private float lastPendingExitTime = 0f;
        private const float PENDING_REENTRY_WINDOW = 1.0f; // Ventana de 1 segundo
        private const int MAX_CONSECUTIVE_PENDING = 2; // Maximum 2 re-entradas antes de bloquear

        // Cache XR Origin
        private Transform xrOriginTransform;

        // Requirements validation
        private bool initialDirectionValidated = false;
        private float initialDirectionGracePeriod = 0.5f; // 50% of minDuration before failing on direction

        // Cache del adaptador como interfaz
        private IPoseAdapter poseAdapter;

        // Cache of the last joints event for direct HandShape validation
        private XRHandJointsUpdatedEventArgs lastJointsEventArgs;
        private bool hasValidJointsEventArgs = false;

        void Awake()
        {
            // Initialize MovementTracker with conservative parameters
            movementTracker = new MovementTracker(windowSize: 3f, historySize: 120);

            // Cachear XR Origin
            if (Camera.main != null)
            {
                xrOriginTransform = Camera.main.transform.parent; // XR Origin es parent de Camera
            }

            // AUTO-FIX: Find pose adapter if not correctly assigned
            if (poseAdapterComponent == null || (poseAdapterComponent as IPoseAdapter) == null)
            {
                Debug.LogWarning("[DynamicGestureRecognizer] Auto-buscando adaptador de poses...");

                // PRIORIDAD 1: Buscar StaticPoseAdapter (ideal para Scene 4 - multi-gesto)
                var staticAdapter = FindObjectOfType<ASL.DynamicGestures.StaticPoseAdapter>();
                if (staticAdapter != null)
                {
                    poseAdapterComponent = staticAdapter;
                    Debug.Log($"[DynamicGestureRecognizer] StaticPoseAdapter found (modo multi-gesto)");
                }
                else
                {
                    // PRIORIDAD 2: Buscar SingleGestureAdapter en RightHandRecognizer
                    var rightHandRecognizer = GameObject.Find("RightHandRecognizer");
                    if (rightHandRecognizer != null)
                    {
                        var adapter = rightHandRecognizer.GetComponent<ASL.DynamicGestures.SingleGestureAdapter>();
                        if (adapter != null)
                        {
                            poseAdapterComponent = adapter;
                            Debug.Log($"[DynamicGestureRecognizer] SingleGestureAdapter found en RightHandRecognizer");
                        }
                        else
                        {
                            Debug.LogError("[DynamicGestureRecognizer] RightHandRecognizer no tiene SingleGestureAdapter!");
                        }
                    }
                    else
                    {
                        Debug.LogError("[DynamicGestureRecognizer] No found adaptador de poses!");
                    }
                }
            }

            // Obtener el adaptador como interfaz
            if (poseAdapterComponent != null)
            {
                poseAdapter = poseAdapterComponent as IPoseAdapter;
                if (poseAdapter == null)
                {
                    Debug.LogError("[DynamicGestureRecognizer] El componente assigned no implementa IPoseAdapter. " +
                                   "Usa StaticPoseAdapter o SingleGestureAdapter.");
                }
                else
                {
                    Debug.Log("[DynamicGestureRecognizer] Pose Adapter configured correctamente!");
                }
            }
        }

        void OnEnable()
        {
            if (poseAdapter == null)
            {
                Debug.LogError("[DynamicGestureRecognizer] Falta asignar Pose Adapter (StaticPoseAdapter o SingleGestureAdapter) en el Inspector!");
            }

            if (handTrackingEvents == null)
            {
                Debug.LogError("[DynamicGestureRecognizer] Falta asignar XRHandTrackingEvents en el Inspector!");
            }
            else
            {
                // Subscribe to jointsUpdated to cache the last event (direct End pose validation)
                handTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
            }

            if (gestureDefinitions.Count == 0)
            {
                Debug.LogWarning("[DynamicGestureRecognizer] No gestures defined en la lista.");
            }
        }

        void OnDisable()
        {
            if (handTrackingEvents != null)
            {
                handTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);
            }
        }

        /// <summary>
        /// Callback when hand joints are updated. Caches the event for direct validation.
        /// </summary>
        private void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            lastJointsEventArgs = eventArgs;
            hasValidJointsEventArgs = true;
        }

        void Update()
        {
            // ONLY work if enabled
            if (!isEnabled)
                return;

            // IMPORTANT: Do not process new gestures during success cooldown
            // This allows the user to see the message "Movement recognized!" for 2 seconds
            if (IsInSuccessCooldown)
            {
                return;
            }

            // Verificar tracking con tolerancia
            if (handTrackingEvents == null || !handTrackingEvents.handIsTracked)
            {
                // Si hay un gesto en progreso, dar un margen de tolerancia
                if (currentState == GestureState.InProgress)
                {
                    if (trackingLostTime == 0f)
                    {
                        trackingLostTime = Time.time;
                        if (debugMode)
                        {
                            Debug.LogWarning($"[TRACKING] Hand tracking LOST during {activeGesture?.gestureName}, waiting {TRACKING_LOSS_TOLERANCE}s...");
                        }
                    }

                    float lossTime = Time.time - trackingLostTime;
                    if (lossTime < TRACKING_LOSS_TOLERANCE)
                    {
                        // Continue using the last known position
                        return;
                    }

                    if (debugMode)
                    {
                        Debug.LogError($"[TRACKING] Tracking lost for {lossTime:F2}s, failing gesture");
                    }
                }

                HandleTrackingLost();
                return;
            }

            // Tracking recuperado
            trackingLostTime = 0f;

            // Get hand position and rotation
            Vector3 currentHandPos = GetHandPosition();
            Quaternion currentHandRot = GetHandRotation();

            // Suavizado para reducir jitter
            if (smoothedHandPosition == Vector3.zero)
            {
                smoothedHandPosition = currentHandPos;
                smoothedHandRotation = currentHandRot;
                if (debugMode)
                {
                    Debug.Log($"[TRACKING] Initial smoothed position: {smoothedHandPosition}");
                }
            }
            else
            {
                Vector3 beforeSmoothing = smoothedHandPosition;
                smoothedHandPosition = Vector3.Lerp(smoothedHandPosition, currentHandPos, positionSmoothingFactor);
                smoothedHandRotation = Quaternion.Slerp(smoothedHandRotation, currentHandRot, positionSmoothingFactor);

                if (debugMode && activeGesture != null)
                {
                    float distanceMoved = Vector3.Distance(beforeSmoothing, smoothedHandPosition);
                    Debug.Log($"[TRACKING] Smoothed: Before={beforeSmoothing}, After={smoothedHandPosition}, Moved={distanceMoved:F4}m, Speed={movementTracker.CurrentSpeed:F4}m/s");
                }
            }

            // Actualizar tracker de movimiento
            movementTracker.UpdateTracking(smoothedHandPosition, smoothedHandRotation);

            // State machine
            switch (currentState)
            {
                case GestureState.Idle:
                    CheckForGestureStart();
                    break;

                case GestureState.PendingConfirmation:
                    UpdatePendingConfirmation();
                    break;

                case GestureState.InProgress:
                    UpdateGestureProgress();
                    break;
            }
        }

        /// <summary>
        /// Busca si alguna pose actual puede iniciar un gesto
        /// </summary>
        private void CheckForGestureStart()
        {
            if (poseAdapter == null)
                return;

            // Respect cooldown after PendingConfirmation timeout
            // Esto evita el loop Idle→Pending→timeout→Idle→Pending
            if (Time.time < pendingTimeoutCooldownEnd)
                return;

            // CRITICAL FILTER: Only apply in learning mode (Scene 3)
            // In self-assessment mode (Scene 4), CurrentSign can be null or static, so we allow everything
            var gameManager = ASL_LearnVR.Core.GameManager.Instance;
            if (gameManager != null && gameManager.CurrentSign != null)
            {
                // Si el current sign NO requiere movimiento, NO iniciar dynamic gestures
                // PERO solo aplicar este filtro si estamos en modo aprendizaje individual
                if (!gameManager.CurrentSign.requiresMovement)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] CurrentSign '{gameManager.CurrentSign.signName}' NO requiere movimiento, bloqueando reconocimiento dynamic");
                    }
                    return;
                }
            }
            else if (debugMode && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[DynamicGesture] CurrentSign es NULL, permitiendo reconocimiento dynamic (modo autoevaluacion)");
            }

            string currentPose = poseAdapter.GetCurrentPoseName();

            // SPECIAL SCENE 4 MODE: If CurrentSign is NULL (self-assessment),
            // considerar TODOS los gestos como candidatos para disambiguation por movimiento
            bool isScene4Mode = (gameManager == null || gameManager.CurrentSign == null);

            // En Scene 3, necesitamos una pose detectada para continuar.
            // En Scene 4, permitimos continuar incluso sin pose detectada por nombre,
            // porque CanStartWithPoseData puede validar directamente el HandShape del gesto compuesto
            // (ej: Bye empieza con "5" que puede no estar en MultiGestureRecognizer).
            if (string.IsNullOrEmpty(currentPose) && !isScene4Mode)
                return;

            // En Scene 4 sin pose y sin joints valids, no hay nada que hacer
            if (string.IsNullOrEmpty(currentPose) && !hasValidJointsEventArgs)
                return;

            // DEBUG: Log de pose detectada
            if (debugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[DynamicGesture] CheckForGestureStart: Current pose = '{currentPose ?? "NULL (poseData mode)"}', Gestures definidos = {gestureDefinitions.Count}");
            }

            // Buscar TODOS los gestos que puedan iniciarse con esta pose
            pendingGestures.Clear();

            List<DynamicGestureDefinition> candidateGestures;
            if (!isScene4Mode)
            {
                string currentSignName = gameManager.CurrentSign.signName;

                bool MatchesCurrent(string gestureName)
                {
                    return gestureName.Equals(currentSignName, System.StringComparison.OrdinalIgnoreCase) ||
                           (currentSignName.Equals("Grey", System.StringComparison.OrdinalIgnoreCase) &&
                            gestureName.Equals("Gray", System.StringComparison.OrdinalIgnoreCase)) ||
                           (currentSignName.Equals("Gray", System.StringComparison.OrdinalIgnoreCase) &&
                            gestureName.Equals("Grey", System.StringComparison.OrdinalIgnoreCase));
                }

                var focused = gestureDefinitions.Find(g => g != null && MatchesCurrent(g.gestureName));
                candidateGestures = focused != null ? new List<DynamicGestureDefinition> { focused } : gestureDefinitions;
            }
            else
            {
                candidateGestures = gestureDefinitions;
            }

            foreach (var gesture in candidateGestures)
            {
                if (gesture == null)
                    continue;

                bool canStart = false;

                if (isScene4Mode)
                {
                    // En Scene 4: Filtrar por pose detectada, luego desambiguar por movimiento
                    // PRIORITY 1: Compare with current adapter pose (if available)
                    if (!string.IsNullOrEmpty(currentPose))
                    {
                        canStart = gesture.CanStartWithPose(currentPose);
                    }

                    // PRIORITY 2: Direct validation with HandShape (if gesture has poseData)
                    if (!canStart)
                    {
                        canStart = CanStartWithPoseData(gesture);
                    }
                }
                else
                {
                    // En Scene 3: Primero intentar match exacto de pose
                    canStart = gesture.CanStartWithPose(currentPose);

                    // GESTOS COMPUESTOS: Si no hay match por nombre pero hay poseData,
                    // attempt direct validation using HandShape
                    if (!canStart)
                    {
                        canStart = CanStartWithPoseData(gesture);
                    }
                }

                if (canStart)
                {
                    pendingGestures.Add(gesture);
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] Gesture '{gesture.gestureName}' puede iniciar con pose '{currentPose}' (Scene4Mode: {isScene4Mode})");
                    }
                }
                else if (debugMode && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[DynamicGesture] Gesture '{gesture.gestureName}' NO puede iniciar con pose '{currentPose}'");
                }
            }

            // If we found candidate gestures, enter pending confirmation state
            if (pendingGestures.Count > 0)
            {
                if (debugMode)
                {
                    Debug.Log($"[DynamicGesture] Encontrados {pendingGestures.Count} gestos candidatos para pose '{currentPose}'");
                }

                if (pendingGestures.Count == 1)
                {
                    // Solo un gesto posible, iniciarlo directamente
                    StartGesture(pendingGestures[0]);
                }
                else
                {
                    // Multiple possible gestures, enter pending state to disambiguate
                    EnterPendingConfirmation();
                }
            }
            else if (debugMode && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[DynamicGesture] Ningun dynamic gesture puede iniciar con pose '{currentPose}'");
            }
        }

        /// <summary>
        /// Inicia el reconocimiento de un gesto
        /// </summary>
        private void StartGesture(DynamicGestureDefinition gesture)
        {
            bool wasPending = currentState == GestureState.PendingConfirmation;

            activeGesture = gesture;
            currentState = GestureState.InProgress;
            gestureStartTime = Time.time;

            movementTracker.Reset();
            initialDirectionValidated = false;

            // Notify that we left pending state if we were in it
            if (wasPending)
            {
                OnPendingConfirmationChanged?.Invoke(false);
            }

            // Emitir evento de initial pose detectada (Fase 1 del feedback)
            OnInitialPoseDetected?.Invoke(gesture.gestureName);

            OnGestureStarted?.Invoke(gesture.gestureName);

            if (debugMode)
            {
                Debug.Log($"[DynamicGesture] INICIADO: {gesture.gestureName}");
            }
        }

        /// <summary>
        /// Enters pending confirmation state when there are multiple gesture candidates
        /// </summary>
        private void EnterPendingConfirmation()
        {
            // Detectar loop de PENDING: si re-entramos muchas veces seguidas, bloquear
            if (Time.time - lastPendingExitTime < PENDING_REENTRY_WINDOW)
            {
                consecutivePendingEntries++;

                if (consecutivePendingEntries >= MAX_CONSECUTIVE_PENDING)
                {
                    // Bloquear por 2 segundos para romper el loop
                    pendingTimeoutCooldownEnd = Time.time + 2.0f;
                    consecutivePendingEntries = 0;

                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] Loop de PENDING detected ({MAX_CONSECUTIVE_PENDING}+ re-entradas en {PENDING_REENTRY_WINDOW}s), bloqueando por 2s");
                    }
                    return;
                }
            }
            else
            {
                consecutivePendingEntries = 0;
            }

            currentState = GestureState.PendingConfirmation;
            pendingStartTime = Time.time;

            // Resetear el tracker para empezar a detectar movimiento
            movementTracker.Reset();

            // Notificar que entramos en pending (bloquear MultiGestureRecognizer)
            OnPendingConfirmationChanged?.Invoke(true);

            if (debugMode)
            {
                string gestureNames = string.Join(", ", pendingGestures.ConvertAll(g => g.gestureName));
                Debug.Log($"[DynamicGesture] PENDING: Desambiguando entre: {gestureNames}");
            }
        }

        /// <summary>
        /// Updates the pending confirmation state, waiting for movement to disambiguate
        /// </summary>
        private void UpdatePendingConfirmation()
        {
            float elapsed = Time.time - pendingStartTime;

            // Use longer timeout in Scene 4 to allow disambiguation time
            var gm = ASL_LearnVR.Core.GameManager.Instance;
            bool isScene4 = (gm == null || gm.CurrentSign == null);
            float timeout = isScene4 ? PENDING_CONFIRMATION_TIMEOUT_SCENE4 : PENDING_CONFIRMATION_TIMEOUT;

            // Timeout: si no se pudo desambiguar a tiempo
            if (elapsed >= timeout)
            {
                // Check if any candidate has End poses (compound gesture).
                // Para compound gestures (Bye:5→S, White:5→White, Sleep:5→White, Thursday:T→H),
                // la disambiguation por movimiento no funciona porque comparten el mismo perfil.
                // In this case, start the first candidate and save the rest as alternatives.
                // The End pose will determine which is the correct gesture.
                bool hasCompoundCandidate = false;
                DynamicGestureDefinition compoundCandidate = null;
                foreach (var gesture in pendingGestures)
                {
                    var endPoses = gesture.GetPosesForTiming(PoseTimingRequirement.End);
                    foreach (var ep in endPoses)
                    {
                        if (!ep.isOptional)
                        {
                            hasCompoundCandidate = true;
                            if (compoundCandidate == null)
                                compoundCandidate = gesture;
                            break;
                        }
                    }
                }

                if (hasCompoundCandidate && compoundCandidate != null)
                {
                    // Save alternatives (the other candidates with End poses)
                    alternativeGestures.Clear();
                    foreach (var gesture in pendingGestures)
                    {
                        if (gesture != compoundCandidate)
                            alternativeGestures.Add(gesture);
                    }

                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] PENDING TIMEOUT: Starting gesto compuesto '{compoundCandidate.gestureName}' con {alternativeGestures.Count} alternativas. " +
                                  $"La End pose determinara el gesto correct.");
                    }
                    StartGesture(compoundCandidate);
                }
                else
                {
                    // No hay compound gestures, asumir gesto static
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] PENDING TIMEOUT: Sin movimiento detected, asumiendo gesto static");
                    }
                    ResetState();
                }
                return;
            }

            // Check if the initial pose was lost
            if (poseAdapter != null)
            {
                string currentPose = poseAdapter.GetCurrentPoseName();

                // Validar que la pose sigue siendo valid
                var gameManager = ASL_LearnVR.Core.GameManager.Instance;
                bool isScene4Mode = (gameManager == null || gameManager.CurrentSign == null);

                // Si perdimos completamente la pose por nombre
                if (string.IsNullOrEmpty(currentPose))
                {
                    // In Scene 4: before resetting, check if pose is still valid via poseData
                    if (isScene4Mode && hasValidJointsEventArgs)
                    {
                        bool anyPoseDataValid = false;
                        foreach (var gesture in pendingGestures)
                        {
                            if (CanStartWithPoseData(gesture))
                            {
                                anyPoseDataValid = true;
                                break;
                            }
                        }
                        if (!anyPoseDataValid)
                        {
                            if (debugMode)
                                Debug.Log($"[DynamicGesture] PENDING: Pose perdida (poseData also invalid), reseteando");
                            ResetState();
                            return;
                        }
                        // Pose still valid via poseData, continue
                    }
                    else
                    {
                        if (debugMode)
                            Debug.Log($"[DynamicGesture] PENDING: Pose completamente perdida, reseteando");
                        ResetState();
                        return;
                    }
                }

                bool poseStillValid = false;

                foreach (var gesture in pendingGestures)
                {
                    // Verificar por nombre de pose
                    if (!string.IsNullOrEmpty(currentPose) && gesture.CanStartWithPose(currentPose))
                    {
                        poseStillValid = true;
                        break;
                    }

                    // En Scene 4: also verificar directamente con HandShape
                    if (isScene4Mode && CanStartWithPoseData(gesture))
                    {
                        poseStillValid = true;
                        break;
                    }
                }

                if (!poseStillValid && !string.IsNullOrEmpty(currentPose))
                {
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] PENDING: Pose inicial perdida, reseteando");
                    }
                    ResetState();
                    return;
                }
            }

            // Analizar si hay movimiento significativo
            // Thresholdes intermedios: filtran jitter (~1-2cm) pero detectan movimiento intencional
            bool hasSignificantMovement = movementTracker.TotalDistance > 0.025f || // 2.5cm (filtra jitter, detecta movimiento real)
                                          movementTracker.CurrentSpeed > 0.08f;      // 0.08 m/s

            if (hasSignificantMovement)
            {
                // Movement detected, now trying to disambiguate which gesture it is
                DynamicGestureDefinition bestMatch = DisambiguateGesture();

                if (bestMatch != null)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] PENDING RESOLVED: '{bestMatch.gestureName}' selected por movimiento");
                    }
                    StartGesture(bestMatch);
                }
                else
                {
                    // No se pudo desambiguar por movimiento.
                    // Para compound gestures con same Start pose (Bye/White/Sleep todos empiezan con "5"),
                    // no esperar al timeout: iniciar inmediatamente y dejar que la End pose determine el gesto.
                    bool hasCompound = false;
                    DynamicGestureDefinition firstCompound = null;
                    foreach (var g in pendingGestures)
                    {
                        var eps = g.GetPosesForTiming(PoseTimingRequirement.End);
                        foreach (var ep in eps)
                        {
                            if (!ep.isOptional) { hasCompound = true; if (firstCompound == null) firstCompound = g; break; }
                        }
                    }

                    if (hasCompound && firstCompound != null)
                    {
                        alternativeGestures.Clear();
                        foreach (var g in pendingGestures)
                        {
                            if (g != firstCompound)
                                alternativeGestures.Add(g);
                        }
                        if (debugMode)
                        {
                            Debug.Log($"[DynamicGesture] PENDING: Desambiguacion imposible, iniciando gesto compuesto '{firstCompound.gestureName}' " +
                                      $"con {alternativeGestures.Count} alternativas. End pose determinara el gesto.");
                        }
                        StartGesture(firstCompound);
                    }
                    else if (debugMode && Time.frameCount % 30 == 0)
                    {
                        Debug.Log($"[DynamicGesture] PENDING: Esperando mas datos de movimiento...");
                    }
                }
            }
        }

        /// <summary>
        /// Intenta desambiguar entre multiples gestos candidatos basandose en el movimiento detected
        /// </summary>
        private DynamicGestureDefinition DisambiguateGesture()
        {
            if (pendingGestures.Count == 0)
                return null;

            // Si solo queda uno, retornarlo
            if (pendingGestures.Count == 1)
                return pendingGestures[0];

            // PRIORIDAD 1: Detectar movimiento CIRCULAR primero (Please)
            foreach (var gesture in pendingGestures)
            {
                if (gesture.requiresCircularMotion)
                {
                    float circularityScore = movementTracker.GetCircularityScore();
                    // Si hay indicios de circularidad (score > 0.15), seleccionarlo inmediatamente
                    if (circularityScore > 0.15f)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[DynamicGesture] Desambiguacion: '{gesture.gestureName}' selected por circularidad (score: {circularityScore:F2})");
                        }
                        return gesture;
                    }
                }
            }

            // PRIORIDAD 2: Detectar ROTACION clara (Blue wrist twist, Purple shake, etc.)
            // Threshold de 20° para evitar falsos positivos por movimiento natural de la mano
            foreach (var gesture in pendingGestures)
            {
                if (gesture.requiresRotation && movementTracker.TotalRotation > 20f)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] Desambiguacion: '{gesture.gestureName}' selected por rotacion ({movementTracker.TotalRotation:F1}°)");
                    }
                    return gesture;
                }
            }

            // PRIORIDAD 3: Detectar CAMBIOS DE DIRECCION con direccion especifica
            foreach (var gesture in pendingGestures)
            {
                if (gesture.requiresDirectionChange && movementTracker.DirectionChanges > 0)
                {
                    Vector3 currentDirection = movementTracker.AverageDirection;

                    // Si el gesto tiene direccion especifica, verificar que coincida
                    if (currentDirection.sqrMagnitude > 0.01f && gesture.primaryDirection.sqrMagnitude > 0.01f)
                    {
                        float angleDiff = Vector3.Angle(currentDirection, gesture.primaryDirection);
                        if (angleDiff <= gesture.directionTolerance)
                        {
                            if (debugMode)
                            {
                                Debug.Log($"[DynamicGesture] Desambiguacion: '{gesture.gestureName}' selected por cambios de direccion ({movementTracker.DirectionChanges}) + direccion ({angleDiff:F1}°)");
                            }
                            return gesture;
                        }
                    }
                    // Si el gesto NO tiene direccion especifica (wrist twist puro),
                    // seleccionar si hay direccion changes + rotacion (ej: Blue, Purple)
                    else if (gesture.primaryDirection.sqrMagnitude < 0.01f && gesture.requiresRotation &&
                             movementTracker.TotalRotation > 12f)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[DynamicGesture] Desambiguacion: '{gesture.gestureName}' selected por cambios de direccion + rotacion (twist)");
                        }
                        return gesture;
                    }
                }
            }

            // PRIORIDAD 4: Analizar por direccion principal (Brown down, ThankYou forward, etc.)
            Vector3 currentDir = movementTracker.AverageDirection;

            if (currentDir.sqrMagnitude < 0.01f)
                return null; // Sin direccion clara aun

            // Buscar el gesto con mejor match de direccion
            DynamicGestureDefinition bestMatch = null;
            float bestAngleDifference = float.MaxValue;

            foreach (var gesture in pendingGestures)
            {
                // Si el gesto no requiere movimiento direccional especifico, saltarlo
                if (gesture.primaryDirection.sqrMagnitude < 0.01f)
                    continue;

                float angleDiff = Vector3.Angle(currentDir, gesture.primaryDirection);

                if (angleDiff < bestAngleDifference && angleDiff <= gesture.directionTolerance)
                {
                    bestAngleDifference = angleDiff;
                    bestMatch = gesture;
                }
            }

            if (bestMatch != null && debugMode)
            {
                Debug.Log($"[DynamicGesture] Desambiguacion: '{bestMatch.gestureName}' selected por direccion (angulo: {bestAngleDifference:F1}°)");
            }

            return bestMatch;
        }

        /// <summary>
        /// Valida requisitos y actualiza progreso del gesto active
        /// </summary>
        private void UpdateGestureProgress()
        {
            if (activeGesture == null)
            {
                currentState = GestureState.Idle;
                return;
            }

            float elapsed = Time.time - gestureStartTime;

            if (debugMode)
            {
                Debug.Log($"[PROGRESS] {activeGesture.gestureName}: Elapsed={elapsed:F2}s, TotalDistance={movementTracker.TotalDistance:F4}m, " +
                          $"CurrentSpeed={movementTracker.CurrentSpeed:F4}m/s, DirChanges={movementTracker.DirectionChanges}");
            }

            // Timeout
            if (elapsed > activeGesture.maxDuration)
            {
                FailGesture("Timeout excedido");
                return;
            }

            // Validar poses intermedias (During)
            if (!ValidatePoses(PoseTimingRequirement.During))
            {
                FailGesture("Pose intermedia perdida");
                return;
            }

            // Validar requisitos de movimiento
            if (activeGesture.requiresMovement)
            {
                // IMPORTANTE: Si el gesto requiere cambios de direccion (zigzag), NO validar direccion estrictamente
                // Pero si solo requiere rotacion (curva), SI validar direccion principal
                bool shouldValidateDirection = !activeGesture.requiresDirectionChange;

                if (shouldValidateDirection)
                {
                    // Dar margen inicial antes de validar direccion
                    if (elapsed >= initialDirectionGracePeriod * activeGesture.minDuration)
                    {
                        if (!ValidateMovementDirection())
                        {
                            if (debugMode)
                            {
                                Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Wrong direction. Esperada: {activeGesture.primaryDirection}, Current: {movementTracker.AverageDirection}");
                            }
                            // No fallar inmediatamente, dar mas margen
                            if (elapsed > activeGesture.minDuration * 0.9f)
                            {
                                FailGesture("Incorrect movement direction");
                                return;
                            }
                        }
                        else
                        {
                            initialDirectionValidated = true;
                        }
                    }
                }
                else
                {
                    // Si no validamos direccion, marcarla como validada automaticamente
                    initialDirectionValidated = true;

                    if (debugMode && elapsed < 0.1f)
                    {
                        Debug.Log($"[DynamicGesture] {activeGesture.gestureName}: Validacion de direccion DESACTIVADA (gesto con rotacion/cambios)");
                    }
                }

                // Validar velocidad (con margen del 50%)
                if (!ValidateSpeed())
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Speed baja. Current: {movementTracker.CurrentSpeed:F3} m/s, " +
                                         $"Minimum: {activeGesture.minSpeed * 0.5f:F3} m/s");
                    }
                }
            }

            // Validar cambios de direccion
            if (activeGesture.requiresDirectionChange)
            {
                if (!ValidateDirectionChanges())
                {
                    // No fallar inmediatamente - dar tiempo suficiente para que el usuario
                    // complete el movimiento de ida y vuelta (ej: Drink va hacia la boca y vuelve).
                    // Usar el mayor entre minDuration*2 y 1.0s como umbral minimo.
                    float dirChangeDeadline = Mathf.Max(activeGesture.minDuration * 2f, 1.0f);
                    // No exceder el 80% de maxDuration
                    dirChangeDeadline = Mathf.Min(dirChangeDeadline, activeGesture.maxDuration * 0.8f);

                    if (elapsed > dirChangeDeadline)
                    {
                        FailGesture($"Insufficient direction changes ({movementTracker.DirectionChanges}/{activeGesture.requiredDirectionChanges})");
                        return;
                    }
                }
            }

            // Validar rotacion
            if (activeGesture.requiresRotation)
            {
                if (!ValidateRotation())
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Rotation insuficiente. " +
                                         $"Current: {movementTracker.TotalRotation:F1}°, " +
                                         $"Minimum: {activeGesture.minRotationAngle:F1}°");
                    }
                }
            }

            // Validar movimiento circular
            if (activeGesture.requiresCircularMotion)
            {
                if (!ValidateCircularity())
                {
                    float circDeadline = Mathf.Max(activeGesture.minDuration * 2f, 1.0f);
                    circDeadline = Mathf.Min(circDeadline, activeGesture.maxDuration * 0.8f);
                    if (elapsed > circDeadline)
                    {
                        FailGesture($"Movimiento no circular (score: {movementTracker.GetCircularityScore():F2})");
                        return;
                    }
                }
            }

            // Validar zona espacial (segun timing)
            if (activeGesture.requiresSpatialZone)
            {
                bool shouldValidateZone = activeGesture.zoneValidationTiming == PoseTimingRequirement.During ||
                                          (activeGesture.zoneValidationTiming == PoseTimingRequirement.Start && elapsed < 0.3f) ||
                                          (activeGesture.zoneValidationTiming == PoseTimingRequirement.End && elapsed >= activeGesture.minDuration * 0.8f);

                if (shouldValidateZone && !IsInSpatialZone(smoothedHandPosition))
                {
                    FailGesture("Fuera de zona espacial requerida");
                    return;
                }
            }

            // Emitir progreso
            float progress = Mathf.Clamp01(elapsed / activeGesture.minDuration);
            OnGestureProgress?.Invoke(activeGesture.gestureName, progress);

            // Emitir progreso con metricas para feedback detallado
            DynamicMetrics currentMetrics = GetCurrentMetrics();
            OnGestureProgressWithMetrics?.Invoke(activeGesture.gestureName, progress, currentMetrics, activeGesture);

            // Emitir evento de "casi completed" cuando supera el 80%
            if (progress >= 0.8f && progress < 1.0f)
            {
                OnGestureNearCompletion?.Invoke(activeGesture.gestureName, progress);
            }

            // Verificar completed
            if (elapsed >= activeGesture.minDuration)
            {
                // Validaciones finales
                bool finalValidation = true;
                bool endPosePending = false;

                // Distance minima
                if (activeGesture.requiresMovement && movementTracker.TotalDistance < activeGesture.minDistance)
                {
                    finalValidation = false;
                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Distance insuficiente al completar. " +
                                         $"Current: {movementTracker.TotalDistance:F3}m, Minimum: {activeGesture.minDistance:F3}m");
                    }
                }

                // Direction changes requeridos
                if (activeGesture.requiresDirectionChange && !ValidateDirectionChanges())
                {
                    finalValidation = false;
                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Insufficient direction changes al completar. " +
                                         $"Current: {movementTracker.DirectionChanges}, Requeridos: {activeGesture.requiredDirectionChanges}");
                    }
                }

                // Rotation requerida
                if (activeGesture.requiresRotation && !ValidateRotation())
                {
                    finalValidation = false;
                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Rotation insuficiente al completar. " +
                                         $"Current: {movementTracker.TotalRotation:F1}°, Minimum: {activeGesture.minRotationAngle:F1}°");
                    }
                }

                // Circularidad requerida
                if (activeGesture.requiresCircularMotion && !ValidateCircularity())
                {
                    finalValidation = false;
                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Circularidad insuficiente al completar. " +
                                         $"Score: {movementTracker.GetCircularityScore():F2}, Minimum: {activeGesture.minCircularityScore:F2}");
                    }
                }

                // Poses finales: verificar End pose del gesto active Y de alternativas
                bool endPoseMatched = ValidatePoses(PoseTimingRequirement.End);

                if (!endPoseMatched && alternativeGestures.Count > 0)
                {
                    // El End pose no coincide con el gesto active.
                    // Verificar si coincide con algun gesto alternativo (misma Start pose, diferente End pose).
                    // Ej: Activo=Bye(5→S), pero el usuario hizo White(5→White).
                    foreach (var alt in alternativeGestures)
                    {
                        var altEndPoses = alt.GetPosesForTiming(PoseTimingRequirement.End);
                        bool altMatches = true;
                        foreach (var ep in altEndPoses)
                        {
                            if (ep.isOptional) continue;
                            bool epMatch = false;
                            if (ep.poseData != null)
                            {
                                epMatch = ValidatePoseDirectly(ep.poseData);
                            }
                            else
                            {
                                string currentPose = poseAdapter != null ? poseAdapter.GetCurrentPoseName() : null;
                                epMatch = !string.IsNullOrEmpty(currentPose) && ep.IsValidPose(currentPose);
                            }
                            if (!epMatch) { altMatches = false; break; }
                        }

                        if (altMatches && altEndPoses.Count > 0)
                        {
                            if (debugMode)
                            {
                                Debug.Log($"[DynamicGesture] End pose coincide con alternativa '{alt.gestureName}' instead of '{activeGesture.gestureName}'. Cambiando gesto active.");
                            }
                            activeGesture = alt;
                            endPoseMatched = true;
                            break;
                        }
                    }
                }

                if (!endPoseMatched)
                {
                    finalValidation = false;
                    // Verificar si el gesto tiene End poses requeridas (compound gestures)
                    var endPoses = activeGesture.GetPosesForTiming(PoseTimingRequirement.End);
                    bool hasRequiredEndPoses = false;
                    foreach (var ep in endPoses)
                    {
                        if (!ep.isOptional) { hasRequiredEndPoses = true; break; }
                    }
                    if (hasRequiredEndPoses)
                    {
                        endPosePending = true;
                    }
                }

                if (finalValidation)
                {
                    CompleteGesture();
                }
                else
                {
                    // Requisitos NO cumplidos aun: NO fallar inmediatamente.
                    // Seguir esperando - el siguiente frame volvera a evaluar.
                    // Los fallos se manejan por:
                    //   - maxDuration timeout (linea ~913)
                    //   - Direction change deadline (linea ~963)
                    //   - Circularity deadline (linea ~995)
                    // Esto permite que:
                    //   - Gestures compuestos (Bye, White) esperen la End pose
                    //   - Gestures con directionChange (Drink, Red) esperen el cambio de direccion
                    //   - Gestures con rotacion (Blue, Purple) esperen la rotacion
                    if (debugMode && Time.frameCount % 30 == 0)
                    {
                        Debug.Log($"[DynamicGesture] {activeGesture.gestureName}: Esperando requisitos... " +
                                  $"Elapsed={elapsed:F2}s, MaxDuration={activeGesture.maxDuration:F1}s");
                    }
                }
            }
        }

        /// <summary>
        /// Valida poses estaticas segun timing
        /// </summary>
        private bool ValidatePoses(PoseTimingRequirement timing)
        {
            if (poseAdapter == null || activeGesture == null)
                return true;

            var requiredPoses = activeGesture.GetPosesForTiming(timing);

            if (requiredPoses.Count == 0)
                return true;

            string currentPose = poseAdapter.GetCurrentPoseName();

            foreach (var poseReq in requiredPoses)
            {
                if (poseReq.isOptional)
                    continue;

                bool matches = false;

                // SOLUCION PARA GESTOS COMPUESTOS:
                // Para poses End, si hay un poseData assigned, validar directamente con HandShape
                // Esto permite detectar transiciones como 5→S (White), O→S (Orange), T→H (Thursday)
                if (timing == PoseTimingRequirement.End && poseReq.poseData != null)
                {
                    matches = ValidatePoseDirectly(poseReq.poseData);

                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] {activeGesture.gestureName}: Validacion DIRECTA de End pose '{poseReq.poseName}' = {matches}");
                    }
                }
                else
                {
                    // Fallback: si no hay poseData pero el CurrentSign coincide por nombre,
                    // validar directamente usando ese SignData para evitar ambiguedades (ej: Gray).
                    SignData fallbackSignData = null;
                    var gm = ASL_LearnVR.Core.GameManager.Instance;
                    if (gm != null && gm.CurrentSign != null &&
                        gm.CurrentSign.signName.Equals(poseReq.poseName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        fallbackSignData = gm.CurrentSign;
                    }

                    if (fallbackSignData != null && timing == PoseTimingRequirement.Start)
                    {
                        matches = ValidatePoseDirectly(fallbackSignData);
                    }
                    else
                    {
                        // Validacion tradicional usando poseAdapter, aceptando familias de pose
                        matches = !string.IsNullOrEmpty(currentPose) && poseReq.IsValidPose(currentPose);
                    }
                }

                if (!matches)
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Pose requerida '{poseReq.poseName}' no detectada. " +
                                         $"Current: '{currentPose}', PoseData: {(poseReq.poseData != null ? poseReq.poseData.signName : "null")}");
                    }
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifica si un gesto puede iniciar usando validacion directa de poseData.
        /// Usado for gestures compuestos donde el Start pose es diferente del TargetSign.
        /// </summary>
        private bool CanStartWithPoseData(DynamicGestureDefinition gesture)
        {
            if (gesture == null)
                return false;

            var startPoses = gesture.GetPosesForTiming(PoseTimingRequirement.Start);

            if (startPoses.Count == 0)
                return false;

            var gm = ASL_LearnVR.Core.GameManager.Instance;

            foreach (var poseReq in startPoses)
            {
                // PRIORIDAD 1: Si tiene poseData, validar directamente con HandShape
                if (poseReq.poseData != null)
                {
                    bool matches = ValidatePoseDirectly(poseReq.poseData);
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] CanStartWithPoseData: '{gesture.gestureName}' Start pose '{poseReq.poseName}' validacion directa = {matches}");
                    }

                    if (matches)
                        return true;
                }

                // PRIORIDAD 2 (Scene 3): Intentar con GameManager.CurrentSign
                if (gm != null && gm.CurrentSign != null &&
                    gm.CurrentSign.signName.Equals(poseReq.poseName, System.StringComparison.OrdinalIgnoreCase))
                {
                    bool matches = ValidatePoseDirectly(gm.CurrentSign);
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] CanStartWithPoseData: fallback CurrentSign '{gm.CurrentSign.signName}' = {matches}");
                    }

                    if (matches)
                        return true;
                }

                // PRIORIDAD 3 (Scene 4): Buscar el SignData por nombre en la category actual
                if (gm != null && gm.CurrentCategory != null && gm.CurrentSign == null)
                {
                    // 3a: Buscar por poseName del gesture definition
                    var signByName = gm.CurrentCategory.GetSignByName(poseReq.poseName);
                    if (signByName != null)
                    {
                        bool matches = ValidatePoseDirectly(signByName);
                        if (debugMode)
                        {
                            Debug.Log($"[DynamicGesture] CanStartWithPoseData: fallback category '{poseReq.poseName}' = {matches}");
                        }

                        if (matches)
                            return true;
                    }

                    // 3b: Buscar por gestureName (ej: Brown_Gesture.gestureName="Brown" → buscar Sign_Brown)
                    if (signByName == null || poseReq.poseName != gesture.gestureName)
                    {
                        var signByGestureName = gm.CurrentCategory.GetSignByName(gesture.gestureName);
                        if (signByGestureName != null)
                        {
                            bool matches = ValidatePoseDirectly(signByGestureName);
                            if (debugMode)
                            {
                                Debug.Log($"[DynamicGesture] CanStartWithPoseData: fallback gestureName '{gesture.gestureName}' = {matches}");
                            }

                            if (matches)
                                return true;
                        }
                    }

                }
            }

            return false;
        }

        /// <summary>
        /// Valida una pose directamente usando el HandShape del SignData contra XRHandSubsystem.
        /// Esto permite detectar poses End en compound gestures sin depender del poseAdapter.
        /// </summary>
        private bool ValidatePoseDirectly(SignData signData)
        {
            if (signData == null || signData.handShapeOrPose == null)
            {
                if (debugMode)
                    Debug.LogWarning($"[DynamicGesture] ValidatePoseDirectly: SignData o handShapeOrPose es null");
                return false;
            }

            // Verificar que tenemos el evento de joints cacheado
            if (!hasValidJointsEventArgs)
            {
                if (debugMode)
                    Debug.LogWarning($"[DynamicGesture] ValidatePoseDirectly: No hay evento de joints cacheado");
                return false;
            }

            // Obtener HandShape o HandPose del SignData
            var handShape = signData.GetHandShape();
            var handPose = signData.GetHandPose();

            if (handShape != null)
            {
                // Usar CheckConditions del XRHandShape con el evento cacheado
                bool result = handShape.CheckConditions(lastJointsEventArgs);
                if (debugMode)
                    Debug.Log($"[DynamicGesture] ValidatePoseDirectly: HandShape '{signData.signName}' CheckConditions = {result}");
                return result;
            }
            else if (handPose != null)
            {
                // Usar CheckConditions del XRHandPose con el evento cacheado
                bool result = handPose.CheckConditions(lastJointsEventArgs);
                if (debugMode)
                    Debug.Log($"[DynamicGesture] ValidatePoseDirectly: HandPose '{signData.signName}' CheckConditions = {result}");
                return result;
            }

            if (debugMode)
                Debug.LogWarning($"[DynamicGesture] ValidatePoseDirectly: '{signData.signName}' no tiene HandShape ni HandPose");
            return false;
        }

        /// <summary>
        /// Valida direccion de movimiento
        /// </summary>
        private bool ValidateMovementDirection()
        {
            if (activeGesture == null || activeGesture.primaryDirection.sqrMagnitude < 0.01f)
                return true;

            return movementTracker.IsMovingInDirection(activeGesture.primaryDirection, activeGesture.directionTolerance);
        }

        /// <summary>
        /// Valida velocidad minima (con margen del 50%)
        /// </summary>
        private bool ValidateSpeed()
        {
            if (activeGesture == null)
                return true;

            return movementTracker.CurrentSpeed >= activeGesture.minSpeed * 0.5f;
        }

        /// <summary>
        /// Valida cambios de direccion
        /// </summary>
        private bool ValidateDirectionChanges()
        {
            if (activeGesture == null)
                return true;

            return movementTracker.DirectionChanges >= activeGesture.requiredDirectionChanges;
        }

        /// <summary>
        /// Valida rotacion total
        /// </summary>
        private bool ValidateRotation()
        {
            if (activeGesture == null)
                return true;

            return movementTracker.TotalRotation >= activeGesture.minRotationAngle;
        }

        /// <summary>
        /// Valida circularidad del movimiento
        /// </summary>
        private bool ValidateCircularity()
        {
            if (activeGesture == null)
                return true;

            return movementTracker.GetCircularityScore() >= activeGesture.minCircularityScore;
        }

        /// <summary>
        /// Verifica si la mano esta dentro de la zona espacial requerida
        /// </summary>
        private bool IsInSpatialZone(Vector3 handPosition)
        {
            if (activeGesture == null || !activeGesture.requiresSpatialZone || xrOriginTransform == null)
                return true;

            Vector3 worldZoneCenter = xrOriginTransform.TransformPoint(activeGesture.zoneCenter);
            float distance = Vector3.Distance(handPosition, worldZoneCenter);

            return distance <= activeGesture.zoneRadius;
        }

        /// <summary>
        /// Completa el gesto exitosamente
        /// </summary>
        private void CompleteGesture()
        {
            string gestureName = activeGesture.gestureName;

            // Recopilar metricas estructuradas
            DynamicMetrics metrics = GetCurrentMetrics();

            if (debugMode)
            {
                Debug.Log($"[DynamicGesture] COMPLETADO: {gestureName} - " +
                          $"Distance: {movementTracker.TotalDistance:F3}m, " +
                          $"Duration: {movementTracker.GetDuration():F2}s, " +
                          $"Direction changes: {movementTracker.DirectionChanges}");
            }

            // IMPORTANTE: Enable cooldown de 2 segundos para que el usuario vea el mensaje
            successCooldownEndTime = Time.time + SUCCESS_COOLDOWN_DURATION;
            Debug.Log($"[DynamicGesture] MOVEMENT RECOGNIZED! Cooldown of {SUCCESS_COOLDOWN_DURATION}s activated");

            // Emitir evento original (compatibilidad)
            OnGestureCompleted?.Invoke(gestureName);

            // Emitir evento estructurado
            var result = DynamicGestureResult.Success(gestureName, metrics);
            OnGestureCompletedStructured?.Invoke(result);

            ResetState();
        }

        /// <summary>
        /// Falla el gesto con una razon
        /// </summary>
        private void FailGesture(string reason)
        {
            string gestureName = activeGesture != null ? activeGesture.gestureName : "Unknown";

            // Recopilar metricas estructuradas
            DynamicMetrics metrics = GetCurrentMetrics();

            // Determinar razon y fase de fallo
            FailureReason failureReason = ParseFailureReason(reason);
            GesturePhase failedPhase = DetermineFailedPhase(reason);

            if (debugMode)
            {
                Debug.LogWarning($"[DynamicGesture] FALLADO: {gestureName} - Razon: {reason} (enum: {failureReason}, fase: {failedPhase})");
            }

            // Emitir evento original (compatibilidad)
            OnGestureFailed?.Invoke(gestureName, reason);

            // Emitir evento estructurado
            string troubleshootingMsg = FeedbackMessages.GetTroubleshootingMessage(failureReason, failedPhase, metrics, gestureName);
            var result = DynamicGestureResult.Failure(gestureName, failureReason, failedPhase, metrics, troubleshootingMsg);
            OnGestureFailedStructured?.Invoke(result);

            ResetState();
        }

        /// <summary>
        /// Obtiene las metricas actuales del MovementTracker.
        /// </summary>
        public DynamicMetrics GetCurrentMetrics()
        {
            return new DynamicMetrics
            {
                averageSpeed = movementTracker.CurrentSpeed,
                maxSpeed = movementTracker.CurrentSpeed, // Aproximacion, idealmente trackear maximo
                totalDistance = movementTracker.TotalDistance,
                duration = movementTracker.GetDuration(),
                directionChanges = movementTracker.DirectionChanges,
                totalRotation = movementTracker.TotalRotation,
                circularityScore = movementTracker.GetCircularityScore()
            };
        }

        /// <summary>
        /// Parsea el string de razon a enum FailureReason.
        /// </summary>
        private FailureReason ParseFailureReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                return FailureReason.Unknown;

            // Normalizar: minusculas y quitar tildes para matching robusto
            string lowerReason = RemoveAccents(reason.ToLower());

            // Pose perdida (intermedia o inicial)
            if (lowerReason.Contains("pose") && (lowerReason.Contains("perdida") || lowerReason.Contains("lost")))
                return FailureReason.PoseLost;

            // Speed
            if (lowerReason.Contains("velocidad") || lowerReason.Contains("speed"))
            {
                if (lowerReason.Contains("baja") || lowerReason.Contains("low") || lowerReason.Contains("lento"))
                    return FailureReason.SpeedTooLow;
                if (lowerReason.Contains("alta") || lowerReason.Contains("high") || lowerReason.Contains("rapido"))
                    return FailureReason.SpeedTooHigh;
            }

            // Distance
            if (lowerReason.Contains("distancia") || lowerReason.Contains("distance") || lowerReason.Contains("corto"))
                return FailureReason.DistanceTooShort;

            // Direccion (ahora sin tilde por RemoveAccents)
            if (lowerReason.Contains("direccion") || lowerReason.Contains("direction"))
            {
                if (lowerReason.Contains("cambios") || lowerReason.Contains("changes") || lowerReason.Contains("insuficientes"))
                    return FailureReason.DirectionChangesInsufficient;
                return FailureReason.DirectionWrong;
            }

            // Rotation (ahora sin tilde por RemoveAccents)
            if (lowerReason.Contains("rotacion") || lowerReason.Contains("rotation") || lowerReason.Contains("giro"))
                return FailureReason.RotationInsufficient;

            // Circular
            if (lowerReason.Contains("circular") || lowerReason.Contains("circulo"))
                return FailureReason.NotCircular;

            // Timeout
            if (lowerReason.Contains("timeout") || lowerReason.Contains("tiempo") || lowerReason.Contains("excedido"))
                return FailureReason.Timeout;

            // Tracking perdido
            if (lowerReason.Contains("tracking") || lowerReason.Contains("visible"))
                return FailureReason.TrackingLost;

            // Zona espacial (fuera de zona)
            if (lowerReason.Contains("zona") || lowerReason.Contains("espacial") || lowerReason.Contains("fuera"))
                return FailureReason.OutOfZone;

            // Requisitos finales no cumplidos
            if (lowerReason.Contains("final") || lowerReason.Contains("end") || lowerReason.Contains("requisitos"))
                return FailureReason.EndPoseMismatch;

            return FailureReason.Unknown;
        }

        /// <summary>
        /// Elimina tildes y diacriticos para matching robusto de strings.
        /// </summary>
        private string RemoveAccents(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Reemplazar caracteres acentuados comunes
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

            if (lowerReason.Contains("inicial") || lowerReason.Contains("start") || lowerReason.Contains("inicio"))
                return GesturePhase.Start;

            if (lowerReason.Contains("final") || lowerReason.Contains("end") || lowerReason.Contains("completar"))
                return GesturePhase.End;

            return GesturePhase.Move;
        }

        /// <summary>
        /// Maneja perdida de tracking
        /// </summary>
        private void HandleTrackingLost()
        {
            if (currentState == GestureState.InProgress)
            {
                FailGesture("Tracking perdido");
            }

            ResetState();
        }

        /// <summary>
        /// Resetea el estado a Idle
        /// </summary>
        private void ResetState()
        {
            bool wasPending = currentState == GestureState.PendingConfirmation;

            currentState = GestureState.Idle;
            activeGesture = null;
            gestureStartTime = 0f;
            initialDirectionValidated = false;
            pendingGestures.Clear();
            alternativeGestures.Clear();

            // Notify that we left pending state if we were in it
            if (wasPending)
            {
                OnPendingConfirmationChanged?.Invoke(false);
                lastPendingExitTime = Time.time; // Rastrear cuando salimos de PENDING
                pendingTimeoutCooldownEnd = Time.time + PENDING_TIMEOUT_COOLDOWN;
            }
        }

        /// <summary>
        /// Obtiene la posicion actual del palm joint de la mano
        /// </summary>
        private Vector3 GetHandPosition()
        {
            var subsystem = XRGeneralSettings.Instance?
                .Manager?
                .activeLoader?
                .GetLoadedSubsystem<XRHandSubsystem>();

            if (subsystem != null)
            {
                XRHand hand = subsystem.rightHand;

                if (hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palmPose))
                {
                    if (debugMode && activeGesture != null)
                    {
                        Debug.Log($"[TRACKING] GetHandPosition: {palmPose.position}, Changed: {Vector3.Distance(palmPose.position, lastHandPosition) > 0.001f}");
                    }
                    lastHandPosition = palmPose.position;
                    return palmPose.position;
                }
                else if (debugMode && activeGesture != null)
                {
                    Debug.LogWarning($"[TRACKING] TryGetPose FAILED for Palm joint");
                }
            }
            else if (debugMode && activeGesture != null)
            {
                Debug.LogError($"[TRACKING] XRHandSubsystem is NULL");
            }

            return lastHandPosition; // Fallback
        }

        /// <summary>
        /// Obtiene la rotacion actual del palm joint de la mano
        /// </summary>
        private Quaternion GetHandRotation()
        {
            var subsystem = XRGeneralSettings.Instance?
                .Manager?
                .activeLoader?
                .GetLoadedSubsystem<XRHandSubsystem>();

            if (subsystem != null)
            {
                XRHand hand = subsystem.rightHand;

                if (hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palmPose))
                {
                    lastHandRotation = palmPose.rotation;
                    return palmPose.rotation;
                }
            }

            return lastHandRotation; // Fallback
        }

        /// <summary>
        /// Dibuja Gizmos de debug en Scene view
        /// </summary>
        void OnDrawGizmos()
        {
            if (!Application.isPlaying || currentState != GestureState.InProgress || !debugMode)
                return;

            if (activeGesture == null)
                return;

            // Direccion esperada (verde)
            Gizmos.color = Color.green;
            Gizmos.DrawRay(smoothedHandPosition, activeGesture.primaryDirection * 0.1f);

            // Current direction (amarillo)
            if (movementTracker.AverageDirection.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(smoothedHandPosition, movementTracker.AverageDirection * 0.1f);
            }

            // Linea desde inicio (cyan)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(movementTracker.StartPosition, smoothedHandPosition);

            // Zona espacial si aplica
            if (activeGesture.requiresSpatialZone && xrOriginTransform != null)
            {
                Gizmos.color = IsInSpatialZone(smoothedHandPosition) ? Color.green : Color.red;
                Vector3 worldZoneCenter = xrOriginTransform.TransformPoint(activeGesture.zoneCenter);
                Gizmos.DrawWireSphere(worldZoneCenter, activeGesture.zoneRadius);
            }
        }

        /// <summary>
        /// Anade una definicion de gesto dinamicamente
        /// </summary>
        public void AddGestureDefinition(DynamicGestureDefinition gesture)
        {
            if (gesture != null && !gestureDefinitions.Contains(gesture))
            {
                gestureDefinitions.Add(gesture);
            }
        }

        /// <summary>
        /// Remueve una definicion de gesto
        /// </summary>
        public void RemoveGestureDefinition(DynamicGestureDefinition gesture)
        {
            gestureDefinitions.Remove(gesture);
        }

        /// <summary>
        /// Limpia todas las definiciones de gestos
        /// </summary>
        public void ClearGestureDefinitions()
        {
            gestureDefinitions.Clear();
        }

        /// <summary>
        /// Obtiene una copia de la lista actual de definiciones de gestos
        /// </summary>
        public List<DynamicGestureDefinition> GetGestureDefinitions()
        {
            return new List<DynamicGestureDefinition>(gestureDefinitions);
        }

        /// <summary>
        /// Filtra las definiciones de gestos activas para que solo se reconozcan
        /// los que pertenecen a una category dada (por nombre de signo).
        /// Guarda internamente la lista completa original para poder restaurarla.
        /// </summary>
        public void FilterGesturesByNames(HashSet<string> allowedNames)
        {
            if (allGestureDefinitions == null || allGestureDefinitions.Count == 0)
            {
                // Primera vez: guardar la lista completa original
                allGestureDefinitions = new List<DynamicGestureDefinition>(gestureDefinitions);
            }

            gestureDefinitions = allGestureDefinitions.FindAll(g => g != null && allowedNames.Contains(g.gestureName));

            if (debugMode)
            {
                Debug.Log($"[DynamicGestureRecognizer] Filtrado a {gestureDefinitions.Count} gestos de {allGestureDefinitions.Count} totales");
            }
        }

        /// <summary>
        /// Restaura la lista completa de definiciones de gestos (deshace FilterGesturesByNames).
        /// </summary>
        public void RestoreAllGestures()
        {
            if (allGestureDefinitions != null && allGestureDefinitions.Count > 0)
            {
                gestureDefinitions = new List<DynamicGestureDefinition>(allGestureDefinitions);

                if (debugMode)
                {
                    Debug.Log($"[DynamicGestureRecognizer] Restaurados {gestureDefinitions.Count} gestos");
                }
            }
        }

        /// <summary>
        /// Activa o desactiva el reconocimiento de dynamic gestures
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;

            if (!enabled)
            {
                // Reset state cuando se desactiva
                if (currentState != GestureState.Idle)
                {
                    ResetState();
                }
            }

            Debug.Log($"[DynamicGestureRecognizer] Recognition dynamic: {(enabled ? "ACTIVADO" : "DESACTIVADO")}");
        }

        /// <summary>
        /// Verifica si esta en estado de confirmacion pendiente (esperando disambiguation)
        /// </summary>
        public bool IsInPendingState()
        {
            return currentState == GestureState.PendingConfirmation;
        }

        /// <summary>
        /// Obtiene el estado actual del reconocedor (para debugging)
        /// </summary>
        public string GetCurrentStateName()
        {
            return currentState.ToString();
        }
    }
}
