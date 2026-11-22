using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static IanGame.NPCDailyAction;

namespace IanGame.Editor
{
    // Times are local game minutes. The room and position of furniture come from the scene that owns it.
    public static class ResidentRoutineAuthoring
    {
        const int Weekdays = 31;
        const int Weekend = 96;

        readonly struct Place
        {
            public readonly string Room;
            public readonly Vector2 Position;

            public Place(string room, Vector2 position)
            {
                Room = room;
                Position = position;
            }
        }

        sealed class DayPlan
        {
            readonly Dictionary<string, Place> places;
            public readonly List<NPCScheduleEntry> Entries = new();

            public DayPlan(Dictionary<string, Place> places)
            {
                this.places = places;
            }

            public bool Has(string id) => places.ContainsKey(id);

            public void At(int time, string id, NPCDailyAction action, int days = 127)
            {
                if (action == Rest && places.ContainsKey(id + "-rest"))
                {
                    id += "-rest";
                }
                var place = places[id];
                Visit(time, place.Room, place.Position, action, days, id);
                if (action == Wake && place.Room == "residence")
                {
                    At(time + 6, id, Rest, days);
                }
            }

            public void Visit(
                int time,
                string room,
                Vector2 point,
                NPCDailyAction action,
                int days = 127,
                string id = ""
            )
            {
                Entries.Add(
                    new NPCScheduleEntry
                    {
                        hour = time / 60,
                        minute = time % 60,
                        zone = room,
                        objectId = id,
                        position = point,
                        action = action,
                        dayMask = days,
                        allowStroll = room == "alley" && action == Walk,
                    }
                );
            }

            public void Commute(int time, string entrance, int lane, int days)
            {
                var door = places[entrance];
                // Keep the doorway clear even when the next shift is already on the sidewalk.
                float lateral = lane == 0 ? 0 : (lane + 1) / 2 * 1.85f * (lane % 2 == 1 ? -1 : 1);
                var sidewalk = new Vector2(
                    Mathf.Clamp(door.Position.x + lateral, -14.7f, 14.7f),
                    Mathf.Max(-1.6f, door.Position.y - 2.6f - lane % 2 * .55f)
                );
                Visit(time, door.Room, sidewalk, Walk, days);
            }

            public void Street(int time, int lane, int days = 127)
            {
                Visit(time, "city", new Vector2(-9 + lane % 8 * 2.3f, lane % 2 == 0 ? 1 : -7.4f), Walk, days);
            }
        }

        static void Family(WorldNPCDefinition d, DayPlan p)
        {
            bool mother = d.npcId == "family-mother";
            d.companionId = mother ? "family-father" : "family-mother";
            string bed = d.npcId + "-bed";
            string seat = mother ? "family-dine-1" : "family-dine-2";
            // Keep the original first 18 indices. Existing saves key meal receipts by day and entry index.
            p.At(0, bed, Sleep);
            p.At(mother ? 405 : 415, bed, Wake);
            if (mother)
            {
                p.At(420, "family-fridge", GetIngredients);
                p.At(430, "family-cook", Cook);
                p.At(450, "family-serve", ServeMeal);
                p.At(455, seat, Eat);
                p.At(480, "family-dishes", WashDishes);
                p.At(540, "family-clean", Clean);
                p.Street(630, 3);
                p.At(660, "counter", Shop);
                p.Street(680, 5);
                p.At(1090, "family-fridge", GetIngredients);
                p.At(1100, "family-cook", Cook);
                p.At(1120, "family-serve", ServeMeal);
                p.At(1125, seat, Eat);
                p.At(1150, "family-dishes", WashDishes);
                p.At(1200, "family-tv-mother", WatchTV);
                p.At(1350, bed, Sleep);
                p.At(409, "family-basin", Wash);
                p.At(735, seat, Eat);
                p.At(775, "family-tv-mother", WatchTV);
                p.At(905, "family-care", Clean);
                p.At(922, "family-tv-mother", Rest);
            }
            else
            {
                p.At(425, "family-care", Clean);
                p.At(458, seat, Eat);
                p.At(485, "family-clean", Clean);
                p.Street(540, 1);
                p.At(720, "family-fridge", GetIngredients);
                p.At(730, "family-cook", Cook);
                p.At(750, "family-serve", ServeMeal);
                p.At(755, seat, Eat);
                p.At(780, "family-dishes", WashDishes);
                p.At(930, "counter", Shop);
                p.Street(950, 4);
                p.At(1020, "family-clean", Clean);
                p.At(1128, seat, Eat);
                p.At(1140, "family-care", Clean);
                p.At(1260, "family-tv-father", WatchTV);
                p.At(1380, bed, Sleep);
                p.At(419, "family-basin", Wash);
                p.At(569, "city-bench", Rest);
                p.At(802, "family-tv-father", WatchTV);
                p.Street(907, 4);
                p.At(1055, "family-tv-father", WatchTV);
                p.At(438, "family-tv-father", WatchTV);
                p.At(1156, "family-tv-father", WatchTV);
            }
            if (p.Has("family-table-wipe"))
            {
                if (mother)
                {
                    p.At(496, "family-table-wipe", Clean);
                    p.At(516, "family-tv-mother", Rest);
                    p.At(569, "family-tv-mother", Rest);
                }
                else
                {
                    p.At(507, "family-waste", Clean);
                    p.At(524, "family-tv-father", Rest);
                }
            }
            string tvSeat = mother ? "family-tv-mother" : "family-tv-father";
            p.At(1225, tvSeat, Talk);
            p.At(1237, tvSeat, WatchTV);
        }

