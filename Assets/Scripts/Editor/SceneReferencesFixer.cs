using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ASL_LearnVR.MainMenu;
using ASL_LearnVR.LevelSelection;

namespace ASL_LearnVR.Editor
{
    /// <summary>
    /// Herramienta de Editor para arreglar referencias rotas en las escenas.
    /// Menú: Tools > ASL Learn VR > Fix Scene References
    /// </summary>
    public class SceneReferencesFixer : EditorWindow
    {
        [MenuItem("Tools/ASL Learn VR/Fix Scene References")]
        public static void ShowWindow()
        {
            GetWindow<SceneReferencesFixer>("Fix Scene References");
        }

        private void OnGUI()
        {
            GUILayout.Label("Arreglar Referencias de Escenas", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Fix 01_MainMenu References", GUILayout.Height(40)))
            {
                FixMainMenuReferences();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Fix 02_LevelSelection References", GUILayout.Height(40)))
            {
                FixLevelSelectionReferences();
            }

            GUILayout.Space(20);

            EditorGUILayout.HelpBox(
                "Este script busca automáticamente los GameObjects necesarios y asigna las referencias.\n\n" +
                "IMPORTANTE: Guarda la escena después de ejecutar.",
                MessageType.Info
            );
        }

        private static void FixMainMenuReferences()
        {
            // Carga la escena
            var scene = EditorSceneManager.OpenScene("Assets/01_MainMenu.unity");

            // Busca el MenuController
            MenuController menuController = FindObjectOfType<MenuController>();

            if (menuController == null)
            {
                Debug.LogError("No se encontró MenuController en la escena.");
                return;
            }

            // Busca el panel de traducción
            GameObject translationPopup = GameObject.Find("Panel Translation Popup");

            if (translationPopup == null)
            {
                Debug.LogError("No se encontró 'Panel Translation Popup' en la escena.");
                return;
            }

            // Usa reflection para asignar el campo privado
            var field = typeof(MenuController).GetField("translationPopup",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(menuController, translationPopup);
                EditorUtility.SetDirty(menuController);
                Debug.Log("✅ MenuController: translationPopup asignado correctamente.");
            }

            // Marca la escena como modificada
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log("✅ Referencias de MainMenu arregladas. Guarda la escena (Ctrl+S).");
        }

        private static void FixLevelSelectionReferences()
        {
            // Carga la escena
            var scene = EditorSceneManager.OpenScene("Assets/02_LevelSelection.unity");

            // Busca el LevelSelectionController
            LevelSelectionController levelController = FindObjectOfType<LevelSelectionController>();

            if (levelController == null)
            {
                Debug.LogError("No se encontró LevelSelectionController en la escena.");
                return;
            }

            // Busca los paneles
            GameObject basicPanel = FindGameObjectByPartialName("Basic");
            GameObject intermediatePanel = FindGameObjectByPartialName("Intermediate");
            GameObject advancedPanel = FindGameObjectByPartialName("Advanced");

            if (basicPanel == null)
                Debug.LogWarning("⚠️ No se encontró panel 'Basic'");
            if (intermediatePanel == null)
                Debug.LogWarning("⚠️ No se encontró panel 'Intermediate'");
            if (advancedPanel == null)
                Debug.LogWarning("⚠️ No se encontró panel 'Advanced'");

            // Usa reflection para asignar los campos privados
            var basicField = typeof(LevelSelectionController).GetField("basicPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var intermediateField = typeof(LevelSelectionController).GetField("intermediatePanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var advancedField = typeof(LevelSelectionController).GetField("advancedPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (basicField != null && basicPanel != null)
            {
                basicField.SetValue(levelController, basicPanel);
                Debug.Log($"✅ LevelSelectionController: basicPanel asignado a '{basicPanel.name}'");
            }

            if (intermediateField != null && intermediatePanel != null)
            {
                intermediateField.SetValue(levelController, intermediatePanel);
                Debug.Log($"✅ LevelSelectionController: intermediatePanel asignado a '{intermediatePanel.name}'");
            }

            if (advancedField != null && advancedPanel != null)
            {
                advancedField.SetValue(levelController, advancedPanel);
                Debug.Log($"✅ LevelSelectionController: advancedPanel asignado a '{advancedPanel.name}'");
            }

            EditorUtility.SetDirty(levelController);

            // Marca la escena como modificada
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log("✅ Referencias de LevelSelection arregladas. Guarda la escena (Ctrl+S).");
        }

        /// <summary>
        /// Busca un GameObject que contenga el texto especificado en su nombre.
        /// </summary>
        private static GameObject FindGameObjectByPartialName(string partialName)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains(partialName))
                {
                    Debug.Log($"Encontrado: {obj.name}");
                    return obj;
                }
            }

            return null;
        }
    }
}
