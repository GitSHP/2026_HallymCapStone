using Gambonanza.Core;
using Gambonanza.Feel;
using UnityEngine;

namespace Gambonanza.Gameplay
{
    /// <summary>
    /// The single place captures happen, so player and enemy captures obey the
    /// same rule and play the same animation. Capture is positional, as in
    /// chess: reaching a piece takes it. There are no hit points.
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

        /// <summary>Captures without moving — used when the attacker cannot advance.</summary>
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
        /// Captures the target and moves the attacker onto its square — the
        /// normal chess capture. Board state changes at once; the animation is a
        /// single sequence so nothing else drives the attacker's position.
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

        // The king is never captured — enemies win by reaching its tile instead.
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
