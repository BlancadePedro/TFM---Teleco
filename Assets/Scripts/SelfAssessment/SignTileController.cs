using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.SelfAssessment
{
    /// <summary>
    /// Controla una casilla individual en el grid de autoevaluación.
    /// Muestra el nombre del signo y cambia de color cuando se completa.
    /// </summary>
    public class SignTileController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Imagen de fondo de la casilla")]
        [SerializeField] private Image backgroundImage;

        [Tooltip("Texto que muestra el nombre del signo")]
        [SerializeField] private TextMeshProUGUI signNameText;

        [Tooltip("Icono del signo (opcional)")]
        [SerializeField] private Image signIcon;

        [Header("Visual Settings")]
        [Tooltip("Color por defecto de la casilla")]
        [SerializeField] private Color defaultColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        [Tooltip("Color cuando el signo está completado")]
        [SerializeField] private Color completedColor = new Color(0f, 0.627451f, 1f, 1f);

        [Tooltip("Duración de la animación de cambio de color")]
        [SerializeField] private float colorTransitionDuration = 0.3f;

        private SignData sign;
        private bool isCompleted = false;

        /// <summary>
        /// El SignData asociado a esta casilla.
        /// </summary>
        public SignData Sign => sign;

        /// <summary>
        /// True si el signo ha sido completado.
        /// </summary>
        public bool IsCompleted => isCompleted;

        /// <summary>
        /// Inicializa la casilla con un SignData.
        /// </summary>
        public void Initialize(SignData signData)
        {
            sign = signData;

            // Actualiza el texto
            if (signNameText != null)
                signNameText.text = sign.signName;

            // Actualiza el icono si está disponible
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
                backgroundImage.color = defaultColor;
        }

        /// <summary>
        /// Marca la casilla como completada o no completada.
        /// </summary>
        public void SetCompleted(bool completed)
        {
            isCompleted = completed;

            if (backgroundImage != null)
            {
                // Animación de cambio de color
                StopAllCoroutines();
                StartCoroutine(AnimateColorChange(completed ? completedColor : defaultColor));
            }
        }

        /// <summary>
        /// Corrutina que anima el cambio de color.
        /// </summary>
        private System.Collections.IEnumerator AnimateColorChange(Color targetColor)
        {
            Color startColor = backgroundImage.color;
            float elapsed = 0f;

            while (elapsed < colorTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / colorTransitionDuration;
                backgroundImage.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            backgroundImage.color = targetColor;
        }
    }
}
