using System;using UnityEngine;
namespace IanGame {
[Flags]public enum MissionFeature {None=0,Core=1,BasicEducation=2,AdvancedEducation=4,Equipment=8,Jobs=16,Investments=32,Housing=64,EventChoices=128,Endings=256,Achievements=512,Gacha=1024,DebtRepayment=2048,Taxation=4096,Combat=8192,FurniturePurchases=16384}
public enum MissionSource {UserProposal,ConfirmedV1}
public enum MissionCounter {None=0,Drink=1,Meal=2,Sleep=3,Rest=4,SchoolEnrollment=10,BooksRead=11,FriendsMade=12,Homework=13,Exams=14,ReportCards=15,AfterSchool=16,Study=17,Work=18,Graduation=19,MiddleSchool=100,HeadphonesEquipped=101,EquipmentEquipped=102,CareerChosen=103,SecondaryGraduation=104,ResumeWritten=110,Interviews=111,Employment=112,Salary=113,TaxesPaid=114,DebtRepaid=115,StockBuys=120,EventChoices=121,Promotions=130,FurnitureBought=131,RentPaid=132,InsuranceLearned=133,StockSells=134,PnLReviews=135,RetirementDecisions=140,LifeEventRecollections=141,IncomeReviews=142,MajorAchievements=143,DebtReviews=144,LifeReviewApplications=145,LifeReviewCompletions=146,GachaDraws=150,BattleWins=160,HousingContracts=170,PropertyBought=171,PropertySold=172,HousingLoans=173}
public enum MissionItemKey {None=0,StarterHeadphones=1,StarterEquipment=2,FirstFurniture=3}
public enum ContentUnlock {None=0,School=1,Education=2,Equipment=3,Employment=4,Finance=5,Housing=6,Retirement=7,LifeReview=8}
public enum MissionConditionKind {All,Any,StatAtLeast,CounterAtLeast,AgeAtLeast,StageAtLeast,DaysSurvived,CashAtLeast,Alive,ItemOwned,MissionClaimed,ContentUnlocked,EventChoiceOutcome,NetWorthAtLeast}
[Serializable]public class MissionCondition {
 public MissionConditionKind kind;public MissionChoiceOutcome choice;public CoreStat stat;public MissionCounter counter;public MissionItemKey item;public ContentUnlock unlock;
 public long target=1;public string missionId;public MissionCondition[] children=new MissionCondition[0];
}
[Serializable]public class MissionItemReward {public MissionItemKey item;[Min(1)]public int amount=1;}
[Serializable]public class MissionRewards {public int gold=50;public long experience=20,growthPoints=1;public int gachaTickets;public MissionItemReward[] items=new MissionItemReward[0];public ContentUnlock[] unlocks=new ContentUnlock[0];}
[CreateAssetMenu(menuName="Ian/Mission Data")]
public class MissionData:ScriptableObject {
 [SerializeField]string stableId;public string Id=>stableId;
 public int number,chapter;public int suggestedAction=-1;public string title;[TextArea]public string description;
 public MissionSource source;public string[] prerequisiteIds=new string[0];
 public MissionCondition condition;public MissionRewards rewards=new MissionRewards();public MissionFeature requiredFeatures=MissionFeature.Core;
 public string futurePhase;[TextArea]public string completionMessage;
 [Tooltip("-1 means no age milestone")]public int targetAge=-1;public bool enterLifeReview;
 #if UNITY_EDITOR
 public void SetEditorId(string value){if(!string.IsNullOrEmpty(stableId)&&stableId!=value)throw new InvalidOperationException("Mission IDs are immutable.");stableId=value;}
 #endif
}
}
