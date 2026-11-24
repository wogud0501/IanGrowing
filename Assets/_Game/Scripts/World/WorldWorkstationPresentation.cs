using System.Linq;
using UnityEngine;

namespace IanGame
{
    // Keep the employee's own body and animation through arrival, work and departure.
    [DefaultExecutionOrder(100)]
    public sealed class WorldWorkstationPresentation : MonoBehaviour
    {
        public WorldActivitySlot slot;
        public WorldInteractable actor;
        public SpriteRenderer empty;
        public SpriteRenderer working;
        public Sprite[] frames;
        public bool IsWorking { get; private set; }
        public int Frame { get; private set; }

        void LateUpdate()
        {
            if (slot == null)
            {
                return;
            }

            if (actor == null || actor.npcId != slot.Occupant)
            {
                actor = GameContext
                    .Instance?.game.world.Host?.GetComponent<WorldNPCScheduler>()
                    ?.actors.FirstOrDefault(a => a && a.npcId == slot.Occupant);
            }

            if (actor == null)
            {
                if (working != null)
                {
                    working.enabled = false;
                }

                if (empty != null)
                {
                    empty.enabled = true;
                }

                return;
            }

            var world = actor == null ? null : actor.GetComponent<WorldActorAnimation>();
            var context = GameContext.Instance;
            bool local = context != null && context.game.world.State.zone == actor.zone;
            bool active =
                local
                && slot.Occupant == actor.npcId
                && slot.IsOccupied
                && Vector2.Distance(actor.transform.position, slot.ApproachPoint) < .2f
                && world != null
                && (world.Motion == ActorMotion.UseComputer || world.Motion == ActorMotion.Work);
            if (active)
            {
                Frame = world.Frame;
                actor.visual.enabled = true;
                if (actor.shadow != null)
                {
                    actor.shadow.enabled = false;
                }
            }
            else if (IsWorking && actor != null)
            {
                actor.visual.enabled = local;
                if (actor.shadow != null)
                {
                    actor.shadow.enabled = local;
                }
            }

            working.enabled = false;
            empty.enabled = true;
            IsWorking = active;
        }

        void OnDisable()
        {
            if (working != null)
            {
                working.enabled = false;
            }

            if (empty != null)
            {
                empty.enabled = true;
            }
        }
    }
}
