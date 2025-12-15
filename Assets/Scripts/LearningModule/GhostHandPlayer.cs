using UnityEngine;
using UnityEngine.XR.Hands;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.LearningModule
{
    /// <summary>
    /// Controla las "ghost hands" que muestran visualmente cómo hacer un signo ASL.
    /// Para gestos estáticos: muestra la pose directamente.
    /// Para gestos dinámicos (J, Z): reproduce una animación o grabación.
    /// </summary>
    public class GhostHandPlayer : MonoBehaviour
    {
        [Header("Ghost Hand References")]
        [Tooltip("Referencia al GameObject de la mano izquierda fantasma")]
        [SerializeField] private GameObject leftGhostHand;

        [Tooltip("Referencia al GameObject de la mano derecha fantasma")]
        [SerializeField] private GameObject rightGhostHand;

        [Header("Visual Settings")]
        [Tooltip("Material para las ghost hands (semi-transparente)")]
        [SerializeField] private Material ghostHandMaterial;

        [Tooltip("Color de las ghost hands")]
        [SerializeField] private Color ghostHandColor = new Color(0f, 0.627451f, 1f, 0.5f);

        [Header("Animation Settings")]
        [Tooltip("Duración de la animación de aparición/desaparición")]
        [SerializeField] private float fadeInDuration = 0.3f;

        [Tooltip("Tiempo que las ghost hands permanecen visibles para gestos estáticos")]
        [SerializeField] private float staticPoseDisplayTime = 3f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        private SignData currentSign;
        private bool isPlaying = false;
        private SkinnedMeshRenderer[] leftHandRenderers;
        private SkinnedMeshRenderer[] rightHandRenderers;

        /// <summary>
        /// True si las ghost hands están reproduciendo actualmente.
        /// </summary>
        public bool IsPlaying => isPlaying;

        void Awake()
        {
            // Obtiene los renderers de las ghost hands
            if (leftGhostHand != null)
                leftHandRenderers = leftGhostHand.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (rightGhostHand != null)
                rightHandRenderers = rightGhostHand.GetComponentsInChildren<SkinnedMeshRenderer>();

            // Aplica el material fantasma
            ApplyGhostMaterial();

            // Oculta las ghost hands al inicio
            SetGhostHandsVisible(false);
        }

        /// <summary>
        /// Aplica el material y color fantasma a las manos.
        /// </summary>
        private void ApplyGhostMaterial()
        {
            if (ghostHandMaterial != null)
            {
                ghostHandMaterial.color = ghostHandColor;

                foreach (var renderer in leftHandRenderers)
                    renderer.material = ghostHandMaterial;

                foreach (var renderer in rightHandRenderers)
                    renderer.material = ghostHandMaterial;
            }
        }

        /// <summary>
        /// Muestra las ghost hands ejecutando el signo especificado.
        /// </summary>
        public void PlaySign(SignData sign)
        {
            if (sign == null)
            {
                Debug.LogError("GhostHandPlayer: SignData es null.");
                return;
            }

            if (isPlaying)
            {
                if (showDebugLogs)
                    Debug.LogWarning("GhostHandPlayer: Ya hay una reproducción en curso.");
                return;
            }

            currentSign = sign;

            if (currentSign.requiresMovement)
            {
                // Gestos dinámicos (J, Z)
                PlayDynamicGesture();
            }
            else
            {
                // Gestos estáticos (A, B, C, etc.)
                PlayStaticGesture();
            }
        }

        /// <summary>
        /// Reproduce un gesto estático (muestra la pose durante un tiempo).
        /// </summary>
        private void PlayStaticGesture()
        {
            if (showDebugLogs)
                Debug.Log($"GhostHandPlayer: Mostrando gesto estático '{currentSign.signName}'");

            isPlaying = true;

            // TODO: Aquí aplicarías la pose del Hand Shape/Pose al skeleton de las ghost hands
            // Por ahora, simplemente muestra las manos en su pose por defecto
            // En una implementación completa, necesitarías:
            // 1. Obtener los datos de joint positions del Hand Shape
            // 2. Aplicarlos al XRHandSkeletonDriver de las ghost hands

            SetGhostHandsVisible(true);

            // Oculta después del tiempo especificado
            Invoke(nameof(StopPlaying), staticPoseDisplayTime);
        }

        /// <summary>
        /// Reproduce un gesto dinámico (animación o grabación).
        /// </summary>
        private void PlayDynamicGesture()
        {
            if (showDebugLogs)
                Debug.Log($"GhostHandPlayer: Reproduciendo gesto dinámico '{currentSign.signName}'");

            isPlaying = true;

            // TODO: Implementar reproducción de grabación de manos
            // Si tienes un handRecordingData en el SignData:
            // 1. Parsear los datos de la grabación
            // 2. Reproducir frame por frame las posiciones de los joints
            // 3. Aplicarlas al XRHandSkeletonDriver

            if (currentSign.handRecordingData != null)
            {
                // Aquí iría la lógica de reproducción de la grabación
                Debug.LogWarning("GhostHandPlayer: Reproducción de grabaciones aún no implementada.");
            }
            else
            {
                Debug.LogWarning($"GhostHandPlayer: El signo '{currentSign.signName}' requiere movimiento pero no tiene handRecordingData.");
            }

            SetGhostHandsVisible(true);

            // Por ahora, simplemente oculta después de un tiempo
            Invoke(nameof(StopPlaying), staticPoseDisplayTime);
        }

        /// <summary>
        /// Detiene la reproducción y oculta las ghost hands.
        /// </summary>
        public void StopPlaying()
        {
            isPlaying = false;
            SetGhostHandsVisible(false);

            if (showDebugLogs)
                Debug.Log("GhostHandPlayer: Reproducción detenida.");
        }

        /// <summary>
        /// Muestra u oculta las ghost hands.
        /// </summary>
        private void SetGhostHandsVisible(bool visible)
        {
            if (leftGhostHand != null)
                leftGhostHand.SetActive(visible);

            if (rightGhostHand != null)
                rightGhostHand.SetActive(visible);
        }

        /// <summary>
        /// Configura qué mano(s) mostrar según el signo.
        /// Algunos signos solo requieren una mano.
        /// </summary>
        private void ConfigureHandsVisibility()
        {
            // Por defecto, muestra ambas manos
            // En una implementación avanzada, podrías configurar esto en SignData
            bool showLeft = true;
            bool showRight = true;

            if (leftGhostHand != null)
                leftGhostHand.SetActive(showLeft);

            if (rightGhostHand != null)
                rightGhostHand.SetActive(showRight);
        }
    }
}
