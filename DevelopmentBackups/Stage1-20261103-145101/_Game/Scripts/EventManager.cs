using System; using UnityEngine;
namespace IanGame {
[Serializable] public class LifeEvent { public string title; [TextArea] public string description; public int happiness,stress,energy,money; }
public class EventManager:MonoBehaviour {
 public LifeEvent[] events={
 new LifeEvent{title="숨 쉬기 구독",description="무료 체험이 종료되었습니다. 공기는 아직 무료입니다.",money=-6,stress=5},
 new LifeEvent{title="칭찬 적립",description="팀장이 칭찬했습니다. 급여 대신입니다.",happiness=5,stress=7},
 new LifeEvent{title="길에서 주운 행운",description="동전을 주웠습니다. 행복 구매에는 조금 부족합니다.",money=8,happiness=4},
 new LifeEvent{title="강제 휴식",description="몸이 파업했습니다. 노조는 없지만 협상은 합니다.",energy=12,money=-5},
 new LifeEvent{title="알고리즘의 위로",description="당신보다 힘든 사람 영상을 추천받았습니다.",happiness=-5,stress=8}
 };
 public string TryEvent(PlayerStats stats,float chance){
 if(stats==null||events==null||events.Length==0||UnityEngine.Random.value>=Mathf.Clamp01(chance))return null;
 var e=events[UnityEngine.Random.Range(0,events.Length)]; if(e==null)return null;
 stats.Apply(e.happiness,e.stress,e.energy,e.money);return e.title+" — "+e.description;
 }
}
}