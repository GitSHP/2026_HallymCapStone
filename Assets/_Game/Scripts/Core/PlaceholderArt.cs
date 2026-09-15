using UnityEngine;

namespace Gambonanza.Core
{
    /// <summary>
    /// Generates the temporary sprites used before real art arrives.
    /// Every view checks its serialized sprite field first, so dropping a real
    /// sprite into the inspector replaces the placeholder with no code change.
    /// </summary>
    public static class PlaceholderArt
    {
        static Sprite _square;
        static Sprite _circle;
        static Material _unlit;

        const int TextureSize = 64;
        const float PixelsPerUnit = 64f;

        public static Sprite Square
        {
            get
            {
                if (_square == null)
                    _square = BuildSquare();
                return _square;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                    _circle = BuildCircle();
                return _circle;
            }
        }

        /// <summary>
        /// Sprites are drawn unlit: the board is a flat 2D playfield with no
        /// lighting design, and the lit shader renders black whenever the URP 2D
        /// renderer has no registered Light2D — a failure mode with no error.
        /// Assign a lit material explicitly if lighting is wanted later.
        /// </summary>
        public static Material UnlitMaterial
        {
            get
            {
                if (_unlit == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                                 ?? Shader.Find("Sprites/Default");
                    _unlit = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
                return _unlit;
            }
        }

        static Sprite BuildSquare()
        {
            var tex = NewTexture();
            var pixels = new Color32[TextureSize * TextureSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels32(pixels);
            tex.Apply();
            return MakeSprite(tex, "PlaceholderSquare");
        }

        static Sprite BuildCircle()
        {
            var tex = NewTexture();
            var pixels = new Color32[TextureSize * TextureSize];
            const float center = (TextureSize - 1) * 0.5f;
            float radius = TextureSize * 0.5f - 1f;

            for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                // One-pixel smoothing on the edge keeps the circle from looking jagged.
                float alpha = Mathf.Clamp01(radius - dist);
                pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return MakeSprite(tex, "PlaceholderCircle");
        }

        static Texture2D NewTexture()
        {
            return new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        static Sprite MakeSprite(Texture2D tex, string name)
        {
            var sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
