using UnityEngine;
using UnityEditor;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.Editor
{
    public class DiagnoseLevelData : EditorWindow
    {
        [MenuItem("ASL/Diagnose Level Data")]
        static void Diagnose()
        {
            // Cargar Level_Basic
            LevelData levelBasic = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Level_Basic.asset");

            if (levelBasic == null)
            {
                Debug.LogError("❌ No se encontró Level_Basic.asset");
                return;
            }

            Debug.Log($"=== DIAGNÓSTICO DE {levelBasic.name} ===");
            Debug.Log($"Level Name: '{levelBasic.levelName}'");
            Debug.Log($"Categories Count: {levelBasic.categories.Count}");

            // Verificar IsValid
            Debug.Log($"\n🔍 Llamando a IsValid()...");
            bool isValid = levelBasic.IsValid();
            Debug.Log($"IsValid() retorna: {isValid}");

            // Verificar cada categoría
            for (int i = 0; i < levelBasic.categories.Count; i++)
            {
                CategoryData category = levelBasic.categories[i];
                if (category == null)
                {
                    Debug.LogError($"❌ Category[{i}] es NULL");
                    continue;
                }

                Debug.Log($"\n--- Category[{i}]: {category.categoryName} ---");
                Debug.Log($"  Signs Count: {category.signs.Count}");

                bool catValid = category.IsValid();
                Debug.Log($"  IsValid(): {catValid}");

                // Verificar cada signo
                for (int j = 0; j < category.signs.Count; j++)
                {
                    SignData sign = category.signs[j];
                    if (sign == null)
                    {
                        Debug.LogError($"  ❌ Sign[{j}] es NULL");
                        continue;
                    }

                    bool signValid = sign.IsValid();
                    string status = signValid ? "✅" : "❌";
                    Debug.Log($"  {status} Sign[{j}]: '{sign.signName}' | handShapeOrPose: {(sign.handShapeOrPose != null ? "OK" : "NULL")}");
                }
            }

            Debug.Log($"\n=== FIN DIAGNÓSTICO ===");
        }
    }
}
