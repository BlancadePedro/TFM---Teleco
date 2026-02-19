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
        [Tooltip("Name de la escena del menu principal")]
        public string mainMenuSceneName = "01_MainMenu";

        [Tooltip("Name de la escena de seleccion de nivel")]
        public string levelSelectionSceneName = "02_LevelSelection";

        [Tooltip("Name de la escena del modulo de aprendizaje")]
        public string learningModuleSceneName = "03_LearningModule";

        [Tooltip("Name de la escena del modo autoevaluacion")]
        public string selfAssessmentSceneName = "04_SelfAssessmentMode";

        [Header("Loading Settings")]
        [Tooltip("Time de espera opcional antes de cargar la escena (para transiciones)")]
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
        /// Carga la escena del menu principal.
        /// </summary>
        public void LoadMainMenu()
        {
            LoadScene(mainMenuSceneName);
        }

        /// <summary>
        /// Loads the level selection scene.
        /// </summary>
        public void LoadLevelSelection()
        {
            LoadScene(levelSelectionSceneName);
        }

        /// <summary>
        /// Carga la escena del modulo de aprendizaje.
        /// </summary>
        public void LoadLearningModule()
        {
            if (!GameManager.Instance.HasLevelAndCategory())
            {
                Debug.LogError("No se puede cargar LearningModule sin un nivel y category selecteds.");
                return;
            }
            LoadScene(learningModuleSceneName);
        }

        /// <summary>
        /// Carga la escena del modo autoevaluacion.
        /// </summary>
        public void LoadSelfAssessmentMode()
        {
            if (!GameManager.Instance.HasLevelAndCategory())
            {
                Debug.LogError("No se puede cargar SelfAssessmentMode sin un nivel y category selecteds.");
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
                Debug.LogWarning("Ya hay una escena cargandose. Ignorando peticion.");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("El nombre de la escena esta vacio.");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName));
        }

        /// <summary>
        /// Corrutina que carga la escena de forma asincrona.
        /// </summary>
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            isLoading = true;

            // Espera opcional antes de cargar (para transiciones visuales)
            if (loadDelay > 0f)
            {
                yield return new WaitForSeconds(loadDelay);
            }

            // Carga la escena de forma asincrona
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            // Espera hasta que la escena este completamente loaded
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            isLoading = false;
        }

        /// <summary>
        /// Cierra la aplicacion (solo funciona en build, no en editor).
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
