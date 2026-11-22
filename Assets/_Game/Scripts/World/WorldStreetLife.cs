using System;
using UnityEngine;

namespace IanGame
{
    [Serializable]
    public sealed class StreetPedestrian
    {
        public SpriteRenderer visual;
        public SpriteRenderer shadow;
        public WorldActorAnimation animation;
        public Sprite[] frames;
        public Vector2[] route;
        public float speed = 1.2f;
        public int next;
        public float waiting;
        public bool crossing;

        [NonSerialized]
        public float phase;

        [NonSerialized]
        public bool crossingActive;
    }

    [Serializable]
    public sealed class StreetVehicle
    {
        public Rigidbody2D body;
        public SpriteRenderer visual;
        public SpriteRenderer headlights;
        public SpriteRenderer shadow;
        public UrbanVehicleKind kind;
        public float length = 2.5f;
        public float stopX = 6;
        public float stopSeconds;
        public float speed = 3;
        public float direction = 1;
        public float laneY = -3.1f;

        [NonSerialized]
        public float velocity;

        [NonSerialized]
        public float dwellRemaining;

        [NonSerialized]
        public bool stopServed;

        [NonSerialized]
        public StreetVehicle leader;
    }

    // One controller for a small authored street. Friends/social data remain in
    // WorldNPCScheduler; background citizens are scenery and do not create fake relations.
    public sealed class WorldStreetLife : MonoBehaviour
    {
        public bool residentSimulation;
        public string zoneId = "city";
        public WorldLifeProfile lifeProfile;
        public float crosswalkHalfWidth = 2.7f;
        public int ActiveVehicleCount { get; private set; }
        public int BusDepartures { get; private set; }
        public int VehicleLoops { get; private set; }
        public int TrafficTargetCount { get; private set; }
        WorldNPCScheduler scheduler;
        float nextTrafficDecision;
        int lastTrafficCount = -1;
        public LifeWorldController world;
        public StreetPedestrian[] pedestrians;
        public StreetVehicle[] vehicles;
        public SpriteRenderer vehicleSignal;
        public SpriteRenderer pedestrianSignal;
        float cycle;
        int lastDensity = -1;
        bool visible;
        public SpriteRenderer signal;
        public Sprite[] signalStates;
        public bool CanEnterCrosswalk => PedestrianGreen && !VehicleOnCrossing();
        public bool PedestrianGreen => cycle % 22 >= 14 && cycle % 22 < 21;
        public bool VehicleGreen => cycle % 22 < 12;

        bool Running =>
            visible && !world.Busy && world.Service.Game.State == GameState.Playing && world.hud.shell.Depth == 0;

        Vector2 Origin => (Vector2)transform.position;

        void Start()
        {
            scheduler = world == null ? null : world.GetComponent<WorldNPCScheduler>();
        }

        bool OutsideView(Vector2 position)
        {
            var camera = world.follow == null ? null : world.follow.target;
            if (camera == null)
            {
                return false;
            }

            var point = camera.WorldToViewportPoint(position);
            return point.x < -.15f || point.x > 1.15f || point.y < -.15f || point.y > 1.15f;
        }

        void RefreshTrafficLeaders()
        {
            foreach (var vehicle in vehicles)
            {
                vehicle.leader = null;
                float closest = float.PositiveInfinity;
                foreach (var other in vehicles)
                {
                    if (
                        other == vehicle
                        || !other.body.simulated
                        || other.direction != vehicle.direction
                        || Mathf.Abs(other.laneY - vehicle.laneY) > .5f
                    )
                    {
                        continue;
                    }

                    float gap = (other.body.position.x - vehicle.body.position.x) * vehicle.direction;
                    if (gap > 0 && gap < closest)
                    {
                        closest = gap;
                        vehicle.leader = other;
                    }
                }
            }
        }

