using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour   // 인벤토리의 최고 부모 클래스 - 모든 InventorySlot의 부모
    // 전체적인 인벤토리를 관리하는 스크립트
    // 슬롯 내부에 어떤 아이템이 있는지 확인하고 슬롯이 비거나 아이템이 저장되어있을 때 그 상황에 맞는 기능을 수행한다
    // 또한 슬롯 내부의 아이템들을 검사하고 아이템이 가득차거나 중복되는 아이템이 있는 등의 상황이 발생하면 이에 맞는 UI도 함께 띄워준다
{   
    [SerializeField]
    private GameObject inventoryBase;   // 모든 인벤토리를 관리할 때 사용할 변수
    [SerializeField]
    private GameObject itemInventoryBase;   // 아이템 인벤토리를 관리할 때 사용할 변수
    [SerializeField]
    private GameObject pieceInventoryBase;  // 체스 말 인벤토리를 관리할 때 사용할 변수
    [SerializeField]
    private GameObject artifactInventoryBase; // 아티팩트 인벤토리를 관리할 때 사용할 변수

    private ItemSlot[] itemSlots;   // 아이템에 대한 정보가 들어있는 배열
    private ItemSlot[] pieceSlots; //  체스 말에 대한 정보가 들어있는 배열
    private ItemSlot[] artifactSlots; // 아티팩트에 대한 정보가 들어있는 배열
    private ItemData pendingItem;   // 저장하지 못해 대기 중인 아이템
    private ItemSlot selectedSlot; // 슬롯을 우클릭했을 때 그 선택된 슬롯의 정보를 기억하도록 하는 변수
    private int pendingItemCount; // 저장하지 못해 대기 중인 아이템의 갯수
    
    // -----UI 변수-----
    [SerializeField]
    private GameObject artifactDuplicateWarningUI;  // 이미 같은 아티팩트를 가지고 있을 때 띄우는 알림창
    [SerializeField]
    private GameObject itemFullWarningUI; // 아티팩트를 저장하지 못했을 때 띄우는 알림창
    [SerializeField]
    private GameObject itemActionMenuUI;    // 아이템 사용, 버리기 메뉴 UI 변수
    [SerializeField]
    private GameObject discardConfirmUI;    // 아이템을 정말 버릴지 묻는 확인 UI 변수

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        itemSlots = itemInventoryBase.GetComponentsInChildren<ItemSlot>();
        pieceSlots = pieceInventoryBase.GetComponentsInChildren<ItemSlot>();
        artifactSlots = artifactInventoryBase.GetComponentsInChildren<ItemSlot>();
    }

    void Update()
    {
        CloseMenuOnOutsideClick();  // 매 프레임마다 마우스의 위치 값을 확인해서 UI 영역 안에 있는지 없는지 확인
        // 마우스 좌표 값을 다른 곳에서 계산하고 그 계산한 값을 이용해서 다른 함수에서도 활용 가능할까?
    }

    public void OnToggleInventory(InputAction.CallbackContext context)  // 인벤토리 창 토글
    {
        if (context.performed)
        {
            inventoryBase.SetActive(!inventoryBase.activeSelf); 
        }
    }

    public void AcquireItem(ItemData _item, int _count = 1)   
    // 인벤토리 내에 있는 아이템과 체스 말이 어떤게 있는지 검사하고 같은 아이템이 있다면 수량을 추가하고
    // 같은 아이템이 없다면 AddItem 함수를 불러와 인벤토리에 아이템을 추가하는 아이템
    {
        if (ItemData.ItemType.Usable == _item.itemType)  // 만약 획득하려는 아이템이 사용 가능한 아이템이라면
        {
            for(int i = 0; i < itemSlots.Length; i++)   // 모든 슬롯 내부의 아이템 정보를 검사
            {
                if (itemSlots[i].item != null && itemSlots[i].item.itemName == _item.itemName) 
                // null 일 때 런타임 에러를 방지 + 만약 인벤토리 내에 같은 아이템이 존재한다면
                {
                    itemSlots[i].SetSlotCount(_count);  // 그 아이템의 수량을 +1 추가한다
                    return;
                } 
            }   // 인벤토리 내에 같은 아이템이 존재하지 않는다면 여기까지 코드가 진행됨
            for (int i = 0; i < itemSlots.Length; i++)  // 모든 슬롯 내부를 검사해서 
            {  
                if (itemSlots[i].item == null)  // 인벤토리 내에 빈 슬롯이 존재한다면 그 슬롯에 아이템을 추가함
                {
                    itemSlots[i].AddItem(_item, _count);
                    return;
                }
            }
        } 
        else if (ItemData.ItemType.Artifact == _item.itemType) // 획득하려는 아이템이 아티팩트라면
        {
            if (HasArtifact(_item)){  // 획득하기 전에 같은 아티팩트를 이미 가지고 있는지 검사 -> 있다면 획득 차단
                artifactDuplicateWarningUI.SetActive(true);
                return;
            } 
            else
            {
                for (int i = 0; i < artifactSlots.Length; i++)  // 빈 슬롯을 찾아서 저장
                { 
                    if (artifactSlots[i].item == null)
                    {
                        artifactSlots[i].AddItem(_item, 1);  // 아티팩트는 항상 1개만 저장
                        return;
                    }
                }
            }
        } 
        else if (ItemData.ItemType.Piece == _item.itemType)
        {
            for (int i = 0; i < pieceSlots.Length; i++)
            {
                if (pieceSlots[i].item != null && pieceSlots[i].item.itemName == _item.itemName) 
                // null 일 때 런타임 에러를 방지 + 만약 인벤토리 내에 같은 체스 말이 존재한다면
                {
                    pieceSlots[i].SetSlotCount(_count); // 인벤토리 내에 있는 체스 말의 갯수를 +1
                    return;
                }
            }

            for (int i = 0; i < pieceSlots.Length; i++)  // 모든 슬롯 내부를 검사해서 
            {  
                if (pieceSlots[i].item == null)  // 인벤토리 내에 빈 슬롯이 존재한다면 그 슬롯에 체스 말을 추가함
                {
                    pieceSlots[i].AddItem(_item, _count);
                    return;
                }
            }
        }

        // 3) 빈 슬롯이 없다면 대기 중인 아이템을 임시 보관하고 교체/버리기 알림창을 띄움
        pendingItem = _item;
        pendingItemCount = 1;
        itemFullWarningUI.SetActive(true);
        return;
    }

    public void DiscardPendingItem()    // 인벤토리에 저장하지 못해 대기 중인 아이템 삭제 함수
    {
        pendingItem = null;
        itemFullWarningUI.SetActive(false);
    }

    public void ReplacePendingItem(int _slotIndex)    
    // _item 값으로 현재 유저가 선택한 아이템의 종류를 확인, _slotIndex 값으로 유저가 선택한 슬롯의 위치 값을 넘겨줌
    // _itemIndex 값으로 현재 선택한 아이템의 갯수를 확인
    // 인벤토리에 저장하지 못한 대기 중인 아티팩트와 인벤토리 내 아이템과 교환하는 함수 -> _slotIndex 값 위치에 있는 아이템과 교환
    {
        if(pendingItem == null)
        {
            return;
        }

        if (_slotIndex < 0 || _slotIndex >= itemSlots.Length) // 슬롯 인덱스의 값을 미리 확인
        {
            Debug.Log("InventoryManager, ReplacePendingItem 함수 _slotIndex 매개변수 문제 발생");
            return; // 슬롯 인덱스 값이 잘못됐다면 함수 실행 종료
        }

        if (pendingItem.itemType == ItemData.ItemType.Usable)
        {
            itemSlots[_slotIndex].AddItem(pendingItem, pendingItemCount);

            pendingItem = null;
            itemFullWarningUI.SetActive(false);
        } 
        else if (pendingItem.itemType == ItemData.ItemType.Artifact)
        {
            artifactSlots[_slotIndex].AddItem(pendingItem, pendingItemCount);

            pendingItem = null;
            itemFullWarningUI.SetActive(false);
        }
        else if (pendingItem.itemType == ItemData.ItemType.Piece)
        {
            pieceSlots[_slotIndex].AddItem(pendingItem, pendingItemCount);

            pendingItem = null;
            itemFullWarningUI.SetActive(false);
        }
    }

    public bool HasArtifact(ItemData _artifact)
    // 아티팩트 인벤토리의 모든 슬롯을 검사해서 같은 아티팩트가 이미 있는지 확인하는 함수
    {
        for (int i = 0; i < artifactSlots.Length; i++)
        {
            if (artifactSlots[i].item != null && artifactSlots[i].item.itemName == _artifact.itemName)
            // null 일 때 런타임 에러를 방지 + 같은 아티팩트가 이미 존재한다면
            {
                return true;
            }
        }
        return false;
    }

    public void CloseArtifactDuplicateWarning()  // 중복 알림창의 닫기 버튼에 연결하는 함수
    {
        artifactDuplicateWarningUI.SetActive(false);
    }

    public void CloseItemFullWarningUI() // 인벤토리가 가득찼을 때 나타나는 경고 UI 창을 끄는 함수
    {
        itemFullWarningUI.SetActive(false);
    }

    public void OpenItemActionMenu(ItemSlot _slot, Vector2 _position)  // 우클릭한 슬롯 위치에 사용, 버리기 메뉴 띄우는 함수
    {
        Debug.Log("슬롯 우클릭 메뉴 열기 확인");
        selectedSlot = _slot;   // 우클릭된 슬롯의 정보를 현재 선택된 슬롯 변수에 정보를 저장한다
        discardConfirmUI.SetActive(false);   // 이전에 버리기 UI 확인창이 남아 있으면 닫음 -> 필요한 코드인지는 의문, 확인해봐야함@@@@@@@@@@@
        itemActionMenuUI.transform.position = _position;  // 사용, 버리기 메뉴가 우클릭했을 때 마우스가 있는 위치에 메뉴 나타나도록 함
        itemActionMenuUI.SetActive(true);
    }

    public void OnClickUse()    // 사용, 버리기 메뉴에서 사용 버튼을 눌렀을 때 아이템이 사용되도록 하는 함수
    {
        Debug.Log("아이템 사용 버튼 눌림");
        // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        // 구현 필요
        // 기존에 GameStage에서 버튼을 눌렀을 때 체스 말이 배치되도록 하는 코드를 붙여넣으면 될 듯
        // 체스 말의 경우 여러 개의 아이템이 들어있을 수 있으므로 사용되면 아이템 카운트가 1씩 줄어들고 0과 같거나 작아지면 빈 슬롯이 되도록 처리
    }

    public void OnClickDiscard()    // 사용, 버리기 메뉴에서 버리기 버튼을 눌렀을 때 아이템을 버리도록 하는 함수
    {
        if (selectedSlot == null || selectedSlot.item == null)  // 우클릭된 슬롯에 정보가 없을 때, 혹은 빈 슬롯을 우클릭 했을 때
        {
            CloseItemActionUI();    // 아무것도 없는 슬롯이기 때문에 그 메뉴를 강제로 닫고 다음 코드를 실행하지 못하게 막음
            return;
        }
        itemActionMenuUI.SetActive(false);  // 아이템 사용, 버리기 메뉴를 안보이게 만듬
        discardConfirmUI.SetActive(true);   // 아이템을 버릴 것인지 확인하는 UI를 안보이게 만듬
    }

    public void OnClickDiscardYes()     // 아이템을 버릴 것인지 확인하는 UI 창에서 "예"를 선택했을 때 실행되는 함수
    {
        if (selectedSlot != null && selectedSlot.item != null)  // 만약 우클릭된 슬롯에 아이템이 있거나, 슬롯에 아이템 정보가 들어있을 때
        {
            selectedSlot.ClearSlot(); // 선택된 슬롯의 아이템을 버린다
        }
        CloseItemActionUI();    // 그 후 확인 UI 창을 닫음
    }

    public void OnClickDiscardNo()      // 아이템을 버릴 것인지 확인 UI 창에서 "아니요" 를 선택했을 때 실행되는 함수
    {
        CloseItemActionUI();    // 확인 UI 창을 안보이게 만듬
    }

    private void CloseItemActionUI()    // 확인 UI 창을 닫으면서 그와 관련된 정보들을 초기화시키는 함수
    {
        itemActionMenuUI.SetActive(false);  // 아이템 사용, 버리기 메뉴를 안보이게 만듬
        discardConfirmUI.SetActive(false);  // 아이템 버리기 확인 UI 창을 안보이게 만듬
        selectedSlot = null;    // 우클릭으로 선택된 슬롯의 정보를 빈 슬롯으로 초기화
    }

    private void CloseMenuOnOutsideClick()
    // 아이템 사용, 버리기 메뉴가 떠 있을 때 메뉴 바깥을 클릭 혹은 우클릭하면 메뉴를 닫는 함수
    {
        if (!itemActionMenuUI.activeSelf) { // 만약 메뉴가 꺼져 있으면 검사하지 않음
            return;
        }   

        Mouse mouse = Mouse.current;    // 마우스 정보를 가져와서 변수에 저장

        if (mouse == null) {    // 마우스가 없는 환경이면 처리하지 않음 -> 추후 모바일 대응 시 수정이 필요함@@@@@@@@@@
            return;                  
        }

        if (!mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.wasPressedThisFrame)
        // 이번 프레임에 마우스 좌클릭 혹은 우클릭을 누르지 않았다면 함수 종료
        // wasPressedThisFrame -> 마우스를 누른 그 순간의 프레임에만 true를 반환
        {
            return;                                 
        }
            

        Vector2 mousePos = mouse.position.ReadValue();  // 마우스의 위치 값을 가져와 mousePos 변수에 저장
        RectTransform menuRect = (RectTransform)itemActionMenuUI.transform;
        // itemActionMenuUI(아이템 사용, 버리기 메뉴)의 RectTransform(UI의 너비, 높이, 앵커 피벗 값이 저장됨) 정보를 가져와서
        // 그 아이템 사용, 버리기 메뉴가 있는 사각형 영역을 계산해서 그 값을 변수에 저장

        Canvas canvas = itemActionMenuUI.GetComponentInParent<Canvas>().rootCanvas;
        // 아이템 사용, 버리기 메뉴 오브젝트에서 부모 방향으로 올라가며 처음 만나는 Canvas를 찾음
        // .rootCanvas는 Canvas 안에 또 Canvas가 중첩되어 있을 때 가장 바깥의 Canvas를 돌려줌
        //  화면에 그리는 방식(Render Mode)은 최상위 Canvas가 결정하기 때문

        Camera cam;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) // 카메라의 렌더 모드가 ScreenSpaceOverlay 면 실행
        // ScreenSpaceOverlay -> 카메라와 상관없이 화면 위에 바로 덮어 그림, UI 좌표가 곧 픽셀 좌표 -> 카메라가 필요 없음 (null)
        {
            cam = null;                 // Overlay 모드면 카메라 없음
        }
        else
        {
            cam = canvas.worldCamera;   // 그 외에는 Canvas에 지정된 카메라
        }

        // Overlay 모드는 카메라 없이, Camera 모드는 Canvas의 카메라를 기준으로 좌표를 계산

        if (RectTransformUtility.RectangleContainsScreenPoint(menuRect, mousePos, cam)) {
        // 클릭 위치의 좌표가 아이템 사용, 버리기 메뉴 UI 사각형 영역 안에 있는지 확인 -> 있다면 true, 없다면 false 반환
            return;   // 메뉴 안(사용/버리기 버튼)을 눌렀다면 닫지 않음 -> 버튼이 정상 동작
        }

        CloseItemActionUI();   // 메뉴 바깥을 눌렀다면 메뉴를 닫음 (기존 함수 재사용)
    }
}
