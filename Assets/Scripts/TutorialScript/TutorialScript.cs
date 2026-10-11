using System;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Tutorial
{
    /// <summary>단계가 다음 단계로 넘어가기 전에 기다리는 조건이다.</summary>
    public enum TutorialWait
    {
        /// <summary>대사 한 줄이다. 플레이어가 클릭하면 계속 진행한다.</summary>
        Click,
        KingPlaced,
        /// <summary>플레이어가 시작 버튼을 눌러 1턴이 시작되었다.</summary>
        StageStarted,
        /// <summary>킹 외의 기물이 하나 이상 소환되었다.</summary>
        PieceSummoned,
        Victory
    }

    /// <summary>단계가 시작될 때 튜토리얼이 보드에 가하는 동작이다.</summary>
    public enum TutorialAction
    {
        None,
        /// <summary>스테이지의 다음 웨이브를 즉시 보드에 내보낸다.</summary>
        SpawnNextWave
    }

    [Serializable]
    public class TutorialStep
    {
        [TextArea(2, 5)]
        public string text;

        [Tooltip("Click이면 플레이어가 읽는 대화창을 띄우고, 그 외에는 작은 힌트를 띄운 뒤 " +
                 "플레이어가 해당 행동을 할 때까지 기다린다.")]
        public TutorialWait wait = TutorialWait.Click;

        public TutorialAction action = TutorialAction.None;

        [Tooltip("이 대사의 표정이다. 비우면 기본 초상화를 보여 준다. 대화 줄에서만 쓴다.")]
        public Sprite portrait;

        [Tooltip("이 대사가 떠 있는 동안 대화창 위에 보여 줄 만화 페이지다. 비우면 숨긴다. " +
                 "대화 줄에서만 쓴다.")]
        public Sprite storyFrame;

        [Tooltip("스토리 장면용으로, 이 대사가 떠 있는 동안 보드를 불투명한 배경 뒤에 숨긴다. " +
                 "이 옵션이 꺼진 첫 대사에서 가림막이 걷힌다. 대화 줄에서만 쓴다.")]
        public bool coverBoard;
    }

    /// <summary>
    /// 튜토리얼 전체를 데이터로 담는다. 누가 말하는지, 무엇을 말하는지, 대사 사이에
    /// 플레이어가 무엇을 해야 하는지가 들어 있다. 스토리를 고칠 때는 이 에셋만 수정하면 된다.
    /// </summary>
    [CreateAssetMenu(menuName = "Promotion/Tutorial Script", fileName = "TutorialScript")]
    public class TutorialScript : ScriptableObject
    {
        [Header("화자")]
        public string speakerName = "안내인";
        [Tooltip("기본 상반신 일러스트다. 자체 표정이 없는 대사는 모두 이것을 쓴다. " +
                 "비워 두면 임시 틀을 보여 준다.")]
        public Sprite portrait;

        [Header("스토리 장면")]
        [Tooltip("보드를 가리는 대사에서 대화창 뒤에 보여 준다. 비우면 단색 검정을 쓴다.")]
        public Sprite storyBackground;
        [Tooltip("만화 페이지가 떠 있는 동안 배경이 어두워지는 정도다. 첫 컷과 함께 서서히 어두워지고 " +
                 "마지막 컷 뒤에 다시 밝아지므로, 만화가 없는 대사에서는 배경이 선명하게 보인다.")]
        [Range(0f, 1f)] public float storyBackgroundShade = 0.8f;

        [Header("전투")]
        [Tooltip("튜토리얼에서 치르는 스테이지다.")]
        public StageDefinition stage;

        [Header("텍스트")]
        [Tooltip("초당 표시되는 글자 수다.")]
        [Min(1f)] public float charactersPerSecond = 40f;

        public TutorialStep[] steps = Array.Empty<TutorialStep>();
    }
}
