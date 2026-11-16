#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace IanGame {
public static class CityLifeQAScenarios {
 public static void Register(TestScenarioRegistry r){r.Register(new QAScenario{id="city/age-guidance",category="2.8 도시 UX",title="연령별 추천·목적지 제한",description="기존 성장 권한을 사용하며 추천은 강제 퀘스트가 아닙니다.",success="0~6세 외출 추천 없음, 7세 외출/19세 금융 유지",run=Age});r.Register(new QAScenario{id="city/progress-save",category="2.8 도시 UX",title="도전과제 중복·저장·실패 복구",description="전용 QA 슬롯에서 신규 선택 저장 블록 검사",success="원본 파일 보존, 실제 디스크 복원 및 영수증 중복 차단",run=Save});r.Register(new QAScenario{id="city/economy-hooks",category="2.8 도시 UX",title="실제 계약·소유 주택 입주 도전과제",run=Housing});}
 static IEnumerator Housing(QAScenarioContext c){for(int mode=0;mode<2;mode++){c.Commands.Fixture(30,false,false);c.Game.world.State.cityLife=new CityLifeProgress();yield return c.Commands.Navigate(SceneId.City,true);var e=c.Game.economy;if(mode==0){c.Check(e.LeaseProperty("qa/city-lease","property-0",HousingTenure.MonthlyRent),"실제 월세 입주");c.Check(!e.LeaseProperty("qa/city-lease-again","property-0",HousingTenure.MonthlyRent),"중복 계약 차단");}else{c.Check(e.BuyProperty("qa/city-buy","property-0")&&e.OccupyProperty("qa/city-occupy","property-0"),"실제 매수 후 입주");c.Check(!e.OccupyProperty("qa/city-occupy-again","property-0"),"중복 입주 차단");}c.Check(c.Game.world.City.Counter("move")==1&&c.Game.world.City.Data.achievements.Count(id=>id=="move")==1,"성공한 입주만 한 번 기록");}c.Check(c.Commands.Sandbox.VerifyFiles(),"경제 경로 테스트 원본 세이브 보존");}

 static IEnumerator Age(QAScenarioContext c){c.Commands.Fixture(0,false,false);var city=c.Game.world.City;foreach(int age in new[]{0,1,6,7,18,19}){c.Commands.SetAge(age);var s=city.Suggest();c.Check(age>=7||s.room==null||FamilyLife.IsFamilyRoom(s.room),"독립 외출을 권하지 않는 추천 "+age);c.Check(city.Track("city",out _)==(age>=7),"외출 목적지 "+age);c.Check(city.Track("finance",out _)==(age>=19),"금융 목적지 "+age);}yield break;}
 static IEnumerator Save(QAScenarioContext c){c.Commands.Fixture(7,false,false);var city=c.Game.world.City;long money=c.Game.economy.Cash;city.Record("walk",1,"qa/city-walk");city.Record("walk",1,"qa/city-walk");c.Check(city.Counter("walk")==1&&c.Game.economy.Cash==money,"동일 영수증 재입력과 무료 재화 보상 없음");city.MarkGuide("qa/city-guide");city.Track("school",out _);c.Check(c.Commands.Save(),"QA 전용 슬롯 저장");string before=JsonUtility.ToJson(city.Data);city.Data.guideSeen.Clear();city.Track(null,out _);c.Check(c.Commands.Load()&&JsonUtility.ToJson(city.Data)==before,"실제 디스크의 가이드·목적지·진행률 복원");long day=city.Counter("day");c.Game.clock.SetDay(c.Game.clock.Day+1);c.Check(city.Counter("day")==day,"날짜 설정은 하루 완주 아님");c.Check(c.Commands.Sandbox.VerifyFiles(),"원본 세이브 해시 보존");yield break;}
}
}
#endif
