using UnityEngine;
namespace IanGame {
public sealed class WorldCameraController:MonoBehaviour {
 public LifeWorldController world;public Camera target;public float transitionZoom;Vector3 velocity;bool snap=true;
 public void Snap(){snap=true;velocity=Vector3.zero;}
 void LateUpdate(){if(world==null||world.Player==null||world.CurrentZone==null)return;var z=world.CurrentZone;if(world.Room?.interior?.enabled==true){FrameInterior();return;}float desiredSize=z.cameraSize>0?z.cameraSize:z.outdoor?world.Service.rules.outdoorCameraSize:world.Service.rules.indoorCameraSize;desiredSize+=transitionZoom;target.orthographicSize=snap?desiredSize:Mathf.Lerp(target.orthographicSize,desiredSize,Time.unscaledDeltaTime*6);float h=target.orthographicSize,w=h*target.aspect;var p=world.Player.Position;var center=z.origin+(world.Room==null?Vector2.zero:world.Room.cameraOffset);float hx=z.size.x/2,hy=z.size.y/2;float x=hx>w?Mathf.Clamp(p.x,center.x-hx+w,center.x+hx-w):center.x;float y=hy>h?Mathf.Clamp(p.y+.8f,center.y-hy+h,center.y+hy-h):center.y;var desired=new Vector3(x,y,-10);var pos=snap?desired:Vector3.SmoothDamp(transform.position,desired,ref velocity,world.Service.rules.cameraDamping);transform.position=new Vector3(pos.x,pos.y,-10);snap=false;}
 void FrameInterior(){var settings=world.Room.interior;float size=settings.QuantizedSize(Mathf.Max(1,target.pixelHeight));target.orthographicSize=size+transitionZoom;var b=settings.bounds;b.position+=world.CurrentZone.origin;if(target.aspect<.9f)b.yMin-=1.25f;var p=world.Player.Position+settings.offset+(target.aspect<.9f?Vector2.down*.7f:Vector2.zero);
  // Follow only where the complete room cannot fit; never alter the player.
  float h=size,w=size*target.aspect;float x=b.width>2*w?Mathf.Clamp(p.x,b.xMin+w,b.xMax-w):b.center.x;float y=b.height>2*h?Mathf.Clamp(p.y,b.yMin+h,b.yMax-h):b.center.y;
  var desired=new Vector3(x,y,-10);var pos=snap?desired:Vector3.SmoothDamp(transform.position,desired,ref velocity,settings.settleSeconds);float ppu=target.pixelHeight/(2*target.orthographicSize);transform.position=new Vector3(Mathf.Round(pos.x*ppu)/ppu,Mathf.Round(pos.y*ppu)/ppu,-10);snap=false;
 }

}
}
