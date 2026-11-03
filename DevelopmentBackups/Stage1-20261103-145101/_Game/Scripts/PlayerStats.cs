using System; using UnityEngine;
namespace IanGame {
[Serializable] public class StatSnapshot { public int happiness,stress,energy,money; }
public class PlayerStats:MonoBehaviour {
 [SerializeField,Range(0,100)] int happiness=50,stress=20,energy=70,money=50;
 public int Happiness=>happiness; public int Stress=>stress; public int Energy=>energy; public int Money=>money;
 public event Action Changed;
 public StatSnapshot Capture()=>new StatSnapshot{happiness=happiness,stress=stress,energy=energy,money=money};
 public void Restore(StatSnapshot s){ if(s==null)return; happiness=Mathf.Clamp(s.happiness,0,100);stress=Mathf.Clamp(s.stress,0,100);energy=Mathf.Clamp(s.energy,0,100);money=Mathf.Clamp(s.money,0,100);Changed?.Invoke(); }
 public void Apply(int h,int s,int e,int m){ Restore(new StatSnapshot{happiness=happiness+h,stress=stress+s,energy=energy+e,money=money+m}); }
 public void ResetStats(GameBalance b){Restore(new StatSnapshot{happiness=b.initialHappiness,stress=b.initialStress,energy=b.initialEnergy,money=b.initialMoney});}
}
}