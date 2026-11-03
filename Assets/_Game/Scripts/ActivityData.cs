using System;using UnityEngine;
namespace IanGame {
public enum EducationAction {EnrollSchool=7,ReadBook=8,MakeFriend=9,Homework=10,Exam=11,CollectReportCard=12,AfterSchool=13,Study=14}
[CreateAssetMenu(menuName="Ian/Basic Activity")]
public class ActivityData:ScriptableObject {
 public EducationAction action;public string displayName;[TextArea]public string message;
 [Min(0)]public int minAge=7,maxAge=18;[Min(1)]public int days=1;[Min(0)]public int cost;
 public StatDelta delta;public MissionCounter producedCounter,requiredCounter;public long requiredCount=1;public bool onceOnly;public bool schoolOnly=true;
}
[CreateAssetMenu(menuName="Ian/Activity Catalog")]
public class ActivityCatalog:ScriptableObject {public ActivityData[] activities=new ActivityData[0];public ActivityData Find(int action)=>Array.Find(activities,v=>v!=null&&(int)v.action==action);}
}