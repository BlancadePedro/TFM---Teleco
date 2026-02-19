using System.Collections.Generic;
using UnityEngine;

namespace ASL_LearnVR.Data
{
    /// <summary>
    /// Represents a category of signs (e.g.: Alphabet, Digits, Colors).
    /// Contains a list of SignData.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCategory", menuName = "ASL Learn VR/Category Data", order = 2)]
    public class CategoryData : ScriptableObject
    {
        [Header("Category Information")]
        [Tooltip("Category name (e.g.: 'Alphabet', 'Digits', 'Colors')")]
        public string categoryName;

        [Tooltip("Category description")]
        [TextArea(2, 4)]
        public string description;

        [Header("Signs")]
        [Tooltip("List of signs belonging to this category")]
        public List<SignData> signs = new List<SignData>();

        [Header("Visual")]
        [Tooltip("Representative category icon")]
        public Sprite icon;

        [Tooltip("Theme color for this category (UI)")]
        public Color themeColor = Color.white;

        /// <summary>
        /// Gets a sign by its name.
        /// </summary>
        public SignData GetSignByName(string signName)
        {
            return signs.Find(s => s.signName.Equals(signName, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates that the category is correctly configured.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                Debug.LogError($"CategoryData '{name}' tiene un categoryName vacio.");
                return false;
            }

            if (signs == null || signs.Count == 0)
            {
                Debug.LogError($"CategoryData '{categoryName}' no tiene signos assigneds.");
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
        /// Gets the total number of signs in this category.
        /// </summary>
        public int GetSignCount()
        {
            return signs != null ? signs.Count : 0;
        }
    }
}
