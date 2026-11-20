using UnityEngine;
using UnityEngine.EventSystems;

namespace IanGame
{
    // The Button remains the input owner. Only its label moves by one UI pixel.
    public sealed class PixelControlFeedback
        : MonoBehaviour,
            IPointerDownHandler,
            IPointerUpHandler,
            IPointerEnterHandler,
            ISelectHandler,
            ISubmitHandler
    {
        RectTransform label;
        Vector2 rest;
        Coroutine release;
        bool pressed;
        PresentationFeedback bus;

        void Start()
        {
            var text = GetComponentInChildren<TMPro.TMP_Text>();
            if (text == null)
            {
                return;
            }

            label = text.rectTransform;
            bus = GetComponentInParent<PresentationFeedback>();
        }

        public void OnPointerDown(PointerEventData e)
        {
            Press();
            bus?.Cue("UIClick");
        }

        public void OnPointerUp(PointerEventData e)
        {
            Release();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            bus?.Cue("UIHover");
        }

        public void OnSelect(BaseEventData e)
        {
            bus?.Cue("UIHover");
        }

        public void OnSubmit(BaseEventData e)
        {
            Press();
            bus?.Cue("UIClick");
        }

        void Press()
        {
            if (!label || UIShell.ReducedMotion)
            {
                return;
            }

            Release();
            rest = label.anchoredPosition;
            label.anchoredPosition = rest + Vector2.down;
            pressed = true;
            release = StartCoroutine(ReleaseAfterPress());
        }

        System.Collections.IEnumerator ReleaseAfterPress()
        {
            yield return new WaitForSecondsRealtime(.1f);
            release = null;
            Release();
        }

        void Release()
        {
            if (release != null)
            {
                StopCoroutine(release);
                release = null;
            }

            if (pressed && label)
            {
                label.anchoredPosition = rest;
            }

            pressed = false;
        }

        void OnDisable()
        {
            Release();
        }
    }
}
