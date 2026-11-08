using System;using System.Linq;using System.Collections.Generic;using UnityEngine;
namespace IanGame {
// Preserve serialized legacy enum values.
public enum TransactionKind {Income,Expense,DebtIncrease,DebtRepayment,AssetValuation,Salary,Tax,StockBuy,StockSell,InvestmentPnL,EventReward,DebtInterest,TaxLateFee}
[Serializable]public class EconomySnapshot {public long debt,otherAssets;public List<string> processedIds=new List<string>();public FinanceSnapshot finance;public HousingSnapshot housing;}
public partial class EconomySystem:MonoBehaviour,IMissionFeatureSource {
 [SerializeField]PlayerStats stats;public EconomyRules rules;EconomySnapshot state=new EconomySnapshot();bool busy;GameManager game;float nextTick;
 public PlayerStats Stats=>stats;public long Cash=>stats==null?0:stats.Money;public long Debt=>state.debt;public long OtherAssets=>state.otherAssets;
 public long StockValue=>state.finance==null?0:state.finance.holdings.Sum(h=>(long)h.quantity*(Quote(h.stockId)?.CurrentPrice??0));
 public long TotalAssets=>Add(Add(Cash,OtherAssets),StockValue);public long NetWorth=>TotalAssets-Debt;
 public long Principal=>Debt-(state.finance?.outstandingInterest??0);public double UnrealizedPnL=>state.finance==null?0:StockValue-state.finance.holdings.Sum(h=>h.costBasis);
 public double RealizedPnL=>state.finance?.realizedPnL??0;public FinanceSnapshot Finance=>Clone(state.finance);
 public event Action<int,int,string> TransactionCompleted;public event Action Changed;public event Action<MissionActivityEvent> ActivityProduced;
 public MissionFeature Features=>MissionFeature.Taxation|MissionFeature.DebtRepayment|MissionFeature.Investments;
 public bool IsAvailable=>rules!=null;public bool HasItem(MissionItemKey item)=>item==MissionItemKey.FirstFurniture&&Housing.owned.Any(id=>housingCatalog!=null&&housingCatalog.Furniture(id)!=null&&!housingCatalog.Furniture(id).starter);
 static long Add(long a,long b)=>a>long.MaxValue-b?long.MaxValue:a+b;
 static T Clone<T>(T value)where T:class=>value==null?null:JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
 public void Configure(PlayerStats model){if(stats!=null)stats.MoneyChanged-=OnCash;stats=model;if(stats!=null)stats.MoneyChanged+=OnCash;}
 public void ConfigureGame(GameManager owner){game=owner;game.missions.RegisterSource(this);}
 void OnDestroy(){if(stats!=null)stats.MoneyChanged-=OnCash;if(game!=null)game.missions.UnregisterSource(this);}
 void OnCash(int value){if(!busy)Changed?.Invoke();}
 public bool HasTransaction(string id)=>state.processedIds.Contains(id);
 public EconomySnapshot Capture(){EnsureHousing();var copy=Clone(state);if(state.finance==null)copy.finance=null;return copy;}
 public void Restore(EconomySnapshot value){if(value==null)return;state=Clone(value);if(value.housing==null)state.housing=null;EnsureHousing();if(value.finance==null)state.finance=null;state.debt=Math.Max(0,state.debt);state.otherAssets=Math.Max(0,state.otherAssets);if(state.processedIds==null)state.processedIds=new List<string>();Changed?.Invoke();}
 public void ResetLife(bool newborn){Restore(new EconomySnapshot{debt=newborn?InitialLifeSettings.NewbornDebt:0});EnsureFinance(1,DateTime.UtcNow.Ticks);}
 public bool CanAfford(int cost)=>stats!=null&&cost>=0&&stats.Money>=cost;
 public bool TrySpend(int cost,string reason="지출")=>TryTransaction(Guid.NewGuid().ToString("N"),TransactionKind.Expense,cost,reason);
 public bool Earn(int amount,string reason="수입")=>TryTransaction(Guid.NewGuid().ToString("N"),TransactionKind.Income,amount,reason);
 public void ApplyDelta(int delta,string reason="인생 이벤트"){if(delta>=0)TryTransaction(Guid.NewGuid().ToString("N"),TransactionKind.EventReward,delta,reason);else TrySpend((int)Math.Min(Cash,-(long)delta),reason);}
 int Day=>game==null?1:game.clock.Day;
 void Signal(string id,MissionCounter counter,long amount=1){ActivityProduced?.Invoke(new MissionActivityEvent{id=id,counter=counter,amount=amount});}
 public bool TryTransaction(string id,TransactionKind kind,long amount,string reason)=>Commit(id,kind,amount,reason,null,null);
 bool Commit(string id,TransactionKind kind,long amount,string reason,string data,Action mutation){
  if(stats==null||busy||string.IsNullOrWhiteSpace(id)||amount<0||HasTransaction(id)||!Enum.IsDefined(typeof(TransactionKind),kind))return false;
  bool income=kind==TransactionKind.Income||kind==TransactionKind.Salary||kind==TransactionKind.EventReward||kind==TransactionKind.StockSell;
  bool expense=kind==TransactionKind.Expense||kind==TransactionKind.Tax||kind==TransactionKind.StockBuy||kind==TransactionKind.DebtRepayment;
  if(mutation==null&&(kind==TransactionKind.StockBuy||kind==TransactionKind.StockSell||kind==TransactionKind.Tax||kind==TransactionKind.Salary||kind==TransactionKind.DebtInterest||kind==TransactionKind.TaxLateFee))return false;
  if(income&&amount>int.MaxValue-Cash||expense&&amount>Cash||kind==TransactionKind.DebtRepayment&&amount>Debt||(kind==TransactionKind.DebtIncrease||kind==TransactionKind.DebtInterest)&&amount>long.MaxValue-Debt)return false;
  EnsureFinance(Day,DateTime.UtcNow.Ticks);var f=state.finance;int before=stats.Money;long principalPaid=kind==TransactionKind.DebtRepayment?Math.Max(0,amount-f.outstandingInterest):0;
  int previousCredit=stats.Credit;busy=true;try{
   state.processedIds.Add(id);
   if(kind==TransactionKind.DebtIncrease||kind==TransactionKind.DebtInterest)state.debt+=amount;
   if(kind==TransactionKind.DebtRepayment){state.debt-=amount;long interest=Math.Min(f.outstandingInterest,amount);f.outstandingInterest-=interest;f.paidThisPeriod=Add(f.paidThisPeriod,amount);}
   if(kind==TransactionKind.AssetValuation)state.otherAssets=amount;
   mutation?.Invoke();
   if(income&&kind!=TransactionKind.StockSell)f.totalIncome=Add(f.totalIncome,amount);
   if(expense&&kind!=TransactionKind.StockBuy&&kind!=TransactionKind.DebtRepayment)f.totalExpenses=Add(f.totalExpenses,amount);
   var record=new EconomyTransaction{id=id,kind=kind,amount=amount,reason=reason,data=data,utcTicks=DateTime.UtcNow.Ticks,day=Day,creditBefore=previousCredit};f.transactions.Add(record);
   if(income||expense)stats.Apply(0,0,0,(int)(income?amount:-amount));
   if(kind==TransactionKind.DebtRepayment&&amount>0&&rules!=null)stats.ApplyCore(new StatDelta{credit=rules.repaymentCredit});
   record.creditAfter=stats.Credit;
  }finally{busy=false;}
  TransactionCompleted?.Invoke(before,stats.Money,reason);Changed?.Invoke();
  if(kind==TransactionKind.DebtRepayment&&principalPaid>0)Signal(id+"/mission",MissionCounter.DebtRepaid,principalPaid);
  return true;
 }
}
}
