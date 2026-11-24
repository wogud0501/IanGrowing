using System.Collections.Generic;
using UnityEngine;

namespace IanGame
{
    // Anchors describe the furniture. Only the character renderer travels from the
    // floor to the cushion. Navigation, save positions and occupancy stay on the floor.
    public static class FurnitureSeatPose
    {
        public const float SitSeconds = .72f;
        public const float StandSeconds = .64f;

        public static Vector2 Offset(Vector2 feet, Vector2 cushion, Vector2 facing, float progress)
        {
            float bend = Mathf.Clamp01(progress);
            float transfer = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.16f, .92f, bend));
            float weight = Mathf.Sin(bend * Mathf.PI);
            return (cushion - feet) * transfer + facing.normalized * (.055f * weight)
                + Vector2.down * (.12f * weight);
        }

        public static List<Vector2> ApproachRoute(WorldNPCNavigation navigation, LifeWorldController world,
            string zone, Vector2 start, WorldActivitySlot slot)
        {
            Vector2 floor = slot.ApproachPoint;
            Vector2 front = slot.facing.normalized;
            Vector2 lead = floor + front * .48f;
            // A direct approach is fine for someone already standing in the front lane.
            // From the side or back, enter the lane before turning toward the furniture.
            var relative = start - floor;
            bool inLane = Vector2.Dot(relative, front) >= -.08f
                && Mathf.Abs(relative.x * front.y - relative.y * front.x) < .38f;
            if (!inLane && slot.soleAnchor && WorldNPCNavigation.StaticFree(lead)
                && !WorldNPCNavigation.Obstacle(lead, floor, planning: true))
            {
                var route = navigation.Find(world, zone, start, lead);
                if (route.Count > 0)
                {
                    route.Add(floor);
                    return route;
                }
            }

            return navigation.Find(world, zone, start, floor);
        }

        public static void BindFrontChair(WorldLifeAction action, WorldActivitySlot slot, SpriteRenderer visual)
        {
            var sprite = visual.sprite;
            if (!sprite || (sprite.name != "Chair" && sprite.name != "Settee"))
            {
                return;
            }

            // Work in sprite space so contact follows the installed furniture even
            // before its renderer has drawn a frame. The seam is between two cushions.
            bool twoPlaces = sprite.name == "Settee";
            var bounds = sprite.bounds;
            SetContacts(slot, twoPlaces ? .28f : .5f);
            if (twoPlaces)
            {
                var root = new GameObject("Right cushion");
                root.transform.SetParent(slot.transform, false);
                var right = root.AddComponent<WorldActivitySlot>();
                right.ConfigureSeat(slot.Point, slot.ApproachPoint, slot.facing, visual);
                SetContacts(right, .72f);
                action.SetSeats(new[] { slot, right });
            }

            void SetContacts(WorldActivitySlot place, float across)
            {
                float x = bounds.min.x + bounds.size.x * across;
                float level = twoPlaces ? .375f : .49f;
                Vector2 hip = visual.transform.TransformPoint(new Vector3(x, bounds.min.y + bounds.size.y * level));
                Vector2 sole = visual.transform.TransformPoint(new Vector3(x, bounds.min.y));
                place.SetSeatContacts(hip, sole + Vector2.down * .035f);
                if (twoPlaces)
                {
                    place.SetFloor(new Vector2(hip.x, Mathf.Min(sole.y - .34f, place.ApproachPoint.y)));
                }
            }
        }

        public static void BindRoom(LifeWorldController world)
        {
            var seats = new Dictionary<SpriteRenderer, List<WorldActivitySlot>>();
            foreach (var table in world.GetComponentsInChildren<FamilySharedDiningPresentation>(true))
            {
                table.BindChairs();
            }

            foreach (var slot in world.GetComponentsInChildren<WorldActivitySlot>(true))
            {
                var station = slot.GetComponent<ResidentActivityStation>();
                if (slot.TryGetComponent<WorldWorkstationPresentation>(out var desk))
                {
                    slot.ConfigureSeat(slot.Point, slot.Point + Vector2.right * .63f, slot.facing, desk.empty);
                    slot.activityMotion = ActorMotion.UseComputer;
                    // The screen and keyboard are in front of the operator; the
                    // backrest is behind. Both contacts are on the chair's centerline.
                    var bounds = desk.empty.bounds;
                    float hip = bounds.min.y + bounds.size.y * .52f;
                    float soles = bounds.min.y + bounds.size.y * .20f;
                    slot.SetSeatContacts(new Vector2(bounds.center.x, hip), new Vector2(bounds.center.x, soles));
                    slot.AddForeground(desk.empty, .575f);
                }
                else if (slot.seated && slot.TryGetComponent<WorldFacilityActivityPresentation>(out var facility))
                {
                    slot.Surface = facility.empty;
                    if (slot.activityMotion == ActorMotion.Eat)
                    {
                        // The diner sits on the far side of the table. The original
                        // anchor was on the tabletop's near edge.
                        var table = facility.empty.bounds;
                        Vector2 chair = new Vector2(table.center.x, table.min.y + table.size.y * .344f);
                        slot.SetSeatContacts(chair + Vector2.up * .34f, chair);
                        slot.SetFloor(chair + new Vector2(.87f, .61f));
                        slot.AddForeground(facility.empty, .53f);
                    }
                    else if (slot.activityMotion == ActorMotion.UseComputer && slot.facing.y > .6f)
                    {
                        slot.activityMotion = ActorMotion.Study;
                        var bounds = facility.empty.bounds;
                        float center = bounds.min.x + bounds.size.x * .466f;
                        slot.SetSeatContacts(new Vector2(center, bounds.min.y + bounds.size.y * .34f),
                            new Vector2(center, bounds.min.y + bounds.size.y * .16f));
                        slot.AddForeground(facility.empty, .465f);
                    }
                }

                if (!station || !station.seatSurface || !slot.seated)
                {
                    continue;
                }

                slot.Surface = station.seatSurface;
                if (!slot.soleAnchor && station.breakSeat && slot.facing.y < -.6f)
                {
                    // Each cushion has its own front lane even when the author placed
                    // the old arrival marker beside a distant armrest.
                    var front = new Vector2(slot.Point.x, slot.Surface.bounds.min.y - .34f);
                    if (Vector2.Distance(front, slot.Point) < 1.7f)
                    {
                        slot.SetFloor(front);
                        var bounds = slot.Surface.bounds;
                        float cushionHeight = slot.Surface.sprite.name == "Chair" ? .49f : .375f;
                        var hip = new Vector2(front.x, bounds.min.y + bounds.size.y * cushionHeight);
                        slot.SetSeatContacts(hip, new Vector2(front.x, bounds.min.y - .045f));
                    }
                }

                if (!seats.TryGetValue(slot.Surface, out var group))
                {
                    group = new List<WorldActivitySlot>();
                    seats.Add(slot.Surface, group);
                }

                group.Add(slot);
            }

            foreach (var obj in world.objects)
            {
                if (!obj || !obj.enabled || !obj.visual)
                {
                    continue;
                }

                var action = obj.GetComponent<WorldLifeAction>();
                if (obj.kind == WorldInteractionKind.Sleep && !action)
                {
                    action = obj.gameObject.AddComponent<WorldLifeAction>();
                    action.mode = LifeActionVisual.Bed;
                    var bedding = BedQuilt.For(obj);
                    action.approach = bedding.approachAnchor ? bedding.approachAnchor : FloorAnchor(obj);
                    action.standUp = bedding.exitAnchor ? bedding.exitAnchor : action.approach;
                    action.anchor = bedding.sitAnchor ? bedding.sitAnchor : obj.visual.transform;
                }

                if (!action && obj.kind == WorldInteractionKind.Meal
                    && obj.TryGetComponent<WorldActivitySlot>(out var dining) && dining.seated && dining.Allows("player"))
                {
                    action = obj.gameObject.AddComponent<WorldLifeAction>();
                    action.mode = LifeActionVisual.Dining;
                    action.SetSeats(new[] { dining });
                }

                if (obj.kind == WorldInteractionKind.Rest && seats.TryGetValue(obj.visual, out var group))
                {
                    action = action ? action : obj.gameObject.AddComponent<WorldLifeAction>();
                    action.mode = LifeActionVisual.Seat;
                    action.activePose = "SeatedIdle";
                    action.SetSeats(group.ToArray());
                }
                else if (!action && obj.id is "living-settee" or "finance-waiting" or "realestate-waiting" or "writing-chair")
                {
                    action = obj.gameObject.AddComponent<WorldLifeAction>();
                    action.mode = LifeActionVisual.Seat;
                    action.activePose = "SeatedIdle";
                    action.facing = Vector2.down;
                    action.approach = FloorAnchor(obj);
                }

                action?.BindSeatAnchors(obj);
            }
        }

        static Transform FloorAnchor(WorldInteractable obj)
        {
            var floor = new GameObject("Standing point").transform;
            floor.SetParent(obj.transform, false);
            floor.position = obj.Point;
            return floor;
        }
    }
}
