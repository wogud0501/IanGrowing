using UnityEngine;
using UnityEngine.Events;

namespace IanGame.Commercial
{
    public sealed class SceneExitPoint : MonoBehaviour
    {
        public UnityEvent departing = new UnityEvent();

        public void Exit()
        {
            departing.Invoke();
            var room = GetComponent<RoomAtmosphere>();
            if (room != null)
            {
                room.Depart();
            }
        }
    }
}
