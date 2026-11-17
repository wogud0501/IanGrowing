using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace IanGame.Editor
{
    public static class Phase25CoreVerification
    {
        [Serializable]
        class Results
        {
            public List<string> passed = new List<string>();
            public string failure;
        }

        public static string Run()
        {
            var result = new Results();
            var go = new GameObject("Phase25 isolated verification");
            GameObject authority = null;
            EconomyRules rules = null;
            Action<bool, string> check = (ok, label) =>
            {
                if (!ok)
                {
                    throw new Exception(label);
                }

                result.passed.Add(label);
            };
            try
            {
                var stats = go.AddComponent<PlayerStats>();
                stats.Restore(new StatSnapshot { money = 0, credit = 50 });
                var e = go.AddComponent<EconomySystem>();
                e.Configure(stats);
                authority = new GameObject("Isolated adult authority");
                authority.SetActive(false);
                var owner = authority.AddComponent<GameManager>();
                owner.lifeEvents = authority.AddComponent<EventManager>();
                owner.growth = authority.AddComponent<LifeProgression>();
                owner.growth.Restore(new ProgressSnapshot { age = 19 });
                owner.missions = authority.AddComponent<MissionService>();
                owner.clock = go.AddComponent<TimeManager>();
                owner.clock.SetRunning(false);
                typeof(EconomySystem)
                    .GetField("game", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(e, owner);
                rules = ScriptableObject.CreateInstance<EconomyRules>();
                e.rules = rules;
                e.ResetLife(true);
                check(
                    e.Cash == 0 && e.Debt == 1000000 && e.NetWorth == -1000000,
                    "Initial cash/debt/net worth preserved"
                );
                check(e.Earn(10000) && e.TrySpend(100) && e.Cash == 9900, "Cash income and expense");
                long cash = e.Cash;
                check(
                    !e.TrySpend(9901) && !e.Earn(-1) && e.Cash == cash,
                    "Insufficient/negative amount cannot alter wallet"
                );
                check(
                    e.TryTransaction("receipt", TransactionKind.Income, 100, "test")
                        && !e.TryTransaction("receipt", TransactionKind.Income, 100, "duplicate"),
                    "Unique receipts deduplicate transactions"
                );
                check(
                    !e.TryTransaction("invalid", (TransactionKind)999, 1, "bad") && !e.HasTransaction("invalid"),
                    "Unknown kinds rejected without receipt"
                );
                check(
                    !e.TryTransaction("forged", TransactionKind.StockSell, 1000, "bad"),
                    "Composite transaction cannot bypass holdings"
                );
                check(!e.Earn(int.MaxValue), "Wallet integer overflow rejected");
                check(
                    e.PaySalary("salary", 1000, "convenience") && !e.PaySalary("salary", 1000, "convenience"),
                    "Salary paid once"
                );
                check(
                    e.Finance.pendingTax == 100 && e.Finance.taxableIncome == 1000,
                    "Salary income taxed once, without withholding"
                );
                e.SettleDay(8);
                check(
                    e.Finance.bills.Count == 1 && e.Finance.bills[0].amount == 100 && e.Finance.pendingTax == 0,
                    "Tax bill generation and period watermark"
                );
                check(
                    e.Debt == 1010000
                        && e.Finance.outstandingInterest == 10000
                        && e.Principal == 1000000
                        && stats.Credit == 48,
                    "Simple principal interest and overdue credit"
                );
                string settled = JsonUtility.ToJson(e.Capture());
                e.SettleDay(8);
                check(settled == JsonUtility.ToJson(e.Capture()), "Same day never bills or charges interest twice");
                check(
                    e.PayTax("tax-part", "tax/8", 40) && e.Finance.bills[0].Remaining == 60,
                    "Partial tax payment remains payable"
                );
                check(
                    !e.PayTax("tax-part", "tax/8", 40) && !e.PayTax("tax-over", "tax/8", 61),
                    "Duplicate and excess tax payments rejected"
                );
                e.SettleDay(18);
                check(
                    e.Finance.bills[0].lateFees == 1 && e.Finance.bills[0].Remaining == 61,
                    "Unpaid tax accrues data-driven late fee"
                );
                check(
                    e.PayTax("tax-final", "tax/8", 61) && e.Finance.bills[0].Remaining == 0,
                    "Overdue bill can be settled"
                );
                long debt = e.Debt,
                    interest = e.Finance.outstandingInterest;
                int credit = stats.Credit;
                check(
                    e.RepayDebt("repay", 100)
                        && e.Debt == debt - 100
                        && e.Finance.outstandingInterest == interest - 100
                        && stats.Credit == credit + 1,
                    "Partial debt repayment pays interest first and raises credit"
                );
                check(
                    !e.RepayDebt("repay", 100) && !e.RepayDebt("overpay", e.Debt + 1),
                    "Duplicate and excess repayment rejected"
                );
                var old = e.Capture();
                old.finance = null;
                old.debt = 12345;
                e.Restore(old);
                e.SettleDay(100);
                check(
                    e.Debt == 12345 && e.Finance.lastDebtDay == 100 && e.Finance.bills.Count == 0,
                    "Legacy migration preserves debt and never backdates charges"
                );
                e.ResetLife(true);
                stats.Restore(new StatSnapshot { money = 100000, credit = 50 });
                check(e.Finance.quotes.Count == 6, "All six fictional stocks registered");
                bool observedAtomic = false,
                    reentrantRejected = false;
                Action<int> observer = _ =>
                {
                    observedAtomic = e.GetHolding("IAN")?.quantity == 2;
                    reentrantRejected = !e.Trade("reentrant", "IAN", 1, true);
                };
                stats.MoneyChanged += observer;
                check(e.Trade("buy1", "IAN", 2, true), "Stock purchase succeeds");
                stats.MoneyChanged -= observer;
                check(
                    observedAtomic && reentrantRejected && e.Cash == 98000,
                    "Wallet observer sees atomic holding mutation; reentrant order rejected"
                );
                check(
                    !e.Trade("buy1", "IAN", 2, true)
                        && !e.Trade("zero", "IAN", 0, true)
                        && !e.Trade("negative", "IAN", -1, true)
                        && !e.Trade("oversell", "IAN", 3, false),
                    "Duplicate/invalid/oversell orders rejected"
                );
                check(!e.Trade("poor", "PHONE", int.MaxValue, true), "Unaffordable order rejected");
                var snapshot = e.Capture();
                var q = snapshot.finance.quotes.Find(x => x.StockId == "IAN");
                q.CurrentPrice = 2000;
                e.Restore(snapshot);
                check(
                    e.Trade("buy2", "IAN", 1, true) && Math.Abs(e.GetHolding("IAN").AveragePrice - 4000d / 3) < .001,
                    "Weighted average acquisition cost"
                );
                check(
                    e.Trade("sell", "IAN", 1, false)
                        && Math.Abs(e.RealizedPnL - 2000d / 3) < .001
                        && Math.Abs(e.UnrealizedPnL - 4000d / 3) < .001,
                    "Realized/unrealized profit uses average cost"
                );
                check(
                    e.TotalAssets == e.Cash + e.StockValue && e.NetWorth == e.TotalAssets - e.Debt,
                    "Stock assets included in total and net worth"
                );
                stats.ApplyCore(new StatDelta { credit = -100 });
                check(
                    !e.Trade("lowcredit", "IAN", 1, true) && e.Trade("exit", "IAN", 1, false),
                    "Low credit restricts purchases but permits liquidation"
                );
                stats.ApplyCore(new StatDelta { credit = 100 });
                foreach (var d in rules.stocks)
                {
                    d.Volatility = 0;
                }

                foreach (var n in rules.news)
                {
                    n.Probability = 0;
                }

                long anchor = e.Finance.marketUtcTicks;
                int before = e.GetQuote("PHONE").CurrentPrice;
                check(
                    e.ApplyNews("port", "news-once", 1) && !e.ApplyNews("port", "news-once", 1),
                    "News occurrence applies only once"
                );
                e.SettleMarket(anchor + 300 * TimeSpan.TicksPerSecond);
                check(
                    e.GetQuote("PHONE").CurrentPrice > before && e.GetQuote("IAN").CurrentPrice == 2000,
                    "Targeted news moves affected stock only"
                );
                string market = JsonUtility.ToJson(e.Capture());
                e.SettleMarket(anchor + 300 * TimeSpan.TicksPerSecond);
                e.SettleMarket(anchor - 300 * TimeSpan.TicksPerSecond);
                check(JsonUtility.ToJson(e.Capture()) == market, "Same/reversed UTC cannot repeat market movement");
                e.SettleMarket(anchor + 1000L * 300 * TimeSpan.TicksPerSecond);
                check(
                    e.Finance.marketSequence == 1000
                        && e.Finance.quotes.All(v => v.CurrentPrice > 0 && v.PriceHistory.Count <= rules.historyLimit),
                    "Long absence bounded with consumed watermark and positive prices"
                );
                var round = JsonUtility.FromJson<EconomySnapshot>(JsonUtility.ToJson(e.Capture()));
                check(EconomySystem.ValidFinance(round), "Complete finance JSON validates");
                e.Restore(round);
                check(
                    !e.Trade("sell", "IAN", 1, false) && e.GetHolding("IAN").quantity == 1,
                    "Restored holdings and receipts prevent replay"
                );
                var bad = JsonUtility.FromJson<EconomySnapshot>(JsonUtility.ToJson(round));
                bad.finance.holdings[0].costBasis = double.NaN;
                check(!EconomySystem.ValidFinance(bad), "Non-finite acquisition cost rejected");
                bad = JsonUtility.FromJson<EconomySnapshot>(JsonUtility.ToJson(round));
                bad.finance.quotes.Add(bad.finance.quotes[0]);
                check(!EconomySystem.ValidFinance(bad), "Duplicate saved stock rejected");
                var clock = go.GetComponent<TimeManager>();
                clock.SetRunning(false);
                var save = go.AddComponent<SaveManager>();
                typeof(SaveManager)
                    .GetField("fileName", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(save, "phase25-core-" + Guid.NewGuid().ToString("N") + ".json");
                save.Configure(null, e, null);
                string message;
                check(
                    save.Save(stats, clock, false, "phase25-core", 1, out message),
                    "Real isolated save file writes: " + message
                );
                SaveData loaded;
                check(save.TryRead(out loaded, out message), "Real isolated save file reads: " + message);
                check(
                    JsonUtility.ToJson(loaded.economy) == JsonUtility.ToJson(e.Capture()),
                    "Disk round trip preserves all transactions/bills/holdings/prices/news/watermarks"
                );
                string legacy = JsonUtility.ToJson(loaded);
                int start = legacy.IndexOf(",\"finance\":", StringComparison.Ordinal);
                int opening = legacy.IndexOf('{', start),
                    depth = 1,
                    end = opening + 1;
                for (; depth > 0; end++)
                {
                    if (legacy[end] == '{')
                    {
                        depth++;
                    }

                    if (legacy[end] == '}')
                    {
                        depth--;
                    }
                }

                legacy = legacy.Remove(start, end - start);
                File.WriteAllText(save.SavePath, legacy);
                check(
                    save.TryRead(out loaded, out message)
                        && loaded.economy.debt == e.Debt
                        && loaded.economy.finance == null,
                    "Existing v4 without finance loads without wiping recorded data"
                );
            }
            catch (Exception ex)
            {
                result.failure = ex.ToString();
            }
            finally
            {
                if (rules != null)
                {
                    UnityEngine.Object.DestroyImmediate(rules);
                }

                UnityEngine.Object.DestroyImmediate(go);
                if (authority != null)
                {
                    UnityEngine.Object.DestroyImmediate(authority);
                }
            }

            Directory.CreateDirectory("Docs/Phase25");
            string json = JsonUtility.ToJson(result, true);
            File.WriteAllText("Docs/Phase25/CORE_TEST_RESULTS.json", json);
            return json;
        }
    }
}
