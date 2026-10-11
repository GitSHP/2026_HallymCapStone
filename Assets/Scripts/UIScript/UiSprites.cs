using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.UI
{
    /// <summary>
    /// 코드로 그리는 임시 아이콘 그림이다. 인터페이스는 문장이 아니라 모양으로 읽히도록
    /// 설계되어 있어서, 아트 담당이 없어도 아이콘이 먼저 필요하다. 그래서 여기의 모든 아이콘은
    /// 로드 시 래스터화한 실루엣이며, 색은 그것을 그리는 Image가 입힌다. 데이터 정의에 실제
    /// 스프라이트를 넣으면 이 파일을 고치지 않고도 해당 아이콘이 대체된다.
    /// </summary>
    // [아이템 담당] 아이템에 그림이 없을 때 대신 쓰는 임시 아이콘 생성기.
    // ItemData.itemImage 에 실제 스프라이트를 넣으면 그쪽이 우선한다.
    public static class UiSprites
    {
        const int Size = 128;
        const float PixelsPerUnit = 100f;

        static readonly Dictionary<IconId, Sprite> Cache = new();

        public static Sprite Get(IconId id)
        {
            if (id == IconId.None)
                return null;

            if (Cache.TryGetValue(id, out var cached) && cached != null)
                return cached;

            // 저장된 아이콘 에셋이 우선한다. 그래야 프리팹과 코드로 만든 뷰가 같은 이미지를 쓴다.
            var saved = UiArt.Icon(id);
            if (saved != null)
            {
                Cache[id] = saved;
                return saved;
            }

            var sprite = Build(id);
            Cache[id] = sprite;
            return sprite;
        }

        /// <summary>기물은 정의에 이미 들어 있는 글리프로 아이콘을 찾는다.</summary>
        public static IconId ForGlyph(string glyph)
        {
            if (string.IsNullOrEmpty(glyph))
                return IconId.Pawn;

            switch (char.ToUpperInvariant(glyph[0]))
            {
                case 'P': return IconId.Pawn;
                case 'R': return IconId.Rook;
                case 'B': return IconId.Bishop;
                case 'N': return IconId.Knight;
                case 'Q': return IconId.Queen;
                case 'K': return IconId.King;
                default: return IconId.Star;
            }
        }

        // ---------- 도형 정의 ----------

        static Sprite Build(IconId id)
        {
            var buffer = new float[Size * Size];

            switch (id)
            {
                case IconId.Pawn:
                    Base(buffer);
                    Poly(buffer, V(0.36f, 0.22f), V(0.64f, 0.22f), V(0.59f, 0.52f), V(0.41f, 0.52f));
                    Circle(buffer, 0.50f, 0.66f, 0.15f);
                    break;

                case IconId.Rook:
                    Base(buffer);
                    Poly(buffer, V(0.33f, 0.22f), V(0.67f, 0.22f), V(0.63f, 0.60f), V(0.37f, 0.60f));
                    Rect(buffer, 0.28f, 0.60f, 0.41f, 0.84f);
                    Rect(buffer, 0.435f, 0.60f, 0.565f, 0.84f);
                    Rect(buffer, 0.59f, 0.60f, 0.72f, 0.84f);
                    Rect(buffer, 0.28f, 0.60f, 0.72f, 0.70f);
                    break;

                case IconId.Bishop:
                    Base(buffer);
                    Poly(buffer, V(0.37f, 0.22f), V(0.63f, 0.22f), V(0.58f, 0.50f), V(0.42f, 0.50f));
                    Circle(buffer, 0.50f, 0.62f, 0.14f);
                    Circle(buffer, 0.50f, 0.79f, 0.055f);
                    // 주교관의 홈이 있어야 비숍과 폰이 한눈에 구별된다.
                    Poly(buffer, true, V(0.52f, 0.60f), V(0.62f, 0.74f), V(0.57f, 0.77f), V(0.47f, 0.63f));
                    break;

                case IconId.Knight:
                    Base(buffer);
                    Poly(buffer,
                        V(0.32f, 0.22f), V(0.70f, 0.22f), V(0.70f, 0.40f), V(0.60f, 0.50f),
                        V(0.72f, 0.64f), V(0.62f, 0.84f), V(0.44f, 0.88f), V(0.33f, 0.74f),
                        V(0.44f, 0.60f), V(0.33f, 0.50f));
                    Circle(buffer, true, 0.57f, 0.72f, 0.035f);
                    break;

                case IconId.Queen:
                    Base(buffer);
                    Poly(buffer, V(0.35f, 0.22f), V(0.65f, 0.22f), V(0.60f, 0.56f), V(0.40f, 0.56f));
                    Poly(buffer,
                        V(0.32f, 0.56f), V(0.68f, 0.56f), V(0.72f, 0.82f), V(0.60f, 0.66f),
                        V(0.50f, 0.86f), V(0.40f, 0.66f), V(0.28f, 0.82f));
                    Circle(buffer, 0.28f, 0.84f, 0.05f);
                    Circle(buffer, 0.50f, 0.88f, 0.05f);
                    Circle(buffer, 0.72f, 0.84f, 0.05f);
                    break;

                case IconId.King:
                    Base(buffer);
                    Poly(buffer, V(0.35f, 0.22f), V(0.65f, 0.22f), V(0.60f, 0.56f), V(0.40f, 0.56f));
                    Rect(buffer, 0.34f, 0.56f, 0.66f, 0.70f);
                    Rect(buffer, 0.455f, 0.70f, 0.545f, 0.94f);
                    Rect(buffer, 0.38f, 0.78f, 0.62f, 0.855f);
                    break;

                case IconId.Heart:
                    Circle(buffer, 0.335f, 0.66f, 0.19f);
                    Circle(buffer, 0.665f, 0.66f, 0.19f);
                    Poly(buffer, V(0.14f, 0.64f), V(0.86f, 0.64f), V(0.50f, 0.14f));
                    break;

                case IconId.Sword:
                    Poly(buffer, V(0.43f, 0.38f), V(0.57f, 0.38f), V(0.57f, 0.84f), V(0.50f, 0.95f), V(0.43f, 0.84f));
                    Rect(buffer, 0.26f, 0.30f, 0.74f, 0.39f);
                    Rect(buffer, 0.455f, 0.08f, 0.545f, 0.31f);
                    Circle(buffer, 0.50f, 0.08f, 0.065f);
                    break;

                case IconId.Bolt:
                    Poly(buffer,
                        V(0.58f, 0.95f), V(0.28f, 0.47f), V(0.46f, 0.47f),
                        V(0.40f, 0.06f), V(0.72f, 0.56f), V(0.54f, 0.56f));
                    break;

                case IconId.Coin:
                    Circle(buffer, 0.50f, 0.50f, 0.40f);
                    Ring(buffer, true, 0.50f, 0.50f, 0.325f, 0.295f);
                    break;

                case IconId.Shield:
                    Poly(buffer,
                        V(0.50f, 0.94f), V(0.87f, 0.78f), V(0.87f, 0.46f), V(0.74f, 0.20f),
                        V(0.50f, 0.06f), V(0.26f, 0.20f), V(0.13f, 0.46f), V(0.13f, 0.78f));
                    break;

                case IconId.Star:
                    Star(buffer, 0.50f, 0.50f, 0.46f, 0.20f);
                    break;

                case IconId.Crown:
                    Poly(buffer,
                        V(0.14f, 0.30f), V(0.86f, 0.30f), V(0.90f, 0.80f), V(0.70f, 0.56f),
                        V(0.50f, 0.86f), V(0.30f, 0.56f), V(0.10f, 0.80f));
                    Rect(buffer, 0.16f, 0.16f, 0.84f, 0.28f);
                    break;

                case IconId.Chevrons:
                    Chevron(buffer, 0.30f);
                    Chevron(buffer, 0.58f);
                    break;

                case IconId.Arrow:
                    Poly(buffer,
                        V(0.10f, 0.39f), V(0.52f, 0.39f), V(0.52f, 0.18f), V(0.92f, 0.50f),
                        V(0.52f, 0.82f), V(0.52f, 0.61f), V(0.10f, 0.61f));
                    break;

                case IconId.Bag:
                    Poly(buffer, V(0.18f, 0.10f), V(0.82f, 0.10f), V(0.74f, 0.62f), V(0.26f, 0.62f));
                    Ring(buffer, 0.50f, 0.62f, 0.24f, 0.17f);
                    Rect(buffer, true, 0.20f, 0.30f, 0.80f, 0.62f);
                    Poly(buffer, V(0.18f, 0.10f), V(0.82f, 0.10f), V(0.78f, 0.40f), V(0.22f, 0.40f));
                    break;

                case IconId.Check:
                    Seg(buffer, 0.16f, 0.54f, 0.42f, 0.26f, 0.075f);
                    Seg(buffer, 0.40f, 0.26f, 0.84f, 0.80f, 0.075f);
                    break;

                default:
                    Circle(buffer, 0.5f, 0.5f, 0.4f);
                    break;
            }

            return ToSprite(buffer, id.ToString());
        }

        /// <summary>모든 체스 기물이 올라서는 받침대다.</summary>
        static void Base(float[] buffer)
        {
            Poly(buffer, V(0.24f, 0.08f), V(0.76f, 0.08f), V(0.70f, 0.23f), V(0.30f, 0.23f));
        }

        static void Seg(float[] buffer, float x0, float y0, float x1, float y1, float halfWidth)
        {
            var dir = new Vector2(x1 - x0, y1 - y0).normalized;
            var normal = new Vector2(-dir.y, dir.x) * halfWidth;
            // 양 끝을 조금 늘려서, 이어지는 획이 홈 없이 맞물리게 한다.
            var pad = dir * halfWidth * 0.6f;

            Poly(buffer,
                new Vector2(x0, y0) - pad + normal,
                new Vector2(x1, y1) + pad + normal,
                new Vector2(x1, y1) + pad - normal,
                new Vector2(x0, y0) - pad - normal);
        }

        static void Chevron(float[] buffer, float y)
        {
            Poly(buffer,
                V(0.16f, y), V(0.50f, y + 0.22f), V(0.84f, y),
                V(0.84f, y + 0.12f), V(0.50f, y + 0.34f), V(0.16f, y + 0.12f));
        }

        static void Star(float[] buffer, float cx, float cy, float outer, float inner)
        {
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float radius = (i % 2 == 0) ? outer : inner;
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                points[i] = new Vector2(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius);
            }
            Poly(buffer, false, points);
        }

        // ---------- 래스터화 ----------
        //
        // 모든 도형은 0..1 좌표계의 커버리지 버퍼에 2x2 슈퍼샘플링으로 그린 뒤,
        // 그 커버리지를 알파로 갖는 흰색 스프라이트로 출력한다. 색은 그것을 그리는
        // Image에 맡기므로, 아이콘 하나로 색상표의 모든 강조색에 대응한다.

        const int Samples = 2;

        static Vector2 V(float x, float y) => new Vector2(x, y);

        static void Circle(float[] buffer, float cx, float cy, float r) => Circle(buffer, false, cx, cy, r);

        static void Circle(float[] buffer, bool erase, float cx, float cy, float r)
        {
            float r2 = r * r;
            Paint(buffer, erase, p =>
            {
                float dx = p.x - cx, dy = p.y - cy;
                return dx * dx + dy * dy <= r2;
            });
        }

        static void Ring(float[] buffer, float cx, float cy, float outer, float inner)
            => Ring(buffer, false, cx, cy, outer, inner);

        static void Ring(float[] buffer, bool erase, float cx, float cy, float outer, float inner)
        {
            float o2 = outer * outer, i2 = inner * inner;
            Paint(buffer, erase, p =>
            {
                float dx = p.x - cx, dy = p.y - cy;
                float d2 = dx * dx + dy * dy;
                return d2 <= o2 && d2 >= i2;
            });
        }

        static void Rect(float[] buffer, float x0, float y0, float x1, float y1)
            => Rect(buffer, false, x0, y0, x1, y1);

        static void Rect(float[] buffer, bool erase, float x0, float y0, float x1, float y1)
            => Paint(buffer, erase, p => p.x >= x0 && p.x <= x1 && p.y >= y0 && p.y <= y1);

        static void Poly(float[] buffer, params Vector2[] points) => Poly(buffer, false, points);

        static void Poly(float[] buffer, bool erase, params Vector2[] points)
            => Paint(buffer, erase, p => Inside(points, p));

        static bool Inside(Vector2[] points, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                if (points[i].y > p.y == points[j].y > p.y)
                    continue;

                float t = (p.y - points[i].y) / (points[j].y - points[i].y);
                if (p.x < points[i].x + t * (points[j].x - points[i].x))
                    inside = !inside;
            }
            return inside;
        }

        static void Paint(float[] buffer, bool erase, System.Func<Vector2, bool> inside)
        {
            const float step = 1f / (Size * Samples);

            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < Samples; sy++)
                for (int sx = 0; sx < Samples; sx++)
                {
                    var p = new Vector2(
                        (x + (sx + 0.5f) / Samples) / Size,
                        (y + (sy + 0.5f) / Samples) / Size);

                    if (inside(p))
                        hits++;
                }

                if (hits == 0)
                    continue;

                float coverage = hits / (float)(Samples * Samples);
                int index = y * Size + x;
                buffer[index] = erase
                    ? Mathf.Min(buffer[index], 1f - coverage)
                    : Mathf.Max(buffer[index], coverage);
            }

            _ = step;
        }

        static Sprite ToSprite(float[] buffer, string name)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(1f, 1f, 1f, buffer[i]);

            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.name = "Icon_" + name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
