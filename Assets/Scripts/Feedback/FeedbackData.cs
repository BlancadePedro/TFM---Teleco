using System;
using UnityEngine;

namespace ASL_LearnVR.Feedback
{
    #region Enums

    /// <summary>
    /// Identifier for each finger.
    /// </summary>
    public enum Finger
    {
        Thumb = 0,
        Index = 1,
        Middle = 2,
        Ring = 3,
        Pinky = 4
    }

    /// <summary>
    /// Severity of the detected error.
    /// </summary>
    public enum Severity
    {
        /// <summary>No error; the finger is correct.</summary>
        None = 0,
        /// <summary>Minor adjustment needed (shown in red).</summary>
        Minor = 1,
        /// <summary>Major error that blocks recognition (red).</summary>
        Major = 2
    }

    /// <summary>
    /// Semantic finger-shape state.
    /// Distinguishes three functional states for ASL:
    /// - Extended: straight finger
    /// - Curved: controlled bend without touching the palm (C, D, O, X, etc.)
    /// - Closed: fully folded into a fist (A, S, T, etc.)
    /// </summary>
    public enum FingerShapeState
    {
        /// <summary>
        /// Finger is straight/extended.
        /// Typical curl: 0.0 - 0.25
        /// </summary>
        Extended = 0,

        /// <summary>
        /// Finger is curved with control (not touching the palm).
        /// Typical curl: 0.25 - 0.75
        /// </summary>
        Curved,

        /// <summary>
        /// Finger is fully closed (fist/palm contact).
        /// Typical curl: 0.75 - 1.0
        /// </summary>
        Closed
    }

    /// <summary>
    /// Specific finger error type.
    /// Includes semantic errors that distinguish between curving and making a fist.
    /// </summary>
    public enum FingerErrorType
    {
        None = 0,

        // === Classic errors (compatibility) ===
        /// <summary>Finger is too extended (generic).</summary>
        TooExtended,
        /// <summary>Finger is too curled/closed (generic).</summary>
        TooCurled,

        // === Semantic 3-state errors ===
        /// <summary>
        /// Finger should CURVE (from extended to curved).
        /// </summary>
        NeedsCurve,

        /// <summary>
        /// Finger should CLOSE into a fist (from curved to closed).
        /// </summary>
        NeedsFist,

        /// <summary>
        /// Finger curled TOO MUCH (made a fist when it should only curve).
        /// </summary>
        TooMuchCurl,

        /// <summary>
        /// Finger should EXTEND (from curved/closed to extended).
        /// </summary>
        NeedsExtend,

        // === Spread/position errors ===
        /// <summary>Not enough separation between fingers.</summary>
        SpreadTooNarrow,
        /// <summary>Too much separation between fingers.</summary>
        SpreadTooWide,
        /// <summary>Thumb is in the wrong position.</summary>
        ThumbPositionWrong,
        /// <summary>Finger should be touching another finger.</summary>
        ShouldTouch,
        /// <summary>Finger should NOT be touching another finger.</summary>
        ShouldNotTouch,
        /// <summary>Wrong finger/hand rotation.</summary>
        RotationWrong
    }

    /// <summary>
    /// Failure reason for dynamic gestures.
    /// </summary>
    public enum FailureReason
    {
        None = 0,
        /// <summary>The hand pose was lost during the gesture.</summary>
        PoseLost,
        /// <summary>Movement was too slow.</summary>
        SpeedTooLow,
        /// <summary>Movement was too fast.</summary>
        SpeedTooHigh,
        /// <summary>Distance traveled was insufficient.</summary>
        DistanceTooShort,
        /// <summary>Movement direction was incorrect.</summary>
        DirectionWrong,
        /// <summary>Not enough direction changes (zigzag gestures).</summary>
        DirectionChangesInsufficient,
        /// <summary>Insufficient rotation.</summary>
        RotationInsufficient,
        /// <summary>Movement was not circular (when required).</summary>
        NotCircular,
        /// <summary>Gesture took too long.</summary>
        Timeout,
        /// <summary>End pose did not match.</summary>
        EndPoseMismatch,
        /// <summary>Hand tracking was lost.</summary>
        TrackingLost,
        /// <summary>Hand moved out of the required spatial zone.</summary>
        OutOfZone,
        /// <summary>Unknown/unclassified reason.</summary>
        Unknown
    }

