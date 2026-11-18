using System;
using System.Collections.Generic;
using UnityEngine;

namespace Promotion.Core
{
    public static class MetaProgress   // 런이 끝나도, 게임을 껐다 켜도 남는 진행 기록 (보스 클리어 수, 검은 왕관, 해금한 특수 기물)
    // -> RunState는 한 판(런) 동안만 유지되고, 이 기록은 PlayerPrefs에 영구 저장됨
    // -> 여기서는 기록만 다루고, 기물 에셋을 찾는 일은 FairyRoulette(데이터)이 맡음 -> 해금 기록은 에셋 이름으로 저장
    {
        const string BossClearsKey = "Meta.BossClears";    // 보스(최종전)를 이긴 횟수
        const string CrownsKey = "Meta.Crowns"; // 가지고 있는 검은 왕관 수
        const string FairyIntroKey = "Meta.FairyIntroShown";    // 특수 기물 버튼이 풀리는 연출과 설명을 이미 봤는지
        const string UnlockedKey = "Meta.UnlockedFairies"; // 해금한 특수 기물 에셋 이름을 '|'로 이은 문자열

        public static event Action Changed; // 기록이 바뀔 때마다 발생하는 이벤트 -> 타이틀 화면의 왕관 수·버튼 상태가 다시 그려짐

        public static int BossClears    // 보스를 이긴 횟수 -> 1 이상이면 특수 기물 버튼이 열림
        {
            get { return PlayerPrefs.GetInt(BossClearsKey, 0); }
        }

        public static int Crowns    // 가지고 있는 검은 왕관 수 -> 룰렛 1회에 1개를 씀
        {
            get { return PlayerPrefs.GetInt(CrownsKey, 0); }
        }

        public static bool FairyUnlocked    // 특수 기물 버튼이 열렸는지 여부 (보스를 한 번이라도 이겼는지)
        {
            get { return BossClears > 0; }
        }

        public static bool FairyIntroShown  // 버튼이 풀리는 연출과 설명을 이미 봤는지 여부
        {
            get { return PlayerPrefs.GetInt(FairyIntroKey, 0) == 1; }
        }

        public static void RecordBossClear()    // 보스를 이겼을 때 호출하는 함수 -> 클리어 수 +1, 검은 왕관 +1
        {
            PlayerPrefs.SetInt(BossClearsKey, BossClears + 1);
            PlayerPrefs.SetInt(CrownsKey, Crowns + 1);
            Save();
        }

        public static void AddCrowns(int amount)    // 검은 왕관을 더하는 함수 (개발자 도구용) -> 0 아래로는 내려가지 않음
        {
            PlayerPrefs.SetInt(CrownsKey, Mathf.Max(0, Crowns + amount));
            Save();
        }

        public static bool TrySpendCrown()  // 검은 왕관 1개를 쓰는 함수 -> 하나도 없으면 false
        {
            if (Crowns <= 0)
                return false;
            PlayerPrefs.SetInt(CrownsKey, Crowns - 1);
            Save();
            return true;
        }

        public static void MarkFairyIntroShown(bool shown)  // 버튼이 풀리는 연출을 본 것으로(또는 다시 보도록) 기록하는 함수
        {
            PlayerPrefs.SetInt(FairyIntroKey, shown ? 1 : 0);
            Save();
        }

        public static bool IsUnlocked(string pieceName) // 그 이름의 특수 기물을 해금했는지 확인하는 함수
        {
            return UnlockedNames().Contains(pieceName);
        }

        public static List<string> UnlockedNames()  // 해금한 특수 기물 이름 목록을 돌려주는 함수
        {
            var names = new List<string>();
            foreach (var name in PlayerPrefs.GetString(UnlockedKey, "").Split('|'))
                if (name.Length > 0)
                    names.Add(name);
            return names;
        }

        public static void Unlock(string pieceName) // 특수 기물 하나를 해금 기록에 넣는 함수 -> 다음 런부터 상점에 나옴
        {
            var names = UnlockedNames();
            if (names.Contains(pieceName))
                return;
            names.Add(pieceName);
            PlayerPrefs.SetString(UnlockedKey, string.Join("|", names));
            Save();
        }

        public static void ClearUnlocks()   // 해금한 특수 기물을 모두 잠그는 함수 (개발자 도구용)
        {
            PlayerPrefs.DeleteKey(UnlockedKey);
            Save();
        }

        public static void SetBossClears(int count) // 보스 클리어 수를 직접 정하는 함수 (개발자 도구용)
        {
            PlayerPrefs.SetInt(BossClearsKey, Mathf.Max(0, count));
            Save();
        }

        public static void ResetAll()   // 이 기록을 모두 지워 처음 실행한 상태로 되돌리는 함수 (개발자 도구용)
        {
            PlayerPrefs.DeleteKey(BossClearsKey);
            PlayerPrefs.DeleteKey(CrownsKey);
            PlayerPrefs.DeleteKey(FairyIntroKey);
            PlayerPrefs.DeleteKey(UnlockedKey);
            Save();
        }

        static void Save()  // 바로 디스크에 저장하고 바뀌었음을 알리는 함수
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
