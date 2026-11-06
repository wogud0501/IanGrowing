using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using IanGame.Commercial;
namespace IanGame.Editor {
public static class CommercialSlicePolish {
 [MenuItem("Ian/Commercial/Polish Home School Slice")]
 public static void Apply(){
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode");
  var theme=AssetDatabase.LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset");var frame=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Commercial/UI/Panel_64.png");
  var path="Assets/_Game/Data/Commercial/IanMotionLibrary.asset";var library=AssetDatabase.LoadAssetAtPath<SpriteMotionLibrary>(path);
  if(library==null){library=ScriptableObject.CreateInstance<SpriteMotionLibrary>();library.fallback=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Characters/Ian/Sprites/Ian_Base_Cutout.png");library.clips=Enum.GetValues(typeof(SpriteMotion)).Cast<SpriteMotion>().Select(s=>new SpriteMotionClip{state=s,loop=s==SpriteMotion.Idle,duration=s==SpriteMotion.Blink?.2f:s==SpriteMotion.Sleep?3:2.4f,framesPerSecond=s==SpriteMotion.Blink?10:6,frames=s==SpriteMotion.Idle?new[]{library.fallback}:new Sprite[0]}).ToArray();AssetDatabase.CreateAsset(library,path);}
  foreach(string name in new[]{"Home","School","City","Workplace"}){
   var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/"+name+".unity");
   var p=UnityEngine.Object.FindFirstObjectByType<PixelHomePresenter>();
   foreach(var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
    var image=b.GetComponent<UnityEngine.UI.Image>();if(image==null)continue;image.sprite=frame;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;b.transition=Selectable.Transition.ColorTint;var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.88f,.65f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.65f,.72f,.8f);colors.disabledColor=new Color(.5f,.5f,.5f);b.colors=colors;foreach(var text in b.GetComponentsInChildren<TMP_Text>(true)){text.color=theme.cream;text.alignment=TextAlignmentOptions.Center;}
   }
   var room=UnityEngine.Object.FindFirstObjectByType<RoomAtmosphere>();var player=room.character.GetComponent<SpriteMotionPlayer>();if(player==null)player=room.character.gameObject.AddComponent<SpriteMotionPlayer>();player.library=library;player.target=room.character;
   // A separately addressable monitor/sunlight layer reuses the background texture region.
   if(room.transform.Find("Animated environmental detail")==null){
    var s=room.background.sprite;var rect=name=="Home"?new Rect(541,205,47,42):new Rect(150,100,25,14);var crop=Sprite.Create(s.texture,rect,new Vector2(.5f,.5f),100);AssetDatabase.CreateAsset(crop,"Assets/_Game/Art/Commercial/"+name+"_Detail.asset");
    var go=new GameObject("Animated environmental detail",typeof(SpriteRenderer));go.transform.SetParent(room.transform);go.transform.localPosition=new Vector3((rect.center.x-320)/100,(rect.center.y-180)/100,0);var detail=go.GetComponent<SpriteRenderer>();detail.sprite=crop;detail.sortingOrder=-19;room.animatedDetail=detail;
   }
   var nav=p.navigation;var shell=nav.stack;
   foreach(var view in new[]{nav.lifePanel,nav.growthPanel,nav.activityPanel,nav.inventoryPanel,nav.menuPanel,nav.missionPanel,shell.settingsPanel,shell.pausePanel,shell.confirmPanel}){
    if(view==null||view.transform.childCount==0)continue;
    var veil=view.GetComponent<UnityEngine.UI.Image>();if(veil!=null){veil.color=new Color(.035f,.055f,.09f,.8f);veil.sprite=null;veil.type=UnityEngine.UI.Image.Type.Simple;}
    var card=view.transform.GetChild(0);var background=card.GetComponent<UnityEngine.UI.Image>();if(background!=null){background.sprite=frame;background.type=UnityEngine.UI.Image.Type.Sliced;background.color=Color.white;}
    foreach(var text in card.GetComponentsInChildren<TMP_Text>(true))text.color=theme.cream;
    if(view==nav.lifePanel){var r=card as RectTransform;r.anchorMin=new Vector2(.22f,.27f);r.anchorMax=new Vector2(.78f,.78f);r.offsetMin=r.offsetMax=Vector2.zero;}
   }
   if(p.notice!=null){var image=p.notice.GetComponent<UnityEngine.UI.Image>();if(image!=null){image.sprite=frame;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;}p.noticeText.color=theme.cream;}
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity");
 }
}
}

