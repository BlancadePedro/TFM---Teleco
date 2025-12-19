# Dynamic Gestures System

Sistema de reconocimiento de gestos dinámicos para Meta Quest 3 basado en **secuencias de poses estáticas + movimiento**.

## Arquitectura

```
┌─────────────────────────────────────────────────────────────┐
│                    Tu Escena Unity                          │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  MultiGestureRecognizer (Sistema Existente)          │  │
│  │  - Detecta poses estáticas (J, 5, OK, S, etc.)     │  │
│  │  - Emite: onGestureRecognized                        │  │
│  └────────────────────┬─────────────────────────────────┘  │
│                       │                                      │
│                       ▼                                      │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  StaticPoseAdapter (Adaptador)                       │  │
│  │  - Convierte eventos a método GetCurrentPoseName()   │  │
│  └────────────────────┬─────────────────────────────────┘  │
│                       │                                      │
│                       ▼                                      │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  DynamicGestureRecognizer (Motor Principal)          │  │
│  │  ┌────────────────────────────────────────────────┐  │  │
│  │  │  Máquina de Estados                            │  │  │
│  │  │  ┌──────┐  pose  ┌───────────┐  validación  │  │  │
│  │  │  │ Idle ├────────►InProgress ├───────────►   │  │  │
│  │  │  └──────┘  inicial└───────────┘  exitosa     │  │  │
│  │  │       ▲                  │                     │  │  │
│  │  │       │                  │ fallo/timeout       │  │  │
│  │  │       └──────────────────┘                     │  │  │
│  │  └────────────────────────────────────────────────┘  │  │
│  │  │                                                     │  │
│  │  │  Usa: MovementTracker                              │  │
│  │  │  Lee: DynamicGestureDefinition (ScriptableObjects)│  │
│  │  │                                                     │  │
│  │  │  Emite Eventos:                                    │  │
│  │  │  - OnGestureStarted(name)                         │  │
│  │  │  - OnGestureProgress(name, progress)              │  │
│  │  │  - OnGestureCompleted(name)                       │  │
│  │  │  - OnGestureFailed(name, reason)                  │  │
│  │  └─────────────────┬───────────────────────────────┘  │
│  │                    │                                    │
│  │                    ▼                                    │
│  │  ┌──────────────────────────────────────────────────┐  │
│  │  │  DynamicGestureListener (Tu Código)              │  │
│  │  │  - Escucha eventos                                │  │
│  │  │  - Actualiza UI, reproduce audio, etc.           │  │
│  │  └──────────────────────────────────────────────────┘  │
│  └─────────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────────┘
```

## Archivos

### Core

- **GestureEnums.cs** - Enumeraciones compartidas
  - `PoseTimingRequirement`: Cuándo se requiere una pose (Start/During/End/Any)
  - `GestureState`: Estados de la máquina (Idle/InProgress)
  - `StaticPoseRequirement`: Definición de pose requerida

- **DynamicGestureDefinition.cs** - ScriptableObject para configurar gestos
  - Define: poses, movimientos, duraciones, tolerancias
  - Soporta: direccionalidad, cambios de dirección, rotación, circularidad, zonas espaciales
  - Validación automática de parámetros

- **MovementTracker.cs** - Análisis de movimiento sin allocations
  - Calcula: distancia, velocidad, dirección, rotación, cambios de dirección
  - Método especial `GetCircularityScore()` para movimientos circulares

### Integración

- **StaticPoseAdapter.cs** - Puente con tu sistema existente
  - Conecta `MultiGestureRecognizer` con `DynamicGestureRecognizer`
  - Proporciona API simple: `GetCurrentPoseName()`

- **DynamicGestureRecognizer.cs** - Reconocedor principal
  - Máquina de estados robusta
  - Validación progresiva de requisitos
  - Suavizado de tracking
  - Gizmos de debug

### Utilidad

- **DynamicGestureListener.cs** - Script de ejemplo
  - Muestra cómo suscribirse a eventos
  - Integración con UI opcional
  - Base para tu implementación personalizada

## Flujo de Detección

1. **Usuario hace pose inicial** (ej: mano abierta "5")
   - `MultiGestureRecognizer` detecta pose → emite evento
   - `StaticPoseAdapter` captura evento → actualiza `currentPoseName`

2. **DynamicGestureRecognizer en Update()**
   - Estado Idle: Llama `CheckForGestureStart()`
   - Lee pose actual de `StaticPoseAdapter.GetCurrentPoseName()`
   - Busca en lista si algún gesto puede iniciar con esa pose
   - Si encuentra: `StartGesture()` → Estado InProgress

3. **Validación continua en Update()**
   - Estado InProgress: Llama `UpdateGestureProgress()`
   - Obtiene posición/rotación de mano vía XRHandSubsystem
   - Aplica suavizado para reducir jitter
   - Actualiza `MovementTracker` con nueva posición
   - Valida requisitos:
     - Poses intermedias (During)
     - Dirección de movimiento
     - Velocidad
     - Distancia (solo después de minDuration)
     - Cambios de dirección
     - Rotación (si aplica)
     - Circularidad (si aplica)
     - Zona espacial (si aplica)
   - Si falla validación crítica → `FailGesture(razón)`
   - Si pasa minDuration y todo OK → `CompleteGesture()`

