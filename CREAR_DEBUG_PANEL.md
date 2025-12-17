# CREAR PANEL DE DEBUG EN VR - 1 MINUTO

## PASOS EXACTOS:

1. **Abre la escena 03_LearningModule en Unity**

2. **Crea un Canvas:**
   - Click derecho en la jerarquía > UI > Canvas
   - Nómbralo "DebugCanvas"

3. **Configura el Canvas:**
   - En el Inspector, cambia "Render Mode" a **World Space**
   - Scale: 0.001, 0.001, 0.001
   - Position: 0, 2, 3 (delante de ti en VR)
   - Width: 2000
   - Height: 1000

4. **Crea el texto:**
   - Click derecho en DebugCanvas > UI > Text - TextMeshPro
   - Nómbralo "DebugText"

5. **Configura DebugText:**
   - Rect Transform > Anchor: Stretch (arriba izquierda + Shift)
   - Left: 20, Top: -20, Right: -20, Bottom: 20
   - Font Size: 24
   - Color: White
   - Alignment: Top Left
   - Wrapping: Enabled
   - Overflow: Overflow

6. **Añade el componente DebugLogger:**
   - Selecciona DebugCanvas
   - Add Component > DebugLogger
   - Arrastra "DebugText" al campo "Debug Text"
   - Max Lines: 20

7. **GUARDA LA ESCENA (Ctrl+S)**

8. **PRUEBA EN VR:**
   - Ahora verás TODOS los logs en pantalla delante de ti en VR
   - Los errores en ROJO
   - Las advertencias en AMARILLO
   - Los logs normales en BLANCO

LISTO. Ahora cuando presiones Practice verás EXACTAMENTE qué está pasando en pantalla.
