using System;
using UnityEngine;

namespace IanGame
{
    // A workstation owns one reservation, including the time spent fetching or returning its tool.
    public sealed class ResidentActivityStation : MonoBehaviour
    {
        public bool breakSeat;
        public string seatGroup;
        public SpriteRenderer seatSurface;
        public int preference;
        public ActorMotion[] tasks = Array.Empty<ActorMotion>();
        public Vector2[] workRoute = Array.Empty<Vector2>();
        public Vector2 workingFacing = Vector2.down;
        public SpriteRenderer dirtyDishes;

        public bool HasWorkRoute => tasks is { Length: > 0 } && workRoute is { Length: >= 3 };

        public bool HasSeat =>
            breakSeat
            && seatSurface
            && TryGetComponent<WorldActivitySlot>(out var slot)
            && slot.seated
            && slot.useSeatAnchors
            && slot.seatAnchor
            && slot.approachAnchor
            && slot.standUpAnchor;

        public Vector2 Waypoint(int step)
        {
            return (Vector2)transform.position + workRoute[step];
        }

        public ActorMotion Task(int day, int scheduleIndex)
        {
            return tasks[(day - 1 + scheduleIndex) % tasks.Length];
        }
    }
}
