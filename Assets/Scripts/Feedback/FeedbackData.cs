using System;
using UnityEngine;

namespace ASL_LearnVR.Feedback
{
    #region Enums

    /// <summary>
    /// Identificador de cada dedo de la mano.
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
    /// Severidad del error detectado.
    /// </summary>
    public enum Severity
    {
        /// <summary>Sin error, el dedo está correcto.</summary>
        None = 0,
        /// <summary>Ajuste menor requerido (se muestra en rojo).</summary>
        Minor = 1,
        /// <summary>Error mayor que impide el reconocimiento (rojo).</summary>
        Major = 2
    }

    /// <summary>
    /// Estado semántico de la forma del dedo.
    /// Distingue entre tres estados funcionales para ASL:
    /// - Extendido: dedo recto, sin flexión
    /// - Curvado: dedo flexionado con control, sin tocar la palma (para C, D, O, X, etc.)
    /// - Cerrado: dedo completamente plegado formando puño (para A, S, T, etc.)
    /// </summary>
    public enum FingerShapeState
    {
        /// <summary>
        /// Dedo extendido/recto. Sin intención de formar figura.
        /// Ejemplo: "Deja el dedo recto, como señalando."
        /// Rango típico de curl: 0.0 - 0.25
        /// </summary>
        Extended = 0,

        /// <summary>
        /// Dedo curvado con control. Flexionado pero sin cerrar contra la palma.
        /// Hay espacio entre los dedos y la palma. Forma funcional controlada.
        /// Ejemplo: "Dobla el dedo suavemente, como si rodearas una pelota pequeña."
        /// Usado en signos como C, D, O, X (gancho), E, M, N.
        /// Rango típico de curl: 0.25 - 0.75
        /// </summary>
        Curved,

        /// <summary>
        /// Dedo completamente cerrado/puño. Toca o presiona la palma.
        /// Ejemplo: "Cierra el dedo del todo, formando un puño."
        /// Usado en signos como A, S, T (con matices de posición del pulgar).
        /// Rango típico de curl: 0.75 - 1.0
        /// </summary>
        Closed
    }

    /// <summary>
    /// Tipo de error específico por dedo.
    /// Incluye errores semánticos que distinguen entre curvar y cerrar.
    /// </summary>
    public enum FingerErrorType
    {
        None = 0,

        // === Errores clásicos (compatibilidad) ===
        /// <summary>El dedo está demasiado extendido (genérico).</summary>
        TooExtended,
        /// <summary>El dedo está demasiado cerrado/curvado (genérico).</summary>
        TooCurled,

        // === Errores semánticos de tres estados ===
        /// <summary>
        /// El dedo debe CURVAR (pasar de extendido a curvado).
        /// "Curva el dedo suavemente, sin cerrar el puño."
        /// </summary>
        NeedsCurve,

        /// <summary>
        /// El dedo debe CERRAR completamente (pasar de curvado a puño).
        /// "Cierra el dedo formando un puño."
        /// </summary>
        NeedsFist,

        /// <summary>
        /// El dedo cerró DEMASIADO (hizo puño cuando solo debía curvar).
        /// "Suelta un poco el dedo, no formes puño."
        /// </summary>
        TooMuchCurl,

        /// <summary>
        /// El dedo debe EXTENDER (pasar de curvado/cerrado a recto).
        /// "Estira el dedo completamente."
        /// </summary>
        NeedsExtend,

        // === Errores de spread y posición ===
        /// <summary>Separación insuficiente entre dedos.</summary>
        SpreadTooNarrow,
        /// <summary>Demasiada separación entre dedos.</summary>
        SpreadTooWide,
        /// <summary>El pulgar está en posición incorrecta.</summary>
        ThumbPositionWrong,
        /// <summary>El dedo debería estar tocando otro.</summary>
        ShouldTouch,
        /// <summary>El dedo no debería estar tocando otro.</summary>
        ShouldNotTouch,
        /// <summary>Rotación incorrecta del dedo/mano.</summary>
        RotationWrong
    }

