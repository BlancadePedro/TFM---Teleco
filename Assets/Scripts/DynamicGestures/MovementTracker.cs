using System.Collections.Generic;
using UnityEngine;

namespace ASL.DynamicGestures
{
    /// <summary>
    /// Rastrea y analiza el movimiento de la mano a lo largo del tiempo sin allocations en Update.
    /// Calcula métricas como distancia total, velocidad, dirección promedio, cambios de dirección,
    /// rotación total y score de circularidad.
    /// </summary>
    public class MovementTracker
    {
        // Configuración
        private readonly float windowSize; // Segundos de historial a mantener
        private readonly int maxHistorySize; // Máximo número de samples

        // Historial (preallocado, cero allocations en runtime)
        private readonly Queue<Vector3> positionHistory;
        private readonly Queue<Quaternion> rotationHistory;
        private readonly Queue<float> timestamps;

        // Estado actual
        private Vector3 startPosition;
        private Quaternion startRotation;
        private float startTime;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private float lastTimestamp;

        // Métricas calculadas
        private float totalDistance;
        private float totalRotation;
        private int directionChanges;
        private Vector3 lastDirection;

        // Cache para evitar allocations
        private readonly List<Vector3> cachedPositions;
        private readonly List<float> cachedRadii;

        /// <summary>
        /// Posición actual de la mano
        /// </summary>
        public Vector3 CurrentPosition { get; private set; }

        /// <summary>
        /// Posición donde empezó el tracking
        /// </summary>
        public Vector3 StartPosition => startPosition;

        /// <summary>
        /// Distancia total recorrida en metros
        /// </summary>
        public float TotalDistance => totalDistance;

        /// <summary>
        /// Velocidad actual en m/s (calculada sobre últimos 3 frames)
        /// </summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>
        /// Dirección promedio del movimiento (normalizada, basada en último 30% de la trayectoria)
        /// </summary>
        public Vector3 AverageDirection { get; private set; }

        /// <summary>
        /// Rotación total acumulada en grados
        /// </summary>
        public float TotalRotation => totalRotation;

        /// <summary>
        /// Número de cambios de dirección significativos detectados (ángulo > 45°)
        /// </summary>
        public int DirectionChanges => directionChanges;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="windowSize">Ventana temporal en segundos (ej: 3.0f)</param>
        /// <param name="historySize">Número máximo de samples (ej: 120 para 60fps * 2s)</param>
        public MovementTracker(float windowSize = 3f, int historySize = 120)
        {
            this.windowSize = windowSize;
            this.maxHistorySize = historySize;

            // Preallocar estructuras
            positionHistory = new Queue<Vector3>(historySize);
            rotationHistory = new Queue<Quaternion>(historySize);
            timestamps = new Queue<float>(historySize);

            cachedPositions = new List<Vector3>(historySize);
            cachedRadii = new List<float>(historySize);

            Reset();
        }

        /// <summary>
        /// Reinicia el tracking para un nuevo gesto
        /// </summary>
        public void Reset()
        {
            positionHistory.Clear();
            rotationHistory.Clear();
            timestamps.Clear();

            startPosition = Vector3.zero;
            startRotation = Quaternion.identity;
            startTime = Time.time;
            lastPosition = Vector3.zero;
            lastRotation = Quaternion.identity;
            lastTimestamp = Time.time;

            CurrentPosition = Vector3.zero;
            totalDistance = 0f;
            totalRotation = 0f;
            directionChanges = 0;
            lastDirection = Vector3.zero;

            CurrentSpeed = 0f;
            AverageDirection = Vector3.zero;
        }

        /// <summary>
        /// Actualiza el tracking con nueva posición y rotación
        /// </summary>
        public void UpdateTracking(Vector3 position, Quaternion rotation)
        {
            float currentTime = Time.time;

            // Primera muestra
            if (positionHistory.Count == 0)
            {
                startPosition = position;
                startRotation = rotation;
                startTime = currentTime;
                lastPosition = position;
                lastRotation = rotation;
                lastTimestamp = currentTime;

                positionHistory.Enqueue(position);
                rotationHistory.Enqueue(rotation);
                timestamps.Enqueue(currentTime);

                CurrentPosition = position;
                return;
            }

            // Agregar nueva muestra
            positionHistory.Enqueue(position);
            rotationHistory.Enqueue(rotation);
            timestamps.Enqueue(currentTime);

            // Limitar tamaño de historial
            while (positionHistory.Count > maxHistorySize)
            {
                positionHistory.Dequeue();
                rotationHistory.Dequeue();
                timestamps.Dequeue();
            }

            // Eliminar muestras fuera de ventana temporal
            while (timestamps.Count > 0 && (currentTime - timestamps.Peek()) > windowSize)
            {
                positionHistory.Dequeue();
                rotationHistory.Dequeue();
                timestamps.Dequeue();
            }

            // Calcular distancia incremental
            float frameDist = Vector3.Distance(position, lastPosition);
            totalDistance += frameDist;

            // Detectar cambio de dirección
            if (frameDist > 0.001f) // Umbral para evitar noise
            {
                Vector3 currentDirection = (position - lastPosition).normalized;

                if (lastDirection.sqrMagnitude > 0.01f)
                {
                    float angle = Vector3.Angle(lastDirection, currentDirection);
                    if (angle > 30f) // 30° (antes 45°) - más sensible para waving suave
                    {
                        directionChanges++;
                    }
                }

                lastDirection = currentDirection;
            }

            // Calcular rotación incremental
            float frameRotation = Quaternion.Angle(rotation, lastRotation);
            totalRotation += frameRotation;

            // Actualizar estado
            CurrentPosition = position;
            lastPosition = position;
            lastRotation = rotation;
            lastTimestamp = currentTime;

            // Recalcular métricas
            CalculateSpeed();
            CalculateAverageDirection();
        }

