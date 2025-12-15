# GUÍA ULTRA-SIMPLE: Configurar Gesture Recognition

## ESCENA 03 - LEARNING MODULE

### PASO 1: Crear los GameObjects Recognizers

1. En la jerarquía, **clic derecho** → Create Empty
2. Nombrar: `LeftHandRecognizer`
3. **Clic derecho** → Create Empty
4. Nombrar: `RightHandRecognizer`

### PASO 2: Añadir el componente GestureRecognizer

**Para CADA recognizer (Left y Right):**

1. Seleccionar el GameObject (`LeftHandRecognizer` o `RightHandRecognizer`)
2. En el Inspector, **Add Component** → buscar `GestureRecognizer`
3. En el componente GestureRecognizer:
   - **Target Sign**: DÉJALO VACÍO (se asigna automáticamente desde código)
   - **Hand Tracking Events**:
     - Para LeftHandRecognizer: Arrastra `XR Origin Hands > LeftHand Controller`
     - Para RightHandRecognizer: Arrastra `XR Origin Hands > RightHand Controller`
   - **Detection Interval**: 0.1
   - **Use Sign Data Hold Time**: ✅ ACTIVADO
   - **Show Debug Logs**: ✅ ACTIVADO (para ver si funciona)

### PASO 3: Conectar con el LearningController

1. Seleccionar el GameObject `LearningController` (o el que tenga el script LearningController)
2. En el Inspector, buscar los campos:
   - **Right Hand Recognizer**: Arrastra `RightHandRecognizer`
   - **Left Hand Recognizer**: Arrastra `LeftHandRecognizer`

### PASO 4: Verificar XRHandTrackingEvents

**IMPORTANTE**: Los GameObjects de las manos DEBEN tener el componente `XRHandTrackingEvents`.

1. Busca en la jerarquía: `XR Origin Hands > LeftHand Controller`
2. En el Inspector, verifica que tiene `XRHandTrackingEvents`
3. Si NO lo tiene, **Add Component** → buscar `XRHandTrackingEvents`
4. Repetir para `RightHand Controller`

---

## ESCENA 04 - SELF-ASSESSMENT MODE

### PASO 1: Crear el GameObject

1. En la jerarquía, **clic derecho** → Create Empty
2. Nombrar: `MultiGestureRecognizer`

### PASO 2: Añadir el componente

1. Seleccionar `MultiGestureRecognizer`
2. **Add Component** → buscar `MultiGestureRecognizer`
3. En el Inspector:
   - **Target Signs**: DÉJALO VACÍO (se asigna desde código)
   - **Left Hand Tracking Events**: Arrastra `XR Origin Hands > LeftHand Controller`
   - **Right Hand Tracking Events**: Arrastra `XR Origin Hands > RightHand Controller`
   - **Detection Interval**: 0.1
   - **Minimum Hold Time**: 0.5
   - **Show Debug Logs**: ✅ ACTIVADO

### PASO 3: Conectar con SelfAssessmentController

1. Seleccionar el GameObject `SelfAssessmentController`
2. En el Inspector:
   - **Multi Gesture Recognizer**: Arrastra `MultiGestureRecognizer`

---

## VERIFICACIÓN - ¿FUNCIONA?

### Escena 03 - Learning Module

1. Ejecutar la escena
2. Pulsar botón "Practice"
3. **En la Console, deberías ver cada 2 segundos:**
   ```
   GestureRecognizer [A]: Tracked=True, Shape=True/False, Pose=True/False, Detected=True/False
   ```

4. Haz el gesto con la mano
5. **Deberías ver:**
   ```
   GestureRecognizer: Gesto 'A' detectado, esperando hold time.
   GestureRecognizer: Gesto 'A' confirmado!
   ```

6. **En la UI debería aparecer:** "✓ Correct! Sign 'A' detected."

### Escena 04 - Self-Assessment

1. Ejecutar la escena
2. **En la Console, deberías ver cada 2 segundos:**
   ```
   MultiGestureRecognizer: Checking gestures... (total: 26)
   ```

3. Haz un gesto (ej: "A")
4. **Deberías ver:**
   ```
   MultiGestureRecognizer: Gesto 'A' detectado, esperando hold time.
   MultiGestureRecognizer: Gesto 'A' confirmado!
   ```

5. **La casilla "A" debería iluminarse/marcarse**

---

## SI NO FUNCIONA - TROUBLESHOOTING

### NO HAY LOGS EN LA CONSOLE

**Problema**: `XRHandTrackingEvents` no está configurado

**Solución**:
1. Verifica que `LeftHand Controller` y `RightHand Controller` tienen el componente `XRHandTrackingEvents`
2. Verifica que los recognizers tienen la referencia conectada

### LOGS DICEN "Tracked=False"

**Problema**: No se detecta hand tracking

**Solución**:
- Ejecuta en Meta Quest, NO en el Editor
- Activa hand tracking en el Quest
- Asegúrate de que tus manos están visibles para las cámaras

### LOGS DICEN "Shape=False, Pose=False"

**Problema**: El SignData no tiene Hand Shape/Pose configurado

**Solución**:
1. Abre el ScriptableObject del signo (ej: `Sign_A`)
2. Verifica que el campo `handShapeOrPose` tiene un valor asignado
3. Si está vacío, necesitas configurar el Hand Shape/Pose desde XR Hands

### GHOST HANDS SIGUEN MIS MANOS

**Problema**: El código no está desparentando las ghost hands

**Solución**:
1. Verifica que `GhostHandPlayer` tiene las referencias a `leftGhostHand` y `rightGhostHand`
2. Activa `showDebugLogs` en `GhostHandPlayer`
3. Ejecuta y verifica en Console que dice:
   ```
   GhostHandPlayer: LeftGhostHand desparentada del XR Origin.
   GhostHandPlayer: RightGhostHand desparentada del XR Origin.
   ```

### GHOST HANDS NO APARECEN

**Problema**: Están ocultas o mal posicionadas

**Solución**:
1. Pulsa el botón "Repetir" en la escena 03
2. Verifica en la Console:
   ```
   GhostHandPlayer: Mostrando gesto estático 'A'
   ```
3. Ajusta `ghostHandsPosition` en el Inspector de `GhostHandPlayer`:
   - Ejemplo: `(0, 1.5, 1.0)` para ponerlas más adelante y a la altura de los ojos
4. Ajusta `ghostHandsRotation`:
   - Ejemplo: `(0, 180, 0)` para que miren hacia ti

---

## RESUMEN - LO QUE NECESITAS

### Escena 03:
- ✅ 2 GameObjects vacíos: `LeftHandRecognizer`, `RightHandRecognizer`
- ✅ Cada uno con componente `GestureRecognizer`
- ✅ Conectados a los `XRHandTrackingEvents` correspondientes
- ✅ Referencias asignadas en `LearningController`

### Escena 04:
- ✅ 1 GameObject vacío: `MultiGestureRecognizer`
- ✅ Con componente `MultiGestureRecognizer`
- ✅ Conectado a ambos `XRHandTrackingEvents`
- ✅ Referencia asignada en `SelfAssessmentController`

**NADA MÁS.** No necesitas configurar SignData, no necesitas crear prefabs, NADA. Solo estos pasos.
