using UnityEngine;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Gestiona el audio de feedback para el sistema de aprendizaje.
    /// Reproduce sonidos al éxito/error con opción de mute.
    /// </summary>
    public class FeedbackAudio : MonoBehaviour
    {
        [Header("Audio Source")]
        [Tooltip("AudioSource para reproducir los clips")]
        [SerializeField] private AudioSource audioSource;

        [Header("Audio Clips")]
        [Tooltip("Sonido al completar gesto correctamente")]
        [SerializeField] private AudioClip successClip;

        [Tooltip("Sonido al fallar gesto (opcional, puede estar muted)")]
        [SerializeField] private AudioClip errorClip;

        [Tooltip("Sonido al iniciar práctica")]
        [SerializeField] private AudioClip startPracticeClip;

        [Tooltip("Sonido de progreso parcial (opcional)")]
        [SerializeField] private AudioClip partialSuccessClip;

        [Header("Settings")]
        [Tooltip("Volumen de los sonidos de feedback")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.5f;

        [Tooltip("Reproducir sonido de éxito")]
        [SerializeField] private bool playSuccessSound = true;

        [Tooltip("Reproducir sonido de error")]
        [SerializeField] private bool playErrorSound = false;

        [Tooltip("Mute global del audio de feedback")]
        [SerializeField] private bool isMuted = false;

        [Header("Cooldown")]
        [Tooltip("Tiempo mínimo entre sonidos (evita spam)")]
        [SerializeField] private float soundCooldown = 0.5f;

        // Estado interno
        private float lastSoundTime = 0f;

        void Awake()
        {
            // Crear AudioSource si no existe
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }

        /// <summary>
        /// Reproduce el sonido de éxito.
        /// </summary>
        public void PlaySuccess()
        {
            if (playSuccessSound && successClip != null)
            {
                PlayClip(successClip);
            }
        }

        /// <summary>
        /// Reproduce el sonido de error.
        /// </summary>
        public void PlayError()
        {
            if (playErrorSound && errorClip != null)
            {
                PlayClip(errorClip);
            }
        }

        /// <summary>
        /// Reproduce el sonido de inicio de práctica.
        /// </summary>
        public void PlayStartPractice()
        {
            if (startPracticeClip != null)
            {
                PlayClip(startPracticeClip);
            }
        }

        /// <summary>
        /// Reproduce el sonido de éxito parcial.
        /// </summary>
        public void PlayPartialSuccess()
        {
            if (partialSuccessClip != null)
            {
                PlayClip(partialSuccessClip);
            }
        }

        /// <summary>
        /// Reproduce un clip respetando cooldown y mute.
        /// </summary>
        private void PlayClip(AudioClip clip)
        {
            if (isMuted || clip == null || audioSource == null)
                return;

            // Verificar cooldown
            if (Time.time - lastSoundTime < soundCooldown)
                return;

            audioSource.PlayOneShot(clip, volume);
            lastSoundTime = Time.time;
        }

        /// <summary>
        /// Establece el estado de mute.
        /// </summary>
        public void SetMuted(bool muted)
        {
            isMuted = muted;
        }

        /// <summary>
        /// Activa/desactiva los sonidos de éxito.
        /// </summary>
        public void SetSuccessSoundEnabled(bool enabled)
        {
            playSuccessSound = enabled;
        }

        /// <summary>
        /// Activa/desactiva los sonidos de error.
        /// </summary>
        public void SetErrorSoundEnabled(bool enabled)
        {
            playErrorSound = enabled;
        }

        /// <summary>
        /// Establece el volumen (0-1).
        /// </summary>
        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
        }

        /// <summary>
        /// True si el audio está muteado.
        /// </summary>
        public bool IsMuted => isMuted;

        /// <summary>
        /// Volumen actual.
        /// </summary>
        public float Volume => volume;
    }
}