        static void School(WorldNPCDefinition d, DayPlan p, int i, string bed)
        {
            bool teacher = d.jobId == "teacher";
            string desk = teacher ? "teacher-lectern" : "school-seat-" + i;
            var lesson = teacher ? Work : Study;
            p.At(0, bed, Sleep);
            p.At(397 + i * 9, bed, Wake);
            p.Commute(460 + i * 4, "school-front", i, Weekdays);
            p.At(486 + i * 6, desk, lesson, Weekdays);
            p.At(
                teacher ? 610 : 615 + i * 3,
                teacher ? "teacher-board-point" : "school-break-" + i,
                teacher ? Talk : Rest,
                Weekdays
            );
            p.At(teacher ? 625 : 630, desk, lesson, Weekdays);
            if (teacher)
            {
                p.At(626, desk, Talk, Weekdays);
                p.At(629, desk, lesson, Weekdays);
            }

            p.At(725 + i * 2, "cafe-table-" + i, Eat, Weekdays);
            p.At(758 + i * 2, desk, lesson, Weekdays);
            p.Street(teacher ? 1002 : 940 + i * 16, i, Weekdays);
            p.At(820 + i * 8, "shop-browse-" + i, Shop, Weekend);
            p.Street(835 + i * 8, i, Weekend);
            p.At(1065 + i * 5, bed, Rest);
            if (d.npcId == "classmate")
            {
                p.Visit(1200, "city", new Vector2(3.6f, -7.6f), Talk);
                p.At(1230, bed, Rest);
            }

            p.At(1337 + i * 7, bed, Sleep);
        }

        static void DayWorker(WorldNPCDefinition d, DayPlan p, int i, string bed)
        {
            int[] starts = { 535, 543, 550, 540, 549, 555, 560 };
            int[] lunches = { 723, 763, 765, 765, 804, 770, 806 };
            int[] seats = { 3, 0, 1, 2, 0, 3, 1 };
            int[] ends = { 1037, 1051, 1087, 1010, 1020, 1058, 1072 };
            string desk = "staff-desk-" + d.npcId;
            string pause =
                d.workZone == "workplace"
                    ? "office-break-" + i % 2
                    : d.workZone + "-staff-rest-" + (i == 4 || i == 6 ? 1 : 0);
            p.At(0, bed, Sleep);
            p.At(402 + i * 5, bed, Wake);
            string entrance = d.workZone == "workplace" ? "office-front" : d.workZone + "-front";
            p.Commute(starts[i] - 38, entrance, i, Weekdays);
            p.At(starts[i], desk, Work, Weekdays);
            p.At(600 + i * 12, pause, Rest, Weekdays);
            p.At(610 + i * 12 + i % 5, desk, Work, Weekdays);
            p.At(lunches[i], "cafe-table-" + seats[i], Eat, Weekdays);
            p.At(lunches[i] + 35, desk, Work, Weekdays);
            p.Street(ends[i], i + 1, Weekdays);
            p.At(ends[i] + 20, "shop-browse-" + i % 6, Shop, Weekdays);
            p.Street(ends[i] + 33, i, Weekdays);
            p.Street(650 + i * 11, i, Weekend);
            p.At(895 + i * 9, "shop-browse-" + i % 6, Shop, Weekend);
            p.Street(909 + i * 9, i + 1, Weekend);
            p.At(1120 + i * 3, bed, Rest);
            if (d.npcId == "colleague")
            {
                p.Visit(1200, "city", new Vector2(2.5f, -7.6f), Talk);
                p.At(1231, bed, Rest);
            }

            p.At(1360 + i * 4, bed, Sleep);
        }

