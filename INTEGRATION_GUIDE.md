# 📘 Guía de Integración - ASL Learn VR Platform

Esta guía te ayudará a configurar correctamente todos los módulos creados en Unity.

---

## 📂 ESTRUCTURA DE ARCHIVOS CREADA

```
Assets/Scripts/
├── Core/
│   ├── GameManager.cs              ✅ Singleton para estado global
│   ├── SceneLoader.cs              ✅ Gestión de transiciones
│   └── HandTrackingStatus.cs       ✅ Monitoreo de tracking
│
├── Data/
│   ├── SignData.cs                 ✅ ScriptableObject para signos
│   ├── CategoryData.cs             ✅ ScriptableObject para categorías
│   └── LevelData.cs                ✅ ScriptableObject para niveles
│
├── Gestures/
│   ├── GestureRecognizer.cs        ✅ Reconocimiento de gestos (NUEVO)
│   ├── StaticHandGesture.cs        ✅ (Ya existente - del sample)
│   └── HandShapeCompletenessCalculator.cs ✅ (Ya existente)
│
├── MainMenu/
│   └── MenuController.cs           ✅ Controlador menú principal
│
├── LevelSelection/
│   └── LevelSelectionController.cs ✅ Selección nivel/categoría
│
├── LearningModule/
│   ├── LearningController.cs       ✅ Módulo aprendizaje
│   └── GhostHandPlayer.cs          ✅ Ghost hands
│
├── SelfAssessment/
│   ├── SelfAssessmentController.cs ✅ Autoevaluación
│   └── SignTileController.cs       ✅ Casilla individual
│
└── Hand Visualizer/ (ya existente)
    └── Hand capture/ (ya existente)
```

---

## 🎯 PASO 1: CREAR SCRIPTABLE OBJECTS

### 1.1 Crear SignData para cada letra del alfabeto

1. En Unity, haz clic derecho en `Assets/Data/` (crea esta carpeta si no existe)
2. Selecciona `Create > ASL Learn VR > Sign Data`
3. Nómbralo según el patrón: `Sign_A`, `Sign_B`, etc.

**Configuración de ejemplo para "Sign_A":**

```
Sign Name: A
Description: Closed fist with thumb on the side
Hand Shape Or Pose: [Arrastra ASL_Letter_A_Shape.asset]
Requires Movement: ☐ (false)
Minimum Hold Time: 0.3
Icon: [Opcional - imagen del signo]
Ghost Hands Prefab: [Dejar vacío por ahora]
Hand Recording Data: [Dejar vacío]
```

**Repite esto para las 26 letras del alfabeto.**

---

### 1.2 Crear CategoryData para "Alphabet"

1. Clic derecho en `Assets/Data/`
2. `Create > ASL Learn VR > Category Data`
3. Nómbralo: `Category_Alphabet`

**Configuración:**

```
Category Name: Alphabet
Description: Learn the ASL alphabet from A to Z
Signs: [Arrastra los 26 SignData creados (Sign_A, Sign_B, ...)]
Icon: [Opcional]
Theme Color: [Elige un color]
```

---

### 1.3 Crear LevelData para "Basic"

1. Clic derecho en `Assets/Data/`
2. `Create > ASL Learn VR > Level Data`
3. Nómbralo: `Level_Basic`

**Configuración:**

```
Level Name: Basic
Description: Start learning ASL with fundamental signs
Categories: [Arrastra Category_Alphabet]
Icon: [Opcional]
Theme Color: [Elige un color]
Minimum Hold Time: 0.3
Recognition Accuracy: 0.8
```

**Nota:** Cuando crees `Category_Digits` y `Category_Colors`, también los añadirás aquí.

---

## 🎬 PASO 2: CONFIGURAR ESCENAS

### 2.1 Renombrar escenas existentes

1. En `Assets/`, renombra tus escenas:
   - `LearningAppVR.unity` → `01_MainMenu.unity`
   - Crea `02_LevelSelection.unity` (duplica 01_MainMenu)
   - Crea `03_LearningModule.unity` (duplica 01_MainMenu)
   - Crea `04_SelfAssessmentMode.unity` (duplica 01_MainMenu)

2. Añade todas las escenas a **Build Settings** (`File > Build Settings`):
   - 01_MainMenu
   - 02_LevelSelection
   - 03_LearningModule
   - 04_SelfAssessmentMode

---

### 2.2 Configurar 01_MainMenu.unity

**Jerarquía recomendada:**

