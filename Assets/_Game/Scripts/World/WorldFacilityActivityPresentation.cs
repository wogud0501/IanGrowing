using System.Linq;
using UnityEngine;

namespace IanGame
{
    // The scheduler owns movement and reservations. This replaces only the occupied
    // artwork after arrival; walking actors always retain velocity-based animation.
    [DefaultExecutionOrder(110)]
    public sealed class WorldFacilityActivityPresentation : MonoBehaviour
    {
        public WorldActivitySlot slot;
        public SpriteRenderer empty;
        public SpriteRenderer occupied;
        public Sprite[] frames;
        public bool citizenRows;
        public bool IsPresenting { get; private set; }
        public int Frame { get; private set; }

        WorldInteractable actor;
        string owner;
        float phase;
        bool familyStove;

        void Awake()
        {
            familyStove = GetComponent<WorldInteractable>()?.id == "family-cook";
            phase = Mathf.Abs(transform.position.x * .37f) % 3;
        }

        void LateUpdate()
        {
            var host = GameContext.Instance?.game.world.Host;
            if (host == null || slot == null)
            {
                return;
            }

            if (owner != slot.Occupant || actor == null && !string.IsNullOrEmpty(slot.Occupant))
            {
                RestoreActor();
                owner = slot.Occupant;
                actor = host.GetComponent<WorldNPCScheduler>().actors.FirstOrDefault(a => a.npcId == owner);
            }

            bool local = actor != null && actor.zone == host.Service.State.zone;
            var animation = actor == null ? null : actor.GetComponent<WorldActorAnimation>();
            bool active =
                local
                && slot.IsOccupied
                && Vector2.Distance(actor.transform.position, slot.seated ? slot.ApproachPoint : slot.Point) < .10f
                && animation != null
                && animation.Motion != ActorMotion.Walk
                && animation.Motion != ActorMotion.SitDown
                && animation.Motion != ActorMotion.StandUp
                && frames != null
                && frames.Length >= 4;
            if (slot.seated)
            {
                Frame = active ? animation.Frame : 0;
                occupied.enabled = false;
                empty.enabled = true;
                IsPresenting = active;
                return;
            }

            if (active)
            {
                int row = 0;
                if (citizenRows && frames.Length >= 16)
                {
                    var name = animation.library.name;
                    row = Mathf.Clamp(name[name.Length - 1] - '0', 0, 3);
                }

                Frame = Mathf.FloorToInt((Time.unscaledTime + phase) * 2.8f) % 4;
                occupied.sprite = frames[row * 4 + Frame];
                occupied.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 100) + 2;
                if (familyStove && FamilyLife.Parent(actor.npcId))
                {
                    occupied.sprite = actor.visual.sprite;
                    occupied.flipX = actor.visual.flipX;
                    occupied.color = actor.visual.color;
                    occupied.transform.position = actor.transform.position + Vector3.up * (actor.visual.sprite.bounds.size.y * actor.visual.transform.lossyScale.y * .46f);
                    occupied.transform.localScale = Vector3.one * (actor.visual.transform.lossyScale.y
                        / occupied.transform.parent.lossyScale.y);
                    occupied.sortingLayerID = empty.sortingLayerID;
                    occupied.sortingOrder = empty.sortingOrder - 1;
                }

                actor.visual.enabled = slot.seated;
                if (actor.shadow != null)
                {
                    actor.shadow.enabled = false;
                }

                if (actor.highlight != null)
                {
                    actor.highlight.enabled = false;
                }
            }
            else if (IsPresenting)
            {
                RestoreActor();
            }

            occupied.enabled = active && !slot.seated;
            empty.enabled = !active || familyStove || slot.seated;
            IsPresenting = active;
        }

        void RestoreActor()
        {
            if (actor == null)
            {
                return;
            }

            var host = GameContext.Instance?.game.world.Host;
            bool local = host != null && host.Service.State.zone == actor.zone;
            actor.visual.enabled = local;
            if (actor.shadow != null)
            {
                actor.shadow.enabled = local;
            }
        }

        void OnDisable()
        {
            RestoreActor();
            if (occupied != null)
            {
                occupied.enabled = false;
            }

            if (empty != null)
            {
                empty.enabled = true;
            }

            IsPresenting = false;
            owner = null;
            actor = null;
        }
    }
}
