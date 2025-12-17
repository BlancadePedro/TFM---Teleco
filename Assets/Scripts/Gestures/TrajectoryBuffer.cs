using UnityEngine;
using System.Collections.Generic;

namespace ASL_LearnVR.Gestures
{
    /// <summary>
    /// Almacena una secuencia temporal de posiciones de un joint.
    /// Proporciona métodos para analizar el movimiento capturado.
    /// </summary>
    public class TrajectoryBuffer
    {
        private struct TrajectoryPoint
        {
            public Vector3 position;
            public float timestamp;

            public TrajectoryPoint(Vector3 position, float timestamp)
            {
                this.position = position;
                this.timestamp = timestamp;
            }
        }

        private List<TrajectoryPoint> points;
        private int maxCapacity;
        private float startTime;

        /// <summary>
        /// Número de puntos actualmente almacenados.
        /// </summary>
        public int Count => points.Count;

        /// <summary>
        /// True si el buffer está vacío.
        /// </summary>
        public bool IsEmpty => points.Count == 0;

        /// <summary>
        /// Duración total de la trayectoria almacenada (segundos).
        /// </summary>
        public float Duration => IsEmpty ? 0f : points[points.Count - 1].timestamp - startTime;

        /// <summary>
        /// Primera posición en el buffer.
        /// </summary>
        public Vector3 FirstPosition => IsEmpty ? Vector3.zero : points[0].position;

        /// <summary>
        /// Última posición en el buffer.
        /// </summary>
        public Vector3 LastPosition => IsEmpty ? Vector3.zero : points[points.Count - 1].position;

        public TrajectoryBuffer(int maxCapacity = 150)
        {
            this.maxCapacity = maxCapacity;
            points = new List<TrajectoryPoint>(maxCapacity);
            startTime = 0f;
        }

        /// <summary>
        /// Añade un nuevo punto a la trayectoria.
        /// </summary>
        public void AddPoint(Vector3 position, float timestamp)
        {
            if (points.Count == 0)
            {
                startTime = timestamp;
            }

            points.Add(new TrajectoryPoint(position, timestamp));

            // Si excede la capacidad, elimina el punto más antiguo
            if (points.Count > maxCapacity)
            {
                points.RemoveAt(0);
                startTime = points[0].timestamp;
            }
        }

        /// <summary>
        /// Limpia todos los puntos del buffer.
        /// </summary>
        public void Clear()
        {
            points.Clear();
            startTime = 0f;
        }

        /// <summary>
        /// Calcula la distancia total recorrida.
        /// </summary>
        public float CalculateTotalDistance()
        {
            if (points.Count < 2)
                return 0f;

            float totalDistance = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                totalDistance += Vector3.Distance(points[i - 1].position, points[i].position);
            }

            return totalDistance;
        }

        /// <summary>
        /// Calcula la distancia directa entre el primer y último punto.
        /// </summary>
        public float CalculateDirectDistance()
        {
            if (points.Count < 2)
                return 0f;

            return Vector3.Distance(FirstPosition, LastPosition);
        }

        /// <summary>
        /// Calcula la velocidad promedio (m/s).
        /// </summary>
        public float CalculateAverageSpeed()
        {
            if (Duration <= 0f)
                return 0f;

            return CalculateTotalDistance() / Duration;
        }

        /// <summary>
        /// Calcula la dirección principal del movimiento (normalizada).
        /// </summary>
        public Vector3 CalculatePrimaryDirection()
        {
            if (points.Count < 2)
                return Vector3.zero;

            Vector3 direction = LastPosition - FirstPosition;
            return direction.normalized;
        }

        /// <summary>
        /// Calcula la curvatura de la trayectoria.
        /// Retorna 0 para línea recta, valores más altos para trayectorias más curvas.
        /// </summary>
        public float CalculateCurvature()
        {
            if (points.Count < 3)
                return 0f;

            float directDistance = CalculateDirectDistance();
            if (directDistance < 0.001f)
                return 1f; // Movimiento mínimo, máxima curvatura

            float totalDistance = CalculateTotalDistance();

            // Ratio entre distancia total y distancia directa
            // 1.0 = línea recta perfecta
            // Valores > 1.0 indican curvatura
            float ratio = totalDistance / directDistance;

            // Normaliza a rango [0, 1] donde 0 = recta, 1 = muy curva
            return Mathf.Clamp01((ratio - 1f) * 2f);
        }

        /// <summary>
        /// Verifica si la trayectoria se mueve principalmente en la dirección especificada.
        /// </summary>
        /// <param name="expectedDirection">Dirección esperada (normalizada).</param>
        /// <param name="toleranceAngle">Tolerancia en grados (0-180).</param>
        /// <returns>True si la dirección está dentro de la tolerancia.</returns>
        public bool IsMovingInDirection(Vector3 expectedDirection, float toleranceAngle)
        {
            if (points.Count < 2)
                return false;

            Vector3 actualDirection = CalculatePrimaryDirection();

            if (actualDirection == Vector3.zero || expectedDirection == Vector3.zero)
                return false;

            float angle = Vector3.Angle(actualDirection, expectedDirection);
            return angle <= toleranceAngle;
        }

        /// <summary>
        /// Evalúa la suavidad del movimiento.
        /// Retorna un valor entre 0 (movimiento errático) y 1 (movimiento suave).
        /// </summary>
        public float EvaluateSmoothness()
        {
            if (points.Count < 3)
                return 1f;

            float totalVariation = 0f;

            for (int i = 2; i < points.Count; i++)
            {
                Vector3 dir1 = (points[i - 1].position - points[i - 2].position).normalized;
                Vector3 dir2 = (points[i].position - points[i - 1].position).normalized;

                // Mide el cambio de dirección
                float angleChange = Vector3.Angle(dir1, dir2);
                totalVariation += angleChange;
            }

            // Promedia la variación angular
            float avgVariation = totalVariation / (points.Count - 2);

            // Normaliza: 0° = suave (1.0), 180° = errático (0.0)
            return Mathf.Clamp01(1f - (avgVariation / 180f));
        }

        /// <summary>
        /// Obtiene una copia de todas las posiciones almacenadas.
        /// </summary>
        public Vector3[] GetPositions()
        {
            Vector3[] positions = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                positions[i] = points[i].position;
            }
            return positions;
        }

        /// <summary>
        /// Obtiene una copia de todos los timestamps almacenados.
        /// </summary>
        public float[] GetTimestamps()
        {
            float[] timestamps = new float[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                timestamps[i] = points[i].timestamp;
            }
            return timestamps;
        }
    }
}
