using System;
using System.Collections;
using UnityEngine;

namespace IanGame
{
    public enum LifeActionVisual
    {
        AlignOnly,
        Bed,
        Dining,
        Appliance,
        Shower,
        Seat,
    }

    // A presentation adapter on existing interactables. LifeWorldController remains
    // the single action/effect owner; building doors are deliberately not involved.
    public sealed class WorldLifeAction : MonoBehaviour
    {
        public LifeActionVisual mode;
        public Transform approach;
        public Transform anchor;
        public Transform standUp;
        public Vector2 facing = Vector2.up;
        public ShowerBoothVisual booth;
        public SpriteRenderer overlay;
        public Sprite[] frames = Array.Empty<Sprite>();
        public WorldActivitySlot seat;
        public bool familyOwned;
        public string activePose;
        public bool restComputer;
        public Sprite mealProp;
        public Sprite emptyMealProp;
        public Sprite bookProp;
        public Sprite studyProp;
        public Sprite chairSprite;
        public bool tableOcclusion;
        SpriteRenderer chair;
        SpriteRenderer front;
        public Transform propAnchor;
        SpriteRenderer prop;
        public string Phase { get; private set; } = "Idle";
        public bool Ready { get; private set; }
        public bool Completed { get; private set; }
        public int Frame { get; private set; }

        bool care;
        bool infantCare;
        bool guardianBath;
        BedQuilt quilt;
        WorldPlayerMotor actor;
        WorldInteractable target;
        bool active;
        bool colliderWasEnabled;
        bool visualWasEnabled;
        SpriteRenderer furnitureVisual;
        public bool UsesExitAnchor => mode is LifeActionVisual.Bed or LifeActionVisual.Dining
            or LifeActionVisual.Seat or LifeActionVisual.Shower;

        WorldActivitySlot[] sharedSeats;
        WorldNPCNavigation approachRoutes;
        bool arrived;
        SeatBend bend;

        public bool Available => sharedSeats == null
            ? seat == null || string.IsNullOrEmpty(seat.Occupant)
            : Array.Exists(sharedSeats, s => s && s.Allows("player") && string.IsNullOrEmpty(s.Occupant));

        public void SetSeats(WorldActivitySlot[] seats)
        {
            sharedSeats = seats;
        }

        public void BindSeatAnchors(WorldInteractable obj)
        {
            if (mode is not (LifeActionVisual.Dining or LifeActionVisual.Seat) || sharedSeats != null)
            {
                return;
            }

            seat = seat ? seat : GetComponent<WorldActivitySlot>() ?? gameObject.AddComponent<WorldActivitySlot>();
            seat.seatAnchor = seat.seatAnchor ? seat.seatAnchor : anchor;
            seat.approachAnchor = seat.approachAnchor ? seat.approachAnchor : approach;
            seat.standUpAnchor = seat.standUpAnchor ? seat.standUpAnchor : standUp;
            seat.ConfigureSeat(anchor ? (Vector2)anchor.position : obj.Point,
                approach ? (Vector2)approach.position : obj.Point, facing, seat.Surface ? seat.Surface : obj.visual);
            if (chair && !seat.soleAnchor)
            {
                // A desk has a separate rendered chair. Use its upholstered seat
                // and feet for contact, independent of the player's standing height.
                var bounds = chair.bounds;
                var hip = new Vector2(bounds.center.x, bounds.min.y + bounds.size.y * .49f);
                seat.SetSeatContacts(hip, new Vector2(hip.x, bounds.min.y));
                seat.Surface = chair;
            }
            else if (mode == LifeActionVisual.Seat && seat.Surface)
            {
                FurnitureSeatPose.BindFrontChair(this, seat, seat.Surface);
            }
        }

