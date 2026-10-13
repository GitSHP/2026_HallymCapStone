using System;
using System.Collections.Generic;
using UnityEngine;

namespace Promotion.Data
{
    [Serializable]
    public class ShopOffer  // 상점의 유물 매물 한 칸 - 아이템(ItemData)에 가격, 등장 확률처럼 '상점에서만 의미 있는 값'을 붙인 것
    // -> 같은 아이템을 상점마다 다른 가격에 팔 수 있고, 아이템 에셋 자체는 건드리지 않아도 됨
    {
        [Tooltip("판매할 아이템. ItemData 의 종류(itemType)가 Artifact 인 것을 넣는다.")]
        public ItemData item;   // 판매할 유물 아이템

        [Tooltip("구매에 필요한 골드.")]
        [Min(0)] public int cost = 4;   // 이 유물을 살 때 드는 골드

        [Tooltip("클수록 상점에 자주 등장한다. 0 이면 등장하지 않는다.")]
        [Min(0f)] public float weight = 1f; // 상점 진열 확률 가중치 -> 다른 매물보다 클수록 자주 뽑힘

        [Tooltip("체크하면 한 런에 하나만 가질 수 있고, 이미 가진 뒤에는 상점에 나오지 않는다.")]
        public bool unique = true;  // true면 한 판에 하나만 가질 수 있는 유물 -> 이미 가지고 있으면 상점에 다시 나오지 않음

        [Tooltip("카드에 표시할 한두 줄 설명. 비워 두면 설명 없이 이름과 그림만 나온다.")]
        [TextArea(2, 3)] public string blurb = "";  // 상점 카드에 보여 줄 짧은 설명 문구

        public bool IsValid => item != null && weight > 0f; // 상점에 올릴 수 있는 매물인지 (아이템이 있고 가중치가 0보다 커야 함)
    }

