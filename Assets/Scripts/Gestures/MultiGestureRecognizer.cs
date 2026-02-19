using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.Gestures
{
    /// <summary>
    /// Reconoce multiples gestos ASL simultaneamente.
    /// Util para el modo de autoevaluacion donde el usuario puede hacer cualquier signo.
    /// </summary>
    public class MultiGestureRecognizer : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("List de signos a detectar")]
        [SerializeField] private List<SignData> targetSigns = new List<SignData>();

        [Tooltip("Component XRHandTrackingEvents de la mano izquierda")]
        [SerializeField] private XRHandTrackingEvents leftHandTrackingEvents;

        [Tooltip("Component XRHandTrackingEvents de la mano derecha")]
        [SerializeField] private XRHandTrackingEvents rightHandTrackingEvents;

        [Tooltip("(OPTIONAL) Dynamic gesture recognizer for coordination")]
        [SerializeField] private ASL.DynamicGestures.DynamicGestureRecognizer dynamicGestureRecognizer;

        [Header("Detection Settings")]
        [Tooltip("Solo reconocer la mano derecha (ignorar mano izquierda)")]
        [SerializeField] private bool rightHandOnly = true;

        [Tooltip("Intervalo de deteccion en segundos")]
        [SerializeField] private float detectionInterval = 0.1f;

        [Tooltip("Time minimo de hold para confirmar el gesto")]
        [SerializeField] private float minimumHoldTime = 0.5f;

        [Header("Events")]
        [Tooltip("Se invoca cuando un gesto es detected y confirmado (despues del hold time)")]
        public UnityEvent<SignData> onGestureDetected;

        [Tooltip("Se invoca cuando un gesto es reconocido instantaneamente (sin hold time). Util para feedback visual.")]
        public UnityEvent<SignData> onGestureRecognized;

        [Tooltip("Se invoca cuando un gesto deja de ser reconocido")]
        public UnityEvent<SignData> onGestureLost;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // State de deteccion por signo
        private Dictionary<SignData, bool> wasDetected = new Dictionary<SignData, bool>();
        private Dictionary<SignData, bool> performedTriggered = new Dictionary<SignData, bool>();
        private Dictionary<SignData, float> holdStartTimes = new Dictionary<SignData, float>();
        private Dictionary<SignData, float> confidenceScores = new Dictionary<SignData, float>();
        private float timeOfLastCheckLeft = 0f;
        private float timeOfLastCheckRight = 0f;

        // State de coordinacion con dynamic gestures
        private bool isDynamicGesturePending = false;

        // Periodo de gracia: evita parpadeo cuando Quest 3 pierde tracking por 1-2 frames
        private int consecutiveMisses = 0;
        private const int MAX_CONSECUTIVE_MISSES = 2; // Tolerar 2 checks sin deteccion (~0.2s)

        /// <summary>
        /// Obtiene el signo active actual (null si ninguno).
        /// </summary>
        public SignData CurrentActiveSign { get; private set; }

        void OnEnable()
        {
            if (leftHandTrackingEvents != null && !rightHandOnly)
                leftHandTrackingEvents.jointsUpdated.AddListener(OnLeftHandJointsUpdated);

            if (rightHandTrackingEvents != null)
                rightHandTrackingEvents.jointsUpdated.AddListener(OnRightHandJointsUpdated);

            // Suscribirse a eventos del reconocedor dynamic si esta assigned
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

            // Unsubscribe from events del reconocedor dynamic
            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.OnPendingConfirmationChanged -= OnDynamicGesturePendingChanged;
            }
        }

        /// <summary>
        /// Inicializa el estado de deteccion para todos los signos.
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
                            Debug.Log($"MultiGestureRecognizer: Asignada camara como targetTransform para '{sign.signName}'");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("MultiGestureRecognizer: No found Camera.main para asignar targetTransform");
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

            // Obtiene el signo que el usuario esta practicando actualmente
            SignData currentPracticingSign = null;
            var gameManager = ASL_LearnVR.Core.GameManager.Instance;
            if (gameManager != null)
            {
                currentPracticingSign = gameManager.CurrentSign;
            }

            // Determina el signo active actual (el que tiene mejor match)
            SignData bestMatchSign = null;
            float bestConfidence = 0f;

            foreach (var sign in targetSigns)
            {
                if (sign == null || sign.handShapeOrPose == null)
                    continue;

                // FILTRO CRITICO: Si el usuario esta practicando un gesto DINAMICO,
                // NO detectar signos ESTATICOS que compartan el mismo HandShape
                if (currentPracticingSign != null && currentPracticingSign.requiresMovement)
                {
                    // Si este signo es static, saltarlo para evitar falsos positivos
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
                    // Por ahora usamos 1.0 para todos los detecteds
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

            // Actualiza el signo active y procesa eventos
            UpdateActiveSign(bestMatchSign);

            timeOfLastCheck = Time.timeSinceLevelLoad;
        }

        /// <summary>
        /// Actualiza el signo active actual y dispara los eventos correspondientes.
        /// </summary>
        private void UpdateActiveSign(SignData newActiveSign)
        {
            // PERIODO DE GRACIA: Si perdemos deteccion pero teniamos un signo active,
            // tolerar hasta MAX_CONSECUTIVE_MISSES checks antes de perderlo.
            // Esto evita el parpadeo cuando Quest 3 pierde tracking por 1-2 frames.
            if (CurrentActiveSign != newActiveSign)
            {
                if (newActiveSign == null && CurrentActiveSign != null)
                {
                    // Sign perdido - aplicar periodo de gracia
                    consecutiveMisses++;
                    if (consecutiveMisses <= MAX_CONSECUTIVE_MISSES)
                    {
                        // Mantener el signo active por ahora, no resetear
                        return;
                    }
                    // Supero el periodo de gracia, continuar con la perdida
                }
                else
                {
                    // Nuevo signot detected (o cambio de signo) - resetear contador
                    consecutiveMisses = 0;
                }

                // Procesa el fin del signo anterior
                if (CurrentActiveSign != null)
                {
                    wasDetected[CurrentActiveSign] = false;
                    onGestureLost?.Invoke(CurrentActiveSign);

                    if (showDebugLogs)
                        Debug.Log($"MultiGestureRecognizer: Gesture '{CurrentActiveSign.signName}' perdido.");
                }

                // Procesa el inicio del nuevo signo
                if (newActiveSign != null)
                {
                    holdStartTimes[newActiveSign] = Time.timeSinceLevelLoad;
                    wasDetected[newActiveSign] = true;

                    // Emite evento de reconocimiento instantaneo (tile amarillo)
                    onGestureRecognized?.Invoke(newActiveSign);

                    if (showDebugLogs)
                        Debug.Log($"MultiGestureRecognizer: Gesture '{newActiveSign.signName}' detected.");
                }

                CurrentActiveSign = newActiveSign;
            }
            else
            {
                // Mismo signot detected consecutivamente - resetear contador de misses
                if (newActiveSign != null)
                    consecutiveMisses = 0;
            }

            // Si hay un signo active, verifica si cumplio el hold time
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

                    // COORDINACION CON GESTOS DINAMICOS (Scene 3):
                    bool shouldWaitForDynamicResolution = !isScene4 && !CurrentActiveSign.requiresMovement && isDynamicGesturePending;

                    if (holdTimer >= requiredHoldTime && !shouldWaitForDynamicResolution)
                    {
                        // SCENE 4: Gestures DINAMICOS NO se confirman aqui.
                        // Deben completar el movimiento via DynamicGestureRecognizer.
                        // Solo gestos ESTATICOS se confirman por hold time.
                        if (isScene4 && CurrentActiveSign.requiresMovement)
                        {
                            if (showDebugLogs)
                                Debug.Log($"MultiGestureRecognizer: '{CurrentActiveSign.signName}' requiere movimiento - esperando DynamicGestureRecognizer");
                            // NO confirmar - el DynamicGesturePracticeManager lo hara cuando complete el movimiento
                        }
                        else
                        {
                            performedTriggered[CurrentActiveSign] = true;
                            onGestureDetected?.Invoke(CurrentActiveSign);

                            if (showDebugLogs)
                                Debug.Log($"MultiGestureRecognizer: Gesture '{CurrentActiveSign.signName}' confirmado!");
                        }
                    }
                    else if (shouldWaitForDynamicResolution && showDebugLogs)
                    {
                        Debug.Log($"MultiGestureRecognizer: Esperando resolucion de dynamic gesture antes de confirmar '{CurrentActiveSign.signName}'");
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
        /// Verifica si un signo especifico ha sido detected.
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

            // Si salimos de pending y no se confirmo nada, resetear el signo active
            if (!isPending && CurrentActiveSign != null && !CurrentActiveSign.requiresMovement)
            {
                // El gesto static puede continuar su hold time
            }
        }
    }
}