        public WorldActivitySlot NearestSeat(Vector2 from)
        {
            if (sharedSeats == null)
            {
                return seat;
            }

            WorldActivitySlot selected = null;
            float nearest = float.PositiveInfinity;
            foreach (var candidate in sharedSeats)
            {
                if (!candidate || !candidate.Allows("player") || !string.IsNullOrEmpty(candidate.Occupant))
                {
                    continue;
                }
                float distance = (candidate.ApproachPoint - from).sqrMagnitude;
                if (distance < nearest)
                {
                    nearest = distance;
                    selected = candidate;
                }
            }
            return selected;
        }

        bool SelectSeat()
        {
            if (sharedSeats != null)
            {
                seat = NearestSeat(actor.Position);
                if (!seat)
                {
                    return false;
                }
            }

            if (seat && seat.seated && mode is LifeActionVisual.Dining or LifeActionVisual.Seat)
            {
                approach = seat.approachAnchor;
                anchor = seat.seatAnchor;
                standUp = seat.standUpAnchor;
                facing = seat.facing;
            }

            return !seat || seat.TryReserve("player");
        }

        IEnumerator WalkToFloor(LifeWorldController world, Vector2 floor, Func<bool> cancelled)
        {
            arrived = false;
            if (!actor.MovementAllowed || Vector2.Distance(actor.Position, floor) <= .05f)
            {
                arrived = mode == LifeActionVisual.Bed || Vector2.Distance(actor.Position, floor) <= .05f;
                yield break;
            }

            approachRoutes ??= new WorldNPCNavigation();
            var path = seat && seat.seated && floor == seat.ApproachPoint
                ? FurnitureSeatPose.ApproachRoute(approachRoutes, world, target.zone, actor.Position, seat)
                : approachRoutes.Find(world, target.zone, actor.Position, floor);
            if (path.Count == 0 || Vector2.Distance(path[path.Count - 1], floor) > .08f)
            {
                yield break;
            }

            foreach (Vector2 step in path)
            {
                if (Vector2.Distance(actor.Position, step) <= .05f)
                {
                    continue;
                }

                actor.BeginTraversal(step, 2.2f);
                while (actor.IsTraversing && actor.TraversalSeconds < 4 && !cancelled())
                {
                    yield return null;
                }

                bool reached = actor.TraversalArrived && !cancelled();
                actor.CancelTraversal();
                if (!reached)
                {
                    yield break;
                }
            }

            arrived = true;
        }

        IEnumerator SeatTransfer(bool sitting, Func<bool> cancelled)
        {
            float duration = sitting ? FurnitureSeatPose.SitSeconds : FurnitureSeatPose.StandSeconds;
            actor.Face(facing);
            if (sitting)
            {
                actor.advanced?.Render(ActorMotion.Idle, facing);
                bend = actor.GetComponent<SeatBend>() ?? actor.gameObject.AddComponent<SeatBend>();
                bend.Remember(actor.sprite, actor.sprite.sprite, actor.sprite.flipX);
            }

            actor.Pose(sitting ? "Sit" : "Stand Up");
            for (float time = 0; time < duration && !cancelled(); time += Time.deltaTime)
            {
                float progress = sitting ? time / duration : 1 - time / duration;
                ApplySeatOffset(progress);
                bend.Show(progress, seat);
                yield return null;
            }

            if (sitting)
            {
                bend.Show(1, seat);
            }
            else
            {
                bend.Hide();
            }

            ApplySeatOffset(sitting ? 1 : 0);
        }

        void ApplySeatOffset(float progress)
        {
            if (actor.growthVisual)
            {
                Vector2 offset = FurnitureSeatPose.Offset(actor.Position, seat.SolePoint, facing, progress);
                actor.growthVisual.ActionOffset = actor.transform.InverseTransformVector(offset);
            }
        }

