using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    // Reuses the scene's authored pixel frame and Korean font.
    public sealed class PixelUXWidgets
    {
        readonly TMP_FontAsset font;
        readonly Sprite frame;
        public static readonly Color Ink = LifeRecordUI.Body;

        public PixelUXWidgets(WorldHUD hud)
        {
            font = hud.needs.font;
            frame = hud.promptPanel.GetComponent<Image>().sprite;
        }

        public PixelUXWidgets(TMP_FontAsset font, Sprite frame)
        {
            this.font = font;
            this.frame = frame;
        }

        public RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        public RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var r = Rect(name, parent, min, max);
            var image = r.gameObject.AddComponent<Image>();
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            return r;
        }

        public TMP_Text Text(Transform parent, string name, float size, Vector2 min, Vector2 max)
        {
            var t = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.fontSize = size;
            t.color = Ink;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            var scale = t.gameObject.AddComponent<UITypeScale>();
            scale.baseSize = size;
            scale.Apply();
            return t;
        }

        public Button Button(Transform parent, string label, Vector2 min, Vector2 max, Action action)
        {
            var r = Panel(label, parent, min, max);
            var b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = r.GetComponent<Image>();
            var text = Text(r, "Label", 18, Vector2.zero, Vector2.one);
            text.rectTransform.offsetMin = new Vector2(8, 4);
            text.rectTransform.offsetMax = new Vector2(-8, -4);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            LifeRecordUI.StyleButton(b);
            b.onClick.AddListener(() => action());
            b.gameObject.AddComponent<PixelControlFeedback>();
            return b;
        }

        public GameObject Window(UIShell shell, string title)
        {
            var r = Panel(title, shell.overlayRoot, new Vector2(.06f, .08f), new Vector2(.94f, .92f));
            Text(r, "Title", 24, new Vector2(.04f, .9f), new Vector2(.8f, .98f)).text = title;
            Button(r, "뒤로", new Vector2(.82f, .9f), new Vector2(.97f, .98f), () => shell.Close(r.gameObject));
            if (title != "편의 설정")
            {
                RecordPage.Apply(r, r.Find("Title").GetComponent<TMP_Text>(), false);
            }
            r.gameObject.SetActive(false);
            return r.gameObject;
        }

        public TMP_Text ScrollText(Transform parent, Vector2 min, Vector2 max)
        {
            var view = Rect("Readable content", parent, min, max);
            view.gameObject.AddComponent<RectMask2D>();
            var image = view.gameObject.AddComponent<Image>();
            image.color = new Color(1, 1, 1, .01f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            var text = Text(view, "Body", 20, new Vector2(0, 1), Vector2.one);
            text.rectTransform.pivot = new Vector2(.5f, 1);
            text.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = text.rectTransform;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32;
            return text;
        }
    }
}
