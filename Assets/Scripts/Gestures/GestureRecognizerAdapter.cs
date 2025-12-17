using UnityEngine;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.Gestures
{
    /// <summary>
    /// Adaptador que permite al GestureRecognizer existente implementar IStaticPoseDetector.
    /// Esto facilita la integración entre gestos estáticos y dinámicos.
    /// </summary>
    public class GestureRecognizerAdapter : MonoBehaviour, IStaticPoseDetector
    {
        [Header("Configuration")]
        [Tooltip("El GestureRecognizer a adaptar")]
        [SerializeField] private GestureRecognizer gestureRecognizer;

        void Awake()
        {
            if (gestureRecognizer == null)
            {
                gestureRecognizer = GetComponent<GestureRecognizer>();
            }

            if (gestureRecognizer == null)
            {
                Debug.LogError("[GestureRecognizerAdapter] No se encontró un GestureRecognizer!");
            }
        }

        /// <summary>
        /// Verifica si una pose estática está siendo detectada.
        /// </summary>
        public bool IsPoseDetected(SignData signData)
        {
            if (gestureRecognizer == null || signData == null)
                return false;

            // Verifica si el signo configurado coincide
            if (gestureRecognizer.TargetSign != signData)
                return false;

            return gestureRecognizer.IsDetected;
        }

        /// <summary>
        /// Verifica si una pose estática ha sido confirmada (hold time cumplido).
        /// </summary>
        public bool IsPosePerformed(SignData signData)
        {
            if (gestureRecognizer == null || signData == null)
                return false;

            // Verifica si el signo configurado coincide
            if (gestureRecognizer.TargetSign != signData)
                return false;

            return gestureRecognizer.IsPerformed;
        }

        /// <summary>
        /// Configura el signo objetivo del recognizer.
        /// </summary>
        public void SetTargetSign(SignData signData)
        {
            if (gestureRecognizer != null)
            {
                gestureRecognizer.TargetSign = signData;
            }
        }

        /// <summary>
        /// Habilita o deshabilita la detección.
        /// </summary>
        public void SetDetectionEnabled(bool enabled)
        {
            if (gestureRecognizer != null)
            {
                gestureRecognizer.SetDetectionEnabled(enabled);
            }
        }

        /// <summary>
        /// Resetea el estado del recognizer.
        /// </summary>
        public void ResetState()
        {
            if (gestureRecognizer != null)
            {
                gestureRecognizer.ResetState();
            }
        }
    }
}
