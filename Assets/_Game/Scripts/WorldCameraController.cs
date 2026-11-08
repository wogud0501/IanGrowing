using UnityEngine;
namespace IanGame {
public sealed class WorldCameraController:MonoBehaviour {
 public LifeWorldController world;public Camera target;Vector3 velocity;bool snap=true;
 public void Snap(){snap=true;velocity=Vector3.zero;}
 void LateUpdate(){if(world==null||world.Player==null||world.CurrentZone==null)return;var z=world.CurrentZone;float desiredSize=z.cameraSize>0?z.cameraSize:z.outdoor?world.Service.rules.outdoorCameraSize:world.Service.rules.indoorCameraSize;target.orthographicSize=snap?desiredSize:Mathf.Lerp(target.orthographicSize,desiredSize,Time.unscaledDeltaTime*6);float h=target.orthographicSize,w=h*target.aspect;var p=world.Player.Position;var center=z.origin;float hx=z.size.x/2,hy=z.size.y/2;float x=hx>w?Mathf.Clamp(p.x,center.x-hx+w,center.x+hx-w):center.x;float y=hy>h?Mathf.Clamp(p.y+.8f,center.y-hy+h,center.y+hy-h):center.y+.6f;var desired=new Vector3(x,y,-10);var pos=snap?desired:Vector3.SmoothDamp(transform.position,desired,ref velocity,world.Service.rules.cameraDamping);transform.position=new Vector3(Mathf.Round(pos.x*100)/100,Mathf.Round(pos.y*100)/100,-10);snap=false;}
}
}
