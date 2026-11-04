using System;using UnityEngine;
namespace IanGame {
public enum GameState {Starting,Playing,Paused,GameOver,Resolving,Ready=Playing}
public class GameManager:MonoBehaviour {
 public GameBalance balance;public PlayerStats stats;public TimeManager clock;public EventManager lifeEvents;public SaveManager saves;
 public Phase23Services phase23;public MissionService missions;public MissionCatalog missionCatalog;public ActivityCatalog activityCatalog;
 bool missionCheckpointPending;
 public ActionSystem actions;public EconomySystem economy;public LifeProgression growth;public SurvivalSystem survival;public StatusEffectSystem statuses;
 public bool autoLoadOnStart=true,autoSaveAfterAction=true,autoSaveOnLifecycle=true,realtimeLifeEnabled=true;
 public GameState State{get;private set;}=GameState.Starting;public bool LifeMode=>balance!=null&&balance.initialLife!=null&&balance.survival!=null;
 readonly System.Collections.Generic.Queue<string> recentLogs=new System.Collections.Generic.Queue<string>();public string[] RecentLogs=>recentLogs.ToArray();
 string runId;int settledDay=1;bool checkpointPending,appSuspended,wasRealtime;long connectionStamp;
 public event Action<string> Logged;public event Action<int> ActionPerformed;public event Action<GameState> StateChanged;
 T Module<T>(T component)where T:Component {if(component!=null)return component;var found=GetComponent<T>();return found!=null?found:gameObject.AddComponent<T>();}
 void Awake(){
 if(balance==null||stats==null||clock==null||lifeEvents==null||saves==null){Debug.LogError("Ian: 시스템 연결을 확인하세요.",this);enabled=false;return;}
 missions=Module(missions);missions.catalog=missionCatalog;economy=Module(economy);actions=Module(actions);growth=Module(growth);survival=Module(survival);statuses=Module(statuses);
 economy.Configure(stats);statuses.Configure(stats);survival.Configure(stats,LifeMode?balance.survival:null,statuses);actions.Configure(balance,stats,clock,economy);actions.ConfigureLife(growth,survival);lifeEvents.Configure(economy);saves.Configure(growth,economy,survival);
 missions.Configure(this);actions.ConfigureEducation(activityCatalog,missions,()=>GameContext.Instance==null||GameContext.Instance.transitions.CurrentScene==SceneId.School);saves.ConfigureMissions(missions);phase23=Module(phase23);phase23.Configure(this);actions.ConfigurePhase23(phase23);saves.ConfigurePhase23(phase23);missions.Completed+=OnMissionCompleted;missions.CheckpointRequested+=OnMissionCheckpoint;
 clock.SetRunning(false);clock.DayAdvanced+=OnDay;clock.SimulationAdvanced+=OnSimulation;survival.Failure+=OnFailure;
 }
 void Start(){if(enabled)StartGame();}
 void OnMissionCompleted(MissionData data){Log(data.completionMessage);}
 void OnMissionCheckpoint(){missionCheckpointPending=true;}
 void OnDestroy(){if(missions!=null){missions.Completed-=OnMissionCompleted;missions.CheckpointRequested-=OnMissionCheckpoint;}if(clock!=null){clock.DayAdvanced-=OnDay;clock.SimulationAdvanced-=OnSimulation;}if(survival!=null)survival.Failure-=OnFailure;}
 void ConfigureMode(){clock.realtimeEnabled=LifeMode&&realtimeLifeEnabled;if(LifeMode)clock.realSecondsPerGameDay=Math.Max(1,balance.survival.realSecondsPerGameDay);survival.Configure(stats,LifeMode?balance.survival:null,statuses);}
 void ResetModel(){
 recentLogs.Clear();phase23.ResetLife();missions.BeginRestore();ConfigureMode();if(LifeMode)stats.ResetLife(balance.initialLife);else stats.ResetStats(balance);
 economy.ResetLife(LifeMode);growth.Restore(new ProgressSnapshot{age=LifeMode?0:19});survival.ResetLife();clock.Restore(1,new TimeSnapshot());runId=Guid.NewGuid().ToString("N");settledDay=1;connectionStamp=DateTime.UtcNow.Ticks;missions.ResetLife(1);missions.EndRestore();missionCheckpointPending=false;
 }
 public void StartGame(){
 if(!enabled||State==GameState.Resolving)return;SetState(GameState.Starting);ResetModel();
 if(autoLoadOnStart)RestoreSave(false);else{SetState(GameState.Playing);BirthMessage();}
 }
 void BirthMessage(){Log(LifeMode?"축하합니다! 이안이 태어났습니다. 0세 / 현금 0G / 부채 1,000,000G / 사회적 지위 없음. 태어난 것만으로 비용이 발생합니다.":"이안이 입주했습니다. 행복은 별도 구매입니다.");}
 public void NewGame(){if(!enabled||State==GameState.Resolving)return;SetState(GameState.Starting);ResetModel();saves.AllowExplicitNewGame();SetState(GameState.Playing);BirthMessage();SaveQuietly();}
 void SetState(GameState state){if(clock!=null)clock.SetRunning(state==GameState.Playing||state==GameState.Resolving);if(State==state)return;State=state;StateChanged?.Invoke(state);if(missions!=null)missions.Refresh();}
 public void RefreshAvailability(){StateChanged?.Invoke(State);}
 public void Pause(){if(State==GameState.Playing){SetState(GameState.Paused);Log("일시정지. 부채는 그대로 기다리고 있습니다.");}}
 public void Resume(){if(State==GameState.Paused)SetState(GameState.Playing);}
 public void TogglePause(){if(State==GameState.Paused)Resume();else Pause();}
 public void EndGame(){if(State==GameState.GameOver)return;SetState(GameState.GameOver);Log(LifeMode?"생존 위험이 유예시간 동안 지속되어 생존에 실패했습니다.":"번아웃입니다. 새 인생에서 다시 시작할 수 있습니다.");}
 void OnFailure(){EndGame();checkpointPending=true;}
 void LateUpdate(){if(missionCheckpointPending&&missions!=null&&!missions.IsBusy&&State!=GameState.Starting&&State!=GameState.Resolving){missionCheckpointPending=false;if(autoSaveAfterAction)SaveQuietly();}if(checkpointPending&&State==GameState.GameOver){checkpointPending=false;if(autoSaveAfterAction)SaveQuietly();}}
 public bool CanPerformAction(int action,out string reason){
 reason=null;if(!enabled||State!=GameState.Playing){reason=State==GameState.Paused?"일시정지 중입니다.":State==GameState.GameOver?"생존 종료 상태입니다.":"행동 처리 또는 준비 중입니다.";return false;}
 if(actions==null){reason="행동 시스템 연결 누락";return false;}return actions.CanPerform(action,out reason);
 }
 public void Work(){Act(0);}public void Rest(){Act(1);}public void Play(){Act(2);}public void Eat(){Act(3);}public void Drink(){Act(4);}public void Sleep(){Act(5);}public void VisitHospital(){Act(6);}
 public bool TryAction(int action)=>Act(action);
 bool Act(int action){
 string message;if(!CanPerformAction(action,out message)){Log(message);return false;}SetState(GameState.Resolving);bool completed=false;
 try{completed=actions.Execute(action,out message);if(completed){Log(message);if(missions!=null&&(phase23==null||!phase23.Handles(action)))missions.RecordAction(missions.NextEventId(runId),action);ActionPerformed?.Invoke(action);}else Log(message);}
 finally{if(State!=GameState.GameOver){if(LifeMode?survival.Failed:balance.IsGameOver(stats))EndGame();else SetState(GameState.Playing);}}
 if(completed&&autoSaveAfterAction){checkpointPending=false;SaveQuietly();}return completed;
 }
 void OnSimulation(double seconds){if(LifeMode&&(State==GameState.Playing||State==GameState.Resolving))survival.Tick(seconds);}
 void OnDay(int day){
 if(day<=settledDay)return;settledDay=day;if(State!=GameState.Playing&&State!=GameState.Resolving)return;
 float chance=balance.eventChance*(LifeMode?survival.EventChanceMultiplier:1);var result=lifeEvents.TryEvent(stats,chance);if(result!=null)Log(result);
 }
 void SaveQuietly(){if(saves==null||State==GameState.Starting||missions!=null&&missions.IsBusy)return;missionCheckpointPending=false;string message;if(!saves.SaveAutomatic(stats,clock,State==GameState.GameOver,runId,settledDay,out message))Log(message);else connectionStamp=saves.LastSavedUtcTicks;}
 public void Save(){if(!enabled||State==GameState.Starting||State==GameState.Resolving||missions!=null&&missions.IsBusy)return;string message;if(saves.Save(stats,clock,State==GameState.GameOver,runId,settledDay,out message)){connectionStamp=saves.LastSavedUtcTicks;SaveQuietly();}Log(message);}
 public void Load(){if(enabled&&State!=GameState.Resolving)RestoreSave(true);}
 void RestoreSave(bool preserve){
 SaveData data=null;string message=null;
 bool loaded=!preserve&&saves.TryReadAutomatic(out data,out message);
 if(!loaded)loaded=saves.TryRead(out data,out message);
 if(loaded){
 SetState(GameState.Starting);saves.ApplyToModels(data,stats,clock);runId=data.runId;settledDay=data.settledDay;connectionStamp=data.lastSeenUtcTicks;
 clock.RecordConnection(data.lastSeenUtcTicks,LifeMode?balance.survival.maxOfflineInspectionSeconds:28800);
 SetState(data.gameOver||survival.Failed?GameState.GameOver:GameState.Playing);if(preserve)SaveQuietly();Log(message);return;
 }
 if(!preserve){SetState(GameState.Playing);if(saves.LastLoadStatus==SaveLoadStatus.Missing)BirthMessage();else Log(message+" / 저장을 보호하는 임시 세션입니다.");}else Log(message);
 }
 void OnApplicationPause(bool paused){
 if(appSuspended==paused)return;appSuspended=paused;
 if(paused){wasRealtime=clock!=null&&clock.realtimeEnabled;if(clock!=null)clock.realtimeEnabled=false;if(autoSaveOnLifecycle&&enabled&&State!=GameState.Starting&&State!=GameState.Resolving)SaveQuietly();}
 else if(clock!=null){clock.RecordConnection(connectionStamp,LifeMode?balance.survival.maxOfflineInspectionSeconds:28800);clock.SkipNextRealtimeTick();clock.realtimeEnabled=wasRealtime;}
 }
 void OnApplicationQuit(){if(autoSaveOnLifecycle&&enabled&&State!=GameState.Starting&&State!=GameState.Resolving)SaveQuietly();}
 public void Log(string text){if(string.IsNullOrEmpty(text))return;string entry="DAY "+(clock==null?1:clock.Day)+"  "+text;recentLogs.Enqueue(entry);while(recentLogs.Count>20)recentLogs.Dequeue();Logged?.Invoke(entry);}
}
}