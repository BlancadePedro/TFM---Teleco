using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ASLLearnVR.Editor
{
    /// <summary>
    /// Editor tool para arreglar el setup XR de todas las escenas del proyecto.
    /// Soluciona:
    /// 1. Manos duplicadas (elimina XR Origin Hands prefab)
    /// 2. UI no funciona en VR (activa TrackedDeviceGraphicRaycaster)
    /// 3. Añade componentes necesarios para interacción UI con hands
    /// </summary>
    public class XRSetupFixer : EditorWindow
    {
        private bool fixDuplicateHands = true;
        private bool fixUIRaycasters = true;
        private bool addInteractionComponents = true;
        private bool addHandInteractionPrefabs = true;

        private Vector2 scrollPosition;
        private string logOutput = "";

        [MenuItem("Tools/ASL/Fix XR Setup")]
        public static void ShowWindow()
        {
            GetWindow<XRSetupFixer>("XR Setup Fixer");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("ASL Learn VR - XR Setup Fixer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Esta herramienta arregla automáticamente los problemas de XR en todas las escenas:\n\n" +
                "✓ Elimina manos duplicadas\n" +
                "✓ Activa TrackedDeviceGraphicRaycaster para UI en VR\n" +
                "✓ Añade XR Interaction Manager\n" +
                "✓ Añade Hand Interaction prefabs para UI",
                MessageType.Info);

            EditorGUILayout.Space();

            fixDuplicateHands = EditorGUILayout.ToggleLeft("Fix: Eliminar manos duplicadas (XR Origin Hands prefab)", fixDuplicateHands);
            fixUIRaycasters = EditorGUILayout.ToggleLeft("Fix: Activar TrackedDeviceGraphicRaycaster en Canvas", fixUIRaycasters);
            addInteractionComponents = EditorGUILayout.ToggleLeft("Fix: Añadir XR Interaction Manager", addInteractionComponents);
            addHandInteractionPrefabs = EditorGUILayout.ToggleLeft("Fix: Añadir Hand Interaction prefabs", addHandInteractionPrefabs);

            EditorGUILayout.Space();

            if (GUILayout.Button("Fix All Scenes", GUILayout.Height(40)))
            {
                FixAllScenes();
            }

            if (GUILayout.Button("Fix Current Scene Only", GUILayout.Height(30)))
            {
                FixCurrentScene();
            }

            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(logOutput))
            {
                EditorGUILayout.LabelField("Log Output:", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(logOutput, GUILayout.Height(200));
            }

            EditorGUILayout.EndScrollView();
        }

        private void FixAllScenes()
        {
            logOutput = "=== FIXING ALL SCENES ===\n\n";

            string[] scenePaths = new string[]
            {
                "Assets/01_MainMenu.unity",
                "Assets/02_LevelSelection.unity",
                "Assets/03_LearningModule.unity",
                "Assets/04_SelfAssessmentMode.unity"
            };

            foreach (string scenePath in scenePaths)
            {
                if (System.IO.File.Exists(scenePath))
                {
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    logOutput += $"--- Processing: {scene.name} ---\n";
                    FixScene(scene);
                    EditorSceneManager.SaveScene(scene);
                    logOutput += $"✓ Saved: {scene.name}\n\n";
                }
                else
                {
                    logOutput += $"✗ Scene not found: {scenePath}\n\n";
                }
            }

            logOutput += "=== ALL SCENES FIXED ===\n";
            Debug.Log(logOutput);
        }

        private void FixCurrentScene()
        {
            logOutput = "=== FIXING CURRENT SCENE ===\n\n";
            Scene scene = SceneManager.GetActiveScene();
            logOutput += $"--- Processing: {scene.name} ---\n";
            FixScene(scene);
            EditorSceneManager.SaveScene(scene);
            logOutput += $"✓ Saved: {scene.name}\n\n";
            logOutput += "=== CURRENT SCENE FIXED ===\n";
            Debug.Log(logOutput);
        }

        private void FixScene(Scene scene)
        {
            if (fixDuplicateHands)
            {
                FixDuplicateHands(scene);
            }

            if (fixUIRaycasters)
            {
                FixUIRaycasters(scene);
            }

            if (addInteractionComponents)
            {
                AddXRInteractionManager(scene);
            }

            if (addHandInteractionPrefabs)
            {
                AddHandInteractionPrefabs(scene);
            }
        }

        private void FixDuplicateHands(Scene scene)
        {
            logOutput += "  [Fix Duplicate Hands]\n";

            // Buscar y eliminar el prefab "XR Origin Hands (XR Rig)"
            GameObject[] rootObjects = scene.GetRootGameObjects();
            int removedCount = 0;

            foreach (GameObject root in rootObjects)
            {
                if (root.name.Contains("XR Origin Hands") && root.name.Contains("XR Rig"))
                {
                    logOutput += $"    - Removing: {root.name}\n";
                    Object.DestroyImmediate(root);
                    removedCount++;
                }
            }

            if (removedCount == 0)
            {
                logOutput += "    - No XR Origin Hands prefab found (OK)\n";
            }
            else
            {
                logOutput += $"    ✓ Removed {removedCount} XR Origin Hands prefab(s)\n";
            }
        }

        private void FixUIRaycasters(Scene scene)
        {
            logOutput += "  [Fix UI Raycasters]\n";

            Canvas[] allCanvas = Object.FindObjectsOfType<Canvas>();
            int fixedCount = 0;

            foreach (Canvas canvas in allCanvas)
            {
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    // Desactivar GraphicRaycaster estándar
                    GraphicRaycaster standardRaycaster = canvas.GetComponent<GraphicRaycaster>();
                    if (standardRaycaster != null)
                    {
                        standardRaycaster.enabled = false;
                        logOutput += $"    - Disabled GraphicRaycaster on: {canvas.name}\n";
                    }

                    // Activar TrackedDeviceGraphicRaycaster
                    TrackedDeviceGraphicRaycaster trackedRaycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
                    if (trackedRaycaster == null)
                    {
                        trackedRaycaster = canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                        logOutput += $"    + Added TrackedDeviceGraphicRaycaster to: {canvas.name}\n";
                    }
                    else
                    {
                        trackedRaycaster.enabled = true;
                        logOutput += $"    - Enabled TrackedDeviceGraphicRaycaster on: {canvas.name}\n";
                    }

                    fixedCount++;
                }
            }

            if (fixedCount == 0)
            {
                logOutput += "    - No World Space Canvas found\n";
            }
            else
            {
                logOutput += $"    ✓ Fixed {fixedCount} Canvas\n";
            }
        }

        private void AddXRInteractionManager(Scene scene)
        {
            logOutput += "  [Add XR Interaction Manager]\n";

            XRInteractionManager existingManager = Object.FindObjectOfType<XRInteractionManager>();

            if (existingManager == null)
            {
                GameObject managerGO = new GameObject("XR Interaction Manager");
                managerGO.AddComponent<XRInteractionManager>();
                logOutput += "    + Added XR Interaction Manager\n";
            }
            else
            {
                logOutput += "    - XR Interaction Manager already exists (OK)\n";
            }
        }

        private void AddHandInteractionPrefabs(Scene scene)
        {
            logOutput += "  [Add Hand Interaction Prefabs]\n";

            // Primero, eliminar TODOS los duplicados existentes
            GameObject[] rootObjects = scene.GetRootGameObjects();
            int removedLeft = 0;
            int removedRight = 0;

            foreach (GameObject root in rootObjects)
            {
                if (root.name.Contains("LeftHandInteraction") || root.name.Contains("Left Hand Interaction"))
                {
                    Object.DestroyImmediate(root);
                    removedLeft++;
                }
                else if (root.name.Contains("RightHandInteraction") || root.name.Contains("Right Hand Interaction"))
                {
                    Object.DestroyImmediate(root);
                    removedRight++;
                }
            }

            if (removedLeft > 0)
                logOutput += $"    - Removed {removedLeft} duplicate LeftHandInteraction instance(s)\n";
            if (removedRight > 0)
                logOutput += $"    - Removed {removedRight} duplicate RightHandInteraction instance(s)\n";

            // Cargar prefabs
            GameObject leftHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LeftHandInteraction.prefab");
            GameObject rightHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RightHandInteraction.prefab");

            if (leftHandPrefab == null || rightHandPrefab == null)
            {
                logOutput += "    ✗ Hand Interaction prefabs not found in Assets/Prefabs/\n";
                logOutput += "      Please ensure LeftHandInteraction.prefab and RightHandInteraction.prefab exist\n";
                return;
            }

            // Siempre añadir nuevos (ya eliminamos todos los duplicados arriba)
            PrefabUtility.InstantiatePrefab(leftHandPrefab);
            logOutput += "    + Added LeftHandInteraction prefab\n";

            PrefabUtility.InstantiatePrefab(rightHandPrefab);
            logOutput += "    + Added RightHandInteraction prefab\n";
        }
    }
}
