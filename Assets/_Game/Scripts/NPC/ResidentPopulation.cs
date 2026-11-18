using System.Collections.Generic;
using UnityEngine;

namespace IanGame
{
    public sealed partial class WorldNPCScheduler
    {
        public WorldLifeProfile lifeProfile;
        readonly Dictionary<string, WorldInteractable> ambientPool = new Dictionary<string, WorldInteractable>();
        readonly HashSet<string> admittedAmbient = new HashSet<string>();
        long populationMinute = -1;
        string populationZone;

        public int PooledAmbientCount => ambientPool.Count;
        public int ActiveAmbientCount
        {
            get
            {
                int count = 0;
                foreach (var resident in residents)
                {
                    if (resident.definition.ambient && resident.active)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int AmbientCapacity =>
            lifeProfile == null ? 0 : lifeProfile.AmbientLimit(world.Room.roomId, world.Service.Hour);

        void RefreshPopulationBudget()
        {
            if (lifeProfile == null || world.Room == null)
            {
                return;
            }

            int hourKey = world.Service.Game.clock.Day * 24 + world.Service.Hour;
            string zone = world.Room.roomId;
            if (Minute == populationMinute && populationZone == zone)
            {
                return;
            }

            populationMinute = Minute;
            populationZone = zone;
            admittedAmbient.Clear();
            var definitions = world.Service.rules.npcs;
            if (definitions.Length == 0)
            {
                return;
            }

            int capacity = AmbientCapacity;
            int offset = hourKey % definitions.Length;
            for (int i = 0; i < definitions.Length && admittedAmbient.Count < capacity; i++)
            {
                var definition = definitions[(i + offset) % definitions.Length];
                if (!definition.ambient || definition.schedule == null || definition.schedule.Length == 0)
                {
                    continue;
                }

                int index = Schedule(
                    definition,
                    world.Service.Game.clock.Day,
                    (int)(world.Service.Game.clock.SubDaySeconds / 60)
                );
                if (definition.schedule[index].zone == zone)
                {
                    admittedAmbient.Add(definition.npcId);
                }
            }
        }

        public bool GreetAmbient(string id, Vector2 player)
        {
            foreach (var resident in residents)
            {
                if (resident.definition.npcId != id || !resident.definition.ambient || !resident.active)
                {
                    continue;
                }

                if (
                    resident.portal != null
                    || resident.door != null
                    || Vector2.Distance(resident.actor.transform.position, player) > resident.actor.radius + .3f
                )
                {
                    return false;
                }

                resident.ambientConversationUntil = Time.unscaledTime + 1.5f;
                resident.facing = (player - (Vector2)resident.actor.transform.position).normalized;
                return true;
            }

            return false;
        }

        void TickAmbientPatrol(Resident resident)
        {
            if (
                !resident.definition.ambient
                || resident.definition.ambientOffsets.Length == 0
                || resident.target == null
                || resident.target.action != NPCDailyAction.Walk
                || resident.actor.zone != resident.target.zone
                || !resident.atActivity
            )
            {
                return;
            }

            if (resident.ambientPauseUntil <= 0)
            {
                resident.ambientPauseUntil = Time.unscaledTime + 1.4f + resident.ambientWaypoint % 3 * .35f;
                return;
            }

            if (Time.unscaledTime < resident.ambientPauseUntil)
            {
                return;
            }

            resident.ambientWaypoint = (resident.ambientWaypoint + 1) % resident.definition.ambientOffsets.Length;
            resident.ambientPauseUntil = 0;
            resident.hasPersonalGoal = false;
            resident.atActivity = false;
            resident.path.Clear();
            resident.waypoint = 0;
        }

        bool AdmitResident(WorldNPCDefinition definition)
        {
            return !definition.ambient || lifeProfile == null || admittedAmbient.Contains(definition.npcId);
        }

        bool OutsidePopulationView(Vector2 position)
        {
            var camera = world.follow == null ? null : world.follow.target;
            if (camera == null)
            {
                return false;
            }

            var point = camera.WorldToViewportPoint(position);
            return point.x < -0.12f || point.x > 1.12f || point.y < -0.12f || point.y > 1.12f;
        }

        WorldInteractable AcquireScopedActor(WorldInteractable template, WorldNPCDefinition definition)
        {
            if (definition.ambient && ambientPool.TryGetValue(definition.npcId, out var pooled))
            {
                ambientPool.Remove(definition.npcId);
                if (pooled != null)
                {
                    pooled.gameObject.SetActive(true);
                    return pooled;
                }
            }

            return Instantiate(template, world.transform);
        }

        void ReleaseScopedActor(Resident resident)
        {
            if (resident.definition.ambient && resident.actor != null)
            {
                resident.actor.visual.color = resident.baseColor;
                resident.actor.gameObject.SetActive(false);
                ambientPool[resident.definition.npcId] = resident.actor;
                return;
            }

            if (resident.actor != null)
            {
                Destroy(resident.actor.gameObject);
            }
        }
    }
}