```
01_MainMenu
├── XR Origin (de XR Interaction Toolkit)
│   ├── Camera Offset
│   │   ├── Main Camera
│   │   ├── LeftHand Controller
│   │   └── RightHand Controller
│
├── GameManager (GameObject vacío)
│   └── GameManager.cs
│
├── SceneLoader (GameObject vacío)
│   └── SceneLoader.cs
│
├── HandTrackingStatus (GameObject vacío)
│   └── HandTrackingStatus.cs
│
└── UI_Frame (tu frame curvo del XR Toolkit)
    ├── Panel_Left
    │   ├── HandStatusText (TextMeshProUGUI)
    │   └── ExitButton (Button)
    │
    ├── Panel_Front
    │   ├── LearningModuleButton (Button)
    │   ├── TranslationModuleButton (Button)
    │   └── TranslationPopup (Panel - oculto por defecto)
    │       ├── PopupText
    │       └── CloseButton
    │
    └── Panel_Right
        └── InstructionsText
```

**Componentes a configurar:**

**GameManager:**
- No necesita configuración extra, se auto-inicializa

**SceneLoader:**
- `Main Menu Scene Name`: "01_MainMenu"
- `Level Selection Scene Name`: "02_LevelSelection"
- `Learning Module Scene Name`: "03_LearningModule"
- `Self Assessment Scene Name`: "04_SelfAssessmentMode"

**MenuController** (añadir al UI_Frame):
- `Hand Status Text`: Arrastra HandStatusText
- `Exit Button`: Arrastra ExitButton
- `Learning Module Button`: Arrastra LearningModuleButton
- `Translation Module Button`: Arrastra TranslationModuleButton
- `Hand Tracking Status`: Arrastra el GameObject HandTrackingStatus
- `Translation Popup`: Arrastra el panel TranslationPopup
- `Close Popup Button`: Arrastra CloseButton

---

### 2.3 Configurar 02_LevelSelection.unity

**Jerarquía:**

```
02_LevelSelection
├── XR Origin
├── GameManager
├── SceneLoader
│
└── UI_Frame
    ├── Title (TextMeshProUGUI)
    ├── IntroText (TextMeshProUGUI)
    │
    ├── Wait  (GameObject - contenedor)
    │   └── (Los botones se generarán dinámicamente)
    │
    ├── CategoryPanel (GameObject - oculto al inicio)
    │   ├── SelectedLevelText (TextMeshProUGUI)
    │   └── CategoryButtons (GameObject - contenedor)
    │
    └── BackButton (Button)
```

**Crear prefab LevelButton:**

1. Crea un botón en la escena: `GameObject > UI > Button - TextMeshPro`
2. Ajusta su tamaño y apariencia
3. Guárdalo como prefab: `Assets/Prefabs/LevelButton.prefab`
4. Elimina el botón de la escena

**Crear prefab CategoryButton:**

1. Similar al LevelButton
2. Guárdalo como: `Assets/Prefabs/CategoryButton.prefab`

**LevelSelectionController** (añadir al UI_Frame):
- `Levels`: Arrastra `Level_Basic` (y otros niveles cuando los crees)
- `Level Buttons Container`: Arrastra el GameObject LevelButtons
- `Level Button Prefab`: Arrastra LevelButton.prefab
- `Category Buttons Container`: Arrastra CategoryButtons
- `Category Button Prefab`: Arrastra CategoryButton.prefab
- `Category Panel`: Arrastra CategoryPanel
- `Selected Level Text`: Arrastra SelectedLevelText
- `Back Button`: Arrastra BackButton

---

### 2.4 Configurar 03_LearningModule.unity

**Jerarquía:**

```
03_LearningModule
├── XR Origin
│   ├── Camera Offset
│   │   ├── Main Camera
│   │   ├── LeftHand Controller
│   │   │   └── XRHandTrackingEvents (Left)
│   │   └── RightHand Controller
│   │       └── XRHandTrackingEvents (Right)
│
├── GameManager
├── SceneLoader
│
├── GhostHands
│   ├── LeftGhostHand (mesh de mano izquierda)
│   └── RightGhostHand (mesh de mano derecha)
│
├── GestureRecognizers
│   ├── RightHandRecognizer (GameObject vacío)
│   │   └── GestureRecognizer.cs
│   └── LeftHandRecognizer (GameObject vacío)
│       └── GestureRecognizer.cs
│
└── UI_Frame
    ├── Panel_Front
    │   ├── SignNameText (TextMeshProUGUI)
    │   ├── SignDescriptionText (TextMeshProUGUI)
    │   ├── RepeatButton (Button)
    │   ├── PracticeButton (Button)
    │   ├── NextSignButton (Button)
    │   ├── PreviousSignButton (Button)
    │   └── FeedbackPanel (Panel - oculto al inicio)
    │       └── FeedbackText (TextMeshProUGUI)
    │
    ├── Panel_Left
    │   └── SelfAssessmentButton (Button)
    │
    ├── Panel_Right
    │   └── InstructionsText
    │
    └── BackButton (Button)
```

