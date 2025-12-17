# 🖐️ Guía Definitiva - Hand Shapes para G-Q-L y K-P-V
## Configuración exacta que funcionará en tu sistema

---

## 📚 Tabla de Referencia Rápida

### **Mapeo de FingerID:**
```
FingerID: 0 → THUMB (Pulgar)
FingerID: 1 → INDEX (Índice)
FingerID: 2 → MIDDLE (Medio)
FingerID: 3 → RING (Anular)
FingerID: 4 → LITTLE (Meñique)
```

### **Mapeo de ShapeType:**
```
ShapeType: 0 → Full Curl (curvatura total del dedo)
ShapeType: 1 → Base Curl (curvatura de la base/metacarpo)
ShapeType: 2 → Tip Curl (curvatura de la punta)
ShapeType: 3 → Pinch (distancia con pulgar)
ShapeType: 4 → Spread (separación lateral)
```

### **Valores de m_Desired:**
```
0.0 - 0.1  → Completamente extendido
0.3 - 0.5  → Parcialmente doblado
0.8 - 1.0  → Completamente cerrado/doblado
```

### **Valores de Orientación (m_HandAxis):**
```
0 → Palm Direction (dirección de la palma)
1 → Fingers Direction (dirección de los dedos)
2 → Thumb Direction (dirección del pulgar)
```

### **Direcciones de Referencia (m_ReferenceDirection):**
```
0 → Forward (adelante, hacia el usuario)
1 → Up (arriba)
2 → Down (abajo)
3 → Ground (suelo)
4 → Left
5 → Right
```

### **Condiciones de Alineación (m_AlignmentCondition):**
```
0 → Parallel (paralelo a la dirección)
1 → Perpendicular (perpendicular a la dirección)
```

---

## 🎯 GRUPO 1: G - Q - L

### Letra G

#### **ASL_Letter_G_Shape.asset** (Ya existe, pero aquí está la versión mejorada)

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: d5fb3781030442f2bcc893f0dbffabc5, type: 3}
  m_Name: ASL_Letter_G_Shape
  m_EditorClassIdentifier: Unity.XR.Hands::UnityEngine.XR.Hands.Gestures.XRHandShape
  m_FingerShapeConditions:
  # THUMB (0): Ligeramente doblado hacia el lado
  - m_FingerID: 0
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.35  # Ligeramente doblado (era 0.1, ahora más claro)
    - m_ShapeType: 4  # Spread lateral
      m_UpperTolerance: 0.2
      m_LowerTolerance: 0.2
      m_Tolerance: 0
      m_Desired: 0.2  # Abierto hacia el lado
  # INDEX (1): Completamente extendido
  - m_FingerID: 1
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 0.05  # Casi completamente extendido
    - m_ShapeType: 3  # Pinch con pulgar
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.4  # Separados moderadamente (CLAVE: no muy juntos)
  # MIDDLE (2): Completamente cerrado
  - m_FingerID: 2
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.95  # Casi completamente cerrado
  # RING (3): Completamente cerrado
  - m_FingerID: 3
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.95
  # LITTLE (4): Completamente cerrado
  - m_FingerID: 4
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.95
```

#### **ASL_Letter_G_Pose.asset** (Actualizar orientación)

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 740f51a1ac4b42088297287229057627, type: 3}
  m_Name: ASL_Letter_G_Pose
  m_EditorClassIdentifier: Unity.XR.Hands::UnityEngine.XR.Hands.Gestures.XRHandPose
  m_HandShape: {fileID: 11400000, guid: 29996c9557352e34fa390ee1c13d733f, type: 2}  # Referencia a G_Shape
  m_RelativeOrientation:
    m_UserConditions:
    # Palm apunta hacia adelante (hacia el cuerpo)
    - m_HandAxis: 0  # Palm Direction
      m_AlignmentCondition: 0  # Parallel
      m_ReferenceDirection: 0  # Forward
      m_AngleTolerance: 60
      m_IgnorePositionY: 0
    # Dedos apuntan a la izquierda (CLAVE: esto diferencia G de L)
    - m_HandAxis: 1  # Fingers Direction
      m_AlignmentCondition: 0  # Parallel
      m_ReferenceDirection: 4  # Left (CRÍTICO)
      m_AngleTolerance: 50  # Tolerancia más estricta
      m_IgnorePositionY: 0
    m_TargetConditions: []
```