        public IEnumerator Enter(LifeWorldController world, int action, Func<bool> cancelled)
        {
            EnsureFurnitureParts();
            var initialCancelled = cancelled;
            cancelled = () => initialCancelled() || !isActiveAndEnabled;
            Phase = "Approach";
            Ready = false;
            Completed = false;
            guardianBath = action == -4;
            care = mode == LifeActionVisual.Bed && action != 5;
            infantCare = care && world.Service.Game.growth.Age == 0;
            actor = world.Player;
            target = GetComponent<WorldInteractable>();
            BindSeatAnchors(target);
            if (!SelectSeat())
            {
                yield break;
            }

            active = true;
            furnitureVisual = target.visual;
            colliderWasEnabled = actor.GetComponent<CircleCollider2D>().enabled;
            visualWasEnabled = actor.sprite.enabled;
            var p = approach == null ? target.Point : (Vector2)approach.position;
            bool sitting = mode is LifeActionVisual.Dining or LifeActionVisual.Seat;
            if (sitting || mode == LifeActionVisual.Bed)
            {
                yield return WalkToFloor(world, p, cancelled);
                if (!arrived || cancelled())
                {
                    yield break;
                }
            }
            else if (actor.MovementAllowed && Vector2.Distance(actor.Position, p) > .05f)
            {
                actor.BeginTraversal(p, 2.2f);
                while (actor.IsTraversing && actor.TraversalSeconds < 5)
                {
                    if (cancelled())
                    {
                        yield break;
                    }

                    yield return null;
                }

                if (!actor.TraversalArrived)
                {
                    yield break;
                }
            }

            Phase = "Align";
            actor.Face(facing);
            actor.Pose(
                mode == LifeActionVisual.Bed ? "Sit"
                : mode == LifeActionVisual.Dining ? "Sit"
                : "Interact"
            );
            if (sitting || mode == LifeActionVisual.Bed)
            {
                seat?.MarkOccupied("player");
            }

            var growth = actor.growthVisual;
            if (sitting)
            {
                if (growth)
                {
                    var surface = seat.Surface ? seat.Surface : furnitureVisual;
                    growth.ActionOrder = surface.sortingOrder + 1;
                }

                if (front)
                {
                    front.enabled = tableOcclusion;
                }

                yield return SeatTransfer(true, cancelled);
                actor.Pose(mode == LifeActionVisual.Dining ? "Eat" : "SeatedIdle");
            }
            else if (growth && mode == LifeActionVisual.Bed)
            {
                growth.ActionOrder = furnitureVisual.sortingOrder + 3;
                actor.sprite.enabled = true;
                furnitureVisual.enabled = true;
                if (front != null)
                {
                    front.enabled = tableOcclusion;
                }

                if (mode == LifeActionVisual.Bed)
                {
                    growth.InBed = action != -4;
                    var destination =
                        action == -4 ? (Vector2)approach.position + Vector2.right * .55f : growth.SleepPoint(this);
                    if (actor.MovementAllowed && !care)
                    {
                        actor.Pose("Sit");
                        quilt = BedQuilt.For(target);
                        for (float t = 0; t < .25f && !cancelled(); t += Time.unscaledDeltaTime)
                        {
                            quilt.PlayerPose(actor, approach, 0, Mathf.SmoothStep(0, 1, t / .25f), false);
                            yield return null;
                        }
                        actor.Pose("Lie Down");
                        yield return BedTransfer(.58f, cancelled);
                    }
                    else if (action == -4 || actor.MovementAllowed)
                    {
                        actor.Pose(actor.MovementAllowed ? "Sit" : "Carried");
                        var from = growth.ActionOffset;
                        var to = destination - actor.Position;
                        for (float t = 0; t < .6f; t += Time.unscaledDeltaTime)
                        {
                            if (cancelled())
                            {
                                yield break;
                            }

                            growth.ActionOffset = Vector2.Lerp(from, to, Mathf.SmoothStep(0, 1, t / .6f));
                            yield return null;
                        }

                        growth.ActionOffset = to;
                    }
                    else
                    {
                        growth.ActionOffset = destination - actor.Position;
                    }

                    if (cancelled())
                    {
                        yield break;
                    }

                    actor.Pose(care ? growth.CarePose(action) : "Sleep");
                    if (!care)
                    {
                        yield return DrawCover(true, cancelled);
                    }
                }
            }
            else if (care && world.Service.Game.growth.Age == 0)
            {
                Show(action == 3 || action == 4 ? 13 : 14);
            }
            else if (mode == LifeActionVisual.Bed)
            {
                yield break;
            }
            else if (mode == LifeActionVisual.Appliance)
            {
                actor.Pose("Open Door");
                yield return Sequence(0, 3, .15f, cancelled);
                actor.Pose("Pick Up Item");
            }
            else if (mode == LifeActionVisual.Shower)
            {
                if (booth == null || !actor.MovementAllowed)
                {
                    yield break;
                }

                actor.GetComponent<CircleCollider2D>().enabled = false;
                actor.Pose("Open Door");
                yield return booth.Door(1, cancelled);
                if (cancelled())
                {
                    yield break;
                }

                actor.BeginTraversal(
                    actor.growthVisual == null ? (Vector2)anchor.position : actor.growthVisual.Inside(this),
                    1.7f
                );
                while (actor.IsTraversing && actor.TraversalSeconds < 4)
                {
                    if (cancelled())
                    {
                        yield break;
                    }

                    yield return null;
                }

                if (!actor.TraversalArrived)
                {
                    yield break;
                }

                actor.CancelTraversal();
                actor.Face(facing);
                yield return booth.Door(0, cancelled);
                if (cancelled())
                {
                    yield break;
                }

                actor.Pose("Shower");
                seat?.MarkOccupied("player");
                booth.WaterRunning = true;
            }

            Ready = !cancelled();
            Phase = Ready ? "Interacting" : "Exit";
            if (Ready && mode == LifeActionVisual.Dining)
            {
                ShowProp(mealProp);
            }

            if (Ready && mode == LifeActionVisual.Seat)
            {
                ShowProp((action == 8 || action == 14) && studyProp != null ? studyProp : bookProp);
            }
        }

