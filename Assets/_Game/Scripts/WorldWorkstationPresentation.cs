using UnityEngine;using System.Linq;
namespace IanGame {
// Authored front-facing workstation frames include the chair, desk and hands.
// The reserved NPC remains the navigation/interaction owner; furniture is world-owned.
[DefaultExecutionOrder(100)]
public sealed class WorldWorkstationPresentation:MonoBehaviour {
 public WorldActivitySlot slot;public WorldInteractable actor;public SpriteRenderer empty,working;public Sprite[] frames;
 public bool IsWorking{get;private set;}public int Frame{get;private set;}float phase;
 void Awake(){foreach(char c in slot.staffId)phase=(phase*31+c)%101;phase*=.071f;}
 void LateUpdate(){
  if(slot==null)return;if(actor==null)actor=GameContext.Instance?.game.world.Host?.GetComponent<WorldNPCScheduler>()?.actors.FirstOrDefault(a=>a.npcId==slot.staffId);if(actor==null){if(working!=null)working.enabled=false;if(empty!=null)empty.enabled=true;return;}var world=actor==null?null:actor.GetComponent<WorldActorAnimation>();var context=GameContext.Instance;bool local=context!=null&&context.game.world.State.zone==actor.zone;
  bool active=local&&slot.Occupant==actor.npcId&&Vector2.Distance(actor.transform.position,slot.Point)<.2f&&world!=null&&(world.Motion==ActorMotion.UseComputer||world.Motion==ActorMotion.Work);
  if(active){float t=(Time.unscaledTime+phase)%8;Frame=t<5?Mathf.FloorToInt(t*3)%2:t<7?2:3;working.sprite=frames[Frame];working.sortingOrder=1000-Mathf.RoundToInt(transform.position.y*100)+2;actor.visual.enabled=false;if(actor.shadow!=null)actor.shadow.enabled=false;}
  else if(IsWorking&&actor!=null){actor.visual.enabled=local;if(actor.shadow!=null)actor.shadow.enabled=local;}
  working.enabled=active;empty.enabled=!active;IsWorking=active;
 }
 void OnDisable(){if(working!=null)working.enabled=false;if(empty!=null)empty.enabled=true;}
}
}
