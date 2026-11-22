using UnityEngine;

namespace IanGame
{
    public sealed partial class WorldNPCScheduler
    {
        public ResidentActivityArt activityArt;

        WorldInteractable BreakSeat(Resident resident)
        {
            var planned = Object(resident.target.objectId);
            if (planned == null || planned.zone != resident.target.zone)
            {
                return null;
            }

            var preferred = planned.GetComponent<ResidentActivityStation>();
            if (preferred == null || !preferred.HasSeat)
            {
                return null;
            }

            var id = resident.definition.npcId;
            bool Available(WorldActivitySlot slot) =>
                slot != null && slot.Allows(id) && (slot.Occupant == id || slot.State == ActivityOccupancy.Free);
            if (Available(planned.GetComponent<WorldActivitySlot>()))
            {
                return planned;
            }

            WorldInteractable best = null;
            float bestScore = float.MaxValue;
            foreach (var candidate in world.objects)
            {
                if (
                    candidate == null
                    || candidate.zone != planned.zone
                    || !candidate.TryGetComponent<ResidentActivityStation>(out var seat)
                    || !seat.HasSeat
                    || seat.seatGroup != preferred.seatGroup
                    || !Available(candidate.GetComponent<WorldActivitySlot>())
                )
                {
                    continue;
                }

                float score = seat.preference * 1000 + Vector2.SqrMagnitude(candidate.Point - resident.actor.Point);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        void BindActivity(Resident r)
        {
            r.anchor = r.target.action == NPCDailyAction.Rest ? BreakSeat(r) : Object(r.target.objectId);
            r.anchorFacing = r.anchor
                ? ((Vector2)r.anchor.transform.position - r.anchor.Point).normalized
                : Vector2.zero;
            r.station = r.anchor ? r.anchor.GetComponent<ResidentActivityStation>() : null;
            r.requestedSlot = r.anchor ? r.anchor.GetComponent<WorldActivitySlot>() : null;
            if (r.anchor && r.target.action == NPCDailyAction.Sleep && !r.requestedSlot)
            {
                r.requestedSlot = r.anchor.gameObject.AddComponent<WorldActivitySlot>();
            }

            if (r.requestedSlot != null && r.requestedSlot.TryReserve(r.definition.npcId))
            {
                r.slot = r.requestedSlot;
            }
        }

        static bool HasChore(Resident r) =>
            r.station != null
            && r.station.HasWorkRoute
            && r.target.action is NPCDailyAction.Clean or NPCDailyAction.WashDishes;

        bool AdvanceChore(Resident r, int index)
        {
            if (!HasChore(r) || r.slot == null && !r.finishedTask)
            {
                return false;
            }

            if (!r.finishedTask && r.atActivity)
            {
                if (r.workUntil <= 0)
                {
                    r.workUntil =
                        Time.time + (r.taskStep == 0 || r.taskStep == r.station.workRoute.Length - 1 ? 1.1f : 4.5f);
                }

                if (Time.time >= r.workUntil)
                {
                    r.workUntil = 0;
                    if (r.taskStep + 1 < r.station.workRoute.Length)
                    {
                        r.taskStep++;
                        r.atActivity = false;
                        r.path.Clear();
                        r.waypoint = 0;
                        r.action = NPCDailyAction.Walk;
                    }
                    else
                    {
                        r.finishedTask = true;
                        r.atActivity = false;
                        r.slot.Release(r.definition.npcId);
                        r.slot = null;
                        r.requestedSlot = null;
                        if (r.station.dirtyDishes)
                        {
                            r.station.dirtyDishes.enabled = false;
                        }
                    }
                }
            }

            if (!r.finishedTask)
            {
                return false;
            }

            r.action = NPCDailyAction.Idle;
            Render(r, index, r.actor.transform.position, ActorMotion.Idle);
            return true;
        }

        ActorMotion LifeMotion(Resident r, ActorMotion motion)
        {
            if (!activityArt)
            {
                return motion;
            }

            if (HasChore(r) && !r.finishedTask && r.slot != null && r.actor.zone == r.target.zone)
            {
                if (
                    r.taskStep > 0
                    && r.taskStep < r.station.workRoute.Length - 1
                    && (r.atActivity || r.taskStep > 1 && r.action == NPCDailyAction.Walk)
                )
                {
                    return r.station.Task(world.Service.Game.clock.Day, r.index);
                }
            }

            if (
                r.station == null
                || !r.station.breakSeat
                || r.slot == null
                || !r.slot.IsOccupied
                || !r.atActivity
                || Time.unscaledTime - r.seatedAt < .55f
            )
            {
                return motion;
            }

            if (r.action == NPCDailyAction.Talk)
            {
                return ActorMotion.TalkSit;
            }

            if (r.action == NPCDailyAction.WatchTV)
            {
                return ActorMotion.SitRelax;
            }

            if (r.action != NPCDailyAction.Rest)
            {
                return motion;
            }

            int variant = (r.index + Mathf.FloorToInt((Time.unscaledTime - r.seatedAt) / 8f)) % 4;
            return variant == 1 ? ActorMotion.SeatedDrink
                : variant == 3 ? ActorMotion.SeatedPhone
                : ActorMotion.SitRelax;
        }

        void DrawHeldItem(Resident r, ActorMotion motion)
        {
            // The seated clips include the cup or phone in the animated hand.
            // A character using a fallback clip still needs the separate prop.
            bool seated = motion is ActorMotion.SeatedDrink or ActorMotion.SeatedPhone;
            bool inFrame = seated && r.animation && !r.animation.UsedMotionFallback;
            Sprite sprite = activityArt && !inFrame ? activityArt.Prop(motion) : null;
            bool carrying =
                HasChore(r)
                && !r.finishedTask
                && r.slot != null
                && r.taskStep > 0
                && r.actor.zone == r.target.zone
                && r.action == NPCDailyAction.Walk;
            if (!sprite && carrying)
            {
                sprite = activityArt ? activityArt.Prop(r.station.Task(world.Service.Game.clock.Day, r.index)) : null;
            }

            if (!r.heldItem && sprite)
            {
                var item = r.actor.transform.Find("Held household item");
                if (!item)
                {
                    item = new GameObject("Held household item", typeof(SpriteRenderer)).transform;
                    item.SetParent(r.actor.transform, false);
                }

                r.heldItem = item.GetComponent<SpriteRenderer>();
                r.heldItem.sharedMaterial = r.actor.visual.sharedMaterial;
            }

            if (!r.heldItem)
            {
                return;
            }

            r.heldItem.enabled = sprite && r.active;
            if (!r.heldItem.enabled)
            {
                return;
            }

            var sr = r.heldItem;
            sr.sprite = sprite;
            float stroke = Mathf.Sin(Time.time * 3.4f);
            bool floor = motion is ActorMotion.Sweep or ActorMotion.Mop or ActorMotion.Vacuum;
            float height =
                floor ? Mathf.Clamp(r.actor.visual.bounds.size.y * .82f, 1.1f, 1.60f)
                : seated ? .41f
                : .62f;
            float side = r.facing.x < -.2f ? -1 : 1;
            sr.transform.position =
                r.actor.transform.position
                + new Vector3(
                    side * (floor ? .39f + .14f * stroke : .32f),
                    floor ? -.02f
                        : seated ? .58f + .14f * stroke
                        : motion is ActorMotion.WashDishes or ActorMotion.WipeTable ? .9f + .06f * stroke
                        : .44f + .06f * stroke,
                    0
                );
            sr.transform.localScale = Vector3.one * (height / sprite.bounds.size.y / r.actor.transform.lossyScale.y);
            sr.transform.rotation = Quaternion.Euler(0, 0, floor ? side * (10 + 15 * stroke) : 3 * stroke);
            sr.sortingOrder = r.actor.visual.sortingOrder + (r.facing.y > .6f && !floor ? -1 : 1);
        }

        static string ChoreLabel(ActorMotion motion) =>
            motion switch
            {
                ActorMotion.Sweep => "빗자루질 중",
                ActorMotion.Mop => "바닥 닦는 중",
                ActorMotion.Vacuum => "청소기 사용 중",
                ActorMotion.WipeTable => "식탁 닦는 중",
                ActorMotion.WashDishes => "설거지 중",
                ActorMotion.CarryLaundry => "빨래 정리 중",
                ActorMotion.CarryTrash => "쓰레기 정리 중",
                ActorMotion.SeatedDrink => "음료 마시는 중",
                ActorMotion.SeatedPhone => "휴대폰 확인 중",
                _ => null,
            };
    }
}
