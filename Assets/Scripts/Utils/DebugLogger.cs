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
            // Solo muestra logs que contengan estos prefijos
            if (logString.Contains("[DynamicGestureRecognizer]") ||
                logString.Contains("[LearningController]") ||
                logString.Contains("[GestureRecognizer]"))
            {
                string coloredLog = logString;

                // Colorea según el tipo
                if (type == LogType.Error)
                    coloredLog = $"<color=red>{logString}</color>";
                else if (type == LogType.Warning)
                    coloredLog = $"<color=yellow>{logString}</color>";
                else
                    coloredLog = $"<color=white>{logString}</color>";

                logLines.Enqueue(coloredLog);

                // Mantiene solo las últimas N líneas
                while (logLines.Count > maxLines)
                    logLines.Dequeue();

                UpdateDebugText();
            }
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
