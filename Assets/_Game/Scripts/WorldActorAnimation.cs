using UnityEngine;
namespace IanGame {
// Shared visual state for player and NPCs. Neither direction nor stride is derived
// from a waypoint index or wall-clock time: both follow observed displacement.
public sealed class WorldActorAnimation:MonoBehaviour {
 public ActorAnimationSet library;public SpriteRenderer visual;public Animator legacyAnimator;
 public ActorMotion Motion{get;private set;}=ActorMotion.Idle;public ActorDirection Direction{get;private set;}=ActorDirection.Down;
 public float Distance{get;private set;}public int Frame{get;private set;}public bool UsedDirectionFallback{get;private set;}
 public event System.Action<ActorMotion> Entered,Finished;
 float entered;bool finished;ActorSpriteClip active;
 public void Face(Vector2 direction){Direction=ActorAnimationSet.Direction(direction,Direction);}
 public void Play(ActorMotion motion,Vector2 facing,bool restart=false){
  Face(facing);var clip=library==null?null:library.Find(motion,Direction);
  if(!restart&&Motion==motion&&active==clip)return;
  Motion=motion;active=clip;entered=Time.unscaledTime;finished=false;Frame=0;
  UsedDirectionFallback=clip!=null&&clip.direction!=Direction;
  Entered?.Invoke(motion);
 }
 public bool Render(ActorMotion motion,Vector2 facing,float actualDistance=0,bool locomotion=false){
  Play(motion,facing);Distance+=Mathf.Max(0,actualDistance);
  if(active==null||visual==null)return false;
  if(legacyAnimator!=null)legacyAnimator.enabled=false;
  int count=active.frames.Length,index;
  if(locomotion)index=motion==ActorMotion.Idle?0:Mathf.FloorToInt(Distance/Mathf.Max(.1f,active.strideLength)*count)%count;
  else{
   int elapsed=Mathf.FloorToInt((Time.unscaledTime-entered)*active.framesPerSecond);
   index=active.loop&&elapsed>=count?Mathf.Clamp(active.loopFrom,0,count-1)+(elapsed-count)%Mathf.Max(1,count-Mathf.Clamp(active.loopFrom,0,count-1)):Mathf.Min(elapsed,count-1);
   if(!active.loop&&elapsed>=count&&!finished){finished=true;Finished?.Invoke(Motion);}
  }
  Frame=index;visual.sprite=active.frames[index];visual.flipX=active.mirror;return true;
 }
 public bool Complete=>active!=null&&!active.loop&&(Time.unscaledTime-entered)*active.framesPerSecond>=active.frames.Length;
 public float Duration(ActorMotion motion,Vector2 face){var c=library?.Find(motion,ActorAnimationSet.Direction(face));return c==null?0:c.frames.Length/Mathf.Max(1,c.framesPerSecond);}
}
}
