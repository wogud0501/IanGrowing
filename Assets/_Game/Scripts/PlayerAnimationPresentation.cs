using UnityEngine;
namespace IanGame {
public enum PlayerVisualState {Idle,Moving,Interacting,Sitting,Sleeping,Eating,Drinking,Studying,Working,Talking,UsingComputer,UsingPhone,Sick,Exhausted}
// Frame and pose owner. Receives observed movement; never queries keyboard or pays rewards.
public sealed class PlayerAnimationPresentation:MonoBehaviour {
 public WorldPlayerMotor motor;public PlayerVisualState State{get;private set;}public ActorMotion Motion{get;private set;}public float IdleSeconds{get;private set;}
 public event System.Action<string> Cue;float nextIdle=7,variationUntil;ActorMotion variation;GameManager game;Vector3 shadowScale;
 void Awake(){if(motor==null)motor=GetComponent<WorldPlayerMotor>();}
 void Start(){game=GameContext.Instance?.game;if(game?.items!=null)game.items.ItemConsumed+=Consumed;if(motor.contactShadow!=null)shadowScale=motor.contactShadow.transform.localScale;nextIdle=7+((System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)&int.MaxValue)%5);}
 public void Render(string pose,bool moving,bool running,float distance,ActorMotion idleMotion){
  if(game==null)game=GameContext.Instance?.game;if(game?.State==GameState.Paused)return;
  var advanced=motor.advanced;ActorMotion motion=WorldPlayerMotor.ParsePose(pose);
  if(pose=="Idle"){
   motion=moving?(running?ActorMotion.Run:ActorMotion.Walk):idleMotion;
   if(!moving&&motor.world.CanControl&&idleMotion==ActorMotion.Idle){IdleSeconds+=Time.unscaledDeltaTime;if(Time.unscaledTime>=variationUntil&&IdleSeconds>=nextIdle){variation=game!=null&&(game.stats.Health<25||motor.world.Service.State.fatigue>85)?ActorMotion.Exhausted:game!=null&&game.stats.Mental<25?ActorMotion.Stressed:ActorMotion.UsePhone;variationUntil=Time.unscaledTime+1.3f;IdleSeconds=0;nextIdle=9+((System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)&int.MaxValue)%7);Cue?.Invoke("IdleVariation");}
    if(Time.unscaledTime<variationUntil&&advanced?.library?.Find(variation,ActorAnimationSet.Direction(motor.Facing),false)!=null)motion=variation;
    else if(game!=null&&game.stats.Health<20&&advanced?.library?.Find(ActorMotion.Exhausted,ActorAnimationSet.Direction(motor.Facing),false)!=null)motion=ActorMotion.Exhausted;
   }else{IdleSeconds=0;variationUntil=0;}
  }else{IdleSeconds=0;variationUntil=0;}
  if(!motor.MovementAllowed&&(motion==ActorMotion.Walk||motion==ActorMotion.Run||motion==ActorMotion.Turn||motion==ActorMotion.Stop))motion=ActorMotion.Idle;
  Motion=motion;State=VisualState(motion);
  if(advanced!=null&&advanced.library!=null){advanced.Render(motion,motor.Facing,distance,motion==ActorMotion.Walk||motion==ActorMotion.Run);motor.bodyVisual.localPosition=motor.growthVisual==null?Vector3.zero:(Vector3)motor.growthVisual.DisplayOffset;motor.bodyVisual.localScale=Vector3.one*motor.visualScale;motor.bodyVisual.localRotation=motor.growthVisual==null?Quaternion.identity:motor.growthVisual.DisplayRotation;}
  else if(motor.locomotion!=null&&motor.walkSet!=null&&motor.walkSet.Complete){motor.locomotion.enabled=true;motor.locomotion.speed=0;motor.locomotion.Play((moving?"Walk_":"Idle_")+motor.walkSet.Direction(motor.Facing),0,moving?Mathf.Repeat(Time.unscaledTime*motor.ActualSpeed/motor.walkSet.strideLength,1):0);motor.locomotion.Update(0);}
  motor.sprite.sortingOrder=motor.growthVisual!=null&&motor.growthVisual.ActionOrder!=int.MinValue?motor.growthVisual.ActionOrder:1000-Mathf.RoundToInt(transform.position.y*100);
  var shadow=motor.contactShadow;if(shadow!=null&&shadowScale!=Vector3.zero){var profile=motor.growthVisual?.Current;float factor=profile==null?1:Mathf.Clamp(profile.height/1.74f,.22f,1.1f);shadow.transform.localScale=new Vector3(shadowScale.x*factor,shadowScale.y*(State==PlayerVisualState.Sitting?.8f:factor),shadowScale.z);shadow.enabled=State!=PlayerVisualState.Sleeping;}
 }
 static PlayerVisualState VisualState(ActorMotion m){switch(m){case ActorMotion.Walk:case ActorMotion.Run:return PlayerVisualState.Moving;case ActorMotion.LieDown:case ActorMotion.Sleep:case ActorMotion.WakeUp:return PlayerVisualState.Sleeping;case ActorMotion.SitDown:case ActorMotion.StandUp:case ActorMotion.SeatedIdle:return PlayerVisualState.Sitting;case ActorMotion.Eat:return PlayerVisualState.Eating;case ActorMotion.Drink:return PlayerVisualState.Drinking;case ActorMotion.Study:return PlayerVisualState.Studying;case ActorMotion.Work:return PlayerVisualState.Working;case ActorMotion.UseComputer:return PlayerVisualState.UsingComputer;case ActorMotion.UsePhone:return PlayerVisualState.UsingPhone;case ActorMotion.Talk:return PlayerVisualState.Talking;case ActorMotion.Sick:return PlayerVisualState.Sick;case ActorMotion.Exhausted:case ActorMotion.Tired:return PlayerVisualState.Exhausted;case ActorMotion.Idle:case ActorMotion.Stop:case ActorMotion.Turn:return PlayerVisualState.Idle;default:return PlayerVisualState.Interacting;}}
 void Consumed(ItemEvent item){if(game.items.catalog.Item(item.itemId)?.itemType==ItemType.Drink){motor.React(ActorMotion.Drink,1.4f);Cue?.Invoke("Drink");}}
 void OnDestroy(){if(game?.items!=null)game.items.ItemConsumed-=Consumed;}
 public void ResetTransient(){IdleSeconds=0;variationUntil=0;State=PlayerVisualState.Idle;}
 void OnDisable(){ResetTransient();}
}
}
