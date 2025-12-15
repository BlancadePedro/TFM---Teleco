# 🔧 Troubleshooting: Botones no funcionan en VR con Pinch

Esta guía te ayuda a solucionar el problema cuando los botones UI no responden al gesto pinch/poke en VR.

---

## ✅ SOLUCIÓN AUTOMÁTICA (Recomendada)

### Paso 1: Ejecutar el Fix Tool

1. En Unity, ve al menú: **`Tools > ASL > Fix XR Setup`**
2. Se abrirá una ventana con opciones
3. Asegúrate de que todas las opciones estén marcadas:
   - ✓ Fix: Eliminar manos duplicadas (XR Origin Hands prefab)
   - ✓ Fix: Activar TrackedDeviceGraphicRaycaster en Canvas
   - ✓ Fix: Añadir XR Interaction Manager
   - ✓ Fix: Añadir Hand Interaction prefabs
4. Click en **"Fix All Scenes"**
5. Espera a que termine y revisa el log

### Paso 2: Añadir XRSetupValidator a tus escenas

1. En las escenas `01_MainMenu`, `02_LevelSelection`, `03_LearningModule`, `04_SelfAssessmentMode`:
   - Crea un GameObject vacío llamado `XRSetupValidator`
   - Añade el componente `XRSetupValidator` (Scripts/Core/XRSetupValidator.cs)
   - Deja las opciones por defecto:
     - Validate On Start: ✓
     - Show Warnings In Console: ✓
     - Show Info In Console: ✓ (opcional, para más detalle)

2. Cuando ejecutes Play Mode, el validator mostrará en consola si hay problemas

### Paso 3: Verificar en Play Mode

1. Da Play en Unity
2. Revisa la consola - debería decir:
   ```
   ✓ Hand Visualizer OK: Solo 1 activo
   ✓ Canvas 'XXX' configurado correctamente para VR
   ✓ XR Interaction Manager encontrado
   ✓ Poke Interactors OK: 2 encontrados
   ✓ Handedness OK: Left=1, Right=1
   ```

3. Si ves advertencias (⚠️), sigue las soluciones que indica

---

## 🔍 DIAGNÓSTICO MANUAL (Si el automático no funciona)

### Problema 1: Los botones NO RESPONDEN al tocarlos

**Síntomas:**
- Veo las manos en VR
- Puedo moverlas
- Pero cuando toco un botón, no pasa nada

**Checklist:**

#### A) Verificar Canvas
1. Selecciona tu Canvas en la jerarquía
2. En el Inspector, verifica:
   - **Render Mode** = `World Space` ✓
   - **Event Camera** = `Main Camera` (la cámara del XR Origin)
   - **Graphic Raycaster** (si existe) = DESACTIVADO ✗
   - **Tracked Device Graphic Raycaster** = ACTIVADO ✓

**Cómo arreglarlo:**
- Si no tiene `Tracked Device Graphic Raycaster`:
  1. Click en "Add Component"
  2. Busca "Tracked Device Graphic Raycaster"
  3. Añádelo
  4. Desactiva el `Graphic Raycaster` normal (desmarca el checkbox)

#### B) Verificar XR Interaction Manager
1. En la jerarquía de tu escena, busca un GameObject con `XR Interaction Manager`
2. Si no existe:
   - Crea un GameObject vacío: `GameObject > Create Empty`
   - Nómbralo "XR Interaction Manager"
   - Añade el componente: `Add Component > XR Interaction Manager`

#### C) Verificar Poke Interactors
1. Busca en la jerarquía: `LeftHandInteraction` y `RightHandInteraction`
2. Si no existen:
   - Arrastra los prefabs desde `Assets/Prefabs/`:
     - `LeftHandInteraction.prefab`
     - `RightHandInteraction.prefab`
3. Selecciona cada uno y verifica en Inspector:
   - Componente `XR Hand Tracking Events`:
     - **Handedness**: Left (para LeftHand), Right (para RightHand) ✓
     - **Update Type**: Dynamic ✓
   - Componente `XR Poke Interactor`:
     - **Enabled**: ✓
     - **Handedness**: Left o Right (según corresponda)
     - **Enable UI Interaction**: ✓
     - **Click UI On Down**: ✓

#### D) Verificar Input Actions
1. Ve a `Edit > Project Settings > XR Plug-in Management > XR Interaction Toolkit`
2. Busca la sección "Input Actions"
3. Asegúrate de que esté asignado: `XRI Default Input Actions` o similar

---

### Problema 2: Los botones FUNCIONAN con ratón pero NO con manos

**Causa:** Estás usando `Graphic Raycaster` estándar en lugar de `Tracked Device Graphic Raycaster`.

**Solución:**
1. Selecciona tu Canvas World Space
2. Desactiva `Graphic Raycaster` (checkbox)
3. Activa `Tracked Device Graphic Raycaster` (checkbox)

---

### Problema 3: Los botones NO detectan HOVER (no cambian color)

