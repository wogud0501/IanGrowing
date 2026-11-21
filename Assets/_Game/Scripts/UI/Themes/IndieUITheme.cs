using TMPro;
using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/UI/Indie Theme")]
    public class IndieUITheme : ScriptableObject
    {
        public Color ink = new Color32(221, 221, 221, 255);
        public Color paper = new Color32(40, 40, 40, 255);
        public Color coral = new Color32(204, 204, 204, 255);
        public Color lime = new Color32(221, 221, 221, 255);
        public Color lavender = new Color32(184, 184, 184, 255);
        public Color mint = new Color32(160, 160, 160, 255);
        public Color white = new Color32(244, 244, 244, 255);
        public TMP_FontAsset font;
        public Sprite rounded;
        public float quick = .12f;
        public float modal = .18f;
        public float number = .24f;
        public float[] spacing = { 4, 8, 12, 16, 24, 32, 48 };
        public float[] typography = { 18, 22, 26, 34, 48, 72 };
    }
}
