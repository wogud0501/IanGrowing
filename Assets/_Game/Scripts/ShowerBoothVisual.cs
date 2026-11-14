using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace IanGame {
// Appliance presentation only. Building WorldDoor and scene travel are untouched.
public sealed class ShowerBoothVisual:MonoBehaviour {
 public SpriteRenderer source;public Sprite ceramic;public BoxCollider2D barrier;
 public float Aperture{get;private set;}public bool WaterRunning{get;set;}
 public Bounds VisualBounds{get;private set;}
 public int RenderedWaterFrames{get;private set;}
 Mesh back,hardware,plumbing,glass,water;MeshRenderer backRenderer,frameRenderer,plumbingRenderer,glassRenderer,waterRenderer;
 readonly List<Vector3> vertices=new List<Vector3>();readonly List<Vector2> uv=new List<Vector2>();readonly List<Color> colors=new List<Color>();readonly List<int> triangles=new List<int>();
 bool initialized;float rendered=-1;int waterTick=-1;
 void Awake(){Build();}
 void Build(){if(initialized||source==null||ceramic==null)return;initialized=true;VisualBounds=source.bounds;
  Make("Booth rear tiles",ceramic.texture,out back,out backRenderer);Make("Booth fixed frame",source.sprite.texture,out hardware,out frameRenderer);Make("Booth rear plumbing",source.sprite.texture,out plumbing,out plumbingRenderer);Make("Visible sliding glass",Texture2D.whiteTexture,out glass,out glassRenderer);Make("Running shower water",Texture2D.whiteTexture,out water,out waterRenderer);
  DrawBack();DrawFront();source.enabled=false;
 }
 void Make(string title,Texture texture,out Mesh mesh,out MeshRenderer renderer){var go=new GameObject(title,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);mesh=new Mesh{name=title};mesh.MarkDynamic();go.GetComponent<MeshFilter>().sharedMesh=mesh;renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=source.sharedMaterial;var properties=new MaterialPropertyBlock();properties.SetTexture("_MainTex",texture);renderer.SetPropertyBlock(properties);}
 Vector3 P(float x,float y)=>transform.InverseTransformPoint(new Vector3(Mathf.Lerp(VisualBounds.min.x,VisualBounds.max.x,x),Mathf.Lerp(VisualBounds.min.y,VisualBounds.max.y,y),0));
 Rect UV(Sprite sprite,Rect r)=>new Rect((sprite.rect.x+r.x*sprite.rect.width)/sprite.texture.width,(sprite.rect.y+r.y*sprite.rect.height)/sprite.texture.height,r.width*sprite.rect.width/sprite.texture.width,r.height*sprite.rect.height/sprite.texture.height);
 void Clear(){vertices.Clear();uv.Clear();colors.Clear();triangles.Clear();}
 void Quad(Rect position,Rect texture,Color tint){int n=vertices.Count;vertices.AddRange(new[]{P(position.xMin,position.yMin),P(position.xMax,position.yMin),P(position.xMax,position.yMax),P(position.xMin,position.yMax)});uv.AddRange(new[]{new Vector2(texture.xMin,texture.yMin),new Vector2(texture.xMax,texture.yMin),new Vector2(texture.xMax,texture.yMax),new Vector2(texture.xMin,texture.yMax)});for(int i=0;i<4;i++)colors.Add(tint);triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
 void Save(Mesh m){m.Clear();m.SetVertices(vertices);m.SetUVs(0,uv);m.SetColors(colors);m.SetTriangles(triangles,0);m.RecalculateBounds();}
 void DrawBack(){Clear();Quad(new Rect(.08f,.08f,.84f,.84f),UV(ceramic,new Rect(0,0,1,1)),Color.white);Save(back);
  Clear();foreach(var r in new[]{new Rect(0,0,.1f,1),new Rect(.9f,0,.1f,1),new Rect(.1f,.9f,.8f,.1f),new Rect(.1f,0,.8f,.1f)})Quad(r,UV(source.sprite,r),Color.white);Save(hardware);Clear();foreach(var r in new[]{new Rect(.58f,.13f,.25f,.74f),new Rect(.16f,.57f,.27f,.22f)})Quad(r,UV(source.sprite,r),Color.white);Save(plumbing);
 }
 void DrawFront(){Clear();float slide=Aperture*.38f;var face=new Rect(0,0,1,1);Quad(new Rect(.1f,.09f,.8f,.8f),face,new Color(.85f,.94f,.95f,.025f));Quad(new Rect(.1f+slide,.1f,.4f,.79f),face,new Color(.85f,.94f,.95f,.055f));Quad(new Rect(.48f+slide,.12f,.015f,.74f),face,new Color(.42f,.53f,.51f,.85f));Quad(new Rect(.43f+slide,.44f,.045f,.08f),face,new Color(.36f,.43f,.42f,1));Save(glass);rendered=Aperture;}
 public IEnumerator Door(float target,System.Func<bool> cancelled){Build();float start=Aperture;for(float t=0;t<1;t+=Time.unscaledDeltaTime/.28f){if(cancelled())yield break;Aperture=Mathf.Lerp(start,target,Mathf.SmoothStep(0,1,t));yield return null;}Aperture=target;}
 void LateUpdate(){Build();if(!initialized)return;source.enabled=false;int order=1000-Mathf.RoundToInt(transform.position.y*100);backRenderer.sortingOrder=order-200;plumbingRenderer.sortingOrder=order-150;frameRenderer.sortingOrder=order+6;glassRenderer.sortingOrder=order+5;waterRenderer.sortingOrder=order+3;if(barrier!=null)barrier.enabled=Aperture<.95f;if(rendered!=Aperture)DrawFront();waterRenderer.enabled=WaterRunning;
  if(WaterRunning){int tick=Mathf.FloorToInt(Time.unscaledTime*10);if(tick!=waterTick){waterTick=tick;RenderedWaterFrames++;Clear();var u=new Rect(0,0,1,1);for(int i=0;i<9;i++){float x=.28f+(i%3)*.13f,y=.15f+Mathf.Repeat(i*.17f-tick*.07f,.65f);Quad(new Rect(x,y,.008f,.065f),u,new Color(.55f,.76f,.86f,.65f));}Save(water);}}
 }
 public void ResetClosed(){Aperture=0;WaterRunning=false;if(barrier!=null)barrier.enabled=true;}
 void OnDisable(){ResetClosed();}
 void OnDestroy(){foreach(var m in new[]{back,hardware,plumbing,glass,water})if(m!=null)Destroy(m);}
}
}
