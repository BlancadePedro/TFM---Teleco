using UnityEngine;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Manages feedback audio for the learning system.
    /// Plays success/error sounds with optional mute.
    /// </summary>
    public class FeedbackAudio : MonoBehaviour
    {
        [Header("Audio Source")]
        [Tooltip("AudioSource used to play the clips")]
        [SerializeField] private AudioSource audioSource;

        [Header("Audio Clips")]
        [Tooltip("Sound when a gesture completes correctly")]
        [SerializeField] private AudioClip successClip;

        [Tooltip("Sound when a gesture fails (optional, can be muted)")]
        [SerializeField] private AudioClip errorClip;

        [Tooltip("Sound when practice starts")]
        [SerializeField] private AudioClip startPracticeClip;

        [Tooltip("Sound for partial success (optional)")]
        [SerializeField] private AudioClip partialSuccessClip;

        [Header("Settings")]
        [Tooltip("Volume for feedback sounds")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.5f;

        [Tooltip("Play success sound")]
        [SerializeField] private bool playSuccessSound = true;

        [Tooltip("Play error sound")]
        [SerializeField] private bool playErrorSound = false;

        [Tooltip("Global mute for feedback audio")]
        [SerializeField] private bool isMuted = false;

        [Header("Cooldown")]
        [Tooltip("Minimum time between sounds (prevents spam)")]
        [SerializeField] private float soundCooldown = 0.5f;

        // State interno
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
        /// Plays the success sound.
        /// </summary>
        public void PlaySuccess()
        {
            if (playSuccessSound && successClip != null)
            {
                PlayClip(successClip);
            }
        }

        /// <summary>
        /// Plays the error sound.
        /// </summary>
        public void PlayError()
        {
            if (playErrorSound && errorClip != null)
            {
                PlayClip(errorClip);
            }
        }

        /// <summary>
        /// Plays the start-practice sound.
        /// </summary>
        public void PlayStartPractice()
        {
            if (startPracticeClip != null)
            {
                PlayClip(startPracticeClip);
            }
        }

        /// <summary>
        /// Plays the partial-success sound.
        /// </summary>
        public void PlayPartialSuccess()
        {
            if (partialSuccessClip != null)
            {
                PlayClip(partialSuccessClip);
            }
        }

        /// <summary>
        /// Plays a clip respecting cooldown and mute.
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
        /// Sets mute state.
        /// </summary>
        public void SetMuted(bool muted)
        {
            isMuted = muted;
        }

        /// <summary>
        /// Enables/disables success sounds.
        /// </summary>
        public void SetSuccessSoundEnabled(bool enabled)
        {
            playSuccessSound = enabled;
        }

        /// <summary>
        /// Enables/disables error sounds.
        /// </summary>
        public void SetErrorSoundEnabled(bool enabled)
        {
            playErrorSound = enabled;
        }

        /// <summary>
        /// Sets the volume (0-1).
        /// </summary>
        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
        }

        /// <summary>
        /// True si el audio esta muteado.
        /// </summary>
        public bool IsMuted => isMuted;

        /// <summary>
        /// Volumen actual.
        /// </summary>
        public float Volume => volume;
    }
}
