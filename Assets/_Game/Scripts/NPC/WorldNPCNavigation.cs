using System.Collections.Generic;
using UnityEngine;

namespace IanGame
{
    // Static occupancy is cached per zone. Dynamic people/traffic are handled by the
    // scheduler's local avoidance, so distant actors do not run physics/path queries.
    public sealed class WorldNPCNavigation
    {
        sealed class Grid
        {
            public Vector2 origin;
            public int width;
            public int height;
            public bool[] blocked;
            public byte[] testedEdges;
            public byte[] closedEdges;

            public Vector2 Point(int i) => origin + new Vector2(i % width, i / width) * .5f;
        }

        readonly Dictionary<string, Grid> cache = new Dictionary<string, Grid>();
        int furnishings = -1;
        public int Bakes { get; private set; }

        public void Clear()
        {
            cache.Clear();
        }

        const float Radius = .22f;
        const float CrossingLaneOffset = .42f;
        static readonly ContactFilter2D solids = new ContactFilter2D { useTriggers = false };
        static readonly List<Collider2D> overlaps = new List<Collider2D>(16);
        static readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(16);

        static bool Blocks(Collider2D collider, Transform furniture, bool planning)
        {
            if (furniture != null && collider.transform.IsChildOf(furniture))
            {
                return false;
            }

            if (planning)
            {
                if (collider.attachedRigidbody != null && collider.attachedRigidbody.bodyType != RigidbodyType2D.Static)
                {
                    return false;
                }

                var door = collider.GetComponentInParent<WorldDoor>();
                if (door != null && door.closedCollider == collider)
                {
                    return false;
                }
            }

            return true;
        }

        public static Collider2D Occupant(Vector2 point, Transform furniture = null, bool planning = false)
        {
            Physics2D.OverlapCircle(point, Radius, solids, overlaps);
            foreach (var collider in overlaps)
            {
                if (Blocks(collider, furniture, planning))
                {
                    return collider;
                }
            }

            return null;
        }

        public static Collider2D Obstacle(Vector2 from, Vector2 to, Transform furniture = null, bool planning = false)
        {
            var delta = to - from;
            if (delta.sqrMagnitude < .0000001f)
            {
                return Occupant(to, furniture, planning);
            }

            Physics2D.CircleCast(from, Radius, delta.normalized, solids, hits, delta.magnitude);
            foreach (var hit in hits)
            {
                if (Blocks(hit.collider, furniture, planning))
                {
                    return hit.collider;
                }
            }

            return planning ? null : Occupant(to, furniture);
        }

        public static bool StaticFree(Vector2 point)
        {
            return Occupant(point, planning: true) == null;
        }

        Grid Get(LifeWorldController world, WorldZoneDefinition zone)
        {
            int count = world.FurnitureRevision;
            if (count != furnishings)
            {
                furnishings = count;
                cache.Clear();
            }

            if (cache.TryGetValue(zone.id, out var found))
            {
                return found;
            }

            float top = Mathf.Min(zone.size.y / 2 - .4f, zone.walkableTop - .4f),
                bottom = -zone.size.y / 2 + .4f;
            var grid = new Grid
            {
                origin = zone.origin + new Vector2(-zone.size.x / 2 + .4f, bottom),
                width = Mathf.FloorToInt((zone.size.x - .8f) / .5f) + 1,
                height = Mathf.FloorToInt((top - bottom) / .5f) + 1,
            };
            grid.blocked = new bool[grid.width * grid.height];
            grid.testedEdges = new byte[grid.blocked.Length];
            grid.closedEdges = new byte[grid.blocked.Length];
            for (int i = 0; i < grid.blocked.Length; i++)
            {
                var p = grid.Point(i);
                var local = p - zone.origin;
                grid.blocked[i] =
                    !StaticFree(p)
                    || (zone.id == "city" && local.y > -6.7f && local.y < -2.1f && Mathf.Abs(local.x) > .65f);
            }

            cache[zone.id] = grid;
            Bakes++;
            return grid;
        }

