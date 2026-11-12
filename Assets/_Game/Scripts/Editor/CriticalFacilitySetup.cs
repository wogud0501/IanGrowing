using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace IanGame.Editor {
public static partial class WorldQualityRecoverySetup {
 static Sprite[] ImportWork(){
  string path="Assets/_Game/Art/CriticalRecovery/FrontWork.png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.spritePixelsPerUnit=120;importer.GetSourceTextureWidthAndHeight(out int width,out int height);
  var parts=new SpriteMetaData[16];for(int r=0;r<4;r++)for(int c=0;c<4;c++)parts[r*4+c]=new SpriteMetaData{name="FrontWork_"+(r*4+c).ToString("D2"),rect=new Rect(c*width/4f,height-(r+1)*height/4f,width/4f,height/4f),alignment=9,pivot=new Vector2(.5f,.04f)};importer.spritesheet=parts;importer.SaveAndReimport();
  var empty=(TextureImporter)AssetImporter.GetAtPath("Assets/_Game/Art/CriticalRecovery/EmptyDesk.png");empty.textureType=TextureImporterType.Sprite;empty.spriteImportMode=SpriteImportMode.Single;empty.filterMode=FilterMode.Point;empty.mipmapEnabled=false;empty.textureCompression=TextureImporterCompression.Uncompressed;empty.alphaIsTransparency=true;empty.spritePixelsPerUnit=400;var settings=new TextureImporterSettings();empty.ReadTextureSettings(settings);settings.spriteAlignment=9;settings.spritePivot=new Vector2(.5f,.165f);empty.SetTextureSettings(settings);empty.SaveAndReimport();
  return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
 }
 static void CriticalRecoveryLayout(){
  var frames=ImportWork();var empty=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/CriticalRecovery/EmptyDesk.png");var scheduler=world.GetComponent<WorldNPCScheduler>();var staff=rules.npcs.Where(d=>d.schedule.Any(e=>e.action==NPCDailyAction.Work)).ToArray();int office=0,finance=0,agency=0;
  foreach(var d in staff){string zone=d.npcId=="resident-3"||d.npcId=="resident-4"?"finance":d.npcId=="resident-8"||d.npcId=="resident-9"?"realestate":"workplace";int index=zone=="finance"?finance++:zone=="realestate"?agency++:office++;
   var p=zone=="workplace"?new[]{new Vector2(-5.6f,1.5f),new Vector2(-1.2f,1.5f),new Vector2(-5.6f,-2.3f),new Vector2(-1.2f,-2.3f),new Vector2(3.2f,1.5f)}[index]:new Vector2(-3+index*5.5f,.4f);
   var o=Prop("staff-desk-"+d.npcId,d.title+" · "+(zone=="finance"?"금융 상담":zone=="realestate"?"주거 상담":"업무석"),zone,p,empty,WorldInteractionKind.Work,1);o.UsePoint=Vector2.up*.8f;var col=o.GetComponent<BoxCollider2D>();col.size=new Vector2(1.9f,.5f);col.offset=Vector2.up*.15f;
   var slot=Component<WorldActivitySlot>(o.gameObject);if(slot.seat!=null)slot.seat.gameObject.SetActive(false);slot.seat=null;slot.staffId=d.npcId;slot.facing=Vector2.down;d.workZone=zone;d.jobId=zone=="finance"?"analyst":"office";
   var actor=scheduler.actors.First(a=>a.npcId==d.npcId);int row=actor.GetComponent<WorldActorAnimation>().library.name.Last()-'0';row=Mathf.Clamp(row,0,3);
   var work=Component<WorldWorkstationPresentation>(o.gameObject);work.actor=actor;work.slot=slot;work.empty=o.visual;work.frames=frames.Skip(row*4).Take(4).ToArray();work.working=Image(o.transform,"Front-facing work animation",work.frames[0],Vector2.zero);work.working.enabled=false;
   foreach(var e in d.schedule.Where(e=>e.action==NPCDailyAction.Work)){e.zone=zone;e.objectId=o.id;e.position=o.Point-rules.Zone(zone).origin;}
   if(zone!="workplace"){
    var rest=Prop(zone+"-staff-rest-"+index,"직원 휴식",zone,new Vector2(-4+index*8,-.7f),AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/VisualRebuild/Furniture.png").OfType<Sprite>().First(s=>s.name=="Chair"),WorldInteractionKind.Rest,.65f);rest.enabled=false;rest.UsePoint=Vector2.zero;var rs=Component<WorldActivitySlot>(rest.gameObject);rs.staffId=d.npcId;rs.facing=Vector2.right;rs.seat=rest.visual;
    foreach(var e in d.schedule.Where(e=>e.objectId.StartsWith("office-break-"))){e.zone=zone;e.objectId=rest.id;e.position=rest.Point-rules.Zone(zone).origin;}
   }
  }
  foreach(var visit in new[]{new[]{"resident-2","finance","resident-3"},new[]{"resident-7","realestate","resident-8"}}){
   var customer=rules.npcs.First(d=>d.npcId==visit[0]);string zone=visit[1],id=zone+"-consultation-point";var point=Find(id);if(point==null){var go=new GameObject(id,typeof(WorldInteractable));go.transform.SetParent(world.transform,false);point=go.GetComponent<WorldInteractable>();world.objects=world.objects.Concat(new[]{point}).ToArray();}point.id=id;point.zone=zone;point.title="상담 방문 위치";point.kind=WorldInteractionKind.Rest;point.UsePoint=Vector2.zero;point.transform.position=rules.Zone(zone).origin+new Vector2(-3,-.6f);point.enabled=false;var slot=Component<WorldActivitySlot>(point.gameObject);slot.staffId=customer.npcId;slot.facing=Vector2.up;customer.companionId=visit[2];
   var entry=customer.schedule.FirstOrDefault(e=>e.objectId==id);if(entry==null){entry=new NPCScheduleEntry();customer.schedule=customer.schedule.Concat(new[]{entry}).ToArray();}entry.hour=zone=="finance"?14:15;entry.minute=20;entry.dayMask=31;entry.zone=zone;entry.objectId=id;entry.action=NPCDailyAction.Talk;entry.position=point.Point-rules.Zone(zone).origin;customer.schedule=customer.schedule.OrderBy(e=>e.hour*60+e.minute).ToArray();
  }
  foreach(string zone in new[]{"finance","realestate"}){
   var root=world.transform.Cast<Transform>().First(t=>t.name.EndsWith(" / "+zone));var terminal=Find(zone=="finance"?"stock-terminal":"property-agency");terminal.transform.position=rules.Zone(zone).origin+new Vector2(-4,-2.4f);terminal.visual.transform.localScale=Vector3.one*.65f;terminal.GetComponent<BoxCollider2D>().size=new Vector2(1.1f,.35f);terminal.visual.sortingOrder=1240;
   var waiting=Find(zone+"-waiting");waiting.transform.position=rules.Zone(zone).origin+new Vector2(3.6f,-3);waiting.visual.sortingOrder=1300;
   var board=Image(root,"Live facility board",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/UI/Pixel/Panel-Parchment.png"),new Vector2(0,4.2f));board.enabled=true;board.drawMode=SpriteDrawMode.Sliced;board.size=new Vector2(8,1.2f);board.sortingOrder=5900;
   Caption(root,"Facility heading",zone=="finance"?"이안 증권 · 시장 안내":"이안 부동산 · 매물 안내",new Vector2(0,4.95f),10);
   Caption(root,"Live facility values","",new Vector2(0,4.2f),7.5f);var label=root.Find("Live facility values").GetComponent<TMPro.TMP_Text>();label.fontSize=2.3f;label.rectTransform.sizeDelta=new Vector2(7.5f,1.1f);var live=Component<WorldFacilityInformation>(label.gameObject);live.label=label;live.property=zone=="realestate";
   Caption(root,"Customer flow","상담 업무석",new Vector2(0,.25f),10);Caption(root,"Self service",zone=="finance"?"거래 단말":"매물 조회",new Vector2(-4,-3.9f),3);
  }
 }
}
}
