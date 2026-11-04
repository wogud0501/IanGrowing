using UnityEngine;
namespace IanGame {
[ExecuteAlways]
public class PixelNotificationLayout:MonoBehaviour {
 bool applying;void OnEnable(){Apply();}void OnRectTransformDimensionsChange(){Apply();}
 void Apply(){if(applying||transform.parent==null)return;var parent=transform.parent as RectTransform;if(parent==null||parent.rect.width<=0)return;applying=true;var r=(RectTransform)transform;r.anchorMin=r.anchorMax=Vector2.one;r.pivot=Vector2.one;r.sizeDelta=new Vector2(Mathf.Min(340,parent.rect.width-24),96);r.anchoredPosition=new Vector2(-12,-12);applying=false;}
}
}
