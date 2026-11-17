using System;
using UnityEngine;

namespace IanGame
{
    [Serializable]
    public class PromotionConditions
    {
        public int experience = 30;
        public int workCount = 3;
        public int knowledge = 20;
        public int social = 15;
        public int maxRank = 3;
        public int wageIncrease = 250;
    }

    [Serializable]
    public class DismissalConditions
    {
        public int mentalBelow = 10;
        public int healthBelow = 10;
        public int strikes = 3;
    }

    [CreateAssetMenu(menuName = "Ian/Jobs/Job")]
    public class JobData : ScriptableObject
    {
        public string JobId;
        public string DisplayName;
        public int BaseDailyWage;
        public int RequiredAge = 19;
        public int RequiredKnowledge;
        public int RequiredSocial;
        public int RequiredHealth = 20;
        public int RequiredExperience;

        [Range(.1f, 4)]
        public float AutoWorkEfficiency = 1;
        public int WorkingTime = 1;
        public int HealthCost = 10;
        public int MentalCost = 5;
        public int ExperienceReward = 10;
        public bool specialUnlock;
        public bool businessIncome;

        [Range(0, 1)]
        public float interviewBaseChance = .5f;
        public PromotionConditions PromotionConditions = new PromotionConditions();
        public DismissalConditions DismissalConditions = new DismissalConditions();
        public string[] SpecialEventIds;
    }
}
