using System;using System.Linq;using System.Collections.Generic;
namespace IanGame {
public static class MissionCatalogValidator {
 public static List<string> Validate(MissionCatalog catalog){
 var errors=new List<string>();if(catalog==null||catalog.missions==null){errors.Add("Missing catalog");return errors;}
 var map=new Dictionary<string,MissionData>();foreach(var m in catalog.missions){
 if(m==null||string.IsNullOrEmpty(m.Id)){errors.Add("Null mission/id");continue;}if(map.ContainsKey(m.Id)){errors.Add("Duplicate "+m.Id);continue;}map.Add(m.Id,m);
 if(m.rewards==null||m.rewards.gold<0||m.rewards.experience<0||m.rewards.growthPoints<0||m.rewards.gachaTickets<0)errors.Add("Invalid rewards "+m.Id);
 if(m.rewards!=null&&m.rewards.items!=null)foreach(var item in m.rewards.items)if(item==null||item.amount<1||item.item==MissionItemKey.None||!Enum.IsDefined(typeof(MissionItemKey),item.item))errors.Add("Invalid item reward "+m.Id);
 if(m.rewards!=null&&m.rewards.unlocks!=null)foreach(var unlock in m.rewards.unlocks)if(!Enum.IsDefined(typeof(ContentUnlock),unlock)||unlock==ContentUnlock.None)errors.Add("Invalid unlock "+m.Id);
 }
 foreach(var m in map.Values){foreach(var id in m.prerequisiteIds??new string[0])if(!map.ContainsKey(id))errors.Add("Missing prerequisite "+id);ValidateCondition(m.condition,map,errors,new HashSet<MissionCondition>(),0);}
 var visiting=new HashSet<string>();var done=new HashSet<string>();foreach(var id in map.Keys)Visit(id,map,visiting,done,errors);return errors;
 }
 static void Visit(string id,Dictionary<string,MissionData> map,HashSet<string> visiting,HashSet<string> done,List<string> errors){
 if(done.Contains(id)||!map.ContainsKey(id))return;if(!visiting.Add(id)){errors.Add("Prerequisite cycle "+id);return;}
 foreach(var next in (map[id].prerequisiteIds??new string[0]).Concat(Dependencies(map[id].condition)))Visit(next,map,visiting,done,errors);visiting.Remove(id);done.Add(id);
 }
 static IEnumerable<string> Dependencies(MissionCondition c,int depth=0){if(c==null||depth>32)yield break;if(c.kind==MissionConditionKind.MissionClaimed)yield return c.missionId;foreach(var child in c.children??new MissionCondition[0])foreach(var id in Dependencies(child,depth+1))yield return id;}
 static void ValidateCondition(MissionCondition c,Dictionary<string,MissionData> map,List<string> errors,HashSet<MissionCondition> path,int depth){
 if(c==null||depth>32){errors.Add("Missing/deep condition");return;}if(!path.Add(c)){errors.Add("Condition cycle");return;}
 if(!Enum.IsDefined(typeof(MissionConditionKind),c.kind)||c.target<0)errors.Add("Invalid condition");
 if(c.kind==MissionConditionKind.StatAtLeast&&!Enum.IsDefined(typeof(CoreStat),c.stat))errors.Add("Invalid stat");
 if(c.kind==MissionConditionKind.CounterAtLeast&&(!Enum.IsDefined(typeof(MissionCounter),c.counter)||c.counter==MissionCounter.None))errors.Add("Invalid counter");
 if(c.kind==MissionConditionKind.MissionClaimed&&!map.ContainsKey(c.missionId??""))errors.Add("Missing condition mission");
 if(c.kind==MissionConditionKind.ItemOwned&&(!Enum.IsDefined(typeof(MissionItemKey),c.item)||c.item==MissionItemKey.None))errors.Add("Invalid item condition");
 if(c.kind==MissionConditionKind.All||c.kind==MissionConditionKind.Any){if(c.children==null||c.children.Length==0)errors.Add("Empty composite");else foreach(var child in c.children)ValidateCondition(child,map,errors,path,depth+1);}path.Remove(c);
 }
}
}