using UnityEngine;

namespace IanGame
{
    public enum ActivityOccupancy
    {
        Free,
        Reserved,
        Occupied,
    }

    // World furniture never belongs to the player's purchased inventory. Legacy slots
    // retain their existing Point; authored facility seats opt into three anchors.
    public sealed class WorldActivitySlot : MonoBehaviour
    {
        public SpriteRenderer seat;
        public string staffId;
        public string[] reliefStaff = System.Array.Empty<string>();

        public bool Allows(string id)
        {
            return string.IsNullOrEmpty(staffId) || staffId == id || System.Array.IndexOf(reliefStaff, id) >= 0;
        }

        public Vector2 facing = Vector2.up;
        public bool useSeatAnchors;
        public bool seated;
        public Transform seatAnchor;
        public Transform approachAnchor;
        public Transform standUpAnchor;
        public ActorMotion activityMotion = ActorMotion.UseComputer;
        public string Occupant { get; private set; }
        public bool IsOccupied { get; private set; }
        public ActivityOccupancy State =>
            string.IsNullOrEmpty(Occupant) ? ActivityOccupancy.Free
            : IsOccupied ? ActivityOccupancy.Occupied
            : ActivityOccupancy.Reserved;

        Vector2 LegacyPoint => GetComponent<WorldInteractable>().Point;
        public Vector2 Point => useSeatAnchors && seatAnchor != null ? (Vector2)seatAnchor.position : LegacyPoint;
        public Vector2 ApproachPoint =>
            useSeatAnchors && approachAnchor != null ? (Vector2)approachAnchor.position : Point;
        public Vector2 StandUpPoint =>
            useSeatAnchors && standUpAnchor != null ? (Vector2)standUpAnchor.position : ApproachPoint;

        public bool TryReserve(string id)
        {
            if (string.IsNullOrEmpty(id) || !Allows(id) || !string.IsNullOrEmpty(Occupant) && Occupant != id)
            {
                return false;
            }

            Occupant = id;
            return true;
        }

        public void MarkOccupied(string id)
        {
            if (Occupant != id)
            {
                return;
            }

            IsOccupied = true;
            if (seat != null)
            {
                seat.enabled = false;
            }
        }

        public void Release(string id)
        {
            if (Occupant == id)
            {
                Occupant = null;
                IsOccupied = false;
                if (seat != null)
                {
                    seat.enabled = true;
                }
            }
        }

        void OnDisable()
        {
            Occupant = null;
            IsOccupied = false;
            if (seat != null)
            {
                seat.enabled = true;
            }
        }
    }
}
