using UnityEngine;
using UnityEngine.XR.Hands;
using ASL_LearnVR.Data;
using ASL_LearnVR.LearningModule.GuideHand;
using ASL_LearnVR.Feedback;

namespace ASL_LearnVR.LearningModule
{
    /// <summary>
    /// Controla las "ghost hands" que muestran visualmente cómo hacer un signo ASL.
    /// Para gestos estáticos: muestra la pose directamente usando GuideHandPoseApplier.
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

        [Header("Pose Appliers")]
        [Tooltip("Componente que aplica poses a la mano izquierda")]
        [SerializeField] private GuideHandPoseApplier leftPoseApplier;

        [Tooltip("Componente que aplica poses a la mano derecha")]
        [SerializeField] private GuideHandPoseApplier rightPoseApplier;

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

        [Header("Auto Setup")]
        [Tooltip("Intentar configurar automáticamente los pose appliers al iniciar")]
        [SerializeField] private bool autoSetupPoseAppliers = true;

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

            // Configurar pose appliers automáticamente si está habilitado
            if (autoSetupPoseAppliers)
            {
                SetupPoseAppliers();
            }

            // Oculta las ghost hands al inicio
            SetGhostHandsVisible(false);
        }

        /// <summary>
        /// Configura los GuideHandPoseApplier automáticamente.
        /// Intenta encontrar o crear los componentes y mapear los joints.
        /// </summary>
        private void SetupPoseAppliers()
        {
            // Left hand
            if (leftGhostHand != null && leftPoseApplier == null)
            {
                leftPoseApplier = leftGhostHand.GetComponent<GuideHandPoseApplier>();
                if (leftPoseApplier == null)
                {
                    leftPoseApplier = leftGhostHand.AddComponent<GuideHandPoseApplier>();
                }
                leftPoseApplier.AutoMapJointsFromHierarchy(leftGhostHand.transform);

                if (showDebugLogs)
                    Debug.Log("GhostHandPlayer: GuideHandPoseApplier configurado en LeftGhostHand");
            }

            // Right hand
            if (rightGhostHand != null && rightPoseApplier == null)
            {
                rightPoseApplier = rightGhostHand.GetComponent<GuideHandPoseApplier>();
                if (rightPoseApplier == null)
                {
                    rightPoseApplier = rightGhostHand.AddComponent<GuideHandPoseApplier>();
                }
                rightPoseApplier.AutoMapJointsFromHierarchy(rightGhostHand.transform);

                if (showDebugLogs)
                    Debug.Log("GhostHandPlayer: GuideHandPoseApplier configurado en RightGhostHand");
            }
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
        /// Usa GuideHandPoseApplier para aplicar la pose del signo a los joints.
        /// </summary>
        private void PlayStaticGesture()
        {
            if (showDebugLogs)
                Debug.Log($"GhostHandPlayer: Mostrando gesto estático '{currentSign.signName}'");

            isPlaying = true;

            // Asegura que las ghost hands estén en su posición fija
            PositionGhostHands();

            // Aplicar la pose del signo usando GuideHandPoseApplier
            ApplySignPose(currentSign);

            SetGhostHandsVisible(true);

            // Oculta después del tiempo especificado
            Invoke(nameof(StopPlaying), staticPoseDisplayTime);
        }

        /// <summary>
        /// Aplica la pose del signo a las ghost hands usando los pose appliers.
        /// </summary>
        private void ApplySignPose(SignData sign)
        {
            if (sign == null)
                return;

            // Obtener la pose desde la biblioteca ASL
            var pose = ASLPoseLibrary.GetPoseBySignName(sign.signName);

            if (pose == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"GhostHandPlayer: No se encontró pose para '{sign.signName}'");
                return;
            }

            // Aplicar a mano derecha (la más común para ASL)
            if (rightPoseApplier != null)
            {
                rightPoseApplier.ApplyPose(pose);

                if (showDebugLogs)
                    Debug.Log($"GhostHandPlayer: Pose '{pose.poseName}' aplicada a RightGhostHand");
            }

            // Aplicar a mano izquierda también (espejada)
            if (leftPoseApplier != null)
            {
                leftPoseApplier.ApplyPose(pose);

                if (showDebugLogs)
                    Debug.Log($"GhostHandPlayer: Pose '{pose.poseName}' aplicada a LeftGhostHand");
            }
        }

        /// <summary>
        /// Aplica una pose específica por nombre a las ghost hands.
        /// Útil para mostrar poses sin necesidad de un SignData.
        /// </summary>
        public void ApplyPoseByName(string poseName)
        {
            var pose = ASLPoseLibrary.GetPoseBySignName(poseName);

            if (pose == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"GhostHandPlayer: No se encontró pose para '{poseName}'");
                return;
            }

            if (rightPoseApplier != null)
                rightPoseApplier.ApplyPose(pose);

            if (leftPoseApplier != null)
                leftPoseApplier.ApplyPose(pose);
        }

        /// <summary>
        /// Aplica una pose desde un FingerConstraintProfile.
        /// Útil para mostrar exactamente la pose objetivo del feedback.
        /// </summary>
        public void ApplyPoseFromProfile(FingerConstraintProfile profile)
        {
            if (profile == null)
                return;

            var pose = ASLPoseLibrary.FromConstraintProfile(profile);

            if (rightPoseApplier != null)
                rightPoseApplier.ApplyPose(pose);

            if (leftPoseApplier != null)
                leftPoseApplier.ApplyPose(pose);

            if (showDebugLogs)
                Debug.Log($"GhostHandPlayer: Pose desde perfil '{profile.signName}' aplicada");
        }

        /// <summary>
        /// Resetea las ghost hands a su pose original (mano abierta).
        /// </summary>
        public void ResetPose()
        {
            if (rightPoseApplier != null)
                rightPoseApplier.ResetToOriginal();

            if (leftPoseApplier != null)
                leftPoseApplier.ResetToOriginal();
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

            // Resetear poses para la próxima vez
            ResetPose();

            if (showDebugLogs)
                Debug.Log("GhostHandPlayer: Reproducción detenida.");
        }

        /// <summary>
        /// Muestra las ghost hands con una pose específica sin límite de tiempo.
        /// Útil para mostrar la pose guía durante toda la práctica.
        /// </summary>
        public void ShowPersistentGuide(SignData sign)
        {
            if (sign == null)
            {
                Debug.LogError("GhostHandPlayer: SignData es null.");
                return;
            }

            currentSign = sign;
            isPlaying = true;

            // Cancelar cualquier auto-ocultación pendiente
            CancelInvoke(nameof(StopPlaying));

            PositionGhostHands();
            ApplySignPose(sign);
            SetGhostHandsVisible(true);

            if (showDebugLogs)
                Debug.Log($"GhostHandPlayer: Guía persistente mostrada para '{sign.signName}'");
        }

        /// <summary>
        /// Oculta la guía persistente.
        /// </summary>
        public void HidePersistentGuide()
        {
            CancelInvoke(nameof(StopPlaying));
            StopPlaying();
        }

        /// <summary>
        /// Configura qué mano mostrar (solo derecha, solo izquierda, o ambas).
        /// </summary>
        public void SetHandsToShow(bool showLeft, bool showRight)
        {
            if (leftGhostHand != null && isPlaying)
                leftGhostHand.SetActive(showLeft);

            if (rightGhostHand != null && isPlaying)
                rightGhostHand.SetActive(showRight);
        }

        /// <summary>
        /// Obtiene el GuideHandPoseApplier de la mano derecha para configuración avanzada.
        /// </summary>
        public GuideHandPoseApplier RightPoseApplier => rightPoseApplier;

        /// <summary>
        /// Obtiene el GuideHandPoseApplier de la mano izquierda para configuración avanzada.
        /// </summary>
        public GuideHandPoseApplier LeftPoseApplier => leftPoseApplier;

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
