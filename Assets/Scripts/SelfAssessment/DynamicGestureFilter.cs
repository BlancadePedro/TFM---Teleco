using UnityEngine;
using ASL.DynamicGestures;

namespace ASL.SelfAssessment
{
    /// <summary>
    /// Filtro simple para Scene 4: solo permite que pase UN gesto específico
    /// NO modifica DynamicGestureRecognizer - solo escucha eventos y filtra
    /// </summary>
    public class DynamicGestureFilter : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private DynamicGestureRecognizer dynamicGestureRecognizer;

        /// <summary>
        /// Acceso público al reconocedor para suscripción a eventos adicionales (ej: OnGestureStarted)
        /// </summary>
        public DynamicGestureRecognizer DynamicGestureRecognizer => dynamicGestureRecognizer;

        [Header("Filtro")]
        [Tooltip("Nombre del gesto que se está practicando (vacío = permite todos)")]
        [SerializeField] private string currentTargetGesture = "";

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Eventos filtrados (solo el gesto objetivo pasa)
        public System.Action<string> OnFilteredGestureCompleted;
        public System.Action<string, float> OnFilteredGestureProgress;
        public System.Action<string, string> OnFilteredGestureFailed;

        void OnEnable()
        {
            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.OnGestureCompleted += HandleGestureCompleted;
                dynamicGestureRecognizer.OnGestureProgress += HandleGestureProgress;
                dynamicGestureRecognizer.OnGestureFailed += HandleGestureFailed;
            }
        }

        void OnDisable()
        {
            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.OnGestureCompleted -= HandleGestureCompleted;
                dynamicGestureRecognizer.OnGestureProgress -= HandleGestureProgress;
                dynamicGestureRecognizer.OnGestureFailed -= HandleGestureFailed;
            }
        }

        /// <summary>
        /// Cambia el gesto objetivo (el que estás practicando)
        /// </summary>
        public void SetTargetGesture(string gestureName)
        {
            currentTargetGesture = gestureName;

            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>[FILTER]</color> Gesto objetivo: '{gestureName}'");
            }
        }

        /// <summary>
        /// Desactiva el filtro (permite todos los gestos)
        /// </summary>
        public void ClearFilter()
        {
            currentTargetGesture = "";

            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>[FILTER]</color> Filtro desactivado");
            }
        }

        private void HandleGestureCompleted(string gestureName)
        {
            if (IsGestureAllowed(gestureName))
            {
                if (showDebugLogs)
                {
                    Debug.Log($"<color=green>[FILTER]</color> ✓ Gesto '{gestureName}' completado (PERMITIDO)");
                }

                OnFilteredGestureCompleted?.Invoke(gestureName);
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"<color=yellow>[FILTER]</color> ✗ Gesto '{gestureName}' completado (BLOQUEADO - esperando '{currentTargetGesture}')");
                }
            }
        }

        private void HandleGestureProgress(string gestureName, float progress)
        {
            if (IsGestureAllowed(gestureName))
            {
                OnFilteredGestureProgress?.Invoke(gestureName, progress);
            }
        }

        private void HandleGestureFailed(string gestureName, string reason)
        {
            if (IsGestureAllowed(gestureName))
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"<color=orange>[FILTER]</color> Gesto '{gestureName}' falló: {reason}");
                }

                OnFilteredGestureFailed?.Invoke(gestureName, reason);
            }
        }

        private bool IsGestureAllowed(string gestureName)
        {
            // Si no hay filtro activo, permite todo
            if (string.IsNullOrEmpty(currentTargetGesture))
                return true;

            // Solo permite el gesto objetivo
            return gestureName.Equals(currentTargetGesture, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
