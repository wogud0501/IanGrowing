using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace IanGame
{
    public sealed partial class WorldNPCScheduler
    {
        public bool sceneScoped;
        public WorldInteractable[] residentPrefabs = Array.Empty<WorldInteractable>();
        public TMP_Text labelPrefab;
        bool restoringScoped;
        float nextScoped;

        public bool HasPhysicalActor(string id) =>
            sceneScoped
            && residents.Any(r => r.definition.npcId == id && r.actor != null && r.actor.zone == world.Room.roomId);

        void StartScoped()
        {
            street = world.GetComponentInChildren<WorldStreetLife>();
            hitFilter = new ContactFilter2D { useTriggers = false };
            ready = true;
            world.Service.Restored += Restore;
            RebuildScoped(true);
        }

        void RebuildScoped(bool restoring, bool timeJump = false)
        {
            foreach (var r in residents)
            {
                r.entryDoor?.Release(r.definition.npcId);
                r.door?.Release(r.definition.npcId);
                r.interiorDoor?.Release(r.definition.npcId);
                r.slot?.Release(r.definition.npcId);
                r.departureSlot?.Release(r.definition.npcId);
                if (r.actor != null)
                {
                    world.objects = world.objects.Where(o => o != r.actor).ToArray();
                    ReleaseScopedActor(r);
                }
            }

            foreach (var label in activityLabels)
            {
                if (label != null)
                {
                    Destroy(label.gameObject);
                }
            }

            actors = Array.Empty<WorldInteractable>();
            activityLabels = Array.Empty<TMP_Text>();
            residents = Array.Empty<Resident>();
            navigation.Clear();
            world.Service.AdvanceAbsentResidents(materialize: true, timeJump: timeJump);
            RefreshPopulationBudget();
            var room = world.Room.roomId;
            foreach (var n in world.Service.State.residents.Where(n => n.zone == room).ToArray())
            {
                SpawnScoped(n, true);
            }

            restoringScoped = true;
            try
            {
                Restore();
            }
            finally
            {
                restoringScoped = false;
            }
        }

        void SpawnScoped(NPCResidentSnapshot saved, bool loading)
        {
            var d = world.Service.rules.npcs.FirstOrDefault(d => d.npcId == saved.id);
            var template = residentPrefabs.FirstOrDefault(a => a.npcId == saved.id);
            if (
                d == null
                || template == null
                || !AdmitResident(d)
                || residents.Any(r => r.definition.npcId == saved.id)
            )
            {
                return;
            }

            string local = world.Room.roomId;
            var target = d.schedule[
                Schedule(d, world.Service.Game.clock.Day, (int)(world.Service.Game.clock.SubDaySeconds / 60))
            ];
            var p =
                loading && saved.zone == local
                    ? world.CurrentZone.origin + saved.position
                    : world.CurrentZone.origin + world.CurrentZone.spawn;
            WorldDoor entryDoor = null;
            if (!loading)
            {
                var portal = world.Room.catalog.EntryFrom(local, saved.zone);
                var o = portal == null ? null : Object(portal.doorId);
                if (o != null)
                {
                    var incoming = world.Room.catalog.portals.FirstOrDefault(e =>
                        e.sourceRoom == portal.destinationRoom && e.destinationRoom == local
                    );
                    p = world.Safe(world.CurrentZone, incoming?.arrival ?? (o.Point - world.CurrentZone.origin));
                    entryDoor = o.GetComponent<WorldDoor>();
                    if (entryDoor != null)
                    {
                        p = entryDoor.CrossPoint;
                    }
                }
            }

            if (entryDoor == null && !SpawnPoint(ref p))
            {
                return;
            }

            if (
                !loading
                && residents.Any(r =>
                    r.active && r.actor != null && Vector2.Distance(r.actor.transform.position, p) < 0.42f
                )
            )
            {
                return;
            }

            if (
                entryDoor != null
                && (
                    Vector2.Distance(p, world.Player.Position) < .5f
                    || !entryDoor.Acquire(
                        saved.id,
                        world.Service.Hour,
                        out _,
                        world.CurrentZone.outdoor
                            && world.Service.rules.Zone(entryDoor.portal.targetZone)?.outdoor == false
                    )
                )
            )
            {
                return;
            }

            var a = AcquireScopedActor(template, d);
            a.name = d.npcId;
            a.zone = local;
            a.transform.position = p;
            a.visual.enabled = true;
            if (a.shadow != null)
            {
                a.shadow.enabled = true;
            }

            world.objects = world.objects.Concat(new[] { a }).ToArray();
            actors = actors.Concat(new[] { a }).ToArray();
            var label = Instantiate(labelPrefab, world.transform);
            label.name = d.npcId + " activity";
            label.text = "";
            label.gameObject.SetActive(false);
            activityLabels = activityLabels.Concat(new[] { label }).ToArray();
            var r = new Resident
            {
                actor = a,
                definition = d,
                animation = a.GetComponent<WorldActorAnimation>(),
                baseColor = a.visual.color,
                target = target,
                entryDoor = entryDoor,
                arrivingUntil = loading ? 0 : Time.unscaledTime + .3f,
                active = true,
                index = -1,
                used = saved.completedUseKey,
                action = loading ? saved.action : NPCDailyAction.Walk,
            };
            residents = residents.Concat(new[] { r }).ToArray();
            saved.passingRoom = null;
            saved.zone = a.zone;
            saved.position = p - world.CurrentZone.origin;
            saved.action = r.action;
            nextDecision = 0;
        }

        void RefreshScoped()
        {
            Capture();
            RefreshPopulationBudget();
            foreach (var resident in residents)
            {
                if (
                    resident.definition.ambient
                    && !AdmitResident(resident.definition)
                    && OutsidePopulationView(resident.actor.transform.position)
                )
                {
                    resident.actor.zone = resident.definition.homeZone;
                    var saved = world.Service.State.residents.Find(n => n.id == resident.definition.npcId);
                    if (saved != null)
                    {
                        saved.zone = resident.definition.homeZone;
                        saved.action = NPCDailyAction.Rest;
                    }
                }
            }

            foreach (var r in residents.Where(r => r.actor.zone != world.Room.roomId).ToArray())
            {
                r.slot?.Release(r.definition.npcId);
                r.departureSlot?.Release(r.definition.npcId);
                r.entryDoor?.Release(r.definition.npcId);
                r.door?.Release(r.definition.npcId);
                r.interiorDoor?.Release(r.definition.npcId);
                world.objects = world.objects.Where(o => o != r.actor).ToArray();
                actors = actors.Where(a => a != r.actor).ToArray();
                int i = Array.IndexOf(residents, r);
                if (i < activityLabels.Length)
                {
                    if (activityLabels[i] != null)
                    {
                        Destroy(activityLabels[i].gameObject);
                    }

                    activityLabels = activityLabels.Where((l, index) => index != i).ToArray();
                }

                ReleaseScopedActor(r);
                residents = residents.Where(x => x != r).ToArray();
            }

            world.Service.AdvanceAbsentResidents();
            int arrivals = 0;
            foreach (var n in world.Service.State.residents.ToArray())
            {
                var d = world.Service.rules.npcs.FirstOrDefault(d => d.npcId == n.id);
                if (d == null || residents.Any(r => r.definition.npcId == n.id))
                {
                    continue;
                }

                var target = d.schedule[
                    Schedule(d, world.Service.Game.clock.Day, (int)(world.Service.Game.clock.SubDaySeconds / 60))
                ];
                if (
                    n.zone == world.Room.roomId
                    || target.zone == world.Room.roomId
                    || n.passingRoom == world.Room.roomId
                )
                {
                    if (lifeProfile != null && arrivals >= lifeProfile.maxArrivalsPerTick)
                    {
                        break;
                    }

                    int before = residents.Length;
                    SpawnScoped(n, false);
                    if (residents.Length > before)
                    {
                        arrivals++;
                    }
                }
            }
        }
    }
}