    /// <summary>
    /// Phase of the dynamic gesture where the failure happened.
    /// </summary>
    public enum GesturePhase
    {
        /// <summary>Not applicable (static gesture or no failure).</summary>
        None = 0,
        /// <summary>Failure at the start pose.</summary>
        Start,
        /// <summary>Failure during movement.</summary>
        Move,
        /// <summary>Failure at the end pose / validation.</summary>
        End
    }

    /// <summary>
    /// Feedback phases for dynamic gestures.
    /// Each phase has a different type of feedback adapted to the moment in the gesture.
    /// </summary>
    public enum DynamicFeedbackPhase
    {
        /// <summary>
        /// Phase 0: Waiting. User must place the hand in the start pose.
        /// Feedback: general guidance, no fine per-finger corrections.
        /// </summary>
        Idle = 0,

        /// <summary>
        /// Phase 1: Start pose detected correctly.
        /// Feedback: positive confirmation + prompt to begin movement.
        /// </summary>
        StartDetected,

        /// <summary>
        /// Phase 2: Movement in progress.
        /// Feedback: direction, speed, amplitude, continuity.
        /// NO per-finger corrections.
        /// </summary>
        InProgress,

        /// <summary>
        /// Phase 3: Near completion (>80%).
        /// Feedback: encouragement to finish, avoid stopping early.
        /// </summary>
        NearCompletion,

        /// <summary>
        /// Phase 4: Completed successfully.
        /// Feedback: clear success confirmation.
        /// </summary>
        Completed,

        /// <summary>
        /// Phase 5: Failed.
        /// Feedback: why it failed + invite to retry.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Specific issue during a dynamic gesture (movement feedback).
    /// </summary>
    public enum DynamicMovementIssue
    {
        None = 0,
        /// <summary>Movement direction is incorrect.</summary>
        DirectionWrong,
        /// <summary>Movement is too fast.</summary>
        TooFast,
        /// <summary>Movement is too slow.</summary>
        TooSlow,
        /// <summary>Movement is too short (insufficient amplitude).</summary>
        TooShort,
        /// <summary>Movement is not continuous (paused/choppy).</summary>
        NotContinuous,
        /// <summary>Movement is not circular when it should be.</summary>
        NotCircular,
        /// <summary>Missing direction changes (zigzag gestures).</summary>
        NeedMoreDirectionChanges,
        /// <summary>Rotation is insufficient.</summary>
        RotationInsufficient,
        /// <summary>Hand pose is degrading during movement.</summary>
        StartPoseDegrading
    }

    /// <summary>
    /// Overall feedback state.
    /// </summary>
    public enum FeedbackState
    {
        /// <summary>Feedback inactive.</summary>
        Inactive,
        /// <summary>Waiting for the user to perform the gesture.</summary>
        Waiting,
        /// <summary>Detecting errors and showing corrections.</summary>
        ShowingErrors,
        /// <summary>Partially correct gesture.</summary>
        PartialMatch,
        /// <summary>Fully correct gesture.</summary>
        Success,
        /// <summary>Dynamic gesture in progress.</summary>
        InProgress
    }

    #endregion

    #region Structures

    /// <summary>
    /// Represents a specific finger error.
    /// </summary>
    [Serializable]
    public struct FingerError
    {
        /// <summary>Affected finger.</summary>
        public Finger finger;

        /// <summary>Detected error type.</summary>
        public FingerErrorType errorType;

        /// <summary>Error severity.</summary>
        public Severity severity;

        /// <summary>Measured value (e.g., curl 0-1).</summary>
        public float currentValue;

        /// <summary>Expected value based on the constraint.</summary>
        public float expectedValue;

        /// <summary>User-facing correction message.</summary>
        public string correctionMessage;

        public FingerError(Finger finger, FingerErrorType errorType, Severity severity,
                          float currentValue, float expectedValue, string correctionMessage)
        {
            this.finger = finger;
            this.errorType = errorType;
            this.severity = severity;
            this.currentValue = currentValue;
            this.expectedValue = expectedValue;
            this.correctionMessage = correctionMessage;
        }

        /// <summary>
        /// Creates an error without numeric values.
        /// </summary>
        public static FingerError Create(Finger finger, FingerErrorType errorType,
                                         Severity severity, string message)
        {
            return new FingerError(finger, errorType, severity, 0f, 0f, message);
        }
    }

