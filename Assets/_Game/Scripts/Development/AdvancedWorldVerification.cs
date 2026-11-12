#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace IanGame.Editor {
public static class AdvancedWorldVerification {
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
