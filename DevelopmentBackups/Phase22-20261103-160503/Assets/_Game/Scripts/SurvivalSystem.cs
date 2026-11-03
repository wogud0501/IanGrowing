using System;using UnityEngine;
namespace IanGame {
[Serializable]public class SurvivalSnapshot {public double hungerRemainder,thirstRemainder,hungerDangerSeconds,thirstDangerSeconds;public bool failed;public ActiveStatusSnapshot[] statuses=new ActiveStatusSnapshot[0];}
public class SurvivalSystem:MonoBehaviour {
 PlayerStats stats;SurvivalSettings settings;StatusEffectSystem statuses;SurvivalSnapshot state=new SurvivalSnapshot();
 public bool Failed=>state.failed;public bool InDanger=>state.hungerDangerSeconds>0||state.thirstDangerSeconds>0||stats!=null&&(stats.Hunger==0||stats.Thirst==0);
 public double HungerDangerSeconds=>state.hungerDangerSeconds;public double ThirstDangerSeconds=>state.thirstDangerSeconds;
 public float Efficiency=>stats==null||settings==null?1:Mathf.Min(stats.Health<=settings.lowHealthThreshold?settings.lowHealthEfficiency:1,statuses==null?1:statuses.Efficiency);
 public float EventChanceMultiplier=>stats!=null&&settings!=null&&stats.Mental<=settings.lowMentalThreshold?settings.lowMentalEventMultiplier:1;
 public event Action Changed;public event Action Failure;
 public void Configure(PlayerStats s,SurvivalSettings data,StatusEffectSystem effects){if(stats!=null)stats.Changed-=OnStats;stats=s;settings=data;statuses=effects;if(stats!=null)stats.Changed+=OnStats;}
 void OnDestroy(){if(stats!=null)stats.Changed-=OnStats;}
 void OnStats(){bool cleared=false;if(stats.Hunger>0&&state.hungerDangerSeconds>0){state.hungerDangerSeconds=0;cleared=true;}if(stats.Thirst>0&&state.thirstDangerSeconds>0){state.thirstDangerSeconds=0;cleared=true;}if(cleared)Changed?.Invoke();}
 public void Tick(double seconds){
 if(settings==null||stats==null||state.failed||seconds<=0)return;
 int beforeBucket=(int)(Math.Max(state.hungerDangerSeconds,state.thirstDangerSeconds)/3600);bool danger=InDanger;
 double hRate=Math.Max(0,settings.hungerLossPerDay)/TimeManager.SecondsPerDay,tRate=Math.Max(0,settings.thirstLossPerDay)/TimeManager.SecondsPerDay;
 double hEmpty=stats.Hunger<=0?0:hRate<=0?double.PositiveInfinity:(stats.Hunger-state.hungerRemainder)/hRate;
 double tEmpty=stats.Thirst<=0?0:tRate<=0?double.PositiveInfinity:(stats.Thirst-state.thirstRemainder)/tRate;
 state.hungerRemainder+=hRate*seconds;state.thirstRemainder+=tRate*seconds;
 int h=(int)Math.Min(100,state.hungerRemainder),t=(int)Math.Min(100,state.thirstRemainder);state.hungerRemainder-=h;state.thirstRemainder-=t;
 if(h!=0||t!=0)stats.ApplyCore(new StatDelta{hunger=-h,thirst=-t});
 if(stats.Hunger==0){state.hungerRemainder=0;state.hungerDangerSeconds+=Math.Max(0,seconds-Math.Max(0,hEmpty));}
 if(stats.Thirst==0){state.thirstRemainder=0;state.thirstDangerSeconds+=Math.Max(0,seconds-Math.Max(0,tEmpty));}
 double grace=Math.Max(1,settings.dangerGraceHours*3600);
 if(state.hungerDangerSeconds>=grace||state.thirstDangerSeconds>=grace){state.failed=true;Changed?.Invoke();Failure?.Invoke();return;}
 statuses?.Tick(seconds);
 if(danger!=InDanger||beforeBucket!=(int)(Math.Max(state.hungerDangerSeconds,state.thirstDangerSeconds)/3600))Changed?.Invoke();
 }
 public SurvivalSnapshot Capture()=>new SurvivalSnapshot{hungerRemainder=state.hungerRemainder,thirstRemainder=state.thirstRemainder,hungerDangerSeconds=state.hungerDangerSeconds,thirstDangerSeconds=state.thirstDangerSeconds,failed=state.failed,statuses=statuses==null?new ActiveStatusSnapshot[0]:statuses.Capture()};
 public void Restore(SurvivalSnapshot value){state=value==null?new SurvivalSnapshot():new SurvivalSnapshot{hungerRemainder=value.hungerRemainder,thirstRemainder=value.thirstRemainder,hungerDangerSeconds=value.hungerDangerSeconds,thirstDangerSeconds=value.thirstDangerSeconds,failed=value.failed};statuses?.Restore(value==null?null:value.statuses);Changed?.Invoke();}
 public void ResetLife(){Restore(new SurvivalSnapshot());}
}
}