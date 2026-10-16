using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class CombatResolver : MonoBehaviour   // 잡기를 처리하는 클래스
    // 이 게임에는 체력이 없음 -> 상대 기물이 있는 칸에 도달하면 그 기물을 잡고(제거) 그 칸을 차지함(체스와 같음)
    {
        BoardGrid _grid;    // 칸 좌표 -> 월드 좌표 변환용
        BoardState _state;  // 보드의 기물 위치 데이터
        PieceRegistry _registry;    // 기물 데이터와 화면 오브젝트를 짝지어 관리하는 컴포넌트

        void Awake() => Resolve();  // 오브젝트가 생성될 때 한 번 실행 -> 필요한 컴포넌트를 찾아 둠

        void Resolve()  // 같은 오브젝트에 붙어 있는 컴포넌트를 찾아 변수에 저장하는 함수 -> 이미 찾은 것은 건너뜀
        {
            if (_grid == null) _grid = GetComponent<BoardGrid>();
            if (_state == null) _state = GetComponent<BoardState>();
            if (_registry == null) _registry = GetComponent<PieceRegistry>();
        }

        public bool Capture(Piece attacker, Piece target)   // 공격자는 제자리에 남고 대상만 잡는 함수 -> 잡았으면 true
        // 공격자가 대상 쪽으로 찌르듯 다녀오는 연출만 하고 칸 이동은 하지 않음
        {
            Resolve();  // 다른 컴포넌트의 Awake보다 먼저 불릴 수 있으므로 컴포넌트 확인

            if (!CanCapture(target))    // 잡을 수 없는 대상(없음 또는 킹)이라면 실패
                return false;

            LoseFromInventory(target);  // 잡히는 것이 확정된 순간 인벤토리 장부부터 정리 -> 연출이 중간에 끊겨도 장부는 정확함

            var attackerView = _registry.GetView(attacker); // 공격자의 화면 오브젝트
            var targetView = _registry.GetView(target); // 대상의 화면 오브젝트

            if (attackerView != null)   // 공격자 오브젝트가 있다면 찌르는 연출을 하고, 연출이 대상에 닿는 순간 대상을 제거
                Fx.Lunge(attackerView.transform, _grid.CoordToWorld(target.Coord),
                    () => Finish(target, targetView));
            else    // 공격자 오브젝트가 없다면 연출 없이 바로 제거
                Finish(target, targetView);

            return true;
        }

        public bool CaptureInto(Piece attacker, Piece target, Coord destination)    // 공격자가 대상의 칸(destination)으로 들어가며 잡는 함수 -> 잡았으면 true
        {
            Resolve();

            if (attacker == null || !CanCapture(target))    // 공격자가 없거나 잡을 수 없는 대상이라면 실패
                return false;

            LoseFromInventory(target);  // 잡히는 것이 확정된 순간 인벤토리 장부부터 정리

            var attackerView = _registry.GetView(attacker);
            var targetView = _registry.GetView(target);
            var victimWorld = _grid.CoordToWorld(target.Coord); // 대상이 있던 위치(연출용) -> 데이터를 옮기기 전에 미리 저장

            _state.Move(attacker, destination); // 보드 데이터에서 공격자를 목적지로 옮김(데이터는 즉시, 화면은 연출로 따라감)

            if (attackerView == null)   // 공격자 오브젝트가 없다면 연출 없이 바로 대상 제거
            {
                Finish(target, targetView);
                return true;
            }

            Fx.CaptureAdvance(attackerView.transform, victimWorld, _grid.CoordToWorld(destination),
                transform, attackerView.BaseScale, () => Finish(target, targetView));
            // 공격자가 대상에게 달려들어 부딪히고 목적지에 자리 잡는 연출 -> 부딪히는 순간 대상을 제거

            return true;
        }

        static void LoseFromInventory(Piece target) // 잡힌 기물이 인벤토리에서 꺼낸 보유 기물이면 런 인벤토리에서 영구히 지우는 함수
        {
            if (target.Team != Team.Player || !target.Owned)   // 플레이어의 보유 기물이 아니라면(적, 킹·상시 폰 같은 임시 기물) 장부와 무관
                return;

            target.Owned = false;   // 먼저 보유 표시를 끔 -> 연출이 끝나 보드에서 사라지기 전에도 "보드에 나가 있는 보유 기물"로 세지 않게 함
            RunState.Instance.RemovePiece(target.Def);   // 런 인벤토리에서 하나 지움 -> 다음 스테이지에 돌아오지 않음 (인벤토리는 이 신호로 다시 그려짐)
        }

        static bool CanCapture(Piece target) => target != null && !target.IsObjective;    // 잡을 수 있는 대상인지 검사하는 함수 -> 킹(목표 기물)은 잡히지 않음

        void Finish(Piece target, PieceView targetView) // 잡힌 기물을 실제로 제거하는 함수
        {
            if (targetView != null) // 대상 오브젝트가 있다면 맞았다는 표시로 번쩍이게 하고
                targetView.Flash();

            _registry.Kill(target); // 보드 데이터와 화면에서 대상을 제거하고
            Fx.HitStop();   // 타격감을 위해 아주 잠깐 화면을 멈춤
        }
    }
}
