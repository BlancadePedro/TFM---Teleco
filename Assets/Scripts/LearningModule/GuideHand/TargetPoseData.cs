using System;
using UnityEngine;

namespace ASL_LearnVR.LearningModule.GuideHand
{
    /// <summary>
    /// Datos de pose objetivo para un dedo individual.
    /// Los valores de curl van de 0 (extendido) a 1 (cerrado).
    /// </summary>
    [Serializable]
    public struct FingerPoseData
    {
        [Range(0f, 1f)]
        [Tooltip("Curl del metacarpal (0=neutro, 1=curvado)")]
        public float metacarpalCurl;

        [Range(0f, 1f)]
        [Tooltip("Curl del proximal (0=extendido, 1=cerrado)")]
        public float proximalCurl;

        [Range(0f, 1f)]
        [Tooltip("Curl del intermediate (0=extendido, 1=cerrado)")]
        public float intermediateCurl;

        [Range(0f, 1f)]
        [Tooltip("Curl del distal (0=extendido, 1=cerrado)")]
        public float distalCurl;

        [Range(-30f, 30f)]
        [Tooltip("Spread (abducción) del dedo en grados")]
        public float spreadAngle;

        /// <summary>
        /// Crea una pose de dedo extendido.
        /// </summary>
        public static FingerPoseData Extended => new FingerPoseData
        {
            metacarpalCurl = 0f,
            proximalCurl = 0f,
            intermediateCurl = 0f,
            distalCurl = 0f,
            spreadAngle = 0f
        };

        /// <summary>
        /// Crea una pose de dedo completamente cerrado (puño).
        /// </summary>
        public static FingerPoseData FullyCurled => new FingerPoseData
        {
            metacarpalCurl = 0.2f,
            proximalCurl = 1f,
            intermediateCurl = 1f,
            distalCurl = 1f,
            spreadAngle = 0f
        };

        /// <summary>
        /// Crea una pose de dedo parcialmente curvado.
        /// </summary>
        public static FingerPoseData PartiallyCurled => new FingerPoseData
        {
            metacarpalCurl = 0.1f,
            proximalCurl = 0.5f,
            intermediateCurl = 0.5f,
            distalCurl = 0.5f,
            spreadAngle = 0f
        };

        /// <summary>
        /// Crea una pose de tip curl (solo puntas curvadas).
        /// </summary>
        public static FingerPoseData TipCurl => new FingerPoseData
        {
            metacarpalCurl = 0.1f,
            proximalCurl = 0.3f,
            intermediateCurl = 0.7f,
            distalCurl = 0.8f,
            spreadAngle = 0f
        };

        /// <summary>
        /// Crea una pose desde un valor de curl promedio.
        /// </summary>
        public static FingerPoseData FromCurlValue(float curl)
        {
            return new FingerPoseData
            {
                metacarpalCurl = curl * 0.2f,
                proximalCurl = curl,
                intermediateCurl = curl,
                distalCurl = curl,
                spreadAngle = 0f
            };
        }

        /// <summary>
        /// Interpola entre dos poses de dedo.
        /// </summary>
        public static FingerPoseData Lerp(FingerPoseData a, FingerPoseData b, float t)
        {
            return new FingerPoseData
            {
                metacarpalCurl = Mathf.Lerp(a.metacarpalCurl, b.metacarpalCurl, t),
                proximalCurl = Mathf.Lerp(a.proximalCurl, b.proximalCurl, t),
                intermediateCurl = Mathf.Lerp(a.intermediateCurl, b.intermediateCurl, t),
                distalCurl = Mathf.Lerp(a.distalCurl, b.distalCurl, t),
                spreadAngle = Mathf.Lerp(a.spreadAngle, b.spreadAngle, t)
            };
        }
    }

    /// <summary>
    /// Datos de pose objetivo para el pulgar.
    /// El pulgar tiene una estructura diferente (sin intermediate).
    /// </summary>
    [Serializable]
    public struct ThumbPoseData
    {
        [Range(0f, 1f)]
        [Tooltip("Curl del metacarpal")]
        public float metacarpalCurl;

