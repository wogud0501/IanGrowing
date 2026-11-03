using UnityEngine;
namespace IanGame {
[CreateAssetMenu(menuName="Ian/Survival Settings")]
public class SurvivalSettings:ScriptableObject {
 [Min(1)]public float realSecondsPerGameDay=300;
 [Min(0)]public float hungerLossPerDay=8,thirstLossPerDay=12,dangerGraceHours=48;
 [Range(0,100)]public int lowHealthThreshold=25,lowMentalThreshold=25;
 [Range(.05f,1)]public float lowHealthEfficiency=.5f;
 [Min(1)]public float lowMentalEventMultiplier=1.5f;
 public StatDelta meal=new StatDelta{hunger=35},drink=new StatDelta{thirst=40},sleep=new StatDelta{health=35,mental=25},hospital=new StatDelta{health=40,mental=40};
 [Min(0)]public int mealCost,drinkCost,sleepCost,hospitalCost;
 [Min(1)]public int careActionDays=1;
 [Min(0)]public float maxOfflineInspectionSeconds=28800;
}
}
