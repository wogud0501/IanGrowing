using System;using UnityEngine;
namespace IanGame {
[CreateAssetMenu(menuName="Ian/Activity Catalog")]
public class ActivityCatalog:ScriptableObject {public ActivityData[] activities=new ActivityData[0];public ActivityData Find(int action)=>Array.Find(activities,v=>v!=null&&(int)v.action==action);}
}
