using UnityEngine;
using UnityEngine.XR.Hands;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.Gestures
{
    /// <summary>
    /// Define un gesto dinámico mediante parámetros configurables.
    /// No almacena grabaciones, sino criterios de movimiento evaluables en tiempo real.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDynamicGesture", menuName = "ASL Learn VR/Dynamic Gesture Definition", order = 2)]
    public class DynamicGestureDefinition : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Nombre del gesto dinámico (ej: 'J', 'Z')")]
        public string gestureName;

        [Tooltip("Descripción del gesto")]
        [TextArea(2, 4)]
        public string description;

        [Header("Joint Tracking")]
        [Tooltip("Joint principal a seguir durante el movimiento")]
        public XRHandJointID trackedJoint = XRHandJointID.IndexTip;

        [Tooltip("Joints secundarios para validación (opcional)")]
        public XRHandJointID[] secondaryJoints = new XRHandJointID[0];

        [Header("Movement Requirements")]
        [Tooltip("Dirección principal del movimiento esperado (en espacio local de la mano)")]
        public Vector3 primaryDirection = Vector3.down;

        [Tooltip("Tolerancia angular para la dirección (en grados, 0-180)")]
        [Range(0f, 180f)]
        public float directionTolerance = 45f;

        [Tooltip("Distancia mínima de movimiento (en metros)")]
        [Range(0.01f, 1f)]
        public float minimumDistance = 0.1f;

        [Tooltip("Distancia máxima de movimiento (en metros). Si es 0, no hay límite superior.")]
        [Range(0f, 2f)]
        public float maximumDistance = 0.5f;

        [Tooltip("Velocidad mínima promedio requerida (m/s)")]
        [Range(0f, 5f)]
        public float minimumAverageSpeed = 0.05f;

        [Tooltip("Velocidad máxima promedio permitida (m/s). Si es 0, no hay límite superior.")]
        [Range(0f, 10f)]
        public float maximumAverageSpeed = 2f;

        [Header("Time Constraints")]
        [Tooltip("Duración mínima del gesto (segundos)")]
        [Range(0.1f, 5f)]
        public float minimumDuration = 0.3f;

        [Tooltip("Duración máxima del gesto (segundos)")]
        [Range(0.1f, 10f)]
        public float maximumDuration = 3f;

        [Tooltip("Tiempo de espera antes de permitir detección de nuevo gesto (segundos)")]
        [Range(0f, 2f)]
        public float cooldownTime = 0.5f;

        [Header("Static Pose Requirements (Optional)")]
        [Tooltip("Pose estática requerida al inicio del gesto (opcional)")]
        public SignData requiredStartPose;

        [Tooltip("Si requiere pose inicial, ¿debe estar confirmada (hold) o solo detectada?")]
        public bool requireStartPoseConfirmed = false;

        [Tooltip("Pose estática requerida al final del gesto (opcional)")]
        public SignData requiredEndPose;

        [Tooltip("Si requiere pose final, ¿debe estar confirmada (hold) o solo detectada?")]
        public bool requireEndPoseConfirmed = false;

        [Header("Trajectory Smoothness")]
        [Tooltip("Curvatura máxima permitida (0 = línea recta, 1 = curva pronunciada permitida)")]
        [Range(0f, 1f)]
        public float maxCurvature = 0.5f;

        [Tooltip("Evaluar la suavidad de la trayectoria")]
        public bool evaluateSmoothness = true;

        [Header("Sampling")]
        [Tooltip("Frecuencia de muestreo de posiciones (Hz)")]
        [Range(10, 90)]
        public int samplingRate = 30;

        [Tooltip("Tamaño máximo del buffer de trayectoria (puntos)")]
        [Range(10, 500)]
        public int maxBufferSize = 150;

        /// <summary>
        /// Valida que la definición del gesto esté correctamente configurada.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(gestureName))
            {
                Debug.LogError($"DynamicGestureDefinition '{name}' tiene un gestureName vacío.");
                return false;
            }

            if (minimumDistance <= 0f)
            {
                Debug.LogError($"DynamicGestureDefinition '{gestureName}' tiene minimumDistance inválida.");
                return false;
            }

            if (maximumDistance > 0f && maximumDistance < minimumDistance)
            {
                Debug.LogError($"DynamicGestureDefinition '{gestureName}': maximumDistance debe ser mayor que minimumDistance.");
                return false;
            }

            if (minimumDuration <= 0f || maximumDuration < minimumDuration)
            {
                Debug.LogError($"DynamicGestureDefinition '{gestureName}': duraciones inválidas.");
                return false;
            }

            if (maximumAverageSpeed > 0f && maximumAverageSpeed < minimumAverageSpeed)
            {
                Debug.LogError($"DynamicGestureDefinition '{gestureName}': maximumAverageSpeed debe ser mayor que minimumAverageSpeed.");
                return false;
            }

            return true;
        }
    }
}
