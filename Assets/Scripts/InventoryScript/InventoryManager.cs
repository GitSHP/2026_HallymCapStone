using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour   // 인벤토리의 최고 부모 클래스 - 모든 InventorySlot의 부모
    // 전체적인 인벤토리를 관리하는 스크립트
    // -> 기물·유물은 슬롯이 직접 저장하지 않고, 런 장부(RunState)에 저장된 내용을 슬롯에 그려서 보여 준다
    //    (슬롯은 화면일 뿐이라 씬이 바뀌어 인벤토리가 새로 만들어져도 장부를 다시 읽어 그대로 복원됨)
    // -> 전투 중에는 기물 슬롯이 소환 UI 역할도 한다: 슬롯을 좌클릭(또는 우클릭 메뉴의 "사용")하면 그 기물을 소환할 기물로 고름
    // -> 아이템(Usable)은 아직 장부가 없어서 예전처럼 슬롯이 직접 저장한다
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

    private RunState run;   // 런 장부 -> 보유 기물(Deck)·유물(Artifacts)이 저장된 곳, 씬이 바뀌어도 유지됨
    private DeployController deploy;    // 전투 씬의 Board 오브젝트에 붙은 소환 담당 컴포넌트 (전투 씬이 아니면 null)

    // -----UI 변수-----
    [SerializeField]
    private GameObject artifactDuplicateWarningUI;  // 이미 같은 아티팩트를 가지고 있을 때 띄우는 알림창
    [SerializeField]
    private GameObject itemFullWarningUI; // 아이템이 가득 찼을 때 띄우는 알림창
    [SerializeField]
    private GameObject itemActionMenuUI;    // 아이템 사용, 버리기 메뉴 UI 변수
    [SerializeField]
    private GameObject discardConfirmUI;    // 아이템을 정말 버릴지 묻는 확인 UI 변수
        [SerializeField]
    private GameObject rewardFullConfirmUI; // 보상 기물을 받을 자리가 없을 때 "버리고 받으시겠습니까?"를 묻는 예/아니요 창
    [SerializeField]
    private TextMeshProUGUI rewardFullConfirmText;  // 위 창의 안내 문구 (비워 두면 문구를 바꾸지 않음)
    [SerializeField]
    private GameObject rewardPendingNoticeUI;   // "예"를 누른 뒤 자리가 날 때까지 띄워 둘 안내 표시 (비워 둬도 동작함)

    private readonly List<PieceDefinition> askingRewardPieces = new List<PieceDefinition>();
    // 받을지 묻는 중인 보상 기물 -> "예"면 pendingRewardPieces로 옮기고, "아니요"면 버림
    private readonly List<PieceDefinition> pendingRewardPieces = new List<PieceDefinition>();
    // "예"를 눌러 임시 저장된 보상 기물 -> 빈 슬롯이 생기면 자동으로 인벤토리에 들어감
    private bool isGrantingRewards; // 보상을 넣는 중인지 여부 -> 넣기 → InventoryChanged → Refresh → 넣기… 로 다시 들어오는 것을 막음
    private bool isBlockingBoardForReward;  // 확인창 때문에 보드 입력을 막고 있는지 여부 -> InputGate.Push/Pop 횟수를 맞추기 위함

    void Start()    // 오브젝트가 생성될 때 한 번 실행 -> 슬롯을 모으고, 장부·소환과 연결한 뒤 처음 그림
    {
        itemSlots = itemInventoryBase.GetComponentsInChildren<ItemSlot>(true);
        pieceSlots = pieceInventoryBase.GetComponentsInChildren<ItemSlot>(true);
        artifactSlots = artifactInventoryBase.GetComponentsInChildren<ItemSlot>(true);
        // true -> 인벤토리 창이 꺼져 있어도 슬롯을 찾음

        run = RunState.Instance;
        run.InventoryChanged += Refresh;    // 장부가 바뀌면(구매, 잡힘, 버리기) 다시 그림

        deploy = FindAnyObjectByType<DeployController>();   // 전투 씬이면 Board 오브젝트의 DeployController를 찾음
        if (deploy != null)
        {
            deploy.DeckChanged += Refresh;  // 덱 카드·선택이 바뀌면 다시 그림 (킹 배치, 선택 표시 등)
            deploy.OwnedPieceDeployed += Refresh;   // 보유 기물을 소환하면 다시 그림 -> 남은 개수가 하나 줄어 보임
        }

        Refresh();
    }

    void OnDestroy()    // 오브젝트가 파괴될 때(씬 이동 등) 이벤트 연결을 끊음 -> 사라진 오브젝트를 RunState가 계속 부르지 않도록
    {
        if (run != null)
            run.InventoryChanged -= Refresh;
        if (deploy != null)
        {
            deploy.DeckChanged -= Refresh;
            deploy.OwnedPieceDeployed -= Refresh;
        }

        if (isBlockingBoardForReward)   // ← 추가: 확인창이 열린 채로 씬이 바뀌었다면 막아 둔 보드 입력을 풀어 줌
        {
            InputGate.Pop();
            isBlockingBoardForReward = false;
        }
    }

    void Update()
    {
        CloseMenuOnOutsideClick();  // 매 프레임마다 마우스의 위치 값을 확인해서 UI 영역 안에 있는지 없는지 확인
    }

    public void OnToggleInventory(InputAction.CallbackContext context)  // 인벤토리 창 토글
    {
        if (context.performed)
        {
            inventoryBase.SetActive(!inventoryBase.activeSelf);
        }
    }

    // ---------- 장부 → 슬롯 그리기 ----------

    public void Refresh()   // 장부(RunState)와 소환 상태(DeployController)를 읽어 기물·유물 슬롯을 다시 그리는 함수
    {
        if (pieceSlots == null) // 아직 Start 전이라면 그릴 슬롯이 없음
            return;

        RefreshPieces();
        RefreshArtifacts();

        TryGrantPendingRewards();   // 슬롯 상황이 바뀌었으니, 임시 저장된 보상 기물이 들어갈 자리가 생겼는지 확인
    }

    private void RefreshPieces()    // 기물 슬롯을 그리는 함수
    // 순서: ① 스테이지가 주는 임시 기물(킹, 상시 폰 등 -> DeployController 덱) ② 런에서 모은 보유 기물(RunState 장부)
    {
        int slot = 0;   // 다음에 채울 슬롯 번호

        if (deploy != null) // ① 전투 씬이라면 덱 카드(임시 기물)를 먼저 채움
        {
            var deck = deploy.Deck;
            var shown = new List<PieceDefinition>();   // 이미 슬롯에 그린 (기물, 무제한 여부) 조합을 기억 -> 같은 카드는 한 슬롯에 묶음
            var shownUnlimited = new List<bool>();
            for (int i = 0; i < deck.Count && slot < pieceSlots.Length; i++)
            {
                bool unlimited = deploy.IsUnlimited(i);
                if (IndexOfPair(shown, shownUnlimited, deck[i], unlimited) >= 0)  // 이미 그린 카드라면 건너뜀(개수는 아래에서 셈)
                    continue;
                shown.Add(deck[i]);
                shownUnlimited.Add(unlimited);

                int count = 0;  // 덱에 같은 카드가 몇 장인지 셈
                for (int j = 0; j < deck.Count; j++)
                    if (deck[j] == deck[i] && deploy.IsUnlimited(j) == unlimited)
                        count++;

                var target = pieceSlots[slot++];
                target.AddItem(deck[i], count);
                target.SetPieceSource(false, i);    // 임시 기물 표시 + 덱 번호(i) 기억 -> 사용할 때 deploy.Select(i)로 고름
                target.SetCountText(unlimited ? "∞" : count.ToString());   // 상시 카드는 개수 대신 ∞ 표시
                target.SetHighlight(deploy.SelectedOwned == null && deploy.Selected == deck[i]);   // 지금 고른 카드라면 강조
            }
        }

        var owned = new List<PieceDefinition>();   // ② 장부의 보유 기물을 종류별로 한 번씩만 모음
        foreach (var piece in run.Deck)
            if (piece != null && !owned.Contains(piece))
                owned.Add(piece);

        foreach (var piece in owned)
        {
            if (slot >= pieceSlots.Length)  // 슬롯이 모자라면 나머지는 그리지 않음
            {
                Debug.LogWarning("[InventoryManager] 기물 슬롯이 부족해 일부 기물을 표시하지 못했습니다.");
                break;
            }

            int count = deploy != null ? deploy.AvailableOwned(piece) : run.CountOf(piece);
            // 전투 중에는 "보유 개수 - 보드에 나가 있는 개수", 전투가 아니면 보유 개수

            var target = pieceSlots[slot++];
            target.AddItem(piece, count);
            target.SetPieceSource(true, -1);    // 보유 기물 표시 -> 사용할 때 deploy.SelectOwned(기물)로 고름
            target.SetCountText(count.ToString());
            target.SetHighlight(deploy != null && deploy.SelectedOwned == piece);
        }

        for (; slot < pieceSlots.Length; slot++)    // 남은 슬롯은 비움
        {
            pieceSlots[slot].ClearSlot();
            pieceSlots[slot].SetHighlight(false);
        }
    }

    private static int IndexOfPair(List<PieceDefinition> defs, List<bool> flags, PieceDefinition def, bool flag)
    // (기물, 무제한 여부) 조합이 목록에 몇 번째로 있는지 찾는 함수 -> 없으면 -1
    {
        for (int i = 0; i < defs.Count; i++)
            if (defs[i] == def && flags[i] == flag)
                return i;
        return -1;
    }

    private void RefreshArtifacts() // 유물 슬롯을 장부의 유물 목록으로 그리는 함수
    {
        var artifacts = run.Artifacts;
        for (int i = 0; i < artifactSlots.Length; i++)
        {
            if (i < artifacts.Count)
                artifactSlots[i].AddItem(artifacts[i], 1);  // 유물은 항상 1개
            else
                artifactSlots[i].ClearSlot();
        }
    }

    // ---------- 사용 ----------

    public void UseSlot(ItemSlot _slot)  // 슬롯의 아이템을 사용하는 함수 -> 슬롯 좌클릭, 우클릭 메뉴의 "사용" 버튼이 모두 여기로 옴
    {
        if (_slot == null || _slot.item == null)
            return;

        var piece = _slot.item as PieceDefinition;  // 기물이라면 PieceDefinition으로 바꿈 (PieceDefinition은 ItemData를 상속함)
        if (piece != null)
        {
            if (deploy == null) // 전투 씬이 아니면 소환할 수 없음
                return;

            if (_slot.IsOwnedPiece)  // 런에서 모은 보유 기물이라면
                deploy.SelectOwned(piece);  // 보유 기물로 고름 -> 소환하면 Piece.Owned가 켜짐
            else    // 킹·상시 폰 같은 임시 기물이라면
                deploy.Select(_slot.DeckIndex);   // 덱 번호로 고름 (HUD 카드 버튼과 같은 방식)
            // -> 이후 보드 칸을 클릭하면 DeployController.TryDeployAt이 AP 차감·생성을 처리
            return;
        }

        Debug.Log("[InventoryManager] 아이템·유물 사용 효과는 아직 연결되지 않았습니다: " + _slot.item.itemName);
    }

    public void AcquireItem(ItemData _item, int _count = 1)
    // 아이템을 얻는 함수 -> 기물·유물은 런 장부(RunState)에 기록하고, 아이템(Usable)은 슬롯에 직접 저장
    {
        if (_item == null)
            return;

        if (ItemData.ItemType.Usable == _item.itemType)  // 만약 획득하려는 아이템이 사용 가능한 아이템이라면
        {
            for (int i = 0; i < itemSlots.Length; i++)   // 모든 슬롯 내부의 아이템 정보를 검사
            {
                if (itemSlots[i].item == _item)   // 인벤토리 내에 같은 아이템이 존재한다면
                {
                    itemSlots[i].SetSlotCount(_count);  // 그 아이템의 수량을 추가한다
                    return;
                }
            }
            for (int i = 0; i < itemSlots.Length; i++)  // 같은 아이템이 없다면 빈 슬롯을 찾아서
            {
                if (itemSlots[i].item == null)
                {
                    itemSlots[i].AddItem(_item, _count);    // 그 슬롯에 아이템을 추가함
                    return;
                }
            }
        }
        else if (ItemData.ItemType.Artifact == _item.itemType) // 획득하려는 아이템이 아티팩트라면
        {
            if (run.Owns(_item))  // 같은 아티팩트를 이미 가지고 있다면 -> 획득 차단
            {
                artifactDuplicateWarningUI.SetActive(true);
                return;
            }
            if (run.Artifacts.Count < artifactSlots.Length) // 빈 슬롯이 있다면 장부에 기록 -> InventoryChanged로 슬롯이 다시 그려짐
            {
                run.AddArtifact(_item);
                return;
            }
        }
        else if (ItemData.ItemType.Piece == _item.itemType)    // 획득하려는 아이템이 체스 말이라면
        {
            var piece = _item as PieceDefinition;
            if (piece == null)  // 기물 유형인데 기물 데이터(PieceDefinition)가 아니라면 소환할 수 없으므로 거부
            {
                Debug.LogWarning("[InventoryManager] 기물은 PieceDefinition 에셋이어야 합니다: " + _item.name);
                return;
            }
            for (int i = 0; i < _count; i++)    // 개수만큼 장부에 기록 -> 같은 기물은 한 슬롯에 개수로 묶여 그려짐
                run.AddPiece(piece);
            return;
        }

        // 빈 슬롯이 없다면 대기 중인 아이템을 임시 보관하고 교체/버리기 알림창을 띄움
        pendingItem = _item;
        pendingItemCount = _count;
        itemFullWarningUI.SetActive(true);
    }

    public void DiscardPendingItem()    // 인벤토리에 저장하지 못해 대기 중인 아이템 삭제 함수
    {
        pendingItem = null;
        itemFullWarningUI.SetActive(false);
    }

    // public void ReplacePendingItem(int _slotIndex)
    // // 인벤토리에 저장하지 못한 대기 중인 아이템을 _slotIndex 위치의 아이템과 교환하는 함수
    // {
    //     if (pendingItem == null)
    //         return;

    //     if (pendingItem.itemType == ItemData.ItemType.Usable)
    //     {
    //         if (_slotIndex < 0 || _slotIndex >= itemSlots.Length)  // 슬롯 인덱스의 값을 미리 확인
    //         {
    //             Debug.Log("InventoryManager, ReplacePendingItem 함수 _slotIndex 매개변수 문제 발생");
    //             return;
    //         }
    //         itemSlots[_slotIndex].AddItem(pendingItem, pendingItemCount);
    //     }
    //     else if (pendingItem.itemType == ItemData.ItemType.Artifact)
    //     {
    //         if (_slotIndex < 0 || _slotIndex >= run.Artifacts.Count)
    //         {
    //             Debug.Log("InventoryManager, ReplacePendingItem 함수 _slotIndex 매개변수 문제 발생");
    //             return;
    //         }
    //         run.RemoveArtifact(run.Artifacts[_slotIndex]);  // 그 자리의 유물을 장부에서 빼고
    //         run.AddArtifact(pendingItem);   // 대기 중이던 유물을 넣음
    //     }

    //     pendingItem = null;
    //     itemFullWarningUI.SetActive(false);
    // }    -- 인벤토리 창이 꽉 차 저장하지 못한 아이템이 있다면 이를 임시로 저장하고 사용자의 선택에 따라 버릴지 교환할 지를 선택하는 함수
    //      -- 였는데 아이템이 꽉차면 꽉찼다는 UI만 띄우고 사용자가 알아서 아이템을 버리고 정리하도록 하는 쪽으로 변경해 사용하지는 않음

    public bool HasArtifact(ItemData _artifact) => run.Owns(_artifact);
    // 같은 아티팩트를 이미 가지고 있는지 확인하는 함수 -> 장부(RunState)에서 확인

    public void CloseArtifactDuplicateWarning()  // 중복 알림창의 닫기 버튼에 연결하는 함수
    {
        artifactDuplicateWarningUI.SetActive(false);
    }

    public void CloseItemFullWarningUI() // 인벤토리가 가득찼을 때 나타나는 경고 UI 창을 끄는 함수
    {
        itemFullWarningUI.SetActive(false);
    }

    // ---------- 우클릭 메뉴 (사용 / 버리기) ----------

    public void OpenItemActionMenu(ItemSlot _slot, Vector2 _position)  // 우클릭한 슬롯 위치에 사용, 버리기 메뉴 띄우는 함수
    {
        selectedSlot = _slot;   // 우클릭된 슬롯의 정보를 현재 선택된 슬롯 변수에 정보를 저장한다
        discardConfirmUI.SetActive(false);   // 이전 버리기 확인창이 남아 있으면 닫음
        itemActionMenuUI.transform.position = _position;  // 우클릭했을 때 마우스가 있는 위치에 메뉴가 나타나도록 함
        itemActionMenuUI.SetActive(true);
    }

    public void OnClickUse()    // 사용, 버리기 메뉴에서 사용 버튼을 눌렀을 때 실행되는 함수
    {
        var slot = selectedSlot;
        CloseItemActionUI();    // 메뉴를 먼저 닫고
        UseSlot(slot);          // 좌클릭과 같은 방식으로 사용 -> 기물이면 소환할 기물로 고름
    }

    public void OnClickDiscard()    // 사용, 버리기 메뉴에서 버리기 버튼을 눌렀을 때 실행되는 함수
    {
        if (selectedSlot == null || selectedSlot.item == null || !CanDiscard(selectedSlot))
        // 빈 슬롯이거나 버릴 수 없는 아이템(킹·상시 폰, 모두 보드에 나가 있는 기물)이라면
        {
            CloseItemActionUI();    // 메뉴를 닫고 다음 코드를 실행하지 못하게 막음
            return;
        }
        itemActionMenuUI.SetActive(false);  // 아이템 사용, 버리기 메뉴를 안보이게 만들고
        discardConfirmUI.SetActive(true);   // 아이템을 버릴 것인지 확인하는 UI를 보이게 만듬
    }

    private bool CanDiscard(ItemSlot _slot)  // 이 슬롯의 아이템을 버릴 수 있는지 검사하는 함수
    {
        var piece = _slot.item as PieceDefinition;
        if (piece == null)  // 기물이 아니면(유물·아이템) 버릴 수 있음
            return true;
        if (!_slot.IsOwnedPiece)   // 킹·상시 폰 같은 임시 기물은 버릴 수 없음
            return false;
        return deploy == null || deploy.AvailableOwned(piece) > 0;  // 보드에 나가 있지 않은 것이 남아 있어야 버릴 수 있음
    }

    public void OnClickDiscardYes()     // 아이템을 버릴 것인지 확인하는 UI 창에서 "예"를 선택했을 때 실행되는 함수
    {
        if (selectedSlot != null && selectedSlot.item != null)  // 우클릭된 슬롯에 아이템이 있다면
        {
            var item = selectedSlot.item;
            if (item is PieceDefinition piece)  // 기물이라면 장부에서 하나 지움 -> InventoryChanged로 슬롯이 다시 그려짐
                run.RemovePiece(piece);
            else if (item.itemType == ItemData.ItemType.Artifact)   // 유물이라면 장부에서 지움
                run.RemoveArtifact(item);
            else    // 아이템(Usable)이라면 슬롯에서 바로 비움
                selectedSlot.ClearSlot();
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

        if (mouse == null) {    // 마우스가 없는 환경이면 처리하지 않음 -> 추후 모바일 대응 시 수정이 필요함
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
        // itemActionMenuUI(아이템 사용, 버리기 메뉴)의 RectTransform(UI의 너비, 높이, 앵커 피벗 값이 저장됨) 정보를 가져옴

        Canvas canvas = itemActionMenuUI.GetComponentInParent<Canvas>().rootCanvas;
        // 메뉴 오브젝트에서 부모 방향으로 올라가며 처음 만나는 Canvas를 찾고, 중첩되어 있다면 가장 바깥 Canvas를 사용
        // -> 화면에 그리는 방식(Render Mode)은 최상위 Canvas가 결정하기 때문

        Camera cam;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) // ScreenSpaceOverlay -> UI 좌표가 곧 픽셀 좌표라 카메라가 필요 없음
        {
            cam = null;
        }
        else
        {
            cam = canvas.worldCamera;   // 그 외에는 Canvas에 지정된 카메라
        }

        if (RectTransformUtility.RectangleContainsScreenPoint(menuRect, mousePos, cam)) {
        // 클릭 위치가 메뉴 사각형 영역 안에 있는지 확인 -> 있다면 true
            return;   // 메뉴 안(사용/버리기 버튼)을 눌렀다면 닫지 않음 -> 버튼이 정상 동작
        }

        CloseItemActionUI();   // 메뉴 바깥을 눌렀다면 메뉴를 닫음
    }

    // ---------- 보상 기물 받기 (가득 찼을 때 예/아니요) ----------

    public void ReceiveRewardPiece(PieceDefinition _piece)  // 아티팩트 효과가 보상 기물을 줄 때 호출하는 입구
    {
        if (_piece == null)
        {
            return;
        }

        if (HasRoomForPiece(_piece))    // 빈 슬롯이 있다면(또는 같은 기물 슬롯이 이미 있다면) 바로 저장
        {
            run.AddPiece(_piece);   // 보유 기물 장부에 1개 추가 -> InventoryChanged → Refresh로 슬롯에 나타남
            return;
        }

        askingRewardPieces.Add(_piece); // 자리가 없다면 일단 "묻는 중" 목록에 보관하고
        ShowRewardFullConfirm();    // 버리고 받을지 묻는 창을 띄움
    }

    private void ShowRewardFullConfirm()    // "가득 찼습니다. 버리고 받으시겠습니까?" 창을 띄우는 함수
    {
        // if (rewardFullConfirmText != null)  // 문구 텍스트가 연결되어 있다면 받을 기물 이름을 넣어 안내
        // {
        //     string pieceName = askingRewardPieces[askingRewardPieces.Count - 1].itemName;
        //     rewardFullConfirmText.text = "기물 인벤토리가 가득 찼습니다.\n"
        //         + "기물을 버려 자리를 만들고 " + pieceName + "을(를) 받으시겠습니까?\n"
        //         + "(아니요를 누르면 " + pieceName + "은(는) 사라집니다)";
        // }

        rewardFullConfirmUI.SetActive(true);
        SetBoardBlockedForReward(true); // 창이 떠 있는 동안 보드 클릭을 막음
    }

    public void OnClickRewardYes()  // 확인창의 "예" 버튼에 연결하는 함수
    {
        Debug.Log("OnClickRewardYes 버튼 클릭 감지");
        for (int i = 0; i < askingRewardPieces.Count; i++)  // 묻는 중이던 보상을 모두 임시 저장 목록으로 옮김
        {
            pendingRewardPieces.Add(askingRewardPieces[i]);
        }
        askingRewardPieces.Clear();

        CloseRewardFullConfirm();   // 창을 닫고
        inventoryBase.SetActive(true);  // 버릴 기물을 고를 수 있게 인벤토리 창을 열어 줌

        TryGrantPendingRewards();   // 창이 떠 있던 사이에 이미 자리가 났을 수도 있으므로 한 번 확인
        UpdateRewardPendingNotice();    // 아직 못 넣었다면 대기 안내를 띄움
    }

    public void OnClickRewardNo()   // 확인창의 "아니요" 버튼에 연결하는 함수
    {
        Debug.Log("OnClickRewardNo 버튼 클릭 감지");
        for (int i = 0; i < askingRewardPieces.Count; i++)
        {
            Debug.Log("[InventoryManager] 보상 기물을 받지 않았습니다: " + askingRewardPieces[i].itemName);
        }
        askingRewardPieces.Clear(); // 묻는 중이던 보상을 저장하지 않고 버림

        CloseRewardFullConfirm();   // 창만 닫음
    }

    private void CloseRewardFullConfirm()   // 확인창을 닫고 보드 입력을 다시 풀어 주는 함수
    {
        rewardFullConfirmUI.SetActive(false);
        SetBoardBlockedForReward(false);
    }

    private void SetBoardBlockedForReward(bool _block)  // 보드 입력을 막거나 푸는 함수 -> Push/Pop을 꼭 한 번씩만 하도록 플래그로 관리
    {
        if (_block)
        {
            if (!isBlockingBoardForReward)  // 아직 막지 않았을 때만 막음
            {
                InputGate.Push();
                isBlockingBoardForReward = true;
            }
        }
        else
        {
            if (isBlockingBoardForReward)   // 막아 둔 상태일 때만 풂
            {
                InputGate.Pop();
                isBlockingBoardForReward = false;
            }
        }
    }

   private void TryGrantPendingRewards()   // 임시 저장된 보상 기물을 들어갈 자리가 있는 만큼 인벤토리에 넣는 함수
    {
        if (isGrantingRewards)  // 이미 넣는 중이라면 (AddPiece → InventoryChanged → Refresh → 이 함수) 중복 실행하지 않음
        {
            return;
        }
        if (pendingRewardPieces.Count == 0) // 기다리는 보상이 없다면 종료
        {
            return;
        }

        isGrantingRewards = true;
        while (pendingRewardPieces.Count > 0)   // 현재 보상으로 받기 위해 대기 중인 아이템이 있다면 실행
        {
            PieceDefinition next = pendingRewardPieces[0];  // 가장 먼저 대기 중인 아이템부터 시작
            if (!HasRoomForPiece(next)) // 자리가 없다면 다음 기회(다음 Refresh)를 기다림
            {
                break;
            }
            pendingRewardPieces.RemoveAt(0);    // 목록에서 먼저 빼고
            run.AddPiece(next); // 보유 기물 장부에 1개 추가
        }
        isGrantingRewards = false;

        UpdateRewardPendingNotice();    // 다 넣었다면 대기 안내를 숨김
    }

    private void UpdateRewardPendingNotice()    // 임시 저장된 보상이 있으면 안내를 보이고, 없으면 숨기는 함수
    {
        if (rewardPendingNoticeUI == null)  // 안내 UI를 연결하지 않았다면 아무것도 하지 않음
        {
            return;
        }

        bool hasPending = pendingRewardPieces.Count > 0;
        rewardPendingNoticeUI.SetActive(hasPending);
    }

    private bool HasRoomForPiece(PieceDefinition _piece)    // 기물을 인벤토리에 추가하려고 할 때 빈 슬롯이 있는지 확인하는 함수
    {
        if (run.CountOf(_piece) > 0)    // 같은 기물을 이미 보유 중이라면 그 슬롯에 1개가 추가되므로 (퀸 ×2) 새 자리가 필요 없음
        {
            return true;
        }
        return CountUsedPieceSlots() < pieceSlots.Length;   // 처음 받는 기물이라면 빈 슬롯이 하나라도 있어야 함
    }

    private int CountUsedPieceSlots()   // 지금 사용되고 있는 슬롯이 몇 슬롯이 있는지 확인하는 함수 -> RefreshPieces와 같은 규칙으로 묶어서 셈
    {
        int used = 0;

        if (deploy != null) // 전투 씬이라면 덱 카드(킹, 상시 폰 등): (기물, 무제한 여부)가 같으면 한 칸
        {
            List<PieceDefinition> shown = new List<PieceDefinition>();
            List<bool> shownUnlimited = new List<bool>();
            for (int i = 0; i < deploy.Deck.Count; i++)
            {
                bool unlimited = deploy.IsUnlimited(i);
                if (IndexOfPair(shown, shownUnlimited, deploy.Deck[i], unlimited) >= 0)
                {
                    continue;
                }
                shown.Add(deploy.Deck[i]);
                shownUnlimited.Add(unlimited);
                used++;
            }
        }

        List<PieceDefinition> owned = new List<PieceDefinition>();  // 보유 기물: 종류가 같으면 한 칸
        foreach (PieceDefinition piece in run.Deck)
        {
            if (piece != null && !owned.Contains(piece))
            {
                owned.Add(piece);
            }
        }
        used += owned.Count;

        return used;
    }
}
