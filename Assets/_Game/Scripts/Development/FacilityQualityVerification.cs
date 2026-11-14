#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
namespace IanGame.Editor {
public static class FacilityQualityVerification {
 public static IEnumerator Wait(LifeWorldController w){float until=Time.unscaledTime+12;while(w.Busy&&Time.unscaledTime<until)yield return null;if(w.Busy)throw new Exception("Facility traversal timeout");yield return new WaitForSecondsRealtime(.25f);}
 public static IEnumerator Capture(LifeWorldController w,GameManager g,Action<bool,string> check,bool before){
  CommercialSliceVerification.SetSize(1280,720);for(int settle=0;settle<12;settle++)yield return null;g.growth.Restore(new ProgressSnapshot{age=19});g.clock.realtimeEnabled=false;Directory.CreateDirectory("Docs/FacilityStandardization/Screenshots");
  foreach(string zone in new[]{"workplace","school","shop","finance","realestate"}){
   g.clock.Restore(1,new TimeSnapshot{subDaySeconds=zone=="school"?9*3600+45*60:zone=="shop"?12*3600+5*60:10*3600+20*60});check(w.Travel(zone),"Facility capture route "+zone);yield return Wait(w);w.Player.Warp(w.Safe(w.CurrentZone,new Vector2(zone=="finance"||zone=="realestate"?0:6,-3)));w.follow.Snap();yield return new WaitForSecondsRealtime(2f);if(!before&&zone=="school"){float until=Time.unscaledTime+12;while(Time.unscaledTime<until&&w.GetComponentsInChildren<WorldFacilityActivityPresentation>().Count(p=>p.IsPresenting&&p.slot.GetComponent<WorldInteractable>().zone=="school")<3)yield return null;check(w.GetComponentsInChildren<WorldFacilityActivityPresentation>().Count(p=>p.IsPresenting&&p.slot.GetComponent<WorldInteractable>().zone=="school")>=3,"Classroom capture waits for actual occupied teacher and pupil furniture");}ScreenCapture.CaptureScreenshot("Docs/FacilityStandardization/Screenshots/"+(before?"Before-":"After-")+zone+".png");yield return null;
  }
 }
}
}
#endif
