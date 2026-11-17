using UnityEngine;

namespace IanGame
{
    public sealed class WorldRoomBackdrop : MonoBehaviour
    {
        public SpriteRenderer floor;
        public Sprite day;
        public Sprite night;

        public void Apply(bool afterDark)
        {
            if (floor != null)
            {
                var chosen = afterDark && night != null ? night : day;
                if (chosen != null && floor.sprite != chosen)
                {
                    floor.sprite = chosen;
                }
            }
        }
    }
}
