using System;using UnityEngine;
namespace IanGame {
public class EconomySystem:MonoBehaviour {
 [SerializeField]PlayerStats stats;public PlayerStats Stats=>stats;
 public event Action<int,int,string> TransactionCompleted;
 public void Configure(PlayerStats model){stats=model;}
 public bool CanAfford(int cost)=>stats!=null&&cost>=0&&stats.Money>=cost;
 public bool TrySpend(int cost,string reason="지출"){
 if(!CanAfford(cost))return false;int before=stats.Money;stats.Apply(0,0,0,-cost);if(cost!=0)TransactionCompleted?.Invoke(before,stats.Money,reason);return true;
 }
 public bool Earn(int amount,string reason="수입"){if(stats==null||amount<0)return false;ApplyDelta(amount,reason);return true;}
 // Mandatory event losses are limited to the available balance. No debt.
 public void ApplyDelta(int delta,string reason="인생 이벤트"){if(stats==null)return;int before=stats.Money;stats.Apply(0,0,0,delta);if(before!=stats.Money)TransactionCompleted?.Invoke(before,stats.Money,reason);}
}
}