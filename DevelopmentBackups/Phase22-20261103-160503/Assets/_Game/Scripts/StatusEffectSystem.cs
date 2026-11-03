using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace IanGame {
public class StatusEffectSystem:MonoBehaviour {
 public StatusEffectDefinition[] definitions=new StatusEffectDefinition[0];PlayerStats stats;
 readonly List<ActiveStatusSnapshot> active=new List<ActiveStatusSnapshot>();public event Action Changed;
 public int ActiveCount=>active.Count;
 public string Summary=>string.Join(", ",active.Select(s=>{var d=Find(s.id);return d==null?s.id:d.displayName;}).ToArray());
 public float Efficiency{get{float result=1;foreach(var s in active){var d=Find(s.id);if(d!=null)result=Mathf.Min(result,d.efficiencyMultiplier);}return Mathf.Clamp(result,.05f,1);}}
 StatusEffectDefinition Find(string id)=>definitions==null?null:Array.Find(definitions,d=>d!=null&&d.id==id);
 public void Configure(PlayerStats model){if(stats!=null)stats.Changed-=Refresh;stats=model;if(stats!=null)stats.Changed+=Refresh;}
 void OnDestroy(){if(stats!=null)stats.Changed-=Refresh;}
 public void Refresh(){
 if(stats==null)return;bool changed=false;
 for(int i=active.Count-1;i>=0;i--){var d=Find(active[i].id);if(d==null||stats.Get(d.triggerStat)>=d.recoverAtOrAbove){active.RemoveAt(i);changed=true;}}
 if(definitions!=null)foreach(var d in definitions)if(d!=null&&!string.IsNullOrEmpty(d.id)&&stats.Get(d.triggerStat)<=d.triggerAtOrBelow&&!active.Any(s=>s.id==d.id)){active.Add(new ActiveStatusSnapshot{id=d.id,remainingSeconds=Math.Max(1,d.durationSeconds)});changed=true;}
 if(changed)Changed?.Invoke();
 }
 public void Tick(double seconds){
 if(seconds<=0)return;Refresh();bool changed=false;
 foreach(var value in active.ToArray()){
 var d=Find(value.id);if(d==null)continue;double dt=Math.Min(seconds,value.remainingSeconds);value.remainingSeconds-=dt;
 value.healthRemainder+=dt*d.healthPerDay/TimeManager.SecondsPerDay;value.mentalRemainder+=dt*d.mentalPerDay/TimeManager.SecondsPerDay;
 int h=(int)value.healthRemainder,m=(int)value.mentalRemainder;value.healthRemainder-=h;value.mentalRemainder-=m;
 if(h!=0||m!=0)stats.ApplyCore(new StatDelta{health=h,mental=m});
 if(value.remainingSeconds<=0){active.Remove(value);changed=true;}
 }
 if(changed)Changed?.Invoke();
 }
 public ActiveStatusSnapshot[] Capture()=>active.Select(s=>new ActiveStatusSnapshot{id=s.id,remainingSeconds=s.remainingSeconds,healthRemainder=s.healthRemainder,mentalRemainder=s.mentalRemainder}).ToArray();
 public void Restore(ActiveStatusSnapshot[] values){active.Clear();if(values!=null)foreach(var s in values)if(s!=null&&Find(s.id)!=null)active.Add(new ActiveStatusSnapshot{id=s.id,remainingSeconds=s.remainingSeconds,healthRemainder=s.healthRemainder,mentalRemainder=s.mentalRemainder});Changed?.Invoke();}
 public void Clear(){if(active.Count==0)return;active.Clear();Changed?.Invoke();}
}
}