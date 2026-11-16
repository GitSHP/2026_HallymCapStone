using System;
using System.Collections.Generic;
using UnityEngine;

namespace Promotion.Analysis
{
    // 런 기록을 분석한 결과 -> 복기 리포트의 유일한 근거 자료
    // -> 리포트에 쓰는 문장은 이 안의 항목(finding)만 근거로 삼음

    [Serializable]
    public class PieceStat      // 기물 하나의 이번 런 통계
    {
        public string piece;    // 화면 표시 이름
        public int deployed;    // 소환 횟수
        public int moved;       // 이동 성공 횟수
        public int captures;    // 잡은 횟수
        public int lost;        // 잃은 횟수
        public int cancelled;   // 집었다가 그냥 내려놓은 횟수
        public int guideOpens;  // [설명]을 다시 본 횟수
        public float avgHoldSec;    // 이동을 결정하기까지 집고 있던 평균 시간(초)
    }

    [Serializable]
    public class Finding        // 리포트의 근거 항목 하나
    {
        public string id;       // 근거 번호 (F1, F2 ...)
        public string kind;     // 항목 종류 (HangingPiece, MissedMate 등)
        public bool positive;   // 잘한 점인지
        public int severity;    // 중요도 (클수록 먼저 보여 줌)
        public int stage;
        public int turn;
        public string piece;    // 관련 기물 (화면 표시 이름)
        public int count;       // 몇 번 일어났는지
        public float value;     // 점수 손실 등 수치
        public string fact;     // 사실만 적은 문장

        public bool hasMove;    // 체스판에 재현할 수가 있는지
        public string board;    // 재현할 판 (RunRecorder.Snapshot 형식)
        public string played;   // 둔 수 (예: d1-d7)
        public string better;   // 더 좋았던 수 (없으면 빈 문자열)
        public string betterPiece;  // 더 좋았던 수의 기물
    }

    [Serializable]
    public class RunAnalysis
    {
        public string result;   // Victory / Defeat / InProgress
        public int stagesPlayed;
        public int stagesCleared;
        public bool reachedDuel;
        public int totalTurns;
        public int totalMoves;
        public int totalCaptures;
        public int totalLost;
        public int duelMoves;
        public int duelCleanMoves;  // 최종전에서 큰 실수가 없었던 수의 개수
        public List<PieceStat> pieces = new();
        public List<Finding> findings = new();

        public string ToJson(bool pretty)
        {
            return JsonUtility.ToJson(this, pretty);
        }

        public static RunAnalysis FromJson(string json)
        {
            return JsonUtility.FromJson<RunAnalysis>(json);
        }
    }
}
