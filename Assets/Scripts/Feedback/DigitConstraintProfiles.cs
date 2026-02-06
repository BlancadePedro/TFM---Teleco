using UnityEngine;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Perfiles de constraints predefinidos para los dígitos ASL (0-9).
    /// Usar con HandPoseAnalyzer para feedback detallado por dedo.
    ///
    /// Valores de curl: 0 = extendido, 1 = cerrado
    /// </summary>
    public static class DigitConstraintProfiles
    {
        // Constantes para rangos comunes
        private const float EXTENDED_MIN = 0f;
        private const float EXTENDED_MAX = 0.45f;

        private const float CURLED_MIN = 0.55f;
        private const float CURLED_MAX = 1f;

        private const float PARTIAL_MIN = 0.3f;
        private const float PARTIAL_MAX = 0.65f;

        /// <summary>
        /// Crea el perfil para el dígito 1.
        /// Índice extendido, resto cerrados.
        /// </summary>
        public static FingerConstraintProfile CreateDigit1()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "1";
            profile.description = "Index extended, others curled";

            // Pulgar: cerrado contra la palma
            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Recoge el pulgar contra la palma"
            };

            // Índice: EXTENDIDO (el único)
            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el índice"
            };

            // Medio: cerrado
            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el medio"
            };

            // Anular: cerrado
            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular"
            };

            // Meñique: cerrado
            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para el dígito 2.
        /// Índice y medio extendidos, resto cerrados.
        /// </summary>
        public static FingerConstraintProfile CreateDigit2()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "2";
            profile.description = "Index and middle extended (V shape)";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Recoge el pulgar contra la palma"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el índice"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el medio"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = 0.55f, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = 0.55f, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para el dígito 3.
        /// Pulgar, índice y medio extendidos.
        /// </summary>
        public static FingerConstraintProfile CreateDigit3()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "3";
            profile.description = "Thumb, index and middle extended";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = 0.45f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooCurled = "Extiende el pulgar"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el índice"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el medio"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = 0.55f, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = 0.55f, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para el dígito 4.
        /// Cuatro dedos extendidos (sin pulgar).
        /// </summary>
        public static FingerConstraintProfile CreateDigit4()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "4";
            profile.description = "Four fingers extended, thumb tucked";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Recoge el pulgar sobre la palma"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el índice"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el medio"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para el dígito 5.
        /// Todos los dedos extendidos (mano abierta).
        /// </summary>
        public static FingerConstraintProfile CreateDigit5()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "5";
            profile.description = "All fingers extended (open hand)";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = 0.45f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooCurled = "Extiende el pulgar"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el índice"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el medio"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para el dígito 6.
        /// Pulgar toca meñique; índice/medio/anular extendidos.
        /// </summary>
        public static FingerConstraintProfile CreateDigit6()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "6";
            profile.description = "Thumb touches pinky; index/middle/ring extended";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.2f, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                shouldTouchPinky = true,
                customMessageGeneric = "Toca el meñique con el pulgar"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el índice"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el medio"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooCurled = "Extiende el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Dobla el meñique para tocar el pulgar"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para el dígito 7.
        /// Pulgar toca anular; índice/medio/meñique extendidos.
        /// </summary>
        public static FingerConstraintProfile CreateDigit7()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "7";
            profile.description = "Thumb touches ring; index/middle/pinky extended";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.2f, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                shouldTouchRing = true,
                customMessageGeneric = "Toca el anular con el pulgar"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el índice"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el medio"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Dobla el anular para tocar el pulgar"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooCurled = "Extiende el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para el dígito 8.
        /// Pulgar y medio se tocan, índice, anular y meñique extendidos.
        /// </summary>
        public static FingerConstraintProfile CreateDigit8()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "8";
            profile.description = "Pulgar toca medio; índice, anular, meñique extendidos";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                shouldTouchMiddle = true,
                customMessageGeneric = "Toca el dedo medio con el pulgar"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el índice"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Dobla el medio para tocar el pulgar"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para el dígito 9.
        /// Pulgar e índice forman círculo (OK sign), resto extendidos.
        /// </summary>
        public static FingerConstraintProfile CreateDigit9()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "9";
            profile.description = "Pulgar e índice forman círculo (OK sign)";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                shouldTouchIndex = true,
                customMessageGeneric = "Toca la punta del índice con el pulgar"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Dobla el índice para tocar el pulgar (forma una O)"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el medio"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para el dígito 0 (o 10).
        /// Forma de O: todos los dedos curvados tocando el pulgar.
        /// </summary>
        public static FingerConstraintProfile CreateDigit0()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "0";
            profile.description = "O shape: all fingers curved touching thumb";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Forma una O con el pulgar"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Curva los dedos para formar una O"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Curva los dedos para formar una O"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Curva los dedos para formar una O"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Curva los dedos para formar una O"
            };

            return profile;
        }

        /// <summary>
        /// Crea todos los perfiles de dígitos (0-9).
        /// </summary>
        public static FingerConstraintProfile[] CreateAllDigitProfiles()
        {
            return new FingerConstraintProfile[]
            {
                CreateDigit0(),
                CreateDigit1(),
                CreateDigit2(),
                CreateDigit3(),
                CreateDigit4(),
                CreateDigit5(),
                CreateDigit6(),
                CreateDigit7(),
                CreateDigit8(),
                CreateDigit9()
            };
        }

        /// <summary>
        /// Obtiene el perfil para un dígito específico.
        /// </summary>
        public static FingerConstraintProfile GetDigitProfile(int digit)
        {
            return digit switch
            {
                0 => CreateDigit0(),
                1 => CreateDigit1(),
                2 => CreateDigit2(),
                3 => CreateDigit3(),
                4 => CreateDigit4(),
                5 => CreateDigit5(),
                6 => CreateDigit6(),
                7 => CreateDigit7(),
                8 => CreateDigit8(),
                9 => CreateDigit9(),
                _ => null
            };
        }
    }
}
