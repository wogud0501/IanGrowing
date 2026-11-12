using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
namespace IanGame {
// Rules and game time choose intentions at 4 Hz; visible movement is smooth.
// The existing scheduler remains the single owner of both friends and citizens.
public sealed class WorldNPCScheduler:MonoBehaviour {
 public LifeWorldController world;public WorldInteractable[] actors;public TMP_Text[] activityLabels=Array.Empty<TMP_Text>();
 sealed class Resident {
  public WorldInteractable actor;public WorldNPCDefinition definition;public WorldActorAnimation animation;public NPCScheduleEntry target;
  public int index=-1;public Vector2 facing=Vector2.down;public NPCDailyAction action;public long used=-1;public List<Vector2> path=new List<Vector2>();public int waypoint;
  public WorldInteractable portal,nextPortal;public WorldDoor door;public int portalPhase;public float retry,releaseSeatUntil,labelUntil,arrivingUntil;public bool active,atActivity,pendingAbstract;
 }
 Resident[] residents=Array.Empty<Resident>();readonly WorldNPCNavigation navigation=new WorldNPCNavigation();float nextDecision;long lastMinute=-1;string viewedZone;bool ready;WorldStreetLife street;readonly RaycastHit2D[] hits=new RaycastHit2D[16];ContactFilter2D hitFilter;
 public int AbstractUpdates{get;private set;}public int VisibleSteps{get;private set;}public int DoorPassages{get;private set;}public int ObjectUses{get;private set;}public int NavigationBakes=>navigation.Bakes;
 public bool OnCrossing=>residents.Any(r=>r.actor.zone=="city"&&r.actor.visual.enabled&&Road((Vector2)r.actor.transform.position-world.Service.rules.Zone("city").origin));
 public long Minute=>(long)(world.Service.Game.clock.Day-1)*1440+(long)(world.Service.Game.clock.SubDaySeconds/60);
 public static int Schedule(WorldNPCDefinition d,int day,int minute){int result=-1,latest=-1,mask=1<<((day-1)%7);for(int i=0;i<d.schedule.Length;i++){var entry=d.schedule[i];int at=entry.hour*60+entry.minute;if((entry.dayMask&mask)==0)continue;if(at<=minute&&at>=latest){result=i;latest=at;}}if(result>=0)return result;for(int i=d.schedule.Length-1;i>=0;i--)if((d.schedule[i].dayMask&mask)!=0)return i;return 0;}
 void Start(){street=world.GetComponentInChildren<WorldStreetLife>();hitFilter=new ContactFilter2D{useTriggers=false};if(world.Service==null)return;var defs=world.Service.rules.npcs;residents=actors.Take(defs.Length).Select((a,i)=>new Resident{actor=a,definition=defs[i],animation=a.GetComponent<WorldActorAnimation>()}).ToArray();world.Service.Restored+=Restore;ready=true;Restore();}
 void OnDisable(){if(world!=null&&world.Service!=null)world.Service.Restored-=Restore;foreach(var r in residents)r.door?.Release(r.definition.npcId);ready=false;}
 public void Restore(){if(!ready)return;navigation.Clear();lastMinute=-1;viewedZone=null;var data=world.Service.State.residents;
  foreach(var r in residents){r.door?.Release(r.definition.npcId);r.door=null;r.portal=null;r.nextPortal=null;r.portalPhase=0;r.index=-1;r.path.Clear();r.waypoint=0;r.used=-1;var saved=data?.FirstOrDefault(n=>n.id==r.definition.npcId);if(saved!=null&&world.Service.rules.Zone(saved.zone)!=null){r.actor.zone=saved.zone;r.actor.transform.position=world.Service.rules.Zone(saved.zone).origin+saved.position;r.facing=saved.facing;r.used=saved.completedUseKey;}else{r.actor.zone=r.definition.homeZone;var z=world.Service.rules.Zone(r.actor.zone);r.actor.transform.position=z.origin+z.spawn;}}
  Decide(true);
  foreach(var r in residents){var saved=data?.FirstOrDefault(n=>n.id==r.definition.npcId);if(saved==null||saved.minute!=Minute)continue;r.actor.zone=saved.zone;r.actor.transform.position=world.Service.rules.Zone(saved.zone).origin+saved.position;r.facing=saved.facing;r.action=saved.action;r.atActivity=saved.action!=NPCDailyAction.Walk;r.active=r.actor.zone==world.Service.State.zone;r.actor.visual.enabled=r.active;if(r.actor.shadow!=null)r.actor.shadow.enabled=r.active;r.animation?.Play(Motion(r.action),r.facing);r.animation?.Seek(30);}

 }
 public void Capture(){if(!ready)return;var state=world.Service.State;if(state.residents==null)state.residents=new List<NPCResidentSnapshot>();foreach(var r in residents){var item=state.residents.FirstOrDefault(n=>n.id==r.definition.npcId);if(item==null){item=new NPCResidentSnapshot{id=r.definition.npcId};state.residents.Add(item);}item.zone=r.actor.zone;item.position=(r.portal!=null?r.portal.Point:(Vector2)r.actor.transform.position)-world.Service.rules.Zone(r.actor.zone).origin;item.facing=r.facing;item.destinationZone=r.target?.zone;item.objectId=r.target?.objectId;item.action=r.action;item.scheduleIndex=r.index;item.minute=Minute;item.completedUseKey=r.used;}}
 void Update(){if(!ready||world.Service==null||world.Busy||world.Service.Game.State!=GameState.Playing||world.hud.shell.Depth>0)return;
  if(Time.unscaledTime>=nextDecision){nextDecision=Time.unscaledTime+.25f;Decide(false);}for(int i=0;i<residents.Length;i++)Step(residents[i],i,Mathf.Min(Time.deltaTime,.05f));
 }
 WorldInteractable Object(string id)=>string.IsNullOrEmpty(id)?null:world.objects.FirstOrDefault(o=>o.id==id);
 Vector2 Destination(Resident r){var z=world.Service.rules.Zone(r.target.zone);return z.origin+r.target.position;}
 void Decide(bool restoring){long now=Minute;bool minuteChanged=now!=lastMinute;string local=world.Service.State.zone;bool zoneChanged=local!=viewedZone;lastMinute=now;viewedZone=local;
  foreach(var r in residents){int index=Schedule(r.definition,world.Service.Game.clock.Day,(int)(world.Service.Game.clock.SubDaySeconds/60));bool changed=index!=r.index;r.target=r.definition.schedule[index];
   if(changed){if(r.action==NPCDailyAction.Sleep||r.action==NPCDailyAction.Rest)r.releaseSeatUntil=Time.unscaledTime+1;r.door?.Release(r.definition.npcId);r.door=null;r.portal=null;r.nextPortal=null;r.portalPhase=0;r.path.Clear();r.waypoint=0;r.index=index;r.atActivity=false;r.labelUntil=Time.unscaledTime+2;r.action=NPCDailyAction.Walk;}
   bool here=r.actor.zone==local;bool goesHere=r.target.zone==local;
   if(!here&&!goesHere&&(minuteChanged||changed||restoring||zoneChanged||r.pendingAbstract)){
    r.pendingAbstract=false;r.actor.zone=r.target.zone;r.actor.transform.position=Destination(r);r.action=r.target.action;r.atActivity=true;if(r.action==NPCDailyAction.Sleep){var bed=Object(r.target.objectId);if(bed!=null)r.actor.transform.position=(Vector2)bed.transform.position+Vector2.up*.1f;}UseObject(r);AbstractUpdates++;
   }else if(!here&&goesHere&&(changed||restoring||zoneChanged||minuteChanged)){
    var arrival=world.objects.FirstOrDefault(o=>o.kind==WorldInteractionKind.Door&&o.zone==local&&o.targetZone==r.actor.zone)??world.objects.FirstOrDefault(o=>o.kind==WorldInteractionKind.Door&&o.zone==local);
    r.actor.zone=local;r.actor.transform.position=arrival==null?world.Service.rules.Zone(local).origin+world.Service.rules.Zone(local).spawn:arrival.Point;
    var door=arrival==null?null:arrival.GetComponent<WorldDoor>();if(door!=null){door.Acquire(r.definition.npcId,world.Service.Hour,out _);door.Release(r.definition.npcId);}r.arrivingUntil=Time.unscaledTime+.3f;r.path.Clear();r.waypoint=0;
   }
   r.active=r.actor.zone==local;r.actor.visual.enabled=r.active;if(r.actor.shadow!=null)r.actor.shadow.enabled=r.active;if(r.actor.highlight!=null&&!r.active)r.actor.highlight.enabled=false;
   if(!r.active){int indexLabel=System.Array.IndexOf(residents,r);if(indexLabel<activityLabels.Length&&activityLabels[indexLabel]!=null)activityLabels[indexLabel].gameObject.SetActive(false);}
   if(!r.active&&r.animation!=null){r.animation.Play(Motion(r.action),r.facing);r.animation.Seek(30);}
  }
 }
 static ActorMotion Motion(NPCDailyAction action)=>action==NPCDailyAction.Sleep?ActorMotion.Sleep:action==NPCDailyAction.Eat?ActorMotion.Eat:action==NPCDailyAction.Work||action==NPCDailyAction.Shop?ActorMotion.Work:action==NPCDailyAction.Study?ActorMotion.UseComputer:action==NPCDailyAction.Rest?ActorMotion.SitDown:action==NPCDailyAction.Wake?ActorMotion.WakeUp:ActorMotion.Idle;
 void UseObject(Resident r){long key=((long)world.Service.Game.clock.Day<<16)+r.index;if(r.used==key)return;var target=Object(r.target.objectId);if(target!=null){target.RecordNPCUse(r.definition.npcId);ObjectUses++;}r.used=key;}
 WorldInteractable NextPortal(string from,string goal){var queue=new Queue<string>();var previous=new Dictionary<string,WorldInteractable>();queue.Enqueue(from);previous[from]=null;while(queue.Count>0){string zone=queue.Dequeue();foreach(var p in world.objects.Where(o=>o.kind==WorldInteractionKind.Door&&o.zone==zone)){if(previous.ContainsKey(p.targetZone))continue;previous[p.targetZone]=p;if(p.targetZone==goal){var edge=p;while(edge.zone!=from)edge=previous[edge.zone];return edge;}queue.Enqueue(p.targetZone);}}return null;}
 static bool Road(Vector2 local)=>local.y> -6.7f&&local.y< -2.1f&&Mathf.Abs(local.x)<1;
 bool Move(Resident r,int index,Vector2 target,float dt){Vector2 from=r.actor.transform.position;var next=Vector2.MoveTowards(from,target,r.definition.speed*dt);if(Vector2.Distance(from,next)<.00001f)return false;
  if(r.actor.zone==world.Service.State.zone&&Vector2.Distance(next,world.Player.Position)<.55f&&Vector2.Distance(next,world.Player.Position)<Vector2.Distance(from,world.Player.Position))return false;
  if(r.actor.zone=="city"){var z=world.Service.rules.Zone("city");if(!Road(from-z.origin)&&Road(next-z.origin)&&street!=null&&!street.CanEnterCrosswalk)return false;}
  for(int i=0;i<residents.Length;i++){if(i==index||!residents[i].active)continue;var other=(Vector2)residents[i].actor.transform.position;if(Vector2.Distance(next,other)<.42f&&Vector2.Distance(next,other)<Vector2.Distance(from,other)&&index>i)return false;}
  if(Time.unscaledTime>r.releaseSeatUntil){int count=Physics2D.CircleCast(from,.2f,(next-from).normalized,hitFilter,hits,Vector2.Distance(from,next));for(int h=0;h<count;h++){var hit=hits[h];if(hit.collider==null||hit.collider.isTrigger||hit.collider.attachedRigidbody!=null||hit.collider.GetComponentInParent<WorldInteractable>()==r.actor)continue;return false;}}
  r.actor.transform.position=next;r.facing=(next-from).normalized;VisibleSteps++;return true;
 }
 void Step(Resident r,int index,float dt){if(!r.active)return;Vector2 before=r.actor.transform.position;
  if(r.portal!=null){
   var portal=r.portal;var door=r.door;
   if(door!=null&&door.State!=DoorState.Open){if(!door.Acquire(r.definition.npcId,world.Service.Hour,out _))Label(r,index,"문 앞 대기");Render(r,index,before);return;}
   Vector2 cross=door==null?portal.Point:door.CrossPoint;
   if(Vector2.Distance(r.actor.transform.position,cross)>.06f){Move(r,index,cross,dt);Render(r,index,before);return;}
   door?.Release(r.definition.npcId);r.actor.zone=portal.targetZone;r.actor.transform.position=world.Service.rules.Zone(r.actor.zone).origin+portal.arrival;r.portal=null;r.door=null;r.path.Clear();r.waypoint=0;r.nextPortal=null;r.pendingAbstract=true;DoorPassages++;r.active=r.actor.zone==world.Service.State.zone;r.actor.visual.enabled=r.active;if(r.actor.shadow!=null)r.actor.shadow.enabled=r.active;return;
  }
  bool leaving=r.actor.zone!=r.target.zone;if(leaving&&(r.nextPortal==null||r.nextPortal.zone!=r.actor.zone))r.nextPortal=NextPortal(r.actor.zone,r.target.zone);var nextPortal=leaving?r.nextPortal:null;Vector2 goal=nextPortal==null?Destination(r):nextPortal.Point;
  if(!leaving&&r.atActivity||Vector2.Distance(r.actor.transform.position,goal)<.16f){
   if(nextPortal!=null){r.portal=nextPortal;r.door=nextPortal.GetComponent<WorldDoor>();if(r.door!=null)r.door.Acquire(r.definition.npcId,world.Service.Hour,out _);r.action=NPCDailyAction.Walk;Render(r,index,before);return;}
   r.atActivity=true;r.action=r.target.action;UseObject(r);if(r.action==NPCDailyAction.Sleep){var bed=Object(r.target.objectId);if(bed!=null)r.actor.transform.position=Vector2.MoveTowards(r.actor.transform.position,(Vector2)bed.transform.position+Vector2.up*.1f,dt*2);}
   if(r.action==NPCDailyAction.Talk){var friend=residents.FirstOrDefault(n=>n.definition.npcId==r.definition.companionId&&n.actor.zone==r.actor.zone);if(friend!=null&&Vector2.Distance(friend.actor.transform.position,r.actor.transform.position)<2)r.facing=(friend.actor.transform.position-r.actor.transform.position).normalized;}
   Render(r,index,before);return;
  }
  if(r.path.Count==0||r.waypoint>=r.path.Count){if(Time.unscaledTime<r.retry){Render(r,index,before);return;}r.path=navigation.Find(world,r.actor.zone,r.actor.transform.position,goal);r.waypoint=0;r.retry=Time.unscaledTime+1;}
  if(r.path.Count>0&&r.waypoint<r.path.Count){var p=r.path[r.waypoint];if(Vector2.Distance(r.actor.transform.position,p)<.09f)r.waypoint++;else Move(r,index,p,dt);}
  r.action=NPCDailyAction.Walk;Render(r,index,before);
 }
 void Label(Resident r,int index,string value){if(index<activityLabels.Length&&activityLabels[index]!=null){activityLabels[index].text=value;activityLabels[index].gameObject.SetActive(r.active);}}
 void Render(Resident r,int index,Vector2 before){Vector2 delta=(Vector2)r.actor.transform.position-before;bool walking=r.action==NPCDailyAction.Walk&&delta.sqrMagnitude>.0000001f;var motion=walking?ActorMotion.Walk:r.action==NPCDailyAction.Walk?ActorMotion.Idle:Motion(r.action);r.animation?.Render(motion,walking?delta:r.facing,delta.magnitude,walking||motion==ActorMotion.Idle);r.actor.visual.sortingOrder=1000-Mathf.RoundToInt(r.actor.transform.position.y*100);if(r.actor.shadow!=null)r.actor.shadow.sortingOrder=r.actor.visual.sortingOrder-1;if(r.actor.highlight!=null){r.actor.highlight.sprite=r.actor.visual.sprite;r.actor.highlight.flipX=r.actor.visual.flipX;r.actor.highlight.sortingOrder=r.actor.visual.sortingOrder-1;}
  var color=r.actor.visual.color;color.a=r.arrivingUntil>Time.unscaledTime?Mathf.Clamp01(1-(r.arrivingUntil-Time.unscaledTime)/.3f):1;r.actor.visual.color=color;
  if(index<activityLabels.Length&&activityLabels[index]!=null){string label=r.action==NPCDailyAction.Work?"근무 중":r.action==NPCDailyAction.Study?"공부 중":r.action==NPCDailyAction.Eat?"식사 중":r.action==NPCDailyAction.Shop?"장보기":r.action==NPCDailyAction.Talk?"이야기 중":r.action==NPCDailyAction.Sleep?"잠든 시간":r.action==NPCDailyAction.Rest?"쉬는 중":"";Label(r,index,label);activityLabels[index].transform.position=r.actor.transform.position+Vector3.up*2.1f;activityLabels[index].gameObject.SetActive(r.active&&Time.unscaledTime<r.labelUntil&&label!="");}
 }
 public NPCResidentSnapshot Inspect(string id){Capture();return world.Service.State.residents.FirstOrDefault(n=>n.id==id);}
}
}
