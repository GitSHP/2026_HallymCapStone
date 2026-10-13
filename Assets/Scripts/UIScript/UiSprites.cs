using System.Collections.Generic;
using Promotion.Data;
using UnityEngine;

namespace Promotion.UI
{
    public static class UiSprites  // 아이콘 스프라이트를 꺼내 주는 클래스
    // -> UiArt 에셋에 아이콘 이미지가 등록되어 있으면 그것을 쓰고, 없으면 코드로 직접 아이콘을 그려서 대신 사용함
    {
        const int Size = 128;  // 코드로 그리는 아이콘 텍스처의 가로·세로 픽셀 수
        const float PixelsPerUnit = 100f;  // 스프라이트 생성 시 1유닛당 픽셀 수

        static readonly Dictionary<IconId, Sprite> Cache = new();  // 한 번 만든(찾은) 아이콘을 저장해 두는 사전 -> 같은 아이콘을 매번 다시 그리지 않도록 함

        public static Sprite Get(IconId id)  // 아이콘 종류에 맞는 스프라이트를 돌려주는 함수
        {
            if (id == IconId.None)  // 만약 아이콘 없음이라면 -> null 반환
                return null;

            if (Cache.TryGetValue(id, out var cached) && cached != null)  // 만약 이미 저장해 둔 아이콘이 있다면 -> 그대로 사용
                return cached;

            var saved = UiArt.Icon(id);  // UiArt 에셋에 등록된 아이콘 이미지를 먼저 찾아보고
            if (saved != null)  // 만약 등록된 이미지가 있다면
            {
                Cache[id] = saved;  // 저장해 두고 돌려줌
                return saved;
            }

            var sprite = Build(id);  // 등록된 이미지가 없다면 코드로 아이콘을 직접 그림
            Cache[id] = sprite;
            return sprite;
        }

        public static IconId ForGlyph(string glyph)  // 기물 글자(P, R, B ...)를 보고 어떤 기물 아이콘을 쓸지 정하는 함수
        {
            if (string.IsNullOrEmpty(glyph))  // 만약 글자가 비어 있다면 -> 폰 아이콘 사용
                return IconId.Pawn;

            switch (char.ToUpperInvariant(glyph[0]))  // 첫 글자를 대문자로 바꿔서 비교
            {
                case 'P': return IconId.Pawn;  // 폰
                case 'R': return IconId.Rook;  // 룩
                case 'B': return IconId.Bishop;  // 비숍
                case 'N': return IconId.Knight;  // 나이트
                case 'Q': return IconId.Queen;  // 퀸
                case 'K': return IconId.King;  // 킹
                default: return IconId.Star;  // 알 수 없는 기물은 별 아이콘으로 표시
            }
        }

        static Sprite Build(IconId id)  // 아이콘 모양을 코드로 그려서 스프라이트로 만드는 함수
        // -> 0~1 범위 좌표로 원·사각형·다각형을 겹쳐 그려 모양을 만들고, 마지막에 텍스처로 변환함
        {
            var buffer = new float[Size * Size];  // 픽셀마다 얼마나 칠해졌는지(0~1)를 저장하는 배열 -> 최종 투명도가 됨

            switch (id)  // 아이콘 종류에 따라 다른 도형 조합으로 그림 (좌표는 왼쪽 아래가 0,0 / 오른쪽 위가 1,1)
            {
                case IconId.Pawn:  // 폰: 받침 + 몸통 + 둥근 머리
                    Base(buffer);
                    Poly(buffer, V(0.36f, 0.22f), V(0.64f, 0.22f), V(0.59f, 0.52f), V(0.41f, 0.52f));
                    Circle(buffer, 0.50f, 0.66f, 0.15f);
                    break;

                case IconId.Rook:  // 룩: 받침 + 몸통 + 위쪽 성벽 모양 톱니 3개
                    Base(buffer);
                    Poly(buffer, V(0.33f, 0.22f), V(0.67f, 0.22f), V(0.63f, 0.60f), V(0.37f, 0.60f));
                    Rect(buffer, 0.28f, 0.60f, 0.41f, 0.84f);
                    Rect(buffer, 0.435f, 0.60f, 0.565f, 0.84f);
                    Rect(buffer, 0.59f, 0.60f, 0.72f, 0.84f);
                    Rect(buffer, 0.28f, 0.60f, 0.72f, 0.70f);
                    break;

                case IconId.Bishop:  // 비숍: 받침 + 몸통 + 머리 + 꼭지 + 머리에 파인 홈(지우기)
                    Base(buffer);
                    Poly(buffer, V(0.37f, 0.22f), V(0.63f, 0.22f), V(0.58f, 0.50f), V(0.42f, 0.50f));
                    Circle(buffer, 0.50f, 0.62f, 0.14f);
                    Circle(buffer, 0.50f, 0.79f, 0.055f);
                    Poly(buffer, true, V(0.52f, 0.60f), V(0.62f, 0.74f), V(0.57f, 0.77f), V(0.47f, 0.63f));
                    break;

                case IconId.Knight:  // 나이트: 받침 + 말 머리 옆모습 다각형 + 눈(지우기)
                    Base(buffer);
                    Poly(buffer,
                        V(0.32f, 0.22f), V(0.70f, 0.22f), V(0.70f, 0.40f), V(0.60f, 0.50f),
                        V(0.72f, 0.64f), V(0.62f, 0.84f), V(0.44f, 0.88f), V(0.33f, 0.74f),
                        V(0.44f, 0.60f), V(0.33f, 0.50f));
                    Circle(buffer, true, 0.57f, 0.72f, 0.035f);
                    break;

                case IconId.Queen:  // 퀸: 받침 + 몸통 + 뾰족한 왕관 + 끝의 구슬 3개
                    Base(buffer);
                    Poly(buffer, V(0.35f, 0.22f), V(0.65f, 0.22f), V(0.60f, 0.56f), V(0.40f, 0.56f));
                    Poly(buffer,
                        V(0.32f, 0.56f), V(0.68f, 0.56f), V(0.72f, 0.82f), V(0.60f, 0.66f),
                        V(0.50f, 0.86f), V(0.40f, 0.66f), V(0.28f, 0.82f));
                    Circle(buffer, 0.28f, 0.84f, 0.05f);
                    Circle(buffer, 0.50f, 0.88f, 0.05f);
                    Circle(buffer, 0.72f, 0.84f, 0.05f);
                    break;

                case IconId.King:  // 킹: 받침 + 몸통 + 머리 + 십자가
                    Base(buffer);
                    Poly(buffer, V(0.35f, 0.22f), V(0.65f, 0.22f), V(0.60f, 0.56f), V(0.40f, 0.56f));
                    Rect(buffer, 0.34f, 0.56f, 0.66f, 0.70f);
                    Rect(buffer, 0.455f, 0.70f, 0.545f, 0.94f);
                    Rect(buffer, 0.38f, 0.78f, 0.62f, 0.855f);
                    break;

                case IconId.Heart:  // 하트: 원 두 개 + 아래로 뾰족한 삼각형
                    Circle(buffer, 0.335f, 0.66f, 0.19f);
                    Circle(buffer, 0.665f, 0.66f, 0.19f);
                    Poly(buffer, V(0.14f, 0.64f), V(0.86f, 0.64f), V(0.50f, 0.14f));
                    break;

                case IconId.Sword:  // 검: 칼날 + 가드 + 손잡이 + 손잡이 끝 구슬
                    Poly(buffer, V(0.43f, 0.38f), V(0.57f, 0.38f), V(0.57f, 0.84f), V(0.50f, 0.95f), V(0.43f, 0.84f));
                    Rect(buffer, 0.26f, 0.30f, 0.74f, 0.39f);
                    Rect(buffer, 0.455f, 0.08f, 0.545f, 0.31f);
                    Circle(buffer, 0.50f, 0.08f, 0.065f);
                    break;

                case IconId.Bolt:  // 번개: 지그재그 다각형 하나
                    Poly(buffer,
                        V(0.58f, 0.95f), V(0.28f, 0.47f), V(0.46f, 0.47f),
                        V(0.40f, 0.06f), V(0.72f, 0.56f), V(0.54f, 0.56f));
                    break;

                case IconId.Coin:  // 동전: 큰 원 + 안쪽 테두리 선(지우기)
                    Circle(buffer, 0.50f, 0.50f, 0.40f);
                    Ring(buffer, true, 0.50f, 0.50f, 0.325f, 0.295f);
                    break;

                case IconId.Shield:  // 방패: 위가 넓고 아래가 뾰족한 다각형
                    Poly(buffer,
                        V(0.50f, 0.94f), V(0.87f, 0.78f), V(0.87f, 0.46f), V(0.74f, 0.20f),
                        V(0.50f, 0.06f), V(0.26f, 0.20f), V(0.13f, 0.46f), V(0.13f, 0.78f));
                    break;

                case IconId.Star:  // 별: 꼭짓점 5개짜리 별
                    Star(buffer, 0.50f, 0.50f, 0.46f, 0.20f);
                    break;

                case IconId.Crown:  // 왕관: 뾰족한 윗부분 + 아래 띠
                    Poly(buffer,
                        V(0.14f, 0.30f), V(0.86f, 0.30f), V(0.90f, 0.80f), V(0.70f, 0.56f),
                        V(0.50f, 0.86f), V(0.30f, 0.56f), V(0.10f, 0.80f));
                    Rect(buffer, 0.16f, 0.16f, 0.84f, 0.28f);
                    break;

                case IconId.Chevrons:  // 위쪽 화살촉(^) 두 개를 겹친 모양 -> 강화·승급 표시
                    Chevron(buffer, 0.30f);
                    Chevron(buffer, 0.58f);
                    break;

                case IconId.Arrow:  // 오른쪽 화살표
                    Poly(buffer,
                        V(0.10f, 0.39f), V(0.52f, 0.39f), V(0.52f, 0.18f), V(0.92f, 0.50f),
                        V(0.52f, 0.82f), V(0.52f, 0.61f), V(0.10f, 0.61f));
                    break;

                case IconId.Bag:  // 가방: 몸통 + 손잡이 고리 -> 몸통 안쪽을 지웠다가 아랫부분만 다시 칠해서 위쪽은 테두리만 남은 모양을 만듦
                    Poly(buffer, V(0.18f, 0.10f), V(0.82f, 0.10f), V(0.74f, 0.62f), V(0.26f, 0.62f));
                    Ring(buffer, 0.50f, 0.62f, 0.24f, 0.17f);
                    Rect(buffer, true, 0.20f, 0.30f, 0.80f, 0.62f);
                    Poly(buffer, V(0.18f, 0.10f), V(0.82f, 0.10f), V(0.78f, 0.40f), V(0.22f, 0.40f));
                    break;

                case IconId.Check:  // 체크 표시: 굵은 선분 두 개
                    Seg(buffer, 0.16f, 0.54f, 0.42f, 0.26f, 0.075f);
                    Seg(buffer, 0.40f, 0.26f, 0.84f, 0.80f, 0.075f);
                    break;

                default:  // 그림이 정의되지 않은 아이콘은 단순한 원으로 그림
                    Circle(buffer, 0.5f, 0.5f, 0.4f);
                    break;
            }

            return ToSprite(buffer, id.ToString());  // 다 그린 배열을 실제 스프라이트로 변환해서 돌려줌
        }

        static void Base(float[] buffer)  // 체스 기물 아이콘 공통의 사다리꼴 받침을 그리는 함수
        {
            Poly(buffer, V(0.24f, 0.08f), V(0.76f, 0.08f), V(0.70f, 0.23f), V(0.30f, 0.23f));
        }

        static void Seg(float[] buffer, float x0, float y0, float x1, float y1, float halfWidth)  // 두 점을 잇는 굵은 선분을 그리는 함수 (체크 표시용)
        {
            var dir = new Vector2(x1 - x0, y1 - y0).normalized;  // 선분의 진행 방향
            var normal = new Vector2(-dir.y, dir.x) * halfWidth;  // 진행 방향에 수직인 방향 -> 선의 두께를 만듦
            var pad = dir * halfWidth * 0.6f;  // 선 양 끝을 조금 더 늘려서 끝이 뭉툭하게 잘리지 않도록 함

            Poly(buffer,  // 두께를 가진 직사각형(네 꼭짓점)으로 선분을 칠함
                new Vector2(x0, y0) - pad + normal,
                new Vector2(x1, y1) + pad + normal,
                new Vector2(x1, y1) + pad - normal,
                new Vector2(x0, y0) - pad - normal);
        }

        static void Chevron(float[] buffer, float y)  // 높이 y 위치에 ^ 모양 꺾쇠 하나를 그리는 함수
        {
            Poly(buffer,
                V(0.16f, y), V(0.50f, y + 0.22f), V(0.84f, y),
                V(0.84f, y + 0.12f), V(0.50f, y + 0.34f), V(0.16f, y + 0.12f));
        }

        static void Star(float[] buffer, float cx, float cy, float outer, float inner)  // 중심(cx, cy)에 별을 그리는 함수 (outer: 뾰족한 끝 반지름, inner: 오목한 곳 반지름)
        {
            var points = new Vector2[10];  // 별의 꼭짓점 10개 (바깥 5개 + 안쪽 5개)
            for (int i = 0; i < 10; i++)
            {
                float radius = (i % 2 == 0) ? outer : inner;  // 짝수 번째는 바깥 꼭짓점, 홀수 번째는 안쪽 꼭짓점
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;  // 위쪽(90도)에서 시작해 36도씩 회전
                points[i] = new Vector2(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius);
            }
            Poly(buffer, false, points);  // 구한 꼭짓점들로 다각형을 칠함
        }

        const int Samples = 2;  // 픽셀 하나를 가로·세로 몇 번씩 나눠 검사할지 -> 2x2 = 4번 검사해서 테두리를 부드럽게(안티에일리어싱) 만듦

        static Vector2 V(float x, float y) => new Vector2(x, y);  // 좌표를 짧게 쓰기 위한 도우미 함수

        static void Circle(float[] buffer, float cx, float cy, float r) => Circle(buffer, false, cx, cy, r);  // 원을 칠하는 함수 (지우기 없음)

        static void Circle(float[] buffer, bool erase, float cx, float cy, float r)  // 원을 칠하거나(erase=false) 지우는(erase=true) 함수
        {
            float r2 = r * r;  // 반지름의 제곱 -> 제곱근 계산 없이 거리 비교하기 위함
            Paint(buffer, erase, p =>
            {
                float dx = p.x - cx, dy = p.y - cy;
                return dx * dx + dy * dy <= r2;  // 중심까지 거리가 반지름 이하라면 원 안쪽
            });
        }

        static void Ring(float[] buffer, float cx, float cy, float outer, float inner)  // 고리(도넛) 모양을 칠하는 함수
            => Ring(buffer, false, cx, cy, outer, inner);

        static void Ring(float[] buffer, bool erase, float cx, float cy, float outer, float inner)  // 고리 모양을 칠하거나 지우는 함수
        {
            float o2 = outer * outer, i2 = inner * inner;  // 바깥·안쪽 반지름의 제곱
            Paint(buffer, erase, p =>
            {
                float dx = p.x - cx, dy = p.y - cy;
                float d2 = dx * dx + dy * dy;
                return d2 <= o2 && d2 >= i2;  // 바깥 원 안쪽이면서 안쪽 원 바깥이라면 고리 영역
            });
        }

        static void Rect(float[] buffer, float x0, float y0, float x1, float y1)  // 사각형을 칠하는 함수
            => Rect(buffer, false, x0, y0, x1, y1);

        static void Rect(float[] buffer, bool erase, float x0, float y0, float x1, float y1)  // 사각형을 칠하거나 지우는 함수 (왼쪽 아래 x0,y0 ~ 오른쪽 위 x1,y1)
            => Paint(buffer, erase, p => p.x >= x0 && p.x <= x1 && p.y >= y0 && p.y <= y1);

        static void Poly(float[] buffer, params Vector2[] points) => Poly(buffer, false, points);  // 다각형을 칠하는 함수

        static void Poly(float[] buffer, bool erase, params Vector2[] points)  // 다각형을 칠하거나 지우는 함수
            => Paint(buffer, erase, p => Inside(points, p));

        static bool Inside(Vector2[] points, Vector2 p)  // 점 p가 다각형 안에 있는지 검사하는 함수
        // -> 점에서 오른쪽으로 선을 그었을 때 다각형 변과 홀수 번 만나면 안쪽, 짝수 번이면 바깥쪽
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)  // 다각형의 모든 변(i번 점 ~ j번 점)을 검사
            {
                if (points[i].y > p.y == points[j].y > p.y)  // 만약 변의 두 끝이 모두 점보다 위거나 모두 아래라면 -> 만날 수 없으므로 건너뜀
                    continue;

                float t = (p.y - points[i].y) / (points[j].y - points[i].y);  // 점의 높이에서 변 위의 위치 비율을 구하고
                if (p.x < points[i].x + t * (points[j].x - points[i].x))  // 만약 그 교차점이 점보다 오른쪽이라면
                    inside = !inside;  // 만난 횟수 홀짝을 뒤집음
            }
            return inside;
        }

        static void Paint(float[] buffer, bool erase, System.Func<Vector2, bool> inside)  // 도형 판정 함수(inside)를 받아서 모든 픽셀을 칠하거나 지우는 함수
        {
            const float step = 1f / (Size * Samples);  // 샘플 하나의 간격 (현재는 사용하지 않음)

            for (int y = 0; y < Size; y++)  // 모든 픽셀을 한 칸씩 돌면서
            for (int x = 0; x < Size; x++)
            {
                int hits = 0;  // 이 픽셀 안의 검사 지점 중 도형 안에 들어간 개수
                for (int sy = 0; sy < Samples; sy++)  // 픽셀을 Samples x Samples 개의 작은 지점으로 나눠서
                for (int sx = 0; sx < Samples; sx++)
                {
                    var p = new Vector2(
                        (x + (sx + 0.5f) / Samples) / Size,
                        (y + (sy + 0.5f) / Samples) / Size);  // 각 지점의 좌표를 0~1 범위로 계산

                    if (inside(p))  // 만약 그 지점이 도형 안이라면
                        hits++;  // 개수를 늘림
                }

                if (hits == 0)  // 만약 한 지점도 도형 안에 없다면 -> 이 픽셀은 건드리지 않음
                    continue;

                float coverage = hits / (float)(Samples * Samples);  // 픽셀이 도형에 덮인 비율 (0~1)
                int index = y * Size + x;  // 2차원 좌표를 1차원 배열 위치로 변환
                buffer[index] = erase
                    ? Mathf.Min(buffer[index], 1f - coverage)  // 지우기라면 덮인 비율만큼 투명하게 만들고
                    : Mathf.Max(buffer[index], coverage);  // 칠하기라면 덮인 비율만큼 진하게 만듦 (기존보다 진할 때만)
            }

            _ = step;  // 사용하지 않는 변수 경고를 막기 위한 줄
        }

        static Sprite ToSprite(float[] buffer, string name)  // 칠해진 배열을 실제 텍스처와 스프라이트로 바꾸는 함수
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)  // 128x128 텍스처를 새로 만들고
            {
                filterMode = FilterMode.Bilinear,  // 확대·축소 시 부드럽게 보이도록
                wrapMode = TextureWrapMode.Clamp,  // 가장자리 픽셀이 반대편으로 번지지 않도록
                hideFlags = HideFlags.HideAndDontSave  // 씬에 저장되지 않는 임시 텍스처로 설정
            };

            var pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++)  // 모든 픽셀을 흰색으로 채우되
                pixels[i] = new Color(1f, 1f, 1f, buffer[i]);  // 투명도만 배열 값으로 설정 -> 흰색이라 Image 색으로 아이콘 색을 자유롭게 바꿀 수 있음

            tex.SetPixels32(pixels);  // 픽셀 데이터를 텍스처에 넣고
            tex.Apply();  // 그래픽 카드에 반영

            var sprite = Sprite.Create(tex, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);  // 텍스처 전체를 가운데 기준점의 스프라이트로 만듦
            sprite.name = "Icon_" + name;  // 디버깅할 때 알아보기 쉽도록 이름을 붙임
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
