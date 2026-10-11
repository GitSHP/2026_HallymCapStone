using Promotion.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.Map
{
    /// <summary>
    /// One map node's parts, as laid out in the MapNode prefab. The map screen
    /// copies the prefab for every node and decides what each part shows.
    /// </summary>
    public class MapNodeView : MonoBehaviour
    {
        [Tooltip("Glow behind a node that can be chosen, or the one the player stands on.")]
        public Image halo;
        public Image face;
        public Image icon;
        [Tooltip("Tick on nodes already passed through.")]
        public GameObject visited;
        [Tooltip("Enabled only while the node can be chosen.")]
        public UiButton button;
        [Tooltip("Dims nodes not yet reached.")]
        public CanvasGroup group;

        public RectTransform Rect => (RectTransform)transform;
    }
}
