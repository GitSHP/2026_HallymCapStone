using System;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Tutorial
{
    public enum TutorialWait // 튜토리얼 단계가 다음 단계로 넘어가기 전에 기다리는 조건
    {
        Click,         // 대사 한 줄 -> 플레이어가 클릭(또는 스페이스/엔터)하면 다음으로 진행
        KingPlaced,    // 플레이어가 킹을 보드에 놓을 때까지 기다림
        StageStarted,  // 플레이어가 시작 버튼을 눌러 1턴이 시작될 때까지 기다림
        PieceSummoned, // 킹 외의 기물이 하나 이상 소환될 때까지 기다림
        Victory        // 스테이지에서 승리할 때까지 기다림
    }

    public enum TutorialAction // 단계가 시작될 때 튜토리얼이 보드에 가하는 동작
    {
        None,         // 아무 동작 없음
        SpawnNextWave // 스테이지의 다음 웨이브를 즉시 보드에 내보냄
    }

    [Serializable]
    public class TutorialStep // 튜토리얼의 한 단계 (대사 한 줄 또는 행동 안내 한 개)
    {
        [TextArea(2, 5)]
        public string text; // 이 단계에서 보여 줄 대사 또는 힌트 문구

        [Tooltip("Click이면 플레이어가 읽는 대화창을 띄우고, 그 외에는 작은 힌트를 띄운 뒤 " +
                 "플레이어가 해당 행동을 할 때까지 기다린다.")]
        public TutorialWait wait = TutorialWait.Click; // 다음 단계로 넘어가는 조건 (기본값 = 클릭)

        public TutorialAction action = TutorialAction.None; // 이 단계가 시작될 때 실행할 동작

        [Tooltip("이 대사의 표정이다. 비우면 기본 초상화를 보여 준다. 대화 줄에서만 쓴다.")]
        public Sprite portrait; // 이 대사에서만 쓸 화자 표정 그림 (비우면 기본 초상화)

        [Tooltip("이 대사가 떠 있는 동안 대화창 위에 보여 줄 만화 페이지다. 비우면 숨긴다. " +
                 "대화 줄에서만 쓴다.")]
        public Sprite storyFrame; // 이 대사와 함께 띄울 만화 페이지 그림 (비우면 만화 숨김)

        [Tooltip("스토리 장면용으로, 이 대사가 떠 있는 동안 보드를 불투명한 배경 뒤에 숨긴다. " +
                 "이 옵션이 꺼진 첫 대사에서 가림막이 걷힌다. 대화 줄에서만 쓴다.")]
        public bool coverBoard; // 이 대사 동안 보드를 가림막으로 가릴지 여부
    }

    [CreateAssetMenu(menuName = "Promotion/Tutorial Script", fileName = "TutorialScript")]
    public class TutorialScript : ScriptableObject // 튜토리얼 전체를 담는 데이터 에셋 (대본)
    // -> 누가 말하는지, 무엇을 말하는지, 대사 사이에 플레이어가 무엇을 해야 하는지가 들어 있음
    // -> 스토리를 고칠 때는 코드가 아니라 이 에셋만 수정하면 됨
    {
        [Header("화자")]
        public string speakerName = "안내인"; // 대화창에 표시할 화자 이름
        [Tooltip("기본 상반신 일러스트다. 자체 표정이 없는 대사는 모두 이것을 쓴다. " +
                 "비워 두면 임시 틀을 보여 준다.")]
        public Sprite portrait; // 화자의 기본 초상화 그림

        [Header("스토리 장면")]
        [Tooltip("보드를 가리는 대사에서 대화창 뒤에 보여 준다. 비우면 단색 검정을 쓴다.")]
        public Sprite storyBackground; // 보드를 가리는 스토리 장면에서 깔리는 배경 그림
        [Tooltip("만화 페이지가 떠 있는 동안 배경이 어두워지는 정도다. 첫 컷과 함께 서서히 어두워지고 " +
                 "마지막 컷 뒤에 다시 밝아지므로, 만화가 없는 대사에서는 배경이 선명하게 보인다.")]
        [Range(0f, 1f)] public float storyBackgroundShade = 0.8f; // 만화가 떠 있을 때 배경 위 음영의 진하기 (0 = 투명, 1 = 완전히 검정)

        [Header("전투")]
        [Tooltip("튜토리얼에서 치르는 스테이지다.")]
        public StageDefinition stage; // 튜토리얼 전투에 쓰는 스테이지 데이터

        [Header("텍스트")]
        [Tooltip("초당 표시되는 글자 수다.")]
        [Min(1f)] public float charactersPerSecond = 40f; // 대사 타이핑 효과의 속도 (초당 글자 수)

        public TutorialStep[] steps = Array.Empty<TutorialStep>(); // 튜토리얼 단계 목록 -> 위에서부터 순서대로 진행됨
    }
}
