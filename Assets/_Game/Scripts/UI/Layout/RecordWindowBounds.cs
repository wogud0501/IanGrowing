using UnityEngine;

namespace IanGame
{
    [DisallowMultipleComponent]
    public sealed class RecordWindowBounds : MonoBehaviour
    {
        Vector2 measured;
        int choiceCount = -1;

        public void FitChoices(int count)
        {
            if (count != choiceCount)
            {
                choiceCount = count;
                measured = Vector2.zero;
                Fit();
            }
        }

        void OnEnable()
        {
            Fit();
        }

        void OnRectTransformDimensionsChange()
        {
            Fit();
        }

        void LateUpdate()
        {
            // A fixed-size child may keep its old rectangle when the safe-area parent resizes.
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
            float width = 1120;
            float height = portrait ? 960 : 780;
            if (choiceCount >= 0)
            {
                width = choiceCount <= 3 ? 850 : 970;
                height = Mathf.Clamp(138 + choiceCount * 83, 350, height);
            }

            rect.localScale = new Vector3(scale, scale, 1);
            rect.sizeDelta = new Vector2(
                Mathf.Min(width, measured.x * .95f / scale),
                Mathf.Min(height, measured.y * .91f / scale)
            );
            float lower = choiceCount >= 0 ? -.38f * (measured.y - rect.sizeDelta.y * scale) : 0;
            rect.anchoredPosition = new Vector2(0, lower);
        }
    }
}
