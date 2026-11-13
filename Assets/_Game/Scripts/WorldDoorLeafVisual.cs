using System.Collections.Generic;
using UnityEngine;
namespace IanGame {
// Presentation only: WorldDoor supplies aperture, and retains all state/physics.
// A textured, six-faced solid is rotated about its vertical hinge in model space,
// then projected into the same 3/4 plane as the sprite world. No transform tween.
public sealed class WorldDoorLeafVisual:MonoBehaviour {
 Mesh mesh;MeshRenderer surface;MaterialPropertyBlock properties;float depthProjection,lastAperture=-1;Color lastTint;Sprite lastSprite;Mesh frameMesh;MeshRenderer frameSurface;MaterialPropertyBlock frameProperties;
 readonly List<Vector3> vertices=new List<Vector3>();readonly List<Vector2> uv=new List<Vector2>();readonly List<Color> colors=new List<Color>();readonly List<int> triangles=new List<int>();
 public MeshRenderer Surface=>surface;public MeshRenderer FrameSurface=>frameSurface;public Mesh Geometry=>mesh;public Mesh FrameGeometry=>frameMesh;
 public void Draw(WorldDoor door){
  if(door.leaf==null||door.style==null||door.style.openingFrames.Length==0)return;
  if(mesh==null){mesh=new Mesh{name="Solid door leaf (hinged volume)"};mesh.MarkDynamic();gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;surface=gameObject.AddComponent<MeshRenderer>();surface.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;surface.receiveShadows=false;properties=new MaterialPropertyBlock();}
  transform.SetPositionAndRotation(door.leaf.transform.position+door.VisualOffset,Quaternion.identity);var scale=transform.parent.lossyScale;transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
  surface.sharedMaterial=door.leaf.sharedMaterial;surface.sortingLayerID=door.leaf.sortingLayerID;surface.sortingOrder=door.leaf.sortingOrder;surface.enabled=door.isActiveAndEnabled;
  if(frameSurface!=null){frameSurface.enabled=door.isActiveAndEnabled;frameSurface.sortingOrder=door.frame.sortingOrder;frameProperties.SetColor("_Color",door.Highlighted?Color.Lerp(door.frame.color,new Color(1,.84f,.48f,1),.25f):door.frame.color);frameSurface.SetPropertyBlock(frameProperties);door.frame.enabled=false;}
  var sprite=door.style.openingFrames[0];properties.SetTexture("_MainTex",sprite.texture);surface.SetPropertyBlock(properties);
  var currentTint=door.leaf.color;currentTint.a=1;door.leaf.enabled=false;
  if(lastAperture==door.Aperture&&lastTint==currentTint&&lastSprite==sprite)return;lastAperture=door.Aperture;lastTint=currentTint;lastSprite=sprite;depthProjection=door.normal.y<0?.62f:-.62f;
  vertices.Clear();uv.Clear();colors.Clear();triangles.Clear();
  bool glass=door.style.mechanism==DoorMechanism.SlidingGlass,jade=door.style.mechanism==DoorMechanism.HingedJade;
  float left=glass?.13f:jade?.318f:.304f,right=glass?.87f:jade?.770f:.784f,bottom=glass?.055f:.06f,top=.85f;
  float unit=sprite.rect.height/sprite.pixelsPerUnit,width=(right-left)*sprite.rect.width/sprite.pixelsPerUnit,height=(top-bottom)*unit;
  var r=sprite.rect;var tex=sprite.texture;Rect face=new Rect((r.x+left*r.width)/tex.width,(r.y+bottom*r.height)/tex.height,(right-left)*r.width/tex.width,(top-bottom)*r.height/tex.height);
  if(frameMesh==null&&door.frame!=null){
   if(door.normal.y>0&&door.apertureShadow!=null&&door.apertureShadow.sprite!=null){
    // Fit the backing to the aperture, accounting for its own Sprite pivot.
    // The former root+up placement let it project beyond the frame on entry.
    var shadow=door.apertureShadow.transform;var bounds=door.apertureShadow.sprite.bounds;var parentScale=shadow.parent.lossyScale;
    shadow.localScale=new Vector3(width/bounds.size.x/parentScale.x,height/bounds.size.y/parentScale.y,1);
    Vector3 centre=door.frame.transform.position+door.VisualOffset+new Vector3(((left+right)/2-.5f)*r.width/sprite.pixelsPerUnit,((bottom+top)/2-.04f)*unit,0);
    shadow.position=centre-shadow.TransformVector(bounds.center);
   }
   // Sprite.OverrideGeometry was lost on the saved frame Sprite. Use explicit
   // mesh UVs for four borders; a closed door can never reappear in the centre.
   foreach(var q in new[]{new Rect(0,0,left,1),new Rect(right,0,1-right,1),new Rect(left,top,right-left,1-top),new Rect(left,0,right-left,bottom)}){
    Vector3 P(float x,float y)=>new Vector3((x-.5f)*r.width/sprite.pixelsPerUnit,(y-.04f)*unit,0);
    Vector2 T(float x,float y)=>new Vector2((r.x+x*r.width)/tex.width,(r.y+y*r.height)/tex.height);
    Quad(P(q.xMin,q.yMin),P(q.xMax,q.yMin),P(q.xMax,q.yMax),P(q.xMin,q.yMax),T(q.xMin,q.yMin),T(q.xMax,q.yMin),T(q.xMax,q.yMax),T(q.xMin,q.yMax),Color.white);
   }
   frameMesh=new Mesh{name="Four fixed door frame borders"};frameMesh.SetVertices(vertices);frameMesh.SetUVs(0,uv);frameMesh.SetColors(colors);frameMesh.SetTriangles(triangles,0);frameMesh.RecalculateBounds();
   var go=new GameObject("Fixed frame borders",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.transform.SetPositionAndRotation(door.frame.transform.position+door.VisualOffset,Quaternion.identity);go.GetComponent<MeshFilter>().sharedMesh=frameMesh;frameSurface=go.GetComponent<MeshRenderer>();frameSurface.sharedMaterial=door.frame.sharedMaterial;frameSurface.sortingOrder=door.frame.sortingOrder;frameSurface.sortingLayerID=door.frame.sortingLayerID;frameProperties=new MaterialPropertyBlock();frameProperties.SetTexture("_MainTex",sprite.texture);frameProperties.SetColor("_Color",door.Highlighted?Color.Lerp(door.frame.color,new Color(1,.84f,.48f,1),.25f):door.frame.color);frameSurface.SetPropertyBlock(frameProperties);frameSurface.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;frameSurface.receiveShadows=false;door.frame.enabled=false;
   vertices.Clear();uv.Clear();colors.Clear();triangles.Clear();
  }
  var tint=door.leaf.color;tint.a=1;
  if(glass){float half=width/2,slide=door.Aperture*half*.92f;Box(new Vector2(-half-slide,0),half,height,0,1,new Rect(face.x,face.y,face.width/2,face.height),tint);Box(new Vector2(slide,0),half,height,0,1,new Rect(face.center.x,face.y,face.width/2,face.height),tint);}
  else Box(Vector2.zero,width,height,door.Aperture*78*Mathf.Deg2Rad,jade?-1:1,face,tint);
  mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();door.leaf.enabled=false;
 }
 void Box(Vector2 origin,float width,float height,float angle,float hand,Rect texture,Color tint){
  const float thickness=.095f;float c=Mathf.Cos(angle),s=Mathf.Sin(angle);
  // Rotate a real box around (0,y,0); project depth onto floor-screen Y.
  Vector3 P(float x,float y,float z)=>new Vector3(origin.x+hand*(x*c-z*s),origin.y+y+(x*s+z*c)*depthProjection,0);
  Vector2 U(float x,float y)=>new Vector2(Mathf.Lerp(texture.xMin,texture.xMax,hand<0?1-x:x),Mathf.Lerp(texture.yMin,texture.yMax,y));
  var a=P(0,0,0);var b=P(width,0,0);var c0=P(width,height,0);var d=P(0,height,0);var e=P(0,0,thickness);var f=P(width,0,thickness);var g=P(width,height,thickness);var h=P(0,height,thickness);
  // Shadow follows the solid leaf footprint, independently of wall cutaway.
  var shade=new Color(.06f,.045f,.03f,.3f);var solid=U(.45f,.45f);Quad(a+Vector3.down*.025f,b+Vector3.down*.025f,f+Vector3.down*.075f,e+Vector3.down*.075f,solid,solid,solid,solid,shade);
  Quad(e,f,g,h,U(0,0),U(1,0),U(1,1),U(0,1),new Color(tint.r*.72f,tint.g*.72f,tint.b*.72f,1));
  Quad(a,e,h,d,solid,solid,solid,solid,new Color(tint.r*.65f,tint.g*.6f,tint.b*.55f,1));
  Quad(b,f,g,c0,solid,solid,solid,solid,new Color(tint.r*.52f,tint.g*.48f,tint.b*.42f,1));
  Quad(d,c0,g,h,solid,solid,solid,solid,new Color(tint.r*.9f,tint.g*.86f,tint.b*.8f,1));
  Quad(a,b,f,e,solid,solid,solid,solid,new Color(tint.r*.45f,tint.g*.4f,tint.b*.35f,1));
  Quad(a,b,c0,d,U(0,0),U(1,0),U(1,1),U(0,1),tint);
 }
 void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 ua,Vector2 ub,Vector2 uc,Vector2 ud,Color color){int i=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{ua,ub,uc,ud});for(int n=0;n<4;n++)colors.Add(color);triangles.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});}
 void OnDisable(){if(surface!=null)surface.enabled=false;if(frameSurface!=null)frameSurface.enabled=false;}
 void OnDestroy(){if(mesh!=null)Destroy(mesh);if(frameMesh!=null)Destroy(frameMesh);}
}
}