**Causa:** El Poke Interactor no está configurado correctamente o el botón no tiene `Button` component.

**Solución:**

**Para el Poke Interactor:**
1. Selecciona `LeftHandInteraction` o `RightHandInteraction`
2. En `XR Poke Interactor`, verifica:
   - **Poke Hover Radius**: 0.015 (ajusta si es muy pequeño)
   - **Poke Depth**: 0.1
   - **Enable UI Interaction**: ✓

**Para el Botón:**
1. Selecciona tu botón en la jerarquía
2. Debe tener componente `Button` (o `Button - TextMeshPro`)
3. En `Button`, verifica:
   - **Interactable**: ✓
   - **Transition**: Color Tint (o el que uses)
   - Asegúrate de que los colores Normal/Highlighted/Pressed sean diferentes para ver el cambio

---

### Problema 4: "Poke Interactor not found" en consola

**Causa:** No hay Poke Interactors en la escena.

**Solución:**
1. Arrastra los prefabs:
   - `Assets/Prefabs/LeftHandInteraction.prefab`
   - `Assets/Prefabs/RightHandInteraction.prefab`
2. Ponlos en la raíz de la escena (no como hijos de otros objetos)

---

### Problema 5: "Multiple Hand Visualizers detected"

**Causa:** Tienes el prefab `XR Origin Hands (XR Rig)` Y el `Hand Visualizer` en la raíz.

**Solución:**
1. Busca en la jerarquía: `XR Origin Hands (XR Rig)`
2. Elimínalo (selecciona y Delete)
3. Mantén solo el `Hand Visualizer` en la raíz de la escena

---

## 🎯 VERIFICACIÓN FINAL

### Test en Unity Play Mode:

1. Da Play
2. Revisa la consola - debe decir:
   ```
   ✓ Hand Visualizer OK: Solo 1 activo
   ✓ Canvas configurado correctamente para VR
   ✓ XR Interaction Manager encontrado
   ✓ Poke Interactors OK: 2 encontrados
   ```

3. En Game View (simulado):
   - Los botones deberían cambiar de color al pasar el mouse (esto simula el hover)

### Test en VR Device:

1. Build & Run en tu visor VR
2. Verifica:
   - ✓ Veo UNA mano por cada mano (no duplicadas)
   - ✓ Las manos se mueven correctamente
   - ✓ Cuando acerco el dedo índice a un botón, cambia de color (hover)
   - ✓ Cuando toco el botón, hace click

---

## 📋 CHECKLIST COMPLETO

Marca cada item:

### Escena Setup:
- [ ] Solo 1 `Hand Visualizer` activo en la escena
- [ ] NO hay `XR Origin Hands (XR Rig)` en la jerarquía
- [ ] Hay un `XR Interaction Manager` en la escena
- [ ] Existen `LeftHandInteraction` y `RightHandInteraction` en la raíz

### Canvas Setup (para cada Canvas World Space):
- [ ] Render Mode = World Space
- [ ] Event Camera asignada (Main Camera del XR Origin)
- [ ] `Graphic Raycaster` DESACTIVADO (o sin ese componente)
- [ ] `Tracked Device Graphic Raycaster` ACTIVADO

### Poke Interactors:
- [ ] `LeftHandInteraction` tiene `XR Poke Interactor` con Handedness=Left
- [ ] `RightHandInteraction` tiene `XR Poke Interactor` con Handedness=Right
- [ ] Ambos tienen `Enable UI Interaction` activado
- [ ] Ambos tienen `XR Hand Tracking Events` con Update Type=Dynamic

### Botones:
- [ ] Cada botón tiene componente `Button`
- [ ] `Interactable` está activado
- [ ] Los colores de transición son visibles (diferentes entre estados)

---

## 🆘 ÚLTIMA OPCIÓN: Reset Completo

Si nada funciona:

1. **Backup tu proyecto**
2. **Elimina** de la escena:
   - Todo lo que tenga "XR Origin"
   - `LeftHandInteraction` / `RightHandInteraction`
   - `XR Interaction Manager`
3. **Ejecuta** `Tools > ASL > Fix XR Setup > Fix Current Scene`
4. **Añade manualmente** el `Hand Visualizer` en la raíz (si no está)
5. **Verifica** que el `Hand Visualizer` tenga:
   - `m_DrawMeshes` = true
   - Referencias a los meshes de manos (Meta Quest o Android XR)

---

## 📞 Ayuda Adicional

Si después de seguir todos estos pasos los botones siguen sin funcionar:

1. Ejecuta el validator: `XRSetupValidator > Validate XR Setup` (clic derecho en component)
2. Copia TODO el output de la consola
3. Toma screenshot del Inspector del Canvas con problemas
4. Toma screenshot del Inspector de `LeftHandInteraction`

---

**Fecha de creación:** 2025-12-15
**Versión Unity:** 2022.3+
**XR Interaction Toolkit:** 3.2.2+
**XR Hands:** 1.7.2+
