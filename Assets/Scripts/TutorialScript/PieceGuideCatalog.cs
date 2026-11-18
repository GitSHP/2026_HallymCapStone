using Promotion.Data;
using UnityEngine;

namespace Promotion.Tutorial
{
    [CreateAssetMenu(menuName = "Promotion/Piece Guide Catalog", fileName = "PieceGuideCatalog")]
    public class PieceGuideCatalog : ScriptableObject // 모든 기물 가이드(PieceGuideData)를 모아 둔 목록
    // -> PieceGuideDirector가 소환된 기물로 이 목록을 뒤져 보여 줄 가이드를 찾음
    {
        public PieceGuideData[] guides = new PieceGuideData[0]; // 기물 가이드 목록

        public PieceGuideData Find(PieceDefinition piece) // 그 기물의 가이드를 찾아 돌려주는 함수 -> 없으면 null
        {
            if (piece == null)
                return null;

            foreach (var guide in guides) // 목록을 앞에서부터 확인해서
                if (guide != null && guide.piece == piece) // 같은 기물을 설명하는 가이드라면
                    return guide; // 그 가이드를 돌려줌
            return null; // 끝까지 없다면 이 기물은 가이드가 없음
        }
    }
}
