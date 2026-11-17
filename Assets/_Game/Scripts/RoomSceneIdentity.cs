using UnityEngine;

namespace IanGame
{
    public sealed class RoomSceneIdentity : MonoBehaviour
    {
        public IndoorCameraSettings interior = new IndoorCameraSettings();
        public Vector2 cameraOffset;
        public string roomId;
        public RoomSceneCatalog catalog;
        public LifeWorldRules rules;
    }
}
