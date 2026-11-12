using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace IanGame {
// Fixed-height header/tabs and visible transactions around the existing scroll.
[DefaultExecutionOrder(50)]
public sealed class PropertyActionDock:MonoBehaviour {
 public RectTransform actions,scroll,heading,close,tabs;public TMP_Text feedback;public GridLayoutGroup grid;
 void LateUpdate(){if(actions==null||scroll==null||grid==null)return;var card=(RectTransform)transform;int count=0;foreach(Transform child in actions)if(child.gameObject.activeSelf)count++;int rows=count==0?0:Mathf.CeilToInt(count/(float)Mathf.Max(1,grid.constraintCount));float h=rows*grid.cellSize.y+Mathf.Max(0,rows-1)*grid.spacing.y;
  float note=48;if(feedback!=null){note=Mathf.Max(48,feedback.GetPreferredValues(feedback.text,Mathf.Max(1,card.rect.width*.96f),Mathf.Infinity).y+12);Bottom((RectTransform)feedback.transform.parent,16,note);feedback.rectTransform.anchorMin=Vector2.zero;feedback.rectTransform.anchorMax=Vector2.one;feedback.rectTransform.offsetMin=new Vector2(8,4);feedback.rectTransform.offsetMax=new Vector2(-8,-4);}
  Bottom(actions,note+28,h);scroll.anchorMin=new Vector2(.02f,0);scroll.anchorMax=new Vector2(.98f,1);scroll.offsetMin=new Vector2(0,note+28+h+12);scroll.offsetMax=new Vector2(0,-156);
  if(heading!=null){heading.anchorMin=new Vector2(.02f,1);heading.anchorMax=new Vector2(.98f,1);heading.pivot=new Vector2(.5f,1);heading.anchoredPosition=new Vector2(-76,-12);heading.sizeDelta=new Vector2(-152,60);}
  if(close!=null){close.anchorMin=close.anchorMax=Vector2.one;close.pivot=Vector2.one;close.anchoredPosition=new Vector2(-16,-12);close.sizeDelta=new Vector2(120,56);}
  if(tabs!=null){tabs.anchorMin=new Vector2(.02f,1);tabs.anchorMax=new Vector2(.98f,1);tabs.pivot=new Vector2(.5f,1);tabs.anchoredPosition=new Vector2(0,-84);tabs.sizeDelta=new Vector2(0,56);}
 }
 static void Bottom(RectTransform r,float y,float height){r.anchorMin=new Vector2(.02f,0);r.anchorMax=new Vector2(.98f,0);r.pivot=new Vector2(.5f,0);r.anchoredPosition=new Vector2(0,y);r.sizeDelta=new Vector2(0,height);}
}
}
