using UnityEngine;
namespace IanGame {
public enum FootstepSurface {Wood,Tile,Road,Grass}
public sealed class WorldLifeAudio:MonoBehaviour {
 public event System.Action<FootstepSurface> FootstepRequested;public FootstepSurface? SurfaceOverride;
 public LifeWorldController world;public AudioClip woodStep,stoneStep,door,cityAir,roomAir;
 public AudioSource effects,ambience;[Range(0,1)]public float volume=.3f;
 Vector2 previous;string zone;float distance;
 void Update(){
  if(world==null||world.Service==null||ambience==null)return;bool outdoor=world.CurrentZone!=null&&world.CurrentZone.outdoor;var profile=world.GetComponent<SceneVisualPresentation>()?.profile;var desired=profile!=null&&profile.ambience!=null?profile.ambience:outdoor?cityAir:roomAir;
  if(ambience.clip!=desired){ambience.Stop();ambience.clip=desired;ambience.volume=0;if(desired!=null)ambience.Play();}
  bool active=world.Service.Game.State==GameState.Playing&&world.hud.shell.Depth==0;float night=world.Service.Hour<6||world.Service.Hour>=22?.55f:1;ambience.volume=Mathf.MoveTowards(ambience.volume,active?volume*(outdoor?.45f:.22f)*night:0,Time.unscaledDeltaTime*.2f);
  Vector2 position=world.Player.Position;if(zone!=world.Service.State.zone){zone=world.Service.State.zone;previous=position;distance=0;return;}float travelled=Vector2.Distance(position,previous);previous=position;
  if(!active||world.Busy&&!world.Player.IsTraversing){distance=0;return;}if(travelled>.2f)return;distance+=travelled;float step=world.Player.walkSet==null?.9f:world.Player.walkSet.strideLength*.5f;if(distance>=step){distance%=step;var surface=SurfaceOverride??(outdoor?FootstepSurface.Road:zone.Contains("bathroom")?FootstepSurface.Tile:FootstepSurface.Wood);FootstepRequested?.Invoke(surface);var clip=surface==FootstepSurface.Road||surface==FootstepSurface.Tile?stoneStep:woodStep;if(clip!=null&&effects!=null){effects.pitch=1+(Time.frameCount%3-1)*.035f;effects.PlayOneShot(clip,volume);}}
 }
 public void OpenDoor(){if(door!=null&&effects!=null){effects.pitch=1;effects.PlayOneShot(door,volume*.8f);}}
 public void CloseDoor(){if(door!=null&&effects!=null){effects.pitch=1.18f;effects.PlayOneShot(door,volume*.45f);}}
}
}
