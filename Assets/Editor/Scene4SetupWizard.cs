using UnityEngine;
using UnityEditor;
using ASL.DynamicGestures;
using ASL_LearnVR.Gestures;
using ASL_LearnVR.SelfAssessment;

/// <summary>
/// 🧙 WIZARD DE CONFIGURACIÓN AUTOMÁTICA PARA SCENE 4
/// Este wizard te guiará paso a paso en la configuración de gestos dinámicos en Scene 4
/// </summary>
public class Scene4SetupWizard : EditorWindow
{
    private enum SetupStep
    {
        Welcome,
        CheckMultiGestureRecognizer,
        AddStaticPoseAdapter,
        CreateDynamicGestureManager,
        ConfigureReferences,
        ConfigureGestureDefinitions,
        ConfigureSpatialZones,
        TestConfiguration,
        Complete
    }

    private SetupStep currentStep = SetupStep.Welcome;
    private GameObject multiGestureRecognizerObject;
    private GameObject dynamicGestureManagerObject;
    private DynamicGestureRecognizer dynamicGestureRecognizer;
    private DynamicGesturePracticeManager practiceManager;
    private StaticPoseAdapter staticPoseAdapter;
    private MultiGestureRecognizer multiGestureRecognizer;
    private SelfAssessmentController selfAssessmentController;

    // ScriptableObjects
    private DynamicGestureDefinition helloGesture;
    private DynamicGestureDefinition byeGesture;
    private DynamicGestureDefinition jGesture;
    private DynamicGestureDefinition zGesture;

    private Vector2 scrollPosition;

    [MenuItem("ASL Learn VR/🧙 Scene 4 Setup Wizard")]
    public static void ShowWindow()
    {
        Scene4SetupWizard window = GetWindow<Scene4SetupWizard>("Scene 4 Setup Wizard");
        window.minSize = new Vector2(600, 500);
        window.Show();
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();

        EditorGUILayout.Space(10);

        switch (currentStep)
        {
            case SetupStep.Welcome:
                DrawWelcomeStep();
                break;
            case SetupStep.CheckMultiGestureRecognizer:
                DrawCheckMultiGestureRecognizerStep();
                break;
            case SetupStep.AddStaticPoseAdapter:
                DrawAddStaticPoseAdapterStep();
                break;
            case SetupStep.CreateDynamicGestureManager:
                DrawCreateDynamicGestureManagerStep();
                break;
            case SetupStep.ConfigureReferences:
                DrawConfigureReferencesStep();
                break;
            case SetupStep.ConfigureGestureDefinitions:
                DrawConfigureGestureDefinitionsStep();
                break;
            case SetupStep.ConfigureSpatialZones:
                DrawConfigureSpatialZonesStep();
                break;
            case SetupStep.TestConfiguration:
                DrawTestConfigurationStep();
                break;
            case SetupStep.Complete:
                DrawCompleteStep();
                break;
        }

        EditorGUILayout.Space(10);

        DrawNavigationButtons();

        EditorGUILayout.EndScrollView();
    }

    void DrawHeader()
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.LabelField("🧙 Scene 4 Setup Wizard", headerStyle);
        EditorGUILayout.LabelField($"Paso {(int)currentStep + 1} de 9", EditorStyles.centeredGreyMiniLabel);

        // Barra de progreso
        Rect progressRect = EditorGUILayout.GetControlRect(false, 10);
        EditorGUI.ProgressBar(progressRect, (int)currentStep / 8f, "");

