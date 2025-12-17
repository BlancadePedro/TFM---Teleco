using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using System.Collections.Generic;
using ASL_LearnVR.Data;

namespace ASL_LearnVR.Gestures
{
    /// <summary>
    /// Reconoce gestos dinámicos (J, Z) comparando la trayectoria del usuario con una grabación.
    /// Usa DTW (Dynamic Time Warping) para comparar secuencias de movimiento.
    /// </summary>
    public class DynamicGestureRecognizer : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("El signo dinámico que se debe detectar")]
        [SerializeField] private SignData targetSign;

        [Tooltip("Componente XRHandTrackingEvents de la mano a detectar")]
        [SerializeField] private XRHandTrackingEvents handTrackingEvents;

        [Header("Detection Settings")]
        [Tooltip("Umbral de similitud DTW (más bajo = más estricto)")]
        [SerializeField] private float dtwThreshold = 0.5f;

        [Tooltip("Duración máxima de grabación del usuario (segundos)")]
        [SerializeField] private float maxRecordingTime = 3f;

        [Tooltip("Frecuencia de muestreo (frames por segundo)")]
        [SerializeField] private int samplingRate = 30;

        [Header("Events")]
        [Tooltip("Se invoca cuando el gesto dinámico es detectado correctamente")]
        public UnityEvent<SignData> onDynamicGestureDetected;

        [Tooltip("Se invoca cuando el gesto dinámico falla")]
        public UnityEvent<SignData> onDynamicGestureFailed;

        [Header("Debug")]
        [Tooltip("Mostrar logs de debug en la consola")]
        [SerializeField] private bool showDebugLogs = true;

        // Estado de grabación
        private bool isRecording = false;
        private List<Vector3> recordedPositions = new List<Vector3>();
        private List<Vector3> referencePositions = new List<Vector3>();
        private float recordingStartTime = 0f;
        private float lastSampleTime = 0f;

        /// <summary>
        /// True si está grabando el movimiento del usuario.
        /// </summary>
        public bool IsRecording => isRecording;

        void OnEnable()
        {
            if (handTrackingEvents != null)
            {
                handTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
                Debug.Log($"[DynamicGestureRecognizer] ACTIVADO con handTrackingEvents para '{(targetSign != null ? targetSign.signName : "sin signo")}'");
            }
            else
            {
                Debug.LogError("[DynamicGestureRecognizer] FALTA ASIGNAR 'handTrackingEvents'! Ve al Inspector y arrastra el GameObject 'Right Hand' al campo 'Hand Tracking Events'");
            }

            LoadReferenceTrajectory();
        }

        void OnDisable()
        {
            if (handTrackingEvents != null)
            {
                handTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);
            }
        }

        /// <summary>
        /// Carga la trayectoria de referencia desde la grabación.
        /// </summary>
        private void LoadReferenceTrajectory()
        {
            referencePositions.Clear();

            Debug.Log($"[DynamicGestureRecognizer] LoadReferenceTrajectory: targetSign={(targetSign != null ? targetSign.signName : "NULL")}");

            if (targetSign == null)
            {
                Debug.LogWarning("[DynamicGestureRecognizer] No hay targetSign asignado.");
                return;
            }

            if (targetSign.handRecordingData == null)
            {
                Debug.LogError($"[DynamicGestureRecognizer] '{targetSign.signName}' NO TIENE handRecordingData asignado!");
                return;
            }

            Debug.Log($"[DynamicGestureRecognizer] handRecordingData tipo: {targetSign.handRecordingData.GetType().Name}");

            // Usa reflection para acceder a los frames de la grabación
            var framesField = targetSign.handRecordingData.GetType().GetField("m_Frames",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (framesField == null)
            {
                Debug.LogError($"[DynamicGestureRecognizer] NO SE PUDO acceder al campo 'frames' de {targetSign.handRecordingData.GetType().Name}");
                Debug.LogError("[DynamicGestureRecognizer] Campos disponibles:");
                foreach (var field in targetSign.handRecordingData.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    Debug.LogError($"  - {field.Name} ({field.FieldType.Name})");
                }
                return;
            }

            var framesList = framesField.GetValue(targetSign.handRecordingData) as System.Collections.IList;

            Debug.Log($"[DynamicGestureRecognizer] framesList obtenido: {(framesList != null ? framesList.Count.ToString() : "NULL")} frames");

            if (framesList == null || framesList.Count == 0)
            {
                Debug.LogError($"[DynamicGestureRecognizer] La grabación NO TIENE FRAMES o framesList es NULL!");
                return;
            }

            // Determina el rango de frames a usar
            int startFrame = targetSign.recordingStartFrame;
            int endFrame = targetSign.recordingEndFrame > 0 ? targetSign.recordingEndFrame : framesList.Count;

            Debug.Log($"[DynamicGestureRecognizer] Rango ANTES de clamp: start={startFrame}, end={endFrame}, total frames={framesList.Count}");

            // Asegura que el rango sea válido
            startFrame = Mathf.Clamp(startFrame, 0, framesList.Count - 1);
            endFrame = Mathf.Clamp(endFrame, startFrame + 1, framesList.Count);

            Debug.Log($"[DynamicGestureRecognizer] Rango DESPUÉS de clamp: start={startFrame}, end={endFrame}");

            // Extrae la posición de la punta del dedo índice (o meñique para J) de cada frame
            for (int i = startFrame; i < endFrame; i++)
            {
                var frame = framesList[i];
                Vector3 tipPosition = ExtractTipPosition(frame);
                referencePositions.Add(tipPosition);
            }

            Debug.Log($"[DynamicGestureRecognizer] CARGADOS {referencePositions.Count} puntos de referencia para '{targetSign.signName}'");

            if (referencePositions.Count == 0)
            {
                Debug.LogError($"[DynamicGestureRecognizer] FALLO CRITICO: 0 puntos cargados! Verifica que ExtractTipPosition funcione.");
            }
        }

        /// <summary>
        /// Extrae la posición de la punta del dedo relevante del frame.
        /// </summary>
        private Vector3 ExtractTipPosition(object frameObj)
        {
            // Cast al tipo correcto
            if (!(frameObj is UnityEngine.XR.Hands.Capture.XRHandCaptureFrame))
            {
                Debug.LogError("[DynamicGestureRecognizer] Frame no es del tipo XRHandCaptureFrame");
                return Vector3.zero;
            }

            var frame = (UnityEngine.XR.Hands.Capture.XRHandCaptureFrame)frameObj;

            // Para J: usa la punta del meñique (XRHandJointID.LittleTip)
            // Para Z: usa la punta del índice (XRHandJointID.IndexTip)
            XRHandJointID tipJoint = targetSign.signName == "J" ? XRHandJointID.LittleTip : XRHandJointID.IndexTip;

            // Intenta obtener el joint de la mano derecha (asumiendo que la grabación es de mano derecha)
            if (frame.TryGetJoint(out XRHandJoint joint, Handedness.Right, tipJoint))
            {
                if (joint.TryGetPose(out Pose pose))
                {
                    return pose.position;
                }
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Callback cuando los joints de la mano se actualizan.
        /// </summary>
        private void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            if (!isRecording)
                return;

            if (handTrackingEvents == null || !handTrackingEvents.handIsTracked)
                return;

            float currentTime = Time.timeSinceLevelLoad;

            // Verifica si ha pasado el intervalo de muestreo
            if (currentTime - lastSampleTime < 1f / samplingRate)
                return;

            // Obtiene la posición de la punta del dedo
            XRHandJointID tipJoint = targetSign.signName == "J" ? XRHandJointID.LittleTip : XRHandJointID.IndexTip;

            if (eventArgs.hand.GetJoint(tipJoint).TryGetPose(out Pose tipPose))
            {
                recordedPositions.Add(tipPose.position);
                lastSampleTime = currentTime;

                if (showDebugLogs && recordedPositions.Count % 10 == 0)
                    Debug.Log($"[DynamicGestureRecognizer] Grabados {recordedPositions.Count} puntos, tiempo: {currentTime - recordingStartTime:F2}s");
            }

            // Verifica si se excedió el tiempo máximo
            if (currentTime - recordingStartTime > maxRecordingTime)
            {
                Debug.Log($"[DynamicGestureRecognizer] Tiempo máximo alcanzado ({maxRecordingTime}s), deteniendo grabación...");
                StopRecording();
            }
        }

        /// <summary>
        /// Inicia la grabación del movimiento del usuario.
        /// </summary>
        public void StartRecording()
        {
            Debug.Log($"[DynamicGestureRecognizer] StartRecording llamado. targetSign={(targetSign != null ? targetSign.signName : "NULL")}, referencePositions.Count={referencePositions.Count}");

            if (targetSign == null)
            {
                Debug.LogError("[DynamicGestureRecognizer] ERROR: targetSign es NULL!");
                return;
            }

            if (referencePositions.Count == 0)
            {
                Debug.LogError($"[DynamicGestureRecognizer] ERROR: No hay puntos de referencia cargados para '{targetSign.signName}'!");
                Debug.LogError($"[DynamicGestureRecognizer] handRecordingData={(targetSign.handRecordingData != null ? "ASIGNADO" : "NULL")}");
                Debug.LogError($"[DynamicGestureRecognizer] Intentando recargar trayectoria...");
                LoadReferenceTrajectory();

                if (referencePositions.Count == 0)
                {
                    Debug.LogError("[DynamicGestureRecognizer] FALLO: Aún no hay puntos después de recargar. NO SE PUEDE GRABAR.");
                    return;
                }
            }

            recordedPositions.Clear();
            isRecording = true;
            recordingStartTime = Time.timeSinceLevelLoad;
            lastSampleTime = recordingStartTime;

            Debug.Log($"[DynamicGestureRecognizer] GRABACION INICIADA para '{targetSign.signName}'. {referencePositions.Count} puntos de referencia cargados.");
        }

        /// <summary>
        /// Detiene la grabación y compara con la trayectoria de referencia.
        /// </summary>
        public void StopRecording()
        {
            Debug.Log($"[DynamicGestureRecognizer] StopRecording llamado, isRecording={isRecording}");

            if (!isRecording)
                return;

            isRecording = false;

            Debug.Log($"[DynamicGestureRecognizer] Detenida grabación. {recordedPositions.Count} puntos grabados.");

            // Verifica que haya suficientes puntos
            if (recordedPositions.Count < 10)
            {
                Debug.LogWarning($"[DynamicGestureRecognizer] Grabación muy corta: {recordedPositions.Count} puntos");
                onDynamicGestureFailed?.Invoke(targetSign);
                return;
            }

            Debug.Log($"[DynamicGestureRecognizer] Comparando {recordedPositions.Count} puntos grabados con {referencePositions.Count} puntos de referencia");

            // Compara usando DTW
            float similarity = ComputeDTW(recordedPositions, referencePositions);

            Debug.Log($"[DynamicGestureRecognizer] Similitud DTW = {similarity:F3} (umbral = {dtwThreshold})");

            if (similarity <= dtwThreshold)
            {
                Debug.Log($"[DynamicGestureRecognizer] GESTO CORRECTO! '{targetSign.signName}' detectado!");
                onDynamicGestureDetected?.Invoke(targetSign);
            }
            else
            {
                Debug.LogWarning($"[DynamicGestureRecognizer] GESTO INCORRECTO. '{targetSign.signName}' no coincide (similitud {similarity:F3} > umbral {dtwThreshold})");
                onDynamicGestureFailed?.Invoke(targetSign);
            }
        }

        /// <summary>
        /// Calcula la distancia DTW (Dynamic Time Warping) entre dos trayectorias.
        /// Retorna un valor normalizado donde 0 = idénticas, 1+ = muy diferentes.
        /// </summary>
        private float ComputeDTW(List<Vector3> sequence1, List<Vector3> sequence2)
        {
            int n = sequence1.Count;
            int m = sequence2.Count;

            // Matriz DTW
            float[,] dtw = new float[n + 1, m + 1];

            // Inicializa con infinito
            for (int i = 0; i <= n; i++)
                for (int j = 0; j <= m; j++)
                    dtw[i, j] = float.MaxValue;

            dtw[0, 0] = 0;

            // Calcula DTW
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    float cost = Vector3.Distance(sequence1[i - 1], sequence2[j - 1]);
                    dtw[i, j] = cost + Mathf.Min(
                        dtw[i - 1, j],      // Inserción
                        dtw[i, j - 1],      // Eliminación
                        dtw[i - 1, j - 1]   // Match
                    );
                }
            }

            // Normaliza por la longitud de las secuencias
            float normalizedDistance = dtw[n, m] / Mathf.Max(n, m);

            return normalizedDistance;
        }

        /// <summary>
        /// Configura el signo objetivo.
        /// </summary>
        public void SetTargetSign(SignData sign)
        {
            Debug.Log($"[DynamicGestureRecognizer] SetTargetSign: {(sign != null ? sign.signName : "NULL")}");
            targetSign = sign;
            LoadReferenceTrajectory();
            Debug.Log($"[DynamicGestureRecognizer] Después de SetTargetSign, referencePositions.Count={referencePositions.Count}");
        }
    }
}