    /// <summary>
    /// Razón de fallo para gestos dinámicos.
    /// </summary>
    public enum FailureReason
    {
        None = 0,
        /// <summary>La pose de la mano se perdió durante el gesto.</summary>
        PoseLost,
        /// <summary>El movimiento fue demasiado lento.</summary>
        SpeedTooLow,
        /// <summary>El movimiento fue demasiado rápido.</summary>
        SpeedTooHigh,
        /// <summary>La distancia recorrida fue insuficiente.</summary>
        DistanceTooShort,
        /// <summary>La dirección del movimiento fue incorrecta.</summary>
        DirectionWrong,
        /// <summary>Cambios de dirección insuficientes (para gestos tipo zigzag).</summary>
        DirectionChangesInsufficient,
        /// <summary>Rotación insuficiente.</summary>
        RotationInsufficient,
        /// <summary>Movimiento no circular (para gestos que lo requieren).</summary>
        NotCircular,
        /// <summary>El gesto tomó demasiado tiempo.</summary>
        Timeout,
        /// <summary>La pose final no coincide.</summary>
        EndPoseMismatch,
        /// <summary>Tracking de la mano perdido.</summary>
        TrackingLost,
        /// <summary>La mano salió de la zona espacial requerida.</summary>
        OutOfZone,
        /// <summary>Razón desconocida o no clasificada.</summary>
        Unknown
    }

    /// <summary>
    /// Fase del gesto dinámico donde ocurrió el fallo.
    /// </summary>
    public enum GesturePhase
    {
        /// <summary>No aplica (gesto estático o sin fallo).</summary>
        None = 0,
        /// <summary>Fallo en la pose inicial.</summary>
        Start,
        /// <summary>Fallo durante el movimiento.</summary>
        Move,
        /// <summary>Fallo en la pose final o validación.</summary>
        End
    }

    /// <summary>
    /// Fases del feedback para gestos dinámicos.
    /// Cada fase tiene un tipo de feedback diferente adaptado al momento del gesto.
    /// </summary>
    public enum DynamicFeedbackPhase
    {
        /// <summary>
        /// Fase 0: Sistema esperando. El usuario debe colocar la mano en posición inicial.
        /// Feedback: orientación general, sin correcciones finas.
        /// </summary>
        Idle = 0,

        /// <summary>
        /// Fase 1: Pose inicial detectada correctamente.
        /// Feedback: confirmación positiva, invitación a comenzar el movimiento.
        /// </summary>
        StartDetected,

        /// <summary>
        /// Fase 2: Movimiento en progreso.
        /// Feedback: guía sobre dirección, velocidad, amplitud, continuidad.
        /// NO se corrigen dedos individuales.
        /// </summary>
        InProgress,

        /// <summary>
        /// Fase 3: Casi completado (>80% del gesto).
        /// Feedback: ánimo para terminar, evitar corte prematuro.
        /// </summary>
        NearCompletion,

        /// <summary>
        /// Fase 4: Gesto completado correctamente.
        /// Feedback: confirmación clara y rotunda de éxito.
        /// </summary>
        Completed,

        /// <summary>
        /// Fase 5: Gesto fallido.
        /// Feedback: explicación de por qué falló, invitación a reintentar.
        /// </summary>
        Failed
    }

    /// <summary>
    /// Problema específico durante un gesto dinámico.
    /// Usado para feedback contextual durante el movimiento.
    /// </summary>
    public enum DynamicMovementIssue
    {
        None = 0,
        /// <summary>La dirección del movimiento es incorrecta.</summary>
        DirectionWrong,
        /// <summary>El movimiento es demasiado rápido.</summary>
        TooFast,
        /// <summary>El movimiento es demasiado lento.</summary>
        TooSlow,
        /// <summary>El movimiento es muy corto (amplitud insuficiente).</summary>
        TooShort,
        /// <summary>El movimiento no es continuo (cortado/pausado).</summary>
        NotContinuous,
        /// <summary>El movimiento no es circular cuando debería serlo.</summary>
        NotCircular,
        /// <summary>Faltan cambios de dirección (para gestos tipo zigzag).</summary>
        NeedMoreDirectionChanges,
        /// <summary>La rotación es insuficiente.</summary>
        RotationInsufficient,
        /// <summary>La pose de la mano se está degradando durante el movimiento.</summary>
        StartPoseDegrading
    }

