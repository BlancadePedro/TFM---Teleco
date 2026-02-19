using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace ASL_LearnVR.UI
{
    /// <summary>
    /// Curves all UI elements on a World Space Canvas along a cylindrical arc.
    /// Can be placed on the Canvas itself or on a parent object.
    /// Auto-finds the largest child Canvas for reference dimensions.
    /// Subdivides UI quads so backgrounds and panels curve smoothly.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class CurvedUIPanel : MonoBehaviour
    {
        [Header("Curve")]
        [Tooltip("Total arc in degrees. 15-30 subtle, 30-60 pronounced.")]
        [SerializeField] [Range(5f, 90f)] public float curveAngle = 30f;

        [Header("Quality")]
        [Tooltip("Horizontal subdivisions per UI element. Higher = smoother curve on wide panels.")]
        [SerializeField] [Range(1, 32)] public int subdivisions = 16;

        private RectTransform _canvasRect;
        private readonly List<CurvedUIVertex> _effects = new List<CurvedUIVertex>();
        private readonly List<CurvedUIVertexTMP> _tmpEffects = new List<CurvedUIVertexTMP>();

        void OnEnable()
        {
            CleanupOldCurvedCanvasMesh();

            if (Application.isPlaying)
                StartCoroutine(ApplyAfterLayout());
            else
                ApplyCurve();
        }

        /// <summary>
        /// Waits one frame so Canvas RectTransform sizes are fully resolved,
        /// then applies the curve. Fixes VR headset where layout isn't ready in OnEnable.
        /// </summary>
        private IEnumerator ApplyAfterLayout()
        {
            yield return null;
            ApplyCurve();
        }

        void OnDisable()
        {
            ClearEffects();
        }

        private void FindCanvasRect()
        {
            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            float bestArea = 0f;
            _canvasRect = null;

            foreach (var c in canvases)
            {
                if (c.renderMode != RenderMode.WorldSpace) continue;
                var rt = c.GetComponent<RectTransform>();
                float area = rt.rect.width * rt.rect.height;
                if (area > bestArea)
                {
                    bestArea = area;
                    _canvasRect = rt;
                }
            }

            if (_canvasRect == null)
                _canvasRect = GetComponent<RectTransform>();
        }

        public void ApplyCurve()
        {
            FindCanvasRect();
            if (_canvasRect == null) return;

            _effects.Clear();
            _tmpEffects.Clear();

            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            {
                // TMP text has its own mesh pipeline — use CurvedUIVertexTMP
                if (graphic is TMP_Text)
                {
                    // Remove stale BaseMeshEffect if left over from before TMP detection
                    var staleFx = graphic.GetComponent<CurvedUIVertex>();
                    if (staleFx != null)
                    {
                        if (Application.isPlaying) Destroy(staleFx);
                        else DestroyImmediate(staleFx);
                    }

                    var tmpFx = graphic.GetComponent<CurvedUIVertexTMP>();
                    if (tmpFx == null)
                        tmpFx = graphic.gameObject.AddComponent<CurvedUIVertexTMP>();

                    tmpFx.Setup(_canvasRect, curveAngle);
                    _tmpEffects.Add(tmpFx);
                }
                else
                {
                    var fx = graphic.GetComponent<CurvedUIVertex>();
                    if (fx == null)
                        fx = graphic.gameObject.AddComponent<CurvedUIVertex>();

                    fx.Setup(_canvasRect, curveAngle, subdivisions);
                    _effects.Add(fx);
                }
            }
        }

        private void ClearEffects()
        {
            foreach (var fx in _effects)
            {
                if (fx != null)
                {
                    if (Application.isPlaying) Destroy(fx);
                    else DestroyImmediate(fx);
                }
            }
            _effects.Clear();

            foreach (var fx in _tmpEffects)
            {
                if (fx != null)
                {
                    if (Application.isPlaying) Destroy(fx);
                    else DestroyImmediate(fx);
                }
            }
            _tmpEffects.Clear();
        }

        private void CleanupOldCurvedCanvasMesh()
        {
            // ONLY remove CurvedCanvasMesh components and their MeshFilter/MeshRenderer
            // on objects that have a Canvas (never touch 3D objects like Table, Frame, etc.)
            foreach (var cm in GetComponentsInChildren<CurvedCanvasMesh>(true))
            {
                var go = cm.gameObject;
                if (Application.isPlaying) Destroy(cm); else DestroyImmediate(cm);

                if (go.GetComponent<Canvas>() != null)
                {
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf != null) { if (Application.isPlaying) Destroy(mf); else DestroyImmediate(mf); }
                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr != null) { if (Application.isPlaying) Destroy(mr); else DestroyImmediate(mr); }
                }
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                CleanupOldCurvedCanvasMesh();
                ApplyCurve();
            };
        }
