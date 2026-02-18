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
        // Full Curl (A, S): Full fist — fingers curled all the way into the palm
        private const float FULL_CURL_MIN = 0.85f;
        private const float FULL_CURL_MAX = 1f;

        // Tip Curl (E, M, N): Only fingertips curl toward the knuckles — NOT a closed fist
        // Important: maxCurl < FULL_CURL_MIN to avoid confusion with a full fist
        private const float TIP_CURL_MIN = 0.55f;
        private const float TIP_CURL_MAX = 0.78f;

        /// <summary>
        /// Creates the profile for letter A.
        /// FULL FIST (full curl) with the thumb beside the fingers.
        /// DIFFERENCE vs E/M/N: fingers are fully closed into a fist,
        /// NOT just fingertip curl.
        /// </summary>
        public static FingerConstraintProfile CreateLetterA()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "A";
            profile.description = "Fist with thumb beside fingers (FULL CURL - not tip curl)";

            profile.thumb = new ThumbConstraint
            {
                // Thumb extended straight along the side of the fist
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = 0.35f, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                shouldBeBesideFingers = true,
                customMessageGeneric = "Keep the thumb straight beside the fist (not over the fingers)."
            };

            // FULL CURL: fingers fully closed into a fist
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
        /// Fingers extended together, thumb across the palm.
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
        /// Hand in a C-shape (fingers curved).
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
        /// Index extended; other fingers curved toward the thumb.
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
        /// TIP CURL: only the fingertips curl toward the knuckles, NOT a full fist.
        /// DIFFERENCE vs A/S: fingers are NOT fully closed;
        /// only the fingertips curl inward.
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
        /// Index and thumb form a circle; others extended.
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
        /// Thumb and index pointing sideways (horizontal).
        /// </summary>
        public static FingerConstraintProfile CreateLetterG()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "G";
            profile.description = "Thumb and index extended SIDEWAYS (horizontal), parallel to each other.";
            profile.checkOrientation = true;
            profile.expectedPalmDirection = Vector3.right; // sideways for right hand
            profile.orientationHint = "Gira la mano de lado (horizontal).";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = PARTIAL_MIN, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Estira el pulgar apuntando hacia fuera, en paralelo al indice.",
                customMessageGeneric = "El pulgar y el indice deben apuntar en paralelo (ambos horizontales)."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Estira el indice apuntando hacia fuera, en paralelo al pulgar.",
                customMessageGeneric = "El indice y el pulgar deben apuntar en paralelo (ambos horizontales)."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el dedo medio."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el menique."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter H.
        /// Index and middle extended sideways (horizontal).
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
        /// Only pinky extended.
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
        /// Creates the profile for letter J (trajectory with index extended).
        /// Uses the starting pose with index extended.
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
        /// Index and middle extended, with thumb touching middle (like a V with the thumb).
        /// </summary>
        public static FingerConstraintProfile CreateLetterK()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "K";
            profile.description = "Index and middle extended in V, thumb touching middle finger";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = 0.6f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                shouldTouchMiddle = true,
                customMessageTooExtended = "Acerca el pulgar al dedo medio hasta tocarlo.",
                customMessageTooCurled = "Acerca el pulgar al dedo medio hasta tocarlo.",
                customMessageNeedsCurve = "Acerca el pulgar al dedo medio hasta tocarlo.",
                customMessageNeedsExtend = "Acerca el pulgar al dedo medio hasta tocarlo.",
                customMessageGeneric = "Acerca el pulgar al dedo medio hasta tocarlo."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Estira el indice.",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = 8f, maxSpreadAngle = 30f, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Separa el indice del medio formando una V."
            };

            // NOTE: No spread constraint on middle - only index measures the V gap.
            // Spread on middle would measure middle-vs-ring, which is meaningless when ring is curled.
            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Estira el dedo medio."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el menique."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter M.
        /// TIP CURL: thumb under THREE fingers (index, middle, ring).
        /// Pinky is also closed (not extended).
        /// DIFFERENCE vs A/S: NOT a full fist; three fingers tip-curl over the thumb.
        /// DIFFERENCE vs N: M has three fingers over the thumb; N has two.
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
        /// TIP CURL: thumb between middle and ring (only two fingers cover it).
        /// Ring and pinky are closed (not extended).
        /// DIFFERENCE vs M: N has two fingers over the thumb; M has three.
        /// DIFFERENCE vs A/S: NOT a full fist; two fingers tip-curl.
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
        /// Creates the profile for letter P (similar to K, with downward orientation).
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
                customMessageTooExtended = "Acerca el pulgar al dedo medio hasta tocarlo.",
                customMessageTooCurled = "Acerca el pulgar al dedo medio hasta tocarlo.",
                customMessageNeedsCurve = "Acerca el pulgar al dedo medio hasta tocarlo.",
                customMessageGeneric = "Acerca el pulgar al dedo medio hasta tocarlo."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Estira el indice.",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = 8f, maxSpreadAngle = 30f, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Separa el indice del medio formando una V."
            };

            // NOTE: No spread constraint on middle - only index measures the V gap.
            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Estira el dedo medio."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el anular."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Cierra el menique."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter Q (like G, rotated downward).
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
        /// Index and middle crossed (approximated with partial curl).
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
        /// DISTINCTIVE FEATURE: the thumb rests on the pinky side (not over the whole fist).
        /// Fingers form a fist holding the thumb on the side.
        /// DIFFERENCE vs S: thumb goes to the pinky side, not across the full fist.
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
        /// Index and middle extended together; others curled.
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
        /// Index in a hook shape (CURVED, not closed); others curled.
        /// </summary>
        public static FingerConstraintProfile CreateLetterX()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "X";
            profile.description = "Index in a HOOK shape (CURVED, not closed). Other fingers curled.";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Tuck your thumb in."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                // Hook: CURVED (0.4–0.7) — NOT closed, NOT extended
                // Range tuned so the semantic system recognizes it as CURVED (midpoint ~0.55)
                curl = new CurlConstraint { minCurl = 0.4f, maxCurl = 0.7f, isEnabled = true, severityIfOutOfRange = Severity.Major },
                expectedState = FingerShapeState.Curved,
                customMessageTooExtended = "Curve your index into a hook (don't keep it straight).",
                customMessageTooCurled = "Don't fully close the index—keep it CURVED, not a fist.",
                customMessageNeedsCurve = "Curve your index into a hook shape.",
                customMessageNeedsFist = "The index should be CURVED (hook), not closed into a fist.",
                customMessageGeneric = "The index should be CURVED like a hook (without touching the palm)."
            };

            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Curl your middle finger (only the index is hooked).",
                customMessageGeneric = "Keep the middle finger curled; only the index is a hook."
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
        /// Creates the profile for letter Z (trajectory; uses index extended).
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
        /// L-shape with thumb and index extended.
        /// </summary>
        public static FingerConstraintProfile CreateLetterL()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "L";
            profile.description = "L shape with thumb and index extended";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = PARTIAL_MIN, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your thumb out to form an L."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Extend your index finger straight."
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
        /// O-shape: all fingers curved to form a circle.
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
        /// FULL FIST (full curl) with the thumb over the fingers.
        /// DIFFERENCE vs A: the thumb goes OVER the curled fingers.
        /// DIFFERENCE vs E/M/N: full fist, not tip curl.
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

            // FULL CURL: full fist
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
        /// Index and middle extended with a V gap.
        /// </summary>
        public static FingerConstraintProfile CreateLetterV()
        {
            var profile = ScriptableObject.CreateInstance<FingerConstraintProfile>();
            profile.signName = "V";
            profile.description = "Index and middle extended with a CLEAR V gap (spread).";

            profile.thumb = new ThumbConstraint
            {
                curl = new CurlConstraint { minCurl = PARTIAL_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Minor },
                customMessageTooExtended = "Recoge el pulgar contra la palma."
            };

            profile.index = new FingerConstraint
            {
                finger = Finger.Index,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Estira el indice para formar la V.",
                spread = new SpreadConstraint { isEnabled = true, minSpreadAngle = 8f, maxSpreadAngle = 30f, severityIfOutOfRange = Severity.Major },
                customMessageGeneric = "Separa el indice del medio para formar una V clara."
            };

            // NOTE: No spread constraint on middle - only index measures the V gap.
            // Spread on middle would measure middle-vs-ring, which is meaningless when ring is curled.
            profile.middle = new FingerConstraint
            {
                finger = Finger.Middle,
                curl = new CurlConstraint { minCurl = EXTENDED_MIN, maxCurl = EXTENDED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooCurled = "Estira el medio para formar la V."
            };

            profile.ring = new FingerConstraint
            {
                finger = Finger.Ring,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el anular."
            };

            profile.pinky = new FingerConstraint
            {
                finger = Finger.Pinky,
                curl = new CurlConstraint { minCurl = CURLED_MIN, maxCurl = CURLED_MAX, isEnabled = true, severityIfOutOfRange = Severity.Major },
                customMessageTooExtended = "Cierra el menique."
            };

            return profile;
        }

        /// <summary>
        /// Creates the profile for letter W.
        /// Index, middle and ring extended with separation.
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
        /// Thumb and pinky extended (hang loose).
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
