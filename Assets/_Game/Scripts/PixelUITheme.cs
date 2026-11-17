using TMPro;
using UnityEngine;

namespace IanGame
{
    public enum PixelIcon
    {
        Coin,
        Heart,
        Water,
        Meal,
        Moon,
        Book,
        Social,
        Work,
        Home,
        Life,
        Growth,
        Activity,
        Gear,
        Quest,
        Arrow,
        Warning,
    }

    [CreateAssetMenu(menuName = "Ian/UI/Pixel Theme")]
    public class PixelUITheme : ScriptableObject
    {
        public Color ink = new Color32(28, 38, 58, 255);
        public Color cream = new Color32(255, 241, 207, 255);
        public Color gold = new Color32(240, 188, 99, 255);
        public Color coral = new Color32(220, 119, 91, 255);
        public Color mint = new Color32(127, 182, 150, 255);
        public Color blue = new Color32(106, 153, 173, 255);
        public Color lilac = new Color32(170, 157, 197, 255);
        public bool paperButtons;
        public Sprite panel;
        public Sprite panelDark;
        public Sprite panelBlue;
        public Sprite button;
        public Sprite hover;
        public Sprite pressed;
        public Sprite disabled;
        public Sprite tab;
        public Sprite track;
        public Sprite fill;
        public Sprite[] icons = new Sprite[16];
        public TMP_FontAsset font;
        public TextAsset fontLicense;
        public int pixelsPerUnit = 100;

        public Sprite Icon(PixelIcon id) => icons != null && (int)id < icons.Length ? icons[(int)id] : null;
    }
}
