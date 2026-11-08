using System;using System.Linq;using System.Collections.Generic;using UnityEngine;
namespace IanGame {
public class MissionService:MonoBehaviour {
 readonly List<IMissionFeatureSource> sources=new List<IMissionFeatureSource>();
 public MissionFeature SupportedFeatures{get{var value=MissionFeature.Core|MissionFeature.BasicEducation;foreach(var source in sources)if(source.IsAvailable)value|=source.Features;return value;}}
 public bool RegisterSource(IMissionFeatureSource source){if(source==null||sources.Contains(source))return false;sources.Add(source);source.ActivityProduced+=OnSourceSignal;Refresh();return true;}
 public bool UnregisterSource(IMissionFeatureSource source){if(source==null||!sources.Remove(source))return false;source.ActivityProduced-=OnSourceSignal;Refresh();return true;}
 void OnSourceSignal(MissionActivityEvent value){RecordSignal(value);}
 public bool HasChoice(MissionChoiceOutcome outcome)=>state.choiceOutcomes.Contains(outcome);
 public bool RecordChoice(string id,MissionChoiceOutcome outcome){if(!Ready||!EnabledForLife||suspended||outcome==MissionChoiceOutcome.None||!Enum.IsDefined(typeof(MissionChoiceOutcome),outcome)||state.seenEventIds.Contains(id)||!provider.Supports(MissionFeature.EventChoices))return false;bool added=!state.choiceOutcomes.Contains(outcome);if(added)state.choiceOutcomes.Add(outcome);bool result=RecordSignal(new MissionActivityEvent{id=id,counter=MissionCounter.EventChoices,amount=1});if(!result&&added)state.choiceOutcomes.Remove(outcome);return result;}
 public MissionCatalog catalog;AgeProgressionService ageProgression;GameManager game;IMissionStateProvider provider;MissionSnapshot state=new MissionSnapshot();bool busy,suspended;
 public bool IsBusy=>busy||suspended;public bool Ready=>provider!=null&&catalog!=null;public bool EnabledForLife=>game!=null&&game.LifeMode&&catalog!=null;
 public MissionData ActiveMission=>!Ready?null:catalog.missions.FirstOrDefault(m=>m!=null&&!state.claimedMissionIds.Contains(m.Id));
 public MissionSnapshot State=>Capture();
 public event Action Changed;public event Action<MissionData> Completed,RewardClaimed;public event Action CheckpointRequested;public event Action<int,LifeStage> CharacterStageRequested;
 public void Configure(GameManager owner,IMissionStateProvider explicitProvider=null){
 Detach();game=owner;ageProgression=new AgeProgressionService(owner.growth);provider=explicitProvider??new RuntimeMissionStateProvider(this,owner);
 if(game!=null){game.stats.Changed+=Refresh;game.clock.DayChanged+=OnDay;game.growth.Changed+=Refresh;game.growth.AgeChanged+=OnAge;game.economy.Changed+=Refresh;}
 }
 void Detach(){if(game==null)return;game.stats.Changed-=Refresh;game.clock.DayChanged-=OnDay;game.growth.Changed-=Refresh;game.growth.AgeChanged-=OnAge;game.economy.Changed-=Refresh;}
 void OnDestroy(){Detach();foreach(var source in sources)source.ActivityProduced-=OnSourceSignal;}
 void OnDay(int day){Refresh();}
 void OnAge(int age,LifeStage stage){UnlockForAge(age);CharacterStageRequested?.Invoke(age,stage);}
 void UnlockForAge(int age){if(age>=7){Unlock(ContentUnlock.School);Unlock(ContentUnlock.Education);}if(age>=13)Unlock(ContentUnlock.Equipment);if(age>=19){Unlock(ContentUnlock.Employment);Unlock(ContentUnlock.Finance);}if(age>=30)Unlock(ContentUnlock.Housing);if(age>=60)Unlock(ContentUnlock.Retirement);}
 void Unlock(ContentUnlock value){if(!state.unlocks.Contains(value))state.unlocks.Add(value);}
 public void ResetLife(int day){state=new MissionSnapshot{startDay=day};if(game!=null)UnlockForAge(game.growth.Age);Refresh();}
 public void BeginRestore(){suspended=true;}
 public void Restore(MissionSnapshot snapshot,int day){
 state=snapshot==null?new MissionSnapshot{startDay=day}:JsonUtility.FromJson<MissionSnapshot>(JsonUtility.ToJson(snapshot));
 if(state.startDay<1)state.startDay=day;if(game!=null)UnlockForAge(game.growth.Age);
 }
 public void EndRestore(){suspended=false;Refresh();}
 public MissionSnapshot Capture(){
 var clone=JsonUtility.FromJson<MissionSnapshot>(JsonUtility.ToJson(state));
 clone.activeMissionId=ActiveMission==null?null:ActiveMission.Id;
 clone.progress=Ready?catalog.missions.Where(m=>m!=null).Select(GetProgress).ToList():new List<MissionProgress>();return clone;
 }
 public long Counter(MissionCounter counter){var value=state.counters.FirstOrDefault(v=>v.counter==counter);return value==null?0:value.value;}
 public bool Claimed(string id)=>state.claimedMissionIds.Contains(id);public bool HasUnlock(ContentUnlock value)=>state.unlocks.Contains(value);
 public string NextEventId(string runId){if(state.eventSequence==long.MaxValue)throw new InvalidOperationException("Activity ID exhausted");state.eventSequence++;return runId+"/activity/"+state.eventSequence;}
 public static MissionFeature FeatureForCounter(MissionCounter counter){
 if(counter==MissionCounter.BattleWins)return MissionFeature.Combat;
 if(counter==MissionCounter.GachaDraws)return MissionFeature.Gacha;int n=(int)counter;if(n<10||counter==MissionCounter.Work)return MissionFeature.Core;if(n<100)return MissionFeature.BasicEducation;
 switch(counter){case MissionCounter.SecondaryGraduation:case MissionCounter.MiddleSchool:case MissionCounter.CareerChosen:return MissionFeature.AdvancedEducation;case MissionCounter.HeadphonesEquipped:case MissionCounter.EquipmentEquipped:return MissionFeature.Equipment;case MissionCounter.ResumeWritten:case MissionCounter.Interviews:case MissionCounter.Employment:case MissionCounter.Salary:case MissionCounter.Promotions:return MissionFeature.Jobs;case MissionCounter.TaxesPaid:return MissionFeature.Taxation;case MissionCounter.DebtRepaid:return MissionFeature.DebtRepayment;case MissionCounter.StockBuys:case MissionCounter.StockSells:case MissionCounter.PnLReviews:return MissionFeature.Investments;case MissionCounter.FurnitureBought:case MissionCounter.RentPaid:case MissionCounter.InsuranceLearned:return MissionFeature.Housing;case MissionCounter.EventChoices:case MissionCounter.LifeEventRecollections:return MissionFeature.EventChoices;case MissionCounter.MajorAchievements:return MissionFeature.Achievements;default:return MissionFeature.Endings;}
 }
 public bool RecordSignal(MissionActivityEvent signal){
 if(!Ready||suspended||!EnabledForLife||string.IsNullOrEmpty(signal.id)||signal.amount<=0||!Enum.IsDefined(typeof(MissionCounter),signal.counter)||signal.counter==MissionCounter.None||state.seenEventIds.Contains(signal.id)||!provider.Supports(FeatureForCounter(signal.counter)))return false;
 state.seenEventIds.Add(signal.id);var value=state.counters.FirstOrDefault(v=>v.counter==signal.counter);if(value==null){value=new MissionCounterValue{counter=signal.counter};state.counters.Add(value);}
 value.value=value.value>long.MaxValue-signal.amount?long.MaxValue:value.value+signal.amount;Refresh();return true;
 }
 public void RecordAction(string eventId,int action){
 MissionCounter counter;switch(action){case 0:counter=MissionCounter.Work;break;case 1:counter=MissionCounter.Rest;break;case 3:counter=MissionCounter.Meal;break;case 4:counter=MissionCounter.Drink;break;case 5:counter=MissionCounter.Sleep;break;case 7:counter=MissionCounter.SchoolEnrollment;break;case 8:counter=MissionCounter.BooksRead;break;case 9:counter=MissionCounter.FriendsMade;break;case 10:counter=MissionCounter.Homework;break;case 11:counter=MissionCounter.Exams;break;case 12:counter=MissionCounter.ReportCards;break;case 13:counter=MissionCounter.AfterSchool;break;case 14:counter=MissionCounter.Study;break;default:return;}
 RecordSignal(new MissionActivityEvent{id=eventId,counter=counter,amount=1});
 }
 bool Prerequisites(MissionData mission)=>mission.prerequisiteIds==null||mission.prerequisiteIds.All(Claimed);
 public MissionProgress GetProgress(MissionData mission){
 var result=Ready?MissionConditionEvaluator.Evaluate(mission.condition,provider):new MissionConditionResult{target=1};
 MissionState status=MissionState.Locked;
 if(state.claimedMissionIds.Contains(mission.Id))status=MissionState.RewardClaimed;
 else if(state.completedMissionIds.Contains(mission.Id))status=MissionState.Completed;
 else if(Ready&&!provider.Supports(mission.requiredFeatures))status=MissionState.Planned;
 else if(Prerequisites(mission))status=ActiveMission==mission?MissionState.InProgress:MissionState.Available;
 if(status==MissionState.Completed||status==MissionState.RewardClaimed)result.current=result.target;
 return new MissionProgress{missionId=mission.Id,state=status,current=result.current,target=result.target,eventCount=RelevantEvents(mission.condition),prerequisiteIds=mission.prerequisiteIds};
 }
 long RelevantEvents(MissionCondition condition){if(condition==null)return 0;if(condition.kind==MissionConditionKind.CounterAtLeast)return Counter(condition.counter);if(condition.children==null)return 0;long total=0;foreach(var c in condition.children){long value=RelevantEvents(c);total=total>long.MaxValue-value?long.MaxValue:total+value;}return total;}
 public void Refresh(){
 if(!Ready||IsBusy)return;busy=true;
 try{
 var mission=ActiveMission;if(EnabledForLife&&(game.State==GameState.Playing||game.State==GameState.Resolving)&&mission!=null&&Prerequisites(mission)&&provider.Supports(mission.requiredFeatures)&&!state.completedMissionIds.Contains(mission.Id)&&MissionConditionEvaluator.Evaluate(mission.condition,provider).satisfied){
 state.completedMissionIds.Add(mission.Id);
 if(!state.milestoneIds.Contains(mission.Id)){
 state.milestoneIds.Add(mission.Id);if(mission.targetAge>=0&&mission.targetAge>=game.growth.Age)ageProgression.ApplyMissionMilestone(mission.Id,mission.targetAge);
 if(mission.enterLifeReview){state.finalReviewRequested=true;Unlock(ContentUnlock.LifeReview);}
 }
 Completed?.Invoke(mission);CheckpointRequested?.Invoke();
 }
 state.activeMissionId=ActiveMission==null?null:ActiveMission.Id;
 }finally{busy=false;}Changed?.Invoke();
 }
 public bool ClaimActive()=>ActiveMission!=null&&Claim(ActiveMission.Id);
 public bool Claim(string id){
 if(!Ready||IsBusy||!EnabledForLife||game.State!=GameState.Playing||GameContext.Instance!=null&&GameContext.Instance.transitions.IsTransitioning)return false;var mission=catalog.missions.FirstOrDefault(m=>m!=null&&m.Id==id);
 if(mission==null||!state.completedMissionIds.Contains(id)||state.claimedMissionIds.Contains(id))return false;
 busy=true;try{string error;if(!MissionRewardService.TryGrant(mission,state,game.economy,game.growth,catalog.experiencePerLevel,out error)){game.Log(error);return false;}state.claimedMissionIds.Add(id);}
 finally{busy=false;}RewardClaimed?.Invoke(mission);Refresh();CheckpointRequested?.Invoke();return true;
 }
 public bool DeliverPending(string id,IPendingMissionRewardGateway gateway){if(IsBusy)return false;bool result;busy=true;try{result=MissionRewardService.TryDeliver(state,id,gateway);}finally{busy=false;}if(result){Changed?.Invoke();CheckpointRequested?.Invoke();}return result;}
 public static bool ValidSnapshot(MissionSnapshot s){
 if(s==null||s.startDay<1||s.eventSequence<0||s.growthPoints<0||s.counters==null||s.seenEventIds==null||s.completedMissionIds==null||s.claimedMissionIds==null||s.walletReceiptIds==null||s.milestoneIds==null||s.unlocks==null||s.pendingRewards==null||s.progress==null||s.choiceOutcomes==null||s.choiceOutcomes.Any(v=>v==MissionChoiceOutcome.None||!Enum.IsDefined(typeof(MissionChoiceOutcome),v))||s.choiceOutcomes.Distinct().Count()!=s.choiceOutcomes.Count)return false;
 foreach(var list in new[]{s.seenEventIds,s.completedMissionIds,s.claimedMissionIds,s.walletReceiptIds,s.milestoneIds})if(list.Any(string.IsNullOrEmpty)||list.Distinct().Count()!=list.Count)return false;
 if(s.claimedMissionIds.Any(id=>!s.completedMissionIds.Contains(id))||s.counters.Any(v=>v==null||v.value<0||!Enum.IsDefined(typeof(MissionCounter),v.counter))||s.counters.Select(v=>v.counter).Distinct().Count()!=s.counters.Count)return false;
 if(s.unlocks.Any(v=>!Enum.IsDefined(typeof(ContentUnlock),v))||s.pendingRewards.Any(v=>v==null||string.IsNullOrEmpty(v.id)||v.amount<=0||!Enum.IsDefined(typeof(PendingMissionRewardKind),v.kind)||v.kind==PendingMissionRewardKind.Item&&(v.item==MissionItemKey.None||!Enum.IsDefined(typeof(MissionItemKey),v.item))))return false;
 if(s.progress.Any(p=>p==null||string.IsNullOrEmpty(p.missionId)||!Enum.IsDefined(typeof(MissionState),p.state)||double.IsNaN(p.current)||double.IsInfinity(p.current)||double.IsNaN(p.target)||double.IsInfinity(p.target)||p.current<0||p.target<1))return false;
 return s.pendingRewards.Select(v=>v.id).Distinct().Count()==s.pendingRewards.Count;
 }
 class RuntimeMissionStateProvider:IMissionStateProvider,IMissionEconomyStateProvider {
 public long NetWorth=>game.economy.NetWorth;
 readonly MissionService owner;readonly GameManager game;public RuntimeMissionStateProvider(MissionService s,GameManager g){owner=s;game=g;}
 public int Stat(CoreStat stat)=>game.stats.Get(stat);public long Counter(MissionCounter counter)=>owner.Counter(counter);public int Age=>game.growth.Age;public LifeStage Stage=>game.growth.Stage;public int DaysSurvived=>Math.Max(0,game.clock.Day-owner.state.startDay);public long Cash=>game.economy.Cash;public bool Alive=>(game.State==GameState.Playing||game.State==GameState.Resolving)&&!game.survival.Failed;
 public bool HasItem(MissionItemKey item)=>owner.sources.Any(s=>s.IsAvailable&&s.HasItem(item));public bool ChoiceOutcome(MissionChoiceOutcome outcome)=>owner.HasChoice(outcome);public bool Claimed(string id)=>owner.Claimed(id);public bool Unlocked(ContentUnlock value)=>owner.state.unlocks.Contains(value);
 public bool Supports(MissionFeature features)=>(features&~owner.SupportedFeatures)==0;
 }
}

}
