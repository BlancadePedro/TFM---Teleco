using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace ASL_LearnVR.Utils
{
    /// <summary>
    /// Muestra logs de debug en pantalla en VR.
    /// </summary>
    public class DebugLogger : MonoBehaviour
    {
        private static DebugLogger instance;
        public static DebugLogger Instance => instance;

        [SerializeField] private TextMeshProUGUI debugText;
        [SerializeField] private int maxLines = 20;

        private Queue<string> logLines = new Queue<string>();

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            Application.logMessageReceived += HandleLog;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            // NOTA: DebugLogger deshabilitado - no se muestran logs en pantalla
            return;
        }

        private void UpdateDebugText()
        {
            if (debugText != null)
            {
                debugText.text = string.Join("\n", logLines);
            }
        }

        public static void Log(string message)
        {
            Debug.Log($"[DEBUG] {message}");
        }
    }
}
