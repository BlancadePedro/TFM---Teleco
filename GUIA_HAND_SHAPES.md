# Guía: Cómo Asignar Hand Shapes a la Detección de Gestos ASL

## Resumen
Tienes dos sistemas de detección de gestos:
1. **ASLHandRecognizer** - Usa Machine Learning (modelo ONNX)
2. **ASLGestureDetector** (NUEVO) - Usa Hand Shapes de Unity XR Hands

## Opción 1: Usar Hand Shapes con ASLGestureDetector (Recomendado)

### Paso 1: Preparar la Escena

1. En Unity, abre tu escena VR
2. Localiza el GameObject que tiene el componente `XRHandTrackingEvents` (debería haber uno para cada mano)

### Paso 2: Añadir el Componente ASLGestureDetector

1. Crea un nuevo GameObject vacío llamado "ASL Gesture Detector Right"
2. Añade el componente `ASLGestureDetector` (está en `Assets/Scripts/`)
3. En el Inspector:
   - **Hand Tracking Events**: Arrastra el componente `XRHandTrackingEvents` de la mano derecha
   - **Handedness**: Selecciona "Right"
   - **Detection Interval**: 0.1 (ajustable)
   - **Show Debug Logs**: ✓ (para ver qué se detecta)
   - **Debug Display**: Arrastra tu `ASLDebugDisplay` si lo tienes

### Paso 3: Configurar los Gestos

#### Opción A: Añadir manualmente cada gesto

1. En el componente `ASLGestureDetector`, expande la lista "Gesture Configs"
2. Haz clic en el botón "+" para añadir un nuevo gesto
3. Para cada gesto configura:
   - **Gesture Name**: "A" (por ejemplo)
   - **Hand Shape Or Pose**: Arrastra el asset `ASL_Letter_A_Shape.asset` desde `Assets/XR/ASL Signs/Alphabet/`
   - **Minimum Hold Time**: 0.3 segundos
   - **On Gesture Detected**: Añade un evento (opcional)

#### Opción B: Usar el menú de contexto

1. Haz clic derecho en el componente `ASLGestureDetector`
2. Selecciona "Add All Alphabet Gestures"
3. Esto creará 26 entradas (A-Z)
4. Ahora debes asignar manualmente cada Hand Shape:
   - Para "A": arrastra `ASL_Letter_A_Shape.asset`
   - Para "B": arrastra `ASL_Letter_B_Shape.asset`
   - Y así sucesivamente...

### Paso 4: Configurar Eventos (Opcional)

Para cada gesto puedes añadir eventos que se ejecuten cuando se detecte:

1. Expande el gesto (ej. "A")
2. En "On Gesture Detected", haz clic en "+"
3. Arrastra un GameObject que tenga un script con el método que quieres llamar
4. Selecciona el método en el dropdown

**Ejemplo rápido**: El script `ASLGestureDetector` ya incluye métodos públicos:
- Añade el mismo GameObject que tiene el `ASLGestureDetector`
- Selecciona `ASLGestureDetector > OnGestureA()` (o B, C, etc.)
- Esto escribirá un log cuando se detecte ese gesto

### Paso 5: Repetir para la Mano Izquierda

1. Duplica el GameObject "ASL Gesture Detector Right"
2. Renómbralo a "ASL Gesture Detector Left"
3. Cambia:
   - **Hand Tracking Events**: Usa el de la mano izquierda
   - **Handedness**: Selecciona "Left"

---

## Opción 2: Combinar con Machine Learning (Híbrido)

Si quieres usar tanto Hand Shapes como el modelo ML:

### Cuándo usar cada uno:
- **Hand Shapes**: Para gestos estáticos simples (A, B, C, S, etc.)
- **ML Model**: Para gestos complejos o con movimiento (J, Z, etc.)

### Configuración híbrida:

1. Mantén tu `ASLHandRecognizer` activo
2. Añade el `ASLGestureDetector` en paralelo
3. En los eventos de `ASLGestureDetector`, puedes desactivar temporalmente el `ASLHandRecognizer` para evitar detecciones duplicadas

---

## Estructura de Archivos

```
Assets/
├── Scripts/
│   ├── ASLGestureDetector.cs          (NUEVO - Detector basado en Hand Shapes)
│   └── Model v1/
│       └── ASLHandRecognizer.cs       (EXISTENTE - Detector con ML)
└── XR/
    └── ASL Signs/
        └── Alphabet/
            ├── ASL_Letter_A_Shape.asset
            ├── ASL_Letter_B_Shape.asset
            ├── ASL_Letter_C_Shape.asset
            ├── ...
            ├── ASL_Letter_G_Pose.asset    (Algunos usan Pose en lugar de Shape)
            ├── ASL_Letter_H_Pose.asset
            └── ...
```

