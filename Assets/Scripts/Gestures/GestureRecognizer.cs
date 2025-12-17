using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.Gestures
{
    /// <summary>
    /// Reconoce gestos ASL utilizando Unity XR Hands.
    /// Basado en StaticHandGesture pero adaptado para usar SignData.
    /// </summary>
    public class GestureRecognizer : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("El signo que se debe detectar")]
        [SerializeField] private SignData targetSign;

        [Tooltip("Componente XRHandTrackingEvents de la mano a detectar")]
        [SerializeField] private XRHandTrackingEvents handTrackingEvents;

        [Header("Detection Settings")]
        [Tooltip("Intervalo de detección en segundos")]
        [SerializeField] private float detectionInterval = 0.1f;

        [Tooltip("Usar el tiempo de hold del SignData (si es false, usa minimumHoldTime de abajo)")]
        [SerializeField] private bool useSignDataHoldTime = true;

        [Tooltip("Tiempo mínimo de hold si useSignDataHoldTime es false")]
        [SerializeField] private float minimumHoldTime = 0.3f;

        [Header("Events")]
        [Tooltip("Se invoca cuando el gesto es detectado")]
        public UnityEvent<SignData> onGestureDetected;

        [Tooltip("Se invoca cuando el gesto termina")]
        public UnityEvent<SignData> onGestureEnded;

        [Header("Debug")]
        [Tooltip("Mostrar logs de debug en la consola")]
        [SerializeField] private bool showDebugLogs = true;

        private XRHandShape handShape;
        private XRHandPose handPose;
        private bool wasDetected = false;
        private bool performedTriggered = false;
        private float timeOfLastCheck = 0f;
        private float holdStartTime = 0f;

        /// <summary>
        /// El signo actualmente configurado para detectar.
        /// </summary>
        public SignData TargetSign
        {
            get => targetSign;
            set
            {
                targetSign = value;
                InitializeGestureReferences();
            }
        }

        /// <summary>
        /// True si el gesto está siendo detectado actualmente.
        /// </summary>
        public bool IsDetected => wasDetected;

        /// <summary>
        /// True si el gesto ha sido confirmado (hold time cumplido).
        /// </summary>
        public bool IsPerformed => performedTriggered;

        void OnEnable()
        {
            if (handTrackingEvents != null)
            {
                handTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
                Debug.Log($"[GestureRecognizer] ACTIVADO con handTrackingEvents para '{(targetSign != null ? targetSign.signName : "sin signo")}'");
            }
            else
            {
                Debug.LogError("[GestureRecognizer] FALTA ASIGNAR 'handTrackingEvents'! Ve al Inspector y arrastra el GameObject 'Right Hand' al campo 'Hand Tracking Events'");
            }

            InitializeGestureReferences();
        }

        void OnDisable()
        {
            if (handTrackingEvents != null)
            {
                handTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);
            }
        }

        /// <summary>
        /// Inicializa las referencias al Hand Shape o Hand Pose.
        /// </summary>
        private void InitializeGestureReferences()
        {
            if (targetSign == null || targetSign.handShapeOrPose == null)
            {
                handShape = null;
                handPose = null;
                return;
            }

            handShape = targetSign.GetHandShape();
            handPose = targetSign.GetHandPose();

            if (handPose != null && handPose.relativeOrientation != null)
            {
                // Asigna automáticamente la cámara principal como target
                if (handPose.relativeOrientation.targetTransform == null)
                {
                    handPose.relativeOrientation.targetTransform = Camera.main.transform;
                    Debug.Log($"GestureRecognizer: Asignada cámara principal como targetTransform para '{targetSign.signName}'");
                }
            }
        }

        /// <summary>
        /// Callback cuando los joints de la mano se actualizan.
        /// </summary>
        private void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            if (!isActiveAndEnabled)
            {
                if (showDebugLogs && Time.timeSinceLevelLoad % 5f < detectionInterval)
                    Debug.LogWarning("[GestureRecognizer] COMPONENTE DESACTIVADO!");
                return;
            }

            if (Time.timeSinceLevelLoad < timeOfLastCheck + detectionInterval)
                return;

            if (targetSign == null)
            {
                if (showDebugLogs && Time.timeSinceLevelLoad % 5f < detectionInterval)
                    Debug.LogWarning("[GestureRecognizer] NO HAY SignData asignado!");
                return;
            }

            if (showDebugLogs && Time.timeSinceLevelLoad % 3f < detectionInterval)
                Debug.Log($"[GestureRecognizer] Recibiendo datos de mano para '{targetSign.signName}'");

            if (handShape == null && handPose == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"GestureRecognizer: El signo '{targetSign.signName}' no tiene Hand Shape ni Hand Pose configurado.");
                return;
            }

            // Verifica si el gesto cumple las condiciones
            bool handTracked = handTrackingEvents.handIsTracked;
            bool shapeMatches = handShape != null && handShape.CheckConditions(eventArgs);
            bool poseMatches = handPose != null && handPose.CheckConditions(eventArgs);
            bool detected = handTracked && (shapeMatches || poseMatches);

            if (showDebugLogs && Time.timeSinceLevelLoad % 2f < detectionInterval)
            {
                Debug.Log($"GestureRecognizer [{targetSign.signName}]: Tracked={handTracked}, Shape={shapeMatches}, Pose={poseMatches}, Detected={detected}");
            }

            // Inicio de detección
            if (!wasDetected && detected)
            {
                holdStartTime = Time.timeSinceLevelLoad;

                if (showDebugLogs)
                    Debug.Log($"GestureRecognizer: Gesto '{targetSign.signName}' detectado, esperando hold time.");
            }
            // Fin de detección
            else if (wasDetected && !detected)
            {
                performedTriggered = false;
                onGestureEnded?.Invoke(targetSign);

                if (showDebugLogs)
                    Debug.Log($"GestureRecognizer: Gesto '{targetSign.signName}' terminado.");
            }

            wasDetected = detected;

            // Verifica si el hold time ha sido cumplido
            if (!performedTriggered && detected)
            {
                float requiredHoldTime = useSignDataHoldTime ? targetSign.minimumHoldTime : minimumHoldTime;
                float holdTimer = Time.timeSinceLevelLoad - holdStartTime;

                if (holdTimer >= requiredHoldTime)
                {
                    performedTriggered = true;
                    onGestureDetected?.Invoke(targetSign);

                    if (showDebugLogs)
                        Debug.Log($"GestureRecognizer: Gesto '{targetSign.signName}' confirmado!");
                }
            }

            timeOfLastCheck = Time.timeSinceLevelLoad;
        }

        /// <summary>
        /// Resetea el estado del recognizer.
        /// </summary>
        public void ResetState()
        {
            wasDetected = false;
            performedTriggered = false;
            holdStartTime = 0f;
        }

        /// <summary>
        /// Habilita o deshabilita la detección.
        /// </summary>
        public void SetDetectionEnabled(bool enabled)
        {
            Debug.Log($"[GestureRecognizer] SetDetectionEnabled({enabled}) llamado para '{(targetSign != null ? targetSign.signName : "sin signo")}'");
            this.enabled = enabled;
            if (!enabled)
                ResetState();
        }
    }
}
