using UnityEngine;
namespace IanGame {
// Data contract for Phase 2.2; no mission instances are authored in Phase 2.1.
[CreateAssetMenu(menuName="Ian/Future Mission Growth Definition")]
public class MissionGrowthDefinition:ScriptableObject {public string missionId,prerequisiteId,objectiveKey;[Min(1)]public int requiredCount=1;[Min(0)]public int targetAge;}
}