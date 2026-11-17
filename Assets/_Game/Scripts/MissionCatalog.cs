using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/Mission Catalog")]
    public class MissionCatalog : ScriptableObject
    {
        public string[] chapterTitles = new string[0];
        public MissionData[] missions = new MissionData[0];

        [Min(1)]
        public int experiencePerLevel = 100;
    }
}
