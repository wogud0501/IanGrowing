using UnityEngine;
namespace IanGame {
[CreateAssetMenu(menuName="Ian/Idle/Enemy")]
public sealed class EnemyData:ScriptableObject {
 public string id="deadline",displayName="마감 알림";public int health=30,attack=7,defense=1,gold=80,experience=20,minAge=7,maxTurns=32;
 public Sprite[] frames=new Sprite[0];
}
}
