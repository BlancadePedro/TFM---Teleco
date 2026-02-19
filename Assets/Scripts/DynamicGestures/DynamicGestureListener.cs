using UnityEngine;
using UnityEngine.UI;

namespace ASL.DynamicGestures
{
    /// <summary>
    /// Script de ejemplo que escucha eventos del DynamicGestureRecognizer.
    /// Muestra en consola y opcionalmente en UI el estado de los gestos detecteds.
    /// </summary>
    public class DynamicGestureListener : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("DynamicGestureRecognizer del que escuchar eventos")]
        [SerializeField] private DynamicGestureRecognizer gestureRecognizer;

        [Tooltip("SingleGestureAdapter para detectar initial pose (opcional)")]
        [SerializeField] private SingleGestureAdapter poseAdapter;

        [Header("UI Feedback (Optional)")]
        [Tooltip("Text UI para mostrar nombre del gesto actual")]
        [SerializeField] private Text gestureNameText;

        [Tooltip("Slider UI para mostrar progreso del gesto")]
        [SerializeField] private Slider gestureProgressSlider;

        [Tooltip("Text UI para mostrar mensajes de estado")]
        [SerializeField] private Text statusText;

        [Header("Debug")]
        [SerializeField] private bool showConsoleLogs = true;

        [Tooltip("Size de fuente para UI debug en VR (recomendado: 24-36)")]
        [SerializeField] private int debugFontSize = 32;

        void OnEnable()
        {
            if (gestureRecognizer != null)
            {
                gestureRecognizer.OnGestureStarted += HandleGestureStarted;
                gestureRecognizer.OnGestureProgress += HandleGestureProgress;
                gestureRecognizer.OnGestureCompleted += HandleGestureCompleted;
                gestureRecognizer.OnGestureFailed += HandleGestureFailed;
            }
            else
            {
                Debug.LogError("[DynamicGestureListener] Falta asignar DynamicGestureRecognizer en el Inspector!");
            }

            // Auto-buscar SingleGestureAdapter si no esta assigned
            if (poseAdapter == null)
            {
                poseAdapter = FindObjectOfType<SingleGestureAdapter>();
            }

            // Configure UI para VR si existe
            ConfigureUIForVR();
        }

        /// <summary>
        /// Configura el tamano de fuente para que sea visible en VR
        /// </summary>
        private void ConfigureUIForVR()
        {
            if (gestureNameText != null)
                gestureNameText.fontSize = debugFontSize;

            if (statusText != null)
                statusText.fontSize = debugFontSize;
        }

        void OnDisable()
        {
            if (gestureRecognizer != null)
            {
                gestureRecognizer.OnGestureStarted -= HandleGestureStarted;
                gestureRecognizer.OnGestureProgress -= HandleGestureProgress;
                gestureRecognizer.OnGestureCompleted -= HandleGestureCompleted;
                gestureRecognizer.OnGestureFailed -= HandleGestureFailed;
            }
        }

        private string lastDetectedPose = null;

        void Update()
        {
            // Monitorear pose detectada para mostrar feedback
            if (poseAdapter != null && statusText != null)
            {
                string currentPose = poseAdapter.GetCurrentPoseName();

                if (!string.IsNullOrEmpty(currentPose) && currentPose != lastDetectedPose)
                {
                    // Nueva pose detectada
                    statusText.text = $"Pose '{currentPose}' detectada\nMueve para iniciar gesto...";
                    statusText.color = Color.cyan;
                    lastDetectedPose = currentPose;

                    if (showConsoleLogs)
                    {
                        Debug.Log($"<color=cyan>[POSE DETECTADA]</color> '{currentPose}' - Esperando movimiento...");
                    }
                }
                else if (string.IsNullOrEmpty(currentPose) && lastDetectedPose != null)
                {
                    // Pose perdida
                    statusText.text = "";
                    lastDetectedPose = null;
                }
            }
        }

        /// <summary>
        /// Callback cuando un gesto es started
        /// </summary>
        private void HandleGestureStarted(string gestureName)
        {
            if (showConsoleLogs)
            {
                Debug.Log($"<color=cyan>[GESTO INICIADO]</color> {gestureName}");
            }

            // Actualizar UI
            if (gestureNameText != null)
            {
                gestureNameText.text = $"Detectando: {gestureName}";
                gestureNameText.color = Color.yellow;
            }

            if (gestureProgressSlider != null)
            {
                gestureProgressSlider.value = 0f;
                gestureProgressSlider.gameObject.SetActive(true);
            }

            if (statusText != null)
            {
                statusText.text = $"GESTURE {gestureName} STARTED!\nComplete the movement...";
                statusText.color = Color.yellow;
            }

            // Aqui puedes anadir tu logica personalizada:
            // - Reproducir audio de feedback
            // - Mostrar animacion de inicio
            // - Enviar analitica
        }

        /// <summary>
        /// Callback para actualizar progreso del gesto (0.0 a 1.0)
        /// </summary>
        private void HandleGestureProgress(string gestureName, float progress)
        {
            if (showConsoleLogs && Time.frameCount % 30 == 0) // Log cada 30 frames para no saturar
            {
                Debug.Log($"<color=yellow>[PROGRESO]</color> {gestureName}: {progress:P0}");
            }

            // Actualizar UI
            if (gestureProgressSlider != null)
            {
                gestureProgressSlider.value = progress;
            }

            // Aqui puedes anadir tu logica personalizada:
            // - Actualizar barra de progreso
            // - Cambiar color de feedback segun progreso
            // - Reproducir audio incremental
        }

        /// <summary>
        /// Callback cuando un gesto se completa exitosamente
        /// </summary>
        private void HandleGestureCompleted(string gestureName)
        {
            if (showConsoleLogs)
            {
                Debug.Log($"<color=green>[GESTURE COMPLETED]</color> {gestureName} ✓");
            }

            // Actualizar UI
            if (gestureNameText != null)
            {
                gestureNameText.text = $"{gestureName}!";
                gestureNameText.color = Color.green;
            }

            if (gestureProgressSlider != null)
            {
                gestureProgressSlider.value = 1f;
            }

            if (statusText != null)
            {
                statusText.text = $"PERFECT!\nGesture '{gestureName}' completed";
                statusText.color = Color.green;
            }

            // Limpiar UI despues de 3 segundos
            Invoke(nameof(ClearUI), 3f);

            // Aqui puedes anadir tu logica personalizada:
            // - Reproducir animacion de exito
            // - Reproducir audio de confirmacion
            // - Dar recompensa al usuario
            // - Avanzar a siguiente nivel
            // - Actualizar puntuacion
        }

        /// <summary>
        /// Callback cuando un gesto falla
        /// </summary>
        private void HandleGestureFailed(string gestureName, string reason)
        {
            if (showConsoleLogs)
            {
                Debug.LogWarning($"<color=red>[GESTO FALLADO]</color> {gestureName} - {reason}");
            }

            // Actualizar UI
            if (gestureNameText != null)
            {
                gestureNameText.text = $"{gestureName} (failed)";
                gestureNameText.color = Color.red;
            }

            if (statusText != null)
            {
                statusText.text = $"Error: {reason}";
                statusText.color = Color.red;
            }

            // Limpiar UI despues de 2 segundos
            Invoke(nameof(ClearUI), 2f);

            // Aqui puedes anadir tu logica personalizada:
            // - Reproducir audio de error
            // - Mostrar hint de como ejecutar correctamente
            // - Permitir reintento
            // - Analizar razon de fallo para sugerencias
        }

        /// <summary>
        /// Limpia los elementos de UI
        /// </summary>
        private void ClearUI()
        {
            if (gestureNameText != null)
            {
                gestureNameText.text = "";
            }

            if (gestureProgressSlider != null)
            {
                gestureProgressSlider.value = 0f;
                gestureProgressSlider.gameObject.SetActive(false);
            }

            if (statusText != null)
            {
                statusText.text = "";
            }
        }

        /// <summary>
        /// Method publico para testing desde Inspector/otros scripts
        /// </summary>
        public void TestGestureCompleted(string gestureName)
        {
            HandleGestureCompleted(gestureName);
        }
    }
}
