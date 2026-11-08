using System;using System.Linq;using UnityEngine;
namespace IanGame {
public class JobService {
 readonly Phase23Services owner;public JobService(Phase23Services value){owner=value;}
 public JobData Current=>owner.catalog==null?null:owner.catalog.Job(owner.Progress.employment.currentJobId);
 public bool Eligible(JobData job)=>job!=null&&!job.specialUnlock&&!job.businessIncome&&job.BaseDailyWage>0&&owner.Game.growth.Age>=Math.Max(19,job.RequiredAge)&&owner.Game.stats.Knowledge>=job.RequiredKnowledge&&owner.Game.stats.Social>=job.RequiredSocial&&owner.Game.stats.Health>=job.RequiredHealth&&owner.Progress.employment.experience>=job.RequiredExperience;
 public JobData[] Search()=>owner.catalog.jobs.Where(Eligible).ToArray();
}
public class EmploymentService {
 readonly Phase23Services owner;public EmploymentService(Phase23Services value){owner=value;}
 public EmploymentProgress Progress=>owner.Progress.employment;
 public float InterviewChance(JobData job)=>Mathf.Clamp(job.interviewBaseChance+owner.Game.stats.Social*.003f+owner.Game.stats.Knowledge*.001f+owner.Game.stats.Mental*.001f,.05f,.95f);
 public bool Can(int action,out string reason){
 reason=null;var p=Progress;var j=owner.Jobs.Current;
 if(owner.Game.growth.Age<19){reason="일반 취업은 19세부터 가능합니다.";return false;}
 if(action==20&&p.resumeWritten){reason="이력서가 이미 작성되었습니다.";return false;}
 if(action==21&&(!p.resumeWritten||!owner.Jobs.Eligible(owner.catalog.Job(p.selectedJobId))||p.currentJobId!="unemployed")){reason="이력서, 지원 자격, 현재 고용 상태를 확인하세요.";return false;}
 if(action==21&&p.history.Any(h=>h.kind=="interview"&&h.jobId==p.selectedJobId&&h.day==owner.Game.clock.Day)){reason="동일 직업 면접은 다음 날 재응시하세요.";return false;}
 if((action==0||action==22||action==23)&&(j==null||j.BaseDailyWage<=0||j.specialUnlock)){reason="먼저 채용되어야 합니다.";return false;}
 if(action==22){var c=j.PromotionConditions;int count=p.work.Count(w=>w.jobId==j.JobId);if(p.rank>=c.maxRank||p.experience<c.experience*(p.rank+1)||count<c.workCount*(p.rank+1)||owner.Game.stats.Knowledge<c.knowledge||owner.Game.stats.Social<c.social){reason="승진에 필요한 경력·근무·지식·사회성이 부족합니다.";return false;}}
 return true;
 }
 public void Complete(int action,string id){var p=Progress;string job=p.currentJobId;bool success=true;string kind="";
 switch(action){
 case 20:p.resumeWritten=true;kind="resume";owner.Signal(id,MissionCounter.ResumeWritten);break;
 case 21:job=p.selectedJobId;success=owner.Roll()<InterviewChance(owner.catalog.Job(job));kind="interview";owner.Signal(id+"/interview",MissionCounter.Interviews);if(success){p.currentJobId=job;p.rank=0;p.strikes=0;owner.Signal(id+"/hired",MissionCounter.Employment);}owner.Game.Log(success?"채용되었습니다. 자유시간이 퇴사했습니다.":"면접 탈락. 공부·대화·휴식 후 다시 지원할 수 있습니다.");break;
 case 22:p.rank++;kind="promotion";owner.Signal(id,MissionCounter.Promotions);break;
 case 23:p.currentJobId="unemployed";p.rank=0;p.strikes=0;kind="resigned";break;
 }
 p.history.Add(new EmploymentRecord{id=id,jobId=job,day=owner.Game.clock.Day,kind=kind,success=success});
 }
 public void EvaluateDismissal(string id){var p=Progress;var j=owner.Jobs.Current;if(j==null)return;var c=j.DismissalConditions;
 if(owner.Game.stats.Mental<c.mentalBelow||owner.Game.stats.Health<c.healthBelow)p.strikes++;else p.strikes=Math.Max(0,p.strikes-1);
 if(p.strikes<Math.Max(1,c.strikes))return;p.history.Add(new EmploymentRecord{id=id+"/dismissed",jobId=j.JobId,day=owner.Game.clock.Day,kind="dismissed",success=true});p.currentJobId="unemployed";p.rank=0;p.strikes=0;owner.Game.Log("해고되었습니다. 이력서와 경력은 남아 있습니다. 회복 후 재지원하세요.");
 }
}
public class WorkActivityService {
 readonly Phase23Services owner;public WorkActivityService(Phase23Services value){owner=value;}
 public int Wage {get{var j=owner.Jobs.Current;return j==null?0:Math.Max(0,j.BaseDailyWage+owner.Progress.employment.rank*j.PromotionConditions.wageIncrease);}}
 internal void Complete(string id){var p=owner.Progress.employment;if(p.work.Any(w=>w.id==id))return;var j=owner.Jobs.Current;
 var record=new WorkRecord{id=id,jobId=j.JobId,day=owner.Game.clock.Day,wage=Wage};p.work.Add(record);p.workCount++;p.experience+=j.ExperienceReward;
 owner.Signal(id+"/work",MissionCounter.Work);Pay(record.id);var events=j.SpecialEventIds;if(events!=null&&events.Length>0&&owner.Roll()<.25f)owner.Game.Log(events[Math.Min(events.Length-1,(int)(owner.Roll()*events.Length))]);owner.Employment.EvaluateDismissal(id);
 }
 // Callers can only settle an existing completed work record. Economy receipts survive scenes and saves.
 public bool Pay(string workId){var r=owner.Progress.employment.work.FirstOrDefault(w=>w.id==workId);if(r==null||r.paid)return false;
 if(!owner.Game.economy.PaySalary(r.id+"/salary",r.wage,r.jobId))return false;
 r.paid=true;owner.Signal(r.id+"/salary-event",MissionCounter.Salary);owner.Game.Log($"급여 {r.wage:N0}G 수령. 근무 기록 {r.id}");return true;}
}
}