    /// <summary>
    /// Result of a static gesture analysis.
    /// </summary>
    [Serializable]
    public class StaticGestureResult
    {
        /// <summary>
        /// True if the gesture meets global conditions (XRHandShape.CheckConditions).
        /// </summary>
        public bool isMatchGlobal;

        /// <summary>
        /// Per-finger errors.
        /// List avoids default empty entries.
        /// </summary>
        public System.Collections.Generic.List<FingerError> perFingerErrors;

        /// <summary>
        /// Prioritized summary message to show the user.
        /// </summary>
        public string summaryMessage;

        /// <summary>Total major errors.</summary>
        public int majorErrorCount;

        /// <summary>Total minor errors.</summary>
        public int minorErrorCount;

        /// <summary>Timestamp when this result was generated.</summary>
        public float timestamp;

        // === "Near match" fields ===

        /// <summary>
        /// Match score (0-1). 1 = perfect, 0 = far.
        /// Computed as: 1 - (majorErrors * 0.3 + minorErrors * 0.1)
        /// </summary>
        public float matchScore;

        /// <summary>
        /// True if the gesture is "almost correct" (only Minor, no Major).
        /// </summary>
        public bool isNearMatch;

        /// <summary>
        /// True if the result is stable (not fluctuating).
        /// </summary>
        public bool isStable;

        /// <summary>
        /// Analysis confidence (0-1).
        /// </summary>
        public float confidence;

        /// <summary>True if there are no errors.</summary>
        public bool IsFullyCorrect => majorErrorCount == 0 && minorErrorCount == 0;

        /// <summary>
        /// Gets the MOST SEVERE error for a specific finger.
        /// If both Major and Minor exist, returns Major.
        /// </summary>
        public FingerError? GetErrorForFinger(Finger finger)
        {
            if (perFingerErrors == null || perFingerErrors.Count == 0) return null;

            FingerError? bestMatch = null;
            Severity highestSeverity = Severity.None;

            foreach (var error in perFingerErrors)
            {
                if (error.finger == finger && error.severity != Severity.None)
                {
                    if (error.severity > highestSeverity)
                    {
                        highestSeverity = error.severity;
                        bestMatch = error;
                    }
                }
            }
            return bestMatch;
        }

        /// <summary>
        /// Gets severity for a specific finger (highest if multiple).
        /// </summary>
        public Severity GetSeverityForFinger(Finger finger)
        {
            var error = GetErrorForFinger(finger);
            return error?.severity ?? Severity.None;
        }

        /// <summary>
        /// Adds an error to the list and updates counters.
        /// </summary>
        public void AddError(FingerError error)
        {
            perFingerErrors ??= new System.Collections.Generic.List<FingerError>();
            perFingerErrors.Add(error);

            if (error.severity == Severity.Major)
                majorErrorCount++;
            else if (error.severity == Severity.Minor)
                minorErrorCount++;

            UpdateMatchScore();
        }

        /// <summary>
        /// Recomputes matchScore and isNearMatch based on current errors.
        /// </summary>
        public void UpdateMatchScore()
        {
            float penalty = (majorErrorCount * 0.3f) + (minorErrorCount * 0.1f);
            matchScore = Mathf.Clamp01(1f - penalty);

            isNearMatch = majorErrorCount == 0 && minorErrorCount > 0;
        }

        public StaticGestureResult()
        {
            perFingerErrors = new System.Collections.Generic.List<FingerError>();
            timestamp = 0f;
            matchScore = 1f;
            isNearMatch = false;
            isStable = true;
            confidence = 1f;
        }

        /// <summary>
        /// Creates a full success result.
        /// </summary>
        public static StaticGestureResult CreateSuccess()
        {
            return new StaticGestureResult
            {
                isMatchGlobal = true,
                matchScore = 1f,
                isNearMatch = false,
                isStable = true,
                confidence = 1f,
                summaryMessage = "Perfect!"
            };
        }
    }

    /// <summary>
    /// Movement metrics for dynamic gestures.
    /// </summary>
    [Serializable]
    public struct DynamicMetrics
    {
        /// <summary>Average speed in m/s.</summary>
        public float averageSpeed;

        /// <summary>Max speed in m/s.</summary>
        public float maxSpeed;

        /// <summary>Total distance traveled in meters.</summary>
        public float totalDistance;

        /// <summary>Gesture duration in seconds.</summary>
        public float duration;

        /// <summary>Number of direction changes detected.</summary>
        public int directionChanges;

