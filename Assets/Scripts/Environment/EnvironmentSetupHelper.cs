using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ASL_LearnVR
{
    /// <summary>
    /// Herramienta de Editor para previsualizar el ambiente y crear el prefab
    /// del StudioEnvironmentManager que persiste entre escenas.
    /// Usa los context menus (clic derecho en el componente) para cada accion.
    /// </summary>
    public class EnvironmentSetupHelper : MonoBehaviour
    {
        [Header("=== ARRASTRA AQUI TU CONFIG ===")]
        [Tooltip("El StudioEnvironmentConfig con todos los valores del ambiente")]
        [SerializeField] private StudioEnvironmentConfig config;

        [Header("Shader References")]
        [Tooltip("Shader: ASL_LearnVR/RadialGradientFloor")]
        [SerializeField] private Shader floorShader;

        [Tooltip("Shader: ASL_LearnVR/WallGradient")]
        [SerializeField] private Shader wallShader;

#if UNITY_EDITOR
        [ContextMenu("1. Preview Environment (en esta escena)")]
        public void PreviewEnvironment()
        {
            if (!ValidateConfig()) return;

            ClearPreview();
            CreatePreviewFloor();
            CreatePreviewWalls();
            CreatePreviewLighting();
            ApplyAmbient();

            Debug.Log("[EnvironmentSetup] Preview creado. Dale Play para verlo con iluminacion completa.");
        }

        [ContextMenu("2. Create StudioEnvironmentManager Prefab")]
        public void CreateManagerPrefab()
        {
            if (!ValidateConfig()) return;

            // Crear el GameObject con el manager
            GameObject managerObj = new GameObject("StudioEnvironmentManager");
            StudioEnvironmentManager manager = managerObj.AddComponent<StudioEnvironmentManager>();

            // Asignar config y shaders via SerializedObject
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("floorShader").objectReferenceValue = floorShader;
            so.FindProperty("wallShader").objectReferenceValue = wallShader;
            so.ApplyModifiedProperties();

            // Guardar como prefab
            EnsureDirectoryExists("Assets/Prefabs");
            string prefabPath = "Assets/Prefabs/StudioEnvironmentManager.prefab";
            PrefabUtility.SaveAsPrefabAsset(managerObj, prefabPath);
            DestroyImmediate(managerObj);

            Debug.Log($"[EnvironmentSetup] Prefab creado en: {prefabPath}");
            Debug.Log("[EnvironmentSetup] SIGUIENTE PASO: Arrastra este prefab a CADA escena (o solo a la primera si usas DontDestroyOnLoad).");

            // Seleccionar el prefab en el Project
            Object prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        [ContextMenu("3. Add Manager Prefab to Current Scene")]
        public void AddManagerToScene()
        {
            // Verificar si ya existe en la escena
            if (FindObjectOfType<StudioEnvironmentManager>() != null)
            {
                Debug.Log("[EnvironmentSetup] StudioEnvironmentManager ya existe en esta escena.");
                return;
            }

            string prefabPath = "Assets/Prefabs/StudioEnvironmentManager.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogError("[EnvironmentSetup] Prefab no encontrado. Ejecuta primero '2. Create StudioEnvironmentManager Prefab'.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Debug.Log("[EnvironmentSetup] StudioEnvironmentManager agregado a la escena. Guarda la escena (Ctrl+S).");
        }

        [ContextMenu("X. Clear Preview")]
        public void ClearPreview()
        {
            DestroyIfExists("Preview_Floor");
            DestroyIfExists("Preview_Walls");
            DestroyIfExists("Preview_Lighting");
            Debug.Log("[EnvironmentSetup] Preview limpiado.");
        }

        private void CreatePreviewFloor()
        {
            // Cylinder aplanado = disco circular (no cuadrado)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Preview_Floor";
            floor.transform.position = new Vector3(0, -0.01f, 0);
            floor.transform.localScale = new Vector3(config.floorSize, 0.01f, config.floorSize);

            Collider floorCol = floor.GetComponent<Collider>();
            if (floorCol != null) DestroyImmediate(floorCol);

            if (floorShader != null)
            {
                Material mat = new Material(floorShader);
                mat.SetColor("_CenterColor", config.floorCenterColor);
                mat.SetColor("_EdgeColor", config.floorEdgeColor);
                mat.SetFloat("_Radius", config.floorGradientRadius);
                mat.SetFloat("_Smoothness", config.floorSmoothness);
                floor.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        private void CreatePreviewWalls()
        {
            GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            walls.name = "Preview_Walls";
            walls.transform.position = new Vector3(0, config.wallCenterY, 0);
            walls.transform.localScale = Vector3.one * config.wallRadius;

            Collider col = walls.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            if (wallShader != null)
            {
                Material mat = new Material(wallShader);
                mat.SetColor("_BottomColor", config.wallBottomColor);
                mat.SetColor("_TopColor", config.wallTopColor);
                mat.SetFloat("_GradientHeight", config.wallGradientHeight);
                mat.SetFloat("_GroundLevel", config.wallGroundLevel);
                mat.SetFloat("_Smoothness", config.wallSmoothness);
                walls.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        private void CreatePreviewLighting()
        {
            GameObject lightObj = new GameObject("Preview_Lighting");

            GameObject main = new GameObject("MainLight");
            main.transform.SetParent(lightObj.transform);
            Light mainLight = main.AddComponent<Light>();
            mainLight.type = LightType.Directional;
            mainLight.color = config.mainLightColor;
            mainLight.intensity = config.mainLightIntensity;
            mainLight.transform.rotation = Quaternion.Euler(config.mainLightAngle.x, config.mainLightAngle.y, 0);

            if (config.enableSoftShadows)
            {
                mainLight.shadows = LightShadows.Soft;
                mainLight.shadowStrength = config.shadowStrength;
            }

            GameObject fill = new GameObject("FillLight");
            fill.transform.SetParent(lightObj.transform);
            Light fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = config.fillLightColor;
            fillLight.intensity = config.fillLightIntensity;
            fillLight.shadows = LightShadows.None;
            fillLight.transform.rotation = Quaternion.Euler(config.mainLightAngle.x, config.mainLightAngle.y + 180f, 0);
        }

        private void ApplyAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = config.ambientColor;
            RenderSettings.ambientIntensity = config.ambientIntensity;
        }

        private bool ValidateConfig()
        {
            if (config == null)
            {
                Debug.LogError("[EnvironmentSetup] Config no asignado! Crea uno: Assets > Create > ASL LearnVR > Studio Environment Config");
                return false;
            }
            return true;
        }

        private void DestroyIfExists(string objectName)
        {
            GameObject obj = GameObject.Find(objectName);
            if (obj != null) DestroyImmediate(obj);
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
#endif
    }
}
