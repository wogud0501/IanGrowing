using System;using System.Collections.Generic;
namespace IanGame {
public enum MissionChoiceOutcome {None=0,AcceptedOffer=1,DeclinedOffer=2,SelectedCareer=3,Retired=4,KeptWorking=5,RequestedLifeReview=6}
public interface IMissionFeatureSource {MissionFeature Features{get;}bool IsAvailable{get;}event Action<MissionActivityEvent> ActivityProduced;bool HasItem(MissionItemKey item);}
public enum MissionState {Locked,Available,InProgress,Completed,RewardClaimed,Planned}
[Serializable]public class MissionProgress {public string missionId;public MissionState state;public double current,target;public long eventCount;public string[] prerequisiteIds;}
[Serializable]public class MissionCounterValue {public MissionCounter counter;public long value;}
public enum PendingMissionRewardKind {GachaTicket,Item}
[Serializable]public class PendingMissionReward {public string id;public PendingMissionRewardKind kind;public MissionItemKey item;public long amount;public bool delivered;}
[Serializable]public class MissionSnapshot {
 public string activeMissionId="M001";public int startDay=1;public long eventSequence,growthPoints;
 public List<MissionCounterValue> counters=new List<MissionCounterValue>();
 public List<string> seenEventIds=new List<string>(),completedMissionIds=new List<string>(),claimedMissionIds=new List<string>(),walletReceiptIds=new List<string>(),milestoneIds=new List<string>();
 public List<ContentUnlock> unlocks=new List<ContentUnlock>();public List<PendingMissionReward> pendingRewards=new List<PendingMissionReward>();public List<MissionProgress> progress=new List<MissionProgress>();
 public List<MissionChoiceOutcome> choiceOutcomes=new List<MissionChoiceOutcome>();public bool finalReviewRequested;
}
public struct MissionActivityEvent {public string id;public MissionCounter counter;public long amount;}
public interface IMissionStateProvider {
 int Stat(CoreStat stat);long Counter(MissionCounter counter);int Age{get;}LifeStage Stage{get;}int DaysSurvived{get;}long Cash{get;}bool Alive{get;}
 bool HasItem(MissionItemKey item);bool ChoiceOutcome(MissionChoiceOutcome outcome);bool Claimed(string missionId);bool Unlocked(ContentUnlock unlock);bool Supports(MissionFeature features);
}
public interface IMissionEconomyStateProvider {long NetWorth{get;}}
public interface IPendingMissionRewardGateway {bool TryDeliver(PendingMissionReward reward);}
}
