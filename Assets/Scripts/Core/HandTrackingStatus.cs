using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

namespace ASL_LearnVR.Core
{
    /// <summary>
    /// Monitorea el estado del hand tracking y emite eventos cuando cambia.
    /// Compatible con Unity XR Hands.
    /// </summary>
    public class HandTrackingStatus : MonoBehaviour
    {
        [Header("Events")]
        [Tooltip("Ambas manos están siendo trackeadas")]
        public UnityEvent onBothHandsTracked;

        [Tooltip("Al menos una mano deja de ser trackeada")]
        public UnityEvent onHandsLost;

        [Header("Status")]
        [SerializeField] private bool leftHandTracked = false;
        [SerializeField] private bool rightHandTracked = false;

        private XRHandSubsystem handSubsystem;
        private bool bothHandsWereTracked = false;

        /// <summary>
        /// True si la mano izquierda está siendo trackeada.
        /// </summary>
        public bool IsLeftHandTracked => leftHandTracked;

        /// <summary>
        /// True si la mano derecha está siendo trackeada.
        /// </summary>
        public bool IsRightHandTracked => rightHandTracked;

        /// <summary>
        /// True si ambas manos están siendo trackeadas.
        /// </summary>
        public bool AreBothHandsTracked => leftHandTracked && rightHandTracked;

        void Update()
        {
            // Busca el XRHandSubsystem activo
            if (handSubsystem == null || !handSubsystem.running)
            {
                handSubsystem = GetActiveHandSubsystem();
                if (handSubsystem == null)
                    return;
            }

            // Actualiza el estado de tracking
            UpdateTrackingStatus();
        }

        /// <summary>
        /// Obtiene el XRHandSubsystem activo.
        /// </summary>
        private XRHandSubsystem GetActiveHandSubsystem()
        {
            var subsystems = new System.Collections.Generic.List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            foreach (var subsystem in subsystems)
            {
                if (subsystem.running)
                    return subsystem;
            }

            return null;
        }

        /// <summary>
        /// Actualiza el estado de tracking de ambas manos.
        /// </summary>
        private void UpdateTrackingStatus()
        {
            bool previousLeftTracked = leftHandTracked;
            bool previousRightTracked = rightHandTracked;

            leftHandTracked = handSubsystem.leftHand.isTracked;
            rightHandTracked = handSubsystem.rightHand.isTracked;

            bool bothTrackedNow = AreBothHandsTracked;

            // Detecta cuando ambas manos empiezan a ser trackeadas
            if (bothTrackedNow && !bothHandsWereTracked)
            {
                onBothHandsTracked?.Invoke();
                bothHandsWereTracked = true;
            }
            // Detecta cuando al menos una mano deja de ser trackeada
            else if (!bothTrackedNow && bothHandsWereTracked)
            {
                onHandsLost?.Invoke();
                bothHandsWereTracked = false;
            }
        }

        /// <summary>
        /// Obtiene una descripción legible del estado actual.
        /// </summary>
        public string GetStatusDescription()
        {
            if (AreBothHandsTracked)
                return "You are all set up, ready to learn!";
            else if (leftHandTracked && !rightHandTracked)
                return "Right hand not detected. Please show your right hand.";
            else if (!leftHandTracked && rightHandTracked)
                return "Left hand not detected. Please show your left hand.";
            else
                return "No hands detected. Please show both hands to the camera.";
        }
    }
}
