using System.Collections.Generic;
using UnityEngine;

namespace ASL_LearnVR.Data
{
    /// <summary>
    /// Representa un nivel de aprendizaje (Básico, Intermedio, Avanzado).
    /// Contiene una lista de categorías disponibles en ese nivel.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevel", menuName = "ASL Learn VR/Level Data", order = 3)]
    public class LevelData : ScriptableObject
    {
        [Header("Level Information")]
        [Tooltip("Nombre del nivel (ej: 'Basic', 'Intermediate', 'Advanced')")]
        public string levelName;

        [Tooltip("Descripción del nivel")]
        [TextArea(3, 6)]
        public string description;

        [Header("Categories")]
        [Tooltip("Categorías disponibles en este nivel")]
        public List<CategoryData> categories = new List<CategoryData>();

        [Header("Visual")]
        [Tooltip("Icono del nivel")]
        public Sprite icon;

        [Tooltip("Color de tema para este nivel (UI)")]
        public Color themeColor = Color.white;

        [Header("Difficulty Settings")]
        [Tooltip("Tiempo mínimo de hold requerido para este nivel")]
        [Range(0.1f, 2f)]
        public float minimumHoldTime = 0.3f;

        [Tooltip("Precisión requerida (0-1) para reconocimiento de gestos")]
        [Range(0.5f, 1f)]
        public float recognitionAccuracy = 0.8f;

        /// <summary>
        /// Obtiene una categoría por su nombre.
        /// </summary>
        public CategoryData GetCategoryByName(string categoryName)
        {
            return categories.Find(c => c.categoryName.Equals(categoryName, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Valida que el nivel esté correctamente configurado.
        /// </summary>
        public bool IsValid()
        {
            // Ya NO requiere que levelName esté lleno - usa el nombre del asset como fallback
            if (categories == null || categories.Count == 0)
            {
                Debug.LogError($"LevelData '{name}' no tiene categorías asignadas.");
                return false;
            }

            bool allValid = true;
            foreach (var category in categories)
            {
                if (category == null)
                {
                    Debug.LogError($"LevelData '{name}' tiene una CategoryData null en la lista.");
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
        /// Obtiene el número total de categorías en este nivel.
        /// </summary>
        public int GetCategoryCount()
        {
            return categories != null ? categories.Count : 0;
        }

        /// <summary>
        /// Obtiene el número total de signos en este nivel (sumando todas las categorías).
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
