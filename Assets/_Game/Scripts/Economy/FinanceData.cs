using System;
using System.Collections.Generic;

namespace IanGame
{
    [Serializable]
    public class EconomyTransaction
    {
        public string id;
        public string reason;
        public string data;
        public TransactionKind kind;
        public long amount;
        public long utcTicks;
        public int day;
        public int creditBefore;
        public int creditAfter;
    }

    [Serializable]
    public class TaxBill
    {
        public string id;
        public int issuedDay;
        public int dueDay;
        public int lastLateDay;
        public long taxableIncome;
        public long amount;
        public long paid;
        public long lateFees;
        public long Remaining => amount + lateFees - paid;
    }

    public enum MarketStatus
    {
        Open,
        Halted,
    }

    [Serializable]
    public class StockQuote
    {
        public string StockId;
        public int CurrentPrice;
        public int PreviousPrice;
        public MarketStatus MarketStatus;
        public List<int> PriceHistory = new List<int>();
    }

    [Serializable]
    public class StockHolding
    {
        public string stockId;
        public int quantity;
        public double costBasis;
        public double AveragePrice => quantity == 0 ? 0 : costBasis / quantity;
    }

    [Serializable]
    public class EconomicNewsRecord
    {
        public string id;
        public string newsId;
        public string title;
        public string description;
        public long tick;
        public long expiresTick;
    }

    [Serializable]
    public class FinanceSnapshot
    {
        public int schema = 1;
        public int lastTaxDay;
        public int lastDebtDay;
        public long marketUtcTicks;
        public long marketSequence;
        public long totalIncome;
        public long totalExpenses;
        public long taxableIncome;
        public long pendingTax;
        public long taxRemainder;
        public long cumulativeInterest;
        public long outstandingInterest;
        public long paidThisPeriod;
        public double realizedPnL;
        public List<EconomyTransaction> transactions = new List<EconomyTransaction>();
        public List<TaxBill> bills = new List<TaxBill>();
        public List<StockQuote> quotes = new List<StockQuote>();
        public List<StockHolding> holdings = new List<StockHolding>();
        public List<EconomicNewsRecord> news = new List<EconomicNewsRecord>();
    }
}
