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
    /// Reconocedor de gestos dinámicos basado en secuencias de poses estáticas + movimiento.
    /// Optimizado para Meta Quest 3 con parámetros conservadores y máquina de estados robusta.
    /// </summary>
    public class DynamicGestureRecognizer : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Lista de definiciones de gestos dinámicos a reconocer")]
        [SerializeField] private List<DynamicGestureDefinition> gestureDefinitions = new List<DynamicGestureDefinition>();

        [Tooltip("Adaptador para detectar poses estáticas (StaticPoseAdapter o SingleGestureAdapter)")]
        [SerializeField] private MonoBehaviour poseAdapterComponent;

        [Tooltip("Componente XRHandTrackingEvents para verificar tracking (Right Hand Controller)")]
        [SerializeField] private UnityEngine.XR.Hands.XRHandTrackingEvents handTrackingEvents;

        [Header("Suavizado")]
        [Tooltip("Factor de suavizado de posición (0.5-0.7 recomendado para Quest 3)")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float positionSmoothingFactor = 0.7f;

        [Header("Debug")]
        [Tooltip("Activar logs detallados y visualización de Gizmos")]
        [SerializeField] private bool debugMode = false;

        // Eventos públicos (API original - mantener compatibilidad)
        public System.Action<string> OnGestureStarted;
        public System.Action<string, float> OnGestureProgress; // nombre, progreso 0-1
        public System.Action<string> OnGestureCompleted;
        public System.Action<string, string> OnGestureFailed; // nombre, razón
        public System.Action<bool> OnPendingConfirmationChanged; // true = entró en pending, false = salió de pending

        // Eventos estructurados (nuevos - para FeedbackSystem)
        /// <summary>
        /// Evento con resultado estructurado al completar un gesto.
        /// Incluye métricas detalladas de movimiento.
        /// </summary>
        public System.Action<DynamicGestureResult> OnGestureCompletedStructured;

        /// <summary>
        /// Evento con resultado estructurado al fallar un gesto.
        /// Incluye razón de fallo, fase y métricas.
        /// </summary>
        public System.Action<DynamicGestureResult> OnGestureFailedStructured;

        // Eventos para feedback por fases (nuevos)
        /// <summary>
        /// Evento cuando la pose inicial es detectada correctamente.
        /// Se emite ANTES de que comience el movimiento (Fase 1: StartDetected).
        /// </summary>
        public System.Action<string> OnInitialPoseDetected;

        /// <summary>
        /// Evento de progreso con métricas detalladas.
        /// Incluye nombre, progreso, métricas y definición del gesto para análisis de feedback.
        /// </summary>
        public System.Action<string, float, DynamicMetrics, DynamicGestureDefinition> OnGestureProgressWithMetrics;

        /// <summary>
        /// Evento cuando el gesto está cerca de completarse (>80%).
        /// Usado para feedback de "casi completado" (Fase 3: NearCompletion).
        /// </summary>
        public System.Action<string, float> OnGestureNearCompletion;

        // Estado interno
        private bool isEnabled = true; // Activado por defecto
        private GestureState currentState = GestureState.Idle;
        private DynamicGestureDefinition activeGesture = null;
        private MovementTracker movementTracker;
        private float gestureStartTime = 0f;

        // Cooldown después de éxito (para que el usuario vea el mensaje)
        private float successCooldownEndTime = 0f;
        private const float SUCCESS_COOLDOWN_DURATION = 2f; // 2 segundos de pausa después de éxito

        /// <summary>
        /// True si el reconocedor está en cooldown después de un éxito.
        /// </summary>
        public bool IsInSuccessCooldown => Time.time < successCooldownEndTime;

        /// <summary>
        /// True si la pose inicial del gesto activo sigue siendo válida.
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

                // Si no hay pose detectada, no es válida
                if (string.IsNullOrEmpty(currentPose))
                    return false;

                // Verificar si la pose actual aún puede iniciar el gesto activo
                return activeGesture.CanStartWithPose(currentPose) || CanStartWithPoseData(activeGesture);
            }
        }

        // Estado de confirmación pendiente
        private List<DynamicGestureDefinition> pendingGestures = new List<DynamicGestureDefinition>();
        private float pendingStartTime = 0f;
        private const float PENDING_CONFIRMATION_TIMEOUT = 0.25f; // Tiempo de espera para desambiguar (REDUCIDO para respuesta rápida)

        // Tracking de mano
        private Vector3 smoothedHandPosition = Vector3.zero;
        private Quaternion smoothedHandRotation = Quaternion.identity;
        private Vector3 lastHandPosition = Vector3.zero;
        private Quaternion lastHandRotation = Quaternion.identity;
        private float trackingLostTime = 0f;
        private const float TRACKING_LOSS_TOLERANCE = 0.2f; // Tolerar hasta 0.2s de pérdida de tracking

        // Cache XR Origin
        private Transform xrOriginTransform;

        // Validación de requisitos
        private bool initialDirectionValidated = false;
        private float initialDirectionGracePeriod = 0.5f; // 50% de minDuration antes de fallar por dirección

        // Cache del adaptador como interfaz
        private IPoseAdapter poseAdapter;

        // Cache del último evento de joints para validación directa de HandShape
        private XRHandJointsUpdatedEventArgs lastJointsEventArgs;
        private bool hasValidJointsEventArgs = false;

        void Awake()
        {
            // Inicializar MovementTracker con parámetros conservadores
            movementTracker = new MovementTracker(windowSize: 3f, historySize: 120);

            // Cachear XR Origin
            if (Camera.main != null)
            {
                xrOriginTransform = Camera.main.transform.parent; // XR Origin es parent de Camera
            }

            // AUTO-FIX: Buscar SingleGestureAdapter si no está asignado correctamente
            if (poseAdapterComponent == null || (poseAdapterComponent as IPoseAdapter) == null)
            {
                Debug.LogWarning("[DynamicGestureRecognizer] Auto-buscando SingleGestureAdapter en RightHandRecognizer...");

                // Buscar específicamente en RightHandRecognizer
                var rightHandRecognizer = GameObject.Find("RightHandRecognizer");
                if (rightHandRecognizer != null)
                {
                    var adapter = rightHandRecognizer.GetComponent<ASL.DynamicGestures.SingleGestureAdapter>();
                    if (adapter != null)
                    {
                        poseAdapterComponent = adapter;
                        Debug.Log($"[DynamicGestureRecognizer] SingleGestureAdapter encontrado en RightHandRecognizer");
                    }
                    else
                    {
                        Debug.LogError("[DynamicGestureRecognizer] RightHandRecognizer no tiene SingleGestureAdapter!");
                    }
                }
                else
                {
                    Debug.LogError("[DynamicGestureRecognizer] No se encontró GameObject 'RightHandRecognizer'!");
                }
            }

            // Obtener el adaptador como interfaz
            if (poseAdapterComponent != null)
            {
                poseAdapter = poseAdapterComponent as IPoseAdapter;
                if (poseAdapter == null)
                {
                    Debug.LogError("[DynamicGestureRecognizer] El componente asignado no implementa IPoseAdapter. " +
                                   "Usa StaticPoseAdapter o SingleGestureAdapter.");
                }
                else
                {
                    Debug.Log("[DynamicGestureRecognizer] Pose Adapter configurado correctamente!");
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
                // Suscribirse a jointsUpdated para cachear el último evento (validación directa de End poses)
                handTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
            }

            if (gestureDefinitions.Count == 0)
            {
                Debug.LogWarning("[DynamicGestureRecognizer] No hay gestos definidos en la lista.");
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
        /// Callback cuando los joints de la mano se actualizan. Cachea el evento para validación directa.
        /// </summary>
        private void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            lastJointsEventArgs = eventArgs;
            hasValidJointsEventArgs = true;
        }

        void Update()
        {
            // SOLO funcionar si está activado
            if (!isEnabled)
                return;

            // IMPORTANTE: No procesar nuevos gestos durante el cooldown de éxito
            // Esto permite que el usuario vea el mensaje "¡Movimiento reconocido!" durante 2 segundos
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
                        // Continuar usando la última posición conocida
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

            // Obtener posición y rotación de la mano
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

            // Máquina de estados
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

            // FILTRO CRÍTICO: Solo aplicar en modo aprendizaje (Scene 3)
            // En modo autoevaluación (Scene 4), CurrentSign puede ser null o estático, así que permitimos todo
            var gameManager = FindObjectOfType<ASL_LearnVR.Core.GameManager>();
            if (gameManager != null && gameManager.CurrentSign != null)
            {
                // Si el signo actual NO requiere movimiento, NO iniciar gestos dinámicos
                // PERO solo aplicar este filtro si estamos en modo aprendizaje individual
                if (!gameManager.CurrentSign.requiresMovement)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] CurrentSign '{gameManager.CurrentSign.signName}' NO requiere movimiento, bloqueando reconocimiento dinámico");
                    }
                    return;
                }
            }
            else if (debugMode && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[DynamicGesture] CurrentSign es NULL, permitiendo reconocimiento dinámico (modo autoevaluación)");
            }

            string currentPose = poseAdapter.GetCurrentPoseName();

            if (string.IsNullOrEmpty(currentPose))
                return;

            // DEBUG: Log de pose detectada
            if (debugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[DynamicGesture] CheckForGestureStart: Pose actual = '{currentPose}', Gestos definidos = {gestureDefinitions.Count}");
            }

            // Buscar TODOS los gestos que puedan iniciarse con esta pose
            pendingGestures.Clear();

            // MODO ESPECIAL SCENE 4: Si CurrentSign es NULL (autoevaluación),
            // considerar TODOS los gestos como candidatos para desambiguación por movimiento
            bool isScene4Mode = (gameManager == null || gameManager.CurrentSign == null);

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
                    // En Scene 4: TODOS los gestos son candidatos, desambiguamos por movimiento
                    canStart = true;
                }
                else
                {
                    // En Scene 3: Primero intentar match exacto de pose
                    canStart = gesture.CanStartWithPose(currentPose);

                    // GESTOS COMPUESTOS: Si no hay match por nombre pero hay poseData,
                    // intentar validación directa usando HandShape
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
                        Debug.Log($"[DynamicGesture] Gesto '{gesture.gestureName}' puede iniciar con pose '{currentPose}' (Scene4Mode: {isScene4Mode})");
                    }
                }
                else if (debugMode && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[DynamicGesture] Gesto '{gesture.gestureName}' NO puede iniciar con pose '{currentPose}'");
                }
            }

            // Si encontramos gestos candidatos, entrar en estado de confirmación pendiente
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
                    // Múltiples gestos posibles, entrar en estado pendiente para desambiguar
                    EnterPendingConfirmation();
                }
            }
            else if (debugMode && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[DynamicGesture] Ningún gesto dinámico puede iniciar con pose '{currentPose}'");
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

            // Notificar que salimos de pending si estábamos en ese estado
            if (wasPending)
            {
                OnPendingConfirmationChanged?.Invoke(false);
            }

            // Emitir evento de pose inicial detectada (Fase 1 del feedback)
            OnInitialPoseDetected?.Invoke(gesture.gestureName);

            OnGestureStarted?.Invoke(gesture.gestureName);

            if (debugMode)
            {
                Debug.Log($"[DynamicGesture] INICIADO: {gesture.gestureName}");
            }
        }

        /// <summary>
        /// Entra en estado de confirmación pendiente cuando hay múltiples gestos candidatos
        /// </summary>
        private void EnterPendingConfirmation()
        {
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
        /// Actualiza el estado de confirmación pendiente, esperando movimiento para desambiguar
        /// </summary>
        private void UpdatePendingConfirmation()
        {
            float elapsed = Time.time - pendingStartTime;

            // Timeout: si no hay movimiento significativo, asumir que es un gesto estático
            if (elapsed >= PENDING_CONFIRMATION_TIMEOUT)
            {
                // No se detectó movimiento suficiente, podría ser un gesto estático
                // En este caso, NO marcamos como completado aquí, dejamos que MultiGestureRecognizer lo maneje
                if (debugMode)
                {
                    Debug.Log($"[DynamicGesture] PENDING TIMEOUT: Sin movimiento detectado, asumiendo gesto estático");
                }
                ResetState();
                return;
            }

            // Verificar si la pose inicial se perdió
            if (poseAdapter != null)
            {
                string currentPose = poseAdapter.GetCurrentPoseName();

                // Si perdimos completamente la pose, resetear
                if (string.IsNullOrEmpty(currentPose))
                {
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] PENDING: Pose completamente perdida, reseteando");
                    }
                    ResetState();
                    return;
                }

                // EN SCENE 4: NO validar si la pose cambió, permitir cualquier pose
                // Solo resetear si se pierde completamente
                var gameManager = FindObjectOfType<ASL_LearnVR.Core.GameManager>();
                bool isScene4Mode = (gameManager == null || gameManager.CurrentSign == null);

                if (!isScene4Mode)
                {
                    // EN SCENE 3: Validar que la pose sigue siendo válida
                    bool poseStillValid = false;

                    foreach (var gesture in pendingGestures)
                    {
                        if (gesture.CanStartWithPose(currentPose))
                        {
                            poseStillValid = true;
                            break;
                        }
                    }

                    if (!poseStillValid)
                    {
                        // Perdió la pose, resetear
                        if (debugMode)
                        {
                            Debug.Log($"[DynamicGesture] PENDING: Pose inicial perdida en Scene 3, reseteando");
                        }
                        ResetState();
                        return;
                    }
                }
            }

            // Analizar si hay movimiento significativo (MÁS SENSIBLE para respuesta rápida)
            bool hasSignificantMovement = movementTracker.TotalDistance > 0.015f || // 1.5cm de movimiento (reducido)
                                          movementTracker.CurrentSpeed > 0.06f;     // Velocidad mínima (reducida)

            if (hasSignificantMovement)
            {
                // Se detectó movimiento, ahora intentamos desambiguar qué gesto es
                DynamicGestureDefinition bestMatch = DisambiguateGesture();

                if (bestMatch != null)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] PENDING RESOLVED: '{bestMatch.gestureName}' seleccionado por movimiento");
                    }
                    StartGesture(bestMatch);
                }
                else
                {
                    // No se pudo desambiguar aún, seguir esperando
                    if (debugMode && Time.frameCount % 30 == 0)
                    {
                        Debug.Log($"[DynamicGesture] PENDING: Esperando más datos de movimiento...");
                    }
                }
            }
        }

        /// <summary>
        /// Intenta desambiguar entre múltiples gestos candidatos basándose en el movimiento detectado
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
                            Debug.Log($"[DynamicGesture] Desambiguación: '{gesture.gestureName}' seleccionado por circularidad (score: {circularityScore:F2})");
                        }
                        return gesture;
                    }
                }
            }

            // PRIORIDAD 2: Detectar ROTACIÓN (J, I, etc.)
            foreach (var gesture in pendingGestures)
            {
                if (gesture.requiresRotation && movementTracker.TotalRotation > 8f)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] Desambiguación: '{gesture.gestureName}' seleccionado por rotación ({movementTracker.TotalRotation:F1}°)");
                    }
                    return gesture;
                }
            }

            // PRIORIDAD 3: Detectar CAMBIOS DE DIRECCIÓN (Hello, Bye con waving)
            foreach (var gesture in pendingGestures)
            {
                if (gesture.requiresDirectionChange && movementTracker.DirectionChanges > 0)
                {
                    // Verificar que también coincida la dirección general
                    Vector3 currentDirection = movementTracker.AverageDirection;
                    if (currentDirection.sqrMagnitude > 0.01f && gesture.primaryDirection.sqrMagnitude > 0.01f)
                    {
                        float angleDiff = Vector3.Angle(currentDirection, gesture.primaryDirection);
                        if (angleDiff <= gesture.directionTolerance)
                        {
                            if (debugMode)
                            {
                                Debug.Log($"[DynamicGesture] Desambiguación: '{gesture.gestureName}' seleccionado por cambios de dirección ({movementTracker.DirectionChanges}) + dirección ({angleDiff:F1}°)");
                            }
                            return gesture;
                        }
                    }
                }
            }

            // PRIORIDAD 4: Analizar SOLO por dirección (Thank You, etc.)
            Vector3 currentDir = movementTracker.AverageDirection;

            if (currentDir.sqrMagnitude < 0.01f)
                return null; // Sin dirección clara aún

            // Buscar el gesto con mejor match de dirección
            DynamicGestureDefinition bestMatch = null;
            float bestAngleDifference = float.MaxValue;

            foreach (var gesture in pendingGestures)
            {
                // Si el gesto no requiere movimiento direccional específico, saltarlo
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
                Debug.Log($"[DynamicGesture] Desambiguación: '{bestMatch.gestureName}' seleccionado por dirección (ángulo: {bestAngleDifference:F1}°)");
            }

            return bestMatch;
        }

        /// <summary>
        /// Valida requisitos y actualiza progreso del gesto activo
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
                // IMPORTANTE: Si el gesto requiere cambios de dirección (zigzag), NO validar dirección estrictamente
                // Pero si solo requiere rotación (curva), SÍ validar dirección principal
                bool shouldValidateDirection = !activeGesture.requiresDirectionChange;

                if (shouldValidateDirection)
                {
                    // Dar margen inicial antes de validar dirección
                    if (elapsed >= initialDirectionGracePeriod * activeGesture.minDuration)
                    {
                        if (!ValidateMovementDirection())
                        {
                            if (debugMode)
                            {
                                Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Dirección incorrecta. Esperada: {activeGesture.primaryDirection}, Actual: {movementTracker.AverageDirection}");
                            }
                            // No fallar inmediatamente, dar más margen
                            if (elapsed > activeGesture.minDuration * 0.9f)
                            {
                                FailGesture("Dirección de movimiento incorrecta");
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
                    // Si no validamos dirección, marcarla como validada automáticamente
                    initialDirectionValidated = true;

                    if (debugMode && elapsed < 0.1f)
                    {
                        Debug.Log($"[DynamicGesture] {activeGesture.gestureName}: Validación de dirección DESACTIVADA (gesto con rotación/cambios)");
                    }
                }

                // Validar velocidad (con margen del 50%)
                if (!ValidateSpeed())
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Velocidad baja. Actual: {movementTracker.CurrentSpeed:F3} m/s, " +
                                         $"Mínima: {activeGesture.minSpeed * 0.5f:F3} m/s");
                    }
                }
            }

            // Validar cambios de dirección
            if (activeGesture.requiresDirectionChange)
            {
                if (!ValidateDirectionChanges())
                {
                    // No fallar inmediatamente, esperar hasta cerca del final
                    if (elapsed > activeGesture.minDuration * 0.9f)
                    {
                        FailGesture($"Cambios de dirección insuficientes ({movementTracker.DirectionChanges}/{activeGesture.requiredDirectionChanges})");
                        return;
                    }
                }
            }

            // Validar rotación
            if (activeGesture.requiresRotation)
            {
                if (!ValidateRotation())
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Rotación insuficiente. " +
                                         $"Actual: {movementTracker.TotalRotation:F1}°, " +
                                         $"Mínima: {activeGesture.minRotationAngle:F1}°");
                    }
                }
            }

            // Validar movimiento circular
            if (activeGesture.requiresCircularMotion)
            {
                if (!ValidateCircularity())
                {
                    if (elapsed > activeGesture.minDuration * 0.9f)
                    {
                        FailGesture($"Movimiento no circular (score: {movementTracker.GetCircularityScore():F2})");
                        return;
                    }
                }
            }

            // Validar zona espacial (según timing)
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

            // Emitir progreso con métricas para feedback detallado
            DynamicMetrics currentMetrics = GetCurrentMetrics();
            OnGestureProgressWithMetrics?.Invoke(activeGesture.gestureName, progress, currentMetrics, activeGesture);

            // Emitir evento de "casi completado" cuando supera el 80%
            if (progress >= 0.8f && progress < 1.0f)
            {
                OnGestureNearCompletion?.Invoke(activeGesture.gestureName, progress);
            }

            // Verificar completado
            if (elapsed >= activeGesture.minDuration)
            {
                // Validaciones finales más estrictas
                bool finalValidation = true;

                // Distancia mínima
                if (activeGesture.requiresMovement && movementTracker.TotalDistance < activeGesture.minDistance)
                {
                    finalValidation = false;
                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Distancia insuficiente al completar. " +
                                         $"Actual: {movementTracker.TotalDistance:F3}m, Mínima: {activeGesture.minDistance:F3}m");
                    }
                }

                // Poses finales
                if (!ValidatePoses(PoseTimingRequirement.End))
                {
                    finalValidation = false;
                }

                if (finalValidation)
                {
                    CompleteGesture();
                }
                else
                {
                    FailGesture("Requisitos finales no cumplidos");
                }
            }
        }

        /// <summary>
        /// Valida poses estáticas según timing
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

                // SOLUCIÓN PARA GESTOS COMPUESTOS:
                // Para poses End, si hay un poseData asignado, validar directamente con HandShape
                // Esto permite detectar transiciones como 5→S (White), O→S (Orange), T→H (Thursday)
                if (timing == PoseTimingRequirement.End && poseReq.poseData != null)
                {
                    matches = ValidatePoseDirectly(poseReq.poseData);

                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] {activeGesture.gestureName}: Validación DIRECTA de End pose '{poseReq.poseName}' = {matches}");
                    }
                }
                else
                {
                    // Fallback: si no hay poseData pero el CurrentSign coincide por nombre,
                    // validar directamente usando ese SignData para evitar ambigüedades (ej: Gray).
                    SignData fallbackSignData = null;
                    var gm = FindObjectOfType<ASL_LearnVR.Core.GameManager>();
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
                        // Validación tradicional usando poseAdapter, aceptando familias de pose
                        matches = !string.IsNullOrEmpty(currentPose) && poseReq.IsValidPose(currentPose);
                    }
                }

                if (!matches)
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Pose requerida '{poseReq.poseName}' no detectada. " +
                                         $"Actual: '{currentPose}', PoseData: {(poseReq.poseData != null ? poseReq.poseData.signName : "null")}");
                    }
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifica si un gesto puede iniciar usando validación directa de poseData.
        /// Usado para gestos compuestos donde el Start pose es diferente del TargetSign.
        /// </summary>
        private bool CanStartWithPoseData(DynamicGestureDefinition gesture)
        {
            if (gesture == null)
                return false;

            var startPoses = gesture.GetPosesForTiming(PoseTimingRequirement.Start);

            if (startPoses.Count == 0)
                return false;

            foreach (var poseReq in startPoses)
            {
                // Si tiene poseData, validar directamente con HandShape
                if (poseReq.poseData != null)
                {
                    bool matches = ValidatePoseDirectly(poseReq.poseData);
                    if (debugMode)
                    {
                        Debug.Log($"[DynamicGesture] CanStartWithPoseData: '{gesture.gestureName}' Start pose '{poseReq.poseName}' validación directa = {matches}");
                    }

                    if (matches)
                        return true;
                }
                else
                {
                    // Fallback: si la pose no tiene poseData pero coincide con el CurrentSign, validar con ese SignData
                    var gm = FindObjectOfType<ASL_LearnVR.Core.GameManager>();
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
                }
            }

            return false;
        }

        /// <summary>
        /// Valida una pose directamente usando el HandShape del SignData contra XRHandSubsystem.
        /// Esto permite detectar poses End en gestos compuestos sin depender del poseAdapter.
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
        /// Valida dirección de movimiento
        /// </summary>
        private bool ValidateMovementDirection()
        {
            if (activeGesture == null || activeGesture.primaryDirection.sqrMagnitude < 0.01f)
                return true;

            return movementTracker.IsMovingInDirection(activeGesture.primaryDirection, activeGesture.directionTolerance);
        }

        /// <summary>
        /// Valida velocidad mínima (con margen del 50%)
        /// </summary>
        private bool ValidateSpeed()
        {
            if (activeGesture == null)
                return true;

            return movementTracker.CurrentSpeed >= activeGesture.minSpeed * 0.5f;
        }

        /// <summary>
        /// Valida cambios de dirección
        /// </summary>
        private bool ValidateDirectionChanges()
        {
            if (activeGesture == null)
                return true;

            return movementTracker.DirectionChanges >= activeGesture.requiredDirectionChanges;
        }

        /// <summary>
        /// Valida rotación total
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
        /// Verifica si la mano está dentro de la zona espacial requerida
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

            // Recopilar métricas estructuradas
            DynamicMetrics metrics = GetCurrentMetrics();

            if (debugMode)
            {
                Debug.Log($"[DynamicGesture] COMPLETADO: {gestureName} - " +
                          $"Distancia: {movementTracker.TotalDistance:F3}m, " +
                          $"Duración: {movementTracker.GetDuration():F2}s, " +
                          $"Cambios dirección: {movementTracker.DirectionChanges}");
            }

            // IMPORTANTE: Activar cooldown de 2 segundos para que el usuario vea el mensaje
            successCooldownEndTime = Time.time + SUCCESS_COOLDOWN_DURATION;
            Debug.Log($"[DynamicGesture] ¡MOVIMIENTO RECONOCIDO! Cooldown de {SUCCESS_COOLDOWN_DURATION}s activado");

            // Emitir evento original (compatibilidad)
            OnGestureCompleted?.Invoke(gestureName);

            // Emitir evento estructurado
            var result = DynamicGestureResult.Success(gestureName, metrics);
            OnGestureCompletedStructured?.Invoke(result);

            ResetState();
        }

        /// <summary>
        /// Falla el gesto con una razón
        /// </summary>
        private void FailGesture(string reason)
        {
            string gestureName = activeGesture != null ? activeGesture.gestureName : "Unknown";

            // Recopilar métricas estructuradas
            DynamicMetrics metrics = GetCurrentMetrics();

            // Determinar razón y fase de fallo
            FailureReason failureReason = ParseFailureReason(reason);
            GesturePhase failedPhase = DetermineFailedPhase(reason);

            if (debugMode)
            {
                Debug.LogWarning($"[DynamicGesture] FALLADO: {gestureName} - Razón: {reason} (enum: {failureReason}, fase: {failedPhase})");
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
        /// Obtiene las métricas actuales del MovementTracker.
        /// </summary>
        public DynamicMetrics GetCurrentMetrics()
        {
            return new DynamicMetrics
            {
                averageSpeed = movementTracker.CurrentSpeed,
                maxSpeed = movementTracker.CurrentSpeed, // Aproximación, idealmente trackear máximo
                totalDistance = movementTracker.TotalDistance,
                duration = movementTracker.GetDuration(),
                directionChanges = movementTracker.DirectionChanges,
                totalRotation = movementTracker.TotalRotation,
                circularityScore = movementTracker.GetCircularityScore()
            };
        }

        /// <summary>
        /// Parsea el string de razón a enum FailureReason.
        /// </summary>
        private FailureReason ParseFailureReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                return FailureReason.Unknown;

            // Normalizar: minúsculas y quitar tildes para matching robusto
            string lowerReason = RemoveAccents(reason.ToLower());

            // Pose perdida (intermedia o inicial)
            if (lowerReason.Contains("pose") && (lowerReason.Contains("perdida") || lowerReason.Contains("lost")))
                return FailureReason.PoseLost;

            // Velocidad
            if (lowerReason.Contains("velocidad") || lowerReason.Contains("speed"))
            {
                if (lowerReason.Contains("baja") || lowerReason.Contains("low") || lowerReason.Contains("lento"))
                    return FailureReason.SpeedTooLow;
                if (lowerReason.Contains("alta") || lowerReason.Contains("high") || lowerReason.Contains("rapido"))
                    return FailureReason.SpeedTooHigh;
            }

            // Distancia
            if (lowerReason.Contains("distancia") || lowerReason.Contains("distance") || lowerReason.Contains("corto"))
                return FailureReason.DistanceTooShort;

            // Dirección (ahora sin tilde por RemoveAccents)
            if (lowerReason.Contains("direccion") || lowerReason.Contains("direction"))
            {
                if (lowerReason.Contains("cambios") || lowerReason.Contains("changes") || lowerReason.Contains("insuficientes"))
                    return FailureReason.DirectionChangesInsufficient;
                return FailureReason.DirectionWrong;
            }

            // Rotación (ahora sin tilde por RemoveAccents)
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
        /// Elimina tildes y diacríticos para matching robusto de strings.
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
        /// Determina la fase donde ocurrió el fallo.
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
        /// Maneja pérdida de tracking
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

            // Notificar que salimos de pending si estábamos en ese estado
            if (wasPending)
            {
                OnPendingConfirmationChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Obtiene la posición actual del palm joint de la mano
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
        /// Obtiene la rotación actual del palm joint de la mano
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

            // Dirección esperada (verde)
            Gizmos.color = Color.green;
            Gizmos.DrawRay(smoothedHandPosition, activeGesture.primaryDirection * 0.1f);

            // Dirección actual (amarillo)
            if (movementTracker.AverageDirection.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(smoothedHandPosition, movementTracker.AverageDirection * 0.1f);
            }

            // Línea desde inicio (cyan)
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
        /// Añade una definición de gesto dinámicamente
        /// </summary>
        public void AddGestureDefinition(DynamicGestureDefinition gesture)
        {
            if (gesture != null && !gestureDefinitions.Contains(gesture))
            {
                gestureDefinitions.Add(gesture);
            }
        }

        /// <summary>
        /// Remueve una definición de gesto
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
        /// Activa o desactiva el reconocimiento de gestos dinámicos
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

            Debug.Log($"[DynamicGestureRecognizer] Reconocimiento dinámico: {(enabled ? "ACTIVADO" : "DESACTIVADO")}");
        }

        /// <summary>
        /// Verifica si está en estado de confirmación pendiente (esperando desambiguación)
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
