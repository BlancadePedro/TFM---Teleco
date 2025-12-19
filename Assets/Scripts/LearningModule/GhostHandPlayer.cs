using UnityEngine;
using UnityEngine.XR.Hands;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.LearningModule
{
    /// <summary>
    /// Controla las "ghost hands" que muestran visualmente cómo hacer un signo ASL.
    /// Para gestos estáticos: muestra la pose directamente.
    /// Para gestos dinámicos (J, Z): reproduce una animación o grabación.
    /// IMPORTANTE: Las ghost hands están DESACOPLADAS del tracking real del usuario.
    /// Se posicionan en un punto fijo del espacio y solo reproducen la pose/animación del signo.
    /// </summary>
    public class GhostHandPlayer : MonoBehaviour
    {
        [Header("Ghost Hand References")]
        [Tooltip("Referencia al GameObject de la mano izquierda fantasma")]
        [SerializeField] private GameObject leftGhostHand;

        [Tooltip("Referencia al GameObject de la mano derecha fantasma")]
        [SerializeField] private GameObject rightGhostHand;

        [Header("Positioning")]
        [Tooltip("Posición fija donde aparecen las ghost hands (relativa al XR Origin)")]
        [SerializeField] private Vector3 ghostHandsPosition = new Vector3(0f, 1.2f, 0.5f);

        [Tooltip("Rotación fija de las ghost hands")]
        [SerializeField] private Vector3 ghostHandsRotation = new Vector3(0f, 180f, 0f);

        [Tooltip("Escala de las ghost hands")]
        [SerializeField] private float ghostHandsScale = 1.0f;

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
        private Vector3 originalLeftPosition;
        private Quaternion originalLeftRotation;
        private Vector3 originalRightPosition;
        private Quaternion originalRightRotation;

        /// <summary>
        /// True si las ghost hands están reproduciendo actualmente.
        /// </summary>
        public bool IsPlaying => isPlaying;

        void Awake()
        {
            // CRÍTICO: Desacopla las ghost hands del tracking XR
            // Elimina cualquier componente XRHandSkeletonDriver que pudiera seguir el tracking
            DisableHandTracking();

            // CRÍTICO: DESPARENTA las ghost hands del XR Origin para que NO sigan el tracking
            DetachFromXROrigin();

            // Guarda las posiciones originales
            if (leftGhostHand != null)
            {
                originalLeftPosition = leftGhostHand.transform.position;
                originalLeftRotation = leftGhostHand.transform.rotation;
                leftHandRenderers = leftGhostHand.GetComponentsInChildren<SkinnedMeshRenderer>();
            }

            if (rightGhostHand != null)
            {
                originalRightPosition = rightGhostHand.transform.position;
                originalRightRotation = rightGhostHand.transform.rotation;
                rightHandRenderers = rightGhostHand.GetComponentsInChildren<SkinnedMeshRenderer>();
            }

            // Posiciona las ghost hands en su ubicación fija en WORLD SPACE
            PositionGhostHands();

            // Aplica el material fantasma
            ApplyGhostMaterial();

            // Oculta las ghost hands al inicio
            SetGhostHandsVisible(false);
        }

        /// <summary>
        /// CRÍTICO: Desparenta las ghost hands del XR Origin.
        /// Esto evita que sigan el movimiento del headset/tracking.
        /// </summary>
        private void DetachFromXROrigin()
        {
            if (leftGhostHand != null)
            {
                // Desparenta del XR Origin, convirtiéndola en un objeto independiente en la escena
                leftGhostHand.transform.SetParent(null, true);
                if (showDebugLogs)
                    Debug.Log("GhostHandPlayer: LeftGhostHand desparentada del XR Origin.");
            }

            if (rightGhostHand != null)
            {
                // Desparenta del XR Origin, convirtiéndola en un objeto independiente en la escena
                rightGhostHand.transform.SetParent(null, true);
                if (showDebugLogs)
                    Debug.Log("GhostHandPlayer: RightGhostHand desparentada del XR Origin.");
            }
        }

        /// <summary>
        /// CRÍTICO: Desactiva cualquier componente que haga seguir el tracking de manos reales.
        /// </summary>
        private void DisableHandTracking()
        {
            if (leftGhostHand != null)
            {
                // Desactiva XRHandSkeletonDriver si existe
                var leftDriver = leftGhostHand.GetComponent<UnityEngine.XR.Hands.XRHandSkeletonDriver>();
                if (leftDriver != null)
                {
                    leftDriver.enabled = false;
                    if (showDebugLogs)
                        Debug.Log("GhostHandPlayer: XRHandSkeletonDriver desactivado en LeftGhostHand.");
                }

                // Desactiva XRHandTrackingEvents si existe
                var leftTracking = leftGhostHand.GetComponent<UnityEngine.XR.Hands.XRHandTrackingEvents>();
                if (leftTracking != null)
                {
                    leftTracking.enabled = false;
                    if (showDebugLogs)
                        Debug.Log("GhostHandPlayer: XRHandTrackingEvents desactivado en LeftGhostHand.");
                }
            }

            if (rightGhostHand != null)
            {
                // Desactiva XRHandSkeletonDriver si existe
                var rightDriver = rightGhostHand.GetComponent<UnityEngine.XR.Hands.XRHandSkeletonDriver>();
                if (rightDriver != null)
                {
                    rightDriver.enabled = false;
                    if (showDebugLogs)
                        Debug.Log("GhostHandPlayer: XRHandSkeletonDriver desactivado en RightGhostHand.");
                }

                // Desactiva XRHandTrackingEvents si existe
                var rightTracking = rightGhostHand.GetComponent<UnityEngine.XR.Hands.XRHandTrackingEvents>();
                if (rightTracking != null)
                {
                    rightTracking.enabled = false;
                    if (showDebugLogs)
                        Debug.Log("GhostHandPlayer: XRHandTrackingEvents desactivado en RightGhostHand.");
                }
            }
        }

        /// <summary>
        /// Posiciona las ghost hands en su ubicación fija en el espacio.
        /// </summary>
        private void PositionGhostHands()
        {
            Quaternion targetRotation = Quaternion.Euler(ghostHandsRotation);

            if (leftGhostHand != null)
            {
                leftGhostHand.transform.position = ghostHandsPosition + new Vector3(-0.1f, 0f, 0f);
                leftGhostHand.transform.rotation = targetRotation;
                leftGhostHand.transform.localScale = Vector3.one * ghostHandsScale;
            }

            if (rightGhostHand != null)
            {
                rightGhostHand.transform.position = ghostHandsPosition + new Vector3(0.1f, 0f, 0f);
                rightGhostHand.transform.rotation = targetRotation;
                rightGhostHand.transform.localScale = Vector3.one * ghostHandsScale;
            }

            if (showDebugLogs)
                Debug.Log($"GhostHandPlayer: Posicionadas en {ghostHandsPosition} con rotación {ghostHandsRotation}.");
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
                // NOTA: Gestos dinámicos no soportados actualmente
                Debug.LogWarning($"GhostHandPlayer: El signo '{currentSign.signName}' requiere movimiento pero no está soportado actualmente.");
                isPlaying = false;
                SetGhostHandsVisible(false);
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

            // Asegura que las ghost hands estén en su posición fija
            PositionGhostHands();

            // TODO: Aquí aplicarías la pose del Hand Shape/Pose al skeleton de las ghost hands
            // Por ahora, simplemente muestra las manos en su pose por defecto
            // En una implementación completa, necesitarías:
            // 1. Obtener los datos de joint positions del Hand Shape
            // 2. Aplicar las rotaciones de los joints manualmente (sin XRHandSkeletonDriver)
            // 3. Usar Animator o manipulación directa de transforms de los joints

            SetGhostHandsVisible(true);

            // Oculta después del tiempo especificado
            Invoke(nameof(StopPlaying), staticPoseDisplayTime);
        }

        // NOTA: Método deshabilitado - gestos dinámicos no soportados
        /*
        private void PlayDynamicGesture()
        {
            if (showDebugLogs)
                Debug.Log($"GhostHandPlayer: Reproduciendo gesto dinámico '{currentSign.signName}'");

            isPlaying = true;
            PositionGhostHands();
            Debug.LogWarning("GhostHandPlayer: Gestos dinámicos no soportados actualmente.");
        }
        */

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
