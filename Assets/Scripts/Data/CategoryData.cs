using System.Collections.Generic;
using UnityEngine;

namespace ASL_LearnVR.Data
{
    /// <summary>
    /// Representa una categoría de signos (ej: Alfabeto, Dígitos, Colores).
    /// Contiene una lista de SignData.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCategory", menuName = "ASL Learn VR/Category Data", order = 2)]
    public class CategoryData : ScriptableObject
    {
        [Header("Category Information")]
        [Tooltip("Nombre de la categoría (ej: 'Alphabet', 'Digits', 'Colors')")]
        public string categoryName;

        [Tooltip("Descripción de la categoría")]
        [TextArea(2, 4)]
        public string description;

        [Header("Signs")]
        [Tooltip("Lista de signos que pertenecen a esta categoría")]
        public List<SignData> signs = new List<SignData>();

        [Header("Visual")]
        [Tooltip("Icono representativo de la categoría")]
        public Sprite icon;

        [Tooltip("Color de tema para esta categoría (UI)")]
        public Color themeColor = Color.white;

        /// <summary>
        /// Obtiene un signo por su nombre.
        /// </summary>
        public SignData GetSignByName(string signName)
        {
            return signs.Find(s => s.signName.Equals(signName, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Valida que la categoría esté correctamente configurada.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                Debug.LogError($"CategoryData '{name}' tiene un categoryName vacío.");
                return false;
            }

            if (signs == null || signs.Count == 0)
            {
                Debug.LogError($"CategoryData '{categoryName}' no tiene signos asignados.");
                return false;
            }

            bool allValid = true;
            foreach (var sign in signs)
            {
                if (sign == null)
                {
                    Debug.LogError($"CategoryData '{categoryName}' tiene un SignData null en la lista.");
                    allValid = false;
                }
                else if (!sign.IsValid())
                {
                    allValid = false;
                }
            }

            return allValid;
        }

        /// <summary>
        /// Obtiene el número total de signos en esta categoría.
        /// </summary>
        public int GetSignCount()
        {
            return signs != null ? signs.Count : 0;
        }
    }
}
