using UnityEngine;
using UnityEngine.UI;
namespace IanGame.Commercial {
// URP's cropped render texture must not expose stale edge pixels in the outer bars.
// These overlay mattes sit below the HUD and outside the integer-scaled world only.
public sealed class PixelViewportMatte:MonoBehaviour {
 public RectTransform left,right,top,bottom;
 public int referenceWidth=640,referenceHeight=360;
 int width=-1,height=-1;
 void OnEnable(){width=height=-1;Canvas.willRenderCanvases+=Refresh;Refresh();}
 void OnDisable(){Canvas.willRenderCanvases-=Refresh;}
 void Refresh(){
  if(width==Screen.width&&height==Screen.height)return;
  width=Screen.width;height=Screen.height;if(width<=0||height<=0)return;
  int scale=Mathf.Max(1,Mathf.FloorToInt(Mathf.Min((float)width/referenceWidth,(float)height/referenceHeight)));
  float x=Mathf.Max(0,(width-referenceWidth*scale)*.5f/width),y=Mathf.Max(0,(height-referenceHeight*scale)*.5f/height);
  Set(left,Vector2.zero,new Vector2(x,1));Set(right,new Vector2(1-x,0),Vector2.one);
  Set(bottom,new Vector2(x,0),new Vector2(1-x,y));Set(top,new Vector2(x,1-y),new Vector2(1-x,1));
 }
 static void Set(RectTransform rect,Vector2 min,Vector2 max){if(rect==null)return;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;rect.gameObject.SetActive(max.x>min.x&&max.y>min.y);}
}
}
