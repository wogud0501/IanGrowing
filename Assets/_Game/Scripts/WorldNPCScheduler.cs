using UnityEngine;
namespace IanGame {
// One scheduler owns all NPC routes; only active-zone visuals animate, distant schedules are cheap targets.
public sealed class WorldNPCScheduler:MonoBehaviour {
 public LifeWorldController world;public WorldInteractable[] actors;float next;
 void Update(){if(Time.unscaledTime<next||world.Service==null)return;next=Time.unscaledTime+.1f;if(world.Busy||world.Service.Game.State!=GameState.Playing)return;var defs=world.Service.rules.npcs;for(int i=0;i<actors.Length&&i<defs.Length;i++){var d=defs[i];if(d.schedule==null||d.schedule.Length==0)continue;var target=d.schedule[0];foreach(var s in d.schedule)if(s.hour<=world.Service.Hour)target=s;var zone=world.Service.rules.Zone(target.zone);var a=actors[i];bool changed=a.zone!=target.zone;a.zone=target.zone;var point=zone.origin+target.position;var old=a.transform.position;bool local=a.zone==world.Service.State.zone;a.visual.enabled=local;if(!local)a.transform.position=point;else{if(changed)a.transform.position=zone.origin+zone.spawn;Vector2 patrol=point+new Vector2(Mathf.FloorToInt(Time.unscaledTime/12)%2==0?.6f:0,0);a.transform.position=Vector2.MoveTowards(a.transform.position,patrol,.14f);}bool walking=((Vector2)old-(Vector2)a.transform.position).sqrMagnitude>.00001f;if(d.frames!=null&&d.frames.Length>0)a.visual.sprite=d.frames[walking?Mathf.FloorToInt(Time.unscaledTime*6)%d.frames.Length:0];a.visual.sortingOrder=1000-Mathf.RoundToInt(a.transform.position.y*100);a.visual.flipX=a.transform.position.x<old.x;}}
}
}
