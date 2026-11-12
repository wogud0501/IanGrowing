#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace IanGame.Editor {
public static class AdvancedWorldVerification {
 public static IEnumerator Doors(LifeWorldController w,GameManager g,Action<bool,string> check){
  System.IO.Directory.CreateDirectory("Docs/Phase26/Advanced/Screenshots");
  foreach(var door in w.GetComponentsInChildren<WorldDoor>(true)){check(door.style.openingFrames.Length==6&&door.style.openingFrames.Distinct().Count()==6,"Six authored aperture frames: "+door.portal.id);check(door.closedCollider.enabled,"Closed door starts solid: "+door.portal.id);}
  long cash=g.economy.Cash;var effects=new HashSet<WorldTransitionStyle>();
  foreach(var id in new[]{"front-door","city-path","shop-front","shop-exit","office-front","work-exit","alley-path","house-entrance","bath-door","bath-exit"}){
   var portal=w.objects.First(o=>o.id==id);check(portal.zone==g.world.State.zone,"Portal belongs to current zone: "+id);w.Player.Warp(portal.Point);yield return null;
   var door=portal.GetComponent<WorldDoor>();bool startedOpen=door!=null&&door.State==DoorState.Open;var seen=new HashSet<Sprite>();var states=new HashSet<DoorState>();bool crossed=false,captured=false,commitOpen=true;check(w.Use(portal),"Actual portal action accepted: "+id);check(!w.Use(portal)&&w.Busy&&!w.CanControl,"Duplicate portal input blocked: "+id);float end=Time.unscaledTime+5;
   while(w.Busy&&Time.unscaledTime<end){if(door!=null){seen.Add(door.leaf.sprite);states.Add(door.State);if(g.world.State.zone==portal.targetZone){crossed=true;commitOpen&=door.State==DoorState.Open||door.State==DoorState.Closing;}if(!captured&&door.State==DoorState.Opening&&door.Aperture>.35f){ScreenCapture.CaptureScreenshot("Docs/Phase26/Advanced/Screenshots/"+id+"-Opening.png");captured=true;}}if(w.transitions.Active)effects.Add(w.transitions.Style);yield return null;}
   check(!w.Busy&&g.world.State.zone==portal.targetZone,"Door passage reaches intended area: "+id);
   if(door!=null){check(commitOpen,"Source was opened before destination commit: "+id);check((startedOpen||seen.Count>=3&&states.Contains(DoorState.Opening))&&crossed,"Visible leaf frames and passage observed: "+id);yield return new WaitForSecondsRealtime(.6f);check(door.State==DoorState.Closed&&door.closedCollider.enabled,"Source closes and collision restores: "+id);}
   check(w.CanControl&&g.economy.Cash==cash,"Control and wallet preserved after portal: "+id);
  }
  check(effects.Count>=3,"Three location-dependent transition profiles actually used");
  var locked=w.objects.First(o=>o.id=="bath-door");var guard=locked.GetComponent<WorldDoor>();guard.locked=true;guard.ResetClosed();w.Player.Warp(locked.Point);yield return null;check(!w.Use(locked)&&!w.Busy,"Locked door rejects entry without displacement");guard.locked=false;guard.ResetClosed();
  check(guard.Acquire("npc-test",g.world.Hour,out var why),"NPC can acquire the same real door");yield return new WaitForSecondsRealtime(.45f);check(guard.State==DoorState.Open&&!guard.closedCollider.enabled,"NPC opening releases physical collision");guard.Release("npc-test");w.Player.Warp(new Vector2(0,-2));yield return new WaitForSecondsRealtime(.6f);check(guard.State==DoorState.Closed,"NPC release closes the door");
  w.Player.Warp(locked.Point);var before=g.CaptureState();check(w.Use(locked),"Start portal before load interruption");yield return new WaitForSecondsRealtime(.12f);g.RestoreState(before);g.clock.realtimeEnabled=false;yield return new WaitForSecondsRealtime(1.2f);check(!w.Busy&&g.world.State.zone==before.world.zone&&w.CanControl,"Load during opening cancels transition and restores control safely");
 }
 public static IEnumerator Animation(LifeWorldController w,GameManager g,Action<bool,string> check){
  var bank=w.Player.advanced.library;foreach(ActorMotion motion in Enum.GetValues(typeof(ActorMotion)))check(bank.Find(motion,ActorDirection.Down)!=null,"Authored clip available: "+motion);
  foreach(ActorDirection d in Enum.GetValues(typeof(ActorDirection)))check(bank.Find(ActorMotion.Run,d,false)!=null,"Actual four-direction run clip: "+d);
  w.Player.Warp(new Vector2(-1,-2.8f));w.Player.BeginTraversal(new Vector2(1,-2.8f),2);var seen=new HashSet<Sprite>();var from=w.Player.Position;float timeout=Time.unscaledTime+3;
  while(w.Player.IsTraversing&&Time.unscaledTime<timeout){seen.Add(w.Player.sprite.sprite);yield return null;}
  check(w.Player.TraversalArrived&&Vector2.Distance(w.Player.Position,from)>1.8f,"Actual player Rigidbody traverses open floor");check(seen.Count>=3,"Player walking advances distinct rendered sprite frames during motion");
  yield return new WaitForSecondsRealtime(.4f);check(w.Player.advanced.Motion==ActorMotion.Idle,"Stopping returns to Idle");
  foreach(var pose in new[]{"Sleep","Wake Up","Sit","Stand Up","Eat","Drink","Wash","Shower","Work","Use Computer","Use Phone","Interact","Open Door","PickUpItem","PutDownItem","Happy","Sad","Tired","Angry","Stressed","Surprised","Thinking"}){
   w.Player.Pose(pose);seen.Clear();float end=Time.unscaledTime+.6f;while(Time.unscaledTime<end){seen.Add(w.Player.sprite.sprite);yield return null;}check(seen.Count>=2,"Real drawn action frames play: "+pose);
  }
  w.Player.Pose("Idle");check(w.Travel("city"),"Visit city for actual resident animation observation");while(w.Busy)yield return null;var street=w.GetComponentInChildren<WorldStreetLife>();
  g.clock.Restore(g.clock.Day,new TimeSnapshot{subDaySeconds=12*3600,elapsedSeconds=g.clock.ElapsedSeconds});yield return new WaitForSecondsRealtime(.3f);
  var p=street.pedestrians[0];var before=(Vector2)p.visual.transform.position;seen.Clear();float until=Time.unscaledTime+1.2f;while(Time.unscaledTime<until){seen.Add(p.visual.sprite);yield return null;}var delta=(Vector2)p.visual.transform.position-before;
  check(delta.magnitude>.2f&&seen.Count>=2,"Citizen actually walks and changes frames");check(p.animation.Direction==ActorAnimationSet.Direction(delta,p.animation.Direction),"Citizen facing follows measured displacement");
  foreach(var person in street.pedestrians)foreach(ActorDirection d in Enum.GetValues(typeof(ActorDirection)))check(person.animation.library.Find(ActorMotion.Walk,d,false)!=null,"Citizen direction bank available: "+person.visual.name+" / "+d);
  check(w.Travel("home"),"Return after animation verification");while(w.Busy)yield return null;
 }
}
}
#endif