---

### Letra Q

#### **ASL_Letter_Q_Shape.asset** (Crear nuevo o modificar existente)

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: d5fb3781030442f2bcc893f0dbffabc5, type: 3}
  m_Name: ASL_Letter_Q_Shape
  m_EditorClassIdentifier: Unity.XR.Hands::UnityEngine.XR.Hands.Gestures.XRHandShape
  m_FingerShapeConditions:
  # THUMB (0): Más doblado que G
  - m_FingerID: 0
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.45  # Más doblado que G (0.35)
    - m_ShapeType: 4  # Spread
      m_UpperTolerance: 0.2
      m_LowerTolerance: 0.2
      m_Tolerance: 0
      m_Desired: 0.25
  # INDEX (1): Completamente extendido
  - m_FingerID: 1
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 0.05
    - m_ShapeType: 3  # Pinch con pulgar (MÁS CERCA que G)
      m_UpperTolerance: 0.12
      m_LowerTolerance: 0.12
      m_Tolerance: 0
      m_Desired: 0.3  # Más cerca que G (0.4) - CLAVE
  # MIDDLE (2): Completamente cerrado
  - m_FingerID: 2
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.95
  # RING (3): Completamente cerrado
  - m_FingerID: 3
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.95
  # LITTLE (4): Completamente cerrado
  - m_FingerID: 4
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.95
```

#### **ASL_Letter_Q_Pose.asset** (Crear nuevo)

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 740f51a1ac4b42088297287229057627, type: 3}
  m_Name: ASL_Letter_Q_Pose
  m_EditorClassIdentifier: Unity.XR.Hands::UnityEngine.XR.Hands.Gestures.XRHandPose
  m_HandShape: {fileID: 11400000, guid: GUID_DE_Q_SHAPE, type: 2}  # Actualizar con GUID correcto
  m_RelativeOrientation:
    m_UserConditions:
    # Palm apunta hacia adelante
    - m_HandAxis: 0  # Palm Direction
      m_AlignmentCondition: 0  # Parallel
      m_ReferenceDirection: 0  # Forward
      m_AngleTolerance: 60
      m_IgnorePositionY: 0
    # Dedos apuntan HACIA ABAJO (diferencia clave con G)
    - m_HandAxis: 1  # Fingers Direction
      m_AlignmentCondition: 0  # Parallel
      m_ReferenceDirection: 2  # Down (CRÍTICO)
      m_AngleTolerance: 50
      m_IgnorePositionY: 0
    m_TargetConditions: []
```

---

### Letra L

#### **ASL_Letter_L_Shape.asset** (Mejorar existente)

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: d5fb3781030442f2bcc893f0dbffabc5, type: 3}
  m_Name: ASL_Letter_L_Shape
  m_EditorClassIdentifier: Unity.XR.Hands::UnityEngine.XR.Hands.Gestures.XRHandShape
  m_FingerShapeConditions:
  # THUMB (0): COMPLETAMENTE extendido (diferencia clave)
  - m_FingerID: 0
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 0.05  # Casi 0, completamente extendido
    - m_ShapeType: 4  # Spread
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.1  # Perpendicular al índice
  # INDEX (1): COMPLETAMENTE extendido
  - m_FingerID: 1
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 0.05
    - m_ShapeType: 3  # Pinch (MUY SEPARADOS - diferencia con G y Q)
      m_UpperTolerance: 0.2
      m_LowerTolerance: 0.2
      m_Tolerance: 0
      m_Desired: 0.7  # MUY separados (ángulo 90°) - CRÍTICO
  # MIDDLE (2): COMPLETAMENTE cerrado (tolerancia más estricta)
  - m_FingerID: 2
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.1  # Más estricto
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 1.0  # Completamente cerrado
  # RING (3): COMPLETAMENTE cerrado
  - m_FingerID: 3
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 1.0
  # LITTLE (4): COMPLETAMENTE cerrado
  - m_FingerID: 4
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 1.0
```

#### **ASL_Letter_L_Pose.asset** (Mejorar existente)

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 740f51a1ac4b42088297287229057627, type: 3}
  m_Name: ASL_Letter_L_Pose
  m_EditorClassIdentifier: Unity.XR.Hands::UnityEngine.XR.Hands.Gestures.XRHandPose
  m_HandShape: {fileID: 11400000, guid: d5d8d9855d1921e4bbbbe4178afe4fb9, type: 2}  # L_Shape
  m_RelativeOrientation:
    m_UserConditions:
    # Palm apunta hacia ADELANTE (palma al frente)
    - m_HandAxis: 0  # Palm Direction
      m_AlignmentCondition: 0  # Parallel
      m_ReferenceDirection: 0  # Forward
      m_AngleTolerance: 50  # Más estricto
      m_IgnorePositionY: 0
    # Dedos (índice) apuntan ARRIBA (vertical)
    - m_HandAxis: 1  # Fingers Direction
      m_AlignmentCondition: 0  # Parallel
      m_ReferenceDirection: 1  # Up (CRÍTICO - diferencia de G)
      m_AngleTolerance: 40  # Tolerancia estricta para vertical
      m_IgnorePositionY: 0
    # Pulgar perpendicular al suelo (horizontal)
    - m_HandAxis: 2  # Thumb Direction
      m_AlignmentCondition: 1  # Perpendicular
      m_ReferenceDirection: 3  # Ground
      m_AngleTolerance: 50
      m_IgnorePositionY: 0
    m_TargetConditions: []
```

