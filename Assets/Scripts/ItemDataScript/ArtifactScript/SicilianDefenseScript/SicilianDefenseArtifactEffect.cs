using Promotion.Core;
using Promotion.Gameplay;
using UnityEngine;

public class SicilianDefenseArtifactEffect : ArtifactEffect   // 킹 처치 보상 아티팩트를 보유하는 동안 동작하는 효과
{
    readonly SicilianDefenseArtifactData _data;   // 이 효과의 설정값(지급할 AP 양)

    public SicilianDefenseArtifactEffect(SicilianDefenseArtifactData data)  // 생성자 -> 어떤 에셋의 효과인지 받아 둠
    {
        _data = data;
    }

    public override void OnActivate()   // 아티팩트를 얻은 순간 -> 기물이 잡히는 이벤트를 듣기 시작
    {
        GameEventManager.PieceCaptured += OnPieceCaptured;
        // 이제 어떤 기물이든 잡힐 때마다 OnPieceCaptured가 호출됨
    }

    public override void OnDeactivate() // 아티팩트를 잃은 순간 -> 기물이 잡히는 이벤트를 듣지 않음
    {
        GameEventManager.PieceCaptured -= OnPieceCaptured;
    }

    void OnPieceCaptured(Piece attacker, Piece target)  // 기물이 잡히면 호출됨 -> 킹이 어떤 적을 잡았는지 확인
    {
        if (attacker == null || target == null) // 공격자 방어자가 전부 널일 때 탈출 (오류 방지)
        {
            return;
        }
        if (attacker.Team != Team.Player)   // 공격자가 플레이어가 아니라면(적이라면) 함수 탈출
        {
            return;
        }
        if (!attacker.IsObjective)  // 공격 기물이 킹이 아니라면 함수 탈출
        {
            return;
        }
        if (target.Team != Team.Enemy)  // 잡힌 기물이 플레이어의 기물이라면(적이 아니라면) 함수 탈출
        {
            return;
        }

        GiveReward();
    }

    void GiveReward()   // AP를 지급하는 함수
    {
        TurnManager turn = Object.FindAnyObjectByType<TurnManager>();
        // 아티팩트는 스테이지(씬)가 바뀌어도 살아 있으므로 TurnManager를 미리 저장하지 않고
        // 보상을 줄 때마다 현재 씬에 있는 TurnManager를 찾음 (QueensGambit의 InventoryManager 찾기와 같은 방식)
        if (turn == null)   // 전투 씬이 아니라면 지급할 곳이 없으므로 종료
        {
            return;
        }

        turn.GainAp(_data.apReward);    // SicilianDefenseArtifactData 내에 있는 apReward 변수에 저장된 값만큼 AP 값을 증가시킴
        Debug.Log("[KingsHuntEffect] 킹 처치 보상 AP +" + _data.apReward);
    }
}