    /// <summary>
    /// Estado general del feedback.
    /// </summary>
    public enum FeedbackState
    {
        /// <summary>Feedback inactivo, sin práctica.</summary>
        Inactive,
        /// <summary>Esperando que el usuario haga el gesto.</summary>
        Waiting,
        /// <summary>Detectando errores, mostrando correcciones.</summary>
        ShowingErrors,
        /// <summary>Gesto parcialmente correcto.</summary>
        PartialMatch,
        /// <summary>Gesto completamente correcto.</summary>
        Success,
        /// <summary>Gesto dinámico en progreso.</summary>
        InProgress
    }

    #endregion

    #region Structures

    /// <summary>
    /// Representa un error específico de un dedo.
    /// </summary>
    [Serializable]
    public struct FingerError
    {
        /// <summary>Dedo afectado.</summary>
        public Finger finger;

        /// <summary>Tipo de error detectado.</summary>
        public FingerErrorType errorType;

        /// <summary>Severidad del error.</summary>
        public Severity severity;

        /// <summary>
        /// Valor actual medido (ej: ángulo de curl 0-1).
        /// </summary>
        public float currentValue;

        /// <summary>
        /// Valor esperado según el constraint.
        /// </summary>
        public float expectedValue;

        /// <summary>
        /// Mensaje de corrección para el usuario.
        /// </summary>
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
        /// Crea un error sin valores numéricos específicos.
        /// </summary>
        public static FingerError Create(Finger finger, FingerErrorType errorType,
                                         Severity severity, string message)
        {
            return new FingerError(finger, errorType, severity, 0f, 0f, message);
        }
    }

    /// <summary>
    /// Resultado del análisis de un gesto estático.
    /// </summary>
    [Serializable]
    public class StaticGestureResult
    {
        /// <summary>
        /// True si el gesto cumple las condiciones globales (XRHandShape.CheckConditions).
        /// </summary>
        public bool isMatchGlobal;

        /// <summary>
        /// Errores detectados por cada dedo.
        /// Usa List para evitar entradas vacías por defecto.
        /// </summary>
        public System.Collections.Generic.List<FingerError> perFingerErrors;

        /// <summary>
        /// Mensaje resumen priorizado para mostrar al usuario.
        /// </summary>
        public string summaryMessage;

        /// <summary>
        /// Número total de errores mayores.
        /// </summary>
        public int majorErrorCount;

        /// <summary>
        /// Número total de errores menores.
        /// </summary>
        public int minorErrorCount;

        /// <summary>
        /// Timestamp de cuando se generó este resultado.
        /// </summary>
        public float timestamp;

        // === Nuevos campos para "casi correcto" ===

        /// <summary>
        /// Puntuación de coincidencia (0-1). 1 = perfecto, 0 = muy lejos.
        /// Calculado como: 1 - (majorErrors * 0.3 + minorErrors * 0.1)
        /// </summary>
        public float matchScore;

        /// <summary>
        /// True si el gesto está "casi correcto" (solo errores Minor, ningún Major).
        /// </summary>
        public bool isNearMatch;

        /// <summary>
        /// True si el resultado es estable (no está fluctuando).
        /// </summary>
        public bool isStable;

        /// <summary>
        /// Nivel de confianza del análisis (0-1).
        /// </summary>
        public float confidence;

        /// <summary>
        /// True si no hay ningún error.
        /// </summary>
        public bool IsFullyCorrect => majorErrorCount == 0 && minorErrorCount == 0;

        /// <summary>
        /// Obtiene el error MÁS SEVERO para un dedo específico.
        /// Si hay Major y Minor para el mismo dedo, devuelve Major.
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
                    // Priorizar el error más severo
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
        /// Obtiene la severidad para un dedo específico (la más alta si hay múltiples).
        /// </summary>
        public Severity GetSeverityForFinger(Finger finger)
        {
            var error = GetErrorForFinger(finger);
            return error?.severity ?? Severity.None;
        }

        /// <summary>
        /// Añade un error a la lista.
        /// </summary>
        public void AddError(FingerError error)
        {
            perFingerErrors ??= new System.Collections.Generic.List<FingerError>();
            perFingerErrors.Add(error);

            // Actualizar contadores
            if (error.severity == Severity.Major)
                majorErrorCount++;
            else if (error.severity == Severity.Minor)
                minorErrorCount++;

            // Recalcular matchScore e isNearMatch
            UpdateMatchScore();
        }

