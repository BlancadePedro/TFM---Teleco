// Creates feedback indicator prefabs (error, warning, correct, hand correct)
// Menu: Tools/Feedback/Create Indicator Prefabs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FeedbackPrefabGenerator
{
    private const string OutputFolder = "Assets/Prefabs/Feedback";

    [MenuItem("Tools/Feedback/Create Indicator Prefabs")]
    public static void CreatePrefabs()
    {
        EnsureFolder(OutputFolder);

        CreateIndicator("ErrorIndicator.prefab", new Color(1f, 0f, 0f, 0.7f), 0.01f, transparent: true);
        CreateIndicator("WarningIndicator.prefab", new Color(1f, 0.55f, 0f, 0.7f), 0.01f, transparent: true);
        CreateIndicator("CorrectIndicator.prefab", new Color(0f, 1f, 0f, 0.7f), 0.01f, transparent: true);
        CreateIndicator("HandCorrectIndicator.prefab", new Color(0.2f, 1f, 0.2f, 1f), 0.03f, transparent: false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FeedbackPrefabGenerator] Prefabs creados en " + OutputFolder);
    }

    private static void CreateIndicator(string prefabName, Color color, float scale, bool transparent)
    {
        // Create material
        var mat = new Material(Shader.Find("Standard")) { name = prefabName.Replace(".prefab", "") + "_Mat" };
        mat.color = color;
        if (transparent)
            MakeTransparent(mat);

        string matPath = $"{OutputFolder}/{mat.name}.mat";
        AssetDatabase.CreateAsset(mat, matPath);

        // Create sphere
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = prefabName.Replace(".prefab", "");
        go.transform.localScale = Vector3.one * scale;

        // Remove collider for lightweight indicators
        Object.DestroyImmediate(go.GetComponent<Collider>());

        // Assign material
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        // Save as prefab
        string prefabPath = $"{OutputFolder}/{prefabName}";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);

        Object.DestroyImmediate(go);
    }

    private static void MakeTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 2); // Fade
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
    }

    private static void EnsureFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            string[] parts = folder.Split('/');
            string path = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = path + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(path, parts[i]);
                path = next;
            }
        }
    }
}
#endif
