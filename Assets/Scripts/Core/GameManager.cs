using UnityEngine;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.Core
{
    /// <summary>
    /// Manages the global application state.
    /// Maintains references to the currently selected level, category, and sign.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Current Session Data")]
        [SerializeField] private LevelData currentLevel;
        [SerializeField] private CategoryData currentCategory;
        [SerializeField] private SignData currentSign;

        /// <summary>
        /// Currently selected level.
        /// </summary>
        public LevelData CurrentLevel
        {
            get => currentLevel;
            set => currentLevel = value;
        }

        /// <summary>
        /// Currently selected category.
        /// </summary>
        public CategoryData CurrentCategory
        {
            get => currentCategory;
            set => currentCategory = value;
        }

        /// <summary>
        /// Currently selected sign.
        /// </summary>
        public SignData CurrentSign
        {
            get => currentSign;
            set => currentSign = value;
        }

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Clears the current session.
        /// </summary>
        public void ClearSession()
        {
            currentLevel = null;
            currentCategory = null;
            currentSign = null;
        }

        /// <summary>
        /// Validates that a level, category, and sign are selected.
        /// </summary>
        public bool HasValidSession()
        {
            return currentLevel != null && currentCategory != null && currentSign != null;
        }

        /// <summary>
        /// Validates that a level and category are selected.
        /// </summary>
        public bool HasLevelAndCategory()
        {
            return currentLevel != null && currentCategory != null;
        }
    }
}
