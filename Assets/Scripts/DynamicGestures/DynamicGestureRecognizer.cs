using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

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
        [Tooltip("Factor de suavizado de posición (0.2-0.4 recomendado para Quest 3)")]
        [Range(0.1f, 0.5f)]
        [SerializeField] private float positionSmoothingFactor = 0.3f;

        [Header("Debug")]
        [Tooltip("Activar logs detallados y visualización de Gizmos")]
        [SerializeField] private bool debugMode = false;

        // Eventos públicos
        public System.Action<string> OnGestureStarted;
        public System.Action<string, float> OnGestureProgress; // nombre, progreso 0-1
        public System.Action<string> OnGestureCompleted;
        public System.Action<string, string> OnGestureFailed; // nombre, razón

        // Estado interno
        private bool isEnabled = false; // AÑADIDO: Control de activación
        private GestureState currentState = GestureState.Idle;
        private DynamicGestureDefinition activeGesture = null;
        private MovementTracker movementTracker;
        private float gestureStartTime = 0f;

        // Tracking de mano
        private Vector3 smoothedHandPosition = Vector3.zero;
        private Quaternion smoothedHandRotation = Quaternion.identity;
        private Vector3 lastHandPosition = Vector3.zero;
        private Quaternion lastHandRotation = Quaternion.identity;

        // Cache XR Origin
        private Transform xrOriginTransform;

        // Validación de requisitos
        private bool initialDirectionValidated = false;
        private float initialDirectionGracePeriod = 0.5f; // 50% de minDuration antes de fallar por dirección

        // Cache del adaptador como interfaz
        private IPoseAdapter poseAdapter;

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
                        Debug.Log($"[DynamicGestureRecognizer] ✅ SingleGestureAdapter encontrado en RightHandRecognizer");
                    }
                    else
                    {
                        Debug.LogError("[DynamicGestureRecognizer] ❌ RightHandRecognizer no tiene SingleGestureAdapter!");
                    }
                }
                else
                {
                    Debug.LogError("[DynamicGestureRecognizer] ❌ No se encontró GameObject 'RightHandRecognizer'!");
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
                    Debug.Log("[DynamicGestureRecognizer] ✅ Pose Adapter configurado correctamente!");
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

            if (gestureDefinitions.Count == 0)
            {
                Debug.LogWarning("[DynamicGestureRecognizer] No hay gestos definidos en la lista.");
            }
        }

        void Update()
        {
            // SOLO funcionar si está activado
            if (!isEnabled)
                return;

            // Verificar tracking
            if (handTrackingEvents == null || !handTrackingEvents.handIsTracked)
            {
                HandleTrackingLost();
                return;
            }

            // Obtener posición y rotación de la mano
            Vector3 currentHandPos = GetHandPosition();
            Quaternion currentHandRot = GetHandRotation();

            // Suavizado para reducir jitter
            if (smoothedHandPosition == Vector3.zero)
            {
                smoothedHandPosition = currentHandPos;
                smoothedHandRotation = currentHandRot;
            }
            else
            {
                smoothedHandPosition = Vector3.Lerp(smoothedHandPosition, currentHandPos, positionSmoothingFactor);
                smoothedHandRotation = Quaternion.Slerp(smoothedHandRotation, currentHandRot, positionSmoothingFactor);
            }

            // Actualizar tracker de movimiento
            movementTracker.UpdateTracking(smoothedHandPosition, smoothedHandRotation);

            // Máquina de estados
            switch (currentState)
            {
                case GestureState.Idle:
                    CheckForGestureStart();
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

            string currentPose = poseAdapter.GetCurrentPoseName();

            if (string.IsNullOrEmpty(currentPose))
                return;

            // Buscar gestos que puedan iniciarse con esta pose
            foreach (var gesture in gestureDefinitions)
            {
                if (gesture == null)
                    continue;

                if (gesture.CanStartWithPose(currentPose))
                {
                    StartGesture(gesture);
                    break; // Solo un gesto a la vez
                }
            }
        }

        /// <summary>
        /// Inicia el reconocimiento de un gesto
        /// </summary>
        private void StartGesture(DynamicGestureDefinition gesture)
        {
            activeGesture = gesture;
            currentState = GestureState.InProgress;
            gestureStartTime = Time.time;

            movementTracker.Reset();
            initialDirectionValidated = false;

            OnGestureStarted?.Invoke(gesture.gestureName);

            if (debugMode)
            {
                Debug.Log($"[DynamicGesture] INICIADO: {gesture.gestureName}");
            }
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
                // IMPORTANTE: Si el gesto requiere rotación, NO validar dirección estrictamente
                // porque la rotación hace que la trayectoria sea curva, no lineal
                bool shouldValidateDirection = !activeGesture.requiresRotation;

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
                            if (elapsed > activeGesture.minDuration * 0.8f)
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
                        Debug.Log($"[DynamicGesture] {activeGesture.gestureName}: Validación de dirección DESACTIVADA (gesto con rotación)");
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

                bool matches = !string.IsNullOrEmpty(currentPose) &&
                               currentPose.Equals(poseReq.poseName, System.StringComparison.OrdinalIgnoreCase);

                if (!matches)
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"[DynamicGesture] {activeGesture.gestureName}: Pose requerida '{poseReq.poseName}' no detectada. Actual: '{currentPose}'");
                    }
                    return false;
                }
            }

            return true;
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

            if (debugMode)
            {
                Debug.Log($"[DynamicGesture] COMPLETADO: {gestureName} - " +
                          $"Distancia: {movementTracker.TotalDistance:F3}m, " +
                          $"Duración: {movementTracker.GetDuration():F2}s, " +
                          $"Cambios dirección: {movementTracker.DirectionChanges}");
            }

            OnGestureCompleted?.Invoke(gestureName);

            ResetState();
        }

        /// <summary>
        /// Falla el gesto con una razón
        /// </summary>
        private void FailGesture(string reason)
        {
            string gestureName = activeGesture != null ? activeGesture.gestureName : "Unknown";

            if (debugMode)
            {
                Debug.LogWarning($"[DynamicGesture] FALLADO: {gestureName} - Razón: {reason}");
            }

            OnGestureFailed?.Invoke(gestureName, reason);

            ResetState();
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
            currentState = GestureState.Idle;
            activeGesture = null;
            gestureStartTime = 0f;
            initialDirectionValidated = false;
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
                    lastHandPosition = palmPose.position;
                    return palmPose.position;
                }
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
    }
}