using System;
using System.Collections.Generic;
using Promotion.Core;
using UnityEngine;

namespace Promotion.Data
{
    [Serializable]
    public class FairyRouletteEntry // 룰렛 결과 하나 -> 두 단어의 조합이 어떤 특수 기물을 뜻하는지
    {
        public PieceDefinition piece;   // 이 조합이 나오면 해금되는 특수 기물
        public string left; // 첫 번째 룰렛에 멈추는 단어 (예: 드래곤)
        public string right;    // 두 번째 룰렛에 멈추는 단어 (예: 왕관)
    }

    [CreateAssetMenu(menuName = "Promotion/Fairy Roulette", fileName = "FairyRoulette")]
    public class FairyRoulette : ScriptableObject   // 특수 기물 뽑기(룰렛)의 데이터 -> 뽑을 수 있는 특수 기물과 그 단어 조합
    // -> Resources/FairyRoulette 에 있어야 타이틀 화면과 상점이 불러옴
    // -> 해금 기록은 MetaProgress에 이름으로 저장되고, 이 에셋이 이름을 실제 기물로 바꿔 줌
    {
        public FairyRouletteEntry[] entries = Array.Empty<FairyRouletteEntry>();   // 뽑을 수 있는 특수 기물 목록

        static FairyRoulette _instance;

        public static FairyRoulette Instance    // Resources에서 한 번만 불러와 재사용하는 속성 -> 없으면 null
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<FairyRoulette>("FairyRoulette");
                return _instance;
            }
        }

        public static IReadOnlyList<PieceDefinition> UnlockedPieces()   // 해금한 특수 기물 목록 -> 상점이 진열 후보에 더함
        {
            var result = new List<PieceDefinition>();
            var roulette = Instance;
            if (roulette == null)
                return result;
            foreach (var entry in roulette.entries)
                if (entry != null && entry.piece != null && MetaProgress.IsUnlocked(entry.piece.name))
                    result.Add(entry.piece);
            return result;
        }

        public List<FairyRouletteEntry> LockedEntries() // 아직 해금하지 않은 결과 목록 -> 룰렛은 이 중에서만 뽑음 (같은 기물이 두 번 나오지 않게)
        {
            var result = new List<FairyRouletteEntry>();
            foreach (var entry in entries)
                if (entry != null && entry.piece != null && !MetaProgress.IsUnlocked(entry.piece.name))
                    result.Add(entry);
            return result;
        }

        public List<string> Words(bool leftSide)    // 한쪽 룰렛에 지나가는 단어 목록 (중복 없이) -> 돌아가는 동안 이 단어들이 번갈아 보임
        {
            var result = new List<string>();
            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;
                var word = leftSide ? entry.left : entry.right;
                if (!string.IsNullOrEmpty(word) && !result.Contains(word))
                    result.Add(word);
            }
            return result;
        }
    }
}
