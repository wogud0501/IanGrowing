using UnityEngine;
using UnityEngine.Events;

namespace IanGame.Commercial
{
    public sealed class SceneEntryPoint : MonoBehaviour
    {
        public UnityEvent arrived = new UnityEvent();

        public void Enter()
        {
            arrived.Invoke();
            var room = GetComponent<RoomAtmosphere>();
            if (room != null)
            {
                room.Enter();
            }
        }
    }
}
