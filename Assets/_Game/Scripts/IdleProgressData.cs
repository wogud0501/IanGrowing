using System;
using System.Collections.Generic;

namespace IanGame
{
    [Serializable]
    public class IdleActivityLedger
    {
        public string activityId;
        public long completedCycles;
        public long lastSettledUtcTicks;
        public double elapsedSeconds;
    }

    [Serializable]
    public class BattleTurn
    {
        public int number;
        public int damageToEnemy;
        public int damageToPlayer;
        public int enemyHealth;
        public int playerHealth;
    }

    [Serializable]
    public class BattleRecord
    {
        public string id;
        public string enemyId;
        public bool won;
        public int healthBefore;
        public int healthAfter;
        public int enemyRemaining;
        public long gold;
        public long experience;
        public List<BattleTurn> turns = new List<BattleTurn>();
    }

    [Serializable]
    public class OfflineProgressReport
    {
        public string id;
        public string activityId;
        public string stopReason;
        public long fromUtcTicks;
        public long toUtcTicks;
        public long gold;
        public long experience;
        public double elapsedSeconds;
        public double eligibleSeconds;
        public double activitySeconds;
        public int cycles;
        public int workCycles;
        public int studyCycles;
        public int battleWins;
        public int battleLosses;
        public int educationExperience;
        public int knowledge;
        public int hunger;
        public int thirst;
        public int health;
        public int mental;
        public bool capped;
        public bool clockReversed;
        public bool acknowledged;
    }

    [Serializable]
    public class IdleProgressSnapshot
    {
        public int schema = 1;
        public bool active;
        public bool offlineEnabled = true;
        public string activityId;
        public string sessionId;
        public string educationId;
        public string jobId;
        public string stopReason;
        public double cycleProgressSeconds;
        public long accountedUtcTicks;
        public long sequence;
        public int sessionCycles;
        public List<IdleActivityLedger> ledgers = new List<IdleActivityLedger>();
        public List<BattleRecord> battles = new List<BattleRecord>();
        public OfflineProgressReport report;
    }

    public static class OfflineRewardCalculator
    {
        public static double EligibleSeconds(long previous, long now, double cap, out bool reversed, out bool capped)
        {
            reversed = now < previous;
            capped = previous > 0 && now > previous && (now - previous) / (double)TimeSpan.TicksPerSecond > cap;
            return TimeManager.SafeOfflineSeconds(previous, now, cap);
        }
    }
}
