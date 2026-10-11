using Promotion.Core;
using Promotion.Feel;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// 잡기가 일어나는 유일한 곳이다. 그래서 아군과 적의 잡기가 같은 규칙을 따르고
    /// 같은 연출을 재생한다. 잡기는 체스처럼 위치 기반이다. 기물에 도달하면 잡는다.
    /// 체력은 없다.
    /// </summary>
    public class CombatResolver : MonoBehaviour
    {
        BoardGrid _grid;
        BoardState _state;
        PieceRegistry _registry;

        void Awake() => Resolve();

        void Resolve()
        {
            if (_grid == null) _grid = GetComponent<BoardGrid>();
            if (_state == null) _state = GetComponent<BoardState>();
            if (_registry == null) _registry = GetComponent<PieceRegistry>();
        }

        /// <summary>이동 없이 잡는다. 공격자가 전진할 수 없을 때 쓴다.</summary>
        public bool Capture(Piece attacker, Piece target)
        {
            Resolve();

            if (!CanCapture(target))
                return false;

            var attackerView = _registry.GetView(attacker);
            var targetView = _registry.GetView(target);

            if (attackerView != null)
                Fx.Lunge(attackerView.transform, _grid.CoordToWorld(target.Coord),
                    () => Finish(target, targetView));
            else
                Finish(target, targetView);

            return true;
        }

        /// <summary>
        /// 대상을 잡고 공격자를 그 칸으로 옮긴다. 일반적인 체스의 잡기다.
        /// 보드 상태는 즉시 바뀐다. 연출은 단일 시퀀스여서 다른 어떤 것도
        /// 공격자의 위치를 움직이지 않는다.
        /// </summary>
        public bool CaptureInto(Piece attacker, Piece target, Coord destination)
        {
            Resolve();

            if (attacker == null || !CanCapture(target))
                return false;

            var attackerView = _registry.GetView(attacker);
            var targetView = _registry.GetView(target);
            var victimWorld = _grid.CoordToWorld(target.Coord);

            _state.Move(attacker, destination);

            if (attackerView == null)
            {
                Finish(target, targetView);
                return true;
            }

            Fx.CaptureAdvance(attackerView.transform, victimWorld, _grid.CoordToWorld(destination),
                transform, attackerView.BaseScale, () => Finish(target, targetView));

            return true;
        }

        // 킹은 절대 잡히지 않는다. 대신 적이 킹의 칸에 도달하면 적이 이긴다.
        static bool CanCapture(Piece target) => target != null && !target.IsObjective;

        void Finish(Piece target, PieceView targetView)
        {
            if (targetView != null)
                targetView.Flash();

            _registry.Kill(target);
            Fx.HitStop();
        }
    }
}
