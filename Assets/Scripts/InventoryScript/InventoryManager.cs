using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour   // 인벤토리의 최고 부모 클래스 - 모든 InventorySlot의 부모
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
    private int pendingItemCount; // 저장하지 못해 대기 중인 아이템의 갯수
    
    // -----UI 변수-----
    [SerializeField]
    private GameObject artifactDuplicateWarningUI;  // 이미 같은 아티팩트를 가지고 있을 때 띄우는 알림창
    [SerializeField]
    private GameObject itemFullWarningUI; // 아티팩트를 저장하지 못했을 때 띄우는 알림창

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        itemSlots = itemInventoryBase.GetComponentsInChildren<ItemSlot>();
        pieceSlots = pieceInventoryBase.GetComponentsInChildren<ItemSlot>();
        artifactSlots = artifactInventoryBase.GetComponentsInChildren<ItemSlot>();
    }

    public void OnToggleInventory(InputAction.CallbackContext context)  // 인벤토리 창 토글
    {
        if (context.performed)
        {
            inventoryBase.SetActive(!inventoryBase.activeSelf); 
        }
    }

    public void AcquireItem(ItemData _item, int _count = 1)   
    // 인벤토리 내에 있는 아이템과 체스 말이 어떤게 있는지 검사하고 수량을 추가하는 함수
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
}
