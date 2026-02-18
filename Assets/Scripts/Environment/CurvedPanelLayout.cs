using UnityEngine;

namespace ASL_LearnVR
{
    /// <summary>
    /// Coloca paneles UI en forma de arco envolvente alrededor del usuario.
    /// Los paneles se distribuyen radialmente y rotan para mirar al centro.
    /// </summary>
    public class CurvedPanelLayout : MonoBehaviour
    {
        [Header("Panel Configuration")]
        [SerializeField] private GameObject panelPrefab;
        [Tooltip("Numero de paneles a generar en el arco")]
        [SerializeField] private int panelCount = 5;

        [Header("Arc Settings")]
        [Tooltip("Radio del arco en metros")]
        [SerializeField] private float arcRadius = 2.5f;

        [Tooltip("Angulo total del arco en grados (ej: 120 crea un arco suave)")]
        [SerializeField] private float arcAngle = 120f;

        [Tooltip("Altura de los paneles (a nivel de ojos del usuario)")]
        [SerializeField] private float panelHeight = 1.6f;

        [Header("Advanced")]
        [Tooltip("Centro del arco (por defecto, posicion del jugador)")]
        [SerializeField] private Vector3 arcCenter = Vector3.zero;

        [Tooltip("Regenerar paneles en cada cambio (solo para testing)")]
        [SerializeField] private bool autoUpdateInEditor = false;

        private GameObject[] instantiatedPanels;

        void Start()
        {
            GeneratePanels();
        }

        /// <summary>
        /// Genera los paneles en formacion de arco
        /// </summary>
        public void GeneratePanels()
        {
            ClearPanels();

            if (panelPrefab == null)
            {
                Debug.LogError("[CurvedPanelLayout] Panel prefab no asignado!");
                return;
            }

            if (panelCount <= 0)
            {
                Debug.LogWarning("[CurvedPanelLayout] Panel count debe ser mayor a 0");
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

            Debug.Log($"[CurvedPanelLayout] Generados {panelCount} paneles en arco de {arcAngle} grados");
        }

        /// <summary>
        /// Elimina todos los paneles generados
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
        /// Actualiza la posicion del centro del arco (util si el usuario se mueve)
        /// </summary>
        public void UpdateArcCenter(Vector3 newCenter)
        {
            arcCenter = newCenter;
            GeneratePanels();
        }

        /// <summary>
        /// Obtiene referencia a un panel especifico por indice
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
