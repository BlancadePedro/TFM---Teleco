using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL_LearnVR.Core;
using ASL_LearnVR.Data;
using ASL_LearnVR.Gestures;

namespace ASL_LearnVR.SelfAssessment
{
    /// <summary>
    /// Controla el modo autoevaluación donde el usuario practica todos los signos de una categoría.
    /// Muestra un grid de casillas que se iluminan cuando el gesto es detectado correctamente.
    /// </summary>
    public class SelfAssessmentController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Texto que muestra el título de la categoría")]
        [SerializeField] private TextMeshProUGUI categoryTitleText;

        [Tooltip("Contenedor del grid de casillas")]
        [SerializeField] private Transform gridContainer;

        [Tooltip("Prefab de la casilla (SignTile)")]
        [SerializeField] private GameObject signTilePrefab;

        [Tooltip("Botón para volver al módulo de aprendizaje")]
        [SerializeField] private Button backButton;

        [Header("Components")]
        [Tooltip("Componente MultiGestureRecognizer que detecta todos los signos")]
        [SerializeField] private MultiGestureRecognizer multiGestureRecognizer;

        [Header("Progress")]
        [Tooltip("Texto que muestra el progreso")]
        [SerializeField] private TextMeshProUGUI progressText;
        [Tooltip("Barra de progreso (Slider o Image con fill)")]
        [SerializeField] private Slider progressBar;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        private CategoryData currentCategory;
        private Dictionary<SignData, SignTileController> signTiles = new Dictionary<SignData, SignTileController>();
        private HashSet<SignData> completedSigns = new HashSet<SignData>();
        private SignData lastActiveSign = null;

        void Start()
        {
            // Obtiene la categoría actual del GameManager
            if (GameManager.Instance != null && GameManager.Instance.CurrentCategory != null)
            {
                currentCategory = GameManager.Instance.CurrentCategory;
            }
            else
            {
                Debug.LogError("SelfAssessmentController: No hay categoría seleccionada en GameManager.");
                return;
            }

            // Actualiza el título
            if (categoryTitleText != null)
                categoryTitleText.text = $"Self-Assessment: {currentCategory.categoryName}";

            // Genera el grid de casillas
            GenerateSignTiles();

            // Configura el botón de volver
            if (backButton != null)
                backButton.onClick.AddListener(OnBackButtonClicked);

            // Activa el reconocimiento de gestos
            StartRecognition();

            // Actualiza el progreso
            UpdateProgress();
        }

        /// <summary>
        /// Genera dinámicamente las casillas del grid.
        /// </summary>
        private void GenerateSignTiles()
        {
            if (gridContainer == null || signTilePrefab == null)
            {
                Debug.LogError("SelfAssessmentController: Faltan referencias para generar casillas.");
                return;
            }

            // Configura el GridLayoutGroup para rellenar el panel completo
            SetupGridLayout();

            foreach (var sign in currentCategory.signs)
            {
                if (sign == null)
                    continue;

                GameObject tileObj = Instantiate(signTilePrefab, gridContainer);
                SignTileController tileController = tileObj.GetComponent<SignTileController>();

                if (tileController != null)
                {
                    tileController.Initialize(sign);
                    signTiles[sign] = tileController;
                }
            }
        }

        /// <summary>
        /// Configura el GridLayoutGroup para adaptarse al tamaño del panel.
        /// Rellena de izquierda a derecha, de arriba a abajo, con filas incompletas centradas.
        /// </summary>
        private void SetupGridLayout()
        {
            GridLayoutGroup gridLayout = gridContainer.GetComponent<GridLayoutGroup>();

            if (gridLayout == null)
            {
                gridLayout = gridContainer.gameObject.AddComponent<GridLayoutGroup>();
            }

            // Obtiene el RectTransform del contenedor
            RectTransform containerRect = gridContainer.GetComponent<RectTransform>();

            // Configuración del grid
            gridLayout.cellSize = new Vector2(100, 120);  // Tamaño de cada tile
            gridLayout.spacing = new Vector2(15, 15);     // Espaciado entre tiles
            gridLayout.padding = new RectOffset(20, 20, 20, 20);  // Padding del contenedor

            // CLAVE: Constraint flexible en columnas
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;

            // Alineación y orden
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;  // Rellena de izquierda a derecha

            // CAMBIO CLAVE: Centrado horizontal para que las filas incompletas queden centradas
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            Debug.Log($"SelfAssessmentController: GridLayout configurado - Panel width: {containerRect.rect.width}");
        }

