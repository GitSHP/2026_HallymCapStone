using System;
using UnityEngine;

namespace Promotion.Data
{
    public enum StageMode   // 스테이지의 진행 방식 종류
    {
        Waves,  // 킹을 향해 걸어오는 적 웨이브를 막아 내는 일반 디펜스 스테이지
        ChessDuel   // AI와 체스 한 판을 두고 체크메이트로 승리하는 최종전 스테이지
    }

    [Serializable]
    public class PiecePlacement // 스테이지 시작 시 특정 칸에 서 있는 기물 하나의 정보 (최종전 적 배치에 사용)
    {
        public PieceDefinition piece;   // 놓을 기물의 종류
        [Tooltip("x = 파일(0 = a), y = 랭크(0 = 플레이어 쪽 맨 뒷줄).")]
        public Vector2Int square;   // 기물을 놓을 칸의 좌표 (x는 가로 a~h, y는 세로 1~8줄을 0부터 셈)
    }

    [Serializable]
    public class WaveDefinition // 적 웨이브(한 번에 몰려오는 적 무리) 하나의 정보
    {
        [Tooltip("이 웨이브가 진입하는 플레이어 턴이다. 해당 턴이 끝날 때 등장한다.")]
        public int spawnOnTurn = 1; // 몇 번째 턴에 등장하는지 -> 그 턴이 끝날 때 적이 판 위로 나타남
        public PieceDefinition enemy;   // 등장할 적 기물의 종류
        public int count = 3;   // 한 번에 등장하는 적의 수
    }

    [CreateAssetMenu(menuName = "Promotion/Stage Definition", fileName = "Stage")]
    public class StageDefinition : ScriptableObject // 스테이지 하나의 모든 설정(AP, 배치, 몰려오는 적, 최종전 규칙)을 담는 데이터
    // -> 새 스테이지는 코드가 아니라 새 에셋으로 만듦
    {
        [Header("모드")]
        public StageMode mode = StageMode.Waves;    // 이 스테이지가 웨이브 방어인지 체스 대결인지

        [Header("외형")]
        [Tooltip("이 스테이지의 보드 그림이다. 비워 두면 씬에 있는 보드를 그대로 쓴다.")]
        public Sprite boardArt; // 이 스테이지에서 쓸 체스판 그림

        [Header("AP")]
        public int apPerTurn = 5;   // 매 턴 플레이어가 받는 행동력(AP) -> 기물 이동과 소환에 소모됨

        [Header("배치")]
        [Tooltip("기물을 배치할 수 있는 줄 수로, 플레이어 쪽 가장자리부터 센다.")]
        public int deployRows = 3;  // 플레이어 쪽 아래에서부터 기물을 놓을 수 있는 줄 수

        [Tooltip("킹이다. 킹을 놓아야 스테이지가 시작된다.")]
        public PieceDefinition kingDefinition;  // 이 스테이지에서 쓸 킹 기물 -> 배치 단계에서 킹을 놓아야 전투가 시작됨

        [Tooltip("플레이어가 킹 외에 가지고 들어가는 기물이다.")]
        public PieceDefinition[] startingDeck = Array.Empty<PieceDefinition>(); // 킹 외에 전투 중 AP로 소환할 수 있는 기물 목록

        [Header("웨이브")]
        public WaveDefinition[] waves = Array.Empty<WaveDefinition>();  // 이 스테이지에서 몰려오는 적 웨이브 목록
        [Tooltip("적이 등장하는 줄 수로, 위쪽 가장자리부터 센다.")]
        public int spawnRows = 2;   // 판 위쪽에서부터 적이 나타날 수 있는 줄 수

        [Header("규칙")]
        [Tooltip("적이 킹을 향해 대각선으로도 이동할 수 있다.")]
        public bool enemiesMoveDiagonally = true;   // true면 적이 킹에게 다가갈 때 대각선 칸도 사용함 -> 더 빨리 접근함

        [Header("체스 대결")]
        [Tooltip("적(흑) 진영의 기물과 그 위치다.")]
        public PiecePlacement[] enemyLayout = Array.Empty<PiecePlacement>();    // 최종전에서 적(흑) 기물들의 시작 배치
        [Tooltip("배치 단계에서 런 동안 모은 기물과 별도로 무료로 지급하는 폰이다.")]
        public PieceDefinition freePawn;    // 최종전 배치 단계에서 무료로 주는 폰의 종류
        [Min(0)] public int maxFreePawns = 8;   // 무료 폰을 최대 몇 개까지 줄지
        [Tooltip("플레이어가 둘 수 있는 수의 한도다. 넘기면 대결에서 패배한 것으로 본다.")]
        [Min(1)] public int moveLimit = 100;    // 플레이어가 둘 수 있는 최대 수 -> 이 안에 체크메이트하지 못하면 패배
        [Tooltip("폰이 맨 끝 랭크에서 승급할 수 있는 기물이다. AI는 항상 첫 번째 항목을 고른다.")]
        public PieceDefinition[] promotionChoices = Array.Empty<PieceDefinition>(); // 프로모션 때 선택할 수 있는 기물 목록 (보통 퀸, 룩, 비숍, 나이트)
        public AIProfile aiProfile; // 최종전 상대 AI의 난이도 설정

        public bool IsChessDuel => mode == StageMode.ChessDuel; // 이 스테이지가 최종전 체스 대결인지 확인하는 속성
    }
}
