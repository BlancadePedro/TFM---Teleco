using UnityEngine;
using UnityEngine.SceneManagement;

namespace ASL_LearnVR
{
    /// <summary>
    /// Versión 2 del StudioEnvironmentManager con ciclorama tipo caja,
    /// iluminación limpia (una sola fuente directional + 2 spots laterales),
    /// y sin conflictos con StudioLightingManager.
    ///
    /// MIGRACIÓN:
    ///   1. Sustituye StudioEnvironmentManager.cs por este archivo.
    ///   2. Elimina el componente StudioLightingManager de TODAS las escenas.
    ///   3. Asigna el nuevo StudioEnvironmentConfig (o actualiza los valores por defecto abajo).
    /// </summary>
    public class StudioEnvironmentManager : MonoBehaviour
    {
        // ─── Singleton ───────────────────────────────────────────────────
        private static StudioEnvironmentManager _instance;
        public static StudioEnvironmentManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<StudioEnvironmentManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("StudioEnvironmentManager");
                        _instance = go.AddComponent<StudioEnvironmentManager>();
                    }
                }
                return _instance;
            }
        }

        // ─── Inspector ───────────────────────────────────────────────────
        [Header("Configuration")]
        [SerializeField] private StudioEnvironmentConfig config;

        [Header("Shaders")]
        [Tooltip("Shader: ASL_LearnVR/CycloramaRoom (nuevo ciclorama tipo caja)")]
        [SerializeField] private Shader cycloramaShader;
        [Tooltip("Shader: ASL_LearnVR/RadialGradientFloor (suelo extra, opcional)")]
        [SerializeField] private Shader floorShader;

        [Header("Options")]
        [SerializeField] private string[] excludedScenes = new string[0];
        [SerializeField] private bool enablePanels = false; // desactivado por defecto; se controla por escena

        // ─── Cyclorama params (se pueden sobreescribir con config) ────────
        [Header("Cyclorama — Box Room")]
        [Tooltip("Semitamaño de la caja en X/Z (metros)")]
        [SerializeField] private float roomHalfSize   = 6f;
        [SerializeField] private float roomHeight      = 4f;
        [Tooltip("Blend de la curva en la unión suelo-pared")]
        [SerializeField] [Range(0.01f, 1.5f)] private float sweepBlend    = 0.55f;
        [SerializeField] [Range(0.01f, 1.0f)] private float sweepSoftness = 0.40f;
        [Tooltip("Radio del degradado radial en el suelo (metros)")]
        [SerializeField] private float floorVignette  = 2.8f;

        // ─── Colores del ciclorama ────────────────────────────────────────
        [Header("Cyclorama Colors")]
        [SerializeField] private Color floorColor      = new Color(0.925f, 0.925f, 0.910f);
        [SerializeField] private Color floorDarkEdge   = new Color(0.720f, 0.720f, 0.710f);
        [SerializeField] private Color wallColor       = new Color(0.960f, 0.960f, 0.950f);
        [SerializeField] private Color ceilingColor    = new Color(0.980f, 0.980f, 0.970f);

        // ─── Iluminación ─────────────────────────────────────────────────
        [Header("Lighting")]
        [SerializeField] private Color   mainLightColor     = new Color(1.00f, 0.97f, 0.93f);
        [SerializeField] private float   mainLightIntensity = 0.65f;
        [SerializeField] private Vector2 mainLightAngle     = new Vector2(48f, -25f);

        [SerializeField] private Color   fillLightColor     = new Color(0.90f, 0.95f, 1.00f);
        [SerializeField] private float   fillLightIntensity = 0.18f;

        // Spots laterales (simulan focos de estudio)
        [SerializeField] private bool  enableSpotLights     = true;
        [SerializeField] private Color spotLeftColor        = new Color(1.00f, 0.97f, 0.93f); // cálido
        [SerializeField] private float spotLeftIntensity    = 1.4f;
        [SerializeField] private Color spotRightColor       = new Color(0.93f, 0.96f, 1.00f); // frío
        [SerializeField] private float spotRightIntensity   = 1.1f;
        [SerializeField] private float spotAngle            = 42f;
        [SerializeField] private float spotRange            = 9f;

        [SerializeField] private Color  ambientColor        = new Color(0.72f, 0.72f, 0.72f);
        [SerializeField] [Range(0f,1f)] private float shadowStrength = 0.22f;

        // ─── Runtime refs ────────────────────────────────────────────────
        private GameObject _roomObject;
        private GameObject _lightingRoot;
        private Material   _cycloramaMat;

        // ─────────────────────────────────────────────────────────────────
        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            CreateCycloramaMaterial();
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var ex in excludedScenes)
                if (scene.name == ex) return;

            BuildEnvironment();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Build
        // ─────────────────────────────────────────────────────────────────
        public void BuildEnvironment()
        {
            Cleanup();
            CreateCyclorama();
            SetupLighting();
            SetupAmbient();

            if (enablePanels)
                CreatePanelManager();

            Debug.Log($"[StudioEnvironmentManager v2] Ambiente creado: {SceneManager.GetActiveScene().name}");
        }

        private void Cleanup()
        {
            DestroyNamed("StudioRoom_Auto");
            DestroyNamed("StudioFloor_Auto");
            DestroyNamed("StudioWalls_Auto");
            DestroyNamed("StudioLighting_Auto");
            DestroyNamed("StudioPanels_Auto");
        }

        // ─────────────────────────────────────────────────────────────────
        //  Ciclorama — cubo simple (interior visible)
        // ─────────────────────────────────────────────────────────────────
        private void CreateCycloramaMaterial()
        {
            if (cycloramaShader == null)
                cycloramaShader = Shader.Find("ASL_LearnVR/CycloramaRoom");

            if (cycloramaShader == null)
            {
                Debug.LogError("[StudioEnvironmentManager] Shader 'ASL_LearnVR/CycloramaRoom' no encontrado.");
                return;
            }

            _cycloramaMat = new Material(cycloramaShader) { name = "Cyclorama_Runtime" };
            ApplyCycloramaColors();
        }

        private void ApplyCycloramaColors()
        {
            if (_cycloramaMat == null) return;
            _cycloramaMat.SetColor("_FloorColor",      floorColor);
            _cycloramaMat.SetColor("_FloorDarkColor",  floorDarkEdge);
            _cycloramaMat.SetColor("_WallColor",       wallColor);
            _cycloramaMat.SetColor("_CeilingColor",    ceilingColor);
            _cycloramaMat.SetFloat("_SweepBlend",      sweepBlend);
            _cycloramaMat.SetFloat("_SweepSoftness",   sweepSoftness);
            _cycloramaMat.SetFloat("_FloorVignette",   floorVignette);
            _cycloramaMat.SetFloat("_FloorY",          0f);
            _cycloramaMat.SetFloat("_CeilingY",        roomHeight);
        }

        private void CreateCyclorama()
        {
            // Un cubo con Cull Front = vemos el interior
            _roomObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _roomObject.name = "StudioRoom_Auto";
            _roomObject.transform.position   = new Vector3(0, roomHeight * 0.5f, 0);
            _roomObject.transform.localScale  = new Vector3(
                roomHalfSize * 2f,
                roomHeight,
                roomHalfSize * 2f
            );

            // Quitar collider
            var col = _roomObject.GetComponent<Collider>();
            if (col != null) Destroy(col);

            if (_cycloramaMat != null)
                _roomObject.GetComponent<Renderer>().sharedMaterial = _cycloramaMat;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Iluminación — sistema limpio de 3 luces
        // ─────────────────────────────────────────────────────────────────
        private void SetupLighting()
        {
            _lightingRoot      = new GameObject("StudioLighting_Auto");

            // 1. Desactivar TODAS las luces que no sean las nuestras
            KillForeignLights();

            // 2. Luz principal
            var mainGO  = new GameObject("MainLight");
            mainGO.transform.SetParent(_lightingRoot.transform);
            var main    = mainGO.AddComponent<Light>();
            main.type       = LightType.Directional;
            main.color      = mainLightColor;
            main.intensity  = mainLightIntensity;
            main.shadows    = LightShadows.Soft;
            main.shadowStrength     = shadowStrength;
            main.shadowBias         = 0.04f;
            main.shadowNormalBias   = 0.3f;
            main.transform.rotation = Quaternion.Euler(mainLightAngle.x, mainLightAngle.y, 0f);

            // 3. Luz de relleno (suave, opuesta)
            var fillGO  = new GameObject("FillLight");
            fillGO.transform.SetParent(_lightingRoot.transform);
            var fill    = fillGO.AddComponent<Light>();
            fill.type       = LightType.Directional;
            fill.color      = fillLightColor;
            fill.intensity  = fillLightIntensity;
            fill.shadows    = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(mainLightAngle.x, mainLightAngle.y + 180f, 0f);

            // 4. Spots laterales (focos de estudio)
            if (enableSpotLights)
            {
                CreateSpot("SpotLeft",
                    position: new Vector3(-3.5f, 3.8f, 0.8f),
                    lookAt:   new Vector3(-1.2f, 0f,   0.3f),
                    color:    spotLeftColor,
                    intensity:spotLeftIntensity);

                CreateSpot("SpotRight",
                    position: new Vector3( 3.5f, 3.8f, 0.8f),
                    lookAt:   new Vector3( 1.2f, 0f,   0.3f),
                    color:    spotRightColor,
                    intensity:spotRightIntensity);
            }
        }

        private void CreateSpot(string lightName, Vector3 position, Vector3 lookAt,
                                 Color color, float intensity)
        {
            var go    = new GameObject(lightName);
            go.transform.SetParent(_lightingRoot.transform);
            go.transform.position = position;
            go.transform.LookAt(lookAt);

            var spot         = go.AddComponent<Light>();
            spot.type        = LightType.Spot;
            spot.color       = color;
            spot.intensity   = intensity;
            spot.spotAngle   = spotAngle;
            spot.range       = spotRange;
            spot.shadows     = LightShadows.None;   // spots sin sombra = más rendimiento
        }

        /// <summary>
        /// Desactiva todas las luces de la escena que NO pertenezcan a nuestro lighting root.
        /// Esto elimina duplicados de StudioLightingManager y la Directional Light por defecto.
        /// </summary>
        private void KillForeignLights()
        {
            var allLights = FindObjectsOfType<Light>();
            foreach (var light in allLights)
            {
                // Si la luz es hija de nuestro root, la respetamos
                if (_lightingRoot != null && light.transform.IsChildOf(_lightingRoot.transform))
                    continue;

                // Desactivar cualquier otra luz (Directional Light de Unity, StudioLightingManager, etc.)
                light.enabled = false;
                Debug.Log($"[StudioEnvironmentManager] Desactivando luz externa: {light.gameObject.name}");
            }
        }

        private void SetupAmbient()
        {
            RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight     = ambientColor;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;

            // Sin fog — evita el filter extra que menciona el usuario
            RenderSettings.fog = false;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Paneles (opcional, controlado por escena)
        // ─────────────────────────────────────────────────────────────────
        private void CreatePanelManager()
        {
            if (config == null || config.panelPrefab == null) return;
            var root   = new GameObject("StudioPanels_Auto");
            var layout = root.AddComponent<CurvedPanelLayout>();
            Debug.Log("[StudioEnvironmentManager] PanelManager creado.");
        }

        // ─────────────────────────────────────────────────────────────────
        //  Utils
        // ─────────────────────────────────────────────────────────────────
        private void DestroyNamed(string n)
        {
            var obj = GameObject.Find(n);
            if (obj != null) Destroy(obj);
        }

        public void RefreshEnvironment()
        {
            if (_cycloramaMat != null) ApplyCycloramaColors();
            BuildEnvironment();
        }

        public void SetGlobalIntensity(float multiplier)
        {
            if (_lightingRoot == null) return;
            foreach (var light in _lightingRoot.GetComponentsInChildren<Light>())
            {
                if (light.name == "MainLight") light.intensity = mainLightIntensity * multiplier;
                else if (light.name == "FillLight") light.intensity = fillLightIntensity * multiplier;
                // Spots se reducen también pero a la mitad de velocidad (se mantienen visibles)
                else if (light.type == LightType.Spot)
                    light.intensity = (light.name == "SpotLeft" ? spotLeftIntensity : spotRightIntensity)
                                    * Mathf.Lerp(1f, multiplier, 0.5f);
            }
            RenderSettings.ambientLight = ambientColor * multiplier;
        }
    }
}
