using UnityEngine;
using UnityEngine.UI;

namespace Promotion.UI
{
    [RequireComponent(typeof(Button))]
    public class GuideBookItemButton : MonoBehaviour    // 도감 리스트의 아이콘 버튼 - 어떤 아이템(아티팩트/기물)을 설명할지 기억함
    // -> 버튼 자신의 Image(외곽선 등 버튼 디자인)는 건드리지 않고, 자식 Image(PieceImage)에만 아이템 그림을 넣음
    {
        const string DefaultIconChildName = "PieceImage";   // Icon 칸이 비어 있을 때 자동으로 찾을 자식 오브젝트 이름

        [Tooltip("이 버튼이 설명할 아이템 에셋 (Assets/Item/Artifacts 또는 ChessPieces)")]
        [SerializeField] ItemData _item;

        [Tooltip("클릭을 전달할 도감 매니저")]
        [SerializeField] GuideBookUIManager _manager;

        [Tooltip("아이템 그림을 띄울 자식 Image (예: PieceImage). 비워 두면 'PieceImage'라는 자식을 자동으로 찾는다. 버튼 자신의 Image는 넣지 않는다.")]
        [SerializeField] Image _icon;

        void Reset()    // 컴포넌트를 처음 붙이거나 인스펙터에서 Reset을 누르면 실행 -> 그림을 넣을 자식 Image를 자동으로 찾아 연결
        {
            FindIconIfEmpty();
        }

        void Awake()    // 클릭 함수를 연결하고, 그림 칸에 아이템 스프라이트를 넣음
        {
            Button button = GetComponent<Button>();
            button.onClick.AddListener(OnClicked);
            ApplyIcon();
        }

        void OnValidate()   // 인스펙터에서 값을 바꿀 때마다 실행 -> 에디터에서도 그림이 바로 바뀌어 보이게 함
        {
            ApplyIcon();
        }

        void FindIconIfEmpty()  // Icon 칸이 비어 있으면 'PieceImage' 자식을 찾아 연결
        {
            if (_icon != null)
            {
                return;
            }

            Transform child = transform.Find(DefaultIconChildName); // 바로 아래 자식 중에서 이름으로 찾음
            if (child != null)
            {
                _icon = child.GetComponent<Image>();
            }
        }

        void ApplyIcon()    // 아이템의 스프라이트를 그림 칸(자식 Image)에 넣음 -> 버튼 자신의 Image는 절대 바꾸지 않음
        {
            if (_item == null || _item.itemImage == null)
            {
                return;
            }

            FindIconIfEmpty();

            if (_icon == null)  // 그림을 넣을 곳이 없으면 아무것도 바꾸지 않음 (예전처럼 버튼 자신의 Image를 대신 쓰지 않음)
            {
                return;
            }

            if (_icon.gameObject == gameObject) // 실수로 버튼 자신의 Image를 넣은 경우 -> 외곽선이 덮어써지지 않게 막음
            {
                Debug.LogWarning("[GuideBookItemButton] Icon에 버튼 자신의 Image가 들어 있습니다. 자식 Image(PieceImage)를 넣어 주세요: " + name);
                return;
            }

            _icon.sprite = _item.itemImage;
            _icon.preserveAspect = true;    // 그림 비율이 찌그러지지 않게 함
        }

        void OnClicked()    // 버튼이 눌리면 -> 매니저에게 "이 아이템 설명을 보여 줘" 라고 요청
        {
            if (_manager == null)
            {
                Debug.LogWarning("[GuideBookItemButton] 매니저가 연결되지 않았습니다: " + name);
                return;
            }
            _manager.ShowItem(_item);
        }
    }
}
