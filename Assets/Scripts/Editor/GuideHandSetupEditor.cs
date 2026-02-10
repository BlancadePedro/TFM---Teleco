#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using ASL_LearnVR.LearningModule.GuideHand;
using ASL_LearnVR.LearningModule;

namespace ASL_LearnVR.Editor
{
    /// <summary>
    /// Editor utility para configurar las ghost hands con GuideHandPoseApplier.
    /// </summary>
    public class GuideHandSetupEditor : EditorWindow
    {
        private GameObject leftHandObject;
        private GameObject rightHandObject;
        private GhostHandPlayer ghostHandPlayer;

        [MenuItem("ASL Learn VR/Guide Hand Setup")]
        public static void ShowWindow()
        {
            GetWindow<GuideHandSetupEditor>("Guide Hand Setup");
        }

        void OnGUI()
        {
            GUILayout.Label("Guide Hand Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Referencias a las manos
            EditorGUILayout.LabelField("Hand References", EditorStyles.boldLabel);
            leftHandObject = (GameObject)EditorGUILayout.ObjectField("Left Ghost Hand", leftHandObject, typeof(GameObject), true);
            rightHandObject = (GameObject)EditorGUILayout.ObjectField("Right Ghost Hand", rightHandObject, typeof(GameObject), true);
            ghostHandPlayer = (GhostHandPlayer)EditorGUILayout.ObjectField("Ghost Hand Player", ghostHandPlayer, typeof(GhostHandPlayer), true);

            EditorGUILayout.Space();

            // Botones de configuración
            EditorGUILayout.LabelField("Setup Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Auto-Setup Left Hand"))
            {
                SetupHand(leftHandObject, "Left");
            }

            if (GUILayout.Button("Auto-Setup Right Hand"))
            {
                SetupHand(rightHandObject, "Right");
            }

            if (GUILayout.Button("Setup Both Hands"))
            {
                SetupHand(leftHandObject, "Left");
                SetupHand(rightHandObject, "Right");
            }

            EditorGUILayout.Space();

            // Herramientas de debug
            EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("List Joints in Hierarchy"))
            {
                ListJointsInHierarchy();
            }

            if (GUILayout.Button("Validate Joint Mapping"))
            {
                ValidateMapping();
            }

            EditorGUILayout.Space();

            // Prueba de poses
            EditorGUILayout.LabelField("Test Poses (Play Mode)", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            if (GUILayout.Button("Test Pose: A"))
                TestPose("A");
            if (GUILayout.Button("Test Pose: B"))
                TestPose("B");
            if (GUILayout.Button("Test Pose: L"))
                TestPose("L");
            if (GUILayout.Button("Test Pose: V"))
                TestPose("V");
            if (GUILayout.Button("Test Pose: 5 (Open Hand)"))
                TestPose("5");
            if (GUILayout.Button("Reset to Open Hand"))
                ResetPose();

            EditorGUI.EndDisabledGroup();
        }

        private void SetupHand(GameObject handObject, string handName)
        {
            if (handObject == null)
            {
                EditorUtility.DisplayDialog("Error", $"No {handName} hand object assigned.", "OK");
                return;
            }

            // Añadir o obtener GuideHandPoseApplier
            var poseApplier = handObject.GetComponent<GuideHandPoseApplier>();
            if (poseApplier == null)
            {
                poseApplier = Undo.AddComponent<GuideHandPoseApplier>(handObject);
            }

            // Auto-mapear joints
            poseApplier.AutoMapJointsFromHierarchy(handObject.transform);

            // Validar
            if (poseApplier.ValidateJointMapping())
            {
                EditorUtility.DisplayDialog("Success", $"{handName} hand setup complete! All critical joints mapped.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Warning", $"{handName} hand setup complete, but some joints are missing. Check console for details.", "OK");
            }

            EditorUtility.SetDirty(handObject);
        }

        private void ListJointsInHierarchy()
        {
            GameObject target = Selection.activeGameObject;
            if (target == null)
            {
                target = leftHandObject ?? rightHandObject;
            }

            if (target == null)
            {
                Debug.LogWarning("Select a hand object or assign one in the setup window.");
                return;
            }

            Debug.Log($"=== Joints in {target.name} ===");

            var transforms = target.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                string indent = new string(' ', GetDepth(t, target.transform) * 2);
                Debug.Log($"{indent}{t.name}");
            }

            Debug.Log($"=== Total: {transforms.Length} transforms ===");
        }

        private int GetDepth(Transform child, Transform root)
        {
            int depth = 0;
            Transform current = child;
            while (current != null && current != root)
            {
                depth++;
                current = current.parent;
            }
            return depth;
        }

