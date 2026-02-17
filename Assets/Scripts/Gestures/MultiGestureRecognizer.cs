using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.Gestures
{
    /// <summary>
    /// Reconoce múltiples gestos ASL simultáneamente.
    /// Útil para el modo de autoevaluación donde el usuario puede hacer cualquier signo.
    /// </summary>
    public class MultiGestureRecognizer : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Lista de signos a detectar")]
        [SerializeField] private List<SignData> targetSigns = new List<SignData>();

        [Tooltip("Componente XRHandTrackingEvents de la mano izquierda")]
        [SerializeField] private XRHandTrackingEvents leftHandTrackingEvents;

        [Tooltip("Componente XRHandTrackingEvents de la mano derecha")]
        [SerializeField] private XRHandTrackingEvents rightHandTrackingEvents;

        [Tooltip("(OPCIONAL) Reconocedor de gestos dinámicos para coordinación")]
        [SerializeField] private ASL.DynamicGestures.DynamicGestureRecognizer dynamicGestureRecognizer;

        [Header("Detection Settings")]
        [Tooltip("Solo reconocer la mano derecha (ignorar mano izquierda)")]
        [SerializeField] private bool rightHandOnly = true;

        [Tooltip("Intervalo de detección en segundos")]
        [SerializeField] private float detectionInterval = 0.1f;

        [Tooltip("Tiempo mínimo de hold para confirmar el gesto")]
        [SerializeField] private float minimumHoldTime = 0.5f;

        [Header("Events")]
        [Tooltip("Se invoca cuando un gesto es detectado y confirmado (después del hold time)")]
        public UnityEvent<SignData> onGestureDetected;

        [Tooltip("Se invoca cuando un gesto es reconocido instantáneamente (sin hold time). Útil para feedback visual.")]
        public UnityEvent<SignData> onGestureRecognized;

        [Tooltip("Se invoca cuando un gesto deja de ser reconocido")]
        public UnityEvent<SignData> onGestureLost;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Estado de detección por signo
        private Dictionary<SignData, bool> wasDetected = new Dictionary<SignData, bool>();
        private Dictionary<SignData, bool> performedTriggered = new Dictionary<SignData, bool>();
        private Dictionary<SignData, float> holdStartTimes = new Dictionary<SignData, float>();
        private Dictionary<SignData, float> confidenceScores = new Dictionary<SignData, float>();
        private float timeOfLastCheckLeft = 0f;
        private float timeOfLastCheckRight = 0f;

        // Estado de coordinación con gestos dinámicos
        private bool isDynamicGesturePending = false;

        // Período de gracia: evita parpadeo cuando Quest 3 pierde tracking por 1-2 frames
        private int consecutiveMisses = 0;
        private const int MAX_CONSECUTIVE_MISSES = 2; // Tolerar 2 checks sin detección (~0.2s)

        /// <summary>
        /// Obtiene el signo activo actual (null si ninguno).
        /// </summary>
        public SignData CurrentActiveSign { get; private set; }

        void OnEnable()
        {
            if (leftHandTrackingEvents != null && !rightHandOnly)
                leftHandTrackingEvents.jointsUpdated.AddListener(OnLeftHandJointsUpdated);

            if (rightHandTrackingEvents != null)
                rightHandTrackingEvents.jointsUpdated.AddListener(OnRightHandJointsUpdated);

            // Suscribirse a eventos del reconocedor dinámico si está asignado
            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.OnPendingConfirmationChanged += OnDynamicGesturePendingChanged;
            }

            InitializeDetectionState();
        }

        void OnDisable()
        {
            if (leftHandTrackingEvents != null && !rightHandOnly)
                leftHandTrackingEvents.jointsUpdated.RemoveListener(OnLeftHandJointsUpdated);

            if (rightHandTrackingEvents != null)
                rightHandTrackingEvents.jointsUpdated.RemoveListener(OnRightHandJointsUpdated);

            // Desuscribirse de eventos del reconocedor dinámico
            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.OnPendingConfirmationChanged -= OnDynamicGesturePendingChanged;
            }
        }

        /// <summary>
        /// Inicializa el estado de detección para todos los signos.
        /// </summary>
        private void InitializeDetectionState()
        {
            wasDetected.Clear();
            performedTriggered.Clear();
            holdStartTimes.Clear();

            foreach (var sign in targetSigns)
            {
                if (sign != null)
                {
                    wasDetected[sign] = false;
                    performedTriggered[sign] = false;
                    holdStartTimes[sign] = 0f;
                }
            }
        }

        /// <summary>
        /// Establece la lista de signos a detectar.
        /// </summary>
        public void SetTargetSigns(List<SignData> signs)
        {
            targetSigns = signs;
            InitializeDetectionState();

            // Configura el targetTransform para todas las poses
            Transform cameraTransform = Camera.main?.transform;
            if (cameraTransform != null)
            {
                foreach (var sign in targetSigns)
                {
                    if (sign == null) continue;

                    var handPose = sign.GetHandPose();
                    if (handPose != null && handPose.relativeOrientation != null)
                    {
                        if (handPose.relativeOrientation.targetTransform == null)
                        {
                            handPose.relativeOrientation.targetTransform = cameraTransform;
                            Debug.Log($"MultiGestureRecognizer: Asignada cámara como targetTransform para '{sign.signName}'");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("MultiGestureRecognizer: No se encontró Camera.main para asignar targetTransform");
            }
        }

        /// <summary>
        /// Callback cuando los joints de la mano izquierda se actualizan.
        /// </summary>
        private void OnLeftHandJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            CheckGestures(eventArgs, leftHandTrackingEvents, ref timeOfLastCheckLeft);
        }

        /// <summary>
        /// Callback cuando los joints de la mano derecha se actualizan.
        /// </summary>
        private void OnRightHandJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            CheckGestures(eventArgs, rightHandTrackingEvents, ref timeOfLastCheckRight);
        }

        /// <summary>
        /// Verifica todos los gestos contra el estado actual de la mano.
        /// </summary>
        private void CheckGestures(XRHandJointsUpdatedEventArgs eventArgs, XRHandTrackingEvents handTrackingEvents, ref float timeOfLastCheck)
        {
            if (!isActiveAndEnabled || Time.timeSinceLevelLoad < timeOfLastCheck + detectionInterval)
                return;

            // Obtiene el signo que el usuario está practicando actualmente
            SignData currentPracticingSign = null;
            var gameManager = ASL_LearnVR.Core.GameManager.Instance;
            if (gameManager != null)
            {
                currentPracticingSign = gameManager.CurrentSign;
            }

            // Determina el signo activo actual (el que tiene mejor match)
            SignData bestMatchSign = null;
            float bestConfidence = 0f;

            foreach (var sign in targetSigns)
            {
                if (sign == null || sign.handShapeOrPose == null)
                    continue;

                // FILTRO CRÍTICO: Si el usuario está practicando un gesto DINÁMICO,
                // NO detectar signos ESTÁTICOS que compartan el mismo HandShape
                if (currentPracticingSign != null && currentPracticingSign.requiresMovement)
                {
                    // Si este signo es estático, saltarlo para evitar falsos positivos
                    if (!sign.requiresMovement)
                    {
                        confidenceScores[sign] = 0f;
                        continue;
                    }
                }

                // Obtiene el Hand Shape o Hand Pose
                var handShape = sign.GetHandShape();
                var handPose = sign.GetHandPose();

                // Verifica si el gesto cumple las condiciones
                bool detected = handTrackingEvents.handIsTracked &&
                                ((handShape != null && handShape.CheckConditions(eventArgs)) ||
                                 (handPose != null && handPose.CheckConditions(eventArgs)));

                if (detected)
                {
                    // Por ahora usamos 1.0 para todos los detectados
                    // En el futuro se puede calcular confianza basada en thresholds
                    float confidence = 1.0f;
                    confidenceScores[sign] = confidence;

                    if (confidence > bestConfidence)
                    {
                        bestConfidence = confidence;
                        bestMatchSign = sign;
                    }
                }
                else
                {
                    confidenceScores[sign] = 0f;
                }
            }

            // Actualiza el signo activo y procesa eventos
            UpdateActiveSign(bestMatchSign);

            timeOfLastCheck = Time.timeSinceLevelLoad;
        }

        /// <summary>
        /// Actualiza el signo activo actual y dispara los eventos correspondientes.
        /// </summary>
        private void UpdateActiveSign(SignData newActiveSign)
        {
            // PERÍODO DE GRACIA: Si perdemos detección pero teníamos un signo activo,
            // tolerar hasta MAX_CONSECUTIVE_MISSES checks antes de perderlo.
            // Esto evita el parpadeo cuando Quest 3 pierde tracking por 1-2 frames.
            if (CurrentActiveSign != newActiveSign)
            {
                if (newActiveSign == null && CurrentActiveSign != null)
                {
                    // Signo perdido - aplicar período de gracia
                    consecutiveMisses++;
                    if (consecutiveMisses <= MAX_CONSECUTIVE_MISSES)
                    {
                        // Mantener el signo activo por ahora, no resetear
                        return;
                    }
                    // Superó el período de gracia, continuar con la pérdida
                }
                else
                {
                    // Nuevo signo detectado (o cambio de signo) - resetear contador
                    consecutiveMisses = 0;
                }

                // Procesa el fin del signo anterior
                if (CurrentActiveSign != null)
                {
                    wasDetected[CurrentActiveSign] = false;
                    onGestureLost?.Invoke(CurrentActiveSign);

                    if (showDebugLogs)
                        Debug.Log($"MultiGestureRecognizer: Gesto '{CurrentActiveSign.signName}' perdido.");
                }

                // Procesa el inicio del nuevo signo
                if (newActiveSign != null)
                {
                    holdStartTimes[newActiveSign] = Time.timeSinceLevelLoad;
                    wasDetected[newActiveSign] = true;

                    // Emite evento de reconocimiento instantáneo (tile amarillo)
                    onGestureRecognized?.Invoke(newActiveSign);

                    if (showDebugLogs)
                        Debug.Log($"MultiGestureRecognizer: Gesto '{newActiveSign.signName}' detectado.");
                }

                CurrentActiveSign = newActiveSign;
            }
            else
            {
                // Mismo signo detectado consecutivamente - resetear contador de misses
                if (newActiveSign != null)
                    consecutiveMisses = 0;
            }

            // Si hay un signo activo, verifica si cumplió el hold time
            if (CurrentActiveSign != null && wasDetected[CurrentActiveSign])
            {
                if (!performedTriggered.ContainsKey(CurrentActiveSign) || !performedTriggered[CurrentActiveSign])
                {
                    float requiredHoldTime = CurrentActiveSign.minimumHoldTime > 0
                        ? CurrentActiveSign.minimumHoldTime
                        : minimumHoldTime;
                    float holdTimer = Time.timeSinceLevelLoad - holdStartTimes[CurrentActiveSign];

                    var gm = ASL_LearnVR.Core.GameManager.Instance;
                    bool isScene4 = (gm == null || gm.CurrentSign == null);

                    // COORDINACIÓN CON GESTOS DINÁMICOS (Scene 3):
                    bool shouldWaitForDynamicResolution = !isScene4 && !CurrentActiveSign.requiresMovement && isDynamicGesturePending;

                    if (holdTimer >= requiredHoldTime && !shouldWaitForDynamicResolution)
                    {
                        // SCENE 4: Gestos DINÁMICOS NO se confirman aquí.
                        // Deben completar el movimiento vía DynamicGestureRecognizer.
                        // Solo gestos ESTÁTICOS se confirman por hold time.
                        if (isScene4 && CurrentActiveSign.requiresMovement)
                        {
                            if (showDebugLogs)
                                Debug.Log($"MultiGestureRecognizer: '{CurrentActiveSign.signName}' requiere movimiento - esperando DynamicGestureRecognizer");
                            // NO confirmar - el DynamicGesturePracticeManager lo hará cuando complete el movimiento
                        }
                        else
                        {
                            performedTriggered[CurrentActiveSign] = true;
                            onGestureDetected?.Invoke(CurrentActiveSign);

                            if (showDebugLogs)
                                Debug.Log($"MultiGestureRecognizer: Gesto '{CurrentActiveSign.signName}' confirmado!");
                        }
                    }
                    else if (shouldWaitForDynamicResolution && showDebugLogs)
                    {
                        Debug.Log($"MultiGestureRecognizer: Esperando resolución de gesto dinámico antes de confirmar '{CurrentActiveSign.signName}'");
                    }
                }
            }
        }

        /// <summary>
        /// Resetea el estado del recognizer.
        /// </summary>
        public void ResetState()
        {
            InitializeDetectionState();
        }

        /// <summary>
        /// Verifica si un signo específico ha sido detectado.
        /// </summary>
        public bool IsSignDetected(SignData sign)
        {
            return performedTriggered.ContainsKey(sign) && performedTriggered[sign];
        }

        /// <summary>
        /// Callback cuando el DynamicGestureRecognizer entra/sale de estado pending
        /// </summary>
        private void OnDynamicGesturePendingChanged(bool isPending)
        {
            isDynamicGesturePending = isPending;

            if (showDebugLogs)
            {
                Debug.Log($"[MultiGestureRecognizer] DynamicGesture pending state: {isPending}");
            }

            // Si salimos de pending y no se confirmó nada, resetear el signo activo
            if (!isPending && CurrentActiveSign != null && !CurrentActiveSign.requiresMovement)
            {
                // El gesto estático puede continuar su hold time
            }
        }
    }
}
