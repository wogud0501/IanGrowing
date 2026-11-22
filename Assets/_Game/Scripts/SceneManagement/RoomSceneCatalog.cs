using System;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    [Serializable]
    public sealed class RoomSceneDefinition
    {
        public string roomId;
        public string sceneName;
        public string title;
        public string homeId;
        public string legacyZone;
        public SceneId sceneId;
        public Vector2 legacyOffset;
        public Rect legacyBounds;
        public bool family;
        public ResidenceFloorPlan floorPlan;
        public Sprite keepsake;
    }

    [Serializable]
    public sealed class RoomPortalDefinition
    {
        public string doorId;
        public string sourceRoom;
        public string destinationRoom;
        public string destinationEntryId;
        public Vector2 arrival;
        public Vector2 facing = Vector2.down;
    }

    [CreateAssetMenu(menuName = "Ian/Room scene catalog")]
    public sealed class RoomSceneCatalog : ScriptableObject
    {
        public LifeWorldRules rules;
        public RoomSceneDefinition[] rooms = Array.Empty<RoomSceneDefinition>();
        public RoomPortalDefinition[] portals = Array.Empty<RoomPortalDefinition>();

        public RoomSceneDefinition Find(string id) => rooms.FirstOrDefault(r => r.roomId == id);

        public RoomSceneDefinition Find(SceneId id) => rooms.FirstOrDefault(r => r.sceneId == id);

        public RoomSceneDefinition Resolve(string zone, Vector2 position, int age = -1)
        {
            var exact = Find(zone);
            if (exact != null)
            {
                return exact;
            }

            if (zone == FamilyLife.Home)
            {
                if (age == 0)
                {
                    return Find("family-ian-bedroom");
                }

                return rooms.FirstOrDefault(r => r.family && r.legacyBounds.Contains(position))
                    ?? Find("family-living");
            }

            return null;
        }

        public void Migrate(LifeWorldSnapshot state, int age)
        {
            var r = Resolve(state.zone, state.position, age);
            if (r != null && state.zone != r.roomId)
            {
                state.position -= r.legacyOffset;
                state.zone = r.roomId;
            }

            if (state.residents == null)
            {
                return;
            }

            foreach (var npc in state.residents)
            {
                var resident = rules?.npcs?.FirstOrDefault(n => n.npcId == npc.id);
                if (resident != null)
                {
                    if (string.IsNullOrEmpty(npc.residenceId) && npc.zone == "residence")
                    {
                        npc.zone = resident.homeZone;
                        var bed = resident.schedule.FirstOrDefault(e => e.zone == resident.homeZone);
                        npc.position = bed?.position ?? Vector2.zero;
                        npc.minute = -1;
                    }

                    if (npc.destinationZone == "residence")
                    {
                        npc.destinationZone = resident.homeZone;
                    }

                    npc.residenceId = resident.homeZone;
                    npc.householdId = Find(resident.homeZone)?.homeId;
                }

                r = Resolve(npc.zone, npc.position);
                if (r != null && npc.zone != r.roomId)
                {
                    npc.position -= r.legacyOffset;
                    npc.zone = r.roomId;
                }
            }
        }

        public void ToLegacy(LifeWorldSnapshot state)
        {
            var r = Find(state.zone);
            if (r != null)
            {
                state.position += r.legacyOffset;
                state.zone = r.legacyZone;
            }

            if (state.residents == null)
            {
                return;
            }

            foreach (var n in state.residents)
            {
                r = Find(n.zone);
                if (r != null)
                {
                    n.position += r.legacyOffset;
                    n.zone = r.legacyZone;
                }
            }
        }

        public string FirstDoor(string from, string destination)
        {
            return FirstEdge(from, destination)?.doorId;
        }

        RoomPortalDefinition FirstEdge(string from, string destination)
        {
            var q = new System.Collections.Generic.Queue<string>();
            var previous = new System.Collections.Generic.Dictionary<string, RoomPortalDefinition>();
            q.Enqueue(from);
            previous[from] = null;
            while (q.Count > 0)
            {
                var at = q.Dequeue();
                foreach (var p in portals.Where(p => p.sourceRoom == at))
                {
                    if (previous.ContainsKey(p.destinationRoom))
                    {
                        continue;
                    }

                    previous[p.destinationRoom] = p;
                    if (p.destinationRoom == destination)
                    {
                        var edge = p;
                        while (edge.sourceRoom != from)
                        {
                            edge = previous[edge.sourceRoom];
                        }

                        return edge;
                    }

                    q.Enqueue(p.destinationRoom);
                }
            }

            return null;
        }

        public bool PassesThrough(string source, string destination, string observed)
        {
            for (int step = 0; step < rooms.Length && source != destination; step++)
            {
                var edge = FirstEdge(source, destination);
                if (edge == null)
                {
                    return false;
                }

                source = edge.destinationRoom;
                if (source == observed)
                {
                    return true;
                }
            }

            return false;
        }

        public RoomPortalDefinition EntryFrom(string room, string prior)
        {
            return FirstEdge(room, prior)
                ?? portals.FirstOrDefault(p => p.sourceRoom == room);
        }
    }
}
