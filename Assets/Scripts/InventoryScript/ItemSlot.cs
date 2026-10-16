using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    // 인벤토리 내부의 아아템 슬롯을 관리하는 스크립트
    // 아이템을 저장, 사용, 버리기, 드래그 앤 드롭 등의 기능을 총괄한다
    // 또한 슬롯 내부의 정보(슬롯에 저장된 아이템 갯수, 아이템 스프라이트 등)를 나타내는 UI를 띄우는 기능을 한다
{
    public ItemData item; // 인벤토리에 저장할 아이템
    public int itemCount; // 저장된 아이템의 갯수
    public Image itemImage; // 저장된 아이템의 이미지

    private InventoryManager inventoryManager;  // itemSlot의 부모인 InventoryManager에 접근하기 위한 변수

    public bool IsOwnedPiece { get; private set; }  // 이 슬롯의 기물이 런에서 모은 보유 기물인지(true) 킹·상시 폰 같은 임시 기물인지(false)
    public int DeckIndex { get; private set; } = -1;    // 임시 기물일 때 DeployController 덱에서 몇 번째 카드인지 -> 사용할 때 deploy.Select(DeckIndex)

    private Image frame;    // 슬롯 바탕(테두리) 이미지 -> 소환할 기물로 고른 슬롯을 강조할 때 색을 바꿈
    private Color frameColor;   // 슬롯 바탕의 원래 색
    private static readonly Color HighlightColor = new Color(0.95f, 0.78f, 0.25f, 1f);  // 고른 슬롯의 강조 색(금색)

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
        frame = GetComponent<Image>();
        if (frame != null)
            frameColor = frame.color;
    }

    void Start()
    {
        if (item == null)   // 아직 아무것도 그려지지 않은 슬롯만 숨김 -> InventoryManager가 먼저 채운 슬롯을 지우지 않도록
        {
            SetImageAlpha(0);   // 최초 인벤토리를 불러올 때 슬롯의 이미지들이 보이지 않도록 설정
            itemCountImage.SetActive(false);
        }
    }

    public void SetPieceSource(bool _owned, int _deckIndex)  // 이 슬롯 기물의 출처를 기록하는 함수 -> InventoryManager.RefreshPieces가 슬롯을 그릴 때 호출
    // _owned: 런 장부(RunState)의 보유 기물이면 true, DeployController 덱의 임시 기물이면 false
    // _deckIndex: 임시 기물일 때 덱에서의 번호(보유 기물이면 -1)
    {
        IsOwnedPiece = _owned;
        DeckIndex = _deckIndex;
    }

    public void SetCountText(string _text)  // 개수 표시 글자를 직접 정하는 함수 -> 상시 폰처럼 개수 대신 "∞"를 보여 줄 때 사용
    {
        itemCountImage.SetActive(true);
        itemTextCount.text = _text;
    }

    public void SetHighlight(bool _on)  // 소환할 기물로 고른 슬롯이면 바탕을 금색으로 강조하는 함수
    {
        if (frame != null)
            frame.color = _on ? HighlightColor : frameColor;
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
        IsOwnedPiece = false;   // 기물 출처 정보도 초기화
        DeckIndex = -1;
        itemCount = 0;  // 아이템과 관련된 모든 정보(아이템 갯수, 이미지, 텍스트 UI, 이미지 투명도)를 초기화 후 UI에서 보이지 않도록 함
        itemImage.sprite = null;
        SetImageAlpha(0);
        itemTextCount.text = "0";
        
        itemCountImage.SetActive(false);    // 마지막으로 아이템 갯수 UI가 보이지 않도록 설정
    }

    // 마우스 드래그가 시작 됐을 때 발생하는 이벤트
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (allowedType != ItemData.ItemType.Usable)    // 기물·유물 슬롯은 장부 순서대로 그려지므로 드래그로 옮기지 않음 -> 아이템 슬롯만 드래그 가능
            return;
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
        if (item != null && DragSlot.instance.dragSlot == this)   // 이 슬롯에서 시작한 드래그일 때만 따라 움직임
            DragSlot.instance.transform.position = eventData.position;
    }

    // 마우스 드래그가 끝났을 때 발생하는 이벤트
    public void OnEndDrag(PointerEventData eventData)
    {
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

    public void OnPointerClick(PointerEventData eventData)  // 슬롯을 클릭했을 때 실행되는 함수
    {
        if (item == null) {    // 만약 아이템이 저장되어있지 않은 빈 슬롯이라면 아무것도 하지 않음
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left) {   // 좌클릭이라면 바로 사용
            if (allowedType == ItemData.ItemType.Piece)    // 기물 슬롯만 좌클릭으로 바로 사용 -> 소환할 기물로 고름 (빠른 조작)
                inventoryManager.UseSlot(this);
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Right) {   // 가운데 버튼 등은 무시
            return;
        }

        inventoryManager.OpenItemActionMenu(this, eventData.position);  
        // 이 슬롯과 관련된 정보를 inventoryManager에게 넘기고 우클릭한 위치에 아이템 사용, 버리기 메뉴를 열도록 함
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
