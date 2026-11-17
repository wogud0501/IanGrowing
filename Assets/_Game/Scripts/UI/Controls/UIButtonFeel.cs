using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IanGame
{
    [RequireComponent(typeof(Button))]
    public class UIButtonFeel
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerDownHandler,
            IPointerUpHandler,
            ISelectHandler,
            IDeselectHandler
    {
        public GameObject focusMark;
        Button button;
        Coroutine motion;
        Vector3 target = Vector3.one;

        void Awake()
        {
            button = GetComponent<Button>();
        }

        void OnDisable()
        {
            if (motion != null)
            {
                StopCoroutine(motion);
            }

            transform.localScale = Vector3.one;
            if (focusMark != null)
            {
                focusMark.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData e)
        {
            Scale(1.025f);
        }

        public void OnPointerExit(PointerEventData e)
        {
            Scale(1);
        }

        public void OnPointerDown(PointerEventData e)
        {
            Scale(.97f);
        }

        public void OnPointerUp(PointerEventData e)
        {
            Scale(1.025f);
        }

        public void OnSelect(BaseEventData e)
        {
            if (focusMark != null)
            {
                focusMark.SetActive(true);
            }

            Scale(1.02f);
        }

        public void OnDeselect(BaseEventData e)
        {
            if (focusMark != null)
            {
                focusMark.SetActive(false);
            }

            Scale(1);
        }

        void Scale(float value)
        {
            if (!isActiveAndEnabled || button != null && !button.IsInteractable())
            {
                return;
            }

            if (motion != null)
            {
                StopCoroutine(motion);
            }

            target = Vector3.one * value;
            if (UIShell.ReducedMotion)
            {
                transform.localScale = Vector3.one;
                return;
            }

            motion = StartCoroutine(Tween());
        }

        IEnumerator Tween()
        {
            Vector3 start = transform.localScale;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / .1f)
            {
                transform.localScale = Vector3.Lerp(start, target, 1 - (1 - t) * (1 - t));
                yield return null;
            }

            transform.localScale = target;
            motion = null;
        }
    }
}
