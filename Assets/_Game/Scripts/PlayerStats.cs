using System;using UnityEngine;
namespace IanGame {
public enum CoreStat {Hunger,Thirst,Knowledge,Health,Mental,Social,Credit,Happiness}
[Serializable]public struct StatDelta {public int hunger,thirst,knowledge,health,mental,social,credit,happiness;}
[Serializable]public class StatSnapshot {
 public int happiness,stress,energy,money;
 public int hunger=100,thirst=100,knowledge,mental=-1,social,credit=50;
}
public class PlayerStats:MonoBehaviour {
 [SerializeField,Range(0,100)]int happiness=50,stress=20,energy=70,hunger=100,thirst=100,knowledge,mental=80,social,credit=50;
 [SerializeField,Min(0)]int money=50;
 public int Hunger=>hunger;public int Thirst=>thirst;public int Knowledge=>knowledge;public int Health=>energy;public int Energy=>energy;public int Mental=>mental;public int Stress=>100-mental;public int Social=>social;public int Credit=>credit;public int Happiness=>happiness;public int Money=>money;
 public event Action Changed;public event Action<CoreStat,int> StatChanged;
 public event Action<int> HungerChanged,ThirstChanged,KnowledgeChanged,HealthChanged,MentalChanged,SocialChanged,CreditChanged,HappinessChanged,StressChanged,EnergyChanged,MoneyChanged;
 public int Get(CoreStat stat){switch(stat){case CoreStat.Hunger:return Hunger;case CoreStat.Thirst:return Thirst;case CoreStat.Knowledge:return Knowledge;case CoreStat.Health:return Health;case CoreStat.Mental:return Mental;case CoreStat.Social:return Social;case CoreStat.Credit:return Credit;default:return Happiness;}}
 public StatSnapshot Capture()=>new StatSnapshot{happiness=happiness,stress=Stress,energy=energy,money=money,hunger=hunger,thirst=thirst,knowledge=knowledge,mental=mental,social=social,credit=credit};
 static int Limit(long value,int max=100)=>(int)Math.Max(0,Math.Min(max,value));
 public void Restore(StatSnapshot v){
 if(v==null)return;var old=Capture();hunger=Limit(v.hunger);thirst=Limit(v.thirst);knowledge=Limit(v.knowledge);energy=Limit(v.energy);mental=Limit(v.mental<0?100L-v.stress:v.mental);stress=100-mental;social=Limit(v.social);credit=Limit(v.credit);happiness=Limit(v.happiness);money=Limit(v.money,int.MaxValue);
 bool changed=false;
 changed|=Raise(CoreStat.Hunger,old.hunger,hunger,HungerChanged);changed|=Raise(CoreStat.Thirst,old.thirst,thirst,ThirstChanged);changed|=Raise(CoreStat.Knowledge,old.knowledge,knowledge,KnowledgeChanged);changed|=Raise(CoreStat.Health,old.energy,energy,HealthChanged);changed|=Raise(CoreStat.Mental,old.mental,mental,MentalChanged);changed|=Raise(CoreStat.Social,old.social,social,SocialChanged);changed|=Raise(CoreStat.Credit,old.credit,credit,CreditChanged);changed|=Raise(CoreStat.Happiness,old.happiness,happiness,HappinessChanged);
 if(old.energy!=energy)EnergyChanged?.Invoke(energy);if(old.stress!=Stress)StressChanged?.Invoke(Stress);if(old.money!=money){MoneyChanged?.Invoke(money);changed=true;}if(changed)Changed?.Invoke();
 }
 bool Raise(CoreStat stat,int old,int value,Action<int> handler){if(old==value)return false;handler?.Invoke(value);StatChanged?.Invoke(stat,value);return true;}
 public void Apply(int h,int s,int e,int m){var v=Capture();v.happiness=Limit((long)happiness+h);v.mental=Limit((long)mental-s);v.energy=Limit((long)energy+e);v.money=Limit((long)money+m,int.MaxValue);Restore(v);}
 public void ApplyCore(StatDelta d){var v=Capture();v.hunger=Limit((long)hunger+d.hunger);v.thirst=Limit((long)thirst+d.thirst);v.knowledge=Limit((long)knowledge+d.knowledge);v.energy=Limit((long)energy+d.health);v.mental=Limit((long)mental+d.mental);v.social=Limit((long)social+d.social);v.credit=Limit((long)credit+d.credit);v.happiness=Limit((long)happiness+d.happiness);Restore(v);}
 public void ResetStats(GameBalance b){if(b!=null)Restore(new StatSnapshot{happiness=b.initialHappiness,stress=b.initialStress,energy=b.initialEnergy,money=b.initialMoney});}
 public void ResetLife(InitialLifeSettings settings){if(settings!=null)Restore(settings.NewStats());}
}
}