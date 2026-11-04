#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.Linq;using System.IO;using System.Reflection;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.UI;
namespace IanGame.Editor {
public class Phase23Runner:MonoBehaviour {
 public static List<string> Passed=new List<string>();public static string Failure,Progress;public static bool Finished;
 GameManager g;SaveData original;string originalSlot,balanceJson;bool auto,lifecycle,realtime;UnityEngine.Random.State random;
 FieldInfo Slot=>typeof(SaveManager).GetField("fileName",BindingFlags.Instance|BindingFlags.NonPublic);
 public void Begin(){StartCoroutine(Run());}
 public static object Status()=>new{finished=Finished,progress=Progress,passed=Passed.ToArray(),failure=Failure};
 void Check(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);Passed.Add("PASS "+name);}
 SaveData Capture()=>new SaveData{day=g.clock.Day,stats=g.stats.Capture(),progression=g.growth.Capture(),economy=g.economy.Capture(),survival=g.survival.Capture(),time=g.clock.Capture(),missions=g.missions.Capture(),phase23=g.phase23.Capture(),gameOver=g.State==GameState.GameOver,runId=(string)typeof(GameManager).GetField("runId",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(g),settledDay=g.clock.Day,lastSeenUtcTicks=DateTime.UtcNow.Ticks};
 IEnumerator Run(){
 Passed.Clear();Failure=null;Finished=false;g=GameContext.Instance.game;original=Capture();originalSlot=(string)Slot.GetValue(g.saves);balanceJson=JsonUtility.ToJson(g.balance);auto=g.autoSaveAfterAction;lifecycle=g.autoSaveOnLifecycle;realtime=g.clock.realtimeEnabled;random=UnityEngine.Random.state;
 Slot.SetValue(g.saves,"phase23-suite-"+Guid.NewGuid().ToString("N")+".json");g.autoSaveAfterAction=false;g.autoSaveOnLifecycle=false;g.realtimeLifeEnabled=false;
 var stack=new Stack<IEnumerator>();stack.Push(Scenario());while(stack.Count>0){object next=null;bool more=false;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception ex){Failure=ex.ToString();break;}if(!more){stack.Pop();continue;}var nested=next as IEnumerator;if(nested!=null){stack.Push(nested);continue;}yield return next;}
 try{g.phase23.catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<Phase23Catalog>("Assets/_Game/Data/Phase23/LifeCatalog.asset");JsonUtility.FromJsonOverwrite(balanceJson,g.balance);g.saves.AllowExplicitNewGame();File.WriteAllText(g.saves.SavePath,JsonUtility.ToJson(original,true));g.Load();Slot.SetValue(g.saves,originalSlot);g.autoSaveAfterAction=auto;g.autoSaveOnLifecycle=lifecycle;g.realtimeLifeEnabled=realtime;g.clock.realtimeEnabled=realtime;UnityEngine.Random.state=random;}catch(Exception e){Failure+="\nRESTORE "+e;}
 Finished=true;Progress="Complete";File.WriteAllText("Docs/PHASE2_3_TEST_RESULTS.json",JsonUtility.ToJson(new Results{passed=Passed.ToArray(),failure=Failure},true));Destroy(gameObject);
 }
 [Serializable]class Results {public string[] passed;public string failure;}
 IEnumerator Route(SceneId id){if(GameContext.Instance.transitions.CurrentScene==id&&!GameContext.Instance.transitions.IsTransitioning)yield break;Check(GameContext.Instance.transitions.Navigate(id),"Navigate "+id);int frames=0;while(GameContext.Instance.transitions.IsTransitioning||SceneManager.GetActiveScene().name!=id.ToString()){if(++frames>3000)throw new Exception("Scene timeout "+id);yield return null;}yield return null;
 Check(GameContext.Instance.game==g&&UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length==1&&UnityEngine.Object.FindObjectsByType<SaveManager>(FindObjectsSortMode.None).Length==1,"Unique persistent services in "+id);
 }
 void Care(){for(int i=0;i<12;i++){if(g.stats.Hunger<55){Check(g.TryAction(3),"Care meal");continue;}if(g.stats.Thirst<55){Check(g.TryAction(4),"Care drink");continue;}if(g.stats.Health<65||g.stats.Mental<65){Check(g.TryAction(5),"Care sleep");continue;}return;}throw new Exception("Care did not converge");}
 void Act(int id){Care();string reason;Check(g.CanPerformAction(id,out reason),"Available action "+id+" / "+reason);int day=g.clock.Day;Check(g.TryAction(id),"Execute action "+id);Check(g.clock.Day-day==g.actions.Describe(id).days,"Time charged once action "+id);}
 void Click(int id){var ui=UnityEngine.Object.FindAnyObjectByType<Phase23Presenter>();int index=Array.IndexOf(ui.actionIds,id);Check(index>=0&&ui.activityButtons[index].interactable,"Scene UI button "+id);int day=g.clock.Day;ui.activityButtons[index].onClick.Invoke();Check(g.clock.Day>day,"UI executes action "+id);}
 IEnumerator Scenario(){
 g.NewGame();g.clock.realtimeEnabled=false;g.balance.eventChance=0;UnityEngine.Random.InitState(2303);Progress="Newborn to adulthood using real actions";
 yield return Route(SceneId.MainMenu);yield return Route(SceneId.Home);
 Check(!g.TryAction(20)&&!g.TryAction(0)&&g.economy.Cash==0,"Under-19 employment/work blocked");
 // Real missions and real actions only; no fake provider in the integration path.
 for(int iteration=0;iteration<400;iteration++){
 var m=g.missions.ActiveMission;if(m.number>=35)break;Progress=m.Id+" "+m.title;
 var p=g.missions.GetProgress(m);if(p.state==MissionState.Completed){Check(g.missions.ClaimActive(),"Claim "+m.Id);continue;}
 int id=m.suggestedAction;if(id<0){Care();g.missions.Refresh();if(g.missions.GetProgress(m).state!=MissionState.Completed)Act(1);continue;}
 if(id>=7||id==0)yield return Route(g.phase23.Destination(id));
 // Graduation is a real separate school activity, with recoverable preparation and report prerequisites.
 if(m.number==30){
 string reason;if(!g.phase23.Education.Can(16,out reason)){
 var current=g.phase23.Education.Current;var data=g.phase23.Education.Available;
 if(current.studies<data.graduationStudies){Act(14);continue;}
 if(current.homework<data.graduationHomework){Act(10);continue;}
 if(g.phase23.Progress.education.exams.Any(e=>e.educationId==current.educationId&&!e.reportReceived)){Act(12);continue;}
 if(g.phase23.Progress.education.preparation<100){Act(14);continue;}Act(11);continue;
 }}
 Act(id);
 }
 Check(g.missions.Claimed("M030")&&g.growth.Age>=19,"M021-M030 real completion and age 19 unlock");
 Check(Enumerable.Range(31,4).All(n=>g.missions.Claimed("M"+n.ToString("000"))),"M031-M034 real resume interview hire salary");
 Check(g.phase23.Progress.education.books>=3&&g.phase23.Progress.education.homework>=5&&g.stats.Knowledge>=35,"Childhood education counts and knowledge");
 Check(g.phase23.Progress.education.enrollments.Any(e=>e.educationId=="secondary"&&e.graduated),"Secondary graduation stored");
 Check(g.phase23.Progress.education.exams.Count>=2&&g.phase23.Progress.education.exams.Any(e=>e.reportReceived),"Exam and report card records stored");
 Check(g.phase23.Progress.social.relationships.Any(r=>r.friend)&&g.stats.Social>=25,"Explicit friendship and social 15/25");
 Check(g.phase23.HasItem(MissionItemKey.StarterHeadphones)&&g.phase23.Progress.equipped.Contains(MissionItemKey.StarterEquipment),"Pending starter reward delivery and equipped state");
 Check(g.missions.ActiveMission.Id=="M035"&&g.missions.GetProgress(g.missions.ActiveMission).state==MissionState.Planned,"Tax mission stays Planned; no fake advancement");
 Check(!g.missions.Claimed("M040")&&g.missions.Counter(MissionCounter.StockBuys)==0&&g.missions.Counter(MissionCounter.EventChoices)==0,"Unimplemented missions never auto-complete");
 Progress="Employment and payroll integrity";yield return Route(SceneId.Workplace);var module=g.phase23;
 while(module.Progress.employment.workCount<3)Act(0);
 Check(g.missions.Counter(MissionCounter.Work)==3&&MissionConditionEvaluator.Evaluate(g.missionCatalog.missions[36].condition,new RuntimeProbe(g)).satisfied,"M037 actual three-work condition independently verified while sequence locked");
 Check(g.missions.GetProgress(g.missionCatalog.missions[36]).state==MissionState.Locked,"M037 prerequisite lock retained");
 string workId=module.Progress.employment.work.Last().id;long cash=g.economy.Cash;int count=module.Progress.employment.workCount;
 Check(!module.Work.Pay(workId)&&!module.Work.Pay("forged-work"),"Duplicate and nonexistent payroll rejected");module.Work.Complete(workId);Check(g.economy.Cash==cash&&module.Progress.employment.workCount==count,"Repeated work completion cannot award salary or experience");
 Care();int health=g.stats.Health,mental=g.stats.Mental,xp=module.Progress.employment.experience;Act(0);Check(g.stats.Health<health&&g.stats.Mental<mental&&module.Progress.employment.experience>xp,"Work spends health mental and gains experience");
 Care();Click(22);Check(module.Progress.employment.rank==1,"Promotion conditions met through real work");
 Act(23);Check(module.Progress.employment.currentJobId=="unemployed"&&!g.TryAction(0),"Resignation blocks salary until reemployment");
 for(int i=0;i<20&&module.Progress.employment.currentJobId=="unemployed";i++){Act(21);}Check(module.Progress.employment.currentJobId=="convenience","Reemployment after resignation");
 long debt=g.economy.Debt;cash=g.economy.Cash;Care();cash=g.economy.Cash;Act(26);Check(g.economy.Debt==debt-100&&g.economy.Cash==cash-100&&g.missions.Counter(MissionCounter.DebtRepaid)==100,"Existing EconomySystem debt repayment charged once");
 // Use an isolated copy of job conditions to exercise recoverable dismissal without changing balancing assets.
 var job=module.Jobs.Current;string jobJson=JsonUtility.ToJson(job);try{job.DismissalConditions.healthBelow=101;job.DismissalConditions.strikes=1;Act(0);Check(module.Progress.employment.currentJobId=="unemployed"&&module.Progress.employment.history.Any(h=>h.kind=="dismissed"),"Job-specific dismissal records and recovery path");}finally{JsonUtility.FromJsonOverwrite(jobJson,job);}
 for(int i=0;i<20&&module.Progress.employment.currentJobId=="unemployed";i++)Act(21);Check(module.Progress.employment.currentJobId=="convenience","Reemployment after dismissal");
 Progress="Social cooldown, decay, failure, restore";yield return Route(SceneId.City);module.SelectedNpcId="colleague";Care();Click(18);int social=g.stats.Social;int dayNow=g.clock.Day;Check(!g.TryAction(18)&&g.clock.Day==dayNow&&g.stats.Social==social,"Repeated NPC interaction blocked without resource consumption");
 Check(module.Social.Relationship("colleague")!=null&&!module.Social.Relationship("colleague").friend,"Conversation alone creates no friend");
 var r=module.Progress.social.relationships.First(v=>v.friend);int affinity=r.affinity;int maintenanceDay=r.lastDecayDay;module.Social.Decay(maintenanceDay+14);Check(r.affinity<affinity,"Neglect decreases relationship affinity");
 // Controlled random seeds exercise both actual branches; gameplay data is unchanged.
 bool failed=module.Progress.social.history.Any(h=>!h.success);for(int i=0;i<40&&!failed;i++){Act(1);Act(18);failed=module.Progress.social.history.Last().success==false;}Check(failed,"Actual dialogue failure path observed");
 var saved=Capture();string json=JsonUtility.ToJson(saved);g.Save();SaveData read;string message;Check(g.saves.TryRead(out read,out message),"Extended save reads");
 Check(JsonUtility.ToJson(read.phase23)==JsonUtility.ToJson(saved.phase23),"All education relationship job and payroll data roundtrip");
 module.ResetLife();g.Load();Check(JsonUtility.ToJson(module.Capture())==JsonUtility.ToJson(saved.phase23),"Model load restores relationships and employment");Check(!module.Work.Pay(workId),"Payroll deduplication survives save/load");
 foreach(var route in new[]{SceneId.MainMenu,SceneId.Home,SceneId.School,SceneId.City,SceneId.Workplace,SceneId.Home}){
 var before=Capture();yield return Route(route);Check(g.stats.Knowledge==before.stats.knowledge&&g.economy.Cash==before.stats.money&&g.growth.Age==before.progression.age&&JsonUtility.ToJson(module.Capture())==JsonUtility.ToJson(before.phase23),"State preserved across "+route);
 }
 // Test legacy v4 JSON missing the new optional block, not a newly emitted empty block.
 var old=JsonUtility.FromJson<SaveData>(File.ReadAllText("DevelopmentBackups/Phase23/LiveSession.json"));string legacyPath=g.saves.SavePath;string legacyJson=File.ReadAllText("DevelopmentBackups/Phase23/LiveSession.json");File.WriteAllText(legacyPath,legacyJson);Check(g.saves.TryRead(out read,out message)&&read.stats.money==old.stats.money&&read.economy.debt==old.economy.debt&&read.progression.age==old.progression.age&&read.missions.claimedMissionIds.SequenceEqual(old.missions.claimedMissionIds),"Actual pre-Phase23 v4 migrates without resetting money debt age or missions");Check(read.phase23.social.relationships.Count==0&&read.phase23.employment.currentJobId=="unemployed","Missing new data gets safe defaults; no invented relationships");
 // Existing regression tests run with the new optional module disabled, retaining their original feature contract.
 Progress="Phase 1 / 2.1 / 2.2 regressions";var catalog=module.catalog;module.catalog=null;try{
 foreach(var item in Stage1Verification.Run())Passed.Add("REGRESSION "+item);
 foreach(var item in Phase2Verification.Run())Passed.Add("REGRESSION "+item);
 Phase22Verification.Begin();int frames=0;while(!Phase22Verification.Finished){if(++frames>16000)throw new Exception("Phase22 regression timeout");yield return null;}Check(string.IsNullOrEmpty(Phase22Verification.Failure),"Phase22 original regression suite: "+Phase22Verification.Failure);foreach(var item in Phase22Verification.Passed)Passed.Add("REGRESSION "+item);
 }finally{module.catalog=catalog;}
 }
 class RuntimeProbe:IMissionStateProvider {
 readonly GameManager g;public RuntimeProbe(GameManager game){g=game;}public int Stat(CoreStat s)=>g.stats.Get(s);public long Counter(MissionCounter c)=>g.missions.Counter(c);public int Age=>g.growth.Age;public LifeStage Stage=>g.growth.Stage;public int DaysSurvived=>g.clock.Day;public long Cash=>g.economy.Cash;public bool Alive=>!g.survival.Failed;public bool HasItem(MissionItemKey i)=>g.phase23.HasItem(i);public bool ChoiceOutcome(MissionChoiceOutcome c)=>g.missions.HasChoice(c);public bool Claimed(string id)=>g.missions.Claimed(id);public bool Unlocked(ContentUnlock u)=>g.missions.HasUnlock(u);public bool Supports(MissionFeature f)=>(f&~g.missions.SupportedFeatures)==0;
 }
}
}
#endif
