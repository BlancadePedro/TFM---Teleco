using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.LearningModule
{
    /// <summary>
    /// UI para practicar secuencias de letras (meses).
    /// Este componente debe estar en un GameObject HIJO del FeedbackPanel.
    /// Cuando se activa, muestra 3 tiles con las letras a practicar.
    /// </summary>
    public class MonthTilesUI : MonoBehaviour
    {
        [Header("Tiles")]
        [Tooltip("Los 3 fondos de los tiles (Image)")]
        [SerializeField] private Image[] tileBackgrounds = new Image[3];

        [Tooltip("Los 3 textos de las letras (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI[] tileLetters = new TextMeshProUGUI[3];

        [Header("Status")]
        [Tooltip("Texto que muestra 'Ahora toca: X' o '¡Completado!'")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Colors")]
        [SerializeField] private Color colorPending = new Color(0.4f, 0.4f, 0.4f);
        [SerializeField] private Color colorCurrent = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color colorComplete = new Color(0.2f, 0.9f, 0.3f);

        [Header("Audio (opcional)")]
        [SerializeField] private AudioSource successSound;

        private string[] letters = new string[3];
        private int currentStep = -1;
        private bool isActive = false;

        void Awake()
        {
            // Ocultar al inicio
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Muestra los tiles para una secuencia de mes.
        /// </summary>
        public void Show(MonthSequenceData monthData)
        {
            if (monthData == null || monthData.letters == null || monthData.letters.Length < 3)
            {
                Debug.LogError($"[MonthTilesUI] MonthSequenceData inválido o sin letras");
                return;
            }

            // Guardar letras
            for (int i = 0; i < 3; i++)
            {
                letters[i] = monthData.letters[i]?.signName ?? "?";
            }

            Debug.Log($"[MonthTilesUI] === MOSTRANDO TILES para {monthData.signName}: {letters[0]} → {letters[1]} → {letters[2]} ===");

            // Activar este GameObject
            gameObject.SetActive(true);
            isActive = true;

            // Configurar tiles
            for (int i = 0; i < 3; i++)
            {
                if (tileLetters[i] != null)
                    tileLetters[i].text = letters[i];

                if (tileBackgrounds[i] != null)
                    tileBackgrounds[i].color = colorPending;
            }

            // Resaltar el primer tile
            SetCurrentStep(0);
        }

        /// <summary>
        /// Oculta los tiles.
        /// </summary>
        public void Hide()
        {
            Debug.Log("[MonthTilesUI] === OCULTANDO TILES ===");
            gameObject.SetActive(false);
            isActive = false;
            currentStep = -1;
        }

        /// <summary>
        /// Establece el paso actual (resalta en amarillo).
        /// </summary>
        public void SetCurrentStep(int step)
        {
            currentStep = step;

            for (int i = 0; i < 3; i++)
            {
                if (tileBackgrounds[i] == null) continue;

                if (i < step)
                    tileBackgrounds[i].color = colorComplete;
                else if (i == step)
                    tileBackgrounds[i].color = colorCurrent;
                else
                    tileBackgrounds[i].color = colorPending;
            }

            if (statusText != null && step >= 0 && step < letters.Length)
                statusText.text = $"Haz la letra: {letters[step]}";

            Debug.Log($"[MonthTilesUI] Paso actual: {step} ({(step < letters.Length ? letters[step] : "FIN")})");
        }

        /// <summary>
        /// Marca un paso como completado (verde).
        /// </summary>
        public void MarkComplete(int step)
        {
            if (step >= 0 && step < tileBackgrounds.Length && tileBackgrounds[step] != null)
            {
                tileBackgrounds[step].color = colorComplete;
                Debug.Log($"[MonthTilesUI] Paso {step} ({letters[step]}) COMPLETADO (verde)");
            }

            if (successSound != null)
                successSound.Play();
        }

        /// <summary>
        /// Muestra estado de secuencia completada.
        /// </summary>
        public void ShowAllComplete()
        {
            currentStep = -1;

            for (int i = 0; i < tileBackgrounds.Length; i++)
            {
                if (tileBackgrounds[i] != null)
                    tileBackgrounds[i].color = colorComplete;
            }

            if (statusText != null)
                statusText.text = "¡MES COMPLETADO!";

            if (successSound != null)
                successSound.Play();

            Debug.Log("[MonthTilesUI] ★★★ SECUENCIA COMPLETADA ★★★");
        }

        /// <summary>
        /// Reinicia los tiles al estado inicial.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < tileBackgrounds.Length; i++)
            {
                if (tileBackgrounds[i] != null)
                    tileBackgrounds[i].color = colorPending;
            }

            SetCurrentStep(0);
        }

        public bool IsActive => isActive;
    }
}
