using System.Collections.Generic;
using UnityEngine;

namespace ASL_LearnVR.Data
{
    /// <summary>
    /// Represents a learning level (Basic, Intermediate, Advanced).
    /// Contains a list of categories available in that level.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevel", menuName = "ASL Learn VR/Level Data", order = 3)]
    public class LevelData : ScriptableObject
    {
        [Header("Level Information")]
        [Tooltip("Level name (e.g.: 'Basic', 'Intermediate', 'Advanced')")]
        public string levelName;

        [Tooltip("Level description")]
        [TextArea(3, 6)]
        public string description;

        [Header("Categories")]
        [Tooltip("Available categories in this level")]
        public List<CategoryData> categories = new List<CategoryData>();

        [Header("Visual")]
        [Tooltip("Level icon")]
        public Sprite icon;

        [Tooltip("Theme color for this level (UI)")]
        public Color themeColor = Color.white;

        [Header("Difficulty Settings")]
        [Tooltip("Minimum hold time required for this level")]
        [Range(0.1f, 2f)]
        public float minimumHoldTime = 0.3f;

        [Tooltip("Required accuracy (0-1) for gesture recognition")]
        [Range(0.5f, 1f)]
        public float recognitionAccuracy = 0.8f;

        /// <summary>
        /// Gets a category by its name.
        /// </summary>
        public CategoryData GetCategoryByName(string categoryName)
        {
            return categories.Find(c => c.categoryName.Equals(categoryName, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates that the level is correctly configured.
        /// </summary>
        public bool IsValid()
        {
            // No longer requires levelName to be filled - uses asset name as fallback
            if (categories == null || categories.Count == 0)
            {
                // Warning only, not error - allows empty levels without console spam
                Debug.LogWarning($"LevelData '{name}' no tiene categories assigned (sera marcado as 'Coming Soon').");
                return false;
            }

            bool allValid = true;
            for (int i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                if (category == null)
                {
                    Debug.LogError($"LevelData '{name}' tiene una CategoryData null en la posicion {i}.");
                    allValid = false;
                }
                else if (!category.IsValid())
                {
                    allValid = false;
                }
            }

            return allValid;
        }

        /// <summary>
        /// Gets the total number of categories in this level.
        /// </summary>
        public int GetCategoryCount()
        {
            return categories != null ? categories.Count : 0;
        }

        /// <summary>
        /// Gets the total number of signs in this level (summing all categories).
        /// </summary>
        public int GetTotalSignCount()
        {
            int total = 0;
            if (categories != null)
            {
                foreach (var category in categories)
                {
                    if (category != null)
                        total += category.GetSignCount();
                }
            }
            return total;
        }
    }
}
