# 🔧 GUÍA RÁPIDA DE CORRECCIÓN - REFERENCIAS FALTANTES

## ❌ PROBLEMA IDENTIFICADO

El código está correcto, pero las **referencias en Unity Editor** no están conectadas porque modificamos los scripts pero la escena tiene las referencias antiguas.

---

## ✅ SOLUCIÓN RÁPIDA

### 1️⃣ ESCENA 01_MainMenu - Popup de Traducción

**Problema:** El popup apunta al texto en lugar del panel completo.

**Solución:**
1. Abrir `01_MainMenu.unity`
2. Seleccionar el GameObject `MenuController`
3. En el Inspector, buscar el campo **Translation Popup**
4. Actualmente apunta a `PopupText` ❌
5. **Cambiarlo** para que apunte a `Panel Translation Popup` ✅
   - Arrastrar desde la jerarquía: `UI Canvas > Panel Principal > Panel Translation Popup`

**Verificación:**
- Pulsar Play
- Click en "Start Translating"
- Debe aparecer un panel grande con el mensaje

---

### 2️⃣ ESCENA 02_LevelSelection - Paneles Visuales

**Problema:** El controlador tiene referencias antiguas que ya no existen en el nuevo código.

**Solución:**
1. Abrir `02_LevelSelection.unity`
2. Seleccionar el GameObject `LevelSelectionController`
3. En el Inspector verás varios campos:
   - ~~levelButtonsContainer~~ (antiguo - ignorar)
   - ~~levelButtonPrefab~~ (antiguo - ignorar)

4. **Buscar los nuevos campos** (si no aparecen, Unity necesita recompilar):
   - **Basic Panel**: Arrastrar `Panel Basic` desde la jerarquía
   - **Intermediate Panel**: Arrastrar `Panel Intermediate` desde la jerarquía
   - **Advanced Panel**: Arrastrar `Panel Advanced` desde la jerarquía
   - **Category Button Prefab**: Mantener el que ya tiene o asignar uno

5. Si NO ves los nuevos campos, significa que Unity no recompiló:
   - Menú: `Assets > Reimport All`
   - O cierra y abre Unity

**Verificación:**
- Pulsar Play
- Click en "Start Learning" desde el Main Menu
- Debes ver los 3 paneles: Basic, Intermediate, Advanced
- Click en "Basic"
- Debe aparecer "Alphabet" (o lo que tengas configurado)

---

## 🔍 CÓMO ENCONTRAR LOS PANELES EN LA JERARQUÍA

### Escena 02_LevelSelection:

```
02_LevelSelection (Scene Root)
└── UI Canvas
    └── Panel Principal (o similar)
        ├── Panel Basic ← ESTE
        ├── Panel Intermediate ← ESTE
        └── Panel Advanced ← ESTE
```

Si no encuentras estos paneles con esos nombres exactos:
- Busca en la jerarquía objetos que contengan "Basic", "Intermediate", "Advanced"
- O cualquier panel visual que represente niveles

---

## 📝 MÉTODO ALTERNATIVO: Editar el archivo .unity directamente

**⚠️ SOLO SI SABES LO QUE HACES - Haz backup primero**

### Para 01_MainMenu.unity:

Buscar en el archivo:
```yaml
translationPopup: {fileID: 1156573504}
```

Cambiar a:
```yaml
translationPopup: {fileID: 1862079399}
```

### Para 02_LevelSelection.unity:

Buscar en el archivo (línea ~699-710):
```yaml
m_EditorClassIdentifier: Assembly-CSharp::ASL_LearnVR.LevelSelection.LevelSelectionController
levels:
  - {fileID: 11400000, guid: 149b0e97135b8ce4daa2eb735b29a42d, type: 2}
  - {fileID: 11400000, guid: af72f657fa9c9434b872340270692aae, type: 2}
  - {fileID: 11400000, guid: afefba9db3cdbb84d8a01bbc619fdcc4, type: 2}
levelButtonsContainer: {fileID: 2139636408}
levelButtonPrefab: {fileID: 8392702887293584617, guid: 9b4b0b4b0b4b0b4b0b4b0b4b0b4b0b4b, type: 3}
```

Necesitas encontrar los FileIDs de:
- Panel Basic
- Panel Intermediate
- Panel Advanced

Busca en el archivo esos nombres y anota sus FileIDs, luego añade:
```yaml
basicPanel: {fileID: XXXXX}
intermediatePanel: {fileID: XXXXX}
advancedPanel: {fileID: XXXXX}
```

---

## 🎯 VERIFICACIÓN FINAL

### Test Escena 01:
1. Play → Click "Start Translating"
2. ✅ Aparece panel grande centrado
3. ✅ Texto visible
4. ✅ Botón "Cerrar" funciona

### Test Escena 02:
1. Desde Main Menu → "Start Learning"
2. ✅ Se ven 3 paneles visuales (Basic, Intermediate, Advanced)
3. ✅ Click en "Basic" → Aparece "Alphabet"
4. ✅ Click en "Alphabet" → Carga escena 03

---

## ❓ Si aún no funciona

**Para debugging:**

1. Abrir Console en Unity (Ctrl+Shift+C)
2. Play la escena
3. Buscar warnings/errors que digan:
   - `LevelSelectionController: No se encontró panel para el nivel 'Basic'`
   - Esto significa que `basicPanel` no está asignado

4. Si ves:
   - `LevelSelectionController: No se encontró botón en el panel 'Basic'`
   - Significa que el panel no tiene un componente `Button`
   - Solución: Añadir un `Button` component al GameObject del panel

---

## 📞 NECESITAS MÁS AYUDA?

Comparte:
1. Screenshot del Inspector del `MenuController` (Escena 01)
2. Screenshot del Inspector del `LevelSelectionController` (Escena 02)
3. Screenshot de la jerarquía de la escena 02
4. Cualquier error/warning en la Console

Y te ayudo a identificar exactamente qué falta.
