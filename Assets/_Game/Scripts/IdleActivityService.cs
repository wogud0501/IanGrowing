using System;using System.Collections;using System.Linq;using UnityEngine;
namespace IanGame {
public sealed class IdleActivityService:MonoBehaviour,IMissionFeatureSource {
 public MissionFeature Features=>MissionFeature.Combat;public bool IsAvailable=>settings!=null&&enemy!=null;public event Action<MissionActivityEvent> ActivityProduced;
 public bool HasItem(MissionItemKey item)=>false;
 public IdleActivitySettings settings;public EnemyData enemy;
 public IdleProgressSnapshot State{get;private set;}=new IdleProgressSnapshot();public CombatService Combat{get;private set;}
 GameManager game;bool processing,suspended;double lastCheckpoint;public event Action Changed,OfflineReportReady;
 public void Configure(GameManager value){game=value;Combat=new CombatService(game);game.missions.RegisterSource(this);StartCoroutine(Online());}
 void OnDestroy(){if(game!=null)game.missions.UnregisterSource(this);}
 internal void RecordVictory(string id){ActivityProduced?.Invoke(new MissionActivityEvent{id=id+"/victory",counter=MissionCounter.BattleWins,amount=1});}
 public IdleProgressSnapshot Capture()=>JsonUtility.FromJson<IdleProgressSnapshot>(JsonUtility.ToJson(State));
 public void Restore(IdleProgressSnapshot value){State=value==null?new IdleProgressSnapshot():JsonUtility.FromJson<IdleProgressSnapshot>(JsonUtility.ToJson(value));if(value?.report==null||string.IsNullOrEmpty(State.report?.id))State.report=null;SyncClock();Changed?.Invoke();}
 public void ResetLife(){suspended=false;State=new IdleProgressSnapshot();SyncClock();Changed?.Invoke();}
 void SyncClock(){if(game!=null)game.clock.RealtimeSuppressed=State.active;}
 public double Period(IdleActivityDefinition a){double value=a.periodSeconds;if(a.kind==IdleActivityKind.Work){var job=game.phase23.Jobs.Current;if(job==null)return double.PositiveInfinity;value=Math.Max(1,game.clock.realSecondsPerGameDay)*Math.Max(1,job.WorkingTime)/Mathf.Clamp(job.AutoWorkEfficiency,.1f,4);}return Math.Max(1,value/Mathf.Clamp(a.efficiency,.1f,4)/(game.items?.Multiplier(ModifierTarget.IdleEfficiency)??1));}
 public bool CanStart(string id,out string reason){if(game!=null&&game.world!=null&&game.world.Active){reason="월드에서는 작업대·책상·출입구를 직접 이용하세요.";return false;}var a=settings==null?null:settings.Find(id);if(a==null){reason="등록된 자동 활동이 없습니다.";return false;}if(GameContext.Instance!=null&&GameContext.Instance.transitions.CurrentScene!=a.startLocation){reason=a.startLocation+"에서 시작하세요.";return false;}return CanCycle(a,out reason);}
 public bool StartActivity(string id){if(processing||State.active)return false;string reason;if(!CanStart(id,out reason)){game.Log(reason);return false;}State.active=true;State.activityId=id;State.sessionId=Guid.NewGuid().ToString("N");State.jobId=game.phase23.Progress.employment.currentJobId;State.educationId=game.phase23.Progress.education.currentEducationId;State.sessionCycles=0;State.cycleProgressSeconds=0;State.accountedUtcTicks=DateTime.UtcNow.Ticks;State.stopReason=null;SyncClock();if(!Checkpoint()){StopInternal("시작 상태 저장 실패");Changed?.Invoke();return false;}Changed?.Invoke();return true;}
 public void StopActivity(){if(processing)return;StopInternal("플레이어가 중지했습니다.");Checkpoint();Changed?.Invoke();}
 void StopInternal(string reason){State.active=false;State.stopReason=reason;SyncClock();}
 bool CanCycle(IdleActivityDefinition a,out string reason){
  reason=null;if(game==null||game.State!=GameState.Playing||game.BirthIntroductionPending){reason="정상 플레이 상태에서 시작하세요.";return false;}
  if(!game.LifeMode||a.kind!=IdleActivityKind.Battle&&!game.phase23.IsAvailable){reason="자동 활동에 필요한 육성 모듈이 없습니다.";return false;}
  if(a.maxCycles<=0||double.IsNaN(a.periodSeconds)||double.IsInfinity(a.periodSeconds)||a.periodSeconds<=0||float.IsNaN(a.efficiency)||float.IsInfinity(a.efficiency)||a.efficiency<=0){reason="자동 활동 설정 오류";return false;}
  if(game.growth.Age<a.minAge){reason="요구 나이: "+a.minAge+"세";return false;}
  if(a.kind==IdleActivityKind.Work&&(game.phase23.Jobs.Current==null||game.phase23.Jobs.Current.BaseDailyWage<=0||game.phase23.Jobs.Current.specialUnlock)){reason="유효한 직업이 없습니다.";return false;}
  if(a.kind==IdleActivityKind.Work){var p=game.phase23.Progress.employment;var j=game.phase23.Jobs.Current;long wage=(long)j.BaseDailyWage+(long)p.rank*Math.Max(0,j.PromotionConditions.wageIncrease);if(wage>int.MaxValue-game.economy.Cash||p.experience>int.MaxValue-Math.Max(0,j.ExperienceReward)||p.workCount==int.MaxValue||float.IsNaN(j.AutoWorkEfficiency)||float.IsInfinity(j.AutoWorkEfficiency)){reason="급여 또는 경력 기록 한도";return false;}}
  if(a.kind==IdleActivityKind.Study){var p=game.phase23.Progress.education;var data=game.phase23.catalog.Find(a.actionId);if(data==null||p.experience>int.MaxValue-Math.Max(0,data.experience)||p.studies==int.MaxValue||game.phase23.Education.Current!=null&&game.phase23.Education.Current.studies==int.MaxValue){reason="교육 기록 한도";return false;}}
  if(State.active&&a.kind==IdleActivityKind.Work&&State.jobId!=game.phase23.Progress.employment.currentJobId){reason="고용 상태가 변경되었습니다.";return false;}
  if(State.active&&a.kind==IdleActivityKind.Study&&State.educationId!=game.phase23.Progress.education.currentEducationId){reason="교육 과정이 변경되었습니다.";return false;}
  var spec=a.kind==IdleActivityKind.Battle?new ActionSpec{days=1,core=new StatDelta{health=-2,mental=-1}}:game.actions.Describe(a.actionId);
  if(game.survival.InDanger||game.stats.Hunger-Math.Ceiling(game.balance.survival.hungerLossPerDay*spec.days)+spec.core.hunger<a.minimumHunger||game.stats.Thirst-Math.Ceiling(game.balance.survival.thirstLossPerDay*spec.days)+spec.core.thirst<a.minimumThirst||game.stats.Health+spec.core.health+spec.energy<a.minimumHealth||game.stats.Mental+spec.core.mental-spec.stress<a.minimumMental){reason="생존 자원이 부족해 안전 정지했습니다. 식사·물·수면으로 회복하세요.";return false;}
  if(a.kind==IdleActivityKind.Battle){if(enemy==null||game.growth.Age<enemy.minAge||!game.clock.CanAdvance(1)){reason="전투 조건을 확인하세요.";return false;}if(enemy.gold<0||enemy.gold>int.MaxValue-game.economy.Cash){reason="전투 보상 현금 한도를 초과합니다.";return false;}return true;}
  return game.CanAutomaticAction(a.actionId,out reason);
 }
 IEnumerator Online(){var wait=new WaitForSecondsRealtime(.25f);while(true){yield return wait;if(game==null||settings==null||!State.active||suspended)continue;long now=DateTime.UtcNow.Ticks;if(game.State!=GameState.Playing||!game.clock.IsRunning||Transitioning){State.accountedUtcTicks=Math.Max(State.accountedUtcTicks,now);continue;}SettleToUtc(now,false);}}
 public bool SettleToUtc(long now,bool offline){
  if(processing||settings==null||now<0||now>DateTime.MaxValue.Ticks)return false;
  long from=State.accountedUtcTicks;if(from<=0){State.accountedUtcTicks=now;return true;}if(!State.active){State.accountedUtcTicks=Math.Max(from,now);return true;}
  bool reversed,capped;double seconds=OfflineRewardCalculator.EligibleSeconds(from,now,Math.Max(0,settings.maximumOfflineSeconds),out reversed,out capped);
  if(!offline)seconds=Math.Min(seconds,5);if(offline&&!State.offlineEnabled)seconds=0;
  // A partial online interval changes no economy/model state. Avoid cloning the full save at 4Hz.
  var onlineDefinition=settings.Find(State.activityId);string onlineReason;
  if(!offline&&onlineDefinition!=null&&CanCycle(onlineDefinition,out onlineReason)&&State.cycleProgressSeconds+seconds<Period(onlineDefinition)){
   double previousProgress=State.cycleProgressSeconds;State.cycleProgressSeconds+=seconds;State.accountedUtcTicks=Math.Max(from,now);
   if(Time.realtimeSinceStartupAsDouble-lastCheckpoint>=30&&!Checkpoint()){State.cycleProgressSeconds=previousProgress;State.accountedUtcTicks=from;suspended=true;game.Log("자동 활동 진행 저장 실패. 다시 시도하세요.");Changed?.Invoke();return false;}return true;
  }
  var rollback=game.CaptureState();bool auto=game.autoSaveAfterAction;game.autoSaveAfterAction=false;processing=true;var before=game.stats.Capture();int educationXp=game.phase23.Progress.education.experience;long experience=game.growth.Experience,cash=game.economy.Cash;var report=new OfflineProgressReport{id=Guid.NewGuid().ToString("N"),activityId=State.activityId,fromUtcTicks=from,toUtcTicks=now,elapsedSeconds=now>from?(now-from)/(double)TimeSpan.TicksPerSecond:0,eligibleSeconds=seconds,capped=capped,clockReversed=reversed};
  try{
   var a=settings.Find(State.activityId);string reason=null;if(a==null){StopInternal("활동 데이터가 없습니다.");}else if(!CanCycle(a,out reason)){StopInternal(reason);}else{
    State.cycleProgressSeconds+=seconds;int limit=Math.Min(Math.Max(1,a.maxCycles),Mathf.Clamp(settings.maximumCyclesPerSettlement,1,4096));
    while(State.active&&State.cycleProgressSeconds>=Period(a)&&report.cycles<limit){
     if(!CanCycle(a,out reason)){StopInternal(reason);break;}if(State.sequence==long.MaxValue){StopInternal("정산 기록 한도");break;}
     double cycleSeconds=Period(a);State.cycleProgressSeconds-=cycleSeconds;State.sequence++;string receipt="idle/"+State.sessionId+"/"+State.sequence;bool completed;
     if(a.kind==IdleActivityKind.Battle){var battle=game.ResolveIdleBattle(enemy,receipt);completed=battle!=null;if(battle!=null){State.battles.Add(battle);if(State.battles.Count>128)State.battles.RemoveAt(0);if(battle.won)report.battleWins++;else{report.battleLosses++;StopInternal("전투 패배 또는 체력 부족");}}}
     else{completed=game.TryAutomaticAction(a.actionId);if(completed){if(a.kind==IdleActivityKind.Work)report.workCycles++;else report.studyCycles++;}}
     if(!completed){StopInternal("활동이 중단되어 완료 보상을 지급하지 않았습니다.");break;}report.cycles++;State.sessionCycles++;
     var ledger=State.ledgers.FirstOrDefault(l=>l.activityId==a.id);if(ledger==null){ledger=new IdleActivityLedger{activityId=a.id};State.ledgers.Add(ledger);}if(ledger.completedCycles==long.MaxValue)throw new OverflowException("활동 기록 한도");ledger.completedCycles++;ledger.elapsedSeconds+=cycleSeconds;ledger.lastSettledUtcTicks=now;report.activitySeconds+=cycleSeconds;
     if(State.sessionCycles>=a.maxCycles){StopInternal("설정된 최대 반복 횟수 도달");break;}
     if(!CanCycle(a,out reason))StopInternal(reason);
    }
    if(State.active&&State.cycleProgressSeconds>=Period(a)){StopInternal("한 번의 정산 처리 한도 도달");State.cycleProgressSeconds=0;}
   }
   State.accountedUtcTicks=Math.Max(from,now);report.gold=game.economy.Cash-cash;report.experience=game.growth.Experience-experience;report.educationExperience=game.phase23.Progress.education.experience-educationXp;report.knowledge=game.stats.Knowledge-before.knowledge;report.hunger=game.stats.Hunger-before.hunger;report.thirst=game.stats.Thirst-before.thirst;report.health=game.stats.Health-before.energy;report.mental=game.stats.Mental-before.mental;report.stopReason=State.stopReason;
   if(offline&&(report.elapsedSeconds>0||reversed))MergeReport(report);
   bool commit=offline||report.cycles>0||!State.active||Time.realtimeSinceStartupAsDouble-lastCheckpoint>=30;
   if(commit&&!Checkpoint()){game.RestoreState(rollback);suspended=true;game.Log("자동 활동 저장 실패. 정산 전 상태로 복구했습니다. 다시 시도하세요.");Changed?.Invoke();return false;}
   if(report.cycles>0||offline||!State.active)Changed?.Invoke();if(offline&&State.report!=null&&!State.report.acknowledged)OfflineReportReady?.Invoke();return true;
  }catch(Exception e){game.RestoreState(rollback);suspended=true;game.Log("자동 활동 복구: "+e.Message);Changed?.Invoke();return false;}finally{game.autoSaveAfterAction=auto;processing=false;SyncClock();}
 }
 void MergeReport(OfflineProgressReport r){var old=State.report;if(old==null||old.acknowledged){State.report=r;return;}checked{old.toUtcTicks=r.toUtcTicks;old.elapsedSeconds+=r.elapsedSeconds;old.eligibleSeconds+=r.eligibleSeconds;old.activitySeconds+=r.activitySeconds;old.gold+=r.gold;old.experience+=r.experience;old.cycles+=r.cycles;old.workCycles+=r.workCycles;old.studyCycles+=r.studyCycles;old.battleWins+=r.battleWins;old.battleLosses+=r.battleLosses;old.educationExperience+=r.educationExperience;old.knowledge+=r.knowledge;old.hunger+=r.hunger;old.thirst+=r.thirst;old.health+=r.health;old.mental+=r.mental;old.capped|=r.capped;old.clockReversed|=r.clockReversed;old.stopReason=r.stopReason;}}
 public bool AcknowledgeReport(){if(State.report==null||State.report.acknowledged||processing)return false;State.report.acknowledged=true;if(!Checkpoint()){State.report.acknowledged=false;return false;}Changed?.Invoke();return true;}
 public bool FightOnce(){string reason;if(processing||State.active||!CanStart("battle",out reason))return false;var rollback=game.CaptureState();processing=true;try{var result=game.ResolveIdleBattle(enemy,"battle/"+Guid.NewGuid().ToString("N"));if(result==null)return false;State.battles.Add(result);if(State.battles.Count>128)State.battles.RemoveAt(0);if(!Checkpoint()){game.RestoreState(rollback);suspended=true;return false;}Changed?.Invoke();return true;}finally{processing=false;}}
 public void RetryPersistence(){suspended=false;Changed?.Invoke();}
 public bool IsPersistenceSuspended=>suspended;
 public void SetOfflineEnabled(bool value){bool old=State.offlineEnabled;State.offlineEnabled=value;if(!Checkpoint())State.offlineEnabled=old;Changed?.Invoke();}
 bool Checkpoint(){string message;bool saved=game.SaveAutomaticCheckpoint(out message);if(saved)lastCheckpoint=Time.realtimeSinceStartupAsDouble;return saved;}
 bool Transitioning=>GameContext.Instance!=null&&GameContext.Instance.transitions.IsTransitioning;
 public void PrepareBackground(){if(State.active&&!suspended){if(game.State==GameState.Playing&&game.clock.IsRunning&&!Transitioning)SettleToUtc(DateTime.UtcNow.Ticks,false);else State.accountedUtcTicks=Math.Max(State.accountedUtcTicks,DateTime.UtcNow.Ticks);}suspended=true;}
 public void ResumeBackground(){suspended=false;if(!State.active)return;if(game.State!=GameState.Playing){State.accountedUtcTicks=Math.Max(State.accountedUtcTicks,DateTime.UtcNow.Ticks);return;}if(Transitioning){suspended=true;StartCoroutine(ResumeAfterTransition(State.sessionId));return;}if(!game.clock.IsRunning){State.accountedUtcTicks=Math.Max(State.accountedUtcTicks,DateTime.UtcNow.Ticks);return;}SettleToUtc(DateTime.UtcNow.Ticks,true);}
 IEnumerator ResumeAfterTransition(string session){while(Transitioning)yield return null;if(State.sessionId!=session)yield break;suspended=false;if(State.active){if(game.State==GameState.Playing&&game.clock.IsRunning)SettleToUtc(DateTime.UtcNow.Ticks,true);else State.accountedUtcTicks=Math.Max(State.accountedUtcTicks,DateTime.UtcNow.Ticks);}}
 public void Loaded(bool manual,long oldWatermark){suspended=false;if(manual){State.accountedUtcTicks=Math.Max(Math.Max(State.accountedUtcTicks,oldWatermark),DateTime.UtcNow.Ticks);if(State.active)StopInternal("불러오기로 자동 활동을 중지했습니다. 다시 설정하세요.");SyncClock();}else if(State.active)ResumeBackground();Changed?.Invoke();}
 public static bool Valid(IdleProgressSnapshot s){if(s==null||s.schema!=1||s.sequence<0||s.sessionCycles<0||s.accountedUtcTicks<0||s.accountedUtcTicks>DateTime.MaxValue.Ticks||double.IsNaN(s.cycleProgressSeconds)||double.IsInfinity(s.cycleProgressSeconds)||s.cycleProgressSeconds<0||s.cycleProgressSeconds>1e9||s.ledgers==null||s.battles==null||s.battles.Count>128)return false;if(s.active&&(string.IsNullOrEmpty(s.activityId)||string.IsNullOrEmpty(s.sessionId)))return false;return s.ledgers.All(l=>l!=null&&l.completedCycles>=0&&l.lastSettledUtcTicks>=0&&l.elapsedSeconds>=0&&!double.IsNaN(l.elapsedSeconds)&&!double.IsInfinity(l.elapsedSeconds))&&s.battles.All(b=>b!=null&&!string.IsNullOrEmpty(b.id)&&b.turns!=null&&b.turns.Count<=128&&b.gold>=0&&b.experience>=0);}
}
}
