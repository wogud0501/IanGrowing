using UnityEngine;using UnityEngine.UI;
namespace IanGame {
[ExecuteAlways]
public class UISafeLayout:MonoBehaviour {
 public RectTransform safe,frame;public float maxWidth=1660;public Rect simulatedSafeArea;public bool simulateSafeArea;
 Rect last;int width,height;public Rect AppliedSafeArea{get;private set;}
 void OnEnable(){Apply(true);}void OnRectTransformDimensionsChange(){Apply(true);}void Update(){Apply(false);}
 public void Apply(bool force=false){if(safe==null||frame==null||Screen.width==0||Screen.height==0)return;Rect area=simulateSafeArea?simulatedSafeArea:Screen.safeArea;if(area.width<=0||area.height<=0)area=new Rect(0,0,Screen.width,Screen.height);if(!force&&area==last&&width==Screen.width&&height==Screen.height)return;last=area;width=Screen.width;height=Screen.height;AppliedSafeArea=area;safe.anchorMin=new Vector2(area.xMin/width,area.yMin/height);safe.anchorMax=new Vector2(area.xMax/width,area.yMax/height);safe.offsetMin=safe.offsetMax=Vector2.zero;
 float w=safe.rect.width;float desired=Mathf.Min(maxWidth,w);frame.anchorMin=new Vector2(.5f,0);frame.anchorMax=new Vector2(.5f,1);frame.pivot=new Vector2(.5f,.5f);frame.sizeDelta=new Vector2(desired,0);frame.anchoredPosition=Vector2.zero;
 }
}
}
