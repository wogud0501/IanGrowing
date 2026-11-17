#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class FamilyLifeVerification
    {
        static WorldInteractable Find(LifeWorldController w, string id) => w.objects.First(o => o.id == id);

        static IEnumerator Wait(LifeWorldController w)
        {
            float end = Time.unscaledTime + 25;
            while (w.Busy && Time.unscaledTime < end)
            {
                yield return null;
            }

            if (w.Busy)
            {
                throw new Exception("Family action/travel timeout");
            }

            yield return new WaitForSecondsRealtime(.2f);
        }

        static void Cash(GameManager g, long cash)
        {
            var s = g.stats.Capture();
            s.money = (int)cash;
            g.stats.Restore(s);
        }

        static IEnumerator Shot(string name)
        {
            Directory.CreateDirectory("Docs/FamilyLife/Screenshots");
            ScreenCapture.CaptureScreenshot("Docs/FamilyLife/Screenshots/" + name + ".png");
            yield return null;
        }

        public static IEnumerator Run(
            LifeWorldController w,
            GameManager g,
            Action<bool, string> check,
            Action<GameObject, string> audit,
            string mode
        )
        {
            g.clock.realtimeEnabled = false;
            var family = FamilyLife.State(g);
            var scheduler = w.GetComponent<WorldNPCScheduler>();
            check(w.CurrentZone.id == FamilyLife.Home && FamilyLife.AtFamily(g), "Real newborn starts in family home");
            check(
                g.growth.Age == 0 && g.economy.Cash == 0 && g.economy.Debt == 1000000,
                "Newborn finances and growth preserved"
            );
            check(
                scheduler.actors.Length == 17 && scheduler.actors.Count(n => FamilyLife.Parent(n.npcId)) == 2,
                "Two distinct parents plus all fifteen existing residents"
            );
            check(
                g.economy.Housing.owned.SequenceEqual(new[] { "basic-bed" }),
                "Family furniture never added to personal inventory"
            );
            check(
                w.objects.Count(o => o.zone == FamilyLife.Home && o.visual != null) > 10,
                "Family home has actual furnished functional objects"
            );
            check(
                !w.Player.MovementAllowed && !w.Travel("city") && !g.CanPerformAction(0, out _),
                "Age0 walking, outdoor and adult work blocked"
            );
            audit(w.hud.gameObject, "Family newborn HUD");
            yield return new WaitForSecondsRealtime(.6f);
            yield return Shot("Birth-FamilyHome");
            if (mode == "familyCheck")
            {
                var starting = w.Player.Position;
                w.Player.BeginTraversal(starting + Vector2.right);
                yield return new WaitForSecondsRealtime(.2f);
                check(
                    Vector2.Distance(starting, w.Player.Position) < .02f,
                    "Age0 internal traversal cannot bypass walking gate"
                );
                g.growth.Restore(new ProgressSnapshot { age = 1 });
                w.Player.BeginTraversal(starting + Vector2.down * .4f);
                yield return new WaitForSecondsRealtime(.5f);
                check(
                    Vector2.Distance(starting, w.Player.Position) > .15f && w.Player.MovementAllowed,
                    "Age1 actual internal walking unlocks"
                );
                w.Player.CancelTraversal();
                check(!w.Travel("alley"), "Age1 walking does not unlock independent outdoor travel");
                g.growth.Restore(new ProgressSnapshot { age = 6 });
                check(!w.Travel("city"), "Age6 independent outdoors still blocked");
                g.growth.Restore(new ProgressSnapshot { age = 7 });
                check(w.Travel("alley"), "Age7 independent outdoor travel unlocks");
                yield return Wait(w);
                check(w.Travel(FamilyLife.Home), "Child returns to family home");
                yield return Wait(w);
                g.growth.Restore(new ProgressSnapshot { age = 18 });
                Cash(g, 10000);
                check(
                    !g.economy.CanMoveResidence("starter", out _) && !g.economy.MoveResidence("qa-underage", "starter"),
                    "Age18 independence rejected in economy execution"
                );
                g.growth.Restore(new ProgressSnapshot { age = 19 });
                check(
                    FamilyLife.AtFamily(g) && w.CurrentZone.id == FamilyLife.Home,
                    "Age19 never forces an automatic move"
                );
                Cash(g, 499);
                check(
                    !g.economy.MoveResidence("qa-lowcash", "starter") && g.economy.Cash == 499,
                    "Independence checks original500G fee atomically"
                );
                Cash(g, 1000);
                var owned = g.economy.Housing.owned.ToArray();
                w.hud.OpenHousing();
                yield return null;
                var home = w.hud.GetComponent<HomeManagementPresenter>();
                home.Tab(3);
                home.move[0].onClick.Invoke();
                yield return null;
                check(home.confirmation.activeInHierarchy, "Real housing UI requires independence confirmation");
                home.accept.onClick.Invoke();
                yield return Wait(w);
                check(
                    !FamilyLife.AtFamily(g) && w.CurrentZone.id == "home" && g.economy.Cash == 500,
                    "Confirmed existing housing UI charges500G and enters personal home"
                );
                check(
                    g.economy.Housing.owned.SequenceEqual(owned) && g.economy.Housing.placed.Count == 0,
                    "First independent home starts with bed; personal inventory preserved"
                );
                check(
                    w.objects.Where(o => o.zone == "home" && StarterHomePresentation.Optional(o.id))
                        .All(o => !o.gameObject.activeInHierarchy),
                    "No free optional personal-home furniture"
                );
                yield return Shot("Independent-BedOnly");
                check(
                    !g.economy.MoveResidence("qa-duplicate", "starter") && g.economy.Cash == 500,
                    "Duplicate independence/move cannot charge again"
                );
                check(
                    scheduler.actors.Count(a => FamilyLife.Parent(a.npcId)) == 2,
                    "Parents remain after independence"
                );
                Cash(g, 100000);
                check(
                    g.economy.BuyFurniture("qa-family-fridge", "fridge"),
                    "Personal furniture uses existing wallet purchase"
                );
                check(
                    g.economy.Housing.owned.Contains("fridge")
                        && !g.economy.Housing.owned.Any(id => id.StartsWith("family-")),
                    "Family furniture ownership never transfers"
                );
                check(w.Travel(FamilyLife.Home), "Independent player can visit parents");
                yield return Wait(w);
                check(!FamilyLife.AtFamily(g), "Visiting family never changes independent residence");
                var snapshot = g.CaptureState();
                SaveData disk = null;
                check(
                    g.SaveAutomaticCheckpoint(out _) && g.saves.TryReadAutomatic(out disk, out _),
                    "Family and independence save through real isolated disk slot"
                );
                g.RestoreState(disk);
                g.clock.realtimeEnabled = false;
                check(
                    !FamilyLife.AtFamily(g)
                        && g.economy.Housing.owned.Contains("fridge")
                        && FamilyLife.State(g).schema == 1,
                    "Disk restore retains residence and purchased inventory"
                );
                var legacy = g.CaptureState();
                legacy.world.family = null;
                legacy.progression.age = 1;
                long cash = g.economy.Cash;
                g.RestoreState(legacy);
                g.clock.realtimeEnabled = false;
                check(
                    FamilyLife.AtFamily(g)
                        && w.CurrentZone.id == FamilyLife.Home
                        && g.economy.Cash == cash
                        && g.economy.Housing.owned.Contains("fridge"),
                    "Legacy child save migrates to family without erasing funds or purchases"
                );
                legacy = g.CaptureState();
                legacy.world.family = null;
                legacy.world.zone = "home";
                legacy.progression.age = 30;
                g.RestoreState(legacy);
                g.clock.realtimeEnabled = false;
                check(
                    !FamilyLife.AtFamily(g) && g.economy.Housing.owned.Contains("fridge"),
                    "Legacy adult personal home remains independent with possessions"
                );
                var young = g.CaptureState();
                young.progression.age = 6;
                g.RestoreState(young);
                g.clock.realtimeEnabled = false;
                check(
                    FamilyLife.AtFamily(g)
                        && FamilyLife.State(g).residency == HomeResidency.FamilyHome
                        && w.CurrentZone.id == FamilyLife.Home
                        && g.economy.Housing.owned.Contains("fridge"),
                    "Existing independent-state save cannot bypass under19 family residence; possessions retained"
                );
                yield break;
            }

            if (mode == "familyActions" || mode == "familyInteraction")
            {
                var bed = Find(w, "family-bed");
                w.Player.Warp(bed.Point);
                var s = g.stats.Capture();
                s.hunger = 20;
                g.stats.Restore(s);
                check(w.Begin(bed, 3), "Newborn care meal uses real bed care action");
                yield return Wait(w);
                check(
                    g.stats.Hunger > 20 && !w.Player.MovementAllowed,
                    "Newborn feeding restores real hunger without enabling walking"
                );
                g.growth.Restore(new ProgressSnapshot { age = 19 });
                g.clock.Restore(1, new TimeSnapshot { subDaySeconds = 10 * 3600 });
                w.Player.Warp(bed.Point);
                var detail = bed.GetComponent<WorldLifeAction>();
                var seen = new HashSet<int>();
                check(w.Begin(bed, 5), "Actual bed sleep starts");
                float end = Time.unscaledTime + 20;
                while (w.Busy && Time.unscaledTime < end)
                {
                    if (detail.overlay.enabled)
                    {
                        seen.Add(detail.Frame);
                    }

                    yield return null;
                }

                check(
                    !w.Busy && seen.Contains(3) && seen.Contains(4) && seen.Contains(7),
                    "Bed shows real lying, sleeping, waking and getting out frames"
                );
                check(
                    w.CanControl && Vector2.Distance(w.Player.Position, bed.Point) < .4f,
                    "Sleep returns to safe bed approach and controls"
                );
                check(g.clock.SubDaySeconds >= 18 * 3600 - 1, "Actual sleep advances existing clock by eight hours");
                yield return Shot("Family-AfterSleep");
                g.clock.Restore(2, new TimeSnapshot { subDaySeconds = 10 * 3600 });
                var fridge = Find(w, "family-fridge");
                w.Player.Warp(fridge.Point);
                var root = fridge.transform.position;
                int pantry = FamilyLife.State(g).pantry;
                check(w.Begin(fridge, -1, "bread"), "Actual family refrigerator interaction starts");
                seen.Clear();
                detail = fridge.GetComponent<WorldLifeAction>();
                while (w.Busy)
                {
                    if (detail.overlay.enabled)
                    {
                        seen.Add(detail.Frame);
                    }

                    yield return null;
                }

                check(
                    seen.Count >= 4 && fridge.transform.position == root && fridge.visual.enabled,
                    "Opaque hinged refrigerator frames open/close without root displacement"
                );
                check(
                    FamilyLife.State(g).pantry == pantry - 1 && g.world.State.prepared == "bread",
                    "Refrigerator consumes real shared ingredient once"
                );
                var table = Find(w, "family-player-seat");
                w.Player.Warp(table.Point);
                int meals = FamilyLife.State(g).meals;
                check(w.Begin(table, 3), "Real family table begins meal");
                check(!w.Begin(table, 3), "Meal duplicate input rejected");
                detail = table.GetComponent<WorldLifeAction>();
                seen.Clear();
                while (w.Busy)
                {
                    if (detail.frames.Length == 0)
                    {
                        seen.Add(w.GetComponentInChildren<FamilySharedDiningPresentation>().Frame);
                    }
                    else if (detail.overlay.enabled)
                    {
                        seen.Add(detail.Frame);
                    }

                    yield return null;
                }

                check(
                    seen.Count >= 3 && FamilyLife.State(g).meals == meals - 1,
                    "Seated spoon animation consumes exactly one family portion"
                );
                check(
                    table.GetComponent<WorldActivitySlot>().State == ActivityOccupancy.Free && w.CanControl,
                    "Meal stands up, releases chair and restores control"
                );
                if (mode == "familyInteraction")
                {
                    w.Player.Warp(fridge.Point);
                    int stock = FamilyLife.State(g).pantry;
                    check(w.Begin(fridge, -1, "rice"), "Start cancellable real fridge action");
                    yield return new WaitForSecondsRealtime(.2f);
                    w.CancelAction();
                    yield return Wait(w);
                    check(
                        FamilyLife.State(g).pantry == stock
                            && !fridge.GetComponent<WorldLifeAction>().overlay.enabled
                            && w.CanControl,
                        "Cancel closes appliance, preserves ingredients and returns control"
                    );
                    yield break;
                }

                // Observe each authored household stage with actual walking between objects.
                check(w.Travel(FamilyLife.Home), "Observe mother cooking in existing family room");
                yield return Wait(w);
                w.Player.Warp(w.CurrentZone.origin + new Vector2(-1, -5));
                g.clock.Restore(3, new TimeSnapshot { subDaySeconds = 7 * 3600 + 60 });
                yield return new WaitForSecondsRealtime(8);
                check(
                    FamilyLife.State(g).ingredientCarrier == "family-mother",
                    "Mother opens fridge and actually takes shared ingredients"
                );
                g.clock.Restore(3, new TimeSnapshot { subDaySeconds = 7 * 3600 + 11 * 60 });
                yield return new WaitForSecondsRealtime(9);
                check(
                    FamilyLife.State(g).preparedMeals == 3 && FamilyLife.State(g).cook == "family-mother",
                    "Mother walks to counter, animates cooking and prepares three portions"
                );
                yield return Shot("Mother-Cooking");
                g.clock.Restore(3, new TimeSnapshot { subDaySeconds = 7 * 3600 + 31 * 60 });
                yield return new WaitForSecondsRealtime(9);
                check(
                    FamilyLife.State(g).preparedMeals == 0 && FamilyLife.State(g).announcement.Contains("준비"),
                    "Mother walks to serving point and sets shared meals"
                );
                int portions = FamilyLife.State(g).meals;
                var serve = Find(w, "family-serve");
                long key = FamilyLife.State(g).servedKey;
                FamilyLife.Use(g, "family-mother", serve, key);
                yield return new WaitForSecondsRealtime(1);
                check(
                    FamilyLife.State(g).meals == portions,
                    "Household receipt prevents duplicate food after repeated use"
                );
                g.clock.Restore(3, new TimeSnapshot { subDaySeconds = 12 * 3600 + 60 });
                yield return new WaitForSecondsRealtime(10);
                check(
                    FamilyLife.State(g).ingredientCarrier == "family-father",
                    "Father independently takes lunch ingredients"
                );
                g.clock.Restore(3, new TimeSnapshot { subDaySeconds = 12 * 3600 + 11 * 60 });
                yield return new WaitForSecondsRealtime(10);
                check(
                    FamilyLife.State(g).cook == "family-father" && FamilyLife.State(g).preparedMeals == 3,
                    "Father also performs visible cooking; chores are shared"
                );
                yield return Shot("Father-Cooking");
                g.clock.Restore(3, new TimeSnapshot { subDaySeconds = 12 * 3600 + 36 * 60 });
                yield return new WaitForSecondsRealtime(10);
                check(
                    Find(w, "family-dine-2").GetComponent<WorldActivitySlot>().Occupant == "family-father",
                    "Parent sits in assigned actual dining chair"
                );
                yield return Shot("Family-Dining");
                yield break;
            }

            if (mode == "familyDoors")
            {
                g.growth.Restore(new ProgressSnapshot { age = 19 });
                Cash(g, 100000);
                check(
                    g.economy.MoveResidence("qa-doors-independent", "starter"),
                    "Door regression uses actual qualified independence fixture"
                );
                foreach (
                    string id in new[]
                    {
                        "family-front",
                        "family-exit",
                        "house-entrance",
                        "front-door",
                        "bath-door",
                        "bath-exit",
                        "school-front",
                        "school-exit",
                        "office-front",
                        "work-exit",
                        "finance-front",
                        "finance-exit",
                        "realestate-front",
                        "realestate-exit",
                        "shop-front",
                        "shop-exit",
                    }
                )
                {
                    var o = Find(w, id);
                    check(w.Travel(o.zone), "Door source " + id);
                    yield return Wait(w);
                    g.clock.Restore(1, new TimeSnapshot { subDaySeconds = 14 * 3600 });
                    w.Player.Warp(o.Point);
                    yield return null;
                    var door = o.GetComponent<WorldDoor>();
                    var pos = door.transform.position;
                    check(w.Use(o) && !w.Use(o), "Real door and repeated input " + id);
                    yield return Wait(w);
                    check(
                        w.CurrentZone.id == o.targetZone && w.CanControl && door.transform.position == pos,
                        "Door arrival, controls, fixed root " + id
                    );
                }

                yield break;
            }
        }
    }
}
#endif
