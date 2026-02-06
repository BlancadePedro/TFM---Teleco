using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Analiza la pose actual de la mano comparándola con un FingerConstraintProfile.
    /// Genera errores por dedo (perFingerErrors) y un mensaje resumen.
    ///
    /// IMPORTANTE: Este analizador NO reemplaza la detección global (XRHandShape.CheckConditions).
    /// Solo explica POR QUÉ un gesto no coincide, para feedback pedagógico.
    /// </summary>
    public class HandPoseAnalyzer : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Diccionario de perfiles de constraints por signo")]
        [SerializeField] private List<FingerConstraintProfile> constraintProfiles = new List<FingerConstraintProfile>();

        [Tooltip("Componente XRHandTrackingEvents de la mano a analizar")]
        [SerializeField] private XRHandTrackingEvents handTrackingEvents;

        [Tooltip("Handedness de la mano a analizar")]
        [SerializeField] private Handedness handedness = Handedness.Right;

        [Header("Analysis Settings")]
        [Tooltip("Tolerancia adicional para los rangos de curl (se suma al min y resta al max)")]
        [Range(0f, 0.2f)]
        [SerializeField] private float curlTolerance = 0.1f;

        [Header("Auto-load Profiles")]
        [Tooltip("Cargar automáticamente perfiles de dígitos (0-9) al iniciar")]
        [SerializeField] private bool autoLoadDigitProfiles = true;

        [Tooltip("Cargar automáticamente perfiles de letras del alfabeto (A-Z) al iniciar")]
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

        // Referencia al XRHandSubsystem
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
        /// Carga todos los perfiles de dígitos (0-9) predefinidos.
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
                Debug.Log($"[HandPoseAnalyzer] Cargados {digitProfiles.Length} perfiles de dígitos");
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
        /// Registra un perfil de constraints. Se puede llamar desde código o configurar en Inspector.
        /// </summary>
        public void RegisterProfile(FingerConstraintProfile profile)
        {
            if (profile != null && !constraintProfiles.Contains(profile))
            {
                constraintProfiles.Add(profile);
                Debug.Log($"[HandPoseAnalyzer] Perfil registrado: {profile.signName}");
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
        /// Analiza la pose actual de la mano comparándola con el perfil del signo especificado.
        /// </summary>
        /// <param name="signData">El SignData del gesto objetivo</param>
        /// <param name="useGlobalMatch">Si true, usa XRHandShape.CheckConditions para isMatchGlobal</param>
        /// <returns>Resultado del análisis con errores por dedo</returns>
        public StaticGestureResult Analyze(SignData signData, bool useGlobalMatch = true)
        {
            // Llamar a la versión que acepta el estado de detección externo
            // Si no se proporciona, se usará false (no detectado) para evitar falsos positivos
            return Analyze(signData, isDetectedByRecognizer: false, useGlobalMatch);
        }

        /// <summary>
        /// Analiza la pose actual de la mano comparándola con el perfil del signo especificado.
        /// Esta versión acepta el estado de detección del GestureRecognizer para evitar falsos positivos.
        /// </summary>
        /// <param name="signData">El SignData del gesto objetivo</param>
        /// <param name="isDetectedByRecognizer">True si el GestureRecognizer está detectando activamente el signo</param>
        /// <param name="useGlobalMatch">Si true y isDetectedByRecognizer es false, analiza errores de dedos</param>
        /// <returns>Resultado del análisis con errores por dedo</returns>
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

            // El match global viene del GestureRecognizer, NO lo calculamos aquí
            // porque no podemos acceder correctamente a XRHandJointsUpdatedEventArgs
            cachedResult.isMatchGlobal = isDetectedByRecognizer;

            // Si el recognizer dice que está detectado, es un éxito
            if (isDetectedByRecognizer)
            {
                cachedResult.summaryMessage = $"¡Correcto! '{signData.signName}' detectado";
                cachedResult.majorErrorCount = 0;
                cachedResult.minorErrorCount = 0;
                cachedResult.perFingerErrors = new System.Collections.Generic.List<FingerError>();
                cachedResult.isMatchGlobal = true;
                cachedResult.matchScore = 1f;
                cachedResult.isNearMatch = false;

                if (showDebugLogs)
                {
                    Debug.Log($"[HandPoseAnalyzer] '{signData.signName}' detectado por GestureRecognizer - ÉXITO");
                }
                return cachedResult;
            }

            // Si no hay perfil definido, mostrar mensaje genérico
            if (profile == null)
            {
                if (showDebugLogs)
                    Debug.Log($"[HandPoseAnalyzer] No hay perfil de constraints para '{signData.signName}'");

                cachedResult.summaryMessage = $"Ajusta la mano para '{signData.signName}'...";
                return cachedResult;
            }

            // Analizar cada dedo contra el perfil para mostrar errores específicos
            AnalyzeAllFingers(profile);

            // Generar mensaje resumen
            cachedResult.perFingerErrors = new System.Collections.Generic.List<FingerError>(errorList);
            cachedResult.majorErrorCount = CountErrors(Severity.Major);
            cachedResult.minorErrorCount = CountErrors(Severity.Minor);
            cachedResult.UpdateMatchScore();

            // Generar mensaje basado en errores encontrados
            if (cachedResult.majorErrorCount == 0 && cachedResult.minorErrorCount == 0)
            {
                // Sin errores detectados por el perfil, pero el recognizer no lo detecta
                // Esto puede pasar si el perfil no está bien configurado o la pose no está exacta
                cachedResult.summaryMessage = "El gesto no se ha reconocido completamente, repítelo de nuevo";
            }
            else
            {
                // Generar mensaje con contexto del signo que se está practicando
                string errorSummary = FeedbackMessages.GenerateSummary(cachedResult.perFingerErrors, 2);
                cachedResult.summaryMessage = $"Para '{signData.signName}':\n{errorSummary}";
            }

            if (showDebugLogs)
            {
                Debug.Log($"[HandPoseAnalyzer] Análisis de '{signData.signName}': " +
                         $"Detected={isDetectedByRecognizer}, Major={cachedResult.majorErrorCount}, Minor={cachedResult.minorErrorCount}");
            }

            return cachedResult;
        }

        // NOTA: EvaluateGlobalMatch, CheckShapeConditionsManual y CheckPoseConditionsManual fueron eliminados
        // porque no podemos evaluar XRHandShape.CheckConditions sin XRHandJointsUpdatedEventArgs.
        // La detección global ahora viene directamente del GestureRecognizer a través del parámetro isDetectedByRecognizer.

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
        /// Calcula el valor de curl (0-1) para un dedo específico.
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
                // Conservar el último valor válido para evitar saltos de feedback
                return lastValidCurlValues[fingerIndex] > 0f ? lastValidCurlValues[fingerIndex] : 0.5f;
            }

            // Direcciones basadas en posiciones para evitar dependencia de la rotación del dispositivo
            Vector3 dirProxToInter = (intermediatePose.position - proximalPose.position).normalized;
            Vector3 dirInterToDistal = (distalPose.position - intermediatePose.position).normalized;
            Vector3 dirDistalToTip = (tipPose.position - distalPose.position).normalized;

            if (dirProxToInter.sqrMagnitude < 0.0001f || dirInterToDistal.sqrMagnitude < 0.0001f)
                return lastValidCurlValues[fingerIndex];

            // Calcular curl basado en ángulos entre segmentos consecutivos
            float angle1 = Vector3.Angle(-dirProxToInter, dirInterToDistal);
            float angle2 = Vector3.Angle(-dirInterToDistal, dirDistalToTip);

            // Para el pulgar, el cálculo es diferente
            if (finger == Finger.Thumb)
            {
                // El pulgar tiene un rango de movimiento diferente y menor
                float thumbAngle = Vector3.Angle(-dirProxToInter, dirDistalToTip);
                return Mathf.Clamp01(Mathf.InverseLerp(180f, 50f, thumbAngle));
            }

            // Normalizar ángulos a 0-1
            // Un dedo extendido tiene ángulos cercanos a 180 grados
            // Un dedo cerrado tiene ángulos cercanos a 45-60 grados
            float avgAngle = (angle1 + angle2) / 2f;

            // Mapear: 165 grados -> 0 (extendido), 45 grados -> 1 (cerrado)
            float curl = Mathf.InverseLerp(180f, 60f, avgAngle);

            return Mathf.Clamp01(curl);
        }

        /// <summary>
        /// Calcula la dirección del dedo (proximal -> tip) para evaluar spread.
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
        /// IMPORTANTE: Siempre genera feedback para CADA dedo, indicando su estado actual.
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

                // Usar tolerancia reducida para feedback más preciso, pero con margen mínimo para evitar ruido
                float effectiveTolerance = Mathf.Max(curlTolerance * 0.5f, 0.08f);
                if (finger == Finger.Thumb)
                {
                    // Pulgar suele tener lecturas más ruidosas, ampliamos tolerancia para evitar falsos rojos.
                    effectiveTolerance = Mathf.Max(effectiveTolerance, 0.12f);
                }
                float minWithTolerance = Mathf.Max(0f, constraint.curl.minCurl - effectiveTolerance);
                float maxWithTolerance = Mathf.Min(1f, constraint.curl.maxCurl + effectiveTolerance);

                FingerErrorType errorType = FingerErrorType.None;
                Severity severity = Severity.None;

                // Calcular qué tan lejos está del rango correcto
                float deviation = 0f;
                if (currentCurl < minWithTolerance)
                {
                    errorType = FingerErrorType.TooExtended;
                    deviation = minWithTolerance - currentCurl;
                }
                else if (currentCurl > maxWithTolerance)
                {
                    errorType = FingerErrorType.TooCurled;
                    deviation = currentCurl - maxWithTolerance;
                }

                if (errorType != FingerErrorType.None)
                {
                    // Determinar severidad basada en qué tan lejos está
                    // Mayor = desviación > 0.18, Menor = desviación 0.08-0.18 (filtrando jitter)
                    if (deviation > 0.18f)
                        severity = Severity.Major;
                    else if (deviation > 0.08f)
                        severity = Severity.Minor;
                    else
                        severity = Severity.None; // Dentro de tolerancia extendida

                    // Relajar severidad en pulgar para evitar rojos persistentes
                    if (finger == Finger.Thumb && severity == Severity.Major && deviation < 0.25f)
                    {
                        severity = Severity.Minor;
                    }

                    if (severity != Severity.None)
                    {
                        float expectedValue = (constraint.curl.minCurl + constraint.curl.maxCurl) / 2f;

                        // Generar mensaje específico con valores actuales
                        string fingerName = FeedbackMessages.GetFingerName(finger);
                        string message = errorType == FingerErrorType.TooExtended
                            ? $"Curl your {fingerName} more ({Mathf.RoundToInt(currentCurl * 100)}% -> {Mathf.RoundToInt(constraint.curl.minCurl * 100)}%+)"
                            : $"Extend your {fingerName} more ({Mathf.RoundToInt(currentCurl * 100)}% -> {Mathf.RoundToInt(constraint.curl.maxCurl * 100)}%-)";

                        // Usar mensaje personalizado si está disponible
                        string customMsg = constraint.GetMessage(errorType);
                        if (!string.IsNullOrEmpty(customMsg) && !customMsg.StartsWith("Adjust"))
                        {
                            message = customMsg;
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
                            Debug.Log($"[HandPoseAnalyzer] {finger}: {errorType} (actual={currentCurl:F2}, rango={minWithTolerance:F2}-{maxWithTolerance:F2}, desv={deviation:F2})");
                        }
                    }
                }
            }

            // Evaluar spread entre dedos adyacentes cuando esté definido
            if (profile != null)
            {
                EvaluateSpreadConstraints(profile);
            }

            // Analizar constraints específicos del pulgar
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

            // Orientación de la mano (solo si el perfil lo pide)
            EvaluateOrientation(profile);
        }

        /// <summary>
        /// Evalúa constraints de spread usando la dirección de dedos adyacentes.
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
        /// Analiza si el pulgar está tocando el dedo especificado.
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

            // Evitar penalizar al pulgar si el dedo objetivo está claramente mal colocado/extendido.
            // Esto da prioridad a corregir primero los dedos largos y reduce la sensación de que el pulgar "nunca se pone verde".
            float targetCurl = currentCurlValues[(int)targetFinger];
            bool targetClearlyExtended = targetCurl < 0.25f; // dedo lejos de la posición de contacto
            if (targetClearlyExtended)
            {
                return;
            }

            // Si están a más de 3cm, no están tocándose
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
        /// Evalúa orientación de la mano cuando el perfil lo requiere.
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
                errorList.Add(FingerError.Create(
                    Finger.Thumb,
                    FingerErrorType.RotationWrong,
                    Severity.Minor,
                    "Gira la muñeca hacia el lado del gesto"
                ));
            }
        }

        /// <summary>
        /// Cuenta errores de una severidad específica.
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
        /// Obtiene los valores de curl actuales (para debug/visualización).
        /// </summary>
        public float[] GetCurrentCurlValues()
        {
            return currentCurlValues;
        }

        /// <summary>
        /// True si hay datos de mano válidos.
        /// </summary>
        public bool HasValidHandData => hasValidHandData;
    }
}
