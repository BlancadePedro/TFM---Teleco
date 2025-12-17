# 🎮 Instrucciones de Configuración en Unity
## Pasos que DEBES hacer manualmente en el Editor de Unity

---

## 📋 Índice

1. [Parte 1: Panel de Categorías Mejorado (Escena 02_LevelSelection)](#parte-1-panel-de-categorías-mejorado)
2. [Parte 2: Feedback Visual en Tiles (Escena 04_SelfAssessmentMode)](#parte-2-feedback-visual-en-tiles)
3. [Parte 3: Prefab de Botón de Categoría](#parte-3-prefab-de-botón-de-categoría)
4. [Verificación Final](#verificación-final)

---

## Parte 1: Panel de Categorías Mejorado

### 🎯 Objetivo
Añadir un texto de encabezado y contador de signos en los botones de categoría.

### Escena: `02_LevelSelection.unity`

### ✅ Paso 1: Verificar que el código se compiló correctamente

1. Abre Unity
2. Espera a que **compile el código** modificado
3. Revisa la **Console** y asegúrate de que **NO hay errores**
4. Si hay errores, cópialos y pégamelos para solucionarlos

### ⚠️ IMPORTANTE: No necesitas hacer nada más para esta parte

El código creará automáticamente:
- ✅ El encabezado "Has seleccionado: BÁSICO"
- ✅ El texto "Elige una categoría para comenzar:"
- ✅ El contador de signos en cada botón

**PERO**, necesitas modificar el **prefab de botón de categoría** para que muestre correctamente el contador de signos.

---

## Parte 2: Feedback Visual en Tiles

### 🎯 Objetivo
Configurar los tiles de autoevaluación para que muestren feedback visual cuando reconozcan un gesto.

### Escena: `04_SelfAssessmentMode.unity`

### ✅ Paso 1: Abrir la Escena

1. En Unity, navega a: `Assets/Scenes/04_SelfAssessmentMode.unity`
2. Haz **doble click** para abrir la escena

### ✅ Paso 2: Localizar el SignTilePrefab

1. En la ventana **Project**, navega a donde esté el prefab del tile
   - Probablemente en: `Assets/Prefabs/SignTile.prefab` o similar
   - Si no sabes dónde está, búscalo: Click en la barra de búsqueda del Project y escribe: `SignTile`

2. Haz **doble click** en el prefab para abrirlo en modo de edición

### ✅ Paso 3: Verificar los Componentes del Prefab

El prefab debe tener un **SignTileController** component. Verifica que tenga estos campos:

#### **Inspector > SignTileController > Visual Settings:**

```
Default Color:        R: 0.2, G: 0.2, B: 0.2, A: 1
Completed Color:      R: 0,   G: 0.627, B: 1, A: 1
Recognized Color:     R: 1,   G: 0.843, B: 0, A: 1  ← NUEVO
Color Transition:     0.3
Pulse Effect:         0.2                           ← NUEVO
Pulse Scale:          1.15                          ← NUEVO
```

#### **Si NO ves los campos nuevos (Recognized Color, Pulse Effect, Pulse Scale):**

1. Cierra Unity completamente
2. Vuelve a abrirlo
3. Espera a que recompile
4. Vuelve a abrir el prefab

### ✅ Paso 4: Configurar los Colores (Opcional)

Los valores por defecto ya están bien, pero si quieres personalizarlos:

- **Recognized Color** (Color dorado cuando detecta el gesto):
  - Actualmente: `RGB(255, 215, 0)` - Dorado
  - Alternativas:
    - Verde claro: `RGB(100, 255, 100)`
    - Naranja: `RGB(255, 165, 0)`
    - Amarillo: `RGB(255, 255, 0)`

- **Pulse Scale** (Cuánto crece el tile al pulsar):
  - `1.0` = Sin crecimiento
  - `1.15` = Crece 15% (recomendado)
  - `1.3` = Crece 30% (muy notorio)

### ✅ Paso 5: Guardar el Prefab

1. Click en **File > Save** (Ctrl+S)
2. Cierra el modo de edición del prefab

---

## Parte 3: Prefab de Botón de Categoría

### 🎯 Objetivo
Añadir un segundo TextMeshProUGUI para mostrar el contador de signos.

### ⚠️ ESTE ES EL PASO MÁS IMPORTANTE QUE DEBES HACER

### ✅ Paso 1: Localizar el Category Button Prefab

1. En la ventana **Project**, busca el prefab del botón de categoría
   - Probablemente: `Assets/Prefabs/CategoryButton.prefab`
   - Si no lo encuentras, en el **Inspector** de la escena `02_LevelSelection`, busca el objeto `LevelSelectionController` y mira el campo **Category Button Prefab**

2. Haz **doble click** en el prefab para editarlo

### ✅ Paso 2: Estructura Actual del Prefab

Probablemente tenga esta estructura:

```
CategoryButton (GameObject)
├─ Background (Image)
└─ Text (TextMeshProUGUI)  ← Nombre de la categoría
```

### ✅ Paso 3: Añadir el Texto del Contador

1. **Click derecho** en `CategoryButton` (el objeto raíz)
2. Click en **UI > Text - TextMeshPro**
3. Renombra el nuevo objeto a: `SignCountText`

### ✅ Paso 4: Configurar SignCountText

Selecciona `SignCountText` y en el **Inspector**:

#### **RectTransform:**
```
Anchor Preset: Bottom-Left (mantén Alt presionado y click en el preset)
Pos X: 10
Pos Y: 10
Width: 200
Height: 20
```

#### **TextMeshProUGUI:**
```
Text: "26 signos"  (solo para preview)
Font Size: 14
Alignment: Left y Bottom
Color: Blanco (o gris claro si prefieres)
```

### ✅ Paso 5: Ajustar el Texto Principal (Nombre de Categoría)

Selecciona `Text` (el texto original del nombre) y ajusta:

#### **RectTransform:**
```
Anchor Preset: Top-Center
Pos Y: -10  (un poco separado del borde superior)
```

#### **TextMeshProUGUI:**
```
Font Size: 18-20 (un poco más grande)
Alignment: Center y Top
Font Style: Bold
```

### ✅ Paso 6: Resultado Esperado

Tu jerarquía debería verse así:

```
CategoryButton (GameObject)
├─ Background (Image)
├─ Text (TextMeshProUGUI)           ← Nombre: "Alfabeto"
└─ SignCountText (TextMeshProUGUI)  ← Contador: "26 signos"
```

### ✅ Paso 7: Guardar el Prefab

1. Click en **File > Save** (Ctrl+S)
2. Cierra el modo de edición del prefab

---

## Verificación Final

### ✅ Checklist Completo

Marca cada item cuando lo hayas completado:

#### **Código:**
- [ ] Unity compiló sin errores
- [ ] No hay warnings críticos en la Console

#### **Prefab de SignTile:**
- [ ] SignTileController tiene los campos nuevos (Recognized Color, Pulse Effect, Pulse Scale)
- [ ] Los colores están configurados correctamente
- [ ] El prefab está guardado

#### **Prefab de CategoryButton:**
- [ ] Tiene dos TextMeshProUGUI: `Text` y `SignCountText`
- [ ] Los textos están posicionados correctamente
- [ ] El prefab está guardado

#### **Escena 02_LevelSelection:**
- [ ] El LevelSelectionController tiene asignado el Category Button Prefab correcto

#### **Escena 04_SelfAssessmentMode:**
- [ ] El SelfAssessmentController tiene asignado el Sign Tile Prefab correcto
- [ ] El MultiGestureRecognizer está presente en la escena

---

## 🧪 Pruebas

### Probar Panel de Categorías (Escena 02):

1. Dale a **Play** en Unity
2. Haz click en el panel **BÁSICO**
3. **Verifica que aparece**:
   - ✅ Texto: "Has seleccionado: BÁSICO"
   - ✅ Texto: "Elige una categoría para comenzar:"
   - ✅ Tres botones con:
     - Nombre de categoría (arriba)
     - Contador de signos (abajo): ej. "26 signos"

### Probar Feedback Visual en Tiles (Escena 04):

1. En Unity, abre la escena `04_SelfAssessmentMode`
2. Dale a **Play**
3. **Haz un signo con tu mano** (por ejemplo, letra A)
4. **Verifica que**:
   - ✅ El tile correspondiente **destella en dorado** inmediatamente
   - ✅ El tile **pulsa** (crece y se encoge)
   - ✅ Si mantienes el gesto por 0.5 segundos, el tile se queda **azul permanentemente**
   - ✅ Si quitas la mano antes de 0.5s, el tile vuelve a **gris oscuro**

---

## 🚨 Problemas Comunes

### Problema 1: "No veo los campos nuevos en SignTileController"

**Solución:**
1. Cierra Unity completamente
2. Abre Unity de nuevo
3. Espera a que recompile todo
4. Abre el prefab de nuevo

### Problema 2: "El contador de signos no aparece"

**Solución:**
1. Verifica que el prefab de CategoryButton tiene **DOS** TextMeshProUGUI
2. Verifica que el segundo se llama `SignCountText`
3. El código busca los componentes en orden:
   - `textComponents[0]` = Nombre de categoría
   - `textComponents[1]` = Contador de signos

### Problema 3: "Los tiles no muestran feedback visual"

**Posibles causas:**
1. El `MultiGestureRecognizer` no está asignado en el Inspector
2. Los eventos `onGestureRecognized` y `onGestureLost` no se crearon correctamente

**Solución:**
1. Selecciona el objeto con `SelfAssessmentController`
2. En el Inspector, verifica que el campo **Multi Gesture Recognizer** está asignado
3. Si no lo está, arrastra el GameObject que tenga el componente `MultiGestureRecognizer`

### Problema 4: "Errores de compilación"

**Solución:**
1. Copia el error completo de la Console
2. Pégamelo y te diré cómo solucionarlo

---

## 📸 Capturas de Referencia

### Estructura del CategoryButton Prefab:

```
CategoryButton
├─ Background (Image)
│  └─ Color: Gris oscuro
├─ Text (TextMeshProUGUI)
│  └─ "Alfabeto" (arriba, centrado)
└─ SignCountText (TextMeshProUGUI)
   └─ "26 signos" (abajo, izquierda)
```

### Jerarquía en 04_SelfAssessmentMode:

```
SelfAssessmentController (GameObject)
├─ CategoryTitleText
├─ ProgressText
├─ GridContainer
│  └─ SignTile (Clone) x N
├─ BackButton
└─ MultiGestureRecognizer ← IMPORTANTE
```

---

## ✅ Resumen Super Estricto

### LO QUE **SÍ** DEBES HACER:

1. ✅ **Modificar el prefab CategoryButton**:
   - Añadir un segundo TextMeshProUGUI llamado `SignCountText`
   - Posicionarlo abajo del nombre de la categoría
   - Guardarlo

2. ✅ **Verificar el prefab SignTile**:
   - Comprobar que tiene los campos nuevos
   - Ajustar colores si quieres personalizarlos

3. ✅ **Asignar referencias en el Inspector**:
   - En `02_LevelSelection`: Asegurar que `Category Button Prefab` está asignado
   - En `04_SelfAssessmentMode`: Asegurar que `Multi Gesture Recognizer` está asignado

### LO QUE **NO** DEBES HACER:

- ❌ NO modificar scripts (ya lo hice yo)
- ❌ NO cambiar nombres de métodos o variables
- ❌ NO crear nuevas escenas
- ❌ NO borrar GameObjects existentes

---

## 🆘 Si Algo Sale Mal

**Copia y pégame:**
1. El error exacto de la Console
2. Qué paso estabas haciendo
3. Una captura de pantalla del Inspector si es posible

**Estaré aquí para ayudarte a solucionarlo inmediatamente.**

---

**Generado el:** 2025-12-16
**Versión del código:** 1.0
**Estado:** Listo para implementar
