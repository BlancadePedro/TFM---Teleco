using UnityEngine;
using UnityEngine.XR.Hands.Gestures;

namespace ASL_LearnVR.Data
{
    /// <summary>
    /// Representa un signo individual del lenguaje de senas (ASL).
    /// Contiene la referencia al Hand Shape/Pose y metadatos del signo.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSign", menuName = "ASL Learn VR/Sign Data", order = 1)]
    public class SignData : ScriptableObject
    {
        [Header("Sign Information")]
        [Tooltip("Name del signo (ej: 'A', '1', 'Red')")]
        public string signName;

        [Tooltip("Sign description (optional)")]
        [TextArea(2, 4)]
        public string description;

        [Header("Gesture Detection")]
        [Tooltip("Hand Shape o Hand Pose de Unity XR Hands para este signo")]
        public ScriptableObject handShapeOrPose;

        [Tooltip("¿Este signo requiere movimiento? (J, Z requieren movimiento)")]
        public bool requiresMovement = false;

        [Tooltip("Time minimo que debe mantenerse el gesto para ser detected (segundos)")]
        [Range(0.1f, 2f)]
        public float minimumHoldTime = 0.3f;

        [Header("Visual Representation")]
        [Tooltip("Icon del signo para mostrar en la UI")]
        public Sprite icon;

        /// <summary>
        /// Obtiene el XRHandShape si existe.
        /// </summary>
        public XRHandShape GetHandShape()
        {
            return handShapeOrPose as XRHandShape;
        }

        /// <summary>
        /// Obtiene el XRHandPose si existe.
        /// </summary>
        public XRHandPose GetHandPose()
        {
            return handShapeOrPose as XRHandPose;
        }

        /// <summary>
        /// Valida que el SignData este correctamente configured.
        /// </summary>
        public virtual bool IsValid()
        {
            if (string.IsNullOrEmpty(signName))
            {
                Debug.LogError($"SignData '{name}' tiene un signName vacio.");
                return false;
            }

            if (handShapeOrPose == null)
            {
                Debug.LogError($"SignData '{signName}' no tiene Hand Shape/Pose assigned.");
                return false;
            }

            return true;
        }
    }
}
