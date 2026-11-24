using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IanGame.Editor
{
    public static class ResidentScheduleValidation
    {
        [MenuItem("Ian/Verification/Validate resident week")]
        public static void Validate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RoomSceneCatalog>(
                "Assets/_Game/Data/SceneManagement/Rooms/RoomScenes.asset"
            );
            var rules = catalog.rules;
            var exclusive = new HashSet<(string zone, string slot)>();
            var valid = new HashSet<(string npc, string zone, NPCDailyAction action, string slot)>();
            var problems = new HashSet<string>();
            foreach (var room in catalog.rooms)
            {
                string path = $"Assets/_Game/Scenes/Rooms/{room.sceneName}.unity";
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    foreach (
                        var world in scene
                            .GetRootGameObjects()
                            .SelectMany(o => o.GetComponentsInChildren<LifeWorldController>(true))
                    )
                    {
                        if (world.TryGetComponent<ResidentApartment>(out var apartment))
                        {
                            if (!room.floorPlan || string.IsNullOrEmpty(room.homeId))
                            {
                                problems.Add(room.roomId + " missing floor plan or household");
                                continue;
                            }

                            apartment.Furnish(world, room);
                            ValidateApartment(world, room, problems);
                        }

                        var beds = world
                            .objects.Where(o => o != null && o.kind == WorldInteractionKind.Sleep)
                            .ToArray();
                        for (int i = 0; i < beds.Length; i++)
                        {
                            foreach (var other in beds.Skip(i + 1))
                            {
                                if (
                                    Vector2.Distance(beds[i].transform.position, other.transform.position)
                                    < NPCCrowdSpace.Clearance
                                )
                                {
                                    problems.Add($"{world.Room.roomId} overlapping beds {beds[i].id} and {other.id}");
                                }
                            }
                        }

                        foreach (var npc in rules.npcs)
                        {
                            foreach (
                                var e in npc.schedule.Where(e =>
                                    e.zone == world.Room.roomId && !string.IsNullOrEmpty(e.objectId)
                                )
                            )
                            {
                                var anchor = world.objects.FirstOrDefault(o =>
                                    o != null && o.id == e.objectId && o.zone == e.zone
                                );
                                if (anchor == null || !NPCActivityLocation.Matches(e.action, anchor))
                                {
                                    continue;
                                }

                                var slot = anchor.GetComponent<WorldActivitySlot>();
                                if (slot != null)
                                {
                                    exclusive.Add((e.zone, e.objectId));
                                    if (!slot.Allows(npc.npcId))
                                    {
                                        continue;
                                    }
                                }

                                valid.Add((npc.npcId, e.zone, e.action, e.objectId));
                            }
                        }
                    }
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }

            var boundaries = new SortedSet<int> { 0 };
            foreach (var npc in rules.npcs)
            {
                if (rules.Zone(npc.homeZone) == null || npc.schedule.Length == 0)
                {
                    problems.Add(npc.npcId + " missing home or schedule");
                    continue;
                }

                var entries = new HashSet<(int, int)>();
                foreach (var e in npc.schedule)
                {
                    int time = e.hour * 60 + e.minute;
                    boundaries.Add(time);
                    if (
                        e.hour < 0
                        || e.hour > 23
                        || e.minute < 0
                        || e.minute > 59
                        || e.dayMask < 1
                        || e.dayMask > 127
                        || rules.Zone(e.zone) == null
                        || !NPCActivityLocation.Allows(npc, e)
                        || !string.IsNullOrEmpty(e.objectId) && !valid.Contains((npc.npcId, e.zone, e.action, e.objectId))
                    )
                    {
                        problems.Add(
                            $"{npc.npcId} {e.hour:00}:{e.minute:00} invalid {e.action} at {e.zone}/{e.objectId}"
                        );
                    }

                    for (int day = 0; day < 7; day++)
                    {
                        if ((e.dayMask & (1 << day)) != 0 && !entries.Add((day, time)))
                        {
                            problems.Add($"{npc.npcId} D{day + 1} duplicate start at {time}");
                        }
                    }
                }
            }

            // A reservation can only change at a schedule boundary.
            for (int day = 1; day <= 7; day++)
            {
                foreach (int time in boundaries)
                {
                    var occupants = new Dictionary<(string zone, string slot), string>();
                    foreach (var npc in rules.npcs.Where(n => n.schedule.Length > 0))
                    {
                        var e = npc.schedule[WorldNPCScheduler.Schedule(npc, day, time)];
                        var slot = (e.zone, e.objectId);
                        if (exclusive.Contains(slot) && !occupants.TryAdd(slot, npc.npcId))
                        {
                            problems.Add(
                                $"D{day} {time / 60:00}:{time % 60:00} {occupants[slot]} and {npc.npcId} need {e.objectId}"
                            );
                        }
                    }
                }
            }

            if (problems.Count > 0)
            {
                throw new InvalidDataException(string.Join("\n", problems));
            }

            Debug.Log($"Resident schedules validated for {rules.npcs.Length} residents across seven game days.");
        }

        static void ValidateApartment(LifeWorldController world, RoomSceneDefinition room, HashSet<string> problems)
        {
            var members = world.Room.rules.npcs.Where(n => !n.ambient && n.homeZone == room.roomId).ToArray();
            var beds = world.objects.Where(o => o.kind == WorldInteractionKind.Sleep).ToArray();
            var origin = world.Room.rules.Zone(room.roomId).origin;
            if (beds.Length != members.Length || !room.floorPlan.doorways.Any())
            {
                problems.Add(room.roomId + " bedroom capacity or doorway missing");
            }

            foreach (var member in members)
            {
                var bed = beds.SingleOrDefault(b => b.id == member.residentBedId);
                var bedding = bed ? bed.GetComponent<BedQuilt>() : null;
                if (!bedding || !bedding.approachAnchor || !bedding.sitAnchor || !bedding.pillowAnchor
                    || !bedding.exitAnchor || bed.GetComponent<WorldActivitySlot>()?.staffId != member.npcId)
                {
                    problems.Add(room.roomId + " invalid personal berth for " + member.npcId);
                    continue;
                }

                foreach (var entry in member.schedule.Where(e => e.action == NPCDailyAction.Sleep))
                {
                    if (entry.zone != room.roomId || entry.objectId != bed.id
                        || Vector2.Distance(origin + entry.position, bed.Point) > .025f)
                    {
                        problems.Add(member.npcId + " offscreen bed differs from physical bedside");
                    }
                }
            }
        }
    }
}
