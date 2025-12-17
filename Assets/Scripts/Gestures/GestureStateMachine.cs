using UnityEngine;

namespace ASL_LearnVR.Gestures
{
    /// <summary>
    /// Estados posibles para el reconocimiento de un gesto dinámico.
    /// </summary>
    public enum GestureState
    {
        /// <summary>
        /// Estado inicial, esperando condiciones para iniciar.
        /// </summary>
        Idle,

        /// <summary>
        /// Esperando la pose inicial requerida (si aplica).
        /// </summary>
        WaitingForStartPose,

        /// <summary>
        /// Grabando la trayectoria del movimiento.
        /// </summary>
        Recording,

        /// <summary>
        /// Esperando la pose final requerida (si aplica).
        /// </summary>
        WaitingForEndPose,

        /// <summary>
        /// Evaluando si el gesto cumple los criterios.
        /// </summary>
        Evaluating,

        /// <summary>
        /// Gesto completado exitosamente.
        /// </summary>
        Completed,

        /// <summary>
        /// Gesto falló la validación.
        /// </summary>
        Failed,

        /// <summary>
        /// Periodo de cooldown antes de permitir nuevo gesto.
        /// </summary>
        Cooldown
    }

    /// <summary>
    /// Máquina de estados para gestionar el reconocimiento de gestos dinámicos.
    /// </summary>
    public class GestureStateMachine
    {
        private GestureState currentState;
        private float stateStartTime;
        private DynamicGestureDefinition gestureDefinition;

        /// <summary>
        /// Estado actual de la máquina.
        /// </summary>
        public GestureState CurrentState => currentState;

        /// <summary>
        /// Tiempo transcurrido en el estado actual (segundos).
        /// </summary>
        public float TimeInCurrentState => Time.timeSinceLevelLoad - stateStartTime;

        /// <summary>
        /// Evento que se dispara cuando cambia el estado.
        /// </summary>
        public event System.Action<GestureState, GestureState> OnStateChanged;

        public GestureStateMachine(DynamicGestureDefinition definition)
        {
            gestureDefinition = definition;
            currentState = GestureState.Idle;
            stateStartTime = Time.timeSinceLevelLoad;
        }

        /// <summary>
        /// Cambia el estado de la máquina.
        /// </summary>
        public void ChangeState(GestureState newState)
        {
            if (currentState == newState)
                return;

            GestureState previousState = currentState;
            currentState = newState;
            stateStartTime = Time.timeSinceLevelLoad;

            OnStateChanged?.Invoke(previousState, newState);
        }

        /// <summary>
        /// Resetea la máquina al estado Idle.
        /// </summary>
        public void Reset()
        {
            ChangeState(GestureState.Idle);
        }

        /// <summary>
        /// Verifica si el estado actual es un estado terminal.
        /// </summary>
        public bool IsInTerminalState()
        {
            return currentState == GestureState.Completed || currentState == GestureState.Failed;
        }

        /// <summary>
        /// Verifica si el estado actual es activo (grabando o procesando).
        /// </summary>
        public bool IsActive()
        {
            return currentState == GestureState.WaitingForStartPose ||
                   currentState == GestureState.Recording ||
                   currentState == GestureState.WaitingForEndPose ||
                   currentState == GestureState.Evaluating;
        }

        /// <summary>
        /// Verifica si se debe abortar el gesto por timeout en el estado actual.
        /// </summary>
        public bool ShouldTimeoutCurrentState()
        {
            switch (currentState)
            {
                case GestureState.WaitingForStartPose:
                    // Timeout de 5 segundos esperando pose inicial
                    return TimeInCurrentState > 5f;

                case GestureState.Recording:
                    // Timeout basado en la duración máxima del gesto
                    return TimeInCurrentState > gestureDefinition.maximumDuration;

                case GestureState.WaitingForEndPose:
                    // Timeout de 3 segundos esperando pose final
                    return TimeInCurrentState > 3f;

                case GestureState.Cooldown:
                    // El cooldown se maneja externamente
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Verifica si el cooldown ha terminado.
        /// </summary>
        public bool IsCooldownFinished()
        {
            if (currentState != GestureState.Cooldown)
                return true;

            return TimeInCurrentState >= gestureDefinition.cooldownTime;
        }

        /// <summary>
        /// Obtiene una descripción del estado actual (para debugging).
        /// </summary>
        public string GetStateDescription()
        {
            switch (currentState)
            {
                case GestureState.Idle:
                    return "Idle - Esperando inicio";

                case GestureState.WaitingForStartPose:
                    return $"Esperando pose inicial: {gestureDefinition.requiredStartPose?.signName}";

                case GestureState.Recording:
                    return $"Grabando movimiento ({TimeInCurrentState:F1}s)";

                case GestureState.WaitingForEndPose:
                    return $"Esperando pose final: {gestureDefinition.requiredEndPose?.signName}";

                case GestureState.Evaluating:
                    return "Evaluando gesto";

                case GestureState.Completed:
                    return "Gesto completado";

                case GestureState.Failed:
                    return "Gesto fallido";

                case GestureState.Cooldown:
                    return $"Cooldown ({gestureDefinition.cooldownTime - TimeInCurrentState:F1}s)";

                default:
                    return "Estado desconocido";
            }
        }
    }
}
