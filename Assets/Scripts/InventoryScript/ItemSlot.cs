using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    // 인벤토리 내부의 각각의 아아템 슬롯을 관리하는 스크립트
    // 아이템을 저장, 사용, 버리기, 드래그 앤 드롭 등의 기능을 총괄한다
    // 또한 슬롯 내부의 정보(슬롯에 저장된 아이템 갯수, 아이템 스프라이트 등)를 나타내는 UI를 띄우는 기능을 한다
{
    public ItemData item; // 인벤토리에 저장할 아이템
    public int itemCount; // 저장된 아이템의 갯수
    public Image itemImage; // 저장된 아이템의 이미지

    private InventoryManager inventoryManager;  // itemSlot의 부모인 InventoryManager에 접근하기 위한 변수

    [SerializeField]
    private TextMeshProUGUI itemTextCount; // 아이템의 갯수를 표시하는 텍스트 UI에 접근하기 위한 변수
    [SerializeField]
    private GameObject itemCountImage;  // 아이템의 갯수 이미지를 표시하는 Image UI에 접근하기 위한 변수
    [SerializeField]
    private ItemData.ItemType allowedType;  // 이 슬롯에 저장할 수 있는 아이템 유형 (인스펙터에서 설정)

    void Awake()
    {
        inventoryManager = GetComponentInParent<InventoryManager>();    
        // 아이템 슬롯의 부모 오브젝트인 InventoryManager 오브젝트를 찾아 할당
    }

    void Start()
    {
        SetImageAlpha(0);   // 최초 인벤토리를 불러올 때 슬롯의 이미지들이 보이지 않도록 설정
        itemCountImage.SetActive(false);
    }
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

    public void ClearSlot() // 현재 슬롯에 저장된 아이템을 삭제하는 함수
    {
        item = null;    // 현재 저장된 아이템 정보를 삭제하고
        itemCount = 0;  // 아이템과 관련된 모든 정보(아이템 갯수, 이미지, 텍스트 UI, 이미지 투명도)를 초기화 후 UI에서 보이지 않도록 함
        itemImage.sprite = null;
        SetImageAlpha(0);
        itemTextCount.text = "0";
        
        itemCountImage.SetActive(false);    // 마지막으로 아이템 갯수 UI가 보이지 않도록 설정
    }

    // 마우스 드래그가 시작됐을 때 발생하는 이벤트
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("드래그 입력 확인 OnBeginDrag");
        if(item != null)    // 현재 슬롯에 저장된 아이템이 있다면
        {
            DragSlot.instance.dragSlot = this;  // 그 아이템 슬롯 객체를 가리키는 참조 정보를 드래그 슬롯에 저장한다
            DragSlot.instance.DragSetImage(itemImage);  // 그 아이템의 이미지로 드래그 슬롯의 이미지도 변경된다
            DragSlot.instance.transform.position = eventData.position;  
            // 드래그 슬롯이 마우스를 따라가야하므로 드래그 슬롯이 마우스의 위치 값을 받아 그 위치로 이동되게함
        }
    }

    // 마우스 드래그 중일 때 계속 발생하는 이벤트
    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("드래그 입력 확인 OnDrag");
        if (item != null)   // 현재 드래그 슬롯이 가리키는 참조 정보가 있다면
            DragSlot.instance.transform.position = eventData.position;
            // 드래그 슬롯이 마우스를 따라가야하므로 드래그 슬롯이 마우스의 위치 값을 받아 그 위치로 이동되게함
    }

    // 마우스 드래그가 끝났을 때 발생하는 이벤트
    // OnEndDrag 는 드래그를 시작한 슬롯에서 호출되고 OnDrop 보다 먼저 호출됨
    // 따라서 이 함수에는 드래그를 시작한 슬롯에 저장된 아이템의 정보가 저장된다
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("드래그 입력 확인 OnEndDrag");
        DragSlot.instance.SetColor(0);  // 드래그 앤 드롭이 끝났으므로 드래그 슬롯의 투명도를 0으로 설정해 안보이게 하고
        DragSlot.instance.dragSlot = null;  // 드래그 슬롯 안의 아이템 슬롯 참조 정보를 삭제함
    }

    // 마우스 드래그가 끝나고 그 위치에 드롭할 대상이 있다면 호출되는 함수
    // OnDrop 는 드롭을 한 위치에 있는 슬롯에서 호출되고 OnEndDrag보다 나중에 호출됨
    // 따라서 이 함수에서 저장하는 item의 정보는 드롭한 위치에 있는 슬롯에 저장된 아이템 정보가 저장된다
    public void OnDrop(PointerEventData eventData)
    {
        if (DragSlot.instance.dragSlot != null && CanSwapWith(DragSlot.instance.dragSlot))
        // 만약 드래그 슬롯 안에 아이템 슬롯 참조 정보가 저장되어있고 드롭을 한 위치에 또 다른 아이템 슬롯이 있고,
        // 그 아이템 슬롯에 저장된 아이템과 드래그 슬롯에 저장된 아이템이 서로 교환 가능하다면
        {
           ChangeSlot();    // 서로 아이템의 위치를 교환한다
        }
    }

    public void OnPointerClick(PointerEventData eventData)  // 슬롯을 클릭했을 때 실행되는 함수
    {
        Debug.Log("슬롯 우클릭 확인");
        if (eventData.button != PointerEventData.InputButton.Right) {   // 우클릭이 아닌 좌클릭을 했을 때는 아무것도 하지 않음
            Debug.Log("슬롯 좌클릭 확인");
            return;  
        }
        if (item == null) {    // 만약 아이템이 저장되어있지 않은 빈 슬롯이라면 아무것도 하지 않음
            Debug.Log("빈 슬롯 우클릭 확인");
            return;  
        } 

        inventoryManager.OpenItemActionMenu(this, eventData.position);  
        // 이 슬롯과 관련된 참조 정보를 inventoryManager에게 넘기고 우클릭한 위치에 아이템 사용, 버리기 메뉴를 열도록 함
    }
    
    private void ChangeSlot()
    {
        ItemData _tempItem = item;  // 현재 아이템 슬롯(드롭한 위치의 슬롯에 저장된 아이템 정보)
        int _tempItemCount = itemCount; // 에 저장된 아이템의 정보와 갯수 정보를 임시로 저장하고

        AddItem(DragSlot.instance.dragSlot.item, DragSlot.instance.dragSlot.itemCount);
        // 드래그 슬롯에 저장된 아이템 참조 정보, 갯수 정보를 이용해 드롭한 위치의 슬롯에 드래그 슬롯에 저장되어있던 아이템을 저장

        if (_tempItem != null)  // 만약 현재 슬롯에 저장되어있던 아이템이 있었다면
        {
            DragSlot.instance.dragSlot.AddItem(_tempItem, _tempItemCount);  
            // 드래그 슬롯에는 드래그를 시작한 아이템 슬롯의 참조 정보가 저장되어있으므로 드래그 슬롯에서 AddItem을 호출하는 것은
            // 드래그를 시작한 아이템 슬롯에서 AddItem 함수를 호출하는 것과 같다
            // 따라서 드래그를 시작한 아이템 슬롯에 임시로 저장된 드롭한 위치의 아이템 슬롯에 저장된 아이템 정보를 저장한다
            // A(드래그를 시작한 위치의 아이템 슬롯의 아이템 정보) <-> B(드롭한 위치의 아이템 슬롯의 아이템 정보)
        }
        else
        {
            DragSlot.instance.dragSlot.ClearSlot(); 
            // 드래그를 시작한 위치의 아이템 슬롯의 아이템 정보를 초기화
        }
    }

    public bool CanAccept(ItemData _item)
    // 이 슬롯에 해당 아이템을 저장할 수 있는지 검사하는 함수
    // _item을 통해서 드래그 슬롯에 저장된 아이템 참조 정보가 전달됨
    {
        if (_item == null)
        {
            return true;          // 빈 칸은 어느 슬롯에든 들어갈 수 있음
        }
        return _item.itemType == allowedType; // 인스펙터 창에서 설정하는 슬롯이 허용하는 아이템 유형과 같다면 true를 반환 -> 아이템 저장 가능
    }

    private bool CanSwapWith(ItemSlot _fromSlot)
    // 드래그한 슬롯과 이 슬롯이 서로 아이템을 교환할 수 있는지 양방향으로 검사하는 함수
    // _fromSlot을 통해서 드래그 슬롯에 저장된 아이템 참조 정보가 들어온다
    {
        if (_fromSlot == this) // 만약 드래그 슬롯에 저장된 아이템 참조 정보가 자기 자신과 같다면
        {
            return false;     // 자기 자신에게 놓은 경우는 교환하지 않음
        }

        return CanAccept(_fromSlot.item)         // 드래그한 아이템이 이 슬롯에 들어갈 수 있는가 (드래그 슬롯 -> 아이템 슬롯 변경 가능한가?)
            && _fromSlot.CanAccept(item);        // 이 슬롯의 아이템이 원래의 슬롯으로 갈 수 있는가 (아이템 슬롯 -> 드래그 슬롯 변경 가능한가?)
        // CanAccept(_fromSlot.item) = this.CanAccept(_fromSlot.item) -> _fromSlot.item = 드래그를 시작한 위치의 아이템 참조 정보를 가짐
        // 여기서 this는 드롭한 위치에 존재하는 아이템 슬롯을 가리킨다
        // -> 즉 _fromSlot.item으로 전달된 아이템 참조 정보(드래그를 시작한 아이템 슬롯에 저장된 아이템)
        // 가 드롭한 위치에 있는 아이템 슬롯에 저장 가능한가를 확인
        // _fromSlot.CanAccept(item) -> 드래그를 시작한 아이템 슬롯에 드롭한 위치에 있는 아이템 슬롯에 저장된 아이템을 저장 가능한가를 확인
    }
}
