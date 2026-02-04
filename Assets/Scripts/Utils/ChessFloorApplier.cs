using UnityEngine;
using UnityEngine.SceneManagement;

namespace ASL_LearnVR.Utils
{
    /// <summary>
    /// At scene load, replaces floor materials with a runtime checkerboard
    /// so no texture asset is needed, applied globally to every scene.
    /// </summary>
    public static class ChessFloorApplier
    {
        private static readonly string[] CandidateNames = { "Plane", "Floor", "Ground" };
        private const string FloorTag = "Floor";
        private static Material _cachedMat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            // Apply to the first scene that just loaded.
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyChessMaterial(scene);
        }

        private static void ApplyChessMaterial(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            if (roots == null || roots.Length == 0) return;

            if (_cachedMat == null)
                _cachedMat = CreateChessMaterial();

            int applied = 0;
            foreach (var root in roots)
                applied += ApplyRecursive(root.transform);

            if (applied > 0)
                Debug.Log($"[ChessFloorApplier] Material tablero aplicado a {applied} objeto(s) en escena '{scene.name}'.");
        }

        private static int ApplyRecursive(Transform t)
        {
            int count = 0;
            if (IsFloorCandidate(t.gameObject))
            {
                var renderer = t.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = _cachedMat;
                    count++;
                }
            }

            for (int i = 0; i < t.childCount; i++)
                count += ApplyRecursive(t.GetChild(i));

            return count;
        }

        private static bool IsFloorCandidate(GameObject go)
        {
            if (go.CompareTag(FloorTag))
                return true;

            foreach (var name in CandidateNames)
            {
                if (go.name == name)
                    return true;
            }
            return false;
        }

        private static Material CreateChessMaterial()
        {
            // Try URP Lit first, fall back to Standard.
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader)
            {
                name = "Runtime_ChessFloor"
            };

            var tex = GenerateCheckerTexture(16, Color.white, new Color32(30, 30, 30, 255));
            tex.name = "Runtime_ChessTexture";
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            // Assign texture to both URP and built-in property names for compatibility.
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", tex);

            var scale = new Vector2(4f, 4f);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTextureScale("_BaseMap", scale);
            if (mat.HasProperty("_MainTex"))
                mat.SetTextureScale("_MainTex", scale);

            return mat;
        }

        private static Texture2D GenerateCheckerTexture(int squaresPerSide, Color c1, Color c2)
        {
            int size = squaresPerSide * 2; // ensures an even number of pixels per tile
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool black = ((x / 2) + (y / 2)) % 2 == 0;
                    tex.SetPixel(x, y, black ? c2 : c1);
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
