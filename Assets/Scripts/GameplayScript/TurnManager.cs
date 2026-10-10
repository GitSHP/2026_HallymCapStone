using System;
using Promotion.Core;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// Holds whose turn it is and how much the player may still do.
    /// It owns no flow of its own — StageDirector drives the sequence and this
    /// reports the state so UI and input can react.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public event Action<TurnPhase> PhaseChanged;
        public event Action<int, int> ApChanged;
        public event Action<int> TurnChanged;

        public TurnPhase Phase { get; private set; } = TurnPhase.Deploy;
        public int Turn { get; private set; }
        public int Ap { get; private set; }
        public int MaxAp { get; private set; } = 5;

        public bool IsOver => Phase == TurnPhase.Victory || Phase == TurnPhase.Defeat;

        public void Configure(int apPerTurn) => MaxAp = Mathf.Max(1, apPerTurn);

        public void ResetForStage()
        {
            Turn = 0;
            Ap = 0;
            Phase = TurnPhase.Deploy;
            PhaseChanged?.Invoke(Phase);
            TurnChanged?.Invoke(Turn);
            ApChanged?.Invoke(Ap, MaxAp);
        }

        public void SetPhase(TurnPhase phase)
        {
            if (Phase == phase)
                return;

            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        public void BeginPlayerTurn()
        {
            Turn++;
            Ap = MaxAp;
            TurnChanged?.Invoke(Turn);
            ApChanged?.Invoke(Ap, MaxAp);
            SetPhase(TurnPhase.PlayerTurn);
        }

        /// <summary>Tops the bar back up without advancing the turn.</summary>
        public void RefillAp()
        {
            Ap = MaxAp;
            ApChanged?.Invoke(Ap, MaxAp);
        }

        public bool CanAfford(int cost) => Phase == TurnPhase.PlayerTurn && cost <= Ap;

        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost))
                return false;

            Ap -= cost;
            ApChanged?.Invoke(Ap, MaxAp);
            return true;
        }
    }
}
