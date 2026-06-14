using UnityEngine;

namespace DesertScene
{
    public sealed class DesertFootprintMark : MonoBehaviour
    {
        public float lifeTime = 28f;
        public float fadeDelay = 1f;
        public Color color = new Color(0.12f, 0.065f, 0.025f, 0.62f);

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private float age;

        public void Initialize(float newLifeTime, Color newColor)
        {
            lifeTime = newLifeTime;
            color = newColor;
            CacheRenderer();
            ApplyFade(1f);
        }

        private void Awake()
        {
            CacheRenderer();
            ApplyFade(1f);
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            age += Time.deltaTime;
            float fade = 1f - Mathf.InverseLerp(fadeDelay, lifeTime, age);
            ApplyFade(Mathf.Clamp01(fade));

            if (age >= lifeTime)
                Destroy(gameObject);
        }

        private void ApplyFade(float fade)
        {
            if (meshRenderer == null)
                return;

            Color fadedColor = color;
            fadedColor.a *= fade;
            propertyBlock.SetColor(ColorId, fadedColor);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private void CacheRenderer()
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
        }
    }
}
