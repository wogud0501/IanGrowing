#if UNITY_EDITOR
using System;using System.Collections;using System.Collections.Generic;using System.Linq;using System.IO;using System.Reflection;using UnityEngine;using UnityEngine.SceneManagement;using UnityEngine.UI;using UnityEditor;
namespace IanGame.Editor {
public class Phase22Runner:MonoBehaviour {
 GameManager game;string balanceJson;bool oldRealtime,oldLifecycle;Dictionary<string,int> milestoneAges=new Dictionary<string,int>();GameContext originalContext;MissionService originalMissions;
 public void StartChecks(){StartCoroutine(Safe());}
 IEnumerator Safe(){
 var steps=Scenario();for(int frame=0;frame<20000;frame++){bool next;object current=null;try{next=steps.MoveNext();if(next)current=steps.Current;}catch(Exception e){Phase22Verification.Failure=e.ToString();break;}if(!next)break;yield return current;}
 if(game!=null){game.missions.Completed-=OnComplete;game.missions.Configure(game);if(balanceJson!=null)JsonUtility.FromJsonOverwrite(balanceJson,game.balance);typeof(SaveManager).GetField("fileName",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game.saves,Phase22Verification.SandboxSlot);game.saves.AllowExplicitNewGame();File.WriteAllText(game.saves.SavePath,Phase22Verification.OriginalStateJson);game.Load();if(Phase22Verification.OriginalPaused)game.Pause();typeof(SaveManager).GetField("fileName",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game.saves,Phase22Verification.OriginalSlot);game.realtimeLifeEnabled=oldRealtime;game.autoSaveOnLifecycle=oldLifecycle;}
 Phase22Verification.Running=false;Phase22Verification.Finished=true;UnityEngine.Object.Destroy(gameObject);
 }
 void Check(bool value,string label){if(!value)throw new Exception("FAIL "+label);Phase22Verification.Passed.Add("PASS "+label);}
 bool Ready(string name)=>SceneManager.GetActiveScene().name==name&&GameContext.Instance!=null&&!GameContext.Instance.transitions.IsTransitioning;
 void WaitLimit(int count,string scene){if(count>900)throw new Exception("Scene transition timed out: "+scene);}
 UIManager HUD()=>UnityEngine.Object.FindAnyObjectByType<UIManager>();
 MissionUI MissionView()=>UnityEngine.Object.FindAnyObjectByType<MissionUI>();
 void Click(Button button){if(button==null||!button.gameObject.activeInHierarchy||!button.interactable)throw new Exception("UI button not usable: "+(button==null?"null":button.name));button.onClick.Invoke();}
 void OnComplete(MissionData data){milestoneAges[data.Id]=game.growth.Age;}
 void ContextInvariant(string label){Check(GameContext.Instance==originalContext&&GameContext.Instance.game==game&&game.missions==originalMissions&&UnityEngine.Object.FindObjectsByType<GameContext>(FindObjectsInactive.Include).Length==1&&UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include).Length==1&&UnityEngine.Object.FindObjectsByType<SaveManager>(FindObjectsInactive.Include).Length==1,label);}
 void Care(){
 var ui=MissionView();ui.CloseEducation();ui.CloseList();
 for(int i=0;i<8&&(game.stats.Hunger<60||game.stats.Thirst<60||game.stats.Health<60||game.stats.Mental<60);i++){
 var hud=HUD();if(game.stats.Hunger<60)Click(hud.mealButton);if(game.stats.Thirst<60)Click(hud.drinkButton);if(game.stats.Health<60||game.stats.Mental<60)Click(hud.sleepButton);
 }
 if(game.State!=GameState.Playing)throw new Exception("Survival failed during actual play path");
 }
 void Education(int action){Care();var ui=MissionView();ui.OpenEducation();Click(ui.activityButtons[action-7]);ui.CloseEducation();}
 IEnumerator Scenario(){
 int wait=0;while(!Ready("MainMenu")){WaitLimit(++wait,"MainMenu");yield return null;}
 game=GameContext.Instance.game;originalContext=GameContext.Instance;originalMissions=game.missions;balanceJson=JsonUtility.ToJson(game.balance);oldRealtime=game.realtimeLifeEnabled;oldLifecycle=game.autoSaveOnLifecycle;game.realtimeLifeEnabled=false;game.clock.realtimeEnabled=false;game.autoSaveOnLifecycle=false;game.balance.eventChance=0;game.missions.Completed+=OnComplete;
 Phase22Verification.Progress="Catalog/validator checks";
 var catalog=game.missions.catalog;Check(catalog.missions.Length==60&&catalog.missions.Select(m=>m.Id).Distinct().Count()==60,"60 registered missions and unique stable IDs");
 Check(MissionCatalogValidator.Validate(catalog).Count==0,"Catalog conditions/rewards/prerequisites valid");
 var confirmed=new[]{"학교 입학","책 3권 읽기","지식 20 달성","친구 1명 만들기","사회성 15 달성","숙제 5회","시험 응시","첫 성적표 받기","방과 후 학습","12세 달성"};
 Check(catalog.missions.Skip(10).Take(10).Select(m=>m.title).SequenceEqual(confirmed)&&catalog.missions.Skip(10).Take(10).All(m=>m.source==MissionSource.ConfirmedV1),"Original M011-M020 names/provenance retained");
 ValidateBadCatalog();
 Check(EditorBuildSettings.scenes.Take(4).Select(s=>Path.GetFileNameWithoutExtension(s.path)).SequenceEqual(new[]{"Bootstrap","MainMenu","Home","School"})&&EditorBuildSettings.scenes.Take(4).All(s=>s.enabled),"Four scenes registered in Build Profiles global Scene List");
 Check(new[]{"Workplace","City","Battle","StockMarket","Gacha","Shop","Housing","Ending"}.All(s=>!File.Exists("Assets/_Game/Scenes/"+s+".unity")),"Future scene names defined without premature content scenes");
 var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuPresenter>();Click(menu.continueButton);
 wait=0;while(!Ready("Home")){WaitLimit(++wait,"Home");yield return null;}yield return null;
 ContextInvariant("MainMenu -> Home keeps one shared context");
 string before=JsonUtility.ToJson(game.stats.Capture());int initialAge=game.growth.Age;long debt=game.economy.Debt;
 Click(UnityEngine.Object.FindAnyObjectByType<LocationPresenter>().schoolButton);
 wait=0;while(!Ready("School")){WaitLimit(++wait,"School");yield return null;}yield return null;
 Check(JsonUtility.ToJson(game.stats.Capture())==before&&game.growth.Age==initialAge&&game.economy.Debt==debt,"Home -> School preserves player/financial state");ContextInvariant("School has no duplicate managers");
 Click(UnityEngine.Object.FindAnyObjectByType<LocationPresenter>().homeButton);
 wait=0;while(!Ready("Home")){WaitLimit(++wait,"Home");yield return null;}yield return null;
 ContextInvariant("School -> Home keeps same manager and mission service");
 Click(HUD().newGameButton);game.clock.realtimeEnabled=false;
 Check(game.growth.Age==0&&game.stats.Money==0&&game.economy.Debt==1000000,"New mission life starts age0 cash0 debt1000000");
 for(int n=1;n<=20;n++){
 string id="M"+n.ToString("000");Phase22Verification.Progress="Actual UI play "+id;
 if(n==11){Click(UnityEngine.Object.FindAnyObjectByType<LocationPresenter>().schoolButton);wait=0;while(!Ready("School")){WaitLimit(++wait,"School");yield return null;}yield return null;}
 Check(game.missions.ActiveMission!=null&&game.missions.ActiveMission.Id==id,"Sequential active mission "+id);
 if(n==16){
 Education(10);Education(10);Click(HUD().saveButton);SaveData saved;string message;if(!game.saves.TryRead(out saved,out message))throw new Exception(message);
 for(int i=0;i<3;i++)Education(10);Check(game.missions.GetProgress(catalog.missions[15]).state==MissionState.Completed,"Homework completion from five actual actions");
 Click(MissionView().claimButton);Click(HUD().loadButton);
 Check(game.missions.Counter(MissionCounter.Homework)==2&&!game.missions.Claimed("M016")&&game.growth.Age==saved.progression.age&&game.stats.Money==saved.stats.money,"Mid-mission manual save/load restores homework2, age and reward receipts");
 }
 for(int attempt=0;game.missions.GetProgress(catalog.missions[n-1]).state!=MissionState.Completed;attempt++){
 if(attempt>24)throw new Exception("Actual mission cannot complete: "+id);
 if(n<=10){Care();int action=catalog.missions[n-1].suggestedAction;if(action<0){game.missions.Refresh();}else{var hud=HUD();Click(action==4?hud.drinkButton:action==3?hud.mealButton:action==5?hud.sleepButton:hud.restButton);}}
 else{int action=catalog.missions[n-1].suggestedAction;if(action<0){game.missions.Refresh();}else Education(action);}
 }
 if(n==10)Check(game.growth.Age==7,"M010 completion -> age7");
 if(n==19)Check(game.growth.Age==12,"M019 completion -> age12");
 if(n==20)Check(game.growth.Age==13,"M020 completion -> age13 without circular dependency");
 Click(MissionView().claimButton);Check(game.missions.Claimed(id),"Actual UI completion/claim "+id);yield return null;
 }
 Check(game.stats.Money==1050&&game.growth.Experience==400&&game.growth.Level==5&&game.missions.State.growthPoints==20,"M001-M020 rewards/XP/level/growth points exactly once");
 long cash=game.economy.Cash,xp=game.growth.Experience;Check(!game.missions.Claim("M001")&&!game.missions.Claim("M020")&&game.economy.Cash==cash&&game.growth.Experience==xp,"Duplicate reward claims rejected");
 var state=game.missions.State;var last=state.seenEventIds.Last();long drinks=game.missions.Counter(MissionCounter.Drink);Check(!game.missions.RecordSignal(new MissionActivityEvent{id=last,counter=MissionCounter.Drink,amount=1})&&game.missions.Counter(MissionCounter.Drink)==drinks,"Duplicate activity event ID rejected");
 Check(!game.missions.RecordSignal(new MissionActivityEvent{id="runtime-unimplemented",counter=MissionCounter.MiddleSchool,amount=1})&&game.missions.GetProgress(catalog.missions[20]).state==MissionState.Planned,"Production future activities cannot fake M021 completion");
 Check(state.pendingRewards.Any(r=>r.kind==PendingMissionRewardKind.GachaTicket&&r.amount==1&&!r.delivered),"Unavailable gacha reward queued, no fake item spawned");
 MissionView().OpenList();Check(MissionView().rowContainer.GetComponentsInChildren<MissionRowView>(true).Length==60,"Scrollable UI exposes 60 mission rows");MissionView().CloseList();
 var buttons=HUD().GetComponentsInChildren<Button>(true);Check(buttons.All(b=>b.GetComponentInChildren<TMPro.TMP_Text>(true).alignment==TMPro.TextAlignmentOptions.Center),"New and existing button labels centered");
 Care();int age=game.growth.Age;game.clock.AdvanceDays(2);Check(game.growth.Age==age&&game.growth.Level!=age,"Days do not change age / level independent");
 Click(HUD().saveButton);string manual=File.ReadAllText(game.saves.SavePath);
 string missionState=JsonUtility.ToJson(game.missions.Capture());string pending=JsonUtility.ToJson(game.economy.Capture());
 Click(UnityEngine.Object.FindAnyObjectByType<LocationPresenter>().homeButton);wait=0;while(!Ready("Home")){WaitLimit(++wait,"Home");yield return null;}yield return null;
 Check(JsonUtility.ToJson(game.missions.Capture())==missionState&&JsonUtility.ToJson(game.economy.Capture())==pending,"School actions/mission rewards/entitlements visible immediately in Home");
 ContextInvariant("Full Home-School-Home path shares one service container");
 Phase22Verification.Progress="Editor-only future condition providers M021-M060";
 var fake=new FakeMissionProvider(game.missions,game);game.missions.Configure(game,fake);
 TestComposite(fake);
 for(int n=21;n<=60;n++){
 var m=catalog.missions[n-1];fake.Satisfy(m.condition,"future-fixture/"+m.Id);
 Check(game.missions.GetProgress(m).state==MissionState.Completed,"Editor fake conditions "+m.Id);
 Check(game.missions.Claim(m.Id),"Editor fake reward receipt "+m.Id);
 }
 Check(milestoneAges["M030"]==19&&milestoneAges["M040"]==30&&milestoneAges["M050"]==60,"Future milestones age19/30/60");
 Check(game.missions.State.finalReviewRequested&&game.missions.State.unlocks.Contains(ContentUnlock.LifeReview)&&SceneManager.GetActiveScene().name!="Ending","M060 creates final-review handoff, not an invented ending system");
 var ticket=game.missions.State.pendingRewards.First();var gateway=new FakePendingGateway{reenter=game.missions};Check(game.missions.DeliverPending(ticket.id,gateway)&&!game.missions.DeliverPending(ticket.id,gateway)&&gateway.calls==1&&!gateway.reentryAccepted,"Deferred reward delivery rejects duplicates and re-entrancy");
 game.missions.Configure(game);File.WriteAllText(game.saves.SavePath,manual);Click(HUD().loadButton);
 Check(game.growth.Age==13&&game.missions.Claimed("M020")&&!game.missions.Claimed("M021")&&game.missions.GetProgress(catalog.missions[20]).state==MissionState.Planned,"Restoring actual progress removes all test-only future state");
 TestV3Migration();
 var context=GameContext.Instance;string currentScene=SceneManager.GetActiveScene().name;
 Check(!context.transitions.Navigate(SceneId.Battle)&&!string.IsNullOrEmpty(context.transitions.LastError)&&SceneManager.GetActiveScene().name==currentScene,"Unimplemented route safely rejected");
 Check(context.transitions.Navigate(SceneId.School)&&!context.transitions.Navigate(SceneId.Home),"Concurrent transitions rejected");
 wait=0;while(!Ready("School")){WaitLimit(++wait,"School");yield return null;}yield return null;
 ContextInvariant("Repeated location transitions retain unique context");
 SceneManager.LoadSceneAsync("Bootstrap",LoadSceneMode.Single);
 wait=0;while(!Ready("MainMenu")){WaitLimit(++wait,"MainMenu after duplicate Bootstrap");yield return null;}yield return null;
 ContextInvariant("Repeated Bootstrap cannot duplicate GameContext/GameManager/SaveManager");
 Click(UnityEngine.Object.FindAnyObjectByType<MainMenuPresenter>().continueButton);wait=0;while(!Ready("Home")){WaitLimit(++wait,"Home final");yield return null;}yield return null;
 Click(HUD().loadButton);Check(game.growth.Age==13&&game.missions.Claimed("M020")&&game.stats.Money==1050,"Save/load after scene cycling restores claimed missions and growth");
 Phase22Verification.Progress="Completed";
 }
 void TestComposite(FakeMissionProvider fake){
 var a=new MissionCondition{kind=MissionConditionKind.All,children=new[]{new MissionCondition{kind=MissionConditionKind.StatAtLeast,stat=CoreStat.Knowledge,target=99},new MissionCondition{kind=MissionConditionKind.CounterAtLeast,counter=MissionCounter.Study,target=999}}};
 fake.SetStat(CoreStat.Knowledge,0);Check(!MissionConditionEvaluator.Evaluate(a,fake).satisfied,"AND rejects partial conditions");fake.Satisfy(a,"and-fixture");Check(MissionConditionEvaluator.Evaluate(a,fake).satisfied,"AND accepts all conditions");
 var owned=new MissionCondition{kind=MissionConditionKind.ItemOwned,item=MissionItemKey.StarterHeadphones};Check(!MissionConditionEvaluator.Evaluate(owned,fake).satisfied,"Item ownership is queried explicitly");fake.Satisfy(owned,"item-fixture");Check(MissionConditionEvaluator.Evaluate(owned,fake).satisfied,"Editor-only item provider condition");var choice=new MissionCondition{kind=MissionConditionKind.EventChoiceOutcome,choice=MissionChoiceOutcome.AcceptedOffer};fake.Satisfy(choice,"choice-fixture");Check(MissionConditionEvaluator.Evaluate(choice,fake).satisfied&&!game.missions.RecordChoice("choice-fixture/choice",MissionChoiceOutcome.AcceptedOffer),"Choice outcome and event deduplication");var draw=new MissionCondition{kind=MissionConditionKind.CounterAtLeast,counter=MissionCounter.GachaDraws,target=2};fake.Satisfy(draw,"gacha-fixture");Check(MissionConditionEvaluator.Evaluate(draw,fake).satisfied,"Gacha condition via editor-only signals, no gacha gameplay");
 var any=new MissionCondition{kind=MissionConditionKind.Any,children=new[]{new MissionCondition{kind=MissionConditionKind.StatAtLeast,stat=CoreStat.Knowledge,target=99},new MissionCondition{kind=MissionConditionKind.CounterAtLeast,counter=MissionCounter.StockBuys,target=9999}}};Check(MissionConditionEvaluator.Evaluate(any,fake).satisfied,"OR accepts one satisfied branch");
 }
 void ValidateBadCatalog(){
 var catalog=ScriptableObject.CreateInstance<MissionCatalog>();var a=ScriptableObject.CreateInstance<MissionData>();var b=ScriptableObject.CreateInstance<MissionData>();a.SetEditorId("TEST_A");b.SetEditorId("TEST_B");a.condition=b.condition=new MissionCondition{kind=MissionConditionKind.Alive};
 try{
 catalog.missions=new[]{a,a};Check(MissionCatalogValidator.Validate(catalog).Any(e=>e.Contains("Duplicate")),"Validator rejects duplicate IDs");
 catalog.missions=new[]{a,b};a.prerequisiteIds=new[]{"TEST_B"};b.prerequisiteIds=new[]{"TEST_A"};Check(MissionCatalogValidator.Validate(catalog).Any(e=>e.Contains("cycle")),"Validator rejects prerequisite cycles");
 a.prerequisiteIds=b.prerequisiteIds=new string[0];a.condition=new MissionCondition{kind=MissionConditionKind.MissionClaimed,missionId="TEST_A"};Check(MissionCatalogValidator.Validate(catalog).Any(e=>e.Contains("cycle")),"Validator rejects condition-reference cycles");
 a.condition=new MissionCondition{kind=MissionConditionKind.CounterAtLeast,counter=(MissionCounter)99999};Check(MissionCatalogValidator.Validate(catalog).Any(e=>e.Contains("counter")),"Validator rejects unknown condition reference");
 a.condition=new MissionCondition{kind=MissionConditionKind.Alive};a.rewards.items=new[]{new MissionItemReward{item=(MissionItemKey)999}};Check(MissionCatalogValidator.Validate(catalog).Any(e=>e.Contains("item")),"Validator rejects unknown reward item");
 }finally{UnityEngine.Object.DestroyImmediate(catalog);UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
 }
 void TestV3Migration(){
 var root=new GameObject("Editor-only V3 Migration Probe");root.hideFlags=HideFlags.HideAndDontSave;
 try{
 var save=root.AddComponent<SaveManager>();var stats=root.AddComponent<PlayerStats>();var time=root.AddComponent<TimeManager>();var growth=root.AddComponent<LifeProgression>();var economy=root.AddComponent<EconomySystem>();var status=root.AddComponent<StatusEffectSystem>();var survival=root.AddComponent<SurvivalSystem>();economy.Configure(stats);status.Configure(stats);survival.Configure(stats,game.balance.survival,status);save.Configure(growth,economy,survival);
 typeof(SaveManager).GetField("fileName",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(save,"phase22-v3-"+Guid.NewGuid().ToString("N")+".json");
 var old=new SaveData{version=3,day=29,stats=new StatSnapshot{happiness=55,energy=44,mental=34,stress=66,money=321,hunger=65,thirst=75},progression=new ProgressSnapshot{age=30,level=12,experience=1234},economy=new EconomySnapshot{debt=2345},survival=new SurvivalSnapshot(),time=new TimeSnapshot{subDaySeconds=123,elapsedSeconds=123},runId="legacy-v3-proof",settledDay=29,lastSeenUtcTicks=DateTime.UtcNow.Ticks,missions=null};
 string source=save.SavePath,json=JsonUtility.ToJson(old);File.WriteAllText(source,json);SaveData migrated;string message;
 Check(save.TryRead(out migrated,out message)&&migrated.version==4&&migrated.stats.money==321&&migrated.progression.age==30&&migrated.progression.level==12&&migrated.economy.debt==2345,"V3 migration preserves money/debt/age/XP/level");
 Check(migrated.missions.claimedMissionIds.Count==0&&migrated.missions.activeMissionId=="M001","V3 without missions gets safe empty mission state");
 save.ApplyToModels(migrated,stats,time);Check(save.Save(stats,time,out message)&&save.SavePath!=source&&File.ReadAllText(source)==json,"V3 original never overwritten by V4 save");
 }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
}

#endif
