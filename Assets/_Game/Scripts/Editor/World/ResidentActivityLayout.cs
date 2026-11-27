using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace IanGame.Editor
{
    public static partial class WorldLayoutAuthoring
    {
        static T Component<T>(GameObject owner)
            where T : Component => owner.TryGetComponent<T>(out var component) ? component : owner.AddComponent<T>();

        static WorldInteractable ActivityPoint(LifeWorldController world, string id, Vector2 point)
        {
            var target = world.objects.FirstOrDefault(o => o && o.id == id);
            if (!target)
            {
                var parent = Anchor(world.transform, "Resident activity points", Vector2.zero);
                var go = new GameObject(id);
                go.transform.SetParent(parent, false);
                target = go.AddComponent<WorldInteractable>();
                target.id = id;
                target.zone = world.Room.roomId;
                target.kind = WorldInteractionKind.Rest;
                target.UsePoint = Vector2.zero;
                target.enabled = false;
                world.objects = world.objects.Append(target).ToArray();
            }

            Place(world, target.transform, point.x, point.y);
            return target;
        }

        static void Seat(
            LifeWorldController world,
            string id,
            Vector2 point,
            Vector2 approach,
            string group,
            SpriteRenderer surface,
            string staff = ""
        )
        {
            var target = ActivityPoint(world, id, point);
            var slot = Component<WorldActivitySlot>(target.gameObject);
            var station = Component<ResidentActivityStation>(target.gameObject);
            station.breakSeat = true;
            station.seatGroup = group;
            station.seatSurface = surface;
            slot.seated = true;
            slot.useSeatAnchors = true;
            slot.facing = Vector2.down;
            slot.seat = null;
            slot.staffId = staff;
            slot.activityMotion = ActorMotion.SitRelax;
            slot.seatAnchor = Anchor(slot.transform, "Seat", Vector2.zero);
            slot.approachAnchor = Anchor(
                slot.transform,
                "Approach",
                slot.transform.InverseTransformVector(approach - point)
            );
            slot.standUpAnchor = slot.approachAnchor;
            var interaction = surface ? surface.GetComponent<WorldInteractable>() : null;
            if (interaction && interaction.enabled && interaction.kind == WorldInteractionKind.Rest)
            {
                var action = Component<WorldLifeAction>(interaction.gameObject);
                action.mode = LifeActionVisual.Seat;
                action.seat = slot;
                action.approach = slot.approachAnchor;
                action.anchor = slot.seatAnchor;
                action.standUp = slot.standUpAnchor;
                action.facing = slot.facing;
                action.activePose = "SeatedIdle";
            }
        }

        static void FamilyCleaningRoute(LifeWorldController world, ResidentActivityArt library)
        {
            // Both parents take tools from the south side of the coffee table.
            var start = new Vector2(-3.93f, -1.62f);
            Chore(world, "family-clean", start, Vector2.right,
                new[] { ActorMotion.Sweep, ActorMotion.Mop, ActorMotion.Vacuum },
                start, new Vector2(-3.10f, -2.15f), new Vector2(-1.45f, -1.65f),
                new Vector2(.15f, -2.35f), start);
            Furnish(world, "Cleaning tool holder", library.tools[0], new Vector2(-4.23f, -1.68f), .58f, Vector2.zero);
            foreach (var resident in world.Room.rules.npcs)
            {
                foreach (var entry in resident.schedule.Where(e => e.zone == "family-living" && e.objectId == "family-clean"))
                {
                    entry.position = start;
                }
            }
            EditorUtility.SetDirty(world.Room.rules);
        }

        static void Fixture(
            LifeWorldController w,
            string name,
            string atlas,
            string sprite,
            float x,
            float y,
            float width,
            Vector2 footprint = default
        )
        {
            Furnish(w, name, SpriteFrom(atlas, sprite), new Vector2(x, y), width, footprint);
        }

        static void Chore(
            LifeWorldController w,
            string id,
            Vector2 at,
            Vector2 face,
            ActorMotion[] tasks,
            params Vector2[] points
        )
        {
            var target = ActivityPoint(w, id, at);
            var station = Component<ResidentActivityStation>(target.gameObject);
            var slot = Component<WorldActivitySlot>(target.gameObject);
            slot.useSeatAnchors = false;
            slot.seated = false;
            slot.facing = face;
            station.workingFacing = face;
            station.tasks = tasks;
            station.workRoute = points.Select(p => p - at).ToArray();
        }

        static bool ActivityLayout(LifeWorldController w)
        {
            var room = w.Room.roomId;
            var library = AssetDatabase.LoadAssetAtPath<ResidentActivityArt>(ResidentActivityArtSetup.LibraryPath);
            var scheduler = w.GetComponent<WorldNPCScheduler>();
            if (!library || !scheduler)
            {
                return false;
            }

            bool assigned = scheduler.activityArt != library;
            scheduler.activityArt = library;
            var origin = w.Room.catalog.rules.Zone(room).origin;
            foreach (
                var bed in w
                    .objects.Where(o => o && o.kind == WorldInteractionKind.Sleep && o.visual && o.visual.sprite)
                    .ToArray()
            )
            {
                var mat = bed.visual.bounds;
                var point = new Vector2(mat.center.x, mat.min.y + .28f) - origin;
                var owner = w.Room.catalog.rules.npcs.FirstOrDefault(n =>
                    n.schedule.Any(e => e.action == NPCDailyAction.Sleep && e.objectId == bed.id)
                );
                Seat(w, bed.id + "-rest", point, bed.Point - origin, "bed-" + bed.id, bed.visual, owner?.npcId ?? "");
                assigned = true;
            }

            switch (room)
            {
                case "family-living":
                    var sofa = Object(w, "family-sofa").visual;
                    var oldBack = sofa.transform.Find("Foreground sofa back");
                    if (oldBack)
                    {
                        oldBack.gameObject.SetActive(false);
                        if (oldBack.TryGetComponent<SpriteRenderer>(out var duplicate))
                        {
                            duplicate.sprite = null;
                        }
                    }
                    foreach (var id in new[] { "family-tv-mother", "family-tv-father" })
                    {
                        var breakSeat = Component<ResidentActivityStation>(Object(w, id).gameObject);
                        breakSeat.breakSeat = true;
                        breakSeat.seatGroup = "family-sofa";
                        breakSeat.seatSurface = sofa;
                    }
                    FamilyCleaningRoute(w, library);
                    Chore(
                        w,
                        "family-waste",
                        new Vector2(1.85f, -.9f),
                        Vector2.right,
                        new[] { ActorMotion.CarryTrash },
                        new(1.85f, -.9f),
                        new(2.35f, -1.4f),
                        new(2.8f, -2.15f),
                        new(2.75f, -2.15f)
                    );
                    break;
                case "family-ian-bedroom":
                    Chore(
                        w,
                        "family-care",
                        new Vector2(.2f, -.9f),
                        Vector2.up,
                        new[] { ActorMotion.CarryLaundry },
                        new(.5f, -.9f),
                        new(1.33f, -.5f),
                        new(1.65f, -.38f),
                        new(1.6f, -.42f)
                    );
                    Furnish(w, "Laundry hamper", library.tools[6], new Vector2(.6f, -.35f), .63f, new Vector2(.43f, .22f));
                    break;
                case "family-kitchen":
                    var sink = Object(w, "family-dishes");
                    Vector2 at = (Vector2)sink.transform.position - origin;
                    Vector2 tap = sink.Point - origin + Vector2.up * .32f;
                    Chore(
                        w,
                        sink.id,
                        at,
                        Vector2.up,
                        new[] { ActorMotion.WashDishes },
                        tap + Vector2.left * .3f,
                        tap,
                        tap + Vector2.right * .34f,
                        tap + Vector2.right * .3f
                    );
                    Furnish(
                        w,
                        "Dishes beside basin",
                        library.tools[7],
                        at + new Vector2(-.44f, .6f),
                        .55f,
                        Vector2.zero
                    );
                    sink.GetComponent<ResidentActivityStation>().dirtyDishes = Named(w, "Dishes beside basin")
                        .GetComponentInChildren<SpriteRenderer>();
                    sink.GetComponent<ResidentActivityStation>().dirtyDishes.enabled = false;
                    Chore(
                        w,
                        "family-table-wipe",
                        new Vector2(1, -2.65f),
                        Vector2.up,
                        new[] { ActorMotion.WipeTable },
                        new(2.8f, -2.65f),
                        new(1.35f, -2.55f),
                        new(0, -2.55f),
                        new(-1.5f, -2.55f),
                        new(-2.6f, -2.65f)
                    );
                    break;
                case "city":
                    var bench = Object(w, "city-bench");
                    Seat(w, "city-bench-rest", new Vector2(4.3f, -7.93f), bench.Point - origin, "plaza", bench.visual);
                    break;
                case "workplace":
                    Place(w, Object(w, "work-rest").transform, -6.2f, 3.2f);
                    Place(w, Object(w, "work-water").transform, -3.3f, 3.65f);
                    Place(w, Named(w, "Pantry shelf"), -3.3f, 4.85f);
                    foreach (
                        var sr in w.GetComponentsInChildren<SpriteRenderer>(true)
                            .Where(sr => sr.name.StartsWith("Lounge chair "))
                    )
                    {
                        sr.enabled = false;
                    }
                    Seat(
                        w,
                        "office-break-0",
                        new(-6.61f, 3.39f),
                        new(-7.7f, 2.7f),
                        "office-pantry",
                        Object(w, "work-rest").visual
                    );
                    Seat(
                        w,
                        "office-break-1",
                        new(-5.78f, 3.39f),
                        new(-4.6f, 2.75f),
                        "office-pantry",
                        Object(w, "work-rest").visual
                    );
                    Fixture(w, "Staff refrigerator", Furniture, "Fridge", -8.0f, 3.75f, 1.0f, new(.8f, .35f));
                    Fixture(
                        w,
                        "Pantry side partition",
                        Architecture,
                        "VerticalWall",
                        -2.0f,
                        3.15f,
                        .27f,
                        new(.23f, 1.45f)
                    );
                    break;
                case "school":
                    Furnish(w, "Student lounge bench", AssetDatabase.LoadAssetAtPath<Sprite>(StreetArt + "ENV_City_Bench_02.png"),
                        new Vector2(-6.65f, -2.8f), 2.7f, new Vector2(2.25f, .34f));
                    Seat(
                        w,
                        "school-break-0",
                        new(-7.2f, -2.56f),
                        new(-7.9f, -3.35f),
                        "school-lounge",
                        Named(w, "Student lounge bench").GetComponentInChildren<SpriteRenderer>()
                    );
                    Seat(
                        w,
                        "school-break-1",
                        new(-6.05f, -2.56f),
                        new(-5.15f, -3.35f),
                        "school-lounge",
                        Named(w, "Student lounge bench").GetComponentInChildren<SpriteRenderer>()
                    );
                    break;
                case "finance":
                case "realestate":
                    var legacy = Object(w, room + "-staff-rest-0").GetComponent<WorldFacilityActivityPresentation>();
                    if (legacy)
                    {
                        legacy.enabled = false;
                        legacy.empty.enabled = false;
                        legacy.occupied.enabled = false;
                    }
                    Fixture(w, "Staff lounge sofa", Furniture, "Settee", 3.55f, -3.0f, 2.65f, new(2.15f, .35f));
                    Fixture(
                        w,
                        "Staff privacy partition",
                        Architecture,
                        "VerticalWall",
                        1.5f,
                        -3.6f,
                        .29f,
                        new(.23f, 1.45f)
                    );
                    StaffFurniture(w, room);
                    Seat(
                        w,
                        room + "-staff-rest-0",
                        new(2.95f, -2.72f),
                        new(2.2f, -3.65f),
                        room + "-staff",
                        Named(w, "Staff lounge sofa").GetComponentInChildren<SpriteRenderer>()
                    );
                    Seat(
                        w,
                        room + "-staff-rest-1",
                        new(4.15f, -2.72f),
                        new(4.95f, -3.65f),
                        room + "-staff",
                        Named(w, "Staff lounge sofa").GetComponentInChildren<SpriteRenderer>()
                    );
                    break;
                case "shop":
                    Place(w, Named(w, "Kitchen service"), 3.5f, 3.85f);
                    Place(w, Named(w, "Beverage station"), 6.25f, 3.85f);
                    Fixture(w, "Staff locker", Furniture, "Wardrobe", 7.9f, 3.65f, .95f, new(.77f, .3f));
                    Fixture(w, "Stockroom partition", Architecture, "VerticalWall", 2.5f, 1.95f, .27f, new(.23f, 1.4f));
                    Fixture(w, "Stockroom folding chair", Furniture, "Chair", 4.2f, 2.05f, .88f, new(.62f, .26f));
                    Fixture(w, "Cafe staff chair", Furniture, "Chair", 6.1f, 2.05f, .88f, new(.62f, .26f));
                    StaffFurniture(w, room);
                    Seat(
                        w,
                        "shop-backroom-seat",
                        new(4.2f, 2.18f),
                        new(4.35f, 1.4f),
                        "retail-staff",
                        Named(w, "Stockroom folding chair").GetComponentInChildren<SpriteRenderer>()
                    );
                    Seat(
                        w,
                        "cafe-backroom-seat",
                        new(6.1f, 2.18f),
                        new(5.82f, 1.4f),
                        "retail-staff",
                        Named(w, "Cafe staff chair").GetComponentInChildren<SpriteRenderer>()
                    );
                    break;
                default:
                    return assigned;
            }

            return true;
        }
    }
}
