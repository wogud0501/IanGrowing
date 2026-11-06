using UnityEngine;
namespace IanGame {
public sealed class BirthPopupSafeArea:MonoBehaviour {
 public RectTransform target;Rect previous;int width,height;
 void OnEnable(){Apply();}void OnRectTransformDimensionsChange(){Apply();}
 void Apply(){if(target==null||Screen.width<=0||Screen.height<=0)return;var area=Screen.safeArea;if(area==previous&&width==Screen.width&&height==Screen.height)return;previous=area;width=Screen.width;height=Screen.height;target.anchorMin=new Vector2(area.xMin/width,area.yMin/height);target.anchorMax=new Vector2(area.xMax/width,area.yMax/height);target.offsetMin=target.offsetMax=Vector2.zero;}
}
}
