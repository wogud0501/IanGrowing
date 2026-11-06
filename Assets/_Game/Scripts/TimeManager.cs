using System;using UnityEngine;
namespace IanGame {
[Serializable]public class TimeSnapshot {public double subDaySeconds,elapsedSeconds;}
public class TimeManager:MonoBehaviour {
 public const int MaximumDay=999999;public const double SecondsPerDay=86400;
 [SerializeField,Min(1)]int day=1;double subDay,elapsed;public bool realtimeEnabled;
 bool skipRealtime;public void SkipNextRealtimeTick(){skipRealtime=true;}
 public float realSecondsPerGameDay=300;public int Day=>day;public double SubDaySeconds=>subDay;public double ElapsedSeconds=>elapsed;public double LastOfflineSeconds{get;private set;}
 public bool RealtimeSuppressed{get;set;}
 public bool IsRunning{get;private set;}=true;
 public event Action<int> DayChanged,DayAdvanced;public event Action<double> SimulationAdvanced;
 public void SetRunning(bool value){IsRunning=value;}
 void Update(){if(skipRealtime){skipRealtime=false;return;}if(realtimeEnabled&&!RealtimeSuppressed&&IsRunning&&realSecondsPerGameDay>0)AdvanceSimulation(Math.Min(.25,Time.unscaledDeltaTime)*SecondsPerDay/realSecondsPerGameDay);}
 public bool CanAdvance(int days)=>IsRunning&&days>0&&(long)day+days<=MaximumDay;
 public void AdvanceDay(){AdvanceDays(1);}
 public bool AdvanceDays(int days){if(!CanAdvance(days))return false;AdvanceSimulation(days*SecondsPerDay);return true;}
 public void AdvanceSimulation(double seconds){
 if(!IsRunning||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;
 seconds=Math.Min(seconds,(MaximumDay-day)*SecondsPerDay-subDay);
 while(seconds>0&&IsRunning){double step=Math.Min(seconds,SecondsPerDay-subDay);if(step<=0)break;subDay+=step;elapsed+=step;seconds-=step;SimulationAdvanced?.Invoke(step);
 if(subDay>=SecondsPerDay-.000001){subDay=0;day++;DayChanged?.Invoke(day);DayAdvanced?.Invoke(day);}}
 }
 public void SetDay(int value){int next=Mathf.Clamp(value,1,MaximumDay);if(day==next)return;day=next;DayChanged?.Invoke(day);}
 public TimeSnapshot Capture()=>new TimeSnapshot{subDaySeconds=subDay,elapsedSeconds=elapsed};
 public void Restore(int value,TimeSnapshot snapshot){day=Mathf.Clamp(value,1,MaximumDay);subDay=snapshot==null?0:Math.Max(0,Math.Min(SecondsPerDay-.000001,snapshot.subDaySeconds));elapsed=snapshot==null?0:Math.Max(0,snapshot.elapsedSeconds);DayChanged?.Invoke(day);}
 public static double SafeOfflineSeconds(long lastUtcTicks,long nowUtcTicks,double cap){
 if(lastUtcTicks<=0||nowUtcTicks<=lastUtcTicks||cap<=0||double.IsNaN(cap)||double.IsInfinity(cap))return 0;
 return Math.Min(cap,(nowUtcTicks-lastUtcTicks)/(double)TimeSpan.TicksPerSecond);
 }
 public void RecordConnection(long previousUtcTicks,double cap){LastOfflineSeconds=SafeOfflineSeconds(previousUtcTicks,DateTime.UtcNow.Ticks,cap);}
}
}
