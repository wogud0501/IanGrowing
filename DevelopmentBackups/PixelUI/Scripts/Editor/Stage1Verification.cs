using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;using UnityEditor;
namespace IanGame.Editor {
public static class Stage1Verification {

 public static string[] Run(){
 if(!Application.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
 var g=UnityEngine.Object.FindAnyObjectByType<GameManager>();if(g==null)throw new InvalidOperationException("Missing scene GameManager.");
 if(!g.LifeMode)return RunLegacy();
 var initial=g.balance.initialLife;var cfg=g.balance.survival;bool realtime=g.clock.realtimeEnabled,lifecycle=g.autoSaveOnLifecycle;bool paused=g.State==GameState.Paused;
 var slot=typeof(SaveManager).GetField("fileName",BindingFlags.Instance|BindingFlags.NonPublic);string original=(string)slot.GetValue(g.saves);string temp="phase1-compat-"+Guid.NewGuid().ToString("N")+".json";
 var snapshot=new SaveData{day=g.clock.Day,stats=g.stats.Capture(),progression=g.growth.Capture(),economy=g.economy.Capture(),survival=g.survival.Capture(),time=g.clock.Capture(),gameOver=g.State==GameState.GameOver,missions=g.missions.Capture(),runId=(string)typeof(GameManager).GetField("runId",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(g),settledDay=g.clock.Day,lastSeenUtcTicks=DateTime.UtcNow.Ticks};
 slot.SetValue(g.saves,temp);g.autoSaveOnLifecycle=false;
 try{g.balance.initialLife=null;g.balance.survival=null;g.NewGame();return RunLegacy();}
 finally{g.balance.initialLife=initial;g.balance.survival=cfg;g.NewGame();g.clock.realtimeEnabled=false;FileRestore(g,snapshot);if(paused)g.Pause();g.clock.realtimeEnabled=realtime;g.autoSaveOnLifecycle=lifecycle;slot.SetValue(g.saves,original);}
 }
 static void FileRestore(GameManager game,SaveData snapshot){System.IO.File.WriteAllText(game.saves.SavePath,JsonUtility.ToJson(snapshot));game.Load();}

 [MenuItem("Ian/Verify Stage 1 Core (Play Mode)")]
 public static void RunMenu(){foreach(var item in Run())Debug.Log(item);}
 static string[] RunLegacy(){
 if(!Application.isPlaying)throw new InvalidOperationException("Enter MainGame2D Play Mode and wait for the first rendered frame.");
 var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();var ui=UnityEngine.Object.FindAnyObjectByType<UIManager>();var character=UnityEngine.Object.FindAnyObjectByType<Character2DPresentation>();
 if(game==null||ui==null||game.State==GameState.Starting)throw new InvalidOperationException("The scene has not finished starting.");
 var field=typeof(SaveManager).GetField("fileName",BindingFlags.NonPublic|BindingFlags.Instance);string originalSlot=(string)field.GetValue(game.saves);
 // Always isolate automated tests from the player's active slot.
 string suiteSlot="stage1-suite-"+Guid.NewGuid().ToString("N")+".json";field.SetValue(game.saves,suiteSlot);
 var initial=game.stats.Capture();int initialDay=game.clock.Day;string balanceJson=JsonUtility.ToJson(game.balance);var originalCatalog=game.lifeEvents.catalog;bool oldAuto=game.autoSaveAfterAction;var random=UnityEngine.Random.state;var results=new List<string>();LifeEventCatalog temporary=null;
 Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL: "+name);results.Add("PASS: "+name);};
 Action<Button> click=button=>{var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);};
 var s=game.stats;var b=game.balance;
 try{
 game.autoSaveAfterAction=false;b.eventChance=0;b.restMoney=0;b.workDays=b.restDays=b.playDays=1;
 game.NewGame();check(game.State==GameState.Playing&&game.clock.Day==1,"Starting to Playing / new game");
 click(ui.workButton);check(game.clock.Day==2&&s.Money==68&&s.Stress==35&&s.Energy==52,"Work button");
 click(ui.restButton);check(game.clock.Day==3&&s.Money==68&&s.Stress==23&&s.Energy==80,"Rest button");
 click(ui.playButton);check(game.clock.Day==4&&s.Money==56&&s.Happiness==70&&s.Energy==72,"Play button");
 check(ui.header.text.Contains("004")&&ui.header.text.Contains("56")&&ui.happinessLabel.text.Contains("70"),"Reactive HUD");
 game.economy.Earn(500);check(s.Money==556,"Money exceeds 100");
 int h=0,stress=0,energy=0,money=0;Action<int> hh=v=>h++,ss=v=>stress++,ee=v=>energy++,mm=v=>money++;s.HappinessChanged+=hh;s.StressChanged+=ss;s.EnergyChanged+=ee;s.MoneyChanged+=mm;
 s.Restore(new StatSnapshot{happiness=40,stress=30,energy=60,money=200});s.Restore(s.Capture());
 s.HappinessChanged-=hh;s.StressChanged-=ss;s.EnergyChanged-=ee;s.MoneyChanged-=mm;check(h==1&&stress==1&&energy==1&&money==1,"Per-stat events only when changed");
 s.Apply(int.MaxValue,int.MinValue,int.MaxValue,int.MaxValue);check(s.Happiness==100&&s.Stress==0&&s.Energy==100&&s.Money==int.MaxValue,"Clamps and integer overflow protection");
 s.Restore(new StatSnapshot{happiness=50,stress=20,energy=70,money=3});int d=game.clock.Day;check(!game.TryAction(2)&&game.clock.Day==d&&s.Money==3,"Unaffordable action is atomic");
 b.restMoney=-4;check(!game.TryAction(1)&&game.clock.Day==d&&s.Money==3,"Paid rest also requires funds");b.restMoney=0;
 s.Restore(new StatSnapshot{happiness=50,stress=20,energy=0,money=0});check(!game.TryAction(0)&&!game.TryAction(2),"Energy/funds guards");click(ui.restButton);check(s.Energy==28&&s.Money==0,"Free default rest prevents soft lock");click(ui.workButton);check(s.Money==18&&s.Energy==10,"Recovery leads back to work");
 d=game.clock.Day;click(ui.pauseButton);check(game.State==GameState.Paused&&!ui.workButton.interactable&&!ui.restButton.interactable&&!ui.playButton.interactable,"Pause gates UI");
 check(!game.TryAction(0)&&!game.clock.AdvanceDays(1)&&game.clock.Day==d,"Paused action and time blocked");click(ui.pauseButton);check(game.State==GameState.Playing&&game.clock.IsRunning,"Resume button");
 s.ResetStats(b);int calls=0;Action reentry=()=>{if(game.State==GameState.Resolving){calls++;game.Work();}};s.Changed+=reentry;d=game.clock.Day;game.Work();s.Changed-=reentry;check(calls>0&&game.clock.Day==d+1&&s.Money==68,"Reentrant actions blocked");
 b.workDays=3;b.eventChance=1;s.ResetStats(b);int ticks=0,events=0;Action<int> tick=day=>ticks++;Action<LifeEvent> occurred=e=>events++;game.clock.DayAdvanced+=tick;game.lifeEvents.EventApplied+=occurred;d=game.clock.Day;game.Work();game.clock.DayAdvanced-=tick;game.lifeEvents.EventApplied-=occurred;
 check(game.clock.Day==d+3&&ticks==3&&events==3,"Configurable action duration and daily events");b.workDays=1;b.eventChance=0;
 int restoredTicks=0;Action<int> onTick=day=>restoredTicks++;game.clock.DayAdvanced+=onTick;game.clock.SetDay(15);game.clock.DayAdvanced-=onTick;check(restoredTicks==0,"Restoring date does not simulate days");
 temporary=ScriptableObject.CreateInstance<LifeEventCatalog>();temporary.events=new[]{new LifeEvent{title="off",weight=0},new LifeEvent{title="A",weight=1},new LifeEvent{title="B",weight=3}};game.lifeEvents.catalog=temporary;int first=0,second=0;for(int i=0;i<1000;i++){var e=game.lifeEvents.Select((i+.5f)/1000);if(e.title=="A")first++;else if(e.title=="B")second++;}check(first==250&&second==750,"Weighted 1:3 selection and zero exclusion");
 check(game.lifeEvents.TryEvent(s,0)==null&&game.lifeEvents.TryEvent(s,1)!=null,"Event probability endpoints");game.lifeEvents.catalog=originalCatalog;
 check(originalCatalog!=null&&originalCatalog.events.Length>=10&&originalCatalog.events.All(e=>!string.IsNullOrEmpty(e.description)),"At least ten data-driven sample events");
 s.Restore(new StatSnapshot{happiness=50,stress=85,energy=18,money=100});b.enableGameOver=true;b.gameOverStress=100;b.gameOverEnergy=0;game.Work();
 check(game.State==GameState.GameOver&&!game.clock.IsRunning&&!ui.workButton.interactable&&!ui.pauseButton.interactable,"Burnout game over and UI");
 d=game.clock.Day;check(!game.TryAction(1)&&game.clock.Day==d,"Game over blocks actions");click(ui.newGameButton);check(game.State==GameState.Playing&&game.clock.Day==1&&s.Money==b.initialMoney,"New life button");
 s.Restore(new StatSnapshot{happiness=73,stress=18,energy=42,money=5432});game.clock.SetDay(19);click(ui.saveButton);s.Apply(-30,20,-10,-4000);game.clock.SetDay(20);click(ui.loadButton);
 check(s.Money==5432&&s.Happiness==73&&game.clock.Day==19&&game.State==GameState.Playing,"Existing save/load button flow");
 game.EndGame();game.Save();game.Load();check(game.State==GameState.GameOver,"Game over persists");
 var v1=new SaveData{version=1,day=7,stats=new StatSnapshot{happiness=50,stress=20,energy=70,money=99}};System.IO.File.WriteAllText(game.saves.SavePath,JsonUtility.ToJson(v1));game.Load();check(game.State==GameState.Playing&&game.clock.Day==7&&s.Money==99,"V1 save compatibility");
 System.IO.File.WriteAllText(game.saves.SavePath,"{broken");game.Load();check(game.saves.LastLoadStatus==SaveLoadStatus.Corrupt&&game.clock.Day==7&&s.Money==99&&game.State==GameState.Playing&&System.IO.File.Exists(game.saves.LastRecoveryCopy),"Corrupt save preserved without resetting current life");
 check(System.IO.File.ReadAllText(game.saves.LastRecoveryCopy)=="{broken","Corrupt original bytes preserved");game.NewGame();
 var future=new SaveData{version=99,day=2,stats=s.Capture()};string futureJson=JsonUtility.ToJson(future);System.IO.File.WriteAllText(game.saves.SavePath,futureJson);s.Apply(0,0,0,38);game.clock.SetDay(9);game.Load();check(game.saves.LastLoadStatus==SaveLoadStatus.UnsupportedVersion&&game.clock.Day==9&&s.Money==88&&System.IO.File.ReadAllText(game.saves.SavePath)==futureJson,"Unsupported future version preserved");
 game.NewGame();game.autoSaveAfterAction=true;game.Work();SaveData stored;string message;check(game.saves.TryReadAutomatic(out stored,out message)&&stored.day==2&&stored.stats.money==68,"Automatic save after successful action");game.autoSaveAfterAction=false;
 field.SetValue(game.saves,"stage1-missing-"+Guid.NewGuid().ToString("N")+".json");d=game.clock.Day;game.Load();check(game.saves.LastLoadStatus==SaveLoadStatus.Missing&&game.clock.Day==d&&s.Money==68,"Missing file preserves active session");field.SetValue(game.saves,suiteSlot);
 Canvas.ForceUpdateCanvases();var rect=ui.workButton.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);check(hits.Any(hit=>hit.gameObject==ui.workButton.gameObject),"Work button reached by EventSystem raycast");
 ui.Bind(game);ui.Bind(game);s.ResetStats(b);d=game.clock.Day;click(ui.workButton);check(game.clock.Day==d+1&&s.Money==68,"Repeated HUD binding does not duplicate actions");
 ui.gameObject.SetActive(false);ui.gameObject.SetActive(true);s.ResetStats(b);d=game.clock.Day;click(ui.workButton);check(game.clock.Day==d+1&&s.Money==68,"HUD re-enable does not duplicate listeners");

 var identity=character.visuals.identity;check(identity!=null&&character.spriteRenderer.sprite==identity.baseSprite&&character.visuals.idle.Concat(character.visuals.work).Concat(character.visuals.rest).Concat(character.visuals.play).All(sprite=>sprite==identity.baseSprite),"Official character retained in all actions");
 check(Camera.main!=null&&Camera.main.orthographic,"2D orthographic camera");
 var archive=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().First(g=>g.name.StartsWith("ARCHIVE"));check(!archive.activeSelf,"Archived 3D remains inactive");
 var hudAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/GameHUD.prefab").GetComponent<UIManager>();check(hudAsset.game==null&&hudAsset.pauseButton!=null&&hudAsset.newGameButton!=null,"HUD prefab is independent");
 return results.ToArray();
 }finally{
 field.SetValue(game.saves,originalSlot);game.lifeEvents.catalog=originalCatalog;JsonUtility.FromJsonOverwrite(balanceJson,b);game.autoSaveAfterAction=false;game.NewGame();s.Restore(initial);game.clock.SetDay(initialDay);game.autoSaveAfterAction=oldAuto;field.SetValue(game.saves,originalSlot);UnityEngine.Random.state=random;if(temporary!=null)UnityEngine.Object.DestroyImmediate(temporary);
 }
 }
}
}