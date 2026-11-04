using UnityEngine;using UnityEngine.UI;
namespace IanGame {
// One layout controller switches direction. Unity forbids two LayoutGroups on one object.
[ExecuteAlways]
public class UIAdaptiveLayout:LayoutGroup {
 public bool vertical;public int columns=1;public float spacing=12,rowHeight=49;
 public override void CalculateLayoutInputHorizontal(){base.CalculateLayoutInputHorizontal();Calculate(0);}
 public override void CalculateLayoutInputVertical(){Calculate(1);}
 void Calculate(int axis){float min=0,pref=0,flex=0;int n=rectChildren.Count;if(columns>1){if(axis==0){min=0;pref=0;flex=1;}else{min=pref=Mathf.Ceil(n/(float)columns)*rowHeight+Mathf.Max(0,Mathf.CeilToInt(n/(float)columns)-1)*spacing;}}
 else{bool along=vertical?axis==1:axis==0;foreach(var r in rectChildren){float a=LayoutUtility.GetMinSize(r,axis),b=Preferred(r,axis),c=LayoutUtility.GetFlexibleSize(r,axis);if(along){min+=a;pref+=b;flex+=c;}else{min=Mathf.Max(min,a);pref=Mathf.Max(pref,b);flex=Mathf.Max(flex,c);}}if(along){min+=Mathf.Max(0,n-1)*spacing;pref+=Mathf.Max(0,n-1)*spacing;}}
 float pad=axis==0?padding.horizontal:padding.vertical;SetLayoutInputForAxis(min+pad,-1,pref+pad,flex,axis);}
 public override void SetLayoutHorizontal(){Place(0);}public override void SetLayoutVertical(){Place(1);}
 void Place(int axis){float available=rectTransform.rect.size[axis]-(axis==0?padding.horizontal:padding.vertical);float start=axis==0?padding.left:padding.top;int n=rectChildren.Count;if(columns>1){float width=(rectTransform.rect.width-padding.horizontal-(columns-1)*spacing)/columns;for(int i=0;i<n;i++)SetChildAlongAxis(rectChildren[i],axis,axis==0?padding.left+i%columns*(width+spacing):padding.top+i/columns*(rowHeight+spacing),axis==0?Mathf.Max(1,width):rowHeight);return;}
 bool along=vertical?axis==1:axis==0;if(!along){foreach(var r in rectChildren)SetChildAlongAxis(r,axis,start,Mathf.Max(1,available));return;}
 float total=0,flex=0;foreach(var r in rectChildren){total+=Preferred(r,axis);flex+=LayoutUtility.GetFlexibleSize(r,axis);}float room=Mathf.Max(0,available-Mathf.Max(0,n-1)*spacing);float extra=Mathf.Max(0,room-total);float shrink=total>room&&total>0?room/total:1;foreach(var r in rectChildren){float size=Preferred(r,axis)*shrink+(flex>0?extra*LayoutUtility.GetFlexibleSize(r,axis)/flex:0);SetChildAlongAxis(r,axis,start,Mathf.Max(1,size));start+=size+spacing;}}
 float Preferred(RectTransform r,int axis){var l=r.GetComponent<LayoutElement>();if(axis==0&&l!=null&&l.flexibleWidth>0&&l.preferredWidth<0)return Mathf.Max(0,l.minWidth);return LayoutUtility.GetPreferredSize(r,axis);} public void Reflow(){SetDirty();}
}
}
