using System;
using Promotion.Core;
using UnityEngine;

namespace Promotion.Gameplay
{
    public class TurnManager : MonoBehaviour  // 현재 턴 단계(배치/플레이어 턴/적 턴/승리/패배), 턴 수, AP를 관리하는 클래스
    // AP(행동력): 플레이어 턴마다 가득 채워지는 포인트 -> 기물 이동과 소환에 소모됨
    {
        public event Action<TurnPhase> PhaseChanged;    // 턴 단계가 바뀔 때 발생하는 이벤트 -> 새 단계를 넘겨줌(상점, HUD가 구독)
        public event Action<int, int> ApChanged;    // AP가 바뀔 때 발생하는 이벤트 -> 현재 AP와 최대 AP를 넘겨줌
        public event Action<int> TurnChanged;   // 턴 수가 바뀔 때 발생하는 이벤트 -> 현재 턴 수를 넘겨줌

        public TurnPhase Phase { get; private set; } = TurnPhase.Deploy;    // 현재 턴 단계(처음엔 배치 단계)
        public int Turn { get; private set; }   // 현재 턴 수(배치 단계는 0, 첫 플레이어 턴이 1)
        public int Ap { get; private set; } // 현재 남은 AP
        public int MaxAp { get; private set; } = 5; // 한 턴에 받는 최대 AP

        public bool IsOver => Phase == TurnPhase.Victory || Phase == TurnPhase.Defeat;  // 스테이지가 끝났는지(승리 또는 패배) 여부

        public bool ApDisabled { get; private set; }    // true면 AP 비용을 무시하고 "한 턴에 한 수"로만 계산(체스 대결용)

        public void Configure(int apPerTurn)    // 일반 스테이지용 설정 -> 한 턴에 받는 AP를 정하는 함수
        {
            ApDisabled = false;
            MaxAp = Mathf.Max(1, apPerTurn);    // 최소 1 AP는 보장
        }

        public void ConfigureOneMovePerTurn()   // 체스 대결용 설정 -> 기물 비용과 상관없이 한 턴에 딱 한 수만 두도록 하는 함수
        {
            ApDisabled = true;
            MaxAp = 1;
        }

        public void ResetForStage() // 스테이지 시작 시 턴 정보를 초기 상태(배치 단계)로 되돌리는 함수
        {
            Turn = 0;
            Ap = 0;
            Phase = TurnPhase.Deploy;
            PhaseChanged?.Invoke(Phase);    // 바뀐 정보를 모두 알림 -> HUD가 처음 상태로 갱신됨
            TurnChanged?.Invoke(Turn);
            ApChanged?.Invoke(Ap, MaxAp);
        }

        public void SetPhase(TurnPhase phase)   // 턴 단계를 바꾸는 함수
        {
            if (Phase == phase) // 같은 단계라면 아무것도 하지 않음 -> 이벤트가 중복 발생하지 않게
                return;

            Phase = phase;
            PhaseChanged?.Invoke(phase);    // 단계가 바뀌었음을 알림
        }

        public void BeginPlayerTurn()   // 새 플레이어 턴을 시작하는 함수 -> 턴 수 +1, AP를 가득 채움
        {
            Turn++;
            Ap = MaxAp;
            TurnChanged?.Invoke(Turn);
            ApChanged?.Invoke(Ap, MaxAp);
            SetPhase(TurnPhase.PlayerTurn);
        }

        public void RefillAp()  // AP를 최대치로 다시 채우는 함수(DevTools F6용)
        {
            Ap = MaxAp;
            ApChanged?.Invoke(Ap, MaxAp);
        }

        public bool CanAfford(int cost) // 지금 cost만큼의 행동을 할 수 있는지 검사하는 함수
            => Phase == TurnPhase.PlayerTurn && (ApDisabled ? Ap > 0 : cost <= Ap);
        // 플레이어 턴이어야 하고, 체스 대결이면 남은 수가 있는지, 아니면 AP가 충분한지 확인

        public bool TrySpend(int cost)  // AP를 소모해 보는 함수 -> 소모했으면 true, 부족하면 false
        {
            if (!CanAfford(cost))   // 낼 수 없다면 실패
                return false;

            Ap -= ApDisabled ? 1 : cost;    // 체스 대결이면 1(한 수), 아니면 비용만큼 차감
            ApChanged?.Invoke(Ap, MaxAp);   // AP가 바뀌었음을 알림 -> HUD의 AP 바 갱신
            return true;
        }
    }
}
