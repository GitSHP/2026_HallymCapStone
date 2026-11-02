using System;
using UnityEngine;

namespace Promotion.Tutorial
{
    [Serializable]
    public class BossLine   // 대화창에 띄울 보스 대사 한 줄
    {
        [TextArea(2, 4)]
        public string text; // 대사
        [Tooltip("이 대사의 표정 그림이다. 비우면 기본 초상화를 쓴다.")]
        public Sprite portrait; // 이 대사에서만 쓸 그림 (비우면 기본 초상화)
        [Tooltip("이 대사 동안 보드를 배경 뒤에 숨긴다. 꺼진 첫 대사에서 보드가 드러난다.")]
        public bool coverBoard = true;  // 이 대사 동안 보드를 가릴지 여부
    }

    [CreateAssetMenu(menuName = "Promotion/Boss Dialogue", fileName = "BossDialogue")]
    public class BossDialogue : ScriptableObject    // 최종전 보스(블랙 킹)의 대사 전체를 담는 데이터 (대본)
    // -> 대결 전·승리 후·패배 후에는 대화창으로, 대결 중에는 오른쪽 아래 말풍선으로 대사를 보여 줌
    // -> 대사를 고칠 때는 코드가 아니라 이 에셋만 수정하면 됨
    {
        [Header("화자")]
        public string speakerName = "블랙 킹";   // 대화창·말풍선에 표시할 이름
        [Tooltip("대화창과 말풍선 옆에 보여 줄 기본 상반신 그림이다.")]
        public Sprite portrait; // 기본 초상화 (상반신)
        [Tooltip("보드를 가리는 대사에서 대화창 뒤에 깔 배경이다. 비우면 단색 검정을 쓴다.")]
        public Sprite storyBackground;  // 보드를 가릴 때 깔리는 배경 그림
        [Min(1f)] public float charactersPerSecond = 32f;   // 대사 타이핑 속도 (초당 글자 수) -> 튜토리얼보다 조금 느리게 해서 무게감을 줌

        [Header("대화창 (클릭해서 넘김)")]
        public BossLine[] intro = Array.Empty<BossLine>();  // 대결 시작 전 대사
        public BossLine[] victory = Array.Empty<BossLine>();    // 플레이어가 이겼을 때(체크메이트) 대사
        public BossLine[] defeat = Array.Empty<BossLine>(); // 플레이어가 졌을 때 대사

        [Header("대결 중 말풍선 (풀에서 무작위로 하나)")]
        [TextArea(1, 3)] public string[] onPlayerCapture = Array.Empty<string>();   // 플레이어가 보스의 기물을 잡았을 때
        [TextArea(1, 3)] public string[] onPlayerCheck = Array.Empty<string>(); // 플레이어가 보스의 킹을 체크했을 때
        [Tooltip("말풍선이 떠 있는 시간(초).")]
        [Min(0.5f)] public float bubbleSeconds = 2.6f;  // 말풍선이 보이는 시간
    }
}
