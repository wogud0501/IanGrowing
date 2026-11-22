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
        public Sprite[] frames;
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
        public bool Available => seat == null || string.IsNullOrEmpty(seat.Occupant);

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
            if (seat != null && !seat.TryReserve("player"))
            {
                yield break;
            }

            active = true;
            furnitureVisual = target.visual;
            colliderWasEnabled = actor.GetComponent<CircleCollider2D>().enabled;
            visualWasEnabled = actor.sprite.enabled;
            var p = approach == null ? target.Point : (Vector2)approach.position;
            if (actor.MovementAllowed && Vector2.Distance(actor.Position, p) > .05f)
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
            if (mode == LifeActionVisual.Bed || mode == LifeActionVisual.Dining || mode == LifeActionVisual.Seat)
            {
                actor.GetComponent<CircleCollider2D>().enabled = false;
                seat?.MarkOccupied("player");
            }

            var growth = actor.growthVisual;
            if (
                growth != null
                && growth.BodyInteractions
                && (mode == LifeActionVisual.Bed || mode == LifeActionVisual.Dining || mode == LifeActionVisual.Seat)
            )
            {
                actor.GetComponent<CircleCollider2D>().enabled = false;
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
                        yield return BedTransfer(destination, .58f, cancelled);
                    }
                    else if (actor.MovementAllowed)
                    {
                        actor.BeginTraversal(destination, 1.7f);
                        while (actor.IsTraversing && actor.TraversalSeconds < 5 && !cancelled())
                        {
                            yield return null;
                        }

                        if (!actor.TraversalArrived)
                        {
                            yield break;
                        }

                        actor.CancelTraversal();
                    }
                    else if (action == -4)
                    {
                        actor.Pose("Carried");
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
                else
                {
                    actor.Pose("Sit");
                    yield return Wait(Mathf.Max(.25f, actor.advanced.Duration(ActorMotion.SitDown, facing)), cancelled);
                    actor.BeginTraversal(growth.Inside(this), 1.7f);
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
                    growth.Seated = true;
                    actor.Face(facing);
                    actor.Pose(mode == LifeActionVisual.Dining ? "Eat" : "SeatedIdle");
                }
            }
            else if (care && world.Service.Game.growth.Age == 0)
            {
                Show(action == 3 || action == 4 ? 13 : 14);
            }
            else if (mode == LifeActionVisual.Bed && !care)
            {
                int start = world.Service.Game.growth.Age == 0 && frames.Length >= 16 ? 12 : 0;
                yield return Sequence(start, Math.Min(start + 2, frames.Length - 1), .2f, cancelled);
                if (cancelled())
                {
                    yield break;
                }

                actor.Warp(anchor == null ? p : (Vector2)anchor.position);
                Show(start == 12 ? 15 : 3);
            }
            else if (mode == LifeActionVisual.Dining)
            {
                yield return Wait(.35f, cancelled);
                if (cancelled())
                {
                    yield break;
                }

                actor.Warp(anchor == null ? p : (Vector2)anchor.position);
                Show(8);
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

        IEnumerator BedTransfer(Vector2 destination, float duration, Func<bool> cancelled)
        {
            Vector2 from = actor.Position;
            for (float t = 0; t < duration && !cancelled(); t += Time.unscaledDeltaTime)
            {
                float blend = Mathf.SmoothStep(0, 1, t / duration);
                actor.growthVisual.ActionOffset = (destination - from) * blend;
                float lean = Phase == "Exit" ? 1 - blend : blend;
                actor.growthVisual.Recline = lean;
                quilt.PlayerPose(actor, approach, lean, 1, false);
                yield return null;
            }

            if (!cancelled())
            {
                actor.Warp(destination);
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
            else if (mode == LifeActionVisual.Dining)
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

            if (
                actor?.growthVisual != null
                && actor.growthVisual.BodyInteractions
                && (mode == LifeActionVisual.Bed || mode == LifeActionVisual.Dining || mode == LifeActionVisual.Seat)
            )
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
                        yield return BedTransfer(standUp != null ? (Vector2)standUp.position : target.Point,
                            .62f, cancelled);
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

                    ResetVisual();
                    Completed = !cancelled();
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

            if (mode == LifeActionVisual.Bed && !care)
            {
                world.Player.Pose("Wake Up");
                if (world.Service.Game.growth.Age == 0)
                {
                    Show(12);
                    yield return Wait(.5f, cancelled);
                }
                else
                {
                    yield return Sequence(4, 7, .18f, cancelled);
                }
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

            bool leaving =
                mode == LifeActionVisual.Bed || mode == LifeActionVisual.Dining || mode == LifeActionVisual.Seat;
            var destination = standUp == null ? target.Point : (Vector2)standUp.position;
            if (leaving && !cancelled())
            {
                if (world.Service.Game.growth.Age == 0)
                {
                    actor.Warp(destination);
                }
                else
                {
                    if (mode == LifeActionVisual.Bed)
                    {
                        actor.Warp((Vector2)transform.position + Vector2.down * .05f);
                    }

                    ResetVisual();
                    actor.GetComponent<CircleCollider2D>().enabled = false;
                    actor.Pose(mode == LifeActionVisual.Dining ? "Stand Up" : "Idle");
                    yield return Wait(mode == LifeActionVisual.Dining ? .45f : .1f, cancelled);
                    if (!cancelled())
                    {
                        actor.BeginTraversal(destination, 1.7f);
                        while (actor.IsTraversing && actor.TraversalSeconds < 2)
                        {
                            if (cancelled())
                            {
                                break;
                            }

                            yield return null;
                        }

                        actor.CancelTraversal();
                    }

                    actor.GetComponent<CircleCollider2D>().enabled = colliderWasEnabled;
                }
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
            if (sprite == null)
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
