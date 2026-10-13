using Promotion.Core;
using UnityEngine;

namespace Promotion.Data
{
    [CreateAssetMenu(menuName = "Promotion/Piece Definition", fileName = "Piece")]
    public class PieceDefinition : ItemData // 기물 한 종류의 모든 정보(외형, 비용, 행마법)를 담는 데이터 - 인벤토리 아이템(ItemData)을 상속함
    // -> 실제 그림으로 교체할 때는 itemImage만 지정하면 되고 코드는 고치지 않음
    {
        // 이름(itemName), 그림(itemImage), 유형(itemType)은 부모 클래스인 ItemData에서 물려받음
        [Header("정체성")]
        [Tooltip("실제 그림이 지정되기 전까지 보여 주는 임시 글자다.")]
        public string glyph = "P";  // 그림이 없을 때 기물 위에 표시할 임시 글자 (예: 킹 "K", 퀸 "Q")

        [Header("외형 (itemImage를 비워 두면 임시 그림을 쓴다)")]
        public Color playerColor = new Color(0.95f, 0.95f, 0.92f); // 아군 기물일 때 임시 그림에 칠할 색 (흰색 계열)
        public Color enemyColor = new Color(0.25f, 0.22f, 0.28f);  // 적 기물일 때 칠할 색 (어두운 색 계열)

        [Header("비용")]
        [Tooltip("이 기물을 이동할 때 소모하는 AP다.")]
        public int moveApCost = 1;  // 이 기물을 한 번 이동할 때 소모하는 AP
        [Tooltip("덱에서 꺼내 배치할 때 소모하는 AP다.")]
        public int deployApCost = 1;    // 전투 중 이 기물을 판 위에 소환할 때 소모하는 AP
        [Tooltip("상점에 이 기물이 나왔을 때의 골드 가격이다.")]
        [Min(0)] public int shopCost = 3;   // 상점에서 이 기물을 살 때 드는 골드
        [Tooltip("가중치가 클수록 상점에 자주 나온다. 0이면 상점에 아예 나오지 않는다.")]
        [Min(0f)] public float shopWeight = 1f; // 상점 진열 확률 가중치 -> 다른 기물보다 클수록 자주 등장, 0이면 등장하지 않음

        [Header("역할")]
        [Tooltip("킹이다. 체력이 없고 공격받지도 않는다. 적이 이 칸에 들어서면 적이 승리한다.")]
        public bool isObjective;    // true면 이 기물이 킹(지켜야 할 목표) -> 적이 이 칸에 들어오면 즉시 패배

        [Header("행마법")]
        public MovePattern[] patterns = new MovePattern[0]; // 이 기물이 움직일 수 있는 규칙들의 목록 -> 여러 규칙을 합쳐 하나의 행마법이 됨

        [Header("체스 대결")]
        [Tooltip("이 기물이 적(흑) 편으로 싸울 때 쓰는 그림이다. 비워 두면 플레이어 그림에 enemyColor를 입혀 쓴다.")]
        public Sprite enemySprite;  // 최종전에서 적(흑) 기물로 나올 때 쓰는 그림
        [Tooltip("체스 AI가 매기는 기물의 가치로, 폰 단위다(폰 1, 나이트/비숍 3, 룩 5, 퀸 9).")]
        [Min(0f)] public float aiValue = 1f;    // 체스 AI가 판세를 계산할 때 쓰는 이 기물의 점수
        [Tooltip("맨 끝 랭크에 도달하면 다른 기물로 바뀐다(폰).")]
        public bool canPromote; // true면 상대편 끝 줄에 도착했을 때 프로모션(다른 기물로 승격) 가능
    }
}
