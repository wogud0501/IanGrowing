using System;
namespace IanGame {
public struct MissionConditionResult {public bool satisfied;public double current,target;}
public static class MissionConditionEvaluator {
 static MissionConditionResult Result(double value,double target)=>new MissionConditionResult{current=Math.Max(0,Math.Min(value,target)),target=Math.Max(1,target),satisfied=value>=target};
 public static MissionConditionResult Evaluate(MissionCondition c,IMissionStateProvider provider,int depth=0){
 if(c==null||provider==null||depth>32)return new MissionConditionResult{target=1};
 switch(c.kind){
 case MissionConditionKind.All:case MissionConditionKind.Any:
 bool all=c.children!=null&&c.children.Length>0,any=false;double sum=0,total=0,best=0;
 if(c.children!=null)foreach(var child in c.children){var r=Evaluate(child,provider,depth+1);all&=r.satisfied;any|=r.satisfied;sum+=r.current;total+=r.target;best=Math.Max(best,r.current/Math.Max(1,r.target));}
 return c.kind==MissionConditionKind.All?new MissionConditionResult{satisfied=all,current=sum,target=Math.Max(1,total)}:new MissionConditionResult{satisfied=any,current=best,target=1};
 case MissionConditionKind.StatAtLeast:return Result(provider.Stat(c.stat),c.target);
 case MissionConditionKind.CounterAtLeast:return Result(provider.Counter(c.counter),c.target);
 case MissionConditionKind.AgeAtLeast:return Result(provider.Age,c.target);
 case MissionConditionKind.StageAtLeast:return Result((int)provider.Stage,c.target);
 case MissionConditionKind.DaysSurvived:var days=Result(provider.DaysSurvived,c.target);days.satisfied&=provider.Alive;return days;
 case MissionConditionKind.CashAtLeast:return Result(provider.Cash,c.target);
 case MissionConditionKind.Alive:return Result(provider.Alive?1:0,1);
 case MissionConditionKind.ItemOwned:return Result(provider.HasItem(c.item)?1:0,1);
 case MissionConditionKind.MissionClaimed:return Result(provider.Claimed(c.missionId)?1:0,1);
 case MissionConditionKind.ContentUnlocked:return Result(provider.Unlocked(c.unlock)?1:0,1);
 default:return new MissionConditionResult{target=1};
 }
 }
}
}