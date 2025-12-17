# Sistema de Reconocimiento de Gestos Dinámicos V2

## Descripción General

Sistema robusto de reconocimiento de gestos dinámicos para Unity XR basado en:
- **XR Hands oficial** (Unity XR Hands + XR Interaction Toolkit)
- **Máquina de estados explícita** (FSM)
- **Evaluación de movimiento en tiempo real**
- **Sin machine learning**
- **Sin comparación directa de grabaciones**

## Arquitectura del Sistema

### Componentes Principales

1. **DynamicGestureDefinition** (ScriptableObject)
   - Define los parámetros configurables de un gesto dinámico
   - No contiene grabaciones, solo criterios de evaluación

2. **TrajectoryBuffer**
   - Almacena secuencia temporal de posiciones de joints
   - Proporciona análisis del movimiento (distancia, velocidad, dirección, curvatura, suavidad)

3. **GestureStateMachine**
   - Gestiona el ciclo de vida del reconocimiento
   - Estados: Idle, WaitingForStartPose, Recording, WaitingForEndPose, Evaluating, Completed, Failed, Cooldown

4. **DynamicGestureRecognizerV2** (MonoBehaviour)
   - Componente principal que integra todos los sistemas
   - Lee datos de XR Hands
   - Evalúa gestos en tiempo real
   - Emite eventos de progreso

5. **IStaticPoseDetector** (Interfaz)
   - Interfaz para integración con detectores de poses estáticas
   - Permite requerir poses inicial/final en gestos dinámicos

6. **GestureRecognizerAdapter**
   - Adaptador que permite al GestureRecognizer existente implementar IStaticPoseDetector

## Cómo Usar el Sistema

### Paso 1: Crear una Definición de Gesto

1. Click derecho en Project → Create → ASL Learn VR → Dynamic Gesture Definition
2. Configura los parámetros del gesto:

```
Ejemplo para letra "J":
- Gesture Name: "J"
- Tracked Joint: IndexTip (o LittleTip)
- Primary Direction: Vector3(0, -1, 0) (hacia abajo)
- Direction Tolerance: 45°
- Minimum Distance: 0.10m
- Maximum Distance: 0.30m
- Minimum Average Speed: 0.05 m/s
- Maximum Average Speed: 2.0 m/s
- Minimum Duration: 0.3s
- Maximum Duration: 2.0s
```

### Paso 2: Configurar el Reconocedor en la Escena

1. Añade `DynamicGestureRecognizerV2` a un GameObject
2. Asigna referencias:
   - **Gesture Definition**: El ScriptableObject creado
   - **Hand Tracking Events**: El componente XRHandTrackingEvents de la mano (Right Hand o Left Hand)
   - **XR Origin**: Transform del XROrigin (se detecta automáticamente si no se asigna)
   - **Static Pose Detector Component** (opcional): Para integración con poses estáticas

3. Configura eventos en el Inspector:
   - On Gesture Started
   - On Gesture Progress
   - On Gesture Completed
   - On Gesture Failed

### Paso 3: Integración con Poses Estáticas (Opcional)

Si tu gesto requiere una pose inicial o final:

1. Añade `GestureRecognizerAdapter` al GameObject que tiene el `GestureRecognizer` estático
2. En `DynamicGestureRecognizerV2`, asigna este componente a "Static Pose Detector Component"
3. En `DynamicGestureDefinition`, asigna:
   - **Required Start Pose**: SignData de la pose inicial
   - **Required End Pose**: SignData de la pose final
   - Marca si requieren estar confirmadas (hold) o solo detectadas

### Paso 4: Código de Ejemplo

```csharp
using UnityEngine;
using ASL_LearnVR.Gestures;

public class MiControlador : MonoBehaviour
{
    [SerializeField] private DynamicGestureRecognizerV2 recognizer;

    void OnEnable()
    {
        recognizer.onGestureCompleted.AddListener(OnGestureSuccess);
        recognizer.onGestureFailed.AddListener(OnGestureFailed);
    }

    void OnGestureSuccess(DynamicGestureDefinition gesture)
    {
        Debug.Log($"¡Correcto! Gesto: {gesture.gestureName}");
        // Avanzar al siguiente signo, reproducir audio, etc.
    }

    void OnGestureFailed(DynamicGestureDefinition gesture, string reason)
    {
        Debug.Log($"Intenta de nuevo. Razón: {reason}");
    }
}
```

## Eventos Disponibles

- **onGestureStarted(DynamicGestureDefinition)**: Cuando comienza el gesto
- **onGestureProgress(DynamicGestureDefinition, float)**: Durante el progreso (0-1)
- **onGestureCompleted(DynamicGestureDefinition)**: Cuando se completa exitosamente
- **onGestureFailed(DynamicGestureDefinition, string)**: Cuando falla (incluye razón)

## Parámetros Configurables

### Identificación
- **Gesture Name**: Nombre del gesto
- **Description**: Descripción textual

### Joint Tracking
- **Tracked Joint**: Joint principal a seguir
- **Secondary Joints**: Joints adicionales (futuro)

