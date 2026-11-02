using System;
using UnityEngine;

namespace Promotion.Feel
{
    public enum SfxId   // 게임에서 쓰는 효과음 종류
    {
        Move,   // 기물이 칸에 내려앉을 때 (소환, 이동, 적 등장·이동)
        Capture,    // 기물이 상대 기물을 잡는 순간
        UiClick,    // 버튼·카드·슬롯 같은 UI를 눌렀을 때
        Shatter,    // 잡힌 기물이 파편으로 부서지는 순간 (잡기 동작이 끝나고 타격 이펙트가 터질 때)
        Lock,   // 최종전에서 AP 패널이 잠기는 순간 (쇠가 맞물리는 소리)
        // -> SfxLibrary 에셋에는 순번(숫자)으로 저장되므로 새 종류는 항상 맨 뒤에 추가할 것
    }

    [Serializable]
    public class SfxEntry   // 효과음 한 종류의 설정 -> 소리 파일 여러 개 중 하나를 무작위로 골라 재생함 (같은 소리 반복으로 귀가 지치지 않게)
    {
        public SfxId id;    // 어떤 효과음인지
        public AudioClip[] clips = Array.Empty<AudioClip>();    // 후보 소리 파일들
        [Range(0f, 1f)] public float volume = 0.8f; // 재생 음량
        [Tooltip("재생할 때마다 음높이를 이 범위에서 무작위로 고른다. 1이 원래 높이.")]
        public Vector2 pitch = new Vector2(0.95f, 1.05f);   // 음높이 범위
    }

    [CreateAssetMenu(menuName = "Promotion/Sfx Library", fileName = "SfxLibrary")]
    public class SfxLibrary : ScriptableObject  // 효과음 파일과 음량·음높이를 한곳에 모은 데이터 (Resources/SfxLibrary 에 있어야 Sfx가 불러옴)
    // -> 소리를 바꾸거나 크기를 조절할 때 코드를 건드리지 않고 이 에셋 인스펙터에서 함
    {
        [Range(0f, 1f)] public float masterVolume = 1f; // 모든 효과음의 전체 음량
        public SfxEntry[] entries = Array.Empty<SfxEntry>();    // 효과음 종류별 설정 목록

        public SfxEntry Find(SfxId id)  // 효과음 종류에 맞는 설정을 찾는 함수 -> 없으면 null
        {
            foreach (var entry in entries)
                if (entry != null && entry.id == id)
                    return entry;
            return null;
        }
    }
}
