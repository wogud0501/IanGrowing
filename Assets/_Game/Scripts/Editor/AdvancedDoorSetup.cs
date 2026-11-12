using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace IanGame.Editor {
public static class AdvancedDoorSetup {
 public static WorldDoorStyle Style(int row){
  string path=AdvancedLifeSetup.Data+"Door"+row+".asset";var style=AssetDatabase.LoadAssetAtPath<WorldDoorStyle>(path);if(style==null){style=ScriptableObject.CreateInstance<WorldDoorStyle>();AssetDatabase.CreateAsset(style,path);}
  var frames=AssetDatabase.LoadAllAssetsAtPath(AdvancedLifeSetup.Art+"Doors.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();style.mechanism=(DoorMechanism)row;style.openingFrames=frames.Skip(row*6).Take(6).ToArray();style.seconds=row==2?.28f:.32f;EditorUtility.SetDirty(style);return style;
 }
 public static WorldDoor Attach(LifeWorldController w,WorldInteractable o){
  int row=o.zone=="bathroom"||o.targetZone=="bathroom"?1:o.zone=="shop"||o.targetZone=="shop"||o.zone=="workplace"||o.targetZone=="workplace"?2:0;
  var door=o.GetComponent<WorldDoor>()??o.gameObject.AddComponent<WorldDoor>();door.portal=o;door.world=w;door.style=Style(row);door.normal=((Vector2)o.transform.position-o.Point).normalized;
  if(door.normal.sqrMagnitude<.1f)door.normal=Vector2.up;var plane=o.Point+door.normal*.35f;door.threshold=plane-(Vector2)o.transform.position;
  var leaf=o.transform.Find("Animated door leaf");if(leaf==null){leaf=new GameObject("Animated door leaf",typeof(SpriteRenderer)).transform;leaf.SetParent(o.transform,false);}leaf.position=plane-Vector2.up*.1f;leaf.localScale=new Vector3(1/o.transform.lossyScale.x,1/o.transform.lossyScale.y,1);
  door.leaf=leaf.GetComponent<SpriteRenderer>();door.leaf.sprite=door.style.openingFrames[0];door.leaf.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/VisualRebuild/WorldAlphaCut.mat");door.leaf.sortingOrder=1000-Mathf.RoundToInt(plane.y*100);
  bool building=o.id=="house-entrance"||o.id=="shop-front"||o.id=="office-front"||o.id=="school-front"||o.id=="npc-residence-front";
  if(o.visual!=door.leaf&&!building)o.visual.enabled=false;
  if(!building)o.visual=door.leaf;
  var original=o.GetComponent<BoxCollider2D>();if(original!=null&&original.enabled){
   if(building&&original.size.x>1.5f){float gap=1.25f/o.transform.lossyScale.x,wing=(original.size.x-gap)/2;
    for(int i=0;i<2;i++){string name="Doorway wall "+i;var edge=o.transform.Find(name);if(edge==null){edge=new GameObject(name,typeof(BoxCollider2D)).transform;edge.SetParent(o.transform,false);}var col=edge.GetComponent<BoxCollider2D>();col.size=new Vector2(wing,original.size.y);edge.localPosition=new Vector2((i==0?-1:1)*(gap/2+wing/2),original.offset.y);}}
   original.enabled=false;
  }
  var barrier=o.transform.Find("Closed leaf collision");if(barrier==null){barrier=new GameObject("Closed leaf collision",typeof(BoxCollider2D)).transform;barrier.SetParent(o.transform,false);}barrier.position=plane;barrier.localScale=new Vector3(1/o.transform.lossyScale.x,1/o.transform.lossyScale.y,1);door.closedCollider=barrier.GetComponent<BoxCollider2D>();door.closedCollider.size=new Vector2(1.12f,.12f);
  var backing=leaf.Find("Doorway interior shadow");if(backing==null){backing=new GameObject("Doorway interior shadow",typeof(SpriteRenderer)).transform;backing.SetParent(leaf,false);}var shadow=backing.GetComponent<SpriteRenderer>();shadow.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/LifeWorld/Wood.png");if(shadow.sprite==null)shadow.sprite=LifeWorldArtBuilder.Solid();backing.localPosition=new Vector2(0,1);backing.localScale=new Vector3(1.05f/shadow.sprite.bounds.size.x,1.8f/shadow.sprite.bounds.size.y,1);shadow.color=new Color(.12f,.14f,.17f);shadow.sortingOrder=door.leaf.sortingOrder-1;
  if(o.id=="shop-front"||o.id=="shop-exit"){door.opens=7;door.closes=23;door.staffId="colleague";}
  door.ResetClosed();EditorUtility.SetDirty(o);EditorUtility.SetDirty(door);return door;
 }
 [MenuItem("Ian/Life World/Advanced/Apply Animated Doors And Transitions")]
 public static void Apply(){
  if(Application.isPlaying)throw new Exception("Stop Play first");var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");var w=UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();
  foreach(var o in w.objects.Where(o=>o.kind==WorldInteractionKind.Door&&o.id!="city-path"&&o.id!="alley-path"))Attach(w,o);
  var director=w.GetComponent<WorldTransitionDirector>()??w.gameObject.AddComponent<WorldTransitionDirector>();director.hud=w.hud;director.cameraRig=w.follow;
  if(director.mask==null){var mask=new GameObject("Pixel transition mask",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));mask.transform.SetParent(w.hud.transform,false);director.mask=mask.GetComponent<RectTransform>();director.mask.anchorMin=Vector2.zero;director.mask.anchorMax=Vector2.one;director.mask.offsetMin=director.mask.offsetMax=Vector2.zero;var canvas=mask.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=350;director.strips=new Image[12];for(int i=0;i<12;i++){var go=new GameObject("Pixel strip "+i,typeof(RectTransform),typeof(Image));go.transform.SetParent(mask.transform,false);director.strips[i]=go.GetComponent<Image>();director.strips[i].raycastTarget=true;}}
  w.transitions=director;director.End();EditorUtility.SetDirty(w);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 }
}
}
