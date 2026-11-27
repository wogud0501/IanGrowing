using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IanGame.Editor
{
    public static partial class WorldLayoutAuthoring
    {
        const string TransitArt = "Assets/_Game/Art/Transit/Resources/CityTransit/";
        const string StreetArt = "Assets/_Game/Art/WorldLife/";
        const string RoomCatalogPath = "Assets/_Game/Data/SceneManagement/Rooms/RoomScenes.asset";

        [MenuItem("Ian/World/Apply school, residences and transit stops")]
        public static void ApplyRealism()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Apply world authoring in Edit Mode.");
            }
            var catalog = AssetDatabase.LoadAssetAtPath<RoomSceneCatalog>(RoomCatalogPath);
            ResidenceOccupancyAuthoring.Apply(catalog);
            var portals = catalog.portals.ToList();
            var arrivals = new Dictionary<(string room, string id), Vector2>();
            foreach (
                string room in new[]
                {
                    "school",
                    "family-living",
                    "city",
                    "alley",
                    "station-city",
                    "station-alley",
                    "finance",
                    "realestate",
                    "shop",
                }
            )
            {
                var definition = catalog.Find(room);
                var path = $"Assets/_Game/Scenes/Rooms/{definition.sceneName}.unity";
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }
                try
                {
                    var world = scene
                        .GetRootGameObjects()
                        .SelectMany(o => o.GetComponentsInChildren<LifeWorldController>())
                        .Single();
                    if (room == "school")
                    {
                        SchoolRooms(world);
                    }
                    else if (room == "family-living")
                    {
                        FamilyCleaningRoute(world, AssetDatabase.LoadAssetAtPath<ResidentActivityArt>(ResidentActivityArtSetup.LibraryPath));
                    }
                    else if (room is "city" or "alley")
                    {
                        StreetStops(world, portals);
                        foreach (var entry in world.GetComponentsInChildren<RoomEntryPoint>())
                        {
                            arrivals[(room, entry.entryId)] =
                                (Vector2)entry.transform.position - world.Room.rules.Zone(room).origin;
                        }
                    }
                    else if (room.StartsWith("station-", StringComparison.Ordinal))
                    {
                        StationPlatform(world);
                        var platform = portals.First(p => p.sourceRoom == room && p.doorId == "platform");
                        platform.destinationEntryId = "platform/arrival";
                        foreach (var entry in world.GetComponentsInChildren<RoomEntryPoint>())
                        {
                            arrivals[(room, entry.entryId)] =
                                (Vector2)entry.transform.position - world.Room.rules.Zone(room).origin;
                        }
                    }
                    else
                    {
                        StaffFurniture(world, room);
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene, path))
                    {
                        throw new InvalidOperationException("Cannot save " + path);
                    }
                }
                finally
                {
                    if (opened)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }
            foreach (var portal in portals)
            {
                if (arrivals.TryGetValue((portal.destinationRoom, portal.destinationEntryId), out var p))
                {
                    portal.arrival = p;
                }
            }
            catalog.portals = portals.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        static void SchoolRooms(LifeWorldController w)
        {
            var bench = Named(w, "Student lounge bench");
            var wood = AssetDatabase.LoadAssetAtPath<Sprite>(StreetArt + "ENV_City_Bench_02.png");
            var surface = bench.GetComponentInChildren<SpriteRenderer>();
            surface.sprite = wood;
            surface.transform.localScale = Vector3.one * (2.7f / wood.bounds.size.x);
            var fountain = Object(w, "school-water");
            fountain.visual.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TransitArt + "SchoolFountain.png");
            fountain.visual.transform.localScale = Vector3.one * (.84f / fountain.visual.sprite.bounds.size.x);
            var box = fountain.GetComponent<BoxCollider2D>();
            box.size = new Vector2(.66f, .35f);
            box.offset = Vector2.up * .14f;

            // West of the desks is a corridor. Leave a real gap at each entrance.
            Partition(w, "Classroom corridor north", new Vector2(-5.12f, 2.75f), 3.4f);
            Partition(w, "Classroom corridor south", new Vector2(-5.12f, -2.65f), 2.65f);
            var divider = SpriteFrom(Architecture, "CutawayWall");
            Furnish(w, "Faculty room wall", divider, new Vector2(-7.95f, 1.0f), 1.65f, new Vector2(1.58f, .18f));
            var desk = SpriteFrom(Furniture, "WritingDesk");
            Furnish(w, "Faculty desk", desk, new Vector2(-7.22f, 3.52f), 1.68f, new Vector2(1.46f, .42f));
            Furnish(
                w,
                "Faculty chair",
                SpriteFrom(Furniture, "Chair"),
                new Vector2(-7.22f, 2.59f),
                .84f,
                new Vector2(.49f, .23f)
            );
            Seat(
                w,
                "faculty-break",
                new Vector2(-7.22f, 2.76f),
                new Vector2(-6.55f, 2.38f),
                "faculty",
                Named(w, "Faculty chair").GetComponentInChildren<SpriteRenderer>(),
                "school-teacher"
            );
            var faculty = Object(w, "faculty-break");
            faculty.title = "교무실 · 교사 휴식";
            Sign(w, "Faculty sign", "교무실", new Vector2(-8f, 1.7f), 1.9f);
            Sign(w, "Corridor sign", "복도 · 휴식석", new Vector2(-6.72f, -1.65f), 2.65f);
            var teacher = w.Room.rules.npcs.Single(n => n.npcId == "school-teacher");
            var entries = teacher.schedule.Where(e => e.objectId != "faculty-break").ToList();
            entries.Add(
                new NPCScheduleEntry
                {
                    hour = 15,
                    minute = 18,
                    dayMask = 31,
                    zone = "school",
                    action = NPCDailyAction.Rest,
                    objectId = faculty.id,
                    position = faculty.GetComponent<WorldActivitySlot>().Point - w.Room.rules.Zone("school").origin,
                }
            );
            teacher.schedule = entries.OrderBy(e => e.hour * 60 + e.minute).ToArray();
            EditorUtility.SetDirty(w.Room.rules);
        }

        static void Partition(LifeWorldController w, string name, Vector2 point, float height)
        {
            var sprite = SpriteFrom(Architecture, "VerticalWall");
            Furnish(w, name, sprite, point, .24f, new Vector2(.20f, height));
            var root = Named(w, name);
            var art = root.GetComponentInChildren<SpriteRenderer>();
            art.transform.localScale = new Vector3(.26f / sprite.bounds.size.x, height / sprite.bounds.size.y, 1);
            root.GetComponent<BoxCollider2D>().offset = Vector2.zero;
        }

        static void Sign(LifeWorldController w, string name, string text, Vector2 position, float width)
        {
            var existing = Named(w, name);
            var template = w.GetComponentsInChildren<TextMeshPro>(true)
                .FirstOrDefault(l =>
                    l.name.Contains("heading") || l.name == "Bus stop label" || l.name.Contains("표지")
                );
            if (!template)
            {
                template = w.GetComponentsInChildren<TextMeshPro>(true).FirstOrDefault();
            }
            if (!template)
            {
                return;
            }
            var label = existing
                ? existing.GetComponent<TextMeshPro>()
                : UnityEngine.Object.Instantiate(template, w.transform);
            label.name = name;
            label.text = text;
            label.rectTransform.sizeDelta = new Vector2(width, .62f);
            label.fontSize = 2.1f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            Place(w, label.transform, position.x, position.y);
            label.gameObject.SetActive(true);
        }

        static void StreetStops(LifeWorldController w, List<RoomPortalDefinition> portals)
        {
            bool city = w.Room.roomId == "city";
            string dest = city ? "alley" : "city";
            var shelter = w.GetComponentsInChildren<SpriteRenderer>(true)
                .FirstOrDefault(s => s.sprite && s.sprite.name == "ENV_City_BusStop_01");
            if (!shelter)
            {
                Furnish(
                    w,
                    "Residential bus stop",
                    AssetDatabase.LoadAssetAtPath<Sprite>(StreetArt + "ENV_City_BusStop_01.png"),
                    new Vector2(1.4f, .65f),
                    2.7f,
                    new Vector2(2.35f, .4f)
                );
                shelter = Named(w, "Residential bus stop").GetComponentInChildren<SpriteRenderer>();
            }
            Vector2 origin = w.Room.rules.Zone(w.Room.roomId).origin;
            // Queue on the pedestrian side of the canopy: the north pavement in the city.
            Vector2 busPoint =
                (Vector2)shelter.transform.position - origin + (city ? new Vector2(1.6f, .9f) : new Vector2(0, -.9f));
            var taxiPoint = city ? new Vector2(3.1f, 1.12f) : new Vector2(-3.4f, -.1f);
            Furnish(
                w,
                "Taxi stand",
                AssetDatabase.LoadAssetAtPath<Sprite>(TransitArt + "TaxiStand.png"),
                taxiPoint + Vector2.up * .72f,
                .72f,
                new Vector2(.22f, .21f)
            );
            Stop(w, "bus-stop", "02번 버스 정류장", busPoint, shelter, dest, portals);
            Stop(
                w,
                "taxi-rank",
                "택시 승차대",
                taxiPoint,
                Named(w, "Taxi stand").GetComponentInChildren<SpriteRenderer>(),
                dest,
                portals
            );
            var stairs = Object(w, "subway-entrance");
            Entry(w, "subway-entrance", stairs.Point - origin);
            var exit = portals.First(p => p.sourceRoom == "station-" + w.Room.roomId && p.doorId == "station-exit");
            exit.destinationEntryId = "subway-entrance/arrival";
            exit.arrival = stairs.Point - origin;
            var old = Named(w, "Bus stop label");
            if (old)
            {
                old.GetComponent<TMP_Text>().text = "02번 · 도심 ↔ 주거지역";
            }
        }

        static void Stop(
            LifeWorldController w,
            string id,
            string name,
            Vector2 point,
            SpriteRenderer art,
            string dest,
            List<RoomPortalDefinition> portals
        )
        {
            var item = w.objects.FirstOrDefault(o => o && o.id == id);
            if (!item)
            {
                var root = Anchor(w.transform, id, Vector2.zero);
                item = root.gameObject.AddComponent<WorldInteractable>();
                w.objects = w.objects.Append(item).ToArray();
            }
            Place(w, item.transform, point.x, point.y);
            item.id = id;
            item.kind = WorldInteractionKind.Door;
            item.title = name;
            item.zone = w.Room.roomId;
            item.targetZone = dest;
            item.radius = 1.45f;
            item.UsePoint = Vector2.zero;
            item.visual = art;
            Entry(w, id, point);
            var portal = portals.FirstOrDefault(p => p.sourceRoom == item.zone && p.doorId == id);
            if (portal == null)
            {
                portal = new RoomPortalDefinition();
                portals.Add(portal);
            }
            portal.doorId = id;
            portal.sourceRoom = item.zone;
            portal.destinationRoom = dest;
            portal.destinationEntryId = id + "/arrival";
            portal.facing = Vector2.down;
        }

        static void Entry(LifeWorldController w, string id, Vector2 point)
        {
            var entry = w.GetComponentsInChildren<RoomEntryPoint>(true)
                .FirstOrDefault(p => p.entryId == id + "/arrival");
            if (!entry)
            {
                entry = new GameObject("Arrival " + id).AddComponent<RoomEntryPoint>();
                entry.transform.SetParent(w.transform, false);
            }
            entry.entryId = id + "/arrival";
            entry.doorId = id;
            entry.facing = Vector2.down;
            Place(w, entry.transform, point.x, point.y);
        }

        static void StaffFurniture(LifeWorldController world, string room)
        {
            if (room == "shop")
            {
                Furnish(
                    world,
                    "Staff tea table",
                    AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Housing/ApartmentTable.png"),
                    new Vector2(5.12f, 2.02f),
                    .9f,
                    new Vector2(.55f, .25f)
                );
                return;
            }
            Furnish(
                world,
                "Staff cupboard",
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Housing/ApartmentCupboard.png"),
                new Vector2(4.9f, -1.35f),
                .75f,
                new Vector2(.57f, .27f)
            );
        }

        static void StationPlatform(LifeWorldController w)
        {
            // The station scenes were cloned from the office washroom. Keep their outer physics shell.
            foreach (var sr in w.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (
                    sr.transform.parent && sr.transform.parent.name.Contains("office-bathroom")
                    || sr.name == "Interior threshold / office-bath-exit"
                )
                {
                    sr.enabled = false;
                }
            }
            var train = Object(w, "platform");
            var body = train.GetComponent<BoxCollider2D>();
            Physics2D.SyncTransforms();
            // The threshold is outside the solid car. The driver's walking circle must fit between queue lanes.
            train.UsePoint = new Vector2(0, body.bounds.min.y - .58f - train.transform.position.y);
            Entry(w, "platform", train.Point - w.Room.rules.Zone(w.Room.roomId).origin);
        }
    }
}
