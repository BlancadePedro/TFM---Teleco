using UnityEngine;
using UnityEditor;
using System.IO;
using ASL_LearnVR.Data;

/// <summary>
/// Editor utility to create MonthSequenceData assets automatically.
/// Creates 12 assets (JAN..DEC) and a CategoryData "Months" containing them.
/// </summary>
public static class CreateMonthSequencesEditor
{
    [MenuItem("Tools/ASL/Create Month Sequences")]
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

        // Find or create CategoryData "Months"
        string categoryAssetPath = Path.Combine(categoriesPath, "Months.asset");
        CategoryData monthsCategory = AssetDatabase.LoadAssetAtPath<CategoryData>(categoryAssetPath);
        if (monthsCategory == null)
        {
            monthsCategory = ScriptableObject.CreateInstance<CategoryData>();
            monthsCategory.categoryName = "Months";
            AssetDatabase.CreateAsset(monthsCategory, categoryAssetPath);
            Debug.Log("CreateMonthSequences: Created CategoryData 'Months'.");
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

            // Assign letters by looking up SignData by name (e.g., 'J','A','N')
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
                    Debug.LogWarning($"CreateMonthSequences: Could not find SignData for letter '{letter}'. Leave the field empty or create a SignData with signName='{letter}'.");
                }
            }

            // Save asset changes
            EditorUtility.SetDirty(msd);

            // Add to the category if not already present
            if (monthsCategory.signs == null)
                monthsCategory.signs = new System.Collections.Generic.List<SignData>();

            if (!monthsCategory.signs.Contains(msd))
            {
                monthsCategory.signs.Add(msd);
                EditorUtility.SetDirty(monthsCategory);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = monthsCategory;
        Debug.Log("CreateMonthSequences: Done. Check 'Assets/Data/Months' and the 'Months' CategoryData.");
    }

    private static SignData FindSignByName(string signName)
    {
        // Look up SignData assets in the project
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