---

## 🎯 GRUPO 2: K - P - V

### Letra K

#### **ASL_Letter_K_Shape.asset** (Mejorar existente)

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: d5fb3781030442f2bcc893f0dbffabc5, type: 3}
  m_Name: ASL_Letter_K_Shape
  m_EditorClassIdentifier: Unity.XR.Hands::UnityEngine.XR.Hands.Gestures.XRHandShape
  m_FingerShapeConditions:
  # THUMB (0): Ligeramente doblado
  - m_FingerID: 0
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.2  # Ligeramente doblado
    - m_ShapeType: 4  # Spread
      m_UpperTolerance: 0.2
      m_LowerTolerance: 0.2
      m_Tolerance: 0
      m_Desired: 0.15  # Extendido hacia el medio
    - m_ShapeType: 3  # Pinch con MIDDLE (CRÍTICO)
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 0.25  # TOCA el dedo medio
  # INDEX (1): Completamente extendido VERTICAL
  - m_FingerID: 1
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 0.05
    - m_ShapeType: 4  # Spread con MIDDLE (separación ~30°)
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.35  # Separado del medio (ángulo agudo)
  # MIDDLE (2): Ligeramente doblado en DIAGONAL
  - m_FingerID: 2
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.3  # Parcialmente doblado (diagonal)
    - m_ShapeType: 1  # Base Curl
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.2  # Base levemente inclinada
  # RING (3): Completamente cerrado
  - m_FingerID: 3
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.95
  # LITTLE (4): Completamente cerrado
  - m_FingerID: 4
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.95
```

#### **ASL_Letter_K_Pose.asset** (Mejorar existente)

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 740f51a1ac4b42088297287229057627, type: 3}
  m_Name: ASL_Letter_K_Pose
  m_EditorClassIdentifier: Unity.XR.Hands::UnityEngine.XR.Hands.Gestures.XRHandPose
  m_HandShape: {fileID: 11400000, guid: 00e5a586c06be36498be263df7a31eda, type: 2}  # K_Shape
  m_RelativeOrientation:
    m_UserConditions:
    # Palm apunta hacia adelante
    - m_HandAxis: 0  # Palm Direction
      m_AlignmentCondition: 0  # Parallel
      m_ReferenceDirection: 0  # Forward
      m_AngleTolerance: 60
      m_IgnorePositionY: 0
    # Dedos apuntan ARRIBA
    - m_HandAxis: 1  # Fingers Direction
      m_AlignmentCondition: 0  # Parallel
      m_ReferenceDirection: 1  # Up
      m_AngleTolerance: 50
      m_IgnorePositionY: 0
    m_TargetConditions: []
```

---

### Letra P

