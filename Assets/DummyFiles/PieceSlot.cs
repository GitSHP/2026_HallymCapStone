using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PieceSlot : Slot
{
    public ItemData piece; // 인벤토리에 저장할 아이템
    public int pieceCount; // 저장된 아이템의 갯수
    public Image pieceImage; // 저장된 아이템의 이미지

    [SerializeField]
    private TextMeshProUGUI pieceTextCount; // 아이템의 갯수를 표시하는 텍스트 UI에 접근하기 위한 변수
    [SerializeField]
    private GameObject pieceCountImage;  // 아이템의 갯수 이미지를 표시하는 Image UI에 접근하기 위한 변수

    private void SetImageAlpha(float _alpha) // 아이템 이미지의 투명도 조절 
    // -> 아이템이 저장된 경우 아이템 이미지의 투명도를 높여 이미지가 보이도록 조절하고 저장되지 않은 상태면 투명도를 낮춰 보이지 않게함
    {
        Color color = pieceImage.color;
        color.a = _alpha;
        pieceImage.color = color;
    }

    public void AddPiece(ItemData _piece, int _count) // 인벤토리에 아이템 새로운 아이템을 추가
    {
        piece = _piece;
        pieceCount = _count;
        pieceImage.sprite = piece.itemImage;

        if(piece.itemType == ItemData.ItemType.Piece) // 만약 인벤토리에 저장할 아이템이 체스 말이라면
        {
            pieceCountImage.SetActive(true); // 저장한 아이템의 갯수 이미지 UI를 보이도록 활성화하고
            pieceTextCount.text = pieceCount.ToString();  // 현재 저장된 아이템의 갯수가 보이도록 텍스트 UI로 현재 저장된 아이템 갯수를 표시함
        } 

        SetImageAlpha(1);
    }

    public void SetSlotCount(int _count)    // 현재 슬롯의 아이템 갯수를 업데이트해주는 함수
    {
        pieceCount += _count;    // 현재 슬롯에 저장된 아이템의 갯수를 매개변수(_count)의 크기만큼 증가
        pieceTextCount.text = pieceCount.ToString();  // 아이템 갯수를 표시하는 텍스트 UI를 현재 갯수에 맞게 초기화

        if(pieceCount <= 0)  // 만약 아이템의 갯수가 0이라면 -> 현재 슬롯에 저장된 아이템의 숫자가 0 -> 아이템이 없어질 예정
        {
            ClearSlot();   
        }
    }

    private void ClearSlot() // 현재 슬롯에 저장된 아이템을 삭제하는 함수
    {
        piece = null;    // 현재 저장된 아이템 정보를 삭제하고
        pieceCount = 0;  // 아이템과 관련된 모든 정보(아이템 갯수, 이미지, 텍스트 UI, 이미지 투명도)를 초기화 후 UI에서 보이지 않도록 함
        pieceImage.sprite = null;
        SetImageAlpha(0);
        pieceTextCount.text = "0";
        
        pieceCountImage.SetActive(false);    // 마지막으로 아이템 갯수 UI가 보이지 않도록 설정
    }
}
