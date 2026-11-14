using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace IanGame {
public sealed partial class WorldNPCScheduler {
 public bool sceneScoped;public WorldInteractable[] residentPrefabs=Array.Empty<WorldInteractable>();public TMP_Text labelPrefab;
 bool restoringScoped;float nextScoped;
 public bool HasPhysicalActor(string id)=>sceneScoped&&residents.Any(r=>r.definition.npcId==id&&r.actor!=null&&r.actor.zone==world.Room.roomId);
 void StartScoped(){street=world.GetComponentInChildren<WorldStreetLife>();hitFilter=new ContactFilter2D{useTriggers=false};ready=true;world.Service.Restored+=Restore;RebuildScoped(true);}
 void RebuildScoped(bool restoring){foreach(var r in residents){r.door?.Release(r.definition.npcId);r.interiorDoor?.Release(r.definition.npcId);r.slot?.Release(r.definition.npcId);r.departureSlot?.Release(r.definition.npcId);if(r.actor!=null){world.objects=world.objects.Where(o=>o!=r.actor).ToArray();Destroy(r.actor.gameObject);}}foreach(var label in activityLabels)if(label!=null)Destroy(label.gameObject);actors=Array.Empty<WorldInteractable>();activityLabels=Array.Empty<TMP_Text>();residents=Array.Empty<Resident>();navigation.Clear();world.Service.AdvanceAbsentResidents();var room=world.Room.roomId;
  foreach(var n in world.Service.State.residents.Where(n=>n.zone==room).ToArray())SpawnScoped(n,true);
  restoringScoped=true;try{Restore();}finally{restoringScoped=false;}
 }
 void SpawnScoped(NPCResidentSnapshot saved,bool loading){var d=world.Service.rules.npcs.FirstOrDefault(d=>d.npcId==saved.id);var template=residentPrefabs.FirstOrDefault(a=>a.npcId==saved.id);if(d==null||template==null||residents.Any(r=>r.definition.npcId==saved.id))return;string local=world.Room.roomId;var target=d.schedule[Schedule(d,world.Service.Game.clock.Day,(int)(world.Service.Game.clock.SubDaySeconds/60))];var p=loading&&saved.zone==local?world.CurrentZone.origin+saved.position:world.CurrentZone.origin+world.CurrentZone.spawn;
  if(!loading){var portal=world.Room.catalog.EntryFrom(local,saved.zone);var o=portal==null?null:Object(portal.doorId);if(o!=null){var incoming=world.Room.catalog.portals.FirstOrDefault(e=>e.sourceRoom==portal.destinationRoom&&e.destinationRoom==local);p=world.Safe(world.CurrentZone,incoming?.arrival??(o.Point-world.CurrentZone.origin));var door=o.GetComponent<WorldDoor>();if(door!=null){door.Acquire(saved.id,world.Service.Hour,out _);door.Release(saved.id);}}}
  if(!loading&&residents.Any(r=>r.active&&r.actor!=null&&Vector2.Distance(r.actor.transform.position,p)<.6f))return;var a=Instantiate(template,world.transform);a.name=d.npcId;a.zone=local;
  a.transform.position=p;a.visual.enabled=true;if(a.shadow!=null)a.shadow.enabled=true;world.objects=world.objects.Concat(new[]{a}).ToArray();actors=actors.Concat(new[]{a}).ToArray();var label=Instantiate(labelPrefab,world.transform);label.name=d.npcId+" activity";label.text="";label.gameObject.SetActive(false);activityLabels=activityLabels.Concat(new[]{label}).ToArray();var r=new Resident{actor=a,definition=d,animation=a.GetComponent<WorldActorAnimation>(),target=target,active=true,index=-1,used=saved.completedUseKey,action=loading?saved.action:NPCDailyAction.Walk};residents=residents.Concat(new[]{r}).ToArray();saved.zone=a.zone;saved.position=p-world.CurrentZone.origin;saved.action=r.action;nextDecision=0;
 }
 void RefreshScoped(){Capture();foreach(var r in residents.Where(r=>r.actor.zone!=world.Room.roomId).ToArray()){r.slot?.Release(r.definition.npcId);world.objects=world.objects.Where(o=>o!=r.actor).ToArray();actors=actors.Where(a=>a!=r.actor).ToArray();int i=Array.IndexOf(residents,r);if(i<activityLabels.Length){if(activityLabels[i]!=null)Destroy(activityLabels[i].gameObject);activityLabels=activityLabels.Where((l,index)=>index!=i).ToArray();}Destroy(r.actor.gameObject);residents=residents.Where(x=>x!=r).ToArray();}
  world.Service.AdvanceAbsentResidents();foreach(var n in world.Service.State.residents.ToArray()){var d=world.Service.rules.npcs.FirstOrDefault(d=>d.npcId==n.id);if(d==null||residents.Any(r=>r.definition.npcId==n.id))continue;var target=d.schedule[Schedule(d,world.Service.Game.clock.Day,(int)(world.Service.Game.clock.SubDaySeconds/60))];if(n.zone==world.Room.roomId||target.zone==world.Room.roomId)SpawnScoped(n,false);}
 }
}
}
