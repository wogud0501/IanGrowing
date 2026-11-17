using UnityEngine;

namespace IanGame
{
    [CreateAssetMenu(menuName = "Ian/Balance")]
    public class GameBalance : ScriptableObject
    {
        public InitialLifeSettings initialLife;
        public SurvivalSettings survival;

        [Range(0, 100)]
        public int initialHappiness = 50;

        [Range(0, 100)]
        public int initialStress = 20;

        [Range(0, 100)]
        public int initialEnergy = 70;

        [Min(0)]
        public int initialMoney = 50;
        public int workMoney = 18;
        public int workStress = 15;
        public int workEnergy = -18;
        public int restMoney = 0;
        public int restStress = -12;
        public int restEnergy = 28;
        public int playMoney = -12;
        public int playHappiness = 20;
        public int playStress = -10;
        public int playEnergy = -8;

        [Min(1)]
        public int workDays = 1;

        [Min(1)]
        public int restDays = 1;

        [Min(1)]
        public int playDays = 1;

        [Range(0, 1)]
        public float eventChance = .4f;
        public bool enableGameOver = true;

        [Range(0, 100)]
        public int gameOverStress = 100;

        [Range(0, 100)]
        public int gameOverEnergy = 0;

        public bool IsGameOver(PlayerStats s) =>
            enableGameOver && s != null && s.Stress >= gameOverStress && s.Energy <= gameOverEnergy;
    }
}
