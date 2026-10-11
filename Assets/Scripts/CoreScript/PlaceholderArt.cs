using UnityEngine;

namespace Promotion.Core
{
    /// <summary>
    /// 실제 아트가 들어오기 전까지 쓰는 임시 스프라이트를 생성한다.
    /// 모든 뷰는 직렬화된 스프라이트 필드를 먼저 확인하므로, 인스펙터에 실제
    /// 스프라이트를 넣기만 하면 코드 수정 없이 임시 스프라이트가 대체된다.
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
        /// 9-슬라이스 둥근 사각형이다. UI 전체가 이 한 가지 모양으로 만들어진다.
        /// 슬라이스 덕분에 크기가 제각각인 패널·카드·버튼이 같은 모서리 반경을 공유하고,
        /// 그래서 미니멀한 인터페이스가 하나의 세트로 읽힌다.
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
        /// 스프라이트는 unlit으로 그린다. 보드는 조명 설계가 없는 평면 2D 플레이필드이고,
        /// lit 셰이더는 URP 2D 렌더러에 등록된 Light2D가 없으면 검게 렌더링된다 —
        /// 에러도 없이 실패하는 경우다. 나중에 조명이 필요하면 lit 머티리얼을 직접 지정한다.
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
                // 가장자리를 1픽셀 부드럽게 처리해 원이 들쭉날쭉해 보이지 않게 한다.
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
                // 가장 가까운 모서리 원 중심까지의 거리다. 직선 변은 꽉 찬 상태로 유지된다.
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
