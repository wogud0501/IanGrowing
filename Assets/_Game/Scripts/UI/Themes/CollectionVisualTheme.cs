using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/Presentation/Collection visual theme")]
    public sealed class CollectionVisualTheme : ScriptableObject
    {
        public Sprite panelSprite;
        public Sprite buttonSprite;
        public Sprite paperSprite;
        public Color panel = new Color(.2f, .2f, .23f);
        public Color cell = new Color(.29f, .28f, .3f);
        public Color button = new Color(.91f, .83f, .67f);
        public Color ink = new Color(.95f, .92f, .86f);
        public Color buttonInk = new Color(.22f, .2f, .18f);
    }
}
