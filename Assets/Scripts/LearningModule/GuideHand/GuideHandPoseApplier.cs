using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using ASL_LearnVR.Data;
using ASL_LearnVR.Feedback;

namespace ASL_LearnVR.LearningModule.GuideHand
{
    /// <summary>
    /// Aplica poses de mano a una jerarquia de transforms (Ghost Hand).
    /// Este componente toma una mano visual (mesh + skeleton) desacoplada del tracking
    /// y la posiciona segun datos de pose objetivo.
    ///
    /// IMPORTANTE: Este script asume que la ghost hand ya esta desacoplada del XRHandSubsystem.
    /// El XRHandSkeletonDriver y XRHandTrackingEvents deben estar desactivados.
    /// </summary>
    public class GuideHandPoseApplier : MonoBehaviour
    {
        [Header("Joint References")]
        [Tooltip("Transform del Wrist (raiz de la mano)")]
        [SerializeField] private Transform wristTransform;

        [Tooltip("Transform del Palm")]
        [SerializeField] private Transform palmTransform;

        [Header("Thumb Joints")]
        [SerializeField] private Transform thumbMetacarpal;
        [SerializeField] private Transform thumbProximal;
        [SerializeField] private Transform thumbDistal;
        [SerializeField] private Transform thumbTip;

        [Header("Index Joints")]
        [SerializeField] private Transform indexMetacarpal;
        [SerializeField] private Transform indexProximal;
        [SerializeField] private Transform indexIntermediate;
        [SerializeField] private Transform indexDistal;
        [SerializeField] private Transform indexTip;

        [Header("Middle Joints")]
        [SerializeField] private Transform middleMetacarpal;
        [SerializeField] private Transform middleProximal;
        [SerializeField] private Transform middleIntermediate;
        [SerializeField] private Transform middleDistal;
        [SerializeField] private Transform middleTip;

        [Header("Ring Joints")]
        [SerializeField] private Transform ringMetacarpal;
        [SerializeField] private Transform ringProximal;
        [SerializeField] private Transform ringIntermediate;
        [SerializeField] private Transform ringDistal;
        [SerializeField] private Transform ringTip;

        [Header("Pinky Joints")]
        [SerializeField] private Transform pinkyMetacarpal;
        [SerializeField] private Transform pinkyProximal;
        [SerializeField] private Transform pinkyIntermediate;
        [SerializeField] private Transform pinkyDistal;
        [SerializeField] private Transform pinkyTip;

        [Header("Rotation Settings")]
        [Tooltip("Angle maximo de flexion para dedos (proximal/intermediate/distal)")]
        [SerializeField] private float maxFingerFlexAngle = 90f;

        [Tooltip("Angle maximo de flexion para el pulgar")]
        [SerializeField] private float maxThumbFlexAngle = 70f;

        [Tooltip("Eje de rotacion para flexion de dedos (local)")]
        [SerializeField] private Vector3 fingerFlexAxis = Vector3.right;

        [Tooltip("Eje de rotacion para abduccion/spread (local)")]
        [SerializeField] private Vector3 fingerSpreadAxis = Vector3.up;

        [Tooltip("Eje de rotacion para abduccion del pulgar (local)")]
        [SerializeField] private Vector3 thumbAbductionAxis = Vector3.forward;

        [Header("Animation")]
        [Tooltip("Speed de transicion entre poses")]
        [SerializeField] private float transitionSpeed = 5f;

        [Tooltip("Usar interpolacion suave para transiciones")]
        [SerializeField] private bool smoothTransitions = true;

        [Header("Handedness")]
        [Tooltip("Lateralidad de esta mano")]
        [SerializeField] private Handedness handedness = Handedness.Right;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // State actual
        private HandPoseData currentPose;
        private HandPoseData targetPose;
        private float transitionProgress = 1f;
        private bool isInitialized = false;

        // Cache de rotaciones y posicion originales
        private Dictionary<Transform, Quaternion> originalRotations = new Dictionary<Transform, Quaternion>();
        private Vector3 originalWristPosition;

        /// <summary>
        /// True si la guide hand esta en transicion entre poses.
        /// </summary>
        public bool IsTransitioning => transitionProgress < 1f;

        /// <summary>
        /// Current pose aplicada.
        /// </summary>
        public string CurrentPoseName => currentPose?.poseName ?? "None";

