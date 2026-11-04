using System;using UnityEngine;
namespace IanGame {
[CreateAssetMenu(menuName="Ian/Fictional Status Effect")]
public class StatusEffectDefinition:ScriptableObject {
 public string id,displayName;public CoreStat triggerStat=CoreStat.Health;
 [Range(0,100)]public int triggerAtOrBelow=25,recoverAtOrAbove=40;
 [Min(1)]public double durationSeconds=86400;
 public float healthPerDay,mentalPerDay;
 [Range(.05f,1)]public float efficiencyMultiplier=.8f;
 [TextArea]public string description="풍자적인 가상 상태입니다.";
}
[Serializable]public class ActiveStatusSnapshot {public string id;public double remainingSeconds,healthRemainder,mentalRemainder;}
}