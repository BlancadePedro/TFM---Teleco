using UnityEngine;
using UnityEngine.XR.Hands.Gestures;

namespace ASL_LearnVR.Data
{
    /// <summary>
    /// Representa un signo individual del lenguaje de señas (ASL).
    /// Contiene la referencia al Hand Shape/Pose y metadatos del signo.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSign", menuName = "ASL Learn VR/Sign Data", order = 1)]
    public class SignData : ScriptableObject
    {
        [Header("Sign Information")]
        [Tooltip("Nombre del signo (ej: 'A', '1', 'Red')")]
        public string signName;

        [Tooltip("Descripción del signo (opcional)")]
        [TextArea(2, 4)]
        public string description;

        [Header("Gesture Detection")]
        [Tooltip("Hand Shape o Hand Pose de Unity XR Hands para este signo")]
        public ScriptableObject handShapeOrPose;

        [Tooltip("¿Este signo requiere movimiento? (J, Z requieren movimiento)")]
        public bool requiresMovement = false;

        [Tooltip("Tiempo mínimo que debe mantenerse el gesto para ser detectado (segundos)")]
        [Range(0.1f, 2f)]
        public float minimumHoldTime = 0.3f;

        [Header("Visual Representation")]
        [Tooltip("Icono del signo para mostrar en la UI")]
        public Sprite icon;

        [Tooltip("Prefab de las ghost hands para este signo (opcional si no hay movimiento)")]
        public GameObject ghostHandsPrefab;

        [Header("Recording (for dynamic gestures)")]
        [Tooltip("Archivo de grabación de manos para gestos dinámicos (J, Z, etc.)")]
        public ScriptableObject handRecordingData;

        [Tooltip("Frame inicial de la grabación (para trimming)")]
        public int recordingStartFrame = 0;

        [Tooltip("Frame final de la grabación (para trimming). Si es 0, usa toda la grabación.")]
        public int recordingEndFrame = 0;

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
        /// Valida que el SignData esté correctamente configurado.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(signName))
            {
                Debug.LogError($"SignData '{name}' tiene un signName vacío.");
                return false;
            }

            if (handShapeOrPose == null)
            {
                Debug.LogError($"SignData '{signName}' no tiene Hand Shape/Pose asignado.");
                return false;
            }

            if (requiresMovement && handRecordingData == null)
            {
                Debug.LogWarning($"SignData '{signName}' requiere movimiento pero no tiene handRecordingData.");
            }

            return true;
        }
    }
}
