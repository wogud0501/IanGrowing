using UnityEngine;

namespace IanGame.Commercial
{
    [CreateAssetMenu(menuName = "Ian/Presentation/Transition Profile")]
    public sealed class TransitionProfile : ScriptableObject
    {
        public SceneId from;
        public SceneId to;

        [Min(0)]
        public float exitSeconds = .32f;

        [Min(0)]
        public float coveredSeconds = .15f;

        [Min(0)]
        public float entrySeconds = .38f;
        public string departure = "외출 준비 중";
        public string arrival = "도착했습니다";
        public Color curtain = Color.white;
        public AudioClip exitSound;
        public AudioClip entrySound;
    }
}
