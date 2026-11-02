using UnityEngine;

public abstract class ArtifactData : ItemData
// 모든 아티팩트 아이템 객체의 공통 부모
// 모든 아티팩트 아이템이 발동하는 효과들의 기능들을 정의
// -> 실행 중에 바뀌는 값(카운터 등)은 에셋이 아니라 여기에 저장함
{
    public abstract ArtifactEffect CreateEffect();  
    // 이 아티팩트를 보유하는 동안 동작할 효과를 정의하는 함수

    protected virtual void OnValidate() 
    // 인스펙터에서 값이 바뀔 때 에디터가 호출 -> 종류를 항상 Artifact로 고정
    {
        itemType = ItemType.Artifact;
    }
}
