#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace IanGame.Editor {
public static class QACenterVerification {
 public static IEnumerator Run(LifeWorldController world,GameManager game,Action<bool,string> check,Action<GameObject,string> audit,bool failedOnly=false){
  var before=game.CaptureState();var slot=game.saves.BasePath;var qa=DeveloperQACenter.Ensure();var keyboard=InputSystem.AddDevice<Keyboard>("QA F10 verification");
  try{InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F10));InputSystem.Update();check(qa.HandleShortcut(),"F10 Input System binding executes the real toggle path");InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();}finally{InputSystem.RemoveDevice(keyboard);}yield return null;
  check(qa.Visible&&qa.Sandbox.Active&&game.saves.BasePath!=slot&&System.IO.Path.GetFileName(game.saves.BasePath).StartsWith("qa-"),"QA overlay opens and switches the actual SaveManager to dedicated channel");check(!world.CanControl,"QA overlay captures world input");
  qa.Commands.SetCash(43210);qa.Commands.SetAge(20);qa.Commands.SetStat(CoreStat.Health,42);qa.Commands.SetStat(CoreStat.Mental,25);check(game.economy.Cash==43210&&game.growth.Age==20&&game.stats.Health==42&&game.stats.Stress==75,"Actual QA cash, age, health and stress commands work");check(qa.Commands.Save()&&qa.Sandbox.VerifyFiles(),"Manual QA save never modifies entry save files");qa.ResetTestData();check(game.economy.Cash==before.stats.money&&game.growth.Age==before.progression.age,"Reset affects QA model only and restores entry state");
  if(failedOnly){qa.Reporter.Import(System.IO.File.ReadAllText("Docs/DeveloperQA/REGISTERED_SCENARIOS_RESULTS.json"));qa.Run(qa.Registry.Scenarios.Where(s=>qa.Reporter.Get(s).status==QAStatus.FAIL||qa.Reporter.Get(s).status==QAStatus.NOT_RUN));}else qa.RunAll();float end=Time.unscaledTime+180;while(qa.Runner.Running&&Time.unscaledTime<end)yield return null;check(!qa.Runner.Running,"All registered QA scenarios finish within bounded run");var failures=qa.Reporter.Results.Where(r=>r.status==QAStatus.FAIL).Select(r=>r.id+": "+r.reason).ToArray();check(qa.Reporter.Results.Any(r=>r.status==QAStatus.PASS)&&qa.Reporter.Results.Any(r=>r.status==QAStatus.NOT_IMPLEMENTED)&&qa.Reporter.Results.Any(r=>r.status==QAStatus.MANUAL),"Runner reports tested pass, unimplemented and manual outcomes separately");check(qa.Reporter.Results.Where(r=>r.status==QAStatus.PASS).All(r=>r.checks>0),"Every PASS contains executed assertions");check(qa.Sandbox.VerifyFiles(),"All scenarios preserve sandbox entry files");
  qa.Open();foreach(var size in new[]{new[]{1280,720},new[]{1920,1080},new[]{904,2316}}){CommercialSliceVerification.SetSize(size[0],size[1]);yield return new WaitForSecondsRealtime(.4f);var canvas=qa.GetComponentInChildren<Canvas>(true);audit(canvas.gameObject,"QA Center "+size[0]);System.IO.Directory.CreateDirectory("Docs/DeveloperQA/Screenshots");ScreenCapture.CaptureScreenshot("Docs/DeveloperQA/Screenshots/QA-"+size[0]+".png");yield return null;}CommercialSliceVerification.SetSize(1280,720);
  yield return qa.CloseAndRestore();check(!qa.Sandbox.Active&&!qa.Visible&&game.saves.BasePath==slot,"Close restores original channel and hides overlay");var after=game.CaptureState();check(JsonUtility.ToJson(before.stats)==JsonUtility.ToJson(after.stats)&&JsonUtility.ToJson(before.economy)==JsonUtility.ToJson(after.economy)&&JsonUtility.ToJson(before.missions)==JsonUtility.ToJson(after.missions)&&JsonUtility.ToJson(before.phase23)==JsonUtility.ToJson(after.phase23)&&JsonUtility.ToJson(before.progression)==JsonUtility.ToJson(after.progression),"Close restores cash, debt, stocks, property, missions, inventory, career and age");check(qa.Sandbox.LastFilesUnchanged,"QA close confirms source files remain byte-identical");check(failures.Length==0,"Registered automated scenarios have no FAIL: "+string.Join(" | ",failures));
 }
}
}
#endif
