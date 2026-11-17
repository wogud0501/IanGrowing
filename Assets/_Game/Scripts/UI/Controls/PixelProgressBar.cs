using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public class PixelProgressBar : MonoBehaviour
    {
        public Image fill;
        public Color normal = new Color32(127, 182, 150, 255);
        public Color warning = new Color32(220, 119, 91, 255);
        float target = -1;
        Coroutine tween;

        public void Set(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            if (Mathf.Approximately(target, ratio))
            {
                return;
            }

            float start = fill.type == Image.Type.Filled ? fill.fillAmount : fill.rectTransform.anchorMax.x;
            target = ratio;
            fill.color = ratio < .25f ? warning : normal;
            if (tween != null)
            {
                StopCoroutine(tween);
            }

            if (!isActiveAndEnabled || UIShell.ReducedMotion)
            {
                Apply(ratio);
                return;
            }

            tween = StartCoroutine(Animate(start, ratio));
        }

        IEnumerator Animate(float start, float end)
        {
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / .14f)
            {
                Apply(Mathf.Lerp(start, end, 1 - (1 - t) * (1 - t)));
                yield return null;
            }

            Apply(end);
            tween = null;
        }

        void Apply(float ratio)
        {
            if (fill.type == Image.Type.Filled)
            {
                fill.fillAmount = ratio;
            }
            else
            {
                fill.rectTransform.anchorMax = new Vector2(ratio, 1);
            }
        }

        void OnDisable()
        {
            if (tween != null)
            {
                StopCoroutine(tween);
            }

            tween = null;
            if (fill != null && target >= 0)
            {
                Apply(target);
            }
        }
    }
}
