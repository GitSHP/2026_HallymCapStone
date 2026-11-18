using UnityEngine;

namespace Promotion.UI
{
    [CreateAssetMenu(menuName = "Promotion/UI Art", fileName = "UiArt")]
    public class UiArt : ScriptableObject  // UI에 쓰는 스프라이트(패널 모양·아이콘)를 모아 둔 데이터 에셋 -> Resources/UiArt 에셋 하나만 사용
    {
        public Sprite roundedRect;  // 모서리가 둥근 사각형 스프라이트 (패널·카드·버튼 바탕)
        public Sprite square;  // 일반 사각형 스프라이트
        public Sprite circle;  // 원형 스프라이트

        [Tooltip("IconId 값을 인덱스로 사용한다.")]
        public Sprite[] icons = new Sprite[0];  // 아이콘 스프라이트 배열 -> IconId 순서와 같은 순서로 넣어야 함

        static UiArt _instance;  // 한 번 불러온 UiArt 에셋을 저장해 두는 변수
        static bool _looked;  // Resources에서 이미 찾아봤는지 여부 -> 에셋이 없어도 매번 다시 찾지 않도록 함

        public static UiArt Instance  // UiArt 에셋을 어디서든 꺼내 쓸 수 있게 해주는 속성
        {
            get
            {
                if (!_looked)  // 만약 아직 한 번도 찾아보지 않았다면
                {
                    _instance = Resources.Load<UiArt>("UiArt");  // Resources 폴더에서 "UiArt" 에셋을 불러오고
                    _looked = true;  // 찾아봤다고 표시함 -> 다음부터는 저장된 값을 그대로 사용
                }
                return _instance;  // 에셋이 없으면 null이 반환됨
            }
        }

        public static Sprite RoundedRect  // 둥근 사각형 스프라이트를 돌려주는 속성
        {
            get
            {
                var art = Instance;
                return art != null ? art.roundedRect : null;  // Resources/UiArt 에셋에 등록된 둥근 사각형 이미지
            }
        }

        public static Sprite Square  // 사각형 스프라이트를 돌려주는 속성
        {
            get
            {
                var art = Instance;
                return art != null ? art.square : null;  // Resources/UiArt 에셋에 등록된 흰 사각형 이미지
            }
        }

        public static Sprite Circle  // 원형 스프라이트를 돌려주는 속성
        {
            get
            {
                var art = Instance;
                return art != null ? art.circle : null;  // Resources/UiArt 에셋에 등록된 흰 원 이미지
            }
        }

        public static Sprite Icon(IconId id)  // 아이콘 종류(IconId)에 맞는 아이콘 스프라이트를 돌려주는 함수
        {
            var art = Instance;
            int index = (int)id;  // IconId 값을 숫자로 바꿔 배열 인덱스로 사용
            if (art == null || art.icons == null || index < 0 || index >= art.icons.Length)  // 만약 에셋이 없거나 인덱스가 배열 범위를 벗어났다면
                return null;  // 아이콘 없음으로 처리 -> UiSprites가 코드로 그린 아이콘으로 대체함
            return art.icons[index];
        }
    }
}
