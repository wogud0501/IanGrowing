using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/2D Character Visuals")]
    public class Character2DVisuals : ScriptableObject
    {
        public CharacterIdentity identity;
        public Sprite[] idle;
        public Sprite[] work;
        public Sprite[] rest;
        public Sprite[] play;

        [Min(1)]
        public float framesPerSecond = 4;

        public Sprite[] Frames(int action)
        {
            return action == 0 ? work
                : action == 1 ? rest
                : action == 2 ? play
                : idle;
        }
    }
}
