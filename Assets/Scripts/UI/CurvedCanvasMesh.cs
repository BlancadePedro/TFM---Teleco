using UnityEngine;
using UnityEngine.UI;

namespace ASL_LearnVR.UI
{
    /// <summary>
    /// Generates a curved mesh for a World Space Canvas.
    /// Attach to the same GameObject as the Canvas.
    ///
    /// How it works:
    ///   - Creates a quad mesh subdivided along the horizontal axis.
    ///   - Each column of vertices is offset in Z following a circular arc.
    ///   - The Canvas CanvasRenderer draws on top of this mesh.
    ///   - UV coords are preserved so all child UI elements render correctly.
    ///
    /// Setup:
    ///   1. Create a World Space Canvas → set its RectTransform size (e.g. 1.8 x 1.2 m).
    ///   2. Add THIS component.
    ///   3. Assign the Canvas's RectTransform to canvasRect (or leave null to auto-find).
    ///   4. Adjust CurveRadius and CurveAngle in the Inspector.
    ///   5. Click "Rebuild Mesh" context menu or enter Play Mode.
    ///
    /// Radius guide (for a panel ~1.8 m wide):
    ///   Tight curve  → radius 1.5,  angle 40°
    ///   Medium curve → radius 2.5,  angle 25°   ← recommended
    ///   Subtle curve → radius 4.0,  angle 15°
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    [ExecuteAlways]
    public class CurvedCanvasMesh : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────
        [Header("Canvas")]
        [Tooltip("RectTransform of the Canvas. Leave null to auto-find on this GameObject.")]
        [SerializeField] private RectTransform canvasRect;

        [Header("Curve")]
        [Tooltip("Radius of the arc in world units. Larger = flatter curve.")]
        [SerializeField] public float curveRadius = 2.5f;

        [Tooltip("Total angle of the arc in degrees. Controls how much the panel bends.")]
        [SerializeField] [Range(1f, 90f)] public float curveAngle = 25f;

        [Tooltip("Number of horizontal subdivisions. 32 is smooth, 16 is fine for subtle curves.")]
        [SerializeField] [Range(8, 64)] public int columns = 32;

        [Tooltip("Number of vertical subdivisions. 1 is enough unless you also want vertical curve.")]
        [SerializeField] [Range(1, 16)] public int rows = 1;

        [Header("Rebuild")]
        [Tooltip("Rebuild mesh when values change in Editor.")]
        [SerializeField] private bool autoRebuildInEditor = true;

        // ── Runtime ───────────────────────────────────────────────────────
        private MeshFilter   _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh         _mesh;

        private float _lastRadius;
        private float _lastAngle;
        private int   _lastColumns;
        private int   _lastRows;
        private Vector2 _lastSize;

        // ─────────────────────────────────────────────────────────────────
        void Awake()
        {
            EnsureComponents();
        }

        void Start()
        {
            if (canvasRect == null)
                canvasRect = GetComponent<RectTransform>();

            RebuildMesh();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!autoRebuildInEditor) return;
            // Delay one frame to avoid errors during OnValidate
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                EnsureComponents();
                if (canvasRect == null) canvasRect = GetComponent<RectTransform>();
                RebuildMesh();
            };
        }
#endif

        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && autoRebuildInEditor)
            {
                if (ParametersChanged())
                    RebuildMesh();
            }
#endif
        }

        // ── Public ────────────────────────────────────────────────────────

        [ContextMenu("Rebuild Mesh")]
        public void RebuildMesh()
        {
            EnsureComponents();

            if (canvasRect == null)
                canvasRect = GetComponent<RectTransform>();

            if (canvasRect == null)
            {
                Debug.LogWarning("[CurvedCanvasMesh] No RectTransform found.");
                return;
            }

            Vector2 size = canvasRect.rect.size;
            if (size.x <= 0 || size.y <= 0) return;

            BuildMesh(size);
            CacheParameters(size);
        }

        // ── Mesh generation ───────────────────────────────────────────────

        private void BuildMesh(Vector2 size)
        {
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "CurvedCanvas_Mesh" };
            }
            else
            {
                _mesh.Clear();
            }

            int vertsX = columns + 1;
            int vertsY = rows + 1;

            var vertices  = new Vector3[vertsX * vertsY];
            var uvs       = new Vector2[vertsX * vertsY];
            var normals   = new Vector3[vertsX * vertsY];
            var triangles = new int[columns * rows * 6];

            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;

            // Arc parameters
            // The panel spans curveAngle degrees along a circle of curveRadius
            float halfAngleRad = (curveAngle * 0.5f) * Mathf.Deg2Rad;

            for (int row = 0; row < vertsY; row++)
            {
                float vt = (float)row / rows;                 // 0..1 bottom to top
                float y  = Mathf.Lerp(-halfH, halfH, vt);

                for (int col = 0; col < vertsX; col++)
                {
                    float ut = (float)col / columns;          // 0..1 left to right
                    // Angle goes from -halfAngle to +halfAngle
                    float angle = Mathf.Lerp(-halfAngleRad, halfAngleRad, ut);

                    // Position on the arc:
                    //   x = sin(angle) * radius   (lateral position)
                    //   z = (cos(angle) - 1) * radius  (depth offset — 0 at centre, negative at edges)
                    float x = Mathf.Sin(angle) * curveRadius;
                    float z = (Mathf.Cos(angle) - 1f) * curveRadius;

                    int idx = row * vertsX + col;
                    vertices[idx] = new Vector3(x, y, z);
                    uvs[idx]      = new Vector2(ut, vt);
                    normals[idx]  = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)); // inward normal
                }
            }

            // Triangles
            int tri = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int bl = row       * vertsX + col;
                    int br = row       * vertsX + col + 1;
                    int tl = (row + 1) * vertsX + col;
                    int tr = (row + 1) * vertsX + col + 1;

                    triangles[tri++] = bl;
                    triangles[tri++] = tl;
                    triangles[tri++] = tr;

                    triangles[tri++] = bl;
                    triangles[tri++] = tr;
                    triangles[tri++] = br;
                }
            }

            _mesh.vertices  = vertices;
            _mesh.uv        = uvs;
            _mesh.normals   = normals;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();

            _meshFilter.mesh         = _mesh;
            _meshFilter.sharedMesh   = _mesh;
        }

        // ── Internal ──────────────────────────────────────────────────────

        private void EnsureComponents()
        {
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();

            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();

                // The MeshRenderer needs the same material as the Canvas Image
                // Leave material assignment to the user — just ensure shadows are off
                _meshRenderer.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
                _meshRenderer.receiveShadows     = false;
            }
        }

        private bool ParametersChanged()
        {
            if (canvasRect == null) return false;
            Vector2 size = canvasRect.rect.size;
            return !Mathf.Approximately(_lastRadius,  curveRadius)
                || !Mathf.Approximately(_lastAngle,   curveAngle)
                || _lastColumns != columns
                || _lastRows    != rows
                || _lastSize    != size;
        }

        private void CacheParameters(Vector2 size)
        {
            _lastRadius  = curveRadius;
            _lastAngle   = curveAngle;
            _lastColumns = columns;
            _lastRows    = rows;
            _lastSize    = size;
        }
    }
}
