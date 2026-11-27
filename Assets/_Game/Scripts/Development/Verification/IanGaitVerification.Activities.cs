#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    public sealed partial class IanGaitVerification
    {
        IEnumerator AuditHousingMigration()
        {
            var baseline = game.CaptureState();
            foreach (int version in new[] { 2, 3 })
            {
                var slot = game.saves.EnterTestingChannel("qa-growth-v" + version + "-" + Guid.NewGuid().ToString("N") + ".json");
                try
                {
                    var legacy = game.CaptureState();
                    legacy.version = version;
                    legacy.stats.money = 1234;
                    legacy.progression.age = LifeProgression.IndependentOutdoorAge;
                    legacy.world.family = null;
                    string json = JsonUtility.ToJson(legacy);
                    string source = game.saves.BasePath;
                    File.WriteAllText(source, json);
                    bool read = game.saves.TryRead(out var value, out var message);
                    if (read)
                    {
                        RestoreFixture(value);
                    }
                    Result(read && game.economy.Cash == 1234 && (version == 2 ? game.growth.Age == 19
                        : FamilyLife.AtFamily(game) && game.growth.Age == legacy.progression.age
                            && game.missions.Claimed(MissionService.FinalGrowthMission)), "legacy-load-v" + version, message);
                    bool wrote = read && game.SaveForMenu(out _);
                    Result(wrote && File.ReadAllText(source) == json, "legacy-source-preserved-v" + version);
                }
                finally
                {
                    game.saves.RestoreTestingChannel(slot);
                    RestoreFixture(baseline);
                }
                yield return null;
            }
        }

        IEnumerator FurnitureAudit()
        {
            auditing = true;
            using var file = new StreamWriter(Path.Combine(directory, "furniture.tsv"), false) { AutoFlush = true };
            results = file;
            file.WriteLine("result\tage\troom\tcheck\tdetail");
            var original = game.CaptureState();
            try
            {
                foreach (var profile in World.Player.growthVisual.catalog.profiles.OrderBy(p => p.minAge))
                {
                    var data = game.CaptureState();
                    data.progression.age = profile.minAge;
                    data.birthIntroCompleted = true;
                    RestoreFixture(data);
                    yield return Exercise("family-ian-bedroom", "family-bed", 5);
                    if (!game.growth.WalkingUnlocked)
                    {
                        continue;
                    }
                    yield return Exercise("family-living", "family-sofa", 1);
                    yield return Exercise("family-kitchen", "family-player-seat", 3);
                    if (game.growth.IndependentOutdoorUnlocked)
                    {
                        yield return Exercise("school", "study-desk", 8);
                        yield return Exercise("city", "city-bench", 1);
                    }
                    if (game.growth.EmploymentUnlocked)
                    {
                        yield return Exercise("workplace", "workdesk", 0);
                        yield return Room("shop");
                        yield return Room("finance");
                        CheckBody("finance-body");
                    }
                }
                results.WriteLine("END");
            }
            finally
            {
                auditing = false;
                if (World && !Transitions.IsTransitioning)
                {
                    RestoreFixture(original);
                }
            }
        }

        IEnumerator SchoolLesson()
        {
            var original = game.CaptureState();
            var data = game.CaptureState();
            var school = game.phase23.Education.Available;
            data.phase23.education.currentEducationId = school.educationId;
            data.phase23.education.enrollments.RemoveAll(entry => entry.educationId == school.educationId);
            data.phase23.education.enrollments.Add(new EducationEnrollment { educationId = school.educationId });
            RestoreFixture(data);
            try
            {
                var desk = World.objects.First(o => o && o.id == "study-desk");
                var point = World.Safe(World.CurrentZone, desk.Point - World.CurrentZone.origin);
                World.Player.Warp(point);
                yield return new WaitForFixedUpdate();
                float settle = Time.realtimeSinceStartup + 3;
                while (World.Player.controlLocks.Locked && Time.realtimeSinceStartup < settle)
                {
                    yield return null;
                }
                int before = game.phase23.Education.Progress.books;
                bool allowed = World.CanBegin(desk, 8, "", out string message);
                bool started = allowed && World.Begin(desk, 8);
                float deadline = Time.realtimeSinceStartup + 20;
                while (started && World.Busy && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                Result(started && !World.Busy && game.phase23.Education.Progress.books == before + 1,
                    "school-read-via-gameplay", started ? message : $"{message} reach={World.IsInReach(desk)} busy={World.Busy} locked={World.Player.controlLocks.Locked} state={game.State}");
            }
            finally
            {
                RestoreFixture(original);
            }
        }

        IEnumerator Exercise(string room, string id, int action)
        {
            yield return Room(room);
            var target = World.objects.FirstOrDefault(o => o && o.isActiveAndEnabled && o.id == id);
            var detail = target ? target.GetComponent<WorldLifeAction>() : null;
            if (!detail)
            {
                Result(false, "furniture-binding", id);
                yield break;
            }
            if (!detail.Available)
            {
                Result(true, "occupied-seat-rejected", id);
                // Re-enter at another scheduled hour, retaining the NPC's reservation.
                foreach (int hour in new[] { 2, 12, 16 })
                {
                    var clock = game.clock.Capture();
                    clock.subDaySeconds = hour * 3600;
                    game.clock.Restore(game.clock.Day, clock);
                    yield return Room(room == "family-living" ? "family-kitchen" : "alley");
                    yield return Room(room);
                    target = World.objects.First(o => o && o.isActiveAndEnabled && o.id == id);
                    detail = target.GetComponent<WorldLifeAction>();
                    if (detail.Available)
                    {
                        break;
                    }
                }
                if (!detail.Available)
                {
                    results.WriteLine($"NOT VERIFIED\t{game.growth.Age}\t{room}\tfurniture-entry\t{id} occupied");
                    yield break;
                }
            }
            var p = World.Player;
            var slot = detail.NearestSeat(p.Position);
            Vector2 approach = slot && slot.approachAnchor ? slot.ApproachPoint
                : detail.approach ? (Vector2)detail.approach.position : target.Point;
            p.Warp(World.Safe(World.CurrentZone, approach - World.CurrentZone.origin));
            yield return new WaitForFixedUpdate();
            // Invoke the same transfer adapter as the actual interaction, independently
            // of a particular employment contract, meal purchase or enrolment.
            var lease = p.controlLocks.Acquire("qa-observe-furniture");
            bool bed = detail.mode == LifeActionVisual.Bed;
            try
            {
                yield return detail.Enter(World, action, () => false);
                Result(detail.Ready, "furniture-entry", id);
                if (!detail.Ready)
                {
                    yield break;
                }
                p.Pose(bed ? "Sleep" : "SeatedIdle");
                for (float time = 0; time < .45f; time += Time.unscaledDeltaTime)
                {
                    detail.Animate(time);
                    yield return null;
                }
                var bend = p.GetComponent<SeatBend>();
                var quilt = target.GetComponent<BedQuilt>();
                bool covered = quilt && quilt.Coverage > .85f;
                bool seated = bend && bend.Visible && detail.seat && Vector2.Distance(bend.HipWorld, detail.seat.Point) < .14f;
                Result(bed ? covered : seated, "furniture-contact", id
                    + (bed ? " quilt=" + (quilt ? quilt.Coverage : 0) : " hip-gap=" +
                        (bend && bend.Visible && detail.seat ? Vector2.Distance(bend.HipWorld, detail.seat.Point) : -1)));
                yield return detail.Exit(World, () => false);
                Result(detail.Completed && (!quilt || quilt.Coverage < .05f), "furniture-exit", id);
            }
            finally
            {
                detail.ResetVisual();
                lease.Dispose();
                p.Pose("Idle");
            }
            yield return null;
            CheckBody("furniture-restored-" + id);
        }
    }
}
#endif
