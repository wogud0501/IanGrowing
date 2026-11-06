using System;using UnityEngine;
namespace IanGame {
[Serializable]public class PromotionConditions {public int experience=30,workCount=3,knowledge=20,social=15,maxRank=3,wageIncrease=250;}
[Serializable]public class DismissalConditions {public int mentalBelow=10,healthBelow=10,strikes=3;}
[CreateAssetMenu(menuName="Ian/Jobs/Job")]
public class JobData:ScriptableObject {
 public string JobId,DisplayName;public int BaseDailyWage,RequiredAge=19,RequiredKnowledge,RequiredSocial,RequiredHealth=20,RequiredExperience;
 [Range(.1f,4)]public float AutoWorkEfficiency=1;
 public int WorkingTime=1,HealthCost=10,MentalCost=5,ExperienceReward=10;
 public bool specialUnlock,businessIncome;[Range(0,1)]public float interviewBaseChance=.5f;
 public PromotionConditions PromotionConditions=new PromotionConditions();public DismissalConditions DismissalConditions=new DismissalConditions();public string[] SpecialEventIds;
}
}
