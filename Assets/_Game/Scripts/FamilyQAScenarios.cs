#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace IanGame {
public static class FamilyQAScenarios {
 static IEnumerator Once(Action body){body();yield break;}
 public static void Register(TestScenarioRegistry r){
  r.Register(new QAScenario{id="family/residency",title="가족 거주·선택 독립 경계",category="가족·주거",description="18세 제한과19세 자동 이사 금지",success="기존 경제 실행 조건",run=c=>Once(()=>{c.Commands.Fixture(18,independentFixture:false);c.Check(FamilyLife.AtFamily(c.Game)&&!c.Game.economy.MoveResidence("qa/young-move","starter"),"18세 실제 이사 실행 거부");c.Commands.SetAge(19);c.Check(FamilyLife.AtFamily(c.Game),"19세 나이 변경만으로 독립하지 않음");c.Check(c.Game.economy.CanMoveResidence("starter",out _),"기존 잔액과19세로 선택 독립 가능");})});
  r.Register(new QAScenario{id="family/independence",title="독립 비용·가구 소유권",category="가족·주거",description="기존 이사 거래와 중복 방지",success="비용 차감·가족 가구 미이전",run=c=>Once(()=>{c.Commands.Fixture(19,independentFixture:false);c.Commands.SetCash(500);var before=c.Game.economy.Housing.owned.ToArray();c.Check(c.Game.economy.MoveResidence("qa/family/move","starter")&&c.Game.economy.Cash==0&&!FamilyLife.AtFamily(c.Game),"실제500G 독립 거래");c.Check(!c.Game.economy.MoveResidence("qa/family/move","starter"),"같은 거래/현재 집 중복 처리 거부");c.Check(c.Game.economy.Housing.owned.SequenceEqual(before)&&!c.Game.economy.Housing.owned.Any(x=>x.StartsWith("family-")),"가족 가구를 개인 자산으로 복제하지 않음");})});
  r.Register(new QAScenario{id="family/save",title="가족 관계·주거 저장",category="가족·주거",description="기존 SaveManager 전용 슬롯 round trip",success="관계·거주·개인 현금 복원",run=c=>Once(()=>{c.Commands.Fixture(1,independentFixture:false);FamilyLife.State(c.Game).motherRelationship=67;long cash=c.Game.economy.Cash;c.Check(c.Commands.Save(),"가족 상태 실제 저장");FamilyLife.State(c.Game).motherRelationship=2;c.Commands.SetCash(0);c.Check(c.Commands.Load()&&FamilyLife.AtFamily(c.Game)&&FamilyLife.State(c.Game).motherRelationship==67&&c.Game.economy.Cash==cash,"실제 파일에서 가족/개인 상태 복원");})});
  r.Register(new QAScenario{id="family/architecture",title="실제 가족 평면·좌석",category="가족·주거",description="실제 LifeWorld 벽·문·공동 식탁",success="씬 연결과 독점 예약",run=Layout});
  r.Register(new QAScenario{id="family/art-review",title="가족 동작·기기 육안 검수",category="가족·주거",description="생애주기별 모든 제스처·실제 모바일 품질은 직접 확인 필요",success="추가 육안·기기 검수",initial=QAStatus.MANUAL});
 }
 static IEnumerator Layout(QAScenarioContext c){yield return c.Commands.Navigate(SceneId.Home);var w=c.Game.world.Host;c.Require(w!=null,"생활 월드 필요");float settle=Time.unscaledTime+10;while(w.Busy&&Time.unscaledTime<settle)yield return null;c.Check(w.Travel(FamilyLife.Home),"부모님 집 실제 영역 진입");while(w.Busy)yield return null;c.Check(w.GetComponentsInChildren<FamilySharedDiningPresentation>().Length==1,"공동 식탁1개");foreach(string id in new[]{"family-parents-door","family-ian-door","family-bath-front"})c.Check(w.objects.Single(o=>o.id==id).GetComponent<WorldDoor>()!=null,"기존 문 계약 연결 "+id);var s=w.objects.Single(o=>o.id=="family-player-seat").GetComponent<WorldActivitySlot>();c.Check(s.TryReserve("player")&&!s.TryReserve("qa/other"),"플레이어 좌석 중복 점유 차단");s.Release("player");}
}
}
#endif
