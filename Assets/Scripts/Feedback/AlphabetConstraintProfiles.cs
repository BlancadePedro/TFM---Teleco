using UnityEngine;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Predefined finger-constraint profiles for ASL alphabet letters (A–Z).
    /// Use with HandPoseAnalyzer to provide per-finger feedback.
    ///
    /// Curl values: 0 = extended, 1 = fully curled
    /// </summary>
    public static class AlphabetConstraintProfiles
    {
        // Common ranges
        private const float EXTENDED_MIN = 0f;
        private const float EXTENDED_MAX = 0.45f;

        private const float CURLED_MIN = 0.55f;
        private const float CURLED_MAX = 1f;

        private const float PARTIAL_MIN = 0.3f;
        private const float PARTIAL_MAX = 0.65f;

        // === CRITICAL DIFFERENTIATION: Full Curl vs Tip Curl ===
        // Full Curl (A, S): Full fist completo - dedos enrollados hasta la palma
        private const float FULL_CURL_MIN = 0.85f;
        private const float FULL_CURL_MAX = 1f;

        // Tip Curl (E, M, N): Solo puntas curvadas hasta los nudillos - NO es puño cerrado
        // Importante: maxCurl < FULL_CURL_MIN para evitar confusión con puño
        private const float TIP_CURL_MIN = 0.55f;
        private const float TIP_CURL_MAX = 0.78f;

        /// <summary>
        /// Creates the profile for letter A.
        /// Full fist COMPLETO (full curl) con thumb al lado de los dedos.
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
                customMessageGeneric = "Keep the thumb straight beside the fist (not over the fingers)."
            };

            // FULL CURL: Fingers completamente cerrados formando puño
            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your index finger fully into a fist."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your middle finger fully into a fist."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your ring finger fully into a fist."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your pinky fully into a fist."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter B.
        /// Fingers extendidos juntos, thumb cruzado sobre la palma.
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
        /// Creates the profile for letter C.
        /// Hand en forma de C (dedos curvados).
        /// </summary>
        public static FingerConstraintProfile CreateLetterC()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "C";
            profile.description = "C-shape: curve ALL fingers (no fist). Fingertips must NOT touch the palm.";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.2f, maxCurl = 0.5f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Curve your thumb to form a C (do not press it into the palm)."
            };

            // Curve fingers (not extended) but also NOT fully curled into a fist / palm contact.
            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.65f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curve your index finger to form a C (leave space; don't keep it straight).",
                customMessageTooCurled = "Don't make a fist—keep a C-shape (no palm contact).",
                customMessageGeneric = "Keep a rounded C-shape (no fingertip touching the palm)."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.65f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curve your middle finger to form a C (leave space).",
                customMessageTooCurled = "Don't curl into a fist—keep the C-shape open.",
                customMessageGeneric = "Rounded C-shape (no palm contact)."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.65f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curve your ring finger to match the C-shape.",
                customMessageTooCurled = "Don't close into a fist—keep it rounded.",
                customMessageGeneric = "Keep the ring finger curved (no palm contact)."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.65f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curve your pinky to match the C-shape.",
                customMessageTooCurled = "Don't close into a fist—keep it rounded.",
                customMessageGeneric = "Keep the pinky curved (no palm contact)."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter D.
        /// Index extendido, otros dedos tocan el thumb.
        /// </summary>
        public static FingerConstraintProfile CreateLetterD()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "D";
            profile.description = "Index extended. Middle/ring/pinky are curved toward the thumb WITHOUT touching the palm.";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.75f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Bring your thumb to touch the middle fingertip (or the side of the middle finger)."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger straight."
            };

            // Curved fingers: NOT extended, NOT a fist (no palm contact).
            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.78f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curve your middle finger toward the thumb (do NOT touch the palm).",
                customMessageTooCurled = "Don't make a fist—keep the finger curved with a small gap to the palm.",
                customMessageGeneric = "Curve toward the thumb without palm contact."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.78f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curve your ring finger (do NOT touch the palm).",
                customMessageTooCurled = "Don't close into a fist—keep a small gap to the palm.",
                customMessageGeneric = "Curved ring finger, no palm contact."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.78f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curve your pinky (do NOT touch the palm).",
                customMessageTooCurled = "Don't close into a fist—keep a small gap to the palm.",
                customMessageGeneric = "Curved pinky, no palm contact."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter E.
        /// TIP CURL: Solo las puntas curvadas hasta los nudillos, NO puño cerrado.
        /// DIFERENCIA con A/S: Los dedos NO están completamente cerrados,
        /// solo las puntas tocan la palma superior.
        /// </summary>
        public static FingerConstraintProfile CreateLetterE()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "E";
            profile.description = "TIP CURL: curl ONLY the fingertips toward the knuckles. NOT a full fist (no full curl).";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.75f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Tuck your thumb under the fingers."
            };

            // TIP CURL: fingertips curled, but NOT full fist.
            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl the fingertip (tip curl). Don't keep it straight.",
                customMessageTooCurled = "Don't make a fist—only curl to the knuckles (tip curl)."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl the fingertip (tip curl).",
                customMessageTooCurled = "Don't make a fist—only curl to the knuckles (tip curl)."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl the fingertip (tip curl).",
                customMessageTooCurled = "Don't make a fist—only curl to the knuckles (tip curl)."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl the fingertip (tip curl).",
                customMessageTooCurled = "Don't make a fist—only curl to the knuckles (tip curl)."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter F.
        /// Index y thumb forman círculo, resto extendidos.
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
        /// Creates the profile for letter G.
        /// Pulgar e index apuntando hacia el lado.
        /// </summary>
        public static FingerConstraintProfile CreateLetterG()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "G";
            profile.description = "Thumb and index extended SIDEWAYS (horizontal).";
            profile.checkOrientation = true;
            profile.expectedPalmDirection = Vector3.right; // sideways for right hand
            profile.orientationHint = "Hold your hand sideways (horizontal).";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = PARTIAL_MIN, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your thumb pointing out (sideways)."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger (sideways)."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your middle finger."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curl your ring finger."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curl your pinky."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter H.
        /// Index y middle extendidos horizontally.
        /// </summary>
        public static FingerConstraintProfile CreateLetterH()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "H";
            profile.description = "Index and middle extended SIDEWAYS (horizontal), together.";
            profile.checkOrientation = true;
            profile.expectedPalmDirection = Vector3.right;
            profile.orientationHint = "Hold your hand sideways (horizontal).";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Tuck your thumb in."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your middle finger."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your ring finger."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curl your pinky."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter I.
        /// Solo pinky extendido.
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
        /// Creates the profile for letter J (trajectory con index extendido).
        /// Usamos la pose inicial con index extendido.
        /// </summary>
        public static FingerConstraintProfile CreateLetterJ()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "J";
            profile.description = "Index extended (draw J)";

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
        /// Creates the profile for letter K.
        /// Index y middle extendidos con thumb tocando middle (similar a V con thumb).
        /// </summary>
        public static FingerConstraintProfile CreateLetterK()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "K";
            profile.description = "Index and middle extended, thumb touching middle";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                shouldTouchMiddle = true,
                customMessageTooExtended = "Toca el pulgar con el dedo corazón",
                customMessageTooCurled = "Toca el pulgar con el dedo corazón",
                customMessageGeneric = "Toca el pulgar con el dedo corazón"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el dedo índice",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = 8f, maxSpreadAngle = 30f, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Separa el índice y el corazón (en V)"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el dedo corazón",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = 8f, maxSpreadAngle = 30f, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Mantén separación entre índice y corazón"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el dedo anular"
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
        /// Creates the profile for letter M.
        /// TIP CURL: Pulgar debajo de TRES dedos (indice, middle, ring).
        /// Menique tambien cerrado (no extendido).
        /// DIFERENCIA con A/S: NO es fist cerrado, los 3 dedos usan tip curl sobre el thumb.
        /// DIFERENCIA con N: M tiene 3 dedos sobre el thumb, N solo tiene 2.
        /// </summary>
        public static FingerConstraintProfile CreateLetterM()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "M";
            profile.description = "Thumb tucked BETWEEN ring and middle; covered by THREE fingers (index/middle/ring). Pinky closed.";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.8f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                shouldTouchMiddle = true,
                shouldTouchRing = true,
                customMessageTooExtended = "Tuck the thumb in between the middle and ring fingers.",
                customMessageGeneric = "Thumb goes between the middle and ring; three fingers cover it."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your index over the thumb (tip curl).",
                customMessageTooCurled = "Don't make a fist—only curl over the thumb."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your middle over the thumb (tip curl).",
                customMessageTooCurled = "Don't make a fist—only curl over the thumb."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your ring finger over the thumb (tip curl).",
                customMessageTooCurled = "Don't make a fist—only curl over the thumb."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = 0.7f, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your pinky."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter N.
        /// TIP CURL: Pulgar entre middle y ring (solo dos dedos lo cubren).
        /// Anular y pinky cerrados (no extendidos).
        /// DIFERENCIA con M: N tiene 2 dedos sobre el thumb, M tiene 3.
        /// DIFERENCIA con A/S: NO es fist cerrado, los 2 dedos usan tip curl.
        /// </summary>
        public static FingerConstraintProfile CreateLetterN()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "N";
            profile.description = "Thumb tucked BETWEEN ring and middle; covered by TWO fingers (index/middle). Ring & pinky closed.";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.8f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                shouldTouchMiddle = true,
                shouldTouchRing = true,
                customMessageTooExtended = "Tuck the thumb in between the middle and ring fingers.",
                customMessageGeneric = "Thumb goes between the middle and ring; only two fingers cover it."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your index over the thumb (tip curl).",
                customMessageTooCurled = "Don't make a fist—only curl over the thumb."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = TIP_CURL_MIN, maxCurl = TIP_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your middle over the thumb (tip curl).",
                customMessageTooCurled = "Don't make a fist—only curl over the thumb."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = 0.7f, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your ring finger."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = 0.7f, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your pinky."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter P (similar a K, sin orientación).
        /// </summary>
        public static FingerConstraintProfile CreateLetterP()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "P";
            profile.description = "K-shape tilted DOWNWARD (palm facing down).";
            profile.checkOrientation = true;
            profile.expectedPalmDirection = Vector3.down;
            profile.orientationHint = "Point the hand downward (palm facing the floor).";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                shouldTouchMiddle = true,
                customMessageTooExtended = "Toca el pulgar con el dedo corazón",
                customMessageTooCurled = "Toca el pulgar con el dedo corazón",
                customMessageGeneric = "Toca el pulgar con el dedo corazón"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el dedo índice",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = 8f, maxSpreadAngle = 30f, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Separa el índice y el corazón (en V)"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extiende el dedo corazón",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = 8f, maxSpreadAngle = 30f, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Mantén separación entre índice y corazón"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el dedo anular"
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
        /// Creates the profile for letter Q (similar a G, sin orientación).
        /// </summary>
        public static FingerConstraintProfile CreateLetterQ()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "Q";
            profile.description = "Thumb and index pointing DOWNWARD (G rotated down).";
            profile.checkOrientation = true;
            profile.expectedPalmDirection = Vector3.down;
            profile.orientationHint = "Point the hand downward (palm facing the floor).";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = PARTIAL_MIN, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your thumb."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your middle finger."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curl your ring finger."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Curl your pinky."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter R.
        /// Index y middle cruzados (aproximado con curvatura parcial).
        /// </summary>
        public static FingerConstraintProfile CreateLetterR()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "R";
            profile.description = "Index and middle crossed (approximated)";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Tuck your thumb in"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = PARTIAL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cross your index over your middle finger."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = PARTIAL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cross your middle under your index finger."
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
        /// Creates the profile for letter T.
        /// CARACTERISTICA DISTINTIVA: Pulgar se apoya sobre el pinky (no sobre todos los dedos).
        /// Fingers en fist sujetando el thumb en el costado.
        /// DIFERENCIA con S: El thumb va al pinky, no encima del fist completo.
        /// </summary>
        public static FingerConstraintProfile CreateLetterT()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "T";
            profile.description = "Fist with the THUMB resting on the PINKY side (distinct from S).";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.3f, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                shouldTouchPinky = true,
                customMessageGeneric = "Place the thumb over the pinky side (not over the whole fist like S)."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your index finger into a fist (thumb stays on the pinky side)."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your middle finger into a fist."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = 0.7f, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Close your ring finger (make the fist firm)."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = 0.7f, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Close your pinky—the thumb should rest here."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter U.
        /// Index y middle extendidos juntos; resto cerrados.
        /// </summary>
        public static FingerConstraintProfile CreateLetterU()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "U";
            profile.description = "Index and middle extended together";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
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
        /// Creates the profile for letter X.
        /// Index en forma de gancho (CURVO, no cerrado); resto cerrados.
        /// </summary>
        public static FingerConstraintProfile CreateLetterX()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "X";
            profile.description = "Index in a HOOK shape (CURVED, not closed). Other fingers curled.";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Recoge el pulgar"
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                // Hook/Gancho: CURVO (0.4-0.7) - NO cerrado, NO extendido
                // Rango ajustado para que el sistema semántico lo reconozca como CURVED (midpoint ~0.55)
                curl = new CurlConstraint { minCurl = 0.4f, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                expectedState = FingerShapeState.Curved,
                customMessageTooExtended = "Curva el índice en forma de gancho (no lo dejes recto)",
                customMessageTooCurled = "No cierres el índice del todo - debe estar CURVO, no cerrado",
                customMessageNeedsCurve = "Curva el índice en forma de gancho",
                customMessageNeedsFist = "El índice debe estar CURVO (gancho), no cerrado en puño",
                customMessageGeneric = "El índice debe estar CURVO en forma de gancho (sin tocar la palma)"
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el dedo corazón (solo el índice va en gancho)",
                customMessageGeneric = "Mantén el corazón cerrado; el gancho es solo para el índice"
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el dedo anular"
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
        /// Creates the profile for letter Z (trajectory, usamos index extendido).
        /// </summary>
        public static FingerConstraintProfile CreateLetterZ()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "Z";
            profile.description = "Index draws Z (index extended)";

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
        /// Creates the profile for letter L.
        /// Forma de L con thumb e index extendidos.
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
        /// Creates the profile for letter O.
        /// Forma de O: todos los dedos curvados formando círculo.
        /// </summary>
        public static FingerConstraintProfile CreateLetterO()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "O";
            profile.description = "O-shape: all fingers curved to form a round O. Fingertips must NOT touch the palm.";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Curve your thumb to help form a round O (do not press into the palm)."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Curve your fingers to form an O (no palm contact).",
                customMessageTooCurled = "Don't close into a fist—keep a rounded O with space inside."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Curve your fingers to form an O (no palm contact).",
                customMessageTooCurled = "Don't close into a fist—keep a rounded O."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Curve your fingers to form an O (no palm contact).",
                customMessageTooCurled = "Don't press into the palm—keep it rounded."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = 0.35f, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Curve your fingers to form an O (no palm contact).",
                customMessageTooCurled = "Don't press into the palm—keep it rounded."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter S.
        /// Full fist COMPLETO (full curl) con thumb sobre los dedos.
        /// DIFERENCIA con A: El thumb va ENCIMA de los dedos cerrados.
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
                customMessageGeneric = "Place the thumb OVER the curled fingers (full fist)."
            };

            // FULL CURL: Full fist completo
            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your index finger fully into a fist."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your middle finger fully into a fist."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your ring finger fully into a fist."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = FULL_CURL_MIN, maxCurl = FULL_CURL_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Close your pinky fully into a fist."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter V.
        /// Index y middle extendidos en forma de V.
        /// </summary>
        public static FingerConstraintProfile CreateLetterV()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "V";
            profile.description = "Index and middle extended with a CLEAR V gap (spread).";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Tuck your thumb in."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger for a V.",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = 8f, maxSpreadAngle = 30f, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Separate index and middle to form a V."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your middle finger for a V.",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = 8f, maxSpreadAngle = 30f, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Keep a clear gap between index and middle (V)."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your ring finger down."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your pinky down."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter W.
        /// Index, middle y ring extendidos.
        /// </summary>
        public static FingerConstraintProfile CreateLetterW()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "W";
            profile.description = "Index, middle and ring extended with separation (spread) between them.";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Tuck your thumb in."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger for W.",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = 6f, maxSpreadAngle = 28f, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Slightly separate the extended fingers (W)."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your middle finger for W.",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = 6f, maxSpreadAngle = 28f, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Keep space between index–middle and middle–ring."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your ring finger for W.",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = 4f, maxSpreadAngle = 24f, severityIfOutOfRange = Severity.Minor },
                customMessageGeneric = "Don't stick the ring to the middle—keep a little gap."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your pinky down."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter Y.
        /// Pulgar y pinky extendidos (hang loose).
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
        /// Creates all available letter profiles.
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
        /// Gets the profile for a specific letter.
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