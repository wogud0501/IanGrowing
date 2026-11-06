using System;using UnityEngine;
namespace IanGame {
public enum IdleActivityKind {Work,Study,Battle}
[Serializable]public class IdleActivityDefinition {
 public string id,title;public IdleActivityKind kind;public int actionId,minAge,maxCycles=512;
 public SceneId startLocation;public double periodSeconds=300;public float efficiency=1;
 public int minimumHealth=20,minimumMental=15,minimumHunger=20,minimumThirst=20;
}
[CreateAssetMenu(menuName="Ian/Idle/Activity settings")]
public sealed class IdleActivitySettings:ScriptableObject {
 public double maximumOfflineSeconds=28800;public int maximumCyclesPerSettlement=512;
 public IdleActivityDefinition[] activities=new IdleActivityDefinition[0];
 public IdleActivityDefinition Find(string id)=>Array.Find(activities,a=>a!=null&&a.id==id);
}
}
