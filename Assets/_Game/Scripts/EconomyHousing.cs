using System;
using System.Linq;
using UnityEngine;
namespace IanGame {
// Uses the existing wallet, atomic transaction guard, receipts, mission source and save snapshot.
public partial class EconomySystem {
 public HousingCatalog housingCatalog;
 public HousingSnapshot Housing {get{EnsureHousing();return Clone(state.housing);}}
 void EnsureHousing(){
  if(state.housing!=null&&state.housing.schema==1){housingCatalog?.placementPlan?.Migrate(state.housing);return;}
  state.housing=new HousingSnapshot();
  if(housingCatalog!=null)foreach(var f in housingCatalog.furniture.Where(f=>f.starter))state.housing.owned.Add(f.id);
  housingCatalog?.placementPlan?.Migrate(state.housing);
 }
 public static bool ValidHousing(HousingSnapshot h)=>h==null||h.schema==0||h.schema==1&&h.starterRevision>=0&&h.starterRevision<=1&&!string.IsNullOrEmpty(h.residenceId)&&!string.IsNullOrEmpty(h.wallpaper)&&!string.IsNullOrEmpty(h.floor)&&!string.IsNullOrEmpty(h.bathroom)&&h.owned!=null&&h.placed!=null&&h.owned.All(id=>!string.IsNullOrWhiteSpace(id))&&h.owned.Distinct().Count()==h.owned.Count&&h.placed.All(p=>p!=null&&h.owned.Contains(p.id)&&float.IsFinite(p.position.x)&&float.IsFinite(p.position.y)&&Math.Abs(p.position.x)<=3.9f&&p.position.y>=-2.5f&&p.position.y<=1.5f)&&h.placed.Select(p=>p.id).Distinct().Count()==h.placed.Count;
 bool HousingReady=>GrowthAccess.Independent(game)&&housingCatalog!=null&&game!=null&&game.State==GameState.Playing&&!game.BirthIntroductionPending&&(GameContext.Instance==null||!GameContext.Instance.transitions.IsTransitioning)&&!game.idle.IsPersistenceSuspended;
 public bool CanBuyFurniture(string id,out string reason){
  EnsureHousing();var f=housingCatalog==null?null:housingCatalog.Furniture(id);
  reason=!HousingReady?"현재는 구매할 수 없습니다.":f==null?"등록되지 않은 가구입니다.":state.housing.owned.Contains(id)?"이미 보유한 가구입니다.":f.price<=0?"구매 가격을 확인하세요.":!CanAfford(f.price)?$"소지금이 {f.price-Cash:N0}G 부족합니다.":null;return reason==null;
 }
 public bool BuyFurniture(string receipt,string id){string reason;if(!CanBuyFurniture(id,out reason))return false;var f=housingCatalog.Furniture(id);bool ok=Commit(receipt,TransactionKind.Expense,f.price,"가구 구매 · "+f.title,id,()=>state.housing.owned.Add(id));if(ok)Signal(receipt+"/furniture",MissionCounter.FurnitureBought);return ok;}
 public bool CanMoveResidence(string id,out string reason){
  EnsureHousing();var r=housingCatalog==null?null:housingCatalog.Residence(id);
  reason=!HousingReady?"현재는 이사할 수 없습니다.":state.property?.contract!=null?"부동산 중개소에서 기존 계약을 종료하세요.":r==null?"등록되지 않은 집입니다.":id==state.housing.residenceId?"현재 거주 중입니다.":game.growth.Age<r.minAge?$"{r.minAge}세부터 입주할 수 있습니다.":r.movingCost<=0?"이사 비용을 확인하세요.":!CanAfford(r.movingCost)?$"소지금이 {r.movingCost-Cash:N0}G 부족합니다.":null;return reason==null;
 }
 public bool MoveResidence(string receipt,string id){string reason;if(!CanMoveResidence(id,out reason))return false;var r=housingCatalog.Residence(id);return Commit(receipt,TransactionKind.Expense,r.movingCost,"이사 · "+r.title,id,()=>{if(state.property!=null)state.property.occupiedPropertyId="";state.housing.residenceId=id;state.housing.placed.Clear();state.housing.layoutVersion=0;});}
 // Placement never charges money. Ownership and a bounded room position are mandatory.
 public bool PlaceFurniture(string id,Vector2 position,bool place=true){
  EnsureHousing();var f=housingCatalog==null?null:housingCatalog.Furniture(id);
  if(!HousingReady||busy||f==null||f.starter||!state.housing.owned.Contains(id)||!float.IsFinite(position.x)||!float.IsFinite(position.y)||Math.Abs(position.x)>3.9f||position.y< -2.5f||position.y>1.5f)return false;
  if(place){if(housingCatalog.placementPlan!=null)position=housingCatalog.placementPlan.Snap(position*2)/2;if(!CanPlaceFurniture(id,position,out var reason))return false;}
  state.housing.placed.RemoveAll(p=>p.id==id);if(place)state.housing.placed.Add(new FurniturePlacement{id=id,position=position});state.housing.layoutAdjusted=0;Changed?.Invoke();return true;
 }
 public bool CanPlaceFurniture(string id,Vector2 position,out string reason){EnsureHousing();reason=null;if(!HousingReady||busy||!state.housing.owned.Contains(id)){reason="보유한 가구를 정상 플레이 중에 배치하세요.";return false;}if(housingCatalog.placementPlan!=null&&!housingCatalog.placementPlan.Validate(id,position*2,state.housing,out reason))return false;if(game.world!=null&&game.world.Active&&!game.world.Host.PlacementClear(id,position*2,out reason))return false;return true;}
 public bool FindFurniturePlacement(string id,bool change,out Vector2 position,out string reason){EnsureHousing();position=Vector2.zero;reason=null;var plan=housingCatalog.placementPlan;if(plan==null){position=housingCatalog.Furniture(id).position;return CanPlaceFurniture(id,position,out reason);}var current=state.housing.placed.Find(p=>p.id==id);var rule=plan.Rule(id);if(rule?.slots==null){reason="배치 구역이 없습니다.";return false;}foreach(var slot in rule.slots){position=plan.Snap(slot)/2;if(change&&current!=null&&Vector2.Distance(position,current.position)<.02f)continue;if(CanPlaceFurniture(id,position,out reason))return true;}reason="현재 가구와 출입 동선을 유지할 수 있는 다른 위치가 없습니다.";return false;}
}
}