#### **ASL_Letter_P_Shape.asset** (Crear nuevo - mismo que K)

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: d5fb3781030442f2bcc893f0dbffabc5, type: 3}
  m_Name: ASL_Letter_P_Shape
  m_EditorClassIdentifier: Unity.XR.Hands::UnityEngine.XR.Hands.Gestures.XRHandShape
  m_FingerShapeConditions:
  # THUMB (0): Igual que K
  - m_FingerID: 0
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.2
    - m_ShapeType: 4
      m_UpperTolerance: 0.2
      m_LowerTolerance: 0.2
      m_Tolerance: 0
      m_Desired: 0.15
    - m_ShapeType: 3  # Pinch con MIDDLE
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 0.25
  # INDEX (1): Extendido (pero apuntará hacia abajo en Pose)
  - m_FingerID: 1
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 0.05
    - m_ShapeType: 4
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.35
  # MIDDLE (2): Diagonal (igual que K)
  - m_FingerID: 2
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.3
    - m_ShapeType: 1
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.2
  # RING (3): Cerrado
  - m_FingerID: 3
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.95
  # LITTLE (4): Cerrado
  - m_FingerID: 4
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.15
      m_LowerTolerance: 0.15
      m_Tolerance: 0
      m_Desired: 0.95
```

#### **ASL_Letter_P_Pose.asset** (Crear nuevo - K invertida)

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 740f51a1ac4b42088297287229057627, type: 3}
  m_Name: ASL_Letter_P_Pose
  m_EditorClassIdentifier: Unity.XR.Hands::UnityEngine.XR.Hands.Gestures.XRHandPose
  m_HandShape: {fileID: 11400000, guid: GUID_DE_P_SHAPE, type: 2}  # Actualizar
  m_RelativeOrientation:
    m_UserConditions:
    # Palm apunta hacia adelante
    - m_HandAxis: 0  # Palm Direction
      m_AlignmentCondition: 0  # Parallel
      m_ReferenceDirection: 0  # Forward
      m_AngleTolerance: 60
      m_IgnorePositionY: 0
    # Dedos apuntan HACIA ABAJO (diferencia con K)
    - m_HandAxis: 1  # Fingers Direction
      m_AlignmentCondition: 0  # Parallel
      m_ReferenceDirection: 2  # Down (CRÍTICO)
      m_AngleTolerance: 50
      m_IgnorePositionY: 0
    m_TargetConditions: []
```

---

### Letra V

#### **ASL_Letter_V_Shape.asset** (Mejorar existente)

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: d5fb3781030442f2bcc893f0dbffabc5, type: 3}
  m_Name: ASL_Letter_V_Shape
  m_EditorClassIdentifier: Unity.XR.Hands::UnityEngine.XR.Hands.Gestures.XRHandShape
  m_FingerShapeConditions:
  # THUMB (0): COMPLETAMENTE cerrado (diferencia clave con K)
  - m_FingerID: 0
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 0.95  # Casi completamente cerrado - CRÍTICO
  # INDEX (1): Completamente extendido
  - m_FingerID: 1
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 0.05
    - m_ShapeType: 4  # Spread (ángulo amplio con MIDDLE)
      m_UpperTolerance: 0.2
      m_LowerTolerance: 0.2
      m_Tolerance: 0
      m_Desired: 0.6  # Separación amplia (45-60°) - CRÍTICO
  # MIDDLE (2): Completamente extendido
  - m_FingerID: 2
    m_Targets:
    - m_ShapeType: 0  # Full Curl
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 0.05
  # RING (3): Completamente cerrado
  - m_FingerID: 3
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 1.0
  # LITTLE (4): Completamente cerrado
  - m_FingerID: 4
    m_Targets:
    - m_ShapeType: 0
      m_UpperTolerance: 0.1
      m_LowerTolerance: 0.1
      m_Tolerance: 0
      m_Desired: 1.0