        IEnumerator BedTransfer(float duration, Func<bool> cancelled)
        {
            for (float t = 0; t < duration && !cancelled(); t += Time.unscaledDeltaTime)
            {
                float blend = Mathf.SmoothStep(0, 1, t / duration);
                float lean = Phase == "Exit" ? 1 - blend : blend;
                actor.growthVisual.Recline = lean;
                quilt.PlayerPose(actor, approach, lean, 1, false);
                yield return null;
            }

            if (!cancelled())
            {
                actor.growthVisual.ActionOffset = Vector2.zero;
            }
        }

        IEnumerator DrawCover(bool pull, Func<bool> cancelled)
        {
            quilt = BedQuilt.For(target);
            float duration = actor.MovementAllowed ? .66f : .38f;
            for (float t = 0; t < duration && !cancelled(); t += Time.unscaledDeltaTime)
            {
                float amount = Mathf.SmoothStep(0, 1, t / duration);
                quilt.PlayerPose(actor, approach, 1, 1, false);
                quilt.Draw(actor.sprite, pull ? amount : 1 - amount);
                yield return null;
            }

            quilt.PlayerPose(actor, approach, 1, 1, pull);
            quilt.Draw(actor.sprite, pull ? 1 : 0);
        }

        public void Animate(float seconds)
        {
            if (!active)
            {
                return;
            }

            if (
                actor.growthVisual?.BodyInteractions == true
                && (mode == LifeActionVisual.Bed || mode == LifeActionVisual.Dining || mode == LifeActionVisual.Seat)
            )
            {
                return;
            }

            if (infantCare && frames.Length >= 16)
            {
                Show(13 + Mathf.FloorToInt(seconds * 3) % 2);
            }
            else if (mode == LifeActionVisual.Dining && !seat)
            {
                Show(8 + Mathf.FloorToInt(seconds * 4) % 4);
            }
        }

