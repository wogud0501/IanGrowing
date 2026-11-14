#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace IanGame.Editor {
public static class PrePhase28WalkingVerification {
 static Rect Rect(RectTransform r){var p=new Vector3[4];r.GetWorldCorners(p);return new Rect(p[0].x,p[0].y,p[2].x-p[0].x,p[2].y-p[0].y);}
 public static IEnumerator Run(LifeWorldController w,GameManager g,Action<bool,string> check,Action<GameObject,string> audit){
  check(!g.growth.WalkingUnlocked&&!g.growth.IndependentOutdoorUnlocked,"Age0 neither walking nor independent outdoors");var bed=w.objects.First(o=>o.id=="bed");int notices=0;Action listener=()=>notices++;g.growth.WalkingIntroduced+=listener;float scale=g.world.rules.activityAnimationScale;g.world.rules.activityAnimationScale=.02f;
  try{
   check(w.Begin(bed,4),"Actual newborn drink care");while(w.Busy)yield return null;check(g.missions.Claim("M001"),"Actual first mission reward");check(w.Begin(bed,3),"Actual newborn meal care");while(w.Busy)yield return null;
   check(g.growth.Age==1&&g.growth.WalkingUnlocked&&!g.growth.IndependentOutdoorUnlocked,"M002 actual growth enables walking1 only");check(notices==1&&g.growth.WalkingIntroductionSeen,"First steps notice delivered once");check(w.CanControl&&w.hud.shell.Depth==0,"Notice does not block controls or require quest acknowledgement");audit(w.hud.gameObject,"First steps Korean/layout");ScreenCapture.CaptureScreenshot("Docs/PrePhase28/Screenshots/FirstSteps.png");yield return null;
   string notice=w.hud.feedback.text;var mobile=w.hud.GetComponent<WorldMobileHUD>();mobile.simulateMobile=true;
   foreach(var size in new[]{new[]{1280,720},new[]{1920,1080},new[]{904,2316},new[]{1840,2208}}){mobile.simulateMobile=size[0]<size[1];CommercialSliceVerification.SetSize(size[0],size[1]);yield return new WaitForSecondsRealtime(.2f);mobile.Apply(true);w.hud.Notify(notice,2);yield return null;audit(w.hud.gameObject,"First steps responsive "+size[0]);var background=w.hud.feedback.transform.parent.GetComponent<UnityEngine.UI.Image>();check(background!=null&&background.color.a>.95f&&!background.raycastTarget&&!w.hud.feedback.isTextOverflowing,"Readable nonblocking notification "+size[0]);if(mobile.IsMobile){var card=Rect((RectTransform)background.transform);foreach(var control in new[]{mobile.interact.transform,mobile.inventory.transform,mobile.menu.transform,mobile.run.transform})check(!card.Overlaps(Rect((RectTransform)control)),"Notice clears touch target "+control.name+" / "+size[0]);}ScreenCapture.CaptureScreenshot("Docs/PrePhase28/Screenshots/FirstSteps-"+size[0]+".png");yield return null;}
   mobile.simulateMobile=false;CommercialSliceVerification.SetSize(1280,720);mobile.Apply(true);
   w.Player.Warp(w.CurrentZone.origin+new Vector2(0,-2));var start=w.Player.Position;w.Player.BeginTraversal(start+Vector2.right*.6f);yield return new WaitForSecondsRealtime(.4f);check(Vector2.Distance(start,w.Player.Position)>.4f,"Age1 can physically walk while notice is displayed");w.Player.CancelTraversal();
   SaveData saved=null;check(g.SaveAutomaticCheckpoint(out _)&&g.saves.TryReadAutomatic(out saved,out _),"Walking flag actual disk save/read");g.RestoreState(saved);g.clock.realtimeEnabled=false;yield return null;check(g.growth.WalkingIntroductionSeen&&notices==1&&w.Player.MovementAllowed,"Reload retains walking and does not repeat notice");
   foreach(int age in new[]{0,1,2,6,7}){var p=g.growth.Capture();p.age=age;g.growth.Restore(p);check(g.growth.WalkingUnlocked==(age>=1)&&w.Player.MovementAllowed==(age>=1),"Walking execution boundary "+age);check(GrowthAccess.Zone(g,"city",out _)==(age>=7),"Independent outdoor boundary "+age);check(!g.growth.TryMarkWalkingIntroduction(),"No repeated first-steps grant "+age);}
   check(w.Travel("city"),"Age7 actual independent travel allowed");while(w.Busy)yield return null;check(w.CurrentZone.id=="city"&&w.CanControl,"Age7 city arrival and controls");
  }finally{g.growth.WalkingIntroduced-=listener;g.world.rules.activityAnimationScale=scale;}
 }
}
}
#endif
