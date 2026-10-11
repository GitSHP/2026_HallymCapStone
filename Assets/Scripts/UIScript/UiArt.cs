using Promotion.Core;
using UnityEngine;

namespace Promotion.UI
{
    /// <summary>
    /// The interface's basic shapes and icons as real image assets, so screens
    /// saved as prefabs keep their art. Lives in Resources/UiArt; when it is
    /// missing, the shapes fall back to being drawn in code.
    /// </summary>
    [CreateAssetMenu(menuName = "Promotion/UI Art", fileName = "UiArt")]
    public class UiArt : ScriptableObject
    {
        public Sprite roundedRect;
        public Sprite square;
        public Sprite circle;

        [Tooltip("Indexed by IconId.")]
        public Sprite[] icons = new Sprite[0];

        static UiArt _instance;
        static bool _looked;

        public static UiArt Instance
        {
            get
            {
                if (!_looked)
                {
                    _instance = Resources.Load<UiArt>("UiArt");
                    _looked = true;
                }
                return _instance;
            }
        }

        public static Sprite RoundedRect
        {
            get
            {
                var art = Instance;
                return art != null && art.roundedRect != null ? art.roundedRect : PlaceholderArt.RoundedRect;
            }
        }

        public static Sprite Square
        {
            get
            {
                var art = Instance;
                return art != null && art.square != null ? art.square : PlaceholderArt.Square;
            }
        }

        public static Sprite Circle
        {
            get
            {
                var art = Instance;
                return art != null && art.circle != null ? art.circle : PlaceholderArt.Circle;
            }
        }

        /// <summary>The saved icon, or null when there is none and it must be drawn.</summary>
        public static Sprite Icon(IconId id)
        {
            var art = Instance;
            int index = (int)id;
            if (art == null || art.icons == null || index < 0 || index >= art.icons.Length)
                return null;
            return art.icons[index];
        }
    }
}
