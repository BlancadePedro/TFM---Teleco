using System.Collections.Generic;
using UnityEngine;

namespace ASL.DynamicGestures
{
    /// <summary>
    /// Define de forma declarativa un gesto dinámico completo con sus requisitos de pose y movimiento.
    /// Optimizado para Meta Quest 3 con parámetros conservadores para compensar limitaciones de tracking.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGesture", menuName = "ASL/Dynamic Gesture Definition", order = 1)]
    public class DynamicGestureDefinition : ScriptableObject
    {
        [Header("Identificación")]
        [Tooltip("Nombre único del gesto (ej: 'Hola', 'Adiós', 'J')")]
        public string gestureName;

        [Tooltip("Descripción técnica del gesto para debugging")]
        public string gestureDescription;

        [Header("Secuencia de Poses")]
        [Tooltip("Lista ordenada de poses estáticas requeridas durante el gesto")]
        public List<StaticPoseRequirement> poseSequence = new List<StaticPoseRequirement>();

        [Header("Requisitos de Movimiento")]
        [Tooltip("Si es false, el gesto es puramente estático (raro)")]
        public bool requiresMovement = true;

        [Tooltip("Dirección principal esperada del movimiento en espacio local (normalizado)")]
        public Vector3 primaryDirection = Vector3.forward;

        [Tooltip("Tolerancia angular en grados. MÍNIMO 40° para Quest 3. Rango: 30-90")]
        [Range(0f, 90f)]
        public float directionTolerance = 45f;

        [Tooltip("Velocidad mínima en m/s. NO menor a 0.12 para usuarios lentos")]
        [Range(0.05f, 1f)]
        public float minSpeed = 0.12f;

        [Tooltip("Distancia total mínima en metros. Rango recomendado: 0.06-0.15")]
        [Range(0.01f, 0.5f)]
        public float minDistance = 0.08f;

        [Header("Requisitos Temporales")]
        [Tooltip("Duración mínima del gesto en segundos. Dar tiempo suficiente: 0.4-0.5s")]
        [Range(0.1f, 2f)]
        public float minDuration = 0.4f;

        [Tooltip("Duración máxima antes de timeout en segundos")]
        [Range(0.5f, 5f)]
        public float maxDuration = 3f;

        [Header("Requisitos Opcionales - Cambios de Dirección")]
        [Tooltip("Activar para gestos tipo zigzag, sacudir, ondular")]
        public bool requiresDirectionChange = false;

        [Tooltip("Número mínimo de cambios de dirección detectados (ángulo > 45°)")]
        [Range(0, 5)]
        public int requiredDirectionChanges = 0;

        [Header("Requisitos Opcionales - Rotación")]
        [Tooltip("USAR CON PRECAUCIÓN: Quest 3 tiene jitter en rotaciones. Preferir cambios de dirección")]
        public bool requiresRotation = false;

        [Tooltip("Eje de rotación esperado en espacio local")]
        public Vector3 rotationAxis = Vector3.up;

        [Tooltip("Ángulo mínimo de rotación en grados. MÁXIMO 45° recomendado")]
        [Range(0f, 180f)]
        public float minRotationAngle = 30f;

        [Header("Requisitos Opcionales - Movimiento Circular")]
        [Tooltip("Activar para gestos que requieran trayectoria circular (ej: 'Por favor')")]
        public bool requiresCircularMotion = false;

        [Tooltip("Score mínimo de circularidad: 0=línea recta, 1=círculo perfecto")]
        [Range(0f, 1f)]
        public float minCircularityScore = 0.6f;

        [Header("Requisitos Opcionales - Zona Espacial")]
        [Tooltip("Activar si el gesto debe ejecutarse en una región específica del espacio")]
        public bool requiresSpatialZone = false;

        [Tooltip("Centro de la zona en coordenadas relativas a XR Origin (ej: pecho = (0, -0.2, 0.3))")]
        public Vector3 zoneCenter = Vector3.zero;

        [Tooltip("Radio aceptable de la zona en metros")]
        [Range(0.05f, 0.5f)]
        public float zoneRadius = 0.15f;

        [Tooltip("Momento en que validar la zona espacial")]
        public PoseTimingRequirement zoneValidationTiming = PoseTimingRequirement.End;

        /// <summary>
        /// Valida que la configuración sea coherente
        /// </summary>
        private void OnValidate()
        {
            // Normalizar dirección principal
            if (primaryDirection.sqrMagnitude > 0.01f)
            {
                primaryDirection = primaryDirection.normalized;
            }

            // Normalizar eje de rotación
            if (rotationAxis.sqrMagnitude > 0.01f)
            {
                rotationAxis = rotationAxis.normalized;
            }

            // Validar duración
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

            // Advertencias para configuraciones problemáticas
            if (requiresRotation && minRotationAngle > 45f)
            {
                Debug.LogWarning($"[{gestureName}] minRotationAngle > 45° puede ser difícil de detectar en Quest 3 debido a jitter", this);
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
        /// Obtiene poses requeridas en un momento específico del gesto
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
                // Usar comparaciÃ³n flexible (familias de pose) para tolerar variaciones
                if (pose.IsValidPose(poseName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
