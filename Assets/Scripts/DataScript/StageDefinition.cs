using System;
using UnityEngine;

namespace Promotion.Data
{
    public enum StageMode
    {
        /// <summary>킹을 향해 걸어오는 적 웨이브를 막아 낸다.</summary>
        Waves,
        /// <summary>AI와 체스 한 판을 두며, 체크메이트로 승리한다.</summary>
        ChessDuel
    }

    /// <summary>스테이지 시작 시 어떤 칸에 서 있는 기물 하나다.</summary>
    [Serializable]
    public class PiecePlacement
    {
        public PieceDefinition piece;
        [Tooltip("x = 파일(0 = a), y = 랭크(0 = 플레이어 쪽 맨 뒷줄).")]
        public Vector2Int square;
    }

    [Serializable]
    public class WaveDefinition
    {
        [Tooltip("이 웨이브가 진입하는 플레이어 턴이다. 해당 턴이 끝날 때 등장한다.")]
        public int spawnOnTurn = 1;
        public PieceDefinition enemy;
        public int count = 3;
    }

    /// <summary>
    /// 스테이지 하나를 정의한다. 플레이어가 턴마다 할 수 있는 양, 배치할 수 있는 기물,
    /// 몰려오는 적을 담는다. 새 스테이지는 새 코드가 아니라 새 에셋으로 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "Promotion/Stage Definition", fileName = "Stage")]
    public class StageDefinition : ScriptableObject
    {
        [Header("모드")]
        public StageMode mode = StageMode.Waves;

        [Header("외형")]
        [Tooltip("이 스테이지의 보드 그림이다. 비워 두면 씬에 있는 보드를 그대로 쓴다.")]
        public Sprite boardArt;

        [Header("AP")]
        public int apPerTurn = 5;

        [Header("배치")]
        [Tooltip("기물을 배치할 수 있는 줄 수로, 플레이어 쪽 가장자리부터 센다.")]
        public int deployRows = 3;

        [Tooltip("킹이다. 킹을 놓아야 스테이지가 시작된다.")]
        public PieceDefinition kingDefinition;

        [Tooltip("플레이어가 킹 외에 가지고 들어가는 기물이다.")]
        public PieceDefinition[] startingDeck = Array.Empty<PieceDefinition>();

        [Header("웨이브")]
        public WaveDefinition[] waves = Array.Empty<WaveDefinition>();
        [Tooltip("적이 등장하는 줄 수로, 위쪽 가장자리부터 센다.")]
        public int spawnRows = 2;

        [Header("규칙")]
        [Tooltip("적이 킹을 향해 대각선으로도 이동할 수 있다.")]
        public bool enemiesMoveDiagonally = true;

        [Header("체스 대결")]
        [Tooltip("적(흑) 진영의 기물과 그 위치다.")]
        public PiecePlacement[] enemyLayout = Array.Empty<PiecePlacement>();
        [Tooltip("배치 단계에서 런 동안 모은 기물과 별도로 무료로 지급하는 폰이다.")]
        public PieceDefinition freePawn;
        [Min(0)] public int maxFreePawns = 8;
        [Tooltip("플레이어가 둘 수 있는 수의 한도다. 넘기면 대결에서 패배한 것으로 본다.")]
        [Min(1)] public int moveLimit = 100;
        [Tooltip("폰이 맨 끝 랭크에서 승급할 수 있는 기물이다. AI는 항상 첫 번째 항목을 고른다.")]
        public PieceDefinition[] promotionChoices = Array.Empty<PieceDefinition>();
        public AIProfile aiProfile;

        public bool IsChessDuel => mode == StageMode.ChessDuel;
    }
}
