using UnityEngine;
using UnityEngine.UI;

namespace ASL_LearnVR.UI
{
    /// <summary>
    /// Helper component that applies the 'UI/Rounded' shader to an Image and exposes radius/feather in the inspector.
    /// Attach to the background Image of the tile (the GameObject that has the Image component you want rounded).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public class UIRounded : MonoBehaviour
    {
        [Range(0f, 0.5f)]
        public float radius = 0.08f;

        [Range(0f, 0.2f)]
        public float feather = 0.01f;

        private Image img;
        private Material runtimeMat;

        void OnEnable()
        {
            EnsureMaterial();
        }

        void OnDisable()
        {
            if (img != null && Application.isPlaying)
            {
                // restore default material if we created one at runtime
                img.material = null;
            }
        }

        void OnValidate()
        {
            EnsureMaterial();
            ApplyProperties();
        }

        void EnsureMaterial()
        {
            img = GetComponent<Image>();
            if (img == null) return;

            Shader shader = Shader.Find("UI/Rounded");
            if (shader == null)
            {
                Debug.LogError("UIRounded: Shader 'UI/Rounded' not found. Make sure it's in the project.");
                return;
            }

            if (img.material == null || img.material.shader.name != "UI/Rounded")
            {
                // create a new instance so we don't modify shared material
                runtimeMat = new Material(shader);
                // copy main texture if there is one
                if (img.sprite != null && img.sprite.texture != null)
                {
                    runtimeMat.SetTexture("_MainTex", img.sprite.texture);
                }
                img.material = runtimeMat;
            }

            ApplyProperties();
        }

        void ApplyProperties()
        {
            if (img == null) img = GetComponent<Image>();
            if (img == null || img.material == null) return;

            img.material.SetFloat("_Radius", radius);
            img.material.SetFloat("_Feather", feather);
        }
    }
}