### Requisitos de Movimiento
- **Primary Direction**: Dirección esperada (Vector3 normalizado)
- **Direction Tolerance**: Tolerancia angular (0-180°)
- **Minimum Distance**: Distancia mínima en metros
- **Maximum Distance**: Distancia máxima (0 = sin límite)
- **Minimum Average Speed**: Velocidad mínima (m/s)
- **Maximum Average Speed**: Velocidad máxima (0 = sin límite)

### Restricciones de Tiempo
- **Minimum Duration**: Duración mínima (segundos)
- **Maximum Duration**: Duración máxima (segundos)
- **Cooldown Time**: Espera antes de nuevo gesto

### Poses Estáticas (Opcional)
- **Required Start Pose**: Pose inicial requerida
- **Require Start Pose Confirmed**: ¿Debe estar confirmada?
- **Required End Pose**: Pose final requerida
- **Require End Pose Confirmed**: ¿Debe estar confirmada?

### Suavidad de Trayectoria
- **Max Curvature**: Curvatura máxima permitida (0=recta, 1=curva)
- **Evaluate Smoothness**: ¿Evaluar suavidad?

### Sampling
- **Sampling Rate**: Frecuencia de muestreo (Hz)
- **Max Buffer Size**: Tamaño máximo del buffer

## Flujo de Estados

```
Idle
  ↓
WaitingForStartPose (si requiere pose inicial)
  ↓
Recording (captura trayectoria)
  ↓
WaitingForEndPose (si requiere pose final)
  ↓
Evaluating (valida criterios)
  ↓
Completed/Failed
  ↓
Cooldown
  ↓
Idle
```

## Validaciones Realizadas

El sistema valida automáticamente:

1. **Puntos mínimos**: Al menos 10 puntos capturados
2. **Duración**: Entre minimumDuration y maximumDuration
3. **Distancia**: Entre minimumDistance y maximumDistance
4. **Velocidad**: Entre minimumAverageSpeed y maximumAverageSpeed
5. **Dirección**: Ángulo con primaryDirection < directionTolerance
6. **Curvatura**: Curvatura < maxCurvature
7. **Suavidad**: Suavidad > 0.3 (si evaluateSmoothness está activo)

Si alguna validación falla, el evento `onGestureFailed` se dispara con la razón específica.

## Ventajas sobre el Sistema Anterior

✅ **No depende de grabaciones específicas** - Los gestos se definen por parámetros
✅ **Más robusto** - Múltiples validaciones de calidad
✅ **Extensible** - Fácil añadir nuevos criterios
✅ **Configurable** - Todo desde el Inspector
✅ **Feedback en tiempo real** - Eventos de progreso
✅ **Integración con poses estáticas** - Mediante interfaz
✅ **Debugging visual** - Gizmos para ver trayectoria
✅ **FSM explícita** - Estados claros y predecibles

## Debugging

### Logs
Activa `Show Debug Logs` en el Inspector para ver:
- Cambios de estado
- Valores de validación (distancia, velocidad, etc.)
- Razones de fallo

### Visualización
Activa `Visualize Trajectory` para ver:
- Trayectoria capturada (cyan)
- Punto inicial (verde)
- Punto final (rojo)

## Migración desde DynamicGestureRecognizer Antiguo

El sistema antiguo (`DynamicGestureRecognizer`) usaba grabaciones y DTW.
Para migrar:

1. Crea `DynamicGestureDefinition` para cada gesto
2. Observa las grabaciones existentes para determinar parámetros aproximados
3. Usa `DynamicGestureRecognizerV2` en lugar de `DynamicGestureRecognizer`
4. Ajusta parámetros mediante prueba y error

## API Pública

### Métodos
- `StartGestureRecognition()`: Inicia manualmente el reconocimiento
- `CancelGesture()`: Cancela el gesto actual
- `ResetRecognizer()`: Resetea al estado inicial

### Propiedades
- `CurrentState`: Estado actual de la FSM
- `IsActive`: ¿Está reconociendo activamente?
- `Progress`: Progreso del gesto (0-1)

## Ejemplos de Configuración

### Gesto "J" (hook hacia abajo)
```
Primary Direction: (0, -1, 0)
Direction Tolerance: 45°
Minimum Distance: 0.1m
Minimum Duration: 0.3s
Tracked Joint: LittleTip
```

### Gesto "Z" (zigzag)
```
Primary Direction: (0, 0, 1) o basado en cámara
Direction Tolerance: 60°
Minimum Distance: 0.15m
Max Curvature: 0.8 (permite zigzag)
Tracked Joint: IndexTip
```

## Troubleshooting

**Problema**: "No hay DynamicGestureDefinition asignada"
- Solución: Asigna un ScriptableObject de tipo DynamicGestureDefinition

**Problema**: "FALTA ASIGNAR handTrackingEvents"
- Solución: Arrastra el GameObject de la mano (Right Hand/Left Hand) al campo

**Problema**: Gesto siempre falla por "Dirección incorrecta"
- Solución: Aumenta Direction Tolerance o ajusta Primary Direction

**Problema**: Gesto siempre falla por "Trayectoria muy curva"
- Solución: Aumenta Max Curvature

**Problema**: El gesto no se inicia
- Solución: Si requiere pose inicial, verifica que el Static Pose Detector esté configurado

## Contacto y Soporte

Para problemas, consulta los logs con `Show Debug Logs` activado.
Los mensajes de error incluyen razones específicas de fallo.
