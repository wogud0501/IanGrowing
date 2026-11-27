#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public sealed partial class IanGaitVerification
    {
        bool WorldCommand(string command)
        {
            if (!command.StartsWith("world-", StringComparison.Ordinal))
            {
                return false;
            }
            if (command == "world-status")
            {
                WorldStatus();
            }
            else if (!auditing)
            {
                string[] words = command.Split(' ');
                if (command == "world-audit")
                {
                    StartCoroutine(AuditWorld());
                }
                if (words[0] == "world-returns")
                {
                    StartCoroutine(AuditHomeReturns(words.Length == 2 ? words[1] : null));
                }
                if (command == "world-commute" || command == "world-door")
                {
                    StartCoroutine(AuditHomeDay(command == "world-door"));
                }
                if (words.Length == 2 && words[0] == "world-click" && World.hud.shell.Top)
                {
                    var label = words[1];
                    var button = World
                        .hud.shell.Top.GetComponentsInChildren<Button>()
                        .FirstOrDefault(b =>
                            b.interactable
                            && b.GetComponentInChildren<TMP_Text>()?.text.StartsWith(label, StringComparison.Ordinal)
                                == true
                        );
                    if (button)
                    {
                        var pointer = new UnityEngine.EventSystems.PointerEventData(
                            UnityEngine.EventSystems.EventSystem.current
                        );
                        UnityEngine.EventSystems.ExecuteEvents.Execute(
                            button.gameObject,
                            pointer,
                            UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler
                        );
                    }
                }
                if (
                    words.Length == 2
                    && words[0] == "world-time"
                    && int.TryParse(words[1], out var minute)
                    && minute >= 0
                    && minute < 1440
                )
                {
                    VisitHour(game.clock.Day, minute);
                }
                if (words.Length == 2 && words[0] == "world-enter")
                {
                    var room = World.Room.catalog.Find(words[1]);
                    if (room != null)
                    {
                        Transitions.NavigateRoom(
                            room.roomId,
                            World.Service.rules.Zone(room.roomId).spawn,
                            Vector2.down
                        );
                    }
                }
                if (words.Length == 2 && words[0] == "world-use")
                {
                    var target = World.objects.FirstOrDefault(o => o && o.id == words[1] && o.isActiveAndEnabled);
                    if (target)
                    {
                        World.Player.Warp(
                            World.Safe(World.CurrentZone, target.Point - World.CurrentZone.origin + Vector2.down * .18f)
                        );
                        World.Use(target);
                        WorldStatus();
                    }
                }
            }
            return true;
        }

        void WorldStatus()
        {
            var shell = World.hud.shell;
            var lines = new System.Collections.Generic.List<string>
            {
                $"{World.Room.roomId}\t{game.clock.Day}\t{game.economy.Cash}\t{World.Player.Position}\t{(shell.Top ? shell.Top.name : "world")}\t{World.CanControl}",
            };
            if (shell.Top)
            {
                Canvas.ForceUpdateCanvases();
                foreach (var button in shell.Top.GetComponentsInChildren<Button>())
                {
                    var r = (RectTransform)button.transform;
                    var p = RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center));
                    var text = button.GetComponentInChildren<TMP_Text>()?.text;
                    lines.Add(
                        $"{text}\t{Mathf.RoundToInt(p.x)}\t{Screen.height - Mathf.RoundToInt(p.y)}\t{button.interactable}"
                    );
                }
            }
            File.WriteAllLines(Path.Combine(directory, "world-status.tsv"), lines);
        }

        void VisitHour(int day, int minute)
        {
            var clock = game.clock.Capture();
            clock.subDaySeconds = minute * 60;
            game.clock.Restore(day, clock);
            game.clock.realtimeEnabled = false;
            World.Service.AdvanceAbsentResidents(true, true);
        }

        IEnumerator ShowWorld(string label)
        {
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                string folder = Path.Combine(directory, "world-shots");
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, label + ".jpg"), shot.EncodeToJPG(86));
            }
            finally
            {
                Destroy(shot);
            }
        }

        IEnumerator AuditHomeReturns(string onlyResident)
        {
            auditing = true;
            using var file = new StreamWriter(
                Path.Combine(directory, onlyResident == null ? "world-returns.tsv" : "world-return-single.tsv"),
                false
            )
            {
                AutoFlush = true,
            };
            results = file;
            try
            {
                foreach (
                    var resident in World
                        .Room.rules.npcs.Where(n => onlyResident == null || n.npcId == onlyResident)
                        .ToArray()
                )
                {
                    const int weekday = 2;
                    bool AtHome(NPCScheduleEntry entry) =>
                        FamilyLife.Parent(resident.npcId)
                            ? FamilyLife.IsFamilyRoom(entry.zone)
                            : entry.zone == resident.homeZone;
                    NPCScheduleEntry At(int minute) =>
                        resident.schedule[WorldNPCScheduler.Schedule(resident, weekday, minute)];
                    int returning = resident
                        .schedule.Select(e => e.hour * 60 + e.minute)
                        .Where(minute => minute > 0 && AtHome(At(minute)) && !AtHome(At(minute - 1)))
                        .Distinct()
                        .OrderByDescending(minute => minute)
                        .DefaultIfEmpty(-1)
                        .First();
                    if (returning < 0)
                    {
                        file.WriteLine("NOT VERIFIED\t" + resident.npcId + "\tno weekday arrival from outside");
                        continue;
                    }
                    var entry = At(returning);
                    var prior = At(returning - 1);
                    yield return Room("station-city");
                    VisitHour(weekday, returning - 1);
                    yield return Room(entry.zone);
                    game.clock.realtimeEnabled = false;
                    var w = World;
                    w.Player.Warp(w.Safe(w.CurrentZone, new Vector2(-2, -2)));
                    var scheduler = w.GetComponent<WorldNPCScheduler>();
                    var portal = w.Room.catalog.EntryFrom(entry.zone, prior.zone);
                    var doorway = w.objects.FirstOrDefault(o => o && o.id == portal?.doorId)?.GetComponent<WorldDoor>();
                    bool wasAway = !scheduler.HasPhysicalActor(resident.npcId);
                    ShiftMinute(returning);
                    game.clock.realtimeEnabled = true;
                    float startDistance = float.PositiveInfinity;
                    float progress = 0;
                    float maxStep = 0;
                    Vector2 start = default,
                        last = default;
                    int samples = 0;
                    float deadline = Time.unscaledTime + 18;
                    using var traceFile =
                        onlyResident == null
                            ? null
                            : new StreamWriter(Path.Combine(directory, "return-trace.tsv"), false) { AutoFlush = true };
                    while (progress < 1.3f && Time.unscaledTime < deadline)
                    {
                        var actor = scheduler.actors.FirstOrDefault(o =>
                            o && o.npcId == resident.npcId && o.zone == entry.zone
                        );
                        if (actor && doorway)
                        {
                            Vector2 pos = actor.transform.position;
                            if (samples == 0)
                            {
                                start = last = pos;
                                startDistance = Vector2.Distance(pos, doorway.CrossPoint);
                            }
                            samples++;
                            if (samples % 4 == 1)
                            {
                                traceFile?.WriteLine(scheduler.JourneyTrace(resident.npcId));
                            }
                            maxStep = Mathf.Max(maxStep, Vector2.Distance(last, pos));
                            progress = Mathf.Max(progress, Vector2.Distance(start, pos));
                            last = pos;
                        }
                        yield return new WaitForSecondsRealtime(.08f);
                    }
                    Result(
                        wasAway && samples >= 3 && startDistance < .65f && progress >= 1.3f && maxStep < .75f,
                        "entered-through-home-door",
                        $"{resident.npcId} from={prior.zone} entry={portal?.doorId} "
                            + $"away={wasAway} firstGap={startDistance:F2} walked={progress:F2} maxStep={maxStep:F2}"
                    );
                }
                file.WriteLine("END");
            }
            finally
            {
                auditing = false;
            }
        }

        IEnumerator AuditHomeDay(bool doorOnly)
        {
            auditing = true;
            using var file = new StreamWriter(
                Path.Combine(directory, doorOnly ? "world-door.tsv" : "world-day.tsv"),
                false
            )
            {
                AutoFlush = true,
            };
            results = file;
            try
            {
                var roster = World.Room.rules.npcs.ToArray();
                foreach (var home in roster.Where(n => !doorOnly).GroupBy(n => n.homeZone))
                {
                    // Pick each resident's sleeping interval, including night-shift workers and both parents.
                    foreach (var n in home)
                    {
                        yield return Room("city");
                        int t = Enumerable
                            .Range(0, 24 * 4)
                            .Select(i => i * 15)
                            .First(m => n.schedule[WorldNPCScheduler.Schedule(n, 2, m)].action == NPCDailyAction.Sleep);
                        VisitHour(2, t);
                        yield return Room(n.homeZone);
                        yield return new WaitForSecondsRealtime(.6f);
                        var e = n.schedule[WorldNPCScheduler.Schedule(n, 2, t)];
                        var bed = World.objects.FirstOrDefault(o => o && o.id == e.objectId);
                        var slot = bed ? bed.GetComponent<WorldActivitySlot>() : null;
                        var scheduler = World.GetComponent<WorldNPCScheduler>();
                        float until = Time.unscaledTime + 3;
                        while (bed && bed.GetComponent<BedQuilt>()?.Coverage < .95f && Time.unscaledTime < until)
                        {
                            yield return null;
                        }
                        Result(
                            scheduler.HasPhysicalActor(n.npcId)
                                && slot
                                && slot.Occupant == n.npcId
                                && bed.GetComponent<BedQuilt>()?.Coverage >= .95f,
                            "day-two-sleep",
                            n.npcId + " " + e.objectId
                        );
                    }
                }
                var who = roster.Single(n => n.npcId == "ambient-urban-1");
                var meal = who.schedule.Last(e => e.action == NPCDailyAction.Eat && e.zone == who.homeZone);
                int returning = meal.hour * 60 + meal.minute;
                yield return Room("city");
                VisitHour(2, returning - 1);
                yield return Room(who.homeZone);
                var w = World;
                var sched = w.GetComponent<WorldNPCScheduler>();
                w.Player.Warp(w.Safe(w.CurrentZone, new Vector2(-2, -2)));
                yield return new WaitForSecondsRealtime(1);
                Result(!sched.HasPhysicalActor(who.npcId), "before-return-away", who.npcId);
                ShiftMinute(returning);
                float deadline = Time.unscaledTime + 30;
                int samples = 0;
                var first = Vector2.zero;
                float travel = 0;
                while (Time.unscaledTime < deadline)
                {
                    var actor = sched.actors.FirstOrDefault(a => a && a.npcId == who.npcId);
                    if (actor)
                    {
                        var p = (Vector2)actor.transform.position;
                        if (samples == 0)
                        {
                            first = p;
                        }
                        travel = Mathf.Max(travel, Vector2.Distance(p, first));
                        samples++;
                    }
                    var chair = w
                        .objects.FirstOrDefault(o => o && o.id == meal.objectId)
                        ?.GetComponent<WorldActivitySlot>();
                    if (samples > 2 && chair && chair.IsOccupied && chair.Occupant == who.npcId)
                    {
                        break;
                    }
                    yield return new WaitForSecondsRealtime(.1f);
                }
                Result(
                    samples > 3 && travel > .7f,
                    "walk-in-from-street",
                    who.npcId + " distance=" + travel.ToString("F2")
                );
                yield return ShowWorld("resident-return");
                var wake = who.schedule.First(e => e.action == NPCDailyAction.Wake);
                int morning = wake.hour * 60 + wake.minute;
                yield return Room("city");
                VisitHour(3, morning - 1);
                yield return Room(who.homeZone);
                World.Player.Warp(World.Safe(World.CurrentZone, new Vector2(-2, -2)));
                yield return new WaitForSecondsRealtime(1);
                var owned = World.objects.First(o => o && o.id == who.residentBedId);
                ShiftMinute(morning);
                yield return new WaitForSecondsRealtime(4.0f);
                Result(owned.GetComponent<BedQuilt>().Coverage < .05f, "wake-in-own-home", who.npcId);
                var exit = who.schedule.First(e => e.zone != who.homeZone);
                int depart = exit.hour * 60 + exit.minute;
                // Run breakfast and the commute on the real game clock. Other occupants also
                // need to finish their morning activities to vacate the shared passage.
                game.clock.realtimeEnabled = true;
                float breakfastDeadline =
                    Time.unscaledTime + game.clock.realSecondsPerGameDay * (depart - morning + 2) / 1440f + 12;
                while (game.clock.SubDaySeconds / 60 < depart && Time.unscaledTime < breakfastDeadline)
                {
                    yield return null;
                }
                sched = World.GetComponent<WorldNPCScheduler>();
                deadline = Time.unscaledTime + 38;
                using (
                    var journey = new StreamWriter(Path.Combine(directory, "commute-trace.tsv"), false)
                    {
                        AutoFlush = true,
                    }
                )
                {
                    while (sched.HasPhysicalActor(who.npcId) && Time.unscaledTime < deadline)
                    {
                        journey.WriteLine(sched.JourneyTrace(who.npcId));
                        yield return new WaitForSecondsRealtime(.5f);
                    }
                }
                game.clock.realtimeEnabled = false;
                var saved = sched.Inspect(who.npcId);
                Result(
                    saved != null && !sched.HasPhysicalActor(who.npcId) && saved.destinationZone == exit.zone,
                    "leave-own-home",
                    who.npcId + " " + saved?.zone + " -> " + saved?.destinationZone
                );
                yield return Room("city");
                Checkpoint("next-day-save");
                file.WriteLine("END");
                WorldStatus();
            }
            finally
            {
                auditing = false;
            }
        }

        void ShiftMinute(int minute)
        {
            var clock = game.clock.Capture();
            clock.subDaySeconds = minute * 60;
            game.clock.Restore(game.clock.Day, clock);
        }

        IEnumerator AuditWorld()
        {
            auditing = true;
            using var log = new StreamWriter(Path.Combine(directory, "world-results.tsv"), false) { AutoFlush = true };
            results = log;
            log.WriteLine("status\tage\troom\tcheck\tdetail");
            try
            {
                yield return Room("school");
                var catalog = World.Room.catalog;
                var original = game.CaptureState();
                yield return Room("family-ian-bedroom");
                foreach (var stage in World.Player.growthVisual.catalog.profiles.OrderBy(p => p.minAge))
                {
                    var state = game.CaptureState();
                    state.progression.age = stage.minAge;
                    RestoreFixture(state);
                    yield return null;
                    yield return new WaitUntil(
                        () =>
                            !Transitions.IsTransitioning
                            && World
                            && World.Player
                            && World.Player.growthVisual.Current == stage
                    );
                    yield return PhotographStage(stage);
                    CheckBody("world-stage");
                }
                RestoreFixture(original);
                VisitHour(1, 580);
                yield return Room("city");
                yield return Room("school");
                yield return new WaitForSecondsRealtime(2);
                yield return ShowWorld("school");
                foreach (var id in new[] { "classmate", "school-pupil", "school-teacher" })
                {
                    var entry = World.GetComponent<WorldNPCScheduler>().Inspect(id);
                    var item = World.objects.FirstOrDefault(o => o && o.id == entry?.objectId);
                    Result(
                        entry?.zone == "school" && item && World.GetComponent<WorldNPCScheduler>().HasPhysicalActor(id),
                        "class-in-session",
                        id + " " + entry?.objectId
                    );
                }
                if (FamilyLife.AtFamily(game))
                {
                    Result(game.economy.MoveResidence("world-qa-first-residence", "starter"), "elect-independent-home");
                }
                foreach (
                    var zone in new[]
                    {
                        "family-living",
                        "family-kitchen",
                        "family-bathroom",
                        "family-parents-bedroom",
                        "home",
                    }
                )
                {
                    yield return Room(zone);
                    if (zone == "home")
                    {
                        Result(
                            World.Room.roomId == "home"
                                && !World.objects.Any(o =>
                                    o
                                    && o.zone == "home"
                                    && StarterHomePresentation.Optional(o.id)
                                    && o.gameObject.activeInHierarchy
                                ),
                            "bed-only-independent-home"
                        );
                    }
                    yield return ShowWorld(zone);
                }
                foreach (var unit in catalog.rooms.Where(r => r.floorPlan))
                {
                    VisitHour(1, 1395);
                    yield return Room(unit.roomId);
                    game.clock.realtimeEnabled = false;
                    var fixtures = World.objects.Where(o => o && o.isActiveAndEnabled).ToArray();
                    var members = catalog.rules.npcs.Where(n => n.homeZone == unit.roomId).ToArray();
                    var scheduler = World.GetComponent<WorldNPCScheduler>();
                    foreach (var n in members)
                    {
                        var bed = fixtures.FirstOrDefault(o => o.id == n.residentBedId);
                        var rest = fixtures.FirstOrDefault(o => o.id == n.residentBedId + "-rest");
                        Result(
                            bed
                                && rest
                                && bed.GetComponent<WorldActivitySlot>().Allows(n.npcId)
                                && fixtures.Count(o => o.kind == WorldInteractionKind.Sleep) == members.Length,
                            "private-bed",
                            n.npcId + " " + n.residentBedId
                        );
                    }
                    Result(
                        fixtures.Any(o => o.kind == WorldInteractionKind.Toilet)
                            && fixtures.Any(o => o.kind == WorldInteractionKind.Shower)
                            && World.GetComponentsInChildren<WorldDoor>().Length >= 2,
                        "rooms-and-bath",
                        unit.floorPlan.name
                    );
                    var sleeping = members
                        .Where(n => n.schedule[WorldNPCScheduler.Schedule(n, 1, 1395)].action == NPCDailyAction.Sleep)
                        .Select(n =>
                        {
                            var bed = fixtures.FirstOrDefault(o => o.id == n.residentBedId);
                            return (
                                n.npcId,
                                slot: bed ? bed.GetComponent<WorldActivitySlot>() : null,
                                quilt: bed ? bed.GetComponent<BedQuilt>() : null
                            );
                        })
                        .ToArray();
                    float deadline = Time.realtimeSinceStartup + 14f;
                    while (
                        Time.realtimeSinceStartup < deadline && sleeping.Any(b => b.quilt && b.quilt.Coverage <= .9f)
                    )
                    {
                        yield return null;
                    }
                    foreach (var (npcId, slot, quilt) in sleeping)
                    {
                        Result(
                            scheduler.HasPhysicalActor(npcId)
                                && slot
                                && slot.Occupant == npcId
                                && quilt
                                && quilt.Coverage > .9f,
                            "asleep-in-assigned-bed",
                            $"{npcId} {slot?.Occupant} {quilt?.Coverage:F2}"
                        );
                    }
                    yield return ShowWorld(unit.roomId);
                    var snapshot = game.CaptureState();
                    bool saved =
                        game.SaveForMenu(out var error)
                        && game.saves.ReadContinue(out snapshot, out _, out _, out error);
                    bool matches =
                        saved
                        && members.All(n =>
                            snapshot.world.residents.Any(r =>
                                r.id == n.npcId && r.householdId == unit.homeId && r.residenceId == unit.roomId
                            )
                        );
                    Result(matches, "household-save-read", error);
                    if (matches)
                    {
                        RestoreFixture(snapshot);
                    }
                }
                VisitHour(1, 750);
                yield return Room("city");
                Checkpoint("world-continue");
                log.WriteLine("END");
                WorldStatus();
            }
            finally
            {
                auditing = false;
            }
        }
    }
}
#endif
