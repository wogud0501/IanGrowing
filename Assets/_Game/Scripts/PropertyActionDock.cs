using UnityEngine;
using UnityEngine.UI;
namespace IanGame {
// Keep transaction controls fully visible while only the listing/terms scroll.
[DefaultExecutionOrder(50)]
public sealed class PropertyActionDock:MonoBehaviour {
 public RectTransform actions,scroll;public GridLayoutGroup grid;
 void LateUpdate(){if(actions==null||scroll==null||grid==null)return;float height=((RectTransform)transform).rect.height;int count=0;foreach(Transform child in actions)if(child.gameObject.activeSelf)count++;int rows=count==0?0:Mathf.CeilToInt(count/(float)Mathf.Max(1,grid.constraintCount));float h=rows*grid.cellSize.y+Mathf.Max(0,rows-1)*grid.spacing.y;
  actions.anchorMin=new Vector2(.02f,0);actions.anchorMax=new Vector2(.98f,0);actions.pivot=new Vector2(.5f,0);actions.anchoredPosition=new Vector2(0,height*.135f);actions.sizeDelta=new Vector2(0,h);
  scroll.anchorMin=new Vector2(.02f,0);scroll.anchorMax=new Vector2(.98f,.75f);scroll.offsetMin=new Vector2(0,height*.135f+h+12);scroll.offsetMax=Vector2.zero;
 }
}
}
