using System;
using System.Collections.Generic;
using UnityEngine;

namespace Promotion.Data
{
    // [아이템 담당] 상점에 유물 하나를 올리는 단위. 아이템 자체(ItemData)는 그대로 두고
    // 가격·등장 확률처럼 "상점에서만 의미 있는 값"을 여기에 붙인다.
    /// <summary>
    /// 한 칸의 매물. 아이템의 정체성(이름·외형·종류)은 <see cref="ItemData"/>가 갖고,
    /// 값과 등장 빈도는 상점이 갖는다. 같은 아이템을 스테이지마다 다른 가격에 팔거나
    /// 특정 상점에만 등장시키는 일이 아이템 에셋을 건드리지 않고 가능해진다.
    /// </summary>
    [Serializable]
    public class ShopOffer
    {
        [Tooltip("판매할 아이템. ItemData 의 종류(itemType)가 Artifact 인 것을 넣는다.")]
        public ItemData item;

        [Tooltip("구매에 필요한 골드.")]
        [Min(0)] public int cost = 4;

        [Tooltip("클수록 상점에 자주 등장한다. 0 이면 등장하지 않는다.")]
        [Min(0f)] public float weight = 1f;

        [Tooltip("체크하면 한 런에 하나만 가질 수 있고, 이미 가진 뒤에는 상점에 나오지 않는다.")]
        public bool unique = true;

        [Tooltip("카드에 표시할 한두 줄 설명. 비워 두면 설명 없이 이름과 그림만 나온다.")]
        [TextArea(2, 3)] public string blurb = "";

        public bool IsValid => item != null && weight > 0f;
    }

    // [아이템 담당] 상점에 무엇이 어떤 확률로 나올지 정하는 데이터. 새 유물을 상점에
    // 올리려면 ItemData 에셋을 만든 뒤 이 에셋의 artifactOffers 목록에 추가하면 된다.
    /// <summary>
    /// 상점에 무엇이 나올 수 있고 몇 칸을 채울지를 정한다. 뽑기 자체도 여기서 하므로
    /// 확률 조정이 화면 코드와 분리되어 있고, 스테이지나 막마다 다른 풀로 갈아끼울 수 있다.
    /// </summary>
    [CreateAssetMenu(menuName = "Promotion/Shop Pool", fileName = "ShopPool")]
    public class ShopPool : ScriptableObject
    {
        [Header("진열 칸 수")]
        [Min(1)] public int artifactSlots = 3;
        [Min(1)] public int pieceSlots = 3;

        [Header("유물 매물 (아이템 담당 영역)")]
        [Tooltip("상점 윗줄에 나올 유물들. ItemData 에셋과 그 가격을 짝지어 넣는다.")]
        public ShopOffer[] artifactOffers = Array.Empty<ShopOffer>();

        [Header("기물 매물")]
        [Tooltip("상점 아랫줄에 나올 체스 기물. 킹은 넣지 않는다.")]
        public PieceDefinition[] pieces = Array.Empty<PieceDefinition>();

        static readonly List<int> Candidates = new();

#if UNITY_EDITOR
        // 잘못된 종류를 넣어도 상점은 그냥 띄워 버리기 때문에, 에셋을 편집하는 순간
        // 알려 준다. 막지는 않는다 - 의도적으로 섞고 싶을 수도 있으므로 경고만 남긴다.
        void OnValidate()
        {
            if (artifactOffers == null)
                return;

            for (int i = 0; i < artifactOffers.Length; i++)
            {
                var offer = artifactOffers[i];
                if (offer == null || offer.item == null)
                    continue;

                if (offer.item.itemType != ItemData.ItemType.Artifact)
                    Debug.LogWarning(
                        $"[ShopPool] 유물 칸 {i}번의 '{offer.item.name}' 은 종류가 " +
                        $"{offer.item.itemType} 입니다. 유물 줄에는 itemType 이 Artifact 인 " +
                        "아이템을 넣어 주세요.", this);
            }
        }
#endif

        /// <param name="ownedArtifacts">이미 가진 고유 유물은 후보에서 빠진다.</param>
        public void Roll(List<ShopOffer> artifactResults,
                         List<PieceDefinition> pieceResults,
                         IReadOnlyList<ItemData> ownedArtifacts = null)
        {
            artifactResults.Clear();
            pieceResults.Clear();

            RollArtifacts(artifactResults, ownedArtifacts);
            RollPieces(pieceResults);
        }

        void RollArtifacts(List<ShopOffer> results, IReadOnlyList<ItemData> owned)
        {
            Candidates.Clear();
            for (int i = 0; i < artifactOffers.Length; i++)
            {
                var offer = artifactOffers[i];
                if (offer == null || !offer.IsValid)
                    continue;
                if (offer.unique && Owns(owned, offer.item))
                    continue;
                Candidates.Add(i);
            }

            for (int slot = 0; slot < artifactSlots && Candidates.Count > 0; slot++)
            {
                int picked = PickWeighted(i => artifactOffers[i].weight);
                results.Add(artifactOffers[Candidates[picked]]);
                // 뽑은 것은 후보에서 뺀다. 한 화면에 같은 유물이 두 장 뜨는 것은 다양성이 아니라 버그다.
                Candidates.RemoveAt(picked);
            }
        }

        void RollPieces(List<PieceDefinition> results)
        {
            Candidates.Clear();
            for (int i = 0; i < pieces.Length; i++)
            {
                var p = pieces[i];
                if (p == null || p.shopWeight <= 0f || p.isObjective)
                    continue;
                Candidates.Add(i);
            }

            // 기물은 중복을 허용한다. 폰을 두 개 사는 것도 합리적인 선택이다.
            for (int slot = 0; slot < pieceSlots && Candidates.Count > 0; slot++)
            {
                int picked = PickWeighted(i => pieces[i].shopWeight);
                results.Add(pieces[Candidates[picked]]);
            }
        }

        static bool Owns(IReadOnlyList<ItemData> owned, ItemData item)
        {
            if (owned == null)
                return false;

            for (int i = 0; i < owned.Count; i++)
                if (owned[i] == item)
                    return true;

            return false;
        }

        /// <summary>반환값은 원본 배열이 아니라 <see cref="Candidates"/> 안의 위치다.</summary>
        static int PickWeighted(Func<int, float> weightOf)
        {
            float total = 0f;
            foreach (var index in Candidates)
                total += weightOf(index);

            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < Candidates.Count; i++)
            {
                roll -= weightOf(Candidates[i]);
                if (roll <= 0f)
                    return i;
            }
            return Candidates.Count - 1;
        }
    }
}
