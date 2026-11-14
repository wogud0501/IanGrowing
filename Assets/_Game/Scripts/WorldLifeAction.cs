using System;
using System.Collections;
using UnityEngine;
namespace IanGame {
public enum LifeActionVisual {AlignOnly,Bed,Dining,Appliance,Shower}
// A presentation adapter on existing interactables. LifeWorldController remains
// the single action/effect owner; building doors are deliberately not involved.
public sealed class WorldLifeAction:MonoBehaviour {
 public LifeActionVisual mode;public Transform approach,anchor,standUp;public Vector2 facing=Vector2.up;
 public ShowerBoothVisual booth;public SpriteRenderer overlay;public Sprite[] frames;public WorldActivitySlot seat;public bool familyOwned;
 public bool Ready{get;private set;}public bool Completed{get;private set;}public int Frame{get;private set;}
 bool care,infantCare;WorldPlayerMotor actor;WorldInteractable target;bool active;bool colliderWasEnabled,visualWasEnabled;SpriteRenderer furnitureVisual;
 public bool Available=>seat==null||string.IsNullOrEmpty(seat.Occupant);
 public IEnumerator Enter(LifeWorldController world,int action,Func<bool> cancelled){
  Ready=false;Completed=false;care=mode==LifeActionVisual.Bed&&action!=5;infantCare=care&&world.Service.Game.growth.Age==0;actor=world.Player;target=GetComponent<WorldInteractable>();if(seat!=null&&!seat.TryReserve("player"))yield break;active=true;furnitureVisual=target.visual;colliderWasEnabled=actor.GetComponent<CircleCollider2D>().enabled;visualWasEnabled=actor.sprite.enabled;
  var p=approach==null?target.Point:(Vector2)approach.position;
  if(actor.MovementAllowed&&Vector2.Distance(actor.Position,p)>.05f){actor.BeginTraversal(p,2.2f);while(actor.IsTraversing&&actor.TraversalSeconds<5){if(cancelled())yield break;yield return null;}if(!actor.TraversalArrived)yield break;}
  actor.Face(facing);actor.Pose(mode==LifeActionVisual.Bed?"Sit":mode==LifeActionVisual.Dining?"Sit":"Interact");
  if(mode==LifeActionVisual.Bed||mode==LifeActionVisual.Dining){actor.GetComponent<CircleCollider2D>().enabled=false;seat?.MarkOccupied("player");}
  var growth=actor.growthVisual;
  if(growth!=null&&growth.Young&&(mode==LifeActionVisual.Bed||mode==LifeActionVisual.Dining)){
   actor.GetComponent<CircleCollider2D>().enabled=false;growth.ActionOrder=furnitureVisual.sortingOrder+3;actor.sprite.enabled=true;furnitureVisual.enabled=true;
   if(mode==LifeActionVisual.Bed){growth.InBed=true;var destination=growth.SleepPoint(this);if(actor.MovementAllowed){actor.Pose("Sit");yield return Wait(.2f,cancelled);actor.BeginTraversal(destination,1.7f);while(actor.IsTraversing&&actor.TraversalSeconds<5){if(cancelled())yield break;yield return null;}if(!actor.TraversalArrived)yield break;actor.CancelTraversal();}else growth.ActionOffset=destination-actor.Position;actor.Pose(action==-4?"CareBath":care?(action==3||action==4?"CareFeed":"Play"):"Sleep");}
   else{actor.Pose("Sit");yield return Wait(.25f,cancelled);actor.BeginTraversal(growth.Inside(this),1.7f);while(actor.IsTraversing&&actor.TraversalSeconds<5){if(cancelled())yield break;yield return null;}if(!actor.TraversalArrived)yield break;actor.CancelTraversal();growth.Seated=true;actor.Face(facing);actor.Pose("Eat");}
  }
  else if(care&&world.Service.Game.growth.Age==0){Show(action==3||action==4?13:14);}
  else if(mode==LifeActionVisual.Bed&&!care){int start=world.Service.Game.growth.Age==0&&frames.Length>=16?12:0;yield return Sequence(start,Math.Min(start+2,frames.Length-1),.2f,cancelled);if(cancelled())yield break;actor.Warp(anchor==null?p:(Vector2)anchor.position);Show(start==12?15:3);}
  else if(mode==LifeActionVisual.Dining){yield return Wait(.35f,cancelled);if(cancelled())yield break;actor.Warp(anchor==null?p:(Vector2)anchor.position);Show(8);}
  else if(mode==LifeActionVisual.Appliance){actor.Pose("Open Door");yield return Sequence(0,3,.15f,cancelled);actor.Pose("Pick Up Item");}
  else if(mode==LifeActionVisual.Shower){
   if(booth==null||!actor.MovementAllowed)yield break;actor.GetComponent<CircleCollider2D>().enabled=false;actor.Pose("Open Door");yield return booth.Door(1,cancelled);if(cancelled())yield break;
   actor.BeginTraversal(actor.growthVisual==null?(Vector2)anchor.position:actor.growthVisual.Inside(this),1.7f);while(actor.IsTraversing&&actor.TraversalSeconds<4){if(cancelled())yield break;yield return null;}if(!actor.TraversalArrived)yield break;actor.CancelTraversal();actor.Face(facing);yield return booth.Door(0,cancelled);if(cancelled())yield break;actor.Pose("Shower");seat?.MarkOccupied("player");booth.WaterRunning=true;
  }
  Ready=!cancelled();
 }
 public void Animate(float seconds){if(!active)return;if(infantCare&&frames.Length>=16)Show(13+Mathf.FloorToInt(seconds*3)%2);else if(mode==LifeActionVisual.Dining)Show(8+Mathf.FloorToInt(seconds*4)%4);}
 void Show(int index){if(overlay==null||frames==null||frames.Length==0)return;Frame=Mathf.Clamp(index,0,frames.Length-1);overlay.sprite=frames[Frame];overlay.enabled=true;overlay.color=Color.white;overlay.sortingOrder=furnitureVisual.sortingOrder+3;furnitureVisual.enabled=false;if(mode==LifeActionVisual.Bed||mode==LifeActionVisual.Dining){actor.sprite.enabled=false;if(actor.contactShadow!=null)actor.contactShadow.enabled=false;}}
 IEnumerator Sequence(int start,int end,float seconds,Func<bool> cancelled){for(int i=start;i<=end;i++){Show(i);yield return Wait(seconds,cancelled);if(cancelled())yield break;}}
 static IEnumerator Wait(float seconds,Func<bool> cancelled){for(float t=0;t<seconds;t+=Time.unscaledDeltaTime){if(cancelled())yield break;yield return null;}}
 public IEnumerator Exit(LifeWorldController world,Func<bool> cancelled){
  if(actor?.growthVisual!=null&&actor.growthVisual.Young&&(mode==LifeActionVisual.Bed||mode==LifeActionVisual.Dining)){
   actor.Pose(mode==LifeActionVisual.Bed?"Wake Up":"Stand Up");yield return Wait(.45f,cancelled);if(cancelled())yield break;
   actor.growthVisual.InBed=actor.growthVisual.Seated=false;actor.Pose("Idle");if(actor.MovementAllowed){actor.BeginTraversal(standUp.position,1.7f);while(actor.IsTraversing&&actor.TraversalSeconds<5){if(cancelled())yield break;yield return null;}if(!actor.TraversalArrived)yield break;actor.CancelTraversal();}ResetVisual();Completed=!cancelled();yield break;
  }
  if(care&&!infantCare){ResetVisual();Completed=!cancelled();yield break;}
  if(mode==LifeActionVisual.Bed&&!care){world.Player.Pose("Wake Up");if(world.Service.Game.growth.Age==0){Show(12);yield return Wait(.5f,cancelled);}else yield return Sequence(4,7,.18f,cancelled);}
  if(mode==LifeActionVisual.Appliance){for(int i=2;i>=0;i--){Show(i);yield return Wait(.15f,cancelled);}}
  if(mode==LifeActionVisual.Shower){booth.WaterRunning=false;actor.Pose("Open Door");yield return booth.Door(1,cancelled);if(cancelled())yield break;actor.Pose("Idle");actor.BeginTraversal(standUp.position,1.7f);while(actor.IsTraversing&&actor.TraversalSeconds<4){if(cancelled())yield break;yield return null;}if(!actor.TraversalArrived)yield break;actor.CancelTraversal();yield return booth.Door(0,cancelled);ResetVisual();Completed=!cancelled();yield break;}
  bool leaving=mode==LifeActionVisual.Bed||mode==LifeActionVisual.Dining;
  var destination=standUp==null?target.Point:(Vector2)standUp.position;
  if(leaving&&!cancelled()){
   if(world.Service.Game.growth.Age==0){actor.Warp(destination);}
   else{if(mode==LifeActionVisual.Bed)actor.Warp((Vector2)transform.position+Vector2.down*.05f);ResetVisual();actor.GetComponent<CircleCollider2D>().enabled=false;
    actor.Pose(mode==LifeActionVisual.Dining?"Stand Up":"Idle");yield return Wait(mode==LifeActionVisual.Dining?.45f:.1f,cancelled);if(!cancelled()){actor.BeginTraversal(destination,1.7f);while(actor.IsTraversing&&actor.TraversalSeconds<2){if(cancelled())break;yield return null;}actor.CancelTraversal();}actor.GetComponent<CircleCollider2D>().enabled=colliderWasEnabled;}
  }
  ResetVisual();Completed=!cancelled();
 }
 public void ResetVisual(){if(!active)return;active=false;Ready=false;if(overlay!=null)overlay.enabled=false;if(booth!=null)booth.ResetClosed();if(furnitureVisual!=null)furnitureVisual.enabled=mode!=LifeActionVisual.Shower;if(actor!=null){actor.growthVisual?.ClearAction();actor.sprite.enabled=visualWasEnabled;actor.GetComponent<CircleCollider2D>().enabled=colliderWasEnabled;if(actor.contactShadow!=null)actor.contactShadow.enabled=true;}seat?.Release("player");}
 public static void CopyTo(WorldInteractable target,WorldLifeAction source){if(source==null)return;var a=target.gameObject.AddComponent<WorldLifeAction>();a.mode=source.mode;a.frames=source.frames;a.facing=source.facing;a.approach=new GameObject("InteractionApproach").transform;a.approach.SetParent(target.transform,false);a.approach.localPosition=target.UsePoint;a.standUp=a.approach;a.anchor=new GameObject("ActionAnchor").transform;a.anchor.SetParent(target.transform,false);a.anchor.localPosition=source.anchor.localPosition;var overlay=new GameObject("Life action occupied",typeof(SpriteRenderer));overlay.transform.SetParent(target.transform,false);a.overlay=overlay.GetComponent<SpriteRenderer>();a.overlay.sharedMaterial=target.visual.sharedMaterial;a.overlay.enabled=false;}
 void OnDisable(){ResetVisual();}
}
}
