#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class PropertyVerification
    {
        [Serializable]
        class FinanceResult
        {
            public string failure;
        }

        public static IEnumerator Run(
            LifeWorldController w,
            GameManager g,
            Action<bool, string> check,
            Action<GameObject, string> audit
        )
        {
            var e = g.economy;
            Directory.CreateDirectory("Docs/Phase27/Screenshots");
            void Fixture(int cash = 1000000, long debt = 0, int credit = 90)
            {
                var d = g.CaptureState();
                d.stats.money = cash;
                d.stats.credit = credit;
                d.progression.age = 40;
                d.time.subDaySeconds = 12 * 3600;
                d.day = 10;
                d.settledDay = 10;
                d.economy = new EconomySnapshot { debt = debt, housing = d.economy.housing };
                g.RestoreState(d);
                g.clock.realtimeEnabled = false;
                var s = e.Properties;
            }

            void Day(int day)
            {
                g.clock.Restore(
                    day,
                    new TimeSnapshot { subDaySeconds = 12 * 3600, elapsedSeconds = (day - 1) * 86400 }
                );
            }

            void Cash(int value)
            {
                var s = g.stats.Capture();
                s.money = value;
                g.stats.Restore(s);
            }

            Fixture();
            check(
                e.propertyCatalog.properties.Length == 6 && e.Properties.quotes.Count == 6,
                "Six property types and runtime quotes are linked to GameContext"
            );
            check(
                e.Properties.contract == null && e.Housing.owned.Contains("basic-bed"),
                "Optional old save migration has no phantom contract and preserves starter bed"
            );
            check(g.missionCatalog.missions.Length == 60, "Original sixty mission definitions remain intact");
            var cash = e.Cash;
            var worth = e.NetWorth;
            var originalOther = e.OtherAssets;
            Cash(0);
            check(
                !e.BuyProperty("poor", "property-0")
                    && !e.LeaseProperty("poor-lease", "property-0", HousingTenure.Jeonse)
                    && e.Cash == 0,
                "Insufficient cash blocks purchase and lease without mutation"
            );
            Cash((int)cash);
            check(
                !e.BuyProperty("stale", "property-0", 1) && e.Cash == cash,
                "Changed quoted price is rejected before committing"
            );
            check(
                e.BuyProperty("buy", "property-0")
                    && e.Cash == cash - 8000
                    && e.PropertyValue == 8000
                    && e.NetWorth == worth,
                "Purchase exchanges cash for one property without losing net worth"
            );
            check(
                !e.BuyProperty("buy", "property-0") && !e.BuyProperty("another-buy", "property-0"),
                "Receipt replay and duplicate ownership are rejected"
            );
            check(
                e.Finance.totalExpenses == 0 && e.OtherAssets == originalOther,
                "Asset purchase does not inflate consumption expenses or legacy assets"
            );
            var d = g.CaptureState();
            d.economy.property.quotes[0].MarketPrice = 10000;
            g.RestoreState(d);
            g.clock.realtimeEnabled = false;
            check(
                e.PropertyValue == 10000 && e.NetWorth == worth + 2000,
                "Market appreciation immediately updates total assets and net worth"
            );
            check(
                e.SellProperty("sell", "property-0") && e.Cash == cash + 2000 && e.Properties.realizedPnL == 2000,
                "Sale credits current price and records realized gain"
            );
            long after = e.Cash;
            check(
                !e.SellProperty("sell-again", "property-0") && e.Cash == after,
                "Non-owner cannot sell or receive duplicate proceeds"
            );
            check(e.BuyProperty("buy-loss", "property-0"), "Repurchase after sale is permitted");
            d = g.CaptureState();
            d.economy.property.quotes[0].MarketPrice = 7000;
            g.RestoreState(d);
            g.clock.realtimeEnabled = false;
            check(
                e.SellProperty("sell-loss", "property-0") && e.Properties.realizedPnL == -1000,
                "Capital losses are retained with gains"
            );
            Fixture();
            cash = e.Cash;
            worth = e.NetWorth;
            check(e.LeaseProperty("jeonse", "property-1", HousingTenure.Jeonse), "Jeonse contract succeeds");
            check(
                e.HousingDeposit == 21000 && e.Cash == cash - 21500 && e.NetWorth == worth - 500,
                "Jeonse deposit is a receivable asset; only moving cost is consumed"
            );
            check(
                !e.CanMoveResidence("improved", out _)
                    && !e.LeaseProperty("duplicate-contract", "property-2", HousingTenure.MonthlyRent),
                "Existing contract blocks duplicate leases and legacy move bypass"
            );
            check(
                e.EndLease("end-jeonse") && e.HousingDeposit == 0 && e.Cash == cash - 500,
                "Early termination refunds full deposit exactly once"
            );
            check(!e.EndLease("end-again"), "Ended contract cannot refund again");
            Fixture();
            cash = e.Cash;
            long rentCount = g.missions.Counter(MissionCounter.RentPaid);
            check(
                e.LeaseProperty("monthly", "property-0", HousingTenure.MonthlyRent),
                "Monthly lease debits deposit and moving fee"
            );
            check(
                e.Cash == cash - 1300 && e.HousingDeposit == 800 && e.Finance.totalExpenses == 500,
                "Monthly deposit and expense accounting remain separate"
            );
            Day(40);
            e.SettlePropertyDay(40);
            check(
                e.Cash == cash - 1396 && e.Properties.bills.Count == 1 && e.HousingArrears == 0,
                "First due date charges 80 rent plus 16 maintenance"
            );
            check(
                g.missions.Counter(MissionCounter.RentPaid) == rentCount + 1,
                "Existing M045 RentPaid mission counter receives a paid period"
            );
            var before = JsonUtility.ToJson(e.Capture());
            e.SettlePropertyDay(40);
            check(
                before == JsonUtility.ToJson(e.Capture()),
                "Repeated settlement never bills or pays the same rental period twice"
            );
            Cash(0);
            Day(70);
            e.SettlePropertyDay(70);
            check(
                e.HousingArrears == 96 && e.Properties.bills.Last().Remaining == 96,
                "Insufficient funds preserve an unpaid invoice without inventing debt"
            );
            int credit = g.stats.Credit;
            Day(74);
            e.SettlePropertyDay(74);
            check(
                g.stats.Credit == credit - e.rules.overdueCreditPenalty,
                "Overdue housing bill affects existing credit once"
            );
            e.SettlePropertyDay(74);
            check(
                g.stats.Credit == credit - e.rules.overdueCreditPenalty,
                "Repeating overdue settlement does not repeat credit penalty"
            );
            worth = e.NetWorth;
            check(
                e.EndLease("net-deposit")
                    && e.Cash == 704
                    && e.HousingDeposit == 0
                    && e.HousingArrears == 0
                    && e.NetWorth == worth,
                "Deposit return nets unpaid rent without double-counting cash or assets"
            );
            check(
                e.Finance.totalExpenses == 692 && g.missions.Counter(MissionCounter.RentPaid) == rentCount + 2,
                "Deposit-settled rent counts once and is recorded as an expense"
            );
            Fixture();
            check(e.LeaseProperty("expiry", "property-0", HousingTenure.MonthlyRent), "Expiry fixture contract starts");
            Day(100);
            e.SettlePropertyDay(100);
            check(
                e.Properties.contract == null && e.HousingDeposit == 0 && e.Housing.residenceId == "starter",
                "Contract expires after three periods and returns to starter home"
            );
            cash = e.Cash;
            e.SettlePropertyDay(100);
            check(e.Cash == cash && e.Properties.bills.Count == 3, "Expired lease and deposit cannot settle twice");
            Fixture(0);
            check(
                e.BorrowHousing("loan", 10000)
                    && e.Cash == 10000
                    && e.Debt == 10000
                    && e.HousingLoanPrincipal == 10000
                    && e.NetWorth == 0,
                "Housing loan increases cash and the existing debt by equal amounts"
            );
            check(
                !e.BorrowHousing("loan", 10000) && !e.BorrowHousing("overlimit", e.AvailableHousingLoan + 1),
                "Duplicate loan and excess limit are rejected"
            );
            check(e.Finance.totalIncome == 0, "Borrowing is not salary or earned income");
            Day(17);
            e.SettleDay(17);
            check(
                e.Debt == 10100 && e.Finance.outstandingInterest == 100 && e.HousingLoanPrincipal == 10000,
                "Housing loan uses original interest once [debt="
                    + e.Debt
                    + ", interest="
                    + e.Finance.outstandingInterest
                    + ", watermark="
                    + e.Finance.lastDebtDay
                    + "]"
            );
            long interest = e.Finance.cumulativeInterest;
            e.SettleDay(17);
            check(e.Finance.cumulativeInterest == interest, "Existing day watermark prevents duplicate loan interest");
            check(
                e.RepayDebt("interest", 100) && e.HousingLoanPrincipal == 10000 && e.Finance.outstandingInterest == 0,
                "Debt repayment pays interest before housing principal"
            );
            check(
                e.RepayDebt("principal", 1000) && e.Debt == 9000 && e.HousingLoanPrincipal == 9000,
                "Existing repayment reduces aggregate and housing principal together"
            );
            check(
                g.missions.Counter(MissionCounter.DebtRepaid) >= 1000,
                "Original principal repayment mission source remains connected"
            );
            g.stats.ApplyCore(new StatDelta { credit = -100 });
            check(
                e.AvailableHousingLoan == 0 && !e.BorrowHousing("badcredit", 1),
                "Low credit blocks new housing loans"
            );
            Fixture();
            check(
                !e.TryTransaction("forged-property", TransactionKind.PropertySell, 1000, "bad")
                    && !e.TryTransaction("forged-loan", TransactionKind.LoanDraw, 1000, "bad"),
                "Composite economic transactions cannot bypass property ownership or loan checks"
            );
            check(
                e.BuyProperty("home", "property-2") && e.OccupyProperty("move", "property-2"),
                "Owned residence can be entered with paid moving cost"
            );
            check(
                e.Housing.residenceId == "improved" && e.Housing.owned.SequenceEqual(new[] { "basic-bed" }),
                "Residence links to existing home shell without free purchased furniture"
            );
            check(w.Travel("home"), "Return to actual home for housing recovery");
            while (w.Busy)
            {
                yield return null;
            }

            var health = g.stats.Capture();
            health.energy = 40;
            health.mental = 40;
            g.stats.Restore(health);
            e.ApplyHousingRecovery(5);
            check(
                g.stats.Health == 43 && g.stats.Mental == 43,
                "Occupied housing applies configured recovery to existing stats"
            );
            var saved = g.CaptureState();
            check(
                g.SaveAutomaticCheckpoint(out var why),
                "Property, contracts, loans, bills and quotes save to isolated disk slot"
            );
            SaveData loaded;
            check(g.saves.TryReadAutomatic(out loaded, out why), "New property snapshot passes real save validation");
            g.RestoreState(loaded);
            g.clock.realtimeEnabled = false;
            check(
                JsonUtility.ToJson(e.Properties) == JsonUtility.ToJson(saved.economy.property),
                "Property save/load preserves ownership, histories, bills, loan and payment watermarks"
            );
            d = g.CaptureState();
            long legacyCash = d.stats.money,
                legacyDebt = d.economy.debt;
            var legacyMissions = JsonUtility.ToJson(d.missions);
            d.economy.property = null;
            g.RestoreState(d);
            g.clock.realtimeEnabled = false;
            check(
                e.Properties.owned.Count == 0
                    && e.Properties.contract == null
                    && e.Cash == legacyCash
                    && e.Debt == legacyDebt
                    && JsonUtility.ToJson(g.CaptureState().missions) == legacyMissions,
                "Old v4 save without property data initializes only the new extension"
            );
            Fixture();
            var utc = e.Properties.marketUtcTicks;
            long later = utc + e.propertyCatalog.marketIntervalSeconds * TimeSpan.TicksPerSecond * 5L;
            e.SettleMarket(later);
            check(
                e.Properties.marketSequence == 5
                    && e.Properties.quotes.All(q => q.PriceHistory.Count == 6 && q.MarketPrice > 0),
                "Regional and type market advances using the existing offline market entry"
            );
            before = JsonUtility.ToJson(e.Properties);
            e.SettleMarket(later);
            check(
                before == JsonUtility.ToJson(e.Properties),
                "Repeated offline timestamp does not repeat property market updates"
            );
            e.SettlePropertyMarket(utc - TimeSpan.TicksPerSecond);
            check(before == JsonUtility.ToJson(e.Properties), "Reversed clock cannot rewind or repeat property prices");
            e.SettleMarket(later + TimeSpan.TicksPerDay * 10L);
            check(
                e.Properties.quotes.All(q =>
                    q.PriceHistory.Count <= e.propertyCatalog.historyLimit
                    && q.MarketPrice > 0
                    && q.MarketPrice <= e.propertyCatalog.Find(q.PropertyId).PurchasePrice * 4
                ),
                "Long absence is capped and price/history bounds remain valid"
            );
            Fixture();
            long insurance = g.missions.Counter(MissionCounter.InsuranceLearned);
            e.LearnHousingInsurance();
            e.LearnHousingInsurance();
            check(
                g.missions.Counter(MissionCounter.InsuranceLearned) == insurance + 1,
                "Original M046 insurance information event is delivered once"
            );
            check(
                (e.Features & MissionFeature.Housing) != 0,
                "Existing Housing mission feature is enabled by the new service"
            );
            var core = AppDomain
                .CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("IanGame.Editor.Phase25CoreVerification"))
                .First(t => t != null);
            var phase25 = (string)core.GetMethod("Run").Invoke(null, null);
            check(
                string.IsNullOrEmpty(JsonUtility.FromJson<FinanceResult>(phase25).failure),
                "Existing Phase25 isolated finance regression completed"
            );
            // Actual world interaction and original UI stack, not a disconnected UI mockup.
            var agent = w.objects.FirstOrDefault(o => o.kind == WorldInteractionKind.RealEstate);
            check(agent != null && agent.zone == "realestate", "Agency interior has the existing property interaction");
            check(w.Travel(agent.zone), "Agency place transition still works");
            while (w.Busy)
            {
                yield return null;
            }

            w.Player.Warp(agent.Point);
            yield return new WaitForSecondsRealtime(.2f);
            check(w.Use(agent), "Player reaches agency through the real E interaction command");
            var ui = UnityEngine.Object.FindFirstObjectByType<PropertyPresenter>();
            check(
                ui != null && ui.shell.Top == ui.panel,
                "Agency opens on original pixel UI stack and blocks world controls"
            );
            Fixture(); // Restore changes do not replace scene UI objects.
            ui.Open();
            ui.Select(0);
            ui.Action(0);
            yield return new WaitForSecondsRealtime(.2f);
            check(ui.shell.Top == ui.confirmation, "Purchase opens a separate scrollable confirmation");
            ui.accept.onClick.Invoke();
            yield return null;
            check(
                e.Properties.owned.Any(h => h.PropertyId == "property-0"),
                "Actual confirmation button purchases and saves selected property"
            );
            ui.accept.onClick.Invoke();
            check(e.Properties.owned.Count == 1, "Repeated confirmation cannot duplicate the purchase");
            ui.Tab(0);
            ui.Select(1);
            ui.Action(2);
            yield return new WaitForSecondsRealtime(.2f);
            ui.accept.onClick.Invoke();
            yield return null;
            check(
                e.Properties.contract?.PropertyId == "property-1",
                "Actual rental confirmation button creates a durable lease"
            );
            int oldLarge = PlayerPrefs.GetInt("ian.ui.largeText", 0);
            try
            {
                foreach (
                    var size in new[]
                    {
                        new[] { 1280, 720 },
                        new[] { 1920, 1080 },
                        new[] { 904, 2316 },
                        new[] { 1840, 2208 },
                    }
                )
                {
                    CommercialSliceVerification.SetSize(size[0], size[1]);
                    yield return new WaitForSecondsRealtime(.3f);
                    for (int page = 0; page < 6; page++)
                    {
                        ui.Tab(page);
                        yield return new WaitForSecondsRealtime(.15f);
                        audit(ui.panel, "Phase27 page " + page + " / " + size[0]);
                    }

                    ui.Tab(0);
                    ScreenCapture.CaptureScreenshot("Docs/Phase27/Screenshots/Agency-" + size[0] + ".png");
                    yield return null;
                }

                PlayerPrefs.SetInt("ian.ui.largeText", 1);
                foreach (var scale in ui.GetComponentsInChildren<UITypeScale>(true))
                {
                    scale.Apply();
                }

                ui.Tab(2);
                yield return new WaitForSecondsRealtime(.2f);
                audit(ui.panel, "Phase27 large Korean contract");
            }
            finally
            {
                PlayerPrefs.SetInt("ian.ui.largeText", oldLarge);
                CommercialSliceVerification.SetSize(1280, 720);
            }

            ui.close.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.2f);
            check(w.CanControl, "Closing agency restores player control");
        }
    }
}
#endif
