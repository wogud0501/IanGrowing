using UnityEngine;

namespace IanGame
{
    public enum RelationshipState
    {
        Acquaintance,
        Friend,
        CloseFriend,
        Strained,
    }

    [CreateAssetMenu(menuName = "Ian/Social/NPC")]
    public class RelationshipData : ScriptableObject
    {
        public string npcId;
        public string displayName;
        public string relationshipType;
        public string personality;
        public int minAge = 7;
        public int maxAge = 150;
        public int minSocial;

        [Range(-1, 1)]
        public float responseModifier;
        public int interactionsPerDay = 1;
        public int neglectDays = 7;
        public int neglectLoss = 2;
    }
}
