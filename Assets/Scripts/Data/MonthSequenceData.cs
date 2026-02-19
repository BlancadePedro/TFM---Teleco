using UnityEngine;

namespace ASL_LearnVR.Data
{
    /// <summary>
    /// Representa una secuencia de 3 letras (ej: un mes: JAN).
    /// Hereda de SignData para permitir polimorfismo en CategoryData.signs.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMonthSequence", menuName = "ASL Learn VR/Month Sequence Data", order = 4)]
    public class MonthSequenceData : SignData
    {
        [Header("Sequence Letters")]
        [Tooltip("References a los SignData de las 3 letras que forman la secuencia (ordenadas)")]
        public SignData[] letters = new SignData[3];

        [Header("Guide Animation Clips")]
        [Tooltip("Combined guide animation for the left hand (optional)")]
        public AnimationClip guideClipLeft;

        [Tooltip("Combined guide animation for the right hand (optional)")]
        public AnimationClip guideClipRight;

        /// <summary>
        /// Valida que la secuencia tenga exactamente 3 letras y cada letra sea valid.
        /// </summary>
        public override bool IsValid()
        {
            if (letters == null || letters.Length != 3)
            {
                Debug.LogError($"MonthSequenceData '{name}' debe tener exactamente 3 letras assigned.");
                return false;
            }

            bool ok = true;
            for (int i = 0; i < letters.Length; i++)
            {
                var l = letters[i];
                if (l == null)
                {
                    Debug.LogError($"MonthSequenceData '{name}': letra en posicion {i} es null.");
                    ok = false;
                }
            }

            // Ademas podemos requerir que el nombre este puesto (ej: 'ENERO')
            if (string.IsNullOrEmpty(signName))
            {
                Debug.LogWarning($"MonthSequenceData '{name}' no tiene 'signName' definido. Usa el nombre del asset como fallback.");
            }

            return ok;
        }
    }
}
