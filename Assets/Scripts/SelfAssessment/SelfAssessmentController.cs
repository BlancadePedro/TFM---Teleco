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
        [Tooltip("Componente GestureRecognizer para la mano derecha")]
        [SerializeField] private GestureRecognizer rightHandRecognizer;

        [Tooltip("Componente GestureRecognizer para la mano izquierda")]
        [SerializeField] private GestureRecognizer leftHandRecognizer;

        [Header("Progress")]
        [Tooltip("Texto que muestra el progreso")]
        [SerializeField] private TextMeshProUGUI progressText;

        private CategoryData currentCategory;
        private Dictionary<SignData, SignTileController> signTiles = new Dictionary<SignData, SignTileController>();
        private HashSet<SignData> completedSigns = new HashSet<SignData>();

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
        /// Inicia el reconocimiento de gestos para todos los signos.
        /// </summary>
        private void StartRecognition()
        {
            // Nota: Para detectar múltiples gestos simultáneamente,
            // necesitarías múltiples GestureRecognizers o un sistema más avanzado.
            // Aquí usamos un enfoque simple: detectamos un gesto a la vez.

            if (rightHandRecognizer != null)
            {
                rightHandRecognizer.onGestureDetected.AddListener(OnGestureDetected);
                rightHandRecognizer.SetDetectionEnabled(true);
            }

            if (leftHandRecognizer != null)
            {
                leftHandRecognizer.onGestureDetected.AddListener(OnGestureDetected);
                leftHandRecognizer.SetDetectionEnabled(true);
            }
        }

        /// <summary>
        /// Callback cuando un gesto es detectado.
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

                // Ilumina la casilla correspondiente
                if (signTiles.ContainsKey(sign))
                {
                    signTiles[sign].SetCompleted(true);
                }

                // Actualiza el progreso
                UpdateProgress();
            }
        }

        /// <summary>
        /// Actualiza el texto de progreso.
        /// </summary>
        private void UpdateProgress()
        {
            if (progressText != null)
            {
                int total = currentCategory.GetSignCount();
                int completed = completedSigns.Count;
                progressText.text = $"Progress: {completed}/{total}";
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

            if (rightHandRecognizer != null)
            {
                rightHandRecognizer.onGestureDetected.RemoveListener(OnGestureDetected);
                rightHandRecognizer.SetDetectionEnabled(false);
            }

            if (leftHandRecognizer != null)
            {
                leftHandRecognizer.onGestureDetected.RemoveListener(OnGestureDetected);
                leftHandRecognizer.SetDetectionEnabled(false);
            }
        }
    }
}
