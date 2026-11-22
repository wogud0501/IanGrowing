using System;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    public sealed partial class WorldNPCScheduler
    {
        readonly NPCCrowdSpace crowd = new NPCCrowdSpace();

        public Vector2 PlayerStep(Vector2 from, Vector2 to)
        {
            const float contact = .59f;
            if (!crowd.Blocks("@player", from, to, contact))
            {
                return to;
            }

            var horizontal = new Vector2(to.x, from.y);
            var vertical = new Vector2(from.x, to.y);
            if (horizontal != from && !crowd.Blocks("@player", from, horizontal, contact))
            {
                return horizontal;
            }

            return crowd.Blocks("@player", from, vertical, contact) ? from : vertical;
        }

        bool SidewalkOrCrossing(Vector2 p)
        {
            var local = p - world.CurrentZone.origin;
            return world.CurrentZone.id != "city" || local.y <= -6.7f || local.y >= -2.1f || Mathf.Abs(local.x) <= .65f;
        }

        void FillCrowd()
        {
            crowd.Clear();
            crowd.Set("@player", world.Player.Position);
            foreach (var r in residents)
            {
                if (r.active && r.actor != null)
                {
                    crowd.Set(r.definition.npcId, r.actor.transform.position);
                }
            }
        }

        bool SpawnPoint(ref Vector2 point, WorldInteractable self = null, Transform furniture = null)
        {
            var requested = point;
            for (int ring = 0; ring < 7; ring++)
            {
                int count = ring == 0 ? 1 : 8 * ring;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * Mathf.PI * 2 / count;
                    var p =
                        requested
                        + (
                            ring == 0
                                ? Vector2.zero
                                : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))
                                    * (ring * (NPCCrowdSpace.Clearance + .1f))
                        );
                    var zone = world.CurrentZone;
                    var local = p - zone.origin;
                    if (
                        !SidewalkOrCrossing(p)
                        || Mathf.Abs(local.x) > zone.size.x / 2 - .4f
                        || local.y < -zone.size.y / 2 + .4f
                        || local.y > Mathf.Min(zone.size.y / 2 - .4f, zone.walkableTop - .4f)
                        || WorldNPCNavigation.Occupant(p, ring == 0 ? furniture : null) != null
                        || Vector2.Distance(p, world.Player.Position) < .6f
                    )
                    {
                        continue;
                    }

                    if (
                        residents.Any(r =>
                            r.actor != null
                            && r.actor != self
                            && r.actor.zone == world.CurrentZone.id
                            && Vector2.Distance(r.actor.transform.position, p) < NPCCrowdSpace.Clearance
                        )
                    )
                    {
                        continue;
                    }

                    point = p;
                    return true;
                }
            }

            return false;
        }

        Vector2 PersonalGoal(Resident r, Vector2 origin)
        {
            if (r.hasPersonalGoal)
            {
                return r.personalGoal;
            }

            for (int ring = 0; ring < 6; ring++)
            {
                int count = ring == 0 ? 1 : 8 * ring;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * Mathf.PI * 2 / count;
                    var p =
                        origin
                        + (
                            ring == 0
                                ? Vector2.zero
                                : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))
                                    * ring
                                    * (NPCCrowdSpace.Clearance + .1f)
                        );
                    if (
                        !SidewalkOrCrossing(p)
                        || !WorldNPCNavigation.StaticFree(p)
                        || Vector2.Distance(world.Safe(world.CurrentZone, p - world.CurrentZone.origin), p) > .02f
                    )
                    {
                        continue;
                    }

                    if (
                        residents.Any(other =>
                            other != r
                            && other.hasPersonalGoal
                            && Vector2.Distance(other.personalGoal, p) < NPCCrowdSpace.Clearance + .05f
                        )
                    )
                    {
                        continue;
                    }

                    r.personalGoal = p;
                    r.hasPersonalGoal = true;
                    return p;
                }
            }

            return origin;
        }

        bool WaitForSpace(Resident r, Vector2 from, Vector2 target, Vector2 next)
        {
            if (!crowd.Blocks(r.definition.npcId, from, next))
            {
                return false;
            }

            float now = Time.unscaledTime;
            if (r.blockedSince <= 0)
            {
                r.blockedSince = now;
            }

            r.waitUntil = now + .35f;
            if (now - r.blockedSince > .7f && now >= r.detourRetry)
            {
                r.detourRetry = now + 1.1f;
                var final = r.nextPortal != null ? r.nextPortal.Point
                    : r.slot != null && r.slot.useSeatAnchors && !r.seatApproached ? r.slot.ApproachPoint
                    : Destination(r);
                const float pathClearance = NPCCrowdSpace.Clearance + .04f;
                var bypass = navigation.Find(
                    world,
                    r.actor.zone,
                    from,
                    final,
                    p => crowd.Free(r.definition.npcId, p, pathClearance),
                    (a, b) => !crowd.Blocks(r.definition.npcId, a, b, pathClearance)
                );
                if (bypass.Count > 0)
                {
                    r.path = bypass;
                    r.waypoint = 0;
                    r.detourUntil = 0;
                    return true;
                }

                Vector2 forward = (target - from).normalized,
                    side = new Vector2(-forward.y, forward.x);
                for (int i = 0; i < 2; i++)
                {
                    var candidate = from + side * (i == 0 ? .8f : -.8f) + forward * .15f;
                    if (
                        !SidewalkOrCrossing(candidate)
                        || !crowd.Free(r.definition.npcId, candidate, .6f)
                        || WorldNPCNavigation.Obstacle(from, candidate) != null
                        || Vector2.Distance(
                            world.Safe(world.CurrentZone, candidate - world.CurrentZone.origin),
                            candidate
                        ) > .02f
                    )
                    {
                        continue;
                    }

                    r.detour = candidate;
                    r.detourUntil = now + 2;
                    r.waitUntil = now + .12f;
                    break;
                }
            }

            return true;
        }
    }
}
