using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Slot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    // 모든 슬롯에 필요한 드래그 앤 드롭 기능을 구현한 모든 슬롯의 부모 Slot 클래스

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {

    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 오버라이딩 하기
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 오버라이딩 하기
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 오버라이딩 하기
    }

    // 해당 슬롯에 무언가가 마우스 드롭 됐을 때 발생하는 이벤트
    public void OnDrop(PointerEventData eventData)
    {

    }
}