        /// <summary>Total rotation in degrees.</summary>
        public float totalRotation;

        /// <summary>Circularity score (0-1).</summary>
        public float circularityScore;

        // === Direction fields ===

        /// <summary>Net displacement from start to current (start→current).</summary>
        public Vector3 netDisplacement;

        /// <summary>Average movement direction (normalized).</summary>
        public Vector3 averageVelocityDirection;

        /// <summary>
        /// Alignment with expected direction (dot product).
        /// 1 = aligned, 0 = perpendicular, -1 = opposite.
        /// </summary>
        public float directionAlignment;

        /// <summary>
        /// Path straightness (0-1).
        /// 1 = straight line, low values = zigzag/curves.
        /// </summary>
        public float pathStraightness;

        /// <summary>True if hand shape remains valid during movement.</summary>
        public bool handShapeStable;
    }

    /// <summary>
    /// Result of a dynamic gesture analysis.
    /// </summary>
    [Serializable]
    public class DynamicGestureResult
    {
        /// <summary>Attempted gesture name.</summary>
        public string gestureName;

        /// <summary>True if successfully completed.</summary>
        public bool isSuccess;

        /// <summary>Failure reason (if any).</summary>
        public FailureReason failureReason;

        /// <summary>Phase where failure occurred.</summary>
        public GesturePhase failedPhase;

        /// <summary>Movement metrics.</summary>
        public DynamicMetrics metrics;

        /// <summary>User-facing troubleshooting message.</summary>
        public string troubleshootingMessage;

        /// <summary>Timestamp when this result was generated.</summary>
        public float timestamp;

        public DynamicGestureResult()
        {
            timestamp = 0f; // set at runtime; avoid Unity API in constructor
        }

        /// <summary>
        /// Creates a success result.
        /// </summary>
        public static DynamicGestureResult Success(string gestureName, DynamicMetrics metrics)
        {
            return new DynamicGestureResult
            {
                gestureName = gestureName,
                isSuccess = true,
                failureReason = FailureReason.None,
                failedPhase = GesturePhase.None,
                metrics = metrics,
                troubleshootingMessage = "Gesture completed successfully!",
                timestamp = Time.time
            };
        }

        /// <summary>
        /// Creates a failure result.
        /// </summary>
        public static DynamicGestureResult Failure(string gestureName, FailureReason reason,
                                                   GesturePhase phase, DynamicMetrics metrics,
                                                   string message)
        {
            return new DynamicGestureResult
            {
                gestureName = gestureName,
                isSuccess = false,
                failureReason = reason,
                failedPhase = phase,
                metrics = metrics,
                troubleshootingMessage = message,
                timestamp = Time.time
            };
        }
    }

    #endregion

    #region Message Dictionary

    /// <summary>
    /// Dictionary of user-facing feedback messages (English).
    /// Semantic distinction:
    /// - Curve = controlled bend without palm contact
    /// - Close = full fist / palm contact
    /// </summary>
    public static class FeedbackMessages
    {
        /// <summary>
        /// Gets a correction message for a finger error.
        /// Softer tone for Minor, more direct for Major.
        /// </summary>
        public static string GetCorrectionMessage(Finger finger, FingerErrorType errorType, Severity severity = Severity.Major)
        {
            string fingerName = GetFingerName(finger);
            bool isMinor = severity == Severity.Minor;

            return errorType switch
            {
                // === Semantic 3-state errors ===

                // CURVE: extended -> curved (no fist)
                FingerErrorType.NeedsCurve => isMinor
                    ? $"Curve your {fingerName} a bit more"
                    : $"Curve your {fingerName} (don’t make a fist)",

                // CLOSE: curved -> closed (full fist)
                FingerErrorType.NeedsFist => isMinor
                    ? $"Close your {fingerName} a bit more"
                    : $"Close your {fingerName} into a fist",

                // TOO MUCH: made a fist when only curving was needed
                FingerErrorType.TooMuchCurl => isMinor
                    ? $"Relax your {fingerName} a bit"
                    : $"Relax your {fingerName}—don’t make a fist",

                // EXTEND: curved/closed -> extended
                FingerErrorType.NeedsExtend => isMinor
                    ? $"Straighten your {fingerName} a bit"
                    : $"Straighten your {fingerName} fully",

                // === Classic errors (compatibility) ===
                FingerErrorType.TooExtended => isMinor
                    ? $"Bend your {fingerName} a bit more"
                    : $"Bend your {fingerName}",
                FingerErrorType.TooCurled => isMinor
                    ? $"Straighten your {fingerName} a bit"
                    : $"Straighten your {fingerName}",

                // === Spread/position errors ===
                FingerErrorType.SpreadTooNarrow => isMinor
                    ? "Spread your fingers a bit more"
                    : "Spread your fingers",
                FingerErrorType.SpreadTooWide => isMinor
                    ? "Bring your fingers a bit closer"
                    : "Bring your fingers together",
                FingerErrorType.ThumbPositionWrong => isMinor
                    ? "Adjust your thumb slightly"
                    : "Adjust your thumb position",
                FingerErrorType.ShouldTouch => isMinor
                    ? $"Bring your {fingerName} closer"
                    : $"Touch with your {fingerName}",
                FingerErrorType.ShouldNotTouch => isMinor
                    ? $"Separate your {fingerName} slightly"
                    : $"Separate your {fingerName}",
                FingerErrorType.RotationWrong => isMinor
                    ? "Rotate your hand slightly"
                    : "Rotate your hand",
                _ => isMinor
                    ? $"Adjust your {fingerName} slightly"
                    : $"Adjust your {fingerName}"
            };
        }

