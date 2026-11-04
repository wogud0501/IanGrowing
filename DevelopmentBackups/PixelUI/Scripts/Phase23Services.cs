using System;using System.Linq;using UnityEngine;
namespace IanGame {
public class Phase23Services:MonoBehaviour,IMissionFeatureSource,IPendingMissionRewardGateway {
 public Phase23Catalog catalog;public GameManager Game{get;private set;}public Phase23Snapshot Progress{get;private set;}=new Phase23Snapshot();
 public EducationService Education{get;private set;}public SocialService Social{get;private set;}public JobService Jobs{get;private set;}public EmploymentService Employment{get;private set;}public WorkActivityService Work{get;private set;}
 public string SelectedNpcId="classmate",SelectedCareerId="vocational";
 public MissionFeature Features=>MissionFeature.AdvancedEducation|MissionFeature.Equipment|MissionFeature.Jobs|MissionFeature.DebtRepayment;
 public bool IsAvailable=>catalog!=null;public event Action<MissionActivityEvent> ActivityProduced;
 public void Configure(GameManager game){Game=game;Education=new EducationService(this);Social=new SocialService(this);Jobs=new JobService(this);Employment=new EmploymentService(this);Work=new WorkActivityService(this);game.missions.RegisterSource(this);game.clock.DayChanged+=OnDay;}
 void OnDay(int day){Social?.Decay(day);}void OnDestroy(){if(Game!=null){Game.clock.DayChanged-=OnDay;Game.missions.UnregisterSource(this);}}
 public float Roll()=>UnityEngine.Random.value;
 public void Signal(string id,MissionCounter counter,long amount=1){if(counter!=MissionCounter.None)ActivityProduced?.Invoke(new MissionActivityEvent{id=id,counter=counter,amount=amount});}
 public bool Handles(int action)=>catalog!=null&&(action==0||action>=7);
 public SceneId Location=>GameContext.Instance==null?SceneId.Home:GameContext.Instance.transitions.CurrentScene;
 public SceneId Destination(int action){var a=catalog.Find(action);return a!=null&&a.locations!=null&&a.locations.Length>0?a.locations[0]:SceneId.Home;}
 public bool Can(int action,out string reason){
 reason=null;var a=catalog.Find(action);if(a==null){reason="등록되지 않은 활동입니다.";return false;}
 if(GameContext.Instance!=null&&GameContext.Instance.transitions.IsTransitioning){reason="이동 중입니다.";return false;}
 if(Game.growth.Age<a.minAge||Game.growth.Age>a.maxAge||Game.growth.Stage<a.requiredStage){reason="성장 단계 또는 나이 조건을 확인하세요.";return false;}
 if(a.locations!=null&&a.locations.Length>0&&!a.locations.Contains(Location)){reason=string.Join(" / ",a.locations.Select(s=>s.ToString()))+"에서 수행하세요.";return false;}
 if(a.maxCount>0&&Progress.activities.Count(r=>r.action==action)>=a.maxCount){reason="수행 횟수 제한에 도달했습니다.";return false;}
 if(action==9||action==18||action==19)return Social.Can(action,out reason);
 if(action==0||action>=20&&action<=23)return Employment.Can(action,out reason);
 if(action==24||action==25){var item=action==24?MissionItemKey.StarterHeadphones:MissionItemKey.StarterEquipment;if((!HasItem(item)&&!Game.missions.Claimed("M020"))||Progress.equipped.Contains(item)){reason="해당 스타터 아이템이 없거나 이미 장착했습니다.";return false;}return true;}
 if(action==26){if(Game.growth.Age<19||Game.economy.Debt<1){reason="상환할 부채가 없거나 아직 미성년입니다.";return false;}return true;}
 return Education.Can(action,out reason);
 }
 public ActionSpec Describe(int action){var a=catalog.Find(action);if(a==null)return default;var d=new ActionSpec{days=Math.Max(1,a.days),money=-Math.Max(0,a.cost),core=a.delta,message=a.message};
 if(action==26)d.money=0;if(action==0){var j=Jobs.Current;if(j!=null){d.days=Math.Max(1,j.WorkingTime);d.core.health=-Math.Max(0,j.HealthCost);d.core.mental=-Math.Max(0,j.MentalCost);}}
 return d;
 }
 internal void Complete(int action,string id){if(Progress.activities.Any(r=>r.id==id))return;var a=catalog.Find(action);Progress.activities.Add(new ActivityReceipt{id=id,action=action,day=Game.clock.Day});
 if(action==9||action==18||action==19)Social.Complete(action,id,(SocialActivityData)a);
 else if(action==0)Work.Complete(id);
 else if(action>=20&&action<=23)Employment.Complete(action,id);
 else if(action==24||action==25){var item=action==24?MissionItemKey.StarterHeadphones:MissionItemKey.StarterEquipment;DeliverStarterRewards();if(!HasItem(item))TryDeliver(new PendingMissionReward{id="phase23/starter/"+item,kind=PendingMissionRewardKind.Item,item=item,amount=1});Progress.equipped.Add(item);Signal(id,a.missionEvent);}
 else if(action==26){long amount=Math.Min(a.cost,Game.economy.Debt);if(Game.economy.TryTransaction(id+"/repay",TransactionKind.DebtRepayment,amount,"부채 원금 상환"))Signal(id,MissionCounter.DebtRepaid,amount);}
 else Education.Complete(action,id,a);
 Game.missions.Refresh();Game.RefreshAvailability();
 }
 public bool HasItem(MissionItemKey item)=>Progress.owned.Contains(item);
 public bool TryDeliver(PendingMissionReward reward){if(reward.kind!=PendingMissionRewardKind.Item||reward.item!=MissionItemKey.StarterHeadphones&&reward.item!=MissionItemKey.StarterEquipment)return false;if(!Progress.deliveryIds.Contains(reward.id)){Progress.deliveryIds.Add(reward.id);if(!Progress.owned.Contains(reward.item))Progress.owned.Add(reward.item);}return true;}
 public void DeliverStarterRewards(){foreach(var r in Game.missions.State.pendingRewards.Where(r=>!r.delivered&&r.kind==PendingMissionRewardKind.Item))Game.missions.DeliverPending(r.id,this);}
 public void ResetLife(){Progress=new Phase23Snapshot();SelectedNpcId="classmate";SelectedCareerId="vocational";}
 public Phase23Snapshot Capture()=>JsonUtility.FromJson<Phase23Snapshot>(JsonUtility.ToJson(Progress));
 public void Restore(Phase23Snapshot value){Progress=value==null?new Phase23Snapshot():JsonUtility.FromJson<Phase23Snapshot>(JsonUtility.ToJson(value));}
 public static bool Valid(Phase23Snapshot p){
 if(p==null||p.education==null||p.social==null||p.employment==null||p.owned==null||p.equipped==null||p.deliveryIds==null||p.activities==null)return false;
 var e=p.education;var s=p.social;var j=p.employment;
 if(e.enrollments==null||e.exams==null||s.relationships==null||s.history==null||j.work==null||j.history==null||e.books<0||e.studies<0||e.homework<0||e.experience<0||e.preparation<0||e.preparation>100||s.experience<0||j.experience<0||j.workCount<0||j.rank<0||j.strikes<0||string.IsNullOrEmpty(j.currentJobId))return false;
 if(e.enrollments.Any(r=>r==null||string.IsNullOrEmpty(r.educationId)||r.studies<0||r.homework<0)||e.exams.Any(r=>r==null||string.IsNullOrEmpty(r.id)||string.IsNullOrEmpty(r.educationId)||r.day<1||r.score<0||r.score>100)||s.relationships.Any(r=>r==null||string.IsNullOrEmpty(r.npcId)||r.affinity<0||r.affinity>100||!Enum.IsDefined(typeof(RelationshipState),r.state))||j.work.Any(r=>r==null||string.IsNullOrEmpty(r.id)||string.IsNullOrEmpty(r.jobId)||r.day<1||r.wage<0))return false;
 if(s.history.Any(r=>r==null||string.IsNullOrEmpty(r.id)||string.IsNullOrEmpty(r.npcId)||r.day<1)||j.history.Any(r=>r==null||string.IsNullOrEmpty(r.id)||string.IsNullOrEmpty(r.jobId)||string.IsNullOrEmpty(r.kind)||r.day<1))return false;
 if(e.enrollments.Select(r=>r.educationId).Distinct().Count()!=e.enrollments.Count||e.exams.Select(r=>r.id).Distinct().Count()!=e.exams.Count||s.history.Select(r=>r.id).Distinct().Count()!=s.history.Count||j.history.Select(r=>r.id).Distinct().Count()!=j.history.Count)return false;
 if(p.owned.Concat(p.equipped).Any(i=>i==MissionItemKey.None||!Enum.IsDefined(typeof(MissionItemKey),i))||p.owned.Distinct().Count()!=p.owned.Count||p.equipped.Distinct().Count()!=p.equipped.Count||p.equipped.Any(i=>!p.owned.Contains(i))||p.deliveryIds.Any(string.IsNullOrEmpty)||p.deliveryIds.Distinct().Count()!=p.deliveryIds.Count)return false;
 return s.relationships.Select(r=>r.npcId).Distinct().Count()==s.relationships.Count&&j.work.Select(r=>r.id).Distinct().Count()==j.work.Count&&p.activities.All(r=>r!=null&&!string.IsNullOrEmpty(r.id))&&p.activities.Select(r=>r.id).Distinct().Count()==p.activities.Count;
 }
}
}
