using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragSlot : MonoBehaviour
{
    // 원본 Slot을 드래그 앤 드롭으로 끌고 다니면 Grid Layout이 깨지고 순서도 바뀔 수 있음
    // 따라서 드래그 하는 동안만 보이는 DragSlot을 마우스에 붙여서 움직이도록 함

    public static DragSlot instance;
    public ItemSlot dragSlot;

    [SerializeField]
    private Image itemImage;

    void Start()
    {
        instance = this;    // 드래그 중인 슬롯은 하나의 아이템만 들어가야하므로 싱글톤 패턴을 사용해 드래그 슬롯의 갱신을 차단
        SetColor(0);    // 드래그 슬롯이 Scene이 시작되면 화면 중앙에 빈 칸으로 보이는 것을 안보이게 함
    }

    public void DragSetImage(Image _itemImage)  // 드래그 슬롯의 아이템 이미지를 설정
    {
        itemImage.sprite = _itemImage.sprite;
        SetColor(1);
    }

    public void SetColor(float _alpha)  // 아이템 이미지와 투명도를 조절해 슬롯을 보이게 할지 안보이게 할지 결정하는 함수
    // _alpha 값으로 투명도 값을 받아서 _alpha 값으로 아이템 슬롯의 투명도를 결정
    {
        Color color = itemImage.color;
        color.a = _alpha;
        itemImage.color = color;
    }
}

