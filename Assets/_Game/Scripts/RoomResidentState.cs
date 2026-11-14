using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace IanGame {
public sealed partial class LifeWorldService {
 public bool RoomMode=>rooms!=null&&GameContext.Instance!=null&&GameContext.Instance.transitions.IsRoomArchitecture;
 public void UseLegacyRules(){if(legacyRules!=null)rules=legacyRules;}
 public void UseRoomRules(RoomSceneCatalog catalog){rooms=catalog;if(catalog?.rules!=null)rules=catalog.rules;catalog?.Migrate(State,Game==null?-1:Game.growth.Age);}
 public void AdvanceAbsentResidents(){if(!RoomMode||Game==null||rules==null)return;if(State.residents==null)State.residents=new List<NPCResidentSnapshot>();long now=(long)(Game.clock.Day-1)*1440+(long)(Game.clock.SubDaySeconds/60);var visible=Host?.GetComponent<WorldNPCScheduler>();foreach(var d in rules.npcs){if(visible!=null&&visible.HasPhysicalActor(d.npcId))continue;int index=WorldNPCScheduler.Schedule(d,Game.clock.Day,(int)(Game.clock.SubDaySeconds/60));var target=d.schedule[index];var n=State.residents.FirstOrDefault(n=>n.id==d.npcId);if(n==null){n=new NPCResidentSnapshot{id=d.npcId,zone=target.zone,position=target.position,action=target.action,objectId=target.objectId,scheduleIndex=index,minute=now};State.residents.Add(n);continue;}if(n.minute==now)continue;n.destinationZone=target.zone;n.objectId=target.objectId;n.scheduleIndex=index;
   // A visitor entering the currently viewed room must appear through its door.
   // Other rooms are data-only; no GameObject is created there.
   if(Host!=null&&Host.Room!=null&&target.zone==Host.Room.roomId&&n.zone!=target.zone){n.action=NPCDailyAction.Walk;n.minute=now;continue;}
   n.zone=target.zone;n.position=target.position;n.action=target.action;n.minute=now;
  }}
}
}
