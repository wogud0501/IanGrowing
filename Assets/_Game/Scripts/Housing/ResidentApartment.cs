using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    public sealed class ResidentApartment : MonoBehaviour
    {
        public WorldDoor interiorDoor;

        public void Furnish(LifeWorldController world, RoomSceneDefinition unit)
        {
            world.Room.roomId = unit.roomId;
            var plan = Instantiate(unit.floorPlan, transform);
            plan.transform.position = world.Room.rules.Zone(unit.roomId).origin;
            if (plan.keepsake && unit.keepsake)
            {
                plan.keepsake.sprite = unit.keepsake;
            }

            var fixtures = new List<WorldInteractable>(world.objects);
            var people = world.Room.rules.npcs.Where(n => !n.ambient && n.homeZone == unit.roomId)
                .ToDictionary(n => n.homePlace);

            foreach (var item in fixtures)
            {
                item.zone = unit.roomId;
            }

            foreach (var item in plan.GetComponentsInChildren<WorldInteractable>())
            {
                item.zone = unit.roomId;
                var slot = item.GetComponent<WorldActivitySlot>();
                if (slot && int.TryParse(item.id.Substring(item.id.LastIndexOf('-') + 1), out int index))
                {
                    if (!people.TryGetValue(index, out var person))
                    {
                        item.gameObject.SetActive(false);
                        continue;
                    }

                    slot.staffId = person.npcId;
                    string bedId = person.residentBedId;
                    if (item.kind == WorldInteractionKind.Sleep)
                    {
                        item.id = bedId;
                        item.title = person.title + "의 침대";
                        var bed = BedQuilt.For(item);
                        item.UsePoint = bed.approachAnchor.position - item.transform.position;
                        slot.facing = -item.UsePoint.normalized;
                    }
                    else if (item.kind == WorldInteractionKind.Rest)
                    {
                        item.id = bedId + "-rest";
                    }
                }

                fixtures.Add(item);
            }

            for (int i = 0; i < plan.doorways.Length; i++)
            {
                var threshold = plan.doorways[i];
                var door = Instantiate(interiorDoor, threshold);
                door.transform.localPosition = -(Vector3)door.threshold;
                door.world = world;
                door.portal.zone = unit.roomId;
                door.portal.targetZone = unit.roomId;
                door.portal.id = unit.roomId + "-door-" + i;
                door.portal.title = threshold.name;
                fixtures.Add(door.portal);
            }

            world.objects = fixtures.ToArray();
            Physics2D.SyncTransforms();
        }
    }
}
