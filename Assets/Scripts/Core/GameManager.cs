using UnityEngine;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.Core
{
    /// <summary>
    /// Singleton que gestiona el estado global de la aplicación.
    /// Mantiene referencias al nivel, categoría y signo actualmente seleccionados.
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
        /// Nivel actualmente seleccionado.
        /// </summary>
        public LevelData CurrentLevel
        {
            get => currentLevel;
            set => currentLevel = value;
        }

        /// <summary>
        /// Categoría actualmente seleccionada.
        /// </summary>
        public CategoryData CurrentCategory
        {
            get => currentCategory;
            set => currentCategory = value;
        }

        /// <summary>
        /// Signo actualmente seleccionado.
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
        /// Limpia la sesión actual.
        /// </summary>
        public void ClearSession()
        {
            currentLevel = null;
            currentCategory = null;
            currentSign = null;
        }

        /// <summary>
        /// Valida que haya un nivel, categoría y signo seleccionados.
        /// </summary>
        public bool HasValidSession()
        {
            return currentLevel != null && currentCategory != null && currentSign != null;
        }

        /// <summary>
        /// Valida que haya un nivel y categoría seleccionados.
        /// </summary>
        public bool HasLevelAndCategory()
        {
            return currentLevel != null && currentCategory != null;
        }
    }
}
