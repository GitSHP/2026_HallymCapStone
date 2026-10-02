using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactSlot : Slot
{
    public ItemData artifact;
    public Image artifactImage;
    public int artifactCount;   // 아티팩트의 갯수를 저장하는 변수 -> 아티팩트가 0개가 될 때 인벤토리에서 삭제하도록 함
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void SetImageAlpha(float _alpha) // 아이템 이미지의 투명도 조절 
    // -> 아이템이 저장된 경우 아이템 이미지의 투명도를 높여 이미지가 보이도록 조절하고 저장되지 않은 상태면 투명도를 낮춰 보이지 않게함
    {
        Color color = artifactImage.color;
        color.a = _alpha;
        artifactImage.color = color;
    }

    public void AddArtifact(ItemData _artifact, int _count = 1)
    {
        artifact = _artifact;
        artifactCount = _count;
        artifactImage.sprite = artifact.itemImage;
    }

    public void SetSlotCount(int _count)    // 현재 슬롯의 아이템 갯수를 업데이트해주는 함수
    {
        artifactCount += _count;    // 현재 슬롯에 저장된 아이템의 갯수를 매개변수(_count)의 크기만큼 증가

        if(artifactCount <= 0)  // 만약 아이템의 갯수가 0이라면 -> 현재 슬롯에 저장된 아이템의 숫자가 0 -> 아이템이 없어질 예정
        {
            ClearSlot();   
        }
    }


    private void ClearSlot() // 현재 슬롯에 저장된 아이템을 삭제하는 함수
    {
        artifact = null;    // 현재 저장된 아이템 정보를 삭제하고
        artifactImage.sprite = null; // 아이템과 관련된 모든 정보(아이템 갯수, 이미지, 텍스트 UI, 이미지 투명도)를 초기화 후 UI에서 보이지 않도록 함
        SetImageAlpha(0);
    }
}
