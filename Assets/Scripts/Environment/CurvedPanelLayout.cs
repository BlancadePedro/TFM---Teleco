using UnityEngine;

namespace ASL_LearnVR
{
    /// <summary>
    /// Places UI panels in a surrounding arc around the user.
    /// Panels are distributed radially and rotate to face the center.
    /// </summary>
    public class CurvedPanelLayout : MonoBehaviour
    {
        [Header("Panel Configuration")]
        [SerializeField] private GameObject panelPrefab;
        [Tooltip("Numero de paneles a generar en el arco")]
        [SerializeField] private int panelCount = 5;

        [Header("Arc Settings")]
        [Tooltip("Arc radius in meters")]
        [SerializeField] private float arcRadius = 2.5f;

        [Tooltip("Angulo total del arco en degrees (ej: 120 crea un arco suave)")]
        [SerializeField] private float arcAngle = 120f;

        [Tooltip("Panel height (at user's eye level)")]
        [SerializeField] private float panelHeight = 1.6f;

        [Header("Advanced")]
        [Tooltip("Arc center (defaults to player position)")]
        [SerializeField] private Vector3 arcCenter = Vector3.zero;

        [Tooltip("Regenerate panels on each change (testing only)")]
        [SerializeField] private bool autoUpdateInEditor = false;

        private GameObject[] instantiatedPanels;

        void Start()
        {
            GeneratePanels();
        }

        /// <summary>
        /// Generates panels in arc formation
        /// </summary>
        public void GeneratePanels()
        {
            ClearPanels();

            if (panelPrefab == null)
            {
                Debug.LogError("[CurvedPanelLayout] Panel prefab not assigned!");
                return;
            }

            if (panelCount <= 0)
            {
                Debug.LogWarning("[CurvedPanelLayout] Panel count must be greater than 0");
                return;
            }

            instantiatedPanels = new GameObject[panelCount];

            float angleStep = (panelCount > 1) ? arcAngle / (panelCount - 1) : 0f;
            float startAngle = -arcAngle / 2f;

            for (int i = 0; i < panelCount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                float rad = currentAngle * Mathf.Deg2Rad;

                Vector3 position = arcCenter + new Vector3(
                    Mathf.Sin(rad) * arcRadius,
                    panelHeight,
                    Mathf.Cos(rad) * arcRadius
                );

                GameObject panel = Instantiate(panelPrefab, position, Quaternion.identity, transform);
                panel.name = $"Panel_{i:00}";

                // Rotar panel para que mire hacia el centro
                Vector3 lookTarget = arcCenter + new Vector3(0, panelHeight, 0);
                panel.transform.LookAt(lookTarget);

                // Canvas mira "hacia atras" por defecto, rotamos 180
                panel.transform.Rotate(0, 180, 0);

                instantiatedPanels[i] = panel;
            }

            Debug.Log($"[CurvedPanelLayout] Generated {panelCount} panels in arc of {arcAngle} degrees");
        }

        /// <summary>
        /// Removes all generated panels
        /// </summary>
        public void ClearPanels()
        {
            if (instantiatedPanels != null)
            {
                foreach (var panel in instantiatedPanels)
                {
                    if (panel != null)
                    {
                        if (Application.isPlaying)
                            Destroy(panel);
                        else
                            DestroyImmediate(panel);
                    }
                }
            }

            // Limpiar hijos directos por si acaso
            while (transform.childCount > 0)
            {
                Transform child = transform.GetChild(0);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            instantiatedPanels = null;
        }

        /// <summary>
        /// Updates the arc center position (useful if the user moves)
        /// </summary>
        public void UpdateArcCenter(Vector3 newCenter)
        {
            arcCenter = newCenter;
            GeneratePanels();
        }

        /// <summary>
        /// Gets a reference to a specific panel by index
        /// </summary>
        public GameObject GetPanel(int index)
        {
            if (instantiatedPanels != null && index >= 0 && index < instantiatedPanels.Length)
                return instantiatedPanels[index];

            return null;
        }

        public int PanelCount => instantiatedPanels?.Length ?? 0;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (autoUpdateInEditor && !Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                        GeneratePanels();
                };
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;

            float angleStep = (panelCount > 1) ? arcAngle / (panelCount - 1) : 0f;
            float startAngle = -arcAngle / 2f;

            Vector3 prevPoint = Vector3.zero;
            for (int i = 0; i <= panelCount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                float rad = currentAngle * Mathf.Deg2Rad;

                Vector3 point = arcCenter + new Vector3(
                    Mathf.Sin(rad) * arcRadius,
                    panelHeight,
                    Mathf.Cos(rad) * arcRadius
                );

                if (i > 0)
                    Gizmos.DrawLine(prevPoint, point);

                Gizmos.DrawWireSphere(point, 0.1f);
                prevPoint = point;
            }

            // Lineas desde el centro a cada panel
            Gizmos.color = Color.yellow;
            Vector3 center = arcCenter + new Vector3(0, panelHeight, 0);
            for (int i = 0; i < panelCount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                float rad = currentAngle * Mathf.Deg2Rad;

                Vector3 point = arcCenter + new Vector3(
                    Mathf.Sin(rad) * arcRadius,
                    panelHeight,
                    Mathf.Cos(rad) * arcRadius
                );

                Gizmos.DrawLine(center, point);
            }
        }
#endif
    }
}
