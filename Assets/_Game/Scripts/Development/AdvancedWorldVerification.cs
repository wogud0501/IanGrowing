#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace IanGame.Editor {
public static class AdvancedWorldVerification {
 public static IEnumerator Mobile(LifeWorldController w,GameManager g,Action<bool,string> check,Action<GameObject,string> audit){
  var mobile=w.hud.GetComponent<WorldMobileHUD>();check(mobile!=null&&mobile.joystick.controlPath=="<Gamepad>/leftStick","Touch joystick drives the shared Gamepad Move binding");check(mobile.interact.controlPath=="<Gamepad>/buttonSouth"&&mobile.inventory.controlPath=="<Gamepad>/buttonWest"&&mobile.menu.controlPath=="<Gamepad>/start","Touch buttons use the existing World actions");
  var previousBackground=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;int fps=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;bool simulated=mobile.simulateMobile;var safe=mobile.safeLayout;bool safeSim=safe.simulateSafeArea;Rect oldSafe=safe.simulatedSafeArea;int oldLarge=PlayerPrefs.GetInt("ian.ui.largeText",0);g.clock.realtimeEnabled=false;
  try{
   mobile.simulateMobile=true;var before=g.CaptureState();Vector2 position=w.Player.Position;var profiles=new HashSet<WorldScreenProfile>();
   foreach(var size in new[]{new[]{904,2316},new[]{1840,2208},new[]{904,2316},new[]{2340,1080},new[]{1920,1440},new[]{1840,2208},new[]{904,2316}}){
    CommercialSliceVerification.SetSize(size[0],size[1]);yield return new WaitForSecondsRealtime(.2f);safe.simulateSafeArea=true;safe.simulatedSafeArea=size[0]<size[1]?new Rect(0,90,size[0],size[1]-140):new Rect(80,0,size[0]-120,size[1]);mobile.Apply(true);yield return new WaitForSecondsRealtime(.3f);profiles.Add(mobile.Profile);
    check(w.Player.Position==position&&g.clock.Day==before.day&&g.economy.Cash==before.stats.money&&g.world.State.zone==before.world.zone,"Resize preserves actual game state: "+size[0]+"x"+size[1]);
    foreach(var r in new[]{mobile.joystick.transform as RectTransform,mobile.interact.transform as RectTransform,mobile.inventory.transform as RectTransform,mobile.menu.transform as RectTransform}){var corners=new Vector3[4];r.GetWorldCorners(corners);check(corners.All(p=>safe.simulatedSafeArea.Contains((Vector2)p)),"Touch target inside simulated safe area: "+r.name+" / "+size[0]);}
    audit(w.hud.gameObject,"Mobile HUD "+size[0]+"x"+size[1]);ScreenCapture.CaptureScreenshot("Docs/Phase26/Advanced/Screenshots/Mobile-"+size[0]+"x"+size[1]+".png");yield return null;
   }
   check(profiles.Contains(WorldScreenProfile.PhonePortrait)&&profiles.Contains(WorldScreenProfile.ExpandedTouch)&&profiles.Contains(WorldScreenProfile.WideTouch),"Fold, unfold and rotation use three distinct layouts without scene reload");
   PlayerPrefs.SetInt("ian.ui.largeText",1);mobile.Apply(true);for(int n=0;n<7;n++){w.hud.navigation.Open(n);yield return new WaitForSecondsRealtime(.3f);audit(w.hud.navigation.panels[n],"Compact 115% menu "+n);}while(w.hud.shell.Depth>0)w.hud.shell.Pop();yield return new WaitForSecondsRealtime(.3f);
   var action=(UnityEngine.InputSystem.InputActionMap)typeof(WorldPlayerMotor).GetField("gameplay",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(w.Player);
   bool received=false;var interactAction=action.FindAction("Interact");System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> delivered=c=>{if(c.ReadValue<float>()>.5f)received=true;};interactAction.performed+=delivered;
   try{mobile.Apply(true);mobile.interact.enabled=true;mobile.interact.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));yield return null;yield return null;check(received,"OnScreenButton performed event reaches the real cloned gameplay action");}finally{mobile.interact.OnPointerUp(null);interactAction.performed-=delivered;}
   if(w.Busy){w.CancelAction();float cancelEnd=Time.unscaledTime+8;while(w.Busy&&Time.unscaledTime<cancelEnd)yield return null;}while(w.hud.shell.Depth>0){int depth=w.hud.shell.Depth;w.hud.shell.Pop();if(w.hud.shell.Depth>=depth)break;}yield return null;mobile.Apply(true);mobile.joystick.enabled=mobile.interact.enabled=w.CanControl;yield return null;yield return null;check(action.FindAction("Interact").ReadValue<float>()<.1f,"Touch release clears interaction action");
   float readyUntil=Time.unscaledTime+2;while(Time.unscaledTime<readyUntil&&(!w.CanControl||mobile.joystick.control==null))yield return null;
   check(mobile.joystick.control!=null,"Virtual joystick ready: enabled="+mobile.joystick.enabled+" active="+mobile.touchRoot.gameObject.activeInHierarchy+" canControl="+w.CanControl+" busy="+w.Busy+" top="+w.hud.shell.Top+" state="+g.State+" room="+w.Room?.roomId+" savedRoom="+g.world.State.zone+" transition="+GameContext.Instance.transitions.IsTransitioning+" birth="+g.BirthIntroductionPending+" text="+w.Player.TextInputFocused+" developer="+DeveloperInputLock.Active);
   UnityEngine.InputSystem.InputSystem.QueueStateEvent(mobile.joystick.control.device,new UnityEngine.InputSystem.LowLevel.GamepadState{leftStick=new Vector2(.6f,.8f)});yield return null;yield return null;check(action.FindAction("Move").ReadValue<Vector2>().magnitude>.8f,"Virtual joystick device shares the actual Move action");
   mobile.SendMessage("OnApplicationPause",true);yield return null;check(!mobile.touchRoot.gameObject.activeSelf,"Background pause releases virtual controls");mobile.SendMessage("OnApplicationPause",false);yield return new WaitForSecondsRealtime(.2f);check(mobile.touchRoot.gameObject.activeSelf&&action.FindAction("Move").ReadValue<Vector2>().magnitude<.1f,"Resume recreates clean touch state without stuck movement");
  }finally{UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=previousBackground;mobile.simulateMobile=simulated;safe.simulateSafeArea=safeSim;safe.simulatedSafeArea=oldSafe;PlayerPrefs.SetInt("ian.ui.largeText",oldLarge);CommercialSliceVerification.SetSize(1280,720);mobile.Apply(true);Application.targetFrameRate=fps;QualitySettings.vSyncCount=vsync;}
 }
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
  var bank=w.Player.advanced.library;foreach(ActorMotion motion in Enum.GetValues(typeof(ActorMotion)).Cast<ActorMotion>().Where(m=>m<=ActorMotion.Thinking))check(bank.Find(motion,ActorDirection.Down)!=null,"Authored clip available: "+motion);
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