        [Range(0f, 1f)]
        [Tooltip("Curl del proximal")]
        public float proximalCurl;

        [Range(0f, 1f)]
        [Tooltip("Curl del distal")]
        public float distalCurl;

        [Range(-45f, 45f)]
        [Tooltip("Rotación lateral del pulgar (abducción)")]
        public float abductionAngle;

        [Range(-30f, 30f)]
        [Tooltip("Oposición del pulgar (cruzar hacia otros dedos)")]
        public float oppositionAngle;

        [Range(-90f, 90f)]
        [Tooltip("Rotación axial del pulgar (twist) para orientar la uña")]
        public float distalTwist;

        [Range(-45f, 45f)]
        [Tooltip("Inclinación del pulgar hacia/desde el usuario (pitch)")]
        public float thumbPitch;

        /// <summary>
        /// Crea una pose de pulgar extendido al lado.
        /// </summary>
        public static ThumbPoseData ExtendedBeside => new ThumbPoseData
        {
            metacarpalCurl = 0.1f,
            proximalCurl = 0.15f,
            distalCurl = 0.1f,
            abductionAngle = 15f,
            oppositionAngle = 0f
        };

        /// <summary>
        /// Crea una pose de pulgar doblado sobre los dedos.
        /// </summary>
        public static ThumbPoseData OverFingers => new ThumbPoseData
        {
            metacarpalCurl = 0.3f,
            proximalCurl = 0.5f,
            distalCurl = 0.4f,
            abductionAngle = -10f,
            oppositionAngle = 20f
        };

        /// <summary>
        /// Crea una pose de pulgar cruzado en la palma.
        /// </summary>
        public static ThumbPoseData AcrossPalm => new ThumbPoseData
        {
            metacarpalCurl = 0.4f,
            proximalCurl = 0.6f,
            distalCurl = 0.5f,
            abductionAngle = -20f,
            oppositionAngle = 30f
        };

        /// <summary>
        /// Crea una pose de pulgar tocando un dedo (como O, F).
        /// </summary>
        public static ThumbPoseData TouchingFinger => new ThumbPoseData
        {
            metacarpalCurl = 0.3f,
            proximalCurl = 0.45f,
            distalCurl = 0.4f,
            abductionAngle = 0f,
            oppositionAngle = 25f
        };

        /// <summary>
        /// Crea una pose desde un valor de curl promedio.
        /// </summary>
        public static ThumbPoseData FromCurlValue(float curl)
        {
            return new ThumbPoseData
            {
                metacarpalCurl = curl * 0.4f,
                proximalCurl = curl * 0.8f,
                distalCurl = curl * 0.6f,
                abductionAngle = Mathf.Lerp(20f, -20f, curl),
                oppositionAngle = Mathf.Lerp(0f, 30f, curl)
            };
        }

        /// <summary>
        /// Interpola entre dos poses de pulgar.
        /// </summary>
        public static ThumbPoseData Lerp(ThumbPoseData a, ThumbPoseData b, float t)
        {
            return new ThumbPoseData
            {
                metacarpalCurl = Mathf.Lerp(a.metacarpalCurl, b.metacarpalCurl, t),
                proximalCurl = Mathf.Lerp(a.proximalCurl, b.proximalCurl, t),
                distalCurl = Mathf.Lerp(a.distalCurl, b.distalCurl, t),
                abductionAngle = Mathf.Lerp(a.abductionAngle, b.abductionAngle, t),
                oppositionAngle = Mathf.Lerp(a.oppositionAngle, b.oppositionAngle, t),
                distalTwist = Mathf.Lerp(a.distalTwist, b.distalTwist, t),
                thumbPitch = Mathf.Lerp(a.thumbPitch, b.thumbPitch, t)
            };
        }
    }

    /// <summary>
    /// Datos completos de pose para una mano.
    /// </summary>
    [Serializable]
    public class HandPoseData
    {
        public string poseName;
        public ThumbPoseData thumb;
        public FingerPoseData index;
        public FingerPoseData middle;
        public FingerPoseData ring;
        public FingerPoseData pinky;

        [Header("Wrist Orientation")]
        [Tooltip("Rotación del wrist respecto a su pose neutral")]
        public Vector3 wristRotationOffset = Vector3.zero;

