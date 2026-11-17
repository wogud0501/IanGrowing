using System;
using UnityEngine;

namespace IanGame
{
    public enum IdleActivityKind
    {
        Work,
        Study,
        Battle,
    }

    [Serializable]
    public class IdleActivityDefinition
    {
        public string id;
        public string title;
        public IdleActivityKind kind;
        public int actionId;
        public int minAge;
        public int maxCycles = 512;
        public SceneId startLocation;
        public double periodSeconds = 300;
        public float efficiency = 1;
        public int minimumHealth = 20;
        public int minimumMental = 15;
        public int minimumHunger = 20;
        public int minimumThirst = 20;
    }

    [CreateAssetMenu(menuName = "Ian/Idle/Activity settings")]
    public sealed class IdleActivitySettings : ScriptableObject
    {
        public double maximumOfflineSeconds = 28800;
        public int maximumCyclesPerSettlement = 512;
        public IdleActivityDefinition[] activities = new IdleActivityDefinition[0];

        public IdleActivityDefinition Find(string id) => Array.Find(activities, a => a != null && a.id == id);
    }
}
