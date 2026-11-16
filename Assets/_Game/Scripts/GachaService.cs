using System;
using System.Linq;
using System.Collections.Generic;
namespace IanGame {
public sealed class GachaService {
 readonly ItemService owner;readonly Random random=new Random();public GachaBatch LastBatch{get;private set;}
 internal GachaService(ItemService service){owner=service;}
 #if UNITY_EDITOR || DEVELOPMENT_BUILD
 Func<double> testRoll;public bool SetTestRoll(Func<double> source){string slot=System.IO.Path.GetFileName(owner.Game.saves.BasePath);if(!slot.StartsWith("qa-")&&!slot.StartsWith("phase26-"))return false;testRoll=source;return true;}
 #endif
 double Roll(){
 #if UNITY_EDITOR || DEVELOPMENT_BUILD
  if(testRoll!=null)return Math.Max(0,Math.Min(.999999999999,testRoll()));
 #endif
  return random.NextDouble();
 }
 public PoolProgress Progress(string id)=>owner.Capture().pools.FirstOrDefault(p=>p.poolId==id)??new PoolProgress{poolId=id};
 public GachaQuote Quote(string poolId,int count,bool material=false){var q=new GachaQuote{count=count};if(!owner.CanUse(out q.reason))return q;var p=owner.catalog.Pool(poolId);q.reason=owner.catalog.Validate();if(q.reason!=null)return q;if(p==null||count!=1&&count!=10){q.reason="Pool 또는 뽑기 수량을 확인하세요.";return q;}if(owner.Game.growth.Age<p.minAge){q.reason=p.minAge+"세부터 이용할 수 있습니다.";return q;}
  if(Progress(poolId).total>1000000000-count){q.reason="누적 횟수 한도에 도달했습니다.";return q;}
  if(material){long cost=(long)p.materialCost*count;if(cost>int.MaxValue||owner.Inventory.Count(p.materialItemId)<cost){q.reason="수집 조각이 부족합니다.";return q;}q.material=(int)cost;}
  else{q.tickets=p.ticketFirst?Math.Min(count,owner.Inventory.Count(p.ticketItemId)):0;long cash=(long)(count-q.tickets)*p.costAmount;if(cash>int.MaxValue||!owner.Game.economy.CanAfford((int)cash)){q.reason="가챠권 또는 인게임 현금이 부족합니다.";return q;}q.cash=(int)cash;}
  if(p.availableItems.Where(e=>owner.catalog.Item(e.itemId).minAge<=owner.Game.growth.Age).Any(e=>(long)owner.Inventory.Count(e.itemId)+count>owner.catalog.Item(e.itemId).maxStack))q.reason="보관 한도에 도달한 아이템을 먼저 정리하세요.";return q;
 }
 PoolProgress MutableProgress(string id){var p=owner.Model.pools.FirstOrDefault(p=>p.poolId==id);if(p==null){p=new PoolProgress{poolId=id};owner.Model.pools.Add(p);}return p;}
 PityProgress Pity(PoolProgress p,PityRule r){var s=p.pity.FirstOrDefault(s=>s.ruleId==r.id);if(s==null){s=new PityProgress{ruleId=r.id};p.pity.Add(s);}return s;}
 public PityRule[] NextGuarantees(GachaPoolDefinition pool){var progress=Progress(pool.poolId);return pool.pityRules.Where(r=>{var s=progress.pity.FirstOrDefault(s=>s.ruleId==r.id);return (s?.progress??0)+1>=r.threshold&&(r.reset!=PityReset.Never||(s?.reached??0)==0);}).ToArray();}
 public Dictionary<string,double> Probabilities(string id,bool nextPull=false){var p=owner.catalog.Pool(id);return Distribution(p,nextPull?NextGuarantees(p):Array.Empty<PityRule>());}
 Dictionary<string,double> Distribution(GachaPoolDefinition pool,PityRule[] guarantees){
  var chosen=guarantees.OrderByDescending(r=>r.priority).FirstOrDefault(r=>!string.IsNullOrEmpty(r.guaranteedItem));if(chosen!=null)return new Dictionary<string,double>{{chosen.guaranteedItem,1}};
  int min=guarantees.Where(r=>!string.IsNullOrEmpty(r.minimumRarityId)).Select(r=>owner.catalog.Rarity(r.minimumRarityId).rank).DefaultIfEmpty(0).Max();
  var eligible=pool.availableItems.Where(e=>owner.catalog.Item(e.itemId).minAge<=owner.Game.growth.Age&&owner.catalog.Rarity(owner.catalog.Item(e.itemId).rarityId).rank>=min).ToArray();var rarity=pool.rarityWeights.Where(r=>eligible.Any(e=>owner.catalog.Item(e.itemId).rarityId==r.rarityId)).ToArray();double total=rarity.Sum(r=>(double)r.weight);var odds=new Dictionary<string,double>();foreach(var r in rarity){var entries=eligible.Where(e=>owner.catalog.Item(e.itemId).rarityId==r.rarityId).ToArray();double itemWeight=entries.Sum(e=>(double)e.weight);foreach(var e in entries)odds[e.itemId]=r.weight/total*e.weight/itemWeight;}return odds;
 }
 string Pick(Dictionary<string,double> odds){double roll=Roll(),sum=0;foreach(var e in odds){sum+=e.Value;if(roll<sum)return e.Key;}return odds.Last().Key;}
 public bool Pull(string receipt,string poolId,int count,bool material,out GachaBatch batch,out string reason){batch=null;var q=Quote(poolId,count,material);reason=q.reason;if(!q.CanPull)return false;var pool=owner.catalog.Pool(poolId);var output=new List<GachaResult>();
  bool ok=owner.Transaction(receipt,()=>{
   if(q.tickets>0)owner.Model.inventory.First(e=>e.itemId==pool.ticketItemId).count-=q.tickets;
   if(q.material>0)owner.Model.inventory.First(e=>e.itemId==pool.materialItemId).count-=q.material;
   // Spend first. No random result is generated when currency is insufficient.
   if(q.cash>0&&!owner.Game.economy.TryTransaction(receipt+"/cost",TransactionKind.Expense,q.cash,"수집 가챠 · "+pool.displayName))return false;
   var progress=MutableProgress(poolId);
   for(int i=0;i<count;i++){
    progress.total++;var reached=new List<PityRule>();foreach(var rule in pool.pityRules){var s=Pity(progress,rule);s.progress++;if(s.progress>=rule.threshold&&(rule.reset!=PityReset.Never||s.reached==0)){s.reached++;reached.Add(rule);}}
    string id=Pick(Distribution(pool,reached.ToArray()));var item=owner.catalog.Item(id);bool first=!owner.Inventory.Discover(id);if(!owner.Add(id,1,receipt+"/draw/"+i))return false;
    foreach(var rule in pool.pityRules){var s=Pity(progress,rule);if(reached.Contains(rule)){s.applied++;var e=new ItemEvent{kind="pity",itemId=id,poolId=poolId,ruleId=rule.id,amount=rule.threshold,transactionId=receipt+"/pity/"+i+"/"+rule.id};owner.QueuePity(e);if(rule.reset==PityReset.Threshold)s.progress=0;}
     if(rule.reset==PityReset.MatchingDrop&&(!string.IsNullOrEmpty(rule.guaranteedItem)?rule.guaranteedItem==id:!string.IsNullOrEmpty(rule.minimumRarityId)&&owner.catalog.Rarity(item.rarityId).rank>=owner.catalog.Rarity(rule.minimumRarityId).rank))s.progress=0;
    }
    output.Add(new GachaResult{itemId=id,rarityId=item.rarityId,isNew=first,duplicate=!first,pityRules=reached.Select(r=>r.id).ToArray()});
   }
   owner.Signal(receipt+"/mission",MissionCounter.GachaDraws,count);return true;
  },out reason);
  if(!ok)return false;batch=new GachaBatch{transactionId=receipt,poolId=poolId,tickets=q.tickets,cash=q.cash,material=q.material,results=output.ToArray()};LastBatch=batch;owner.PublishBatch(batch);return true;
 }
}
}
