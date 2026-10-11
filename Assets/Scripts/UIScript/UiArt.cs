using Promotion.Core;
using UnityEngine;

namespace Promotion.UI
{
    /// <summary>
    /// 인터페이스의 기본 도형과 아이콘을 실제 이미지 에셋으로 보관한다. 그래서 프리팹으로
    /// 저장한 화면도 그림을 잃지 않는다. Resources/UiArt에 두며, 이 에셋이 없으면 도형을
    /// 코드로 그려 대신한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Promotion/UI Art", fileName = "UiArt")]
    public class UiArt : ScriptableObject
    {
        public Sprite roundedRect;
        public Sprite square;
        public Sprite circle;

        [Tooltip("IconId 값을 인덱스로 사용한다.")]
        public Sprite[] icons = new Sprite[0];

        static UiArt _instance;
        static bool _looked;

        public static UiArt Instance
        {
            get
            {
                if (!_looked)
                {
                    _instance = Resources.Load<UiArt>("UiArt");
                    _looked = true;
                }
                return _instance;
            }
        }

        public static Sprite RoundedRect
        {
            get
            {
                var art = Instance;
                return art != null && art.roundedRect != null ? art.roundedRect : PlaceholderArt.RoundedRect;
            }
        }

        public static Sprite Square
        {
            get
            {
                var art = Instance;
                return art != null && art.square != null ? art.square : PlaceholderArt.Square;
            }
        }

        public static Sprite Circle
        {
            get
            {
                var art = Instance;
                return art != null && art.circle != null ? art.circle : PlaceholderArt.Circle;
            }
        }

        /// <summary>저장된 아이콘을 돌려준다. 없어서 코드로 그려야 하면 null이다.</summary>
        public static Sprite Icon(IconId id)
        {
            var art = Instance;
            int index = (int)id;
            if (art == null || art.icons == null || index < 0 || index >= art.icons.Length)
                return null;
            return art.icons[index];
        }
    }
}
