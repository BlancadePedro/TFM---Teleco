using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace ASLLearnVR.Core
{
    /// <summary>
    /// Valida el setup de XR en runtime y muestra advertencias en consola
    /// si detecta configuraciones incorrectas.
    /// </summary>
    public class XRSetupValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private bool validateOnStart = true;
        [SerializeField] private bool showWarningsInConsole = true;
        [SerializeField] private bool showInfoInConsole = false;

        private void Start()
        {
            if (validateOnStart)
            {
                ValidateXRSetup();
            }
        }

        [ContextMenu("Validate XR Setup")]
        public void ValidateXRSetup()
        {
            Log("=== XR SETUP VALIDATION ===", LogType.Info);

            ValidateDuplicateHandRenderers();
            ValidateUIRaycasters();
            ValidateInteractionManager();
            ValidateHandInteractors();

            Log("=== VALIDATION COMPLETE ===", LogType.Info);
        }

        private void ValidateDuplicateHandRenderers()
        {
            Log("[Validating Hand Renderers]", LogType.Info);

            // Buscar HandVisualizer por nombre de componente usando reflection
            var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>(true);
            var visualizers = allMonoBehaviours.Where(mb => mb.GetType().Name == "HandVisualizer").ToArray();
            int activeVisualizers = visualizers.Count(v => v.enabled && v.gameObject.activeInHierarchy);

            if (activeVisualizers > 1)
            {
                Log($"⚠️ DUPLICATE DETECTED: There are {activeVisualizers} active HandVisualizers. You should have only 1.", LogType.Warning);

                foreach (var visualizer in visualizers)
                {
                    if (visualizer.enabled && visualizer.gameObject.activeInHierarchy)
                    {
                        Log($"  - HandVisualizer en: {GetGameObjectPath(visualizer.gameObject)}", LogType.Warning);
                    }
                }

                Log("  FIX: Remove the 'XR Origin Hands (XR Rig)' GameObject or disable its HandVisualizer.", LogType.Warning);
            }
            else if (activeVisualizers == 1)
            {
                Log($"✓ Hand Visualizer OK: Only 1 active (correct)", LogType.Info);
            }
            else
            {
                Log($"⚠️ No active HandVisualizer found. Hands will not be rendered.", LogType.Warning);
            }
        }

        private void ValidateUIRaycasters()
        {
            Log("[Validating UI Raycasters]", LogType.Info);

            Canvas[] allCanvas = FindObjectsOfType<Canvas>(true);
            bool foundIssues = false;

            foreach (Canvas canvas in allCanvas)
            {
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    GraphicRaycaster standardRaycaster = canvas.GetComponent<GraphicRaycaster>();

                    // Buscar TrackedDeviceGraphicRaycaster por nombre
                    var allRaycasters = canvas.GetComponents<MonoBehaviour>();
                    var trackedRaycaster = allRaycasters.FirstOrDefault(r => r.GetType().Name == "TrackedDeviceGraphicRaycaster");

                    bool hasStandardEnabled = standardRaycaster != null && standardRaycaster.enabled;
                    bool hasTrackedEnabled = trackedRaycaster != null && trackedRaycaster.enabled;

                    if (hasStandardEnabled && !hasTrackedEnabled)
                    {
                        Log($"⚠️ Canvas '{canvas.name}' uses GraphicRaycaster (mouse only). Will NOT work in VR with hands.", LogType.Warning);
                        Log($"  FIX: Disable GraphicRaycaster and enable TrackedDeviceGraphicRaycaster", LogType.Warning);
                        foundIssues = true;
                    }
                    else if (hasTrackedEnabled)
                    {
                        if (hasStandardEnabled)
                        {
                            Log($"✓ Canvas '{canvas.name}' has TrackedDeviceGraphicRaycaster (VR), but GraphicRaycaster is also active (redundant)", LogType.Info);
                        }
                        else
                        {
                            Log($"✓ Canvas '{canvas.name}' correctly configured for VR", LogType.Info);
                        }
                    }
                    else if (!hasTrackedEnabled && trackedRaycaster == null)
                    {
                        Log($"⚠️ Canvas '{canvas.name}' does NOT have TrackedDeviceGraphicRaycaster. Add this component.", LogType.Warning);
                        foundIssues = true;
                    }
                }
            }

            if (!foundIssues)
            {
                Log("✓ UI Raycasters OK", LogType.Info);
            }
        }

        private void ValidateInteractionManager()
        {
            Log("[Validating XR Interaction Manager]", LogType.Info);

            // Buscar XRInteractionManager por nombre
            var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>(true);
            var manager = allMonoBehaviours.FirstOrDefault(mb => mb.GetType().Name == "XRInteractionManager");

            if (manager == null)
            {
                Log("⚠️ NO XR Interaction Manager in the scene. Interactors will not work.", LogType.Warning);
                Log("  FIX: Add a GameObject with XRInteractionManager component", LogType.Warning);
            }
            else
            {
                Log($"✓ XR Interaction Manager found at: {manager.gameObject.name}", LogType.Info);
            }
        }

        private void ValidateHandInteractors()
        {
            Log("[Validating Hand Interactors]", LogType.Info);

            // Buscar XRPokeInteractor por nombre
            var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>(true);
            var pokeInteractors = allMonoBehaviours.Where(mb => mb.GetType().Name == "XRPokeInteractor").ToArray();
            int activePokeInteractors = pokeInteractors.Count(p => p.enabled && p.gameObject.activeInHierarchy);

            if (activePokeInteractors == 0)
            {
                Log("⚠️ NO active Poke Interactors. You will not be able to touch UI buttons with your hands.", LogType.Warning);
                Log("  FIX: Add LeftHandInteraction and RightHandInteraction prefabs to the scene", LogType.Warning);
            }
            else if (activePokeInteractors < 2)
            {
                Log($"⚠️ Only {activePokeInteractors} active Poke Interactor. You should have 2 (one per hand).", LogType.Warning);
            }
            else
            {
                Log($"✓ Poke Interactors OK: {activePokeInteractors} found", LogType.Info);

                // Verificar handedness usando reflection
                int leftCount = 0;
                int rightCount = 0;

                foreach (var poke in pokeInteractors)
                {
                    if (!poke.enabled || !poke.gameObject.activeInHierarchy) continue;

                    var handednessField = poke.GetType().GetField("m_Handedness",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (handednessField != null)
                    {
                        var handednessValue = (int)handednessField.GetValue(poke);
                        if (handednessValue == 0) leftCount++;
                        else if (handednessValue == 1) rightCount++;
                    }
                }

                if (leftCount == 0 || rightCount == 0)
                {
                    Log($"  ⚠️ Incorrect Handedness: Left={leftCount}, Right={rightCount}. You should have 1 of each.", LogType.Warning);
                }
                else
                {
                    Log($"  ✓ Handedness OK: Left={leftCount}, Right={rightCount}", LogType.Info);
                }
            }
        }

        private void Log(string message, LogType logType)
        {
            if (logType == LogType.Warning && showWarningsInConsole)
            {
                Debug.LogWarning($"[XRSetupValidator] {message}", this);
            }
            else if (logType == LogType.Info && showInfoInConsole)
            {
                Debug.Log($"[XRSetupValidator] {message}", this);
            }
            else if (logType == LogType.Error)
            {
                Debug.LogError($"[XRSetupValidator] {message}", this);
            }
        }

        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private enum LogType
        {
            Info,
            Warning,
            Error
        }
    }
}
