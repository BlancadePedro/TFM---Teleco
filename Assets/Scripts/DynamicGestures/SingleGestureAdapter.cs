using UnityEngine;
using ASL_LearnVR.Gestures;

namespace ASL.DynamicGestures
{
    /// <summary>
    /// Adaptador para GestureRecognizer (singular) cuando NO tienes MultiGestureRecognizer.
    /// Úsalo cuando tienes LeftHandRecognizer y RightHandRecognizer en la escena.
    /// </summary>
    public class SingleGestureAdapter : MonoBehaviour, IPoseAdapter
    {
        [Header("Referencias")]
        [Tooltip("GestureRecognizer que detecta el gesto activo (ej: RightHandRecognizer)")]
        [SerializeField] private GestureRecognizer gestureRecognizer;

        private string currentPoseName = null;

        void OnEnable()
        {
            if (gestureRecognizer != null && gestureRecognizer.TargetSign != null)
            {
                // Suscribirse a eventos
                gestureRecognizer.onGestureDetected.AddListener(OnPoseDetected);
                gestureRecognizer.onGestureEnded.AddListener(OnPoseEnded);
            }
            else
            {
                Debug.LogWarning("[SingleGestureAdapter] GestureRecognizer no asignado o no tiene TargetSign configurado");
            }
        }

        void OnDisable()
        {
            if (gestureRecognizer != null)
            {
                gestureRecognizer.onGestureDetected.RemoveListener(OnPoseDetected);
                gestureRecognizer.onGestureEnded.RemoveListener(OnPoseEnded);
            }
        }

        void Update()
        {
            // Sincronizar con estado actual
            if (gestureRecognizer != null && gestureRecognizer.IsPerformed && gestureRecognizer.TargetSign != null)
            {
                string newPoseName = gestureRecognizer.TargetSign.signName;

                // DEBUG TEMPORAL - Mostrar cuando cambia la pose detectada
                if (newPoseName != currentPoseName)
                {
                    Debug.Log($"[SingleGestureAdapter] POSE DETECTADA: '{newPoseName}' (anterior: '{currentPoseName}')");
                }

                currentPoseName = newPoseName;
            }
            else if (currentPoseName != null)
            {
                // DEBUG TEMPORAL - Mostrar cuando se pierde la pose
                Debug.Log($"[SingleGestureAdapter] POSE PERDIDA: '{currentPoseName}'");
                currentPoseName = null;
            }
        }

        /// <summary>
        /// Callback cuando se detecta el gesto
        /// </summary>
        private void OnPoseDetected(ASL_LearnVR.Data.SignData sign)
        {
            if (sign != null)
            {
                Debug.Log($"[SingleGestureAdapter] OnPoseDetected EVENT: '{sign.signName}'");
                currentPoseName = sign.signName;
            }
        }

        /// <summary>
        /// Callback cuando el gesto termina
        /// </summary>
        private void OnPoseEnded(ASL_LearnVR.Data.SignData sign)
        {
            if (sign != null && currentPoseName == sign.signName)
            {
                currentPoseName = null;
            }
        }

        /// <summary>
        /// Obtiene el nombre de la pose actualmente detectada.
        /// Compatible con DynamicGestureRecognizer.
        /// </summary>
        public string GetCurrentPoseName()
        {
            return currentPoseName;
        }

        /// <summary>
        /// Verifica si hay tracking activo
        /// </summary>
        public bool IsHandTracked()
        {
            return gestureRecognizer != null && gestureRecognizer.IsDetected;
        }

        /// <summary>
        /// Cambia dinámicamente el GestureRecognizer a monitorear.
        /// Útil cuando cambias entre diferentes gestos en Learning Module.
        /// </summary>
        public void SetGestureRecognizer(GestureRecognizer newRecognizer)
        {
            // Desuscribir del anterior
            if (gestureRecognizer != null)
            {
                gestureRecognizer.onGestureDetected.RemoveListener(OnPoseDetected);
                gestureRecognizer.onGestureEnded.RemoveListener(OnPoseEnded);
            }

            // Asignar nuevo
            gestureRecognizer = newRecognizer;
            currentPoseName = null;

            // Suscribir al nuevo
            if (gestureRecognizer != null)
            {
                gestureRecognizer.onGestureDetected.AddListener(OnPoseDetected);
                gestureRecognizer.onGestureEnded.AddListener(OnPoseEnded);
            }
        }
    }
}
