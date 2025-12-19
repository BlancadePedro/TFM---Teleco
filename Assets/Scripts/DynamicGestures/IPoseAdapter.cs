namespace ASL.DynamicGestures
{
    /// <summary>
    /// Interfaz común para adaptadores de poses (Static y Single).
    /// Permite que DynamicGestureRecognizer funcione con ambos tipos.
    /// </summary>
    public interface IPoseAdapter
    {
        /// <summary>
        /// Obtiene el nombre de la pose actualmente detectada.
        /// </summary>
        /// <returns>Nombre del signo (ej: "A", "J", "5") o null si no hay pose detectada</returns>
        string GetCurrentPoseName();

        /// <summary>
        /// Verifica si hay tracking activo de la mano.
        /// </summary>
        bool IsHandTracked();
    }
}
