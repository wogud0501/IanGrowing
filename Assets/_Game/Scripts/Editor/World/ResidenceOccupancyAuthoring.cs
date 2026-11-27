using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IanGame.Editor
{
    internal static class ResidenceOccupancyAuthoring
    {
        const string Folder = "Assets/_Game/Prefabs/World/Residence";

        internal static void Apply(RoomSceneCatalog catalog)
        {
            foreach (string type in new[] { "Family", "Studio", "Small", "Older" })
            {
                string path = Folder + type + ".prefab";
                using var edit = new PrefabUtility.EditPrefabContentsScope(path);
                AddBathroomFixture(edit.prefabContentsRoot, "WC", "house-wc", "화장실", WorldInteractionKind.Toilet);
                AddBathroomFixture(
                    edit.prefabContentsRoot,
                    "Shower bay",
                    "house-shower",
                    "샤워실",
                    WorldInteractionKind.Shower
                );
            }
            var small = Load("Small");
            var family = Load("Family");
            var children = SharedBedroom(family, true);
            var adults = SharedBedroom(family, false);
            foreach (var unit in catalog.rooms.Where(r => r.floorPlan))
            {
                var people = catalog.rules.npcs.Where(n => n.homeZone == unit.roomId).ToList();
                var assigned = people
                    .Where(n => !string.IsNullOrEmpty(n.residentBedId))
                    .Select(n => n.homePlace)
                    .ToHashSet();
                foreach (var n in people.Where(n => string.IsNullOrEmpty(n.residentBedId)))
                {
                    int place = 0;
                    while (!assigned.Add(place))
                    {
                        place++;
                    }
                    n.homePlace = place;
                    n.residentBedId = n.npcId + "-bed";
                }
                var plan = unit.roomId switch
                {
                    "apt-102" => children,
                    "apt-203" => adults,
                    "residence" => family,
                    "apt-201" or "apt-202" or "apt-302" => small,
                    _ => unit.floorPlan,
                };
                unit.floorPlan = plan;
                var zone = catalog.rules.Zone(unit.roomId);
                if (plan == family || plan == children || plan == adults || plan == small)
                {
                    zone.size = new Vector2(plan == small ? 13.1f : 14.24f, 11.8f);
                    zone.walkableTop = plan == small ? 4.05f : 4.65f;
                    zone.cameraSize = plan == small ? 5.95f : 6.2f;
                }
                var fixtures = plan.GetComponentsInChildren<WorldInteractable>(true).ToDictionary(o => o.id);
                foreach (var n in people)
                {
                    if (
                        n.ambient
                        && !n.schedule.Any(e =>
                            e.zone == n.homeZone && e.action == NPCDailyAction.Sleep && e.objectId == n.residentBedId
                        )
                    )
                    {
                        GiveHomeRoutine(n);
                    }
                    var bed = fixtures["bed-" + n.homePlace].GetComponent<BedQuilt>();
                    n.hasHomePosition = true;
                    n.homePosition = plan.transform.InverseTransformPoint(bed.approachAnchor.position);
                    foreach (var e in n.schedule.Where(e => e.zone == unit.roomId))
                    {
                        string key = e.objectId;
                        if (key == n.residentBedId)
                        {
                            e.position = n.homePosition;
                            continue;
                        }
                        if (key == n.residentBedId + "-rest")
                        {
                            key = "rest-" + n.homePlace;
                        }
                        if (!string.IsNullOrEmpty(key) && fixtures.TryGetValue(key, out var item))
                        {
                            var slot = item.GetComponent<WorldActivitySlot>();
                            e.position = plan.transform.InverseTransformPoint(slot ? slot.Point : item.Point);
                        }
                    }
                }
            }
            EditorUtility.SetDirty(catalog.rules);
            EditorUtility.SetDirty(catalog);
        }

        static ResidenceFloorPlan Load(string type)
        {
            return AssetDatabase
                .LoadAssetAtPath<GameObject>(Folder + type + ".prefab")
                .GetComponent<ResidenceFloorPlan>();
        }

        static void GiveHomeRoutine(WorldNPCDefinition npc)
        {
            int i = int.Parse(npc.npcId.Substring("ambient-urban-".Length));
            var entries = npc.schedule.Where(e => e.zone != npc.homeZone).ToList();
            void At(int minute, string id, NPCDailyAction action)
            {
                entries.Add(
                    new NPCScheduleEntry
                    {
                        hour = minute / 60,
                        minute = minute % 60,
                        zone = npc.homeZone,
                        objectId = id,
                        action = action,
                        dayMask = 127,
                    }
                );
            }
            int rise = 335 + i * 4;
            At(0, npc.residentBedId, NPCDailyAction.Sleep);
            At(rise, npc.residentBedId, NPCDailyAction.Wake);
            At(rise + 8, "house-wash", NPCDailyAction.Wash);
            At(rise + 25, "house-dining-" + npc.homePlace, NPCDailyAction.Eat);
            int evening = Mathf.Max(1205 + i * 7, entries.Max(e => e.hour * 60 + e.minute) + 12);
            At(evening, "house-dining-" + npc.homePlace, NPCDailyAction.Eat);
            At(evening + 15, npc.residentBedId + "-rest", NPCDailyAction.WatchTV);
            At(Mathf.Max(1340 + i * 5, evening + 35), npc.residentBedId, NPCDailyAction.Sleep);
            npc.schedule = entries.OrderBy(e => e.hour * 60 + e.minute).ToArray();
        }

        static void AddBathroomFixture(GameObject root, string name, string id, string title, WorldInteractionKind kind)
        {
            var fixture = root.GetComponentsInChildren<Transform>().First(t => t.name == name);
            if (!fixture.TryGetComponent<WorldInteractable>(out var item))
            {
                item = fixture.gameObject.AddComponent<WorldInteractable>();
            }
            item.id = id;
            item.title = title;
            item.kind = kind;
            item.visual = fixture.GetComponentInChildren<SpriteRenderer>();
            item.UsePoint = Vector2.down * (kind == WorldInteractionKind.Shower ? 1.12f : .86f);
            item.radius = 1.35f;
        }

        static void AlignSharedBedroom(GameObject apartment, bool children)
        {
            var bed = apartment.GetComponentsInChildren<WorldInteractable>().Single(o => o.id == "bed-2");
            var otherBed = apartment
                .GetComponentsInChildren<WorldInteractable>()
                .Single(o => o.id == (children ? "bed-1" : "bed-0"));
            var companion = bed.transform.parent.Find(children ? "Writing desk" : "Linen wardrobe");
            // Both headboards meet the back wall, with room for two people in the bedside aisle.
            float backWall = otherBed.transform.localPosition.y;
            otherBed.transform.localPosition = new Vector2(children ? -1.38f : -6.30f, backWall);
            bed.transform.localPosition = new Vector2(children ? 1.73f : -3.06f, backWall);
            var quilt = bed.GetComponent<BedQuilt>();
            foreach (var landing in new[] { quilt.approachAnchor, quilt.exitAnchor }.Distinct())
            {
                var local = landing.localPosition;
                landing.localPosition = new Vector3(local.x, -.73f, local.z);
            }
            bed.UsePoint = quilt.approachAnchor.position - bed.transform.position;
            companion.localPosition = children ? new Vector2(.14f, 4.18f) : new Vector2(-4.60f, 4.18f);
        }

        static ResidenceFloorPlan SharedBedroom(ResidenceFloorPlan source, bool children)
        {
            string path = Folder + (children ? "SharedChildren" : "SharedAdults") + ".prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset)
            {
                using (var edit = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    AlignSharedBedroom(edit.prefabContentsRoot, children);
                }
                return Load(children ? "SharedChildren" : "SharedAdults");
            }
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(source.gameObject, scene);
                var fixtures = root.GetComponentsInChildren<WorldInteractable>().ToDictionary(o => o.id);
                var original = fixtures[children ? "bed-1" : "bed-0"];
                var bed = Object.Instantiate(original, original.transform.parent);
                bed.name = children ? "Child bed by inner wall" : "Second adult bed";
                bed.id = "bed-2";
                AlignSharedBedroom(root, children);
                var quilt = bed.GetComponent<BedQuilt>();
                foreach (var anchor in new[] { quilt.approachAnchor, quilt.exitAnchor, quilt.sitAnchor }.Distinct())
                {
                    var p = anchor.localPosition;
                    anchor.localPosition = new Vector3(-p.x, p.y, p.z);
                }
                bed.UsePoint = quilt.approachAnchor.position - bed.transform.position;

                var originalDining = fixtures["house-dining-0"];
                var extra = Object.Instantiate(originalDining, originalDining.transform.parent);
                extra.id = "house-dining-2";
                extra.name = "Dining chair 2";
                extra.transform.localPosition = new Vector2(5.22f, -3.62f);
                originalDining.transform.localPosition = new Vector2(4.38f, -3.62f);
                fixtures["house-dining-1"].transform.localPosition = new Vector2(6.06f, -3.62f);

                var rest = Object.Instantiate(fixtures["rest-1"], fixtures["rest-1"].transform.parent);
                rest.name = "Reading chair beside sofa";
                rest.id = "rest-2";
                rest.title = "거실 독서 의자";
                var chair = Object.Instantiate(originalDining.visual, rest.transform);
                chair.name = "Chair";
                // Match the individual chair to the sofa slot's authored hip contact.
                var station = rest.GetComponent<ResidentActivityStation>();
                var slot = rest.GetComponent<WorldActivitySlot>();
                rest.transform.localPosition = new Vector2(-3.45f, -3.42f);
                chair.transform.position = slot.Point + Vector2.down * .08f;
                if (station)
                {
                    station.seatSurface = chair;
                }
                rest.visual = chair;
                var chairBody = rest.gameObject.AddComponent<BoxCollider2D>();
                chairBody.size = new Vector2(.46f, .20f);
                chairBody.offset = new Vector2(0, .19f);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (!saved)
                {
                    throw new InvalidOperationException("Could not author " + path);
                }
                return saved.GetComponent<ResidenceFloorPlan>();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
