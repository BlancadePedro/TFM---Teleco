using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL.DynamicGestures;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.SelfAssessment
{
    /// <summary>
    ///  MANAGER DE PRÁCTICA DE GESTOS DINÁMICOS v1.0
    /// Integra DynamicGestureRecognizer_v3_FINAL en Scene 4 (Self-Assessment Mode).
    ///
    /// CARACTERÍSTICAS:
    /// - Auto-evaluación por gesto específico
    /// - Feedback visual en tiles del grid
    /// - Tracking de progreso con barra
    /// - Logs detallados de fallos para guiar al usuario
    /// - Compatible con gestos estáticos y dinámicos simultáneos
    /// </summary>
    public class DynamicGesturePracticeManager : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Filtro de gestos dinámicos (filtra solo el gesto que se practica)")]
        [SerializeField] private ASL.SelfAssessment.DynamicGestureFilter gestureFilter;

        [Tooltip("SelfAssessmentController para acceder al grid de tiles")]
        [SerializeField] private SelfAssessmentController selfAssessmentController;

        [Header("Gesture Definitions")]
        [Tooltip("Lista de gestos dinámicos a practicar (ej: Hello, Bye, J, Z)")]
        [SerializeField] private List<DynamicGestureDefinition> practiceGestures = new List<DynamicGestureDefinition>();

        [Header("UI Feedback (Opcional)")]
        [Tooltip("Panel de feedback en pantalla (opcional)")]
        [SerializeField] private GameObject feedbackPanel;

        [Tooltip("Texto de feedback del gesto actual")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Tooltip("Barra de progreso del gesto en curso")]
        [SerializeField] private Slider progressBar;

        [Tooltip("Texto de razón de fallo")]
        [SerializeField] private TextMeshProUGUI failureReasonText;

        [Header("Configuración")]
        [Tooltip("Tiempo para auto-ocultar el feedback de éxito (segundos)")]
        [Range(1f, 5f)]
        [SerializeField] private float successFeedbackDuration = 2f;

        [Tooltip("Tiempo para auto-ocultar el feedback de fallo (segundos)")]
        [Range(2f, 10f)]
        [SerializeField] private float failureFeedbackDuration = 4f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Estado interno
        private Dictionary<string, SignData> gestureNameToSignData = new Dictionary<string, SignData>();
        private Dictionary<string, SignTileController> gestureTiles = new Dictionary<string, SignTileController>();
        private HashSet<string> completedDynamicGestures = new HashSet<string>();
        private string currentPracticingGesture = null;
        private float feedbackHideTime = 0f;

        void Start()
        {
            if (gestureFilter == null)
            {
                Debug.LogError("[DynamicGesturePracticeManager] ❌ Falta asignar DynamicGestureFilter!");
                enabled = false;
                return;
            }

            // Suscribirse a eventos FILTRADOS (solo el gesto que se practica)
            gestureFilter.OnFilteredGestureCompleted += OnDynamicGestureCompleted;
            gestureFilter.OnFilteredGestureProgress += OnDynamicGestureProgress;
            gestureFilter.OnFilteredGestureFailed += OnDynamicGestureFailed;

            // Los gesture definitions ya deben estar en el DynamicGestureRecognizer
            // NO los modificamos aquí para no romper Scene 3
            if (practiceGestures.Count > 0)
            {
                // Solo registramos cuántos tenemos para logging
                int count = practiceGestures.Count;

                if (showDebugLogs)
                {
                    Debug.Log($"[DynamicGesturePracticeManager]  {practiceGestures.Count} gestos dinámicos cargados");
                }
            }

            // Mapear gestos dinámicos a SignData del SelfAssessmentController
            MapDynamicGesturesToSignData();

            // Ocultar feedback inicial
            HideFeedback();
        }

        void Update()
        {
            // Auto-ocultar feedback después de tiempo configurado
            if (feedbackPanel != null && feedbackPanel.activeSelf && Time.time >= feedbackHideTime)
            {
                HideFeedback();
            }
        }

        /// <summary>
        /// Mapea nombres de gestos dinámicos a SignData del grid
        /// </summary>
        private void MapDynamicGesturesToSignData()
        {
            if (selfAssessmentController == null)
            {
                Debug.LogWarning("[DynamicGesturePracticeManager] SelfAssessmentController no asignado, no se podrá iluminar tiles");
                return;
            }

            // Acceder a la categoría actual del GameManager
            var currentCategory = ASL_LearnVR.Core.GameManager.Instance?.CurrentCategory;
            if (currentCategory == null)
            {
                Debug.LogError("[DynamicGesturePracticeManager] No hay categoría actual en GameManager");
                return;
            }

            // Buscar SignData que correspondan a gestos dinámicos
            foreach (var gesture in practiceGestures)
            {
                string gestureName = gesture.gestureName;

                // Buscar en los signs de la categoría
                SignData matchingSign = currentCategory.GetSignByName(gestureName);

                if (matchingSign != null)
                {
                    gestureNameToSignData[gestureName] = matchingSign;

                    if (showDebugLogs)
                    {
                        Debug.Log($"[DynamicGesturePracticeManager]  Gesto '{gestureName}' mapeado a SignData");
                    }
                }
                else
                {
                    Debug.LogWarning($"[DynamicGesturePracticeManager] ⚠️ Gesto dinámico '{gestureName}' no encontrado en categoría '{currentCategory.categoryName}'");
                }
            }

            // TODO: Cachear tiles del grid (requiere acceso público en SelfAssessmentController)
            // Por ahora, usaremos eventos del SelfAssessmentController
        }

        /// <summary>
        /// Activa modo auto-evaluación para un gesto específico
        /// </summary>
        public void StartPracticingGesture(string gestureName)
        {
            if (gestureFilter == null)
                return;

            currentPracticingGesture = gestureName;
            gestureFilter.SetTargetGesture(gestureName);

            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>[PRÁCTICA]</color> Iniciando práctica de '{gestureName}'");
            }

            ShowFeedback($"Practica: {gestureName}", Color.white, showProgress: true);
        }

        /// <summary>
        /// Desactiva modo auto-evaluación
        /// </summary>
        public void StopPracticing()
        {
            if (gestureFilter == null)
                return;

            gestureFilter.ClearFilter();
            currentPracticingGesture = null;

            HideFeedback();

            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>[PRÁCTICA]</color> Modo práctica desactivado");
            }
        }

        /// <summary>
        /// Cambia a practicar otro gesto
        /// </summary>
        public void SwitchToGesture(string newGestureName)
        {
            StartPracticingGesture(newGestureName);
        }

        // ============================================
        // EVENTOS DEL RECONOCEDOR
        // ============================================

        private void OnDynamicGestureStarted(string gestureName)
        {
            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>[GESTO INICIADO]</color> {gestureName}");
            }

            ShowFeedback($"Reconociendo: {gestureName}", new Color(1f, 0.843f, 0f), showProgress: true); // Golden

            // Iluminar tile temporalmente (equivalente a onGestureRecognized)
            if (gestureNameToSignData.TryGetValue(gestureName, out SignData signData))
            {
                // El SelfAssessmentController ya maneja esto con MultiGestureRecognizer
                // No necesitamos duplicar lógica aquí
            }
        }

        private void OnDynamicGestureProgress(string gestureName, float progress)
        {
            // Actualizar barra de progreso
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (showDebugLogs && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[PROGRESO] {gestureName}: {progress:P0}");
            }
        }

        private void OnDynamicGestureCompleted(string gestureName)
        {
            if (showDebugLogs)
            {
                Debug.Log($"<color=green>[ COMPLETADO]</color> {gestureName}");
            }

            // Marcar como completado
            if (!completedDynamicGestures.Contains(gestureName))
            {
                completedDynamicGestures.Add(gestureName);
            }

            // Feedback visual
            ShowFeedback($"¡Perfecto! {gestureName}", Color.green, showProgress: false);
            feedbackHideTime = Time.time + successFeedbackDuration;

            // Disparar evento de detección confirmada en SelfAssessmentController
            if (gestureNameToSignData.TryGetValue(gestureName, out SignData signData))
            {
                // NOTA: Esto requiere acceso al MultiGestureRecognizer del SelfAssessmentController
                // Alternativamente, usar UnityEvent o sistema de mensajes
                BroadcastGestureCompleted(signData);
            }

            // TODO: Reproducir sonido de éxito, animación, etc.
        }

        private void OnDynamicGestureFailed(string gestureName, string reason)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"<color=red>[❌ FALLADO]</color> {gestureName} - {reason}");
            }

            // Feedback detallado del fallo
            string userFriendlyReason = TranslateFailureReason(reason);
            ShowFeedback($"❌ {gestureName}\n{userFriendlyReason}", Color.red, showProgress: false);
            feedbackHideTime = Time.time + failureFeedbackDuration;

            // Mostrar razón específica
            if (failureReasonText != null)
            {
                failureReasonText.text = userFriendlyReason;
                failureReasonText.gameObject.SetActive(true);
            }

            // TODO: Vibración háptica, sonido de error
        }

        /// <summary>
        /// Traduce razones técnicas de fallo a mensajes amigables
        /// </summary>
        private string TranslateFailureReason(string technicalReason)
        {
            if (technicalReason.Contains("Dirección incorrecta"))
                return "Mueve tu mano en la dirección correcta";

            if (technicalReason.Contains("Velocidad muy baja"))
                return "Mueve tu mano más rápido";

            if (technicalReason.Contains("Pose 'During' perdida"))
                return "Mantén la forma de la mano durante el movimiento";

            if (technicalReason.Contains("Timeout"))
                return "Demasiado lento, intenta más rápido";

            if (technicalReason.Contains("Tracking perdido"))
                return "Mantén tu mano visible para la cámara";

            if (technicalReason.Contains("zona espacial"))
                return "Realiza el gesto en la posición correcta";

            if (technicalReason.Contains("cambios de dirección"))
                return "Necesitas cambiar de dirección más veces";

            if (technicalReason.Contains("rotación"))
                return "Rota tu mano más durante el movimiento";

            return technicalReason; // Fallback
        }

        /// <summary>
        /// Notifica al SelfAssessmentController que un gesto dinámico se completó
        /// </summary>
        private void BroadcastGestureCompleted(SignData signData)
        {
            // Estrategia 1: Buscar MultiGestureRecognizer y simular detección
            var multiGestureRecognizer = FindObjectOfType<ASL_LearnVR.Gestures.MultiGestureRecognizer>();
            if (multiGestureRecognizer != null)
            {
                // Invocar el evento onGestureDetected manualmente
                multiGestureRecognizer.onGestureDetected?.Invoke(signData);

                if (showDebugLogs)
                {
                    Debug.Log($"[DynamicGesturePracticeManager]  Evento onGestureDetected invocado para '{signData.signName}'");
                }
            }
            else
            {
                Debug.LogWarning("[DynamicGesturePracticeManager] No se encontró MultiGestureRecognizer para notificar completitud");
            }
        }

        /// <summary>
        /// Muestra feedback en pantalla
        /// </summary>
        private void ShowFeedback(string message, Color color, bool showProgress)
        {
            if (feedbackPanel != null)
            {
                feedbackPanel.SetActive(true);
            }

            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
            }

            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(showProgress);
                if (showProgress)
                {
                    progressBar.value = 0f;
                }
            }

            if (failureReasonText != null && color != Color.red)
            {
                failureReasonText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Oculta feedback en pantalla
        /// </summary>
        private void HideFeedback()
        {
            if (feedbackPanel != null)
            {
                feedbackPanel.SetActive(false);
            }

            if (failureReasonText != null)
            {
                failureReasonText.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            // Limpiar suscripciones del filtro
            if (gestureFilter != null)
            {
                gestureFilter.OnFilteredGestureCompleted -= OnDynamicGestureCompleted;
                gestureFilter.OnFilteredGestureProgress -= OnDynamicGestureProgress;
                gestureFilter.OnFilteredGestureFailed -= OnDynamicGestureFailed;
            }
        }

        // ============================================
        // API PÚBLICA
        // ============================================

        /// <summary>
        /// Verifica si un gesto dinámico específico ha sido completado
        /// </summary>
        public bool IsGestureCompleted(string gestureName)
        {
            return completedDynamicGestures.Contains(gestureName);
        }

        /// <summary>
        /// Obtiene el número de gestos dinámicos completados
        /// </summary>
        public int GetCompletedCount()
        {
            return completedDynamicGestures.Count;
        }

        /// <summary>
        /// Obtiene el número total de gestos dinámicos a practicar
        /// </summary>
        public int GetTotalCount()
        {
            return practiceGestures.Count;
        }

        /// <summary>
        /// Resetea el progreso de gestos completados
        /// </summary>
        public void ResetProgress()
        {
            completedDynamicGestures.Clear();
            HideFeedback();

            if (showDebugLogs)
            {
                Debug.Log("[DynamicGesturePracticeManager] Progreso reseteado");
            }
        }
    }
}