#endif
    }

    /// <summary>
    /// Per-Graphic vertex modifier added automatically by CurvedUIPanel.
    /// Subdivides quads horizontally then bends all vertices along a cylinder.
    /// </summary>
    [AddComponentMenu("")]
    public class CurvedUIVertex : BaseMeshEffect
    {
        private RectTransform _canvasRect;
        private float _angle;
        private int _subdivisions = 16;

        public void Setup(RectTransform canvasRect, float angle, int subdivisions)
        {
            _canvasRect = canvasRect;
            _angle = angle;
            _subdivisions = Mathf.Max(1, subdivisions);

            var g = GetComponent<Graphic>();
            if (g != null) g.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || _canvasRect == null || vh.currentVertCount == 0)
                return;

            float halfW = _canvasRect.rect.width * 0.5f;
            float halfAngle = _angle * 0.5f * Mathf.Deg2Rad;
            if (halfAngle < 0.001f || halfW < 1f) return;

            float effRadius = halfW / Mathf.Sin(halfAngle);
            var myRect = (RectTransform)transform;

            // Get original triangle stream
            var original = new List<UIVertex>();
            vh.GetUIVertexStream(original);

            var output = new List<UIVertex>();

            // Process in groups of 6 (two triangles = one quad)
            for (int q = 0; q + 5 < original.Count; q += 6)
            {
                // Standard UI quad vertex order in stream:
                // Tri1: 0,1,2  Tri2: 3,4,5
                // 0=BL, 1=TL, 2=TR, 3=TR, 4=BR, 5=BL
                UIVertex bl = original[q + 0];
                UIVertex tl = original[q + 1];
                UIVertex tr = original[q + 2];
                UIVertex br = original[q + 4];

                // Subdivide horizontally then curve each vertex
                SubdivideAndCurve(output, bl, tl, tr, br, myRect, halfW, halfAngle, effRadius);
            }

            // Handle leftover triangles (not part of a quad)
            int leftover = original.Count % 6;
            if (leftover > 0)
            {
                for (int i = original.Count - leftover; i < original.Count; i++)
                {
                    var v = original[i];
                    v.position = CurveVertex(v.position, myRect, halfW, halfAngle, effRadius);
                    output.Add(v);
                }
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(output);
        }

        private void SubdivideAndCurve(List<UIVertex> output,
            UIVertex bl, UIVertex tl, UIVertex tr, UIVertex br,
            RectTransform myRect, float halfW, float halfAngle, float effRadius)
        {
            int divs = _subdivisions;

            for (int x = 0; x < divs; x++)
            {
                float t0 = (float)x / divs;
                float t1 = (float)(x + 1) / divs;

                UIVertex v00 = LerpVertex(bl, br, t0); // bottom at t0
                UIVertex v10 = LerpVertex(bl, br, t1); // bottom at t1
                UIVertex v01 = LerpVertex(tl, tr, t0); // top at t0
                UIVertex v11 = LerpVertex(tl, tr, t1); // top at t1

                // Curve all 4 corners
                v00.position = CurveVertex(v00.position, myRect, halfW, halfAngle, effRadius);
                v10.position = CurveVertex(v10.position, myRect, halfW, halfAngle, effRadius);
                v01.position = CurveVertex(v01.position, myRect, halfW, halfAngle, effRadius);
                v11.position = CurveVertex(v11.position, myRect, halfW, halfAngle, effRadius);

                // Tri 1: BL, TL, TR
                output.Add(v00);
                output.Add(v01);
                output.Add(v11);

                // Tri 2: TR, BR, BL
                output.Add(v11);
                output.Add(v10);
                output.Add(v00);
            }
        }

        private Vector3 CurveVertex(Vector3 localPos, RectTransform myRect, float halfW, float halfAngle, float effRadius)
        {
            Vector3 world = myRect.TransformPoint(localPos);
            Vector3 cl = _canvasRect.InverseTransformPoint(world);

            float t = Mathf.Clamp(cl.x / halfW, -1f, 1f);
            float a = t * halfAngle;

            cl.x = Mathf.Sin(a) * effRadius;
            cl.z += (Mathf.Cos(a) - 1f) * effRadius;

            world = _canvasRect.TransformPoint(cl);
            return myRect.InverseTransformPoint(world);
        }

        private static UIVertex LerpVertex(UIVertex a, UIVertex b, float t)
        {
            UIVertex v = new UIVertex();
            v.position = Vector3.Lerp(a.position, b.position, t);
            v.color = Color32.Lerp(a.color, b.color, t);
            v.uv0 = Vector4.Lerp(a.uv0, b.uv0, t);
            v.uv1 = Vector4.Lerp(a.uv1, b.uv1, t);
            v.normal = Vector3.Lerp(a.normal, b.normal, t).normalized;
            v.tangent = Vector4.Lerp(a.tangent, b.tangent, t);
            return v;
        }
    }

    /// <summary>
    /// Curves TextMeshPro text vertices along a cylinder.
    /// TMP bypasses BaseMeshEffect so we ForceMeshUpdate (flat) → curve → UpdateVertexData
    /// each frame in LateUpdate (after TMP has finished its own rendering).
    /// </summary>
    [AddComponentMenu("")]
    public class CurvedUIVertexTMP : MonoBehaviour
    {
        private TMP_Text _tmpText;
        private RectTransform _canvasRect;
        private RectTransform _myRect;
        private float _angle;

        public void Setup(RectTransform canvasRect, float angle)
        {
            _canvasRect = canvasRect;
            _angle = angle;
            _tmpText = GetComponent<TMP_Text>();
            _myRect = GetComponent<RectTransform>();
        }

        void LateUpdate()
        {
            if (_tmpText == null || _canvasRect == null) return;

            float halfW = _canvasRect.rect.width * 0.5f;
            float halfAngle = _angle * 0.5f * Mathf.Deg2Rad;
            if (halfAngle < 0.001f || halfW < 1f) return;
            float effRadius = halfW / Mathf.Sin(halfAngle);

            // Reset to flat vertices, then curve them
            _tmpText.ForceMeshUpdate();
            var textInfo = _tmpText.textInfo;

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                for (int j = 0; j < meshInfo.vertexCount; j++)
                {
                    Vector3 pos = meshInfo.vertices[j];
                    Vector3 world = _myRect.TransformPoint(pos);
                    Vector3 cl = _canvasRect.InverseTransformPoint(world);

                    float t = Mathf.Clamp(cl.x / halfW, -1f, 1f);
                    float a = t * halfAngle;

                    cl.x = Mathf.Sin(a) * effRadius;
                    cl.z += (Mathf.Cos(a) - 1f) * effRadius;

                    world = _canvasRect.TransformPoint(cl);
                    meshInfo.vertices[j] = _myRect.InverseTransformPoint(world);
                }
            }

            _tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }
}