        void Awake()
        {
            // NO inicializamos aqui - esperamos a que los joints esten mapeados
            // La inicializacion se hace en Initialize() cuando se llama ApplyPose
            // o despues de AutoMapJointsFromHierarchy()
        }

        /// <summary>
        /// Inicializa el componente guardando las rotaciones originales.
        /// Puede llamarse multiples veces con forceReinitialize para actualizar despues de mapear joints.
        /// </summary>
        public void Initialize(bool forceReinitialize = false)
        {
            if (isInitialized && !forceReinitialize)
                return;

            // Limpiar cache anterior si estamos reinicializando
            if (forceReinitialize)
            {
                originalRotations.Clear();
                isInitialized = false;
            }

            // Guardar posicion y rotaciones originales de todos los joints
            if (wristTransform != null)
                originalWristPosition = wristTransform.localPosition;
            CacheOriginalRotation(wristTransform);
            CacheOriginalRotation(palmTransform);

            // Thumb
            CacheOriginalRotation(thumbMetacarpal);
            CacheOriginalRotation(thumbProximal);
            CacheOriginalRotation(thumbDistal);

            // Index
            CacheOriginalRotation(indexMetacarpal);
            CacheOriginalRotation(indexProximal);
            CacheOriginalRotation(indexIntermediate);
            CacheOriginalRotation(indexDistal);

            // Middle
            CacheOriginalRotation(middleMetacarpal);
            CacheOriginalRotation(middleProximal);
            CacheOriginalRotation(middleIntermediate);
            CacheOriginalRotation(middleDistal);

            // Ring
            CacheOriginalRotation(ringMetacarpal);
            CacheOriginalRotation(ringProximal);
            CacheOriginalRotation(ringIntermediate);
            CacheOriginalRotation(ringDistal);

            // Pinky
            CacheOriginalRotation(pinkyMetacarpal);
            CacheOriginalRotation(pinkyProximal);
            CacheOriginalRotation(pinkyIntermediate);
            CacheOriginalRotation(pinkyDistal);

            // Inicializar con pose abierta
            currentPose = HandPoseData.OpenHand();
            targetPose = currentPose;

            isInitialized = true;

            if (showDebugLogs)
                Debug.Log($"[GuideHandPoseApplier] Inicializado con {originalRotations.Count} joints");
        }

        private void CacheOriginalRotation(Transform joint)
        {
            if (joint != null && !originalRotations.ContainsKey(joint))
            {
                originalRotations[joint] = joint.localRotation;
            }
        }

        void Update()
        {
            if (!isInitialized || targetPose == null)
                return;

            if (smoothTransitions && transitionProgress < 1f)
            {
                transitionProgress += Time.deltaTime * transitionSpeed;
                transitionProgress = Mathf.Clamp01(transitionProgress);

                // Interpolar entre pose actual y objetivo
                var interpolatedPose = HandPoseData.Lerp(currentPose, targetPose, transitionProgress);
                ApplyPoseImmediate(interpolatedPose);

                if (transitionProgress >= 1f)
                {
                    currentPose = targetPose;
                }
            }
        }

        /// <summary>
        /// Aplica una pose de mano con transicion suave.
        /// </summary>
        public void ApplyPose(HandPoseData pose)
        {
            if (pose == null)
            {
                Debug.LogWarning("[GuideHandPoseApplier] ApplyPose llamado con pose null");
                return;
            }

            if (!isInitialized)
            {
                Debug.Log("[GuideHandPoseApplier] No initialized, inicializando ahora...");
                Initialize(forceReinitialize: false);
            }

            // Verificar que tenemos rotaciones cacheadas
            if (originalRotations.Count == 0)
            {
                Debug.LogError($"[GuideHandPoseApplier] ERROR: No hay rotaciones originales cacheadas. " +
                    $"Joints mapeados: {CountMappedJoints()}. La pose NO se aplicara correctamente.");
            }

            targetPose = pose;

            if (smoothTransitions)
            {
                transitionProgress = 0f;
                Debug.Log($"[GuideHandPoseApplier] Starting transicion suave hacia pose: {pose.poseName}");
            }
            else
            {
                currentPose = pose;
                transitionProgress = 1f;
                ApplyPoseImmediate(pose);
            }

            if (showDebugLogs)
                Debug.Log($"[GuideHandPoseApplier] Aplicando pose: {pose.poseName} " +
                    $"(joints: {CountMappedJoints()}, rotaciones cacheadas: {originalRotations.Count})");
        }

