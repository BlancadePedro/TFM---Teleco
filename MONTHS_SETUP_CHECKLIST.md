# Checklist de Setup — Categoría "Meses" (Fingerspelling)

## Resumen rápido
Este documento te guía paso-a-paso para configurar la nueva categoría "Meses" con secuencias de 3 letras (JAN, FEB, etc.) en la escena LearningModule. Cada paso es una acción concreta en el Editor de Unity.

---

## PASO 1: Crear los assets de SignData de LETRAS (si no existen)

**¿Qué es?** Necesitas tener un `SignData` para cada letra (A, B, C, ..., Z, J, F, E, etc.). Estos se reutilizarán en las secuencias de meses.

**¿Cómo verificar si existen?**
1. En Project, ve a `Assets > Data > Signs` (o la carpeta donde tengas los SignData de letras).
2. Busca assets con nombres como `Sign_A`, `Sign_J`, `Sign_F`, etc.
3. Si ya existen todos (A..Z), salta al **PASO 2**.

**Si faltan letters:**
1. Right-click en la carpeta `Assets/Data/Signs` (o crea la carpeta si no existe).
2. Create → ASL Learn VR → Sign Data.
3. Nombra el asset: `Sign_A` (o usa el nombre que quieras, pero que el campo `Sign Name` sea exactamente `A`).
4. En el Inspector:
   - `Sign Name`: escribe `A`.
   - `Description`: "Letter A" (o vacío).
   - `Hand Shape Or Pose`: asigna el XRHandShape o XRHandPose correspondiente a la letra A.
   - `Requires Movement`: unchecked (false).
   - `Minimum Hold Time`: 0.3.
   - `Icon`: asigna un sprite de la letra A (opcional).
5. Guarda (Ctrl+S) y repite para cada letra que necesites (J, A, N, F, E, B, M, R, P, Y, U, L, G, S, O, C, T, V, D).

**Resultado esperado:** En `Assets/Data/Signs` tienes `Sign_A`, `Sign_J`, `Sign_F`, `Sign_E`, `Sign_B`, etc., cada uno con su `signName` exacto (A, J, F, E, B, ...).

---

## PASO 2: Crear los 12 assets MonthSequenceData automáticamente

**¿Qué es?** Un script de Editor que genera los 12 meses (ENERO..DICIEMBRE) como assets `MonthSequenceData` e intenta asignar las letras automáticamente.

**¿Cómo ejecutar?**
1. En Unity Editor, ve al menú: **Tools → ASL → Create Month Sequences (Meses)**.
2. Espera a que termine. Deberías ver en la **Console** mensajes como:
   - `CreateMonthSequences: Created MonthSequenceData 'ENERO'.`
   - Si alguna letra no se encuentra: `No se encontró SignData para la letra 'J'`. En ese caso, ve al PASO 1 y crea ese SignData.
3. Después de ejecutar el menú, comprueba que se crearon los assets:
   - Ve a `Assets > Data > Months` — deberías ver 12 assets: `Month_01_ENERO.asset`, `Month_02_FEBRERO.asset`, ..., `Month_12_DICIEMBRE.asset`.
   - Ve a `Assets > Data > Categories > Meses.asset` — deberías ver una `CategoryData` con 12 entradas en su lista `signs`.

**Si hay errores:**
- Si ves warnings sobre letras no encontradas, crea esos SignData en PASO 1 y ejecuta de nuevo el menú.
- Si los assets ya existen (lo hiciste antes), el menú actualiza las asignaciones de letras automáticamente.

---

## PASO 3: Añadir descripciones a cada MonthSequenceData (opcional pero recomendado)

**¿Qué es?** Rellenar el campo `Description` de cada mes con un texto que ayude al usuario.

**¿Cómo hacerlo?**
1. Ve a `Assets > Data > Months > Month_01_ENERO.asset`.
2. En el Inspector, expand `Sign Information`.
3. En el campo `Description` copia-pega el texto correspondiente (ver tabla de descripciones abajo).
4. Repite para los otros 11 meses.

**Descripciones (copia-pega):**

