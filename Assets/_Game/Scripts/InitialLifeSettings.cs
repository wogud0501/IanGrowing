using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/Initial Life Settings")]
    public class InitialLifeSettings : ScriptableObject
    {
        [Range(0, 100)]
        public int hunger = 100;

        [Range(0, 100)]
        public int thirst = 100;

        [Range(0, 100)]
        public int knowledge = 0;

        [Range(0, 100)]
        public int health = 100;

        [Range(0, 100)]
        public int mental = 100;

        [Range(0, 100)]
        public int social = 0;

        [Range(0, 100)]
        public int credit = 50;

        [Range(0, 100)]
        public int happiness = 50;

        public StatSnapshot NewStats() =>
            new StatSnapshot
            {
                hunger = hunger,
                thirst = thirst,
                knowledge = knowledge,
                energy = health,
                mental = mental,
                stress = 100 - mental,
                social = social,
                credit = credit,
                happiness = happiness,
                money = 0,
            };

        public const long NewbornDebt = 1000000;
    }
}
