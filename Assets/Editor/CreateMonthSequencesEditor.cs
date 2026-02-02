using UnityEngine;
using UnityEditor;
using System.IO;
using ASL_LearnVR.Data;

/// <summary>
/// Editor utility para crear los MonthSequenceData (Meses) automáticamente.
/// Crea 12 assets (ENERO..DICIEMBRE) con las letras tipo JAN,FEB,... y un CategoryData "Meses" que los contiene.
/// </summary>
public static class CreateMonthSequencesEditor
{
    [MenuItem("Tools/ASL/Create Month Sequences (Meses)")]
    public static void CreateMonthSequences()
    {
        string basePath = "Assets/Data/Months";
        string categoriesPath = "Assets/Data/Categories";

        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        if (!Directory.Exists(categoriesPath))
            Directory.CreateDirectory(categoriesPath);

        // Spanish month names and English 3-letter abbreviations (JAN..DEC)
        string[] monthNames = new string[] { "ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO", "JULIO", "AGOSTO", "SEPTIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE" };
        string[] monthAbbr = new string[] { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

        // Buscar o crear CategoryData "Meses"
        string categoryAssetPath = Path.Combine(categoriesPath, "Meses.asset");
        CategoryData mesesCategory = AssetDatabase.LoadAssetAtPath<CategoryData>(categoryAssetPath);
        if (mesesCategory == null)
        {
            mesesCategory = ScriptableObject.CreateInstance<CategoryData>();
            mesesCategory.categoryName = "Meses";
            AssetDatabase.CreateAsset(mesesCategory, categoryAssetPath);
            Debug.Log("CreateMonthSequences: Created CategoryData 'Meses'.");
        }

        for (int i = 0; i < monthNames.Length; i++)
        {
            string monthName = monthNames[i];
            string abbr = monthAbbr[i];

            string assetPath = Path.Combine(basePath, $"Month_{i + 1:00}_{monthName}.asset");

            MonthSequenceData msd = AssetDatabase.LoadAssetAtPath<MonthSequenceData>(assetPath);
            if (msd == null)
            {
                msd = ScriptableObject.CreateInstance<MonthSequenceData>();
                msd.signName = monthName;
                AssetDatabase.CreateAsset(msd, assetPath);
                Debug.Log($"CreateMonthSequences: Created MonthSequenceData '{monthName}'.");
            }

            // Asignar letras buscando SignData por nombre (ej: 'J','A','N')
            for (int j = 0; j < 3; j++)
            {
                string letter = abbr[j].ToString();
                SignData found = FindSignByName(letter);
                if (found != null)
                {
                    msd.letters[j] = found;
                }
                else
                {
                    Debug.LogWarning($"CreateMonthSequences: No se encontró SignData para la letra '{letter}'. Deja vacío el campo. Crea un SignData con signName='{letter}'.");
                }
            }

            // Guardar cambios del asset
            EditorUtility.SetDirty(msd);

            // Añadir a la categoría si no está ya
            if (mesesCategory.signs == null)
                mesesCategory.signs = new System.Collections.Generic.List<SignData>();

            if (!mesesCategory.signs.Contains(msd))
            {
                mesesCategory.signs.Add(msd);
                EditorUtility.SetDirty(mesesCategory);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = mesesCategory;
        Debug.Log("CreateMonthSequences: Proceso completado. Revisa 'Assets/Data/Months' y el CategoryData 'Meses'.");
    }

    private static SignData FindSignByName(string signName)
    {
        // Buscar por assets SignData en el proyecto
        string[] guids = AssetDatabase.FindAssets("t:SignData");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sd = AssetDatabase.LoadAssetAtPath<SignData>(path);
            if (sd != null && sd.signName != null && sd.signName.Equals(signName, System.StringComparison.OrdinalIgnoreCase))
                return sd;
        }

        // Fallback: Resources
        var all = Resources.FindObjectsOfTypeAll<SignData>();
        foreach (var sd in all)
        {
            if (sd != null && sd.signName != null && sd.signName.Equals(signName, System.StringComparison.OrdinalIgnoreCase))
                return sd;
        }

        return null;
    }
}