    [CreateAssetMenu(menuName = "Promotion/Shop Pool", fileName = "ShopPool")]
    public class ShopPool : ScriptableObject    // 상점에 무엇이 어떤 확률로 나오고 몇 칸을 채울지 정하는 데이터 - 뽑기도 여기서 함
    // -> 새 유물을 상점에 올리려면 ItemData 에셋을 만든 뒤 이 에셋의 artifactOffers 목록에 추가하면 됨
    {
        [Header("진열 칸 수")]
        [Min(1)] public int artifactSlots = 3;  // 상점 윗줄에 진열할 유물 칸 수
        [Min(1)] public int pieceSlots = 3; // 상점 아랫줄에 진열할 기물 칸 수

        [Header("유물 매물 (아이템 담당 영역)")]
        [Tooltip("상점 윗줄에 나올 유물들. ItemData 에셋과 그 가격을 짝지어 넣는다.")]
        public ShopOffer[] artifactOffers = Array.Empty<ShopOffer>();   // 상점에 나올 수 있는 유물 매물 전체 목록

        [Header("기물 매물")]
        [Tooltip("상점 아랫줄에 나올 체스 기물. 킹은 넣지 않는다.")]
        public PieceDefinition[] pieces = Array.Empty<PieceDefinition>();   // 상점에 나올 수 있는 기물 전체 목록

        static readonly List<int> Candidates = new();   // 뽑기 후보의 번호(배열 인덱스)를 잠시 담아 두는 목록 -> 매번 새로 만들지 않고 재사용함

#if UNITY_EDITOR
        void OnValidate()   // 인스펙터에서 에셋 값을 바꿀 때마다 유니티가 호출하는 함수 -> 유물 줄에 잘못된 종류가 들어갔는지 검사
        // -> 막지는 않고 경고만 남김 (의도적으로 섞고 싶을 수도 있으므로)
        {
            if (artifactOffers == null) // 만약 매물 목록이 아직 없다면 검사할 것이 없으므로 종료
                return;

            for (int i = 0; i < artifactOffers.Length; i++) // 모든 유물 매물을 하나씩 검사
            {
                var offer = artifactOffers[i];
                if (offer == null || offer.item == null)    // 빈 칸은 건너뜀
                    continue;

                if (offer.item.itemType != ItemData.ItemType.Artifact)  // 만약 넣은 아이템의 종류가 유물이 아니라면 콘솔에 경고를 띄움
                    Debug.LogWarning(
                        $"[ShopPool] 유물 칸 {i}번의 '{offer.item.name}' 은 종류가 " +
                        $"{offer.item.itemType} 입니다. 유물 줄에는 itemType 이 Artifact 인 " +
                        "아이템을 넣어 주세요.", this);
            }
        }
#endif

        public void Roll(List<ShopOffer> artifactResults,
                         List<PieceDefinition> pieceResults,
                         IReadOnlyList<ItemData> ownedArtifacts = null)
        // 상점을 열 때 진열할 유물과 기물을 무작위로 뽑아 결과 목록에 채워 주는 함수
        // -> ownedArtifacts(이미 가진 유물)를 넘기면 고유 유물은 후보에서 빠짐
        {
            artifactResults.Clear();    // 이전 결과를 비우고
            pieceResults.Clear();

            RollArtifacts(artifactResults, ownedArtifacts); // 유물 줄을 뽑은 뒤
            RollPieces(pieceResults);   // 기물 줄을 뽑음
        }

        void RollArtifacts(List<ShopOffer> results, IReadOnlyList<ItemData> owned)  // 유물 매물을 진열 칸 수만큼 중복 없이 뽑는 함수
        {
            Candidates.Clear();
            for (int i = 0; i < artifactOffers.Length; i++) // 모든 유물 매물 중에서 뽑을 수 있는 것만 후보로 모음
            {
                var offer = artifactOffers[i];
                if (offer == null || !offer.IsValid)    // 비어 있거나 가중치가 0인 매물은 제외
                    continue;
                if (offer.unique && Owns(owned, offer.item))    // 만약 고유 유물인데 이미 가지고 있다면 제외
                    continue;
                Candidates.Add(i);  // 조건을 통과한 매물의 번호를 후보에 추가
            }

            for (int slot = 0; slot < artifactSlots && Candidates.Count > 0; slot++)    // 진열 칸이 다 차거나 후보가 바닥날 때까지 반복
            {
                int picked = PickWeighted(i => artifactOffers[i].weight);   // 가중치에 따라 후보 하나를 고르고
                results.Add(artifactOffers[Candidates[picked]]);    // 결과에 추가한 뒤
                Candidates.RemoveAt(picked);    // 뽑은 것은 후보에서 뺌 -> 한 화면에 같은 유물이 두 장 뜨지 않게 함
            }
        }

        void RollPieces(List<PieceDefinition> results)  // 기물 매물을 진열 칸 수만큼 뽑는 함수 (중복 허용)
        {
            Candidates.Clear();
            for (int i = 0; i < pieces.Length; i++) // 모든 기물 중에서 상점에 나올 수 있는 것만 후보로 모음
            {
                var p = pieces[i];
                if (p == null || p.shopWeight <= 0f || p.isObjective)   // 비어 있거나, 가중치가 0이거나, 킹이라면 제외
                    continue;
                Candidates.Add(i);
            }

            for (int slot = 0; slot < pieceSlots && Candidates.Count > 0; slot++)   // 진열 칸 수만큼 반복
            {
                int picked = PickWeighted(i => pieces[i].shopWeight);   // 가중치에 따라 하나를 골라
                results.Add(pieces[Candidates[picked]]);    // 결과에 추가함 -> 후보에서 빼지 않으므로 같은 기물이 여러 번 나올 수 있음 (폰 두 개 구매도 합리적인 선택)
            }
        }

        static bool Owns(IReadOnlyList<ItemData> owned, ItemData item)  // 가진 유물 목록 안에 해당 아이템이 있는지 확인하는 함수
        {
            if (owned == null)  // 만약 가진 목록이 전달되지 않았다면 없는 것으로 봄
                return false;

            for (int i = 0; i < owned.Count; i++)   // 가진 유물을 하나씩 비교해서
                if (owned[i] == item)   // 같은 것이 있으면 true
                    return true;

            return false;
        }

        static int PickWeighted(Func<int, float> weightOf)  // 후보들 중 하나를 가중치 확률로 뽑는 함수
        // -> 반환값은 원본 배열의 번호가 아니라 Candidates 목록 안의 위치임
        {
            float total = 0f;
            foreach (var index in Candidates)   // 모든 후보의 가중치를 더해 전체 합을 구하고
                total += weightOf(index);

            float roll = UnityEngine.Random.value * total;  // 0 ~ 전체 합 사이의 무작위 값을 하나 뽑음
            for (int i = 0; i < Candidates.Count; i++)  // 후보의 가중치를 차례로 빼 나가다가
            {
                roll -= weightOf(Candidates[i]);
                if (roll <= 0f) // 0 이하가 되는 순간의 후보가 당첨 -> 가중치가 클수록 당첨 구간이 넓음
                    return i;
            }
            return Candidates.Count - 1;    // 소수점 오차로 끝까지 못 고른 경우 마지막 후보를 반환
        }
    }
}
