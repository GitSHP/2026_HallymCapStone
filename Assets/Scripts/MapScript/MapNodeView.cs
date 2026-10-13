using Promotion.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.Map
{
    public class MapNodeView : MonoBehaviour // MapNode 프리팹에 붙는 지도 노드 하나의 화면 구성 요소
    // -> MapScreen이 노드마다 이 프리팹을 복제하고, 각 요소에 무엇을 보여 줄지 정해 줌 (이 스크립트는 참조만 들고 있음)
    {
        [Tooltip("선택할 수 있는 노드나 플레이어가 서 있는 노드 뒤에 깔리는 빛이다.")]
        public Image halo; // 노드 뒤에 깔리는 후광 이미지 (선택 가능/현재 위치일 때만 켜짐)
        public Image face; // 노드의 바탕(원판) 이미지
        public Image icon; // 노드 종류를 나타내는 아이콘 이미지 (칼 = 전투, 가방 = 상점, 왕관 = 보스)
        [Tooltip("이미 지나온 노드에 표시되는 체크다.")]
        public GameObject visited; // 이미 지나온 노드에 띄우는 체크 표시 오브젝트
        [Tooltip("노드를 선택할 수 있을 때만 활성화된다.")]
        public UiButton button; // 노드를 클릭할 때 쓰는 버튼 (선택 가능한 노드에서만 켜짐)
        [Tooltip("아직 도달하지 않은 노드를 흐리게 한다.")]
        public CanvasGroup group; // 노드 전체의 투명도를 한 번에 조절하기 위한 그룹 -> 아직 못 가는 노드를 흐리게 표시

        public RectTransform Rect => (RectTransform)transform; // 이 노드의 RectTransform (UI 위치·크기 조절용)
    }
}
