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
            var rules = AssetDatabase.LoadAssetAtPath<LifeWorldRules>(
                "Assets/_Game/Data/SceneManagement/Rooms/RoomWorldRules.asset"
            );
            var exclusive = new HashSet<string>();
            var valid = new HashSet<(string, NPCDailyAction, string)>();
            var problems = new HashSet<string>();
            foreach (string path in Directory.GetFiles("Assets/_Game/Scenes/Rooms", "*.unity"))
            {
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    foreach (
                        var world in scene
                            .GetRootGameObjects()
                            .SelectMany(o => o.GetComponentsInChildren<LifeWorldController>(true))
                    )
                    {
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
                                    exclusive.Add(e.objectId);
                                    if (!slot.Allows(npc.npcId))
                                    {
                                        continue;
                                    }
                                }

                                valid.Add((npc.npcId, e.action, e.objectId));
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
                        || !string.IsNullOrEmpty(e.objectId) && !valid.Contains((npc.npcId, e.action, e.objectId))
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
                    var occupants = new Dictionary<string, string>();
                    foreach (var npc in rules.npcs.Where(n => n.schedule.Length > 0))
                    {
                        var e = npc.schedule[WorldNPCScheduler.Schedule(npc, day, time)];
                        if (exclusive.Contains(e.objectId) && !occupants.TryAdd(e.objectId, npc.npcId))
                        {
                            problems.Add(
                                $"D{day} {time / 60:00}:{time % 60:00} {occupants[e.objectId]} and {npc.npcId} need {e.objectId}"
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
    }
}
