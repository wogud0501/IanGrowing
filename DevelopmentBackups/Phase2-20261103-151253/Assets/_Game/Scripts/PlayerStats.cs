using System;using UnityEngine;
namespace IanGame {
[Serializable] public class StatSnapshot {public int happiness,stress,energy,money;}
public class PlayerStats:MonoBehaviour {
 [SerializeField,Range(0,100)]int happiness=50,stress=20,energy=70;
 [SerializeField,Min(0)]int money=50;
 public int Happiness=>happiness;public int Stress=>stress;public int Energy=>energy;public int Money=>money;
 public event Action Changed;public event Action<int> HappinessChanged,StressChanged,EnergyChanged,MoneyChanged;
 public StatSnapshot Capture()=>new StatSnapshot{happiness=happiness,stress=stress,energy=energy,money=money};
 public void Restore(StatSnapshot value){if(value==null)return;Set(value.happiness,value.stress,value.energy,value.money);}
 public void Apply(int h,int s,int e,int m){Set((long)happiness+h,(long)stress+s,(long)energy+e,(long)money+m);}
 void Set(long h,long s,long e,long m){
 int nh=(int)Math.Max(0,Math.Min(100,h)),ns=(int)Math.Max(0,Math.Min(100,s)),ne=(int)Math.Max(0,Math.Min(100,e)),nm=(int)Math.Max(0,Math.Min(int.MaxValue,m));
 bool hc=happiness!=nh,sc=stress!=ns,ec=energy!=ne,mc=money!=nm;
 happiness=nh;stress=ns;energy=ne;money=nm;
 if(hc)HappinessChanged?.Invoke(happiness);if(sc)StressChanged?.Invoke(stress);if(ec)EnergyChanged?.Invoke(energy);if(mc)MoneyChanged?.Invoke(money);
 if(hc||sc||ec||mc)Changed?.Invoke();
 }
 public void ResetStats(GameBalance b){if(b!=null)Restore(new StatSnapshot{happiness=b.initialHappiness,stress=b.initialStress,energy=b.initialEnergy,money=b.initialMoney});}
}
}