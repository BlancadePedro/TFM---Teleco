using System;
using UnityEngine;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Constraint de curl (extensión/flexión) para un dedo.
    /// Los valores van de 0 (completamente extendido) a 1 (completamente cerrado).
    /// </summary>
    [Serializable]
    public class CurlConstraint
    {
        [Tooltip("Valor mínimo de curl aceptable (0=extendido, 1=cerrado)")]
        [Range(0f, 1f)]
        public float minCurl = 0f;

        [Tooltip("Valor máximo de curl aceptable")]
        [Range(0f, 1f)]
        public float maxCurl = 1f;

        [Tooltip("Severidad si está fuera del rango")]
        public Severity severityIfOutOfRange = Severity.Major;

        [Tooltip("True si este constraint está activo")]
        public bool isEnabled = true;

        /// <summary>
        /// Evalúa si el valor de curl está dentro del rango aceptable.
        /// </summary>
        /// <param name="curlValue">Valor actual de curl (0-1)</param>
        /// <param name="errorType">Tipo de error si está fuera de rango</param>
        /// <returns>True si está dentro del rango</returns>
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
        /// Calcula qué tan lejos está el valor actual del rango aceptable.
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
    /// Constraint de spread (separación) para un dedo respecto al siguiente.
    /// </summary>
    [Serializable]
    public class SpreadConstraint
    {
        [Tooltip("Ángulo mínimo de separación en grados")]
        [Range(-30f, 30f)]
        public float minSpreadAngle = -15f;

        [Tooltip("Ángulo máximo de separación en grados")]
        [Range(-30f, 30f)]
        public float maxSpreadAngle = 15f;

        [Tooltip("Severidad si está fuera del rango")]
        public Severity severityIfOutOfRange = Severity.Minor;

        [Tooltip("True si este constraint está activo")]
        public bool isEnabled = false;

        /// <summary>
        /// Evalúa si el spread está dentro del rango aceptable.
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
    /// </summary>
    [Serializable]
    public class FingerConstraint
    {
        [Header("Finger")]
        [Tooltip("Dedo al que aplica este constraint")]
        public Finger finger;

        [Header("Curl (Extension/Flexion)")]
        [Tooltip("Constraint de curl para este dedo")]
        public CurlConstraint curl = new CurlConstraint();

        [Header("Spread (Separation)")]
        [Tooltip("Constraint de separación respecto al dedo siguiente")]
        public SpreadConstraint spread = new SpreadConstraint();

        [Header("Custom Messages")]
        [Tooltip("Mensaje personalizado si el dedo está demasiado extendido")]
        public string customMessageTooExtended;

        [Tooltip("Mensaje personalizado si el dedo está demasiado cerrado")]
        public string customMessageTooCurled;

        [Tooltip("Mensaje personalizado genérico para este dedo")]
        public string customMessageGeneric;

        /// <summary>
        /// Obtiene el mensaje apropiado para un tipo de error.
        /// </summary>
        public string GetMessage(FingerErrorType errorType)
        {
            switch (errorType)
            {
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
        /// Crea un constraint con valores típicos para dedo extendido.
        /// </summary>
        public static FingerConstraint Extended(Finger finger)
        {
            return new FingerConstraint
            {
                finger = finger,
                curl = new CurlConstraint { minCurl = 0f, maxCurl = 0.25f, isEnabled = true }
            };
        }

        /// <summary>
        /// Crea un constraint con valores típicos para dedo cerrado.
        /// </summary>
        public static FingerConstraint Curled(Finger finger)
        {
            return new FingerConstraint
            {
                finger = finger,
                curl = new CurlConstraint { minCurl = 0.7f, maxCurl = 1f, isEnabled = true }
            };
        }

        /// <summary>
        /// Crea un constraint con valores típicos para dedo parcialmente cerrado.
        /// </summary>
        public static FingerConstraint PartiallyCurled(Finger finger)
        {
            return new FingerConstraint
            {
                finger = finger,
                curl = new CurlConstraint { minCurl = 0.3f, maxCurl = 0.7f, isEnabled = true }
            };
        }
    }

    /// <summary>
    /// Constraint especial para el pulgar que incluye posición.
    /// </summary>
    [Serializable]
    public class ThumbConstraint : FingerConstraint
    {
        [Header("Thumb-Specific")]
        [Tooltip("El pulgar debe estar sobre los otros dedos")]
        public bool shouldBeOverFingers = false;

        [Tooltip("El pulgar debe estar al lado de los otros dedos")]
        public bool shouldBeBesideFingers = false;

        [Tooltip("El pulgar debe tocar el dedo índice")]
        public bool shouldTouchIndex = false;

        [Tooltip("El pulgar debe tocar el dedo medio")]
        public bool shouldTouchMiddle = false;

        [Tooltip("El pulgar debe tocar el dedo anular")]
        public bool shouldTouchRing = false;

        [Tooltip("El pulgar debe tocar el dedo meñique")]
        public bool shouldTouchPinky = false;

        public ThumbConstraint()
        {
            finger = Finger.Thumb;
        }
    }

    /// <summary>
    /// Perfil completo de constraints para un signo específico.
    /// Este ScriptableObject define qué posición debe tener cada dedo.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFingerProfile", menuName = "ASL Learn VR/Finger Constraint Profile", order = 2)]
    public class FingerConstraintProfile : ScriptableObject
    {
        [Header("Sign Reference")]
        [Tooltip("Nombre del signo al que aplica este perfil (debe coincidir con SignData.signName)")]
        public string signName;

        [Tooltip("Descripción del perfil para referencia")]
        [TextArea(2, 3)]
        public string description;

        [Header("Finger Constraints")]
        [Tooltip("Constraint para el pulgar")]
        public ThumbConstraint thumb = new ThumbConstraint();

        [Tooltip("Constraint para el índice")]
        public FingerConstraint index = new FingerConstraint { finger = Finger.Index };

        [Tooltip("Constraint para el medio")]
        public FingerConstraint middle = new FingerConstraint { finger = Finger.Middle };

        [Tooltip("Constraint para el anular")]
        public FingerConstraint ring = new FingerConstraint { finger = Finger.Ring };

        [Tooltip("Constraint para el meñique")]
        public FingerConstraint pinky = new FingerConstraint { finger = Finger.Pinky };

        [Header("Hand Orientation")]
        [Tooltip("Activar si la orientación de la mano es importante")]
        public bool checkOrientation = false;

        [Tooltip("Dirección esperada de la palma (relativa a la cámara)")]
        public Vector3 expectedPalmDirection = Vector3.forward;

        [Tooltip("Tolerancia angular en grados para la orientación")]
        [Range(10f, 90f)]
        public float orientationTolerance = 45f;

        /// <summary>
        /// Obtiene el constraint para un dedo específico.
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
        /// Valida la configuración del perfil.
        /// </summary>
        private void OnValidate()
        {
            // Asegurar que los dedos estén correctamente asignados
            thumb.finger = Finger.Thumb;
            index.finger = Finger.Index;
            middle.finger = Finger.Middle;
            ring.finger = Finger.Ring;
            pinky.finger = Finger.Pinky;

            // Normalizar dirección de palma
            if (expectedPalmDirection.sqrMagnitude > 0.01f)
            {
                expectedPalmDirection = expectedPalmDirection.normalized;
            }
        }

        #region Static Factory Methods

        /// <summary>
        /// Crea un perfil básico para la letra A (puño con pulgar al lado).
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
        /// Crea un perfil básico para la letra B (dedos extendidos juntos, pulgar cruzado).
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
        /// Crea un perfil básico para la letra C (mano en forma de C).
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
