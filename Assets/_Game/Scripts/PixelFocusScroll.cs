using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;
namespace IanGame {
public class PixelFocusScroll:MonoBehaviour,ISelectHandler {
 public void OnSelect(BaseEventData data){var scroll=GetComponentInParent<ScrollRect>();if(scroll==null||scroll.content==null||scroll.viewport==null)return;Canvas.ForceUpdateCanvases();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,transform);var view=scroll.viewport.rect;float delta=bounds.min.y<view.yMin?view.yMin-bounds.min.y:bounds.max.y>view.yMax?view.yMax-bounds.max.y:0;var p=scroll.content.anchoredPosition;p.y=Mathf.Clamp(p.y+delta,0,Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height));scroll.StopMovement();scroll.content.anchoredPosition=p;}
}
}
