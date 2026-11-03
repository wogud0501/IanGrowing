using System;using System.Collections.Generic;using UnityEngine;
namespace IanGame {
public enum TransactionKind {Income,Expense,DebtIncrease,DebtRepayment,AssetValuation}
[Serializable]public class EconomySnapshot {public long debt,otherAssets;public List<string> processedIds=new List<string>();}
public class EconomySystem:MonoBehaviour {
 [SerializeField]PlayerStats stats;EconomySnapshot state=new EconomySnapshot();bool busy;
 public PlayerStats Stats=>stats;public long Cash=>stats==null?0:stats.Money;public long Debt=>state.debt;public long OtherAssets=>state.otherAssets;
 public long TotalAssets=>Cash>long.MaxValue-state.otherAssets?long.MaxValue:Cash+state.otherAssets;
 public long NetWorth=>TotalAssets-Debt;
 public event Action<int,int,string> TransactionCompleted;public event Action Changed;
 public void Configure(PlayerStats model){if(stats!=null)stats.MoneyChanged-=OnCash;stats=model;if(stats!=null)stats.MoneyChanged+=OnCash;}
 void OnDestroy(){if(stats!=null)stats.MoneyChanged-=OnCash;}
 void OnCash(int value){Changed?.Invoke();}
 public EconomySnapshot Capture()=>new EconomySnapshot{debt=Debt,otherAssets=OtherAssets,processedIds=new List<string>(state.processedIds)};
 public void Restore(EconomySnapshot value){if(value==null)return;state=new EconomySnapshot{debt=Math.Max(0,value.debt),otherAssets=Math.Max(0,value.otherAssets),processedIds=value.processedIds==null?new List<string>():new List<string>(value.processedIds)};Changed?.Invoke();}
 public void ResetLife(bool newborn){Restore(new EconomySnapshot{debt=newborn?InitialLifeSettings.NewbornDebt:0});}
 public bool CanAfford(int cost)=>stats!=null&&cost>=0&&stats.Money>=cost;
 public bool TrySpend(int cost,string reason="지출"){if(busy||!CanAfford(cost))return false;CommitCash(-cost,reason);return true;}
 public bool Earn(int amount,string reason="수입"){if(busy||stats==null||amount<0)return false;CommitCash(amount,reason);return true;}
 void CommitCash(int delta,string reason){busy=true;try{int before=stats.Money;stats.Apply(0,0,0,delta);if(before!=stats.Money)TransactionCompleted?.Invoke(before,stats.Money,reason);}finally{busy=false;}}
 public void ApplyDelta(int delta,string reason="인생 이벤트"){if(stats==null||busy)return;CommitCash(delta,reason);}
 public bool TryTransaction(string id,TransactionKind kind,long amount,string reason){
 if(stats==null||busy||string.IsNullOrWhiteSpace(id)||amount<0||state.processedIds.Contains(id))return false;
 if((kind==TransactionKind.Income&&(amount>int.MaxValue-stats.Money))||(kind==TransactionKind.Expense&&amount>stats.Money)||(kind==TransactionKind.DebtIncrease&&amount>long.MaxValue-Debt)||(kind==TransactionKind.DebtRepayment&&(amount>stats.Money||amount>Debt)))return false;
 busy=true;try{
 // Record before notifying listeners to reject re-entrant duplicate claims.
 state.processedIds.Add(id);int before=stats.Money;
 switch(kind){case TransactionKind.Income:stats.Apply(0,0,0,(int)amount);break;case TransactionKind.Expense:stats.Apply(0,0,0,-(int)amount);break;case TransactionKind.DebtIncrease:state.debt+=amount;break;case TransactionKind.DebtRepayment:state.debt-=amount;stats.Apply(0,0,0,-(int)amount);break;case TransactionKind.AssetValuation:state.otherAssets=amount;break;default:state.processedIds.Remove(id);return false;}
 TransactionCompleted?.Invoke(before,stats.Money,reason);Changed?.Invoke();return true;
 }finally{busy=false;}
 }
}
}