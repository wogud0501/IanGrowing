using UnityEngine;
namespace IanGame {
public struct ActionSpec {public int happiness,stress,energy,money,days;public string message;}
public class ActionSystem:MonoBehaviour {
 [SerializeField]GameBalance balance;[SerializeField]PlayerStats stats;[SerializeField]TimeManager clock;[SerializeField]EconomySystem economy;
 public void Configure(GameBalance b,PlayerStats s,TimeManager t,EconomySystem e){balance=b;stats=s;clock=t;economy=e;}
 public ActionSpec Describe(int action){
 if(balance==null)return default(ActionSpec);
 if(action==0)return new ActionSpec{money=balance.workMoney,stress=balance.workStress,energy=balance.workEnergy,days=Mathf.Max(1,balance.workDays),message="일했습니다. 통장은 웃고, 이안은 아닙니다."};
 if(action==1)return new ActionSpec{money=balance.restMoney,stress=balance.restStress,energy=balance.restEnergy,days=Mathf.Max(1,balance.restDays),message="쉬었습니다. 충전 요금은 인생에 청구됩니다."};
 return new ActionSpec{money=balance.playMoney,happiness=balance.playHappiness,stress=balance.playStress,energy=balance.playEnergy,days=Mathf.Max(1,balance.playDays),message="놀았습니다. 행복 영수증은 보관하세요."};
 }
 public bool CanPerform(int action,out string reason){
 reason=null;if(action<0||action>2){reason="알 수 없는 행동입니다.";return false;}
 if(balance==null||stats==null||clock==null||economy==null){reason="행동 시스템 연결 누락";return false;}
 var d=Describe(action);
 if(!clock.CanAdvance(d.days)){reason="날짜를 진행할 수 없습니다.";return false;}
 long cost=d.money<0?-(long)d.money:0;
 if(cost>int.MaxValue||!economy.CanAfford((int)cost)){reason="잔액 부족. 행복은 외상을 받지 않습니다.";return false;}
 if(d.energy<0&&stats.Energy<-(long)d.energy){reason="체력이 부족합니다. 먼저 쉬어 주세요.";return false;}
 return true;
 }
 // Only GameManager in the runtime assembly executes an action; it owns the state gate.
 internal bool Execute(int action,out string message){
 if(!CanPerform(action,out message))return false;var d=Describe(action);
 if(d.money<0){if(!economy.TrySpend((int)(-(long)d.money),"행동 비용"))return false;}
 else economy.Earn(d.money,"행동 수입");
 stats.Apply(d.happiness,d.stress,d.energy,0);
 clock.AdvanceDays(d.days);message=d.message;return true;
 }
}
}