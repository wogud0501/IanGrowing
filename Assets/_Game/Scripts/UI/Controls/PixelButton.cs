using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IanGame
{
    [RequireComponent(typeof(Button))]
    public class PixelButton
        : MonoBehaviour,
            ISelectHandler,
            IDeselectHandler,
            IPointerEnterHandler,
            IPointerExitHandler
    {
        public GameObject cursor;

        public void OnSelect(BaseEventData e)
        {
            if (cursor != null)
            {
                cursor.SetActive(true);
            }
        }

        public void OnDeselect(BaseEventData e)
        {
            if (cursor != null)
            {
                cursor.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (cursor != null && GetComponent<Button>().IsInteractable())
            {
                cursor.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (cursor != null)
            {
                cursor.SetActive(
                    EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject
                );
            }
        }

        void OnDisable()
        {
            if (cursor != null)
            {
                cursor.SetActive(false);
            }
        }
    }
}