        /// <summary>
        /// Calcula velocidad actual basada en últimos 3 frames
        /// </summary>
        private void CalculateSpeed()
        {
            if (positionHistory.Count < 3)
            {
                CurrentSpeed = 0f;
                return;
            }

            // Convertir a array temporal para acceso indexado
            Vector3[] positions = new Vector3[positionHistory.Count];
            float[] times = new float[timestamps.Count];

            int idx = 0;
            foreach (var pos in positionHistory)
                positions[idx++] = pos;

            idx = 0;
            foreach (var t in timestamps)
                times[idx++] = t;

            // Calcular distancia de últimos 3 frames
            int count = positions.Length;
            float dist = Vector3.Distance(positions[count - 1], positions[count - 2]) +
                         Vector3.Distance(positions[count - 2], positions[count - 3]);

            float deltaTime = times[count - 1] - times[count - 3];

            CurrentSpeed = deltaTime > 0.001f ? dist / deltaTime : 0f;
        }

        /// <summary>
        /// Calcula dirección promedio del último 30% del movimiento
        /// </summary>
        private void CalculateAverageDirection()
        {
            if (positionHistory.Count < 2)
            {
                AverageDirection = Vector3.zero;
                return;
            }

            // Convertir a array
            Vector3[] positions = new Vector3[positionHistory.Count];
            int idx = 0;
            foreach (var pos in positionHistory)
                positions[idx++] = pos;

            // Último 30% de samples
            int startIdx = Mathf.Max(0, positions.Length - Mathf.CeilToInt(positions.Length * 0.3f));

            Vector3 sumDirection = Vector3.zero;
            int validSamples = 0;

            for (int i = startIdx; i < positions.Length - 1; i++)
            {
                Vector3 dir = positions[i + 1] - positions[i];
                if (dir.sqrMagnitude > 0.0001f) // Evitar vectores cero
                {
                    sumDirection += dir.normalized;
                    validSamples++;
                }
            }

            AverageDirection = validSamples > 0 ? (sumDirection / validSamples).normalized : Vector3.zero;
        }

        /// <summary>
        /// Calcula score de circularidad del movimiento (0=línea, 1=círculo perfecto)
        /// </summary>
        public float GetCircularityScore()
        {
            // Requiere al menos 10 samples para análisis significativo
            if (positionHistory.Count < 10)
                return 0f;

            // Copiar a lista temporal (cached, sin allocations repetidas)
            cachedPositions.Clear();
            foreach (var pos in positionHistory)
                cachedPositions.Add(pos);

            // 1. Calcular centro promedio de todas las posiciones
            Vector3 center = Vector3.zero;
            for (int i = 0; i < cachedPositions.Count; i++)
                center += cachedPositions[i];
            center /= cachedPositions.Count;

            // 2. Calcular radio promedio (distancia al centro)
            float avgRadius = 0f;
            cachedRadii.Clear();
            for (int i = 0; i < cachedPositions.Count; i++)
            {
                float radius = Vector3.Distance(cachedPositions[i], center);
                cachedRadii.Add(radius);
                avgRadius += radius;
            }
            avgRadius /= cachedPositions.Count;

            // Si el radio es muy pequeño, no hay movimiento circular significativo
            if (avgRadius < 0.01f)
                return 0f;

            // 3. Calcular varianza de radios
            float variance = 0f;
            for (int i = 0; i < cachedRadii.Count; i++)
            {
                float diff = cachedRadii[i] - avgRadius;
                variance += diff * diff;
            }
            variance /= cachedRadii.Count;

            // 4. Score de circularidad basado en consistencia de radio
            // Baja varianza = puntos equidistantes del centro = círculo
            float circularity = 1f - Mathf.Clamp01(variance / (avgRadius * avgRadius));

            return circularity;
        }

        /// <summary>
        /// Comprueba si el movimiento actual va en una dirección específica
        /// </summary>
        /// <param name="targetDirection">Dirección objetivo (normalizada)</param>
        /// <param name="toleranceDegrees">Tolerancia angular en grados</param>
        /// <returns>True si la dirección promedio está dentro de la tolerancia</returns>
        public bool IsMovingInDirection(Vector3 targetDirection, float toleranceDegrees)
        {
            if (AverageDirection.sqrMagnitude < 0.01f || targetDirection.sqrMagnitude < 0.01f)
                return false;

            float angle = Vector3.Angle(AverageDirection, targetDirection.normalized);
            return angle <= toleranceDegrees;
        }

        /// <summary>
        /// Obtiene duración total del tracking en segundos
        /// </summary>
        public float GetDuration()
        {
            return Time.time - startTime;
        }

        /// <summary>
        /// Obtiene número de samples en historial
        /// </summary>
        public int GetSampleCount()
        {
            return positionHistory.Count;
        }
    }
}
