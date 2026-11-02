using System;
using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class KillStreakTracker : MonoBehaviour  // 플레이어 기물이 이번 스테이지에서 잡은 적의 수를 세고, 기준 수 이상이면 기물에 불을 붙이는 클래스 (Board 오브젝트에 붙어 있음)
    // 규칙: 잡을 때마다 +1, 턴이 지나도 줄어들지 않고 스테이지 동안 계속 누적됨
    // -> 기준 수(TweenPresets.killStreakThreshold, 기본 3) 이상이 되면 불이 붙고, 그 스테이지가 끝날 때까지(또는 기물이 잡힐 때까지) 계속 탐
    // -> 스테이지마다 기물을 새로 소환하므로 기록도 자연스럽게 새로 시작됨
    // -> 연출 전용이라 AP·이동·잡기 규칙에는 영향이 없음. 최종전(체스 대결)에서는 동작하지 않음
    // -> 킬 수 규칙은 전부 이 파일에만 있음. CombatResolver는 "누가 잡았다"는 신호만 보내고 이 클래스가 있는지 모름
    {
        public event Action<Piece, Piece, int> KillCounted; // 플레이어 기물의 킬이 세어질 때마다 발생하는 이벤트 -> (잡은 기물, 잡힌 기물, 이번 스테이지 누적 킬 수)
        // -> 처치 강조 문구(KillCalloutView)가 구독함

        [Tooltip("불붙은 기물에 붙일 불꽃 입자 묶음 프리팹.")]
        [SerializeField] FireAura _fireAura;

        CombatResolver _combat; // 잡기 신호(Captured)를 보내는 컴포넌트
        StageDirector _stage;   // 지금 스테이지가 최종전인지 확인하는 데 사용
        BoardState _state;  // 보드 위 기물 목록
        PieceRegistry _registry;    // 기물 데이터 -> 화면 오브젝트(PieceView)를 찾는 데 사용

        void Awake()
        {
            _combat = GetComponent<CombatResolver>();
            _stage = GetComponent<StageDirector>();
            _state = GetComponent<BoardState>();
            _registry = GetComponent<PieceRegistry>();
        }

        void OnEnable()
        {
            if (_combat != null)
                _combat.Captured += OnCaptured; // 잡기가 확정될 때마다 OnCaptured가 실행되게 연결
        }

        void OnDisable()
        {
            if (_combat != null)
                _combat.Captured -= OnCaptured;
        }

        bool Active // 킬 수를 세는 스테이지인지 여부 -> 최종전에서는 끔
        {
            get { return _stage == null || !_stage.IsChessDuel; }
        }

        void OnCaptured(Piece attacker, Piece target)
        // 잡기가 확정될 때마다 실행되는 함수 -> 잡은 기물이 플레이어 기물이면 킬 수 +1
        // attacker: 잡은 기물, target: 잡힌 기물 (CombatResolver.Capture / CaptureInto에서 보냄)
        {
            if (!Active || attacker == null || attacker.Team != Team.Player)    // 최종전이거나 적이 잡은 경우는 세지 않음
                return;

            attacker.Kills++;   // 이번 스테이지 누적 킬 +1
            KillCounted?.Invoke(attacker, target, attacker.Kills);  // 킬이 세어졌음을 알림 -> 처치 문구가 뜸

            if (attacker.Kills >= Fx.P.killStreakThreshold)    // 기준 수 이상이면 불을 붙임 (이미 붙어 있으면 PieceView가 무시함)
                SetBurning(attacker, true);
        }

        public bool DebugToggleAll()
        // 개발자 도구(Shift+5)용 -> 보드 위 플레이어 기물 전체의 불을 한꺼번에 켜고 끄는 함수. 켰으면 true
        // -> 연출 확인용이라 킬 수도 함께 맞춤 (켜면 기준 수, 끄면 0)
        {
            bool anyBurning = false;
            foreach (var piece in _state.AllPieces) // 하나라도 불이 붙어 있으면 이번에는 끄는 쪽
            {
                var view = _registry.GetView(piece);
                if (piece.Team == Team.Player && view != null && view.Burning)
                    anyBurning = true;
            }

            bool on = !anyBurning;
            foreach (var piece in _state.AllPieces)
            {
                if (piece.Team != Team.Player || piece.IsObjective) // 킹은 잡지 않으므로 제외
                    continue;
                piece.Kills = on ? Fx.P.killStreakThreshold : 0;
                SetBurning(piece, on);
            }
            return on;
        }

        void SetBurning(Piece piece, bool on)   // 기물의 화면 오브젝트를 찾아 불을 켜고 끄는 함수
        {
            var view = _registry.GetView(piece);
            if (view != null)
                view.SetBurning(on, _fireAura, transform);  // 점화 순간 Board(이 오브젝트)를 흔듦
        }
    }
}
