using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/Idle/Enemy")]
    public sealed class EnemyData : ScriptableObject
    {
        public string id = "deadline";
        public string displayName = "마감 알림";
        public int health = 30;
        public int attack = 7;
        public int defense = 1;
        public int gold = 80;
        public int experience = 20;
        public int minAge = 7;
        public int maxTurns = 32;
        public Sprite[] frames = new Sprite[0];
    }
}
