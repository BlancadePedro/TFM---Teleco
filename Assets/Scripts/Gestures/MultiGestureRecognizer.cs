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

        [Header("Detection Settings")]
        [Tooltip("Intervalo de detección en segundos")]
        [SerializeField] private float detectionInterval = 0.1f;

        [Tooltip("Tiempo mínimo de hold para confirmar el gesto")]
        [SerializeField] private float minimumHoldTime = 0.5f;

        [Header("Events")]
        [Tooltip("Se invoca cuando un gesto es detectado")]
        public UnityEvent<SignData> onGestureDetected;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Estado de detección por signo
        private Dictionary<SignData, bool> wasDetected = new Dictionary<SignData, bool>();
        private Dictionary<SignData, bool> performedTriggered = new Dictionary<SignData, bool>();
        private Dictionary<SignData, float> holdStartTimes = new Dictionary<SignData, float>();
        private float timeOfLastCheck = 0f;

        void OnEnable()
        {
            if (leftHandTrackingEvents != null)
                leftHandTrackingEvents.jointsUpdated.AddListener(OnLeftHandJointsUpdated);

            if (rightHandTrackingEvents != null)
                rightHandTrackingEvents.jointsUpdated.AddListener(OnRightHandJointsUpdated);

            InitializeDetectionState();
        }

        void OnDisable()
        {
            if (leftHandTrackingEvents != null)
                leftHandTrackingEvents.jointsUpdated.RemoveListener(OnLeftHandJointsUpdated);

            if (rightHandTrackingEvents != null)
                rightHandTrackingEvents.jointsUpdated.RemoveListener(OnRightHandJointsUpdated);
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
        }

        /// <summary>
        /// Callback cuando los joints de la mano izquierda se actualizan.
        /// </summary>
        private void OnLeftHandJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            CheckGestures(eventArgs, leftHandTrackingEvents);
        }

        /// <summary>
        /// Callback cuando los joints de la mano derecha se actualizan.
        /// </summary>
        private void OnRightHandJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            CheckGestures(eventArgs, rightHandTrackingEvents);
        }

        /// <summary>
        /// Verifica todos los gestos contra el estado actual de la mano.
        /// </summary>
        private void CheckGestures(XRHandJointsUpdatedEventArgs eventArgs, XRHandTrackingEvents handTrackingEvents)
        {
            if (!isActiveAndEnabled || Time.timeSinceLevelLoad < timeOfLastCheck + detectionInterval)
                return;

            foreach (var sign in targetSigns)
            {
                if (sign == null || sign.handShapeOrPose == null)
                    continue;

                // Si el gesto ya fue confirmado, no lo vuelvas a detectar
                if (performedTriggered.ContainsKey(sign) && performedTriggered[sign])
                    continue;

                // Obtiene el Hand Shape o Hand Pose
                var handShape = sign.GetHandShape();
                var handPose = sign.GetHandPose();

                // Verifica si el gesto cumple las condiciones
                bool detected = handTrackingEvents.handIsTracked &&
                                ((handShape != null && handShape.CheckConditions(eventArgs)) ||
                                 (handPose != null && handPose.CheckConditions(eventArgs)));

                bool wasDetectedBefore = wasDetected.ContainsKey(sign) && wasDetected[sign];

                // Inicio de detección
                if (!wasDetectedBefore && detected)
                {
                    holdStartTimes[sign] = Time.timeSinceLevelLoad;
                    wasDetected[sign] = true;

                    if (showDebugLogs)
                        Debug.Log($"MultiGestureRecognizer: Gesto '{sign.signName}' detectado, esperando hold time.");
                }
                // Fin de detección
                else if (wasDetectedBefore && !detected)
                {
                    wasDetected[sign] = false;

                    if (showDebugLogs)
                        Debug.Log($"MultiGestureRecognizer: Gesto '{sign.signName}' perdido.");
                }

                // Verifica si el hold time ha sido cumplido
                if (detected && wasDetected[sign])
                {
                    float requiredHoldTime = sign.minimumHoldTime > 0 ? sign.minimumHoldTime : minimumHoldTime;
                    float holdTimer = Time.timeSinceLevelLoad - holdStartTimes[sign];

                    if (holdTimer >= requiredHoldTime && !performedTriggered[sign])
                    {
                        performedTriggered[sign] = true;
                        onGestureDetected?.Invoke(sign);

                        if (showDebugLogs)
                            Debug.Log($"MultiGestureRecognizer: Gesto '{sign.signName}' confirmado!");
                    }
                }
            }

            timeOfLastCheck = Time.timeSinceLevelLoad;
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
    }
}
