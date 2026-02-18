using UnityEngine;
using UnityEngine.SceneManagement;

namespace ASL_LearnVR
{
    /// <summary>
    /// Singleton que persiste entre escenas y recrea el ambiente visual automaticamente.
    /// Usa un StudioEnvironmentConfig (ScriptableObject) como fuente unica de configuracion.
    /// Cambias el config UNA VEZ y todas las escenas se ven igual.
    /// </summary>
    public class StudioEnvironmentManager : MonoBehaviour
    {
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
                        GameObject go = new GameObject("StudioEnvironmentManager");
                        _instance = go.AddComponent<StudioEnvironmentManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Configuration")]
        [Tooltip("Arrastra aqui el StudioEnvironmentConfig asset")]
        [SerializeField] private StudioEnvironmentConfig config;

        [Header("Shader References")]
        [Tooltip("Shader: ASL_LearnVR/RadialGradientFloor")]
        [SerializeField] private Shader floorShader;

        [Tooltip("Shader: ASL_LearnVR/WallGradient")]
        [SerializeField] private Shader wallShader;

        [Header("Options")]
        [Tooltip("Escenas donde NO crear el ambiente (dejar vacio para crear en todas)")]
        [SerializeField] private string[] excludedScenes = new string[0];

        [Tooltip("Activar paneles en arco (requiere panelPrefab en el config)")]
        [SerializeField] private bool enablePanels = true;

        // Referencias a los objetos creados (se destruyen y recrean por escena)
        private GameObject floorObject;
        private GameObject wallsObject;
        private GameObject lightingObject;
        private GameObject panelManagerObject;
        private Material floorMaterial;
        private Material wallMaterial;

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

            if (config == null)
            {
                Debug.LogError("[StudioEnvironmentManager] Config no asignado! Arrastra un StudioEnvironmentConfig asset.");
                return;
            }

            // Crear materiales una vez (persisten con DontDestroyOnLoad)
            CreateMaterials();
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
            if (config == null) return;

            // Verificar si la escena esta excluida
            foreach (string excluded in excludedScenes)
            {
                if (scene.name == excluded) return;
            }

            // Recrear el ambiente en la nueva escena
            BuildEnvironment();
        }

        /// <summary>
        /// Crea los materiales compartidos (se reusan entre escenas)
        /// </summary>
        private void CreateMaterials()
        {
            if (floorShader != null)
            {
                floorMaterial = new Material(floorShader);
                floorMaterial.name = "StudioFloor_Runtime";
            }
            else
            {
                // Intentar buscar el shader por nombre
                floorShader = Shader.Find("ASL_LearnVR/RadialGradientFloor");
                if (floorShader != null)
                    floorMaterial = new Material(floorShader);
                else
                    Debug.LogWarning("[StudioEnvironmentManager] No se encontro el shader RadialGradientFloor.");
            }

            if (wallShader != null)
            {
                wallMaterial = new Material(wallShader);
                wallMaterial.name = "StudioWalls_Runtime";
            }
            else
            {
                wallShader = Shader.Find("ASL_LearnVR/WallGradient");
                if (wallShader != null)
                    wallMaterial = new Material(wallShader);
                else
                    Debug.LogWarning("[StudioEnvironmentManager] No se encontro el shader WallGradient.");
            }

            ApplyConfigToMaterials();
        }

        /// <summary>
        /// Aplica los valores del config a los materiales
        /// </summary>
        private void ApplyConfigToMaterials()
        {
            if (floorMaterial != null)
            {
                floorMaterial.SetColor("_CenterColor", config.floorCenterColor);
                floorMaterial.SetColor("_EdgeColor", config.floorEdgeColor);
                floorMaterial.SetFloat("_Radius", config.floorGradientRadius);
                floorMaterial.SetFloat("_Smoothness", config.floorSmoothness);
            }

            if (wallMaterial != null)
            {
                wallMaterial.SetColor("_BottomColor", config.wallBottomColor);
                wallMaterial.SetColor("_TopColor", config.wallTopColor);
                wallMaterial.SetFloat("_GradientHeight", config.wallGradientHeight);
                wallMaterial.SetFloat("_GroundLevel", config.wallGroundLevel);
                wallMaterial.SetFloat("_Smoothness", config.wallSmoothness);
            }
        }

        /// <summary>
        /// Construye todos los elementos del ambiente en la escena actual
        /// </summary>
        public void BuildEnvironment()
        {
            // Limpiar objetos anteriores de esta escena
            CleanupSceneObjects();

            // Actualizar materiales con config actual
            ApplyConfigToMaterials();

            CreateFloor();
            CreateWalls();
            SetupLighting();
            SetupAmbient();

            if (enablePanels)
                CreatePanelManager();

            Debug.Log($"[StudioEnvironmentManager] Ambiente creado en escena: {SceneManager.GetActiveScene().name}");
        }

