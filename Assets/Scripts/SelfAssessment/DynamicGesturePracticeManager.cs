using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL.DynamicGestures;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.SelfAssessment
{
    /// <summary>
    /// CARACTERISTICAS:
    /// - Auto-evaluacion por gesto especifico
    /// - Feedback visual en tiles del grid
    /// - Tracking de progreso con barra
    /// - Logs detallados de fallos para guiar al usuario
    /// - Compatible con gestos statics y dynamics simultaneos
    /// </summary>
    public class DynamicGesturePracticeManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Filtro de dynamic gestures (filtra solo el gesto que se practica)")]
        [SerializeField] private ASL.SelfAssessment.DynamicGestureFilter gestureFilter;

        [Tooltip("SelfAssessmentController para acceder al grid de tiles")]
        [SerializeField] private SelfAssessmentController selfAssessmentController;

        [Header("Gesture Definitions")]
        [Tooltip("List de dynamic gestures a practicar (ej: Hello, Bye, J, Z)")]
        [SerializeField] private List<DynamicGestureDefinition> practiceGestures = new List<DynamicGestureDefinition>();

        [Header("UI Feedback (Optional)")]
        [Tooltip("Feedback panel en pantalla (opcional)")]
        [SerializeField] private GameObject feedbackPanel;

        [Tooltip("Text de feedback del gesto actual")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Tooltip("Barra de progreso del gesto en curso")]
        [SerializeField] private Slider progressBar;

        [Tooltip("Text de razon de fallo")]
        [SerializeField] private TextMeshProUGUI failureReasonText;

        [Header("Configuration")]
        [Tooltip("Time para auto-ocultar el feedback de exito (segundos)")]
        [Range(1f, 5f)]
        [SerializeField] private float successFeedbackDuration = 2f;

        [Tooltip("Time para auto-ocultar el feedback de fallo (segundos)")]
        [Range(2f, 10f)]
        [SerializeField] private float failureFeedbackDuration = 4f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Events publicos para comunicacion con SelfAssessmentController
        /// <summary>
        /// Event publico para notificar cuando un dynamic gesture se completa.
        /// El SelfAssessmentController se suscribe a este evento.
        /// </summary>
        public System.Action<SignData> OnDynamicGestureCompletedSignal;

        /// <summary>
        /// Event para feedback visual en tiempo real (reconocimiento instantaneo).
        /// Permite iluminar tiles mientras el gesto esta en progreso.
        /// </summary>
        public System.Action<string> OnDynamicGestureRecognizedSignal;

        // State interno
        private Dictionary<string, SignData> gestureNameToSignData = new Dictionary<string, SignData>();
        private Dictionary<string, SignTileController> gestureTiles = new Dictionary<string, SignTileController>();
        private HashSet<string> completedDynamicGestures = new HashSet<string>();
        private string currentPracticingGesture = null;
        private float feedbackHideTime = 0f;

        void Start()
        {
            // Auto-buscar DynamicGestureFilter si no esta assigned
            if (gestureFilter == null)
            {
                gestureFilter = FindObjectOfType<ASL.SelfAssessment.DynamicGestureFilter>();
            }

            if (gestureFilter == null)
            {
                Debug.LogError("[DynamicGesturePracticeManager] No found DynamicGestureFilter en la escena!");
                enabled = false;
                return;
            }

            // Auto-buscar SelfAssessmentController si no esta assigned
            if (selfAssessmentController == null)
            {
                selfAssessmentController = FindObjectOfType<SelfAssessmentController>();
            }

            // Suscribirse a eventos FILTRADOS
            gestureFilter.OnFilteredGestureCompleted += OnDynamicGestureCompleted;
            gestureFilter.OnFilteredGestureProgress += OnDynamicGestureProgress;
            gestureFilter.OnFilteredGestureFailed += OnDynamicGestureFailed;

            // MODO AUTOEVALUACION: Permitir TODOS los dynamic gestures (sin filtro)
            gestureFilter.ClearFilter();

            // Suscribirse al evento de inicio de gesto para feedback visual
            if (gestureFilter.DynamicGestureRecognizer != null)
            {
                gestureFilter.DynamicGestureRecognizer.OnGestureStarted += OnDynamicGestureStarted;
            }

            // Mapear dynamic gestures a SignData AUTOMATICAMENTE desde la category
            MapDynamicGesturesToSignData();

            if (showDebugLogs)
            {
                Debug.Log($"[DynamicGesturePracticeManager] {gestureNameToSignData.Count} dynamic gestures mapeados (modo autoevaluacion: todos actives)");
            }

            // Hide feedback inicial
            HideFeedback();
        }

        void Update()
        {
            // Auto-ocultar feedback despues de tiempo configured
            if (feedbackPanel != null && feedbackPanel.activeSelf && Time.time >= feedbackHideTime)
            {
                HideFeedback();
            }
        }

        /// <summary>
        /// Mapea AUTOMATICAMENTE dynamic gestures a SignData.
        /// Recorre los signos de la category actual que tienen requiresMovement = true
        /// y los cruza con los gestos loadeds en DynamicGestureRecognizer.
        /// Ya NO depende de la lista manual practiceGestures del Inspector.
        /// </summary>
        private void MapDynamicGesturesToSignData()
        {
            gestureNameToSignData.Clear();

            // Acceder a la category actual del GameManager
            var currentCategory = ASL_LearnVR.Core.GameManager.Instance?.CurrentCategory;
            if (currentCategory == null)
            {
                Debug.LogError("[DynamicGesturePracticeManager] No hay category actual en GameManager");
                return;
            }

            // Obtener los gestos loadeds en el DynamicGestureRecognizer
            HashSet<string> loadedGestureNames = new HashSet<string>();
            if (gestureFilter?.DynamicGestureRecognizer != null)
            {
                var definitions = gestureFilter.DynamicGestureRecognizer.GetGestureDefinitions();
                foreach (var def in definitions)
                {
                    if (def != null)
                    {
                        loadedGestureNames.Add(def.gestureName);
                    }
                }
            }

            // Recorrer TODOS los signos de la category que requieren movimiento
            foreach (var sign in currentCategory.signs)
            {
                if (sign == null || !sign.requiresMovement)
                    continue;

                string signName = sign.signName;

                // Verificar que el dynamic gesture exista en el DynamicGestureRecognizer
                if (loadedGestureNames.Contains(signName))
                {
                    gestureNameToSignData[signName] = sign;

                    if (showDebugLogs)
                    {
                        Debug.Log($"[DynamicGesturePracticeManager] Dynamic gesture '{signName}' mapeado automaticamente");
                    }
                }
                else
                {
                    Debug.LogWarning($"[DynamicGesturePracticeManager] Sign '{signName}' requiere movimiento pero NO tiene DynamicGestureDefinition loaded en el reconocedor");
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"[DynamicGesturePracticeManager] Total mapeados: {gestureNameToSignData.Count} dynamic gestures de category '{currentCategory.categoryName}'");
            }
        }

        /// <summary>
        /// Activa modo auto-evaluacion para un gesto especifico
        /// </summary>
        public void StartPracticingGesture(string gestureName)
        {
            if (gestureFilter == null)
                return;

            currentPracticingGesture = gestureName;
            gestureFilter.SetTargetGesture(gestureName);

            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>[PRACTICE]</color> Starting practice for '{gestureName}'");
            }

            ShowFeedback($"Practica: {gestureName}", Color.white, showProgress: true);
        }

        /// <summary>
        /// Desactiva modo auto-evaluacion
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
                Debug.Log($"<color=cyan>[PRACTICE]</color> Practice mode disabled");
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

            // Emit recognition event visual para iluminar tile
            OnDynamicGestureRecognizedSignal?.Invoke(gestureName);
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

            // Marcar como completed
            if (!completedDynamicGestures.Contains(gestureName))
            {
                completedDynamicGestures.Add(gestureName);
            }

            // Feedback visual
            ShowFeedback($"Perfect! {gestureName}", Color.green, showProgress: false);
            feedbackHideTime = Time.time + successFeedbackDuration;

            // Disparar evento de deteccion confirmada en SelfAssessmentController
            if (gestureNameToSignData.TryGetValue(gestureName, out SignData signData))
            {
                // NOTA: Esto requiere acceso al MultiGestureRecognizer del SelfAssessmentController
                // Alternativamente, usar UnityEvent o sistema de mensajes
                BroadcastGestureCompleted(signData);
            }

            // TODO: Play success sound, animacion, etc.
        }

        private void OnDynamicGestureFailed(string gestureName, string reason)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"<color=red>[FALLADO]</color> {gestureName} - {reason}");
            }

            // Feedback detallado del fallo
            string userFriendlyReason = TranslateFailureReason(reason);
            ShowFeedback($"KO {gestureName}\n{userFriendlyReason}", Color.red, showProgress: false);
            feedbackHideTime = Time.time + failureFeedbackDuration;

            // Mostrar razon especifica
            if (failureReasonText != null)
            {
                failureReasonText.text = userFriendlyReason;
                failureReasonText.gameObject.SetActive(true);
            }

            // TODO: Vibracion haptica, sonido de error
        }

        /// <summary>
        /// Traduce razones tecnicas de fallo a mensajes amigables
        /// </summary>
        private string TranslateFailureReason(string technicalReason)
        {
            if (technicalReason.Contains("Wrong direction"))
                return "Mueve tu mano en la direccion correcta";

            if (technicalReason.Contains("Speed muy baja"))
                return "Mueve tu mano faster";

            if (technicalReason.Contains("Pose 'During' perdida"))
                return "Manten la forma de la mano durante el movimiento";

            if (technicalReason.Contains("Timeout"))
                return "Too slow, try faster";

            if (technicalReason.Contains("Tracking perdido"))
                return "Manten tu mano visible para la camara";

            if (technicalReason.Contains("zona espacial"))
                return "Realiza el gesto en la posicion correcta";

            if (technicalReason.Contains("cambios de direccion"))
                return "Necesitas cambiar de direccion mas veces";

            if (technicalReason.Contains("rotacion"))
                return "Rota tu mano mas durante el movimiento";

            return technicalReason; // Fallback
        }

        /// <summary>
        /// Notifica al SelfAssessmentController que un dynamic gesture se completo
        /// </summary>
        private void BroadcastGestureCompleted(SignData signData)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[DynamicGesturePracticeManager] Enviando senal de gesto completed: '{signData.signName}'");
            }

            // Invocar el evento propio para que SelfAssessmentController pueda escucharlo
            OnDynamicGestureCompletedSignal?.Invoke(signData);
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

                // Limpiar suscripcion al evento de inicio
                if (gestureFilter.DynamicGestureRecognizer != null)
                {
                    gestureFilter.DynamicGestureRecognizer.OnGestureStarted -= OnDynamicGestureStarted;
                }
            }
        }

        // ============================================
        // API PUBLICA
        // ============================================

        /// <summary>
        /// Verifica si un dynamic gesture especifico ha sido completed
        /// </summary>
        public bool IsGestureCompleted(string gestureName)
        {
            return completedDynamicGestures.Contains(gestureName);
        }

        /// <summary>
        /// Obtiene el numero de dynamic gestures completeds
        /// </summary>
        public int GetCompletedCount()
        {
            return completedDynamicGestures.Count;
        }

        /// <summary>
        /// Obtiene el numero total de dynamic gestures a practicar (auto-calculado desde la category)
        /// </summary>
        public int GetTotalCount()
        {
            return gestureNameToSignData.Count;
        }

        /// <summary>
        /// Resetea el progreso de gestos completeds
        /// </summary>
        public void ResetProgress()
        {
            completedDynamicGestures.Clear();
            HideFeedback();

            if (showDebugLogs)
            {
                Debug.Log("[DynamicGesturePracticeManager] Progress reseteado");
            }
        }
    }
}