---

## Diferencias entre Hand Shape y Hand Pose

### XRHandShape
- Solo verifica la **forma de los dedos**
- Ignora la orientación de la mano
- Ejemplos: A, B, C, D, E, F, I, M, N, O, R, S, T, V, W, Y

### XRHandPose
- Verifica **forma + orientación de la mano**
- Necesita que la mano esté en una posición específica
- Ejemplos: G, H, K, L, P, Q, U, X (que requieren orientación específica)

En tu carpeta tienes ambos:
- `ASL_Letter_A_Shape.asset` - Solo forma
- `ASL_Letter_G_Pose.asset` - Forma + orientación

El script `ASLGestureDetector` **maneja ambos automáticamente**.

---

## Troubleshooting

### Los gestos no se detectan

1. **Verifica que Hand Tracking esté funcionando:**
   - Pon las manos frente al visor
   - Verifica que `XRHandTrackingEvents.handIsTracked` sea `true`

2. **Ajusta las tolerancias:**
   - Abre el Hand Shape asset en el Inspector
   - Aumenta los valores de `Upper Tolerance` y `Lower Tolerance` (prueba con 0.3-0.4)

3. **Verifica la configuración del Hand Shape:**
   - Los Hand Shapes deben estar correctamente configurados
   - Cada dedo debe tener sus condiciones definidas

4. **Reduce el Minimum Hold Time:**
   - Si los gestos son muy rápidos, reduce este valor a 0.1-0.2 segundos

### Se detectan gestos incorrectos

1. **Aumenta el Minimum Hold Time:**
   - Esto evita detecciones accidentales durante transiciones

2. **Ajusta las tolerancias del Hand Shape:**
   - Reduce las tolerancias para hacer la detección más estricta

3. **Usa Hand Poses en lugar de Hand Shapes:**
   - Para gestos que requieren orientación específica (G, H, K, L, P, Q, U, X)

---

## Ejemplo de Configuración Completa

### GameObject Hierarchy:
```
XR Origin
├── Camera Offset
│   ├── Main Camera
│   ├── LeftHand Controller
│   │   └── XRHandTrackingEvents (Left)
│   └── RightHand Controller
│       └── XRHandTrackingEvents (Right)
├── ASL Gesture Detector Left
│   └── ASLGestureDetector (handedness: Left)
├── ASL Gesture Detector Right
│   └── ASLGestureDetector (handedness: Right)
└── Debug Display
    └── ASLDebugDisplay
```

---

## Próximos Pasos

1. ✅ Crea los GameObjects con `ASLGestureDetector`
2. ✅ Asigna los Hand Shapes a cada gesto
3. ✅ Prueba en el Quest con manos reales
4. ✅ Ajusta tolerancias según sea necesario
5. ⬜ Decide si mantener ML, Hand Shapes, o ambos

---

## Comparación: ML vs Hand Shapes

| Aspecto | Machine Learning (ASLHandRecognizer) | Hand Shapes (ASLGestureDetector) |
|---------|-------------------------------------|----------------------------------|
| **Precisión** | Depende del entrenamiento | Depende de las tolerancias configuradas |
| **Configuración** | Requiere modelo ONNX entrenado | Requiere configurar Hand Shapes manualmente |
| **Flexibilidad** | Aprende de datos, más flexible | Reglas fijas, menos flexible |
| **Rendimiento** | Más pesado (inferencia del modelo) | Más ligero (comparaciones simples) |
| **Mantenimiento** | Requiere reentrenar modelo | Ajustar tolerancias en Inspector |
| **Gestos dinámicos** | Puede detectar movimientos (J, Z) | Solo gestos estáticos |
| **Detección de orientación** | Incluida en el modelo | Requiere Hand Pose (no solo Shape) |

**Recomendación**: Empieza con Hand Shapes para gestos simples y estáticos. Si necesitas detección de movimiento o mayor flexibilidad, mantén el sistema ML en paralelo.

---

## Contacto y Soporte

Si tienes problemas:
1. Revisa los logs en Unity Console
2. Activa `Show Debug Logs` en `ASLGestureDetector`
3. Verifica que `XRHandTrackingEvents` esté correctamente asignado
