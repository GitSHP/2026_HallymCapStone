using Promotion.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.Map
{
    /// <summary>
    /// MapNode 프리팹에 배치된 지도 노드 하나의 구성 요소다. 지도 화면이 노드마다
    /// 프리팹을 복제하고, 각 요소가 무엇을 보여 줄지 결정한다.
    /// </summary>
    public class MapNodeView : MonoBehaviour
    {
        [Tooltip("선택할 수 있는 노드나 플레이어가 서 있는 노드 뒤에 깔리는 빛이다.")]
        public Image halo;
        public Image face;
        public Image icon;
        [Tooltip("이미 지나온 노드에 표시되는 체크다.")]
        public GameObject visited;
        [Tooltip("노드를 선택할 수 있을 때만 활성화된다.")]
        public UiButton button;
        [Tooltip("아직 도달하지 않은 노드를 흐리게 한다.")]
        public CanvasGroup group;

        public RectTransform Rect => (RectTransform)transform;
    }
}
