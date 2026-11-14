#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace IanGame {
[Serializable]public class QALegacyEconomy {public long debt,otherAssets;public System.Collections.Generic.List<string> processedIds=new System.Collections.Generic.List<string>();}
[Serializable]public class QALegacyV3 {public int version=3,day,settledDay;public StatSnapshot stats;public ProgressSnapshot progression;public QALegacyEconomy economy=new QALegacyEconomy();public SurvivalSnapshot survival;public TimeSnapshot time;public string runId;public long lastSeenUtcTicks;}
[Serializable]public class QALegacyV4:QALegacyV3 {public MissionSnapshot missions;}
public static class PrePhase28Scenarios {
 public static void Register(GameManager g,TestScenarioRegistry registry){
  foreach(var course in g.phase23.catalog.education){var data=course;registry.Register(new QAScenario{id="education/course/"+data.educationId,title="교육 과정 · "+data.educationId,category="2.3 교육·직업",description="과정별 최소 연령 fixture 후 실제 입학/학습 실행",success="실제 과정/학습/저장 복원",run=c=>Course(c,data),initial=QAStatus.NOT_RUN});}
  foreach(string path in Directory.GetFiles(Application.persistentDataPath,"ian-black-comedy*.json*")){string source=path;registry.Register(new QAScenario{id="save/production-copy/"+Path.GetFileName(source),title="실제 저장 복사본 · "+Path.GetFileName(source),category="1 기본",description="원본 읽기만 수행, QA 채널에 복사하여 실제 로더 검사",success="원본 바이트 보존과 읽기 성공",initial=QAStatus.NOT_RUN,run=c=>Once(()=>{var bytes=File.ReadAllBytes(source);var channel=c.Game.saves.EnterTestingChannel("qa-user-copy-"+Guid.NewGuid().ToString("N")+".json");try{File.WriteAllBytes(c.Game.saves.BasePath,bytes);c.Check(c.Game.saves.TryRead(out var data,out var reason),"실제 원본 복사본 로드: "+reason);c.Check(File.ReadAllBytes(source).SequenceEqual(bytes),"실제 원본 파일 보존");}finally{c.Game.saves.RestoreTestingChannel(channel);}})});}
  registry.Register(new QAScenario{id="save/presence-integrity",title="선택 블록 표시·실제 자산 불일치 거부",category="1 기본",description="존재 표시를 위조한 QA 파일로 실제 로더 검증",success="유효 금융 데이터를 조용히 버리지 않고 거부",initial=QAStatus.NOT_RUN,run=c=>Once(()=>{c.Game.economy.EnsureFinance(c.Game.clock.Day,DateTime.UtcNow.Ticks);var d=c.Game.CaptureState();d.hasFinance=false;var channel=c.Game.saves.EnterTestingChannel("qa-presence-"+Guid.NewGuid().ToString("N")+".json");try{File.WriteAllText(c.Game.saves.BasePath,JsonUtility.ToJson(d));long cash=c.Game.economy.Cash;c.Check(!c.Game.saves.TryRead(out _,out _),"불일치 파일 거부");c.Check(c.Game.economy.Cash==cash,"현재 인생 초기화 없음");}finally{c.Game.saves.RestoreTestingChannel(channel);}})});
  registry.Register(new QAScenario{id="core/weighted-events",title="기존 가중 생활 이벤트",category="1 기본",description="기존 EventManager 확률1로 실행; Phase2.8 선택 이벤트와 별개",success="실제 이벤트 신호와 결과",initial=QAStatus.NOT_RUN,run=c=>Once(()=>{int fired=0;Action<LifeEvent> listener=e=>fired++;c.Game.lifeEvents.EventApplied+=listener;try{c.Check(c.Game.lifeEvents.Select(0)!=null&&c.Game.lifeEvents.Select(1)!=null,"양 끝 샘플 유효 이벤트");c.Check(c.Game.lifeEvents.TryEvent(c.Game.stats,1)!=null&&fired==1,"실제 가중 생활 이벤트 실행");}finally{c.Game.lifeEvents.EventApplied-=listener;}})});
  for(int v=1;v<=4;v++){int version=v;registry.Register(new QAScenario{id="save/version/"+v,title="v"+v+" 저장 복사본 호환",category="1 기본",description="격리 파일에 구 버전 fixture를 쓰고 실제 SaveManager로 읽고 저장",success="기존 현금/나이 보존 및 구버전 원본 보호",initial=QAStatus.NOT_RUN,run=c=>Once(()=>Migration(c,version))});}
  registry.Register(new QAScenario{id="survival/clamp-delta",title="8개 능력치 증감·극값",category="2.1 생존",description="실제 ApplyCore에 양/음수 극값 적용",success="0..100 범위와 스트레스 연동",initial=QAStatus.NOT_RUN,run=c=>Once(()=>{c.Game.stats.ApplyCore(new StatDelta{hunger=int.MaxValue,thirst=int.MaxValue,knowledge=int.MaxValue,health=int.MaxValue,mental=int.MaxValue,social=int.MaxValue,credit=int.MaxValue,happiness=int.MaxValue});c.Check(Enum.GetValues(typeof(CoreStat)).Cast<CoreStat>().All(s=>c.Game.stats.Get(s)==100),"양수 극값 overflow 없이 상한");c.Game.stats.ApplyCore(new StatDelta{hunger=int.MinValue,thirst=int.MinValue,knowledge=int.MinValue,health=int.MinValue,mental=int.MinValue,social=int.MinValue,credit=int.MinValue,happiness=int.MinValue});c.Check(Enum.GetValues(typeof(CoreStat)).Cast<CoreStat>().All(s=>c.Game.stats.Get(s)==0)&&c.Game.stats.Stress==100,"음수 극값 하한 및 스트레스");})});
 }
 static IEnumerator Once(Action a){a();yield break;}
 static IEnumerator Course(QAScenarioContext c,EducationData data){c.Commands.Fixture(data.minAge);yield return c.Commands.Navigate(data.requiredStage==LifeStage.Infancy?SceneId.Home:SceneId.School,true);
  if(data.requiredStage==LifeStage.Infancy){c.Check(!c.Game.CanPerformAction(7,out _)&&c.Commands.Act(17),"학교 입학은 거부하고 기초 학습 실행");}
  else{c.Check(c.Commands.Act(7)&&c.Game.phase23.Education.Current.educationId==data.educationId,"해당 과정 실제 입학");c.Commands.SetAllStats(100);c.Check(c.Commands.Act(14),"해당 과정 실제 학습");}
  c.Check(c.Commands.Save()&&c.Commands.Load(),"교육 상태 실제 디스크 복원");
 }
 static void Migration(QAScenarioContext c,int version){var game=c.Game;var saves=game.saves;var channel=saves.EnterTestingChannel("qa-legacy-"+Guid.NewGuid().ToString("N")+".json");try{
   string json;if(version<3)json="{\"version\":"+version+",\"day\":12,\"gameOver\":false,\"stats\":{\"happiness\":70,\"stress\":25,\"energy\":65,\"money\":12345}}";
   else{var sourceData=game.CaptureState();QALegacyV3 fixture=version==3?new QALegacyV3():new QALegacyV4{missions=sourceData.missions};fixture.version=version;fixture.day=sourceData.day;fixture.settledDay=sourceData.settledDay;fixture.stats=sourceData.stats;fixture.progression=sourceData.progression;fixture.survival=sourceData.survival;fixture.time=sourceData.time;fixture.runId=sourceData.runId;fixture.lastSeenUtcTicks=sourceData.lastSeenUtcTicks;json=JsonUtility.ToJson(fixture);}
   string source=saves.BasePath;File.WriteAllText(source,json);c.Check(saves.TryRead(out var data,out var why),"기존 SaveManager 읽기: "+why);
   c.Check(data.stats.money==(version<3?12345:1000000)&&data.progression.age==(version<3?19:30),"구 세이브 현금/나이 보존");
   if(version<3)c.Check(data.economy.debt==0&&data.birthIntroCompleted,"레거시 debt0/성인 호환 및 출생 재출력 없음");
   game.RestoreState(data);c.Check(saves.Save(game.stats,game.clock,false,out _),"마이그레이션 후 실제 수동 저장");
   if(version<4)c.Check(File.ReadAllText(source)==json&&saves.SavePath!=source,"구버전 파일 바이트 보존 및 별도 v4 저장");
   c.Check(saves.TryRead(out var loaded,out _)&&loaded.version==4&&loaded.stats.money==data.stats.money,"새 v4 복원");
  }finally{saves.RestoreTestingChannel(channel);}
 }
}
}
#endif
