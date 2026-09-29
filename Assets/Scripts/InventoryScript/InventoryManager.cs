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

    private ItemSlot[] itemSlots;   // 아이템에 대한 정보가 들어있는 itemSlots 배열
    private PieceSlot[] pieceSlots; //  체스 말에 대한 정보가 들어있는 pieceSlots 배열
    private ArtifactSlot[] artifactSlots; // 아티팩트에 대한 정보가 들어있는 artifactSlots 배열
    private ItemData pendingArtifact;   // 저장하지 못해 대기 중인 아티팩트

    [SerializeField]
    private GameObject artifactFullWarningUI; // 아티팩트를 저장하지 못했을 때 띄우는 알림창

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        itemSlots = itemInventoryBase.GetComponentsInChildren<ItemSlot>();
        pieceSlots = pieceInventoryBase.GetComponentsInChildren<PieceSlot>();
        artifactSlots = artifactInventoryBase.GetComponentsInChildren<ArtifactSlot>();
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
            }
        } 
    }

    public void AcquirePiece(ItemData _piece, int _count = 1)
    // 인벤토리 내에 있는 체스 말이 어떤게 있는지 검사하고 수량을 추가하는 함수
    {
        if (ItemData.ItemType.Piece == _piece.itemType)
        {
            for (int i = 0; i < pieceSlots.Length; i++)
            {
                if (pieceSlots[i].piece != null && pieceSlots[i].piece.itemName == _piece.itemName) 
                // null 일 때 런타임 에러를 방지 + 만약 인벤토리 내에 같은 체스 말이 존재한다면
                {
                    pieceSlots[i].SetSlotCount(_count);
                    return;
                }
            }
        }
    }
    public void DiscardPendingArtifact()    // 인벤토리에 저장하지 못해 대기 중인 아티팩트 삭제 함수
    {
        pendingArtifact = null;
        artifactFullWarningUI.SetActive(false);
    }

    public void ReplacePendingArtifact(int _slotIndex)    // _slotIndex 값으로 유저가 선택한 슬롯의 위치 값을 넘겨줌
    // 인벤토리에 저장하지 못한 대기 중인 아티팩트와 인벤토리 내 아이템과 교환하는 함수 -> _slotIndex 값 위치에 있는 아이템과 교환
    {
        if(pendingArtifact == null)
        {
            return;
        }

        if (_slotIndex < 0 || _slotIndex >= pieceSlots.Length) {
            return;
        }
        artifactSlots[_slotIndex].AddArtifact(pendingArtifact);

        pendingArtifact = null;
        artifactFullWarningUI.SetActive(false);
    }
}
