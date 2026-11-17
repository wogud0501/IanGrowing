using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/Education/Curriculum")]
    public class EducationData : ScriptableObject
    {
        public string educationId;
        public string displayName;
        public string prerequisiteMission;
        public int minAge;
        public int maxAge = 150;
        public LifeStage requiredStage;
        public int graduationStudies = 3;
        public int graduationHomework = 5;
        public int graduationScore = 40;
        public MissionCounter enrollmentEvent = MissionCounter.SchoolEnrollment;
    }
}
