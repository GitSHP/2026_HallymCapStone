using UnityEngine;
using System.Collections.Generic;

public class ArtifactManager    
// 보유 중인 아티팩트의 효과를 괸리하는 스크립트 -> RunState가 하나 들고 있음
{
    class ActiveArtifact    
    // 어떤 아티팩트 아이템(data)이 어떤 효과(effect)를 가지고 있는지 저장하는 InnerClass
    {
        public ItemData data;   // 아티팩트 아이템 데이터
        public ArtifactEffect effect;   // 아티팩트 아이템 효과
    }

    readonly List<ActiveArtifact> _active = new List<ActiveArtifact>();    
    // 지금 켜져 있는 아티팩트 아이템 효과 목록

    public void Activate(ItemData artifact) // 아티팩트를 얻었을 때 -> 효과를 만든 후 켬
    {
        ArtifactData data = artifact as ArtifactData;   // 저장하려는 아이템이 아티팩트인지 체크
        if (data == null)   // 아티팩트가 아닌 다른 아이템이라면 함수 종료 
        {
            return;
        }

        ArtifactEffect effect = data.CreateEffect();    // 이 아티팩트가 가지고 있는 효과를 저장
        if (effect == null) // 효과를 저장하지 못했다면 함수 종료
        {
            return;
        }
        
        ActiveArtifact entry = new ActiveArtifact();
        entry.data = artifact;  // 효과를 활성화하고자 하는 아티팩트에 대한 정보와 효과를 저장
        entry.effect = effect; 
        _active.Add(entry); // 현재 켜져있는 아티팩트 아이템 효과 목록에 저장

        effect.OnActivate();    // 효과 시작 (이벤트 구독)
        // 즉 위 과정에서 entry.data에서는 아티팩트 아이템의 데이터가 
        // entry.effect에서는 data에 저장된 아이템이 발동할 효과가 저장된다
        // 그 후 _active 라는 리스트 변수에 현재 효과가 발동 중인 아티팩트들을 저장한다
    }

    public void Deactivate(ItemData artifact)   
    // 아티팩트를 잃었을 때 -> 그 아티팩트 아이템 효과를 끔
    {
        for (int i = 0; i < _active.Count; i++) // 현재 활성화되어있는 모든 아티팩트 목록을 검사
        {
            if (_active[i].data == artifact)    // 그 중 비활성화하고자 하는 아티팩트를 찾았다면
            {
                _active[i].effect.OnDeactivate();   // 아티팩트 효과 비활성화 (구독 해제)
                _active.RemoveAt(i);    // 활성화된 아티팩트 목록에서 지움
                return; // 하나만 끄고 종료 -> 같은 아티팩트가 두 개였다면 나머지 하나는 계속 동작
            }
        }
    }

    public void DeactivateAll() 
    // 모든 아티팩트 효과를 끄는 함수 -> 새로운 런을 시작할 때 사용
    {
        for (int i = 0; i < _active.Count; i++)
        {
            _active[i].effect.OnDeactivate();
        }
        _active.Clear();
    }
}

