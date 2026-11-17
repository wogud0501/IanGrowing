namespace IanGame
{
    public static class LifeStageService
    {
        public static LifeStage FromAge(int age) =>
            age <= 6 ? LifeStage.Infancy
            : age <= 12 ? LifeStage.Childhood
            : age <= 18 ? LifeStage.Adolescence
            : age <= 29 ? LifeStage.YoungAdult
            : age <= 59 ? LifeStage.MiddleAge
            : LifeStage.OldAge;
    }

    public sealed class AgeProgressionService
    {
        readonly LifeProgression owner;

        public AgeProgressionService(LifeProgression value)
        {
            owner = value;
        }

        public bool ApplyMissionMilestone(string missionId, int targetAge)
        {
            return owner != null
                && targetAge >= owner.Age
                && owner.TryAdvanceAgeForMission("mission-age:" + missionId, targetAge);
        }
    }
}