        EditorGUILayout.Space(5);
    }

    void DrawWelcomeStep()
    {
        EditorGUILayout.HelpBox("¡Bienvenido al wizard de configuración de Scene 4!\n\n" +
            "Este asistente te guiará paso a paso para configurar:\n" +
            "✅ Gestos dinámicos (Hello, Bye, J, Z)\n" +
            "✅ Zona espacial para diferenciar gestos\n" +
            "✅ Integración con el grid de tiles existente\n\n" +
            "Tiempo estimado: 5-10 minutos", MessageType.Info);

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("📋 Pre-requisitos:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• Debes tener Scene 4 (04_SelfAssessmentMode) abierta", EditorStyles.label);
        EditorGUILayout.LabelField("• MultiGestureRecognizer debe existir en la escena", EditorStyles.label);
        EditorGUILayout.LabelField("• SelfAssessmentController debe existir en la escena", EditorStyles.label);

        EditorGUILayout.Space(10);

        // Verificar scene actual
        var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (currentScene.name.Contains("SelfAssessment") || currentScene.name == "04_SelfAssessmentMode")
        {
            EditorGUILayout.HelpBox($"✅ Scene correcta: {currentScene.name}", MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox($"⚠️ Scene actual: {currentScene.name}\n\n" +
                "Asegúrate de abrir 04_SelfAssessmentMode antes de continuar", MessageType.Warning);
        }
    }

    void DrawCheckMultiGestureRecognizerStep()
    {
        EditorGUILayout.LabelField("🔍 Paso 1: Verificar MultiGestureRecognizer", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox("Vamos a buscar el GameObject que tiene MultiGestureRecognizer en tu escena.", MessageType.Info);

        EditorGUILayout.Space(10);

        multiGestureRecognizerObject = EditorGUILayout.ObjectField(
            "MultiGestureRecognizer Object:",
            multiGestureRecognizerObject,
            typeof(GameObject),
            true
        ) as GameObject;

        EditorGUILayout.Space(10);

        if (GUILayout.Button("🔍 Buscar Automáticamente", GUILayout.Height(30)))
        {
            multiGestureRecognizer = FindObjectOfType<MultiGestureRecognizer>();
            if (multiGestureRecognizer != null)
            {
                multiGestureRecognizerObject = multiGestureRecognizer.gameObject;
                EditorUtility.DisplayDialog("✅ Encontrado",
                    $"MultiGestureRecognizer encontrado en:\n{multiGestureRecognizerObject.name}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("❌ No encontrado",
                    "No se encontró MultiGestureRecognizer en la escena.\n\n" +
                    "Asegúrate de tener Scene 4 abierta.", "OK");
            }
        }

        if (multiGestureRecognizerObject != null)
        {
            multiGestureRecognizer = multiGestureRecognizerObject.GetComponent<MultiGestureRecognizer>();
            if (multiGestureRecognizer != null)
            {
                EditorGUILayout.HelpBox($"✅ MultiGestureRecognizer encontrado en:\n{multiGestureRecognizerObject.name}", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("❌ El GameObject seleccionado no tiene MultiGestureRecognizer", MessageType.Error);
            }
        }
    }

    void DrawAddStaticPoseAdapterStep()
    {
        EditorGUILayout.LabelField("➕ Paso 2: Añadir StaticPoseAdapter", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (multiGestureRecognizerObject == null)
        {
            EditorGUILayout.HelpBox("❌ Primero debes seleccionar el GameObject con MultiGestureRecognizer", MessageType.Error);
            return;
        }

        staticPoseAdapter = multiGestureRecognizerObject.GetComponent<StaticPoseAdapter>();

        if (staticPoseAdapter == null)
        {
            EditorGUILayout.HelpBox(
                "StaticPoseAdapter conecta MultiGestureRecognizer con DynamicGestureRecognizer.\n\n" +
                "Es el 'puente' que permite que gestos dinámicos detecten poses estáticas.", MessageType.Info);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("➕ Añadir StaticPoseAdapter Ahora", GUILayout.Height(35)))
            {
                Undo.RecordObject(multiGestureRecognizerObject, "Add StaticPoseAdapter");
                staticPoseAdapter = multiGestureRecognizerObject.AddComponent<StaticPoseAdapter>();

                // Usar SerializedObject para asignar la referencia
                SerializedObject so = new SerializedObject(staticPoseAdapter);
                SerializedProperty multiGestureRecognizerProp = so.FindProperty("multiGestureRecognizer");
                multiGestureRecognizerProp.objectReferenceValue = multiGestureRecognizer;
                so.ApplyModifiedProperties();

                EditorUtility.SetDirty(multiGestureRecognizerObject);
                EditorUtility.DisplayDialog("✅ Completado",
                    "StaticPoseAdapter añadido y configurado correctamente!", "OK");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("✅ StaticPoseAdapter ya existe en el GameObject", MessageType.None);

            EditorGUILayout.Space(10);

            // Mostrar referencia actual
            SerializedObject so = new SerializedObject(staticPoseAdapter);
            SerializedProperty multiGestureRecognizerProp = so.FindProperty("multiGestureRecognizer");

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(multiGestureRecognizerProp);
            EditorGUI.EndDisabledGroup();

            if (multiGestureRecognizerProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ La referencia a MultiGestureRecognizer está vacía", MessageType.Warning);

                if (GUILayout.Button("🔧 Reparar Referencia"))
                {
                    multiGestureRecognizerProp.objectReferenceValue = multiGestureRecognizer;
                    so.ApplyModifiedProperties();
                    EditorUtility.DisplayDialog("✅ Reparado", "Referencia reparada correctamente", "OK");
                }
            }
        }
    }

    void DrawCreateDynamicGestureManagerStep()
    {
        EditorGUILayout.LabelField("🎯 Paso 3: Crear DynamicGestureManager", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Vamos a crear un nuevo GameObject con:\n" +
            "• DynamicGestureRecognizer (reconoce movimientos)\n" +
            "• DynamicGesturePracticeManager (integra con Scene 4)", MessageType.Info);

        EditorGUILayout.Space(10);

        dynamicGestureManagerObject = EditorGUILayout.ObjectField(
            "DynamicGestureManager:",
            dynamicGestureManagerObject,
            typeof(GameObject),
            true
        ) as GameObject;

        EditorGUILayout.Space(10);

        if (dynamicGestureManagerObject == null)
        {
            if (GUILayout.Button("➕ Crear GameObject Ahora", GUILayout.Height(35)))
            {
                // Crear GameObject
                dynamicGestureManagerObject = new GameObject("DynamicGestureManager");
                Undo.RegisterCreatedObjectUndo(dynamicGestureManagerObject, "Create DynamicGestureManager");

                // Añadir componentes
                dynamicGestureRecognizer = dynamicGestureManagerObject.AddComponent<DynamicGestureRecognizer>();
                practiceManager = dynamicGestureManagerObject.AddComponent<DynamicGesturePracticeManager>();

                EditorUtility.DisplayDialog("✅ Creado",
                    "DynamicGestureManager creado con ambos componentes", "OK");
            }
        }
        else
        {
            dynamicGestureRecognizer = dynamicGestureManagerObject.GetComponent<DynamicGestureRecognizer>();
            practiceManager = dynamicGestureManagerObject.GetComponent<DynamicGesturePracticeManager>();

            if (dynamicGestureRecognizer == null || practiceManager == null)
            {
                EditorGUILayout.HelpBox("⚠️ Faltan componentes en el GameObject", MessageType.Warning);

                if (GUILayout.Button("🔧 Añadir Componentes Faltantes"))
                {
                    if (dynamicGestureRecognizer == null)
                        dynamicGestureRecognizer = dynamicGestureManagerObject.AddComponent<DynamicGestureRecognizer>();
                    if (practiceManager == null)
                        practiceManager = dynamicGestureManagerObject.AddComponent<DynamicGesturePracticeManager>();

                    EditorUtility.DisplayDialog("✅ Añadidos", "Componentes añadidos correctamente", "OK");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✅ GameObject configurado correctamente", MessageType.None);
            }
        }
    }

    void DrawConfigureReferencesStep()
    {
        EditorGUILayout.LabelField("🔗 Paso 4: Configurar Referencias", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (dynamicGestureRecognizer == null || practiceManager == null)
        {
            EditorGUILayout.HelpBox("❌ Primero debes crear el DynamicGestureManager", MessageType.Error);
            return;
        }

        EditorGUILayout.HelpBox("Configurando referencias automáticamente...", MessageType.Info);

        EditorGUILayout.Space(10);

        // Buscar Right Hand Controller
        var rightHandController = GameObject.Find("Right Hand Controller");
        if (rightHandController == null)
        {
            rightHandController = GameObject.Find("RightHandController");
        }

        // Buscar SelfAssessmentController
        if (selfAssessmentController == null)
        {
            selfAssessmentController = FindObjectOfType<SelfAssessmentController>();
        }

        if (GUILayout.Button("🔧 Auto-Configurar Referencias", GUILayout.Height(35)))
        {
            SerializedObject recognizerSO = new SerializedObject(dynamicGestureRecognizer);
            SerializedObject managerSO = new SerializedObject(practiceManager);

            // Configurar DynamicGestureRecognizer
            SerializedProperty poseAdapterProp = recognizerSO.FindProperty("poseAdapterComponent");
            poseAdapterProp.objectReferenceValue = staticPoseAdapter;

            if (rightHandController != null)
            {
                var handTracking = rightHandController.GetComponent<UnityEngine.XR.Hands.XRHandTrackingEvents>();
                SerializedProperty handTrackingProp = recognizerSO.FindProperty("handTrackingEvents");
                handTrackingProp.objectReferenceValue = handTracking;
            }

            SerializedProperty debugModeProp = recognizerSO.FindProperty("debugMode");
            debugModeProp.boolValue = true; // Activar debug mode

            recognizerSO.ApplyModifiedProperties();

            // Configurar DynamicGesturePracticeManager
            SerializedProperty recognizerRefProp = managerSO.FindProperty("dynamicGestureRecognizer");
            recognizerRefProp.objectReferenceValue = dynamicGestureRecognizer;

            SerializedProperty controllerProp = managerSO.FindProperty("selfAssessmentController");
            controllerProp.objectReferenceValue = selfAssessmentController;

            SerializedProperty showDebugProp = managerSO.FindProperty("showDebugLogs");
            showDebugProp.boolValue = true;

            managerSO.ApplyModifiedProperties();

            EditorUtility.SetDirty(dynamicGestureManagerObject);
            EditorUtility.DisplayDialog("✅ Configurado", "Referencias configuradas correctamente", "OK");
        }

        EditorGUILayout.Space(10);

        // Mostrar estado actual
        EditorGUILayout.LabelField("Estado de Referencias:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"• Pose Adapter: {(staticPoseAdapter != null ? "✅" : "❌")}");
        EditorGUILayout.LabelField($"• Hand Tracking: {(rightHandController != null ? "✅" : "❌")}");
        EditorGUILayout.LabelField($"• Self Assessment Controller: {(selfAssessmentController != null ? "✅" : "❌")}");
    }

    void DrawConfigureGestureDefinitionsStep()
    {
        EditorGUILayout.LabelField("📚 Paso 5: Asignar Gesture Definitions", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox("Busca y asigna los ScriptableObjects de gestos dinámicos", MessageType.Info);

        EditorGUILayout.Space(10);

        // Buscar automáticamente
        if (GUILayout.Button("🔍 Buscar Gestos Automáticamente", GUILayout.Height(30)))
        {
            string[] guids = AssetDatabase.FindAssets("t:DynamicGestureDefinition");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DynamicGestureDefinition gesture = AssetDatabase.LoadAssetAtPath<DynamicGestureDefinition>(path);

                if (gesture.gestureName.Contains("Hello"))
                    helloGesture = gesture;
                else if (gesture.gestureName.Contains("Bye"))
                    byeGesture = gesture;
                else if (gesture.gestureName.Contains("J"))
                    jGesture = gesture;
                else if (gesture.gestureName.Contains("Z"))
                    zGesture = gesture;
            }

            EditorUtility.DisplayDialog("✅ Búsqueda completa",
                $"Gestos encontrados:\n" +
                $"• Hello: {(helloGesture != null ? "✅" : "❌")}\n" +
                $"• Bye: {(byeGesture != null ? "✅" : "❌")}\n" +
                $"• J: {(jGesture != null ? "✅" : "❌")}\n" +
                $"• Z: {(zGesture != null ? "✅" : "❌")}", "OK");
        }

        EditorGUILayout.Space(10);

        // Campos manuales
        helloGesture = EditorGUILayout.ObjectField("Hello Gesture:", helloGesture, typeof(DynamicGestureDefinition), false) as DynamicGestureDefinition;
        byeGesture = EditorGUILayout.ObjectField("Bye Gesture:", byeGesture, typeof(DynamicGestureDefinition), false) as DynamicGestureDefinition;
        jGesture = EditorGUILayout.ObjectField("J Gesture:", jGesture, typeof(DynamicGestureDefinition), false) as DynamicGestureDefinition;
        zGesture = EditorGUILayout.ObjectField("Z Gesture:", zGesture, typeof(DynamicGestureDefinition), false) as DynamicGestureDefinition;

        EditorGUILayout.Space(10);

        if (dynamicGestureRecognizer != null && practiceManager != null)
        {
            if (GUILayout.Button("💾 Asignar a Componentes", GUILayout.Height(30)))
            {
                SerializedObject recognizerSO = new SerializedObject(dynamicGestureRecognizer);
                SerializedProperty gestureDefsProp = recognizerSO.FindProperty("gestureDefinitions");
                gestureDefsProp.ClearArray();

                int index = 0;
                if (helloGesture != null)
                {
                    gestureDefsProp.InsertArrayElementAtIndex(index);
                    gestureDefsProp.GetArrayElementAtIndex(index).objectReferenceValue = helloGesture;
                    index++;
                }
                if (byeGesture != null)
                {
                    gestureDefsProp.InsertArrayElementAtIndex(index);
                    gestureDefsProp.GetArrayElementAtIndex(index).objectReferenceValue = byeGesture;
                    index++;
                }
                if (jGesture != null)
                {
                    gestureDefsProp.InsertArrayElementAtIndex(index);
                    gestureDefsProp.GetArrayElementAtIndex(index).objectReferenceValue = jGesture;
                    index++;
                }
                if (zGesture != null)
                {
                    gestureDefsProp.InsertArrayElementAtIndex(index);
                    gestureDefsProp.GetArrayElementAtIndex(index).objectReferenceValue = zGesture;
                    index++;
                }

                recognizerSO.ApplyModifiedProperties();

                // También asignar a practice manager
                SerializedObject managerSO = new SerializedObject(practiceManager);
                SerializedProperty practiceGesturesProp = managerSO.FindProperty("practiceGestures");
                practiceGesturesProp.ClearArray();

                index = 0;
                if (helloGesture != null)
                {
                    practiceGesturesProp.InsertArrayElementAtIndex(index);
                    practiceGesturesProp.GetArrayElementAtIndex(index).objectReferenceValue = helloGesture;
                    index++;
                }
                if (byeGesture != null)
                {
                    practiceGesturesProp.InsertArrayElementAtIndex(index);
                    practiceGesturesProp.GetArrayElementAtIndex(index).objectReferenceValue = byeGesture;
                    index++;
                }
                if (jGesture != null)
                {
                    practiceGesturesProp.InsertArrayElementAtIndex(index);
                    practiceGesturesProp.GetArrayElementAtIndex(index).objectReferenceValue = jGesture;
                    index++;
                }
                if (zGesture != null)
                {
                    practiceGesturesProp.InsertArrayElementAtIndex(index);
                    practiceGesturesProp.GetArrayElementAtIndex(index).objectReferenceValue = zGesture;
                    index++;
                }

                managerSO.ApplyModifiedProperties();

                EditorUtility.SetDirty(dynamicGestureManagerObject);
                EditorUtility.DisplayDialog("✅ Asignado", $"{index} gestos asignados correctamente", "OK");
            }
        }
    }

    void DrawConfigureSpatialZonesStep()
    {
        EditorGUILayout.LabelField("📍 Paso 6: Configurar Zonas Espaciales", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Las zonas espaciales permiten diferenciar Hello (cerca cabeza) de Bye (extendido adelante)", MessageType.Info);

        EditorGUILayout.Space(10);

        if (helloGesture != null)
        {
            EditorGUILayout.LabelField("Hello Gesture:", EditorStyles.boldLabel);
            if (GUILayout.Button("⚙️ Configurar Zona Espacial para Hello"))
            {
                SerializedObject so = new SerializedObject(helloGesture);

                so.FindProperty("requiresSpatialZone").boolValue = true;
                so.FindProperty("zoneCenter").vector3Value = new Vector3(0, 0.1f, 0.3f);
                so.FindProperty("zoneRadius").floatValue = 0.20f;
                so.FindProperty("zoneValidationTiming").enumValueIndex = (int)PoseTimingRequirement.During;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(helloGesture);

                EditorUtility.DisplayDialog("✅ Configurado",
                    "Zona espacial de Hello configurada:\n" +
                    "• Centro: (0, 0.1, 0.3) - Cerca cabeza\n" +
                    "• Radio: 0.20m\n" +
                    "• Timing: During", "OK");
            }
        }

        EditorGUILayout.Space(5);

        if (byeGesture != null)
        {
            EditorGUILayout.LabelField("Bye Gesture:", EditorStyles.boldLabel);
            if (GUILayout.Button("⚙️ Configurar Zona Espacial para Bye"))
            {
                SerializedObject so = new SerializedObject(byeGesture);

                so.FindProperty("requiresSpatialZone").boolValue = true;
                so.FindProperty("zoneCenter").vector3Value = new Vector3(0, -0.1f, 0.5f);
                so.FindProperty("zoneRadius").floatValue = 0.25f;
                so.FindProperty("zoneValidationTiming").enumValueIndex = (int)PoseTimingRequirement.During;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(byeGesture);

                EditorUtility.DisplayDialog("✅ Configurado",
                    "Zona espacial de Bye configurada:\n" +
                    "• Centro: (0, -0.1, 0.5) - Extendido\n" +
                    "• Radio: 0.25m\n" +
                    "• Timing: During", "OK");
            }
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "💡 Tip: Puedes ajustar estos valores después en los ScriptableObjects", MessageType.Info);
    }

    void DrawTestConfigurationStep()
    {
        EditorGUILayout.LabelField("🧪 Paso 7: Verificar Configuración", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox("Vamos a verificar que todo está configurado correctamente", MessageType.Info);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("🔍 Verificar Todo", GUILayout.Height(35)))
        {
            string report = "VERIFICACIÓN DE CONFIGURACIÓN\n\n";
            bool allGood = true;

            // 1. MultiGestureRecognizer
            if (multiGestureRecognizer != null)
                report += "✅ MultiGestureRecognizer encontrado\n";
            else
            {
                report += "❌ MultiGestureRecognizer NO encontrado\n";
                allGood = false;
            }

            // 2. StaticPoseAdapter
            if (staticPoseAdapter != null)
                report += "✅ StaticPoseAdapter añadido\n";
            else
            {
                report += "❌ StaticPoseAdapter falta\n";
                allGood = false;
            }

            // 3. DynamicGestureRecognizer
            if (dynamicGestureRecognizer != null)
                report += "✅ DynamicGestureRecognizer creado\n";
            else
            {
                report += "❌ DynamicGestureRecognizer falta\n";
                allGood = false;
            }

            // 4. DynamicGesturePracticeManager
            if (practiceManager != null)
                report += "✅ DynamicGesturePracticeManager creado\n";
            else
            {
                report += "❌ DynamicGesturePracticeManager falta\n";
                allGood = false;
            }

            // 5. Gesture Definitions
            int gestureCount = 0;
            if (helloGesture != null) gestureCount++;
            if (byeGesture != null) gestureCount++;
            if (jGesture != null) gestureCount++;
            if (zGesture != null) gestureCount++;

            report += $"✅ {gestureCount}/4 Gesture Definitions asignados\n";

            // 6. Referencias
            if (dynamicGestureRecognizer != null)
            {
                SerializedObject so = new SerializedObject(dynamicGestureRecognizer);
                var poseAdapter = so.FindProperty("poseAdapterComponent").objectReferenceValue;
                var handTracking = so.FindProperty("handTrackingEvents").objectReferenceValue;

                if (poseAdapter != null)
                    report += "✅ Pose Adapter asignado\n";
                else
                {
                    report += "❌ Pose Adapter NO asignado\n";
                    allGood = false;
                }

                if (handTracking != null)
                    report += "✅ Hand Tracking asignado\n";
                else
                {
                    report += "⚠️ Hand Tracking NO asignado (opcional)\n";
                }
            }

            report += "\n";
            if (allGood && gestureCount >= 2)
                report += "🎉 ¡TODO CONFIGURADO CORRECTAMENTE!\n\nPuedes proceder al siguiente paso.";
            else
                report += "⚠️ Hay configuraciones pendientes.\nRevisa los pasos anteriores.";

            EditorUtility.DisplayDialog("Resultado de Verificación", report, "OK");
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Checklist:", EditorStyles.boldLabel);
        DrawChecklistItem("MultiGestureRecognizer", multiGestureRecognizer != null);
        DrawChecklistItem("StaticPoseAdapter", staticPoseAdapter != null);
        DrawChecklistItem("DynamicGestureManager", dynamicGestureManagerObject != null);
        DrawChecklistItem("Gesture Definitions", helloGesture != null || byeGesture != null);
        DrawChecklistItem("Zonas Espaciales", (helloGesture?.requiresSpatialZone ?? false) || (byeGesture?.requiresSpatialZone ?? false));
    }

    void DrawCompleteStep()
    {
        EditorGUILayout.LabelField("🎉 ¡Configuración Completa!", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Scene 4 está configurada correctamente para gestos dinámicos.\n\n" +
            "Próximos pasos:\n" +
            "1. Guarda la escena (Ctrl+S)\n" +
            "2. Presiona Play para testear\n" +
            "3. Revisa Console para logs detallados (Debug Mode activado)\n" +
            "4. Ajusta parámetros según necesidad", MessageType.Info);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("📄 Abrir Guía de Uso", GUILayout.Height(30)))
        {
            string guidePath = "Assets/Scripts/DynamicGestures/GUIA_IMPLEMENTACION_v3_FINAL.md";
            var guideAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(guidePath);
            if (guideAsset != null)
            {
                Selection.activeObject = guideAsset;
                EditorGUIUtility.PingObject(guideAsset);
            }
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("📋 Abrir Verificación Completa", GUILayout.Height(30)))
        {
            string verificationPath = "Assets/../VERIFICACION_INTEGRACION_COMPLETA.md";
            var verificationAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(verificationPath);
            if (verificationAsset != null)
            {
                Selection.activeObject = verificationAsset;
                EditorGUIUtility.PingObject(verificationAsset);
            }
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("💾 Guardar Escena Ahora", GUILayout.Height(35)))
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            EditorUtility.DisplayDialog("✅ Guardado", "Escena guardada correctamente", "OK");
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "💡 Recuerda:\n" +
            "• Debug Mode está activado para ayudarte a testear\n" +
            "• Revisa Console para logs detallados durante testing\n" +
            "• Puedes ajustar parámetros en Inspector después", MessageType.Info);
    }

    void DrawChecklistItem(string label, bool completed)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = completed ? Color.green : Color.red;
        EditorGUILayout.LabelField($"{(completed ? "✅" : "❌")} {label}", style);
    }

    void DrawNavigationButtons()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = currentStep > SetupStep.Welcome;
        if (GUILayout.Button("⬅️ Anterior", GUILayout.Height(30)))
        {
            currentStep--;
        }
        GUI.enabled = true;

        GUILayout.FlexibleSpace();

        if (currentStep < SetupStep.Complete)
        {
            if (GUILayout.Button("Siguiente ➡️", GUILayout.Height(30), GUILayout.Width(150)))
            {
                currentStep++;
            }
        }
        else
        {
            if (GUILayout.Button("🔄 Reiniciar Wizard", GUILayout.Height(30), GUILayout.Width(150)))
            {
                if (EditorUtility.DisplayDialog("Reiniciar",
                    "¿Quieres reiniciar el wizard?\n\nEsto NO eliminará tu configuración actual.", "Sí", "No"))
                {
                    currentStep = SetupStep.Welcome;
                }
            }
        }

        EditorGUILayout.EndHorizontal();
    }
}