        /// <summary>
        /// Determines current semantic state from curl value.
        /// </summary>
        public static FingerShapeState GetFingerState(float curlValue)
        {
            if (curlValue < 0.25f)
                return FingerShapeState.Extended;
            if (curlValue < 0.75f)
                return FingerShapeState.Curved;
            return FingerShapeState.Closed;
        }

        /// <summary>
        /// Determines semantic error type based on current vs expected state.
        /// </summary>
        public static FingerErrorType GetSemanticErrorType(FingerShapeState currentState, FingerShapeState expectedState)
        {
            if (currentState == expectedState)
                return FingerErrorType.None;

            return (currentState, expectedState) switch
            {
                (FingerShapeState.Extended, FingerShapeState.Curved) => FingerErrorType.NeedsCurve,
                (FingerShapeState.Extended, FingerShapeState.Closed) => FingerErrorType.NeedsFist,
                (FingerShapeState.Curved, FingerShapeState.Closed) => FingerErrorType.NeedsFist,
                (FingerShapeState.Curved, FingerShapeState.Extended) => FingerErrorType.NeedsExtend,
                (FingerShapeState.Closed, FingerShapeState.Extended) => FingerErrorType.NeedsExtend,
                (FingerShapeState.Closed, FingerShapeState.Curved) => FingerErrorType.TooMuchCurl,
                _ => FingerErrorType.None
            };
        }

        /// <summary>
        /// Friendly description of the expected finger state.
        /// </summary>
        public static string GetStateDescription(FingerShapeState state, Finger finger)
        {
            string fingerName = GetFingerName(finger);
            return state switch
            {
                FingerShapeState.Extended => $"{fingerName} straight, like pointing",
                FingerShapeState.Curved => $"{fingerName} curved, without touching the palm",
                FingerShapeState.Closed => $"{fingerName} closed into a fist",
                _ => $"{fingerName} in position"
            };
        }

        /// <summary>
        /// Troubleshooting message for a dynamic gesture failure (English).
        /// </summary>
        public static string GetTroubleshootingMessage(FailureReason reason, GesturePhase phase,
                                                       DynamicMetrics metrics, string gestureName)
        {
            string phaseStr = phase switch
            {
                GesturePhase.Start => "at the start",
                GesturePhase.Move => "during the movement",
                GesturePhase.End => "at the end",
                _ => ""
            };

            return reason switch
            {
                FailureReason.PoseLost => $"Keep the correct hand shape {phaseStr}.",
                FailureReason.SpeedTooLow => $"Move faster (avg speed: {metrics.averageSpeed:F2} m/s).",
                FailureReason.SpeedTooHigh => "Move slower and with control.",
                FailureReason.DistanceTooShort => $"Make a bigger movement (distance: {metrics.totalDistance:F2} m).",
                FailureReason.DirectionWrong => $"Move in the correct direction for '{gestureName}'.",
                FailureReason.DirectionChangesInsufficient => $"Add more direction changes ({metrics.directionChanges} detected).",
                FailureReason.RotationInsufficient => $"Rotate your wrist more ({metrics.totalRotation:F0}° detected).",
                FailureReason.NotCircular => "Make the movement more circular.",
                FailureReason.Timeout => "Complete the gesture faster.",
                FailureReason.EndPoseMismatch => "Finish with the correct end hand shape.",
                FailureReason.TrackingLost => "Keep your hand visible to the sensors.",
                FailureReason.OutOfZone => "Keep your hand in the correct area in front of you.",
                FailureReason.Unknown => $"Adjust the movement for '{gestureName}'.",
                _ => $"Try the '{gestureName}' gesture again."
            };
        }

