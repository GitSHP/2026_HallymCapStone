using System;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Analysis
{
    // 한 런 동안의 플레이 기록을 담는 데이터
    // -> RunRecorder가 채우고, 런이 끝나면 복기 리포트의 분석 재료가 됨
    // -> 기물 이름·좌표 같은 게임 내 행동만 담는다. 플레이어 개인 정보는 담지 않음

    [Serializable]
    public class StageRecord    // 스테이지 하나의 요약
    {
        public int index;       // 이번 런에서 몇 번째로 치른 전투인지 (0부터)
        public string name;     // StageDefinition 에셋 이름
        public bool isDuel;     // 최종전 체스 대결인지
        public string outcome = "InProgress";   // InProgress / Victory / Defeat
        public string reason;   // 끝난 이유 (체크메이트 등)
        public int turns;       // 진행한 턴 수
    }

    [Serializable]
    public class MoveRecord     // 기물을 집어서 놓은 행동 하나
    {
        public int stage;
        public int turn;
        public string piece;
        public int fromX;
        public int fromY;
        public int toX;
        public int toY;
        public string outcome;      // DragController.DragOutcome 이름
        public float holdSec;       // 집고 있던 시간(초)
        public int optionCount;     // 갈 수 있던 칸 수
        public bool hadCaptureOption;   // 이 기물로 잡을 수 있는 상대가 있었는지
        public bool captured;       // 실제로 잡았는지
        public string victim;       // 잡은 상대 이름 (없으면 빈 문자열)
        public string board;        // 집기 직전의 판 (RunRecorder.Snapshot 형식)

        [NonSerialized] public SearchBoard before;  // 최종전의 집기 직전 판 -> 런이 끝난 뒤 엔진으로 수를 평가할 때 사용
        [NonSerialized] public PieceDefinition promotion;   // 최종전에서 폰이 승격할 때 평가에 쓸 기물
    }

    [Serializable]
    public class TurnEndRecord  // 플레이어 턴이 끝난 시점
    {
        public int stage;
        public int turn;
        public int apLeft;      // 쓰지 않고 남긴 AP
        public List<string> missedCaptures = new();     // 남은 AP로 잡을 수 있었던 기회 ("기물>대상")
        public string board;
    }

    [Serializable]
    public class PieceEvent     // 기물 이름 하나로 표현되는 사건 (소환, 설명 열람, 승격)
    {
        public int stage;
        public int turn;
        public string piece;
    }

    [Serializable]
    public class LossRecord     // 아군 기물을 잃은 사건
    {
        public int stage;
        public int turn;
        public string piece;
        public string by;       // 잡은 적 이름
    }

    [Serializable]
    public class CheckRecord    // 최종전의 체크 발생
    {
        public int stage;
        public int turn;
        public string checkedTeam;  // 체크를 당한 진영
    }

    [Serializable]
    public class RunLog
    {
        public static RunLog Current { get; set; }  // 지금(또는 방금 끝난) 런의 기록 -> 튜토리얼이나 시작 전이면 null

        public bool finished;   // 런이 끝났는지
        public string result = "InProgress";    // InProgress / Victory / Defeat
        public List<StageRecord> stages = new();
        public List<MoveRecord> moves = new();
        public List<TurnEndRecord> turnEnds = new();
        public List<PieceEvent> deploys = new();
        public List<PieceEvent> guideOpens = new();
        public List<PieceEvent> promotions = new();
        public List<LossRecord> losses = new();
        public List<CheckRecord> checks = new();

        [NonSerialized] readonly Dictionary<string, string> _displayNames = new();  // 에셋 이름 -> 화면 표시 이름

        public void RegisterName(PieceDefinition def)   // 기물의 화면 표시 이름을 기억하는 함수
        {
            if (def != null)
                _displayNames[def.name] = def.itemName;
        }

        public string DisplayName(string assetName)     // 에셋 이름을 화면 표시 이름으로 바꾸는 함수 (모르면 에셋 이름 그대로)
        {
            return _displayNames.TryGetValue(assetName, out var display) ? display : assetName;
        }

        public string ToJson(bool pretty)
        {
            return JsonUtility.ToJson(this, pretty);
        }
    }
}
