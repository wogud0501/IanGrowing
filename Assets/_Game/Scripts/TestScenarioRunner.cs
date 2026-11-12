#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace IanGame {
public enum QAStatus {NOT_RUN,PASS,FAIL,NOT_IMPLEMENTED,BLOCKED,MANUAL}
public sealed class QABlockedException:Exception {public QABlockedException(string message):base(message){}}
public sealed class QANotImplementedException:Exception {public QANotImplementedException(string message):base(message){}}
[Serializable]public sealed class QATestResult {public string id,title,category,reason,utc,statusName;public QAStatus status;public int checks;public double seconds;public List<string> log=new List<string>();}
public sealed class QAScenario {
 public string id,title,category,description,success;public QAStatus initial;public Func<QAScenarioContext,IEnumerator> run;public Func<DebugCommandService,IEnumerator> related;
}
public sealed class QAScenarioContext {
 public DebugCommandService Commands;public QATestResult Result;public GameManager Game=>Commands.Game;
 public void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);Result.checks++;Result.log.Add(label);}
 public void Require(bool ok,string reason){if(!ok)throw new QABlockedException(reason);}
}
public sealed class TestResultReporter {
 [Serializable]class Report {public string utc,unity,platform;public bool productionFilesUnchanged;public List<QATestResult> results;}
 public readonly List<QATestResult> Results=new List<QATestResult>();public readonly List<string> Log=new List<string>();public string LastPath{get;private set;}
 public void Note(string line){Log.Add(DateTime.Now.ToString("HH:mm:ss")+" "+line);if(Log.Count>200)Log.RemoveAt(0);}
 public QATestResult Get(QAScenario s){var r=Results.FirstOrDefault(r=>r.id==s.id);if(r==null){r=new QATestResult{id=s.id,title=s.title,category=s.category,status=s.initial};Results.Add(r);}return r;}
 public void Import(string json){var report=JsonUtility.FromJson<Report>(json);if(report?.results==null)return;foreach(var r in report.results){if(string.IsNullOrEmpty(r.id)||!Enum.IsDefined(typeof(QAStatus),r.status)||r.status==QAStatus.PASS&&r.checks<1)continue;Results.RemoveAll(x=>x.id==r.id);Results.Add(r);}}
 public string Text()=>string.Join("\n",Results.Select(r=>$"{r.status} | {r.category} | {r.id} {r.title} | checks={r.checks} | {r.reason}"))+"\n\n"+string.Join("\n",Log);
 public string Save(bool filesUnchanged){foreach(var result in Results)result.statusName=result.status.ToString();var folder=Path.Combine(Application.persistentDataPath,"DeveloperQA");Directory.CreateDirectory(folder);LastPath=Path.Combine(folder,"QA-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".json");var json=JsonUtility.ToJson(new Report{utc=DateTime.UtcNow.ToString("O"),unity=Application.unityVersion,platform=Application.platform.ToString(),productionFilesUnchanged=filesUnchanged,results=Results},true);File.WriteAllText(LastPath,json);
#if UNITY_EDITOR
 Directory.CreateDirectory("Docs/DeveloperQA");File.WriteAllText("Docs/DeveloperQA/LATEST_RESULTS.json",json);
#endif
 return LastPath;}
}
public sealed class TestScenarioRunner {
 public bool Running{get;private set;}public bool CancelRequested;public int Completed{get;private set;}public int Total{get;private set;}public string Current{get;private set;}public event Action Changed;
 public TestResultReporter Reporter{get;}public TestScenarioRunner(TestResultReporter reporter){Reporter=reporter;}
 public IEnumerator Run(IEnumerable<QAScenario> selected,DebugCommandService commands){if(Running)yield break;Running=true;CancelRequested=false;Completed=0;var list=selected.ToArray();Total=list.Length;
  try{foreach(var scenario in list){if(CancelRequested)break;Current=scenario.id;var r=Reporter.Get(scenario);r.checks=0;r.reason="";r.log.Clear();r.utc=DateTime.UtcNow.ToString("O");double start=Time.realtimeSinceStartupAsDouble;
   if(scenario.run==null){r.status=scenario.initial==QAStatus.NOT_RUN?QAStatus.NOT_IMPLEMENTED:scenario.initial;r.reason=scenario.description;}
   else {var stack=new Stack<IEnumerator>();Exception failure=null;try{commands.Sandbox.ResetData();commands.Fixture();stack.Push(scenario.run(new QAScenarioContext{Commands=commands,Result=r}));}catch(Exception e){failure=e;}
    while(stack.Count>0&&failure==null&&!CancelRequested){if(Time.realtimeSinceStartupAsDouble-start>30){failure=new QABlockedException("시나리오 30초 제한");break;}object current=null;bool more=false;try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){failure=e;}if(failure!=null)break;if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;}
    while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception e){failure=failure??e;}}
    if(CancelRequested){r.status=QAStatus.BLOCKED;r.reason="사용자가 중지했습니다.";}else if(failure!=null){r.status=failure is QANotImplementedException?QAStatus.NOT_IMPLEMENTED:failure is QABlockedException?QAStatus.BLOCKED:QAStatus.FAIL;r.reason=failure.Message;Reporter.Note(scenario.id+" "+failure);}else{r.status=r.checks>0?QAStatus.PASS:QAStatus.BLOCKED;r.reason=r.checks>0?"실제 검사 통과":"실행된 성공 조건이 없습니다.";}
   }
   r.seconds=Time.realtimeSinceStartupAsDouble-start;Completed++;Reporter.Note(r.status+" "+r.id+" "+r.reason);Changed?.Invoke();yield return null;
  }}finally{Running=false;Current="";Changed?.Invoke();Reporter.Save(commands.Sandbox.VerifyFiles());}
 }
}
}
#endif
