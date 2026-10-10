using UnityEngine;

namespace Promotion.Core
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
        static Sprite _roundedRect;
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
        /// A 9-sliced rounded rectangle: the one shape the whole UI is built from.
        /// Slicing means panels, cards and buttons of any size share a single
        /// corner radius, which is what makes a minimal interface read as one set.
        /// </summary>
        public static Sprite RoundedRect
        {
            get
            {
                if (_roundedRect == null)
                    _roundedRect = BuildRoundedRect();
                return _roundedRect;
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

        static Sprite BuildRoundedRect()
        {
            const int radius = RoundedCornerRadius;
            var tex = NewTexture();
            var pixels = new Color32[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
            {
                // Distance to the nearest corner circle centre; straight edges stay solid.
                float cx = Mathf.Clamp(x, radius - 0.5f, TextureSize - radius - 0.5f);
                float cy = Mathf.Clamp(y, radius - 0.5f, TextureSize - radius - 0.5f);
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float alpha = Mathf.Clamp01(radius - dist);
                pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            sprite.name = "PlaceholderRoundedRect";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        const int RoundedCornerRadius = 20;

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