        /// <summary>
        /// Human-readable finger names (English).
        /// </summary>
        public static string GetFingerName(Finger finger)
        {
            return finger switch
            {
                Finger.Thumb => "thumb",
                Finger.Index => "index",
                Finger.Middle => "middle",
                Finger.Ring => "ring",
                Finger.Pinky => "pinky",
                _ => "finger"
            };
        }

        /// <summary>
        /// Generates a prioritized summary, adapting tone based on error count/types.
        /// </summary>
        public static string GenerateSummary(System.Collections.Generic.List<FingerError> errors, int maxMessages = 2)
        {
            if (errors == null || errors.Count == 0)
                return "Perfect! Your hand is in the correct position.";

            var sortedErrors = new System.Collections.Generic.List<FingerError>();
            int majorCount = 0;
            int minorCount = 0;

            foreach (var e in errors)
            {
                if (e.severity == Severity.Major)
                {
                    sortedErrors.Add(e);
                    majorCount++;
                }
            }

            foreach (var e in errors)
            {
                if (e.severity == Severity.Minor)
                {
                    sortedErrors.Add(e);
                    minorCount++;
                }
            }

            if (sortedErrors.Count == 0)
                return "Perfect! Your hand is in the correct position.";

            string prefix = "";
            if (majorCount == 0 && minorCount > 0)
            {
                prefix = "Almost there: ";
            }
            else if (majorCount > 2)
            {
                prefix = "Start with: ";
            }

            var messages = new System.Collections.Generic.List<string>();
            int count = Mathf.Min(maxMessages, sortedErrors.Count);

            for (int i = 0; i < count; i++)
            {
                if (!string.IsNullOrEmpty(sortedErrors[i].correctionMessage))
                    messages.Add(sortedErrors[i].correctionMessage);
            }

            return prefix + string.Join("\n", messages);
        }

        /// <summary>
        /// Summary overload for arrays (compatibility).
        /// </summary>
        public static string GenerateSummary(FingerError[] errors, int maxMessages = 2)
        {
            if (errors == null) return "Perfect! Your hand is in the correct position.";
            var list = new System.Collections.Generic.List<FingerError>(errors);
            return GenerateSummary(list, maxMessages);
        }

        #region Dynamic Gesture Phase Messages

        public static string GetIdlePhaseMessage(string gestureName)
        {
            return $"Place your hand for '{gestureName}'.";
        }

        public static string GetStartDetectedMessage(string gestureName)
        {
            return "Good! Now start the movement.";
        }

        public static string GetInProgressMessage(DynamicMovementIssue issue, DynamicMetrics metrics, string gestureName)
        {
            return issue switch
            {
                DynamicMovementIssue.None => "Keep going.",
                DynamicMovementIssue.DirectionWrong => GetDirectionHint(gestureName),
                DynamicMovementIssue.TooFast => "Slower.",
                DynamicMovementIssue.TooSlow => "Faster.",
                DynamicMovementIssue.TooShort => "Make the movement bigger.",
                DynamicMovementIssue.NotContinuous => "Keep moving—don’t pause.",
                DynamicMovementIssue.NotCircular => "Make the movement more circular.",
                DynamicMovementIssue.NeedMoreDirectionChanges => "Move side to side more.",
                DynamicMovementIssue.RotationInsufficient => "Rotate your wrist more.",
                DynamicMovementIssue.StartPoseDegrading => "Keep the hand shape.",
                _ => "Keep going."
            };
        }

        private static string GetDirectionHint(string gestureName)
        {
            return "Adjust the movement direction.";
        }

        private static string _cachedNearCompletionMessage = null;
        private static float _nearCompletionMessageTime = 0f;
        private const float NEAR_COMPLETION_MESSAGE_DURATION = 2f;

