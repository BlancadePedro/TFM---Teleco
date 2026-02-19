using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.SelfAssessment
{
    /// <summary>
    /// Controla una casilla individual en el grid de autoevaluacion.
    /// Muestra el nombre del signo y cambia de color cuando se completa.
    /// </summary>
    public class SignTileController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Image de fondo de la casilla")]
        [SerializeField] private Image backgroundImage;

        [Tooltip("Text que muestra el nombre del signo")]
        [SerializeField] private TextMeshProUGUI signNameText;

        [Tooltip("Icon del signo (opcional)")]
        [SerializeField] private Image signIcon;

        [Header("Visual Settings")]
        [Tooltip("Color por defecto de la casilla")]
        [SerializeField] private Color defaultColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        [Tooltip("Color when sign is completed")]
        [SerializeField] private Color completedColor = new Color(0f, 0.627451f, 1f, 1f);

        [Tooltip("Color when sign is being recognized (temporary feedback)")]
        [SerializeField] private Color recognizedColor = new Color(1f, 0.843f, 0f, 1f); // Dorado

        private SignData sign;
        private bool isCompleted = false;
        private bool isCurrentlyRecognized = false;

        /// <summary>
        /// El SignData asociado a esta casilla.
        /// </summary>
        public SignData Sign => sign;

        /// <summary>
        /// True si el signo ha sido completed.
        /// </summary>
        public bool IsCompleted => isCompleted;

        /// <summary>
        /// Inicializa la casilla con un SignData.
        /// </summary>
        public void Initialize(SignData signData)
        {
            sign = signData;

            // BUSCA el backgroundImage si no esta assigned
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
                if (backgroundImage == null)
                {
                    Debug.LogError($"SignTileController: NO found Image component en '{gameObject.name}'");
                }
            }

            // Actualiza el texto
            if (signNameText != null)
                signNameText.text = sign.signName;

            // Actualiza el icono si esta available
            if (signIcon != null && sign.icon != null)
            {
                signIcon.sprite = sign.icon;
                signIcon.enabled = true;
            }
            else if (signIcon != null)
            {
                signIcon.enabled = false;
            }

            // Aplica el color por defecto
            if (backgroundImage != null)
            {
                backgroundImage.color = defaultColor;
                Debug.Log($"[INIT] Tile '{sign.signName}' initialized con color: {backgroundImage.color}");
            }
        }

        /// <summary>
        /// Marca la casilla como completed o no completed.
        /// </summary>
        public void SetCompleted(bool completed)
        {
            isCompleted = completed;

            if (backgroundImage != null)
            {
                // Cambio directo de color sin animacion
                backgroundImage.color = completed ? completedColor : defaultColor;
            }
        }

        /// <summary>
        /// Muestra feedback visual cuando el signo es reconocido (sin marcarlo como completed).
        /// Feedback puramente cromatico, sin animaciones de escala ni pulso.
        /// </summary>
        public void ShowRecognitionFeedback()
        {
            Debug.Log($">>> ShowRecognitionFeedback() para '{sign?.signName}' | isCompleted={isCompleted} | backgroundImage={backgroundImage != null}");

            // No mostrar feedback si ya esta completed
            if (isCompleted)
            {
                Debug.Log($"    -> Tile '{sign?.signName}' YA COMPLETADO, no cambia color");
                return;
            }

            isCurrentlyRecognized = true;

            // Cambio directo de color sin animacion
            if (backgroundImage != null)
            {
                backgroundImage.color = recognizedColor;
                Debug.Log($"    -> TILE '{sign?.signName}' CAMBIADO A DORADO: {recognizedColor}");
            }
            else
            {
                Debug.LogError($"    -> ERROR: backgroundImage es NULL para '{sign?.signName}'");
            }
        }

        /// <summary>
        /// Oculta el feedback de reconocimiento y vuelve al color por defecto.
        /// </summary>
        public void HideRecognitionFeedback()
        {
            Debug.Log($"<<< HideRecognitionFeedback() para '{sign?.signName}' | isCompleted={isCompleted}");

            // No hacer nada si ya esta completed
            if (isCompleted)
            {
                Debug.Log($"    -> Tile '{sign?.signName}' YA COMPLETADO, no cambia color");
                return;
            }

            isCurrentlyRecognized = false;

            // Cambio directo de color sin animacion
            if (backgroundImage != null)
            {
                backgroundImage.color = defaultColor;
                Debug.Log($"    -> TILE '{sign?.signName}' CAMBIADO A GRIS: {defaultColor}");
            }
        }
    }
}
