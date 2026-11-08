using UnityEngine;
namespace IanGame {
// The world camera must render directly to Display 1. Point-filtered artwork and
// pixel-aligned camera motion are retained without an off-screen UI presentation.
[DefaultExecutionOrder(1000)]public sealed class WorldPixelViewport:MonoBehaviour {
 public LifeWorldController world;public Camera target;
 public int referenceHeight=360;public float indoorPixelsPerUnit=32,outdoorPixelsPerUnit=22;
 void OnEnable(){RestoreDisplay();}
 void RestoreDisplay(){if(target==null)target=GetComponent<Camera>();if(target==null)return;target.targetTexture=null;target.targetDisplay=0;target.rect=new Rect(0,0,1,1);var old=transform.Find("Pixel world presentation");if(old!=null)old.gameObject.SetActive(false);}
 void LateUpdate(){if(target==null||world==null||world.CurrentZone==null)return;float ppu=world.CurrentZone.cameraSize>0?referenceHeight/(2*world.CurrentZone.cameraSize):world.CurrentZone.outdoor?outdoorPixelsPerUnit:indoorPixelsPerUnit;var p=target.transform.position;target.transform.position=new Vector3(Mathf.Round(p.x*ppu)/ppu,Mathf.Round(p.y*ppu)/ppu,p.z);}
}
}