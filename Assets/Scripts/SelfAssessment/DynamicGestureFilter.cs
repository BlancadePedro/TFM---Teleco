using UnityEngine;
using ASL.DynamicGestures;

namespace ASL.SelfAssessment
{
    /// <summary>
    /// Filtro simple para Scene 4: solo permite que pase UN gesto especifico
    /// NO modifica DynamicGestureRecognizer - solo escucha eventos y filtra
    /// </summary>
    public class DynamicGestureFilter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DynamicGestureRecognizer dynamicGestureRecognizer;

        /// <summary>
        /// Acceso publico al reconocedor para suscripcion a eventos adicionales (ej: OnGestureStarted)
        /// </summary>
        public DynamicGestureRecognizer DynamicGestureRecognizer => dynamicGestureRecognizer;

        [Header("Filtro")]
        [Tooltip("Name del gesto que se esta practicando (vacio = permite todos)")]
        [SerializeField] private string currentTargetGesture = "";

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Events filtrados (solo el gesto objetivo pasa)
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
        /// Cambia el gesto objetivo (el que estas practicando)
        /// </summary>
        public void SetTargetGesture(string gestureName)
        {
            currentTargetGesture = gestureName;

            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>[FILTER]</color> Gesture objetivo: '{gestureName}'");
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
                    Debug.Log($"<color=green>[FILTER]</color> ✓ Gesture '{gestureName}' completed (ALLOWED)");
                }

                OnFilteredGestureCompleted?.Invoke(gestureName);
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"<color=yellow>[FILTER]</color> ✗ Gesture '{gestureName}' completed (BLOQUEADO - esperando '{currentTargetGesture}')");
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
                    Debug.LogWarning($"<color=orange>[FILTER]</color> Gesture '{gestureName}' failed: {reason}");
                }

                OnFilteredGestureFailed?.Invoke(gestureName, reason);
            }
        }

        private bool IsGestureAllowed(string gestureName)
        {
            // Si no hay filtro active, permite todo
            if (string.IsNullOrEmpty(currentTargetGesture))
                return true;

            // Solo permite el gesto objetivo
            return gestureName.Equals(currentTargetGesture, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
