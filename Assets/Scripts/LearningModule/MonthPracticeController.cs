using UnityEngine;
using ASL_LearnVR.Data;
using ASL_LearnVR.Gestures;
using System;

namespace ASL_LearnVR.LearningModule
{
    /// <summary>
    /// Controla la practica de secuencias de 3 letras (meses).
    /// Gestiona el estado, configura los recognizers, y actualiza la UI.
    /// </summary>
    public class MonthPracticeController : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Component MonthTilesUI que muestra los 3 tiles")]
        [SerializeField] private MonthTilesUI tilesUI;

        // References a los recognizers (se configuran desde LearningController)
        private GestureRecognizer rightRecognizer;
        private GestureRecognizer leftRecognizer;

        // State actual
        private MonthSequenceData currentMonth;
        private int currentStep = 0;
        private bool isPracticing = false;

        // Event cuando se completa la secuencia
        public event Action<MonthSequenceData> OnSequenceCompleted;

        /// <summary>
        /// Propiedad publica para saber si esta practicando.
        /// </summary>
        public bool IsPracticing => isPracticing;

        /// <summary>
        /// Propiedad publica para saber el paso actual (0, 1, 2).
        /// </summary>
        public int CurrentStep => currentStep;

        /// <summary>
        /// Configura los recognizers a usar.
        /// </summary>
        public void SetRecognizers(GestureRecognizer right, GestureRecognizer left)
        {
            rightRecognizer = right;
            leftRecognizer = left;
            Debug.Log($"[MonthPracticeController] Recognizers configureds: R={right != null}, L={left != null}");
        }

        /// <summary>
        /// Starts practice for a specific month.
        /// </summary>
        public void StartPractice(MonthSequenceData month)
        {
            if (month == null)
            {
                Debug.LogError("[MonthPracticeController] No se puede iniciar practica: month es null");
                return;
            }

            if (month.letters == null || month.letters.Length < 3)
            {
                Debug.LogError($"[MonthPracticeController] El mes '{month.signName}' no tiene 3 letras configureds");
                return;
            }

            currentMonth = month;
            currentStep = 0;
            isPracticing = true;

            Debug.Log($"[MonthPracticeController] ========================================");
            Debug.Log($"[MonthPracticeController] INICIANDO PRACTICA: {month.signName}");
            Debug.Log($"[MonthPracticeController] Letras: {month.letters[0]?.signName} → {month.letters[1]?.signName} → {month.letters[2]?.signName}");
            Debug.Log($"[MonthPracticeController] ========================================");

            // Mostrar UI
            if (tilesUI != null)
            {
                tilesUI.Show(month);
            }
            else
            {
                Debug.LogError("[MonthPracticeController] tilesUI es NULL - no se mostrara la UI de tiles");
            }

            // Configure recognizer para la primera letra
            ConfigureRecognizerForCurrentStep();
        }

        /// <summary>
        /// Stops current practice.
        /// </summary>
        public void StopPractice()
        {
            Debug.Log("[MonthPracticeController] DETENIENDO PRACTICA");

            isPracticing = false;
            currentMonth = null;
            currentStep = 0;

            // Ocultar UI
            if (tilesUI != null)
                tilesUI.Hide();
        }

        /// <summary>
        /// Llamar cuando el usuario navega a otro signo.
        /// </summary>
        public void OnSignChanged()
        {
            // Always stop/clear any month practice UI when the selected sign changes.
            // This ensures tiles from the previous month are hidden or reset when
            // the user navigates with Next/Previous.
            StopPractice();
        }

        /// <summary>
        /// Procesa una deteccion de gesto.
        /// Retorna true si la letra detectada era la esperada.
        /// </summary>
        public bool ProcessDetection(SignData detectedSign)
        {
            if (!isPracticing || currentMonth == null || detectedSign == null)
                return false;

            if (currentStep >= 3)
                return false;

            SignData expectedLetter = currentMonth.letters[currentStep];
            if (expectedLetter == null)
                return false;

            Debug.Log($"[MonthPracticeController] Detectado: '{detectedSign.signName}' | Esperado: '{expectedLetter.signName}'");

            // Comparar por nombre
            if (detectedSign.signName == expectedLetter.signName)
            {
                Debug.Log($"[MonthPracticeController] ✓ CORRECT! Letter '{expectedLetter.signName}' completed");

                // Marcar como completed en UI
                if (tilesUI != null)
                    tilesUI.MarkComplete(currentStep);

                currentStep++;

                if (currentStep >= 3)
                {
                    // Sequence completed!
                    Debug.Log($"[MonthPracticeController] ★★★ SECUENCIA COMPLETADA: {currentMonth.signName} ★★★");

                    if (tilesUI != null)
                        tilesUI.ShowAllComplete();

                    isPracticing = false;
                    OnSequenceCompleted?.Invoke(currentMonth);
                }
                else
                {
                    // Avanzar al siguiente paso
                    if (tilesUI != null)
                        tilesUI.SetCurrentStep(currentStep);

                    ConfigureRecognizerForCurrentStep();
                }

                return true;
            }

            // Letra incorrecta - no hacer nada, el usuario sigue intentando
            return false;
        }

        /// <summary>
        /// Reinicia la practica actual al paso 0.
        /// </summary>
        public void ResetPractice()
        {
            if (currentMonth == null)
                return;

            currentStep = 0;
            isPracticing = true;

            if (tilesUI != null)
                tilesUI.Reset();

            ConfigureRecognizerForCurrentStep();

            Debug.Log("[MonthPracticeController] Practice restarted");
        }

        /// <summary>
        /// Configura el recognizer para detectar la letra del paso actual.
        /// </summary>
        private void ConfigureRecognizerForCurrentStep()
        {
            if (currentMonth == null || currentMonth.letters == null)
                return;

            if (currentStep >= currentMonth.letters.Length)
                return;

            SignData targetLetter = currentMonth.letters[currentStep];
            if (targetLetter == null)
            {
                Debug.LogError($"[MonthPracticeController] Letra en paso {currentStep} es null");
                return;
            }

            Debug.Log($"[MonthPracticeController] Configuring recognizer para letra: '{targetLetter.signName}'");

            if (rightRecognizer != null)
                rightRecognizer.TargetSign = targetLetter;

            if (leftRecognizer != null)
                leftRecognizer.TargetSign = targetLetter;
        }
    }
}
