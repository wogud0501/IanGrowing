using System;
using System.Collections.Generic;
using UnityEngine;
namespace IanGame {
public enum DoorState {Closed,Opening,Open,Closing,Locked}
public enum DoorMechanism {HingedOak,HingedJade,SlidingGlass}
public sealed class WorldDoor:MonoBehaviour {
 public WorldInteractable portal;public LifeWorldController world;public WorldDoorStyle style;public SpriteRenderer leaf,apertureShadow;public BoxCollider2D closedCollider;
 public Vector2 normal=Vector2.up,threshold;public bool locked;public int opens=0,closes=24;public string staffId;
 public DoorState State{get;private set;}=DoorState.Closed;public float Aperture{get;private set;}public int Users=>users.Count;
 public Vector2 Plane=>(Vector2)transform.position+threshold;public Vector2 CrossPoint=>Plane+normal*.2f;
 public event Action<DoorState> StateChanged;readonly HashSet<string> users=new HashSet<string>();float releaseAt;
 void Awake(){ResetClosed();}
 public bool CanEnter(string actor,int hour,out string reason){reason=locked?"잠긴 문입니다.":actor==staffId?null:opens<closes?(hour<opens||hour>=closes?$"출입 시간 {opens:00}:00–{closes:00}:00":null):(hour>=closes&&hour<opens?"지금은 문이 닫혀 있습니다.":null);return reason==null;}
 public bool Acquire(string actor,int hour,out string reason){if(!CanEnter(actor,hour,out reason)){Set(DoorState.Locked);return false;}users.Add(actor);if(State!=DoorState.Open&&State!=DoorState.Opening){Set(DoorState.Opening);if(world!=null&&world.audio!=null&&world.CurrentZone!=null&&portal.zone==world.CurrentZone.id)world.audio.OpenDoor();}return true;}
 public void Release(string actor){users.Remove(actor);releaseAt=Time.unscaledTime+.12f;}
 public void ResetClosed(){users.Clear();Aperture=0;Set(locked?DoorState.Locked:DoorState.Closed);Draw();}
 void OnDisable(){ResetClosed();}
 void Update(){
  if(world!=null&&world.Service!=null&&world.Service.Game.State==GameState.Paused)return;
  if(State==DoorState.Opening){Aperture=Mathf.MoveTowards(Aperture,1,Time.unscaledDeltaTime/Mathf.Max(.1f,style==null?.32f:style.seconds));if(Aperture>=1)Set(DoorState.Open);}
  if(State==DoorState.Open&&users.Count==0&&Time.unscaledTime>=releaseAt&&!PlayerInSweep())Set(DoorState.Closing);
  if(State==DoorState.Closing){if(PlayerInSweep()){Set(DoorState.Opening);return;}Aperture=Mathf.MoveTowards(Aperture,0,Time.unscaledDeltaTime/Mathf.Max(.1f,style==null?.32f:style.seconds));if(Aperture<=0)Set(locked?DoorState.Locked:DoorState.Closed);}
  Draw();
 }
 bool PlayerInSweep()=>world!=null&&world.CurrentZone!=null&&world.CurrentZone.id==portal.zone&&Mathf.Abs(world.Player.Position.x-Plane.x)<.65f&&Mathf.Abs(world.Player.Position.y-Plane.y)<.38f;
 void Set(DoorState state){if(State==state)return;State=state;StateChanged?.Invoke(state);}
 void Draw(){bool cutaway=normal.y<0&&world!=null&&world.CurrentZone!=null&&world.CurrentZone.id==portal.zone&&Mathf.Abs(world.Player.Position.x-Plane.x)<.85f&&world.Player.Position.y>Plane.y&&world.Player.Position.y<Plane.y+2;
  if(leaf!=null){var color=leaf.color;color.a=cutaway?.32f:1;leaf.color=color;}if(apertureShadow!=null){var color=apertureShadow.color;color.a=cutaway?.25f:1;apertureShadow.color=color;}
  if(closedCollider!=null)closedCollider.enabled=State!=DoorState.Open;if(leaf!=null&&style!=null&&style.openingFrames.Length>1){leaf.sprite=style.openingFrames[Mathf.Clamp(Mathf.RoundToInt(Aperture*(style.openingFrames.Length-1)),0,style.openingFrames.Length-1)];leaf.sortingOrder=1000-Mathf.RoundToInt(Plane.y*100);}}
}
}
