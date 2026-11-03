using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using UnityEngine;using UnityEngine.UI;using UnityEditor;
namespace IanGame.Editor {
public static class Phase2Verification {
 [MenuItem("Ian/Verify Phase 2.1 (Play Mode)")]public static void RunMenu(){foreach(var value in Run())Debug.Log(value);}
 public static string[] Run(){
 if(!Application.isPlaying)throw new InvalidOperationException("Open Game View, enter Play, wait for the rendered first frame.");
 var g=UnityEngine.Object.FindAnyObjectByType<GameManager>();var ui=UnityEngine.Object.FindAnyObjectByType<UIManager>();
 if(g==null||!g.LifeMode||g.State==GameState.Starting)throw new InvalidOperationException("Phase 2.1 scene not ready.");
 var slot=typeof(SaveManager).GetField("fileName",BindingFlags.Instance|BindingFlags.NonPublic);string originalSlot=(string)slot.GetValue(g.saves);
 string suite="phase2-suite-"+Guid.NewGuid().ToString("N")+".json";slot.SetValue(g.saves,suite);
 var balance=g.balance;var cfg=balance.survival;string oldCfg=JsonUtility.ToJson(cfg),oldBalance=JsonUtility.ToJson(balance);
 bool auto=g.autoSaveAfterAction,lifecycle=g.autoSaveOnLifecycle,realtime=g.clock.realtimeEnabled;
 var start=new SaveData{day=g.clock.Day,stats=g.stats.Capture(),progression=g.growth.Capture(),economy=g.economy.Capture(),survival=g.survival.Capture(),time=g.clock.Capture(),gameOver=g.State==GameState.GameOver,missions=g.missions.Capture(),runId="restore-test-session",settledDay=g.clock.Day,lastSeenUtcTicks=DateTime.UtcNow.Ticks};
 var random=UnityEngine.Random.state;var results=new List<string>();
 Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL: "+name);results.Add("PASS: "+name);};
 Action reset=()=>{g.NewGame();g.clock.realtimeEnabled=false;balance.eventChance=0;};
 var s=g.stats;string birth=null;Action<string> log=text=>birth=text;
 g.autoSaveAfterAction=false;g.autoSaveOnLifecycle=false;
 try{
 g.Logged+=log;reset();g.Logged-=log;
 check(g.growth.Age==0&&s.Money==0&&g.economy.Debt==1000000&&g.economy.NetWorth==-1000000&&g.growth.SocialStanding=="없음","Newborn age/cash/debt/standing/net worth");
 check(birth!=null&&birth.Contains("태어났습니다"),"Birth system message");
 check(!g.CanPerformAction(0,out birth)&&!ui.workButton.interactable,"Employment locked before adulthood");
 int before=g.growth.Age;g.clock.AdvanceDays(3);check(g.growth.Age==before,"Date alone never increases age");
 check(LifeProgression.StageAtAge(6)==LifeStage.Infancy&&LifeProgression.StageAtAge(7)==LifeStage.Childhood&&LifeProgression.StageAtAge(12)==LifeStage.Childhood&&LifeProgression.StageAtAge(13)==LifeStage.Adolescence&&LifeProgression.StageAtAge(18)==LifeStage.Adolescence&&LifeProgression.StageAtAge(19)==LifeStage.YoungAdult&&LifeProgression.StageAtAge(29)==LifeStage.YoungAdult&&LifeProgression.StageAtAge(30)==LifeStage.MiddleAge&&LifeProgression.StageAtAge(59)==LifeStage.MiddleAge&&LifeProgression.StageAtAge(60)==LifeStage.OldAge,"All life-stage boundaries");
 reset();g.clock.AdvanceSimulation(86400);check(s.Hunger==92&&s.Thirst==88&&g.clock.Day==2,"Shared time decays hunger/thirst");
 reset();s.ApplyCore(new StatDelta{hunger=-80,thirst=-80,health=-80,mental=-80});int h=s.Hunger;ui.mealButton.onClick.Invoke();check(s.Hunger>h&&s.Money==0,"Meal button and free recovery");
 int t=s.Thirst;ui.drinkButton.onClick.Invoke();check(s.Thirst>t&&s.Money==0,"Drink button");
 int health=s.Health,mental=s.Mental;ui.sleepButton.onClick.Invoke();check(s.Health>health&&s.Mental>mental,"Sleep restores health and mental");
 s.ApplyCore(new StatDelta{health=-100,mental=-100});check(g.statuses.ActiveCount>0,"Fictional statuses triggered by data conditions");ui.hospitalButton.onClick.Invoke();check(s.Health>=40&&s.Mental>=40&&g.statuses.ActiveCount==0&&s.Money==0,"Hospital recovery needs no paid ticket");
 reset();cfg.dangerGraceHours=2;s.ApplyCore(new StatDelta{hunger=-100});g.clock.AdvanceSimulation(3600);check(g.State==GameState.Playing&&g.survival.InDanger&&!g.survival.Failed,"Zero need has grace period");
 ui.mealButton.onClick.Invoke();check(!g.survival.InDanger&&g.State==GameState.Playing,"Recovery clears danger before deadline");
 reset();s.ApplyCore(new StatDelta{thirst=-100});g.clock.AdvanceSimulation(7201);check(g.State==GameState.GameOver&&g.survival.Failed&&!g.CanPerformAction(4,out birth),"Sustained danger causes survival failure");cfg.dangerGraceHours=48;
 reset();g.growth.TryAdvanceAgeForMission("test-adult",19);s.ApplyCore(new StatDelta{health=-80});g.Work();check(s.Money==9&&s.Health==2,"Low-health positive yield reduced");
 s.ApplyCore(new StatDelta{mental=-100});check(g.survival.EventChanceMultiplier>1&&g.statuses.ActiveCount>0,"Low mental changes event chance and fictional state");
 check(g.growth.Experience==0&&g.growth.Level==1,"Age, experience and level remain separate");
 check(!g.growth.TryAdvanceAgeForMission("test-adult",30)&&g.growth.Age==19,"Duplicate mission-growth trigger rejected");
 check(!g.growth.MeetsCareerRequirements(s,20,20),"Knowledge/social requirements reject ineligible career");
 s.ApplyCore(new StatDelta{knowledge=30,social=30});check(g.growth.MeetsCareerRequirements(s,20,20),"Knowledge/social extension point");
 check(g.growth.MeetsLoanRequirements(s,50),"Credit/age eligibility extension point");
 reset();check(g.economy.TryTransaction("income",TransactionKind.Income,100,"test")&&!g.economy.TryTransaction("income",TransactionKind.Income,100,"duplicate")&&s.Money==100,"Stable transaction id blocks duplicate grant");
 check(!g.economy.TryTransaction("unaffordable",TransactionKind.Expense,101,"test")&&s.Money==100,"Explicit consumption rejects negative cash");
 check(g.economy.TryTransaction("charge",TransactionKind.Expense,25,"test")&&!g.economy.TryTransaction("charge",TransactionKind.Expense,25,"duplicate")&&s.Money==75,"Duplicate billing blocked");
 check(g.economy.TryTransaction("debt",TransactionKind.DebtIncrease,500,"test")&&s.Money==75&&g.economy.Debt==1000500,"Debt increase does not spend or grant cash");
 check(g.economy.NetWorth==75-1000500,"Net worth assets minus debt");
 s.ApplyCore(new StatDelta{hunger=-100,health=-90,mental=-90});g.clock.AdvanceSimulation(123.5);g.growth.TryAdvanceAgeForMission("test-save-stage",7);g.Save();
 SaveData saved;string msg;check(g.saves.TryRead(out saved,out msg)&&saved.version==4,"V4 save readable");
 s.ApplyCore(new StatDelta{hunger=60,health=80,mental=80});g.economy.Earn(1000);g.clock.AdvanceSimulation(400);g.Load();
 check(JsonUtility.ToJson(s.Capture())==JsonUtility.ToJson(saved.stats)&&g.clock.Day==saved.day&&Math.Abs(g.clock.SubDaySeconds-saved.time.subDaySeconds)<.00001,"Eight stats and fractional time round trip");
 check(JsonUtility.ToJson(g.economy.Capture())==JsonUtility.ToJson(saved.economy)&&!g.economy.TryTransaction("income",TransactionKind.Income,100,"duplicate after load"),"Debt/assets/transaction ledger round trip");
 check(JsonUtility.ToJson(g.survival.Capture())==JsonUtility.ToJson(saved.survival)&&g.growth.Age==7,"Danger timers/statuses/growth round trip");
 saved.lastSeenUtcTicks=DateTime.UtcNow.AddDays(-2).Ticks;File.WriteAllText(g.saves.SavePath,JsonUtility.ToJson(saved));g.Load();
 check(g.clock.LastOfflineSeconds==cfg.maxOfflineInspectionSeconds&&s.Money==saved.stats.money&&Math.Abs(g.clock.ElapsedSeconds-saved.time.elapsedSeconds)<.00001,"Offline elapsed capped without rewards or simulation");
 check(TimeManager.SafeOfflineSeconds(DateTime.UtcNow.AddDays(1).Ticks,DateTime.UtcNow.Ticks,28800)==0&&TimeManager.SafeOfflineSeconds(0,DateTime.UtcNow.Ticks,28800)==0,"Clock rollback/unknown timestamp safe");
 string statsBefore=JsonUtility.ToJson(s.Capture());int dayBefore=g.clock.Day;File.WriteAllText(g.saves.SavePath,"{broken");g.Load();
 check(g.saves.LastLoadStatus==SaveLoadStatus.Corrupt&&g.saves.WritesBlocked&&g.clock.Day==dayBefore&&JsonUtility.ToJson(s.Capture())==statsBefore&&File.Exists(g.saves.LastRecoveryCopy),"Corrupt load preserves current state and backup");
 check(!g.saves.Save(s,g.clock,out msg)&&File.ReadAllText(g.saves.SavePath)=="{broken","Corrupt source not automatically overwritten");
 for(int version=1;version<=2;version++){
 string legacyName="phase2-legacy-v"+version+"-"+Guid.NewGuid().ToString("N")+".json";slot.SetValue(g.saves,legacyName);
 var legacy=new SaveData{version=version,day=17,stats=new StatSnapshot{happiness=55,stress=66,energy=44,money=321}};string legacyPath=g.saves.SavePath;string json=JsonUtility.ToJson(legacy);File.WriteAllText(legacyPath,json);g.Load();
 check(g.saves.WasMigrated&&s.Money==321&&s.Health==44&&s.Stress==66&&s.Mental==34&&g.clock.Day==17&&g.economy.Debt==0&&g.growth.Age==19,"Legacy v"+version+" preserves recorded stats/day and infers compatibility age");
 g.Save();check(File.ReadAllText(legacyPath)==json&&g.saves.SavePath!=legacyPath&&File.Exists(legacyPath+".pre-phase22.bak"),"Legacy v"+version+" original never overwritten");
 }
 slot.SetValue(g.saves,suite);reset();balance.eventChance=1;int events=0;Action<LifeEvent> applied=e=>events++;g.lifeEvents.EventApplied+=applied;g.clock.AdvanceDays(1);
 typeof(GameManager).GetMethod("OnDay",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,new object[]{g.clock.Day});g.lifeEvents.EventApplied-=applied;
 check(events==1,"Duplicate day settlement rejected");balance.eventChance=0;
 reset();s.ApplyCore(new StatDelta{hunger=-1000,thirst=1000,knowledge=1000,health=-1000,mental=1000,social=1000,credit=-1000,happiness=-1000});
 check(Enum.GetValues(typeof(CoreStat)).Cast<CoreStat>().All(stat=>s.Get(stat)>=0&&s.Get(stat)<=100)&&s.Stress==100-s.Mental,"All eight ranges and stress relation");
 reset();ui.pauseButton.onClick.Invoke();double elapsed=g.clock.ElapsedSeconds;g.clock.AdvanceSimulation(86400);check(g.State==GameState.Paused&&g.clock.ElapsedSeconds==elapsed&&!g.TryAction(3),"Pause gates new actions and shared clock");ui.pauseButton.onClick.Invoke();
 check(ui.coreLabels.Length==8&&ui.coreLabels.All(label=>label!=null)&&ui.economyLabel.text.Contains("1,000,000")&&ui.header.text.Contains("0세"),"Temporary UI displays eight stats, age, cash and debt");
 ui.Bind(g);ui.Bind(g);int d=g.clock.Day;ui.drinkButton.onClick.Invoke();check(g.clock.Day==d+1,"Care button binding does not duplicate");
 var character=UnityEngine.Object.FindAnyObjectByType<Character2DPresentation>();check(character.spriteRenderer.sprite==character.visuals.identity.baseSprite,"Official Ian sprite unchanged");
 check(g.missions.catalog.missions.Length==60,"Explicit 60-mission catalog connected");
 return results.ToArray();
 }finally{
 g.Logged-=log;slot.SetValue(g.saves,suite);JsonUtility.FromJsonOverwrite(oldCfg,cfg);JsonUtility.FromJsonOverwrite(oldBalance,balance);
 g.saves.AllowExplicitNewGame();File.WriteAllText(g.saves.SavePath,JsonUtility.ToJson(start));g.Load();g.autoSaveAfterAction=auto;g.autoSaveOnLifecycle=lifecycle;g.clock.realtimeEnabled=realtime;slot.SetValue(g.saves,originalSlot);UnityEngine.Random.state=random;
 }
 }
}
}