        static int Nearest(
            Grid g,
            Vector2 p,
            System.Func<Vector2, bool> available,
            System.Func<Vector2, Vector2, bool> clearSegment,
            bool enteringGrid
        )
        {
            int cx = Mathf.Clamp(Mathf.RoundToInt((p.x - g.origin.x) / .5f), 0, g.width - 1),
                cy = Mathf.Clamp(Mathf.RoundToInt((p.y - g.origin.y) / .5f), 0, g.height - 1);
            int best = -1;
            float distance = float.PositiveInfinity;
            for (int y = Mathf.Max(0, cy - 3); y <= Mathf.Min(g.height - 1, cy + 3); y++)
            {
                for (int x = Mathf.Max(0, cx - 3); x <= Mathf.Min(g.width - 1, cx + 3); x++)
                {
                    int i = y * g.width + x;
                    float d = (g.Point(i) - p).sqrMagnitude;
                    if (
                        !g.blocked[i]
                        && d < distance
                        && (available == null || available(g.Point(i)))
                        && (enteringGrid
                            ? CrowdSegmentFree(p, g.Point(i), available, clearSegment)
                            : CrowdSegmentFree(g.Point(i), p, available, clearSegment))
                        && Obstacle(p, g.Point(i), planning: true) == null
                    )
                    {
                        distance = d;
                        best = i;
                    }
                }
            }

            return best;
        }

        public List<Vector2> Find(
            LifeWorldController world,
            string zoneId,
            Vector2 start,
            Vector2 goal,
            System.Func<Vector2, bool> available = null,
            System.Func<Vector2, Vector2, bool> clearSegment = null
        )
        {
            var zone = world.Service.rules.Zone(zoneId);
            if (zoneId == "city")
            {
                const float south = -6.7f;
                const float north = -2.1f;
                float fromY = start.y - zone.origin.y;
                float toY = goal.y - zone.origin.y;
                bool northbound = fromY <= south && toY >= north;
                if (northbound || fromY >= north && toY <= south)
                {
                    // Keep both curb points even when the general path can be simplified to one line.
                    float x = northbound ? CrossingLaneOffset : -CrossingLaneOffset;
                    var entry = zone.origin + new Vector2(x, northbound ? south - .15f : north + .15f);
                    var exit = zone.origin + new Vector2(x, northbound ? north + .15f : south - .15f);
                    if (
                        Obstacle(entry, exit, planning: true) == null
                        && CrowdSegmentFree(entry, exit, available, clearSegment)
                    )
                    {
                        var approach = FindPath(world, zone, start, entry, available, clearSegment);
                        var departure = FindPath(world, zone, exit, goal, available, clearSegment);
                        if (approach.Count > 0 && departure.Count > 0)
                        {
                            approach.Add(exit);
                            approach.AddRange(departure);
                            return approach;
                        }
                    }
                }
            }

            return FindPath(world, zone, start, goal, available, clearSegment);
        }

