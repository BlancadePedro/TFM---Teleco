using UnityEngine;

namespace ASL_LearnVR.UI
{
    /// <summary>
    /// Puente entre el sistema de feedback de gestos y el PanelInteractionController.
    ///
    /// Llama a SetPending() cuando el gesto está en proceso de reconocimiento
    /// y a SetCorrect() cuando el gesto se valida correctamente.
    ///
    /// INTEGRACIÓN:
    ///   Adjunta este componente al mismo GameObject que PanelInteractionController.
    ///   Desde FeedbackUI o GestureRecognizer, llama a:
    ///       FeedbackBridgeUI.Instance.OnGestureStateChanged(state)
    ///
    ///   O si prefieres sin singleton, obtén la referencia directamente:
    ///       feedbackBridge.NotifyPending();
    ///       feedbackBridge.NotifyCorrect();
    ///       feedbackBridge.NotifyIdle();
    /// </summary>
    [AddComponentMenu("ASL_LearnVR/UI/Feedback Bridge UI")]
    public class FeedbackBridgeUI : MonoBehaviour
    {
        public static FeedbackBridgeUI Instance { get; private set; }

        [Tooltip("Panel que cambia de color según el estado del gesto (escena 3 y 4)")]
        [SerializeField] private PanelInteractionController feedbackPanel;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(this);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// El gesto ha empezado a evaluarse pero aún no es correcto.
        /// Activa ámbar #F5A623 con pulso lento.
        /// </summary>
        public void NotifyPending()
        {
            feedbackPanel?.SetState(PanelInteractionController.BorderState.Pending);
        }

        /// <summary>
        /// El gesto se ha reconocido correctamente.
        /// Activa verde menta #48C99E con flash.
        /// </summary>
        public void NotifyCorrect()
        {
            feedbackPanel?.SetState(PanelInteractionController.BorderState.Correct);
        }

        /// <summary>
        /// Vuelve al estado neutral (sin gesto activo).
        /// </summary>
        public void NotifyIdle()
        {
            feedbackPanel?.SetState(PanelInteractionController.BorderState.Idle);
        }

        /// <summary>
        /// Enum unificado para pasar desde cualquier sistema externo.
        /// </summary>
        public enum GestureState { Idle, Pending, Correct }

        public void OnGestureStateChanged(GestureState state)
        {
            switch (state)
            {
                case GestureState.Pending: NotifyPending(); break;
                case GestureState.Correct: NotifyCorrect(); break;
                default:                   NotifyIdle();    break;
            }
        }
    }
}
