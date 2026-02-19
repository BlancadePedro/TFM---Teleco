using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

namespace ASL_LearnVR.Core
{
    /// <summary>
    /// Monitors hand tracking status and emits events when it changes.
    /// Compatible with Unity XR Hands.
    /// </summary>
    public class HandTrackingStatus : MonoBehaviour
    {
        [Header("Events")]
        [Tooltip("Both hands are being tracked")]
        public UnityEvent onBothHandsTracked;

        [Tooltip("At least one hand is no longer tracked")]
        public UnityEvent onHandsLost;

        [Header("Status")]
        [SerializeField] private bool leftHandTracked = false;
        [SerializeField] private bool rightHandTracked = false;

        private XRHandSubsystem handSubsystem;
        private bool bothHandsWereTracked = false;

        /// <summary>
        /// True si la mano izquierda esta being tracked.
        /// </summary>
        public bool IsLeftHandTracked => leftHandTracked;

        /// <summary>
        /// True si la mano derecha esta being tracked.
        /// </summary>
        public bool IsRightHandTracked => rightHandTracked;

        /// <summary>
        /// True si ambas manos estan being trackeds.
        /// </summary>
        public bool AreBothHandsTracked => leftHandTracked && rightHandTracked;

        void Update()
        {
            // Busca el XRHandSubsystem active
            if (handSubsystem == null || !handSubsystem.running)
            {
                handSubsystem = GetActiveHandSubsystem();
                if (handSubsystem == null)
                    return;
            }

            // Update tracking state
            UpdateTrackingStatus();
        }

        /// <summary>
        /// Obtiene el XRHandSubsystem active.
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
        /// Updates the tracking state of both hands.
        /// </summary>
        private void UpdateTrackingStatus()
        {
            bool previousLeftTracked = leftHandTracked;
            bool previousRightTracked = rightHandTracked;

            leftHandTracked = handSubsystem.leftHand.isTracked;
            rightHandTracked = handSubsystem.rightHand.isTracked;

            bool bothTrackedNow = AreBothHandsTracked;

            // Detects when both hands start being tracked
            if (bothTrackedNow && !bothHandsWereTracked)
            {
                onBothHandsTracked?.Invoke();
                bothHandsWereTracked = true;
            }
            // Detects when at least one hand stops being tracked
            else if (!bothTrackedNow && bothHandsWereTracked)
            {
                onHandsLost?.Invoke();
                bothHandsWereTracked = false;
            }
        }

        /// <summary>
        /// Gets a human-readable description of the current state.
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
