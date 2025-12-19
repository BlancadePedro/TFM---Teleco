using System;

namespace ASL.DynamicGestures
{
    /// <summary>
    /// Define cuándo debe cumplirse un requisito de pose durante el gesto
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
    /// Estados de la máquina de estados del reconocedor de gestos
    /// </summary>
    internal enum GestureState
    {
        /// <summary>
        /// No hay gesto activo, esperando detección de pose inicial
        /// </summary>
        Idle,

        /// <summary>
        /// Gesto en progreso, validando requisitos
        /// </summary>
        InProgress
    }

    /// <summary>
    /// Define un requisito de pose estática dentro de la secuencia del gesto
    /// </summary>
    [Serializable]
    public class StaticPoseRequirement
    {
        /// <summary>
        /// Nombre de la pose estática requerida (debe coincidir con StaticPoseDetector)
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
    }
}
