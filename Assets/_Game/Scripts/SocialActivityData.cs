using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/Social/Activity")]
    public class SocialActivityData : LifeActivityData
    {
        [Range(0, 1)]
        public float successWeight = .8f;
        public int affinityGain = 15;
        public int affinityLoss = 5;
        public int cooldownDays = 1;

        [TextArea]
        public string[] successMessages;

        [TextArea]
        public string failureMessage;
    }
}
