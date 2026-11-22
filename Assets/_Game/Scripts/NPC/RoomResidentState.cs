using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    public sealed partial class LifeWorldService
    {
        public bool RoomMode =>
            rooms != null && GameContext.Instance != null && GameContext.Instance.transitions.IsRoomArchitecture;

        public void UseLegacyRules()
        {
            if (legacyRules != null)
            {
                rules = legacyRules;
            }
        }

        public void UseRoomRules(RoomSceneCatalog catalog)
        {
            rooms = catalog;
            if (catalog?.rules != null)
            {
                rules = catalog.rules;
            }

            catalog?.Migrate(State, Game == null ? -1 : Game.growth.Age);
        }

        public void AdvanceAbsentResidents(bool materialize = false, bool timeJump = false)
        {
            if (!RoomMode || Game == null || rules == null)
            {
                return;
            }

            if (State.residents == null)
            {
                State.residents = new List<NPCResidentSnapshot>();
            }

            long now = (long)(Game.clock.Day - 1) * 1440 + (long)(Game.clock.SubDaySeconds / 60);
            var visible = Host?.GetComponent<WorldNPCScheduler>();
            foreach (var d in rules.npcs)
            {
                if (visible != null && visible.HasPhysicalActor(d.npcId))
                {
                    continue;
                }

                int index = WorldNPCScheduler.Schedule(d, Game.clock.Day, (int)(Game.clock.SubDaySeconds / 60));
                var target = d.schedule[index];
                var action = NPCActivityLocation.Allows(d, target) ? target.action : NPCDailyAction.Idle;
                var n = State.residents.FirstOrDefault(n => n.id == d.npcId);
                if (n == null)
                {
                    n = new NPCResidentSnapshot
                    {
                        id = d.npcId,
                        residenceId = d.homeZone,
                        householdId = rooms.Find(d.homeZone)?.homeId,
                        zone = target.zone,
                        destinationZone = target.zone,
                        position = target.position,
                        action = action,
                        objectId = target.objectId,
                        scheduleIndex = index,
                        minute = now,
                    };
                    State.residents.Add(n);
                    continue;
                }

                if (
                    !timeJump
                    && n.minute == now
                    && n.scheduleIndex == index
                    && n.objectId == target.objectId
                    && (n.action == NPCDailyAction.Walk || n.zone == target.zone && n.action == action)
                )
                {
                    continue;
                }

                var observed = Host?.Room?.roomId;
                n.passingRoom =
                    !materialize
                    && !d.ambient
                    && observed != null
                    && n.zone != observed
                    && n.zone != target.zone
                    && rooms.PassesThrough(n.zone, target.zone, observed)
                        ? observed
                        : null;
                n.destinationZone = target.zone;
                n.objectId = target.objectId;
                n.scheduleIndex = index;
                // A visitor entering the currently viewed room must appear through its door.
                // Other rooms are data-only; no GameObject is created there.
                if (!materialize && n.zone != observed && (target.zone == observed || n.passingRoom != null))
                {
                    n.action = NPCDailyAction.Walk;
                    n.minute = now;
                    continue;
                }

                n.zone = target.zone;
                n.position = target.position;
                n.action = action;
                n.minute = now;
                long key = ((long)Game.clock.Day << 16) + index;
                if (
                    action == target.action
                    && FamilyLife.Parent(d.npcId)
                    && !FamilyLife.Done(Game, d.npcId, target.objectId, key)
                )
                {
                    FamilyLife.ApplyLogicalUse(Game, d.npcId, target.objectId, key);
                    n.completedUseKey = key;
                }
            }
        }
    }
}
