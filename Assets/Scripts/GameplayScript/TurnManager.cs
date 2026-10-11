using System;
using Promotion.Core;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// 누구의 턴인지와 플레이어가 아직 얼마나 행동할 수 있는지를 보관한다.
    /// 자체 흐름은 갖지 않는다. 진행 순서는 StageDirector가 이끌고, 이 클래스는
    /// UI와 입력이 반응할 수 있도록 상태를 알린다.
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

        /// <summary>
        /// 체스 대결에는 AP가 없다. 기물에 상관없이 한 턴에 한 수다.
        /// HUD는 이 값을 읽어 바를 잠긴 상태로 표시한다.
        /// </summary>
        public bool ApDisabled { get; private set; }

        public void Configure(int apPerTurn)
        {
            ApDisabled = false;
            MaxAp = Mathf.Max(1, apPerTurn);
        }

        public void ConfigureOneMovePerTurn()
        {
            ApDisabled = true;
            MaxAp = 1;
        }

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

        /// <summary>턴을 넘기지 않고 AP 바를 다시 가득 채운다.</summary>
        public void RefillAp()
        {
            Ap = MaxAp;
            ApChanged?.Invoke(Ap, MaxAp);
        }

        public bool CanAfford(int cost)
            => Phase == TurnPhase.PlayerTurn && (ApDisabled ? Ap > 0 : cost <= Ap);

        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost))
                return false;

            Ap -= ApDisabled ? 1 : cost;
            ApChanged?.Invoke(Ap, MaxAp);
            return true;
        }
    }
}
