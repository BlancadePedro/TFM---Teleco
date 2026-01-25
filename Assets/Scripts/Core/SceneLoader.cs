using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ASL_LearnVR.Core
{
    /// <summary>
    /// Gestiona la carga de escenas con transiciones opcionales.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader _instance;

        public static SceneLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SceneLoader>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SceneLoader");
                        _instance = go.AddComponent<SceneLoader>();
                    }
                }
                return _instance;
            }
        }

        [Header("Scene Names")]
        [Tooltip("Nombre de la escena del menú principal")]
        public string mainMenuSceneName = "01_MainMenu";

        [Tooltip("Nombre de la escena de selección de nivel")]
        public string levelSelectionSceneName = "02_LevelSelection";

        [Tooltip("Nombre de la escena del módulo de aprendizaje")]
        public string learningModuleSceneName = "03_LearningModule";

        [Tooltip("Nombre de la escena del modo autoevaluación")]
        public string selfAssessmentSceneName = "04_SelfAssessmentMode";

        [Header("Loading Settings")]
        [Tooltip("Tiempo de espera opcional antes de cargar la escena (para transiciones)")]
        [SerializeField] private float loadDelay = 0f;

        private bool isLoading = false;

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
        /// Carga la escena del menú principal.
        /// </summary>
        public void LoadMainMenu()
        {
            LoadScene(mainMenuSceneName);
        }

        /// <summary>
        /// Carga la escena de selección de nivel.
        /// </summary>
        public void LoadLevelSelection()
        {
            LoadScene(levelSelectionSceneName);
        }

        /// <summary>
        /// Carga la escena del módulo de aprendizaje.
        /// </summary>
        public void LoadLearningModule()
        {
            if (!GameManager.Instance.HasLevelAndCategory())
            {
                Debug.LogError("No se puede cargar LearningModule sin un nivel y categoría seleccionados.");
                return;
            }
            LoadScene(learningModuleSceneName);
        }

        /// <summary>
        /// Carga la escena del modo autoevaluación.
        /// </summary>
        public void LoadSelfAssessmentMode()
        {
            if (!GameManager.Instance.HasLevelAndCategory())
            {
                Debug.LogError("No se puede cargar SelfAssessmentMode sin un nivel y categoría seleccionados.");
                return;
            }
            LoadScene(selfAssessmentSceneName);
        }

        /// <summary>
        /// Carga una escena por nombre.
        /// </summary>
        private void LoadScene(string sceneName)
        {
            if (isLoading)
            {
                Debug.LogWarning("Ya hay una escena cargándose. Ignorando petición.");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("El nombre de la escena está vacío.");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName));
        }

        /// <summary>
        /// Corrutina que carga la escena de forma asíncrona.
        /// </summary>
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            isLoading = true;

            // Espera opcional antes de cargar (para transiciones visuales)
            if (loadDelay > 0f)
            {
                yield return new WaitForSeconds(loadDelay);
            }

            // Carga la escena de forma asíncrona
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            // Espera hasta que la escena esté completamente cargada
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            isLoading = false;
        }

        /// <summary>
        /// Cierra la aplicación (solo funciona en build, no en editor).
        /// </summary>
        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
