using System; using UnityEngine;
namespace IanGame {
public enum GameState { Ready, Resolving }
public class GameManager:MonoBehaviour {
 public GameBalance balance; public PlayerStats stats; public TimeManager clock; public EventManager lifeEvents; public SaveManager saves;
 public GameState State{get;private set;} public event Action<string> Logged; public event Action<int> ActionPerformed;
 void Awake(){
 if(balance==null||stats==null||clock==null||lifeEvents==null||saves==null){Debug.LogError("Ian: 시스템 연결을 확인하세요.",this);enabled=false;return;}
 stats.ResetStats(balance);clock.SetDay(1);State=GameState.Ready;
 }
 public void Work(){Act(0);} public void Rest(){Act(1);} public void Play(){Act(2);}
 void Act(int action){
 if(!enabled||State!=GameState.Ready)return;
 if(clock.Day>=999999){Log("달력이 먼저 은퇴했습니다.");return;}
 int cost=action==1?Mathf.Max(0,-balance.restMoney):action==2?Mathf.Max(0,-balance.playMoney):Mathf.Max(0,-balance.workMoney);
 if(action!=1&&stats.Money<cost){Log("잔액 부족. 행복은 외상을 받지 않습니다.");return;}
 if(action==0&&stats.Energy<Mathf.Max(0,-balance.workEnergy)){Log("출근할 체력이 없습니다. 먼저 쉬어 주세요.");return;}
 State=GameState.Resolving;
 try {
 if(action==0){stats.Apply(0,balance.workStress,balance.workEnergy,balance.workMoney);Log("일했습니다. 통장은 웃고, 이안은 아닙니다.");}
 else if(action==1){stats.Apply(0,balance.restStress,balance.restEnergy,balance.restMoney);Log("쉬었습니다. 휴식에도 생활비가 듭니다.");}
 else{stats.Apply(balance.playHappiness,balance.playStress,balance.playEnergy,balance.playMoney);Log("놀았습니다. 행복 영수증은 보관하세요.");}
 ActionPerformed?.Invoke(action);clock.AdvanceDay();var result=lifeEvents.TryEvent(stats,balance.eventChance);if(result!=null)Log(result);
 }finally{State=GameState.Ready;}
 }
 public void Save(){if(!enabled)return;string m;saves.Save(stats,clock,out m);Log(m);}
 public void Load(){if(!enabled)return;string m;saves.Load(stats,clock,out m);Log(m);}
 public void Log(string text){Logged?.Invoke("DAY "+(clock==null?1:clock.Day)+"  "+text);}
}
}