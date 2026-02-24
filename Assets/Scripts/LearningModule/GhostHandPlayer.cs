using System.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;
using ASL_LearnVR.Data;
using ASL_LearnVR.LearningModule.GuideHand;
using ASL_LearnVR.Feedback;

namespace ASL_LearnVR.LearningModule
{
    /// <summary>
    /// Controla las "ghost hands" que muestran visualmente como hacer un signo ASL.
    ///
    /// PROTOTIPO V1:
    /// - Solo la mano DERECHA hace el gesto
    /// - Hand izquierda permanece en estado neutro sobre la mesa
    /// - Secuencia: Neutro → Esperar 1.5s → Gesture → Mantener → Back a Neutro
    /// - El usuario puede repetir la animacion
    /// </summary>
    public class GhostHandPlayer : MonoBehaviour
    {
        [Header("Ghost Hand References")]
        [Tooltip("Reference al GameObject de la mano izquierda fantasma")]
        [SerializeField] private GameObject leftGhostHand;

        [Tooltip("Reference al GameObject de la mano derecha fantasma")]
        [SerializeField] private GameObject rightGhostHand;

        [Header("Pose Appliers")]
        [Tooltip("Component que aplica poses a la mano izquierda")]
        [SerializeField] private GuideHandPoseApplier leftPoseApplier;

        [Tooltip("Component que aplica poses a la mano derecha")]
        [SerializeField] private GuideHandPoseApplier rightPoseApplier;

        [Header("Right Hand Positioning (Active Hand)")]
        [Tooltip("Position de la mano derecha (preparada para hacer el gesto)")]
        [SerializeField] private Vector3 rightHandPosition = new Vector3(-0.07f, 0.85f, 0.5f);

        [Tooltip("Rotation de la mano derecha (preparada para el gesto)")]
        [SerializeField] private Vector3 rightHandNeutralRotation = new Vector3(-90f, -180f, 0f);

        [Header("Left Hand Positioning (Resting Hand)")]
        [Tooltip("Position de la mano izquierda (posada en la mesa)")]
        [SerializeField] private Vector3 leftHandPosition = new Vector3(-0.07f, 0.8f, 0.5f);

        [Tooltip("Rotation de la mano izquierda (posada en la mesa)")]
        [SerializeField] private Vector3 leftHandRestingRotation = new Vector3(0f, -180f, 0f);

        [Header("Scale")]
        [Tooltip("Scale de las ghost hands")]
        [SerializeField] private float ghostHandsScale = 1.0f;

        [Header("Visual Settings")]
        [Tooltip("Material para las ghost hands (semi-transparente)")]
        [SerializeField] private Material ghostHandMaterial;

        [Tooltip("Color de las ghost hands")]
        [SerializeField] private Color ghostHandColor = new Color(0f, 0.627451f, 1f, 1f);

        [Header("Animation Timing")]
        [Tooltip("Time en estado neutro antes de mostrar el gesto (segundos)")]
        [SerializeField] private float delayBeforeGesture = 0.5f;

        [Tooltip("Time que se muestra el gesto antes de volver a neutro (segundos)")]
        [SerializeField] private float gestureDisplayTime = 3f;

        [Tooltip("Time en neutro despues del gesto antes de ocultar (segundos)")]
        [SerializeField] private float neutralAfterGestureTime = 1f;

        [Header("Auto Setup")]
        [Tooltip("Intentar configurar automaticamente los pose appliers al iniciar")]
        [SerializeField] private bool autoSetupPoseAppliers = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        [Tooltip("Si esta active, las manos se muestran visibles al iniciar (para testing)")]
        [SerializeField] private bool showOnStart = true;

        [Header("Fade Settings")]
        [Tooltip("Duration del fade in/out en segundos")]
        [SerializeField] private float fadeDuration = 0.5f;

