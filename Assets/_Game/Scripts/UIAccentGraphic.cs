using UnityEngine;using UnityEngine.UI;
namespace IanGame {
// Small vector ornaments. No character or background artwork is generated.
public class UIAccentGraphic:MaskableGraphic {
 public enum Shape {Stripes,Sunburst,Cross,Diamond,Grid}public Shape shape;
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;
 if(shape==Shape.Sunburst){for(int i=0;i<32;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;var v0=r.center;var v1=v0+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?.5f:.38f)*Mathf.Min(r.width,r.height);var v2=v0+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(i%2==0?.38f:.5f)*Mathf.Min(r.width,r.height);Tri(vh,v0,v1,v2);}return;}
 if(shape==Shape.Grid){for(float x=r.xMin;x<r.xMax;x+=48)Quad(vh,new Rect(x,r.yMin,1,r.height));for(float y=r.yMin;y<r.yMax;y+=48)Quad(vh,new Rect(r.xMin,y,r.width,1));return;}
 if(shape==Shape.Stripes){for(float x=r.xMin-r.height;x<r.xMax;x+=24){Tri(vh,new Vector2(x,r.yMin),new Vector2(x+10,r.yMin),new Vector2(x+r.height+10,r.yMax));Tri(vh,new Vector2(x,r.yMin),new Vector2(x+r.height+10,r.yMax),new Vector2(x+r.height,r.yMax));}return;}
 if(shape==Shape.Cross){Quad(vh,new Rect(r.center.x-2,r.yMin,4,r.height));Quad(vh,new Rect(r.xMin,r.center.y-2,r.width,4));return;}
 Tri(vh,new Vector2(r.center.x,r.yMax),new Vector2(r.xMax,r.center.y),new Vector2(r.center.x,r.yMin));Tri(vh,new Vector2(r.center.x,r.yMax),new Vector2(r.center.x,r.yMin),new Vector2(r.xMin,r.center.y));
 }
 void Tri(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c){int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
 void Quad(VertexHelper vh,Rect r){Tri(vh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax));Tri(vh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin));}
}
}
