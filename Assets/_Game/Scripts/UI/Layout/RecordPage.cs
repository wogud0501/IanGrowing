using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    // Install on the actual window surface. The authored ScrollRects, buttons and commands remain its children.
    [DisallowMultipleComponent]
    public sealed class RecordPage : MonoBehaviour
    {
        public static void FitReadingColumn(RectTransform content, int inset)
        {
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(inset, inset, 0, 32);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public static void Apply(RectTransform frame, TMP_Text title, bool constrain = true)
        {
            if (!frame || !title || frame.TryGetComponent<RecordPage>(out _))
            {
                return;
            }

            frame.gameObject.AddComponent<RecordPage>();
            if (constrain && !frame.TryGetComponent<RecordWindowBounds>(out _))
            {
                frame.gameObject.AddComponent<RecordWindowBounds>();
            }

            LifeRecordUI.Skin(frame.GetComponent<Image>(), RecordSurface.Window);
            foreach (var text in frame.GetComponentsInChildren<TMP_Text>(true))
            {
                text.color = LifeRecordUI.Body;
            }

            foreach (var button in frame.GetComponentsInChildren<Button>(true))
            {
                LifeRecordUI.StyleButton(button);
            }

            Transform heading = title.transform.parent;
            if (heading == frame)
            {
                var r = title.rectTransform;
                var mast = LifeRecordUI.Surface(frame, "Title band", RecordSurface.Header,
                    .009f, Mathf.Max(.78f, r.anchorMin.y - .012f), .991f, .989f);
                mast.transform.SetAsFirstSibling();
                if (frame.TryGetComponent<LayoutGroup>(out _))
                {
                    mast.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                }
            }
            else
            {
                var bg = heading.TryGetComponent<Image>(out var found) ? found : heading.gameObject.AddComponent<Image>();
                LifeRecordUI.Skin(bg, RecordSurface.Header);
                bg.raycastTarget = false;
            }

            MissionRecordLayout.SetText(title, 29, LifeRecordUI.Heading);
            title.margin = new Vector4(8, 0, 4, 0);
        }

        // Migrate authored vertex tints when a view enters the window stack.
        // Artwork, masks, portraits and the world camera retain their own materials.
        public static void StyleAuthored(GameObject panel)
        {
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                Color c = text.color;
                if ((c.r != c.g || c.g != c.b) && !text.GetComponentInParent<RecordButtonInk>(true))
                {
                    var ink = LifeRecordUI.Body;
                    ink.a = c.a;
                    text.color = ink;
                }
            }

            foreach (var image in panel.GetComponentsInChildren<Image>(true))
            {
                var c = image.color;
                if (c.r == c.g && c.g == c.b)
                {
                    continue;
                }

                float shade = c.grayscale;
                image.color = new Color(shade, shade, shade, c.a);
            }

            foreach (var field in panel.GetComponentsInChildren<TMP_InputField>(true))
            {
                field.selectionColor = new Color32(184, 184, 184, 100);
                field.customCaretColor = true;
                field.caretColor = LifeRecordUI.Heading;
            }

            foreach (var button in panel.GetComponentsInChildren<Button>(true))
            {
                if (!button.GetComponent<RecordButtonInk>() && button.GetComponentInChildren<TMP_Text>(true))
                {
                    LifeRecordUI.StyleButton(button);
                }
            }
        }

        // Older authored scenes wrap their window in a full-screen modal or safe-area object.
        public static void OpenAuthored(GameObject panel)
        {
            string path = panel.name switch
            {
                "Mission List Overlay" => "Window/Mission archive header/Heading",
                "Home management" => "Pixel window/Housing balance",
                "Stock market screen" => "Safe area/Pixel finance window/Market heading",
                "Real estate agency" => "Safe area/Pixel property panel/Heading/Agency title",
                "Offline progress report" or "Automatic activity settings" => "Safe area/Pixel window/Title",
                "World object activities" => "Pixel window/Object title",
                "Education Overlay" => "School Activities/Education heading/Title",
                "Settings" => "Window / Settings/Modal heading/Title",
                "Pause" => "Window / Pause/Modal heading/Title",
                "Detailed status" => "Window / Detailed status/Modal heading/Title",
                "Confirm new life" => "Window / New life confirmation/Modal heading/Title",
                _ => null
            };
            var node = path == null ? null : panel.transform.Find(path);
            if (path != null && !node && panel.TryGetComponent<Image>(out _))
            {
                // A legacy modal has a slash inside the actual window name.
                foreach (var label in panel.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.name == "Title" && label.transform.parent.name == "Modal heading")
                    {
                        node = label.transform;
                        break;
                    }
                }
            }

            if (!node || !node.TryGetComponent<TMP_Text>(out var title))
            {
                return;
            }

            var frame = (RectTransform)node.parent;
            if (frame.name is "Heading" or "Modal heading" or "Education heading" or "Mission archive header")
            {
                frame = (RectTransform)frame.parent;
            }

            if (panel.name == "Mission List Overlay")
            {
                node.parent.SetAsFirstSibling();
            }
            else if (panel.name == "Stock market screen")
            {
                var background = panel.TryGetComponent<Image>(out var veil) ? veil : panel.AddComponent<Image>();
                background.color = LifeRecordUI.ReverseInk;
                background.raycastTarget = true;
            }

            bool installed = frame.TryGetComponent<RecordPage>(out _);
            Apply(frame, title);
            if (!installed && panel.name == "Offline progress report")
            {
                var receipt = LifeRecordUI.Surface(frame, "Away report", RecordSurface.Inset, .035f, .277f, .326f, .833f);
                LifeRecordUI.Print(receipt.transform, RecordPrint.City, .07f, .43f, .93f, .95f);
                LifeRecordUI.Place((RectTransform)frame.Find("Scrollable text"), .352f, .26f, .959f, .836f);
            }
        }
    }
}