        // State interno
        private SignData currentSign;
        private bool isPlaying = false;
        private bool isShowingGesture = false;
        private bool isFading = false;
        private Coroutine currentAnimation;
        private Coroutine currentFade;
        private SkinnedMeshRenderer[] leftHandRenderers;
        private SkinnedMeshRenderer[] rightHandRenderers;

        /// <summary>
        /// True si las ghost hands estan reproduciendo actualmente.
        /// </summary>
        public bool IsPlaying => isPlaying;

        /// <summary>
        /// True si esta mostrando el gesto (no el estado neutro).
        /// </summary>
        public bool IsShowingGesture => isShowingGesture;

        void Awake()
        {
            Debug.Log($"[GhostHandPlayer] Awake - Left: {(leftGhostHand != null ? leftGhostHand.name : "NULL")}, Right: {(rightGhostHand != null ? rightGhostHand.name : "NULL")}");

            // SEGURIDAD: Solo procesar si las ghost hands son objetos separados
            // NO desactivar tracking si son las manos reales del usuario
            if (IsValidGhostHand(leftGhostHand) && IsValidGhostHand(rightGhostHand))
            {
                Debug.Log("[GhostHandPlayer] Hands guia VALIDAS detectadas. Disabling tracking...");
                // Desacopla las ghost hands del tracking XR
                DisableHandTracking();

                // DESPARENTA las ghost hands del XR Origin
                DetachFromXROrigin();
            }
            else
            {
                Debug.LogError("[GhostHandPlayer] ADVERTENCIA: Las ghost hands NO son valids (deben contener 'Guide' en el nombre). NO se procesaran correctamente.");
            }

            // Obtener renderers
            if (leftGhostHand != null)
                leftHandRenderers = leftGhostHand.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (rightGhostHand != null)
                rightHandRenderers = rightGhostHand.GetComponentsInChildren<SkinnedMeshRenderer>();

            // Aplica el material fantasma
            ApplyGhostMaterial();

            // Configure pose appliers automaticamente
            if (autoSetupPoseAppliers)
            {
                SetupPoseAppliers();
            }

            // Posicionar las manos
            PositionHands();

            // Mostrar u ocultar segun configuracion
            if (showOnStart)
            {
                // PROTOTIPO: Mostrar las manos visibles al iniciar
                SetGhostHandsVisible(true);
                Debug.Log("[GhostHandPlayer] Hands VISIBLES al inicio (showOnStart=true)");
            }
            else
            {
                SetGhostHandsVisible(false);
            }

            Debug.Log("[GhostHandPlayer] Inicializado - Prototipo V1 (solo mano derecha)");
        }

        void Start()
        {
            // Aplicar pose neutra despues de que todo este initialized
            if (showOnStart)
            {
                ApplyNeutralPose(rightPoseApplier);
                ApplyNeutralPose(leftPoseApplier);
                Debug.Log("[GhostHandPlayer] Pose neutra aplicada en Start()");
            }
        }

        /// <summary>
        /// Configura los GuideHandPoseApplier automaticamente.
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
                    Debug.Log("[GhostHandPlayer] Anadido GuideHandPoseApplier a LeftGhostHand");
                }
                leftPoseApplier.AutoMapJointsFromHierarchy(leftGhostHand.transform);

