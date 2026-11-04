using System;using System.Collections.Generic;using UnityEngine;
namespace IanGame {
public enum LifeStage {Infancy,Childhood,Adolescence,YoungAdult,MiddleAge,OldAge}
[Serializable]public class ProgressSnapshot {public int age,level=1;public long experience;public string socialStanding="없음";public List<string> completedGrowthIds=new List<string>();}
public class LifeProgression:MonoBehaviour {
 ProgressSnapshot state=new ProgressSnapshot();
 public int Age=>state.age;public int Level=>state.level;public long Experience=>state.experience;public string SocialStanding=>state.socialStanding;
 public LifeStage Stage=>StageAtAge(Age);public bool EmploymentUnlocked=>Age>=19;public bool FinanceUnlocked=>Age>=19;
 public event Action Changed;public event Action<int,LifeStage> AgeChanged;public event Action<LifeStage> StageChanged;
 void NotifyAge(int oldAge){if(oldAge==Age)return;AgeChanged?.Invoke(Age,Stage);if(StageAtAge(oldAge)!=Stage)StageChanged?.Invoke(Stage);}
 public static LifeStage StageAtAge(int age)=>LifeStageService.FromAge(age);
 public ProgressSnapshot Capture()=>new ProgressSnapshot{age=Age,level=Level,experience=Experience,socialStanding=SocialStanding,completedGrowthIds=new List<string>(state.completedGrowthIds)};
 public void Restore(ProgressSnapshot value){if(value==null)return;int oldAge=Age;state=new ProgressSnapshot{age=Math.Max(0,value.age),level=Math.Max(1,value.level),experience=Math.Max(0,value.experience),socialStanding=string.IsNullOrEmpty(value.socialStanding)?"없음":value.socialStanding,completedGrowthIds=value.completedGrowthIds==null?new List<string>():new List<string>(value.completedGrowthIds)};NotifyAge(oldAge);Changed?.Invoke();}
 public void ResetLife(){Restore(new ProgressSnapshot());}
 // Monotonic mission milestones; explicit save restore may load an earlier checkpoint.
 public bool TryAdvanceAgeForMission(string completionId,int targetAge){if(string.IsNullOrWhiteSpace(completionId)||targetAge<Age||targetAge<0||state.completedGrowthIds.Contains(completionId))return false;int oldAge=Age;state.completedGrowthIds.Add(completionId);state.age=targetAge;NotifyAge(oldAge);Changed?.Invoke();return true;}
 public bool GrantMissionExperience(string receipt,long amount,int experiencePerLevel){if(string.IsNullOrEmpty(receipt)||amount<0||state.completedGrowthIds.Contains(receipt))return false;state.completedGrowthIds.Add(receipt);state.experience=state.experience>long.MaxValue-amount?long.MaxValue:state.experience+amount;long quotient=state.experience/Math.Max(1,experiencePerLevel);state.level=Math.Max(state.level,quotient>=int.MaxValue-1?int.MaxValue:(int)quotient+1);Changed?.Invoke();return true;}
 public bool MeetsCareerRequirements(PlayerStats stats,int requiredKnowledge,int requiredSocial)=>EmploymentUnlocked&&stats!=null&&stats.Knowledge>=requiredKnowledge&&stats.Social>=requiredSocial;
 public bool MeetsLoanRequirements(PlayerStats stats,int requiredCredit)=>FinanceUnlocked&&stats!=null&&stats.Credit>=requiredCredit;
}
}