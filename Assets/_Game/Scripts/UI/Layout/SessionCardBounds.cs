using UnityEngine;

namespace IanGame
{
    public sealed class SessionCardBounds : MonoBehaviour
    {
        float height;
        Vector2 measured;

        public void SetHeight(float value)
        {
            height = value;
            measured = Vector2.zero;
            OnRectTransformDimensionsChange();
        }

        void LateUpdate()
        {
            OnRectTransformDimensionsChange();
        }

        void OnRectTransformDimensionsChange()
        {
            if (height <= 0 || transform.parent is not RectTransform parent || measured == parent.rect.size)
            {
                return;
            }

            measured = parent.rect.size;
            var rect = (RectTransform)transform;
            var size = new Vector2(Mathf.Min(540, parent.rect.width * .94f), Mathf.Min(height, parent.rect.height * .94f));
            if (rect.sizeDelta != size)
            {
                rect.sizeDelta = size;
            }
        }
    }
}
