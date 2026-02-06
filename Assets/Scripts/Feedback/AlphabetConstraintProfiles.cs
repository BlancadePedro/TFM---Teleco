using UnityEngine;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Perfiles de constraints predefinidos para las letras del alfabeto ASL (A-Z).
    /// Usar con HandPoseAnalyzer para feedback detallado por dedo.
    ///
    /// Valores de curl: 0 = extendido, 1 = cerrado
    /// </summary>
    public static class AlphabetConstraintProfiles
    {
        // Constantes para rangos comunes
        private const float EXTENDED_MIN = 0f;
        private const float EXTENDED_MAX = 0.45f;

        private const float CURLED_MIN = 0.55f;
        private const float CURLED_MAX = 1f;

        private const float PARTIAL_MIN = 0.3f;
        private const float PARTIAL_MAX = 0.65f;

        // === DIFERENCIACIÓN CRÍTICA: Full Curl vs Tip Curl ===
        // Full Curl (A, S): Puño cerrado completo - dedos enrollados hasta la palma
        private const float FULL_CURL_MIN = 0.85f;
        private const float FULL_CURL_MAX = 1f;

        // Tip Curl (E, M, N): Solo puntas curvadas hasta los nudillos - NO es puño cerrado
        // Importante: maxCurl < FULL_CURL_MIN para evitar confusión con puño
        private const float TIP_CURL_MIN = 0.45f;
        private const float TIP_CURL_MAX = 0.75f;

        /// <summary>
        /// Crea el perfil para la letra A.
        /// Puño cerrado COMPLETO (full curl) con pulgar al lado de los dedos.
        /// DIFERENCIA con E/M/N: Los dedos están completamente cerrados en puño,
        /// NO solo las puntas curvadas.
        /// </summary>
        public static FingerConstraintProfile CreateLetterA()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "A";
            profile.description = "Fist with thumb beside fingers (FULL CURL - not tip curl)";

            profile.thumb = new ThumbConstraint
            {
                // Pulgar extendido recto al costado del puño
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = 0.35f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                shouldBeBesideFingers = true,
                customMessageGeneric = "Mantén el pulgar recto al lado del puño (no encima)"
            };

            // FULL CURL: Dedos completamente cerrados formando puño
            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el índice completamente en puño"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el medio completamente en puño"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el anular completamente en puño"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el meñique completamente en puño"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra B.
        /// Dedos extendidos juntos, pulgar cruzado sobre la palma.
        /// </summary>
        public static FingerConstraintProfile CreateLetterB()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "B";
            profile.description = "Fingers extended together, thumb across palm";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.5f, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Tuck your thumb firmly across your palm"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger straight up",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = -2f, maxSpreadAngle = 8f, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Keep fingers together (no gaps)"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your middle finger straight up",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = -2f, maxSpreadAngle = 8f, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Keep fingers together (no gaps)"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your ring finger straight up",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = -2f, maxSpreadAngle = 8f, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Keep fingers together (no gaps)"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your pinky straight up"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra C.
        /// Mano en forma de C (dedos curvados).
        /// </summary>
        public static FingerConstraintProfile CreateLetterC()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "C";
            profile.description = "Hand curved in C shape";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.2f, maxCurl = 0.5f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Curve your thumb to form a C shape"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = PARTIAL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curve your index finger more",
                customMessageTooCurled = "Don't curl your index finger so much"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = PARTIAL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curve your middle finger more",
                customMessageTooCurled = "Don't curl your middle finger so much"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = PARTIAL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curve your ring finger more",
                customMessageTooCurled = "Don't curl your ring finger so much"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = PARTIAL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curve your pinky more",
                customMessageTooCurled = "Don't curl your pinky so much"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra D.
        /// Índice extendido, otros dedos tocan el pulgar.
        /// </summary>
        public static FingerConstraintProfile CreateLetterD()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "D";
            profile.description = "Index extended, others touch thumb";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Touch thumb to middle finger"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger straight"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.8f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl middle finger to touch thumb"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.8f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curl ring finger to touch thumb"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.8f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curl pinky to touch thumb"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra E.
        /// TIP CURL: Solo las puntas curvadas hasta los nudillos, NO puño cerrado.
        /// DIFERENCIA con A/S: Los dedos NO están completamente cerrados,
        /// solo las puntas tocan la palma superior.
        /// </summary>
        public static FingerConstraintProfile CreateLetterE()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "E";
            profile.description = "Fingertips curved to palm (TIP CURL - not full fist)";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Dobla el pulgar bajo los dedos"
            };

            // TIP CURL: Puntas curvadas pero NO puño cerrado
            // maxCurl < FULL_CURL_MIN para rechazar puño cerrado
            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curva las puntas de los dedos hacia la palma",
                customMessageTooCurled = "No cierres el puño completo, solo curva las puntas"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curva las puntas de los dedos hacia la palma",
                customMessageTooCurled = "No cierres el puño completo, solo curva las puntas"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curva las puntas de los dedos hacia la palma",
                customMessageTooCurled = "No cierres el puño completo, solo curva las puntas"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curva las puntas de los dedos hacia la palma",
                customMessageTooCurled = "No cierres el puño completo, solo curva las puntas"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra F.
        /// Índice y pulgar forman círculo, resto extendidos.
        /// </summary>
        public static FingerConstraintProfile CreateLetterF()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "F";
            profile.description = "Index and thumb form circle, others extended";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                shouldTouchIndex = true,
                customMessageGeneric = "Touch your thumb to index fingertip"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Curve index to touch thumb tip"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your middle finger"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your ring finger"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your pinky"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra G.
        /// Pulgar e índice apuntando hacia el lado.
        /// </summary>
        public static FingerConstraintProfile CreateLetterG()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "G";
            profile.description = "Thumb and index pointing sideways";
            profile.checkOrientation = true;
            profile.expectedPalmDirection = Vector3.right; // palmar forward hacia la derecha para mano derecha

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = PARTIAL_MIN, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your thumb pointing out"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your middle finger"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curl your ring finger"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curl your pinky"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra H.
        /// Índice y medio extendidos horizontalmente.
        /// </summary>
        public static FingerConstraintProfile CreateLetterH()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "H";
            profile.description = "Index and middle extended horizontally";
            profile.checkOrientation = true;
            profile.expectedPalmDirection = Vector3.right;

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Tuck your thumb in"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your middle finger"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your ring finger"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curl your pinky"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra I.
        /// Solo meñique extendido.
        /// </summary>
        public static FingerConstraintProfile CreateLetterI()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "I";
            profile.description = "Only pinky extended";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Tuck your thumb in"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your index finger"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your middle finger"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your ring finger"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your pinky straight up"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra J (trayectoria con índice extendido).
        /// Usamos la pose inicial con índice extendido.
        /// </summary>
        public static FingerConstraintProfile CreateLetterJ()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "J";
            profile.description = "Index extended (draw J)";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Recoge el pulgar"
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
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el medio"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra K.
        /// Índice y medio extendidos con pulgar tocando medio (similar a V con pulgar).
        /// </summary>
        public static FingerConstraintProfile CreateLetterK()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "K";
            profile.description = "Index and middle extended, thumb touching middle";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                shouldTouchMiddle = true,
                customMessageGeneric = "Toca el medio con el pulgar"
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
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra M.
        /// TIP CURL: Pulgar debajo de TRES dedos (índice, medio, anular).
        /// Meñique extendido.
        /// DIFERENCIA con A/S: NO es puño cerrado, los 3 dedos usan tip curl sobre el pulgar.
        /// DIFERENCIA con N: M tiene 3 dedos sobre el pulgar, N solo tiene 2.
        /// </summary>
        public static FingerConstraintProfile CreateLetterM()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "M";
            profile.description = "Thumb under THREE fingers (tip curl), pinky extended";

            profile.thumb = new ThumbConstraint
            {
                // Pulgar debajo de los 3 dedos
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.8f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Esconde el pulgar bajo índice, medio y anular"
            };

            // TIP CURL para los 3 dedos que cubren el pulgar
            // NO full curl - el pulgar ocupa espacio debajo
            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curva el índice sobre el pulgar",
                customMessageTooCurled = "No cierres el puño, solo curva sobre el pulgar"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curva el medio sobre el pulgar",
                customMessageTooCurled = "No cierres el puño, solo curva sobre el pulgar"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curva el anular sobre el pulgar",
                customMessageTooCurled = "No cierres el puño, solo curva sobre el pulgar"
            };

            // Meñique extendido - característica distintiva de M
            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "El meñique debe estar extendido en la M"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra N.
        /// TIP CURL: Pulgar debajo de DOS dedos (índice y medio).
        /// Anular y meñique extendidos.
        /// DIFERENCIA con M: N tiene 2 dedos sobre el pulgar, M tiene 3.
        /// DIFERENCIA con A/S: NO es puño cerrado, los 2 dedos usan tip curl.
        /// </summary>
        public static FingerConstraintProfile CreateLetterN()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "N";
            profile.description = "Thumb under TWO fingers (tip curl), ring/pinky extended";

            profile.thumb = new ThumbConstraint
            {
                // Pulgar debajo de índice y medio
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.8f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Esconde el pulgar bajo índice y medio"
            };

            // TIP CURL para los 2 dedos que cubren el pulgar
            // NO full curl - el pulgar ocupa espacio debajo
            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curva el índice sobre el pulgar",
                customMessageTooCurled = "No cierres el puño, solo curva sobre el pulgar"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curva el medio sobre el pulgar",
                customMessageTooCurled = "No cierres el puño, solo curva sobre el pulgar"
            };

            // Anular y meñique extendidos - característica distintiva de N
            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "El anular debe estar extendido en la N"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "El meñique debe estar extendido en la N"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra P (similar a K, sin orientación).
        /// </summary>
        public static FingerConstraintProfile CreateLetterP()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "P";
            profile.description = "K shape tilted (thumb touching middle)";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                shouldTouchMiddle = true,
                customMessageGeneric = "Toca el medio con el pulgar"
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
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra Q (similar a G, sin orientación).
        /// </summary>
        public static FingerConstraintProfile CreateLetterQ()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "Q";
            profile.description = "Thumb and index pointing down (G mirrored)";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = PARTIAL_MIN, isEnabled = true, severityIfOutOfRange = Severity.Major },
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
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el medio"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra R.
        /// Índice y medio cruzados (aproximado con curvatura parcial).
        /// </summary>
        public static FingerConstraintProfile CreateLetterR()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "R";
            profile.description = "Index and middle crossed (approximated)";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Recoge el pulgar"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = PARTIAL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cruza el índice sobre el medio"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = PARTIAL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cruza el medio bajo el índice"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra T.
        /// CARACTERÍSTICA DISTINTIVA: Pulgar sobresale entre índice y medio.
        /// Los dedos están parcialmente curvados (tip curl) sobre el pulgar.
        /// DIFERENCIA con A/S: El pulgar está ENTRE los dedos, no al lado ni encima.
        /// DIFERENCIA con E: El pulgar está entre índice/medio, no debajo de todos.
        /// </summary>
        public static FingerConstraintProfile CreateLetterT()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "T";
            profile.description = "Thumb between index and middle (tip curl over thumb)";

            profile.thumb = new ThumbConstraint
            {
                // El pulgar sobresale entre índice y medio - parcialmente curvado
                curl = new CurlConstraint { minCurl = 0.2f, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Coloca el pulgar entre el índice y medio",
                customMessageTooCurled = "El pulgar debe sobresalir entre índice y medio"
            };

            // Índice y medio: tip curl (parcialmente curvados sobre el pulgar)
            // NO full curl - el pulgar ocupa espacio entre ellos
            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = 0.85f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curva el índice sobre el pulgar",
                customMessageTooCurled = "No cierres tanto, el pulgar debe estar entre los dedos"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = 0.85f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curva el medio sobre el pulgar",
                customMessageTooCurled = "No cierres tanto, el pulgar debe estar entre los dedos"
            };

            // Anular y meñique: más cerrados (no tienen el pulgar debajo)
            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = 0.7f, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = 0.7f, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra U.
        /// Índice y medio extendidos juntos; resto cerrados.
        /// </summary>
        public static FingerConstraintProfile CreateLetterU()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "U";
            profile.description = "Index and middle extended together";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Recoge el pulgar"
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
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra X.
        /// Índice en forma de gancho; resto cerrados.
        /// </summary>
        public static FingerConstraintProfile CreateLetterX()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "X";
            profile.description = "Hooked index, others curled";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Recoge el pulgar"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = 0.5f, maxCurl = 0.9f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curva el índice (gancho)",
                customMessageTooCurled = "No cierres tanto el índice"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el medio"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra Z (trayectoria, usamos índice extendido).
        /// </summary>
        public static FingerConstraintProfile CreateLetterZ()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "Z";
            profile.description = "Index draws Z (index extended)";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Recoge el pulgar"
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
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el medio"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el meñique"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra L.
        /// Forma de L con pulgar e índice extendidos.
        /// </summary>
        public static FingerConstraintProfile CreateLetterL()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "L";
            profile.description = "L shape with thumb and index extended";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = PARTIAL_MIN, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your thumb out to form L"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger straight"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your middle finger"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curl your ring finger"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curl your pinky"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra O.
        /// Forma de O: todos los dedos curvados formando círculo.
        /// </summary>
        public static FingerConstraintProfile CreateLetterO()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "O";
            profile.description = "O shape: all fingers curved forming circle";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Curve thumb to form O shape"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Curve fingers to form O shape"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Curve fingers to form O shape"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Curve fingers to form O shape"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Curve fingers to form O shape"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra S.
        /// Puño cerrado COMPLETO (full curl) con pulgar sobre los dedos.
        /// DIFERENCIA con A: El pulgar va ENCIMA de los dedos cerrados.
        /// DIFERENCIA con E/M/N: Es puño completo, no tip curl.
        /// </summary>
        public static FingerConstraintProfile CreateLetterS()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "S";
            profile.description = "Fist with thumb OVER fingers (FULL CURL)";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                shouldBeOverFingers = true,
                customMessageGeneric = "Coloca el pulgar sobre los dedos cerrados"
            };

            // FULL CURL: Puño cerrado completo
            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el índice completamente en puño"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el medio completamente en puño"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el anular completamente en puño"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el meñique completamente en puño"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra V.
        /// Índice y medio extendidos en forma de V.
        /// </summary>
        public static FingerConstraintProfile CreateLetterV()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "V";
            profile.description = "Index and middle extended in V shape";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Tuck your thumb in"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger for V"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your middle finger for V"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your ring finger down"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your pinky down"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra W.
        /// Índice, medio y anular extendidos.
        /// </summary>
        public static FingerConstraintProfile CreateLetterW()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "W";
            profile.description = "Index, middle and ring extended";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Tuck your thumb in"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger for W"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your middle finger for W"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your ring finger for W"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your pinky down"
            };

            return profile;
        }

        /// <summary>
        /// Crea el perfil para la letra Y.
        /// Pulgar y meñique extendidos (hang loose).
        /// </summary>
        public static FingerConstraintProfile CreateLetterY()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "Y";
            profile.description = "Thumb and pinky extended (hang loose)";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = PARTIAL_MIN, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your thumb out"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your index finger down"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your middle finger down"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your ring finger down"
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your pinky out"
            };

            return profile;
        }

        /// <summary>
        /// Crea todos los perfiles de letras disponibles.
        /// </summary>
        public static FingerConstraintProfile[] CreateAllAlphabetProfiles()
        {
            return new FingerConstraintProfile[]
            {
                CreateLetterA(),
                CreateLetterB(),
                CreateLetterC(),
                CreateLetterD(),
                CreateLetterE(),
                CreateLetterF(),
                CreateLetterG(),
                CreateLetterH(),
                CreateLetterI(),
                CreateLetterJ(),
                CreateLetterK(),
                CreateLetterL(),
                CreateLetterM(),
                CreateLetterN(),
                CreateLetterO(),
                CreateLetterP(),
                CreateLetterQ(),
                CreateLetterR(),
                CreateLetterS(),
                CreateLetterT(),
                CreateLetterU(),
                CreateLetterV(),
                CreateLetterW(),
                CreateLetterX(),
                CreateLetterY(),
                CreateLetterZ()
            };
        }

        /// <summary>
        /// Obtiene el perfil para una letra específica.
        /// </summary>
        public static FingerConstraintProfile GetLetterProfile(string letter)
        {
            return letter?.ToUpper() switch
            {
                "A" => CreateLetterA(),
                "B" => CreateLetterB(),
                "C" => CreateLetterC(),
                "D" => CreateLetterD(),
                "E" => CreateLetterE(),
                "F" => CreateLetterF(),
                "G" => CreateLetterG(),
                "H" => CreateLetterH(),
                "I" => CreateLetterI(),
                "J" => CreateLetterJ(),
                "K" => CreateLetterK(),
                "L" => CreateLetterL(),
                "M" => CreateLetterM(),
                "N" => CreateLetterN(),
                "O" => CreateLetterO(),
                "P" => CreateLetterP(),
                "Q" => CreateLetterQ(),
                "R" => CreateLetterR(),
                "S" => CreateLetterS(),
                "T" => CreateLetterT(),
                "U" => CreateLetterU(),
                "V" => CreateLetterV(),
                "W" => CreateLetterW(),
                "X" => CreateLetterX(),
                "Y" => CreateLetterY(),
                "Z" => CreateLetterZ(),
                _ => null
            };
        }
    }
}
