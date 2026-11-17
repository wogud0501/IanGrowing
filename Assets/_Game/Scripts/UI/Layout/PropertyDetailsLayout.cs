using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    // Master/detail presentation within the existing scroll view. Phone layouts
    // stack the image and terms; expanded displays read them side by side.
    [RequireComponent(typeof(LayoutElement))]
    public sealed class PropertyDetailsLayout : MonoBehaviour
    {
        public TMP_Text body;
        public Image preview;
        string measured;

        void OnEnable()
        {
            measured = null;
        }

        void LateUpdate()
        {
            if (body == null || preview == null)
            {
                return;
            }

            var rect = (RectTransform)transform;
            float width = rect.rect.width;
            if (width <= 0)
            {
                return;
            }

            bool image = preview.gameObject.activeSelf;
            string key = width + "/" + body.text + "/" + body.fontSize + "/" + image;
            if (key == measured)
            {
                return;
            }

            measured = key;
            bool wide = width >= 760 && image;
            float textWidth = wide ? width * .61f - 16 : width;
            float h = Mathf.Max(
                96,
                body.GetPreferredValues(body.text, Mathf.Max(1, textWidth - 12), Mathf.Infinity).y + 24
            );
            float picture = image ? 196 : 0;
            Place(body.rectTransform, 0, wide ? 0 : picture, textWidth, h);
            if (image)
            {
                Place(preview.rectTransform, wide ? width * .63f : 0, 0, wide ? width * .37f : width, 180);
            }

            var layout = GetComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = wide ? Mathf.Max(h, 196) : h + picture;
            LayoutRebuilder.MarkLayoutForRebuild(rect);
        }

        static void Place(RectTransform r, float x, float y, float width, float height)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(width, height);
        }
    }
}
