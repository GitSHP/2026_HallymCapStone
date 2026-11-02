using System.Collections;
using Promotion.Core;
using Promotion.Gameplay;
using Promotion.UI;
using UnityEngine;

namespace Promotion.Tutorial
{
    public class DuelStoryDirector : MonoBehaviour  // 최종전(체스 대결)에서 보스(블랙 킹)의 대사를 진행하는 클래스 (Board 오브젝트에 붙어 있음)
    // 흐름: 대결 시작 전 대화창(intro) -> AP 패널 잠김 연출 -> 대결 중 오른쪽 아래 말풍선(플레이어가 잡거나 체크할 때) -> 승리·패배 후 대화창
    // -> 최종전이 아니면 아무것도 하지 않음. 대사는 BossDialogue 에셋, 화면 모양은 BossDialogue·BossReaction 프리팹이 원본
    // -> StageDirector·CombatResolver의 이벤트를 구독만 함 -> 그쪽은 대사가 있는지 모름
    {
        [SerializeField] BossDialogue _dialogue;    // 보스 대사 데이터
        [SerializeField] DialogueView _dialogueViewPrefab;  // 대화창 프리팹 (튜토리얼 대화창의 변형, 초상화가 오른쪽)
        [SerializeField] BossReactionView _reactionPrefab;  // 대결 중 오른쪽 아래 초상화 + 말풍선 프리팹
        [Tooltip("승리·패배가 정해진 뒤 대화창을 띄우기까지 기다리는 시간(실제 시간). 마지막 수의 연출을 보여 주기 위함.")]
        [SerializeField] float _outroDelay = 1.2f;

        StageDirector _stage;   // 최종전인지 확인하고 체크 신호(CheckDeclared)를 받음
        TurnManager _turn;  // 승리·패배 신호(PhaseChanged)를 받음
        CombatResolver _combat; // 잡기 신호(Captured)를 받음

        DialogueView _view; // 만들어 둔 대화창
        BossReactionView _reaction; // 만들어 둔 오른쪽 아래 초상화
        bool _talking;  // 지금 대화창이 떠 있는지 -> 그동안은 말풍선을 띄우지 않음
        bool _outroDone;    // 승리·패배 대사를 이미 띄웠는지 (한 번만)
        int _lastLine = -1; // 직전에 띄운 말풍선 대사 번호 -> 같은 대사가 연달아 나오지 않게

        void Awake()
        {
            _stage = GetComponent<StageDirector>();
            _turn = GetComponent<TurnManager>();
            _combat = GetComponent<CombatResolver>();
        }

        IEnumerator Start()
        {
            // StageDirector는 자신의 Start 한 프레임 뒤에 스테이지를 적용함 -> 기다렸다가 최종전인지 확인
            yield return null;
            yield return null;

            if (_dialogue == null || _stage == null || !_stage.IsChessDuel)  // 최종전이 아니면 이 컴포넌트는 할 일이 없음
            {
                enabled = false;
                yield break;
            }

            if (_dialogueViewPrefab != null)
            {
                _view = Instantiate(_dialogueViewPrefab);   // 최종전에서만 대화창을 만듦 -> 일반 스테이지의 튜토리얼 대화창과 겹치지 않게
                _view.Init(_dialogue.speakerName, _dialogue.portrait, _dialogue.storyBackground, 0.8f, _dialogue.charactersPerSecond);
            }
            if (_reactionPrefab != null)
            {
                _reaction = Instantiate(_reactionPrefab);
                _reaction.Setup(_dialogue.speakerName, _dialogue.portrait);
                _reaction.SetVisible(false);
            }

            _combat.Captured += OnCaptured; // 기물이 잡힐 때마다
            _stage.CheckDeclared += OnCheck;    // 체크가 선언될 때마다
            _turn.PhaseChanged += OnPhaseChanged;   // 승리·패배가 정해질 때 반응하도록 연결

            var hud = FindAnyObjectByType<HudView>();
            if (hud != null)
                hud.HoldDuelApUnlocked();   // 대화 동안 AP 패널을 평소 모습으로 두고

            yield return Talk(_dialogue.intro); // 대결 시작 전 대사

            if (hud != null)
                hud.PlayDuelApLock();   // 대화가 끝나 체스판이 드러나면 AP 패널이 '철컥' 잠기는 연출
        }

        void OnDestroy()
        {
            if (_combat != null)
                _combat.Captured -= OnCaptured;
            if (_stage != null)
                _stage.CheckDeclared -= OnCheck;
            if (_turn != null)
                _turn.PhaseChanged -= OnPhaseChanged;
        }

        IEnumerator Talk(BossLine[] lines)  // 대사 묶음을 대화창으로 한 줄씩 보여 주는 코루틴 -> 끝나면 대화창을 닫고 오른쪽 아래 초상화를 다시 보여 줌
        {
            if (_view == null || lines == null || lines.Length == 0)
            {
                ShowReaction(true);
                yield break;
            }

            _talking = true;
            ShowReaction(false);    // 대화창과 겹치지 않게 오른쪽 아래 초상화는 숨김

            if (lines[0] != null && lines[0].coverBoard)    // 첫 대사가 보드를 가린다면 즉시 가림 -> 보드가 잠깐 비치지 않게
                _view.SetBoardCovered(true);

            foreach (var line in lines)
            {
                if (line == null || string.IsNullOrEmpty(line.text))
                    continue;
                _view.SetBoardCovered(line.coverBoard);
                yield return _view.Say(line.text, line.portrait);   // 대사를 보여 주고 플레이어가 넘길 때까지 기다림
            }

            _view.SetBoardCovered(false);
            _view.HideAll();
            _talking = false;
            ShowReaction(!_turn.IsOver);    // 대결이 끝난 뒤가 아니라면 오른쪽 아래 초상화를 다시 보여 줌
        }

        void ShowReaction(bool visible)
        {
            if (_reaction != null)
                _reaction.SetVisible(visible);
        }

        void OnCaptured(Piece attacker, Piece target)   // 기물이 잡힐 때마다 실행되는 함수 -> 플레이어가 보스의 기물을 잡았다면 말풍선
        {
            if (attacker != null && attacker.Team == Team.Player)
                Bubble(_dialogue.onPlayerCapture);
        }

        void OnCheck(Team checkedSide)  // 체크가 선언될 때마다 실행되는 함수 -> 보스의 킹이 체크당했다면 말풍선 (같은 수의 잡기 대사보다 나중에 와서 덮어씀)
        {
            if (checkedSide == Team.Enemy)
                Bubble(_dialogue.onPlayerCheck);
        }

        void Bubble(string[] pool)  // 대사 풀에서 하나를 무작위로 골라 말풍선으로 띄우는 함수 -> 직전 대사와 같은 것은 피함
        {
            if (_talking || _reaction == null || pool == null || pool.Length == 0 || _turn.IsOver)
                return;

            int pick = Random.Range(0, pool.Length);
            if (pool.Length > 1 && pick == _lastLine)   // 직전과 같은 대사라면 다음 것으로
                pick = (pick + 1) % pool.Length;
            _lastLine = pick;

            _reaction.Say(pool[pick], _dialogue.bubbleSeconds);
        }

        void OnPhaseChanged(TurnPhase phase)    // 턴 단계가 바뀔 때마다 실행되는 함수 -> 승리·패배가 정해지면 마지막 대화를 시작
        {
            if (_outroDone || (phase != TurnPhase.Victory && phase != TurnPhase.Defeat))
                return;

            _outroDone = true;
            StartCoroutine(Outro(phase == TurnPhase.Victory ? _dialogue.victory : _dialogue.defeat));
        }

        IEnumerator Outro(BossLine[] lines) // 마지막 수의 연출을 잠깐 보여 준 뒤 승리·패배 대사를 띄우는 코루틴
        {
            ShowReaction(false);
            yield return new WaitForSecondsRealtime(_outroDelay);
            yield return Talk(lines);
        }
    }
}
