using System;
using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/Phase 2.3 Catalog")]
    public class Phase23Catalog : ScriptableObject
    {
        public EducationData[] education;
        public LifeActivityData[] activities;
        public RelationshipData[] npcs;
        public JobData[] jobs;

        public LifeActivityData Find(int id) => Array.Find(activities, a => a != null && (int)a.action == id);

        public JobData Job(string id) => Array.Find(jobs, j => j != null && j.JobId == id);

        public RelationshipData NPC(string id) => Array.Find(npcs, n => n != null && n.npcId == id);
    }
}
