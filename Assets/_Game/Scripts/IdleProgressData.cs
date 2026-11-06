using System;using System.Collections.Generic;
namespace IanGame {
[Serializable]public class IdleActivityLedger {public string activityId;public long completedCycles,lastSettledUtcTicks;public double elapsedSeconds;}
[Serializable]public class BattleTurn {public int number,damageToEnemy,damageToPlayer,enemyHealth,playerHealth;}
[Serializable]public class BattleRecord {
 public string id,enemyId;public bool won;public int healthBefore,healthAfter,enemyRemaining;public long gold,experience;public List<BattleTurn> turns=new List<BattleTurn>();
}
[Serializable]public class OfflineProgressReport {
 public string id,activityId,stopReason;public long fromUtcTicks,toUtcTicks,gold,experience;public double elapsedSeconds,eligibleSeconds,activitySeconds;public int cycles,workCycles,studyCycles,battleWins,battleLosses,educationExperience,knowledge,hunger,thirst,health,mental;public bool capped,clockReversed,acknowledged;
}
[Serializable]public class IdleProgressSnapshot {
 public int schema=1;public bool active,offlineEnabled=true;public string activityId,sessionId,educationId,jobId,stopReason;public double cycleProgressSeconds;public long accountedUtcTicks,sequence;public int sessionCycles;
 public List<IdleActivityLedger> ledgers=new List<IdleActivityLedger>();public List<BattleRecord> battles=new List<BattleRecord>();public OfflineProgressReport report;
}
public static class OfflineRewardCalculator {
 public static double EligibleSeconds(long previous,long now,double cap,out bool reversed,out bool capped){reversed=now<previous;capped=previous>0&&now>previous&&(now-previous)/(double)TimeSpan.TicksPerSecond>cap;return TimeManager.SafeOfflineSeconds(previous,now,cap);}
}
}
