#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace IanGame.Editor {
public static class RoomVisualVerification {
 static LifeWorldController W=>GameContext.Instance.game.world.Host;static SceneTransitionService T=>GameContext.Instance.transitions;
 static IEnumerator Wait(){float end=Time.unscaledTime+25;while((T.IsTransitioning||W.Busy)&&Time.unscaledTime<end)yield return null;if(T.IsTransitioning||W.Busy)throw new Exception("Indoor action/transition timeout");yield return null;yield return null;}
 static IEnumerator Go(string room,Action<bool,string> check){check(T.NavigateRoom(room,T.roomCatalog.rules.Zone(room).spawn,Vector2.down),"Room requested "+room);yield return Wait();check(W.Room.roomId==room,"Actual room active "+room);}
 static IEnumerator Shot(string name){Directory.CreateDirectory("Docs/RoomVisual/Screenshots");ScreenCapture.CaptureScreenshot("Docs/RoomVisual/Screenshots/"+name+".png");yield return null;}
 public static IEnumerator Run(GameManager g,Action<bool,string> check,Action<GameObject,string> audit,string mode){
  if(mode.StartsWith("roomVisualGrowth")){yield return GrowthVerification.Run(g,check,audit,mode);yield break;}
  check(!W.Player.MovementAllowed,"Newborn walking remains blocked");g.growth.Restore(new ProgressSnapshot{age=19});g.clock.realtimeEnabled=false;g.clock.Restore(4,new TimeSnapshot{subDaySeconds=14*3600});var stats=g.stats.Capture();stats.money=1000;g.stats.Restore(stats);check(g.economy.MoveResidence("room-visual/fixture","starter"),"Qualified paid personal-home fixture");
  yield return Go("family-bathroom",check);if(mode=="roomVisualBefore"){yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot("Docs/InteriorReconstruction/01_Current_Unity_Bathroom.png");yield return null;yield break;}W.hud.Notify("",2);yield return new WaitForSecondsRealtime(.3f);yield return Shot("Family-Bathroom-After");check(W.follow.target.orthographicSize<3.5f&&W.CurrentZone.size==new Vector2(5.2f,2.6f),"Compact real bathroom and closer integer-density framing");
  var rows=new List<string>();
  if(mode=="roomVisualArchitecture"){
   var z=W.CurrentZone;var construction=W.transform.Find("Architectural contact and cutaway");check(construction!=null&&construction.childCount>15,"Separate construction, wall, shelf and cabinet objects exist");
   foreach(var p in new[]{new Vector2(-2.8f,-1.58f),new Vector2(2.8f,2.55f)}){var v=W.follow.target.WorldToViewportPoint(z.origin+p);check(v.x>0&&v.x<1&&v.y>0&&v.y<1,"Full physical room frame fits the actual viewport");}
   var door=W.objects.Single(o=>o.kind==WorldInteractionKind.Door).GetComponent<WorldDoor>();check(Mathf.Abs(door.Plane.y-(z.origin.y-z.size.y/2))<.01f,"Door threshold meets the front wall boundary");check(door.apertureShadow.sortingOrder<door.leaf.sortingOrder,"Door backing cannot cover the solid leaf");check(door.GetComponentInChildren<WorldDoorLeafVisual>().FrameGeometry.bounds.size.y<1.8f,"Bathroom-only frame is proportional without changing other door styles");
   W.Player.BeginTraversal(z.origin+new Vector2(1.15f,-.65f),1.5f);float end=Time.unscaledTime+5;while(W.Player.IsTraversing&&Time.unscaledTime<end)yield return null;check(W.Player.TraversalArrived,"Actual Rigidbody walks clear of the doorway toward the shower");W.Player.CancelTraversal();W.Player.Face(Vector2.right);W.follow.Snap();yield return new WaitForSecondsRealtime(.25f);Directory.CreateDirectory("Docs/InteriorReconstruction/Screenshots");ScreenCapture.CaptureScreenshot("Docs/InteriorReconstruction/Screenshots/03_Unity_After.png");yield return null;
   var basin=W.objects.Single(o=>o.kind==WorldInteractionKind.Wash);W.Player.BeginTraversal(basin.Point,1.5f);end=Time.unscaledTime+5;while(W.Player.IsTraversing&&Time.unscaledTime<end)yield return null;check(W.Player.TraversalArrived,"Actual walkable route reaches basin without colliding with cabinet or toilet");W.Player.CancelTraversal();check(W.Use(basin),"Existing basin wash interaction remains usable");yield return Wait();check(W.CanControl,"Washing restores player input");yield break;
  }
  if(mode=="roomVisualShower"){
   foreach(string room in new[]{"family-bathroom","bathroom"}){yield return Go(room,check);if(room=="family-bathroom"){var o=W.objects.Single(o=>o.kind==WorldInteractionKind.Shower);W.Player.Warp(o.Point);g.growth.Restore(new ProgressSnapshot{age=0});yield return null;check(!W.Use(o)&&!W.Busy&&!W.Player.MovementAllowed,"Newborn cannot start adult shower or walk through the new booth");g.growth.Restore(new ProgressSnapshot{age=19});}yield return Shower(g,check,room);}
   yield break;
  }
  if(mode=="roomVisualCycles"){yield return Cycles(check);yield break;}
  if(mode=="roomVisualDoors"){foreach(var edge in T.roomCatalog.portals){yield return Go(edge.sourceRoom,check);var o=W.objects.Single(o=>o.id==edge.doorId);var d=o.GetComponent<WorldDoor>();if(d!=null){check(d.apertureShadow==null||d.apertureShadow.sortingOrder<d.leaf.sortingOrder,"Door backing depth "+edge.doorId);d.opens=0;d.closes=24;W.Player.Warp(d.Approach(d.Plane-d.normal));}else W.Player.Warp(o.Point);yield return null;check(W.Use(o)&&!W.Use(o),"Existing opening/travel and input exclusion "+edge.doorId);yield return Wait();check(W.Room.roomId==edge.destinationRoom&&W.CanControl,"Bidirectional destination/control "+edge.doorId);}yield break;}
  if(mode=="roomVisualMobile"){W.Player.Warp(W.CurrentZone.origin+new Vector2(-2,.9f));yield return null;yield return null;check(W.Nearest==null,"Raw touch binding probe is outside automatic interaction range");yield return AdvancedWorldVerification.Mobile(W,g,check,audit);yield return Shot("Family-Bathroom-Mobile-Final");yield break;}
  foreach(var room in T.roomCatalog.rooms.Where(r=>!T.roomCatalog.rules.Zone(r.roomId).outdoor&&(mode!="roomVisualPolish"||new[]{"bathroom","family-ian-bedroom","family-parents-bedroom","family-kitchen"}.Contains(r.roomId)))){
   long money=g.economy.Cash;double time=g.clock.SubDaySeconds;yield return Go(room.roomId,check);var c=W.follow.target;float scale=c.pixelHeight/(c.orthographicSize*2*W.Room.interior.referencePixelsPerUnit);check(W.Room.interior.enabled&&Mathf.Abs(scale-Mathf.Round(scale))<.001f,"Integer camera density "+room.roomId);check(g.economy.Cash==money&&g.clock.SubDaySeconds==time,"Presentation preserves cash and time "+room.roomId);check(SceneManager.sceneCount==1&&UnityEngine.Object.FindObjectsByType<WorldPlayerMotor>(FindObjectsSortMode.None).Length==1,"Separated scene / single player "+room.roomId);if(room.roomId=="bathroom")check(W.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="Walkable floor"&&r.enabled&&r.bounds.size.x>=W.CurrentZone.size.x-.01f&&r.bounds.size.y>=W.CurrentZone.size.y-.01f),"Previously disabled personal bathroom floor is actually rendered");var mass=W.GetComponent<InteriorSurround>();check(mass!=null&&mass.plaster.bounds.size.x>=c.orthographicSize*2*c.aspect&&mass.plaster.bounds.size.y>=c.orthographicSize*2,"Textured building surround covers view "+room.roomId);var locationLabel=W.hud.GetComponentInChildren<CompactWorldStatusBar>().place;locationLabel.ForceMeshUpdate();check(locationLabel.textInfo.lineCount==1&&!locationLabel.isTextOverflowing,"Actual Korean location text stays on one unclipped line "+room.roomId);audit(W.hud.gameObject,"Indoor HUD1280 / "+room.roomId);W.hud.Notify("",2);yield return new WaitForSecondsRealtime(.15f);yield return Shot(room.sceneName);rows.Add(room.sceneName+" size="+W.CurrentZone.size+" camera="+c.orthographicSize+" density="+scale+" actors="+W.GetComponent<WorldNPCScheduler>().actors.Length);
  }
  File.WriteAllLines("Docs/RoomVisual/AFTER_METRICS.txt",rows);yield return Go("family-bathroom",check);Vector2 position=W.Player.Position;float ppu720=W.follow.target.pixelHeight/(2*W.follow.target.orthographicSize);CommercialSliceVerification.SetSize(1920,1080);yield return new WaitForSecondsRealtime(.3f);float ppu1080=W.follow.target.pixelHeight/(2*W.follow.target.orthographicSize);check(Mathf.Abs(ppu1080/ppu720-1.5f)<.08f&&W.Player.Position==position,"1080 framing preserves relative density and player position");audit(W.hud.gameObject,"Bathroom HUD1920");yield return Shot("Family-Bathroom-1920");CommercialSliceVerification.SetSize(1280,720);
 }
 static IEnumerator Cycles(Action<bool,string> check){
  foreach(string room in new[]{"family-bathroom","bathroom","workplace"}){
   yield return Go(room,check);var w=W;var d=w.objects.First(o=>o.kind==WorldInteractionKind.Door).GetComponent<WorldDoor>();w.Player.Warp(d.Plane+Vector2.up*1.5f);yield return null;
   var cycleGame=GameContext.Instance.game;cycleGame.Pause();try{
   var root=d.transform.position;var frame=d.frame.transform.position;bool stable=true,opaque=true;d.opens=0;d.closes=24;
   foreach(int fps in new[]{30,60})for(int cycle=0;cycle<20;cycle++){
    check(d.Acquire("visual-cycle",14,out _),"Cycle request "+room+"/"+fps+"/"+cycle);
    int steps=Mathf.CeilToInt((d.style.seconds+.1f)*fps);for(int i=0;i<steps;i++){d.AdvanceAnimation(1f/fps);if(cycle==0&&fps==30&&i==steps/2){yield return Shot("Door-Half-"+room);}}
    stable &= d.State==DoorState.Open&&!d.closedCollider.enabled;
    if(cycle==0&&fps==30)yield return Shot("Door-Open-"+room);
    var mesh=d.GetComponentInChildren<WorldDoorLeafVisual>();opaque &= mesh!=null&&mesh.GetComponentsInChildren<MeshRenderer>().Any(m=>m.enabled);
    d.Release("visual-cycle");if(cycle==0)yield return new WaitForSecondsRealtime(.14f);
    // Same-frame cycles must honor the real release latch before simulating close.
    yield return new WaitForSecondsRealtime(.13f);for(int i=0;i<Mathf.CeilToInt((d.style.closeSeconds+.15f)*fps);i++)d.AdvanceAnimation(1f/fps);
    stable &= d.State==DoorState.Closed&&d.closedCollider.enabled&&root==d.transform.position&&frame==d.frame.transform.position;
   }
   check(stable&&opaque,"20 cycles per30/60Hz step: exact root/frame, visible solid leaf, collision "+room);yield return Shot("Door-Closed-"+room);
   }finally{cycleGame.Resume();}
  }
 }
 static IEnumerator Shower(GameManager g,Action<bool,string> check,string room){var w=W;var o=w.objects.Single(o=>o.kind==WorldInteractionKind.Shower);var a=o.GetComponent<WorldLifeAction>();var b=a.booth;w.Player.Warp(a.approach.position);w.follow.Snap();g.world.State.hygiene=20;double time=g.clock.SubDaySeconds;yield return null;check(w.Use(o)&&!w.Use(o),"Actual shower interaction and duplicate guard "+room);var frames=new HashSet<Sprite>();var positions=new HashSet<Vector2>();bool inside=false,open=false,aligned=true,shot=false,entryShot=false;float end=Time.unscaledTime+20;while(w.Busy&&Time.unscaledTime<end){open|=b.Aperture>.95f;positions.Add(w.Player.Position);if(b.Aperture>.95f&&!entryShot&&!inside){entryShot=true;yield return Shot("Shower-Entering-"+room);}if(b.WaterRunning){inside=true;frames.Add(w.Player.sprite.sprite);aligned &= Vector2.Distance(w.Player.Position,a.anchor.position)<.06f&&w.Player.sprite.enabled;if(frames.Count>=2&&!shot){shot=true;yield return Shot("Shower-Running-"+room);}}yield return null;}
  check(aligned,"Visible real actor inside shower "+room);check(!w.Busy&&inside&&open&&frames.Count>1&&positions.Count>5&&b.RenderedWaterFrames>3,"Actual enter/door/water/character animation "+room);check(g.world.State.hygiene>80&&Math.Abs(g.clock.SubDaySeconds-time-1200)<1,"One real20-minute hygiene effect "+room);check(w.CanControl&&w.Player.GetComponent<CircleCollider2D>().enabled&&Vector2.Distance(w.Player.Position,a.standUp.position)<.1f&&b.Aperture==0&&!b.WaterRunning&&a.seat.State==ActivityOccupancy.Free,"Exit alignment, closed booth, collision and input restored "+room);
  yield return Shot("Shower-Exited-"+room);w.Player.Warp(a.approach.position);yield return null;float hygiene=g.world.State.hygiene;time=g.clock.SubDaySeconds;check(w.Use(o),"Shower cancellation fixture "+room);end=Time.unscaledTime+10;while(!b.WaterRunning&&w.Busy&&Time.unscaledTime<end)yield return null;w.CancelAction();yield return Wait();check(g.world.State.hygiene==hygiene&&g.clock.SubDaySeconds==time&&!b.WaterRunning&&a.seat.State==ActivityOccupancy.Free&&w.CanControl,"Cancelled shower gives no effects and releases object "+room);
 }
}
}
#endif
