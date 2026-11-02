public abstract class ArtifactEffect
// 아티팩트의 획득, 버릴 시에 발생하는 기능을 정의한 스크립트
// 아티팩트 획득 시 GameEventManger가 알려주는 이벤트를 듣고
// 아티팩트 버릴 시 GameEventManger가 알려주는 이벤트를 듣지 않음
{
    public abstract void OnActivate();      
    // 아티팩트를 얻었을 때 호출(구매) -> GameEventManager 이벤트 구독
    // 이 함수를 호출하면 아티팩트 아이템이 GameEventManager에서 발생했다고 알려주는 이벤트들이 어떤 것이 있는지 확인하게 만듬
    // 아티팩트가 이벤트를 확인하고 있다가 특정 조건이 되면 효과가 발동되게 함
    public abstract void OnDeactivate();    
    // 아티팩트를 잃었을 때(판매, 버리기) 호출 -> 구독 해제 (반드시 OnActivate와 짝을 맞춰야 함)
    // 이 함수를 호출하면 아티팩트 아이템이 GameEventManager에서 발생했다고 알려주는 이벤트들을 듣지 않음
}
