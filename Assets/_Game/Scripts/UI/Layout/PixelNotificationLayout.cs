using UnityEngine;

namespace IanGame
{
    [ExecuteAlways]
    public class PixelNotificationLayout : MonoBehaviour
    {
        bool applying;
        bool dirty;

        void OnEnable()
        {
            TMPro.TMPro_EventManager.TEXT_CHANGED_EVENT.Add(TextChanged);
            Apply();
        }

        void OnDisable()
        {
            TMPro.TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(TextChanged);
        }

        void TextChanged(Object target)
        {
            var t = target as TMPro.TMP_Text;
            if (t != null && t.transform.IsChildOf(transform))
            {
                dirty = true;
            }
        }

        void OnRectTransformDimensionsChange()
        {
            dirty = true;
        }

        void LateUpdate()
        {
            if (dirty)
            {
                dirty = false;
                Apply();
            }
        }

        void Apply()
        {
            if (applying || transform.parent == null)
            {
                return;
            }

            var parent = transform.parent as RectTransform;
            if (parent == null || parent.rect.width <= 0)
            {
                return;
            }

            applying = true;
            float width = Mathf.Min(340, parent.rect.width - 24);
            var text = GetComponentInChildren<TMPro.TMP_Text>(true);
            float height =
                text == null
                    ? 96
                    : Mathf.Max(96, text.GetPreferredValues(text.text, width - 24, Mathf.Infinity).y + 24);
            var r = (RectTransform)transform;
            r.anchorMin = r.anchorMax = Vector2.one;
            r.pivot = Vector2.one;
            r.sizeDelta = new Vector2(width, Mathf.Min(parent.rect.height * .6f, height));
            r.anchoredPosition = new Vector2(-12, -12);
            applying = false;
        }
    }
}