        /// <summary>
        /// Inicia el reconocimiento de gestos para todos los signos.
        /// </summary>
        private void StartRecognition()
        {
            if (multiGestureRecognizer != null)
            {
                // Configura el MultiGestureRecognizer con todos los signos de la categoría
                multiGestureRecognizer.SetTargetSigns(currentCategory.signs);

                // Suscribe a los eventos de detección
                multiGestureRecognizer.onGestureDetected.AddListener(OnGestureDetected);
                multiGestureRecognizer.onGestureRecognized.AddListener(OnGestureRecognized);
                multiGestureRecognizer.onGestureLost.AddListener(OnGestureLost);

                if (showDebugLogs)
                    Debug.Log($"SelfAssessmentController: MultiGestureRecognizer configurado con {currentCategory.signs.Count} signos.");
            }
            else
            {
                Debug.LogError("SelfAssessmentController: MultiGestureRecognizer no está asignado.");
            }
        }

        /// <summary>
        /// Callback cuando un gesto es detectado y confirmado (después del hold time).
        /// </summary>
        private void OnGestureDetected(SignData sign)
        {
            // Verifica que el signo pertenezca a la categoría actual
            if (!currentCategory.signs.Contains(sign))
                return;

            // Marca el signo como completado
            if (!completedSigns.Contains(sign))
            {
                completedSigns.Add(sign);

                // Ilumina la casilla correspondiente permanentemente
                if (signTiles.ContainsKey(sign))
                {
                    signTiles[sign].SetCompleted(true);
                }

                // Actualiza el progreso
                UpdateProgress();

                if (showDebugLogs)
                    Debug.Log($"SelfAssessmentController: Signo '{sign.signName}' completado!");
            }
        }

        /// <summary>
        /// Callback cuando un gesto es reconocido (feedback instantáneo).
        /// </summary>
        private void OnGestureRecognized(SignData sign)
        {
            Debug.Log($"=== OnGestureRecognized: '{sign?.signName}' ===");

            if (!currentCategory.signs.Contains(sign))
            {
                Debug.LogWarning($"    -> Signo '{sign?.signName}' NO está en la categoría actual");
                return;
            }

            // Apaga el tile anterior si es diferente
            if (lastActiveSign != null && lastActiveSign != sign && signTiles.ContainsKey(lastActiveSign))
            {
                Debug.Log($"    -> Apagando tile anterior: '{lastActiveSign.signName}'");
                if (!completedSigns.Contains(lastActiveSign))
                {
                    signTiles[lastActiveSign].HideRecognitionFeedback();
                }
            }

            // Enciende el tile actual si no está completado
            if (signTiles.ContainsKey(sign))
            {
                Debug.Log($"    -> Encendiendo tile actual: '{sign.signName}'");
                if (!completedSigns.Contains(sign))
                {
                    signTiles[sign].ShowRecognitionFeedback();
                }
                else
                {
                    Debug.Log($"    -> Tile '{sign.signName}' ya está completado");
                }
            }
            else
            {
                Debug.LogError($"    -> NO EXISTE tile para '{sign.signName}' en el diccionario");
            }

            lastActiveSign = sign;
        }

        /// <summary>
        /// Callback cuando un gesto se pierde.
        /// </summary>
        private void OnGestureLost(SignData sign)
        {
            if (!currentCategory.signs.Contains(sign))
                return;

            // Apaga el tile si no está completado
            if (signTiles.ContainsKey(sign))
            {
                if (!completedSigns.Contains(sign))
                {
                    signTiles[sign].HideRecognitionFeedback();
                }
            }

            if (lastActiveSign == sign)
            {
                lastActiveSign = null;
            }
        }

        /// <summary>
        /// Actualiza el texto de progreso.
        /// </summary>
        private void UpdateProgress()
        {
            int total = currentCategory.GetSignCount();
            int completed = completedSigns.Count;

            if (progressText != null)
                progressText.text = $"Progreso: {completed}/{total} ({(total > 0 ? (completed * 100f / total) : 0f):0.#}%)";

            if (progressBar != null)
            {
                progressBar.maxValue = Mathf.Max(1, total);
                progressBar.value = completed;
            }
        }

        /// <summary>
        /// Callback cuando se hace clic en el botón "Volver".
        /// </summary>
        private void OnBackButtonClicked()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadLearningModule();
            }
            else
            {
                Debug.LogError("SelfAssessmentController: SceneLoader.Instance es null.");
            }
        }

        void OnDestroy()
        {
            // Limpia los listeners
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackButtonClicked);

            if (multiGestureRecognizer != null)
            {
                multiGestureRecognizer.onGestureDetected.RemoveListener(OnGestureDetected);
                multiGestureRecognizer.onGestureRecognized.RemoveListener(OnGestureRecognized);
                multiGestureRecognizer.onGestureLost.RemoveListener(OnGestureLost);
            }
        }
    }
}
