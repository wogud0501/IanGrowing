using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace IanGame
{
    // Rules and game time choose intentions at 4 Hz; visible movement is smooth.
    // The existing scheduler remains the single owner of both friends and citizens.
    public sealed partial class WorldNPCScheduler : MonoBehaviour
    {
        public LifeWorldController world;
        public WorldInteractable[] actors;
        public TMP_Text[] activityLabels = Array.Empty<TMP_Text>();

        sealed class Resident
        {
            public WorldInteractable actor;
            public WorldNPCDefinition definition;
            public WorldActorAnimation animation;
            public NPCScheduleEntry target;
            public bool hasPersonalGoal;
            public Vector2 personalGoal;
            public Vector2 detour;
            public float blockedSince;
            public float waitUntil;
            public float detourUntil;
            public float detourRetry;
            public int index = -1;
            public Vector2 facing = Vector2.down;
            public NPCDailyAction action;
            public long used = -1;
            public List<Vector2> path = new List<Vector2>();
            public int waypoint;
            public WorldInteractable portal;
            public WorldInteractable nextPortal;
            public WorldDoor door;
            public WorldDoor entryDoor;
            public int portalPhase;
            public float retry;
            public float releaseSeatUntil;
            public float labelUntil;
            public float arrivingUntil;
            public float departingUntil;
            public bool active;
            public float nextVisualTick;
            public float lastVisualStep;
            public int ambientWaypoint;
            public float ambientPauseUntil;
            public Color baseColor = Color.white;
            public float ambientConversationUntil;
            public bool atActivity;
            public ResidentActivityStation station;
            public SpriteRenderer heldItem;
            public int taskStep;
            public bool finishedTask;
            public float workUntil;
            public bool pendingAbstract;
            public float seatedAt;
            public float standingUntil;
            public ActorMotion exitMotion;
            public WorldInteractable anchor;
            public WorldInteractable leavingBed;
            public Vector2 anchorFacing;
            public WorldActivitySlot slot;
            public WorldActivitySlot requestedSlot;
            public WorldActivitySlot departureSlot;
            public WorldDoor interiorDoor;
            public float interiorSide;
            public bool seatApproached;
        }

        Resident[] residents = Array.Empty<Resident>();
        readonly WorldNPCNavigation navigation = new WorldNPCNavigation();
        float nextDecision;
        long lastMinute = -1;
        string viewedZone;
        bool ready;
        WorldStreetLife street;
        SceneVisualPresentation scenePresentation;
        readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        ContactFilter2D hitFilter;
        public int AbstractUpdates { get; private set; }
        public int VisibleSteps { get; private set; }
        public int DoorPassages { get; private set; }
        public int ObjectUses { get; private set; }
        public int NavigationBakes => navigation.Bakes;
        public bool OnCrossing =>
            residents.Any(r =>
                r.actor.zone == "city"
                && r.actor.visual.enabled
                && Road((Vector2)r.actor.transform.position - world.Service.rules.Zone("city").origin)
            );
        public long Minute =>
            (long)(world.Service.Game.clock.Day - 1) * 1440 + (long)(world.Service.Game.clock.SubDaySeconds / 60);

        public static int Schedule(WorldNPCDefinition d, int day, int minute)
        {
            int result = -1,
                latest = -1,
                mask = 1 << ((day - 1) % 7);
            for (int i = 0; i < d.schedule.Length; i++)
            {
                var entry = d.schedule[i];
                int at = entry.hour * 60 + entry.minute;
                if ((entry.dayMask & mask) == 0)
                {
                    continue;
                }

                if (at <= minute && at >= latest)
                {
                    result = i;
                    latest = at;
                }
            }

            if (result >= 0)
            {
                return result;
            }

            for (int daysAgo = 1; daysAgo <= 7; daysAgo++)
            {
                mask = 1 << ((day - 1 - daysAgo + 7) % 7);
                for (int i = 0; i < d.schedule.Length; i++)
                {
                    var entry = d.schedule[i];
                    int at = entry.hour * 60 + entry.minute;
                    if ((entry.dayMask & mask) != 0 && at >= latest)
                    {
                        result = i;
                        latest = at;
                    }
                }

                if (result >= 0)
                {
                    return result;
                }
            }

            return 0;
        }

        void Start()
        {
            if (world == null || world.Service == null)
            {
                return;
            }

            scenePresentation = world.GetComponent<SceneVisualPresentation>();
            if (sceneScoped)
            {
                StartScoped();
                return;
            }

            street = world.GetComponentInChildren<WorldStreetLife>();
            hitFilter = new ContactFilter2D { useTriggers = false };
            if (world.Service == null)
            {
                return;
            }

            var defs = world.Service.rules.npcs;
            foreach (var group in actors.Where(a => a != null).GroupBy(a => a.npcId))
            {
                foreach (var duplicate in group.Skip(1))
                {
                    duplicate.gameObject.SetActive(false);
                }
            }

            residents = actors
                .Where(a => a != null)
                .GroupBy(a => a.npcId)
                .Select(group => group.First())
                .Select(a => new { actor = a, definition = defs.FirstOrDefault(d => d.npcId == a.npcId) })
                .Where(x => x.definition != null)
                .Select(x => new Resident
                {
                    actor = x.actor,
                    definition = x.definition,
                    animation = x.actor.GetComponent<WorldActorAnimation>(),
                })
                .ToArray();
            world.Service.Restored += Restore;
            ready = true;
            Restore();
        }

        void OnDisable()
        {
            if (world != null && world.Service != null)
            {
                world.Service.Restored -= Restore;
            }

            foreach (var r in residents)
            {
                r.entryDoor?.Release(r.definition.npcId);
                r.entryDoor = null;
                r.door?.Release(r.definition.npcId);
                r.slot?.Release(r.definition.npcId);
                r.departureSlot?.Release(r.definition.npcId);
                r.interiorDoor?.Release(r.definition.npcId);
            }

            ready = false;
        }

        public void Restore()
        {
            if (!ready || sceneScoped && world.Room.roomId != world.Service.State.zone)
            {
                return;
            }

            if (sceneScoped && !restoringScoped)
            {
                RebuildScoped(true);
                return;
            }

            navigation.Clear();
            lastMinute = -1;
            viewedZone = null;
            var data = world.Service.State.residents;
            foreach (var r in residents)
            {
                r.entryDoor?.Release(r.definition.npcId);
                r.entryDoor = null;
                r.door?.Release(r.definition.npcId);
                r.slot?.Release(r.definition.npcId);
                r.departureSlot?.Release(r.definition.npcId);
                r.interiorDoor?.Release(r.definition.npcId);
                r.interiorDoor = null;
                r.departureSlot = null;
                r.leavingBed = null;
                r.seatApproached = false;
                r.slot = null;
                r.requestedSlot = null;
                r.door = null;
                r.portal = null;
                r.nextPortal = null;
                r.portalPhase = 0;
                r.index = -1;
                r.path.Clear();
                r.waypoint = 0;
                r.used = -1;
                var saved = data?.FirstOrDefault(n => n.id == r.definition.npcId);
                if (saved != null && world.Service.rules.Zone(saved.zone) != null)
                {
                    r.actor.zone = saved.zone;
                    r.actor.transform.position = world.Service.rules.Zone(saved.zone).origin + saved.position;
                    r.facing = saved.facing;
                    r.used = saved.completedUseKey;
                }
                else
                {
                    r.actor.zone = r.definition.homeZone;
                    var z = world.Service.rules.Zone(r.actor.zone);
                    r.actor.transform.position =
                        z.origin + (r.definition.hasHomePosition ? r.definition.homePosition : z.spawn);
                }
            }

            Decide(true);
            foreach (var r in residents)
            {
                var saved = data?.FirstOrDefault(n => n.id == r.definition.npcId);
                if (
                    saved == null
                    || world.Service.rules.Zone(saved.zone) == null
                    || saved.minute != Minute
                    || saved.scheduleIndex != r.index
                    || saved.objectId != r.target.objectId
                    || saved.action != NPCDailyAction.Walk
                        && (saved.action != r.target.action || saved.zone != r.target.zone || !CanStartHere(r))
                )
                {
                    continue;
                }

                var loaded = world.Service.rules.Zone(saved.zone).origin + saved.position;
                if (saved.action != NPCDailyAction.Walk && r.anchor != null)
                {
                    var pose = saved.action == NPCDailyAction.Sleep ? BedPose(r.anchor) : Destination(r);
                    if (Vector2.Distance(loaded, pose) > .3f)
                    {
                        // A stationary snapshot is valid only at its current activity anchor.
                        r.actor.transform.position = Destination(r);
                        loaded = pose;
                    }
                }

                bool seated = saved.action != NPCDailyAction.Walk && r.slot != null && r.slot.seated;
                if (sceneScoped && !seated && !SpawnPoint(ref loaded, r.actor))
                {
                    continue;
                }

                r.actor.zone = saved.zone;
                r.actor.transform.position = loaded;
                r.facing = saved.action != NPCDailyAction.Walk && r.slot != null ? r.slot.facing : saved.facing;
                r.action = saved.action;
                r.atActivity = saved.action != NPCDailyAction.Walk;
                r.active = r.actor.zone == world.Service.State.zone;
                r.actor.visual.enabled = r.active;
                if (r.actor.shadow != null)
                {
                    r.actor.shadow.enabled = r.active;
                }

                r.animation?.Play(Motion(r.action), r.facing);
                r.animation?.Seek(30);
            }
        }

        public void Capture()
        {
            if (!ready || sceneScoped && world.Room.roomId != world.Service.State.zone)
            {
                return;
            }

            var state = world.Service.State;
            if (state.residents == null)
            {
                state.residents = new List<NPCResidentSnapshot>();
            }

            foreach (var r in residents)
            {
                var item = state.residents.FirstOrDefault(n => n.id == r.definition.npcId);
                if (item == null)
                {
                    item = new NPCResidentSnapshot { id = r.definition.npcId };
                    state.residents.Add(item);
                }

                item.zone = r.actor.zone;
                item.position =
                    (r.portal != null ? r.portal.Point : (Vector2)r.actor.transform.position)
                    - world.Service.rules.Zone(r.actor.zone).origin;
                item.facing = r.facing;
                item.destinationZone = r.target?.zone;
                item.objectId = r.target?.objectId;
                item.action = r.action;
                item.scheduleIndex = r.index;
                item.minute = Minute;
                item.completedUseKey = r.used;
            }
        }

        void Update()
        {
            if (world == null || world.Service == null)
            {
                return;
            }

            if (sceneScoped && world.Room.roomId != world.Service.State.zone)
            {
                return;
            }

            if (sceneScoped && ready && lastMinute >= 0 && Math.Abs(Minute - lastMinute) >= 60)
            {
                RebuildScoped(true, true);
            }

            if (
                sceneScoped
                && ready
                && world.Service != null
                && !world.Busy
                && world.Service.Game.State == GameState.Playing
                && Time.unscaledTime >= nextScoped
            )
            {
                nextScoped = Time.unscaledTime + (lifeProfile == null ? .25f : lifeProfile.populationTickSeconds);
                RefreshScoped();
            }

            if (
                !ready
                || world.Service == null
                || world.Busy
                || world.Service.Game.State != GameState.Playing
                || world.hud.shell.Depth > 0
            )
            {
                return;
            }

            if (Time.unscaledTime >= nextDecision)
            {
                nextDecision = Time.unscaledTime + .25f;
                Decide(false);
            }

            FillCrowd();
            for (int i = 0; i < residents.Length; i++)
            {
                var resident = residents[i];
                float dt = Mathf.Min(Time.deltaTime, .1f);
                if (
                    resident.definition.ambient
                    && resident.active
                    && OutsidePopulationView(resident.actor.transform.position)
                )
                {
                    if (Time.unscaledTime < resident.nextVisualTick)
                    {
                        continue;
                    }

                    dt =
                        resident.lastVisualStep <= 0 ? dt : Mathf.Min(Time.unscaledTime - resident.lastVisualStep, .3f);
                    resident.nextVisualTick = Time.unscaledTime + .25f;
                }

                resident.lastVisualStep = Time.unscaledTime;
                if (Time.unscaledTime < resident.ambientConversationUntil)
                {
                    Render(resident, i, resident.actor.transform.position, ActorMotion.Talk);
                    continue;
                }

                TickAmbientPatrol(resident);
                Step(resident, i, dt);
                if (residents[i].active)
                {
                    crowd.Set(residents[i].definition.npcId, residents[i].actor.transform.position);
                }
                else
                {
                    crowd.Remove(residents[i].definition.npcId);
                }
            }
        }

        WorldInteractable Object(string id) =>
            string.IsNullOrEmpty(id) ? null : world.objects.FirstOrDefault(o => o.id == id);

        Vector2 Destination(Resident r)
        {
            if (HasChore(r) && r.slot != null)
            {
                return r.station.Waypoint(r.taskStep);
            }

            if (r.target.action == NPCDailyAction.Rest && r.slot == null && r.actor.zone == r.target.zone)
            {
                return r.actor.transform.position;
            }

            if (r.slot != null)
            {
                return r.target.action == NPCDailyAction.GetIngredients ? r.slot.ApproachPoint : r.slot.Point;
            }

            if (r.requestedSlot != null)
            {
                return world.Service.rules.Zone(r.target.zone).origin
                    + new Vector2(3.4f, -4.5f + Array.IndexOf(residents, r) * .65f);
            }

            var z = world.Service.rules.Zone(r.target.zone);
            var point = r.anchor != null && r.anchor.zone == z.id ? r.anchor.Point : z.origin + r.target.position;
            if (
                r.definition.ambient
                && r.target.allowStroll
                && r.target.action == NPCDailyAction.Walk
                && r.definition.ambientOffsets.Length > 0
            )
            {
                point += r.definition.ambientOffsets[r.ambientWaypoint % r.definition.ambientOffsets.Length];
            }
            return z.id == "city" && world.CurrentZone.id == "city" ? PersonalGoal(r, point) : point;
        }

        void Decide(bool restoring)
        {
            long now = Minute;
            bool minuteChanged = now != lastMinute;
            string local = world.Service.State.zone;
            bool zoneChanged = local != viewedZone;
            lastMinute = now;
            viewedZone = local;
            foreach (var r in residents)
            {
                int index = Schedule(
                    r.definition,
                    world.Service.Game.clock.Day,
                    (int)(world.Service.Game.clock.SubDaySeconds / 60)
                );
                bool changed = index != r.index;
                r.target = r.definition.schedule[index];
                bool sameSeat =
                    changed
                    && r.atActivity
                    && r.slot != null
                    && r.slot.seated
                    && r.slot.IsOccupied
                    && r.slot.Occupant == r.definition.npcId
                    && r.anchor != null
                    && r.anchor.id == r.target.objectId
                    && r.target.zone == r.actor.zone
                    && r.target.action is NPCDailyAction.Rest or NPCDailyAction.Talk or NPCDailyAction.WatchTV;
                if (sameSeat)
                {
                    r.index = index;
                    r.action = r.target.action;
                    r.labelUntil = Time.unscaledTime + 2;
                }
                else if (changed)
                {
                    r.hasPersonalGoal = false;
                    r.ambientWaypoint = 0;
                    r.ambientPauseUntil = 0;
                    r.waitUntil = r.detourUntil = r.blockedSince = 0;
                    r.interiorDoor?.Release(r.definition.npcId);
                    r.interiorDoor = null;
                    if (
                        r.slot != null
                        && r.slot.useSeatAnchors
                        && r.slot.seated
                        && r.atActivity
                        && r.actor.zone == local
                        && r.target.objectId != r.slot.GetComponent<WorldInteractable>().id
                    )
                    {
                        r.departureSlot = r.slot;
                        r.standingUntil = Time.unscaledTime + .55f;
                        r.exitMotion = ActorMotion.StandUp;
                    }
                    else
                    {
                        r.slot?.Release(r.definition.npcId);
                    }

                    r.seatApproached = false;
                    r.slot = null;
                    if (r.action == NPCDailyAction.Sleep && r.atActivity && r.actor.zone == local)
                    {
                        r.leavingBed = r.anchor;
                    }

                    r.taskStep = 0;
                    r.finishedTask = false;
                    r.workUntil = 0;
                    BindActivity(r);

                    if (r.action == NPCDailyAction.Sleep || r.action == NPCDailyAction.Rest)
                    {
                        r.releaseSeatUntil = Time.unscaledTime + 1;
                        r.standingUntil = Time.unscaledTime + .55f;
                        r.exitMotion = r.action == NPCDailyAction.Sleep ? ActorMotion.WakeUp : ActorMotion.StandUp;
                    }

                    r.door?.Release(r.definition.npcId);
                    r.door = null;
                    r.portal = null;
                    r.nextPortal = null;
                    r.portalPhase = 0;
                    r.path.Clear();
                    r.waypoint = 0;
                    r.index = index;
                    r.atActivity = false;
                    r.departingUntil = 0;
                    r.labelUntil = Time.unscaledTime + 2;
                    r.action = NPCDailyAction.Walk;
                }

                if (!changed && r.target.action == NPCDailyAction.Rest && r.slot == null)
                {
                    BindActivity(r);
                }

                if (r.slot == null && r.requestedSlot != null && r.requestedSlot.TryReserve(r.definition.npcId))
                {
                    r.slot = r.requestedSlot;
                    r.path.Clear();
                    r.waypoint = 0;
                    r.atActivity = false;
                    r.pendingAbstract = true;
                }

                bool here = r.actor.zone == local;
                bool goesHere = r.target.zone == local;
                if (!here && r.departureSlot != null)
                {
                    r.departureSlot.Release(r.definition.npcId);
                    r.departureSlot = null;
                    r.standingUntil = 0;
                }

                if (!here && !goesHere && (minuteChanged || changed || restoring || zoneChanged || r.pendingAbstract))
                {
                    r.pendingAbstract = false;
                    r.actor.zone = r.target.zone;
                    r.actor.transform.position = Destination(r);
                    r.action = CanStartHere(r) ? r.target.action : NPCDailyAction.Idle;
                    r.atActivity = r.requestedSlot == null || r.slot != null;
                    if (r.action == NPCDailyAction.Sleep)
                    {
                        var bed = Object(r.target.objectId);
                        if (bed != null)
                        {
                            r.actor.transform.position = BedPose(bed);
                        }
                    }

                    UseObject(r);
                    AbstractUpdates++;
                }
                else if (!here && goesHere && (changed || restoring || zoneChanged || minuteChanged))
                {
                    var arrival =
                        world.objects.FirstOrDefault(o =>
                            o.kind == WorldInteractionKind.Door && o.zone == local && o.targetZone == r.actor.zone
                        ) ?? world.objects.FirstOrDefault(o => o.kind == WorldInteractionKind.Door && o.zone == local);
                    r.actor.zone = local;
                    // Re-entering a zone materializes the already simulated activity. Live arrivals
                    // use a numbered entrance lane, never a shared doorway coordinate.
                    if (restoring || zoneChanged)
                    {
                        r.actor.transform.position = Destination(r);
                        r.action = CanStartHere(r) ? r.target.action : NPCDailyAction.Idle;
                        r.atActivity = CanStartHere(r);
                        r.facing = r.slot != null ? r.slot.facing : r.facing;
                        UseObject(r);
                    }
                    else
                    {
                        int lane = Array.IndexOf(residents, r);
                        var origin =
                            arrival == null
                                ? world.Service.rules.Zone(local).origin + world.Service.rules.Zone(local).spawn
                                : arrival.Point;
                        var candidate = origin + new Vector2((lane % 3 - 1) * .7f, (lane / 3) * .65f + .4f);
                        r.actor.transform.position = world.Safe(
                            world.Service.rules.Zone(local),
                            candidate - world.Service.rules.Zone(local).origin
                        );
                        r.atActivity = false;
                    }

                    var door = arrival == null ? null : arrival.GetComponent<WorldDoor>();
                    if (door != null)
                    {
                        door.Acquire(r.definition.npcId, world.Service.Hour, out _);
                        door.Release(r.definition.npcId);
                    }

                    r.arrivingUntil = Time.unscaledTime + .3f;
                    r.path.Clear();
                    r.waypoint = 0;
                }

                if (r.atActivity && r.slot != null)
                {
                    r.slot.MarkOccupied(r.definition.npcId);
                }

                r.active = r.actor.zone == local;
                r.actor.visual.enabled = r.active;
                if (r.actor.shadow != null)
                {
                    r.actor.shadow.enabled = r.active;
                }

                if (r.actor.highlight != null && !r.active)
                {
                    r.actor.highlight.enabled = false;
                }

                if (!r.active)
                {
                    int indexLabel = System.Array.IndexOf(residents, r);
                    if (indexLabel < activityLabels.Length && activityLabels[indexLabel] != null)
                    {
                        activityLabels[indexLabel].gameObject.SetActive(false);
                    }
                }

                if (!r.active && r.animation != null)
                {
                    r.animation.Play(Motion(r.action), r.facing);
                    r.animation.Seek(30);
                }
            }
        }

        static Vector2 BedPose(WorldInteractable bed)
        {
            var mattress = bed.visual.bounds;
            return new Vector2(mattress.center.x, mattress.min.y + mattress.size.y * .40f);
        }

        bool CanStartHere(Resident resident)
        {
            var entry = resident.target;
            if (resident.actor.zone != entry.zone || !NPCActivityLocation.Allows(resident.definition, entry))
            {
                return false;
            }

            if (string.IsNullOrEmpty(entry.objectId))
            {
                return !NPCActivityLocation.RequiresAnchor(entry.action);
            }

            return resident.anchor != null
                && resident.anchor.zone == entry.zone
                && NPCActivityLocation.Matches(entry.action, resident.anchor)
                && (resident.requestedSlot == null || resident.slot != null);
        }

        static ActorMotion Motion(NPCDailyAction action) =>
            action == NPCDailyAction.Sleep ? ActorMotion.Sleep
            : action == NPCDailyAction.Eat ? ActorMotion.Eat
            : action == NPCDailyAction.Work || action == NPCDailyAction.Shop ? ActorMotion.Work
            : action == NPCDailyAction.GetIngredients ? ActorMotion.PickUpItem
            : action == NPCDailyAction.ServeMeal ? ActorMotion.PutDownItem
            : action == NPCDailyAction.WatchTV ? ActorMotion.SeatedIdle
            : action == NPCDailyAction.Wash ? ActorMotion.WashFace
            : action
                is NPCDailyAction.Cook
                    or NPCDailyAction.Clean
                    or NPCDailyAction.WashDishes
                    or NPCDailyAction.StockShelf
                ? ActorMotion.Interact
            : action == NPCDailyAction.Study ? ActorMotion.Study
            : action == NPCDailyAction.Rest ? ActorMotion.SitDown
            : action == NPCDailyAction.Phone ? ActorMotion.UsePhone
            : action == NPCDailyAction.Talk ? ActorMotion.Talk
            : action == NPCDailyAction.Wake ? ActorMotion.WakeUp
            : ActorMotion.Idle;

        void UseObject(Resident r)
        {
            long key = ((long)world.Service.Game.clock.Day << 16) + r.index;
            if (r.used == key || !CanStartHere(r))
            {
                return;
            }

            var target = r.anchor;
            if (target != null)
            {
                target.RecordNPCUse(r.definition.npcId);
                FamilyLife.Use(world.Service.Game, r.definition.npcId, target, key);
                ObjectUses++;
            }

            r.used = key;
        }

        WorldInteractable NextPortal(string from, string goal)
        {
            if (sceneScoped)
            {
                return Object(world.Room.catalog.FirstDoor(from, goal));
            }

            var queue = new Queue<string>();
            var previous = new Dictionary<string, WorldInteractable>();
            queue.Enqueue(from);
            previous[from] = null;
            while (queue.Count > 0)
            {
                string zone = queue.Dequeue();
                foreach (var p in world.objects.Where(o => o.kind == WorldInteractionKind.Door && o.zone == zone))
                {
                    if (previous.ContainsKey(p.targetZone))
                    {
                        continue;
                    }

                    previous[p.targetZone] = p;
                    if (p.targetZone == goal)
                    {
                        var edge = p;
                        while (edge.zone != from)
                        {
                            edge = previous[edge.zone];
                        }

                        return edge;
                    }

                    queue.Enqueue(p.targetZone);
                }
            }

            return null;
        }

        static bool Road(Vector2 local) => local.y > -6.7f && local.y < -2.1f && Mathf.Abs(local.x) < 1;

        bool Move(Resident r, int index, Vector2 target, float dt)
        {
            if (Time.unscaledTime < r.waitUntil)
            {
                return false;
            }

            Vector2 from = r.actor.transform.position;
            if (r.detourUntil > Time.unscaledTime)
            {
                if (Vector2.Distance(from, r.detour) > .1f)
                {
                    target = r.detour;
                }
                else
                {
                    r.detourUntil = 0;
                }
            }

            var next = Vector2.MoveTowards(from, target, r.definition.speed * dt);
            if (Vector2.Distance(from, next) < .00001f)
            {
                return false;
            }

            if (r.actor.zone == "city")
            {
                var z = world.Service.rules.Zone("city");
                if (!WorldNPCNavigation.OnPedestrianRoute(z, from, next))
                {
                    return false;
                }

                if (!Road(from - z.origin) && Road(next - z.origin) && street != null && !street.CanEnterCrosswalk)
                {
                    return false;
                }
            }

            if (WaitForSpace(r, from, target, next))
            {
                return false;
            }

            if (Time.unscaledTime > r.releaseSeatUntil)
            {
                int count = Physics2D.CircleCast(
                    from,
                    .2f,
                    (next - from).normalized,
                    hitFilter,
                    hits,
                    Vector2.Distance(from, next)
                );
                for (int h = 0; h < count; h++)
                {
                    var hit = hits[h];
                    if (
                        hit.collider == null
                        || hit.collider.isTrigger
                        || hit.collider.attachedRigidbody != null
                        || hit.collider.GetComponentInParent<WorldInteractable>() == r.actor
                    )
                    {
                        continue;
                    }

                    var interior = hit.collider.GetComponentInParent<WorldDoor>();
                    if (interior != null && interior.portal.zone == interior.portal.targetZone)
                    {
                        if (r.interiorDoor != interior)
                        {
                            r.interiorDoor?.Release(r.definition.npcId);
                            r.interiorSide = interior.SideOf(from);
                            r.interiorDoor = interior;
                        }

                        interior.Acquire(r.definition.npcId, world.Service.Hour, out _);
                    }

                    return false;
                }
            }

            r.blockedSince = 0;
            r.actor.transform.position = next;
            crowd.Set(r.definition.npcId, next);
            r.facing = (next - from).normalized;
            VisibleSteps++;
            return true;
        }

        void Step(Resident r, int index, float dt)
        {
            if (!r.active)
            {
                return;
            }

            if (
                r.interiorDoor != null
                && r.interiorDoor.SideOf(r.actor.transform.position) != r.interiorSide
                && Vector2.Distance(r.actor.transform.position, r.interiorDoor.Plane) > 1
            )
            {
                r.interiorDoor.Release(r.definition.npcId);
                r.interiorDoor = null;
            }

            Vector2 before = r.actor.transform.position;
            if (r.entryDoor != null)
            {
                if (r.entryDoor.State != DoorState.Open)
                {
                    Render(r, index, before, ActorMotion.Idle);
                    return;
                }

                if (Vector2.Distance(before, r.entryDoor.portal.Point) > .05f)
                {
                    r.action = NPCDailyAction.Walk;
                    Move(r, index, r.entryDoor.portal.Point, dt);
                    Render(r, index, before);
                    return;
                }

                r.entryDoor.Release(r.definition.npcId);
                r.entryDoor = null;
            }

            if (r.departureSlot != null)
            {
                if (Time.unscaledTime < r.standingUntil)
                {
                    Render(r, index, before, ActorMotion.StandUp);
                    return;
                }

                if (Vector2.Distance(before, r.departureSlot.StandUpPoint) > .025f)
                {
                    r.releaseSeatUntil = Time.unscaledTime + .1f;
                    Move(r, index, r.departureSlot.StandUpPoint, dt);
                    r.action = NPCDailyAction.Walk;
                    Render(r, index, before);
                    return;
                }

                r.departureSlot.Release(r.definition.npcId);
                r.departureSlot = null;
                r.path.Clear();
                r.waypoint = 0;
            }

            if (Time.unscaledTime < r.standingUntil)
            {
                Render(r, index, before, r.exitMotion);
                return;
            }

            if (r.leavingBed != null)
            {
                if (Vector2.Distance(before, r.leavingBed.Point) > .04f)
                {
                    r.releaseSeatUntil = Time.unscaledTime + .1f;
                    Move(r, index, r.leavingBed.Point, dt);
                    r.action = NPCDailyAction.Walk;
                    Render(r, index, before);
                    return;
                }

                r.leavingBed = null;
            }

            if (r.portal != null)
            {
                var portal = r.portal;
                var door = r.door;
                if (door != null && door.State != DoorState.Open)
                {
                    if (!door.Acquire(r.definition.npcId, world.Service.Hour, out _))
                    {
                        Label(r, index, "문 앞 대기");
                    }

                    Render(r, index, before);
                    return;
                }

                Vector2 cross = door == null ? portal.Point : door.CrossPoint;
                if (Vector2.Distance(r.actor.transform.position, cross) > .06f)
                {
                    Move(r, index, cross, dt);
                    Render(r, index, before);
                    return;
                }

                if (r.departingUntil == 0)
                {
                    r.departingUntil = Time.unscaledTime + .18f;
                }

                if (Time.unscaledTime < r.departingUntil)
                {
                    Render(r, index, before);
                    return;
                }

                r.departingUntil = 0;
                door?.Release(r.definition.npcId);
                r.actor.zone = portal.targetZone;
                r.actor.transform.position = world.Service.rules.Zone(r.actor.zone).origin + portal.arrival;
                r.portal = null;
                r.door = null;
                r.path.Clear();
                r.waypoint = 0;
                r.nextPortal = null;
                r.pendingAbstract = true;
                DoorPassages++;
                r.active = r.actor.zone == world.Service.State.zone;
                r.actor.visual.enabled = r.active;
                if (r.actor.shadow != null)
                {
                    r.actor.shadow.enabled = r.active;
                }

                return;
            }

            bool leaving = r.actor.zone != r.target.zone;
            if (leaving && (r.nextPortal == null || r.nextPortal.zone != r.actor.zone))
            {
                r.nextPortal = NextPortal(r.actor.zone, r.target.zone);
            }

            var nextPortal = leaving ? r.nextPortal : null;
            if (leaving && nextPortal == null)
            {
                r.action = NPCDailyAction.Idle;
                Render(r, index, before);
                return;
            }

            if (!leaving && AdvanceChore(r, index))
            {
                return;
            }

            Vector2 goal = nextPortal == null ? Destination(r) : nextPortal.Point;
            bool anchored = !leaving && r.slot != null && r.slot.useSeatAnchors;
            if (anchored && !r.atActivity)
            {
                goal = r.seatApproached ? Destination(r) : r.slot.ApproachPoint;
                if (!r.seatApproached && Vector2.Distance(before, goal) < .08f)
                {
                    r.seatApproached = true;
                    r.path.Clear();
                    r.waypoint = 0;
                    r.facing = r.slot.facing;
                    Render(r, index, before, ActorMotion.Idle);
                    return;
                }

                if (r.seatApproached && Vector2.Distance(before, goal) > .025f)
                {
                    r.releaseSeatUntil = Time.unscaledTime + .1f;
                    Move(r, index, goal, dt);
                    r.action = NPCDailyAction.Walk;
                    Render(r, index, before);
                    return;
                }
            }

            if (
                !leaving && r.atActivity
                || Vector2.Distance(r.actor.transform.position, goal) < (anchored ? .03f : .16f)
            )
            {
                if (nextPortal != null)
                {
                    var door = nextPortal.GetComponent<WorldDoor>();
                    if (door != null && !door.Acquire(r.definition.npcId, world.Service.Hour, out _))
                    {
                        r.action = NPCDailyAction.Idle;
                        Render(r, index, before);
                        return;
                    }

                    r.door = door;
                    r.portal = nextPortal;

                    r.action = NPCDailyAction.Walk;
                    Render(r, index, before);
                    return;
                }

                if (r.requestedSlot != null && r.slot == null)
                {
                    r.action = NPCDailyAction.Idle;
                    r.atActivity = false;
                    Label(r, index, "휴게 자리 대기");
                    Render(r, index, before);
                    return;
                }

                if (!r.atActivity)
                {
                    r.seatedAt = Time.unscaledTime;
                    if (anchored)
                    {
                        r.actor.transform.position = Destination(r);
                    }
                }

                if (!CanStartHere(r))
                {
                    r.action = NPCDailyAction.Idle;
                    r.atActivity = false;
                    Render(r, index, before);
                    return;
                }

                r.atActivity = true;
                if (r.slot != null)
                {
                    r.slot.MarkOccupied(r.definition.npcId);
                }

                r.action = r.target.action;
                if (r.slot != null)
                {
                    r.facing = r.slot.facing;
                }
                else if (r.anchorFacing != Vector2.zero)
                {
                    r.facing = r.anchorFacing;
                }

                if (HasChore(r) && r.slot != null)
                {
                    r.facing = r.station.workingFacing;
                    if (r.station.dirtyDishes)
                    {
                        r.station.dirtyDishes.enabled = true;
                    }
                }

                UseObject(r);
                if (r.action == NPCDailyAction.Sleep)
                {
                    var bed = Object(r.target.objectId);
                    if (bed != null)
                    {
                        r.actor.transform.position = BedPose(bed);
                    }
                }

                if (r.action == NPCDailyAction.Talk)
                {
                    var friend = residents.FirstOrDefault(n =>
                        n.definition.npcId == r.definition.companionId && n.actor.zone == r.actor.zone
                    );
                    if (
                        friend != null
                        && Vector2.Distance(friend.actor.transform.position, r.actor.transform.position) < 2
                    )
                    {
                        r.facing = (friend.actor.transform.position - r.actor.transform.position).normalized;
                    }
                }

                Render(r, index, before);
                return;
            }

            if (r.path.Count == 0 || r.waypoint >= r.path.Count)
            {
                if (Time.unscaledTime < r.retry)
                {
                    Render(r, index, before);
                    return;
                }

                r.path = navigation.Find(world, r.actor.zone, r.actor.transform.position, goal);
                r.waypoint = 0;
                r.retry = Time.unscaledTime + 1;
            }

            if (r.path.Count > 0 && r.waypoint < r.path.Count)
            {
                var p = r.path[r.waypoint];
                if (
                    Vector2.Distance(r.actor.transform.position, p)
                    < (anchored && r.waypoint == r.path.Count - 1 ? .015f : .09f)
                )
                {
                    r.waypoint++;
                }
                else
                {
                    Move(r, index, p, dt);
                }
            }

            r.action = NPCDailyAction.Walk;
            Render(r, index, before);
        }

        void Label(Resident r, int index, string value)
        {
            if (index < activityLabels.Length && activityLabels[index] != null)
            {
                activityLabels[index].text = value;
                activityLabels[index].gameObject.SetActive(r.active);
            }
        }

        void Render(Resident r, int index, Vector2 before, ActorMotion? forced = null)
        {
            Vector2 delta = (Vector2)r.actor.transform.position - before;
            bool walking = r.action == NPCDailyAction.Walk && delta.sqrMagnitude > .0000001f;
            var motion =
                walking ? ActorMotion.Walk
                : r.action == NPCDailyAction.Walk ? ActorMotion.Idle
                : r.action == NPCDailyAction.Work && r.slot != null
                    ? (Time.unscaledTime - r.seatedAt < .55f ? ActorMotion.SitDown : ActorMotion.UseComputer)
                : Motion(r.action);
            if (r.slot != null && r.slot.useSeatAnchors && r.atActivity && r.action != NPCDailyAction.Walk)
            {
                motion =
                    r.slot.seated && Time.unscaledTime - r.seatedAt < .55f
                        ? ActorMotion.SitDown
                        : r.slot.activityMotion;
                if (
                    r.slot.GetComponent<WorldFacilityActivityPresentation>() == null
                    && r.animation != null
                    && r.animation.library.Find(motion, ActorAnimationSet.Direction(r.facing), false) == null
                )
                {
                    motion = ActorMotion.Idle;
                }
            }

            if (r.action == NPCDailyAction.Talk && FamilyLife.Parent(r.definition.npcId))
            {
                motion = ActorMotion.Talk;
            }

            motion = LifeMotion(r, motion);
            if (forced.HasValue)
            {
                motion = forced.Value;
                walking = false;
            }

            r.animation?.Render(
                motion,
                walking ? delta : r.facing,
                delta.magnitude,
                motion == ActorMotion.Walk || motion == ActorMotion.Idle
            );
            r.actor.visual.sortingOrder =
                1000
                - Mathf.RoundToInt(r.actor.transform.position.y * 100)
                + (r.action == NPCDailyAction.Sleep ? 100 : 0);
            if (r.action == NPCDailyAction.Sleep && r.anchor != null)
            {
                r.actor.visual.sortingOrder = Mathf.Max(r.actor.visual.sortingOrder, r.anchor.visual.sortingOrder + 1);
            }

            if (r.station != null && r.station.seatSurface && r.slot != null && r.slot.IsOccupied && r.atActivity)
            {
                r.actor.visual.sortingOrder = Mathf.Max(
                    r.actor.visual.sortingOrder,
                    r.station.seatSurface.sortingOrder + 1
                );
            }

            DrawHeldItem(r, motion);

            if (r.actor.shadow != null)
            {
                r.actor.shadow.enabled = r.active && r.action != NPCDailyAction.Sleep;
                r.actor.shadow.sortingOrder = r.actor.visual.sortingOrder - 1;
            }

            if (r.actor.highlight != null)
            {
                r.actor.highlight.sprite = r.actor.visual.sprite;
                r.actor.highlight.flipX = r.actor.visual.flipX;
                r.actor.highlight.sortingOrder = r.actor.visual.sortingOrder - 1;
            }

            var color = r.actor.visual.color;
            if (scenePresentation != null)
            {
                var tint = scenePresentation.CharacterTint;
                color = new Color(r.baseColor.r * tint.r, r.baseColor.g * tint.g, r.baseColor.b * tint.b, color.a);
            }

            color.a =
                r.departingUntil > 0 ? Mathf.Clamp01((r.departingUntil - Time.unscaledTime) / .18f)
                : r.arrivingUntil > Time.unscaledTime ? Mathf.Clamp01(1 - (r.arrivingUntil - Time.unscaledTime) / .3f)
                : 1;
            r.actor.visual.color = color;
            if (index < activityLabels.Length && activityLabels[index] != null)
            {
                string label =
                    r.action == NPCDailyAction.Work ? "근무 중"
                    : r.action == NPCDailyAction.GetIngredients ? "식사 준비"
                    : r.action == NPCDailyAction.Cook ? "요리 중"
                    : r.action == NPCDailyAction.ServeMeal ? "상 차리는 중"
                    : r.action == NPCDailyAction.WashDishes ? "설거지 중"
                    : r.action == NPCDailyAction.Clean ? "청소 중"
                    : r.action == NPCDailyAction.WatchTV ? "TV 보는 중"
                    : r.action == NPCDailyAction.Wash ? "세수 중"
                    : r.action == NPCDailyAction.StockShelf ? "상품 정리 중"
                    : r.action == NPCDailyAction.Study ? "공부 중"
                    : r.action == NPCDailyAction.Eat ? "식사 중"
                    : r.action == NPCDailyAction.Shop ? "장보기"
                    : r.action == NPCDailyAction.Talk ? "이야기 중"
                    : r.action == NPCDailyAction.Sleep ? "잠든 시간"
                    : r.action == NPCDailyAction.Phone ? "휴대폰 확인"
                    : r.action == NPCDailyAction.Rest ? "쉬는 중"
                    : "";
                label = ChoreLabel(motion) ?? label;
                if (
                    HasChore(r)
                    && r.action is NPCDailyAction.Clean or NPCDailyAction.WashDishes
                    && ChoreLabel(motion) == null
                )
                {
                    label = r.taskStep == 0 ? "도구 준비 중" : "도구 정리 중";
                }
                Label(r, index, label);
                activityLabels[index].transform.position = r.actor.transform.position + Vector3.up * 2.1f;
                activityLabels[index].gameObject.SetActive(r.active && Time.unscaledTime < r.labelUntil && label != "");
            }
        }

#if UNITY_EDITOR
        public bool TryInspectActivity(string id, out NPCDailyAction action, out string anchor)
        {
            var resident = Array.Find(residents, r => r.definition.npcId == id);
            action = resident?.action ?? NPCDailyAction.Idle;
            anchor = resident?.anchor == null ? "—" : resident.anchor.name;
            return resident != null;
        }
#endif

        public NPCResidentSnapshot Inspect(string id)
        {
            Capture();
            return world.Service.State.residents.FirstOrDefault(n => n.id == id);
        }
    }
}
