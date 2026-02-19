using System.Collections.Generic;
using UnityEngine;

namespace ASL.DynamicGestures
{
    /// <summary>
    /// Define de forma declarativa un dynamic gesture completo con sus requisitos de pose y movimiento.
    /// Optimizado para Meta Quest 3 con parametros conservadores para compensar limitaciones de tracking.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGesture", menuName = "ASL/Dynamic Gesture Definition", order = 1)]
    public class DynamicGestureDefinition : ScriptableObject
    {
        [Header("Identificacion")]
        [Tooltip("Name unico del gesto (ej: 'Hola', 'Adios', 'J')")]
        public string gestureName;

        [Tooltip("Technical gesture description for debugging")]
        public string gestureDescription;

        [Header("Secuencia de Poses")]
        [Tooltip("List ordenada de poses estaticas requeridas durante el gesto")]
        public List<StaticPoseRequirement> poseSequence = new List<StaticPoseRequirement>();

        [Header("Requisitos de Movimiento")]
        [Tooltip("Si es false, el gesto es puramente static (raro)")]
        public bool requiresMovement = true;

        [Tooltip("Direccion principal esperada del movimiento en espacio local (normalizado)")]
        public Vector3 primaryDirection = Vector3.forward;

        [Tooltip("Tolerancia angular en degrees. MINIMO 40° para Quest 3. Rango: 30-90")]
        [Range(0f, 90f)]
        public float directionTolerance = 45f;

        [Tooltip("Speed minima en m/s. NO menor a 0.12 para usuarios lentos")]
        [Range(0.05f, 1f)]
        public float minSpeed = 0.12f;

        [Tooltip("Distance total minima en metros. Rango recomendado: 0.06-0.15")]
        [Range(0.01f, 0.5f)]
        public float minDistance = 0.08f;

        [Header("Requisitos Temporales")]
        [Tooltip("Duration minima del gesto en segundos. Dar tiempo suficiente: 0.4-0.5s")]
        [Range(0.1f, 2f)]
        public float minDuration = 0.4f;

        [Tooltip("Duration maxima antes de timeout en segundos")]
        [Range(0.5f, 5f)]
        public float maxDuration = 3f;

        [Header("Requisitos Optionales - Cambios de Direccion")]
        [Tooltip("Enable for gestures tipo zigzag, sacudir, ondular")]
        public bool requiresDirectionChange = false;

        [Tooltip("Minimum number of direction changes detecteds (angulo > 45°)")]
        [Range(0, 5)]
        public int requiredDirectionChanges = 0;

        [Header("Requisitos Optionales - Rotation")]
        [Tooltip("USAR CON PRECAUCION: Quest 3 tiene jitter en rotaciones. Preferir cambios de direccion")]
        public bool requiresRotation = false;

        [Tooltip("Eje de rotacion esperado en espacio local")]
        public Vector3 rotationAxis = Vector3.up;

        [Tooltip("Angle minimo de rotacion en degrees. MAXIMO 45° recomendado")]
        [Range(0f, 180f)]
        public float minRotationAngle = 30f;

        [Header("Requisitos Optionales - Movimiento Circular")]
        [Tooltip("Enable for gestures que requieran trayectoria circular (ej: 'Por favor')")]
        public bool requiresCircularMotion = false;

        [Tooltip("Score minimo de circularidad: 0=linea recta, 1=circulo perfecto")]
        [Range(0f, 1f)]
        public float minCircularityScore = 0.6f;

        [Header("Requisitos Optionales - Zona Espacial")]
        [Tooltip("Enable if the gesture must be performed in a specific spatial region")]
        public bool requiresSpatialZone = false;

        [Tooltip("Center de la zona en coordenadas relativas a XR Origin (ej: pecho = (0, -0.2, 0.3))")]
        public Vector3 zoneCenter = Vector3.zero;

        [Tooltip("Radius aceptable de la zona en metros")]
        [Range(0.05f, 0.5f)]
        public float zoneRadius = 0.15f;

        [Tooltip("Momento en que validar la zona espacial")]
        public PoseTimingRequirement zoneValidationTiming = PoseTimingRequirement.End;

        /// <summary>
        /// Valida que la configuracion sea coherente
        /// </summary>
        private void OnValidate()
        {
            // Normalizar direccion principal
            if (primaryDirection.sqrMagnitude > 0.01f)
            {
                primaryDirection = primaryDirection.normalized;
            }

            // Normalizar eje de rotacion
            if (rotationAxis.sqrMagnitude > 0.01f)
            {
                rotationAxis = rotationAxis.normalized;
            }

            // Validar duracion
            if (minDuration >= maxDuration)
            {
                maxDuration = minDuration + 0.5f;
            }

            // Validar secuencia de poses
            if (poseSequence != null && poseSequence.Count > 0)
            {
                foreach (var pose in poseSequence)
                {
                    if (string.IsNullOrEmpty(pose.poseName))
                    {
                        Debug.LogWarning($"[{gestureName}] Pose sin nombre en la secuencia", this);
                    }
                }
            }

            // Warnings para configuraciones problematicas
            if (requiresRotation && minRotationAngle > 45f)
            {
                Debug.LogWarning($"[{gestureName}] minRotationAngle > 45° puede ser dificil de detectar en Quest 3 due to jitter", this);
            }

            if (directionTolerance < 40f && requiresMovement)
            {
                Debug.LogWarning($"[{gestureName}] directionTolerance < 40° puede ser muy estricto para Quest 3", this);
            }

            if (minSpeed < 0.12f && requiresMovement)
            {
                Debug.LogWarning($"[{gestureName}] minSpeed < 0.12 m/s puede ser indetectable con usuarios lentos", this);
            }
        }

        /// <summary>
        /// Obtiene poses requeridas en un momento especifico del gesto
        /// </summary>
        public List<StaticPoseRequirement> GetPosesForTiming(PoseTimingRequirement timing)
        {
            List<StaticPoseRequirement> result = new List<StaticPoseRequirement>();

            if (poseSequence == null) return result;

            foreach (var pose in poseSequence)
            {
                if (pose.timing == timing || pose.timing == PoseTimingRequirement.Any)
                {
                    result.Add(pose);
                }
            }

            return result;
        }

        /// <summary>
        /// Comprueba si el gesto tiene poses de inicio definidas
        /// </summary>
        public bool HasStartPose()
        {
            if (poseSequence == null || poseSequence.Count == 0) return false;

            foreach (var pose in poseSequence)
            {
                if (pose.timing == PoseTimingRequirement.Start && !pose.isOptional)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Comprueba si una pose puede iniciar este gesto
        /// </summary>
        public bool CanStartWithPose(string poseName)
        {
            if (string.IsNullOrEmpty(poseName) || !HasStartPose()) return false;

            var startPoses = GetPosesForTiming(PoseTimingRequirement.Start);

            foreach (var pose in startPoses)
            {
                // Use flexible comparison (pose families) to tolerate variations
                if (pose.IsValidPose(poseName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