4. **Emisión de eventos**
   - `OnGestureStarted`: Al iniciar (paso 2)
   - `OnGestureProgress`: Cada frame con progreso 0-1
   - `OnGestureCompleted`: Al validar exitosamente
   - `OnGestureFailed`: Si falla cualquier requisito

## Optimizaciones

### Cero Allocations en Update

```csharp
// ✅ BIEN - Sin allocations
Queue<Vector3> positionHistory = new Queue<Vector3>(120); // Preallocado en constructor

void Update() {
    positionHistory.Enqueue(newPosition);
    // ... procesamiento
}

// ❌ MAL - Allocations cada frame
void Update() {
    var positions = positionHistory.ToArray(); // ¡ALLOCATIONS!
}
```

### Caché de Estructuras

```csharp
// MovementTracker cachea arrays para cálculos
private readonly List<Vector3> cachedPositions;
private readonly List<float> cachedRadii;

public float GetCircularityScore() {
    cachedPositions.Clear(); // Reusa lista existente
    foreach (var pos in positionHistory)
        cachedPositions.Add(pos);
    // ... cálculos sin allocations
}
```

### Validaciones Progresivas

```csharp
// No valida todo desde el inicio
float elapsed = Time.time - gestureStartTime;

if (elapsed < minDuration * 0.5f) {
    // Solo valida tracking y pose inicial
} else if (elapsed < minDuration * 0.8f) {
    // Valida dirección con margen
} else {
    // Validación completa estricta
}
```

## Parámetros Conservadores

Para Quest 3, estos son valores **seguros** que minimizan false negatives:

```csharp
directionTolerance = 45f;  // Angular - MÍNIMO 40°
minSpeed = 0.12f;          // m/s - NO menor a 0.12
minDistance = 0.08f;       // metros - Mínimo 0.06m
minDuration = 0.4f;        // segundos - Dar tiempo
maxDuration = 3f;          // segundos - Timeout generoso
```

**Razón:** Quest 3 tracking tiene:
- Jitter en posición (~1-2mm)
- Jitter en rotación (~2-5°)
- Latencia (~50ms)
- Oclusión frecuente

Valores muy estrictos = usuarios frustrados.

## Extensibilidad

### Añadir Nuevo Tipo de Validación

1. Añade campo a `DynamicGestureDefinition`:
```csharp
[Header("Mi Nueva Validación")]
public bool requiresMyFeature = false;
public float myFeatureThreshold = 0.5f;
```

2. Añade método a `MovementTracker`:
```csharp
public float GetMyFeatureScore() {
    // Tu lógica
    return score;
}
```

3. Añade validación en `DynamicGestureRecognizer.UpdateGestureProgress()`:
```csharp
if (activeGesture.requiresMyFeature) {
    if (!ValidateMyFeature()) {
        FailGesture("Mi feature no cumplida");
        return;
    }
}
```

### Añadir Soporte a Mano Izquierda

En `DynamicGestureRecognizer.GetHandPosition()`:
```csharp
[SerializeField] private bool useLeftHand = false; // Añadir campo

Vector3 GetHandPosition() {
    // ...
    XRHand hand = useLeftHand ? subsystem.leftHand : subsystem.rightHand;
    // ...
}
```

## Testing

### Unit Tests (Futuros)

Estructura sugerida:
```
Tests/
├── MovementTrackerTests.cs
│   └── TestCircularityScore_PerfectCircle_Returns1()
│   └── TestDirectionChanges_ZigzagPath_CountsCorrectly()
├── DynamicGestureDefinitionTests.cs
│   └── TestValidation_InvalidDurations_LogsWarning()
└── DynamicGestureRecognizerTests.cs
    └── TestGestureCompletion_ValidMovement_EmitsEvent()
```

### Integration Tests

1. **Mock StaticPoseAdapter**: Simular poses sin XR
2. **Playback de Trayectorias**: Reproducir movimientos grabados
3. **Validar Eventos**: Verificar que emite en orden correcto

## Performance

### Benchmarks Esperados (Quest 3)

- CPU: < 0.5ms/frame
- GC: 0 B/frame (después de warm-up)
- Detección: < 100ms latencia

### Profiling

Puntos a medir:
- `MovementTracker.UpdateTracking()` - Debe ser < 0.1ms
- `DynamicGestureRecognizer.Update()` - Debe ser < 0.3ms
- Allocations en hot paths - Debe ser 0 B

## Troubleshooting

Ver documentación completa en:
- [DYNAMIC_GESTURES_INTEGRATION_GUIDE.md](../../../DYNAMIC_GESTURES_INTEGRATION_GUIDE.md) - Troubleshooting detallado
- [QUICK_START_DYNAMIC_GESTURES.md](../../../QUICK_START_DYNAMIC_GESTURES.md) - Problemas comunes

## Licencia

Libre para uso en proyecto ASL Learn VR.

## Autor

Sistema diseñado e implementado por Claude Sonnet 4.5 (Anthropic) - Diciembre 2025
