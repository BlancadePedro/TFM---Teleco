using UnityEngine;
using UnityEditor;
using ASL_LearnVR.Gestures;
using ASL_LearnVR.Data;

namespace ASL.DynamicGestures.Editor
{
    /// <summary>
    /// Helper de Unity Editor para configurar automáticamente el sistema de gestos dinámicos.
    /// Uso: Tools → ASL → Setup Dynamic Gestures
    /// </summary>
    public class DynamicGestureSetupHelper : EditorWindow
    {
        private MultiGestureRecognizer multiGestureRecognizer;
        private CategoryData alphabetCategory;
        private bool autoPopulateTargetSigns = true;

        [MenuItem("Tools/ASL/Setup Dynamic Gestures")]
        public static void ShowWindow()
        {
            GetWindow<DynamicGestureSetupHelper>("Dynamic Gesture Setup");
        }

        void OnGUI()
        {
            GUILayout.Label("Configuración Automática de Gestos Dinámicos", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Esta herramienta te ayudará a configurar el sistema de gestos dinámicos automáticamente.\n\n" +
                "Paso 1: Asigna MultiGestureRecognizer\n" +
                "Paso 2: Asigna Category_Alphabet\n" +
                "Paso 3: Click en 'Auto-Configurar'",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Campo para MultiGestureRecognizer
            multiGestureRecognizer = (MultiGestureRecognizer)EditorGUILayout.ObjectField(
                "Multi Gesture Recognizer",
                multiGestureRecognizer,
                typeof(MultiGestureRecognizer),
                true
            );

            // Campo para CategoryData
            alphabetCategory = (CategoryData)EditorGUILayout.ObjectField(
                "Category Alphabet",
                alphabetCategory,
                typeof(CategoryData),
                false
            );

            EditorGUILayout.Space();

            autoPopulateTargetSigns = EditorGUILayout.Toggle(
                "Auto-llenar Target Signs",
                autoPopulateTargetSigns
            );

            EditorGUILayout.Space();

            GUI.enabled = multiGestureRecognizer != null && alphabetCategory != null;

            if (GUILayout.Button("Auto-Configurar Target Signs", GUILayout.Height(40)))
            {
                AutoConfigureTargetSigns();
            }

            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Ver Guía de Setup Completa"))
            {
                string path = "Assets/../SETUP_DEFINITIVO_J_Z.md";
                Application.OpenURL("file://" + System.IO.Path.GetFullPath(path));
            }
        }

        private void AutoConfigureTargetSigns()
        {
            if (multiGestureRecognizer == null || alphabetCategory == null)
            {
                EditorUtility.DisplayDialog("Error", "Falta asignar MultiGestureRecognizer o CategoryData", "OK");
                return;
            }

            // Obtener todos los SignData del alphabetCategory
            var signs = alphabetCategory.signs;

            if (signs == null || signs.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "CategoryData no tiene signos asignados", "OK");
                return;
            }

            // Registrar Undo para poder deshacer
            Undo.RecordObject(multiGestureRecognizer, "Auto-configure Target Signs");

            // Usar reflexión para acceder a targetSigns (es privado)
            var field = typeof(MultiGestureRecognizer).GetField("targetSigns",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "No se pudo acceder al campo targetSigns. Versión de MultiGestureRecognizer incompatible.",
                    "OK");
                return;
            }

            var targetSignsList = field.GetValue(multiGestureRecognizer) as System.Collections.Generic.List<SignData>;

            if (targetSignsList == null)
            {
                targetSignsList = new System.Collections.Generic.List<SignData>();
                field.SetValue(multiGestureRecognizer, targetSignsList);
            }

            // Limpiar lista existente
            targetSignsList.Clear();

            // Añadir todos los signos
            int added = 0;
            foreach (var sign in signs)
            {
                if (sign != null)
                {
                    targetSignsList.Add(sign);
                    added++;
                }
            }

            // Marcar como modificado
            EditorUtility.SetDirty(multiGestureRecognizer);

            EditorUtility.DisplayDialog("Éxito",
                $"Se añadieron {added} SignData a Target Signs.\n\n" +
                $"Ahora verifica que en tus assets J.asset y Z.asset:\n" +
                $"- Pose Name coincida EXACTAMENTE con signName de los SignData",
                "OK");

            Debug.Log($"[DynamicGestureSetup] Configurados {added} Target Signs en MultiGestureRecognizer");
        }
    }
}