        static void NightWorker(WorldNPCDefinition d, DayPlan p, int i, string bed)
        {
            // One overnight crew member each night. The mask at midnight belongs to the previous evening's shift.
            int starts = i == 0 ? 21 : 106;
            int mornings = ((starts << 1) & 127) | (starts >> 6);
            string desk = "staff-desk-" + d.npcId;
            p.At(0, bed, Sleep, 127 ^ mornings);
            p.At(0, desk, Work, mornings);
            p.At(150 + i * 12, "office-break-" + i, Rest, mornings);
            p.At(170 + i * 12, desk, Work, mornings);
            p.Street(395 + i * 8, 3 + i, mornings);
            p.At(432 + i * 7, bed, Sleep, mornings);
            p.At(548 + i * 11, bed, Wake, 127 ^ mornings);
            p.At(906 + i * 9, bed, Wake, mornings);
            p.Street(1005 + i * 12, 4 + i);
            p.At(1205 + i * 9, "cafe-table-" + i, Eat);
            p.At(1260 + i * 6, bed, Rest);
            p.Commute(1293 + i * 8, "office-front", i, starts);
            p.At(1321 + i * 6, desk, Work, starts);
            p.At(1370 + i * 4, bed, Sleep, 127 ^ starts);
        }

        static void ShopWorker(WorldNPCDefinition d, DayPlan p, int i, string bed)
        {
            d.workZone = "shop";
            p.At(0, bed, Sleep);
            p.At(new[] { 375, 448, 525 }[i], bed, Wake);
            if (i == 0)
            {
                p.Commute(393, "shop-front", 3, 127);
                p.At(415, "shop-staff-counter", Work);
                p.At(690, "cafe-table-0", Eat);
                p.At(720, "shop-browse-5", StockShelf);
                p.At(775, "shop-staff-counter", Work);
                p.Street(900, 3);
            }
            else if (i == 1)
            {
                p.Street(534, 1);
                p.At(620, "finance-consultation-point", Talk, Weekdays);
                p.Commute(671, "shop-front", 1, 127);
                p.At(690, "shop-staff-counter", Work);
                p.Street(775, 3);
                p.At(816, "cafe-table-3", Eat);
                p.Street(856, 5);
                p.At(900, "shop-staff-counter", Work);
                p.Street(1000, 1);
            }
            else
            {
                p.Street(612, 5);
                p.Visit(689, "alley", new Vector2(4.6f, -1.4f), Phone);
                p.At(815, "cafe-table-2", Eat);
                p.At(905, "realestate-consultation-point", Talk, Weekdays);
                p.Commute(962, "shop-front", 5, 127);
                p.At(1000, "shop-staff-counter", Work);
                p.Street(1380, 5);
            }

            if (p.Has("shop-backroom-seat"))
            {
                if (i == 0)
                {
                    p.At(744, "shop-backroom-seat", Rest);
                    p.At(756, "shop-browse-5", StockShelf);
                }
                else if (i == 1)
                {
                    p.At(786, "cafe-backroom-seat", Rest);
                    p.At(800, "shop-browse-4", StockShelf);
                }
                else
                {
                    p.At(980, "shop-browse-4", StockShelf);
                    p.At(987, "cafe-backroom-seat", Rest);
                }
            }
            p.At(i == 2 ? 1398 : 1080 + i * 15, bed, Rest);
            p.At(i == 2 ? 1430 : 1365 + i * 10, bed, Sleep);
        }