        private void CleanupSceneObjects()
        {
            // Destruir objetos de ambiente que puedan existir en la escena
            DestroyIfFound("StudioFloor_Auto");
            DestroyIfFound("StudioWalls_Auto");
            DestroyIfFound("StudioLighting_Auto");
            DestroyIfFound("StudioPanels_Auto");
        }

        private void DestroyIfFound(string objectName)
        {
            GameObject obj = GameObject.Find(objectName);
            if (obj != null)
                Destroy(obj);
        }

        private void CreateFloor()
        {
            floorObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floorObject.name = "StudioFloor_Auto";
            floorObject.transform.position = Vector3.zero;
            floorObject.transform.localScale = new Vector3(
                config.floorSize / 10f, 1, config.floorSize / 10f
            );

            if (floorMaterial != null)
                floorObject.GetComponent<Renderer>().material = floorMaterial;
        }

        private void CreateWalls()
        {
            wallsObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            wallsObject.name = "StudioWalls_Auto";
            wallsObject.transform.position = new Vector3(0, config.wallCenterY, 0);
            wallsObject.transform.localScale = Vector3.one * config.wallRadius;

            // Quitar collider (no necesitamos colision con las paredes)
            Collider col = wallsObject.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            if (wallMaterial != null)
                wallsObject.GetComponent<Renderer>().material = wallMaterial;
        }

        private void SetupLighting()
        {
            lightingObject = new GameObject("StudioLighting_Auto");

            // Luz principal
            GameObject mainLightObj = new GameObject("MainLight");
            mainLightObj.transform.SetParent(lightingObject.transform);
            Light mainLight = mainLightObj.AddComponent<Light>();
            mainLight.type = LightType.Directional;
            mainLight.color = config.mainLightColor;
            mainLight.intensity = config.mainLightIntensity;
            mainLight.transform.rotation = Quaternion.Euler(
                config.mainLightAngle.x, config.mainLightAngle.y, 0
            );

            if (config.enableSoftShadows)
            {
                mainLight.shadows = LightShadows.Soft;
                mainLight.shadowStrength = config.shadowStrength;
                mainLight.shadowBias = 0.05f;
                mainLight.shadowNormalBias = 0.4f;
            }
            else
            {
                mainLight.shadows = LightShadows.None;
            }

            // Luz de relleno (opuesta a la principal)
            GameObject fillLightObj = new GameObject("FillLight");
            fillLightObj.transform.SetParent(lightingObject.transform);
            Light fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = config.fillLightColor;
            fillLight.intensity = config.fillLightIntensity;
            fillLight.shadows = LightShadows.None;
            fillLight.transform.rotation = Quaternion.Euler(
                config.mainLightAngle.x, config.mainLightAngle.y + 180f, 0
            );

            // Desactivar la Directional Light por defecto si existe
            DisableDefaultDirectionalLight();
        }

        private void DisableDefaultDirectionalLight()
        {
            // Buscar la luz por defecto de Unity y desactivarla para evitar doble iluminacion
            GameObject defaultLight = GameObject.Find("Directional Light");
            if (defaultLight != null && defaultLight.transform.parent == null)
            {
                Light light = defaultLight.GetComponent<Light>();
                if (light != null)
                    light.enabled = false;
            }
        }

        private void SetupAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = config.ambientColor;
            RenderSettings.ambientIntensity = config.ambientIntensity;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        }

        private void CreatePanelManager()
        {
            if (config.panelPrefab == null)
            {
                Debug.Log("[StudioEnvironmentManager] Panel prefab no asignado en config. Paneles omitidos.");
                return;
            }

            panelManagerObject = new GameObject("StudioPanels_Auto");
            CurvedPanelLayout layout = panelManagerObject.AddComponent<CurvedPanelLayout>();

            // Inyectar valores desde config via reflection o metodo publico
            // Usamos SerializedFields, asi que inyectamos via el setup manual
            // El CurvedPanelLayout se configura desde el Inspector o se puede extender
            Debug.Log("[StudioEnvironmentManager] PanelManager creado. Configura el prefab y valores en el componente CurvedPanelLayout.");
        }

        /// <summary>
        /// Fuerza reconstruccion del ambiente (util si cambias el config en runtime)
        /// </summary>
        public void RefreshEnvironment()
        {
            if (config == null) return;
            BuildEnvironment();
        }

        /// <summary>
        /// Ajusta intensidad global de iluminacion (para transiciones entre escenas)
        /// </summary>
        public void SetGlobalIntensity(float multiplier)
        {
            if (lightingObject == null) return;

            Light[] lights = lightingObject.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
                if (light.gameObject.name == "MainLight")
                    light.intensity = config.mainLightIntensity * multiplier;
                else if (light.gameObject.name == "FillLight")
                    light.intensity = config.fillLightIntensity * multiplier;
            }

            RenderSettings.ambientIntensity = config.ambientIntensity * multiplier;
        }
    }
}