        [Tooltip("Offset de posición local del wrist (para subir/bajar la mano)")]
        public Vector3 wristPositionOffset = Vector3.zero;

        /// <summary>
        /// Crea una pose de mano abierta (todos los dedos extendidos).
        /// </summary>
        public static HandPoseData OpenHand()
        {
            return new HandPoseData
            {
                poseName = "OpenHand",
                thumb = ThumbPoseData.ExtendedBeside,
                index = FingerPoseData.Extended,
                middle = FingerPoseData.Extended,
                ring = FingerPoseData.Extended,
                pinky = FingerPoseData.Extended
            };
        }

        /// <summary>
        /// Crea una pose de puño cerrado.
        /// </summary>
        public static HandPoseData Fist()
        {
            return new HandPoseData
            {
                poseName = "Fist",
                thumb = ThumbPoseData.OverFingers,
                index = FingerPoseData.FullyCurled,
                middle = FingerPoseData.FullyCurled,
                ring = FingerPoseData.FullyCurled,
                pinky = FingerPoseData.FullyCurled
            };
        }

        /// <summary>
        /// Interpola entre dos poses de mano.
        /// </summary>
        public static HandPoseData Lerp(HandPoseData a, HandPoseData b, float t)
        {
            return new HandPoseData
            {
                poseName = t < 0.5f ? a.poseName : b.poseName,
                thumb = ThumbPoseData.Lerp(a.thumb, b.thumb, t),
                index = FingerPoseData.Lerp(a.index, b.index, t),
                middle = FingerPoseData.Lerp(a.middle, b.middle, t),
                ring = FingerPoseData.Lerp(a.ring, b.ring, t),
                pinky = FingerPoseData.Lerp(a.pinky, b.pinky, t),
                wristRotationOffset = Vector3.Lerp(a.wristRotationOffset, b.wristRotationOffset, t),
                wristPositionOffset = Vector3.Lerp(a.wristPositionOffset, b.wristPositionOffset, t)
            };
        }
    }

    /// <summary>
    /// Un keyframe de pose: pose completa en un instante de tiempo.
    /// </summary>
    [Serializable]
    public struct PoseKeyframe
    {
        [Tooltip("Tiempo en segundos desde el inicio de la animación")]
        public float time;

        [Tooltip("Pose completa en este keyframe")]
        public HandPoseData pose;

        public PoseKeyframe(float time, HandPoseData pose)
        {
            this.time = time;
            this.pose = pose;
        }
    }

    /// <summary>
    /// Secuencia animada de poses (para letras como J y Z que requieren movimiento).
    /// </summary>
    [Serializable]
    public class AnimatedPoseSequence
    {
        public string poseName;
        public PoseKeyframe[] keyframes;
        public bool loop;

        /// <summary>
        /// Duración total de la secuencia (tiempo del último keyframe).
        /// </summary>
        public float Duration => keyframes != null && keyframes.Length > 0
            ? keyframes[keyframes.Length - 1].time
            : 0f;

        /// <summary>
        /// Muestrea la secuencia en un instante dado, interpolando entre keyframes.
        /// </summary>
        public HandPoseData SampleAtTime(float t)
        {
            if (keyframes == null || keyframes.Length == 0)
                return HandPoseData.OpenHand();

            // Antes del primer keyframe
            if (t <= keyframes[0].time)
                return keyframes[0].pose;

            // Después del último keyframe
            if (t >= keyframes[keyframes.Length - 1].time)
                return keyframes[keyframes.Length - 1].pose;

            // Buscar los dos keyframes que rodean t
            for (int i = 0; i < keyframes.Length - 1; i++)
            {
                if (t >= keyframes[i].time && t <= keyframes[i + 1].time)
                {
                    float segmentDuration = keyframes[i + 1].time - keyframes[i].time;
                    float localT = (t - keyframes[i].time) / segmentDuration;
                    return HandPoseData.Lerp(keyframes[i].pose, keyframes[i + 1].pose, localT);
                }
            }

            return keyframes[keyframes.Length - 1].pose;
        }
    }
}