        /// <summary>
        /// Aplica una pose de mano desde un SignData.
        /// </summary>
        public void ApplyPoseFromSign(SignData signData)
        {
            if (signData == null)
                return;

            var pose = ASLPoseLibrary.GetPoseBySignName(signData.signName);
            ApplyPose(pose);
        }

        /// <summary>
        /// Aplica una pose de mano desde un nombre de signo.
        /// </summary>
        public void ApplyPoseByName(string signName)
        {
            var pose = ASLPoseLibrary.GetPoseBySignName(signName);
            ApplyPose(pose);
        }

        /// <summary>
        /// Aplica una pose de mano desde un FingerConstraintProfile.
        /// </summary>
        public void ApplyPoseFromProfile(FingerConstraintProfile profile)
        {
            if (profile == null)
                return;

            var pose = ASLPoseLibrary.FromConstraintProfile(profile);
            ApplyPose(pose);
        }

        /// <summary>
        /// Aplica una pose inmediatamente sin transicion.
        /// </summary>
        public void ApplyPoseImmediate(HandPoseData pose)
        {
            if (pose == null)
                return;

            // Aplicar rotacion y posicion de muneca
            ApplyWristRotation(pose.wristRotationOffset);
            ApplyWristPosition(pose.wristPositionOffset);

            // Aplicar pose del pulgar
            ApplyThumbPose(pose.thumb);

            // Aplicar pose de cada dedo
            ApplyFingerPose(
                pose.index,
                indexMetacarpal,
                indexProximal,
                indexIntermediate,
                indexDistal
            );

            ApplyFingerPose(
                pose.middle,
                middleMetacarpal,
                middleProximal,
                middleIntermediate,
                middleDistal
            );

            ApplyFingerPose(
                pose.ring,
                ringMetacarpal,
                ringProximal,
                ringIntermediate,
                ringDistal
            );

            ApplyFingerPose(
                pose.pinky,
                pinkyMetacarpal,
                pinkyProximal,
                pinkyIntermediate,
                pinkyDistal
            );
        }

        /// <summary>
        /// Resetea la mano a su pose original (abierta).
        /// </summary>
        public void ResetToOriginal()
        {
            foreach (var kvp in originalRotations)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.localRotation = kvp.Value;
                }
            }

            if (wristTransform != null)
                wristTransform.localPosition = originalWristPosition;

