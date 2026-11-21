using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public enum RecordSurface
    {
        Window,
        Header,
        Context,
        Warning,
        Button,
        Inset,
        Focus,
        Reward,
        Inverted,
        Hover,
        Pressed,
        Disabled,
        InvertedFocus,
        GradeOne,
        GradeTwo,
        GradeThree,
        GradeFour,
        Specimen
    }

    public enum RecordPrint
    {
        Mission,
        Inventory,
        Finance,
        Equipment,
        City,
        Settings,
        Life,
        Bag,
        Wallet,
        Controls,
        Achievement,
        Education
    }

    // The frame atlas is a single row of 32-pixel, nine-sliced stationery samples.
    public static class LifeRecordUI
    {
        public static readonly Color Heading = new Color32(244, 244, 244, 255);
        public static readonly Color Body = new Color32(221, 221, 221, 255);
        public static readonly Color Secondary = new Color32(184, 184, 184, 255);
        public static readonly Color ReverseInk = new Color32(16, 16, 16, 255);
        public static readonly Color Track = new Color32(72, 72, 72, 255);
        public static readonly Color Inactive = new Color32(160, 160, 160, 255);
        static readonly Sprite[] frames = new Sprite[(int)RecordSurface.Specimen + 1];
        static Material specimenInk;
        static Sprite equippedMark;

        public static Sprite EquippedMark
        {
            get
            {
                if (!equippedMark)
                {
                    var texture = Resources.Load<Texture2D>("IanLedger/EquippedMark");
                    equippedMark = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        Vector2.one * .5f, 100);
                }

                return equippedMark;
            }
        }

        static readonly Sprite[] prints = new Sprite[12];

        public static RectTransform Rect(Transform parent, string name, float left, float bottom, float right, float top)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, left, bottom, right, top);
            return rect;
        }

        public static void Place(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Sprite Frame(RecordSurface style)
        {
            int index = (int)style;
            if (frames[index] == null)
            {
                var atlas = Resources.Load<Texture2D>("IanLedger/Surfaces");
                frames[index] = Sprite.Create(atlas, new Rect(index * 32, 0, 32, 32),
                    Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * 6);
            }

            return frames[index];
        }

        public static void Skin(Image image, RecordSurface style)
        {
            if (!image)
            {
                return;
            }

            image.sprite = Frame(style);
            image.overrideSprite = null;
            image.color = Color.white;
            image.type = Image.Type.Sliced;
        }

        public static Image Surface(Transform parent, string name, RecordSurface style, float left, float bottom, float right, float top)
        {
            var image = Rect(parent, name, left, bottom, right, top).gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            Skin(image, style);
            return image;
        }

        public static Image Print(Transform parent, RecordPrint id, float left, float bottom, float right, float top)
        {
            var image = Rect(parent, id + " illustration", left, bottom, right, top).gameObject.AddComponent<Image>();
            int i = (int)id;
            if (prints[i] == null)
            {
                var atlas = Resources.Load<Texture2D>(i < 6 ? "IanLedger/Prints" : "IanLedger/SectionIcons");
                if (atlas != null)
                {
                    int tile = i % 6;
                    int w = atlas.width / 3;
                    int h = atlas.height / 2;
                    prints[i] = Sprite.Create(atlas, new Rect(tile % 3 * w, (1 - tile / 3) * h, w, h),
                        Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
                }
            }

            image.sprite = prints[i];
            image.enabled = image.sprite != null;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static TMP_Text Label(Transform parent, TMP_FontAsset font, string value, float size, Color color,
            float left, float bottom, float right, float top)
        {
            var label = Rect(parent, "Text " + value, left, bottom, right, top).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = value;
            label.color = color;
            label.fontSize = size;
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            var scale = label.gameObject.AddComponent<UITypeScale>();
            scale.baseSize = size;
            scale.Apply();
            return label;
        }

        public static void InkSpecimen(Image image)
        {
            if (!specimenInk)
            {
                specimenInk = Resources.Load<Material>("IanLedger/InventorySpecimen");
            }

            image.material = specimenInk;
        }

        public static void StyleButton(Button button, bool selected = false, RecordSurface normal = RecordSurface.Button)
        {
            if (button == null || button.targetGraphic is not Image image)
            {
                return;
            }

            var state = button.GetComponent<RecordButtonInk>();
            if (!state)
            {
                state = button.gameObject.AddComponent<RecordButtonInk>();
            }

            state.Bind(button, image, selected, normal);
        }
    }
}
