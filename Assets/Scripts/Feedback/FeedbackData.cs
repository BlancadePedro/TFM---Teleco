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
            return GetTroubleshootingMessage(reason, phase, metrics, gestureName, Vector3.zero);
        }

        public static string GetTroubleshootingMessage(FailureReason reason, GesturePhase phase,
                                                       DynamicMetrics metrics, string gestureName, Vector3 expectedDirection)
        {
            string phaseStr = phase switch
            {
                GesturePhase.Start => "al inicio",
                GesturePhase.Move => "durante el movimiento",
                GesturePhase.End => "al final",
                _ => ""
            };

            if (reason == FailureReason.DirectionWrong)
            {
                string specific = GetGestureSpecificDirection(gestureName);
                if (!string.IsNullOrEmpty(specific))
                    return specific;
                if (expectedDirection.sqrMagnitude > 0.01f)
                    return $"Mueve la mano {GetDirectionDescription(expectedDirection)}.";
                return $"Ajusta la direccion del movimiento para '{gestureName}'.";
            }

            return reason switch
            {
                FailureReason.PoseLost => $"Manten la forma correcta de la mano {phaseStr}.",
                FailureReason.SpeedTooLow => $"Mueve mas rapido (velocidad: {metrics.averageSpeed:F2} m/s).",
                FailureReason.SpeedTooHigh => "Mueve mas lento y con control.",
                FailureReason.DistanceTooShort => $"Haz un movimiento mas amplio (distancia: {metrics.totalDistance:F2} m).",
                FailureReason.DirectionChangesInsufficient => $"Anade mas cambios de direccion ({metrics.directionChanges} detectados).",
                FailureReason.RotationInsufficient => $"Rota la muneca mas ({metrics.totalRotation:F0}° detectados).",
                FailureReason.NotCircular => "Haz el movimiento mas circular.",
                FailureReason.Timeout => "Completa el gesto mas rapido.",
                FailureReason.EndPoseMismatch => "Termina con la forma de mano correcta.",
                FailureReason.TrackingLost => "Manten la mano visible para los sensores.",
                FailureReason.OutOfZone => "Manten la mano en la zona correcta frente a ti.",
                FailureReason.Unknown => $"Ajusta el movimiento para '{gestureName}'.",
                _ => $"Intentalo de nuevo con '{gestureName}'."
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
            return $"Coloca la mano para '{gestureName}'.";
        }

        public static string GetStartDetectedMessage(string gestureName)
        {
            return "Bien! Ahora empieza el movimiento.";
        }

        public static string GetInProgressMessage(DynamicMovementIssue issue, DynamicMetrics metrics, string gestureName)
        {
            return GetInProgressMessage(issue, metrics, gestureName, Vector3.zero);
        }

        public static string GetInProgressMessage(DynamicMovementIssue issue, DynamicMetrics metrics, string gestureName, Vector3 expectedDirection)
        {
            return issue switch
            {
                DynamicMovementIssue.None => "Sigue asi.",
                DynamicMovementIssue.DirectionWrong => GetDirectionHint(gestureName, expectedDirection),
                DynamicMovementIssue.TooFast => "Mas despacio.",
                DynamicMovementIssue.TooSlow => "Mas rapido.",
                DynamicMovementIssue.TooShort => "Haz el movimiento mas grande.",
                DynamicMovementIssue.NotContinuous => "No pares, sigue moviendo.",
                DynamicMovementIssue.NotCircular => "Haz el movimiento mas circular.",
                DynamicMovementIssue.NeedMoreDirectionChanges => "Mueve de lado a lado mas veces.",
                DynamicMovementIssue.RotationInsufficient => "Rota la muneca mas.",
                DynamicMovementIssue.StartPoseDegrading => "Mantén la forma de la mano.",
                _ => "Sigue asi."
            };
        }

        private static string GetDirectionHint(string gestureName, Vector3 expectedDirection)
        {
            // First check gesture-specific descriptions
            string specific = GetGestureSpecificDirection(gestureName);
            if (!string.IsNullOrEmpty(specific))
                return specific;

            // Fall back to vector-based description
            if (expectedDirection.sqrMagnitude > 0.01f)
            {
                string dirDesc = GetDirectionDescription(expectedDirection);
                return $"Mueve la mano {dirDesc}.";
            }

            return "Ajusta la direccion del movimiento.";
        }

        /// <summary>
        /// Converts a direction vector to a human-readable Spanish description.
        /// </summary>
        public static string GetDirectionDescription(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.01f)
                return "en la direccion correcta";

            Vector3 dir = direction.normalized;

            // Determine primary axis components
            string vertical = "";
            string horizontal = "";
            string depth = "";

            if (dir.y > 0.4f) vertical = "hacia arriba";
            else if (dir.y < -0.4f) vertical = "hacia abajo";

            if (dir.x > 0.4f) horizontal = "hacia la derecha";
            else if (dir.x < -0.4f) horizontal = "hacia la izquierda";

            if (dir.z > 0.4f) depth = "hacia adelante";
            else if (dir.z < -0.4f) depth = "hacia ti";

            // Combine components
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(vertical)) parts.Add(vertical);
            if (!string.IsNullOrEmpty(horizontal)) parts.Add(horizontal);
            if (!string.IsNullOrEmpty(depth)) parts.Add(depth);

            if (parts.Count == 0)
                return "en la direccion correcta";

            return string.Join(" y ", parts);
        }

        /// <summary>
        /// Returns gesture-specific direction descriptions in Spanish.
        /// </summary>
        private static string GetGestureSpecificDirection(string gestureName)
        {
            if (string.IsNullOrEmpty(gestureName))
                return null;

            string upper = gestureName.ToUpper();

            // Check by prefix for names like "J_Right", "Z_Move", etc.
            if (upper.StartsWith("J"))
                return "Dibuja una J: mueve el menique hacia abajo y curva hacia la izquierda.";
            if (upper.StartsWith("Z"))
                return "Dibuja una Z: derecha, diagonal abajo-izquierda, y derecha.";

            // Basic communication
            if (upper.Contains("HELLO") || upper.Contains("HOLA"))
                return "Agita la mano de lado a lado (como saludando).";
            if (upper.Contains("BYE") || upper.Contains("ADIOS"))
                return "Agita la mano de lado a lado (despidiendote).";
            if (upper.Contains("YES") || upper.Contains("SI"))
                return "Mueve el puno hacia abajo y arriba (como asintiendo).";
            if (upper.Contains("NO"))
                return "Mueve los dedos de lado a lado (como negando).";
            if (upper.Contains("THANK") || upper.Contains("GRACIA"))
                return "Mueve la mano hacia adelante desde la barbilla.";
            if (upper.Contains("PLEASE") || upper.Contains("POR FAVOR"))
                return "Haz un movimiento circular sobre el pecho.";
            if (upper.Contains("GOOD") || upper.Contains("BIEN") || upper.Contains("BUENO"))
                return "Mueve la mano hacia adelante desde la barbilla.";
            if (upper.Contains("BAD") || upper.Contains("MAL"))
                return "Mueve la mano hacia abajo desde la barbilla.";

            // Colors with movement
            if (upper.Contains("BLUE") || upper.Contains("AZUL"))
                return "Gira la mano con la B hacia la derecha.";
            if (upper.Contains("GREEN") || upper.Contains("VERDE"))
                return "Mueve la G hacia adelante y atras.";
            if (upper.Contains("YELLOW") || upper.Contains("AMARILLO"))
                return "Gira la mano con la Y hacia fuera.";
            if (upper.Contains("PURPLE") || upper.Contains("MORADO"))
                return "Agita la P de lado a lado.";
            if (upper.Contains("ORANGE") || upper.Contains("NARANJA"))
                return "Aprieta la mano frente a la barbilla (como exprimiendo).";
            if (upper.Contains("BROWN") || upper.Contains("MARRON"))
                return "Desliza la B hacia abajo por la mejilla.";
            if (upper.Contains("PINK") || upper.Contains("ROSA"))
                return "Desliza la P hacia abajo por los labios.";
            if (upper.Contains("WHITE") || upper.Contains("BLANCO"))
                return "Mueve la mano desde el pecho hacia fuera cerrando los dedos.";
            if (upper.Contains("BLACK") || upper.Contains("NEGRO"))
                return "Desliza el indice de lado a lado por la frente.";
            if (upper.Contains("RED") || upper.Contains("ROJO"))
                return "Desliza el indice hacia abajo desde los labios.";
            if (upper.Contains("GRAY") || upper.Contains("GRIS"))
                return "Mueve ambas manos adelante y atras cruzandose.";

            // Days of the week
            if (upper.Contains("MONDAY") || upper.Contains("LUNES"))
                return "Haz un pequeno circulo con la M.";
            if (upper.Contains("TUESDAY") || upper.Contains("MARTES"))
                return "Haz un pequeno circulo con la T.";
            if (upper.Contains("WEDNESDAY") || upper.Contains("MIERCOLES"))
                return "Haz un pequeno circulo con la W.";
            if (upper.Contains("THURSDAY") || upper.Contains("JUEVES"))
                return "Haz un pequeno circulo con la H (o T+H).";
            if (upper.Contains("FRIDAY") || upper.Contains("VIERNES"))
                return "Haz un pequeno circulo con la F.";
            if (upper.Contains("SATURDAY") || upper.Contains("SABADO"))
                return "Haz un pequeno circulo con la S.";
            if (upper.Contains("SUNDAY") || upper.Contains("DOMINGO"))
                return "Mueve ambas manos hacia abajo y hacia fuera (abriendo).";

            // Verbs
            if (upper.Contains("EAT") || upper.Contains("COMER"))
                return "Lleva la mano cerrada hacia la boca repetidamente.";
            if (upper.Contains("DRINK") || upper.Contains("BEBER"))
                return "Lleva la mano como un vaso hacia la boca.";
            if (upper.Contains("SLEEP") || upper.Contains("DORMIR"))
                return "Lleva la mano abierta hacia la cara cerrando los dedos.";
            if (upper.Contains("READ") || upper.Contains("LEER"))
                return "Mueve los dedos en V sobre la palma de lado a lado.";
            if (upper.Contains("WRITE") || upper.Contains("ESCRIBIR"))
                return "Haz como si escribieras sobre la palma abierta.";
            if (upper.Contains("DRAW") || upper.Contains("DIBUJAR"))
                return "Mueve el menique en zigzag sobre la palma.";
            if (upper.Contains("PLAY") || upper.Contains("JUGAR"))
                return "Agita ambas manos con los pulgares y meniques extendidos.";
            if (upper.Contains("HURT") || upper.Contains("DOLOR"))
                return "Gira ambos indices uno hacia el otro repetidamente.";
            if (upper.Contains("GET") || upper.Contains("OBTENER"))
                return "Mueve ambas manos hacia ti cerrando los dedos.";
            if (upper.Contains("TAP") || upper.Contains("TOCAR"))
                return "Toca con el indice hacia abajo repetidamente.";

            return null;
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
                "Casi!",
                "Termina el movimiento.",
                "Solo un poco mas.",
                "Ya casi esta."
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
            return "Movimiento reconocido!";
        }

        public static string GetFailedMessage(FailureReason reason, GesturePhase phase, DynamicMetrics metrics, string gestureName)
        {
            return GetFailedMessage(reason, phase, metrics, gestureName, Vector3.zero);
        }

        public static string GetFailedMessage(FailureReason reason, GesturePhase phase, DynamicMetrics metrics, string gestureName, Vector3 expectedDirection)
        {
            string explanation;
            if (reason == FailureReason.DirectionWrong)
            {
                // Try gesture-specific direction first
                string specific = GetGestureSpecificDirection(gestureName);
                if (!string.IsNullOrEmpty(specific))
                {
                    explanation = $"La direccion no era correcta. {specific}";
                }
                else if (expectedDirection.sqrMagnitude > 0.01f)
                {
                    string dirDesc = GetDirectionDescription(expectedDirection);
                    explanation = $"La direccion no era correcta. Debes mover la mano {dirDesc}";
                }
                else
                {
                    explanation = "La direccion del movimiento no era correcta";
                }
            }
            else
            {
                explanation = reason switch
                {
                    FailureReason.SpeedTooLow => "El gesto fue demasiado lento",
                    FailureReason.SpeedTooHigh => "El gesto fue demasiado rapido",
                    FailureReason.DistanceTooShort => "El movimiento fue demasiado corto",
                    FailureReason.DirectionChangesInsufficient => "No hubo suficientes cambios de direccion",
                    FailureReason.RotationInsufficient => "No rotaste la muneca lo suficiente",
                    FailureReason.NotCircular => "El movimiento no fue suficientemente circular",
                    FailureReason.Timeout => "El gesto tardo demasiado",
                    FailureReason.PoseLost => phase == GesturePhase.Start
                        ? "Empezaste a mover la mano demasiado pronto"
                        : "Perdiste la forma de la mano durante el movimiento",
                    FailureReason.EndPoseMismatch => "La pose final no era correcta",
                    FailureReason.TrackingLost => "Manten la mano visible para los sensores",
                    FailureReason.OutOfZone => "Tu mano salio de la zona de tracking",
                    FailureReason.Unknown => $"El gesto '{gestureName}' no se completo",
                    _ => $"Vuelve a intentar el gesto '{gestureName}'"
                };
            }

            return $"{explanation}. Intentalo de nuevo manteniendo la forma de la mano.";
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
