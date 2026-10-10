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
        instance = this;
    }

    public void DragSetImage(Image _itemImage)
    {
        itemImage.sprite = _itemImage.sprite;
        SetColor(1);
    }

    public void SetColor(float _alpha)
    {
        Color color = itemImage.color;
        color.a = _alpha;
        itemImage.color = color;
    }
}

