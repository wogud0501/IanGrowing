using UnityEngine;

namespace IanGame
{
    // Non-interactive cutaway construction around the room. The room keeps its real
    // dimensions and colliders; this is plaster/wood structure, not another location.
    [DefaultExecutionOrder(1500)]
    public sealed class InteriorSurround : MonoBehaviour
    {
        public WorldCameraController cameraRig;
        public SpriteRenderer plaster;

        void LateUpdate()
        {
            if (cameraRig?.target == null || plaster == null)
            {
                return;
            }

            var c = cameraRig.target;
            plaster.transform.position = new Vector3(c.transform.position.x, c.transform.position.y, 0);
            plaster.size = new Vector2(c.orthographicSize * 2 * c.aspect + 2, c.orthographicSize * 2 + 2);
        }
    }
}