| Mes | Abreviatura | Descripción |
|-----|-------------|-------------|
| ENERO | JAN | Fingerspelling for JANUARY: J → A → N. Practice the three letters in strict order; hold each letter for approximately 0.3s until it is recognized. Tip: make each letter shape clearly before moving to the next. |
| FEBRERO | FEB | Fingerspelling for FEBRUARY: F → E → B. Perform the letters in strict order and hold each position until the system confirms it. Do not skip or change the order. |
| MARZO | MAR | Fingerspelling for MARCH: M → A → R. Practice each letter calmly; hold each shape until the system confirms it before proceeding. |
| ABRIL | APR | Fingerspelling for APRIL: A → P → R. Follow the shown order and wait for confirmation for each letter. If a letter is not recognized, hold the shape slightly longer. |
| MAYO | MAY | Fingerspelling for MAY: M → A → Y. Perform the letters in order; adjust finger positions if a letter fails to be recognized. |
| JUNIO | JUN | Fingerspelling for JUNE: J → U → N. Keep the sequence in order and make gentle transitions between letters when necessary. |
| JULIO | JUL | Fingerspelling for JULY: J → U → L. Practice the transitions and ensure each letter shape is clear before moving on. |
| AGOSTO | AUG | Fingerspelling for AUGUST: A → U → G. Hold each letter until recognized; the expected order is strict. |
| SEPTIEMBRE | SEP | Fingerspelling for SEPTEMBER: S → E → P. Focus on correctly forming the S shape (it can be confused if not well articulated). |
| OCTUBRE | OCT | Fingerspelling for OCTOBER: O → C → T. Execute each letter clearly and in the shown order. |
| NOVIEMBRE | NOV | Fingerspelling for NOVEMBER: N → O → V. Maintain the sequence without skipping steps; repeat a letter if it is not detected. |
| DICIEMBRE | DEC | Fingerspelling for DECEMBER: D → E → C. Practice the three letters in order; once all three are correct the month will be marked as completed. |

**Resultado esperado:** Cada `MonthSequenceData` tiene un `Description` rellenado.

---

## PASO 4: Crear el prefab/Canvas de tiles (panel de 3 letras)

**¿Qué es?** Una UI (World Space Canvas) que muestra 3 tiles (uno por letra) y un estado. Cada tile usa `SignTileController` (el mismo componente que Auto-Evaluación).

**¿Cómo crear?**

### 4a. Crear un Canvas (World Space)
1. Right-click en la jerarquía (Hierarchy) de la escena `03_LearningModule`.
2. UI → Canvas → Text - TextMeshPro (esto crea un Canvas).
3. Cambia el Canvas a **World Space**:
   - Selecciona el Canvas en la jerarquía.
   - En el Inspector, busca `Canvas` component.
   - Cambia `Render Mode` a `World Space`.
   - Ajusta `Rect Transform` (Position, Rotation, Scale) para que aparezca en un lugar visible frente al usuario en VR.
4. Nombra el Canvas: `MonthTilesPanel`.

### 4b. Crear los 3 tiles dentro del Canvas
1. Ve a la escena `04_SelfAssessmentMode` y observa cómo están estructurados los tiles en el grid de autoevaluación.
   - Busca un GameObject que tenga el componente `SignTileController` (ej: un tile individual).
   - Nota su estructura: probablemente es un GameObject con una `Image` (background), un `TextMeshProUGUI` para el nombre, y un `Image` para el icono.
2. Opción A (Rápida): Copia el prefab/GameObject del tile de Auto-Evaluación:
   - En `04_SelfAssessmentMode`, busca el tile prefab o el GameObject del tile en la jerarquía.
   - Drag-and-drop al Project para crear un prefab (si no existe ya).
   - Ve a tu escena `03_LearningModule` y arrastra ese prefab 3 veces dentro del `MonthTilesPanel` Canvas.
3. Opción B (Manual): Crea 3 GameObjects manualmente:
   - En `MonthTilesPanel`, crea 3 GameObjects: `Tile_0`, `Tile_1`, `Tile_2`.
   - Cada uno debe tener:
     - Un `Image` component (background).
     - Un `TextMeshProUGUI` component (para mostrar el nombre de la letra).
     - Un `Image` component hijo (para el icono de la letra, opcional).
     - El script `SignTileController` asignado.

### 4c. Nombra y posiciona los tiles
1. Renombra los GameObjects dentro del Canvas como `Tile_0`, `Tile_1`, `Tile_2`.
2. Posiciona los 3 tiles lado-a-lado (horizontalmente) en el Canvas:
   - Cada tile debe estar visible y bien separado.
   - Ajusta sus `Rect Transform` para que no se superpongan.

### 4d. Configura cada SignTileController
1. Selecciona `Tile_0` en la jerarquía.
2. En el Inspector, encuentra el componente `SignTileController`.
3. Asigna sus campos:
   - `Background Image`: arrastra el componente `Image` (background) del propio Tile_0.
   - `Sign Name Text`: arrastra el componente `TextMeshProUGUI` que muestra el nombre.
   - `Sign Icon` (opcional): arrastra el `Image` hijo para el icono.
   - Mantén los colores por defecto:
     - `Default Color`: gris (≈ 0.6, 0.6, 0.6).
     - `Completed Color`: azul (≈ 0, 0.63, 1).
     - `Recognized Color`: amarillo/dorado (≈ 1, 0.84, 0).
