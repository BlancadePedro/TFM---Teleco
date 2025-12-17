using UnityEngine;
using ASL_LearnVR.Gestures;

namespace ASL_LearnVR.Examples
{
    /// <summary>
    /// Ejemplo de uso del sistema de reconocimiento de gestos dinámicos.
    /// Muestra cómo configurar y responder a eventos del DynamicGestureRecognizerV2.
    /// </summary>
    public class DynamicGestureExample : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("El reconocedor de gestos dinámicos")]
        [SerializeField] private DynamicGestureRecognizerV2 dynamicGestureRecognizer;

        [Header("Feedback")]
        [Tooltip("Texto UI para mostrar el estado (opcional)")]
        [SerializeField] private UnityEngine.UI.Text feedbackText;

        void OnEnable()
        {
            if (dynamicGestureRecognizer != null)
            {
                // Suscribirse a los eventos
                dynamicGestureRecognizer.onGestureStarted.AddListener(OnGestureStarted);
                dynamicGestureRecognizer.onGestureProgress.AddListener(OnGestureProgress);
                dynamicGestureRecognizer.onGestureCompleted.AddListener(OnGestureCompleted);
                dynamicGestureRecognizer.onGestureFailed.AddListener(OnGestureFailed);
            }
        }

        void OnDisable()
        {
            if (dynamicGestureRecognizer != null)
            {
                // Desuscribirse de los eventos
                dynamicGestureRecognizer.onGestureStarted.RemoveListener(OnGestureStarted);
                dynamicGestureRecognizer.onGestureProgress.RemoveListener(OnGestureProgress);
                dynamicGestureRecognizer.onGestureCompleted.RemoveListener(OnGestureCompleted);
                dynamicGestureRecognizer.onGestureFailed.RemoveListener(OnGestureFailed);
            }
        }

        /// <summary>
        /// Llamado cuando el gesto comienza.
        /// </summary>
        private void OnGestureStarted(DynamicGestureDefinition gesture)
        {
            Debug.Log($"[Example] Gesto iniciado: {gesture.gestureName}");
            UpdateFeedback($"Realizando: {gesture.gestureName}...");
        }

        /// <summary>
        /// Llamado durante el progreso del gesto.
        /// </summary>
        private void OnGestureProgress(DynamicGestureDefinition gesture, float progress)
        {
            // Puedes usar el progreso para feedback visual (barra de progreso, etc.)
            UpdateFeedback($"{gesture.gestureName}: {Mathf.RoundToInt(progress * 100)}%");
        }

        /// <summary>
        /// Llamado cuando el gesto se completa exitosamente.
        /// </summary>
        private void OnGestureCompleted(DynamicGestureDefinition gesture)
        {
            Debug.Log($"[Example] ¡Gesto completado exitosamente: {gesture.gestureName}!");
            UpdateFeedback($"¡Correcto! {gesture.gestureName}", Color.green);

            // Aquí puedes ejecutar acciones específicas del gesto
            // Por ejemplo: avanzar a la siguiente letra, reproducir audio, etc.
        }

        /// <summary>
        /// Llamado cuando el gesto falla.
        /// </summary>
        private void OnGestureFailed(DynamicGestureDefinition gesture, string reason)
        {
            Debug.LogWarning($"[Example] Gesto fallido: {gesture.gestureName} - Razón: {reason}");
            UpdateFeedback($"Inténtalo de nuevo", Color.red);

            // Aquí puedes dar feedback al usuario sobre qué salió mal
        }

        /// <summary>
        /// Actualiza el texto de feedback.
        /// </summary>
        private void UpdateFeedback(string message, Color? color = null)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                if (color.HasValue)
                {
                    feedbackText.color = color.Value;
                }
            }
        }

        /// <summary>
        /// Método público para iniciar el reconocimiento manualmente.
        /// </summary>
        public void StartRecognition()
        {
            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.StartGestureRecognition();
            }
        }

        /// <summary>
        /// Método público para cancelar el reconocimiento.
        /// </summary>
        public void CancelRecognition()
        {
            if (dynamicGestureRecognizer != null)
            {
                dynamicGestureRecognizer.CancelGesture();
            }
        }
    }
}
