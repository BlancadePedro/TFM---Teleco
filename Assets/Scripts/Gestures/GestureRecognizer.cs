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

        [Tooltip("Component XRHandTrackingEvents de la mano a detectar")]
        [SerializeField] private XRHandTrackingEvents handTrackingEvents;

        [Header("Detection Settings")]
        [Tooltip("Intervalo de deteccion en segundos")]
        [SerializeField] private float detectionInterval = 0.1f;

        [Tooltip("Usar el tiempo de hold del SignData (si es false, usa minimumHoldTime de abajo)")]
        [SerializeField] private bool useSignDataHoldTime = true;

        [Tooltip("Time minimo de hold si useSignDataHoldTime es false")]
        [SerializeField] private float minimumHoldTime = 0.3f;

        [Header("Events")]
        [Tooltip("Se invoca cuando el gesto es detected")]
        public UnityEvent<SignData> onGestureDetected;

        [Tooltip("Se invoca cuando el gesto termina")]
        public UnityEvent<SignData> onGestureEnded;

        [Header("Debug")]
        [Tooltip("Mostrar logs de debug en la consola")]
        [SerializeField] private bool showDebugLogs = true;

        [Header("Tolerancia")]
        [Tooltip("Time de tolerancia antes de considerar que el gesto se perdio (segundos)")]
        [SerializeField] private float detectionLossTolerance = 0.2f;

        private XRHandShape handShape;
        private XRHandPose handPose;
        private bool wasDetected = false;
        private bool performedTriggered = false;
        private float timeOfLastCheck = 0f;
        private float holdStartTime = 0f;
        private float lastDetectionTime = 0f;

        /// <summary>
        /// El current signmente configured para detectar.
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
        /// True si el gesto is being detected actualmente.
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
                // Asigna automaticamente la camara principal como target
                if (handPose.relativeOrientation.targetTransform == null)
                {
                    handPose.relativeOrientation.targetTransform = Camera.main.transform;
                    Debug.Log($"GestureRecognizer: Asignada camara principal como targetTransform para '{targetSign.signName}'");
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
                    Debug.LogWarning("[GestureRecognizer] NO HAY SignData assigned!");
                return;
            }

            if (showDebugLogs && Time.timeSinceLevelLoad % 3f < detectionInterval)
                Debug.Log($"[GestureRecognizer] Recibiendo datos de mano para '{targetSign.signName}'");

            if (handShape == null && handPose == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"GestureRecognizer: El signo '{targetSign.signName}' no tiene Hand Shape ni Hand Pose configured.");
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

            // Actualizar timestamp si esta detected
            if (detected)
            {
                lastDetectionTime = Time.timeSinceLevelLoad;
            }

            // Inicio de deteccion
            if (!wasDetected && detected)
            {
                holdStartTime = Time.timeSinceLevelLoad;

                if (showDebugLogs)
                    Debug.Log($"GestureRecognizer: Gesture '{targetSign.signName}' detected, esperando hold time.");

                wasDetected = true;
            }
            // Fin de deteccion con TOLERANCIA
            else if (wasDetected && !detected)
            {
                float timeSinceLoss = Time.timeSinceLevelLoad - lastDetectionTime;

                // Solo terminar si ha pasado suficiente tiempo sin deteccion
                if (timeSinceLoss > detectionLossTolerance)
                {
                    performedTriggered = false;
                    onGestureEnded?.Invoke(targetSign);
                    wasDetected = false;

                    if (showDebugLogs)
                        Debug.Log($"GestureRecognizer: Gesture '{targetSign.signName}' terminado (perdida de {timeSinceLoss:F2}s).");
                }
                // Si no, mantener wasDetected=true y continuar
            }

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
                        Debug.Log($"GestureRecognizer: Gesture '{targetSign.signName}' confirmado!");
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
        /// Habilita o deshabilita la deteccion.
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
