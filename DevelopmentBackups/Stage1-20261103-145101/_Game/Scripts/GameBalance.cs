using UnityEngine;
namespace IanGame {
[CreateAssetMenu(menuName="Ian/Balance")]
public class GameBalance:ScriptableObject {
 [Range(0,100)] public int initialHappiness=50, initialStress=20, initialEnergy=70, initialMoney=50;
 public int workMoney=18, workStress=15, workEnergy=-18;
 public int restMoney=-4, restStress=-12, restEnergy=28;
 public int playMoney=-12, playHappiness=20, playStress=-10, playEnergy=-8;
 [Range(0,1)] public float eventChance=.4f;
}
}