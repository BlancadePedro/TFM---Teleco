using System;
using UnityEngine;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Constraint de curl (extension/flexion) para un dedo.
    /// Los valores van de 0 (completamente extendido) a 1 (completamente cerrado).
    /// </summary>
    [Serializable]
    public class CurlConstraint
    {
        [Tooltip("Value minimo de curl aceptable (0=extendido, 1=cerrado)")]
        [Range(0f, 1f)]
        public float minCurl = 0f;

        [Tooltip("Value maximo de curl aceptable")]
        [Range(0f, 1f)]
        public float maxCurl = 1f;

        [Tooltip("Severidad si esta fuera del rango")]
        public Severity severityIfOutOfRange = Severity.Major;

        [Tooltip("True si este constraint esta active")]
        public bool isEnabled = true;

        /// <summary>
        /// Evalua si el valor de curl esta dentro del rango aceptable.
        /// </summary>
        /// <param name="curlValue">Value actual de curl (0-1)</param>
        /// <param name="errorType">Type de error si esta fuera de rango</param>
        /// <returns>True si esta dentro del rango</returns>
        public bool Evaluate(float curlValue, out FingerErrorType errorType)
        {
            errorType = FingerErrorType.None;

            if (!isEnabled)
                return true;

            if (curlValue < minCurl)
            {
                errorType = FingerErrorType.TooExtended;
                return false;
            }

            if (curlValue > maxCurl)
            {
                errorType = FingerErrorType.TooCurled;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calcula que tan lejos esta el valor actual del rango aceptable.
        /// </summary>
        public float GetDeviation(float curlValue)
        {
            if (!isEnabled) return 0f;

            if (curlValue < minCurl)
                return minCurl - curlValue;
            if (curlValue > maxCurl)
                return curlValue - maxCurl;

            return 0f;
        }
    }

    /// <summary>
    /// Constraint de spread (separacion) para un dedo regardingl siguiente.
    /// </summary>
    [Serializable]
    public class SpreadConstraint
    {
        [Tooltip("Angle minimo de separacion en degrees")]
        [Range(-30f, 30f)]
        public float minSpreadAngle = -15f;

        [Tooltip("Angle maximo de separacion en degrees")]
        [Range(-30f, 30f)]
        public float maxSpreadAngle = 15f;

        [Tooltip("Severidad si esta fuera del rango")]
        public Severity severityIfOutOfRange = Severity.Minor;

        [Tooltip("True si este constraint esta active")]
        public bool isEnabled = false;

        /// <summary>
        /// Evalua si el spread esta dentro del rango aceptable.
        /// </summary>
        public bool Evaluate(float spreadAngle, out FingerErrorType errorType)
        {
            errorType = FingerErrorType.None;

            if (!isEnabled)
                return true;

            if (spreadAngle < minSpreadAngle)
            {
                errorType = FingerErrorType.SpreadTooNarrow;
                return false;
            }

            if (spreadAngle > maxSpreadAngle)
            {
                errorType = FingerErrorType.SpreadTooWide;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Constraint completo para un dedo individual.
    /// Soporta la filosofia de tres estados: Extendido, Curvado, Cerrado.
    /// </summary>
    [Serializable]
    public class FingerConstraint
    {
        [Header("Finger")]
        [Tooltip("Finger al que aplica este constraint")]
        public Finger finger;

        [Header("Expected State (Semantic)")]
        [Tooltip("State semantico esperado para este dedo.\n" +
                 "- Extended: finger straight, no flexion\n" +
                 "- Curved: dedo curvado con control, sin tocar palma (para C, D, O, X)\n" +
                 "- Closed: finger in fist, touching the palm (for A, S, T)\n" +
                 "Si no se especifica, se deriva automaticamente del rango de curl.")]
        [SerializeField]
        private FingerShapeState _expectedState = FingerShapeState.Extended;

        [Tooltip("Si true, usa el valor de _expectedState. Si false, lo deriva del rango de curl.")]
        [SerializeField]
        private bool _useExplicitState = false;

        /// <summary>
        /// State semantico esperado para este dedo.
        /// Si no se ha especificado explicitamente, se deriva del rango de curl.
        /// </summary>
        public FingerShapeState expectedState
        {
            get
            {
                if (_useExplicitState)
                    return _expectedState;

                // Derivar automaticamente del rango de curl
                return DeriveStateFromCurlRange();
            }
            set
            {
                _expectedState = value;
                _useExplicitState = true;
            }
        }

        /// <summary>
        /// Deriva el estado semantico esperado basandose en el rango de curl.
        /// Usa los umbrales de la filosofia de tres estados:
        /// - Extended: centro < 0.25
        /// - Curved: centro 0.25-0.75
        /// - Closed: centro > 0.75
        /// </summary>
        private FingerShapeState DeriveStateFromCurlRange()
        {
            if (curl == null || !curl.isEnabled)
                return FingerShapeState.Extended;

            // Calcular el punto medio del rango esperado
            float midpoint = (curl.minCurl + curl.maxCurl) / 2f;

            // Thresholdes basados en la filosofia de tres estados
            if (midpoint < 0.30f)
                return FingerShapeState.Extended;
            if (midpoint > 0.72f)
                return FingerShapeState.Closed;

            return FingerShapeState.Curved;
        }

        [Header("Curl (Extension/Flexion)")]
        [Tooltip("Constraint de curl para este dedo")]
        public CurlConstraint curl = new CurlConstraint();

        [Header("Spread (Separation)")]
        [Tooltip("Separation constraint relative to the next finger")]
        public SpreadConstraint spread = new SpreadConstraint();

        [Header("Custom Messages")]
        [Tooltip("Mensaje personalizado si el dedo debe CURVAR (de extendido a curvado)")]
        public string customMessageNeedsCurve;

        [Tooltip("Mensaje personalizado si el dedo debe CERRAR (a puno)")]
        public string customMessageNeedsFist;

        [Tooltip("Mensaje personalizado si el dedo cerro DEMASIADO (puno cuando debia curvar)")]
        public string customMessageTooMuchCurl;

        [Tooltip("Mensaje personalizado si el dedo debe EXTENDER")]
        public string customMessageNeedsExtend;

        [Tooltip("Mensaje personalizado si el dedo esta demasiado extendido (legacy)")]
        public string customMessageTooExtended;

        [Tooltip("Mensaje personalizado si el dedo esta demasiado cerrado (legacy)")]
        public string customMessageTooCurled;

        [Tooltip("Mensaje personalizado generico para este dedo")]
        public string customMessageGeneric;

        /// <summary>
        /// Obtiene el mensaje apropiado para un tipo de error.
        /// Prioriza mensajes personalizados sobre los genericos.
        /// </summary>
        public string GetMessage(FingerErrorType errorType)
        {
            switch (errorType)
            {
                // === Errores semanticos de tres estados ===
                case FingerErrorType.NeedsCurve:
                    return !string.IsNullOrEmpty(customMessageNeedsCurve)
                        ? customMessageNeedsCurve
                        : FeedbackMessages.GetCorrectionMessage(finger, errorType);

                case FingerErrorType.NeedsFist:
                    return !string.IsNullOrEmpty(customMessageNeedsFist)
                        ? customMessageNeedsFist
                        : FeedbackMessages.GetCorrectionMessage(finger, errorType);

                case FingerErrorType.TooMuchCurl:
                    return !string.IsNullOrEmpty(customMessageTooMuchCurl)
                        ? customMessageTooMuchCurl
                        : FeedbackMessages.GetCorrectionMessage(finger, errorType);

                case FingerErrorType.NeedsExtend:
                    return !string.IsNullOrEmpty(customMessageNeedsExtend)
                        ? customMessageNeedsExtend
                        : FeedbackMessages.GetCorrectionMessage(finger, errorType);

                // === Errores legacy ===
                case FingerErrorType.TooExtended:
                    return !string.IsNullOrEmpty(customMessageTooExtended)
                        ? customMessageTooExtended
                        : FeedbackMessages.GetCorrectionMessage(finger, errorType);

                case FingerErrorType.TooCurled:
                    return !string.IsNullOrEmpty(customMessageTooCurled)
                        ? customMessageTooCurled
                        : FeedbackMessages.GetCorrectionMessage(finger, errorType);

                default:
                    return !string.IsNullOrEmpty(customMessageGeneric)
                        ? customMessageGeneric
                        : FeedbackMessages.GetCorrectionMessage(finger, errorType);
            }
        }

        /// <summary>
        /// Crea un constraint con valores tipicos para dedo EXTENDIDO.
        /// El dedo debe estar recto, sin flexion.
        /// </summary>
        public static FingerConstraint Extended(Finger finger)
        {
            var constraint = new FingerConstraint
            {
                finger = finger,
                curl = new CurlConstraint { minCurl = 0f, maxCurl = 0.25f, isEnabled = true }
            };
            constraint.expectedState = FingerShapeState.Extended;
            return constraint;
        }

        /// <summary>
        /// Crea un constraint con valores tipicos para dedo CERRADO (puno).
        /// El dedo debe estar completamente plegado, tocando la palma.
        /// Usado en signos como A, S, T.
        /// </summary>
        public static FingerConstraint Closed(Finger finger)
        {
            var constraint = new FingerConstraint
            {
                finger = finger,
                curl = new CurlConstraint { minCurl = 0.75f, maxCurl = 1f, isEnabled = true }
            };
            constraint.expectedState = FingerShapeState.Closed;
            return constraint;
        }

        /// <summary>
        /// Crea un constraint con valores tipicos para dedo CURVADO (forma controlada).
        /// El dedo debe flexionarse sin cerrar la mano ni tocar la palma.
        /// Usado en signos como C, D, O, X (gancho), E, M, N.
        /// </summary>
        public static FingerConstraint Curved(Finger finger)
        {
            var constraint = new FingerConstraint
            {
                finger = finger,
                curl = new CurlConstraint { minCurl = 0.25f, maxCurl = 0.75f, isEnabled = true }
            };
            constraint.expectedState = FingerShapeState.Curved;
            return constraint;
        }

        /// <summary>
        /// Alias de Closed para compatibilidad (legacy).
        /// Prefiere usar Closed() para claridad semantica.
        /// </summary>
        public static FingerConstraint Curled(Finger finger) => Closed(finger);

        /// <summary>
        /// Alias de Curved para compatibilidad (legacy).
        /// Prefiere usar Curved() para claridad semantica.
        /// </summary>
        public static FingerConstraint PartiallyCurled(Finger finger) => Curved(finger);

        /// <summary>
        /// Crea un constraint para TIP CURL (curvatura solo de la punta).
        /// Usado en signos como E, M, N donde solo las puntas se curvan hasta el nudillo.
        /// </summary>
        public static FingerConstraint TipCurled(Finger finger)
        {
            var constraint = new FingerConstraint
            {
                finger = finger,
                curl = new CurlConstraint { minCurl = 0.4f, maxCurl = 0.7f, isEnabled = true },
                customMessageNeedsCurve = $"Curva solo la punta del {FeedbackMessages.GetFingerName(finger)} hasta el nudillo"
            };
            constraint.expectedState = FingerShapeState.Curved;
            return constraint;
        }

        /// <summary>
        /// Crea un constraint para GANCHO (hook shape).
        /// Usado en signos como X donde el dedo forma un gancho.
        /// </summary>
        public static FingerConstraint Hook(Finger finger)
        {
            var constraint = new FingerConstraint
            {
                finger = finger,
                curl = new CurlConstraint { minCurl = 0.5f, maxCurl = 0.8f, isEnabled = true },
                customMessageNeedsCurve = $"Curva el {FeedbackMessages.GetFingerName(finger)} formando un gancho",
                customMessageTooMuchCurl = $"Suelta un poco el {FeedbackMessages.GetFingerName(finger)}, es un gancho, no un puno"
            };
            constraint.expectedState = FingerShapeState.Curved;
            return constraint;
        }
    }

    /// <summary>
    /// Constraint especial para el pulgar que incluye posicion.
    /// </summary>
    [Serializable]
    public class ThumbConstraint : FingerConstraint
    {
        [Header("Thumb-Specific")]
        [Tooltip("El pulgar debe estar sobre los otros dedos")]
        public bool shouldBeOverFingers = false;

        [Tooltip("El pulgar debe estar al lado de los otros dedos")]
        public bool shouldBeBesideFingers = false;

        [Tooltip("El pulgar debe tocar el dedo index")]
        public bool shouldTouchIndex = false;

        [Tooltip("El pulgar debe tocar el dedo medio")]
        public bool shouldTouchMiddle = false;

        [Tooltip("El pulgar debe tocar el dedo anular")]
        public bool shouldTouchRing = false;

        [Tooltip("El pulgar debe tocar el dedo menique")]
        public bool shouldTouchPinky = false;

        public ThumbConstraint()
        {
            finger = Finger.Thumb;
        }
    }

    /// <summary>
    /// Profile completo de constraints para un signo especifico.
    /// Este ScriptableObject define que posicion debe tener cada dedo.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFingerProfile", menuName = "ASL Learn VR/Finger Constraint Profile", order = 2)]
    public class FingerConstraintProfile : ScriptableObject
    {
        [Header("Sign Reference")]
        [Tooltip("Name del signo al que aplica este perfil (debe coincidir con SignData.signName)")]
        public string signName;

        [Tooltip("Profile description for reference")]
        [TextArea(2, 3)]
        public string description;

        [Header("Finger Constraints")]
        [Tooltip("Constraint para el pulgar")]
        public ThumbConstraint thumb = new ThumbConstraint();

        [Tooltip("Index finger constraint")]
        public FingerConstraint index = new FingerConstraint { finger = Finger.Index };

        [Tooltip("Constraint para el medio")]
        public FingerConstraint middle = new FingerConstraint { finger = Finger.Middle };

        [Tooltip("Constraint para el anular")]
        public FingerConstraint ring = new FingerConstraint { finger = Finger.Ring };

        [Tooltip("Pinky finger constraint")]
        public FingerConstraint pinky = new FingerConstraint { finger = Finger.Pinky };

        [Header("Hand Orientation")]
        [Tooltip("Enable if hand orientation matters")]
        public bool checkOrientation = false;

        [Tooltip("Expected palm direction (relative to camera)")]
        public Vector3 expectedPalmDirection = Vector3.forward;

        [Tooltip("Tolerancia angular en degrees para la orientacion")]
        [Range(10f, 90f)]
        public float orientationTolerance = 45f;

        [Tooltip("Mensaje de ayuda especifico para orientar la palma (opcional)")]
        [TextArea(1, 2)]
        public string orientationHint;

        /// <summary>
        /// Obtiene el constraint para un dedo especifico.
        /// </summary>
        public FingerConstraint GetConstraint(Finger finger)
        {
            return finger switch
            {
                Finger.Thumb => thumb,
                Finger.Index => index,
                Finger.Middle => middle,
                Finger.Ring => ring,
                Finger.Pinky => pinky,
                _ => null
            };
        }

        /// <summary>
        /// Obtiene todos los constraints como array.
        /// </summary>
        public FingerConstraint[] GetAllConstraints()
        {
            return new FingerConstraint[] { thumb, index, middle, ring, pinky };
        }

        /// <summary>
        /// Valida la configuracion del perfil.
        /// </summary>
        private void OnValidate()
        {
            // Asegurar que los dedos esten correctamente assigneds
            thumb.finger = Finger.Thumb;
            index.finger = Finger.Index;
            middle.finger = Finger.Middle;
            ring.finger = Finger.Ring;
            pinky.finger = Finger.Pinky;

            // Normalizar direccion de palma
            if (expectedPalmDirection.sqrMagnitude > 0.01f)
            {
                expectedPalmDirection = expectedPalmDirection.normalized;
            }
        }

        #region Static Factory Methods

        /// <summary>
        /// Crea un perfil basico para la letra A (puno con thumb to the side).
        /// </summary>
        public static FingerConstraintProfile CreateLetterA()
        {
            var profile = CreateInstance<FingerConstraintProfile>();
            profile.signName = "A";
            profile.description = "Fist with thumb beside fingers";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.2f, maxCurl = 0.6f, isEnabled = true },
                shouldBeBesideFingers = true
            };
            profile.index = FingerConstraint.Curled(Finger.Index);
            profile.middle = FingerConstraint.Curled(Finger.Middle);
            profile.ring = FingerConstraint.Curled(Finger.Ring);
            profile.pinky = FingerConstraint.Curled(Finger.Pinky);

            return profile;
        }

        /// <summary>
        /// Crea un perfil basico para la letra B (dedos extendidos juntos, pulgar cruzado).
        /// </summary>
        public static FingerConstraintProfile CreateLetterB()
        {
            var profile = CreateInstance<FingerConstraintProfile>();
            profile.signName = "B";
            profile.description = "Fingers extended together, thumb across palm";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.3f, maxCurl = 0.8f, isEnabled = true },
                shouldBeOverFingers = false
            };
            profile.index = FingerConstraint.Extended(Finger.Index);
            profile.middle = FingerConstraint.Extended(Finger.Middle);
            profile.ring = FingerConstraint.Extended(Finger.Ring);
            profile.pinky = FingerConstraint.Extended(Finger.Pinky);

            return profile;
        }

        /// <summary>
        /// Crea un perfil basico para la letra C (mano en forma de C).
        /// </summary>
        public static FingerConstraintProfile CreateLetterC()
        {
            var profile = CreateInstance<FingerConstraintProfile>();
            profile.signName = "C";
            profile.description = "Hand curved in C shape";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.2f, maxCurl = 0.5f, isEnabled = true }
            };
            profile.index = FingerConstraint.PartiallyCurled(Finger.Index);
            profile.middle = FingerConstraint.PartiallyCurled(Finger.Middle);
            profile.ring = FingerConstraint.PartiallyCurled(Finger.Ring);
            profile.pinky = FingerConstraint.PartiallyCurled(Finger.Pinky);

            return profile;
        }

        #endregion
    }
}
