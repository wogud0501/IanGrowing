using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace IanGame {
[DefaultExecutionOrder(1200)]
public sealed class FamilyHomeRuntime:MonoBehaviour {
 public LifeWorldController world;public SpriteRenderer crib;public Sprite[] cribFrames;public Sprite emptyBed;
 readonly HashSet<string> pending=new HashSet<string>();bool infant;float checkAt;
 public int CompletedVisibleActions{get;private set;}
 public bool Queue(string parent,WorldInteractable target,long key){if(world==null||world.Service==null||!FamilyLife.IsFamilyRoom(world.Service.State.zone))return false;string receipt=parent+"/"+target.id+"/"+key;if(pending.Add(receipt))StartCoroutine(Perform(parent,target,key,receipt));return true;}
 IEnumerator Perform(string parent,WorldInteractable target,long key,string receipt){var state=world.Service.State;var a=target.GetComponent<WorldLifeAction>();bool fridge=target.id=="family-fridge";try{
   if(fridge&&a!=null)for(int i=0;i<4;i++){Appliance(a,i);yield return new WaitForSecondsRealtime(.15f);}
   float duration=target.id=="family-cook"?2.2f:target.id.StartsWith("family-dine-")?1.4f:.7f;
   for(float t=0;t<duration;){if(world.Service.State!=state||!target.gameObject.activeInHierarchy)yield break;if(world.Service.Game.State!=GameState.Playing){yield return null;continue;}t+=Time.unscaledDeltaTime;yield return null;}
   if(world.Service.State!=state)yield break;FamilyLife.ApplyUse(world.Service.Game,parent,target,key);CompletedVisibleActions++;
   if(fridge&&a!=null)for(int i=2;i>=0;i--){Appliance(a,i);yield return new WaitForSecondsRealtime(.15f);}
  }finally{if(fridge&&a!=null){a.overlay.enabled=false;target.visual.enabled=true;}pending.Remove(receipt);}}
 void Appliance(WorldLifeAction a,int frame){a.overlay.sprite=a.frames[frame];a.overlay.color=Color.white;a.overlay.sortingOrder=a.GetComponent<WorldInteractable>().visual.sortingOrder+2;a.overlay.enabled=true;a.GetComponent<WorldInteractable>().visual.enabled=false;}
 void LateUpdate(){if(world==null||world.Service==null)return;if(crib!=null&&world.Player.growthVisual?.Current!=null){crib.enabled=false;}else if(crib!=null){bool show=world.Service.Game.growth.Age==0&&FamilyLife.IsNursery(world.CurrentZone?.id)&&!world.Busy;
  if(show){var bed=world.objects.First(o=>o.id=="family-bed");world.Player.sprite.enabled=false;if(world.Player.contactShadow!=null)world.Player.contactShadow.enabled=false;crib.enabled=true;crib.sprite=cribFrames[0];crib.sortingOrder=bed.visual.sortingOrder+3;bed.visual.enabled=false;infant=true;}
  else if(infant&&!world.Busy){infant=false;crib.enabled=false;world.Player.sprite.enabled=true;if(world.Player.contactShadow!=null)world.Player.contactShadow.enabled=true;var bed=world.objects.First(o=>o.id=="family-bed");bed.visual.enabled=true;}
  else if(world.Busy){var bed=world.objects.First(o=>o.id=="family-bed");bool own=world.Service.Game.growth.Age==0&&FamilyLife.IsNursery(world.CurrentZone?.id);crib.enabled=own&&!bed.GetComponent<WorldLifeAction>().overlay.enabled;if(crib.enabled){crib.sprite=cribFrames[0];bed.visual.enabled=false;world.Player.sprite.enabled=false;}}
  }
  // Resume an interrupted, uncommitted visible household action after a load.
  if(Time.unscaledTime<checkAt||world.Busy)return;checkAt=Time.unscaledTime+.5f;var scheduler=world.GetComponent<WorldNPCScheduler>();if(scheduler==null||!FamilyLife.IsFamilyRoom(world.CurrentZone?.id))return;
  foreach(string parent in new[]{"family-mother","family-father"}){var data=scheduler.Inspect(parent);if(data==null||!FamilyLife.IsFamilyRoom(data.zone)||data.action==NPCDailyAction.Walk||string.IsNullOrEmpty(data.objectId))continue;var target=world.objects.FirstOrDefault(o=>o.id==data.objectId);if(target!=null&&!FamilyLife.Done(world.Service.Game,parent,target.id,data.completedUseKey))Queue(parent,target,data.completedUseKey);}
 }
 void OnDisable(){StopAllCoroutines();pending.Clear();if(crib!=null)crib.enabled=false;}
}
}
