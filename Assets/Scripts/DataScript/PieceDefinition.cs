using Promotion.Core;
using UnityEngine;

namespace Promotion.Data
{
    /// <summary>
    /// 기물을 규정하는 모든 것, 즉 외형·비용·행마법을 담는다.
    /// 나중에 실제 그림으로 교체할 때는 여기서 <see cref="ItemData.itemImage"/>만 지정하면 되고 코드는 고치지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Promotion/Piece Definition", fileName = "Piece")]
    public class PieceDefinition : ItemData
    {
        // 이름(itemName)·그림(itemImage)·유형(itemType)은 ItemData에서 물려받는다.
        [Header("정체성")]
        [Tooltip("실제 그림이 지정되기 전까지 보여 주는 임시 글자다.")]
        public string glyph = "P";

        [Header("외형 (itemImage를 비워 두면 임시 그림을 쓴다)")]
        public Color playerColor = new Color(0.95f, 0.95f, 0.92f);
        public Color enemyColor = new Color(0.25f, 0.22f, 0.28f);

        [Header("비용")]
        [Tooltip("이 기물을 이동할 때 소모하는 AP다.")]
        public int moveApCost = 1;
        [Tooltip("덱에서 꺼내 배치할 때 소모하는 AP다.")]
        public int deployApCost = 1;
        [Tooltip("상점에 이 기물이 나왔을 때의 골드 가격이다.")]
        [Min(0)] public int shopCost = 3;
        [Tooltip("가중치가 클수록 상점에 자주 나온다. 0이면 상점에 아예 나오지 않는다.")]
        [Min(0f)] public float shopWeight = 1f;

        [Header("역할")]
        [Tooltip("킹이다. 체력이 없고 공격받지도 않는다. 적이 이 칸에 들어서면 적이 승리한다.")]
        public bool isObjective;

        [Header("행마법")]
        public MovePattern[] patterns = new MovePattern[0];

        [Header("체스 대결")]
        [Tooltip("이 기물이 적(흑) 편으로 싸울 때 쓰는 그림이다. 비워 두면 플레이어 그림에 enemyColor를 입혀 쓴다.")]
        public Sprite enemySprite;
        [Tooltip("체스 AI가 매기는 기물의 가치로, 폰 단위다(폰 1, 나이트/비숍 3, 룩 5, 퀸 9).")]
        [Min(0f)] public float aiValue = 1f;
        [Tooltip("맨 끝 랭크에 도달하면 다른 기물로 바뀐다(폰).")]
        public bool canPromote;
    }
}