        /// <summary>
        /// Recalcula matchScore e isNearMatch basándose en los errores actuales.
        /// </summary>
        public void UpdateMatchScore()
        {
            // matchScore: 1 = perfecto, 0 = muy mal
            // Penalizar Major más que Minor
            float penalty = (majorErrorCount * 0.3f) + (minorErrorCount * 0.1f);
            matchScore = Mathf.Clamp01(1f - penalty);

            // isNearMatch: solo Minor, ningún Major
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
        /// Crea un resultado de éxito completo.
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
                summaryMessage = "¡Perfecto!"
            };
        }
    }

    /// <summary>
    /// Métricas de movimiento para gestos dinámicos.
    /// </summary>
    [Serializable]
    public struct DynamicMetrics
    {
        /// <summary>Velocidad promedio en m/s.</summary>
        public float averageSpeed;

        /// <summary>Velocidad máxima alcanzada en m/s.</summary>
        public float maxSpeed;

        /// <summary>Distancia total recorrida en metros.</summary>
        public float totalDistance;

        /// <summary>Duración del gesto en segundos.</summary>
        public float duration;

        /// <summary>Número de cambios de dirección detectados.</summary>
        public int directionChanges;

        /// <summary>Rotación total en grados.</summary>
        public float totalRotation;

        /// <summary>Score de circularidad (0-1).</summary>
        public float circularityScore;

        // === Nuevos campos de dirección ===

        /// <summary>Desplazamiento neto desde inicio hasta posición actual (start→current).</summary>
        public Vector3 netDisplacement;

        /// <summary>Dirección promedio del movimiento (normalizada).</summary>
        public Vector3 averageVelocityDirection;

        /// <summary>
        /// Alineación con la dirección esperada (dot product, 0-1).
        /// 1 = perfectamente alineado, 0 = perpendicular, -1 = opuesto.
        /// </summary>
        public float directionAlignment;

        /// <summary>
        /// Qué tan recto es el camino (0-1).
        /// 1 = línea recta, valores bajos = zigzag o curvas.
        /// </summary>
        public float pathStraightness;

        /// <summary>
        /// True si la pose de la mano sigue siendo válida durante el movimiento.
        /// </summary>
        public bool handShapeStable;
    }

    /// <summary>
    /// Resultado del análisis de un gesto dinámico.
    /// </summary>
    [Serializable]
    public class DynamicGestureResult
    {
        /// <summary>Nombre del gesto intentado.</summary>
        public string gestureName;

        /// <summary>True si el gesto se completó exitosamente.</summary>
        public bool isSuccess;

        /// <summary>Razón del fallo (si aplica).</summary>
        public FailureReason failureReason;

        /// <summary>Fase donde ocurrió el fallo.</summary>
        public GesturePhase failedPhase;

        /// <summary>Métricas del movimiento.</summary>
        public DynamicMetrics metrics;

        /// <summary>Mensaje de troubleshooting para el usuario.</summary>
        public string troubleshootingMessage;

        /// <summary>Timestamp de cuando se generó este resultado.</summary>
        public float timestamp;

        public DynamicGestureResult()
        {
            timestamp = 0f; // set when used at runtime, avoid Unity API in constructor
        }

        /// <summary>
        /// Crea un resultado de éxito.
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
        /// Crea un resultado de fallo.
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
    /// Diccionario de mensajes de corrección por tipo de error.
    /// Todos los mensajes en castellano con tono amable.
    /// </summary>
    public static class FeedbackMessages
    {
        /// <summary>
        /// Obtiene el mensaje de corrección para un error de dedo.
        /// Usa tono menos imperativo para errores Minor.
        /// Los mensajes distinguen semánticamente entre CURVAR y CERRAR:
        /// - Curvar = flexionar con control, sin tocar la palma
        /// - Cerrar = formar puño, dedo completamente plegado
        /// </summary>
        public static string GetCorrectionMessage(Finger finger, FingerErrorType errorType, Severity severity = Severity.Major)
        {
            string fingerName = GetFingerName(finger);

            // Tono suave para Minor, más directo para Major
            bool isMinor = severity == Severity.Minor;

            return errorType switch
            {
                // === Errores semánticos de tres estados (NUEVA FILOSOFÍA) ===

                // CURVAR: de extendido a curvado (sin cerrar puño)
                FingerErrorType.NeedsCurve => isMinor
                    ? $"Curva un poco más el {fingerName}"
                    : $"Curva el {fingerName} (sin cerrar)",

                // CERRAR: de curvado a puño completo
                FingerErrorType.NeedsFist => isMinor
                    ? $"Cierra un poco más el {fingerName}"
                    : $"Cierra el {fingerName} en puño",

                // DEMASIADO CERRADO: hizo puño cuando solo debía curvar
                FingerErrorType.TooMuchCurl => isMinor
                    ? $"Suelta un poco el {fingerName}"
                    : $"Suelta el {fingerName}, no cierres puño",

                // EXTENDER: de curvado/cerrado a recto
                FingerErrorType.NeedsExtend => isMinor
                    ? $"Estira un poco el {fingerName}"
                    : $"Estira el {fingerName} completamente",

                // === Errores clásicos (compatibilidad) ===
                FingerErrorType.TooExtended => isMinor
                    ? $"Flexiona un poco más el {fingerName}"
                    : $"Flexiona el {fingerName}",
                FingerErrorType.TooCurled => isMinor
                    ? $"Estira un poco el {fingerName}"
                    : $"Estira el {fingerName}",

                // === Errores de spread y posición ===
                FingerErrorType.SpreadTooNarrow => isMinor
                    ? $"Separa un poco más los dedos"
                    : $"Separa los dedos",
                FingerErrorType.SpreadTooWide => isMinor
                    ? $"Junta un poco más los dedos"
                    : $"Junta los dedos",
                FingerErrorType.ThumbPositionWrong => isMinor
                    ? "Ajusta un poco el pulgar"
                    : "Ajusta la posición del pulgar",
                FingerErrorType.ShouldTouch => isMinor
                    ? $"Acerca el {fingerName}"
                    : $"Toca con el {fingerName}",
                FingerErrorType.ShouldNotTouch => isMinor
                    ? $"Separa un poco el {fingerName}"
                    : $"Separa el {fingerName}",
                FingerErrorType.RotationWrong => isMinor
                    ? "Gira un poco la mano"
                    : "Gira la mano",
                _ => isMinor
                    ? $"Ajusta un poco el {fingerName}"
                    : $"Ajusta el {fingerName}"
            };
        }

        /// <summary>
        /// Determina el estado actual del dedo basándose en el valor de curl.
        /// </summary>
        /// <param name="curlValue">Valor de curl (0=extendido, 1=cerrado)</param>
        /// <returns>Estado semántico del dedo</returns>
        public static FingerShapeState GetFingerState(float curlValue)
        {
            // Umbrales basados en la filosofía de tres estados:
            // - Extendido: 0.0 - 0.25 (dedo recto)
            // - Curvado: 0.25 - 0.75 (forma controlada, sin tocar palma)
            // - Cerrado: 0.75 - 1.0 (puño, toca palma)
            if (curlValue < 0.25f)
                return FingerShapeState.Extended;
            if (curlValue < 0.75f)
                return FingerShapeState.Curved;
            return FingerShapeState.Closed;
        }

        /// <summary>
        /// Determina el tipo de error semántico basándose en el estado actual y el esperado.
        /// Esta es la función clave que implementa la filosofía de tres estados.
        /// </summary>
        /// <param name="currentState">Estado actual del dedo</param>
        /// <param name="expectedState">Estado que debería tener</param>
        /// <returns>Tipo de error semántico apropiado</returns>
        public static FingerErrorType GetSemanticErrorType(FingerShapeState currentState, FingerShapeState expectedState)
        {
            if (currentState == expectedState)
                return FingerErrorType.None;

            return (currentState, expectedState) switch
            {
                // De extendido a curvado → NeedsCurve
                (FingerShapeState.Extended, FingerShapeState.Curved) => FingerErrorType.NeedsCurve,

                // De extendido a cerrado → NeedsFist (saltar curvado, ir a puño)
                (FingerShapeState.Extended, FingerShapeState.Closed) => FingerErrorType.NeedsFist,

                // De curvado a cerrado → NeedsFist
                (FingerShapeState.Curved, FingerShapeState.Closed) => FingerErrorType.NeedsFist,

                // De curvado a extendido → NeedsExtend
                (FingerShapeState.Curved, FingerShapeState.Extended) => FingerErrorType.NeedsExtend,

                // De cerrado a extendido → NeedsExtend
                (FingerShapeState.Closed, FingerShapeState.Extended) => FingerErrorType.NeedsExtend,

                // De cerrado a curvado → TooMuchCurl (cerró demasiado, debe soltar)
                (FingerShapeState.Closed, FingerShapeState.Curved) => FingerErrorType.TooMuchCurl,

                // Fallback
                _ => FingerErrorType.None
            };
        }

        /// <summary>
        /// Obtiene una descripción amigable del estado esperado para un dedo.
        /// Útil para feedback contextual.
        /// </summary>
        public static string GetStateDescription(FingerShapeState state, Finger finger)
        {
            string fingerName = GetFingerName(finger);
            return state switch
            {
                FingerShapeState.Extended => $"{fingerName} recto, como señalando",
                FingerShapeState.Curved => $"{fingerName} curvado, sin tocar la palma",
                FingerShapeState.Closed => $"{fingerName} cerrado en puño",
                _ => $"{fingerName} en posición"
            };
        }

        /// <summary>
        /// Obtiene el mensaje de troubleshooting para un fallo de gesto dinámico.
        /// En castellano con tono constructivo.
        /// </summary>
        public static string GetTroubleshootingMessage(FailureReason reason, GesturePhase phase,
                                                       DynamicMetrics metrics, string gestureName)
        {
            string phaseStr = phase switch
            {
                GesturePhase.Start => "al inicio",
                GesturePhase.Move => "durante el movimiento",
                GesturePhase.End => "al final",
                _ => ""
            };

            return reason switch
            {
                FailureReason.PoseLost => $"Mantén la forma de la mano {phaseStr}",
                FailureReason.SpeedTooLow => $"Muévete más rápido (velocidad: {metrics.averageSpeed:F2} m/s)",
                FailureReason.SpeedTooHigh => "Muévete más lento y con control",
                FailureReason.DistanceTooShort => $"Haz un movimiento más amplio ({metrics.totalDistance:F2}m)",
                FailureReason.DirectionWrong => $"Muévete en la dirección correcta para '{gestureName}'",
                FailureReason.DirectionChangesInsufficient => $"Haz más movimientos de sacudida ({metrics.directionChanges} detectados)",
                FailureReason.RotationInsufficient => $"Gira más la muñeca ({metrics.totalRotation:F0}° detectados)",
                FailureReason.NotCircular => $"Haz un movimiento más circular",
                FailureReason.Timeout => "Completa el gesto más rápido",
                FailureReason.EndPoseMismatch => "Termina con la forma de mano correcta",
                FailureReason.TrackingLost => "Mantén la mano visible",
                FailureReason.OutOfZone => "Mantén la mano en la zona correcta frente a ti",
                FailureReason.Unknown => $"Ajusta el movimiento para '{gestureName}'",
                _ => $"Repite el gesto '{gestureName}'"
            };
        }

        /// <summary>
        /// Obtiene el nombre legible del dedo en castellano.
        /// </summary>
        public static string GetFingerName(Finger finger)
        {
            return finger switch
            {
                Finger.Thumb => "pulgar",
                Finger.Index => "índice",
                Finger.Middle => "corazón",
                Finger.Ring => "anular",
                Finger.Pinky => "meñique",
                _ => "dedo"
            };
        }

        /// <summary>
        /// Genera un mensaje resumen priorizando el error más importante.
        /// Adapta el tono según la cantidad y tipo de errores.
        /// </summary>
        public static string GenerateSummary(System.Collections.Generic.List<FingerError> errors, int maxMessages = 2)
        {
            if (errors == null || errors.Count == 0)
                return "¡Perfecto! Tu mano está en la posición correcta.";

            // Ordenar por severidad (Major primero)
            var sortedErrors = new System.Collections.Generic.List<FingerError>();

            // Contar tipos
            int majorCount = 0;
            int minorCount = 0;

            // Primero los Major
            foreach (var e in errors)
            {
                if (e.severity == Severity.Major)
                {
                    sortedErrors.Add(e);
                    majorCount++;
                }
            }
            // Luego los Minor
            foreach (var e in errors)
            {
                if (e.severity == Severity.Minor)
                {
                    sortedErrors.Add(e);
                    minorCount++;
                }
            }

            if (sortedErrors.Count == 0)
                return "¡Perfecto! Tu mano está en la posición correcta.";

            // Adaptar tono según contexto
            string prefix = "";
            if (majorCount == 0 && minorCount > 0)
            {
                // Solo Minor = "Casi perfecto"
                prefix = "Casi perfecto: ";
            }
            else if (majorCount > 2)
            {
                // Muchos Major = orientación macro
                prefix = "Empieza por: ";
            }

            var messages = new System.Collections.Generic.List<string>();
            int count = Mathf.Min(maxMessages, sortedErrors.Count);

            for (int i = 0; i < count; i++)
            {
                if (!string.IsNullOrEmpty(sortedErrors[i].correctionMessage))
                {
                    messages.Add(sortedErrors[i].correctionMessage);
                }
            }

            return prefix + string.Join("\n", messages);
        }

        /// <summary>
        /// Genera un mensaje resumen (overload para arrays, compatibilidad).
        /// </summary>
        public static string GenerateSummary(FingerError[] errors, int maxMessages = 2)
        {
            if (errors == null) return "¡Perfecto! Tu mano está en la posición correcta.";
            var list = new System.Collections.Generic.List<FingerError>(errors);
            return GenerateSummary(list, maxMessages);
        }

        #region Dynamic Gesture Phase Messages

        /// <summary>
        /// Obtiene el mensaje de feedback para la fase Idle (esperando pose inicial).
        /// </summary>
        public static string GetIdlePhaseMessage(string gestureName)
        {
            return $"Coloca la mano para '{gestureName}'";
        }

        /// <summary>
        /// Obtiene el mensaje cuando la pose inicial es detectada.
        /// </summary>
        public static string GetStartDetectedMessage(string gestureName)
        {
            return $"¡Bien! Ahora empieza el movimiento";
        }

        /// <summary>
        /// Obtiene el mensaje de feedback durante el movimiento.
        /// Prioriza un solo mensaje para evitar sobrecarga cognitiva.
        /// </summary>
        public static string GetInProgressMessage(DynamicMovementIssue issue, DynamicMetrics metrics, string gestureName)
        {
            return issue switch
            {
                DynamicMovementIssue.None => "Sigue el movimiento",
                DynamicMovementIssue.DirectionWrong => GetDirectionHint(gestureName),
                DynamicMovementIssue.TooFast => "Más lento",
                DynamicMovementIssue.TooSlow => "Más rápido",
                DynamicMovementIssue.TooShort => "Haz el gesto más amplio",
                DynamicMovementIssue.NotContinuous => "Continúa sin pausar",
                DynamicMovementIssue.NotCircular => "Haz un movimiento más circular",
                DynamicMovementIssue.NeedMoreDirectionChanges => "Mueve hacia un lado y otro",
                DynamicMovementIssue.RotationInsufficient => "Gira más la muñeca",
                DynamicMovementIssue.StartPoseDegrading => "Mantén la forma de la mano",
                _ => "Sigue el movimiento"
            };
        }

        /// <summary>
        /// Obtiene una pista de dirección específica para el gesto.
        /// </summary>
        private static string GetDirectionHint(string gestureName)
        {
            // Mensajes genéricos que funcionan para la mayoría de gestos
            // El sistema puede extenderse con un diccionario de direcciones por gesto
            return "Ajusta la dirección del movimiento";
        }

        // Cache para evitar parpadeo de mensajes de NearCompletion
        private static string _cachedNearCompletionMessage = null;
        private static float _nearCompletionMessageTime = 0f;
        private const float NEAR_COMPLETION_MESSAGE_DURATION = 2f;

        /// <summary>
        /// Obtiene el mensaje cuando el gesto está casi completado.
        /// El mensaje se cachea para evitar parpadeos (latch).
        /// </summary>
        public static string GetNearCompletionMessage()
        {
            // Si tenemos un mensaje cacheado y no ha expirado, usarlo
            if (_cachedNearCompletionMessage != null && (Time.time - _nearCompletionMessageTime) < NEAR_COMPLETION_MESSAGE_DURATION)
            {
                return _cachedNearCompletionMessage;
            }

            // Elegir un nuevo mensaje y cachearlo
            string[] messages = new string[]
            {
                "¡Casi!",
                "Termina el movimiento",
                "Un poco más",
                "Ya casi"
            };
            _cachedNearCompletionMessage = messages[UnityEngine.Random.Range(0, messages.Length)];
            _nearCompletionMessageTime = Time.time;

            return _cachedNearCompletionMessage;
        }

        /// <summary>
        /// Resetea el cache de mensaje de NearCompletion.
        /// Llamar cuando se sale de la fase NearCompletion.
        /// </summary>
        public static void ResetNearCompletionCache()
        {
            _cachedNearCompletionMessage = null;
            _nearCompletionMessageTime = 0f;
        }

        /// <summary>
        /// Obtiene el mensaje de éxito al completar el gesto dinámico.
        /// Mensaje claro y rotundo que indica éxito.
        /// </summary>
        public static string GetCompletedMessage(string gestureName)
        {
            return "¡Movimiento reconocido!";
        }

        /// <summary>
        /// Obtiene el mensaje de fallo con explicación clara de la razón.
        /// Invita a reintentar sin ser punitivo.
        /// </summary>
        public static string GetFailedMessage(FailureReason reason, GesturePhase phase, DynamicMetrics metrics, string gestureName)
        {
            string explanation = reason switch
            {
                FailureReason.SpeedTooLow => "El gesto fue demasiado lento",
                FailureReason.SpeedTooHigh => "El gesto fue demasiado rápido",
                FailureReason.DistanceTooShort => "El movimiento fue muy corto",
                FailureReason.DirectionWrong => "La dirección no fue correcta",
                FailureReason.DirectionChangesInsufficient => "Faltaron cambios de dirección",
                FailureReason.RotationInsufficient => "Faltó rotación de muñeca",
                FailureReason.NotCircular => "El movimiento no fue circular",
                FailureReason.Timeout => "El gesto tomó demasiado tiempo",
                FailureReason.PoseLost => phase == GesturePhase.Start
                    ? "Empezaste a moverte antes de tiempo"
                    : "Perdiste la forma de la mano durante el movimiento",
                FailureReason.EndPoseMismatch => "La pose final no fue correcta",
                FailureReason.TrackingLost => "Mantén la mano visible para el sensor",
                FailureReason.OutOfZone => "La mano salió de la zona de detección",
                FailureReason.Unknown => $"El gesto '{gestureName}' no se completó",
                _ => $"Repite el gesto '{gestureName}'"
            };

            return $"{explanation}. Repite manteniendo la forma de la mano.";
        }

        /// <summary>
        /// Detecta el problema principal durante el movimiento basándose en las métricas.
        /// Retorna solo UN problema (el más importante) para evitar sobrecarga.
        /// </summary>
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
            // Prioridad de problemas (de más a menos importante)

            // 0. PRIORIDAD MÁXIMA: Pose de la mano inestable
            if (!metrics.handShapeStable)
                return DynamicMovementIssue.StartPoseDegrading;

            // 1. Velocidad demasiado lenta (bloquea todo)
            if (metrics.averageSpeed < expectedMinSpeed * 0.5f)
                return DynamicMovementIssue.TooSlow;

            // 2. Velocidad demasiado rápida (pierde control)
            if (expectedMaxSpeed > 0 && metrics.maxSpeed > expectedMaxSpeed * 1.5f)
                return DynamicMovementIssue.TooFast;

            // 3. Dirección incorrecta (si se requiere dirección específica)
            if (requiresSpecificDirection && metrics.directionAlignment < minDirectionAlignment)
                return DynamicMovementIssue.DirectionWrong;

            // 4. Movimiento muy corto
            if (metrics.duration > 0.3f && metrics.totalDistance < expectedMinDistance * 0.5f)
                return DynamicMovementIssue.TooShort;

            // 5. Rotación insuficiente
            if (requiresRotation && metrics.totalRotation < minRotationAngle * 0.7f)
                return DynamicMovementIssue.RotationInsufficient;

            // 6. Circularidad insuficiente
            if (requiresCircular && metrics.circularityScore < minCircularityScore * 0.7f)
                return DynamicMovementIssue.NotCircular;

            // 7. Cambios de dirección insuficientes
            if (requiresDirectionChanges && metrics.directionChanges < requiredChanges)
                return DynamicMovementIssue.NeedMoreDirectionChanges;

            // Sin problemas detectados
            return DynamicMovementIssue.None;
        }

        /// <summary>
        /// Versión simplificada de DetectMovementIssue para compatibilidad.
        /// </summary>
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