        void Show(int index)
        {
            if (overlay == null || frames == null || frames.Length == 0)
            {
                return;
            }

            Frame = Mathf.Clamp(index, 0, frames.Length - 1);
            overlay.sprite = frames[Frame];
            overlay.enabled = true;
            overlay.color = Color.white;
            overlay.sortingOrder = furnitureVisual.sortingOrder + 3;
            furnitureVisual.enabled = false;
            if (mode == LifeActionVisual.Appliance)
            {
                var bounds = furnitureVisual.bounds;
                float scale = bounds.size.y / overlay.sprite.bounds.size.y;
                overlay.transform.localScale = Vector3.one * scale;
                overlay.transform.position = new Vector3(
                    bounds.center.x - overlay.sprite.bounds.center.x * scale,
                    bounds.min.y - overlay.sprite.bounds.min.y * scale,
                    0
                );
            }

            if (mode == LifeActionVisual.Bed || mode == LifeActionVisual.Dining || mode == LifeActionVisual.Seat)
            {
                actor.sprite.enabled = false;
                if (actor.contactShadow != null)
                {
                    actor.contactShadow.enabled = false;
                }
            }
        }

        IEnumerator Sequence(int start, int end, float seconds, Func<bool> cancelled)
        {
            for (int i = start; i <= end; i++)
            {
                Show(i);
                yield return Wait(seconds, cancelled);
                if (cancelled())
                {
                    yield break;
                }
            }
        }

        static IEnumerator Wait(float seconds, Func<bool> cancelled)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (cancelled())
                {
                    yield break;
                }

