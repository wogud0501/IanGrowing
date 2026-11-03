using System; using UnityEngine;
namespace IanGame {
public class TimeManager:MonoBehaviour {
 [SerializeField,Min(1)] int day=1; public int Day=>day; public event Action<int> DayChanged;
 public void AdvanceDay(){SetDay(day+1);} public void SetDay(int value){day=Mathf.Clamp(value,1,999999);DayChanged?.Invoke(day);}
}
}