        void Update()
        {
            if (world == null || world.Service == null)
            {
                return;
            }

            bool local = world.Service.State.zone == zoneId;
            int hour = world.Service.Hour,
                density = hour >= 7 && hour < 22 ? pedestrians.Length : Mathf.Min(3, pedestrians.Length);
            int trafficCount =
                lifeProfile == null
                    ? (hour >= 7 && hour < 22 ? vehicles.Length : Mathf.Min(2, vehicles.Length))
                    : Mathf.Clamp(lifeProfile.TrafficLimit(zoneId, hour, vehicles.Length), 0, vehicles.Length);
            TrafficTargetCount = local ? trafficCount : 0;
            if (
                local != visible
                || density != lastDensity
                || trafficCount != lastTrafficCount
                || Time.unscaledTime >= nextTrafficDecision
            )
            {
                visible = local;
                lastDensity = density;
                lastTrafficCount = trafficCount;
                nextTrafficDecision =
                    Time.unscaledTime + (lifeProfile == null ? .25f : lifeProfile.trafficDecisionSeconds);
                for (int i = 0; !residentSimulation && i < pedestrians.Length; i++)
                {
                    pedestrians[i].visual.enabled = visible && i < density;
                    if (pedestrians[i].shadow != null)
                    {
                        pedestrians[i].shadow.enabled = visible && i < density;
                    }
                }

                int count = trafficCount;
                ActiveVehicleCount = 0;
                for (int i = 0; i < vehicles.Length; i++)
                {
                    var v = vehicles[i];
                    bool show = visible && i < count;
                    // Retire at the lane edge; never make a visible car disappear at a clock boundary.
                    if (!show && visible && v.body.simulated && !OutsideView(v.body.position))
                    {
                        show = true;
                    }
                    if (show && !v.body.simulated && Vector2.Distance(v.body.position, world.Player.Position) < 3)
                    {
                        v.body.position = Origin + new Vector2(-v.direction * 16, v.laneY);
                    }

                    if (show)
                    {
                        ActiveVehicleCount++;
                    }

                    v.visual.enabled = show;
                    if (v.shadow != null)
                    {
                        v.shadow.enabled = show;
                    }

                    v.body.simulated = show;
                    if (!show)
                    {
                        v.velocity = 0;
                    }
                }

                RefreshTrafficLeaders();
            }

            foreach (var v in vehicles)
            {
                if (v.headlights != null)
                {
                    v.headlights.enabled = v.visual.enabled && (hour < 7 || hour >= 19);
                }
            }

            if (!Running)
            {
                return;
            }

            cycle += Time.deltaTime;
            if (signal != null && signalStates != null && signalStates.Length >= 3)
            {
                signal.sprite = signalStates[
                    VehicleGreen ? 0
                    : PedestrianGreen ? 1
                    : 2
                ];
            }

            if (vehicleSignal != null)
            {
                vehicleSignal.color = VehicleGreen ? new Color(.35f, .95f, .58f) : new Color(1, .35f, .25f);
            }

            if (pedestrianSignal != null)
            {
                pedestrianSignal.color = PedestrianGreen ? new Color(.35f, .95f, .58f) : new Color(1, .35f, .25f);
            }

            Vector2 player = world.Player.Position - Origin;
            foreach (var p in pedestrians)
            {
                if (residentSimulation)
                {
                    break;
                }

                if (
                    !p.visual.enabled
                    || p.route == null
                    || p.route.Length < 2
                    || p.frames == null
                    || p.frames.Length == 0
                )
                {
                    continue;
                }

                Vector2 from = p.visual.transform.localPosition,
                    target = p.route[p.next];
                bool blocked = p.crossing && !p.crossingActive && (!PedestrianGreen || VehicleOnCrossing());
                if (p.waiting > 0)
                {
                    p.waiting -= Time.deltaTime;
                }

                Vector2 to = Vector2.MoveTowards(from, target, p.speed * Time.deltaTime);
                bool nearPlayer =
                    Vector2.Distance(to, player) < .7f && Vector2.Distance(to, player) < Vector2.Distance(from, player);
                blocked |= WorldNPCNavigation.Obstacle(Origin + from, Origin + to) != null;
                bool walking = !blocked && !nearPlayer && p.waiting <= 0 && Vector2.Distance(from, target) > .025f;
                if (walking)
                {
                    if (p.crossing)
                    {
                        p.crossingActive = true;
                    }

                    p.visual.transform.localPosition = to;
                    p.phase += Vector2.Distance(from, to) * 5.2f;
                    if (Mathf.Abs(to.x - from.x) > .001f)
                    {
                        p.visual.flipX = to.x < from.x;
                    }
                }
                else if (!blocked && !nearPlayer && p.waiting <= 0)
                {
                    p.next = (p.next + 1) % p.route.Length;
                    p.waiting = .4f + (p.next % 3) * .35f;
                    p.crossingActive = false;
                }

                Vector2 actual = (Vector2)p.visual.transform.localPosition - from;
                if (p.animation != null)
                {
                    p.animation.Render(
                        actual.sqrMagnitude > .000001f ? ActorMotion.Walk : ActorMotion.Idle,
                        actual,
                        actual.magnitude,
                        true
                    );
                }
                else
                {
                    p.visual.sprite = p.frames[walking ? Mathf.FloorToInt(p.phase) % p.frames.Length : 0];
                }

                p.visual.sortingOrder = 1000 - Mathf.RoundToInt(p.visual.transform.position.y * 100);
                if (p.shadow != null)
                {
                    p.shadow.sortingOrder = p.visual.sortingOrder - 1;
                }
            }
        }

        bool VehicleOnCrossing()
        {
            foreach (var v in vehicles)
            {
                if (v.body.simulated && Mathf.Abs(v.body.position.x - Origin.x) < crosswalkHalfWidth + v.length * .5f)
                {
                    return true;
                }
            }

            return false;
        }

