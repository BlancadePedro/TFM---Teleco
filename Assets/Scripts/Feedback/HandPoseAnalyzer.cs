using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Analyzes the current hand pose against a FingerConstraintProfile.
    /// Genera errores por dedo (perFingerErrors) y un mensaje resumen.
    ///
    /// IMPORTANT: This analyzer does NOT replace global detection (XRHandShape.CheckConditions).
    /// It only explains WHY a gesture doesn't match, for pedagogical feedback.
    /// </summary>
    public class HandPoseAnalyzer : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Diccionario de perfiles de constraints por signo")]
        [SerializeField] private List<FingerConstraintProfile> constraintProfiles = new List<FingerConstraintProfile>();

        [Tooltip("XRHandTrackingEvents component for the hand to analyze")]
        [SerializeField] private XRHandTrackingEvents handTrackingEvents;

        [Tooltip("Handedness of the hand to analyze")]
        [SerializeField] private Handedness handedness = Handedness.Right;

        [Header("Analysis Settings")]
        [Tooltip("Tolerancia adicional para los rangos de curl (se suma al min y resta al max)")]
        [Range(0f, 0.2f)]
        [SerializeField] private float curlTolerance = 0.1f;

        [Header("Auto-load Profiles")]
        [Tooltip("Automatically load digit profiles (0-9) on start")]
        [SerializeField] private bool autoLoadDigitProfiles = true;

        [Tooltip("Automatically load alphabet letter profiles (A-Z) on start")]
        [SerializeField] private bool autoLoadAlphabetProfiles = true;

        [Header("Debug")]
        [Tooltip("Mostrar logs de debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Cache para evitar allocations
        private StaticGestureResult cachedResult = new StaticGestureResult();
        private List<FingerError> errorList = new List<FingerError>(5);

        // Cache de datos de mano
        private float[] currentCurlValues = new float[5];
        private Vector3[] currentFingerDirections = new Vector3[5];
        private bool[] hasFingerDirection = new bool[5];
        private bool hasValidHandData = false;
        private float[] lastValidCurlValues = new float[5];

        // Reference al XRHandSubsystem
        private XRHandSubsystem handSubsystem;

        // Mappings de XRHandJointID a dedos
        private static readonly XRHandJointID[] fingerTipJoints = new XRHandJointID[]
        {
            XRHandJointID.ThumbTip,
            XRHandJointID.IndexTip,
            XRHandJointID.MiddleTip,
            XRHandJointID.RingTip,
            XRHandJointID.LittleTip
        };

        private static readonly XRHandJointID[] fingerProximalJoints = new XRHandJointID[]
        {
            XRHandJointID.ThumbProximal,
            XRHandJointID.IndexProximal,
            XRHandJointID.MiddleProximal,
            XRHandJointID.RingProximal,
            XRHandJointID.LittleProximal
        };

        private static readonly XRHandJointID[] fingerIntermediateJoints = new XRHandJointID[]
        {
            XRHandJointID.ThumbDistal, // Thumb no tiene intermediate, usamos distal
            XRHandJointID.IndexIntermediate,
            XRHandJointID.MiddleIntermediate,
            XRHandJointID.RingIntermediate,
            XRHandJointID.LittleIntermediate
        };

        private static readonly XRHandJointID[] fingerDistalJoints = new XRHandJointID[]
        {
            XRHandJointID.ThumbDistal,
            XRHandJointID.IndexDistal,
            XRHandJointID.MiddleDistal,
            XRHandJointID.RingDistal,
            XRHandJointID.LittleDistal
        };

        void Start()
        {
            if (autoLoadDigitProfiles)
            {
                LoadDigitProfiles();
            }

            if (autoLoadAlphabetProfiles)
            {
                LoadAlphabetProfiles();
            }
        }

        /// <summary>
        /// Loads all predefined digit profiles (0-9).
        /// </summary>
        public void LoadDigitProfiles()
        {
            var digitProfiles = DigitConstraintProfiles.CreateAllDigitProfiles();
            foreach (var profile in digitProfiles)
            {
                RegisterProfile(profile);
            }

            if (showDebugLogs)
            {
                Debug.Log($"[HandPoseAnalyzer] Loaded {digitProfiles.Length} digit profiles");
            }
        }

        /// <summary>
        /// Carga todos los perfiles de letras del alfabeto (A-Z) predefinidos.
        /// </summary>
        public void LoadAlphabetProfiles()
        {
            var alphabetProfiles = AlphabetConstraintProfiles.CreateAllAlphabetProfiles();
            foreach (var profile in alphabetProfiles)
            {
                RegisterProfile(profile);
            }

            if (showDebugLogs)
            {
                Debug.Log($"[HandPoseAnalyzer] Cargados {alphabetProfiles.Length} perfiles de letras");
            }
        }

        /// <summary>
        /// Registers a constraint profile. Can be called from code or configured in Inspector.
        /// </summary>
        public void RegisterProfile(FingerConstraintProfile profile)
        {
            if (profile != null && !constraintProfiles.Contains(profile))
            {
                constraintProfiles.Add(profile);
                Debug.Log($"[HandPoseAnalyzer] Profile registrado: {profile.signName}");
            }
        }

        /// <summary>
        /// Obtiene el perfil de constraints para un signo por nombre.
        /// </summary>
        public FingerConstraintProfile GetProfile(string signName)
        {
            if (string.IsNullOrEmpty(signName))
                return null;

            foreach (var profile in constraintProfiles)
            {
                if (profile != null && profile.signName == signName)
                    return profile;
            }

            return null;
        }

        /// <summary>
        /// Analyzes the current hand pose against the profile of the specified sign.
        /// </summary>
        /// <param name="signData">El SignData del gesto objetivo</param>
        /// <param name="useGlobalMatch">Si true, usa XRHandShape.CheckConditions para isMatchGlobal</param>
        /// <returns>Analysis result with per-finger errors</returns>
        public StaticGestureResult Analyze(SignData signData, bool useGlobalMatch = true)
        {
            // Call the version that accepts external detection state
            // If not provided, false (not detected) will be used to avoid false positives
            return Analyze(signData, isDetectedByRecognizer: false, useGlobalMatch);
        }

        /// <summary>
        /// Analyzes the current hand pose against the profile of the specified sign.
        /// This version accepts the GestureRecognizer detection state to avoid false positives.
        /// </summary>
        /// <param name="signData">El SignData del gesto objetivo</param>
        /// <param name="isDetectedByRecognizer">True if the GestureRecognizer is actively detecting the sign</param>
        /// <param name="useGlobalMatch">Si true y isDetectedByRecognizer es false, analiza errores de dedos</param>
        /// <returns>Analysis result with per-finger errors</returns>
        public StaticGestureResult Analyze(SignData signData, bool isDetectedByRecognizer, bool useGlobalMatch = true)
        {
            cachedResult = new StaticGestureResult();
            errorList.Clear();

            if (signData == null)
            {
                cachedResult.summaryMessage = "No sign data provided";
                return cachedResult;
            }

            // Obtener perfil de constraints
            FingerConstraintProfile profile = GetProfile(signData.signName);

            // Actualizar datos de curl de la mano
            UpdateHandData();

            if (!hasValidHandData)
            {
                cachedResult.summaryMessage = "Hand not tracked";
                return cachedResult;
            }

            // The global match comes from the GestureRecognizer, we do NOT calculate it here
            // porque no podemos acceder correctamente a XRHandJointsUpdatedEventArgs
            cachedResult.isMatchGlobal = isDetectedByRecognizer;

            // If the recognizer says it is detected, it's a success
            if (isDetectedByRecognizer)
            {
                cachedResult.summaryMessage = $"Correct! '{signData.signName}' detected";
                cachedResult.majorErrorCount = 0;
                cachedResult.minorErrorCount = 0;
                cachedResult.perFingerErrors = new System.Collections.Generic.List<FingerError>();
                cachedResult.isMatchGlobal = true;
                cachedResult.matchScore = 1f;
                cachedResult.isNearMatch = false;

                if (showDebugLogs)
                {
                    Debug.Log($"[HandPoseAnalyzer] '{signData.signName}' detected by GestureRecognizer - SUCCESS");
                }
                return cachedResult;
            }

            // If no profile is defined, show a generic message
            if (profile == null)
            {
                if (showDebugLogs)
                    Debug.Log($"[HandPoseAnalyzer] No constraint profile for '{signData.signName}'");

                cachedResult.summaryMessage = $"Adjust your hand for '{signData.signName}'...";
                return cachedResult;
            }

            // Analyze each finger against the profile to show specific errors
            AnalyzeAllFingers(profile);

            // Generar mensaje resumen
            cachedResult.perFingerErrors = new System.Collections.Generic.List<FingerError>(errorList);
            cachedResult.majorErrorCount = CountErrors(Severity.Major);
            cachedResult.minorErrorCount = CountErrors(Severity.Minor);
            cachedResult.UpdateMatchScore();

            // Generar mensaje basado en errores founds
            if (cachedResult.majorErrorCount == 0 && cachedResult.minorErrorCount == 0)
            {
                // Sin errores detecteds por el perfil, pero el recognizer no lo detecta
                // This can happen if the profile is not well configured or the pose is not exact
                cachedResult.summaryMessage = "The gesture has not been fully recognized, please try again";
            }
            else
            {
                // Generate message with context of the sign being practiced
                string errorSummary = FeedbackMessages.GenerateSummary(cachedResult.perFingerErrors, 2);
                cachedResult.summaryMessage = $"Para '{signData.signName}':\n{errorSummary}";
            }

            if (showDebugLogs)
            {
                Debug.Log($"[HandPoseAnalyzer] Analysis of '{signData.signName}': " +
                         $"Detected={isDetectedByRecognizer}, Major={cachedResult.majorErrorCount}, Minor={cachedResult.minorErrorCount}");
            }

            return cachedResult;
        }

        // NOTA: EvaluateGlobalMatch, CheckShapeConditionsManual y CheckPoseConditionsManual fueron removeds
        // porque no podemos evaluar XRHandShape.CheckConditions sin XRHandJointsUpdatedEventArgs.
        // Global detection now comes directly from GestureRecognizer through the isDetectedByRecognizer parameter.

        /// <summary>
        /// Actualiza los valores de curl actuales de la mano.
        /// </summary>
        private void UpdateHandData()
        {
            hasValidHandData = false;

            var hand = GetCurrentHand();
            if (!hand.isTracked)
                return;

            // Calcular curl para cada dedo
            for (int i = 0; i < 5; i++)
            {
                currentCurlValues[i] = CalculateFingerCurl(hand, (Finger)i);
                lastValidCurlValues[i] = currentCurlValues[i];
                currentFingerDirections[i] = CalculateFingerDirection(hand, i, out bool hasDir);
                hasFingerDirection[i] = hasDir;
            }

            hasValidHandData = true;
        }

        /// <summary>
        /// Obtiene la mano actual del subsystem.
        /// </summary>
        private XRHand GetCurrentHand()
        {
            if (handSubsystem == null)
            {
                handSubsystem = XRGeneralSettings.Instance?
                    .Manager?
                    .activeLoader?
                    .GetLoadedSubsystem<XRHandSubsystem>();
            }

            if (handSubsystem == null)
                return default;

            return handedness == Handedness.Right ? handSubsystem.rightHand : handSubsystem.leftHand;
        }

        /// <summary>
        /// Calculates the curl value (0-1) for a specific finger.
        /// 0 = completamente extendido, 1 = completamente cerrado.
        /// </summary>
        private float CalculateFingerCurl(XRHand hand, Finger finger)
        {
            int fingerIndex = (int)finger;

            // Obtener joints del dedo
            var proximalJoint = hand.GetJoint(fingerProximalJoints[fingerIndex]);
            var intermediateJoint = hand.GetJoint(fingerIntermediateJoints[fingerIndex]);
            var distalJoint = hand.GetJoint(fingerDistalJoints[fingerIndex]);
            var tipJoint = hand.GetJoint(fingerTipJoints[fingerIndex]);

            if (!proximalJoint.TryGetPose(out Pose proximalPose) ||
                !intermediateJoint.TryGetPose(out Pose intermediatePose) ||
                !distalJoint.TryGetPose(out Pose distalPose) ||
                !tipJoint.TryGetPose(out Pose tipPose))
            {
                // Preserve the last valid value to avoid feedback jumps
                return lastValidCurlValues[fingerIndex] > 0f ? lastValidCurlValues[fingerIndex] : 0.5f;
            }

            // Direction based on positions to avoid dependency on device rotation
            Vector3 dirProxToInter = (intermediatePose.position - proximalPose.position).normalized;
            Vector3 dirInterToDistal = (distalPose.position - intermediatePose.position).normalized;
            Vector3 dirDistalToTip = (tipPose.position - distalPose.position).normalized;

            if (dirProxToInter.sqrMagnitude < 0.0001f || dirInterToDistal.sqrMagnitude < 0.0001f)
                return lastValidCurlValues[fingerIndex];

            // Calculate curl based on angles between consecutive segments
            float angle1 = Vector3.Angle(-dirProxToInter, dirInterToDistal);
            float angle2 = Vector3.Angle(-dirInterToDistal, dirDistalToTip);

            // For the thumb, the calculation is different
            if (finger == Finger.Thumb)
            {
                // El pulgar tiene un rango de movimiento diferente y menor
                float thumbAngle = Vector3.Angle(-dirProxToInter, dirDistalToTip);
                return Mathf.Clamp01(Mathf.InverseLerp(180f, 50f, thumbAngle));
            }

            // Normalize angles to 0-1
            // An extended finger has angles close to 180 degrees
            // A closed finger has angles close to 45-60 degrees
            float avgAngle = (angle1 + angle2) / 2f;

            // Mapear: 165 degrees -> 0 (extendido), 45 degrees -> 1 (cerrado)
            float curl = Mathf.InverseLerp(180f, 60f, avgAngle);

            return Mathf.Clamp01(curl);
        }

        /// <summary>
        /// Calculates the finger direction (proximal -> tip) to evaluate spread.
        /// </summary>
        private Vector3 CalculateFingerDirection(XRHand hand, int fingerIndex, out bool hasDir)
        {
            hasDir = false;

            var proximalJoint = hand.GetJoint(fingerProximalJoints[fingerIndex]);
            var tipJoint = hand.GetJoint(fingerTipJoints[fingerIndex]);

            if (!proximalJoint.TryGetPose(out Pose proximalPose) ||
                !tipJoint.TryGetPose(out Pose tipPose))
            {
                return Vector3.zero;
            }

            Vector3 direction = tipPose.position - proximalPose.position;
            if (direction.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            hasDir = true;
            return direction.normalized;
        }

        /// <summary>
        /// Analiza todos los dedos contra el perfil de constraints.
        /// Uses the THREE-STATE philosophy: Extended, Curled, Closed.
        /// Generates semantic errors that distinguish between curling and closing.
        /// </summary>
        private void AnalyzeAllFingers(FingerConstraintProfile profile)
        {
            for (int i = 0; i < 5; i++)
            {
                Finger finger = (Finger)i;
                FingerConstraint constraint = profile.GetConstraint(finger);

                if (constraint == null || !constraint.curl.isEnabled)
                    continue;

                float currentCurl = currentCurlValues[i];

                // === THREE-STATE PHILOSOPHY ===
                // Determinar estado actual y esperado
                FingerShapeState currentState = FeedbackMessages.GetFingerState(currentCurl);
                FingerShapeState expectedState = constraint.expectedState;

                // Use reduced tolerance for more precise feedback
                float effectiveTolerance = Mathf.Max(curlTolerance * 0.5f, 0.08f);
                if (finger == Finger.Thumb)
                {
                    effectiveTolerance = Mathf.Max(effectiveTolerance, 0.12f);
                }
                float minWithTolerance = Mathf.Max(0f, constraint.curl.minCurl - effectiveTolerance);
                float maxWithTolerance = Mathf.Min(1f, constraint.curl.maxCurl + effectiveTolerance);

                // Calculate range deviation
                float deviation = 0f;
                bool outOfRange = false;
                if (currentCurl < minWithTolerance)
                {
                    deviation = minWithTolerance - currentCurl;
                    outOfRange = true;
                }
                else if (currentCurl > maxWithTolerance)
                {
                    deviation = currentCurl - maxWithTolerance;
                    outOfRange = true;
                }

                if (!outOfRange)
                    continue;

                // Determine severity based on deviation
                Severity severity = Severity.None;
                if (deviation > 0.18f)
                    severity = Severity.Major;
                else if (deviation > 0.08f)
                    severity = Severity.Minor;
                else
                    continue; // Dentro de tolerancia extendida

                // === GET SEMANTIC ERROR TYPE ===
                FingerErrorType errorType = FeedbackMessages.GetSemanticErrorType(currentState, expectedState);

                // Fallback to legacy errors if no clear semantic difference
                if (errorType == FingerErrorType.None)
                {
                    errorType = currentCurl < minWithTolerance
                        ? FingerErrorType.TooExtended
                        : FingerErrorType.TooCurled;
                }

                // Soften severity in specific cases
                // If curled and expecting closed, reduce to Minor (on the way there)
                if (currentState == FingerShapeState.Curved &&
                    expectedState == FingerShapeState.Closed &&
                    severity == Severity.Major)
                {
                    severity = Severity.Minor;
                }

                // Relajar severidad en pulgar
                if (finger == Finger.Thumb && severity == Severity.Major && deviation < 0.25f)
                {
                    severity = Severity.Minor;
                }

                if (severity == Severity.None)
                    continue;

                // === GENERATE SEMANTIC MESSAGE ===
                float expectedValue = (constraint.curl.minCurl + constraint.curl.maxCurl) / 2f;
                string fingerName = FeedbackMessages.GetFingerName(finger);
                string message;

                // Intentar usar mensaje personalizado del constraint
                string customMsg = constraint.GetMessage(errorType);
                if (!string.IsNullOrEmpty(customMsg) && !customMsg.StartsWith("Adjust"))
                {
                    message = customMsg;
                }
                else
                {
                    // Generate message based on semantic error type
                    message = errorType switch
                    {
                        FingerErrorType.NeedsCurve =>
                            $"Curl the {fingerName} (without closing fist)",
                        FingerErrorType.NeedsFist =>
                            $"Close the {fingerName} into a fist",
                        FingerErrorType.TooMuchCurl =>
                            $"Release the {fingerName}, don't close into a fist",
                        FingerErrorType.NeedsExtend =>
                            $"Estira el {fingerName}",
                        FingerErrorType.TooExtended =>
                            $"Flexiona el {fingerName}",
                        FingerErrorType.TooCurled =>
                            $"Abre el {fingerName}",
                        _ => FeedbackMessages.GetCorrectionMessage(finger, errorType, severity)
                    };
                }

                errorList.Add(new FingerError(
                    finger,
                    errorType,
                    severity,
                    currentCurl,
                    expectedValue,
                    message
                ));

                if (showDebugLogs)
                {
                    Debug.Log($"[HandPoseAnalyzer] {finger}: {errorType} " +
                             $"(estado={currentState}->{expectedState}, curl={currentCurl:F2}, " +
                             $"rango={minWithTolerance:F2}-{maxWithTolerance:F2}, desv={deviation:F2})");
                }
            }

            // Evaluate spread between adjacent fingers when defined
            if (profile != null)
            {
                EvaluateSpreadConstraints(profile);
            }

            // Analyze specific thumb constraints
            if (profile.thumb.shouldTouchIndex)
            {
                AnalyzeThumbTouch(profile.thumb, Finger.Index);
            }
            if (profile.thumb.shouldTouchMiddle)
            {
                AnalyzeThumbTouch(profile.thumb, Finger.Middle);
            }
            if (profile.thumb.shouldTouchRing)
            {
                AnalyzeThumbTouch(profile.thumb, Finger.Ring);
            }
            if (profile.thumb.shouldTouchPinky)
            {
                AnalyzeThumbTouch(profile.thumb, Finger.Pinky);
            }

            // Thumb placement rules (over or beside the fingers)
            AnalyzeThumbPlacement(profile.thumb);

            // Hand orientation (only if the profile requires it)
            EvaluateOrientation(profile);
        }

        /// <summary>
        /// Evaluates spread constraints using adjacent finger directions.
        /// </summary>
        private void EvaluateSpreadConstraints(FingerConstraintProfile profile)
        {
            var constraints = profile.GetAllConstraints();
            for (int i = 0; i < constraints.Length - 1; i++)
            {
                var constraint = constraints[i];
                if (constraint == null || constraint.spread == null || !constraint.spread.isEnabled)
                    continue;

                if (!hasFingerDirection[i] || !hasFingerDirection[i + 1])
                    continue;

                float angle = Vector3.Angle(currentFingerDirections[i], currentFingerDirections[i + 1]);

                if (angle < constraint.spread.minSpreadAngle || angle > constraint.spread.maxSpreadAngle)
                {
                    FingerErrorType errorType = angle < constraint.spread.minSpreadAngle
                        ? FingerErrorType.SpreadTooNarrow
                        : FingerErrorType.SpreadTooWide;

                    string message = constraint.GetMessage(errorType);
                    if (string.IsNullOrEmpty(message))
                    {
                        message = FeedbackMessages.GetCorrectionMessage(constraint.finger, errorType);
                    }

                    errorList.Add(new FingerError(
                        constraint.finger,
                        errorType,
                        constraint.spread.severityIfOutOfRange,
                        angle,
                        0f,
                        message
                    ));
                }
            }
        }

        /// <summary>
        /// Analyzes whether the thumb is touching the specified finger.
        /// </summary>
        private void AnalyzeThumbTouch(ThumbConstraint thumbConstraint, Finger targetFinger)
        {
            var hand = GetCurrentHand();
            if (!hand.isTracked)
                return;

            var thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
            var targetTip = hand.GetJoint(fingerTipJoints[(int)targetFinger]);

            if (!thumbTip.TryGetPose(out Pose thumbPose) ||
                !targetTip.TryGetPose(out Pose targetPose))
                return;

            float distance = Vector3.Distance(thumbPose.position, targetPose.position);

            // Avoid penalizing the thumb if the target finger is clearly misplaced/extended.
            // This prioritizes correcting the long fingers first and reduces the feeling that the thumb 'never turns green'.
            float targetCurl = currentCurlValues[(int)targetFinger];
            bool targetClearlyExtended = targetCurl < 0.25f; // finger far from contact position
            if (targetClearlyExtended)
            {
                return;
            }

            // If more than 3cm apart, they are not touching
            if (distance > 0.03f)
            {
                errorList.Add(FingerError.Create(
                    Finger.Thumb,
                    FingerErrorType.ShouldTouch,
                    Severity.Major,
                    $"Touch your thumb to your {FeedbackMessages.GetFingerName(targetFinger)}"
                ));
            }
        }

        /// <summary>
        /// Reinforces thumb position feedback (over or beside the fingers).
        /// </summary>
        private void AnalyzeThumbPlacement(ThumbConstraint thumbConstraint)
        {
            if (thumbConstraint == null)
                return;

            float thumbCurl = currentCurlValues[(int)Finger.Thumb];

            // Thumb sobre los dedos (ej. S): no debe estar demasiado extendido
            if (thumbConstraint.shouldBeOverFingers && thumbCurl < 0.35f)
            {
                string msg = !string.IsNullOrEmpty(thumbConstraint.customMessageGeneric)
                    ? thumbConstraint.customMessageGeneric
                    : "Coloca el pulgar encima de los dedos (no lo extiendas)";

                errorList.Add(FingerError.Create(
                    Finger.Thumb,
                    FingerErrorType.ThumbPositionWrong,
                    Severity.Major,
                    msg
                ));
            }

            // Thumb al lado de los dedos (ej. A): no debe cruzar ni meterse sobre ellos
            if (thumbConstraint.shouldBeBesideFingers && thumbCurl > 0.6f)
            {
                string msg = !string.IsNullOrEmpty(thumbConstraint.customMessageGeneric)
                    ? thumbConstraint.customMessageGeneric
                    : "Manten el thumb to the side del puno, sin cruzarlo";

                errorList.Add(FingerError.Create(
                    Finger.Thumb,
                    FingerErrorType.ThumbPositionWrong,
                    Severity.Major,
                    msg
                ));
            }
        }

        /// <summary>
        /// Evalua orientacion de la mano cuando el perfil lo requiere.
        /// </summary>
        private void EvaluateOrientation(FingerConstraintProfile profile)
        {
            if (profile == null || !profile.checkOrientation)
                return;

            var hand = GetCurrentHand();
            if (!hand.isTracked)
                return;

            var palmJoint = hand.GetJoint(XRHandJointID.Palm);
            if (!palmJoint.TryGetPose(out Pose palmPose))
                return;

            Vector3 palmForward = palmPose.rotation * Vector3.forward;
            if (palmForward.sqrMagnitude < 0.0001f || profile.expectedPalmDirection.sqrMagnitude < 0.0001f)
                return;

            float angle = Vector3.Angle(palmForward.normalized, profile.expectedPalmDirection.normalized);
            if (angle > profile.orientationTolerance)
            {
                string orientationMessage = !string.IsNullOrEmpty(profile.orientationHint)
                    ? profile.orientationHint
                    : "Rotate your wrist toward the gesture side";

                errorList.Add(FingerError.Create(
                    Finger.Thumb,
                    FingerErrorType.RotationWrong,
                    Severity.Minor,
                    orientationMessage
                ));
            }
        }

        /// <summary>
        /// Cuenta errores de una severidad especifica.
        /// </summary>
        private int CountErrors(Severity severity)
        {
            int count = 0;
            foreach (var error in errorList)
            {
                if (error.severity == severity)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Obtiene los valores de curl actuales (para debug/visualizacion).
        /// </summary>
        public float[] GetCurrentCurlValues()
        {
            return currentCurlValues;
        }

        /// <summary>
        /// True si hay datos de mano valids.
        /// </summary>
        public bool HasValidHandData => hasValidHandData;
    }
}
