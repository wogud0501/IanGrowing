#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace IanGame.Editor
{
    public static class FacilityActorVerification
    {
        public static IEnumerator ClerkExit(LifeWorldController w, GameManager g, Action<bool, string> check)
        {
            g.growth.Restore(new ProgressSnapshot { age = 19 });
            g.clock.realtimeEnabled = false;
            g.clock.Restore(1, new TimeSnapshot { subDaySeconds = 22 * 3600 + 45 * 60 });
            check(w.Travel("shop"), "Visit actual clerk before closing");
            yield return FacilityQualityVerification.Wait(w);
            w.Player.Warp(w.Safe(w.CurrentZone, new Vector2(-1.5f, -4.4f)));
            yield return new WaitForSecondsRealtime(.7f);
            var actor = w.GetComponent<WorldNPCScheduler>().actors.First(a => a.npcId == "shop-clerk");
            var gate = w.objects.First(o => o.id == "shop-exit");
            check(Vector2.Distance(w.Player.Position, gate.Point) > 1.2f, "Observer leaves clerk exit corridor clear");
            g.clock.Restore(1, new TimeSnapshot { subDaySeconds = 23 * 3600 + 5 * 60 });
            float until = Time.unscaledTime + 20;
            var sprites = new HashSet<Sprite>();
            bool walked = false,
                opened = false;
            while (actor.zone == "shop" && Time.unscaledTime < until)
            {
                walked |= actor.GetComponent<WorldActorAnimation>().Motion == ActorMotion.Walk;
                opened |= gate.GetComponent<WorldDoor>().Aperture > .5f;
                sprites.Add(actor.visual.sprite);
                yield return null;
            }

            check(walked && sprites.Count >= 2, "Clerk actually walks with changing frames after closing");
            check(
                actor.zone == "city" && opened,
                "Clerk uses existing exit after shop closes: "
                    + JsonUtility.ToJson(w.GetComponent<WorldNPCScheduler>().Inspect(actor.npcId))
            );
            check(w.CanControl, "Observer retains player control after clerk departure");
        }

        public static IEnumerator Run(
            LifeWorldController w,
            GameManager g,
            Action<bool, string> check,
            bool remaining = false
        )
        {
            g.growth.Restore(new ProgressSnapshot { age = 19 });
            g.clock.realtimeEnabled = false;
            var scheduler = w.GetComponent<WorldNPCScheduler>();
            foreach (var d in g.world.rules.npcs.Where(n => n.personality == NPCPersonality.NightWorker))
            {
                check(
                    d.schedule.Any(e => e.action == NPCDailyAction.Eat && e.hour == 20)
                        && !d.schedule.Any(e => e.action == NPCDailyAction.Eat && e.hour == 12),
                    "Night worker retains authored evening meal " + d.npcId
                );
            }

            foreach (var zone in new[] { "finance", "realestate" }.Where(z => !remaining || z != "finance"))
            {
                var definition = g.world.rules.npcs.First(d =>
                    d.schedule.Any(e => e.zone == zone && e.objectId == zone + "-staff-rest-0")
                );
                var rest = definition.schedule.First(e => e.zone == zone && e.objectId == zone + "-staff-rest-0");
                int minute = rest.hour * 60 + rest.minute;
                g.clock.Restore(1, new TimeSnapshot { subDaySeconds = (minute - 2) * 60 });
                check(w.Travel(zone), "Visit scheduled facility refreshment break " + zone);
                yield return FacilityQualityVerification.Wait(w);
                w.Player.Warp(w.Safe(w.CurrentZone, new Vector2(0, -4)));
                yield return new WaitForSecondsRealtime(1);
                var station = w.objects.First(o => o.id == zone + "-staff-rest-0");
                var slot = station.GetComponent<WorldActivitySlot>();
                var presentation = station.GetComponent<WorldFacilityActivityPresentation>();
                g.clock.Restore(1, new TimeSnapshot { subDaySeconds = (minute + 2) * 60 });
                float arrivalLimit = Time.unscaledTime + 18;
                while (
                    Time.unscaledTime < arrivalLimit
                    && !(slot.Occupant == definition.npcId && slot.IsOccupied && presentation.IsPresenting)
                )
                {
                    yield return null;
                }

                check(
                    slot.Occupant == definition.npcId && slot.IsOccupied && presentation.IsPresenting,
                    "Staff sits on exact real lounge chair "
                        + zone
                        + " owner="
                        + slot.Occupant
                        + " actual="
                        + JsonUtility.ToJson(scheduler.Inspect(definition.npcId))
                );
                check(!slot.TryReserve("qa/other"), "Occupied lounge rejects another user " + zone);
                var frames = new HashSet<Sprite>();
                float end = Time.unscaledTime + 1.5f;
                while (Time.unscaledTime < end)
                {
                    frames.Add(presentation.occupied.sprite);
                    yield return null;
                }

                check(frames.Count >= 3, "Frontal coffee/eating frames actually advance " + zone);
                ScreenCapture.CaptureScreenshot("Docs/FacilityStandardization/Screenshots/Lounge-" + zone + ".png");
                yield return null;
                var next = definition.schedule.First(e =>
                    e.hour * 60 + e.minute > minute && e.action == NPCDailyAction.Work
                );
                g.clock.Restore(1, new TimeSnapshot { subDaySeconds = (next.hour * 60 + next.minute + 1) * 60 });
                end = Time.unscaledTime + 18;
                bool moved = false;
                var actor = scheduler.actors.First(a => a.npcId == definition.npcId);
                while (
                    Time.unscaledTime < end
                    && (
                        slot.Occupant == definition.npcId
                        || actor.GetComponent<WorldActorAnimation>().Motion != ActorMotion.UseComputer
                    )
                )
                {
                    moved |= actor.GetComponent<WorldActorAnimation>().Motion == ActorMotion.Walk;
                    yield return null;
                }

                check(
                    moved
                        && slot.State == ActivityOccupancy.Free
                        && actor.GetComponent<WorldActorAnimation>().Motion == ActorMotion.UseComputer,
                    "Staff stands, releases chair and walks back to real desk " + zone
                );
            }

            foreach (
                var info in new[]
                {
                    new[] { "school-teacher", "school-front", "515" },
                    new[] { "school-pupil", "school-front", "530" },
                    new[] { "shop-clerk", "shop-front", "540" },
                }
            )
            {
                var portal = w.objects.First(o => o.id == info[1]);
                var state = g.CaptureState();
                int minute = int.Parse(info[2]);
                state.time.subDaySeconds = minute * 60;
                state.world.zone = "city";
                state.world.position = new Vector2(-10, 1);
                state.world.residents.RemoveAll(n => n.id == info[0]);
                state.world.residents.Add(
                    new NPCResidentSnapshot
                    {
                        id = info[0],
                        zone = "city",
                        position = portal.Point - g.world.rules.Zone("city").origin + Vector2.down * .9f,
                        action = NPCDailyAction.Walk,
                        minute = minute,
                        completedUseKey = -1,
                    }
                );
                g.RestoreState(state);
                g.clock.realtimeEnabled = false;
                yield return null;
                var actor = scheduler.actors.First(a => a.npcId == info[0]);
                var door = portal.GetComponent<WorldDoor>();
                int passages = scheduler.DoorPassages;
                var sprites = new HashSet<Sprite>();
                float until = Time.unscaledTime + 15;
                bool opened = false;
                while (actor.zone == "city" && Time.unscaledTime < until)
                {
                    opened |= door.Aperture > .5f;
                    sprites.Add(actor.visual.sprite);
                    yield return null;
                }

                check(
                    actor.zone == portal.targetZone && scheduler.DoorPassages > passages && opened,
                    "Role physically uses existing entrance door " + info[0]
                );
                check(sprites.Count >= 2, "Visible role walk frames advance " + info[0]);
            }

            foreach (
                var info in new[]
                {
                    new[] { "school-teacher", "school", "965" },
                    new[] { "shop-clerk", "shop", "1385" },
                }
            )
            {
                g.clock.Restore(1, new TimeSnapshot { subDaySeconds = (int.Parse(info[2]) - 20) * 60 });
                check(w.Travel(info[1]), "Observe staff before closing " + info[0]);
                yield return FacilityQualityVerification.Wait(w);
                w.Player.Warp(w.Safe(w.CurrentZone, new Vector2(-1.5f, -4.4f)));
                g.clock.Restore(1, new TimeSnapshot { subDaySeconds = int.Parse(info[2]) * 60 });
                var actor = scheduler.actors.First(a => a.npcId == info[0]);
                float until = Time.unscaledTime + 20;
                bool walked = false;
                while (actor.zone == info[1] && Time.unscaledTime < until)
                {
                    walked |= actor.GetComponent<WorldActorAnimation>().Motion == ActorMotion.Walk;
                    yield return null;
                }

                check(walked && actor.zone != info[1], "Staff walks through exit at end of shift " + info[0]);
            }
        }
    }
}
#endif