        bool PedestrianOnCrossing()
        {
            foreach (var p in pedestrians)
            {
                if (residentSimulation || p.visual == null)
                {
                    continue;
                }

                Vector2 at = p.visual.transform.localPosition;
                if (p.visual.enabled && Mathf.Abs(at.x) < 2.7f && at.y > -6.7f && at.y < -2.1f)
                {
                    return true;
                }
            }

            var player = world.Player.Position - Origin;
            return Mathf.Abs(player.x) < 2.7f && player.y > -6.7f && player.y < -2.1f;
        }

        void FixedUpdate()
        {
            if (world == null || world.Service == null)
            {
                return;
            }

            bool running = Running;
            Vector2 player = world.Player.Position - Origin;
            bool pedestrian = visible && (PedestrianOnCrossing() || (scheduler != null && scheduler.OnCrossing));
            foreach (var v in vehicles)
            {
                if (!v.body.simulated)
                {
                    continue;
                }

                if (!running)
                {
                    v.velocity = 0;
                    v.body.linearVelocity = Vector2.zero;
                    continue;
                }

                Vector2 at = v.body.position - Origin;
                float desired = v.speed;
                float stopLine = -v.direction * (crosswalkHalfWidth + v.length * .5f + .2f);
                float stop = (stopLine - at.x) * v.direction;
                if ((!VehicleGreen || pedestrian) && stop >= 0)
                {
                    desired = Mathf.Min(desired, Mathf.Sqrt(2 * 8 * Mathf.Max(0, stop - .1f)));
                }

                float ahead = (player.x - at.x) * v.direction;
                if (ahead > 0 && ahead < 3 && Mathf.Abs(player.y - v.laneY) < 1.05f)
                {
                    desired = 0;
                }

                var leader = v.leader;
                if (leader != null && leader.body.simulated)
                {
                    float gap = (leader.body.position.x - v.body.position.x) * v.direction;
                    if (gap > 0 && gap < (v.length + leader.length) * .5f + 1.2f)
                    {
                        desired = 0;
                    }
                }

                if (!v.stopServed && v.stopSeconds > 0)
                {
                    float distanceToStop = (v.stopX - at.x) * v.direction;
                    if (distanceToStop >= 0 && distanceToStop < 2)
                    {
                        desired = Mathf.Min(desired, Mathf.Sqrt(2 * 8 * Mathf.Max(0, distanceToStop - .08f)));
                    }

                    if (Mathf.Abs(distanceToStop) <= .15f && v.velocity < .2f)
                    {
                        v.stopServed = true;
                        v.dwellRemaining = v.stopSeconds;
                    }
                }

                if (v.dwellRemaining > 0)
                {
                    desired = 0;
                    v.dwellRemaining = Mathf.Max(0, v.dwellRemaining - Time.fixedDeltaTime);
                    if (v.dwellRemaining == 0 && v.kind == UrbanVehicleKind.Bus)
                    {
                        BusDepartures++;
                    }
                }

                v.velocity = Mathf.MoveTowards(v.velocity, desired, (desired == 0 ? 10 : 2.8f) * Time.fixedDeltaTime);
                Vector2 to = at + Vector2.right * (v.velocity * v.direction * Time.fixedDeltaTime);
                if (to.x * v.direction > 17)
                {
                    if (Array.IndexOf(vehicles, v) >= TrafficTargetCount)
                    {
                        v.body.simulated = false;
                        v.visual.enabled = false;
                        if (v.shadow != null)
                        {
                            v.shadow.enabled = false;
                        }

                        v.velocity = 0;
                        v.body.linearVelocity = Vector2.zero;
                        continue;
                    }

                    Vector2 spawn = new Vector2(-v.direction * 16.8f, v.laneY);
                    if (Vector2.Distance(spawn, player) > 3)
                    {
                        bool free = true;
                        foreach (var other in vehicles)
                        {
                            if (
                                other != v
                                && other.body.simulated
                                && Mathf.Abs(other.laneY - v.laneY) < .5f
                                && Mathf.Abs(other.body.position.x - Origin.x - spawn.x)
                                    < (v.length + other.length) * .5f + 1.2f
                            )
                            {
                                free = false;
                                break;
                            }
                        }

                        if (!free)
                        {
                            v.velocity = 0;
                            continue;
                        }

                        v.body.position = Origin + spawn;
                        v.velocity = 0;
                        v.stopServed = false;
                        v.dwellRemaining = 0;
                        VehicleLoops++;
                    }
                    else
                    {
                        v.velocity = 0;
                        v.body.linearVelocity = Vector2.zero;
                    }
                }
                else
                {
                    v.body.MovePosition(Origin + to);
                }

                v.visual.sortingOrder = 1000 - Mathf.RoundToInt(v.body.position.y * 100);
                if (v.shadow != null)
                {
                    v.shadow.sortingOrder = v.visual.sortingOrder - 1;
                }
            }
        }
    }
}
