using System;
using System.Linq;
using UnityEngine;
namespace IanGame {
// Uses the existing wallet, atomic transaction guard, receipts, mission source and save snapshot.
public partial class EconomySystem {
 public HousingCatalog housingCatalog;
 public HousingSnapshot Housing {get{EnsureHousing();return Clone(state.housing);}}
 void EnsureHousing(){
  if(state.housing!=null&&state.housing.schema==1)return;
  state.housing=new HousingSnapshot();
  if(housingCatalog!=null)foreach(var f in housingCatalog.furniture.Where(f=>f.starter))state.housing.owned.Add(f.id);
 }
 public static bool ValidHousing(HousingSnapshot h)=>h==null||h.schema==0||h.schema==1&&!string.IsNullOrEmpty(h.residenceId)&&!string.IsNullOrEmpty(h.wallpaper)&&!string.IsNullOrEmpty(h.floor)&&!string.IsNullOrEmpty(h.bathroom)&&h.owned!=null&&h.placed!=null&&h.owned.All(id=>!string.IsNullOrWhiteSpace(id))&&h.owned.Distinct().Count()==h.owned.Count&&h.placed.All(p=>p!=null&&h.owned.Contains(p.id)&&float.IsFinite(p.position.x)&&float.IsFinite(p.position.y)&&Math.Abs(p.position.x)<=2.8f&&p.position.y>=-1.3f&&p.position.y<=-.35f)&&h.placed.Select(p=>p.id).Distinct().Count()==h.placed.Count;
 bool HousingReady=>housingCatalog!=null&&game!=null&&game.State==GameState.Playing&&!game.BirthIntroductionPending&&(GameContext.Instance==null||!GameContext.Instance.transitions.IsTransitioning)&&!game.idle.IsPersistenceSuspended;
 public bool CanBuyFurniture(string id,out string reason){
  EnsureHousing();var f=housingCatalog==null?null:housingCatalog.Furniture(id);
  reason=!HousingReady?"현재는 구매할 수 없습니다.":f==null?"등록되지 않은 가구입니다.":state.housing.owned.Contains(id)?"이미 보유한 가구입니다.":f.price<=0?"구매 가격을 확인하세요.":!CanAfford(f.price)?$"소지금이 {f.price-Cash:N0}G 부족합니다.":null;return reason==null;
 }
 public bool BuyFurniture(string receipt,string id){string reason;if(!CanBuyFurniture(id,out reason))return false;var f=housingCatalog.Furniture(id);bool ok=Commit(receipt,TransactionKind.Expense,f.price,"가구 구매 · "+f.title,id,()=>state.housing.owned.Add(id));if(ok)Signal(receipt+"/furniture",MissionCounter.FurnitureBought);return ok;}
 public bool CanMoveResidence(string id,out string reason){
  EnsureHousing();var r=housingCatalog==null?null:housingCatalog.Residence(id);
  reason=!HousingReady?"현재는 이사할 수 없습니다.":r==null?"등록되지 않은 집입니다.":id==state.housing.residenceId?"현재 거주 중입니다.":game.growth.Age<r.minAge?$"{r.minAge}세부터 입주할 수 있습니다.":r.movingCost<=0?"이사 비용을 확인하세요.":!CanAfford(r.movingCost)?$"소지금이 {r.movingCost-Cash:N0}G 부족합니다.":null;return reason==null;
 }
 public bool MoveResidence(string receipt,string id){string reason;if(!CanMoveResidence(id,out reason))return false;var r=housingCatalog.Residence(id);return Commit(receipt,TransactionKind.Expense,r.movingCost,"이사 · "+r.title,id,()=>state.housing.residenceId=id);}
 // Placement never charges money. Ownership and a bounded room position are mandatory.
 public bool PlaceFurniture(string id,Vector2 position,bool place=true){
  EnsureHousing();var f=housingCatalog==null?null:housingCatalog.Furniture(id);
  if(!HousingReady||busy||f==null||f.starter||!state.housing.owned.Contains(id)||!float.IsFinite(position.x)||!float.IsFinite(position.y)||Math.Abs(position.x)>2.8f||position.y< -1.3f||position.y>-.35f)return false;
  state.housing.placed.RemoveAll(p=>p.id==id);if(place)state.housing.placed.Add(new FurniturePlacement{id=id,position=position});Changed?.Invoke();return true;
 }
}
}