        static void Ambient(WorldNPCDefinition d, DayPlan p, int i)
        {
            float x = -8.5f + i * 2.3f;
            p.Visit(0, d.homeZone, Vector2.zero, Sleep);
            p.Visit(390 + i * 7, "alley", new Vector2(x, -1.5f), Walk);
            if (i < 4)
            {
                bool student = i < 2;
                p.Commute(465 + i * 4, student ? "school-front" : "office-front", student ? i + 3 : i + 1, Weekdays);
                p.Street(465 + i * 4, i, Weekend);
                d.jobId = student ? "student" : "office";
                d.workZone = student ? "school" : "workplace";
                string desk = student ? "school-seat-" + (i + 2) : "staff-desk-resident-" + (i == 2 ? 1 : 6);
                p.At((student ? 505 : 539) + i * 4, desk, student ? Study : Work, Weekdays);
                p.Street(717 + i * 6, i + 2, Weekdays);
                p.At(770 + i * 6, desk, student ? Study : Work, Weekdays);
                p.Street((student ? 937 : 1049) + i * 9, i, Weekdays);
                p.Visit(624 + i * 5, "alley", new Vector2(x, -.5f), Phone, Weekend);
            }
            else
            {
                p.Street(465 + i * 4, i);
                p.Visit(610 + i * 4, "alley", new Vector2(x, -.5f), Phone);
                p.Street(719 + i * 3, i + 2);
                if (i == 5)
                {
                    p.At(758, "shop-browse-1", Shop);
                    p.Street(776, i);
                }

                p.Visit(856 + i * 5, "alley", new Vector2(x, -1.5f), Walk);
                p.Street(1025 + i * 4, i);
            }

            p.Visit(1192 + i * 7, "alley", new Vector2(x, -1.2f), Walk);
            if (i == 4 || i == 6)
            {
                // Evening delivery pickup and neighbourhood walk use opposite sidewalks.
                p.Street(1235 + i * 3, i == 4 ? 6 : 3);
                p.Visit(1267 + i * 3, "alley", new Vector2(x, -1.2f), Walk);
            }

            p.Visit(1318 + i * 6, d.homeZone, Vector2.zero, Idle);
        }

        static void Apply(LifeWorldRules rules, IEnumerable<string> paths)
        {
            var places = new Dictionary<string, Place>();
            foreach (var path in paths)
            {
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var objects = scene
                        .GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<LifeWorldController>(true))
                        .SelectMany(world => world.objects);
                    foreach (var o in objects)
                    {
                        if (o == null || string.IsNullOrEmpty(o.id) || rules.Zone(o.zone) == null)
                        {
                            continue;
                        }

                        var slot = o.GetComponent<WorldActivitySlot>();
                        Vector2 point =
                            slot == null ? o.Point
                            : o.kind == WorldInteractionKind.Fridge ? slot.ApproachPoint
                            : slot.Point;
                        places[o.id] = new Place(o.zone, point - rules.Zone(o.zone).origin);
                    }
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }

            string[] workers =
            {
                "colleague",
                "resident-0",
                "resident-5",
                "resident-3",
                "resident-4",
                "resident-8",
                "resident-9",
            };
            foreach (var d in rules.npcs)
            {
                var p = new DayPlan(places);
                string id = d.npcId;
                if (FamilyLife.Parent(id))
                {
                    Family(d, p);
                }
                else if (d.ambient)
                {
                    Ambient(d, p, int.Parse(id.Substring("ambient-urban-".Length)));
                }
                else
                {
                    string bed = d.schedule.First(e => e.action == Sleep).objectId;
                    int i = Array.IndexOf(workers, id);
                    if (i >= 0)
                    {
                        DayWorker(d, p, i, bed);
                    }
                    else if (id is "resident-1" or "resident-6")
                    {
                        NightWorker(d, p, id == "resident-1" ? 0 : 1, bed);
                    }
                    else if (id is "shop-clerk" or "resident-2" or "resident-7")
                    {
                        ShopWorker(
                            d,
                            p,
                            id == "shop-clerk" ? 0
                                : id == "resident-2" ? 1
                                : 2,
                            bed
                        );
                    }
                    else if (d.workZone == "school")
                    {
                        School(
                            d,
                            p,
                            id == "classmate" ? 0
                                : id == "school-pupil" ? 1
                                : 2,
                            bed
                        );
                    }
                    else
                    {
                        continue;
                    }
                }

                d.schedule = FamilyLife.Parent(id)
                    ? p.Entries.ToArray()
                    : p.Entries.OrderBy(e => e.hour * 60 + e.minute).ToArray();
            }

            EditorUtility.SetDirty(rules);
            AssetDatabase.SaveAssetIfDirty(rules);
        }

        [MenuItem("Ian/Life World/Apply resident routines")]
        public static void Apply()
        {
            Apply(
                AssetDatabase.LoadAssetAtPath<LifeWorldRules>("Assets/_Game/Data/World/Shared/LifeWorldRules.asset"),
                new[] { "Assets/_Game/Scenes/LifeWorld.unity" }
            );
            ApplyRoomRules();
        }

        internal static void ApplyRoomRules()
        {
            Apply(
                AssetDatabase.LoadAssetAtPath<LifeWorldRules>(
                    "Assets/_Game/Data/SceneManagement/Rooms/RoomWorldRules.asset"
                ),
                Directory.GetFiles("Assets/_Game/Scenes/Rooms", "*.unity")
            );
        }
    }
}
