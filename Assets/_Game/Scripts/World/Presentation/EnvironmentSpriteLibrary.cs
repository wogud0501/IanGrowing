using UnityEngine;

namespace IanGame.Commercial
{
    [CreateAssetMenu(menuName = "Ian/Presentation/Environment Sprites")]
    public sealed class EnvironmentSpriteLibrary : ScriptableObject
    {
        public Sprite[] walk;
        public Sprite[] typing;
        public Sprite[] vehicle;
        public Sprite[] billboard;
    }
}
