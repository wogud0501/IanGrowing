using System;
using System.Collections.Generic;
using UnityEngine;
namespace IanGame {
public enum DoorState {Closed,Opening,Open,Closing,Locked}
public enum DoorMechanism {HingedOak,HingedJade,SlidingGlass}
public sealed class WorldDoor:MonoBehaviour {
 public WorldInteractable portal;public LifeWorldController world;public WorldDoorStyle style;public SpriteRenderer leaf,frame,apertureShadow;public BoxCollider2D closedCollider;
 public Vector2 normal=Vector2.up,threshold;public bool locked;public int opens=0,closes=24;public string staffId;
 public DoorState State{get;private set;}=DoorState.Closed;public float Aperture{get;private set;}public int Users=>users.Count;
 // Serialized normal/threshold remain world-space for existing scene compatibility.
 public Vector2 Plane=>(Vector2)transform.position+threshold;public Vector2 CrossPoint=>Plane+normal*.2f;
 public float SideOf(Vector2 position){var local=transform.InverseTransformPoint(position);var plane=transform.InverseTransformPoint(Plane);var axis=transform.InverseTransformVector(normal).normalized;return Vector2.Dot((Vector2)(local-plane),axis)>=0?1:-1;}
 public Vector2 Approach(Vector2 from)=>Plane+normal*SideOf(from)*.35f;
 public Vector2 Passage(Vector2 from)=>Plane-normal*SideOf(from)*.2f;
 public event Action<DoorState> StateChanged;readonly HashSet<string> users=new HashSet<string>();float releaseAt,progress;bool passable;WorldNPCScheduler scheduler;
 void Awake(){scheduler=world==null?null:world.GetComponent<WorldNPCScheduler>();ResetClosed();}
 bool IsExit=>world!=null&&world.Service!=null&&portal!=null&&world.Service.rules.Zone(portal.zone)?.outdoor==false&&world.Service.rules.Zone(portal.targetZone)?.outdoor==true;
 public bool CanEnter(string actor,int hour,out string reason){if(actor=="player"&&world!=null&&world.Service!=null&&portal!=null&&!GrowthAccess.Zone(world.Service.Game,portal.targetZone,out reason))return false;reason=locked?"잠긴 문입니다.":actor==staffId||IsExit?null:opens<closes?(hour<opens||hour>=closes?$"출입 시간 {opens:00}:00–{closes:00}:00":null):(hour>=closes&&hour<opens?"지금은 문이 닫혀 있습니다.":null);return reason==null;}
 public bool Acquire(string actor,int hour,out string reason){
  // A rejected new request must never interrupt another user's active animation.
  if(!CanEnter(actor,hour,out reason)){if(progress<=0&&users.Count==0)Set(DoorState.Locked);return false;}
  if(string.IsNullOrEmpty(actor)){reason="출입자 ID가 없습니다.";return false;}
  users.Add(actor);if(State!=DoorState.Open&&State!=DoorState.Opening){Set(DoorState.Opening);if(world!=null&&world.audio!=null&&world.CurrentZone!=null&&portal.zone==world.CurrentZone.id)world.audio.OpenDoor();}return true;
 }
 public void Release(string actor){if(users.Remove(actor))releaseAt=Time.unscaledTime+.12f;}
 public void ResetClosed(){users.Clear();progress=Aperture=0;passable=false;Set(locked?DoorState.Locked:DoorState.Closed);Draw();}
 void OnDisable(){ResetClosed();}
 void Update(){if(world!=null&&world.Service!=null&&world.Service.Game.State==GameState.Paused)return;AdvanceAnimation(Time.unscaledDeltaTime);}
 public void AdvanceAnimation(float seconds){
  if(seconds<=0)return;
  if(State==DoorState.Opening){progress=Mathf.MoveTowards(progress,1,seconds/Mathf.Max(.1f,style==null?.38f:style.seconds));if(progress>=1){passable=true;Set(DoorState.Open);}}
  else if(State==DoorState.Open&&users.Count==0&&Time.unscaledTime>=releaseAt&&!Occupied())Set(DoorState.Closing);
  else if(State==DoorState.Closing){if(users.Count>0||Occupied())Set(DoorState.Opening);else{progress=Mathf.MoveTowards(progress,0,seconds/Mathf.Max(.1f,style==null?.38f:style.closeSeconds));if(progress<=0){passable=false;Set(locked?DoorState.Locked:DoorState.Closed);}}}
  Aperture=progress*progress*(3-2*progress);Draw();
 }
 bool InSweep(Vector2 p,float radius){if(closedCollider==null)return Vector2.Distance(p,Plane)<.8f;Vector2 local=closedCollider.transform.InverseTransformPoint(p);local-=closedCollider.offset;var scale=closedCollider.transform.lossyScale;return Mathf.Abs(local.x)<closedCollider.size.x/2+radius/Mathf.Abs(scale.x)&&Mathf.Abs(local.y)<closedCollider.size.y/2+(radius+.12f)/Mathf.Abs(scale.y);}
 bool Occupied(){if(world==null||world.CurrentZone==null||world.CurrentZone.id!=portal.zone)return false;if(world.Player!=null&&InSweep(world.Player.Position,.32f))return true;if(scheduler!=null)foreach(var actor in scheduler.actors)if(actor!=null&&actor.isActiveAndEnabled&&actor.zone==portal.zone&&InSweep(actor.transform.position,.28f))return true;return false;}
 void Set(DoorState state){if(State==state)return;bool latched=State==DoorState.Closing&&(state==DoorState.Closed||state==DoorState.Locked);State=state;if(latched&&world!=null&&world.audio!=null&&world.CurrentZone!=null&&portal.zone==world.CurrentZone.id)world.audio.CloseDoor();StateChanged?.Invoke(state);}
 static void Fade(SpriteRenderer sr,float alpha){if(sr==null)return;var c=sr.color;c.a=alpha;sr.color=c;}
 void Draw(){bool cutaway=normal.y<0&&world!=null&&world.Player!=null&&world.CurrentZone!=null&&world.CurrentZone.id==portal.zone&&Mathf.Abs(world.Player.Position.x-Plane.x)<.85f&&world.Player.Position.y>Plane.y&&world.Player.Position.y<Plane.y+2;
  Fade(leaf,cutaway?.32f:1);Fade(frame,cutaway?.32f:1);Fade(apertureShadow,cutaway?.25f:1);
  // Latch collision only while closed. Closing graphics never push an actor.
  if(closedCollider!=null)closedCollider.enabled=!passable;
  if(leaf!=null&&style!=null&&style.openingFrames.Length>1){leaf.sprite=style.openingFrames[Mathf.Clamp(Mathf.RoundToInt(Aperture*(style.openingFrames.Length-1)),0,style.openingFrames.Length-1)];leaf.sortingOrder=1000-Mathf.RoundToInt(Plane.y*100);if(frame!=null)frame.sortingOrder=leaf.sortingOrder+1;}
 }
}
}