        private void ValidateMapping()
        {
            if (leftHandObject != null)
            {
                var leftApplier = leftHandObject.GetComponent<GuideHandPoseApplier>();
                if (leftApplier != null)
                {
                    Debug.Log($"Left Hand Validation: {(leftApplier.ValidateJointMapping() ? "PASSED" : "FAILED")}");
                }
                else
                {
                    Debug.LogWarning("Left hand doesn't have GuideHandPoseApplier component.");
                }
            }

            if (rightHandObject != null)
            {
                var rightApplier = rightHandObject.GetComponent<GuideHandPoseApplier>();
                if (rightApplier != null)
                {
                    Debug.Log($"Right Hand Validation: {(rightApplier.ValidateJointMapping() ? "PASSED" : "FAILED")}");
                }
                else
                {
                    Debug.LogWarning("Right hand doesn't have GuideHandPoseApplier component.");
                }
            }
        }

        private void TestPose(string poseName)
        {
            if (ghostHandPlayer != null)
            {
                ghostHandPlayer.ApplyPoseByName(poseName);
                Debug.Log($"Testing pose: {poseName}");
            }
            else
            {
                // Intentar aplicar directamente a los pose appliers
                if (rightHandObject != null)
                {
                    var applier = rightHandObject.GetComponent<GuideHandPoseApplier>();
                    if (applier != null)
                    {
                        applier.ApplyPoseByName(poseName);
                        Debug.Log($"Applied pose {poseName} to right hand");
                    }
                }

                if (leftHandObject != null)
                {
                    var applier = leftHandObject.GetComponent<GuideHandPoseApplier>();
                    if (applier != null)
                    {
                        applier.ApplyPoseByName(poseName);
                        Debug.Log($"Applied pose {poseName} to left hand");
                    }
                }
            }
        }

        private void ResetPose()
        {
            if (ghostHandPlayer != null)
            {
                ghostHandPlayer.ResetPose();
            }
            else
            {
                if (rightHandObject != null)
                {
                    var applier = rightHandObject.GetComponent<GuideHandPoseApplier>();
                    applier?.ResetToOriginal();
                }

                if (leftHandObject != null)
                {
                    var applier = leftHandObject.GetComponent<GuideHandPoseApplier>();
                    applier?.ResetToOriginal();
                }
            }
        }
    }

    /// <summary>
    /// Custom editor para GuideHandPoseApplier con botones de prueba.
    /// </summary>
    [CustomEditor(typeof(GuideHandPoseApplier))]
    public class GuideHandPoseApplierEditor : UnityEditor.Editor
    {
        private string testPoseName = "A";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GuideHandPoseApplier applier = (GuideHandPoseApplier)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Setup Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Auto-Map Joints from Hierarchy"))
            {
                applier.AutoMapJointsFromHierarchy(applier.transform);
                EditorUtility.SetDirty(applier);
            }

            if (GUILayout.Button("Validate Joint Mapping"))
            {
                bool isValid = applier.ValidateJointMapping();
                Debug.Log($"Joint mapping validation: {(isValid ? "PASSED" : "FAILED")}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Test Poses (Play Mode)", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            EditorGUILayout.BeginHorizontal();
            testPoseName = EditorGUILayout.TextField("Pose Name:", testPoseName);
            if (GUILayout.Button("Apply"))
            {
                applier.ApplyPoseByName(testPoseName);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("A")) applier.ApplyPoseByName("A");
            if (GUILayout.Button("B")) applier.ApplyPoseByName("B");
            if (GUILayout.Button("C")) applier.ApplyPoseByName("C");
            if (GUILayout.Button("D")) applier.ApplyPoseByName("D");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("L")) applier.ApplyPoseByName("L");
            if (GUILayout.Button("V")) applier.ApplyPoseByName("V");
            if (GUILayout.Button("Y")) applier.ApplyPoseByName("Y");
            if (GUILayout.Button("5")) applier.ApplyPoseByName("5");
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Reset to Original"))
            {
                applier.ResetToOriginal();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Rotation Test", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Usa esto para probar qué eje de rotación produce qué movimiento.\n" +
                "Finger: 0=Thumb, 1=Index, 2=Middle, 3=Ring, 4=Pinky\n" +
                "Joint: 0=Metacarpal, 1=Proximal, 2=Intermediate, 3=Distal\n" +
                "Axis: 0=X(right), 1=Y(up), 2=Z(forward)",
                MessageType.Info);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Rotation"))
            {
                applier.TestDirectRotation();
            }
            if (GUILayout.Button("Reset Joint"))
            {
                applier.ResetDebugJoint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Current Pose: {applier.CurrentPoseName}", EditorStyles.helpBox);
        }
    }
}
#endif
