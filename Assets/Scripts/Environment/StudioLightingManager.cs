using UnityEngine;

namespace ASL_LearnVR
{
    /// <summary>
    /// Gestiona la iluminacion para crear el ambiente minimalista tipo estudio XR.
    /// Configura luz principal calida, luz de relleno fria, y ambiente suave.
    /// </summary>
    public class StudioLightingManager : MonoBehaviour
    {
        [Header("Main Directional Light")]
        [SerializeField] private Light mainLight;

        [Tooltip("Intensidad de la luz principal")]
        [SerializeField] private float mainLightIntensity = 0.8f;

        [Tooltip("Color de la luz principal (ligeramente calido)")]
        [SerializeField] private Color mainLightColor = new Color(1f, 0.98f, 0.95f);

        [Tooltip("Angulo de la luz (X: pitch, Y: yaw)")]
        [SerializeField] private Vector2 mainLightAngle = new Vector2(50f, -30f);

        [Header("Fill Light (opcional)")]
        [SerializeField] private Light fillLight;

        [Tooltip("Intensidad de la luz de relleno")]
        [SerializeField] private float fillLightIntensity = 0.3f;

        [Tooltip("Color de la luz de relleno (mas fria que la principal)")]
        [SerializeField] private Color fillLightColor = new Color(0.9f, 0.95f, 1f);

        [Header("Ambient Settings")]
        [Tooltip("Color ambiente para suavizar sombras")]
        [SerializeField] private Color ambientColor = new Color(0.4f, 0.4f, 0.4f);

        [Tooltip("Intensidad de la luz ambiente")]
        [SerializeField] private float ambientIntensity = 0.5f;

        [Header("Shadow Settings")]
        [Tooltip("Activar sombras suaves")]
        [SerializeField] private bool enableSoftShadows = true;

        [Tooltip("Fuerza de las sombras (0 = transparentes, 1 = negras)")]
        [SerializeField][Range(0f, 1f)] private float shadowStrength = 0.4f;

        void Start()
        {
            SetupLighting();
        }

        /// <summary>
        /// Configura toda la iluminacion del ambiente
        /// </summary>
        public void SetupLighting()
        {
            SetupMainLight();
            SetupFillLight();
            SetupAmbient();

            Debug.Log("[StudioLightingManager] Iluminacion configurada");
        }

        private void SetupMainLight()
        {
            if (mainLight == null)
            {
                GameObject lightObj = GameObject.Find("Directional Light");
                if (lightObj != null)
                    mainLight = lightObj.GetComponent<Light>();
                else
                {
                    lightObj = new GameObject("Main Directional Light");
                    mainLight = lightObj.AddComponent<Light>();
                }
            }

            mainLight.type = LightType.Directional;
            mainLight.color = mainLightColor;
            mainLight.intensity = mainLightIntensity;

            if (enableSoftShadows)
            {
                mainLight.shadows = LightShadows.Soft;
                mainLight.shadowStrength = shadowStrength;
                mainLight.shadowBias = 0.05f;
                mainLight.shadowNormalBias = 0.4f;
            }
            else
            {
                mainLight.shadows = LightShadows.None;
            }

            mainLight.transform.rotation = Quaternion.Euler(mainLightAngle.x, mainLightAngle.y, 0);
        }

        private void SetupFillLight()
        {
            if (fillLight != null)
            {
                fillLight.type = LightType.Directional;
                fillLight.color = fillLightColor;
                fillLight.intensity = fillLightIntensity;
                fillLight.shadows = LightShadows.None;

                // Opuesta a la luz principal
                fillLight.transform.rotation = Quaternion.Euler(
                    mainLightAngle.x,
                    mainLightAngle.y + 180f,
                    0
                );
            }
        }

        private void SetupAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        }

        /// <summary>
        /// Ajusta la intensidad de todas las luces (util para transiciones)
        /// </summary>
        public void SetGlobalIntensity(float multiplier)
        {
            if (mainLight != null)
                mainLight.intensity = mainLightIntensity * multiplier;

            if (fillLight != null)
                fillLight.intensity = fillLightIntensity * multiplier;

            RenderSettings.ambientIntensity = ambientIntensity * multiplier;
        }

        /// <summary>
        /// Crea setup de iluminacion ideal desde cero
        /// </summary>
        [ContextMenu("Create Default Studio Lighting")]
        public void CreateDefaultSetup()
        {
            GameObject mainLightObj = new GameObject("Main Directional Light");
            mainLight = mainLightObj.AddComponent<Light>();
            mainLightObj.transform.SetParent(transform);

            GameObject fillLightObj = new GameObject("Fill Light");
            fillLight = fillLightObj.AddComponent<Light>();
            fillLightObj.transform.SetParent(transform);

            SetupLighting();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                SetupLighting();
        }
#endif
    }
}
