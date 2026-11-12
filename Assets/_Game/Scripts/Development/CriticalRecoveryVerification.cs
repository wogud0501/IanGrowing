#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace IanGame.Editor {
public static class CriticalRecoveryVerification {
 static IEnumerator Wait(LifeWorldController w){float until=Time.unscaledTime+8;while(w.Busy&&Time.unscaledTime<until)yield return null;if(w.Busy)throw new Exception("Door traversal remained busy");yield return new WaitForSecondsRealtime(.2f);}
 public static IEnumerator Growth(LifeWorldController w,GameManager g,Action<bool,string> check){
  var initial=g.CaptureState();var e=g.economy;var stock=e.rules.stocks[0].StockId;
  foreach(int age in new[]{0,6,7,12,13,18,19,30}){
   var fixture=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(initial));fixture.progression.age=age;fixture.stats.money=10000000;fixture.stats.credit=100;g.RestoreState(fixture);g.clock.realtimeEnabled=false;
   check(GrowthAccess.Zone(g,"city",out _)==(age>=7),"Independent travel boundary "+age);
   check(GrowthAccess.Zone(g,"workplace",out _)==(age>=19),"Workplace boundary "+age);
   check(GrowthAccess.Scene(g,SceneId.StockMarket,out _)==(age>=19),"Stock scene boundary "+age);
   check(GrowthAccess.Zone(g,"realestate",out _)==(age>=19),"Agency boundary "+age);
   if(age<19){long cash=e.Cash;check(!e.Trade("underage-buy-"+age,stock,1,true)&&e.Cash==cash,"Direct stock execution rejected without cash change "+age);check(!e.BuyProperty("underage-home-"+age,"property-0"),"Direct property buy rejected "+age);check(!e.LeaseProperty("underage-lease-"+age,"property-0",HousingTenure.MonthlyRent),"Direct lease rejected "+age);check(!e.BorrowHousing("underage-loan-"+age,1000),"Direct housing loan rejected "+age);check(!g.phase23.Employment.Can(20,out _),"Employment execution blocked "+age);check(!w.Travel("workplace")&&!GameContext.Instance.transitions.NavigateForTesting(SceneId.Workplace),"World and debug scene route blocked "+age);}
   if(age<7){var front=w.objects.First(o=>o.id=="front-door");w.Player.Warp(front.Point);yield return new WaitForSecondsRealtime(.1f);check(!w.Use(front)&&!w.Travel("city"),"Shared keyboard/touch door action and direct travel reject infant "+age);var bed=w.objects.First(o=>o.id=="bed");w.Player.Warp(bed.Point);check(w.Use(bed),"Infant care context accessible "+age);while(w.hud.shell.Depth>0)w.hud.shell.Pop();check(g.CanPerformAction(17,out _),"Existing basic life learning can run at home "+age);}
   else if(age>=19){check(e.Trade("adult-buy-"+age,stock,1,true),"Adult stock buy remains available "+age);check(e.Trade("adult-sell-"+age,stock,1,false),"Adult stock sell remains available "+age);check(e.BuyProperty("adult-property-"+age,"property-0")&&e.SellProperty("adult-sale-"+age,"property-0"),"Adult property buy and sale preserved "+age);}
  }
  g.RestoreState(initial);g.clock.realtimeEnabled=false;var seeded=g.CaptureState();seeded.stats.money=12345;g.RestoreState(seeded);var invalid=g.CaptureState();invalid.world.zone="workplace";var missionJson=JsonUtility.ToJson(invalid.missions);g.RestoreState(invalid);g.clock.realtimeEnabled=false;check(g.world.State.zone=="home"&&g.economy.Cash==12345&&missionJson==JsonUtility.ToJson(g.CaptureState().missions),"Old newborn workplace save returns home preserving money and missions");
  var bedObject=w.objects.First(o=>o.id=="bed");w.Player.Warp(bedObject.Point);int priorAge=g.growth.Age;check(w.Begin(bedObject,3),"Newborn meal care executes without buying kitchen furniture");yield return Wait(w);check(g.growth.Age==priorAge,"Care time does not automatically advance age");check(g.missionCatalog.missions.Length==60,"Original 60 mission definitions retained");
  check(g.SaveAutomaticCheckpoint(out _)&&g.saves.TryReadAutomatic(out var saved,out _),"Growth-limited state saves in sandbox");
 }
 public static IEnumerator Facilities(LifeWorldController w,GameManager g,Action<bool,string> check,Action<GameObject,string> audit){
  g.growth.Restore(new ProgressSnapshot{age=19});g.clock.Restore(1,new TimeSnapshot{subDaySeconds=10*3600+20*60});g.clock.realtimeEnabled=false;
  var presentations=w.GetComponentsInChildren<WorldWorkstationPresentation>();check(presentations.Length==9,"Nine employee workstations retained across actual workplaces");
  var scheduler=w.GetComponent<WorldNPCScheduler>();
  foreach(var zone in new[]{"workplace","finance","realestate"}){
   check(w.Travel(zone),"Facility route accepted "+zone);yield return Wait(w);w.follow.Snap();yield return new WaitForSecondsRealtime(1.2f);
   var seats=presentations.Where(p=>p.GetComponent<WorldInteractable>().zone==zone).ToArray();check(seats.Length==(zone=="workplace"?5:2),"Purposeful facility capacity "+zone);
   var active=scheduler.actors.Where(a=>a.zone==zone).ToArray();check(active.Length>0&&active.Length<=seats.Length,"Current shift fits facility capacity "+zone);
   foreach(var a in active)foreach(var b in active.Where(b=>string.CompareOrdinal(a.npcId,b.npcId)<0))check(Vector2.Distance(a.transform.position,b.transform.position)>1,"No worker overlap "+a.npcId+" / "+b.npcId);
   var working=seats.Where(s=>s.IsWorking).ToArray();check(working.Length>0,"Authored frontal work presentation active "+zone);foreach(var p in working)check(p.slot.facing==Vector2.down&&!p.empty.enabled&&!p.actor.visual.enabled&&p.working.enabled,"One front-facing seated presentation without duplicate desk "+p.actor.npcId);
   var frames=new System.Collections.Generic.HashSet<Sprite>();float end=Time.unscaledTime+3;while(Time.unscaledTime<end){frames.Add(working[0].working.sprite);yield return null;}check(frames.Count>=2,"Actual hand/document frames advance "+zone);
   System.IO.Directory.CreateDirectory("Docs/CriticalRecovery/Screenshots");ScreenCapture.CaptureScreenshot("Docs/CriticalRecovery/Screenshots/"+zone+".png");yield return new WaitForSecondsRealtime(.25f);audit(w.hud.gameObject,"Critical facility HUD "+zone);
  }
  var property=UnityEngine.Object.FindFirstObjectByType<PropertyPresenter>();var mobile=w.hud.GetComponent<WorldMobileHUD>();mobile.simulateMobile=true;
  foreach(var size in new[]{new[]{904,2316},new[]{1840,2208},new[]{1280,720},new[]{1920,1080}}){CommercialSliceVerification.SetSize(size[0],size[1]);yield return new WaitForSecondsRealtime(.25f);mobile.Apply(true);property.Open();yield return new WaitForSecondsRealtime(.5f);audit(property.panel,"Property viewport "+size[0]);var corners=new Vector3[4];property.scroll.viewport.GetWorldCorners(corners);check(property.selections.Count(b=>b.gameObject.activeSelf)==6,"All six listings available "+size[0]);foreach(var b in property.selections.Where(b=>b.gameObject.activeSelf)){var bc=new Vector3[4];((RectTransform)b.transform).GetWorldCorners(bc);check(bc.All(p=>p.x>=corners[0].x-1&&p.x<=corners[2].x+1),"Listing fits scroll width "+b.name+" / "+size[0]);}ScreenCapture.CaptureScreenshot("Docs/CriticalRecovery/Screenshots/Property-"+size[0]+".png");yield return new WaitForSecondsRealtime(.2f);while(w.hud.shell.Depth>0)w.hud.shell.Pop();}
  mobile.simulateMobile=false;CommercialSliceVerification.SetSize(1280,720);mobile.Apply(true);
  // Visible staff leaves own desk via existing pathfinding, reaches a real door.
  check(w.Travel("workplace"),"Observe office departure");yield return Wait(w);var colleague=scheduler.actors.First(a=>a.npcId=="colleague");Vector2 start=colleague.transform.position;
  g.clock.Restore(1,new TimeSnapshot{subDaySeconds=18*3600+60});float began=Time.unscaledTime,deadline=Time.unscaledTime+30;bool walked=false,cleared=false;while(Time.unscaledTime<deadline&&colleague.zone=="workplace"){walked|=colleague.GetComponent<WorldActorAnimation>().Motion==ActorMotion.Walk;if(!cleared&&Time.unscaledTime-began>5){check(Vector2.Distance(w.Player.Position,g.world.rules.Zone("workplace").origin+g.world.rules.Zone("workplace").spawn)<.1f,"Waiting staff does not push player occupying doorway");w.Player.Warp(g.world.rules.Zone("workplace").origin+new Vector2(6,-2));cleared=true;}yield return null;}
  check(walked&&Vector2.Distance(start,colleague.transform.position)>1,"Employee physically leaves assigned workstation");check(colleague.zone!="workplace","Employee passes office exit and resumes daily route");
  var saved=g.CaptureState();SaveData loaded=null;check(g.SaveAutomaticCheckpoint(out _)&&g.saves.TryReadAutomatic(out loaded,out _),"Actual facility and residents save in isolated disk slot");g.RestoreState(loaded);g.clock.realtimeEnabled=false;check(g.world.State.zone==saved.world.zone&&g.world.State.residents.Count==saved.world.residents.Count,"Facility reload preserves NPC records without duplicate spawn");
 }
 public static IEnumerator Capture(LifeWorldController w,GameManager g){g.growth.Restore(new ProgressSnapshot{age=19});g.clock.Restore(1,new TimeSnapshot{subDaySeconds=10*3600+20*60});g.clock.realtimeEnabled=false;CommercialSliceVerification.SetSize(1280,720);foreach(var zone in new[]{"workplace","finance","realestate"}){w.Travel(zone);yield return Wait(w);yield return new WaitForSecondsRealtime(.8f);ScreenCapture.CaptureScreenshot("Docs/CriticalRecovery/Screenshots/"+zone+".png");yield return new WaitForSecondsRealtime(.2f);}}
 public static IEnumerator Cycles(LifeWorldController w,GameManager g,Action<bool,string> check){check(!g.economy.PaySalary("infant-salary",100,"office"),"Direct newborn salary payment rejected");var entry=w.objects.First(o=>o.id=="front-door").GetComponent<WorldDoor>();check(!entry.Acquire("player",12,out _),"Direct door acquisition cannot bypass infant travel gate");g.growth.Restore(new ProgressSnapshot{age=19});w.Travel("city");yield return Wait(w);
  foreach(string id in new[]{"front-door","bath-door","work-exit"}){var d=w.objects.First(o=>o.id==id).GetComponent<WorldDoor>();d.enabled=false;var root=d.transform.position;var frame=d.frame.transform.position;var hinge=d.leaf.transform.position;try{foreach(float dt in new[]{1f/30,1f/60,.11f})for(int cycle=0;cycle<20;cycle++){d.ResetClosed();d.Acquire("player",12,out _);d.Acquire("colleague",12,out _);for(int n=0;n<100&&d.State!=DoorState.Open;n++)d.AdvanceAnimation(dt);check(d.State==DoorState.Open&&!d.closedCollider.enabled&&d.Users==2,"Shared door opens "+id+" / "+dt+" / "+cycle);d.Release("player");d.AdvanceAnimation(dt);check(d.State==DoorState.Open&&d.Users==1,"NPC lease retains open door "+id);d.Release("colleague");yield return new WaitForSecondsRealtime(.14f);for(int n=0;n<100&&d.State!=DoorState.Closed;n++)d.AdvanceAnimation(dt);check(d.State==DoorState.Closed&&d.closedCollider.enabled&&d.transform.position==root&&d.frame.transform.position==frame&&d.leaf.transform.position==hinge,"Exact closed pose, fixed root/frame/hinge "+id);}}finally{d.ResetClosed();d.enabled=true;}}
 }
 public static IEnumerator Doors(LifeWorldController w,GameManager g,Action<bool,string> check){
  g.growth.Restore(new ProgressSnapshot{age=19});g.clock.Restore(1,new TimeSnapshot{subDaySeconds=12*3600});g.clock.realtimeEnabled=false;
  var exit=w.objects.First(o=>o.id=="shop-exit").GetComponent<WorldDoor>();var entrance=w.objects.First(o=>o.id=="shop-front").GetComponent<WorldDoor>();check(exit.CanEnter("player",23,out _)&&!entrance.CanEnter("player",23,out _),"Closed shop rejects entrance but allows egress");
  foreach(var id in new[]{"front-door","house-entrance","bath-door","bath-exit","office-front","work-exit","finance-front","finance-exit","realestate-front","realestate-exit","school-front","school-exit","shop-front","shop-exit","office-bath-front","office-bath-exit"}){
   var o=w.objects.FirstOrDefault(x=>x.id==id);if(o==null){check(false,"Missing expected portal: "+id);yield break;}var door=o.GetComponent<WorldDoor>();check(door!=null,"Animated door connected: "+id);
   for(int side=0;side<2;side++){
    check(w.Travel(o.zone),"Source route accepted: "+id);yield return Wait(w);door.ResetClosed();Vector3 root=door.transform.position,frame=door.frame.transform.position,leaf=door.leaf.transform.position;
    var start=side==0?o.Point:door.Plane+door.normal*.5f;w.Player.Warp(start);w.follow.Snap();yield return new WaitForSecondsRealtime(.15f);
    check(w.Use(o),"Interaction accepted: "+id+" side "+side);check(!w.Use(o),"Repeated input rejected: "+id);if(id=="front-door"&&side==0){yield return null;g.Pause();yield return new WaitForSecondsRealtime(1.2f);check(w.Busy,"Pause preserves pending door traversal");g.Resume();}yield return Wait(w);
    check(g.world.State.zone==o.targetZone,"Passage committed: "+id+" side "+side+" (actual "+g.world.State.zone+")");check(w.CanControl,"Input released: "+id);check(root==door.transform.position&&frame==door.frame.transform.position&&leaf==door.leaf.transform.position,"Door transforms fixed: "+id);
   }
  }
 }
}
}
#endif
