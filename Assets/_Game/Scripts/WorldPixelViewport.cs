using UnityEngine;
using UnityEngine.UI;
namespace IanGame {
// Render the world at a stable pixel grid and present it at an integer scale.
// Overlay HUD remains on its existing canvas at native screen resolution.
[DefaultExecutionOrder(1000)]public sealed class WorldPixelViewport:MonoBehaviour {
 public LifeWorldController world;public Camera target;
 public int referenceHeight=360;public float indoorPixelsPerUnit=32,outdoorPixelsPerUnit=22;
 RenderTexture texture,previousTarget;Canvas canvas;RawImage image;int width,scale;
 void Start(){
  if(target==null)target=GetComponent<Camera>();previousTarget=target.targetTexture;
  var root=new GameObject("Pixel world presentation",typeof(RectTransform),typeof(Canvas));root.transform.SetParent(transform,false);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=-1000;
  var background=new GameObject("Letterbox",typeof(RectTransform),typeof(Image));background.transform.SetParent(root.transform,false);var bg=background.GetComponent<Image>();bg.raycastTarget=false;bg.color=new Color(.055f,.067f,.085f);var rect=background.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
  var display=new GameObject("Integer world pixels",typeof(RectTransform),typeof(RawImage));display.transform.SetParent(root.transform,false);image=display.GetComponent<RawImage>();image.raycastTarget=false;var r=image.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
 }
 void LateUpdate(){
  if(image==null||world==null||world.CurrentZone==null||Screen.width<1||Screen.height<1)return;
  int nextScale=Mathf.Max(1,Screen.height/referenceHeight),nextWidth=Mathf.Max(1,Screen.width/nextScale);
  if(texture==null||nextWidth!=width||scale!=nextScale){
   if(texture!=null){target.targetTexture=previousTarget;texture.Release();Destroy(texture);}
   scale=nextScale;width=nextWidth;texture=new RenderTexture(width,referenceHeight,24,RenderTextureFormat.ARGB32){name="LifeWorld pixel surface",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,antiAliasing=1,useMipMap=false,autoGenerateMips=false};texture.Create();target.targetTexture=texture;image.texture=texture;image.rectTransform.sizeDelta=new Vector2(width*scale,referenceHeight*scale);
  }
  float ppu=world.CurrentZone.cameraSize>0?referenceHeight/(2*world.CurrentZone.cameraSize):world.CurrentZone.outdoor?outdoorPixelsPerUnit:indoorPixelsPerUnit;target.orthographicSize=referenceHeight/(2*ppu);var p=target.transform.position;target.transform.position=new Vector3(Mathf.Round(p.x*ppu)/ppu,Mathf.Round(p.y*ppu)/ppu,p.z);
 }
 void OnDestroy(){if(target!=null)target.targetTexture=previousTarget;if(texture!=null){texture.Release();Destroy(texture);}if(canvas!=null)Destroy(canvas.gameObject);}
}
}
