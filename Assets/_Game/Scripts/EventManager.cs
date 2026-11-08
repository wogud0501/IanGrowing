using System;using UnityEngine;
namespace IanGame {
public class EventManager:MonoBehaviour {
 public event Action<EconomicNewsRecord> EconomicNewsApplied;
 public void PublishEconomicNews(EconomicNewsRecord news){EconomicNewsApplied?.Invoke(news);}
 public LifeEventCatalog catalog;
 [Tooltip("Legacy scene data retained for compatibility; a catalog takes precedence.")]public LifeEvent[] events=new LifeEvent[0];
 [SerializeField]EconomySystem economy;
 public event Action<LifeEvent> EventApplied;
 public void Configure(EconomySystem model){economy=model;}
 public LifeEvent[] Entries=>catalog!=null?catalog.events:events;
 static bool Eligible(LifeEvent e)=>e!=null&&e.weight>0&&!float.IsNaN(e.weight)&&!float.IsInfinity(e.weight);
 public LifeEvent Select(float sample){
 var entries=Entries;if(entries==null)return null;double total=0;foreach(var e in entries)if(Eligible(e))total+=e.weight;
 if(total<=0)return null;double target=Mathf.Clamp01(float.IsNaN(sample)?0:sample)*total;double sum=0;LifeEvent last=null;
 foreach(var e in entries)if(Eligible(e)){last=e;sum+=e.weight;if(target<sum)return e;}return last;
 }
 public string TryEvent(PlayerStats stats,float chance){
 if(stats==null||float.IsNaN(chance)||chance<=0)return null;
 if(UnityEngine.Random.value>=Mathf.Clamp01(chance))return null;
 var e=Select(UnityEngine.Random.value);if(e==null)return null;
 stats.Apply(e.happiness,e.stress,e.energy,0);
 if(economy!=null&&economy.Stats==stats)economy.ApplyDelta(e.money,e.title);else stats.Apply(0,0,0,e.money);
 EventApplied?.Invoke(e);return e.title+" — "+e.description;
 }
}
}
