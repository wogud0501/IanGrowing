using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace IanGame.Editor {
public static class AdvancedNPCSetup {
 const string Zone="residence";static LifeWorldController world;static LifeWorldRules rules;
 static WorldInteractable Find(string id)=>world.objects.FirstOrDefault(o=>o.id==id);
 static SpriteRenderer Sprite(Transform parent,string name,Sprite sprite,Vector2 position,Vector2? tiled=null){var t=parent.Find(name);if(t==null){t=new GameObject(name,typeof(SpriteRenderer)).transform;t.SetParent(parent,false);}t.localPosition=position;var sr=t.GetComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/VisualRebuild/WorldAlphaCut.mat");sr.sortingOrder=tiled.HasValue?-5000:1000-Mathf.RoundToInt(t.position.y*100);if(tiled.HasValue){sr.drawMode=SpriteDrawMode.Tiled;sr.size=tiled.Value;}return sr;}
 static void Wall(Transform parent,string name,Vector2 p,Vector2 size){var t=parent.Find(name);if(t==null){t=new GameObject(name,typeof(BoxCollider2D)).transform;t.SetParent(parent,false);}t.localPosition=p;t.GetComponent<BoxCollider2D>().size=size;}
 static WorldInteractable Prop(string id,string title,string zone,Vector2 p,Sprite sprite,WorldInteractionKind kind,Vector2 footprint){var o=Find(id);if(o==null){var go=new GameObject(id,typeof(WorldInteractable));go.transform.SetParent(world.transform,false);o=go.GetComponent<WorldInteractable>();world.objects=world.objects.Concat(new[]{o}).ToArray();}o.id=id;o.title=title;o.zone=zone;o.kind=kind;o.radius=1.1f;o.UsePoint=kind==WorldInteractionKind.Sleep?new Vector2(1.1f,-.3f):Vector2.down*.8f;o.transform.position=rules.Zone(zone).origin+p;var sr=o.GetComponent<SpriteRenderer>();if(sr==null)sr=o.gameObject.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/VisualRebuild/WorldAlphaCut.mat");sr.sortingOrder=1000-Mathf.RoundToInt(o.transform.position.y*100);o.visual=sr;if(footprint.x>0){var c=o.GetComponent<BoxCollider2D>();if(c==null)c=o.gameObject.AddComponent<BoxCollider2D>();c.size=footprint;c.offset=Vector2.up*(footprint.y/2);}return o;}
 static void Residence(){
  if(rules.Zone(Zone)==null)rules.zones=rules.zones.Concat(new[]{new WorldZoneDefinition{id=Zone,title="주민 생활관",location=SceneId.Home,origin=new Vector2(310,0),size=new Vector2(18,12),spawn=new Vector2(0,-4.5f),walkableTop=4.8f,cameraSize=6.8f}}).ToArray();
  var root=world.transform.Find("Resident home interior");if(root==null){root=new GameObject("Resident home interior").transform;root.SetParent(world.transform,false);}root.position=rules.Zone(Zone).origin;
  var architecture=AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/VisualRebuild/Architecture.png").OfType<Sprite>().ToDictionary(s=>s.name);
  Sprite(root,"Wood floor",architecture["OakFloor"],Vector2.zero,new Vector2(18,12));Sprite(root,"White rear wall",architecture["IvoryWall"],new Vector2(0,5),new Vector2(18,architecture["IvoryWall"].bounds.size.y));
  Wall(root,"Rear boundary",new Vector2(0,5),new Vector2(18,.2f));Wall(root,"West boundary",new Vector2(-9,0),new Vector2(.2f,12));Wall(root,"East boundary",new Vector2(9,0),new Vector2(.2f,12));Wall(root,"Front left",new Vector2(-4.9f,-6),new Vector2(8.2f,.2f));Wall(root,"Front right",new Vector2(4.9f,-6),new Vector2(8.2f,.2f));
  for(int i=0;i<12;i++)Prop("resident-bed-"+i,"주민 침대 "+(201+i),Zone,new Vector2(-6+i%4*4,-3+i/4*3),Find("bed").visual.sprite,WorldInteractionKind.Sleep,new Vector2(1.2f,1.25f));
  var entry=Prop("npc-residence-front","주민 생활관으로","alley",new Vector2(7,4),Find("house-entrance").visual.sprite,WorldInteractionKind.Door,new Vector2(4,.6f));entry.targetZone=Zone;entry.arrival=new Vector2(0,-4.5f);
  var exit=Prop("resident-exit","생활관에서 나가기",Zone,new Vector2(0,-5.7f),Find("shop-exit").visual.sprite,WorldInteractionKind.Door,Vector2.zero);exit.UsePoint=Vector2.up*.8f;exit.targetZone="alley";exit.arrival=new Vector2(7,2);AdvancedDoorSetup.Attach(world,entry);AdvancedDoorSetup.Attach(world,exit);
 }
 [MenuItem("Ian/Life World/Advanced/Apply Resident Daily Life")]
 public static void Apply(){
  if(Application.isPlaying)throw new Exception("Stop Play first");var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");world=UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();rules=AssetDatabase.LoadAssetAtPath<LifeWorldRules>("Assets/_Game/Data/Phase26/LifeWorldRules.asset");Residence();
  var actions=AdvancedLifeSetup.Grid("CitizenActions.png",8,8);var banks=new ActorAnimationSet[4];for(int i=0;i<4;i++){
   var bank=AssetDatabase.LoadAssetAtPath<ActorAnimationSet>(AdvancedLifeSetup.Data+"Citizen"+i+".asset");var list=bank.clips.Where(c=>c.motion==ActorMotion.Walk||c.motion==ActorMotion.Idle).ToList();int top=i/2*4,left=i%2*4;
   Sprite[] Frames(int row)=>Enumerable.Range(0,4).Select(n=>actions[(top+row)*8+left+n]).ToArray();
   void Add(ActorMotion motion,Sprite[] frames,bool loop=false,int loopFrom=0){list.Add(new ActorSpriteClip{motion=motion,direction=ActorDirection.Right,frames=frames,loop=loop,loopFrom=loopFrom,framesPerSecond=7});}
   Add(ActorMotion.SitDown,Frames(0));Add(ActorMotion.StandUp,Frames(0).Reverse().ToArray());Add(ActorMotion.Eat,Frames(1),true);Add(ActorMotion.Work,Frames(2).Take(2).ToArray(),true);Add(ActorMotion.UseComputer,Frames(2).Skip(2).Take(2).ToArray(),true);Add(ActorMotion.Sleep,Frames(3),true,3);Add(ActorMotion.WakeUp,Frames(3).Reverse().ToArray());bank.clips=list.ToArray();EditorUtility.SetDirty(bank);banks[i]=bank;
  }
  var scheduler=world.GetComponent<WorldNPCScheduler>();var street=world.GetComponentInChildren<WorldStreetLife>(true);street.residentSimulation=true;var actors=scheduler.actors.Take(2).ToList();
  for(int i=0;i<street.pedestrians.Length;i++){var person=street.pedestrians[i];var a=person.visual.GetComponent<WorldInteractable>()??person.visual.gameObject.AddComponent<WorldInteractable>();a.id="resident-"+i;a.title=new[]{"지은","정호","도윤","순자"}[i%4];a.npcId=a.id;a.zone="city";a.kind=WorldInteractionKind.NPC;a.visual=person.visual;a.shadow=person.shadow;a.UsePoint=Vector2.down*.15f;a.radius=1.1f;actors.Add(a);person.frames=banks[i%4].Find(ActorMotion.Walk,ActorDirection.Right).frames;}
  scheduler.actors=actors.ToArray();var defs=new List<WorldNPCDefinition>();
  NPCScheduleEntry Entry(int hour,int minute,string zone,NPCDailyAction action,Vector2 position,string objectId="",int mask=127)=>new NPCScheduleEntry{hour=hour,minute=minute,zone=zone,action=action,position=position,objectId=objectId,dayMask=mask};
  for(int i=0;i<actors.Count;i++){
   var a=actors[i];int art=i==0?0:i==1?2:(i-2)%4;var animation=a.GetComponent<WorldActorAnimation>()??a.gameObject.AddComponent<WorldActorAnimation>();animation.visual=a.visual;animation.library=banks[art];
   var bed=Find("resident-bed-"+i);Vector2 home=bed.Point-rules.Zone(Zone).origin;Vector2 desk=Find(i==1?"study-desk":"workdesk").Point-rules.Zone(i==1?"school":"workplace").origin+new Vector2((i%3-1)*.7f,-(i/3)*.5f);Vector2 counter=Find("counter").Point-rules.Zone("shop").origin+new Vector2((i%3-1)*.65f,-(i/3)*.35f);
   var d=new WorldNPCDefinition{npcId=a.npcId,title=i<2?rules.npcs[i].title:a.title,homeZone=Zone,workZone=i==1?"school":"workplace",jobId=i==1?"student":"convenience",personality=i==1?NPCPersonality.Student:i%5==3?NPCPersonality.NightWorker:i%5==4?NPCPersonality.Homebody:NPCPersonality.OfficeWorker,speed=1.1f+i%4*.13f,animations=banks[art],frames=banks[art].Find(ActorMotion.Walk,ActorDirection.Right).frames,companionId=i==0?"classmate":i==1?"colleague":""};
   int minute=(i*7)%40;var entries=new List<NPCScheduleEntry>();
   if(d.personality==NPCPersonality.NightWorker){entries.Add(Entry(0,0,"workplace",NPCDailyAction.Work,desk,"workdesk"));entries.Add(Entry(7,minute,Zone,NPCDailyAction.Sleep,home,bed.id));entries.Add(Entry(15,minute,Zone,NPCDailyAction.Wake,home,bed.id));entries.Add(Entry(17,minute,"city",NPCDailyAction.Rest,new Vector2(-6,-7.2f),"city-bench"));entries.Add(Entry(20,minute,"shop",NPCDailyAction.Eat,counter,"counter"));entries.Add(Entry(22,minute,"workplace",NPCDailyAction.Work,desk,"workdesk"));}
   else if(d.personality==NPCPersonality.Homebody){entries.Add(Entry(0,0,Zone,NPCDailyAction.Sleep,home,bed.id));entries.Add(Entry(9,minute,Zone,NPCDailyAction.Rest,home,bed.id));entries.Add(Entry(14,minute,"city",NPCDailyAction.Walk,new Vector2(5,-7.3f)));entries.Add(Entry(16,minute,"shop",NPCDailyAction.Shop,counter,"counter"));entries.Add(Entry(18,minute,Zone,NPCDailyAction.Rest,home,bed.id));entries.Add(Entry(23,minute,Zone,NPCDailyAction.Sleep,home,bed.id));}
   else{
    entries.Add(Entry(0,0,Zone,NPCDailyAction.Sleep,home,bed.id));entries.Add(Entry(7,minute,Zone,NPCDailyAction.Wake,home,bed.id));entries.Add(Entry(8,minute,"city",NPCDailyAction.Walk,new Vector2(-10+i*1.6f,1)));
    entries.Add(Entry(9,0,d.workZone,i==1?NPCDailyAction.Study:NPCDailyAction.Work,desk,i==1?"study-desk":"workdesk"));entries.Add(Entry(12,minute,"shop",NPCDailyAction.Eat,counter,"counter"));entries.Add(Entry(13,minute,d.workZone,i==1?NPCDailyAction.Study:NPCDailyAction.Work,desk,i==1?"study-desk":"workdesk"));
    entries.Add(Entry(i==1?16:18,minute,"city",NPCDailyAction.Rest,new Vector2(-5+i*.65f,-7.2f),"city-bench"));entries.Add(Entry(19,minute,"shop",NPCDailyAction.Shop,counter,"counter"));entries.Add(Entry(20,0,"city",i<2?NPCDailyAction.Talk:NPCDailyAction.Walk,new Vector2(-2+i*.65f,1.2f)));entries.Add(Entry(21,minute,Zone,NPCDailyAction.Rest,home,bed.id));entries.Add(Entry(23,minute,Zone,NPCDailyAction.Sleep,home,bed.id));
   }
   if(i>=2&&i%3==0){foreach(var e in entries.Where(e=>e.action==NPCDailyAction.Work))e.dayMask=31;entries.Add(Entry(9,minute,"city",NPCDailyAction.Walk,new Vector2(6,-7.2f),"",96));}
   d.schedule=entries.OrderBy(e=>e.hour*60+e.minute).ToArray();defs.Add(d);
  }
  rules.npcs=defs.ToArray();
  scheduler.activityLabels=new TMP_Text[actors.Count];for(int i=0;i<actors.Count;i++){var child=actors[i].transform.Find("Activity caption");if(child==null){child=new GameObject("Activity caption",typeof(TextMeshPro)).transform;child.SetParent(actors[i].transform,false);}var text=child.GetComponent<TextMeshPro>();text.font=world.hud.needs.font;text.fontSize=2.4f;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(4,.5f);text.color=new Color(1,.95f,.78f);text.renderer.sortingOrder=6000;text.text="";text.gameObject.SetActive(false);scheduler.activityLabels[i]=text;}
  foreach(var d in rules.npcs)foreach(var e in d.schedule){if(rules.Zone(e.zone)==null)throw new Exception("Unknown resident zone "+e.zone);if(e.objectId!=""&&Find(e.objectId)==null)throw new Exception("Unknown resident object "+e.objectId);}
  EditorUtility.SetDirty(rules);EditorUtility.SetDirty(scheduler);EditorUtility.SetDirty(street);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 }
}
}