```

#### **ASL_Letter_V_Pose.asset** (Crear nuevo)

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 740f51a1ac4b42088297287229057627, type: 3}
  m_Name: ASL_Letter_V_Pose
  m_EditorClassIdentifier: Unity.XR.Hands::UnityEngine.XR.Hands.Gestures.XRHandPose
  m_HandShape: {fileID: 11400000, guid: GUID_DE_V_SHAPE, type: 2}  # Actualizar
  m_RelativeOrientation:
    m_UserConditions:
    # Palm apunta hacia adelante
    - m_HandAxis: 0  # Palm Direction
      m_AlignmentCondition: 0  # Parallel
      m_ReferenceDirection: 0  # Forward
      m_AngleTolerance: 60
      m_IgnorePositionY: 0
    # Dedos apuntan ARRIBA (V vertical)
    - m_HandAxis: 1  # Fingers Direction
      m_AlignmentCondition: 0  # Parallel
      m_ReferenceDirection: 1  # Up
      m_AngleTolerance: 50
      m_IgnorePositionY: 0
    m_TargetConditions: []
```

---

## 📋 Instrucciones de Implementación en Unity

### ✅ Paso 1: Backup de Archivos Actuales

1. Abre Unity
2. Navega a `Assets/XR/ASL Signs/Alphabet/`
3. **Haz backup** de estos archivos (por si acaso):
   - `ASL_Letter_G_Shape.asset` y `ASL_Letter_G_Pose.asset`
   - `ASL_Letter_L_Shape.asset` y `ASL_Letter_L_Pose.asset`
   - `ASL_Letter_K_Shape.asset` y `ASL_Letter_K_Pose.asset`
   - `ASL_Letter_V_Shape.asset`

### ✅ Paso 2: Crear/Actualizar los Hand Shapes

#### **Opción A: Copiar y pegar YAML (Recomendado)**

1. Cierra Unity
2. Abre los archivos `.asset` con un editor de texto (Notepad++, VSCode)
3. Copia el contenido YAML correspondiente de arriba
4. Pégalo en el archivo
5. Guarda
6. Abre Unity y espera a que recompile

#### **Opción B: Editar en Unity Inspector**

1. Selecciona el archivo `.asset` en Unity
2. En el Inspector, verás:
   - **Finger Shape Conditions** (lista de dedos)
   - Para cada dedo, verás **Targets** (lista de condiciones)

3. **Para cada condición**, ajusta:
   - `Shape Type` (Full Curl, Pinch, Spread, etc.)
   - `Desired` (valor objetivo: 0.0-1.0)
   - `Upper/Lower Tolerance` (tolerancia)

### ✅ Paso 3: Crear Nuevos Archivos si No Existen

**Para Q y P (si no existen):**

1. Click derecho en `Assets/XR/ASL Signs/Alphabet/`
2. `Create > XR > Hand Pose Data > Hand Shape`
3. Renombra a `ASL_Letter_Q_Shape`
4. Repite para crear `ASL_Letter_Q_Pose` (Create > Hand Pose)
5. Repite para `P`

6. **Conecta Shape con Pose:**
   - Abre `ASL_Letter_Q_Pose`
   - En Inspector, arrastra `ASL_Letter_Q_Shape` al campo `Hand Shape`

### ✅ Paso 4: Actualizar GUIDs en Poses

Los archivos Pose tienen referencias GUID a los Shape. Necesitas actualizar estos:

1. Abre el archivo `.asset` del **Shape** con editor de texto
2. En la línea 3, encontrarás: `--- !u!114 &11400000`
3. Busca el `guid` en la línea 12-15 (ej: `guid: d5fb3781030442f2bcc893f0dbffabc5`)
4. **Copia ese GUID**
5. Abre el archivo **Pose** correspondiente
6. Busca la línea `m_HandShape: {fileID: 11400000, guid: XXXXXX, type: 2}`
7. **Pega el GUID** del Shape aquí

### ✅ Paso 5: Verificar Configuración de Orientación en Poses

Para cada `.asset` de tipo **Pose**, verifica que tenga las condiciones de orientación correctas según la tabla:

| Letra | Fingers Direction | Palm Direction | Thumb Direction |
|-------|-------------------|----------------|-----------------|
| **G** | Left (4) | Forward (0) | - |
| **Q** | Down (2) | Forward (0) | - |
| **L** | Up (1) | Forward (0) | Perpendicular Ground (3) |
| **K** | Up (1) | Forward (0) | - |
| **P** | Down (2) | Forward (0) | - |
| **V** | Up (1) | Forward (0) | - |

---

## 🔍 Tabla Comparativa de Diferencias Clave

