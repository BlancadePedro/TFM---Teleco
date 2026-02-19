using UnityEngine;
using ASL.DynamicGestures;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Analiza el estado de un dynamic gesture en progreso y genera feedback por fases.
    ///
    /// A diferencia del feedback static (que corrige dedos individuales),
    /// el feedback dynamic se centra en propiedades globales del movimiento:
    /// - Direccion
    /// - Speed
    /// - Amplitud
    /// - Continuidad
    ///
    /// El feedback se organiza en 6 fases:
    /// 0. Idle: Esperando initial pose
    /// 1. StartDetected: Pose inicial correcta, listo para moverse
    /// 2. InProgress: Movimiento en curso, guia sobre el movimiento
    /// 3. NearCompletion: Casi completed (>80%)
    /// 4. Completed: Gesture correct
    /// 5. Failed: Gesture failed con explicacion
    /// </summary>
    public class DynamicGestureFeedbackAnalyzer
    {
        // Configuration de umbrales
        private const float NEAR_COMPLETION_THRESHOLD = 0.8f; // 80% del gesto
        private const float SPEED_TOO_SLOW_FACTOR = 0.5f;     // 50% de la velocidad minima
        private const float SPEED_TOO_FAST_FACTOR = 2.5f;     // 250% de la velocidad maxima estimada
        private const float DISTANCE_SHORT_FACTOR = 0.5f;     // 50% de la distancia minima
        private const float CIRCULARITY_LOW_FACTOR = 0.7f;    // 70% del score minimo

        // State actual
        private DynamicFeedbackPhase currentPhase = DynamicFeedbackPhase.Idle;
        private DynamicMovementIssue currentIssue = DynamicMovementIssue.None;
        private string currentMessage = "";
        private float phaseStartTime = 0f;
        private string activeGestureName = "";

        // Cache del ultimo resultado para evitar spam de mensajes
        private DynamicMovementIssue lastReportedIssue = DynamicMovementIssue.None;
        private float lastMessageTime = 0f;
        private const float MESSAGE_COOLDOWN = 0.5f; // 500ms entre mensajes de la misma fase

        /// <summary>
        /// Fase actual del feedback.
        /// </summary>
        public DynamicFeedbackPhase CurrentPhase => currentPhase;

        /// <summary>
        /// Problema actual detected durante el movimiento.
        /// </summary>
        public DynamicMovementIssue CurrentIssue => currentIssue;

        /// <summary>
        /// Mensaje de feedback actual.
        /// </summary>
        public string CurrentMessage => currentMessage;

        /// <summary>
        /// Name del gesto active.
        /// </summary>
        public string ActiveGestureName => activeGestureName;

        /// <summary>
        /// Event cuando cambia la fase del feedback.
        /// </summary>
        public System.Action<DynamicFeedbackPhase, string> OnPhaseChanged;

        /// <summary>
        /// Event cuando hay un nuevo mensaje de feedback.
        /// </summary>
        public System.Action<string> OnFeedbackMessage;

        /// <summary>
        /// Resetea el analizador para un nuevo intento.
        /// </summary>
        public void Reset()
        {
            SetPhase(DynamicFeedbackPhase.Idle, "");
            currentIssue = DynamicMovementIssue.None;
            lastReportedIssue = DynamicMovementIssue.None;
            lastMessageTime = 0f;
            activeGestureName = "";
        }

        /// <summary>
        /// Notifica que el sistema esta esperando la initial pose.
        /// </summary>
        public void NotifyIdle(string gestureName)
        {
            activeGestureName = gestureName;
            string message = FeedbackMessages.GetIdlePhaseMessage(gestureName);
            SetPhase(DynamicFeedbackPhase.Idle, message);
        }

        /// <summary>
        /// Notifica que la initial pose fue detectada correctamente.
        /// </summary>
        public void NotifyStartDetected(string gestureName)
        {
            activeGestureName = gestureName;
            string message = FeedbackMessages.GetStartDetectedMessage(gestureName);
            SetPhase(DynamicFeedbackPhase.StartDetected, message);
        }

        /// <summary>
        /// Analiza el progreso del gesto y genera feedback apropiado.
        /// </summary>
        /// <param name="gestureName">Name del gesto</param>
        /// <param name="progress">Progress del gesto (0-1)</param>
        /// <param name="metrics">Metricas actuales del movimiento</param>
        /// <param name="gestureDefinition">Definicion del gesto para comparar requisitos</param>
        public void AnalyzeProgress(
            string gestureName,
            float progress,
            DynamicMetrics metrics,
            DynamicGestureDefinition gestureDefinition)
        {
            activeGestureName = gestureName;

            // Determinar fase basada en progreso
            DynamicFeedbackPhase targetPhase;
            if (progress >= NEAR_COMPLETION_THRESHOLD)
            {
                targetPhase = DynamicFeedbackPhase.NearCompletion;
            }
            else
            {
                targetPhase = DynamicFeedbackPhase.InProgress;
            }

            // Detectar problemas en el movimiento
            DynamicMovementIssue issue = DynamicMovementIssue.None;

            if (gestureDefinition != null)
            {
                issue = FeedbackMessages.DetectMovementIssue(
                    metrics,
                    expectedMinSpeed: gestureDefinition.minSpeed,
                    expectedMaxSpeed: gestureDefinition.minSpeed * 3f, // Estimacion
                    expectedMinDistance: gestureDefinition.minDistance,
                    requiresCircular: gestureDefinition.requiresCircularMotion,
                    minCircularityScore: gestureDefinition.minCircularityScore,
                    requiresDirectionChanges: gestureDefinition.requiresDirectionChange,
                    requiredChanges: gestureDefinition.requiredDirectionChanges
                );
            }

            currentIssue = issue;

            // Get expected direction from gesture definition for specific feedback
            Vector3 expectedDirection = gestureDefinition != null ? gestureDefinition.primaryDirection : Vector3.zero;

            // Generar mensaje basado en fase y problema
            string message;
            if (targetPhase == DynamicFeedbackPhase.NearCompletion)
            {
                message = FeedbackMessages.GetNearCompletionMessage();
            }
            else
            {
                message = FeedbackMessages.GetInProgressMessage(issue, metrics, gestureName, expectedDirection);
            }

            // Evitar spam de mensajes repetidos
            bool shouldUpdateMessage = ShouldUpdateMessage(issue, targetPhase);

            if (shouldUpdateMessage)
            {
                SetPhase(targetPhase, message);
                lastReportedIssue = issue;
                lastMessageTime = Time.time;
            }
            else if (currentPhase != targetPhase)
            {
                // Cambio de fase sin spam
                SetPhase(targetPhase, message);
            }
        }

        /// <summary>
        /// Notifica que el gesto fue completed exitosamente.
        /// </summary>
        public void NotifyCompleted(string gestureName, DynamicMetrics metrics)
        {
            activeGestureName = gestureName;
            currentIssue = DynamicMovementIssue.None;
            string message = FeedbackMessages.GetCompletedMessage(gestureName);
            SetPhase(DynamicFeedbackPhase.Completed, message);
        }

        /// <summary>
        /// Notifica que el gesto fallo.
        /// </summary>
        public void NotifyFailed(string gestureName, FailureReason reason, GesturePhase failedPhase, DynamicMetrics metrics)
        {
            activeGestureName = gestureName;
            currentIssue = MapFailureReasonToIssue(reason);
            string message = FeedbackMessages.GetFailedMessage(reason, failedPhase, metrics, gestureName);
            SetPhase(DynamicFeedbackPhase.Failed, message);
        }

        /// <summary>
        /// Obtiene un resultado estructurado del analisis actual.
        /// </summary>
        public DynamicFeedbackResult GetCurrentResult()
        {
            return new DynamicFeedbackResult
            {
                phase = currentPhase,
                issue = currentIssue,
                message = currentMessage,
                gestureName = activeGestureName,
                timestamp = Time.time
            };
        }

        private void SetPhase(DynamicFeedbackPhase phase, string message)
        {
            bool phaseChanged = currentPhase != phase;
            currentPhase = phase;
            currentMessage = message;

            if (phaseChanged)
            {
                phaseStartTime = Time.time;
                OnPhaseChanged?.Invoke(phase, message);
            }

            OnFeedbackMessage?.Invoke(message);
        }

        private bool ShouldUpdateMessage(DynamicMovementIssue newIssue, DynamicFeedbackPhase targetPhase)
        {
            // Siempre actualizar si cambia el problema
            if (newIssue != lastReportedIssue)
                return true;

            // Respetar cooldown para el mismo problema
            if (Time.time - lastMessageTime < MESSAGE_COOLDOWN)
                return false;

            // Actualizar si cambio la fase
            if (currentPhase != targetPhase)
                return true;

            return false;
        }

        private DynamicMovementIssue MapFailureReasonToIssue(FailureReason reason)
        {
            return reason switch
            {
                FailureReason.SpeedTooLow => DynamicMovementIssue.TooSlow,
                FailureReason.SpeedTooHigh => DynamicMovementIssue.TooFast,
                FailureReason.DistanceTooShort => DynamicMovementIssue.TooShort,
                FailureReason.DirectionWrong => DynamicMovementIssue.DirectionWrong,
                FailureReason.DirectionChangesInsufficient => DynamicMovementIssue.NeedMoreDirectionChanges,
                FailureReason.RotationInsufficient => DynamicMovementIssue.RotationInsufficient,
                FailureReason.NotCircular => DynamicMovementIssue.NotCircular,
                FailureReason.OutOfZone => DynamicMovementIssue.DirectionWrong, // Similar a direccion incorrecta
                FailureReason.PoseLost => DynamicMovementIssue.None, // Pose, no movimiento
                FailureReason.Timeout => DynamicMovementIssue.TooSlow, // Si hay timeout, probablemente fue lento
                _ => DynamicMovementIssue.None
            };
        }
    }

    /// <summary>
    /// Resultado estructurado del analisis de feedback dynamic.
    /// </summary>
    public struct DynamicFeedbackResult
    {
        public DynamicFeedbackPhase phase;
        public DynamicMovementIssue issue;
        public string message;
        public string gestureName;
        public float timestamp;

        /// <summary>
        /// True si el gesto esta en progreso (fases 1-3).
        /// </summary>
        public bool IsInProgress =>
            phase == DynamicFeedbackPhase.StartDetected ||
            phase == DynamicFeedbackPhase.InProgress ||
            phase == DynamicFeedbackPhase.NearCompletion;

        /// <summary>
        /// True si el gesto termino (completed o failed).
        /// </summary>
        public bool IsTerminal =>
            phase == DynamicFeedbackPhase.Completed ||
            phase == DynamicFeedbackPhase.Failed;

        /// <summary>
        /// True si el gesto fue exitoso.
        /// </summary>
        public bool IsSuccess => phase == DynamicFeedbackPhase.Completed;
    }
}
