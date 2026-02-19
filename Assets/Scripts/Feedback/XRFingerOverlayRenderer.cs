using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ASL_LearnVR.Feedback
{
    /// <summary>
    /// Status visual para los overlays de dedos.
    /// Mapea a los colores: Correct=verde, Almost=rojo (sin naranjas), Wrong=rojo, None=transparente.
    /// </summary>
    public enum FingerOverlayStatus
    {
        None,
        Correct,
        Almost,
        Wrong
    }

    /// <summary>
    /// Renderiza overlays visuales (capsulas/cilindros) sobre los dedos de la mano.
    /// Cada dedo se pinta con 3 segmentos (prox→inter, inter→dist, dist→tip).
    /// No depende del mesh/material de XRHandVisualizer, usa geometria independiente.
    /// </summary>
    public class XRFingerOverlayRenderer : MonoBehaviour
    {
        [Header("XR Hands")]
        [Tooltip("Hand a visualizar")]
        public Handedness handedness = Handedness.Right;

        [Header("Visual")]
        [Tooltip("Prefab de segmento (Capsule/Cylinder orientado en +Y)")]
        public Transform segmentPrefab;

        [Range(0.002f, 0.02f)]
        [Tooltip("Radius de los segmentos")]
        public float radius = 0.006f;

        [Tooltip("Incluir el pulgar en la visualizacion")]
        public bool includeThumb = true;

        [Header("Colors")]
        [Tooltip("Color when finger is correct")]
        public Color correctColor = new Color(0.2f, 1f, 0.2f, 0.8f);

        [Tooltip("Color when finger is almost correct")]
        public Color almostColor = new Color(1f, 0.2f, 0.2f, 0.8f);

        [Tooltip("Color when finger is incorrect")]
        public Color wrongColor = new Color(1f, 0.2f, 0.2f, 0.8f);

        [Tooltip("Color cuando no hay estado (transparente/oculto)")]
        public Color noneColor = new Color(1f, 1f, 1f, 0.0f);

        private XRHandSubsystem _subsystem;
        private readonly Dictionary<Finger, FingerOverlayStatus> _status = new();
        private readonly Dictionary<Finger, Transform[]> _segments = new();
        private bool _isVisible = true;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _mpb;

        void Awake()
        {
            // Inicializar todos los dedos con estado None
            foreach (Finger f in Enum.GetValues(typeof(Finger)))
            {
                _status[f] = FingerOverlayStatus.None;
            }
        }

        void OnEnable()
        {
            TryGetSubsystem();
            BuildSegmentsIfNeeded();
        }

        void Update()
        {
            // Si esta desactivado manualmente, no hacer nada
            if (!_isVisible)
            {
                return;
            }

            if (_subsystem == null && !TryGetSubsystem())
            {
                SetAllVisible(false);
                return;
            }

            XRHand hand = handedness == Handedness.Left ? _subsystem.leftHand : _subsystem.rightHand;
            if (!hand.isTracked)
            {
                SetAllVisible(false);
                return;
            }

            SetAllVisible(true);

            // Actualizar cada dedo
            UpdateFinger(Finger.Index, XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip, hand);
            UpdateFinger(Finger.Middle, XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip, hand);
            UpdateFinger(Finger.Ring, XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip, hand);
            UpdateFinger(Finger.Pinky, XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip, hand);

            if (includeThumb)
            {
                // El pulgar tiene joints distintos (sin metacarpal visible en overlays)
                UpdateFinger(Finger.Thumb, XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip, XRHandJointID.ThumbTip, hand);
            }
        }

        #region Public API

        /// <summary>
        /// Establece el estado visual de un dedo especifico.
        /// </summary>
        public void SetFingerStatus(Finger finger, FingerOverlayStatus status)
        {
            _status[finger] = status;
            ApplyFingerColor(finger);
        }

        /// <summary>
        /// Establece el estado visual de todos los dedos a la vez.
        /// </summary>
        public void SetAllStatuses(FingerOverlayStatus thumb, FingerOverlayStatus index, FingerOverlayStatus middle, FingerOverlayStatus ring, FingerOverlayStatus pinky)
        {
            _status[Finger.Thumb] = thumb;
            _status[Finger.Index] = index;
            _status[Finger.Middle] = middle;
            _status[Finger.Ring] = ring;
            _status[Finger.Pinky] = pinky;

            foreach (var f in _segments.Keys)
            {
                ApplyFingerColor(f);
            }
        }

        /// <summary>
        /// Convierte Severity a FingerOverlayStatus para integracion con el sistema de feedback existente.
        /// </summary>
        public static FingerOverlayStatus SeverityToOverlayStatus(Severity severity)
        {
            return severity switch
            {
                Severity.None => FingerOverlayStatus.Correct,  // Sin error = correct
                Severity.Minor => FingerOverlayStatus.Wrong,   // Error menor = corregir (rojo)
                Severity.Major => FingerOverlayStatus.Wrong,   // Error mayor = incorrect
                _ => FingerOverlayStatus.None
            };
        }

        /// <summary>
        /// Actualiza los overlays desde un StaticGestureResult.
        /// </summary>
        public void UpdateFromResult(StaticGestureResult result)
        {
            if (result == null)
            {
                ClearAllStatuses();
                return;
            }

            // Si todo esta correct globalmente, mostrar todos en verde
            if (result.isMatchGlobal)
            {
                SetAllStatuses(
                    FingerOverlayStatus.Correct,
                    FingerOverlayStatus.Correct,
                    FingerOverlayStatus.Correct,
                    FingerOverlayStatus.Correct,
                    FingerOverlayStatus.Correct
                );
                return;
            }

            // Procesar errores por dedo
            foreach (Finger finger in Enum.GetValues(typeof(Finger)))
            {
                Severity severity = result.GetSeverityForFinger(finger);
                FingerOverlayStatus status = SeverityToOverlayStatus(severity);
                SetFingerStatus(finger, status);
            }
        }

        /// <summary>
        /// Limpia todos los estados (pone todos en None/transparente).
        /// </summary>
        public void ClearAllStatuses()
        {
            foreach (Finger f in Enum.GetValues(typeof(Finger)))
            {
                _status[f] = FingerOverlayStatus.None;
            }

            foreach (var f in _segments.Keys)
            {
                ApplyFingerColor(f);
            }
        }

        /// <summary>
        /// Oculta todos los overlays.
        /// </summary>
        public void HideAll()
        {
            SetAllVisible(false);
        }

        /// <summary>
        /// Muestra todos los overlays.
        /// </summary>
        public void ShowAll()
        {
            SetAllVisible(true);
        }

        /// <summary>
        /// Activa o desactiva la visualizacion de overlays.
        /// Cuando esta desactivado, el Update no actualiza posiciones.
        /// </summary>
        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            SetAllVisible(visible);
        }

        /// <summary>
        /// True si los overlays estan visibles.
        /// </summary>
        public bool IsVisible => _isVisible;

        #endregion

        #region Private Methods

        private bool TryGetSubsystem()
        {
            var loaders = new List<UnityEngine.XR.Management.XRLoader>();
            UnityEngine.XR.Management.XRGeneralSettings.Instance?.Manager?.activeLoader?.GetType();

            _subsystem = UnityEngine.XR.Management.XRGeneralSettings.Instance?
                .Manager?
                .activeLoader?
                .GetLoadedSubsystem<XRHandSubsystem>();

            return _subsystem != null;
        }

        private void BuildSegmentsIfNeeded()
        {
            if (segmentPrefab == null)
            {
                Debug.LogWarning("[XRFingerOverlayRenderer] segmentPrefab not assigned. Asigna un prefab de Capsule/Cylinder.");
                return;
            }

            foreach (Finger f in Enum.GetValues(typeof(Finger)))
            {
                if (f == Finger.Thumb && !includeThumb) continue;
                if (_segments.ContainsKey(f)) continue;

                // 3 segmentos por dedo (excepto pulgar que tiene 2 utiles)
                int segmentCount = (f == Finger.Thumb) ? 2 : 3;
                var arr = new Transform[segmentCount];

                for (int i = 0; i < segmentCount; i++)
                {
                    var seg = Instantiate(segmentPrefab, transform);
                    seg.name = $"{handedness}_{f}_seg{i}";
                    arr[i] = seg;
                    SetSegmentScale(seg, 0.01f); // Size inicial
                }

                _segments[f] = arr;
                ApplyFingerColor(f);
            }
        }

        private void UpdateFinger(Finger finger, XRHandJointID a, XRHandJointID b, XRHandJointID c, XRHandJointID d, XRHand hand)
        {
            if (!_segments.TryGetValue(finger, out var segs)) return;

            // Obtener posiciones de joints
            if (!TryGetJointPos(hand, a, out var p0) ||
                !TryGetJointPos(hand, b, out var p1) ||
                !TryGetJointPos(hand, c, out var p2))
            {
                // Si falla algo, esconder ese dedo
                foreach (var s in segs)
                {
                    if (s != null) s.gameObject.SetActive(false);
                }
                return;
            }

            // Para dedos normales (3 segmentos)
            if (segs.Length == 3)
            {
                if (!TryGetJointPos(hand, d, out var p3))
                {
                    foreach (var s in segs)
                    {
                        if (s != null) s.gameObject.SetActive(false);
                    }
                    return;
                }

                segs[0].gameObject.SetActive(true);
                segs[1].gameObject.SetActive(true);
                segs[2].gameObject.SetActive(true);

                PlaceSegment(segs[0], p0, p1);
                PlaceSegment(segs[1], p1, p2);
                PlaceSegment(segs[2], p2, p3);
            }
            // Para el pulgar (2 segmentos)
            else if (segs.Length == 2)
            {
                segs[0].gameObject.SetActive(true);
                segs[1].gameObject.SetActive(true);

                PlaceSegment(segs[0], p0, p1);
                PlaceSegment(segs[1], p1, p2);
            }
        }

        private bool TryGetJointPos(XRHand hand, XRHandJointID id, out Vector3 pos)
        {
            var joint = hand.GetJoint(id);
            if (!joint.TryGetPose(out Pose pose))
            {
                pos = default;
                return false;
            }
            pos = pose.position;
            return true;
        }

        private void PlaceSegment(Transform seg, Vector3 a, Vector3 b)
        {
            Vector3 dir = b - a;
            float len = dir.magnitude;

            if (len < 1e-5f)
            {
                seg.gameObject.SetActive(false);
                return;
            }

            seg.position = (a + b) * 0.5f;
            seg.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            SetSegmentScale(seg, len);
        }

        private void SetSegmentScale(Transform seg, float length)
        {
            // Asume que el prefab esta alineado en Y y mide 1 unidad de alto.
            // Ajusta si tu capsule/cylinder tiene otras dimensiones.
            seg.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
        }

        private void ApplyFingerColor(Finger finger)
        {
            if (!_segments.TryGetValue(finger, out var segs)) return;

            Color c = _status[finger] switch
            {
                FingerOverlayStatus.Correct => correctColor,
                FingerOverlayStatus.Almost => almostColor,
                FingerOverlayStatus.Wrong => wrongColor,
                _ => noneColor
            };

            foreach (var s in segs)
            {
                if (s == null) continue;
                var r = s.GetComponentInChildren<Renderer>();
                if (r != null)
                {
                    SetRendererColor(r, c);
                }
            }
        }

        private void SetRendererColor(Renderer r, Color c)
        {
            _mpb ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(_mpb);

            // URP Lit/Unlit usan _BaseColor; Built-in Unlit/Color usa _Color.
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId))
            {
                _mpb.SetColor(BaseColorId, c);
            }
            else
            {
                _mpb.SetColor("_Color", c);
            }

            r.SetPropertyBlock(_mpb);
        }

        private void SetAllVisible(bool visible)
        {
            foreach (var kv in _segments)
            {
                foreach (var s in kv.Value)
                {
                    if (s != null)
                    {
                        s.gameObject.SetActive(visible);
                    }
                }
            }
        }

        #endregion
    }
}
