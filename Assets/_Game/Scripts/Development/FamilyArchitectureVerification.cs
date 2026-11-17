#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IanGame.Editor
{
    public static class FamilyArchitectureVerification
    {
        static WorldInteractable Find(LifeWorldController w, string id) => w.objects.First(o => o.id == id);

        static IEnumerator Wait(LifeWorldController w)
        {
            float end = Time.unscaledTime + 20;
            while (w.Busy && Time.unscaledTime < end)
            {
                yield return null;
            }

            if (w.Busy)
            {
                throw new Exception("Internal family door/action timeout");
            }

            yield return new WaitForSecondsRealtime(.2f);
        }

        static IEnumerator Until(Func<bool> condition, float seconds)
        {
            float until = Time.unscaledTime + seconds;
            while (!condition() && Time.unscaledTime < until)
            {
                yield return null;
            }
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
            bool household
        )
        {
            var plan = UnityEditor.AssetDatabase.LoadAssetAtPath<FamilyHomeArchitecturePlan>(
                "Assets/_Game/Data/Phase26/FamilyHomeArchitecture.asset"
            );
            check(plan != null && plan.rooms.Length == 5, "Authoritative family plan has five functional rooms");
            check(w.CurrentZone.id == FamilyLife.Home && g.growth.Age == 0, "Newborn starts in own family nursery");
            var origin = w.CurrentZone.origin;
            var scheduler = w.GetComponent<WorldNPCScheduler>();
            check(
                plan.rooms.First(r => r.id == "ian")
                    .floor.Contains((Vector2)Find(w, "family-bed").transform.position - origin),
                "Ian sleep/care bed belongs to independent nursery"
            );
            foreach (string id in new[] { "family-mother-bed", "family-father-bed" })
            {
                check(
                    plan.rooms.First(r => r.id == "parents")
                        .floor.Contains((Vector2)Find(w, id).transform.position - origin),
                    "Parents sleep in separate bedroom " + id
                );
            }

            foreach (string id in new[] { "family-basin", "family-shower", "family-toilet" })
            {
                check(
                    plan.rooms.First(r => r.id == "bath")
                        .floor.Contains((Vector2)Find(w, id).transform.position - origin)
                        && Find(w, id).zone == FamilyLife.Home,
                    "Bathroom fitting is within walled inline bathroom " + id
                );
            }

            var dining = w.GetComponentInChildren<FamilySharedDiningPresentation>();
            check(
                dining != null && dining.frames.Length == 32,
                "One shared table has actual occupancy-mask animation states"
            );
            foreach (string id in new[] { "family-dine-1", "family-dine-2", "family-player-seat" })
            {
                check(
                    Find(w, id).visual.sprite == null && Find(w, id).GetComponents<Collider2D>().All(c => !c.enabled),
                    "Old separate table image and collider removed " + id
                );
            }

            check(dining.GetComponents<Collider2D>().Length == 1, "Single physical family dining table");
            check(!w.Player.MovementAllowed && !w.Travel("city"), "Nursery layout preserves newborn mobility limits");
            g.growth.Restore(new ProgressSnapshot { age = 19 });
            g.clock.realtimeEnabled = false;
            g.clock.Restore(1, new TimeSnapshot { subDaySeconds = 13 * 3600 + 45 * 60 });
            w.Player.Warp(origin + new Vector2(-3, -5));
            yield return new WaitForSecondsRealtime(.5f);
            if (!household)
            {
                w.hud.Notify("", 99);
                var camera = w.follow.target;
                bool follow = w.follow.enabled;
                w.follow.enabled = false;
                var oldSize = camera.orthographicSize;
                camera.orthographicSize = 10;
                camera.transform.position = new Vector3(origin.x, origin.y, -10);
                yield return new WaitForSecondsRealtime(.3f);
                yield return Shot("Architecture-Overview-1280");
                foreach (var region in plan.rooms)
                {
                    camera.orthographicSize = 4.8f;
                    camera.transform.position = new Vector3(
                        origin.x + region.floor.center.x,
                        origin.y + region.floor.center.y,
                        -10
                    );
                    yield return new WaitForSecondsRealtime(.2f);
                    yield return Shot("Room-" + region.id);
                }

                camera.orthographicSize = oldSize;
                w.follow.enabled = follow;
                w.follow.Snap();
                foreach (string id in new[] { "family-parents-door", "family-ian-door", "family-bath-front" })
                {
                    var o = Find(w, id);
                    var door = o.GetComponent<WorldDoor>();
                    var root = door.transform.position;
                    var frame = door.frame.transform.position;
                    var expected =
                        id == "family-parents-door" ? plan.parentsDoor
                        : id == "family-ian-door" ? plan.ianDoor
                        : plan.bathDoor;
                    check(
                        Vector2.Distance(door.Plane, origin + expected) < .02f,
                        "Door threshold aligned to actual interior wall " + id
                    );
                    for (int side = -1; side <= 1; side += 2)
                    {
                        w.Player.Warp(door.Plane + door.normal * side * .9f);
                        yield return null;
                        check(
                            w.Use(o) && !w.Use(o),
                            "Interior door accepts correct-side input and rejects duplicate " + id + " / " + side
                        );
                        yield return Wait(w);
                        check(
                            w.CurrentZone.id == FamilyLife.Home
                                && w.CanControl
                                && door.SideOf(w.Player.Position) != side,
                            "Actual body crosses internal doorway without scene change " + id + " / " + side
                        );
                        check(
                            door.transform.position == root && door.frame.transform.position == frame,
                            "DoorRoot and frame fixed " + id
                        );
                    }
                }

                foreach (
                    string id in new[]
                    {
                        "family-mother-bed",
                        "family-father-bed",
                        "family-bed",
                        "family-fridge",
                        "family-dishes",
                        "family-player-seat",
                    }
                )
                {
                    var o = Find(w, id);
                    var goal = o.GetComponent<WorldLifeAction>()?.approach?.position ?? (Vector3)o.Point;
                    var route = new WorldNPCNavigation().Find(w, FamilyLife.Home, origin + new Vector2(-3, -5), goal);
                    check(route.Count > 0, "Real collision navigation connects living room to furniture " + id);
                }

                var wall = w.GetComponentsInChildren<BoxCollider2D>()
                    .First(c => c.name == "Parents Ian partition collision");
                check(wall.enabled && wall.size.y > 6, "Bedroom partition has persistent physical collision");
                var before = w.Player.Position;
                w.Player.Warp(origin + new Vector2(-4.6f, 4));
                w.Player.BeginTraversal(origin + new Vector2(-3.4f, 4), 2);
                yield return new WaitForSecondsRealtime(.8f);
                check(
                    w.Player.Position.x < origin.x - 4.3f,
                    "Actual player Rigidbody cannot walk through bedroom wall"
                );
                w.Player.CancelTraversal();
                w.Player.Warp(before);
                audit(w.hud.gameObject, "Architecture HUD 1280");
                yield break;
            }

            // Set up before waking, then observe real walking, door opening and object use.
            g.clock.Restore(4, new TimeSnapshot { subDaySeconds = 6 * 3600 + 30 * 60 });
            w.Player.Warp(origin + new Vector2(-3, -5));
            yield return new WaitForSecondsRealtime(12);
            var mother = scheduler.actors.First(a => a.npcId == "family-mother");
            var parentDoor = Find(w, "family-parents-door").GetComponent<WorldDoor>();
            bool opened = false,
                moved = false;
            Action<DoorState> listener = s => opened |= s == DoorState.Open;
            parentDoor.StateChanged += listener;
            Vector2 start = mother.transform.position;
            g.clock.Restore(4, new TimeSnapshot { subDaySeconds = 7 * 3600 + 60 });
            float until = Time.unscaledTime + 40;
            while (FamilyLife.State(g).ingredientCarrier != "family-mother" && Time.unscaledTime < until)
            {
                moved |= Vector2.Distance(start, mother.transform.position) > .5f;
                yield return null;
            }

            parentDoor.StateChanged -= listener;
            check(moved && opened, "Mother actually leaves private bedroom and opens its real door");
            check(
                FamilyLife.State(g).ingredientCarrier == "family-mother",
                "Mother reaches kitchen refrigerator and takes ingredients"
            );
            g.clock.Restore(4, new TimeSnapshot { subDaySeconds = 7 * 3600 + 11 * 60 });
            yield return Until(() => FamilyLife.State(g).preparedMeals == 3, 16);
            check(
                FamilyLife.State(g).preparedMeals == 3
                    && Find(w, "family-cook").GetComponent<WorldFacilityActivityPresentation>().IsPresenting,
                "Mother cooks at correctly placed kitchen counter"
            );
            yield return Shot("Architecture-Mother-Cooking");
            g.clock.Restore(4, new TimeSnapshot { subDaySeconds = 7 * 3600 + 31 * 60 });
            yield return Until(() => FamilyLife.State(g).preparedMeals == 0, 15);
            check(FamilyLife.State(g).preparedMeals == 0, "Food moves from kitchen preparation to family service");
            g.clock.Restore(4, new TimeSnapshot { subDaySeconds = 7 * 3600 + 40 * 60 });
            yield return Until(() => dining.Mask == 3, 25);
            check(dining.Mask == 3, "Both parents occupy their chairs at the ONE shared dining table");
            yield return new WaitForSecondsRealtime(1.6f);
            var seat = Find(w, "family-player-seat");
            w.Player.Warp(seat.Point);
            int meals = FamilyLife.State(g).meals;
            check(w.Begin(seat, 3), "Ian joins real shared table meal");
            var masks = new HashSet<int>();
            var frames = new HashSet<int>();
            bool shot = false;
            while (w.Busy)
            {
                masks.Add(dining.Mask);
                if (dining.Mask == 7)
                {
                    frames.Add(dining.Frame);
                    if (!shot)
                    {
                        yield return Shot("Architecture-Family-Meal");
                        shot = true;
                    }
                }

                yield return null;
            }

            check(
                masks.Contains(7) && frames.Count >= 3,
                "Shared table shows exactly three actual family occupants with eating frames"
            );
            check(
                dining.ActorFrames.Distinct().Count() > 1,
                "Family eating phases are staggered rather than synchronized"
            );
            check(
                FamilyLife.State(g).meals == meals - 1
                    && seat.GetComponent<WorldActivitySlot>().State == ActivityOccupancy.Free
                    && w.CanControl,
                $"Shared meal consumes one portion, stands and releases seat/control before={meals} after={FamilyLife.State(g).meals} seat={seat.GetComponent<WorldActivitySlot>().State} control={w.CanControl}"
            );
            g.clock.Restore(4, new TimeSnapshot { subDaySeconds = 12 * 3600 + 60 });
            yield return Until(() => FamilyLife.State(g).ingredientCarrier == "family-father", 25);
            check(
                FamilyLife.State(g).ingredientCarrier == "family-father",
                "Father follows same working kitchen route for lunch"
            );
            g.clock.Restore(4, new TimeSnapshot { subDaySeconds = 12 * 3600 + 11 * 60 });
            yield return Until(
                () => FamilyLife.State(g).cook == "family-father" && FamilyLife.State(g).preparedMeals == 3,
                16
            );
            check(
                FamilyLife.State(g).cook == "family-father" && FamilyLife.State(g).preparedMeals == 3,
                "Father also cooks in the functional kitchen"
            );
            yield return Shot("Architecture-Father-Cooking");
        }
    }
}
#endif
