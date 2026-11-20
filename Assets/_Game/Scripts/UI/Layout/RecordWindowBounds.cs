using UnityEngine;

namespace IanGame
{
    [DisallowMultipleComponent]
    public sealed class RecordWindowBounds : MonoBehaviour
    {
        Vector2 measured;

        void OnEnable()
        {
            Fit();
        }

        void OnRectTransformDimensionsChange()
        {
            Fit();
        }

        void Fit()
        {
            var rect = (RectTransform)transform;
            if (rect.parent is not RectTransform parent || parent.rect.size == measured)
            {
                return;
            }

            measured = parent.rect.size;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            bool portrait = measured.y > measured.x * 1.2f;
            float scale = portrait ? Mathf.Max(1, measured.x / 680f) : 1;
            rect.localScale = new Vector3(scale, scale, 1);
            rect.sizeDelta = new Vector2(
                Mathf.Min(1120, measured.x * .95f / scale),
                Mathf.Min(portrait ? 960 : 780, measured.y * .91f / scale)
            );
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
