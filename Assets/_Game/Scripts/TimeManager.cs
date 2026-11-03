using System;using UnityEngine;
namespace IanGame {
public class TimeManager:MonoBehaviour {
 public const int MaximumDay=999999;
 [SerializeField,Min(1)]int day=1;
 public int Day=>day;public bool IsRunning{get;private set;}=true;
 public event Action<int> DayChanged;public event Action<int> DayAdvanced;
 public void SetRunning(bool value){IsRunning=value;}
 public bool CanAdvance(int days)=>IsRunning&&days>0&&(long)day+days<=MaximumDay;
 public void AdvanceDay(){AdvanceDays(1);}
 public bool AdvanceDays(int days){
 if(!CanAdvance(days))return false;
 for(int i=0;i<days;i++){day++;DayChanged?.Invoke(day);DayAdvanced?.Invoke(day);}return true;
 }
 // Restoring a date does not simulate days or roll random events.
 public void SetDay(int value){int next=Mathf.Clamp(value,1,MaximumDay);if(day==next)return;day=next;DayChanged?.Invoke(day);}
}
}