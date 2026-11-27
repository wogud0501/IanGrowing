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
        bool auditing;
        StreamWriter results;
        LifeWorldController World => game.world.Host;
        SceneTransitionService Transitions => GameContext.Instance.transitions;

        void Result(bool passed, string check, string detail = "")
        {
            results.WriteLine($"{(passed ? "PASS" : "FAIL")}\t{game.growth.Age}\t{World?.Room?.roomId}\t{check}\t{detail}");
        }

        IEnumerator Room(string id)
        {
            if (World.Room.roomId != id)
            {
                var rule = World.Service.rules.Zone(id);
                bool requested = rule != null && Transitions.NavigateRoom(id, rule.spawn, Vector2.down);
                float deadline = Time.realtimeSinceStartup + 15;
                while (requested && Time.realtimeSinceStartup < deadline &&
                    (Transitions.IsTransitioning || !World || World.Room.roomId != id))
                {
                    yield return null;
                }
                yield return null;
            }
            Result(World && World.Room.roomId == id, "load-room", id);
        }

        void RestoreFixture(SaveData data)
        {
            game.RestoreState(data);
            World.ApplySavedState();
            game.clock.realtimeEnabled = false;
        }

        void Checkpoint(string label)
        {
            var current = game.CaptureState();
            if (!game.SaveForMenu(out var issue) || !game.saves.ReadContinue(out var loaded, out _, out _, out issue))
            {
                Result(false, label, issue);
                return;
            }
            RestoreFixture(loaded);
            var actual = game.CaptureState();
            Result(actual.progression.age == current.progression.age && actual.stats.money == current.stats.money
                && actual.world.zone == current.world.zone && actual.world.family.residency == current.world.family.residency
                && actual.missions.freeLifeDays == current.missions.freeLifeDays
                && actual.missions.claimedMissionIds.SequenceEqual(current.missions.claimedMissionIds)
                && actual.progression.completedGrowthIds.SequenceEqual(current.progression.completedGrowthIds)
                && actual.economy.housing.owned.SequenceEqual(current.economy.housing.owned)
                && JsonUtility.ToJson(actual.phase23.social) == JsonUtility.ToJson(current.phase23.social),
                label);
        }

        void CheckBody(string label)
        {
            var p = World.Player;
            var profile = p.growthVisual.catalog.AtAge(game.growth.Age);
            var shape = p.GetComponent<CircleCollider2D>();
            var sr = p.sprite;
            var neutral = profile?.idlePreview;
            bool upright = p.advanced.Motion is ActorMotion.Idle or ActorMotion.Walk or ActorMotion.Stop or ActorMotion.Turn or ActorMotion.Run;
            Result(profile != null && p.growthVisual.Current == profile && p.advanced.library == profile.animations
                && p.growthVisual.catalog.growthSheet && neutral && neutral.name == "Ian_" + profile.id
                && shape && shape.enabled && Mathf.Approximately(shape.radius, profile.colliderRadius)
                && shape.offset == profile.colliderOffset && p.GetComponent<Rigidbody2D>().simulated
                && Mathf.Approximately(sr.transform.lossyScale.x, sr.transform.lossyScale.y)
                && sr.transform.localPosition.sqrMagnitude < .00001f
                && Mathf.Abs(Quaternion.Angle(sr.transform.localRotation, Quaternion.identity)) < .01f
                && sr.sprite && (sr.sprite == neutral || sr.sprite.texture.name == neutral.name + "_Motion")
                && (!upright || Mathf.Abs(sr.bounds.size.y - profile.height) < .001f), label, profile?.id);
        }

        IEnumerator PhotographStage(GrowthVisualProfile profile)
        {
            var player = World.Player;
            player.Face(Vector2.down);
            player.ClearReaction();
            player.Pose("Idle");
            yield return new WaitForSecondsRealtime(.75f);
            yield return new WaitForEndOfFrame();
            var camera = Camera.main;
            if (!camera)
            {
                Result(false, "front-screenshot", "Missing gameplay camera");
                yield break;
            }
            var bounds = player.sprite.bounds;
            var center = new Vector3(bounds.center.x, bounds.min.y + .92f, bounds.center.z);
            var low = camera.WorldToScreenPoint(center - new Vector3(1.05f, 1.12f));
            var high = camera.WorldToScreenPoint(center + new Vector3(1.05f, 1.12f));
            int width = Mathf.Clamp(Mathf.RoundToInt(high.x - low.x), 1, Screen.width);
            int height = Mathf.Clamp(Mathf.RoundToInt(high.y - low.y), 1, Screen.height);
            var rect = new Rect(Mathf.Clamp(Mathf.RoundToInt(low.x), 0, Screen.width - width),
                Mathf.Clamp(Mathf.RoundToInt(low.y), 0, Screen.height - height), width, height);
            var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                shot.ReadPixels(rect, 0, 0);
                shot.Apply();
                string folder = Path.Combine(directory, "stages");
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, profile.id + ".png"), shot.EncodeToPNG());
                Result(player.sprite.sprite == profile.idlePreview, "front-screenshot", player.sprite.sprite.name);
            }
            finally
            {
                Destroy(shot);
            }
        }

        IEnumerator Move(Vector2 offset)
        {
            var player = World.Player;
            var start = player.Position;
            player.BeginTraversal(start + offset, 1.15f);
            float deadline = Time.unscaledTime + 2;
            bool walked = false;
            while (player.IsTraversing && Time.unscaledTime < deadline)
            {
                walked |= player.advanced.Motion == ActorMotion.Walk;
                yield return null;
            }
            player.CancelTraversal();
            var distance = Vector2.Distance(start, player.Position);
            Result(player.MovementAllowed ? walked && distance > .06f : !walked && distance < .01f,
                "locomotion", $"{offset} {distance:F3}");
            yield return null;
        }

        IEnumerator AuditLifecycle()
        {
            auditing = true;
            using var file = new StreamWriter(Path.Combine(directory, "lifecycle.tsv"), false) { AutoFlush = true };
            results = file;
            results.WriteLine("result\tage\troom\tcheck\tdetail");
            var catalog = World.Player.growthVisual.catalog;
            var samples = GrowthQAScenarios.SampleAges(catalog);
            foreach (var school in game.phase23.catalog.education)
            {
                samples.Add(school.minAge);
                samples.Add(school.maxAge);
                if (school.maxAge < int.MaxValue)
                {
                    samples.Add(school.maxAge + 1);
                }
            }
            var original = game.CaptureState();
            var progression = new AgeProgressionService(game.growth);
            try
            {
                foreach (int age in samples)
                {
                    int before = Math.Max(0, age - 1);
                    var setup = game.CaptureState();
                    setup.progression.age = before;
                    setup.birthIntroCompleted = true;
                    setup.stats.hunger = setup.stats.thirst = 100;
                    setup.world.pendingAction = "";
                    if (age == MissionService.FreeLifeAge && !setup.missions.completedMissionIds.Contains(MissionService.FinalGrowthMission))
                    {
                        setup.missions.completedMissionIds.Add(MissionService.FinalGrowthMission);
                    }
                    setup.missions.freeLifeDays = MissionService.DaysPerFreeLifeYear - 1;
                    RestoreFixture(setup);
                    string startRoom = age <= LifeProgression.IndependentOutdoorAge ? "family-ian-bedroom"
                        : GrowthAccess.Zone(game, "school", out _) ? "school" : "shop";
                    yield return Room(startRoom);
                    Checkpoint("before-save-load");
                    int events = 0;
                    void Changed(int _, LifeStage __) => events++;
                    game.growth.AgeChanged += Changed;
                    try
                    {
                        if (age == MissionService.FreeLifeAge - 1)
                        {
                            var missions = game.CaptureState().missions;
                            foreach (var earlier in game.missionCatalog.missions.Where(m => m && m.number < 30))
                            {
                                if (!missions.completedMissionIds.Contains(earlier.Id))
                                {
                                    missions.completedMissionIds.Add(earlier.Id);
                                }
                                if (!missions.claimedMissionIds.Contains(earlier.Id))
                                {
                                    missions.claimedMissionIds.Add(earlier.Id);
                                }
                            }
                            game.missions.Restore(missions, game.clock.Day);
                            game.phase23.Signal("qa-final-graduation", MissionCounter.SecondaryGraduation);
                            game.missions.Refresh();
                            Result(game.missions.FreeLifeMode && !game.missions.ActiveMission
                                && game.missions.UnclaimedReward && game.missions.UnclaimedReward.Id == MissionService.FinalGrowthMission,
                                "final-mission-free-life-before-claim");
                            bool paid = game.missions.Claim(MissionService.FinalGrowthMission);
                            Result(paid && !game.missions.Claim(MissionService.FinalGrowthMission), "final-reward-once");
                        }
                        else if (age > 0 && age < MissionService.FreeLifeAge)
                        {
                            var milestone = game.missionCatalog.missions.First(m => m && m.targetAge == age);
                            bool first = progression.ApplyMissionMilestone(milestone.Id, age);
                            bool twice = progression.ApplyMissionMilestone(milestone.Id, age);
                            Result(first && !twice, "milestone-receipt", milestone.Id);
                        }
                        else if (age >= MissionService.FreeLifeAge)
                        {
                            game.clock.Restore(game.clock.Day, new TimeSnapshot { subDaySeconds = TimeManager.SecondsPerDay - 1 });
                            game.clock.SetRunning(true);
                            game.clock.AdvanceSimulation(1.01);
                        }
                    }
                    finally
                    {
                        game.growth.AgeChanged -= Changed;
                    }
                    yield return null;
                    yield return null;
                    Result(game.growth.Age == age && events == (age == 0 ? 0 : 1), "age-event", $"expected={age} events={events}");
                    if (age == int.MaxValue)
                    {
                        game.growth.AdvanceFreeLifeYear();
                        Result(game.growth.Age == int.MaxValue, "no-integer-overflow");
                    }
                    Result(game.growth.Stage == LifeProgression.StageAtAge(age)
                        && game.growth.WalkingUnlocked == (age >= LifeProgression.WalkingAge)
                        && game.growth.IndependentOutdoorUnlocked == (age >= LifeProgression.IndependentOutdoorAge)
                        && (age < MissionService.FreeLifeAge || game.missions.FreeLifeMode && !game.missions.ActiveMission), "rules");
                    CheckBody("after-growth");
                    if (World.Room.roomId == "school" && age == game.phase23.Education.Available?.minAge + 2)
                    {
                        yield return SchoolLesson();
                    }
                    if (age == MissionService.FreeLifeAge - 1)
                    {
                        var housing = game.CaptureState();
                        housing.stats.money = 1000;
                        RestoreFixture(housing);
                        bool moved = game.economy.MoveResidence("qa-age19-move", "starter");
                        Result(moved && !FamilyLife.AtFamily(game) && game.economy.Cash == 500
                            && !game.economy.MoveResidence("qa-age19-move", "starter"), "independence-once");
                        yield return Room("home");
                        Result(!World.objects.Any(o => o && o.zone == "home" && StarterHomePresentation.Optional(o.id)
                            && o.gameObject.activeInHierarchy) && World.objects.Any(o => o && o.zone == "home"
                            && o.GetComponent<WorldLifeAction>()?.mode == LifeActionVisual.Bed), "starter-bed-only");
                        Checkpoint("independence-save-load");
                        yield return Room("family-living");
                    }
                    if (age == catalog.AtAge(age).minAge)
                    {
                        yield return PhotographStage(catalog.AtAge(age));
                        foreach (Vector2 step in new[] { Vector2.right, Vector2.up, Vector2.left, Vector2.down,
                            new Vector2(1, 1), new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, -1) })
                        {
                            yield return Move(step.normalized * .40f);
                        }
                    }
                    if (age == LifeProgression.IndependentOutdoorAge || age == MissionService.FreeLifeAge - 1)
                    {
                        yield return Room("alley");
                    }
                    else
                    {
                        yield return Room(age < LifeProgression.IndependentOutdoorAge ? "family-living" : "city");
                    }
                    if (age == catalog.profiles.Max(p => p.minAge))
                    {
                        foreach (var zone in Transitions.roomCatalog.rooms.Where(z => GrowthAccess.Zone(game, z.roomId, out _)))
                        {
                            yield return Room(zone.roomId);
                            CheckBody("room-body");
                        }
                    }
                    Checkpoint("after-travel-save-load");
                    yield return null;
                    yield return null;
                    CheckBody("after-travel-load");
                }
                yield return AuditHousingMigration();
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
    }
}
#endif
