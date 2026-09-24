using System.Collections;
using System.Collections.Generics;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject    // 아이템의 데이터를 저장하는 ScriptableObject 파일
{
    public enum ItemType    // 아이템의 유형
    {
        
    }

    public string itemName; // 아이템 이름
    public ItemType itemType; // 아이템 유형
    public Sprite itemImage; // 아이템 외형(2D 외형)
    // Sprite와 Image의 차이 = Image -> Canvas 위에서만 이미지를 띄울 수 있음, Sprite -> 게임 월드 어디에서든 이미지를 띄울 수 있음
    public GameObject itemPrefab; // 아이템 프리팹 (프리팹을 통해 아이템을 찍어낸다)
     


}
