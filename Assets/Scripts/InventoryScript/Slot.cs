using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public ItemData item; // 인벤토리에 저장할 아이템
    public int itemCount; // 저장된 아이템의 갯수
    public Image itemImage; // 저장된 아이템의 이미지

    [SerializeField]
    private TextMeshProUGUI itemTextCount; // 아이템의 갯수를 표시하는 텍스트 UI에 접근하기 위한 변수
    [SerializeField]
    private GameObject itemCountImage;  // 아이템의 갯수 이미지를 표시하는 Image UI에 접근하기 위한 변수

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
        else // 만약 인벤토리에 저장할 아이템이 아티팩트라면 -> 아티팩트는 패시브처럼 1개만 있어도 발동되는 아이템이므로 한 개만 있어야함
        {
            itemTextCount.text = "0";   // 아이템의 갯수는 의미가 없으므로 String 타입의 0으로 초기화
            itemCountImage.SetActive(false);    // 아이템 갯수 이미지가 보이지 않도록 설정
        }
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
        itemCount = 0;  // 아이템과 관련된 모든 정보를 초기화(아이템 갯수, 이미지, 텍스트 UI)
        itemImage.sprite = null;

        itemTextCount.text = "0";
        itemCountImage.SetActive(false);    // 마지막으로 아이템 갯수 UI가 보이지 않도록 설정
    }
}
