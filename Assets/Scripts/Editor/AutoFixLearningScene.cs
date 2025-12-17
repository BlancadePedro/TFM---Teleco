using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ASL_LearnVR.LearningModule;
using ASL_LearnVR.Gestures;
using UnityEngine.XR.Hands;
using TMPro;
using UnityEngine.UI;

namespace ASL_LearnVR.Editor
{
    /// <summary>
    /// Arregla AUTOMÁTICAMENTE la escena Learning para Quest 3.
    /// Menú: Tools > ASL Learn VR > AUTO FIX Learning Scene for Quest 3
    /// </summary>
    public class AutoFixLearningScene : MonoBehaviour
    {
        [MenuItem("Tools/ASL Learn VR/AUTO FIX Learning Scene for Quest 3")]
        public static void AutoFix()
        {
            Debug.Log("=== AUTO FIX LEARNING SCENE PARA QUEST 3 ===");

            // Abre la escena Learning si no está abierta
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.name != "03_LearningModule")
            {
                Scene learningScene = EditorSceneManager.OpenScene("Assets/03_LearningModule.unity", OpenSceneMode.Single);
                if (learningScene == null || !learningScene.IsValid())
                {
                    Debug.LogError("No se pudo abrir la escena 03_LearningModule.unity");
                    return;
                }
            }

            // 1. Encuentra el LearningController
            LearningController controller = FindObjectOfType<LearningController>();
            if (controller == null)
            {
                Debug.LogError("NO SE ENCONTRÓ LearningController en la escena!");
                return;
            }
            Debug.Log("Encontrado LearningController");

            // 2. Encuentra los GameObjects de las manos
            GameObject rightHand = GameObject.Find("Right Hand");
            GameObject leftHand = GameObject.Find("Left Hand");

            if (rightHand == null)
            {
                Debug.LogError("NO SE ENCONTRÓ 'Right Hand' en la escena! Busca en XR Origin > Camera Offset > Right Hand");
                return;
            }
            Debug.Log("Encontrado Right Hand");

            XRHandTrackingEvents rightHandTracking = rightHand.GetComponent<XRHandTrackingEvents>();
            if (rightHandTracking == null)
            {
                Debug.LogError("'Right Hand' NO TIENE componente XRHandTrackingEvents!");
                return;
            }

            XRHandTrackingEvents leftHandTracking = null;
            if (leftHand != null)
            {
                leftHandTracking = leftHand.GetComponent<XRHandTrackingEvents>();
            }

            // 3. Obtiene los recognizers usando reflection
            var rightHandRecognizerField = controller.GetType().GetField("rightHandRecognizer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var leftHandRecognizerField = controller.GetType().GetField("leftHandRecognizer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dynamicRecognizerField = controller.GetType().GetField("dynamicGestureRecognizer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            GestureRecognizer rightHandRecognizer = rightHandRecognizerField.GetValue(controller) as GestureRecognizer;
            GestureRecognizer leftHandRecognizer = leftHandRecognizerField.GetValue(controller) as GestureRecognizer;
            DynamicGestureRecognizer dynamicRecognizer = dynamicRecognizerField.GetValue(controller) as DynamicGestureRecognizer;

            // 4. Si los recognizers no existen, créalos
            if (rightHandRecognizer == null)
            {
                GameObject rightRecognizerObj = new GameObject("RightHandRecognizer");
                rightRecognizerObj.transform.SetParent(controller.transform);
                rightHandRecognizer = rightRecognizerObj.AddComponent<GestureRecognizer>();
                rightHandRecognizerField.SetValue(controller, rightHandRecognizer);
                Debug.Log("CREADO RightHandRecognizer");
            }

            if (leftHandRecognizer == null && leftHand != null)
            {
                GameObject leftRecognizerObj = new GameObject("LeftHandRecognizer");
                leftRecognizerObj.transform.SetParent(controller.transform);
                leftHandRecognizer = leftRecognizerObj.AddComponent<GestureRecognizer>();
                leftHandRecognizerField.SetValue(controller, leftHandRecognizer);
                Debug.Log("CREADO LeftHandRecognizer");
            }

            if (dynamicRecognizer == null)
            {
                GameObject dynamicRecognizerObj = new GameObject("DynamicGestureRecognizer");
                dynamicRecognizerObj.transform.SetParent(controller.transform);
                dynamicRecognizer = dynamicRecognizerObj.AddComponent<DynamicGestureRecognizer>();
                dynamicRecognizerField.SetValue(controller, dynamicRecognizer);
                Debug.Log("CREADO DynamicGestureRecognizer");
            }

            // 5. Asigna handTrackingEvents a los recognizers
            var rightHandTrackingField = rightHandRecognizer.GetType().GetField("handTrackingEvents",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rightHandTrackingField.SetValue(rightHandRecognizer, rightHandTracking);
            Debug.Log("ASIGNADO handTrackingEvents a RightHandRecognizer");

            if (leftHandRecognizer != null && leftHandTracking != null)
            {
                var leftHandTrackingField = leftHandRecognizer.GetType().GetField("handTrackingEvents",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                leftHandTrackingField.SetValue(leftHandRecognizer, leftHandTracking);
                Debug.Log("ASIGNADO handTrackingEvents a LeftHandRecognizer");
            }

            var dynamicHandTrackingField = dynamicRecognizer.GetType().GetField("handTrackingEvents",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            dynamicHandTrackingField.SetValue(dynamicRecognizer, rightHandTracking);
            Debug.Log("ASIGNADO handTrackingEvents a DynamicGestureRecognizer");

            // 6. Verifica o crea el RecordingStatusText
            var recordingStatusTextField = controller.GetType().GetField("recordingStatusText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            TextMeshProUGUI recordingStatusText = recordingStatusTextField.GetValue(controller) as TextMeshProUGUI;

            if (recordingStatusText == null)
            {
                // Busca el Canvas
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    // Crea el RecordingStatusText
                    GameObject textObj = new GameObject("RecordingStatusText");
                    textObj.transform.SetParent(canvas.transform, false);

                    RectTransform rectTransform = textObj.AddComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0.5f, 1f);
                    rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    rectTransform.pivot = new Vector2(0.5f, 1f);
                    rectTransform.anchoredPosition = new Vector2(0, -100);
                    rectTransform.sizeDelta = new Vector2(800, 100);

                    recordingStatusText = textObj.AddComponent<TextMeshProUGUI>();
                    recordingStatusText.text = "READY";
                    recordingStatusText.fontSize = 48;
                    recordingStatusText.color = Color.white;
                    recordingStatusText.alignment = TextAlignmentOptions.Center;

                    recordingStatusTextField.SetValue(controller, recordingStatusText);
                    Debug.Log("CREADO RecordingStatusText");
                }
                else
                {
                    Debug.LogWarning("No se encontró Canvas para crear RecordingStatusText");
                }
            }

            // 7. Marca la escena como modificada y guarda
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("=== AUTO FIX COMPLETADO ===");
            Debug.Log("AHORA PRUEBA EN QUEST 3:");
            Debug.Log("1. Presiona Play");
            Debug.Log("2. Ve a la escena Learning");
            Debug.Log("3. Presiona Practice");
            Debug.Log("4. Haz un signo con tu mano");
        }
    }
}