                // Validar que los joints se mapearon correctamente
                bool isValid = leftPoseApplier.ValidateJointMapping();
                Debug.Log($"[GhostHandPlayer] LeftHand joints mapeados. Valido: {isValid}");
            }

            // Right hand
            if (rightGhostHand != null && rightPoseApplier == null)
            {
                rightPoseApplier = rightGhostHand.GetComponent<GuideHandPoseApplier>();
                if (rightPoseApplier == null)
                {
                    rightPoseApplier = rightGhostHand.AddComponent<GuideHandPoseApplier>();
                    Debug.Log("[GhostHandPlayer] Anadido GuideHandPoseApplier a RightGhostHand");
                }
                rightPoseApplier.AutoMapJointsFromHierarchy(rightGhostHand.transform);

                // Validar que los joints se mapearon correctamente
                bool isValid = rightPoseApplier.ValidateJointMapping();
                Debug.Log($"[GhostHandPlayer] RightHand joints mapeados. Valido: {isValid}");
            }
        }

        /// <summary>
        /// Verifica si un objeto es una guide hand valid (no las manos reales del usuario).
        /// Las guide hands valids deben tener "Guide" en su nombre (no "Ghost" que son las del usuario).
        /// </summary>
        private bool IsValidGhostHand(GameObject hand)
        {
            if (hand == null) return false;

            string name = hand.name.ToLower();
            // SOLO aceptar si tiene "guide" - las "ghost" son las manos del usuario
            bool isValid = name.Contains("guide");

            if (!isValid)
            {
                Debug.LogWarning($"[GhostHandPlayer] '{hand.name}' NO es una guide hand valid. " +
                    "El nombre debe contener 'Guide' para ser procesado. NO se desactivara tracking.");
            }

            return isValid;
        }

        /// <summary>
        /// Desparenta las ghost hands del XR Origin.
        /// </summary>
        private void DetachFromXROrigin()
        {
            if (leftGhostHand != null)
            {
                leftGhostHand.transform.SetParent(null, true);
            }

            if (rightGhostHand != null)
            {
                rightGhostHand.transform.SetParent(null, true);
            }
        }

        /// <summary>
        /// Desactiva cualquier componente de tracking de manos reales.
        /// </summary>
        private void DisableHandTracking()
        {
            DisableTrackingOnHand(leftGhostHand);
            DisableTrackingOnHand(rightGhostHand);
        }

        private void DisableTrackingOnHand(GameObject hand)
        {
            if (hand == null) return;

            var driver = hand.GetComponent<XRHandSkeletonDriver>();
            if (driver != null) driver.enabled = false;

            var tracking = hand.GetComponent<XRHandTrackingEvents>();
            if (tracking != null) tracking.enabled = false;
        }

        /// <summary>
        /// Posiciona las manos en sus ubicaciones fijas.
        /// - Hand derecha: vertical, frente al usuario
        /// - Hand izquierda: posada sobre la mesa
        /// </summary>
        private void PositionHands()
        {
            // Hand derecha: vertical, palma hacia el usuario
            if (rightGhostHand != null)
            {
                rightGhostHand.transform.position = rightHandPosition;
                rightGhostHand.transform.rotation = Quaternion.Euler(rightHandNeutralRotation);
                rightGhostHand.transform.localScale = Vector3.one * ghostHandsScale;
            }

            // Hand izquierda: posada sobre la mesa
            if (leftGhostHand != null)
            {
                leftGhostHand.transform.position = leftHandPosition;
                leftGhostHand.transform.rotation = Quaternion.Euler(leftHandRestingRotation);
                leftGhostHand.transform.localScale = Vector3.one * ghostHandsScale;
            }

            if (showDebugLogs)
                Debug.Log($"[GhostHandPlayer] Hands posicionadas - Derecha: {rightHandPosition}, Izquierda: {leftHandPosition}");
        }

        /// <summary>
        /// Aplica el material fantasma a las manos.
        /// Fuerza modo OPACO para que las manos se vean solidas.
        /// </summary>
        private void ApplyGhostMaterial()
        {
            if (ghostHandMaterial == null) return;

            // Color solido (alpha 1)
            ghostHandMaterial.color = new Color(ghostHandColor.r, ghostHandColor.g, ghostHandColor.b, 1f);

            // Forzar Standard shader a modo Opaque
            ghostHandMaterial.SetFloat("_Mode", 0f);
            ghostHandMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            ghostHandMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            ghostHandMaterial.SetInt("_ZWrite", 1);
            ghostHandMaterial.DisableKeyword("_ALPHATEST_ON");
            ghostHandMaterial.DisableKeyword("_ALPHABLEND_ON");
            ghostHandMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            ghostHandMaterial.renderQueue = -1;

            if (leftHandRenderers != null)
            {
                foreach (var renderer in leftHandRenderers)
                    if (renderer != null) renderer.material = ghostHandMaterial;
            }

            if (rightHandRenderers != null)
            {
                foreach (var renderer in rightHandRenderers)
                    if (renderer != null) renderer.material = ghostHandMaterial;
            }
        }

        /// <summary>
        /// Aplica la pose neutra (mano abierta) a una mano.
        /// </summary>
        private void ApplyNeutralPose(GuideHandPoseApplier applier)
        {
            if (applier == null) return;

            // Pose neutra = mano abierta (5)
            var neutralPose = ASLPoseLibrary.GetPoseBySignName("5");
            if (neutralPose != null)
            {
                applier.ApplyPose(neutralPose);
            }
            else
            {
                applier.ResetToOriginal();
            }
        }

        /// <summary>
        /// METODO PRINCIPAL: Reproduce el signo con la secuencia completa.
        /// Neutro → Esperar 1.5s → Gesture → Mantener → Back a Neutro
        /// </summary>
        public void PlaySign(SignData sign)
        {
            if (sign == null)
            {
                Debug.LogError("[GhostHandPlayer] SignData es null.");
                return;
            }

            // Si ya esta reproduciendo, detener primero
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            currentSign = sign;
            currentAnimation = StartCoroutine(PlaySignSequence(sign));

            Debug.Log($"[GhostHandPlayer] Starting secuencia para '{sign.signName}'");
        }

        /// <summary>
        /// Repite la animacion del current sign.
        /// </summary>
        public void RepeatSign()
        {
            if (currentSign != null)
            {
                PlaySign(currentSign);
            }
            else
            {
                Debug.LogWarning("[GhostHandPlayer] No hay current sign para repetir.");
            }
        }

        /// <summary>
        /// Corrutina principal: secuencia de animacion del gesto.
        /// </summary>
        private IEnumerator PlaySignSequence(SignData sign)
        {
            Debug.Log($"[GhostHandPlayer] ========== INICIANDO SECUENCIA para '{sign.signName}' ==========");

            isPlaying = true;
            isShowingGesture = false;

            // 1. Mostrar manos y posicionarlas
            Debug.Log("[GhostHandPlayer] Paso 1: Posicionando manos...");
            PositionHands();
            SetGhostHandsVisible(true);

            // 2. Aplicar pose NEUTRA a ambas manos
            Debug.Log("[GhostHandPlayer] Paso 2: Aplicando pose NEUTRA...");
            ApplyNeutralPose(rightPoseApplier);
            ApplyNeutralPose(leftPoseApplier);

            Debug.Log($"[GhostHandPlayer] Paso 3: Esperando {delayBeforeGesture}s antes de mostrar gesto...");

            // 3. Esperar antes de mostrar el gesto
            yield return new WaitForSeconds(delayBeforeGesture);

            Debug.Log("[GhostHandPlayer] Paso 4: Delay completed, aplicando GESTO...");

            // 4. Aplicar el GESTO solo a la mano DERECHA
            isShowingGesture = true;

            // Comprobar si es una pose animada (J, Z, comunicacion basica)
            var animatedPose = ASLPoseLibrary.GetAnimatedPose(sign.signName);
            if (animatedPose != null && rightPoseApplier != null)
            {
                // Comprobar si requiere ambas manos
                bool isDoubleHanded = ASLPoseLibrary.IsDoubleHandedSign(sign.signName);

                if (isDoubleHanded && leftPoseApplier != null)
                {
                    Debug.Log($"[GhostHandPlayer] Pose ANIMADA DOBLE MANO '{animatedPose.poseName}' found.");
                    var leftAnimatedPose = ASLPoseLibrary.GetAnimatedPoseLeftHand(sign.signName) ?? animatedPose;

                    // Posicionar mano izquierda en posicion activa (vertical)
                    PositionLeftHandActive();
                    yield return PlayDualAnimatedPoseSequence(animatedPose, rightPoseApplier, leftAnimatedPose, leftPoseApplier);
                    // Devolver mano izquierda a posicion de descanso
                    PositionLeftHandResting();
                }
                else
                {
                    Debug.Log($"[GhostHandPlayer] Pose ANIMADA '{animatedPose.poseName}' found. Reproduciendo secuencia...");
                    yield return PlayAnimatedPoseSequence(animatedPose, rightPoseApplier);
                }
            }
            else
            {
                // Pose estatica normal
                Debug.Log($"[GhostHandPlayer] Searching pose para '{sign.signName}' en ASLPoseLibrary...");
                var pose = ASLPoseLibrary.GetPoseBySignName(sign.signName);

                if (pose != null && rightPoseApplier != null)
                {
                    Debug.Log($"[GhostHandPlayer] Pose '{pose.poseName}' found. Aplicando a mano derecha...");
                    rightPoseApplier.ApplyPose(pose);
                    Debug.Log($"[GhostHandPlayer] GESTURE '{sign.signName}' APPLIED!");
                }
                else
                {
                    Debug.LogError($"[GhostHandPlayer] ERROR: No found pose para '{sign.signName}'. " +
                        $"pose={pose}, rightPoseApplier={rightPoseApplier}");
                }

                // 5. Mantener el gesto visible (solo para poses estaticas)
                yield return new WaitForSeconds(gestureDisplayTime);
            }

            // 6. Back a NEUTRO (ambas manos)
            isShowingGesture = false;
            ApplyNeutralPose(rightPoseApplier);
            ApplyNeutralPose(leftPoseApplier);

            if (showDebugLogs)
                Debug.Log("[GhostHandPlayer] Volviendo a estado NEUTRO");

            // 7. Esperar un momento en neutro
            yield return new WaitForSeconds(neutralAfterGestureTime);

            // 8. Fin de la secuencia (las manos siguen visibles en neutro)
            isPlaying = false;
            currentAnimation = null;

            Debug.Log($"[GhostHandPlayer] ========== SECUENCIA COMPLETADA para '{sign.signName}' ==========");
        }

        /// <summary>
        /// Corrutina para reproducir una secuencia animada de poses (J, Z).
        /// Interpola entre keyframes frame a frame y mantiene la pose final.
        /// </summary>
        private IEnumerator PlayAnimatedPoseSequence(AnimatedPoseSequence sequence, GuideHandPoseApplier applier)
        {
            float elapsed = 0f;
            float totalDuration = sequence.Duration;

            Debug.Log($"[GhostHandPlayer] Reproduciendo secuencia animada '{sequence.poseName}' ({totalDuration}s, {sequence.keyframes.Length} keyframes)");

            // Reproducir la secuencia interpolando entre keyframes
            while (elapsed < totalDuration)
            {
                var pose = sequence.SampleAtTime(elapsed);
                applier.ApplyPoseImmediate(pose);
                elapsed += Time.deltaTime;
                yield return null; // Esperar un frame
            }

            // Aplicar pose final exacta
            applier.ApplyPoseImmediate(sequence.SampleAtTime(totalDuration));

            // Mantener la pose final visible
            yield return new WaitForSeconds(gestureDisplayTime);

            Debug.Log($"[GhostHandPlayer] Secuencia animada '{sequence.poseName}' completed.");
        }

        /// <summary>
        /// Corrutina para reproducir secuencias animadas en AMBAS manos simultaneamente.
        /// </summary>
        private IEnumerator PlayDualAnimatedPoseSequence(
            AnimatedPoseSequence rightSequence, GuideHandPoseApplier rightApplier,
            AnimatedPoseSequence leftSequence, GuideHandPoseApplier leftApplier)
        {
            float elapsed = 0f;
            float totalDuration = Mathf.Max(rightSequence.Duration, leftSequence.Duration);

            Debug.Log($"[GhostHandPlayer] Reproduciendo secuencia DOBLE MANO '{rightSequence.poseName}' ({totalDuration}s)");

            while (elapsed < totalDuration)
            {
                rightApplier.ApplyPoseImmediate(rightSequence.SampleAtTime(elapsed));
                leftApplier.ApplyPoseImmediate(leftSequence.SampleAtTime(elapsed));
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Aplicar poses finales exactas
            rightApplier.ApplyPoseImmediate(rightSequence.SampleAtTime(rightSequence.Duration));
            leftApplier.ApplyPoseImmediate(leftSequence.SampleAtTime(leftSequence.Duration));

            yield return new WaitForSeconds(gestureDisplayTime);

            Debug.Log($"[GhostHandPlayer] Secuencia doble mano '{rightSequence.poseName}' completed.");
        }

        /// <summary>
        /// Posiciona la mano izquierda en posicion activa (vertical, junto a la derecha).
        /// </summary>
        private void PositionLeftHandActive()
        {
            if (leftGhostHand == null) return;

            // Posicionar junto a la mano derecha pero en el lado izquierdo (X mas negativo)
            leftGhostHand.transform.position = new Vector3(
                rightHandPosition.x - 0.14f,  // A la izquierda de la mano derecha
                rightHandPosition.y,
                rightHandPosition.z
            );
            leftGhostHand.transform.rotation = Quaternion.Euler(rightHandNeutralRotation);

            if (showDebugLogs)
                Debug.Log($"[GhostHandPlayer] Hand izquierda posicionada ACTIVA en {leftGhostHand.transform.position}");
        }

        /// <summary>
        /// Devuelve la mano izquierda a su posicion de descanso (sobre la mesa).
        /// </summary>
        private void PositionLeftHandResting()
        {
            if (leftGhostHand == null) return;

            leftGhostHand.transform.position = leftHandPosition;
            leftGhostHand.transform.rotation = Quaternion.Euler(leftHandRestingRotation);

            if (showDebugLogs)
                Debug.Log("[GhostHandPlayer] Hand izquierda devuelta a posicion de DESCANSO");
        }

        /// <summary>
        /// Muestra la guia persistente (sin auto-ocultar).
        /// Util para el modo de practica continua.
        /// </summary>
        public void ShowPersistentGuide(SignData sign)
        {
            if (sign == null)
            {
                Debug.LogError("[GhostHandPlayer] SignData es null.");
                return;
            }

            // Detener cualquier animacion en curso
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }

            currentSign = sign;
            isPlaying = true;

            // Posicionar y mostrar
            PositionHands();
            SetGhostHandsVisible(true);

            // Hand izquierda siempre en neutro
            ApplyNeutralPose(leftPoseApplier);

            // Iniciar la secuencia
            currentAnimation = StartCoroutine(PlaySignSequence(sign));

            if (showDebugLogs)
                Debug.Log($"[GhostHandPlayer] Guia persistente para '{sign.signName}'");
        }

        /// <summary>
        /// Oculta las ghost hands y detiene la reproduccion.
        /// </summary>
        public void Hide()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }

            isPlaying = false;
            isShowingGesture = false;
            SetGhostHandsVisible(false);

            if (showDebugLogs)
                Debug.Log("[GhostHandPlayer] Oculto");
        }

        /// <summary>
        /// Detiene la reproduccion y oculta las ghost hands.
        /// </summary>
        public void StopPlaying()
        {
            Hide();
        }

        /// <summary>
        /// Oculta la guia persistente.
        /// </summary>
        public void HidePersistentGuide()
        {
            Hide();
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
        /// Aplica una pose por nombre (para testing).
        /// </summary>
        public void ApplyPoseByName(string poseName)
        {
            var pose = ASLPoseLibrary.GetPoseBySignName(poseName);

            if (pose == null)
            {
                Debug.LogWarning($"[GhostHandPlayer] No found pose para '{poseName}'");
                return;
            }

            // Solo mano derecha muestra el gesto
            if (rightPoseApplier != null)
                rightPoseApplier.ApplyPose(pose);

            // Hand izquierda siempre en neutro
            ApplyNeutralPose(leftPoseApplier);
        }

        /// <summary>
        /// Resetea ambas manos a pose neutra.
        /// </summary>
        public void ResetPose()
        {
            ApplyNeutralPose(rightPoseApplier);
            ApplyNeutralPose(leftPoseApplier);
        }

        /// <summary>
        /// Obtiene el GuideHandPoseApplier de la mano derecha.
        /// </summary>
        public GuideHandPoseApplier RightPoseApplier => rightPoseApplier;

        /// <summary>
        /// Obtiene el GuideHandPoseApplier de la mano izquierda.
        /// </summary>
        public GuideHandPoseApplier LeftPoseApplier => leftPoseApplier;

        /// <summary>
        /// Configura que mano mostrar.
        /// </summary>
        public void SetHandsToShow(bool showLeft, bool showRight)
        {
            if (leftGhostHand != null)
                leftGhostHand.SetActive(showLeft);

            if (rightGhostHand != null)
                rightGhostHand.SetActive(showRight);
        }

        // Methods legacy para compatibilidad
        public void ApplyPoseFromProfile(FingerConstraintProfile profile)
        {
            if (profile == null) return;
            var pose = ASLPoseLibrary.FromConstraintProfile(profile);
            if (rightPoseApplier != null)
                rightPoseApplier.ApplyPose(pose);
        }

        #region Fade In/Out para practice mode

        /// <summary>
        /// True si las manos estan en transicion de fade.
        /// </summary>
        public bool IsFading => isFading;

        /// <summary>
        /// Fade out de las guide hands (para entrar en practice mode).
        /// Las manos se desvanecen en fadeDuration segundos y luego se desactivan.
        /// </summary>
        public void FadeOut()
        {
            // Detener animacion en curso
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }
            isPlaying = false;
            isShowingGesture = false;

            // Detener fade anterior si existe
            if (currentFade != null)
            {
                StopCoroutine(currentFade);
                currentFade = null;
            }

            currentFade = StartCoroutine(FadeCoroutine(fadeOut: true));
        }

        /// <summary>
        /// Fade in de las guide hands (para salir del practice mode).
        /// Las manos se activan y aparecen en fadeDuration segundos.
        /// NO reproduce animacion automaticamente - el usuario debe dar a "Repeat".
        /// </summary>
        public void FadeIn()
        {
            // Detener fade anterior si existe
            if (currentFade != null)
            {
                StopCoroutine(currentFade);
                currentFade = null;
            }

            currentFade = StartCoroutine(FadeCoroutine(fadeOut: false));
        }

        /// <summary>
        /// Actualiza el current sign sin reproducir animacion.
        /// Util cuando se navega entre signos en practice mode.
        /// </summary>
        public void SetCurrentSign(SignData sign)
        {
            currentSign = sign;
        }

        /// <summary>
        /// Establece la visibilidad inmediata sin fade.
        /// </summary>
        public void SetVisibilityImmediate(bool visible)
        {
            SetGhostHandsVisible(visible);
        }

        private IEnumerator FadeCoroutine(bool fadeOut)
        {
            isFading = true;

            if (!fadeOut)
            {
                // Fade in: activar, posicionar, aplicar pose
                SetGhostHandsVisible(true);
                PositionHands();

                if (currentSign != null && rightPoseApplier != null)
                {
                    var pose = ASLPoseLibrary.GetPoseBySignName(currentSign.signName);
                    if (pose != null)
                        rightPoseApplier.ApplyPose(pose);
                    else
                        ApplyNeutralPose(rightPoseApplier);
                }
                else
                {
                    ApplyNeutralPose(rightPoseApplier);
                }
                ApplyNeutralPose(leftPoseApplier);
            }
            else
            {
                // Fade out: desactivar
                SetGhostHandsVisible(false);
            }

            yield return null;

            isFading = false;
            currentFade = null;

            Debug.Log($"[GhostHandPlayer] Fade {(fadeOut ? "OUT" : "IN")} completed");
        }

        #endregion
    }
}
