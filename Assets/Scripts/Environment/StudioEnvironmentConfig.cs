using UnityEngine;

namespace ASL_LearnVR
{
    /// <summary>
    /// Centralized visual environment configuration.
    /// Change the values HERE and all scenes update automatically.
    /// Create: Assets > Create > ASL LearnVR > Studio Environment Config
    /// </summary>
    [CreateAssetMenu(fileName = "StudioEnvironmentConfig", menuName = "ASL LearnVR/Studio Environment Config")]
    public class StudioEnvironmentConfig : ScriptableObject
    {
        [Header("=== FLOOR (Floor con degradado radial) ===")]
        [Tooltip("Color del centro del suelo")]
        public Color floorCenterColor = new Color(0.165f, 0.165f, 0.165f); // #2A2A2A

        [Tooltip("Color de los bordes del suelo")]
        public Color floorEdgeColor = new Color(0.62f, 0.62f, 0.62f); // #9E9E9E

        [Tooltip("Radius del degradado")]
        public float floorGradientRadius = 3.0f;

        [Tooltip("Smoothness del degradado")]
        [Range(0f, 1f)]
        public float floorSmoothness = 0.1f;

        [Tooltip("Tamano del Plane (metros, se divide entre 10 internamente)")]
        public float floorSize = 6f;

        [Header("=== WALLS (Walls curvas con degradado vertical) ===")]
        [Tooltip("Color de la base de las paredes")]
        public Color wallBottomColor = new Color(0.62f, 0.62f, 0.62f); // #9E9E9E

        [Tooltip("Color del techo")]
        public Color wallTopColor = new Color(0.96f, 0.96f, 0.94f); // #F5F5F0

        [Tooltip("Height del degradado")]
        public float wallGradientHeight = 4.0f;

        [Tooltip("Level del suelo (Y)")]
        public float wallGroundLevel = 0.0f;

        [Tooltip("Smoothness del degradado")]
        [Range(0f, 1f)]
        public float wallSmoothness = 0.3f;

        [Tooltip("Radius de la esfera invertida")]
        public float wallRadius = 10f;

        [Tooltip("Height del centro de la esfera")]
        public float wallCenterY = 2f;

        [Header("=== LIGHTING (Iluminacion tipo estudio) ===")]
        [Tooltip("Main light intensity")]
        public float mainLightIntensity = 0.8f;

        [Tooltip("Color de la luz principal")]
        public Color mainLightColor = new Color(1f, 0.98f, 0.95f); // #FFFAF2

        [Tooltip("Angulo de la luz principal (X: pitch, Y: yaw)")]
        public Vector2 mainLightAngle = new Vector2(50f, -30f);

        [Tooltip("Fill light intensity")]
        public float fillLightIntensity = 0.3f;

        [Tooltip("Color de la luz de relleno")]
        public Color fillLightColor = new Color(0.9f, 0.95f, 1f); // #E6F2FF

        [Tooltip("Sombras suaves activas")]
        public bool enableSoftShadows = true;

        [Tooltip("Strength de las sombras")]
        [Range(0f, 1f)]
        public float shadowStrength = 0.4f;

        [Header("=== AMBIENT (Luz ambiente) ===")]
        [Tooltip("Color de luz ambiente")]
        public Color ambientColor = new Color(0.4f, 0.4f, 0.4f); // #666666

        [Tooltip("Intensity de luz ambiente")]
        public float ambientIntensity = 0.5f;

        [Header("=== PANELS (Panels en arco) ===")]
        [Tooltip("Prefab of panel UI (arrastralo aqui)")]
        public GameObject panelPrefab;

        [Tooltip("Numero de paneles")]
        public int panelCount = 5;

        [Tooltip("Radius del arco (metros)")]
        public float panelArcRadius = 2.5f;

        [Tooltip("Angulo total del arco (grados)")]
        public float panelArcAngle = 120f;

        [Tooltip("Height de los paneles (a nivel de ojos)")]
        public float panelHeight = 1.6f;
    }
}