        List<Vector2> FindPath(
            LifeWorldController world,
            WorldZoneDefinition zone,
            Vector2 start,
            Vector2 goal,
            System.Func<Vector2, bool> available,
            System.Func<Vector2, Vector2, bool> clearSegment
        )
        {
            var g = Get(world, zone);
            int first = Nearest(g, start, available, clearSegment, true),
                last = Nearest(g, goal, available, clearSegment, false);
            if (first < 0 || last < 0)
            {
                return new List<Vector2>();
            }

            var parent = new int[g.blocked.Length];
            System.Array.Fill(parent, -1);
            var queue = new Queue<int>();
            queue.Enqueue(first);
            parent[first] = first;
            int[] dx = { 0, 1, 0, -1, 1, 1, -1, -1 },
                dy = { 1, 0, -1, 0, 1, -1, 1, -1 };
            while (queue.Count > 0 && parent[last] < 0)
            {
                int current = queue.Dequeue(),
                    x = current % g.width,
                    y = current / g.width;
                for (int s = 0; s < 8; s++)
                {
                    int nx = x + dx[s],
                        ny = y + dy[s];
                    if (nx < 0 || ny < 0 || nx >= g.width || ny >= g.height)
                    {
                        continue;
                    }

                    int next = ny * g.width + nx;
                    if (parent[next] >= 0 || g.blocked[next] || available != null && !available(g.Point(next)))
                    {
                        continue;
                    }

                    if (s >= 4 && (g.blocked[y * g.width + nx] || g.blocked[ny * g.width + x]))
                    {
                        continue;
                    }

                    byte edge = (byte)(1 << s);
                    if ((g.testedEdges[current] & edge) == 0)
                    {
                        g.testedEdges[current] |= edge;
                        if (Obstacle(g.Point(current), g.Point(next), planning: true) != null)
                        {
                            g.closedEdges[current] |= edge;
                        }
                    }

                    if (
                        (g.closedEdges[current] & edge) != 0
                        || !CrowdSegmentFree(g.Point(current), g.Point(next), available, clearSegment)
                    )
                    {
                        continue;
                    }

                    parent[next] = current;
                    queue.Enqueue(next);
                }
            }

            var result = new List<Vector2>();
            if (parent[last] < 0)
            {
                return result;
            }

            for (int i = last; i != first; i = parent[i])
            {
                result.Add(g.Point(i));
            }

            result.Add(g.Point(first));
            result.Reverse();
            // Grid rounding can put the final node beyond a door's interaction point,
            // inside a fixture. Join only physically reachable endpoints. Door leaves
            // are planned as portals and checked in their live state on every footstep.
            while (result.Count > 0)
            {
                var previous = result.Count > 1 ? result[result.Count - 2] : start;
                if (
                    !OnPedestrianRoute(zone, previous, goal)
                    || Obstacle(previous, goal, planning: true) != null
                    || !CrowdSegmentFree(previous, goal, available, clearSegment)
                )
                {
                    break;
                }

                result.RemoveAt(result.Count - 1);
            }

            var tail = result.Count > 0 ? result[result.Count - 1] : start;
            if (
                !OnPedestrianRoute(zone, tail, goal)
                || Obstacle(tail, goal, planning: true) != null
                || !CrowdSegmentFree(tail, goal, available, clearSegment)
            )
            {
                return new List<Vector2>();
            }

            result.Add(goal);
            // Fixed opposing pedestrian lanes keep the shared crossing from becoming a
            // head-on single-file bottleneck. This changes waypoints, never actor poses.
            if (zone.id == "city" && Mathf.Abs(goal.y - start.y) > .2f)
            {
                float lane = goal.y > start.y ? CrossingLaneOffset : -CrossingLaneOffset;
                for (int i = 0; i < result.Count - 1; i++)
                {
                    var p = result[i];
                    var local = p - zone.origin;
                    if (local.y > -6.7f && local.y < -2.1f && Mathf.Abs(local.x) <= .65f)
                    {
                        var lanePoint = new Vector2(zone.origin.x + lane, p.y);
                        var previous = i == 0 ? start : result[i - 1];
                        var next = result[i + 1];
                        if (OnPedestrianRoute(zone, previous, lanePoint)
                            && OnPedestrianRoute(zone, lanePoint, next)
                            && Obstacle(previous, lanePoint, planning: true) == null
                            && Obstacle(lanePoint, next, planning: true) == null
                            && (available == null || available(lanePoint))
                            && CrowdSegmentFree(previous, lanePoint, available, clearSegment)
                            && CrowdSegmentFree(lanePoint, next, available, clearSegment))
                        {
                            result[i] = lanePoint;
                        }
                    }
                }
            }

            return result;
        }

        static bool CrowdSegmentFree(
            Vector2 from,
            Vector2 to,
            System.Func<Vector2, bool> available,
            System.Func<Vector2, Vector2, bool> clearSegment
        )
        {
            if (clearSegment != null)
            {
                return clearSegment(from, to);
            }
            if (available == null)
            {
                return true;
            }

            int steps = Mathf.CeilToInt(Vector2.Distance(from, to) / .25f);
            for (int i = 1; i <= steps; i++)
            {
                if (!available(Vector2.Lerp(from, to, (float)i / steps)))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool OnPedestrianRoute(WorldZoneDefinition zone, Vector2 from, Vector2 to)
        {
            if (zone.id != "city")
            {
                return true;
            }

            from -= zone.origin;
            to -= zone.origin;
            const float south = -6.7f;
            const float north = -2.1f;
            if (Mathf.Max(from.y, to.y) <= south || Mathf.Min(from.y, to.y) >= north)
            {
                return true;
            }

            float enter = Mathf.InverseLerp(from.y, to.y, south);
            float exit = Mathf.InverseLerp(from.y, to.y, north);
            if (Mathf.Approximately(from.y, to.y))
            {
                exit = 1;
            }

            return Mathf.Abs(Mathf.Lerp(from.x, to.x, enter)) <= .65f
                && Mathf.Abs(Mathf.Lerp(from.x, to.x, exit)) <= .65f;
        }
    }
}
