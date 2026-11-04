using UnityEngine;
namespace IanGame {
[CreateAssetMenu(menuName="Ian/Education/Curriculum")]
public class EducationData:ScriptableObject {
 public string educationId,displayName,prerequisiteMission;public int minAge,maxAge=150;
 public LifeStage requiredStage; public int graduationStudies=3,graduationHomework=5,graduationScore=40;
 public MissionCounter enrollmentEvent=MissionCounter.SchoolEnrollment;
}
}