4. Repite para `Tile_1` y `Tile_2`.

**Resultado esperado:** Canvas `MonthTilesPanel` contiene 3 tiles (Tile_0, Tile_1, Tile_2), cada uno con `SignTileController` correctamente configurado.

---

## PASO 5: Crear y configurar el componente MonthTilesUI

**¿Qué es?** Un script que gestiona el panel de 3 tiles (mostrar/ocultar, marcar completadas, actualizar estado).

**¿Cómo hacerlo?**
1. Selecciona el Canvas `MonthTilesPanel` en la jerarquía.
2. En el Inspector, haz click en "Add Component".
3. Busca `MonthTilesUI` y asigna el script.
4. Ahora rellena los campos del componente `MonthTilesUI`:
   - `Root Panel`: arrastra el propio GameObject `MonthTilesPanel` (o el Canvas).
   - `Tile Backgrounds` (array):
     - Size = 3.
     - Element 0: arrastra el componente `Image` de `Tile_0`.
     - Element 1: arrastra el componente `Image` de `Tile_1`.
     - Element 2: arrastra el componente `Image` de `Tile_2`.
   - `Tile Letters` (array):
     - Size = 3.
     - Element 0: arrastra el `TextMeshProUGUI` de `Tile_0`.
     - Element 1: arrastra el `TextMeshProUGUI` de `Tile_1`.
     - Element 2: arrastra el `TextMeshProUGUI` de `Tile_2`.
   - `Status Text`: crea un nuevo `TextMeshProUGUI` debajo de los tiles (que diga "Ahora toca: X") y arrástralo aquí.
   - `Reset Button` (opcional): crea un botón `Reset` y arrástralo aquí (onclick vincularlo a `MonthTilesUI.ResetUI()`).
   - `Success Audio` (opcional): arrastra un `AudioSource` si quieres sonido de éxito.
   - `Sign Tile Controllers` (array) — **IMPORTANTE**:
     - Size = 3.
     - Element 0: arrastra el componente `SignTileController` de `Tile_0`.
     - Element 1: arrastra el componente `SignTileController` de `Tile_1`.
     - Element 2: arrastra el componente `SignTileController` de `Tile_2`.

**Resultado esperado:** El componente `MonthTilesUI` en el Canvas tiene todos sus campos asignados.

---

## PASO 6: Configurar MonthPracticeController en la escena

**¿Qué es?** Un script que gestiona la lógica de práctica (detectar gestos, avanzar pasos, actualizar tiles).

**¿Cómo hacerlo?**
1. En la escena `03_LearningModule`, encuentra el GameObject que tiene el componente `LearningController` (probablemente se llama `LearningController` o está en un GameObject principal).
2. Selecciona ese GameObject.
3. Add Component → `MonthPracticeController`.
4. Rellena los campos de `MonthPracticeController`:
   - `Right Hand Recognizer`: arrastra el componente `GestureRecognizer` de la mano derecha (ya existe en la escena, busca `RightHandRecognizer` o similar en la jerarquía).
   - `Left Hand Recognizer`: arrastra el componente `GestureRecognizer` de la mano izquierda.
   - `Tiles UI`: arrastra el Canvas `MonthTilesPanel` (o el GameObject con el componente `MonthTilesUI`).

**Resultado esperado:** `MonthPracticeController` tiene sus 3 campos asignados correctamente.

---

## PASO 7: Configurar LearningController para usar MonthPracticeController

**¿Qué es?** Enlazar el nuevo controlador de meses con el controlador principal de aprendizaje.

**¿Cómo hacerlo?**
1. Selecciona el GameObject con el componente `LearningController`.
2. En el Inspector, busca el campo `Month Practice Controller` (fue añadido en los cambios de código).
3. Arrastra el GameObject que tiene `MonthPracticeController` (probablemente el mismo GameObject, o el padre si está en otro lugar).

**Resultado esperado:** `LearningController` tiene una referencia al `MonthPracticeController`.

---

## PASO 8: Asignar la categoría "Meses" al GameManager o selector de categorías

**¿Qué es?** Asegurarse que cuando el usuario selecciona la categoría "Meses", se carga correctamente en la escena.

**¿Cómo hacerlo?**
1. Busca en tu sistema de selección de categorías dónde se asigna la `CurrentCategory` al `GameManager`.
2. Verifica que el asset `Assets/Data/Categories/Meses.asset` está incluido en la lista de categorías disponibles para el nivel actual.
3. Prueba: en el Editor, ejecuta la escena, selecciona el nivel, y asegúrate que "Meses" aparece como opción.

**Resultado esperado:** Al ejecutar la escena de LearningModule y seleccionar "Meses", carga correctamente.

---

## PASO 9: Probar en la escena

**¿Qué es?** Verificar que todo funciona: navegación, tiles, práctica, reconocimiento.

