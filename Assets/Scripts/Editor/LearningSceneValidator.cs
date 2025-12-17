using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ASL_LearnVR.LearningModule;
using ASL_LearnVR.Gestures;

namespace ASL_LearnVR.Editor
{
    /// <summary>
    /// Valida que la escena Learning esté correctamente configurada.
    /// Menú: Tools > ASL Learn VR > Validate Learning Scene
    /// </summary>
    public class LearningSceneValidator : MonoBehaviour
    {
        [MenuItem("Tools/ASL Learn VR/Validate Learning Scene")]
        public static void ValidateScene()
        {
            Debug.Log("=== INICIANDO VALIDACIÓN DE ESCENA LEARNING ===");

            bool allOk = true;

            // 1. Busca el LearningController
            LearningController controller = FindObjectOfType<LearningController>();
            if (controller == null)
            {
                Debug.LogError("❌ NO SE ENCONTRÓ LearningController en la escena!");
                allOk = false;
                return;
            }
            Debug.Log("✅ LearningController encontrado");

            // 2. Verifica los recognizers usando reflection
            var rightHandRecognizerField = controller.GetType().GetField("rightHandRecognizer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var leftHandRecognizerField = controller.GetType().GetField("leftHandRecognizer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dynamicRecognizerField = controller.GetType().GetField("dynamicGestureRecognizer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (rightHandRecognizerField == null || leftHandRecognizerField == null || dynamicRecognizerField == null)
            {
                Debug.LogError("❌ No se pudieron obtener los campos de recognizers");
                allOk = false;
                return;
            }

            GestureRecognizer rightHandRecognizer = rightHandRecognizerField.GetValue(controller) as GestureRecognizer;
            GestureRecognizer leftHandRecognizer = leftHandRecognizerField.GetValue(controller) as GestureRecognizer;
            DynamicGestureRecognizer dynamicRecognizer = dynamicRecognizerField.GetValue(controller) as DynamicGestureRecognizer;

            // 3. Verifica Right Hand Recognizer
            if (rightHandRecognizer == null)
            {
                Debug.LogError("❌ Right Hand Recognizer NO está asignado en LearningController!");
                allOk = false;
            }
            else
            {
                Debug.Log("✅ Right Hand Recognizer asignado");

                // Verifica si tiene handTrackingEvents
                var handTrackingField = rightHandRecognizer.GetType().GetField("handTrackingEvents",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (handTrackingField != null)
                {
                    var handTracking = handTrackingField.GetValue(rightHandRecognizer);
                    if (handTracking == null)
                    {
                        Debug.LogError("❌❌❌ RIGHT HAND RECOGNIZER NO TIENE 'handTrackingEvents' ASIGNADO!");
                        Debug.LogError("SOLUCIÓN: Selecciona el GameObject con GestureRecognizer y arrastra 'XR Origin/Camera Offset/Right Hand' al campo 'Hand Tracking Events'");
                        allOk = false;
                    }
                    else
                    {
                        Debug.Log("✅ Right Hand Recognizer tiene handTrackingEvents asignado");
                    }
                }
            }

            // 4. Verifica Left Hand Recognizer
            if (leftHandRecognizer == null)
            {
                Debug.LogWarning("⚠️ Left Hand Recognizer NO está asignado (no es crítico si solo usas la mano derecha)");
            }
            else
            {
                Debug.Log("✅ Left Hand Recognizer asignado");

                var handTrackingField = leftHandRecognizer.GetType().GetField("handTrackingEvents",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (handTrackingField != null)
                {
                    var handTracking = handTrackingField.GetValue(leftHandRecognizer);
                    if (handTracking == null)
                    {
                        Debug.LogWarning("⚠️ LEFT HAND RECOGNIZER NO TIENE 'handTrackingEvents' ASIGNADO");
                    }
                    else
                    {
                        Debug.Log("✅ Left Hand Recognizer tiene handTrackingEvents asignado");
                    }
                }
            }

            // 5. Verifica Dynamic Gesture Recognizer
            if (dynamicRecognizer == null)
            {
                Debug.LogError("❌ Dynamic Gesture Recognizer NO está asignado en LearningController!");
                allOk = false;
            }
            else
            {
                Debug.Log("✅ Dynamic Gesture Recognizer asignado");

                var handTrackingField = dynamicRecognizer.GetType().GetField("handTrackingEvents",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (handTrackingField != null)
                {
                    var handTracking = handTrackingField.GetValue(dynamicRecognizer);
                    if (handTracking == null)
                    {
                        Debug.LogError("❌❌❌ DYNAMIC GESTURE RECOGNIZER NO TIENE 'handTrackingEvents' ASIGNADO!");
                        Debug.LogError("SOLUCIÓN: Selecciona el GameObject con DynamicGestureRecognizer y arrastra 'XR Origin/Camera Offset/Right Hand' al campo 'Hand Tracking Events'");
                        allOk = false;
                    }
                    else
                    {
                        Debug.Log("✅ Dynamic Gesture Recognizer tiene handTrackingEvents asignado");
                    }
                }
            }

            // 6. Verifica el Recording Status Text
            var recordingStatusTextField = controller.GetType().GetField("recordingStatusText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (recordingStatusTextField != null)
            {
                var statusText = recordingStatusTextField.GetValue(controller);
                if (statusText == null)
                {
                    Debug.LogWarning("⚠️ Recording Status Text NO está asignado - no verás feedback visual en VR");
                    Debug.LogWarning("SOLUCIÓN: Crea un TextMeshProUGUI en el Canvas y asígnalo al campo 'Recording Status Text'");
                }
                else
                {
                    Debug.Log("✅ Recording Status Text asignado");
                }
            }

            Debug.Log("=== FIN DE VALIDACIÓN ===");

            if (allOk)
            {
                Debug.Log("🎉🎉🎉 TODO ESTÁ CORRECTAMENTE CONFIGURADO 🎉🎉🎉");
            }
            else
            {
                Debug.LogError("🔴🔴🔴 HAY ERRORES DE CONFIGURACIÓN - LEE LOS MENSAJES DE ARRIBA 🔴🔴🔴");
            }
        }
    }
}
