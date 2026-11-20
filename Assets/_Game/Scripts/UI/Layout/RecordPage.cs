using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    // Install on the actual window surface. The authored ScrollRects, buttons and commands remain its children.
    [DisallowMultipleComponent]
    public sealed class RecordPage : MonoBehaviour
    {
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

            LifeRecordUI.Skin(frame.GetComponent<Image>(), RecordSurface.Paper);
            foreach (var text in frame.GetComponentsInChildren<TMP_Text>(true))
            {
                text.color = LifeRecordUI.Ink;
            }

            foreach (var button in frame.GetComponentsInChildren<Button>(true))
            {
                LifeRecordUI.StyleButton(button);
            }

            Transform heading = title.transform.parent;
            if (heading == frame)
            {
                var r = title.rectTransform;
                var mast = LifeRecordUI.Surface(frame, "Title band", RecordSurface.Charcoal,
                    .009f, Mathf.Max(.78f, r.anchorMin.y - .012f), .991f, .989f);
                mast.transform.SetAsFirstSibling();
            }
            else
            {
                var bg = heading.TryGetComponent<Image>(out var found) ? found : heading.gameObject.AddComponent<Image>();
                LifeRecordUI.Skin(bg, RecordSurface.Charcoal);
                bg.raycastTarget = false;
            }

            MissionRecordLayout.SetText(title, 29, LifeRecordUI.Paper);
            title.margin = new Vector4(8, 0, 4, 0);
        }

        // Older authored scenes wrap their window in a full-screen modal or safe-area object.
        public static void OpenAuthored(GameObject panel)
        {
            string path = panel.name switch
            {
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
            if (frame.name is "Heading" or "Modal heading" or "Education heading")
            {
                frame = (RectTransform)frame.parent;
            }

            bool installed = frame.TryGetComponent<RecordPage>(out _);
            Apply(frame, title);
            if (!installed && panel.name == "Offline progress report")
            {
                var receipt = LifeRecordUI.Surface(frame, "Away report", RecordSurface.Inset, .035f, .277f, .326f, .833f);
                LifeRecordUI.Print(receipt.transform, RecordPrint.City, .07f, .12f, .93f, .88f);
                LifeRecordUI.Place((RectTransform)frame.Find("Scrollable text"), .352f, .26f, .959f, .836f);
            }
        }
    }
}
