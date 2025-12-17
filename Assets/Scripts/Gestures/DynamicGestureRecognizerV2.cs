using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.Gestures
{
    /// <summary>
    /// Reconocedor robusto de gestos dinámicos basado en evaluación de movimiento en tiempo real.
    /// Utiliza XR Hands oficial y una máquina de estados explícita.
    /// NO usa grabaciones ni machine learning.
    /// </summary>
    public class DynamicGestureRecognizerV2 : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Definición del gesto dinámico a reconocer")]
        [SerializeField] private DynamicGestureDefinition gestureDefinition;

        [Tooltip("Componente XRHandTrackingEvents de la mano a detectar")]
        [SerializeField] private XRHandTrackingEvents handTrackingEvents;

        [Tooltip("Transform del XROrigin para conversión de coordenadas")]
        [SerializeField] private Transform xrOrigin;

        [Header("Static Pose Integration (Optional)")]
        [Tooltip("Detector de poses estáticas para integración (opcional)")]
        [SerializeField] private MonoBehaviour staticPoseDetectorComponent;

        private IStaticPoseDetector staticPoseDetector;

        [Header("Events")]
        [Tooltip("Se invoca cuando el gesto comienza")]
        public UnityEvent<DynamicGestureDefinition> onGestureStarted;

        [Tooltip("Se invoca durante el progreso del gesto")]
        public UnityEvent<DynamicGestureDefinition, float> onGestureProgress;

        [Tooltip("Se invoca cuando el gesto se completa exitosamente")]
        public UnityEvent<DynamicGestureDefinition> onGestureCompleted;

        [Tooltip("Se invoca cuando el gesto falla")]
        public UnityEvent<DynamicGestureDefinition, string> onGestureFailed;

        [Header("Debug")]
        [Tooltip("Mostrar logs de debug en la consola")]
        [SerializeField] private bool showDebugLogs = false;

        [Tooltip("Mostrar visualización de la trayectoria con Gizmos")]
        [SerializeField] private bool visualizeTrajectory = true;

        // Sistema de reconocimiento
        private GestureStateMachine stateMachine;
        private TrajectoryBuffer trajectoryBuffer;
        private float lastSampleTime;
        private float minimumMovementThreshold = 0.001f; // Umbral mínimo para considerar movimiento

        // Estado interno
        private bool isEnabled = false;
        private Vector3 lastTrackedPosition;
        private bool hasStartedMovement = false;

        /// <summary>
        /// Estado actual del reconocimiento.
        /// </summary>
        public GestureState CurrentState => stateMachine?.CurrentState ?? GestureState.Idle;

        /// <summary>
        /// True si el reconocedor está activo.
        /// </summary>
        public bool IsActive => stateMachine?.IsActive() ?? false;

        /// <summary>
        /// Progreso actual del gesto (0-1).
        /// </summary>
        public float Progress
        {
            get
            {
                if (trajectoryBuffer == null || gestureDefinition == null)
                    return 0f;

                float distance = trajectoryBuffer.CalculateDirectDistance();
                return Mathf.Clamp01(distance / gestureDefinition.minimumDistance);
            }
        }

        void Awake()
        {
            // Intenta obtener la interfaz del componente
            if (staticPoseDetectorComponent != null)
            {
                staticPoseDetector = staticPoseDetectorComponent as IStaticPoseDetector;
                if (staticPoseDetector == null)
                {
                    Debug.LogWarning($"[DynamicGestureRecognizerV2] El componente {staticPoseDetectorComponent.GetType().Name} no implementa IStaticPoseDetector");
                }
            }

            // Intenta encontrar XROrigin si no está asignado
            if (xrOrigin == null)
            {
                var originComponent = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (originComponent != null)
                {
                    xrOrigin = originComponent.transform;
                    if (showDebugLogs)
                        Debug.Log($"[DynamicGestureRecognizerV2] XROrigin encontrado automáticamente");
                }
            }
        }

        void OnEnable()
        {
            if (gestureDefinition == null)
            {
                Debug.LogError("[DynamicGestureRecognizerV2] No hay DynamicGestureDefinition asignada!");
                return;
            }

            if (!gestureDefinition.IsValid())
            {
                Debug.LogError($"[DynamicGestureRecognizerV2] La definición del gesto '{gestureDefinition.gestureName}' no es válida!");
                return;
            }

            if (handTrackingEvents == null)
            {
                Debug.LogError("[DynamicGestureRecognizerV2] FALTA ASIGNAR 'handTrackingEvents'!");
                return;
            }

            // Inicializa los sistemas
            stateMachine = new GestureStateMachine(gestureDefinition);
            trajectoryBuffer = new TrajectoryBuffer(gestureDefinition.maxBufferSize);

            stateMachine.OnStateChanged += OnStateChanged;
            handTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);

            isEnabled = true;

            if (showDebugLogs)
                Debug.Log($"[DynamicGestureRecognizerV2] Inicializado para gesto '{gestureDefinition.gestureName}'");
        }

        void OnDisable()
        {
            if (handTrackingEvents != null)
            {
                handTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);
            }

            if (stateMachine != null)
            {
                stateMachine.OnStateChanged -= OnStateChanged;
            }

            isEnabled = false;
        }

        void Update()
        {
            if (!isEnabled || stateMachine == null)
                return;

            // Verifica timeouts
            if (stateMachine.ShouldTimeoutCurrentState())
            {
                FailGesture("Timeout - El gesto tomó demasiado tiempo");
                return;
            }

            // Maneja el cooldown
            if (stateMachine.CurrentState == GestureState.Cooldown)
            {
                if (stateMachine.IsCooldownFinished())
                {
                    stateMachine.Reset();
                }
            }
        }

        /// <summary>
        /// Callback cuando cambia el estado de la máquina.
        /// </summary>
        private void OnStateChanged(GestureState previousState, GestureState newState)
        {
            if (showDebugLogs)
                Debug.Log($"[DynamicGestureRecognizerV2] Estado: {previousState} -> {newState}");

            switch (newState)
            {
                case GestureState.Recording:
                    hasStartedMovement = false;
                    onGestureStarted?.Invoke(gestureDefinition);
                    break;

                case GestureState.Completed:
                    onGestureCompleted?.Invoke(gestureDefinition);
                    stateMachine.ChangeState(GestureState.Cooldown);
                    break;

                case GestureState.Failed:
                    // El mensaje de fallo se envía desde FailGesture()
                    stateMachine.ChangeState(GestureState.Cooldown);
                    break;
            }
        }

        /// <summary>
        /// Callback cuando los joints de la mano se actualizan.
        /// </summary>
        private void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            if (!isEnabled || !handTrackingEvents.handIsTracked)
                return;

            // Obtiene la pose del joint trackeado
            if (!eventArgs.hand.GetJoint(gestureDefinition.trackedJoint).TryGetPose(out Pose jointPose))
                return;

            // Convierte al espacio mundo
            Vector3 worldPosition = ConvertToWorldSpace(jointPose.position);

            // Procesa según el estado actual
            switch (stateMachine.CurrentState)
            {
                case GestureState.Idle:
                    HandleIdleState(worldPosition);
                    break;

                case GestureState.WaitingForStartPose:
                    HandleWaitingForStartPoseState();
                    break;

                case GestureState.Recording:
                    HandleRecordingState(worldPosition);
                    break;

                case GestureState.WaitingForEndPose:
                    HandleWaitingForEndPoseState(worldPosition);
                    break;
            }
        }

        /// <summary>
        /// Maneja el estado Idle.
        /// </summary>
        private void HandleIdleState(Vector3 currentPosition)
        {
            lastTrackedPosition = currentPosition;

            // Si requiere pose inicial, espera por ella
            if (gestureDefinition.requiredStartPose != null)
            {
                stateMachine.ChangeState(GestureState.WaitingForStartPose);
            }
            else
            {
                // Inicia directamente la grabación
                StartRecording(currentPosition);
            }
        }

        /// <summary>
        /// Maneja el estado de espera de pose inicial.
        /// </summary>
        private void HandleWaitingForStartPoseState()
        {
            if (staticPoseDetector == null || gestureDefinition.requiredStartPose == null)
            {
                // No puede verificar, inicia grabación directamente
                stateMachine.ChangeState(GestureState.Recording);
                return;
            }

            bool poseDetected = gestureDefinition.requireStartPoseConfirmed
                ? staticPoseDetector.IsPosePerformed(gestureDefinition.requiredStartPose)
                : staticPoseDetector.IsPoseDetected(gestureDefinition.requiredStartPose);

            if (poseDetected)
            {
                StartRecording(lastTrackedPosition);
            }
        }

        /// <summary>
        /// Maneja el estado de grabación.
        /// </summary>
        private void HandleRecordingState(Vector3 currentPosition)
        {
            float currentTime = Time.timeSinceLevelLoad;

            // Verifica intervalo de muestreo
            if (currentTime - lastSampleTime < 1f / gestureDefinition.samplingRate)
                return;

            // Añade punto al buffer
            trajectoryBuffer.AddPoint(currentPosition, currentTime);
            lastSampleTime = currentTime;

            // Detecta inicio de movimiento significativo
            if (!hasStartedMovement)
            {
                float movementDistance = Vector3.Distance(trajectoryBuffer.FirstPosition, currentPosition);
                if (movementDistance >= minimumMovementThreshold)
                {
                    hasStartedMovement = true;
                    if (showDebugLogs)
                        Debug.Log($"[DynamicGestureRecognizerV2] Movimiento iniciado");
                }
            }

            // Emite progreso
            onGestureProgress?.Invoke(gestureDefinition, Progress);

            // Verifica si se cumple la distancia mínima
            float directDistance = trajectoryBuffer.CalculateDirectDistance();
            if (directDistance >= gestureDefinition.minimumDistance)
            {
                // Si requiere pose final, espera por ella
                if (gestureDefinition.requiredEndPose != null)
                {
                    stateMachine.ChangeState(GestureState.WaitingForEndPose);
                }
                else
                {
                    // Evalúa inmediatamente
                    EvaluateGesture();
                }
            }

            lastTrackedPosition = currentPosition;
        }

        /// <summary>
        /// Maneja el estado de espera de pose final.
        /// </summary>
        private void HandleWaitingForEndPoseState(Vector3 currentPosition)
        {
            lastTrackedPosition = currentPosition;

            if (staticPoseDetector == null || gestureDefinition.requiredEndPose == null)
            {
                // No puede verificar, evalúa directamente
                EvaluateGesture();
                return;
            }

            bool poseDetected = gestureDefinition.requireEndPoseConfirmed
                ? staticPoseDetector.IsPosePerformed(gestureDefinition.requiredEndPose)
                : staticPoseDetector.IsPoseDetected(gestureDefinition.requiredEndPose);

            if (poseDetected)
            {
                EvaluateGesture();
            }
        }

        /// <summary>
        /// Inicia la grabación de la trayectoria.
        /// </summary>
        private void StartRecording(Vector3 startPosition)
        {
            trajectoryBuffer.Clear();
            trajectoryBuffer.AddPoint(startPosition, Time.timeSinceLevelLoad);
            lastSampleTime = Time.timeSinceLevelLoad;
            stateMachine.ChangeState(GestureState.Recording);

            if (showDebugLogs)
                Debug.Log($"[DynamicGestureRecognizerV2] Grabación iniciada");
        }

        /// <summary>
        /// Evalúa si el gesto cumple todos los criterios.
        /// </summary>
        private void EvaluateGesture()
        {
            stateMachine.ChangeState(GestureState.Evaluating);

            if (showDebugLogs)
                Debug.Log($"[DynamicGestureRecognizerV2] Evaluando gesto con {trajectoryBuffer.Count} puntos");

            // Verifica puntos mínimos
            if (trajectoryBuffer.Count < 10)
            {
                FailGesture("Trayectoria muy corta - menos de 10 puntos");
                return;
            }

            // Verifica duración
            float duration = trajectoryBuffer.Duration;
            if (duration < gestureDefinition.minimumDuration)
            {
                FailGesture($"Duración muy corta ({duration:F2}s < {gestureDefinition.minimumDuration:F2}s)");
                return;
            }

            // Verifica distancia
            float distance = trajectoryBuffer.CalculateDirectDistance();
            if (distance < gestureDefinition.minimumDistance)
            {
                FailGesture($"Distancia insuficiente ({distance:F3}m < {gestureDefinition.minimumDistance:F3}m)");
                return;
            }

            if (gestureDefinition.maximumDistance > 0f && distance > gestureDefinition.maximumDistance)
            {
                FailGesture($"Distancia excesiva ({distance:F3}m > {gestureDefinition.maximumDistance:F3}m)");
                return;
            }

            // Verifica velocidad
            float avgSpeed = trajectoryBuffer.CalculateAverageSpeed();
            if (avgSpeed < gestureDefinition.minimumAverageSpeed)
            {
                FailGesture($"Velocidad muy baja ({avgSpeed:F3}m/s < {gestureDefinition.minimumAverageSpeed:F3}m/s)");
                return;
            }

            if (gestureDefinition.maximumAverageSpeed > 0f && avgSpeed > gestureDefinition.maximumAverageSpeed)
            {
                FailGesture($"Velocidad muy alta ({avgSpeed:F3}m/s > {gestureDefinition.maximumAverageSpeed:F3}m/s)");
                return;
            }

            // Verifica dirección
            if (gestureDefinition.primaryDirection != Vector3.zero)
            {
                if (!trajectoryBuffer.IsMovingInDirection(gestureDefinition.primaryDirection, gestureDefinition.directionTolerance))
                {
                    FailGesture("Dirección incorrecta del movimiento");
                    return;
                }
            }

            // Verifica curvatura
            float curvature = trajectoryBuffer.CalculateCurvature();
            if (curvature > gestureDefinition.maxCurvature)
            {
                FailGesture($"Trayectoria demasiado curva ({curvature:F2} > {gestureDefinition.maxCurvature:F2})");
                return;
            }

            // Verifica suavidad
            if (gestureDefinition.evaluateSmoothness)
            {
                float smoothness = trajectoryBuffer.EvaluateSmoothness();
                if (smoothness < 0.3f) // Umbral mínimo de suavidad
                {
                    FailGesture($"Movimiento muy errático (suavidad: {smoothness:F2})");
                    return;
                }
            }

            // Todas las validaciones pasaron
            stateMachine.ChangeState(GestureState.Completed);

            if (showDebugLogs)
            {
                Debug.Log($"[DynamicGestureRecognizerV2] GESTO COMPLETADO:");
                Debug.Log($"  - Puntos: {trajectoryBuffer.Count}");
                Debug.Log($"  - Duración: {duration:F2}s");
                Debug.Log($"  - Distancia: {distance:F3}m");
                Debug.Log($"  - Velocidad: {avgSpeed:F3}m/s");
                Debug.Log($"  - Curvatura: {curvature:F2}");
            }
        }

        /// <summary>
        /// Marca el gesto como fallido.
        /// </summary>
        private void FailGesture(string reason)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[DynamicGestureRecognizerV2] Gesto fallido: {reason}");

            onGestureFailed?.Invoke(gestureDefinition, reason);
            trajectoryBuffer.Clear();
            stateMachine.ChangeState(GestureState.Failed);
        }

        /// <summary>
        /// Convierte una posición del espacio de tracking al espacio mundo.
        /// </summary>
        private Vector3 ConvertToWorldSpace(Vector3 trackingPosition)
        {
            if (xrOrigin != null)
            {
                return xrOrigin.TransformPoint(trackingPosition);
            }
            return trackingPosition;
        }

        /// <summary>
        /// Inicia manualmente el reconocimiento del gesto.
        /// </summary>
        public void StartGestureRecognition()
        {
            if (stateMachine != null && stateMachine.CurrentState == GestureState.Idle)
            {
                stateMachine.ChangeState(GestureState.WaitingForStartPose);
            }
        }

        /// <summary>
        /// Cancela el reconocimiento actual.
        /// </summary>
        public void CancelGesture()
        {
            if (stateMachine != null && stateMachine.IsActive())
            {
                FailGesture("Cancelado por el usuario");
            }
        }

        /// <summary>
        /// Resetea el reconocedor al estado inicial.
        /// </summary>
        public void ResetRecognizer()
        {
            trajectoryBuffer?.Clear();
            stateMachine?.Reset();
            hasStartedMovement = false;
        }

        void OnDrawGizmos()
        {
            if (!visualizeTrajectory || trajectoryBuffer == null || trajectoryBuffer.Count < 2)
                return;

            Vector3[] positions = trajectoryBuffer.GetPositions();

            // Dibuja la trayectoria
            Gizmos.color = Color.cyan;
            for (int i = 1; i < positions.Length; i++)
            {
                Gizmos.DrawLine(positions[i - 1], positions[i]);
            }

            // Dibuja puntos de inicio y fin
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(positions[0], 0.01f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(positions[positions.Length - 1], 0.01f);
        }
    }
}