                yield return null;
            }
        }

        public IEnumerator Exit(LifeWorldController world, Func<bool> cancelled)
        {
            Phase = "Exit";
            if (mode == LifeActionVisual.Dining)
            {
                ShowProp(emptyMealProp);
            }

            if (mode is LifeActionVisual.Seat or LifeActionVisual.Dining)
            {
                yield return SeatTransfer(false, cancelled);
                if (!cancelled() && standUp != approach)
                {
                    yield return WalkToFloor(world, seat.StandUpPoint, cancelled);
                }

                ResetVisual();
                Completed = !cancelled() && arrived;
                yield break;
            }

            if (actor.growthVisual && mode == LifeActionVisual.Bed)
            {
                if (mode == LifeActionVisual.Bed && !actor.MovementAllowed && guardianBath)
                {
                    var growth = actor.growthVisual;
                    actor.Pose("Carried");
                    var from = growth.ActionOffset;
                    var to = growth.SleepPoint(this) - actor.Position;
                    for (float t = 0; t < .6f; t += Time.unscaledDeltaTime)
                    {
                        if (cancelled())
                        {
                            yield break;
                        }

                        growth.ActionOffset = Vector2.Lerp(from, to, Mathf.SmoothStep(0, 1, t / .6f));
                        yield return null;
                    }
                }

                if (mode == LifeActionVisual.Bed && !care)
                {
                    yield return DrawCover(false, cancelled);
                    actor.Pose("Wake Up");
                    if (actor.MovementAllowed)
                    {
                        yield return BedTransfer(.62f, cancelled);
                        actor.Pose("Sit");
                        for (float t = 0; t < .25f && !cancelled(); t += Time.unscaledDeltaTime)
                        {
                            quilt.PlayerPose(actor, approach, 0, 1 - Mathf.SmoothStep(0, 1, t / .25f), false);
                            yield return null;
                        }

                        quilt.Release(actor.sprite);
                    }

                    if (!cancelled())
                    {
                        actor.Pose(actor.MovementAllowed ? "Stand Up" : "Wake Up");
                        yield return Wait(.24f, cancelled);
                    }

                    if (actor.MovementAllowed && standUp && standUp != approach && !cancelled())
                    {
                        yield return WalkToFloor(world, standUp.position, cancelled);
                    }

                    ResetVisual();
                    Completed = !cancelled() && arrived;
                    yield break;
                }

                if (!infantCare || guardianBath)
                {
                    actor.Pose(mode == LifeActionVisual.Bed ? "Wake Up" : "Stand Up");
                    yield return Wait(
                        Mathf.Max(
                            .45f,
                            actor.advanced.Duration(
                                mode == LifeActionVisual.Bed ? ActorMotion.WakeUp : ActorMotion.StandUp,
                                facing
                            )
                        ),
                        cancelled
                    );
                }

                if (cancelled())
                {
                    yield break;
                }

                actor.growthVisual.InBed = actor.growthVisual.Seated = false;
                actor.Pose("Idle");
                if (actor.MovementAllowed)
                {
                    actor.BeginTraversal(standUp.position, 1.7f);
                    while (actor.IsTraversing && actor.TraversalSeconds < 5)
                    {
                        if (cancelled())
                        {
                            yield break;
                        }

                        yield return null;
                    }

                    if (!actor.TraversalArrived)
                    {
                        yield break;
                    }

                    actor.CancelTraversal();
                }

                ResetVisual();
                Completed = !cancelled();
                yield break;
            }

            if (care && !infantCare)
            {
                ResetVisual();
                Completed = !cancelled();
                yield break;
            }

            if (mode == LifeActionVisual.Appliance)
            {
                for (int i = 2; i >= 0; i--)
                {
                    Show(i);
                    yield return Wait(.15f, cancelled);
                }
            }

            if (mode == LifeActionVisual.Shower)
            {
                booth.WaterRunning = false;
                actor.Pose("Open Door");
                yield return booth.Door(1, cancelled);
                if (cancelled())
                {
                    yield break;
                }

                actor.Pose("Idle");
                actor.BeginTraversal(standUp.position, 1.7f);
                while (actor.IsTraversing && actor.TraversalSeconds < 4)
                {
                    if (cancelled())
                    {
                        yield break;
                    }

                    yield return null;
                }

                if (!actor.TraversalArrived)
                {
                    yield break;
                }

                actor.CancelTraversal();
                yield return booth.Door(0, cancelled);
                ResetVisual();
                Completed = !cancelled();
                yield break;
            }

            ResetVisual();
            Completed = !cancelled();
        }

        public void ResetVisual()
        {
            if (!active)
            {
                return;
            }

            bend?.Hide();
            active = false;
            Ready = false;
            Phase = "Idle";
            if (front != null)
            {
                front.enabled = false;
            }

            if (prop != null)
            {
                prop.enabled = false;
            }

            if (overlay != null)
            {
                overlay.enabled = false;
            }

            if (booth != null)
            {
                booth.ResetClosed();
            }

            if (furnitureVisual != null)
            {
                furnitureVisual.enabled = mode != LifeActionVisual.Shower;
            }

            if (actor != null)
            {
                quilt?.Release(actor.sprite);
                actor.growthVisual?.ClearAction();
                actor.sprite.enabled = visualWasEnabled;
                actor.CancelTraversal();
                actor.GetComponent<CircleCollider2D>().enabled = colliderWasEnabled;
                if (actor.contactShadow != null)
                {
                    actor.contactShadow.enabled = true;
                }
            }

            seat?.Release("player");
        }

        void Start()
        {
            EnsureFurnitureParts();
        }

        void EnsureFurnitureParts()
        {
            var target = GetComponent<WorldInteractable>();
            if (target == null || target.visual == null)
            {
                return;
            }

            if (mode == LifeActionVisual.Bed)
            {
                quilt = BedQuilt.For(target);
                seat = seat ? seat : GetComponent<WorldActivitySlot>() ?? gameObject.AddComponent<WorldActivitySlot>();
            }

            if (chairSprite != null && chair == null && anchor != null)
            {
                var go = new GameObject("Interaction chair", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                chair = go.GetComponent<SpriteRenderer>();
                chair.sprite = chairSprite;
                chair.sharedMaterial = target.visual.sharedMaterial;
                chair.sortingOrder = target.visual.sortingOrder + 1;
                chair.transform.position = anchor.position;
                chair.transform.localScale = Vector3.one * (.85f / chairSprite.bounds.size.y);
                if (mode == LifeActionVisual.Dining)
                {
                    var feet = go.AddComponent<BoxCollider2D>();
                    var bounds = chairSprite.bounds;
                    float depth = bounds.size.y * .22f;
                    feet.size = new Vector2(bounds.size.x * .78f, depth);
                    feet.offset = new Vector2(bounds.center.x, bounds.min.y + depth * .5f);
                }
            }

            if (tableOcclusion && front == null)
            {
                var go = new GameObject("Furniture foreground", typeof(SpriteRenderer));
                go.transform.SetParent(target.visual.transform, false);
                front = go.GetComponent<SpriteRenderer>();
                front.sprite = target.visual.sprite;
                front.sharedMaterial = target.visual.sharedMaterial;
                front.sortingOrder = target.visual.sortingOrder + 4;
                front.enabled = false;
            }
        }

        void ShowProp(Sprite sprite)
        {
            // The shared dining sprite already has a place setting at each chair.
            if (sprite == null || (mode == LifeActionVisual.Dining && seat && seat.Surface != furnitureVisual))
            {
                return;
            }

            if (prop == null)
            {
                var go = new GameObject("Life action prop", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                prop = go.GetComponent<SpriteRenderer>();
                prop.sharedMaterial = furnitureVisual.sharedMaterial;
            }

            prop.sprite = sprite;
            prop.enabled = true;
            prop.sortingOrder = furnitureVisual.sortingOrder + (mode == LifeActionVisual.Seat ? 2 : 6);
            float scale = (mode == LifeActionVisual.Seat && sprite != studyProp ? .65f : .32f) / sprite.bounds.size.y;
            prop.transform.localScale = Vector3.one * scale;
            prop.transform.position =
                propAnchor != null
                    ? propAnchor.position
                    : (Vector3)new Vector2(furnitureVisual.bounds.center.x, furnitureVisual.bounds.center.y + .15f);
        }

        public static void CopyTo(WorldInteractable target, WorldLifeAction source)
        {
            if (source == null)
            {
                return;
            }

            var a = target.gameObject.AddComponent<WorldLifeAction>();
            a.mode = source.mode;
            a.activePose = source.activePose;
            a.restComputer = source.restComputer;
            if (a.restComputer)
            {
                target.kind = WorldInteractionKind.Rest;
                target.title = "컴퓨터로 쉬기";
            }

            a.frames = source.frames;
            a.mealProp = source.mealProp;
            a.emptyMealProp = source.emptyMealProp;
            a.bookProp = source.bookProp;
            a.studyProp = source.studyProp;
            a.chairSprite = source.chairSprite;
            a.tableOcclusion = source.tableOcclusion;
            a.facing = source.facing;
            a.approach = new GameObject("InteractionApproach").transform;
            a.approach.SetParent(target.transform, false);
            a.approach.localPosition = source.approach == null ? target.UsePoint : source.approach.localPosition;
            a.standUp = a.approach;
            a.anchor = new GameObject("ActionAnchor").transform;
            a.anchor.SetParent(target.transform, false);
            a.anchor.localPosition = source.anchor.localPosition;
            if (source.propAnchor != null)
            {
                a.propAnchor = new GameObject("PropAnchor").transform;
                a.propAnchor.SetParent(target.transform, false);
                a.propAnchor.localPosition = source.propAnchor.localPosition;
            }

            if (a.mode == LifeActionVisual.Seat || a.mode == LifeActionVisual.Dining)
            {
                a.seat = target.gameObject.AddComponent<WorldActivitySlot>();
                a.seat.facing = a.facing;
                a.BindSeatAnchors(target);
            }

            var overlay = new GameObject("Life action occupied", typeof(SpriteRenderer));
            overlay.transform.SetParent(target.transform, false);
            a.overlay = overlay.GetComponent<SpriteRenderer>();
            a.overlay.sharedMaterial = target.visual.sharedMaterial;
            a.overlay.enabled = false;
        }

        void OnDisable()
        {
            ResetVisual();
        }
    }
}
