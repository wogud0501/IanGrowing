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
        public Color ink = new Color32(221, 221, 221, 255);
        public Color cream = new Color32(244, 244, 244, 255);
        public Color gold = new Color32(232, 232, 232, 255);
        public Color coral = new Color32(204, 204, 204, 255);
        public Color mint = new Color32(221, 221, 221, 255);
        public Color blue = new Color32(184, 184, 184, 255);
        public Color lilac = new Color32(160, 160, 160, 255);
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
