using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public ItemData item; // 인벤토리에 저장할 아이템
    public int itemCount; // 저장된 아이템의 갯수
    public Image itemImage; // 저장된 아이템의 이미지

    [SerializeField]
    private TextMeshProUGUI itemTextCount; // 아이템의 갯수를 표시하는 텍스트 UI에 접근하기 위한 변수
    [SerializeField]
    private GameObject itemCountImage;  // 아이템의 갯수 이미지를 표시하는 Image UI에 접근하기 위한 변수
    [SerializeField]
    private ItemData.ItemType allowedType;  // 이 슬롯에 저장할 수 있는 아이템 유형 (인스펙터에서 설정)

    private void SetImageAlpha(float _alpha) // 아이템 이미지의 투명도 조절 
    // -> 아이템이 저장된 경우 아이템 이미지의 투명도를 높여 이미지가 보이도록 조절하고 저장되지 않은 상태면 투명도를 낮춰 보이지 않게함
    {
        Color color = itemImage.color;
        color.a = _alpha;
        itemImage.color = color;
    }

    public void AddItem(ItemData _item, int _count) // 인벤토리에 아이템 새로운 아이템을 추가
    {
        item = _item;
        itemCount = _count;
        itemImage.sprite = item.itemImage;

        if(item.itemType == ItemData.ItemType.Usable) // 만약 인벤토리에 저장할 아이템이 사용 가능한 아이템이라면 -> 여러 개를 저장할 수 도 있음
        {
            itemCountImage.SetActive(true); // 저장한 아이템의 갯수 이미지 UI를 보이도록 활성화하고
            itemTextCount.text = itemCount.ToString();  // 현재 저장된 아이템의 갯수가 보이도록 텍스트 UI로 현재 저장된 아이템 갯수를 표시함
        }
        else if (item.itemType == ItemData.ItemType.Piece)
        {
            itemCountImage.SetActive(true); 
            itemTextCount.text = itemCount.ToString();  
        }
        else    // 저장할 아이템이 아티팩트일 경우 -> 한 개만 저장할 수 있음
        {
            itemTextCount.text = "0";   // 현재 저장된 아이템 갯수를 0으로 초기화하고
            itemCountImage.SetActive(false);    // 아이템 갯수를 표시하는 UI가 보이지않도록 함
        }
        
        SetImageAlpha(1);
    }

    public void SetSlotCount(int _count)    // 현재 슬롯의 아이템 갯수를 업데이트해주는 함수
    {
        itemCount += _count;    // 현재 슬롯에 저장된 아이템의 갯수를 매개변수(_count)의 크기만큼 증가
        itemTextCount.text = itemCount.ToString();  // 아이템 갯수를 표시하는 텍스트 UI를 현재 갯수에 맞게 초기화

        if(itemCount <= 0)  // 만약 아이템의 갯수가 0이라면 -> 현재 슬롯에 저장된 아이템의 숫자가 0 -> 아이템이 없어질 예정
        {
            ClearSlot();   
        }
    }

    private void ClearSlot() // 현재 슬롯에 저장된 아이템을 삭제하는 함수
    {
        item = null;    // 현재 저장된 아이템 정보를 삭제하고
        itemCount = 0;  // 아이템과 관련된 모든 정보(아이템 갯수, 이미지, 텍스트 UI, 이미지 투명도)를 초기화 후 UI에서 보이지 않도록 함
        itemImage.sprite = null;
        SetImageAlpha(0);
        itemTextCount.text = "0";
        
        itemCountImage.SetActive(false);    // 마지막으로 아이템 갯수 UI가 보이지 않도록 설정
    }

    // 마우스 드래그가 시작 됐을 때 발생하는 이벤트
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("드래그 입력 확인 OnBeginDrag");
        if(item != null)
        {
            DragSlot.instance.dragSlot = this;
            DragSlot.instance.DragSetImage(itemImage);
            DragSlot.instance.transform.position = eventData.position;
        }
    }

    // 마우스 드래그 중일 때 계속 발생하는 이벤트
    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("드래그 입력 확인 OnDrag");
        if (item != null)
            DragSlot.instance.transform.position = eventData.position;
    }

    // 마우스 드래그가 끝났을 때 발생하는 이벤트
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("드래그 입력 확인 OnEndDrag");
        DragSlot.instance.SetColor(0);
        DragSlot.instance.dragSlot = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (DragSlot.instance.dragSlot != null && CanSwapWith(DragSlot.instance.dragSlot))
        {
           ChangeSlot();
        }
    }
    
    private void ChangeSlot()
    {
        ItemData _tempItem = item;
        int _tempItemCount = itemCount;

        AddItem(DragSlot.instance.dragSlot.item, DragSlot.instance.dragSlot.itemCount);

        if (_tempItem != null)
        {
            DragSlot.instance.dragSlot.AddItem(_tempItem, _tempItemCount);
        }
        else
        {
            DragSlot.instance.dragSlot.ClearSlot();
        }
    }

    public bool CanAccept(ItemData _item)
    // 이 슬롯에 해당 아이템을 저장할 수 있는지 검사하는 함수
    {
        if (_item == null) 
        {
            return true;          // 빈 칸은 어느 슬롯에든 들어갈 수 있음
        }
        return _item.itemType == allowedType;    // 슬롯이 허용하는 유형과 같아야만 저장 가능
    }

    private bool CanSwapWith(ItemSlot _fromSlot)
    // 드래그한 슬롯과 이 슬롯이 서로 아이템을 교환할 수 있는지 양방향으로 검사하는 함수
    {
        if (_fromSlot == this) 
        {
            return false;     // 자기 자신에게 놓은 경우는 교환하지 않음
        }

        return CanAccept(_fromSlot.item)         // 드래그한 아이템이 이 슬롯에 들어갈 수 있는가
            && _fromSlot.CanAccept(item);        // 이 슬롯의 아이템이 원래 슬롯으로 갈 수 있는가
    }
}