**GhostHandPlayer** (añadir a GhostHands):
- `Left Ghost Hand`: Arrastra LeftGhostHand
- `Right Ghost Hand`: Arrastra RightGhostHand
- `Ghost Hand Material`: Crea un material semi-transparente
- `Ghost Hand Color`: Color azul con alpha 0.5
- `Fade In Duration`: 0.3
- `Static Pose Display Time`: 3

**RightHandRecognizer (GestureRecognizer):**
- `Target Sign`: Dejar vacío (se asigna dinámicamente)
- `Hand Tracking Events`: Arrastra XRHandTrackingEvents (Right)
- `Detection Interval`: 0.1
- `Use Sign Data Hold Time`: ✓ (true)
- `Show Debug Logs`: ✓ (true) - para desarrollo

**LeftHandRecognizer (GestureRecognizer):**
- Igual que RightHandRecognizer, pero con XRHandTrackingEvents (Left)

**LearningController** (añadir al UI_Frame):
- `Sign Name Text`: Arrastra SignNameText
- `Sign Description Text`: Arrastra SignDescriptionText
- `Repeat Button`: Arrastra RepeatButton
- `Practice Button`: Arrastra PracticeButton
- `Self Assessment Button`: Arrastra SelfAssessmentButton
- `Back Button`: Arrastra BackButton
- `Ghost Hand Player`: Arrastra el GameObject GhostHands
- `Right Hand Recognizer`: Arrastra RightHandRecognizer
- `Left Hand Recognizer`: Arrastra LeftHandRecognizer
- `Feedback Panel`: Arrastra FeedbackPanel
- `Feedback Text`: Arrastra FeedbackText
- `Next Sign Button`: Arrastra NextSignButton
- `Previous Sign Button`: Arrastra PreviousSignButton

---

### 2.5 Configurar 04_SelfAssessmentMode.unity

**Jerarquía:**

```
04_SelfAssessmentMode
├── XR Origin
│   ├── Camera Offset
│   │   ├── Main Camera
│   │   ├── LeftHand Controller
│   │   │   └── XRHandTrackingEvents (Left)
│   │   └── RightHand Controller
│   │       └── XRHandTrackingEvents (Right)
│
├── GameManager
├── SceneLoader
│
├── GestureRecognizers
│   ├── RightHandRecognizer
│   └── LeftHandRecognizer
│
└── UI_Frame
    ├── CategoryTitleText (TextMeshProUGUI)
    ├── ProgressText (TextMeshProUGUI)
    ├── GridContainer (GameObject con Grid Layout Group)
    │   └── (Las casillas se generarán dinámicamente)
    └── BackButton (Button)
```

**Crear prefab:**

1. Crea un panel: `GameObject > UI > Panel`
2. Añade:
   - Un `Image` como fondo (configura color por defecto)
   - Un `TextMeshProUGUI` para el nombre del signo
   - (Opcional) Un `Image` para el icono del signo
3. Añade el componente `SignTileController.cs`
4. Configura:
   - `Background Image`: Arrastra el Image de fondo
   - `Sign Name Text`: Arrastra el TextMeshProUGUI
   - `Sign Icon`: Arrastra el Image del icono (opcional)
   - `Default Color`: Gris oscuro (0.2, 0.2, 0.2, 1)
   - `Completed Color`: Azul (0, 0.627, 1, 1)
5. Guárdalo como prefab: `Assets/Prefabs/SignTile.prefab`

**GridContainer (Grid Layout Group):**
- Cell Size: (150, 150) - ajusta según tu diseño
- Spacing: (10, 10)
- Start Corner: Upper Left
- Start Axis: Horizontal
- Child Alignment: Upper Left
- Constraint: Fixed Column Count (4-6 columnas)

**SelfAssessmentController** (añadir al UI_Frame):
- `Category Title Text`: Arrastra CategoryTitleText
- `Grid Container`: Arrastra GridContainer
- `Sign Tile Prefab`: Arrastra SignTile.prefab
- `Back Button`: Arrastra BackButton
- `Right Hand Recognizer`: Arrastra RightHandRecognizer
- `Left Hand Recognizer`: Arrastra LeftHandRecognizer
- `Progress Text`: Arrastra ProgressText

---

## 🔧 PASO 3: CONFIGURACIÓN ADICIONAL

### 3.1 XRHandTrackingEvents

En cada escena que use reconocimiento de gestos, asegúrate de que:

1. Cada `LeftHand Controller` y `RightHand Controller` tenga el componente `XRHandTrackingEvents`
2. Configuración:
   - `Update Type`: Dynamic
   - `Handedness`: Left (o Right según corresponda)

### 3.2 Material de Ghost Hands