        public static string GetNearCompletionMessage()
        {
            if (_cachedNearCompletionMessage != null && (Time.time - _nearCompletionMessageTime) < NEAR_COMPLETION_MESSAGE_DURATION)
                return _cachedNearCompletionMessage;

            string[] messages = new string[]
            {
                "Almost!",
                "Finish the movement.",
                "Just a bit more.",
                "Nearly there."
            };

            _cachedNearCompletionMessage = messages[UnityEngine.Random.Range(0, messages.Length)];
            _nearCompletionMessageTime = Time.time;

            return _cachedNearCompletionMessage;
        }

        public static void ResetNearCompletionCache()
        {
            _cachedNearCompletionMessage = null;
            _nearCompletionMessageTime = 0f;
        }

        public static string GetCompletedMessage(string gestureName)
        {
            return "Movement recognized!";
        }

        public static string GetFailedMessage(FailureReason reason, GesturePhase phase, DynamicMetrics metrics, string gestureName)
        {
            string explanation = reason switch
            {
                FailureReason.SpeedTooLow => "The gesture was too slow",
                FailureReason.SpeedTooHigh => "The gesture was too fast",
                FailureReason.DistanceTooShort => "The movement was too short",
                FailureReason.DirectionWrong => "The direction was not correct",
                FailureReason.DirectionChangesInsufficient => "There were not enough direction changes",
                FailureReason.RotationInsufficient => "Not enough wrist rotation",
                FailureReason.NotCircular => "The movement was not circular",
                FailureReason.Timeout => "The gesture took too long",
                FailureReason.PoseLost => phase == GesturePhase.Start
                    ? "You started moving too early"
                    : "You lost the hand shape during the movement",
                FailureReason.EndPoseMismatch => "The final pose was not correct",
                FailureReason.TrackingLost => "Keep your hand visible to the sensors",
                FailureReason.OutOfZone => "Your hand moved out of the tracking area",
                FailureReason.Unknown => $"The '{gestureName}' gesture was not completed",
                _ => $"Try the '{gestureName}' gesture again"
            };

            return $"{explanation}. Try again while keeping the correct hand shape.";
        }

        public static DynamicMovementIssue DetectMovementIssue(
            DynamicMetrics metrics,
            float expectedMinSpeed,
            float expectedMaxSpeed,
            float expectedMinDistance,
            bool requiresCircular,
            float minCircularityScore,
            bool requiresDirectionChanges,
            int requiredChanges,
            bool requiresSpecificDirection = false,
            float minDirectionAlignment = 0.5f,
            bool requiresRotation = false,
            float minRotationAngle = 0f)
        {
            if (!metrics.handShapeStable)
                return DynamicMovementIssue.StartPoseDegrading;

            if (metrics.averageSpeed < expectedMinSpeed * 0.5f)
                return DynamicMovementIssue.TooSlow;

            if (expectedMaxSpeed > 0 && metrics.maxSpeed > expectedMaxSpeed * 1.5f)
                return DynamicMovementIssue.TooFast;

            if (requiresSpecificDirection && metrics.directionAlignment < minDirectionAlignment)
                return DynamicMovementIssue.DirectionWrong;

            if (metrics.duration > 0.3f && metrics.totalDistance < expectedMinDistance * 0.5f)
                return DynamicMovementIssue.TooShort;

            if (requiresRotation && metrics.totalRotation < minRotationAngle * 0.7f)
                return DynamicMovementIssue.RotationInsufficient;

            if (requiresCircular && metrics.circularityScore < minCircularityScore * 0.7f)
                return DynamicMovementIssue.NotCircular;

            if (requiresDirectionChanges && metrics.directionChanges < requiredChanges)
                return DynamicMovementIssue.NeedMoreDirectionChanges;

            return DynamicMovementIssue.None;
        }

        public static DynamicMovementIssue DetectMovementIssue(
            DynamicMetrics metrics,
            float expectedMinSpeed,
            float expectedMaxSpeed,
            float expectedMinDistance,
            bool requiresCircular,
            float minCircularityScore,
            bool requiresDirectionChanges,
            int requiredChanges)
        {
            return DetectMovementIssue(
                metrics,
                expectedMinSpeed,
                expectedMaxSpeed,
                expectedMinDistance,
                requiresCircular,
                minCircularityScore,
                requiresDirectionChanges,
                requiredChanges,
                requiresSpecificDirection: false,
                minDirectionAlignment: 0.5f,
                requiresRotation: false,
                minRotationAngle: 0f
            );
        }

        #endregion
    }

    #endregion
}
