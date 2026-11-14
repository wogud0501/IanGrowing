using System;using System.Linq;using UnityEngine;
namespace IanGame {
public partial class EconomySystem {
 public void EnsureFinance(int day,long utc){
  if(state.finance!=null&&state.finance.marketUtcTicks>0)return;
  state.finance=new FinanceSnapshot{lastTaxDay=day,lastDebtDay=day,marketUtcTicks=utc};
  if(rules!=null)foreach(var d in rules.stocks)state.finance.quotes.Add(new StockQuote{StockId=d.StockId,CurrentPrice=Math.Max(1,d.BasePrice),PreviousPrice=Math.Max(1,d.BasePrice),PriceHistory=new System.Collections.Generic.List<int>{Math.Max(1,d.BasePrice)}});
 }
 public bool PaySalary(string id,int amount,string jobId){
  if(!GrowthAccess.Adult(game))return false;
  if(rules==null)return TryTransaction(id,TransactionKind.Income,amount,"급여");
  EnsureFinance(Day,DateTime.UtcNow.Ticks);var f=state.finance;
  long numerator=(long)amount*Mathf.Clamp(rules.TaxRate(jobId),0,5000)+f.taxRemainder;
  bool ok=Commit(id,TransactionKind.Salary,amount,"급여 (세금은 고지서로 납부)",jobId,()=>{f.taxableIncome=Add(f.taxableIncome,amount);f.pendingTax=Add(f.pendingTax,numerator/10000);f.taxRemainder=numerator%10000;});
  if(ok&&game!=null)game.Log(rules.incomeMessage);return ok;
 }
 public bool PayTax(string orderId,string billId,long amount){
  if(game!=null&&game.LifeMode&&!GrowthAccess.Adult(game))return false;
  if(busy||state.finance==null||amount<=0)return false;var bill=state.finance.bills.Find(b=>b.id==billId);
  if(bill==null||amount>bill.Remaining)return false;
  bool ok=Commit(orderId,TransactionKind.Tax,amount,"세금 납부",billId,()=>bill.paid+=amount);
  // Mission counts fully paid bills, not repeated installments.
  if(ok&&bill.Remaining==0){Signal(bill.id+"/paid",MissionCounter.TaxesPaid);game?.Log(rules.taxPaidMessage);}return ok;
 }
 public bool RepayDebt(string id,long amount)=>amount>0&&TryTransaction(id,TransactionKind.DebtRepayment,amount,"부채 상환");
 public void SettleDay(int day){
  if(rules==null||busy)return;EnsureFinance(day,DateTime.UtcNow.Ticks);var f=state.finance;
  int taxPeriod=Math.Max(1,rules.taxPeriodDays),debtPeriod=Math.Max(1,rules.debtPeriodDays);
  while(day-f.lastTaxDay>=taxPeriod){
   f.lastTaxDay+=taxPeriod;
   if(f.pendingTax>0){f.bills.Add(new TaxBill{id="tax/"+f.lastTaxDay,issuedDay=f.lastTaxDay,dueDay=Math.Min(TimeManager.MaximumDay,f.lastTaxDay+Math.Max(1,rules.taxGraceDays)),lastLateDay=f.lastTaxDay+Math.Max(1,rules.taxGraceDays),amount=f.pendingTax,taxableIncome=f.taxableIncome});f.pendingTax=0;f.taxableIncome=0;}
  }
  foreach(var b in f.bills){
   if(b.Remaining<=0||day<=b.dueDay)continue;
   int periods=(day-b.lastLateDay)/taxPeriod;
   if(periods<=0)continue;
   // Simple late fees on unpaid assessed tax; no exponential catch-up.
   long baseUnpaid=Math.Max(0,b.amount-b.paid);long fee=(long)Math.Min(long.MaxValue-(decimal)b.amount-b.lateFees,(decimal)baseUnpaid*Mathf.Clamp(rules.lateTaxBasisPoints,0,5000)/10000*periods);
   string id=b.id+"/late/"+(b.lastLateDay+periods*taxPeriod);
   Commit(id,TransactionKind.TaxLateFee,fee,"세금 연체",b.id,()=>{b.lateFees+=fee;b.lastLateDay+=periods*taxPeriod;stats.ApplyCore(new StatDelta{credit=-Math.Min(100,periods*Math.Max(0,rules.overdueCreditPenalty))});});
  }
  while(day-f.lastDebtDay>=debtPeriod){
   f.lastDebtDay+=debtPeriod;long interest=(long)Math.Min(long.MaxValue-(decimal)Debt,(decimal)Principal*Mathf.Clamp(rules.debtInterestBasisPoints,0,5000)/10000);
   bool overdue=Debt>0&&f.paidThisPeriod<Math.Min(Debt,Math.Max(0,rules.minimumDebtPayment));f.paidThisPeriod=0;
   Commit("debt/interest/"+f.lastDebtDay,TransactionKind.DebtInterest,interest,"부채 이자",overdue?"연체":"정상",()=>{f.outstandingInterest+=interest;f.cumulativeInterest=Add(f.cumulativeInterest,interest);if(overdue)stats.ApplyCore(new StatDelta{credit=-Math.Max(0,rules.overdueCreditPenalty)});});
  }
  SettlePropertyDay(day);Changed?.Invoke();
 }
 StockQuote Quote(string stockId)=>state.finance?.quotes.Find(q=>q.StockId==stockId);
 public StockQuote GetQuote(string stockId)=>Clone(Quote(stockId));
 public StockHolding GetHolding(string stockId)=>Clone(state.finance?.holdings.Find(h=>h.stockId==stockId));
 public bool Trade(string orderId,string stockId,int quantity,bool buy){
  if(!GrowthAccess.Adult(game)||rules==null||busy||quantity<=0||string.IsNullOrWhiteSpace(orderId)||HasTransaction(orderId))return false;
  EnsureFinance(Day,DateTime.UtcNow.Ticks);var q=Quote(stockId);var f=state.finance;var h=f.holdings.Find(v=>v.stockId==stockId);
  if(q==null||q.MarketStatus!=MarketStatus.Open||buy&&stats.Credit<rules.minimumTradingCredit||buy&&(long)(h?.quantity??0)+quantity>int.MaxValue||!buy&&(h==null||h.quantity<quantity))return false;
  long amount=(long)q.CurrentPrice*quantity;
  bool ok=Commit(orderId,buy?TransactionKind.StockBuy:TransactionKind.StockSell,amount,buy?"주식 매수":"주식 매도",stockId+"/"+quantity+"@"+q.CurrentPrice,()=>{
   if(h==null){h=new StockHolding{stockId=stockId};f.holdings.Add(h);}
   if(buy){h.quantity+=quantity;h.costBasis+=amount;}else{double basis=h.AveragePrice*quantity;h.quantity-=quantity;h.costBasis=h.quantity==0?0:Math.Max(0,h.costBasis-basis);f.realizedPnL+=amount-basis;}
  });
  if(ok)Signal(orderId+"/mission",buy?MissionCounter.StockBuys:MissionCounter.StockSells);return ok;
 }
 public void ReviewInvestment(){if(state.finance!=null&&state.finance.transactions.Any(t=>t.kind==TransactionKind.StockBuy))Signal("finance/pnl-review",MissionCounter.PnLReviews);}
 public void ReanchorMarket(long minimumUtc){EnsureFinance(Day,minimumUtc);state.finance.marketUtcTicks=Math.Max(state.finance.marketUtcTicks,minimumUtc);if(state.property!=null)state.property.marketUtcTicks=Math.Max(state.property.marketUtcTicks,minimumUtc);}
 static double Sample(string key,long tick){unchecked{uint h=2166136261;foreach(char c in key){h^=c;h*=16777619;}h^=(uint)tick;h*=16777619;h^=(uint)(tick>>32);h^=h>>16;h*=2246822519;h^=h>>13;return h/(double)uint.MaxValue;}}
 public bool ApplyNews(string newsId,string occurrenceId,long tick){
  if(rules==null||state.finance==null||busy||string.IsNullOrWhiteSpace(occurrenceId))return false;
  var f=state.finance;var d=Array.Find(rules.news,n=>n.NewsId==newsId);
  if(d==null||Day<d.MinimumDay||stats.Credit<d.MinimumCredit||f.news.Any(n=>n.id==occurrenceId))return false;
  var record=new EconomicNewsRecord{id=occurrenceId,newsId=d.NewsId,title=d.Title,description=d.Description,tick=tick,expiresTick=tick+Math.Max(1,d.Duration)};f.news.Add(record);game?.lifeEvents.PublishEconomicNews(record);return true;
 }
 public void SettleMarket(long utc){
  if(rules==null||busy||utc<0||utc>DateTime.MaxValue.Ticks)return;EnsureFinance(Day,utc);var f=state.finance;
  long interval=(long)Math.Max(1,rules.marketIntervalSeconds)*TimeSpan.TicksPerSecond;
  long count=(utc-f.marketUtcTicks)/interval;if(count<=0){SettlePropertyMarket(utc);return;}
  // Bound very long absences, consuming skipped time so it cannot be reclaimed.
  long skip=Math.Max(0,count-Math.Max(1,rules.maximumCatchupTicks));f.marketUtcTicks+=skip*interval;f.marketSequence+=skip;
  for(long i=skip;i<count;i++){
   f.marketUtcTicks+=interval;long tick=++f.marketSequence;
   foreach(var n in rules.news)if(Sample(n.NewsId,f.marketUtcTicks/interval)<Mathf.Clamp01(n.Probability))ApplyNews(n.NewsId,n.NewsId+"/"+tick,tick);
   foreach(var q in f.quotes){if(q.MarketStatus!=MarketStatus.Open)continue;var d=Array.Find(rules.stocks,s=>s.StockId==q.StockId);if(d==null)continue;
    double impact=0;foreach(var n in f.news.Where(n=>n.tick<=tick&&n.expiresTick>tick)){var def=Array.Find(rules.news,x=>x.NewsId==n.newsId);if(def!=null&&(def.AffectedStocks.Length==0||def.AffectedStocks.Contains(q.StockId))&&(string.IsNullOrEmpty(def.Sector)||def.Sector==d.Sector))impact+=def.PriceImpact*d.NewsSensitivity;}
    double move=(Sample(q.StockId,f.marketUtcTicks/interval)*2-1)*Mathf.Clamp(d.Volatility,0,.2f)+impact;
    q.PreviousPrice=q.CurrentPrice;q.CurrentPrice=(int)Math.Max(Math.Max(1,d.BasePrice/10),Math.Min(Math.Min(int.MaxValue/100,Math.Max(1,d.BasePrice)*20L),Math.Round(q.CurrentPrice*(1+Math.Max(-.12,Math.Min(.12,move))))));
    q.PriceHistory.Add(q.CurrentPrice);while(q.PriceHistory.Count>Math.Max(2,rules.historyLimit))q.PriceHistory.RemoveAt(0);
   }
  }SettlePropertyMarket(utc);Changed?.Invoke();
 }
 void Update(){if(game==null||game.State!=GameState.Playing||game.BirthIntroductionPending||Time.unscaledTime<nextTick||game.idle!=null&&game.idle.IsPersistenceSuspended)return;nextTick=Time.unscaledTime+1;SettleMarket(DateTime.UtcNow.Ticks);}
 public static bool ValidFinance(EconomySnapshot e){
  NormalizeProperty(e);if(e==null||e.processedIds==null||!ValidProperty(e))return false;var f=e.finance;if(f==null)return true;
  if(f.schema!=1||f.lastTaxDay<1||f.lastDebtDay<1||f.marketUtcTicks<0||f.marketUtcTicks>DateTime.MaxValue.Ticks||f.marketSequence<0||f.totalIncome<0||f.totalExpenses<0||f.taxableIncome<0||f.pendingTax<0||f.taxRemainder<0||f.taxRemainder>=10000||f.cumulativeInterest<0||f.outstandingInterest<0||f.outstandingInterest>e.debt||f.paidThisPeriod<0||double.IsNaN(f.realizedPnL)||double.IsInfinity(f.realizedPnL)||f.transactions==null||f.bills==null||f.quotes==null||f.holdings==null||f.news==null)return false;
  if(f.transactions.Any(t=>t==null||string.IsNullOrWhiteSpace(t.id)||t.amount<0||t.utcTicks<0||t.utcTicks>DateTime.MaxValue.Ticks||!Enum.IsDefined(typeof(TransactionKind),t.kind)||!e.processedIds.Contains(t.id))||f.transactions.Select(t=>t.id).Distinct().Count()!=f.transactions.Count)return false;
  if(f.quotes.Any(q=>q==null||string.IsNullOrWhiteSpace(q.StockId)||q.CurrentPrice<1||q.PreviousPrice<1||!Enum.IsDefined(typeof(MarketStatus),q.MarketStatus)||q.PriceHistory==null||q.PriceHistory.Any(p=>p<1))||f.quotes.Select(q=>q.StockId).Distinct().Count()!=f.quotes.Count)return false;
  if(f.holdings.Any(h=>h==null||h.quantity<0||double.IsNaN(h.costBasis)||double.IsInfinity(h.costBasis)||h.costBasis<0||h.quantity==0&&h.costBasis!=0||!f.quotes.Any(q=>q.StockId==h.stockId))||f.holdings.Select(h=>h.stockId).Distinct().Count()!=f.holdings.Count)return false;
  if(f.bills.Any(b=>b==null||string.IsNullOrWhiteSpace(b.id)||b.issuedDay<1||b.dueDay<b.issuedDay||b.lastLateDay<b.dueDay||b.amount<0||b.lateFees<0||b.amount>long.MaxValue-b.lateFees||b.paid<0||b.paid>b.amount+b.lateFees)||f.bills.Select(b=>b.id).Distinct().Count()!=f.bills.Count)return false;
  return !f.news.Any(n=>n==null||string.IsNullOrWhiteSpace(n.id)||n.tick<0||n.expiresTick<=n.tick)&&f.news.Select(n=>n.id).Distinct().Count()==f.news.Count;
 }
}
}
