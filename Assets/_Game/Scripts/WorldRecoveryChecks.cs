#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine;
namespace IanGame {
public static class WorldRecoveryChecks {
 public static void Layout(LifeWorldController world,Action<bool,string> check){
  check(world!=null,"LifeWorld is loaded");var definitions=world.Service.rules.npcs.Where(n=>n.schedule.Any(e=>e.action==NPCDailyAction.Work)).ToArray();var slots=world.GetComponentsInChildren<WorldActivitySlot>().Where(s=>s.GetComponent<WorldWorkstationPresentation>()!=null).ToArray();
  check(definitions.Length==9&&slots.Length==9,"Nine employees and nine assigned facility workstations retained");check(slots.Select(s=>s.staffId).Distinct().Count()==slots.Length,"Seat ownership IDs are unique");
  foreach(var d in definitions)check(d.schedule.Where(e=>e.action==NPCDailyAction.Work).All(e=>e.objectId=="staff-desk-"+d.npcId),"Assigned work schedule: "+d.npcId);
  check(world.objects.Single(o=>o.id=="stock-terminal").zone=="finance","Stock terminal is in finance interior");check(world.objects.Single(o=>o.id=="property-agency").zone=="realestate","Property terminal is in agency interior");
  foreach(var zone in new[]{"finance","realestate","office-bathroom"})check(world.objects.Any(o=>o.kind==WorldInteractionKind.Door&&o.targetZone==zone)&&world.objects.Any(o=>o.kind==WorldInteractionKind.Door&&o.zone==zone),"Bidirectional facility route: "+zone);
 }
 public static void DoorLease(LifeWorldController world,Action<bool,string> check){var door=world.objects.First(o=>o.id=="bath-door").GetComponent<WorldDoor>();bool enabled=door.enabled,locked=door.locked;try{door.enabled=false;door.locked=false;door.ResetClosed();var root=door.transform.position;var frame=door.frame.transform.position;check(door.Acquire("qa/door",12,out _),"Actual door lease accepted");door.AdvanceAnimation(.05f);door.locked=true;check(!door.Acquire("qa/denied",12,out _)&&door.State==DoorState.Opening,"Denied lease preserves active animation");door.locked=false;for(int i=0;i<60;i++)door.AdvanceAnimation(1f/60);check(door.State==DoorState.Open&&!door.closedCollider.enabled,"Opening releases collision at full aperture");check(door.transform.position==root&&door.frame.transform.position==frame,"DoorRoot and frame remain fixed");check(door.SideOf(door.Plane+door.normal)!=door.SideOf(door.Plane-door.normal),"Approach sides are distinguished");}finally{door.locked=locked;door.ResetClosed();door.enabled=enabled;}}
}
}
#endif
