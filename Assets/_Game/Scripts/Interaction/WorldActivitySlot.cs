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
        // The seat anchor is the hip contact on an authored cushion. The sole
        // anchor is the front edge at floor level, independent of navigation.
        public Transform soleAnchor;
        public Vector2 SolePoint => soleAnchor ? (Vector2)soleAnchor.position : Point;

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

        public SpriteRenderer Surface { get; set; }
        public SeatBend OccupiedPose { get; set; }

        public void ConfigureSeat(Vector2 cushion, Vector2 floor, Vector2 direction, SpriteRenderer surface)
        {
            facing = direction;
            Surface = surface;
            seated = true;
            useSeatAnchors = true;
            if (!seatAnchor)
            {
                seatAnchor = MakeAnchor("Seat contact", cushion);
            }

            if (!approachAnchor)
            {
                approachAnchor = MakeAnchor("Seat approach", floor);
            }

            if (!standUpAnchor)
            {
                standUpAnchor = approachAnchor;
            }
        }

        public void SetSeatContacts(Vector2 hip, Vector2 soles)
        {
            if (soleAnchor)
            {
                return;
            }

            if (!seatAnchor || seatAnchor == transform || seatAnchor == approachAnchor || seatAnchor == standUpAnchor)
            {
                seatAnchor = MakeAnchor("Cushion hip contact", hip);
            }
            else
            {
                seatAnchor.position = hip;
            }

            soleAnchor = MakeAnchor("Seated soles", soles);
        }

        public void SetFloor(Vector2 floor)
        {
            if (approachAnchor && WorldNPCNavigation.StaticFree(floor))
            {
                var previous = approachAnchor.position;
                approachAnchor.position = floor;
                if (standUpAnchor && standUpAnchor != approachAnchor
                    && (standUpAnchor.position - previous).sqrMagnitude < .0025f)
                {
                    standUpAnchor.position = floor;
                }
            }
        }

        Sprite foregroundSprite;

        // The chair back and tabletop are painted in one sprite. Keep the original
        // behind the person and draw the foreground part in front of their lap.
        public void AddForeground(SpriteRenderer surface, float heightFraction)
        {
            if (foregroundSprite || !surface || !surface.sprite)
            {
                return;
            }

            var source = surface.sprite;
            if (source.packed && (source.packingMode == SpritePackingMode.Tight
                || source.packingRotation != SpritePackingRotation.None))
            {
                return;
            }

            Rect rect = source.packed ? source.textureRect : source.rect;
            Vector2 trim = source.packed ? source.textureRectOffset : Vector2.zero;
            float top = Mathf.Round(source.rect.height * heightFraction) - trim.y;
            rect.height = Mathf.Clamp(top, 1, rect.height);
            foregroundSprite = Sprite.Create(source.texture, rect, new Vector2(.5f, 0), source.pixelsPerUnit,
                0, SpriteMeshType.FullRect);
            var front = new GameObject("Chair or tabletop foreground").AddComponent<SpriteRenderer>();
            front.transform.SetParent(surface.transform, false);
            front.transform.localPosition = (new Vector2(rect.width * .5f, 0) + trim - source.pivot) / source.pixelsPerUnit;
            front.sprite = foregroundSprite;
            front.color = surface.color;
            front.sharedMaterial = surface.sharedMaterial;
            front.sortingLayerID = surface.sortingLayerID;
            front.sortingOrder = surface.sortingOrder + 2;
        }

        void OnDestroy()
        {
            if (foregroundSprite)
            {
                Destroy(foregroundSprite);
            }
        }

        Transform MakeAnchor(string label, Vector2 point)
        {
            var anchor = new GameObject(label).transform;
            anchor.SetParent(transform, false);
            anchor.position = point;
            return anchor;
        }

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
            if (seat != null && !seated)
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
                if (seat != null && !seated)
                {
                    seat.enabled = true;
                }
            }
        }

        void OnDisable()
        {
            Occupant = null;
            IsOccupied = false;
            if (seat != null && !seated)
            {
                seat.enabled = true;
            }
        }
    }
}
