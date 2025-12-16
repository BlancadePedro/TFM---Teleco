# GUÍA: Grabar Gestos Dinámicos (J, Z, Colores)

Unity XR Hands ya incluye un sistema completo de grabación de hand poses en los samples. **NO necesitas crear nada custom.**

## UBICACIÓN DEL SISTEMA OFICIAL

El sistema de grabación está en:
```
Assets/Samples/XR Hands/1.7.2/Hand Capture/
```

### Archivos Clave:
- **Scripts/RecordingController.cs**: Controlador principal de grabación
- **Scripts/CaptureSessionManager.cs**: Manager de sesiones de grabación con UI
- Usa el namespace: `UnityEngine.XR.Hands.Capture.Recording`

## CÓMO GRABAR GESTOS DINÁMICOS

### PASO 1: Abrir la Escena de Grabación

1. En Unity, navega a: `Assets/Samples/XR Hands/1.7.2/Hand Capture/`
2. Busca la escena de ejemplo (HandCapture scene)
3. Ábrela

### PASO 2: Configurar para Quest

1. Build Settings → Android
2. Asegúrate de que tienes las configuraciones XR correctas
3. Build and Run en tu Meta Quest

### PASO 3: Grabar en VR

**En el Quest:**
1. Ejecuta la app
2. Verás una UI con instrucciones
3. Haz click en "Start Recording"
4. Realiza el gesto dinámico (ej: J, Z, o movimiento de color)
5. Haz click en "Stop Recording"
6. Dale un nombre (ej: "Sign_J", "Sign_Z", "Color_Red")
7. Haz click en "Save"

**Puedes grabar hasta 5 grabaciones** en una sesión.

### PASO 4: Exportar las Grabaciones

**Después de grabar en el Quest:**

1. Conecta el Quest al PC
2. En Unity, ve a: **Window → XR → XR Hand Capture**
3. Verás una ventana que te permite importar las grabaciones del Quest
4. Haz click en "Import Recordings"
5. Selecciona las grabaciones que hiciste
6. Unity creará automáticamente **XRHandShape assets** o **XRHandPose assets**

### PASO 5: Usar en tu SignData

1. Ve a tu SignData ScriptableObject (ej: `Sign_J`)
2. En el campo `handShapeOrPose`:
   - Arrastra el XRHandShape o XRHandPose que se creó automáticamente
3. Marca `isDynamic = true` si es un gesto con movimiento
4. ¡Listo!

## FLUJO COMPLETO

```
1. Quest: Graba gesto → Guarda como "Sign_J"
2. Unity PC: Window → XR → XR Hand Capture → Import
3. Unity PC: Se crea automáticamente Sign_J.asset (XRHandShape)
4. Unity PC: Asigna Sign_J.asset al SignData correspondiente
5. GhostHandPlayer y GestureRecognizer lo usarán automáticamente
```

## VENTAJAS DEL SISTEMA OFICIAL

✅ **Ya está probado y optimizado** por Unity
✅ **Guarda en formato binario eficiente** (no JSON custom)
✅ **Integración directa** con XRHandSubsystem
✅ **UI completa** para gestionar grabaciones
✅ **Importación automática** desde Quest
✅ **Crea assets listos para usar** en tu proyecto

## GESTOS QUE NECESITAS GRABAR

### Letras con Movimiento:
- **J**: Movimiento de "J" con la mano
- **Z**: Movimiento de "Z" con el dedo índice

### Colores (si requieren movimiento):
- Graba cada color según el gesto ASL correspondiente
- Ejemplo: "Red" → Sign_Color_Red

## TROUBLESHOOTING

### "No veo la escena HandCapture"

**Solución**: Asegúrate de haber importado el sample "Hand Capture" desde Package Manager:
1. Window → Package Manager
2. Busca "XR Hands"
3. Pestaña "Samples"
4. Import "Hand Capture"

### "No puedo importar grabaciones"

**Solución**:
1. Conecta el Quest al PC
2. Autoriza la conexión USB
3. Ve a Window → XR → XR Hand Capture
4. Si no aparece nada, verifica que las grabaciones se guardaron correctamente en el Quest

### "Las grabaciones están vacías"

**Problema**: No se detectó hand tracking durante la grabación

**Solución**:
- Asegúrate de que hand tracking esté activado en el Quest
- Verifica que tus manos estén bien iluminadas
- Mantén las manos visibles para las cámaras

## INTEGRACIÓN CON TU PROYECTO

### GhostHandPlayer

El `GhostHandPlayer` ya está preparado para usar XRHandShape/XRHandPose:
- Si el `SignData.handShapeOrPose` tiene un valor, lo usará automáticamente
- No necesitas modificar nada en el código

### GestureRecognizer

El `GestureRecognizer` también está listo:
- Usa `handShape.CheckConditions()` para detectar el gesto
- Funciona tanto con gestos estáticos como dinámicos

### Diferencia entre Estático y Dinámico

- **Estático** (A, B, C, etc.): Hand Shape fijo, sin movimiento
- **Dinámico** (J, Z, colores con movimiento): Secuencia de Hand Shapes en el tiempo

El sistema oficial de Unity maneja **ambos** automáticamente.

## PRÓXIMOS PASOS

1. **Abre la escena Hand Capture** en Unity
2. **Build and Run en Quest**
3. **Graba los gestos J y Z**
4. **Importa las grabaciones** en Unity
5. **Asígnalas a tus SignData**
6. **Prueba en tu escena 03** (Learning Module)

¡El sistema oficial de Unity hace todo el trabajo pesado por ti!