            currentPose = HandPoseData.OpenHand();
            targetPose = currentPose;
            transitionProgress = 1f;
        }

        private void ApplyWristRotation(Vector3 rotationOffset)
        {
            if (wristTransform == null)
                return;

            if (originalRotations.TryGetValue(wristTransform, out Quaternion originalRot))
            {
                wristTransform.localRotation = originalRot * Quaternion.Euler(rotationOffset);
            }
        }

        private void ApplyWristPosition(Vector3 positionOffset)
        {
            if (wristTransform == null)
                return;

            wristTransform.localPosition = originalWristPosition + positionOffset;
        }

        private void ApplyThumbPose(ThumbPoseData thumbPose)
        {
            // El pulgar tiene una estructura diferente y ejes de rotacion mas complejos
            // Metacarpal: abduccion + twist (rotacion axial de todo el dedo)
            if (thumbMetacarpal != null && originalRotations.TryGetValue(thumbMetacarpal, out Quaternion metaOriginal))
            {
                float flexAngle = thumbPose.metacarpalCurl * maxThumbFlexAngle * 0.5f;
                float abductAngle = thumbPose.abductionAngle;
                float twistAngle = thumbPose.distalTwist;
                float pitchAngle = thumbPose.thumbPitch;

                Quaternion flexRot = Quaternion.AngleAxis(flexAngle, fingerFlexAxis);
                Quaternion abductRot = Quaternion.AngleAxis(abductAngle, thumbAbductionAxis);
                Quaternion twistRot = Quaternion.AngleAxis(twistAngle, Vector3.forward);
                Quaternion pitchRot = Quaternion.AngleAxis(pitchAngle, Vector3.right); // Hacia/desde usuario

                thumbMetacarpal.localRotation = metaOriginal * abductRot * pitchRot * twistRot * flexRot;
            }

            // Proximal: flexion + oposicion
            if (thumbProximal != null && originalRotations.TryGetValue(thumbProximal, out Quaternion proxOriginal))
            {
                float flexAngle = thumbPose.proximalCurl * maxThumbFlexAngle;
                float oppAngle = thumbPose.oppositionAngle;

                Quaternion flexRot = Quaternion.AngleAxis(flexAngle, fingerFlexAxis);
                Quaternion oppRot = Quaternion.AngleAxis(oppAngle, Vector3.up);

                thumbProximal.localRotation = proxOriginal * oppRot * flexRot;
            }

            // Distal: solo flexion (el twist se aplica en metacarpal para rotar todo el dedo)
            if (thumbDistal != null && originalRotations.TryGetValue(thumbDistal, out Quaternion distOriginal))
            {
                float flexAngle = thumbPose.distalCurl * maxThumbFlexAngle;
                Quaternion flexRot = Quaternion.AngleAxis(flexAngle, fingerFlexAxis);

                thumbDistal.localRotation = distOriginal * flexRot;
            }
        }

        private void ApplyFingerPose(
            FingerPoseData fingerPose,
            Transform metacarpal,
            Transform proximal,
            Transform intermediate,
            Transform distal)
        {
            // Metacarpal: ligera flexion + spread
            if (metacarpal != null && originalRotations.TryGetValue(metacarpal, out Quaternion metaOriginal))
            {
                float flexAngle = fingerPose.metacarpalCurl * maxFingerFlexAngle * 0.3f;
                float spreadAngle = fingerPose.spreadAngle;

                Quaternion flexRot = Quaternion.AngleAxis(flexAngle, fingerFlexAxis);
                Quaternion spreadRot = Quaternion.AngleAxis(spreadAngle, fingerSpreadAxis);

                metacarpal.localRotation = metaOriginal * spreadRot * flexRot;
            }

            // Proximal: principal flexion
            if (proximal != null && originalRotations.TryGetValue(proximal, out Quaternion proxOriginal))
            {
                float flexAngle = fingerPose.proximalCurl * maxFingerFlexAngle;
                Quaternion flexRot = Quaternion.AngleAxis(flexAngle, fingerFlexAxis);

                proximal.localRotation = proxOriginal * flexRot;
            }

            // Intermediate: flexion
            if (intermediate != null && originalRotations.TryGetValue(intermediate, out Quaternion interOriginal))
            {
                float flexAngle = fingerPose.intermediateCurl * maxFingerFlexAngle;
                Quaternion flexRot = Quaternion.AngleAxis(flexAngle, fingerFlexAxis);

                intermediate.localRotation = interOriginal * flexRot;
            }

            // Distal: flexion
            if (distal != null && originalRotations.TryGetValue(distal, out Quaternion distOriginal))
            {
                float flexAngle = fingerPose.distalCurl * maxFingerFlexAngle * 0.8f;
                Quaternion flexRot = Quaternion.AngleAxis(flexAngle, fingerFlexAxis);

                distal.localRotation = distOriginal * flexRot;
            }
        }

        /// <summary>
        /// Intenta encontrar y asignar los joints automaticamente desde una jerarquia.
        /// Busca transforms con nombres que contengan los IDs de joints de XR Hands.
        /// </summary>
        public void AutoMapJointsFromHierarchy(Transform root)
        {
            if (root == null)
                return;

            var allTransforms = root.GetComponentsInChildren<Transform>(true);

            foreach (var t in allTransforms)
            {
                string name = t.name.ToLower();

                // Wrist y Palm
                if (name.Contains("wrist")) wristTransform = t;
                else if (name.Contains("palm")) palmTransform = t;

                // Thumb
                else if (name.Contains("thumb"))
                {
                    if (name.Contains("metacarpal")) thumbMetacarpal = t;
                    else if (name.Contains("proximal")) thumbProximal = t;
                    else if (name.Contains("distal") && !name.Contains("tip")) thumbDistal = t;
                    else if (name.Contains("tip")) thumbTip = t;
                }

                // Index
                else if (name.Contains("index"))
                {
                    if (name.Contains("metacarpal")) indexMetacarpal = t;
                    else if (name.Contains("proximal")) indexProximal = t;
                    else if (name.Contains("intermediate")) indexIntermediate = t;
                    else if (name.Contains("distal") && !name.Contains("tip")) indexDistal = t;
                    else if (name.Contains("tip")) indexTip = t;
                }

                // Middle
                else if (name.Contains("middle"))
                {
                    if (name.Contains("metacarpal")) middleMetacarpal = t;
                    else if (name.Contains("proximal")) middleProximal = t;
                    else if (name.Contains("intermediate")) middleIntermediate = t;
                    else if (name.Contains("distal") && !name.Contains("tip")) middleDistal = t;
                    else if (name.Contains("tip")) middleTip = t;
                }

                // Ring
                else if (name.Contains("ring"))
                {
                    if (name.Contains("metacarpal")) ringMetacarpal = t;
                    else if (name.Contains("proximal")) ringProximal = t;
                    else if (name.Contains("intermediate")) ringIntermediate = t;
                    else if (name.Contains("distal") && !name.Contains("tip")) ringDistal = t;
                    else if (name.Contains("tip")) ringTip = t;
                }

                // Pinky / Little
                else if (name.Contains("pinky") || name.Contains("little"))
                {
                    if (name.Contains("metacarpal")) pinkyMetacarpal = t;
                    else if (name.Contains("proximal")) pinkyProximal = t;
                    else if (name.Contains("intermediate")) pinkyIntermediate = t;
                    else if (name.Contains("distal") && !name.Contains("tip")) pinkyDistal = t;
                    else if (name.Contains("tip")) pinkyTip = t;
                }
            }

            if (showDebugLogs)
            {
                int mappedCount = CountMappedJoints();
                Debug.Log($"[GuideHandPoseApplier] Auto-mapeados {mappedCount} joints desde {root.name}");
            }

            // IMPORTANTE: Reinicializar para cachear las rotaciones originales de los joints recien mapeados
            Initialize(forceReinitialize: true);

            if (showDebugLogs)
            {
                Debug.Log($"[GuideHandPoseApplier] Rotaciones originales cacheadas: {originalRotations.Count}");
            }
        }

        private int CountMappedJoints()
        {
            int count = 0;
            if (wristTransform != null) count++;
            if (palmTransform != null) count++;
            if (thumbMetacarpal != null) count++;
            if (thumbProximal != null) count++;
            if (thumbDistal != null) count++;
            if (indexMetacarpal != null) count++;
            if (indexProximal != null) count++;
            if (indexIntermediate != null) count++;
            if (indexDistal != null) count++;
            if (middleMetacarpal != null) count++;
            if (middleProximal != null) count++;
            if (middleIntermediate != null) count++;
            if (middleDistal != null) count++;
            if (ringMetacarpal != null) count++;
            if (ringProximal != null) count++;
            if (ringIntermediate != null) count++;
            if (ringDistal != null) count++;
            if (pinkyMetacarpal != null) count++;
            if (pinkyProximal != null) count++;
            if (pinkyIntermediate != null) count++;
            if (pinkyDistal != null) count++;
            return count;
        }

        /// <summary>
        /// Valida que todos los joints necesarios esten assigneds.
        /// </summary>
        public bool ValidateJointMapping()
        {
            bool isValid = true;
            var missingJoints = new List<string>();

            // Joints criticos
            if (wristTransform == null) { missingJoints.Add("Wrist"); isValid = false; }
            if (thumbProximal == null) { missingJoints.Add("ThumbProximal"); isValid = false; }
            if (thumbDistal == null) { missingJoints.Add("ThumbDistal"); isValid = false; }
            if (indexProximal == null) { missingJoints.Add("IndexProximal"); isValid = false; }
            if (indexIntermediate == null) { missingJoints.Add("IndexIntermediate"); isValid = false; }
            if (indexDistal == null) { missingJoints.Add("IndexDistal"); isValid = false; }
            if (middleProximal == null) { missingJoints.Add("MiddleProximal"); isValid = false; }
            if (middleIntermediate == null) { missingJoints.Add("MiddleIntermediate"); isValid = false; }
            if (middleDistal == null) { missingJoints.Add("MiddleDistal"); isValid = false; }
            if (ringProximal == null) { missingJoints.Add("RingProximal"); isValid = false; }
            if (ringIntermediate == null) { missingJoints.Add("RingIntermediate"); isValid = false; }
            if (ringDistal == null) { missingJoints.Add("RingDistal"); isValid = false; }
            if (pinkyProximal == null) { missingJoints.Add("PinkyProximal"); isValid = false; }
            if (pinkyIntermediate == null) { missingJoints.Add("PinkyIntermediate"); isValid = false; }
            if (pinkyDistal == null) { missingJoints.Add("PinkyDistal"); isValid = false; }

            if (!isValid && showDebugLogs)
            {
                Debug.LogWarning($"[GuideHandPoseApplier] Joints faltantes: {string.Join(", ", missingJoints)}");
            }

            return isValid;
        }
    }
}