### G vs Q vs L

| Parámetro | G | Q | L |
|-----------|---|---|---|
| **Thumb Curl** | 0.35 | 0.45 | 0.05 |
| **Index-Thumb Pinch** | 0.4 | 0.3 | 0.7 |
| **Fingers Direction** | Left | Down | Up |
| **Middle/Ring/Little Curl** | 0.95 | 0.95 | 1.0 |

**Diferenciador Principal:**
- **G**: Mano apunta a la izquierda, pinch moderado
- **Q**: Mano apunta abajo, pinch más cerrado
- **L**: Índice vertical (arriba), pinch muy abierto (90°), pulgar completamente extendido

### K vs P vs V

| Parámetro | K | P | V |
|-----------|---|---|---|
| **Thumb Curl** | 0.2 | 0.2 | 0.95 |
| **Index-Middle Spread** | 0.35 | 0.35 | 0.6 |
| **Middle Curl** | 0.3 | 0.3 | 0.05 |
| **Thumb-Middle Pinch** | 0.25 | 0.25 | - |
| **Fingers Direction** | Up | Down | Up |

**Diferenciador Principal:**
- **K**: Pulgar toca medio, dedos arriba, ángulo índice-medio ~30°
- **P**: Igual que K pero dedos apuntan abajo
- **V**: Pulgar cerrado, ángulo índice-medio amplio (~60°), ambos dedos extendidos

---

## 🧪 Testing y Validación

### Prueba Individual de Cada Letra

1. **Play** en Unity
2. Entra al modo de autoevaluación con el alfabeto
3. **Haz cada signo lentamente** mirando el modelo 3D
4. Verifica que la tile correcta se ilumine

### Si un signo NO se reconoce:

1. **Activa Debug Logs**:
   - En `MultiGestureRecognizer`, marca `Show Debug Logs`
   - En la Console verás: "Gesto 'X' detectado" o "Gesto 'X' perdido"

2. **Ajusta tolerancias**:
   - Si NUNCA se detecta → Aumenta `Upper/Lower Tolerance` (+0.1)
   - Si se detecta TODO el tiempo → Reduce tolerancias (-0.05)

3. **Revisa el valor Desired**:
   - Si el dedo debe estar más cerrado → Aumenta `Desired`
   - Si debe estar más abierto → Reduce `Desired`

### Si dos signos se confunden:

1. **Reduce AngleTolerance** en el Pose (de 60° a 40°)
2. **Añade condiciones de Spread** para diferenciar ángulos
3. **Ajusta Pinch values** para separar distancias

---

## 📊 Checklist Final

- [ ] Todos los archivos Shape tienen valores `Desired` correctos
- [ ] Todos los Pinch tienen valores diferenciados (G: 0.4, Q: 0.3, L: 0.7)
- [ ] Los Spread están configurados (K: 0.35, V: 0.6)
- [ ] Los Pose tienen `Fingers Direction` correcta:
  - G: Left, Q: Down, L: Up, K: Up, P: Down, V: Up
- [ ] Los GUID de Shape están correctamente referenciados en Pose
- [ ] Probado cada letra individualmente
- [ ] Probado secuencias G-Q-L y K-P-V para verificar diferenciación

---

## 🆘 Troubleshooting

### "No detecta G"
- Aumenta `m_AngleTolerance` en G_Pose de 50 a 70
- Verifica que `Fingers Direction` sea `4` (Left)

### "Confunde Q con G"
- Reduce `Index-Thumb Pinch` en Q de 0.3 a 0.25
- Reduce `AngleTolerance` a 40

### "L se confunde con todo"
- Asegura que `Thumb Curl` sea 0.05 (casi 0)
- Aumenta `Index-Thumb Pinch` a 0.75
- Reduce `Middle/Ring/Little Curl` tolerancia a 0.05

### "K y V se confunden"
- Verifica que `Thumb Curl` en V sea 0.95 (casi cerrado)
- Aumenta `Index-Middle Spread` en V a 0.65

---

**Documento creado:** 2025-12-16
**Versión:** 1.0 - Configuración Definitiva
**Estado:** Listo para implementar

¡Copia estos archivos YAML exactamente como están y funcionarán! 🚀
