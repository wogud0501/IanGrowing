using System.Collections.Generic;
using UnityEngine;

namespace IanGame
{
    // Reuses resident entries and cell buckets after warmup. No all-agent movement scan.
    public sealed class NPCCrowdSpace
    {
        public const float Clearance = .78f;

        public sealed class Entry
        {
            public string id;
            public Vector2 position;
            public Vector2Int cell;
            internal uint generation;
        }

        sealed class Bucket
        {
            public readonly List<Entry> entries = new List<Entry>();
            public uint generation;
        }

        readonly Dictionary<Vector2Int, Bucket> cells = new Dictionary<Vector2Int, Bucket>();
        readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        uint generation = 1;

        Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x / .8f), Mathf.FloorToInt(p.y / .8f));

        public void Clear()
        {
            generation++;
            if (generation == 0)
            {
                cells.Clear();
                entries.Clear();
                generation = 1;
            }
        }

        Bucket CurrentBucket(Vector2Int key)
        {
            if (!cells.TryGetValue(key, out var bucket))
            {
                bucket = new Bucket();
                cells[key] = bucket;
            }

            if (bucket.generation != generation)
            {
                bucket.entries.Clear();
                bucket.generation = generation;
            }

            return bucket;
        }

        public void Set(string id, Vector2 p)
        {
            var key = Cell(p);
            if (!entries.TryGetValue(id, out var entry))
            {
                entry = new Entry { id = id };
                entries[id] = entry;
            }

            bool current = entry.generation == generation;
            if (current && entry.cell != key)
            {
                cells[entry.cell].entries.Remove(entry);
            }

            if (!current || entry.cell != key)
            {
                CurrentBucket(key).entries.Add(entry);
            }

            entry.cell = key;
            entry.position = p;
            entry.generation = generation;
        }

        public void Remove(string id)
        {
            if (!entries.TryGetValue(id, out var entry) || entry.generation != generation)
            {
                return;
            }

            cells[entry.cell].entries.Remove(entry);
            entry.generation = 0;
        }

        public IEnumerable<Entry> Nearby(Vector2 p)
        {
            var cell = Cell(p);
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (
                        cells.TryGetValue(cell + new Vector2Int(x, y), out var bucket)
                        && bucket.generation == generation
                    )
                    {
                        foreach (var entry in bucket.entries)
                        {
                            yield return entry;
                        }
                    }
                }
            }
        }

        bool Blocked(string id, Vector2 from, Vector2 next, float radius, bool inwardOnly)
        {
            var cell = Cell(next);
            // Query radius can exceed one cell when used by a future larger archetype.
            var delta = next - from;
            float lengthSquared = delta.sqrMagnitude;
            if (inwardOnly && lengthSquared == 0)
            {
                return false;
            }

            int reach = Mathf.Max(1, Mathf.CeilToInt((radius + delta.magnitude) / .8f));
            for (int y = -reach; y <= reach; y++)
            {
                for (int x = -reach; x <= reach; x++)
                {
                    if (
                        !cells.TryGetValue(cell + new Vector2Int(x, y), out var bucket)
                        || bucket.generation != generation
                    )
                    {
                        continue;
                    }

                    foreach (var entry in bucket.entries)
                    {
                        if (entry.id == id)
                        {
                            continue;
                        }

                        var offset = entry.position - from;
                        float fraction = inwardOnly && lengthSquared > 0
                            ? Mathf.Clamp01(Vector2.Dot(offset, delta) / lengthSquared)
                            : 1f;
                        if (fraction > 0 && (offset - fraction * delta).sqrMagnitude < radius * radius)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool Free(string id, Vector2 p, float radius = Clearance)
        {
            return !Blocked(id, p, p, radius, false);
        }

        public bool Blocks(string id, Vector2 from, Vector2 next)
        {
            return Blocks(id, from, next, Clearance);
        }

        internal bool Blocks(string id, Vector2 from, Vector2 next, float radius)
        {
            // Limit the spatial hash query to nearby cells, even when testing a whole path edge.
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, next) / .5f));
            Vector2 previous = from;
            for (int i = 1; i <= steps; i++)
            {
                var point = Vector2.Lerp(from, next, (float)i / steps);
                if (Blocked(id, previous, point, radius, true))
                {
                    return true;
                }
                previous = point;
            }
            return false;
        }
    }
}
