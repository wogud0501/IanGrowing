using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace IanGame {
// Activity buttons contain both a title and a changing availability explanation.
// Grow their layout row rather than clipping the explanation or reducing the type.
[RequireComponent(typeof(Button),typeof(LayoutElement))]
public sealed class PixelButtonTextFlow:MonoBehaviour {
 TMP_Text text;LayoutElement layout;bool dirty=true;
 void OnEnable(){text=GetComponentInChildren<TMP_Text>(true);layout=GetComponent<LayoutElement>();TMPro_EventManager.TEXT_CHANGED_EVENT.Add(Changed);dirty=true;}
 void OnDisable(){TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(Changed);}
 void Changed(Object value){if(value==text)dirty=true;}void OnRectTransformDimensionsChange(){dirty=true;}
 void LateUpdate(){if(!dirty||text==null)return;dirty=false;float width=text.rectTransform.rect.width;if(width<=0)return;float height=Mathf.Max(56,text.GetPreferredValues(text.text,Mathf.Max(1,width-28),Mathf.Infinity).y+12);if(Mathf.Abs(layout.preferredHeight-height)>1){layout.minHeight=layout.preferredHeight=height;}}
}
}
