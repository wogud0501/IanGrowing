using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/Presentation/Collection visual theme")]
    public sealed class CollectionVisualTheme : ScriptableObject
    {
        public Sprite panelSprite;
        public Sprite buttonSprite;
        public Sprite paperSprite;
        public Color panel = new Color32(32, 32, 32, 255);
        public Color cell = new Color32(40, 40, 40, 255);
        public Color button = new Color32(50, 50, 50, 255);
        public Color ink = new Color32(221, 221, 221, 255);
        public Color buttonInk = new Color32(221, 221, 221, 255);
    }
}
