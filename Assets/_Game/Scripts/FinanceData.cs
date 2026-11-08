using System;using System.Collections.Generic;
namespace IanGame {
[Serializable] public class EconomyTransaction {public string id,reason,data;public TransactionKind kind;public long amount,utcTicks;public int day,creditBefore,creditAfter;}
[Serializable] public class TaxBill {public string id;public int issuedDay,dueDay,lastLateDay;public long taxableIncome,amount,paid,lateFees;public long Remaining=>amount+lateFees-paid;}
public enum MarketStatus {Open,Halted}
[Serializable] public class StockQuote {public string StockId;public int CurrentPrice,PreviousPrice;public MarketStatus MarketStatus;public List<int> PriceHistory=new List<int>();}
[Serializable] public class StockHolding {public string stockId;public int quantity;public double costBasis;public double AveragePrice=>quantity==0?0:costBasis/quantity;}
[Serializable] public class EconomicNewsRecord {public string id,newsId,title,description;public long tick,expiresTick;}
[Serializable] public class FinanceSnapshot {
 public int schema=1,lastTaxDay,lastDebtDay;public long marketUtcTicks,marketSequence;
 public long totalIncome,totalExpenses,taxableIncome,pendingTax,taxRemainder,cumulativeInterest,outstandingInterest,paidThisPeriod;
 public double realizedPnL;public List<EconomyTransaction> transactions=new List<EconomyTransaction>();
 public List<TaxBill> bills=new List<TaxBill>();public List<StockQuote> quotes=new List<StockQuote>();
 public List<StockHolding> holdings=new List<StockHolding>();public List<EconomicNewsRecord> news=new List<EconomicNewsRecord>();
}
}