1. Crea un material: `Assets/Materials/GhostHandMaterial.mat`
2. Shader: `Standard` o `Universal Render Pipeline/Lit`
3. Rendering Mode: `Transparent`
4. Base Color: Azul claro con alpha 0.5
5. Asigna este material al `GhostHandPlayer`

---

## 🎮 PASO 4: PROBAR EL FLUJO COMPLETO

### 4.1 Flujo esperado

1. **01_MainMenu**:
   - Verifica que el texto de tracking se actualice correctamente
   - Botón "Learning Module" → 02_LevelSelection
   - Botón "Translation Module" → Popup "En desarrollo"
   - Botón "Exit" → Cierra la aplicación

2. **02_LevelSelection**:
   - Se generan botones de nivel (Basic, Intermediate, Advanced)
   - Al hacer clic en "Basic" → aparecen categorías (Alphabet)
   - Al hacer clic en "Alphabet" → 03_LearningModule

3. **03_LearningModule**:
   - Se muestra el primer signo (A)
   - Botón "Repeat" → muestra ghost hands
   - Botón "Practice" → activa feedback en tiempo real
   - Botón "Next/Previous" → navega entre signos
   - Botón "Self Assessment" → 04_SelfAssessmentMode

4. **04_SelfAssessmentMode**:
   - Se genera un grid con 26 casillas (A-Z)
   - Al hacer un signo correctamente → casilla se ilumina
   - Progress muestra "X/26"

---

## ⚠️ PROBLEMAS COMUNES Y SOLUCIONES

### Problema: "No hay categoría seleccionada en GameManager"

**Solución:**
- Asegúrate de que el `GameManager` esté en todas las escenas
- Verifica que `DontDestroyOnLoad` esté funcionando
- Comprueba que los datos se estén guardando correctamente en el GameManager

### Problema: "Hand Shapes no se detectan"

**Solución:**
- Verifica que `XRHandTrackingEvents` esté configurado correctamente
- Aumenta las tolerancias de los Hand Shapes (Upper/Lower Tolerance)
- Activa "Show Debug Logs" en `GestureRecognizer` para ver los eventos

### Problema: "Los botones no se generan dinámicamente"

**Solución:**
- Verifica que los prefabs estén asignados correctamente
- Comprueba que los contenedores (LevelButtons, CategoryButtons, GridContainer) existan
- Mira la consola para errores de null reference

---

## 🚀 PRÓXIMOS PASOS

1. **Crear Digits y Colors:**
   - Crea Hand Shapes para dígitos (0-9)
   - Crea Hand Shapes para colores
   - Crea los SignData, CategoryData correspondientes

2. **Implementar grabación de manos para gestos dinámicos (J, Z):**
   - Usa el sistema de Hand Capture del XR Hands
   - Graba las poses frame por frame
   - Implementa la reproducción en `GhostHandPlayer`

3. **Mejorar Ghost Hands:**
   - Implementar sistema para aplicar poses estáticas a los skeletons
   - Usar `XRHandSkeletonDriver` para controlar las joint positions

4. **Añadir niveles Intermediate y Advanced:**
   - Crear `Level_Intermediate` y `Level_Advanced`
   - Añadir categorías más complejas (frases, números, etc.)

5. **UI mejorada:**
   - Añadir animaciones de transición
   - Sonidos de feedback
   - Vibración háptica cuando se detecta un gesto correctamente

---

## 📞 RECURSOS ADICIONALES

- **Documentación oficial XR Hands:** https://docs.unity3d.com/Packages/com.unity.xr.hands@latest
- **Documentación XR Interaction Toolkit:** https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest
- **Guía Hand Shapes:** Ver `GUIA_HAND_SHAPES.md` en la raíz del proyecto

---

## ✅ CHECKLIST DE INTEGRACIÓN

- [ ] Creados 26 SignData (A-Z)
- [ ] Creado CategoryData "Alphabet"
- [ ] Creado LevelData "Basic"
- [ ] Escenas renombradas y añadidas a Build Settings
- [ ] GameManager, SceneLoader y HandTrackingStatus en todas las escenas
- [ ] MenuController configurado en 01_MainMenu
- [ ] LevelSelectionController configurado en 02_LevelSelection
- [ ] LearningController configurado en 03_LearningModule
- [ ] SelfAssessmentController configurado en 04_SelfAssessmentMode
- [ ] Prefabs creados (LevelButton, CategoryButton, SignTile)
- [ ] XRHandTrackingEvents configurado en cada mano
- [ ] GestureRecognizers configurados con referencias correctas
- [ ] Material de Ghost Hands creado y asignado
- [ ] Probado flujo completo de navegación

---

**¡Listo! Con esta guía deberías poder integrar todo el sistema correctamente.**

Si encuentras problemas, verifica la consola de Unity y activa "Show Debug Logs" en los componentes para ver qué está sucediendo.
