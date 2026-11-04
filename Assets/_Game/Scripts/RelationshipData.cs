using UnityEngine;
namespace IanGame {
public enum RelationshipState {Acquaintance,Friend,CloseFriend,Strained}
[CreateAssetMenu(menuName="Ian/Social/NPC")]
public class RelationshipData:ScriptableObject {
 public string npcId,displayName,relationshipType,personality;public int minAge=7,maxAge=150,minSocial;
 [Range(-1,1)]public float responseModifier;public int interactionsPerDay=1,neglectDays=7,neglectLoss=2;
}
}
