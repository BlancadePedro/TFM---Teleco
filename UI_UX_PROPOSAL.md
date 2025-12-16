# Propuesta UI/UX - ASL Learn VR
## Plataforma Educativa de Lengua de Signos Americana

> **Diseño centrado en el usuario** - Claridad, jerarquía visual y flujo pedagógico optimizado

---

## 📋 Índice

1. [Escena 1 - Pantalla Principal / Onboarding](#escena-1---pantalla-principal--onboarding)
2. [Escena 2 - Selección de Nivel](#escena-2---selección-de-nivel)
3. [Escena 3 - Módulo de Aprendizaje](#escena-3---módulo-de-aprendizaje)
4. [Escena 4 - Modo Autoevaluación](#escena-4---modo-autoevaluación)
5. [Especificaciones de Hand Shapes](#especificaciones-de-hand-shapes)
6. [Guía de Implementación](#guía-de-implementación)

---

## Escena 1 - Pantalla Principal / Onboarding

### 🎯 Objetivo
Presentar la plataforma de forma clara y acogedora, establecer expectativas y guiar al usuario en su primera interacción.

### 📐 Estructura de Paneles

#### **Panel Frontal Central** (Main Menu)

```
┌─────────────────────────────────────────┐
│                                         │
│      Bienvenido a ASL Learn VR          │
│   Tu plataforma de aprendizaje de       │
│    Lengua de Signos Americana          │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  🎓 Modo Aprendizaje (SLR)       │  │
│  │                                  │  │
│  │  Aprende y practica signos ASL  │  │
│  │  a tu propio ritmo              │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  🌐 Modo Traducción (SLT)        │  │
│  │                                  │  │
│  │  Traduce en tiempo real          │  │
│  │  [Próximamente]                  │  │
│  └──────────────────────────────────┘  │
│                                         │
│              [Salir]                    │
└─────────────────────────────────────────┘
```

#### **Textos Propuestos**

**Título Principal:**
```
Bienvenido a ASL Learn VR
Tu plataforma de aprendizaje de Lengua de Signos Americana
```

**Panel SLR (Aprendizaje):**
```
🎓 Modo Aprendizaje

Aprende y practica signos ASL a tu propio ritmo.
Progresa desde el alfabeto básico hasta vocabulario avanzado
con retroalimentación en tiempo real.

[Comenzar Aprendizaje]
```

**Panel SLT (Traducción):**
```
🌐 Modo Traducción

Traduce tus signos a texto en tiempo real.
Practica conversaciones completas con reconocimiento automático.

⏳ Este módulo estará disponible próximamente
```

Estilo del mensaje "Próximamente":
- Color: `#FFA726` (naranja cálido, no agresivo)
- Icono: Reloj de arena o calendario
- Posición: Abajo del texto descriptivo
- Opacidad del botón: 0.6
- Estado: Deshabilitado pero visualmente presente

#### **Panel Derecho** (Instrucciones / Onboarding)

```
┌─────────────────────────────────────┐
│  👋 Primeros Pasos                  │
│                                     │
│  Estás en un entorno de realidad   │
│  virtual diseñado para aprender     │
│  Lengua de Signos Americana.       │
│                                     │
│  🖐️ Interacción:                   │
│  • Mueve tus manos naturalmente    │
│  • Apunta y selecciona con el      │
│    dedo índice                      │
│  • Los paneles responden a tus     │
│    gestos                           │
│                                     │
│  🎯 Siguiente paso:                 │
│  Selecciona "Modo Aprendizaje"     │
│  para comenzar tu primera          │
│  lección de signos.                 │
│                                     │
│  📊 Estado del Sistema:             │
│  [Indicador de tracking de manos]  │
└─────────────────────────────────────┘
```

**Texto Completo del Panel de Instrucciones:**

```
👋 Primeros Pasos

Estás en un entorno de realidad virtual diseñado para
aprender Lengua de Signos Americana de forma interactiva.

🖐️ Cómo Interactuar:
• Mueve tus manos de forma natural - el sistema las detecta automáticamente
• Apunta con tu dedo índice para seleccionar botones y paneles
• Los elementos resaltan cuando están listos para ser activados
• Puedes mirar alrededor moviendo la cabeza

🎯 Tu Primer Paso:
1. Selecciona "Modo Aprendizaje" en el panel central
2. Elige un nivel (te recomendamos empezar por "Básico")
3. Selecciona una categoría (Alfabeto, Dígitos o Colores)
4. ¡Comienza a aprender!

📊 Estado del Sistema:
[Indicador dinámico del HandTrackingStatus]
• Verde: Manos detectadas correctamente
• Amarillo: Tracking parcial
• Rojo: Coloca tus manos frente a la cámara
```

#### **Panel Izquierdo** (Estado de Tracking - Opcional)

```
┌────────────────────────┐
│  📡 Estado de Manos    │
│                        │
│  ✓ Mano izquierda: OK  │
│  ✓ Mano derecha: OK    │
│                        │
│  Precisión: Alta       │
└────────────────────────┘
```

---

## Escena 2 - Selección de Nivel

### 🎯 Objetivo
Permitir al usuario elegir su nivel de competencia y navegar por categorías de forma clara y organizada.

### 📐 Estructura de Paneles

#### **Panel Superior** (Título y Contexto)

```
┌─────────────────────────────────────────────┐
│  Selecciona tu Nivel de Aprendizaje        │
│                                             │
│  Elige el nivel que mejor se adapte a tu   │
│  experiencia con la Lengua de Signos       │
└─────────────────────────────────────────────┘
```

**Título:**
```
Selecciona tu Nivel de Aprendizaje
```

**Subtítulo:**
```
Elige el nivel que mejor se adapte a tu experiencia
con la Lengua de Signos Americana
```

#### **Paneles de Nivel** (Tres botones principales)

Layout horizontal con espaciado uniforme:

```
┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│   🌱 BÁSICO │  │  🌿 MEDIO   │  │ 🌳 AVANZADO │
│             │  │             │  │             │
│ Aprende los │  │ Expande tu  │  │ Domina      │
│ fundamentos │  │ vocabulario │  │ vocabulario │
│ esenciales  │  │ y frases    │  │ complejo y  │
│             │  │ comunes     │  │ gramática   │
│             │  │             │  │             │
│ [Alfabeto]  │  │ Próximamente│  │ Próximamente│
│ [Dígitos]   │  │             │  │             │
│ [Colores]   │  │             │  │             │
└─────────────┘  └─────────────┘  └─────────────┘
```

**Textos de los Niveles:**

**Nivel Básico:**
```
🌱 BÁSICO
Fundamentos Esenciales

Perfecto si estás comenzando tu viaje en ASL.
Aprende el alfabeto manual, números básicos y
colores fundamentales.

Categorías disponibles:
• Alfabeto (A-Z)
• Dígitos (0-9)
• Colores (Básicos)
```

**Nivel Medio:**
```
🌿 MEDIO
Vocabulario Expandido

Amplía tu conocimiento con signos de uso cotidiano,
frases comunes y vocabulario temático.

⏳ Próximamente
Este nivel estará disponible en futuras versiones
```

**Nivel Avanzado:**
```
🌳 AVANZADO
Dominio Completo

Vocabulario especializado, expresiones complejas
y estructuras gramaticales avanzadas de ASL.

⏳ Próximamente
Este nivel estará disponible en futuras versiones
```

#### **Panel de Categorías** (Dinámico - aparece tras seleccionar nivel)

Aparece debajo del nivel seleccionado con animación suave:

```
┌──────────────────────────────────────┐
│  Has seleccionado: BÁSICO            │
│                                      │
│  Elige una categoría para comenzar:  │
│                                      │
│  ┌──────────────────────────────┐   │
│  │  🔤 Alfabeto                 │   │
│  │  26 signos • A-Z             │   │
│  └──────────────────────────────┘   │
│                                      │
│  ┌──────────────────────────────┐   │
│  │  🔢 Dígitos                  │   │
│  │  10 signos • 0-9             │   │
│  └──────────────────────────────┘   │
│                                      │
│  ┌──────────────────────────────┐   │
│  │  🎨 Colores                  │   │
│  │  8 signos • Básicos          │   │
│  └──────────────────────────────┘   │
│                                      │
│              [← Volver]              │
└──────────────────────────────────────┘
```

**Especificaciones del Contenedor de Categorías:**

```csharp
// Component: VerticalLayoutGroup
spacing: 15f
padding: { left: 20, right: 20, top: 20, bottom: 20 }
childAlignment: MiddleCenter
childControlWidth: true
childControlHeight: false
childForceExpandWidth: true
childForceExpandHeight: false

// Component: ContentSizeFitter
verticalFit: PreferredSize
horizontalFit: Unconstrained
```

**Botón de Categoría Individual:**

```
┌─────────────────────────────────────┐
│  [Icono] CATEGORÍA                  │
│  X signos • Descripción breve       │
│  ────────────────────────────        │
│  Progreso: ██████░░░░ 60%          │
└─────────────────────────────────────┘

Altura mínima: 80px
Ancho: 100% del contenedor padre
Padding interno: 15px
```

---

## Escena 3 - Módulo de Aprendizaje

### 🎯 Objetivo
Facilitar el aprendizaje individual de signos con retroalimentación clara y navegación intuitiva.

### 📐 Estructura de Paneles

#### **Panel Superior** (Título y Navegación)

```
┌─────────────────────────────────────────────┐
│  [←] Alfabeto • Letra B                    │
│                                             │
│  Progreso: 2/26                             │
└─────────────────────────────────────────────┘
```

#### **Panel Central** (Información del Signo)

```
┌─────────────────────────────────────────┐
│                                         │
│            [Modelo 3D]                  │
│           Manos Fantasma                │
│                                         │
│  ────────────────────────────────       │
│                                         │
│  📌 Letra B                             │
│                                         │
│  Cierra el puño con todos los dedos    │
│  extendidos hacia arriba, excepto el   │
│  pulgar que queda doblado dentro.      │
│                                         │
│  💡 Consejo:                            │
│  Mantén los dedos juntos y rectos      │
│                                         │
└─────────────────────────────────────────┘
```

#### **Panel Inferior** (Controles)

```
┌─────────────────────────────────────────┐
│                                         │
│  [◄ Anterior]  [🔁 Repetir]  [Siguiente ►]│
│                                         │
│  [✋ Practicar]    [✓ Autoevaluación]   │
│                                         │
└─────────────────────────────────────────┘
```

#### **Panel de Retroalimentación** (Durante práctica)

```
┌─────────────────────────────────────────┐
│  🎯 Modo Práctica Activo                │
│                                         │
│  ✓ ¡Correcto! Signo 'B' detectado.     │
│                                         │
│  Tiempo mantenido: 1.2s / 0.3s         │
│  Precisión: 94%                         │
│                                         │
│  [Detener Práctica]                     │
└─────────────────────────────────────────┘
```

**Estados del Panel de Retroalimentación:**

**Estado Inicial (Esperando):**
```
🎯 Modo Práctica Activo

Realiza el signo '{SignName}' con tu mano...

Coloca tus manos en el campo de visión
y forma el signo correctamente.
```

**Estado de Éxito:**
```
✓ ¡Correcto!

Signo '{SignName}' detectado con éxito.

Precisión: 94%
Tiempo mantenido: 1.2s
```

**Estado de Casi Correcto:**
```
⚠️ Casi lo tienes

El signo está casi correcto.
Revisa la posición de tus dedos.

Similar a: {SimilarSign}
Precisión actual: 67%
```

**Estado de Incorrecto:**
```
⊘ Signo no reconocido

Compara tu mano con el modelo 3D.
Puedes usar el botón "Repetir" para
ver la animación nuevamente.
```

---

## Escena 4 - Modo Autoevaluación

### 🎯 Objetivo
Presentar todos los signos de una categoría en formato grid para evaluación progresiva con feedback visual claro.

### 📐 Estructura de Paneles

#### **Panel Superior** (Título y Progreso)

```
┌─────────────────────────────────────────────┐
│  Autoevaluación: Alfabeto                  │
│                                             │
│  Progreso: 15/26 signos completados        │
│  ██████████████░░░░░░░░░░ 58%             │
│                                             │
│  [← Volver al Aprendizaje]                 │
└─────────────────────────────────────────────┘
```

**Título:**
```
Autoevaluación: {CategoryName}
```

**Progreso:**
```
Progreso: {completed}/{total} signos completados
[Barra de progreso visual]
```

#### **Panel Central** (Grid de Signos)

**Layout Responsive con GridLayoutGroup:**

```
┌───────────────────────────────────────────────┐
│                                               │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐   │
│  │  A  │ │  B  │ │  C  │ │  D  │ │  E  │   │
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘   │
│                                               │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐   │
│  │  F  │ │  G  │ │  H  │ │  I  │ │  J  │   │
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘   │
│                                               │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐   │
│  │  K  │ │  L  │ │  M  │ │  N  │ │  O  │   │
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘   │
│                                               │
│  ... [más filas según necesario]              │
│                                               │
└───────────────────────────────────────────────┘
```

**Especificaciones del GridLayoutGroup:**

```csharp
// Container: CategoryTilesContainer (nuevo GameObject)
// Parent: Panel Central, posicionado debajo del título

// Component: RectTransform
anchorMin: (0, 0)
anchorMax: (1, 1)
pivot: (0.5, 1)
offsetMin: (20, 20)   // Margen inferior
offsetMax: (-20, -120) // Margen superior (espacio para título)

// Component: GridLayoutGroup
padding: { left: 20, right: 20, top: 20, bottom: 20 }
cellSize: (100, 120)
spacing: (15, 15)
startCorner: UpperLeft
startAxis: Horizontal
childAlignment: UpperCenter
constraint: FixedColumnCount // O Flexible según diseño
constraintCount: 5 // 5 columnas por defecto

// Component: ContentSizeFitter (Opcional)
horizontalFit: Unconstrained
verticalFit: PreferredSize
```

**Tile Individual (SignTile):**

```
┌─────────────┐
│             │
│   [Icono]   │
│             │
│      B      │
│             │
└─────────────┘

Estados:
• Pendiente: Gris oscuro (#333333)
• Completado: Cyan brillante (#00A0FF)
• Activo: Borde amarillo (#FFD700)
```

**Especificaciones del Tile:**

```csharp
// Component: Image (Background)
color: (0.2, 0.2, 0.2, 1) // Gris oscuro por defecto
completedColor: (0, 0.627, 1, 1) // Cyan brillante
transitionDuration: 0.3s
cornerRadius: 10px // Si usas UI con bordes redondeados

// Component: TextMeshProUGUI (SignName)
fontSize: 32
alignment: Center
color: White
font: Bold

// Component: Image (Icon) - Opcional
size: 60x60
position: Centro superior del tile
alpha: 0.8
```

#### **Layout Responsivo - Especificaciones Detalladas**

**Comportamiento del Grid:**
1. **Ancho fijo por tile**, altura flexible
2. **Relleno de izquierda a derecha**, luego nueva fila
3. **Número de columnas adaptable** según resolución:
   - Desktop/VR alta res: 6-8 columnas
   - VR estándar: 4-5 columnas
   - Adaptación automática basada en ancho del contenedor

**Algoritmo de Posicionamiento:**

```
Tiles por fila = floor(AnchoContenedor / (CellWidth + Spacing))
Filas totales = ceil(TotalSignos / TilesPorFila)

Posición tile[i]:
  fila = floor(i / TilesPorFila)
  columna = i % TilesPorFila

  x = padding.left + columna * (cellWidth + spacing.x)
  y = -padding.top - fila * (cellHeight + spacing.y)
```

#### **Panel de Instrucciones** (Lateral derecho - opcional)

```
┌──────────────────────────────┐
│  📋 Instrucciones            │
│                              │
│  Realiza cada signo de la    │
│  cuadrícula para marcarlo    │
│  como completado.            │
│                              │
│  ✓ Verde: Completado         │
│  ○ Gris: Pendiente           │
│                              │
│  Puedes hacerlos en          │
│  cualquier orden.            │
│                              │
│  🎯 Objetivo:                │
│  Completar el 100% para      │
│  desbloquear la siguiente    │
│  categoría.                  │
└──────────────────────────────┘
```

---

## Especificaciones de Hand Shapes

### 🎯 Objetivo
Asegurar claridad visual y diferenciación perceptual entre signos similares.

### 📋 Hand Shapes Pendientes

#### **Dígitos Faltantes**

**Prioridad Alta:**
```
0 - Forma circular con pulgar e índice
2 - Índice y medio extendidos (forma V)
4 - Cuatro dedos extendidos, pulgar doblado
6 - Pulgar e meñique extendidos, otros doblados
```

**Reutilización Potencial:**
- **0**: Similar a 'O', pero con orientación específica
- **2**: Similar a 'V', ajustar ángulo de dedos
- **4**: Similar a 'B', pero con pulgar visible al lado
- **6**: Nueva configuración única

#### **Letras con Ambigüedad Visual**

**Grupo 1: G - Q - L**

| Letra | Diferenciador Principal | Especificación |
|-------|------------------------|----------------|
| **G** | Índice y pulgar horizontales, apuntan a la izquierda | Pulgar y índice forman L horizontal, otros dedos cerrados |
| **Q** | Índice y pulgar apuntan hacia abajo | Similar a G pero rotado 90° hacia abajo |
| **L** | Índice vertical, pulgar horizontal (forma L clara) | Ángulo de 90° perfecto, otros dedos completamente cerrados |

**Estrategias de Diferenciación:**
1. **Ángulo de orientación**:
   - G: Horizontal (0°)
   - L: Vertical (90°)
   - Q: Hacia abajo (180° respecto a G)

2. **Posición de la mano**:
   - G: Palma orientada hacia el cuerpo
   - L: Palma orientada hacia adelante
   - Q: Palma orientada hacia el suelo

3. **Indicadores visuales en el modelo 3D**:
   - Flechas sutiles indicando dirección
   - Destacar el ángulo crítico con color
   - Animación lenta mostrando la orientación correcta

**Grupo 2: K - P - V**

| Letra | Diferenciador Principal | Especificación |
|-------|------------------------|----------------|
| **K** | Índice vertical, medio en diagonal | Forma una "K" visual con índice, medio y pulgar |
| **P** | Similar a K, pero apuntando hacia abajo | Inversión completa de K |
| **V** | Índice y medio en V, palma hacia adelante | Dedos separados formando V clara |

**Estrategias de Diferenciación:**
1. **Configuración de dedos**:
   - K: Índice arriba, medio diagonal, pulgar toca medio
   - P: Igual que K pero invertido (apunta hacia abajo)
   - V: Índice y medio separados en V, sin pulgar tocando

2. **Orientación de palma**:
   - K: Palma hacia adelante
   - P: Palma hacia adelante, pero dedos hacia abajo
   - V: Palma hacia adelante

3. **Ángulo entre dedos**:
   - K: Ángulo agudo entre índice y medio (~30°)
   - V: Ángulo amplio (~45-60°)

### 🎨 Visualización Recomendada

**Para cada hand shape ambiguo:**

```
┌─────────────────────────────────────┐
│  Letra: K                           │
│                                     │
│  [Modelo 3D rotable 360°]          │
│                                     │
│  Vista frontal: [Imagen]           │
│  Vista lateral: [Imagen]           │
│  Vista superior: [Imagen]          │
│                                     │
│  ⚠️ Fácil de confundir con: P, V   │
│                                     │
│  🔍 Diferencia clave:               │
│  • Índice apunta hacia arriba      │
│  • Medio en ángulo de 30°          │
│  • Pulgar toca la articulación     │
│    del dedo medio                   │
└─────────────────────────────────────┘
```

### 🔧 Especificaciones Técnicas

**XRHandShape Assets - Configuración Recomendada:**

```csharp
// Para letras ambiguas, aumentar tolerancia en ángulos específicos
// pero ser más estrictos en otros parámetros

// Ejemplo para 'K' vs 'P':
public class ASL_Letter_K_Shape : XRHandShape
{
    // Índice debe estar vertical (± 10°)
    indexFingerCurl: 0.0f
    indexFingerSpread: 0.0f
    indexFingerRotation: (0, 0, 0) // Tolerancia: ±10°

    // Medio en diagonal específica (± 15°)
    middleFingerCurl: 0.0f
    middleFingerSpread: 0.3f
    middleFingerRotation: (30, 0, 0) // Tolerancia: ±15°

    // Pulgar debe tocar medio
    thumbCurl: 0.5f
    thumbTouchesMiddleFinger: true // CRÍTICO

    // Otros dedos completamente cerrados (± 5°)
    ringFingerCurl: 1.0f
    pinkyFingerCurl: 1.0f
}
```

---

## Guía de Implementación

### 📝 Checklist de Implementación

#### **Fase 1: Escena 1 - Main Menu**

- [ ] **Panel Frontal**
  - [ ] Actualizar título principal (`MenuController.cs`)
  - [ ] Añadir subtítulo descriptivo
  - [ ] Expandir texto del botón SLR con descripción
  - [ ] Añadir descripción al botón SLT
  - [ ] Mejorar estilo del popup "Próximamente"
  - [ ] Añadir iconos a los botones

- [ ] **Panel de Instrucciones**
  - [ ] Crear nuevo panel UI "InstructionsPanel"
  - [ ] Añadir TextMeshProUGUI con texto de onboarding
  - [ ] Integrar HandTrackingStatus dinámicamente
  - [ ] Posicionar a la derecha de la pantalla
  - [ ] Añadir iconos visuales (👋, 🖐️, 🎯, 📊)

- [ ] **Panel de Estado (Opcional)**
  - [ ] Crear panel lateral izquierdo
  - [ ] Mostrar estado de tracking de cada mano
  - [ ] Indicador de precisión visual

#### **Fase 2: Escena 2 - Level Selection**

- [ ] **Panel Superior**
  - [ ] Añadir título "Selecciona tu Nivel de Aprendizaje"
  - [ ] Añadir subtítulo explicativo
  - [ ] Ajustar posicionamiento

- [ ] **Paneles de Nivel**
  - [ ] Actualizar textos de cada nivel (Básico, Medio, Avanzado)
  - [ ] Añadir descripciones expandidas
  - [ ] Añadir iconos (🌱, 🌿, 🌳)
  - [ ] Mejorar mensaje "Próximamente" para niveles bloqueados

- [ ] **Panel de Categorías**
  - [ ] Crear contenedor "CategoryTilesContainer"
  - [ ] Configurar VerticalLayoutGroup con specs
  - [ ] Actualizar texto de encabezado dinámico
  - [ ] Añadir contador de signos por categoría
  - [ ] Añadir barra de progreso por categoría (opcional)
  - [ ] Modificar `LevelSelectionController.cs`:
    ```csharp
    // Añadir header text
    [SerializeField] private TextMeshProUGUI categoryHeaderText;

    void ShowCategories(LevelData level) {
        categoryHeaderText.text = $"Has seleccionado: {level.levelName}\n\nElige una categoría para comenzar:";
        // ... resto del código
    }
    ```

- [ ] **Botones de Categoría**
  - [ ] Actualizar prefab con layout mejorado
  - [ ] Añadir línea de "X signos • Descripción"
  - [ ] Añadir barra de progreso individual (opcional)
  - [ ] Asegurar tamaño mínimo de 80px

#### **Fase 3: Escena 3 - Learning Module**

- [ ] **Panel de Información**
  - [ ] Añadir icono al título del signo
  - [ ] Mejorar formato de descripción
  - [ ] Añadir sección "💡 Consejo"
  - [ ] Asegurar legibilidad del texto

- [ ] **Panel de Controles**
  - [ ] Reorganizar botones según mockup
  - [ ] Añadir iconos visuales (◄, 🔁, ►, ✋, ✓)
  - [ ] Mejorar espaciado y alineación

- [ ] **Panel de Retroalimentación**
  - [ ] Expandir estados de feedback
  - [ ] Añadir feedback "Casi correcto" (67-85% precisión)
  - [ ] Añadir sugerencia de signo similar
  - [ ] Mostrar métrica de precisión
  - [ ] Mostrar tiempo de mantenimiento
  - [ ] Modificar `LearningController.cs`:
    ```csharp
    void UpdateFeedback(float accuracy, float holdTime) {
        if (accuracy >= 0.85f) {
            feedbackText.text = $"✓ ¡Correcto!\n\nSigno '{currentSign.signName}' detectado con éxito.\n\nPrecisión: {accuracy:P0}\nTiempo: {holdTime:F1}s";
        } else if (accuracy >= 0.67f) {
            feedbackText.text = $"⚠️ Casi lo tienes\n\nEl signo está casi correcto.\nRevisa la posición de tus dedos.\n\nPrecisión: {accuracy:P0}";
        } else {
            feedbackText.text = "⊘ Signo no reconocido\n\nCompara tu mano con el modelo 3D.";
        }
    }
    ```

#### **Fase 4: Escena 4 - Self-Assessment**

- [ ] **Panel Superior**
  - [ ] Actualizar formato de título
  - [ ] Añadir barra de progreso visual
  - [ ] Añadir porcentaje de completado
  - [ ] Modificar `SelfAssessmentController.cs`:
    ```csharp
    void UpdateProgressDisplay() {
        int completed = tiles.Count(t => t.IsCompleted);
        int total = tiles.Count;
        float percentage = (float)completed / total;

        progressText.text = $"Progreso: {completed}/{total} signos completados";
        progressBar.fillAmount = percentage;
        progressPercentageText.text = $"{percentage:P0}";
    }
    ```

- [ ] **Grid de Signos**
  - [ ] Crear "CategoryTilesContainer" con RectTransform specs
  - [ ] Añadir GridLayoutGroup con configuración
  - [ ] Posicionar debajo del título (offsetMax: -120)
  - [ ] Configurar 5 columnas por defecto
  - [ ] Asegurar relleno de izquierda a derecha
  - [ ] Modificar `SelfAssessmentController.cs`:
    ```csharp
    void SetupGrid() {
        // Crear contenedor si no existe
        if (gridContainer == null) {
            GameObject container = new GameObject("CategoryTilesContainer");
            container.transform.SetParent(transform);

            RectTransform rt = container.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(20, 20);
            rt.offsetMax = new Vector2(-20, -120);

            GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.cellSize = new Vector2(100, 120);
            grid.spacing = new Vector2(15, 15);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            gridContainer = container.transform;
        }
    }
    ```

- [ ] **Tiles Individuales**
  - [ ] Ajustar tamaño a 100x120
  - [ ] Implementar transición de color suave
  - [ ] Añadir bordes redondeados (si es posible)
  - [ ] Asegurar iconos centrados
  - [ ] Modificar `SignTileController.cs`:
    ```csharp
    Color pendingColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    Color completedColor = new Color(0f, 0.627f, 1f, 1f);
    float transitionDuration = 0.3f;

    public void MarkAsCompleted() {
        StartCoroutine(TransitionColor(completedColor));
    }

    IEnumerator TransitionColor(Color target) {
        Color start = backgroundImage.color;
        float elapsed = 0f;

        while (elapsed < transitionDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            backgroundImage.color = Color.Lerp(start, target, t);
            yield return null;
        }

        backgroundImage.color = target;
    }
    ```

- [ ] **Panel de Instrucciones (Opcional)**
  - [ ] Crear panel lateral derecho
  - [ ] Añadir instrucciones claras
  - [ ] Explicar código de colores
  - [ ] Añadir objetivo de completado

#### **Fase 5: Hand Shapes**

- [ ] **Dígitos Faltantes**
  - [ ] Crear `ASL_Digit_0_Shape.asset`
  - [ ] Crear `ASL_Digit_2_Shape.asset`
  - [ ] Crear `ASL_Digit_4_Shape.asset`
  - [ ] Crear `ASL_Digit_6_Shape.asset`
  - [ ] Añadir iconos a cada dígito
  - [ ] Testear reconocimiento de cada uno

- [ ] **Refinar Letras Ambiguas - Grupo G-Q-L**
  - [ ] Revisar `ASL_Letter_G_Shape.asset`
    - [ ] Ajustar orientación horizontal
    - [ ] Especificar tolerancia de rotación: ±10°
    - [ ] Añadir parámetro de palma hacia cuerpo
  - [ ] Revisar `ASL_Letter_Q_Shape.asset`
    - [ ] Ajustar orientación hacia abajo
    - [ ] Diferenciar rotación respecto a G (90°)
  - [ ] Revisar `ASL_Letter_L_Shape.asset`
    - [ ] Asegurar ángulo de 90° perfecto
    - [ ] Palma hacia adelante
    - [ ] Otros dedos completamente cerrados

- [ ] **Refinar Letras Ambiguas - Grupo K-P-V**
  - [ ] Revisar `ASL_Letter_K_Shape.asset`
    - [ ] Índice vertical con tolerancia ±10°
    - [ ] Medio en diagonal 30° ±15°
    - [ ] Añadir parámetro: pulgar toca medio
  - [ ] Revisar `ASL_Letter_P_Shape.asset`
    - [ ] Configuración inversa a K (apunta abajo)
    - [ ] Mantener misma tolerancia
  - [ ] Revisar `ASL_Letter_V_Shape.asset`
    - [ ] Ángulo entre dedos: 45-60°
    - [ ] Sin contacto de pulgar

- [ ] **Visualización Multi-ángulo**
  - [ ] Añadir vistas laterales a modelos 3D
  - [ ] Implementar rotación 360° interactiva
  - [ ] Añadir indicadores de orientación
  - [ ] Crear sección "Fácil de confundir con"

#### **Fase 6: Testing y Refinamiento**

- [ ] **Testing de UI**
  - [ ] Verificar legibilidad de todos los textos en VR
  - [ ] Asegurar espaciado adecuado entre elementos
  - [ ] Testear navegación fluida entre escenas
  - [ ] Validar que todos los iconos son visibles

- [ ] **Testing de Hand Shapes**
  - [ ] Testear cada dígito nuevo (0, 2, 4, 6)
  - [ ] Testear diferenciación G-Q-L en diferentes ángulos
  - [ ] Testear diferenciación K-P-V en diferentes ángulos
  - [ ] Ajustar tolerancias según resultados

- [ ] **Testing de Layout Responsivo**
  - [ ] Verificar grid en diferentes resoluciones
  - [ ] Asegurar que tiles se adaptan correctamente
  - [ ] Verificar que el contenedor de categorías escala bien

- [ ] **Feedback de Usuarios**
  - [ ] Sesión de testing con usuarios nuevos
  - [ ] Recoger feedback sobre claridad de textos
  - [ ] Validar que el flujo es intuitivo
  - [ ] Ajustar según comentarios

---

## 🎨 Paleta de Colores Sugerida

### Colores Principales

```css
/* Fondos */
--background-dark: #1a1a1a
--background-medium: #2d2d2d
--background-light: #3d3d3d

/* Acentos */
--primary-blue: #00a0ff      /* Elementos completados */
--primary-cyan: #00d4ff      /* Hover states */
--accent-orange: #ffa726     /* Próximamente */
--accent-yellow: #ffd700     /* Elementos activos */

/* Estados */
--success-green: #4caf50
--warning-yellow: #ffeb3b
--error-red: #f44336

/* Texto */
--text-primary: #ffffff
--text-secondary: #b0b0b0
--text-disabled: #666666
```

### Uso de Colores

| Elemento | Color | Uso |
|----------|-------|-----|
| Tile Pendiente | `#333333` | Gris oscuro |
| Tile Completado | `#00a0ff` | Azul brillante |
| Botón Hover | `#00d4ff` | Cyan |
| Próximamente | `#ffa726` | Naranja |
| Feedback Correcto | `#4caf50` | Verde |
| Feedback Casi | `#ffeb3b` | Amarillo |
| Feedback Error | `#f44336` | Rojo |

---

## 📐 Especificaciones Tipográficas

### Fuentes

```css
/* Títulos */
font-family: 'Roboto Bold' o 'Arial Bold'
font-size: 36-48px (VR)
font-weight: 700
line-height: 1.2

/* Subtítulos */
font-size: 24-32px (VR)
font-weight: 600
line-height: 1.3

/* Cuerpo */
font-size: 18-24px (VR)
font-weight: 400
line-height: 1.5

/* Botones */
font-size: 20-28px (VR)
font-weight: 600
text-transform: uppercase (opcional)
```

### Jerarquía Visual

1. **Nivel 1 - Títulos de Escena**: 48px, Bold, Color primario
2. **Nivel 2 - Subtítulos**: 32px, Semi-bold, Color secundario
3. **Nivel 3 - Encabezados de Panel**: 28px, Semi-bold
4. **Nivel 4 - Cuerpo**: 24px, Regular
5. **Nivel 5 - Anotaciones**: 18px, Regular, Opacidad 0.8

---

## 🔄 Animaciones y Transiciones

### Transiciones Recomendadas

```csharp
// Cambio de color en tiles
Duration: 0.3s
Easing: EaseInOutQuad

// Aparición de paneles
Duration: 0.4s
Easing: EaseOutBack
Effect: Scale from 0.9 to 1.0 + Fade in

// Hover en botones
Duration: 0.15s
Easing: EaseOutQuad
Effect: Scale 1.0 to 1.05

// Cambio de escena
Duration: 0.5s
Effect: Fade to black + Fade in
```

### Feedback Háptico (Opcional)

- **Botón presionado**: Pulso corto (50ms)
- **Signo reconocido**: Pulso doble (100ms cada uno)
- **Nivel completado**: Pulso largo (200ms)

---

## 📚 Recursos Adicionales

### Iconos Sugeridos

- 🎓 Modo Aprendizaje
- 🌐 Modo Traducción
- 🔤 Alfabeto
- 🔢 Dígitos
- 🎨 Colores
- 🌱 Básico
- 🌿 Medio
- 🌳 Avanzado
- 👋 Bienvenida
- 🖐️ Interacción
- 🎯 Objetivo
- 📊 Estado
- ✓ Correcto
- ⚠️ Casi
- ⊘ Incorrecto
- 🔁 Repetir
- ◄ Anterior
- ► Siguiente
- ✋ Practicar

### Fuentes de Iconos

- **Material Icons**: https://fonts.google.com/icons
- **Font Awesome**: https://fontawesome.com/
- **Unity UI Icons**: Asset Store

---

## 🎯 Métricas de Éxito

### KPIs de UX

1. **Tiempo hasta primera interacción**: < 10 segundos
2. **Tasa de comprensión del flujo**: > 90% sin ayuda
3. **Tasa de error en navegación**: < 5%
4. **Tiempo de permanencia en onboarding**: 30-60 segundos
5. **Tasa de completado de categorías**: > 70%

### Testing de Usabilidad

- **Test con 5 usuarios nuevos**: ¿Entienden qué hacer?
- **Test de diferenciación de signos**: ¿Distinguen G-Q-L y K-P-V?
- **Test de navegación**: ¿Encuentran todas las funciones?
- **Test de feedback**: ¿Comprenden los mensajes de error/éxito?

---

## 📝 Notas de Implementación

### Prioridades

**Alta Prioridad:**
1. Textos de onboarding en Escena 1
2. Layout de categorías en Escena 2
3. Grid responsivo en autoevaluación
4. Hand shapes de dígitos faltantes

**Media Prioridad:**
5. Refinamiento de letras ambiguas
6. Panel de instrucciones lateral
7. Animaciones y transiciones
8. Iconografía completa

**Baja Prioridad:**
9. Feedback háptico
10. Visualización multi-ángulo avanzada
11. Estadísticas de progreso detalladas

### Consideraciones Técnicas

- **Performance en VR**: Mantener frame rate > 72 FPS
- **Legibilidad**: Textos suficientemente grandes para VR (mínimo 18px)
- **Contraste**: Ratio mínimo 4.5:1 para accesibilidad
- **Navegación**: Siempre ofrecer botón "Volver" claro
- **Feedback inmediato**: Respuesta visual en < 100ms

---

## 📄 Resumen Ejecutivo

Esta propuesta UI/UX para ASL Learn VR se centra en:

1. **Claridad**: Textos explicativos que eliminan ambigüedad
2. **Jerarquía Visual**: Estructura clara de paneles y flujo lógico
3. **Pedagogía**: Diseño orientado al aprendizaje progresivo
4. **Accesibilidad**: Interfaz intuitiva para usuarios sin experiencia previa
5. **Escalabilidad**: Layouts responsivos adaptables a futuros contenidos

### Beneficios Esperados

- ✅ Reducción del tiempo de onboarding
- ✅ Mayor retención de usuarios
- ✅ Menor fricción cognitiva en la navegación
- ✅ Mejor diferenciación de signos complejos
- ✅ Experiencia de aprendizaje más efectiva

---

**Documento generado por**: Claude Code (UI/UX Design Agent)
**Fecha**: 2025-12-16
**Versión**: 1.0
**Estado**: Propuesta para revisión
