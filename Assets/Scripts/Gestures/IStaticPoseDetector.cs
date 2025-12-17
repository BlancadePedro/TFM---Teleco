using ASL_LearnVR.Data;

namespace ASL_LearnVR.Gestures
{
    /// <summary>
    /// Interfaz para detectores de poses estáticas.
    /// Permite a los gestos dinámicos consultar el estado de una pose estática.
    /// </summary>
    public interface IStaticPoseDetector
    {
        /// <summary>
        /// Verifica si una pose estática está siendo detectada actualmente.
        /// </summary>
        /// <param name="signData">El SignData de la pose a verificar.</param>
        /// <returns>True si la pose está siendo detectada.</returns>
        bool IsPoseDetected(SignData signData);

        /// <summary>
        /// Verifica si una pose estática ha sido confirmada (hold time cumplido).
        /// </summary>
        /// <param name="signData">El SignData de la pose a verificar.</param>
        /// <returns>True si la pose ha sido confirmada.</returns>
        bool IsPosePerformed(SignData signData);
    }
}
