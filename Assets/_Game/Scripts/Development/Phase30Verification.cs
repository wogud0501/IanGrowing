#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace IanGame.Editor {
public static class Phase30Verification {
 static LifeWorldController W=>GameContext.Instance.game.world.Host;
 static SceneTransitionService T=>GameContext.Instance.transitions;
 static IEnumerator Wait(){float end=Time.unscaledTime+25;while((T.IsTransitioning||W==null||W.Busy)&&Time.unscaledTime<end)yield return null;if(T.IsTransitioning||W==null||W.Busy)throw new Exception("Phase30 world timeout");yield return null;yield return null;}
 static IEnumerator Go(string room,Action<bool,string> check){check(T.NavigateRoom(room,T.roomCatalog.rules.Zone(room).spawn,Vector2.down),"Scene request "+room);yield return Wait();check(W.Room.roomId==room,"Scene active "+room);W.follow.Snap();}
 static IEnumerator Shot(string name,bool before=false){string folder="Docs/Phase30/"+(before?"Before":"Screenshots");Directory.CreateDirectory(folder);yield return new WaitForSecondsRealtime(.25f);ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return null;yield return null;}
 public static IEnumerator Run(GameManager g,Action<bool,string> check,Action<GameObject,string> audit,string mode){
  g.growth.Restore(new ProgressSnapshot{age=19});g.clock.realtimeEnabled=false;g.clock.Restore(4,new TimeSnapshot{subDaySeconds=14*3600});var stats=g.stats.Capture();stats.money=1000;g.stats.Restore(stats);check(g.economy.MoveResidence("phase30/fixture","starter"),"Qualified personal-home fixture");
  if(mode.EndsWith("Before")){foreach(string room in new[]{"home","family-living","family-ian-bedroom","city"}){yield return Go(room,check);W.Player.Warp(W.CurrentZone.origin+new Vector2(.2f,-.7f));W.hud.Notify("",1);yield return Shot(room,true);}yield break;}
  if(mode.EndsWith("Scenes")){yield return Scenes(g,check,audit);yield break;}
  yield return Go("home",check);var w=W;var motor=w.Player;var controller=motor;long cash=g.economy.Cash;double time=g.clock.SubDaySeconds;
  check(w.GetComponent<StarterHomePresentation>().productionShell.activeInHierarchy,"New independent home uses authored production shell");
  check(w.objects.Where(o=>StarterHomePresentation.Optional(o.id)).All(o=>!o.gameObject.activeInHierarchy),"First independent home retains bed-only rule");
  check(w.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="Continuous muted oak floor")&&w.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="White plaster rear wall"),"Separate floor and white wall renderers are visible");
  var catalog=motor.growthVisual.catalog;
  foreach(int age in new[]{0,1,7,15,18,19,29,30,59,60,65}){
   g.growth.Restore(new ProgressSnapshot{age=age});yield return new WaitForSecondsRealtime(.08f);var p=motor.growthVisual.Current;
   check(p==catalog.AtAge(age)&&motor==controller,"Authoritative age changes visual without replacing controller: "+age);
   check(motor.MovementAllowed==(age>=1)&&g.growth.IndependentOutdoorUnlocked==(age>=7),"Walking/outdoor permissions preserved: "+age);
   check(Mathf.Abs(motor.GetComponent<CircleCollider2D>().radius-p.colliderRadius)<.001f,"Collider profile: "+age);
   if(age>0)foreach(var direction in (ActorDirection[])Enum.GetValues(typeof(ActorDirection))){var idle=p.animations.Find(ActorMotion.Idle,direction,false);var walk=p.animations.Find(ActorMotion.Walk,direction,false);check(idle!=null&&walk!=null&&walk.frames.Distinct().Count()>1,"Authored directional idle/walk "+age+"/"+direction);}
  }
  check(catalog.AtAge(30).animations!=catalog.AtAge(19).animations&&catalog.AtAge(60).animations!=catalog.AtAge(30).animations,"Middle and elder use separate original animation art, not scale substitutions");
  yield return Preview(g,check);
  g.growth.Restore(new ProgressSnapshot{age=19});yield return null;motor.Warp(new Vector2(.2f,-.7f));w.follow.Snap();w.hud.Notify("",3);yield return Shot("Home-Day");
  motor.BeginTraversal(new Vector2(2,-.7f),1.5f);float end=Time.unscaledTime+8;var moving=new HashSet<Sprite>();while(motor.IsTraversing&&Time.unscaledTime<end){moving.Add(motor.sprite.sprite);yield return null;}check(motor.TraversalArrived&&moving.Count>1,"Real Rigidbody traversal and displacement-driven walk frames in studio");motor.CancelTraversal();
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1440,900),new Vector2Int(904,2316)}){CommercialSliceVerification.SetSize(size.x,size.y);yield return new WaitForSecondsRealtime(.3f);var c=w.follow.target;float scale=c.pixelHeight/(2*c.orthographicSize*w.Room.interior.referencePixelsPerUnit);check(Mathf.Abs(scale-Mathf.Round(scale))<.001f,"Integer logical density "+size);var pos=c.transform.position;float ppu=c.pixelHeight/(2*c.orthographicSize);check(Mathf.Abs(pos.x*ppu-Mathf.Round(pos.x*ppu))<.005f&&Mathf.Abs(pos.y*ppu-Mathf.Round(pos.y*ppu))<.005f,"One render pixel snap lattice "+size);audit(w.hud.gameObject,"Phase30 Home "+size);yield return Shot("Home-"+size.x+"x"+size.y);}
  CommercialSliceVerification.SetSize(1280,720);g.clock.Restore(4,new TimeSnapshot{subDaySeconds=22*3600});yield return new WaitForSecondsRealtime(.4f);yield return Shot("Home-Night");g.clock.Restore(4,new TimeSnapshot{subDaySeconds=time});
  check(g.economy.Cash==cash,"Presentation never changes cash");
  foreach(int age in new[]{35,65}){g.growth.Restore(new ProgressSnapshot{age=age});yield return null;var bed=w.objects.Single(o=>o.id=="bed");var action=bed.GetComponent<WorldLifeAction>();motor.Warp(action.approach.position);yield return null;check(w.Begin(bed,5),"Actual bed interaction starts "+age);bool slept=false,woke=false,own=true,shot=false;end=Time.unscaledTime+25;while(w.Busy&&Time.unscaledTime<end){if(motor.advanced.Motion==ActorMotion.Sleep){slept=true;own&=motor.sprite.enabled&&motor.sprite.sprite.texture==motor.growthVisual.Current.idlePreview.texture;if(!shot){shot=true;yield return Shot("Sleep-"+age);}}woke|=motor.advanced.Motion==ActorMotion.WakeUp;yield return null;}check(slept&&woke&&own&&!w.Busy&&w.CanControl,"Age-specific sleep, wake and restored input "+age);}
  g.growth.Restore(new ProgressSnapshot{age=65});check(g.SaveAutomaticCheckpoint(out var reason),"Actual QA disk save: "+reason);check(g.saves.TryReadAutomatic(out var saved,out reason),"Actual QA disk read: "+reason);g.growth.Restore(new ProgressSnapshot{age=19});g.RestoreState(saved);yield return Wait();check(g.growth.Age==65&&W.Player.growthVisual.Current.stage==VisualGrowthStage.Elder,"Age authority restores elder bank from existing SaveData");
  g.growth.Restore(new ProgressSnapshot{age=19});yield return Go("family-living",check);yield return Shot("ParentsHome-Living");yield return Go("home",check);check(W.Player.growthVisual.Current.stage==VisualGrowthStage.Adult&&UnityEngine.Object.FindObjectsByType<WorldPlayerMotor>(FindObjectsSortMode.None).Length==1,"Scene roundtrip preserves age and single player");
 }
 static IEnumerator Preview(GameManager g,Action<bool,string> check){var w=W;var root=new GameObject("QA-only six life-stage sprites");var font=w.hud.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.font!=null).font;int[] ages={0,7,15,19,35,65};for(int i=0;i<ages.Length;i++){g.growth.Restore(new ProgressSnapshot{age=ages[i]});yield return null;var p=w.Player.growthVisual.Current;var go=new GameObject("Art preview "+p.id,typeof(SpriteRenderer));go.transform.SetParent(root.transform);go.transform.position=w.CurrentZone.origin+new Vector2(-4.7f+i*1.9f,-.7f);go.transform.localScale=Vector3.one*w.Player.visualScale;var s=go.GetComponent<SpriteRenderer>();s.sharedMaterial=w.Player.sprite.sharedMaterial;s.sprite=p.idlePreview;s.sortingOrder=3000;var label=new GameObject("Age label",typeof(TMPro.TextMeshPro));label.transform.SetParent(root.transform);label.transform.position=go.transform.position+Vector3.down*.4f;var text=label.GetComponent<TMPro.TextMeshPro>();text.font=font;text.fontSize=2.8f;text.alignment=TMPro.TextAlignmentOptions.Center;text.text=ages[i]+"세";text.GetComponent<MeshRenderer>().sortingOrder=3001;}
  bool shown=w.Player.sprite.enabled;w.Player.sprite.enabled=false;w.Player.contactShadow.enabled=false;w.hud.Notify("",3);yield return new WaitForSecondsRealtime(.6f);yield return Shot("Six-LifeStage-Lineup");w.Player.sprite.enabled=shown;w.Player.contactShadow.enabled=true;UnityEngine.Object.Destroy(root);check(w.Player.GetComponent<GrowthPresentationEffects>().PlayedCount>0,"Growth event plays pooled visual transition");
 }
 static IEnumerator Scenes(GameManager g,Action<bool,string> check,Action<GameObject,string> audit){
  foreach(string room in new[]{"home","family-living","family-kitchen","family-parents-bedroom","family-ian-bedroom","family-bathroom","bathroom","city","school","workplace","shop","finance","realestate"}){
   g.clock.Restore(4,new TimeSnapshot{subDaySeconds=10*3600});long cash=g.economy.Cash;yield return Go(room,check);var w=W;var profile=w.GetComponent<SceneVisualPresentation>();check(profile!=null&&profile.profile!=null,"Explicit scene visual profile "+room);check(g.economy.Cash==cash&&w.Service.Hour==10,"Presentation route preserves money/time "+room);var actors=w.GetComponent<WorldNPCScheduler>().actors;check(actors.Select(a=>a.npcId).Distinct().Count()==actors.Length,"No duplicate NPC identities "+room);check(UnityEngine.Object.FindObjectsByType<WorldPlayerMotor>(FindObjectsSortMode.None).Length==1,"One player after loading "+room);w.hud.Notify("",3);w.Player.Warp(w.Safe(w.CurrentZone,new Vector2(.2f,-.7f)));w.follow.Snap();yield return Shot("Scene-"+room);audit(w.hud.gameObject,"Phase30 Scene "+room);
   if(room=="city"||room=="workplace"||room=="family-living"){var renderer=w.GetComponentsInChildren<SpriteRenderer>().First(r=>r.enabled&&r!=w.Player.sprite&&r.sprite!=null&&r.color.a>.9f);var day=renderer.color;g.clock.Restore(4,new TimeSnapshot{subDaySeconds=22*3600});yield return new WaitForSecondsRealtime(.4f);check(renderer.color!=day,"Actual day/night renderer change "+room);yield return Shot("Scene-"+room+"-Night");}
  }
 }
}
}
#endif
