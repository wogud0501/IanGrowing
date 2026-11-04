using TMPro;using UnityEngine;using UnityEngine.EventSystems;
namespace IanGame {
public class PixelTooltip:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler {
 [TextArea]public string message;public TMP_Text output;public bool hideOutputOnExit=true;
 public void OnPointerEnter(PointerEventData e){Show();}public void OnSelect(BaseEventData e){Show();}public void OnPointerExit(PointerEventData e){Hide();}public void OnDeselect(BaseEventData e){Hide();}
 void Show(){if(output!=null){output.text=message;output.gameObject.SetActive(true);}}void Hide(){if(output!=null&&hideOutputOnExit)output.gameObject.SetActive(false);}
}
}
