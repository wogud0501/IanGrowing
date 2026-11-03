using UnityEngine;
namespace IanGame {
[CreateAssetMenu(menuName="Ian/Balance")]
public class GameBalance:ScriptableObject {
 [Range(0,100)]public int initialHappiness=50,initialStress=20,initialEnergy=70;
 [Min(0)]public int initialMoney=50;
 public int workMoney=18,workStress=15,workEnergy=-18;
 public int restMoney=0,restStress=-12,restEnergy=28;
 public int playMoney=-12,playHappiness=20,playStress=-10,playEnergy=-8;
 [Min(1)]public int workDays=1,restDays=1,playDays=1;
 [Range(0,1)]public float eventChance=.4f;
 public bool enableGameOver=true;
 [Range(0,100)]public int gameOverStress=100,gameOverEnergy=0;
 public bool IsGameOver(PlayerStats s)=>enableGameOver&&s!=null&&s.Stress>=gameOverStress&&s.Energy<=gameOverEnergy;
}
}