**¿Cómo hacerlo?**
1. Abre la escena `03_LearningModule`.
2. En el Editor, presiona **Play** (triángulo verde).
3. Navega a la categoría **"Meses"** (usando los selectores de nivel/categoría).
4. Selecciona un mes (ej. ENERO).
5. Verifica:
   - ✅ El nombre del mes aparece en pantalla (ej. "ENERO").
   - ✅ El botón `Repetir` muestra ghost hands (opcional, si está implementado).
   - ✅ El botón `Practicar` muestra el panel de 3 tiles con las letras (J, A, N para ENERO).
   - ✅ Los tiles están inicialmente en color gris (default).
   - ✅ El texto de estado dice "Ahora toca: J".
   - ✅ Si simulas una detección de gesto (o haces manualmente) la letra J, el primer tile se pone **verde/azul** (color completed).
   - ✅ El estado actualiza a "Ahora toca: A".
   - ✅ Al completar las 3 letras (J, A, N en orden), todos los tiles se ponen verdes y aparece "Mes completado ✅".
6. Prueba navegación:
   - Pulsa "Siguiente" para ir al próximo mes (FEBRERO).
   - Verifica que el panel se resetea (vuelve a gris, los tiles muestran las nuevas letras F, E, B).
7. Prueba "Anterior" para ir al mes previo.

**Resultado esperado:** Todo funciona sin errores; los tiles cambian de color, se actualizan los nombres, y la secuencia se puede practicar.

---

## PASO 10: Probar con reconocimiento real (opcional, si tienes gloves/hand-tracking)

**¿Qué es?** Verificar que los `GestureRecognizer` detectan las letras correctamente durante la práctica.

**¿Cómo hacerlo?**
1. Ejecuta la escena con hand-tracking habilitado (Meta Quest 3, etc.).
2. Selecciona un mes en la práctica.
3. Realiza el gesto de la primera letra (ej. J para ENERO):
   - El `GestureRecognizer` debería detectarlo.
   - El tile debería volverse verde.
   - El estado debería avanzar a la siguiente letra.
4. Repite para las 3 letras.
5. Al completar, debería mostrarse "Mes completado ✅".

**Resultado esperado:** El reconocimiento funciona y la secuencia avanza correctamente.

---

## Resumen de assets/scripts creados

| Archivo | Tipo | Ubicación | Descripción |
|---------|------|-----------|-------------|
| `MonthSequenceData.cs` | Script | `Assets/Scripts/Data/` | ScriptableObject que hereda de SignData; almacena 3 letras y clips guía. |
| `MonthTilesUI.cs` | Script | `Assets/Scripts/LearningModule/` | Controla UI de tiles (mostrar/ocultar, marcar completadas). |
| `MonthPracticeController.cs` | Script | `Assets/Scripts/LearningModule/` | Gestiona lógica de práctica (pasos, detecciones, reset). |
| `CreateMonthSequencesEditor.cs` | Script Editor | `Assets/Editor/` | Menú que genera automáticamente los 12 MonthSequenceData. |
| `Month_01_ENERO.asset` ... `Month_12_DICIEMBRE.asset` | MonthSequenceData | `Assets/Data/Months/` | 12 assets de meses (generados por el Editor script). |
| `Meses.asset` | CategoryData | `Assets/Data/Categories/` | Categoría que contiene los 12 meses (generada por Editor script). |

---

## Troubleshooting

| Problema | Solución |
|----------|----------|
| "No se encontró SignData para la letra X" | Crea el SignData faltante en PASO 1 y ejecuta el menú de nuevo. |
| Los tiles no aparecen en pantalla | Verifica que `MonthTilesPanel` es World Space y está posicionado correctamente. |
| Los tiles no cambian de color | Comprueba que `SignTileController` está asignado a cada tile y que los campos `Background Image`, `Sign Name Text` están enlazados. |
| No se detectan gestos | Verifica que `GestureRecognizer` está correctamente asignado en `MonthPracticeController` y que los recognizers tienen listeners activos. |
| La navegación (Anterior/Siguiente) no resetea tiles | Asegúrate que `LearningController.LoadSign()` llama a `monthPracticeController.OnSignChanged()`. |

---

## ¡Listo!

Una vez completados estos 10 pasos, la categoría "Meses" estará funcional. El flujo será:
1. Usuario selecciona "Meses" → ve ENERO (o cualquier mes).
2. Pulsa "Practicar" → aparecen 3 tiles con las letras (ej. J, A, N).
3. Realiza los gestos en orden → tiles se ponen verdes.
4. Al completar las 3 letras → "Mes completado ✅".
5. Pulsa "Siguiente" → siguiente mes, tiles se resetean.

¡Que disfrutes!
