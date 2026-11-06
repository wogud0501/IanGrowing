using TMPro;using UnityEngine;using UnityEngine.UI;
namespace IanGame {
// Expand paragraph rows inside existing ScrollRects; never shrink text to fit.
[RequireComponent(typeof(TMP_Text),typeof(LayoutElement))]
public sealed class PixelTextFlow:MonoBehaviour {
 public float minimumHeight=48;TMP_Text text;LayoutElement layout;bool measuring,dirty;
 void OnEnable(){text=GetComponent<TMP_Text>();layout=GetComponent<LayoutElement>();TMPro_EventManager.TEXT_CHANGED_EVENT.Add(Changed);Measure();}
 void OnDisable(){TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(Changed);}
 void Changed(Object target){if(target==text)dirty=true;}
 void OnRectTransformDimensionsChange(){dirty=true;}
 void LateUpdate(){if(dirty){dirty=false;Measure();}}
 void Measure(){if(measuring||text==null||layout==null||text.rectTransform.rect.width<=0)return;measuring=true;float h=Mathf.Max(minimumHeight,text.GetPreferredValues(text.text,text.rectTransform.rect.width,Mathf.Infinity).y+12);if(Mathf.Abs(layout.preferredHeight-h)>1)layout.preferredHeight=h;measuring=false;}
}
}
