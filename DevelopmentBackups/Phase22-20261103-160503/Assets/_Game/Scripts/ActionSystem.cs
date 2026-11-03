using UnityEngine;
namespace IanGame {
public struct ActionSpec {public int happiness,stress,energy,money,days;public StatDelta core;public string message;}
public class ActionSystem:MonoBehaviour {
 [SerializeField]GameBalance balance;[SerializeField]PlayerStats stats;[SerializeField]TimeManager clock;[SerializeField]EconomySystem economy;
 LifeProgression growth;SurvivalSystem survival;
 public void Configure(GameBalance b,PlayerStats s,TimeManager t,EconomySystem e){balance=b;stats=s;clock=t;economy=e;}
 public void ConfigureLife(LifeProgression p,SurvivalSystem s){growth=p;survival=s;}
 public ActionSpec Describe(int action){
 if(balance==null)return default(ActionSpec);
 if(action==0)return new ActionSpec{money=balance.workMoney,stress=balance.workStress,energy=balance.workEnergy,days=Mathf.Max(1,balance.workDays),message="일했습니다. 통장은 웃고, 이안은 아닙니다."};
 if(action==1)return new ActionSpec{money=balance.restMoney,stress=balance.restStress,energy=balance.restEnergy,days=Mathf.Max(1,balance.restDays),message="쉬었습니다. 충전 요금은 인생에 청구됩니다."};
 if(action==2)return new ActionSpec{money=balance.playMoney,happiness=balance.playHappiness,stress=balance.playStress,energy=balance.playEnergy,days=Mathf.Max(1,balance.playDays),message="놀았습니다. 행복 영수증은 보관하세요."};
 var cfg=balance.survival;if(cfg==null)return default(ActionSpec);
 var d=new ActionSpec{days=Mathf.Max(1,cfg.careActionDays)};
 if(action==3){d.core=cfg.meal;d.money=-cfg.mealCost;d.message="식사했습니다. 출생 비용에 식비는 포함되지 않았습니다.";}
 else if(action==4){d.core=cfg.drink;d.money=-cfg.drinkCost;d.message="물을 마셨습니다. 프리미엄 수돗물은 다음에.";}
 else if(action==5){d.core=cfg.sleep;d.money=-cfg.sleepCost;d.message="잠을 잤습니다. 현실은 로그아웃되지 않았습니다.";}
 else if(action==6){d.core=cfg.hospital;d.money=-cfg.hospitalCost;d.message="무료 재화 병원에서 가상 피로를 회복했습니다. 유료권은 필요 없습니다.";}
 return d;
 }
 public bool CanPerform(int action,out string reason){
 reason=null;if(balance==null||stats==null||clock==null||economy==null){reason="행동 시스템 연결 누락";return false;}
 if(action<0||action>6||action>2&&balance.survival==null){reason="알 수 없는 행동입니다.";return false;}
 if(action==0&&balance.initialLife!=null&&growth!=null&&!growth.EmploymentUnlocked){reason="취업은 청년기(19세)부터 해금됩니다. 나이는 미션으로 성장합니다.";return false;}
 var d=Describe(action);if(!clock.CanAdvance(d.days)){reason="날짜를 진행할 수 없습니다.";return false;}
 long cost=d.money<0?-(long)d.money:0;if(cost>int.MaxValue||!economy.CanAfford((int)cost)){reason="잔액 부족. 부채로 자동 결제하지 않습니다.";return false;}
 if(d.energy<0&&stats.Health<-(long)d.energy){reason="체력이 부족합니다. 수면이나 병원으로 회복하세요.";return false;}return true;
 }
 internal bool Execute(int action,out string message){
 if(!CanPerform(action,out message))return false;var d=Describe(action);float efficiency=balance.initialLife==null||survival==null?1:survival.Efficiency;
 if(d.money<0){if(!economy.TrySpend((int)(-(long)d.money),"행동 비용"))return false;}else economy.Earn(Mathf.FloorToInt(d.money*efficiency),"행동 수입");
 // Recovery stays fully effective at low health, avoiding a paid recovery dependency.
 stats.Apply(d.happiness>0?Mathf.FloorToInt(d.happiness*efficiency):d.happiness,d.stress,d.energy,0);stats.ApplyCore(d.core);clock.AdvanceDays(d.days);message=d.message;return true;
 }
}
}