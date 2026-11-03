using System;using UnityEngine;
namespace IanGame {
public enum GameState {Starting,Playing,Paused,GameOver,Resolving,Ready=Playing}
public class GameManager:MonoBehaviour {
 public GameBalance balance;public PlayerStats stats;public TimeManager clock;public EventManager lifeEvents;public SaveManager saves;
 public ActionSystem actions;public EconomySystem economy;
 public bool autoLoadOnStart=true,autoSaveAfterAction=true;
 public GameState State{get;private set;}=GameState.Starting;
 public event Action<string> Logged;public event Action<int> ActionPerformed;public event Action<GameState> StateChanged;
 void Awake(){
 if(balance==null||stats==null||clock==null||lifeEvents==null||saves==null){Debug.LogError("Ian: 시스템 연결을 확인하세요.",this);enabled=false;return;}
 if(economy==null)economy=GetComponent<EconomySystem>();if(economy==null)economy=gameObject.AddComponent<EconomySystem>();
 if(actions==null)actions=GetComponent<ActionSystem>();if(actions==null)actions=gameObject.AddComponent<ActionSystem>();
 economy.Configure(stats);actions.Configure(balance,stats,clock,economy);lifeEvents.Configure(economy);clock.SetRunning(false);clock.DayAdvanced+=OnDay;
 }
 void Start(){if(enabled)StartGame();}
 void OnDestroy(){if(clock!=null)clock.DayAdvanced-=OnDay;}
 public void StartGame(){
 if(!enabled||State==GameState.Resolving)return;
 SetState(GameState.Starting);ResetModel();
 if(autoLoadOnStart)RestoreSave(false);else{SetState(GameState.Playing);Log("이안이 입주했습니다. 행복은 별도 구매입니다.");}
 }
 void ResetModel(){stats.ResetStats(balance);clock.SetDay(1);}
 public void NewGame(){
 if(!enabled||State==GameState.Resolving)return;
 SetState(GameState.Starting);ResetModel();SetState(GameState.Playing);Log("새 인생을 시작했습니다. 약관은 같습니다.");SaveQuietly();
 }
 void SetState(GameState state){if(clock!=null)clock.SetRunning(state==GameState.Playing||state==GameState.Resolving);if(State==state)return;State=state;StateChanged?.Invoke(state);}
 public void Pause(){if(State==GameState.Playing){SetState(GameState.Paused);Log("일시정지. 청구서는 기다려 주지 않지만 게임은 기다립니다.");}}
 public void Resume(){if(State==GameState.Paused)SetState(GameState.Playing);}
 public void TogglePause(){if(State==GameState.Paused)Resume();else Pause();}
 public void EndGame(){if(State==GameState.GameOver)return;SetState(GameState.GameOver);Log("번아웃입니다. 새 인생 버튼에서 다시 시작할 수 있습니다.");}
 public bool CanPerformAction(int action,out string reason){
 reason=null;if(!enabled||State!=GameState.Playing){reason=State==GameState.Paused?"일시정지 중입니다.":State==GameState.GameOver?"게임오버입니다. 새 인생을 시작해 주세요.":"게임을 준비하거나 행동을 처리하고 있습니다.";return false;}
 if(actions==null){reason="행동 시스템 연결 누락";return false;}return actions.CanPerform(action,out reason);
 }
 public void Work(){Act(0);}public void Rest(){Act(1);}public void Play(){Act(2);}
 public bool TryAction(int action){return Act(action);}
 bool Act(int action){
 string message;if(!CanPerformAction(action,out message)){Log(message);return false;}
 SetState(GameState.Resolving);bool completed=false;
 try{completed=actions.Execute(action,out message);if(completed){Log(message);ActionPerformed?.Invoke(action);}else Log(message);}
 finally{if(balance.IsGameOver(stats))EndGame();else SetState(GameState.Playing);}
 if(completed&&autoSaveAfterAction)SaveQuietly();return completed;
 }
 void OnDay(int day){var result=lifeEvents.TryEvent(stats,balance.eventChance);if(result!=null)Log(result);}
 void SaveQuietly(){string message;if(!saves.Save(stats,clock,State==GameState.GameOver,out message))Log(message);}
 public void Save(){if(!enabled||State==GameState.Starting||State==GameState.Resolving)return;string m;saves.Save(stats,clock,State==GameState.GameOver,out m);Log(m);}
 public void Load(){if(enabled&&State!=GameState.Resolving)RestoreSave(true);}
 void RestoreSave(bool preserveIfMissing){
 SaveData data;string message;
 if(saves.TryRead(out data,out message)){
 SetState(GameState.Starting);stats.Restore(data.stats);clock.SetDay(data.day);SetState(data.gameOver||balance.IsGameOver(stats)?GameState.GameOver:GameState.Playing);Log(message);return;
 }
 if(saves.LastLoadStatus==SaveLoadStatus.Corrupt){SetState(GameState.Starting);ResetModel();SetState(GameState.Playing);Log(message+" / 원본을 보존하고 안전하게 초기화했습니다.");return;}
 if(!preserveIfMissing)SetState(GameState.Playing);Log(message);
 }
 public void Log(string text){if(!string.IsNullOrEmpty(text))Logged?.Invoke("DAY "+(clock==null?1:clock.Day)+"  "+text);}
}
}