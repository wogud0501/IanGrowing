#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IanGame.Editor
{
    public class Phase25Verification : MonoBehaviour
    {
        [Serializable]
        class Results
        {
            public List<string> passed = new List<string>();
            public string failure;
        }

        public static bool Finished;
        public static string Failure;
        Results result = new Results();
        GameManager g;
        SaveData original;
        string slot;
        bool auto;
        bool lifecycle;
        bool realtime;
        bool life;
        float chance;
        FieldInfo Slot => typeof(SaveManager).GetField("fileName", BindingFlags.Instance | BindingFlags.NonPublic);

        public void Begin()
        {
            DontDestroyOnLoad(gameObject);
            StartCoroutine(Run());
        }

        void Check(bool ok, string label)
        {
            if (!ok)
            {
                throw new Exception(label);
            }

            result.passed.Add(label);
        }

        IEnumerator Frames(int n = 5)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
        }

        IEnumerator Route(SceneId id)
        {
            var shell = UnityEngine.Object.FindAnyObjectByType<UIShell>();
            if (shell != null)
            {
                while (shell.Depth > 0)
                {
                    shell.Pop();
                }
            }

            Check(GameContext.Instance.transitions.Navigate(id), "Navigate " + id);
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return Frames();
            Check(
                GameContext.Instance.game == g
                    && UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1,
                "Single persistent model in " + id
            );
        }

        void Fixture()
        {
            g.idle.StopActivity();
            var data = g.CaptureState();
            data.birthIntroCompleted = true;
            data.day = 1;
            data.time = new TimeSnapshot();
            data.settledDay = 1;
            data.stats = new StatSnapshot
            {
                money = 50000,
                hunger = 100,
                thirst = 100,
                energy = 100,
                mental = 100,
                happiness = 50,
                knowledge = 50,
                social = 50,
                credit = 50,
            };
            data.economy = new EconomySnapshot { debt = 1000000 };
            data.progression = new ProgressSnapshot { age = 25 };
            data.survival = new SurvivalSnapshot();
            data.missions = new MissionSnapshot { startDay = 1 };
            data.idle = new IdleProgressSnapshot();
            data.phase23 = new Phase23Snapshot();
            g.RestoreState(data);
            g.economy.EnsureFinance(1, DateTime.UtcNow.Ticks);
            g.clock.realtimeEnabled = false;
            g.phase23.Progress.employment.currentJobId = "convenience";
        }

        void MissionFixture(string id)
        {
            var m = new MissionSnapshot { startDay = 1 };
            foreach (var item in g.missionCatalog.missions.Where(x => string.CompareOrdinal(x.Id, id) < 0))
            {
                m.completedMissionIds.Add(item.Id);
                m.claimedMissionIds.Add(item.Id);
            }

            g.missions.BeginRestore();
            g.missions.Restore(m, g.clock.Day);
            g.missions.EndRestore();
        }

        IEnumerator Run()
        {
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            Finished = false;
            Failure = null;
            g = GameContext.Instance.game;
            original = g.CaptureState();
            slot = (string)Slot.GetValue(g.saves);
            auto = g.autoSaveAfterAction;
            lifecycle = g.autoSaveOnLifecycle;
            realtime = g.clock.realtimeEnabled;
            life = g.realtimeLifeEnabled;
            chance = g.balance.eventChance;
            Slot.SetValue(g.saves, "phase25-integration-" + Guid.NewGuid().ToString("N") + ".json");
            g.autoSaveAfterAction = g.autoSaveOnLifecycle = g.clock.realtimeEnabled = g.realtimeLifeEnabled = false;
            g.balance.eventChance = 0;
            var stack = new Stack<IEnumerator>();
            stack.Push(Scenario());
            while (stack.Count > 0)
            {
                bool more = false;
                object next = null;
                try
                {
                    more = stack.Peek().MoveNext();
                    if (more)
                    {
                        next = stack.Peek().Current;
                    }
                }
                catch (Exception ex)
                {
                    result.failure = ex.ToString();
                    break;
                }

                if (!more)
                {
                    stack.Pop();
                    continue;
                }

                if (next is IEnumerator)
                {
                    stack.Push((IEnumerator)next);
                    continue;
                }

                yield return next;
            }

            try
            {
                g.RestoreState(original);
                Slot.SetValue(g.saves, slot);
                g.autoSaveAfterAction = auto;
                g.autoSaveOnLifecycle = lifecycle;
                g.clock.realtimeEnabled = realtime;
                g.realtimeLifeEnabled = life;
                g.balance.eventChance = chance;
            }
            catch (Exception ex)
            {
                result.failure += " RESTORE " + ex;
            }

            Failure = result.failure;
            File.WriteAllText("Docs/Phase25/PLAY_TEST_RESULTS.json", JsonUtility.ToJson(result, true));
            Finished = true;
            Destroy(gameObject);
        }

        IEnumerator Scenario()
        {
            Fixture();
            yield return Route(SceneId.City);
            var ui = UnityEngine.Object.FindAnyObjectByType<FinancePresenter>();
            Check(
                ui.entry != null && ui.entry.gameObject.activeInHierarchy,
                "City exposes actual StockMarket entry button"
            );
            ui.entry.onClick.Invoke();
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return Frames();
            ui = UnityEngine.Object.FindAnyObjectByType<FinancePresenter>();
            Check(
                ui.marketScene && ui.marketPanel.activeSelf && ui.shell.Top == ui.marketPanel,
                "City button opens independent stock scene and screen"
            );
            Check(
                ui.stocks.Length == 6 && ui.tabs.Length == 4 && ui.chart != null,
                "Six stock selectors, four tabs and pixel chart present"
            );
            long cash = g.economy.Cash;
            ui.quantity.text = "2";
            ui.buy.onClick.Invoke();
            yield return Frames();
            Check(
                g.economy.GetHolding("IAN")?.quantity == 2 && g.economy.Cash == cash - 2000,
                "Actual UI buy updates cash and holdings"
            );
            SaveData saved;
            string message;
            Check(
                g.saves.TryReadAutomatic(out saved, out message) && saved.economy.finance.holdings[0].quantity == 2,
                "UI order checkpoint persists holdings"
            );
            ui.tabs[1].onClick.Invoke();
            yield return Frames();
            Check(
                g.missions.Counter(MissionCounter.PnLReviews) == 1 && ui.pages[1].activeSelf,
                "Portfolio tab records actual PnL review"
            );
            ui.tabs[0].onClick.Invoke();
            ui.quantity.text = "1";
            yield return Frames();
            cash = g.economy.Cash;
            ui.sell.onClick.Invoke();
            yield return Frames();
            Check(
                g.economy.GetHolding("IAN").quantity == 1 && g.economy.Cash == cash + 1000,
                "Actual UI sell updates cash and holdings"
            );
            ui.quantity.text = "999999999";
            yield return Frames();
            Check(!ui.buy.interactable && !ui.sell.interactable, "Order UI disables unaffordable/oversell quantities");
            ui.quantity.text = "1";
            Check(
                g.economy.Finance.transactions.All(t => t.utcTicks > 0 && !string.IsNullOrEmpty(t.id)),
                "Live transactions have IDs and timestamps"
            );
            // Force real filesystem failure in isolated slot: the target is an existing directory.
            string goodSlot = (string)Slot.GetValue(g.saves);
            string badSlot = "phase25-failed-" + Guid.NewGuid().ToString("N") + ".json";
            Directory.CreateDirectory(
                Path.Combine(
                    Application.persistentDataPath,
                    Path.GetFileNameWithoutExtension(badSlot) + ".autosave.json"
                )
            );
            Slot.SetValue(g.saves, badSlot);
            string before = JsonUtility.ToJson(g.economy.Capture());
            ui.buy.onClick.Invoke();
            yield return Frames();
            Check(
                before == JsonUtility.ToJson(g.economy.Capture()) && ui.feedback.text.Contains("저장 실패"),
                "Failed order save restores complete economy and holdings"
            );
            Slot.SetValue(g.saves, goodSlot);
            g.economy.PaySalary("ui-tax-salary", 1000, "convenience");
            g.economy.SettleDay(8);
            ui.OpenEconomy();
            yield return Frames();
            long debt = g.economy.Debt;
            ui.payment.text = "100";
            ui.payTax.onClick.Invoke();
            yield return Frames();
            Check(
                g.economy.Finance.bills[0].Remaining == 0 && g.missions.Counter(MissionCounter.TaxesPaid) == 1,
                "Economy UI pays actual bill and signals tax mission"
            );
            ui.repay.onClick.Invoke();
            yield return Frames();
            Check(g.economy.Debt == debt - 100, "Economy UI repays actual debt");
            ui.economyClose.onClick.Invoke();
            yield return Frames();
            foreach (var d in g.economy.rules.stocks)
            {
                Check(d.BasePrice > 0 && d.Volatility >= 0, "Configured fictional stock " + d.CompanyName);
            }

            ui.tabs[2].onClick.Invoke();
            g.economy.ApplyNews("port", "play-news", g.economy.Finance.marketSequence + 1);
            long stamp = g.economy.Finance.marketUtcTicks;
            int price = g.economy.GetQuote("PHONE").CurrentPrice;
            g.economy.SettleMarket(stamp + 300 * TimeSpan.TicksPerSecond);
            yield return Frames();
            Check(
                ui.news.text.Contains("2mm") && g.economy.GetQuote("PHONE").CurrentPrice > price,
                "Economic news appears in UI and affects target quote"
            );
            ui.tabs[0].onClick.Invoke();
            yield return Frames();
            Directory.CreateDirectory("Docs/Phase25/Screenshots");
            yield return new WaitForSecondsRealtime(.25f);
            ScreenCapture.CaptureScreenshot("Docs/Phase25/Screenshots/StockMarket.png");
            yield return Frames();
            ui.OpenEconomy();
            yield return new WaitForSecondsRealtime(.25f);
            ScreenCapture.CaptureScreenshot("Docs/Phase25/Screenshots/Economy.png");
            yield return Frames();
            ui.economyClose.onClick.Invoke();
            ui.marketClose.onClick.Invoke();
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return Frames();
            Check(
                GameContext.Instance.transitions.CurrentScene == SceneId.City
                    && g.economy.GetHolding("IAN").quantity == 1,
                "Market exit returns to City without losing portfolio"
            );
            yield return Route(SceneId.Workplace);
            Fixture();
            Check(g.idle.StartActivity("work"), "Existing automatic work starts");
            long start = DateTime.UtcNow.AddDays(1).Ticks;
            g.idle.State.accountedUtcTicks = start;
            cash = g.economy.Cash;
            Check(
                g.idle.SettleToUtc(start + 600 * TimeSpan.TicksPerSecond, true),
                "Existing offline two-cycle work settles"
            );
            Check(
                g.economy.Cash - cash == 2000
                    && g.economy.Finance.pendingTax == 200
                    && g.economy.Finance.taxableIncome == 2000,
                "Offline salary taxed once with full cash receipt"
            );
            Check(
                g.phase23.Progress.employment.work.Count == 2 && g.missions.Counter(MissionCounter.Salary) == 2,
                "Existing work receipts and salary mission counters preserved"
            );
            string finances = JsonUtility.ToJson(g.economy.Capture());
            g.idle.SettleToUtc(start + 600 * TimeSpan.TicksPerSecond, true);
            Check(
                finances == JsonUtility.ToJson(g.economy.Capture()),
                "Repeated offline interval cannot repeat wages or tax"
            );
            Check(
                !g.phase23.Work.Pay(g.phase23.Progress.employment.work[0].id),
                "Existing payroll receipt cannot replay"
            );
            Check(g.saves.TryReadAutomatic(out saved, out message), "Offline settlement saved full finance block");
            var expected = JsonUtility.ToJson(saved.economy);
            g.RestoreState(saved);
            Check(
                JsonUtility.ToJson(g.economy.Capture()) == expected,
                "Full model restoration preserves offline finance"
            );
            g.idle.StopActivity();
            Fixture();
            MissionFixture("M035");
            var m = g.missionCatalog.missions.First(x => x.Id == "M035");
            Check(
                g.missions.GetProgress(m).state == MissionState.InProgress,
                "M035 available but incomplete without tax action"
            );
            g.economy.PaySalary("mission-salary", 1000, "convenience");
            g.economy.SettleDay(8);
            g.economy.PayTax("mission-tax", "tax/8", 100);
            Check(
                g.missions.GetProgress(m).state == MissionState.Completed,
                "M035 completes on actual full tax payment"
            );
            cash = g.economy.Cash;
            Check(
                g.missions.Claim("M035") && !g.missions.Claim("M035") && g.economy.Cash == cash + m.rewards.gold,
                "Existing mission reward cannot be claimed twice"
            );
            Fixture();
            MissionFixture("M036");
            g.economy.RepayDebt("mission-repay", 100);
            Check(
                g.missions.Counter(MissionCounter.DebtRepaid) == 100,
                "M036 repayment counter records actual principal once"
            );
            Fixture();
            MissionFixture("M038");
            m = g.missionCatalog.missions.First(x => x.Id == "M038");
            Check(g.missions.GetProgress(m).state == MissionState.InProgress, "M038 incomplete before stock buy");
            g.economy.Trade("mission-buy", "IAN", 1, true);
            Check(g.missions.GetProgress(m).state == MissionState.Completed, "M038 completes on actual first purchase");
            Fixture();
            MissionFixture("M047");
            g.economy.Trade("sell-fixture", "IAN", 1, true);
            g.economy.Trade("mission-sell", "IAN", 1, false);
            Check(
                g.missions.GetProgress(g.missionCatalog.missions.First(x => x.Id == "M047")).state
                    == MissionState.Completed,
                "M047 completes on actual sale"
            );
            Fixture();
            MissionFixture("M048");
            g.economy.ReviewInvestment();
            Check(
                g.missions.Counter(MissionCounter.PnLReviews) == 0,
                "Empty portfolio review cannot fake investment action"
            );
            g.economy.Trade("pnl-fixture", "IAN", 1, true);
            g.economy.ReviewInvestment();
            g.economy.ReviewInvestment();
            Check(
                g.missions.Counter(MissionCounter.PnLReviews) == 1
                    && g.missions.GetProgress(g.missionCatalog.missions.First(x => x.Id == "M048")).state
                        == MissionState.Completed,
                "M048 completes once on real investment review"
            );
            Fixture();
            MissionFixture("M043");
            g.stats.ApplyCore(new StatDelta { credit = 10 });
            Check(
                g.missions.GetProgress(g.missionCatalog.missions.First(x => x.Id == "M043")).state
                    == MissionState.Completed,
                "M043 uses existing real Credit stat"
            );
            yield return Route(SceneId.Home);
            Check(
                UnityEngine.Object.FindAnyObjectByType<FinancePresenter>().entry == null,
                "Home has no added market button cluster"
            );
            Check(g.missionCatalog.missions.Length == 60, "Existing 60 mission catalog preserved");
        }
    }
}
#endif
