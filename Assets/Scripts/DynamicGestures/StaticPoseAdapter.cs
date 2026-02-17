using UnityEngine;
using ASL_LearnVR.Gestures;
using ASL_LearnVR.Data;

namespace ASL.DynamicGestures
{
    /// <summary>
    /// Adaptador que convierte MultiGestureRecognizer en una interfaz simple para DynamicGestureRecognizer.
    /// Proporciona el método GetCurrentPoseName() que retorna el nombre del signo actualmente detectado.
    /// </summary>
    public class StaticPoseAdapter : MonoBehaviour, IPoseAdapter
    {
        [Header("Referencias")]
        [Tooltip("MultiGestureRecognizer que detecta poses estáticas")]
        [SerializeField] private MultiGestureRecognizer multiGestureRecognizer;

        private string currentPoseName = null;

        void OnEnable()
        {
            if (multiGestureRecognizer != null)
            {
                // Suscribirse a eventos de reconocimiento instantáneo (sin hold time)
                multiGestureRecognizer.onGestureRecognized.AddListener(OnPoseRecognized);
                multiGestureRecognizer.onGestureLost.AddListener(OnPoseLost);
            }
            else
            {
                Debug.LogError("[StaticPoseAdapter] Falta asignar MultiGestureRecognizer en el Inspector!");
            }
        }

        void OnDisable()
        {
            if (multiGestureRecognizer != null)
            {
                multiGestureRecognizer.onGestureRecognized.RemoveListener(OnPoseRecognized);
                multiGestureRecognizer.onGestureLost.RemoveListener(OnPoseLost);
            }
        }

        void Update()
        {
            // Sincroniza con el estado actual del MultiGestureRecognizer
            if (multiGestureRecognizer != null && multiGestureRecognizer.CurrentActiveSign != null)
            {
                currentPoseName = multiGestureRecognizer.CurrentActiveSign.signName;
            }
            else
            {
                currentPoseName = null;
            }
        }

        /// <summary>
        /// Callback cuando se reconoce instantáneamente una pose
        /// </summary>
        private void OnPoseRecognized(SignData sign)
        {
            if (sign != null)
            {
                currentPoseName = sign.signName;
            }
        }

        /// <summary>
        /// Callback cuando se pierde una pose
        /// </summary>
        private void OnPoseLost(SignData sign)
        {
            // Solo limpiar si era el signo activo
            if (sign != null && currentPoseName == sign.signName)
            {
                currentPoseName = null;
            }
        }

        /// <summary>
        /// Obtiene el nombre de la pose actualmente detectada.
        /// Compatible con la interfaz esperada por DynamicGestureRecognizer.
        /// </summary>
        /// <returns>Nombre del signo (ej: "A", "J", "5", "OK") o null si no hay pose detectada</returns>
        public string GetCurrentPoseName()
        {
            return currentPoseName;
        }

        /// <summary>
        /// Verifica si hay tracking activo de la mano
        /// </summary>
        public bool IsHandTracked()
        {
            // El MultiGestureRecognizer internamente valida handIsTracked
            return multiGestureRecognizer != null && multiGestureRecognizer.CurrentActiveSign != null;
        }

        /// <summary>
        /// Obtiene el SignData actualmente detectado por el MultiGestureRecognizer.
        /// Útil para validación directa de HandShape sin depender del nombre.
        /// </summary>
        public SignData GetCurrentSignData()
        {
            return multiGestureRecognizer != null ? multiGestureRecognizer.CurrentActiveSign : null;
        }
    }
}
