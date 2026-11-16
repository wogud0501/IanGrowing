using UnityEngine;
using UnityEngine.EventSystems;
namespace IanGame {
// The Button remains the input owner. Only its label moves by one UI pixel.
public sealed class PixelControlFeedback:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerEnterHandler,ISelectHandler,ISubmitHandler {
 RectTransform label;Vector2 rest;float until;PresentationFeedback bus;
 void Start(){var text=GetComponentInChildren<TMPro.TMP_Text>();if(text==null)return;label=text.rectTransform;rest=label.anchoredPosition;bus=GetComponentInParent<PresentationFeedback>();}
 public void OnPointerDown(PointerEventData e){until=Time.unscaledTime+.1f;bus?.Cue("UIClick");}
 public void OnPointerUp(PointerEventData e){until=0;}
 public void OnPointerEnter(PointerEventData e){bus?.Cue("UIHover");}
 public void OnSelect(BaseEventData e){bus?.Cue("UIHover");}
 public void OnSubmit(BaseEventData e){until=Time.unscaledTime+.1f;bus?.Cue("UIClick");}
 void Update(){if(label!=null)label.anchoredPosition=rest+(Time.unscaledTime<until&&!UIShell.ReducedMotion?Vector2.down:Vector2.zero);}
 void OnDisable(){if(label!=null)label.anchoredPosition=rest;}
}
}
