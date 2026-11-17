#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace IanGame.Editor
{
    public static class PrePhase28Verification
    {
        public static IEnumerator Birth(LifeWorldController w, GameManager g, Action<bool, string> check)
        {
            check(
                g.growth.Age == 0 && g.economy.Cash == 0 && g.economy.Debt == 1000000,
                "Newborn age/cash/debt match original design"
            );
            var bed = w.objects.First(o => o.id == "bed");
            var start = w.Player.Position;
            w.Player.BeginTraversal(start + Vector2.right * .6f);
            yield return new WaitForSecondsRealtime(.4f);
            var delta = Vector2.Distance(start, w.Player.Position);
            w.Player.CancelTraversal();
            check(delta < .02f, "Newborn cannot walk through motor execution; observed displacement=" + delta);
            check(
                w.Player.advanced.Motion != ActorMotion.Walk && w.Player.advanced.Motion != ActorMotion.Run,
                "No newborn walk/run motion"
            );
            check(w.IsInReach(bed), "Immobile newborn can reach existing bed care without adult walking");
            foreach (int action in new[] { 0, 7, 20, 21, 26 })
            {
                check(!g.CanPerformAction(action, out _), "Newborn action rejected " + action);
            }

            check(
                !w.Travel("city")
                    && !w.Travel("workplace")
                    && !g.economy.Trade("audit/infant", "IAN", 1, true)
                    && !g.economy.BuyProperty("audit/infant-home", "property-0"),
                "Newborn travel and direct finance blocked"
            );
            SaveData saved = null;
            check(
                g.SaveAutomaticCheckpoint(out _) && g.saves.TryReadAutomatic(out saved, out _),
                "Newborn actual disk checkpoint"
            );
            g.RestoreState(saved);
            g.clock.realtimeEnabled = false;
            check(
                !g.BirthIntroductionPending && g.growth.Age == 0 && w.IsInReach(bed) && !w.Player.MovementAllowed,
                "Reload preserves birth acknowledgement and accessible care"
            );
        }

        public static IEnumerator FinanceGate(GameManager g, Action<bool, string> check)
        {
            var stats = g.stats.Capture();
            stats.money = 1000;
            g.stats.Restore(stats);
            var before = g.economy.Cash;
            check(
                !g.economy.RepayDebt("audit/newborn-repay", 100) && g.economy.Cash == before,
                "Newborn direct debt repayment cannot bypass age restriction"
            );
            check(
                !g.economy.TryTransaction("audit/generic-repay", TransactionKind.DebtRepayment, 100, "test")
                    && g.economy.Cash == before,
                "Generic transaction cannot bypass age gate"
            );
            g.economy.Finance.bills.Add(
                new TaxBill
                {
                    id = "audit/infant-bill",
                    amount = 100,
                    issuedDay = 1,
                    dueDay = 7,
                }
            );
            check(
                !g.economy.PayTax("audit/infant-tax", "audit/infant-bill", 100) && g.economy.Cash == before,
                "Newborn tax execution blocked; passive bills retained"
            );
            yield break;
        }

        static void Checkpoint(GameManager g)
        {
            if (!g.SaveAutomaticCheckpoint(out var reason))
            {
                throw new Exception("Natural checkpoint: " + reason);
            }

            System.IO.File.WriteAllText(
                "Docs/PrePhase28/NATURAL_CHECKPOINT.json",
                JsonUtility.ToJson(g.CaptureState(), true)
            );
        }

        static IEnumerator Go(GameManager g, SceneId scene)
        {
            var t = GameContext.Instance.transitions;
            if (!t.NavigateForTesting(scene))
            {
                throw new Exception("Natural route rejected " + scene);
            }

            while (t.IsTransitioning)
            {
                yield return null;
            }

            g.clock.realtimeEnabled = false;
            g.clock.SetRunning(true);
            yield return null;
        }

        static void Care(GameManager g)
        {
            for (int n = 0; n < 3; n++)
            {
                if (g.stats.Hunger < 65 && !g.TryAction(3))
                {
                    throw new Exception("Meal recovery failed");
                }

                if (g.stats.Thirst < 65 && !g.TryAction(4))
                {
                    throw new Exception("Drink recovery failed");
                }

                if ((g.stats.Health < 65 || g.stats.Mental < 65) && !g.TryAction(5))
                {
                    throw new Exception("Sleep recovery failed");
                }
            }

            if (g.State != GameState.Playing)
            {
                throw new Exception("Care did not preserve playable state");
            }
        }

        public static IEnumerator Natural(
            LifeWorldController w,
            GameManager g,
            Action<bool, string> check,
            bool resume = false
        )
        {
            if (resume)
            {
                g.RestoreState(
                    JsonUtility.FromJson<SaveData>(
                        System.IO.File.ReadAllText("Docs/PrePhase28/NATURAL_CHECKPOINT.json")
                    )
                );
                g.clock.realtimeEnabled = false;
                check(g.missions.Claimed("M001"), "Resume actual audit-earned checkpoint, not a mission fixture");
            }
            else
            {
                yield return Birth(w, g, check);
            }

            g.world.rules.activityAnimationScale = .02f;
            var bed = w.objects.First(o => o.id == "bed");
            for (int i = 1; i <= 10; i++)
            {
                var m = g.missionCatalog.missions.First(m => m.number == i);
                if (g.missions.Claimed(m.Id))
                {
                    continue;
                }

                int limit = 0;
                while (g.missions.GetProgress(m).state != MissionState.Completed && limit++ < 15)
                {
                    int action =
                        i == 1 ? 4
                        : i == 2 ? 3
                        : i == 3 ? 5
                        : i == 4 ? 3
                        : i == 5 ? 4
                        : i == 6 ? 1
                        : 5;
                    check(w.Begin(bed, action), "Actual bed care for " + m.Id);
                    while (w.Busy)
                    {
                        yield return null;
                    }

                    g.missions.Refresh();
                }

                check(g.missions.GetProgress(m).state == MissionState.Completed, "Natural conditions " + m.Id);
                check(g.missions.Claim(m.Id), "Natural claim " + m.Id);
                long cash = g.economy.Cash;
                check(!g.missions.Claim(m.Id) && g.economy.Cash == cash, "Natural reward dedup " + m.Id);
                Checkpoint(g);
            }

            check(
                g.growth.Age >= 7 && g.missions.Claimed("M010"),
                "M001-M010 actual actions unlock Childhood without age injection"
            );
            yield return Go(g, SceneId.School);
            var roll = g.phase23.TestRollSource;
            g.phase23.TestRollSource = () => 0;
            try
            {
                for (int i = 11; i <= 38; i++)
                {
                    var m = g.missionCatalog.missions.First(m => m.number == i);
                    if (g.missions.Claimed(m.Id))
                    {
                        continue;
                    }

                    int attempts = 0;
                    while (
                        g.missions.GetProgress(m).state != MissionState.Completed && attempts++ < (i == 36 ? 180 : 50)
                    )
                    {
                        Care(g);
                        int action = -1;
                        switch (i)
                        {
                            case 11:
                            case 21:
                                action = 7;
                                break;
                            case 12:
                                action = 8;
                                break;
                            case 13:
                            case 22:
                            case 23:
                                action = 14;
                                break;
                            case 14:
                                action = 9;
                                break;
                            case 15:
                            case 26:
                                action = 18;
                                break;
                            case 16:
                                action = 10;
                                break;
                            case 17:
                            case 27:
                                action = 11;
                                break;
                            case 18:
                                action = 12;
                                break;
                            case 19:
                                action = 13;
                                break;
                            case 24:
                                action = 24;
                                break;
                            case 25:
                                action = 25;
                                break;
                            case 28:
                                action = 15;
                                break;
                            case 29:
                                action = 5;
                                break;
                            case 30:
                                var education = g.phase23.Education;
                                var data = education.Available;
                                var current = education.Current;
                                if (current.studies < data.graduationStudies)
                                {
                                    action = 14;
                                }
                                else if (current.homework < data.graduationHomework)
                                {
                                    action = 10;
                                }
                                else if (
                                    education.Progress.exams.Any(e =>
                                        e.educationId == current.educationId && !e.reportReceived
                                    )
                                )
                                {
                                    action = 12;
                                }
                                else if (
                                    !education.Progress.exams.Any(e =>
                                        e.educationId == current.educationId
                                        && e.reportReceived
                                        && e.score >= data.graduationScore
                                    )
                                )
                                {
                                    action = 11;
                                }
                                else
                                {
                                    action = 16;
                                }

                                break;
                            case 31:
                                action = 20;
                                break;
                            case 32:
                                action = 21;
                                break;
                            case 34:
                            case 37:
                                action = 0;
                                break;
                            case 35:
                                if (g.economy.Finance.bills.Any(b => b.Remaining > 0))
                                {
                                    var bill = g.economy.Finance.bills.First(b => b.Remaining > 0);
                                    if (g.economy.Cash >= bill.Remaining)
                                    {
                                        check(
                                            g.economy.PayTax("audit/tax/" + Guid.NewGuid(), bill.id, bill.Remaining),
                                            "Natural tax payment"
                                        );
                                    }
                                    else
                                    {
                                        action = 0;
                                    }
                                }
                                else
                                {
                                    action = 5;
                                }

                                break;
                            case 36:
                                var best = g
                                    .phase23.Jobs.Search()
                                    .OrderByDescending(j => j.BaseDailyWage)
                                    .FirstOrDefault();
                                if (best != null && best.BaseDailyWage > g.phase23.Work.Wage)
                                {
                                    yield return Go(g, SceneId.Workplace);
                                    check(g.TryAction(23), "Natural career resignation");
                                    g.phase23.Progress.employment.selectedJobId = best.JobId;
                                    check(g.TryAction(21), "Natural eligible higher-wage interview");
                                }
                                else if (g.economy.Cash > 0)
                                {
                                    check(
                                        g.economy.RepayDebt(
                                            "audit/debt/" + Guid.NewGuid(),
                                            Math.Min(g.economy.Debt, g.economy.Cash)
                                        ),
                                        "Natural debt repayment"
                                    );
                                }
                                else
                                {
                                    action = 0;
                                }

                                break;
                            case 38:
                                if (g.stats.Credit < g.economy.rules.minimumTradingCredit)
                                {
                                    if (g.economy.Cash >= g.economy.rules.minimumDebtPayment)
                                    {
                                        check(
                                            g.economy.RepayDebt(
                                                "audit/credit-recovery/" + Guid.NewGuid(),
                                                g.economy.rules.minimumDebtPayment
                                            ),
                                            "Natural repayment restores trading credit"
                                        );
                                    }
                                    else
                                    {
                                        action = 0;
                                    }
                                }
                                else if (g.economy.Cash >= g.economy.GetQuote("IAN").CurrentPrice)
                                {
                                    yield return Go(g, SceneId.StockMarket);
                                    check(
                                        g.economy.Trade("audit/stock/" + Guid.NewGuid(), "IAN", 1, true),
                                        "Natural first stock purchase"
                                    );
                                }
                                else
                                {
                                    action = 0;
                                }

                                break;
                        }

                        if (action >= 0)
                        {
                            if (action >= 7 || action == 0)
                            {
                                var destination = g.phase23.Destination(action);
                                if (GameContext.Instance.transitions.CurrentScene != destination)
                                {
                                    yield return Go(g, destination);
                                }
                            }

                            if (!g.TryAction(action))
                            {
                                if (action == 9 || action == 18)
                                {
                                    check(g.TryAction(5), "Natural next-day social cooldown");
                                }
                                else
                                {
                                    throw new Exception(
                                        "Natural action rejected "
                                            + m.Id
                                            + " action="
                                            + action
                                            + " age="
                                            + g.growth.Age
                                            + " reason="
                                            + (g.CanPerformAction(action, out var why) ? "execution" : why)
                                    );
                                }
                            }
                        }

                        g.missions.Refresh();
                        yield return null;
                    }

                    check(g.missions.GetProgress(m).state == MissionState.Completed, "Natural conditions " + m.Id);
                    check(g.missions.Claim(m.Id), "Natural claim " + m.Id);
                    long cash = g.economy.Cash;
                    check(!g.missions.Claim(m.Id) && g.economy.Cash == cash, "Natural reward dedup " + m.Id);
                    Checkpoint(g);
                }
            }
            finally
            {
                g.phase23.TestRollSource = roll;
            }

            check(g.missions.Claimed("M038") && g.growth.Age >= 19, "Real actions reached employment and investment");
            SaveData save = null;
            check(
                g.SaveAutomaticCheckpoint(out _) && g.saves.TryReadAutomatic(out save, out _),
                "Natural life checkpoint"
            );
            g.RestoreState(save);
            check(g.missions.Claimed("M038"), "Natural life restore");
        }

        public static IEnumerator Traffic(LifeWorldController w, GameManager g, Action<bool, string> check)
        {
            g.clock.Restore(
                g.clock.Day,
                new TimeSnapshot { subDaySeconds = 12 * 3600, elapsedSeconds = g.clock.ElapsedSeconds }
            );
            check(w.Travel("city"), "Traffic player enters actual city");
            while (w.Busy)
            {
                yield return null;
            }

            w.Player.Warp(w.CurrentZone.origin + new Vector2(0, 1.5f));
            yield return new WaitForSecondsRealtime(.4f);
            var street = w.GetComponentInChildren<WorldStreetLife>();
            check(street != null && street.vehicles.Length == 4, "Four actual traffic rigidbodies present");
            var starts = street.vehicles.Select(v => v.body.position).ToArray();
            var moved = new bool[starts.Length];
            var signals = new System.Collections.Generic.HashSet<Sprite>();
            float until = Time.unscaledTime + 27;
            bool stopped = false;
            while (Time.unscaledTime < until)
            {
                for (int i = 0; i < starts.Length; i++)
                {
                    moved[i] |= Vector2.Distance(starts[i], street.vehicles[i].body.position) > 1;
                }

                if (street.signal != null)
                {
                    signals.Add(street.signal.sprite);
                }

                stopped |= !street.VehicleGreen && street.vehicles.Any(v => v.velocity < .01f);
                yield return null;
            }

            check(moved.All(v => v), "All four real vehicles move during observed signal cycle");
            check(signals.Count == 3 && stopped, "Actual signal animation and vehicle braking");
            ScreenCapture.CaptureScreenshot("Docs/PrePhase28/Screenshots/City-Day.png");
            yield return null;
            g.clock.Restore(
                g.clock.Day,
                new TimeSnapshot { subDaySeconds = 23 * 3600 + 50 * 60, elapsedSeconds = g.clock.ElapsedSeconds }
            );
            yield return new WaitForSecondsRealtime(.5f);
            check(
                street.vehicles.Count(v => v.body.simulated) == 2,
                "Night traffic follows configured two-vehicle density"
            );
            check(
                street.vehicles.Where(v => v.body.simulated).All(v => v.headlights != null && v.headlights.enabled),
                "Actual night vehicle lights enabled"
            );
            ScreenCapture.CaptureScreenshot("Docs/PrePhase28/Screenshots/City-Night.png");
            yield return null;
        }
    }
}
#endif
