using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using ASL_LearnVR.Core;

namespace ASL_LearnVR.UI
{
    /// <summary>
    /// Botón EXIT físico que vive sobre la mesa.
    ///
    /// Estructura esperada del prefab:
    ///   ExitButton_Physical  (este script + XRSimpleInteractable)
    ///   ├── ButtonBase        (Cylinder, escala ~0.08 x 0.01 x 0.08) — base gris oscura
    ///   ├── ButtonCap         (Cylinder, escala ~0.06 x 0.018 x 0.06) — tapa roja que se presiona
    ///   └── ButtonLabel       (Canvas WorldSpace + TextMeshPro "EXIT")
    ///
    /// Setup en Unity:
    ///   1. Añade XRSimpleInteractable al GameObject raíz.
    ///   2. Arrastra ButtonCap a capTransform.
    ///   3. El botón llama a SceneLoader.QuitApplication() o Application.Quit().
    ///
    /// El script gestiona:
    ///   - Animación de presionado (la tapa baja y sube)
    ///   - Cambio de color al hacer hover (requiere material con _BaseColor o _Color)
    ///   - Cooldown para evitar doble-press
    ///   - Compatibilidad con XR Direct Interactor (toque físico de mano)
    /// </summary>
    [AddComponentMenu("ASL_LearnVR/UI/Physical Exit Button")]
    public class PhysicalExitButton : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────
        [Header("3D References")]
        [Tooltip("La tapa del botón que se anima hacia abajo al presionar")]
        [SerializeField] private Transform capTransform;

        [Tooltip("Renderer de la tapa (para cambio de color en hover)")]
        [SerializeField] private Renderer capRenderer;

        [Header("Animation")]
        [Tooltip("Cuánto baja la tapa al presionar (metros)")]
        [SerializeField] private float pressDepth = 0.006f;

        [Tooltip("Duración de la animación de presionado")]
        [SerializeField] private float pressDuration = 0.08f;

        [Tooltip("Duración de la animación de subida")]
        [SerializeField] private float releaseDuration = 0.15f;

        [Header("Colors")]
        [SerializeField] private Color idleColor    = new Color(0.85f, 0.18f, 0.18f); // rojo
        [SerializeField] private Color hoverColor   = new Color(1.00f, 0.28f, 0.28f); // rojo más vivo
        [SerializeField] private Color pressedColor = new Color(0.55f, 0.10f, 0.10f); // rojo oscuro

        [Header("Cooldown")]
        [SerializeField] private float pressCooldown = 1.5f; // segundos antes de poder volver a presionar

        [Header("Confirm Dialog (opcional)")]
        [Tooltip("Si true, muestra un segundo botón de confirmación antes de salir")]
        [SerializeField] private bool requireConfirmation = false;
        [SerializeField] private GameObject confirmPanel;   // panel con Confirm/Cancel

        // ─── Runtime ─────────────────────────────────────────────────────
        private Vector3         _capBaseLocalPos;
        private bool            _isPressed    = false;
        private bool            _onCooldown   = false;
        private Material        _capMat;
        private Coroutine       _animCoroutine;
        private bool            _awaitingConfirm = false;

        // Shader property IDs
        private static readonly int _BaseColorID = Shader.PropertyToID("_BaseColor"); // URP
        private static readonly int _ColorID     = Shader.PropertyToID("_Color");     // Built-in

        // ─────────────────────────────────────────────────────────────────
        void Awake()
        {
            if (capTransform != null)
                _capBaseLocalPos = capTransform.localPosition;

            if (capRenderer != null)
            {
                _capMat = capRenderer.material; // instancia propia
                SetCapColor(idleColor);
            }

            // Suscribirse a XRSimpleInteractable
            var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            if (interactable != null)
            {
                interactable.hoverEntered.AddListener(OnHoverEnter);
                interactable.hoverExited.AddListener(OnHoverExit);
                interactable.selectEntered.AddListener(OnSelectEnter);
                interactable.selectExited.AddListener(OnSelectExit);
            }
            else
            {
                Debug.LogWarning("[PhysicalExitButton] XRSimpleInteractable no encontrado. " +
                                 "Añade el componente al mismo GameObject.");
            }

            if (confirmPanel != null)
                confirmPanel.SetActive(false);
        }

        void OnDestroy()
        {
            var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            if (interactable != null)
            {
                interactable.hoverEntered.RemoveListener(OnHoverEnter);
                interactable.hoverExited.RemoveListener(OnHoverExit);
                interactable.selectEntered.RemoveListener(OnSelectEnter);
                interactable.selectExited.RemoveListener(OnSelectExit);
            }
        }

        // ─── XR Events ───────────────────────────────────────────────────
        private void OnHoverEnter(HoverEnterEventArgs args)
        {
            if (!_isPressed) SetCapColor(hoverColor);
        }

        private void OnHoverExit(HoverExitEventArgs args)
        {
            if (!_isPressed) SetCapColor(idleColor);
        }

        private void OnSelectEnter(SelectEnterEventArgs args)
        {
            if (_onCooldown) return;
            Press();
        }

        private void OnSelectExit(SelectExitEventArgs args)
        {
            // La animación de subida se gestiona desde Press() automáticamente
        }

        // ─── Lógica de presionado ─────────────────────────────────────────
        private void Press()
        {
            if (_isPressed || _onCooldown) return;

            _isPressed = true;
            SetCapColor(pressedColor);

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(PressAnimation());
        }

        private IEnumerator PressAnimation()
        {
            // ── Bajar tapa ──
            Vector3 pressedPos = _capBaseLocalPos - new Vector3(0, pressDepth, 0);
            yield return LerpCapTo(pressedPos, pressDuration);

            // ── Acción ──
            ExecuteAction();

            // ── Subir tapa ──
            yield return new WaitForSeconds(0.1f);
            yield return LerpCapTo(_capBaseLocalPos, releaseDuration);

            _isPressed = false;
            SetCapColor(idleColor);

            // Cooldown
            _onCooldown = true;
            yield return new WaitForSeconds(pressCooldown);
            _onCooldown = false;
        }

        private IEnumerator LerpCapTo(Vector3 target, float duration)
        {
            if (capTransform == null) yield break;
            Vector3 start = capTransform.localPosition;
            float   t     = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                capTransform.localPosition = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            capTransform.localPosition = target;
        }

        private void ExecuteAction()
        {
            if (requireConfirmation && !_awaitingConfirm)
            {
                // Mostrar panel de confirmación
                _awaitingConfirm = true;
                if (confirmPanel != null) confirmPanel.SetActive(true);
                return;
            }

            // Salir directamente
            _awaitingConfirm = false;
            if (confirmPanel != null) confirmPanel.SetActive(false);

            if (SceneLoader.Instance != null)
                SceneLoader.Instance.QuitApplication();
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        // ─── Llamado desde botón Confirm del panel de confirmación ────────
        public void ConfirmExit()   => ExecuteAction();
        public void CancelExit()
        {
            _awaitingConfirm = false;
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }

        // ─── Color helper ─────────────────────────────────────────────────
        private void SetCapColor(Color c)
        {
            if (_capMat == null) return;
            // Intenta URP primero, luego Built-in
            if (_capMat.HasProperty(_BaseColorID))
                _capMat.SetColor(_BaseColorID, c);
            else if (_capMat.HasProperty(_ColorID))
                _capMat.SetColor(_ColorID, c);
        }

        // ─── Gizmos de debug ──────────────────────────────────────────────
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.05f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.1f, "EXIT BUTTON");
        }
#endif
    }
}
