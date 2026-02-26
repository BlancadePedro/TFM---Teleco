using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Displays feedback indicators (rings/halos) on finger joints.
    /// Does not depend on modifying XR Hands visualizer materials.
    /// Uses prefabs anchored to each fingertip.
    /// </summary>
    public class FingerIndicatorVisualizer : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Prefab for error indicator (red)")]
        [SerializeField] private GameObject errorIndicatorPrefab;

        [Tooltip("Prefab for warning indicator (orange)")]
        [SerializeField] private GameObject warningIndicatorPrefab;

        [Tooltip("Prefab for correct indicator (green)")]
        [SerializeField] private GameObject correctIndicatorPrefab;

        [Tooltip("Prefab for hand-wide correct indicator")]
        [SerializeField] private GameObject handCorrectIndicatorPrefab;

        [Header("Settings")]
        [Tooltip("Indicator offset from fingertip (forward direction)")]
        [SerializeField] private float indicatorOffset = 0.015f;

        [Tooltip("Indicator scale")]
        [SerializeField] private float indicatorScale = 0.025f;

        [Tooltip("Handedness of the hand to visualize")]
        [SerializeField] private Handedness handedness = Handedness.Right;

        [Tooltip("XRHandTrackingEvents component (optional fallback)")]
        [SerializeField] private XRHandTrackingEvents handTrackingEvents;

        [Header("Visibility")]
        [Tooltip("Show visual indicators")]
        [SerializeField] private bool showIndicators = true;

        [Tooltip("Show global indicator when all fingers are correct")]
        [SerializeField] private bool showHandCorrectIndicator = true;

        // Indicadores instanciados por dedo [5 dedos]
        private GameObject[] fingerIndicators = new GameObject[5];
        private Severity[] currentSeverities = new Severity[5];

        // Indicador global de mano
        private GameObject handIndicator;

        // Cache del subsystem
        private XRHandSubsystem handSubsystem;

        // Joints de fingertips
        private static readonly XRHandJointID[] fingerTipJoints = new XRHandJointID[]
        {
            XRHandJointID.ThumbTip,
            XRHandJointID.IndexTip,
            XRHandJointID.MiddleTip,
            XRHandJointID.RingTip,
            XRHandJointID.LittleTip
        };

        void Start()
        {
            // Crear indicadores iniciales (inactives)
            InitializeIndicators();
        }

        void Update()
        {
            if (!showIndicators)
            {
                HideAll();
                return;
            }

            // Actualizar posiciones de indicadores actives
            UpdateIndicatorPositions();
        }

        void OnDestroy()
        {
            // Limpiar indicadores
            for (int i = 0; i < fingerIndicators.Length; i++)
            {
                if (fingerIndicators[i] != null)
                    Destroy(fingerIndicators[i]);
            }

            if (handIndicator != null)
                Destroy(handIndicator);
        }

        /// <summary>
        /// Inicializa los indicadores como objetos inactives.
        /// </summary>
        private void InitializeIndicators()
        {
            for (int i = 0; i < 5; i++)
            {
                currentSeverities[i] = Severity.None;

                // Crear un contenedor vacio que se llenara cuando sea necesario
                fingerIndicators[i] = new GameObject($"FingerIndicator_{(Finger)i}");
                fingerIndicators[i].transform.SetParent(transform);
                fingerIndicators[i].SetActive(false);
            }

            // Indicador global de mano
            if (handCorrectIndicatorPrefab != null)
            {
                handIndicator = Instantiate(handCorrectIndicatorPrefab, transform);
                handIndicator.transform.localScale = Vector3.one * indicatorScale * 2f;
                handIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Actualiza los indicadores basandose en los errores por dedo.
        /// </summary>
        public void UpdateFromResult(StaticGestureResult result)
        {
            if (!showIndicators || result == null)
            {
                HideAll();
                return;
            }

            // Si todo esta correct (el recognizer confirmo), mostrar solo indicador global
            if (result.isMatchGlobal)
            {
                ShowHandCorrect(true);
                for (int i = 0; i < 5; i++)
                {
                    SetFingerIndicator((Finger)i, Severity.None);
                }
                return;
            }
            else
            {
                ShowHandCorrect(false);
            }

            // Crear un array para rastrear que dedos tienen errores
            Severity[] fingerSeverities = new Severity[5];
            for (int i = 0; i < 5; i++)
                fingerSeverities[i] = Severity.None;

            // Procesar errores y asignar severidades
            if (result.perFingerErrors != null)
            {
                foreach (var error in result.perFingerErrors)
                {
                    if (error.severity != Severity.None)
                    {
                        int fingerIndex = (int)error.finger;
                        // Mantener la severidad mas alta si hay multiples errores para el mismo dedo
                        if ((int)error.severity > (int)fingerSeverities[fingerIndex])
                        {
                            fingerSeverities[fingerIndex] = error.severity;
                        }
                    }
                }
            }

            // Actualizar indicadores para TODOS los dedos (mostrar/ocultar segun severidad)
            for (int i = 0; i < 5; i++)
            {
                SetFingerIndicator((Finger)i, fingerSeverities[i]);

                if (fingerSeverities[i] != Severity.None)
                {
                    Debug.Log($"[FingerIndicatorVisualizer] {(Finger)i}: Severity={fingerSeverities[i]}");
                }
            }
        }

        /// <summary>
        /// Establece el indicador para un dedo especifico.
        /// </summary>
        public void SetFingerIndicator(Finger finger, Severity severity)
        {
            int index = (int)finger;

            if (index < 0 || index >= fingerIndicators.Length)
                return;

            bool needsPrefabUpdate = fingerIndicators[index] == null ||
                                     currentSeverities[index] != severity ||
                                     fingerIndicators[index].transform.childCount == 0;

            currentSeverities[index] = severity;

            if (needsPrefabUpdate)
            {
                UpdateFingerIndicatorPrefab(index, severity);
            }

            // Enable/desactivar segun severidad
            if (fingerIndicators[index] != null)
            {
                bool shouldShow = showIndicators;
                if (fingerIndicators[index].activeSelf != shouldShow)
                {
                    fingerIndicators[index].SetActive(shouldShow);
                }
            }
        }

        /// <summary>
        /// Actualiza el prefab del indicador segun severidad.
        /// </summary>
        private void UpdateFingerIndicatorPrefab(int fingerIndex, Severity severity)
        {
            // Destruir indicador anterior si existe contenido
            if (fingerIndicators[fingerIndex] != null)
            {
                foreach (Transform child in fingerIndicators[fingerIndex].transform)
                {
                    Destroy(child.gameObject);
                }
            }
            else
            {
                fingerIndicators[fingerIndex] = new GameObject($"FingerIndicator_{(Finger)fingerIndex}");
                fingerIndicators[fingerIndex].transform.SetParent(transform);
            }

            // Seleccionar prefab segun severidad
            GameObject prefab = severity switch
            {
                Severity.Major => errorIndicatorPrefab,
                Severity.Minor => errorIndicatorPrefab, // No orange state: minor errors also use red
                Severity.None => correctIndicatorPrefab,
                _ => null
            };

            if (prefab != null)
            {
                var instance = Instantiate(prefab, fingerIndicators[fingerIndex].transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localScale = Vector3.one * indicatorScale;
            }
        }

        /// <summary>
        /// Actualiza las posiciones de los indicadores segun los joints de la mano.
        /// </summary>
        private void UpdateIndicatorPositions()
        {
            var hand = GetCurrentHand();
            if (!hand.isTracked)
            {
                HideAll();
                return;
            }

            // Actualizar posicion de cada indicador de dedo
            for (int i = 0; i < 5; i++)
            {
                if (fingerIndicators[i] == null || !fingerIndicators[i].activeSelf)
                    continue;

                var joint = hand.GetJoint(fingerTipJoints[i]);
                if (joint.TryGetPose(out Pose pose))
                {
                    // Posicionar el indicador en el fingertip con offset
                    Vector3 position = pose.position + pose.rotation * Vector3.forward * indicatorOffset;
                    fingerIndicators[i].transform.position = position;
                    fingerIndicators[i].transform.rotation = pose.rotation;
                }
            }

            // Update indicator position global (en la palma)
            if (handIndicator != null && handIndicator.activeSelf)
            {
                var palmJoint = hand.GetJoint(XRHandJointID.Palm);
                if (palmJoint.TryGetPose(out Pose palmPose))
                {
                    // Posicionar encima de la palma
                    Vector3 position = palmPose.position + palmPose.rotation * Vector3.up * 0.05f;
                    handIndicator.transform.position = position;
                    handIndicator.transform.rotation = palmPose.rotation;
                }
            }
        }

        /// <summary>
        /// Obtiene la mano actual del subsystem.
        /// </summary>
        private XRHand GetCurrentHand()
        {
            if (handSubsystem == null)
            {
                handSubsystem = XRGeneralSettings.Instance?
                    .Manager?
                    .activeLoader?
                    .GetLoadedSubsystem<XRHandSubsystem>();
            }

            if (handSubsystem == null)
                return default;

            return handedness == Handedness.Right ? handSubsystem.rightHand : handSubsystem.leftHand;
        }

        /// <summary>
        /// Oculta todos los indicadores.
        /// </summary>
        public void HideAll()
        {
            HideAllFingers();
            ShowHandCorrect(false);
        }

        /// <summary>
        /// Oculta todos los indicadores de dedos.
        /// </summary>
        public void HideAllFingerIndicators()
        {
            for (int i = 0; i < fingerIndicators.Length; i++)
            {
                if (fingerIndicators[i] != null)
                {
                    fingerIndicators[i].SetActive(false);
                    currentSeverities[i] = Severity.None;
                }
            }
        }

        /// <summary>
        /// Oculta todos los indicadores de dedos (alias privado para compatibilidad interna).
        /// </summary>
        private void HideAllFingers()
        {
            HideAllFingerIndicators();
        }

        /// <summary>
        /// Muestra/oculta el indicador global de mano correcta.
        /// </summary>
        public void ShowHandCorrect(bool show)
        {
            if (handIndicator != null && showHandCorrectIndicator)
            {
                handIndicator.SetActive(show);
            }
        }

        /// <summary>
        /// Activa/desactiva la visualizacion de indicadores.
        /// </summary>
        public void SetVisible(bool visible)
        {
            showIndicators = visible;
            if (!visible)
            {
                HideAll();
            }
        }

        /// <summary>
        /// True si los indicadores estan visibles.
        /// </summary>
        public bool IsVisible => showIndicators;

        /// <summary>
        /// Establece la escala de los indicadores.
        /// </summary>
        public void SetIndicatorScale(float scale)
        {
            indicatorScale = scale;

            // Actualizar escala de indicadores existentes
            for (int i = 0; i < fingerIndicators.Length; i++)
            {
                if (fingerIndicators[i] != null)
                {
                    foreach (Transform child in fingerIndicators[i].transform)
                    {
                        child.localScale = Vector3.one * scale;
                    }
                }
            }

            if (handIndicator != null)
            {
                handIndicator.transform.localScale = Vector3.one * scale * 2f;
            }
        }
    }
}
