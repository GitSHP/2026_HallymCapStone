using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChessPiecesData", menuName = "Scriptable Objects/ChessPiecesData")]
public class ChessPiecesData : ScriptableObject // 체스 말의 데이터를 저장하는 SciptableObject 파일
{
    public string pieceName; // 체스 말 이름
    public Sprite pieceImage; // 체스 말 외형(2D 외형)
    // Sprite와 Image의 차이 = Image -> Canvas 위에서만 이미지를 띄울 수 있음, Sprite -> 게임 월드 어디에서든 이미지를 띄울 수 있음
    public GameObject piecePrefab; // 체스 말 프리팹 (프리팹을 통해 아이템을 찍어낸다)
}
