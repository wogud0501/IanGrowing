using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/World/Directional Walk Set")]
    public sealed class WorldWalkSet : ScriptableObject
    {
        public RuntimeAnimatorController controller;
        public Sprite[] down;
        public Sprite[] up;
        public Sprite[] left;
        public Sprite[] right;

        [Min(.1f)]
        public float strideLength = 1.8f;
        public bool Complete =>
            controller != null
            && down != null
            && up != null
            && left != null
            && right != null
            && down.Length >= 6
            && up.Length >= 6
            && left.Length >= 6
            && right.Length >= 6;

        public string Direction(Vector2 facing) =>
            Mathf.Abs(facing.x) > Mathf.Abs(facing.y)
                ? facing.x < 0
                    ? "Left"
                    : "Right"
                : facing.y > 0
                    ? "Up"
                    : "Down";
    }
}
