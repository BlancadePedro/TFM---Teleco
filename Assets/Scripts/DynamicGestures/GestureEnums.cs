using System;
using UnityEngine;
using ASL_LearnVR.Data;

namespace ASL.DynamicGestures
{
    /// <summary>
    /// Define cuando debe cumplirse un requisito de pose durante el gesto
    /// </summary>
    public enum PoseTimingRequirement
    {
        /// <summary>
        /// La pose debe estar presente al iniciar el gesto
        /// </summary>
        Start,

        /// <summary>
        /// La pose debe mantenerse durante todo el gesto
        /// </summary>
        During,

        /// <summary>
        /// La pose debe estar presente al finalizar el gesto
        /// </summary>
        End,

        /// <summary>
        /// La pose puede ocurrir en cualquier momento del gesto
        /// </summary>
        Any
    }

    /// <summary>
    /// States de la maquina de estados del reconocedor de gestos
    /// </summary>
    internal enum GestureState
    {
        /// <summary>
        /// No hay gesto active, esperando deteccion de initial pose
        /// </summary>
        Idle,

        /// <summary>
        /// Pose inicial detectada, esperando para desambiguar entre gestos statics/dynamics
        /// o entre dynamic gestures con misma initial pose
        /// </summary>
        PendingConfirmation,

        /// <summary>
        /// Gesture en progreso, validando requisitos
        /// </summary>
        InProgress
    }

    /// <summary>
    /// Define un requisito de pose estatica dentro de la secuencia del gesto
    /// </summary>
    [Serializable]
    public class StaticPoseRequirement
    {
        /// <summary>
        /// Name de la pose estatica requerida (debe coincidir con StaticPoseDetector)
        /// </summary>
        public string poseName;

        /// <summary>
        /// Momento en que se requiere esta pose
        /// </summary>
        public PoseTimingRequirement timing;

        /// <summary>
        /// Si es true, el gesto puede completarse sin esta pose
        /// </summary>
        public bool isOptional;

        /// <summary>
        /// NUEVO: Reference directa al SignData para validacion de poses End.
        /// Cuando se usa SingleGestureAdapter (que solo retorna el TargetSign),
        /// esta referencia permite validar poses End usando el HandShape directamente.
        /// OBLIGATORIO for gestures compuestos (White: 5→S, Orange: O→S, Thursday: T→H)
        /// </summary>
        [Tooltip("Reference directa al SignData para validacion de poses End en compound gestures")]
        public SignData poseData;

        /// <summary>
        ///  NUEVO: Familia de poses aceptables (alternativas). Si esta vacio, solo se acepta poseName.
        /// Ejemplo: ["OpenHand", "5", "FlatHand"] for gestures que aceptan variaciones de mano abierta
        /// </summary>
        public string[] poseFamilyAlternatives = new string[0];

        /// <summary>
        ///  NUEVO: Verifica si una pose detectada es valid para este requisito (nombre exacto o familia)
        /// </summary>
        public bool IsValidPose(string detectedPose)
        {
            if (string.IsNullOrEmpty(detectedPose))
                return false;

            // Verificar nombre exacto
            if (poseName.Equals(detectedPose, StringComparison.OrdinalIgnoreCase))
                return true;

            // Verificar familia de alternativas
            if (poseFamilyAlternatives != null && poseFamilyAlternatives.Length > 0)
            {
                foreach (var alternative in poseFamilyAlternatives)
                {
                    if (!string.IsNullOrEmpty(alternative) &&
                        alternative.Equals(detectedPose, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
