using UnityEngine;

namespace DesertScene
{
    public sealed class DesertScalarField
    {
        private readonly Color[] pixels;
        private readonly int width;
        private readonly int height;
        private readonly Vector2 terrainSize;
        private readonly float heightMultiplier;
        private readonly bool makeEdgesTileable;

        public DesertScalarField(Texture2D sourceTexture, Vector2 terrainSize, float heightMultiplier, bool makeEdgesTileable)
        {
            this.terrainSize = new Vector2(Mathf.Max(0.1f, terrainSize.x), Mathf.Max(0.1f, terrainSize.y));
            this.heightMultiplier = Mathf.Max(0.1f, heightMultiplier);
            this.makeEdgesTileable = makeEdgesTileable;

            width = sourceTexture.width;
            height = sourceTexture.height;
            pixels = ReadPixels(sourceTexture);
        }

        public float SampleDensity(Vector3 localPosition)
        {
            float imageHeight = SampleHeight(localPosition.x, localPosition.z);
            return imageHeight - localPosition.y;
        }

        public float SampleHeight(float x, float z)
        {
            float u = Mathf.InverseLerp(-terrainSize.x * 0.5f, terrainSize.x * 0.5f, x);
            float v = Mathf.InverseLerp(-terrainSize.y * 0.5f, terrainSize.y * 0.5f, z);
            float brightness = SampleBrightness(u, v);

            return brightness * heightMultiplier;
        }

        private float SampleBrightness(float u, float v)
        {
            if (pixels == null || pixels.Length == 0)
                return 0f;

            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);

            if (makeEdgesTileable)
                return SampleTileableBrightness(u, v);

            return SampleRawBrightness(u, v);
        }

        private float SampleTileableBrightness(float u, float v)
        {
            float xBlend = Mathf.SmoothStep(0f, 1f, u);
            float yBlend = Mathf.SmoothStep(0f, 1f, v);

            float bottom = Mathf.Lerp(SampleRawBrightness(u, v), SampleRawBrightness(1f - u, v), xBlend);
            float top = Mathf.Lerp(SampleRawBrightness(u, 1f - v), SampleRawBrightness(1f - u, 1f - v), xBlend);

            return Mathf.Lerp(bottom, top, yBlend);
        }

        private float SampleRawBrightness(float u, float v)
        {
            float x = Mathf.Clamp01(u) * (width - 1);
            float y = Mathf.Clamp01(v) * (height - 1);

            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int y1 = Mathf.Min(y0 + 1, height - 1);

            float tx = x - x0;
            float ty = y - y0;

            Color bottom = Color.Lerp(GetPixel(x0, y0), GetPixel(x1, y0), tx);
            Color top = Color.Lerp(GetPixel(x0, y1), GetPixel(x1, y1), tx);
            return Color.Lerp(bottom, top, ty).grayscale;
        }

        private Color GetPixel(int x, int y)
        {
            return pixels[y * width + x];
        }

        private static Color[] ReadPixels(Texture2D sourceTexture)
        {
            try
            {
                return sourceTexture.GetPixels();
            }
            catch (UnityException)
            {
                Texture2D readableCopy = CopyUnreadableTexture(sourceTexture);
                Color[] copiedPixels = readableCopy.GetPixels();
                Object.DestroyImmediate(readableCopy);
                return copiedPixels;
            }
        }

        private static Texture2D CopyUnreadableTexture(Texture2D sourceTexture)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(
                sourceTexture.width,
                sourceTexture.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(sourceTexture, temporary);
            RenderTexture.active = temporary;

            Texture2D copy = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false, true);
            copy.ReadPixels(new Rect(0, 0, sourceTexture.width, sourceTexture.height), 0, 0);
            copy.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);

            return copy;
        }
    }
}
