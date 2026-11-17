using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/Survival Settings")]
    public class SurvivalSettings : ScriptableObject
    {
        [Min(1)]
        public float realSecondsPerGameDay = 300;

        [Min(0)]
        public float hungerLossPerDay = 8;

        [Min(0)]
        public float thirstLossPerDay = 12;

        [Min(0)]
        public float dangerGraceHours = 48;

        [Range(0, 100)]
        public int lowHealthThreshold = 25;

        [Range(0, 100)]
        public int lowMentalThreshold = 25;

        [Range(.05f, 1)]
        public float lowHealthEfficiency = .5f;

        [Min(1)]
        public float lowMentalEventMultiplier = 1.5f;
        public StatDelta meal = new StatDelta { hunger = 35 };
        public StatDelta drink = new StatDelta { thirst = 40 };
        public StatDelta sleep = new StatDelta { health = 35, mental = 25 };
        public StatDelta hospital = new StatDelta { health = 40, mental = 40 };

        [Min(0)]
        public int mealCost;

        [Min(0)]
        public int drinkCost;

        [Min(0)]
        public int sleepCost;

        [Min(0)]
        public int hospitalCost;

        [Min(1)]
        public int careActionDays = 1;

        [Min(0)]
        public float maxOfflineInspectionSeconds = 28800;
    }
}
