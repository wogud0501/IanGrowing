using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using IanGame.Commercial;
namespace IanGame.Editor {
public static class CommercialSliceBuilder {
 const string Art="Assets/_Game/Art/Commercial/";
 const string Data="Assets/_Game/Data/Commercial/";
 static PixelUITheme theme;static Sprite panel;
 static T Ensure<T>(Component host)where T:Component {var c=host.GetComponent<T>();return c!=null?c:host.gameObject.AddComponent<T>();}
 static RectTransform Rect(Transform parent,string name){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
 static void Place(RectTransform r,Transform parent,Vector2 min,Vector2 max){r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;var le=r.GetComponent<LayoutElement>();if(le!=null)le.ignoreLayout=true;}
 static UnityEngine.UI.Image Window(Transform parent,string name,Vector2 min,Vector2 max){var r=Rect(parent,name);Place(r,parent,min,max);var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();i.sprite=panel;i.type=UnityEngine.UI.Image.Type.Sliced;i.raycastTarget=false;return i;}
 static TMP_Text Text(Transform parent,string name,string value,int size){var r=Rect(parent,name);Place(r,parent,Vector2.zero,Vector2.one);r.offsetMin=new Vector2(18,8);r.offsetMax=new Vector2(-18,-8);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=theme.font;t.fontSize=size;t.color=theme.cream;t.text=value;t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;return t;}
 public static Sprite Import(string path,int border=0){AssetDatabase.ImportAsset(path);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=100;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.spriteBorder=new Vector4(border,border,border,border);var settings=new TextureImporterSettings();i.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(settings);i.npotScale=TextureImporterNPOTScale.None;i.maxTextureSize=2048;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
 public static void Derivative(string source,string destination,int width,int height){var src=new Texture2D(2,2,TextureFormat.RGBA32,false);src.LoadImage(File.ReadAllBytes(source));var dst=new Texture2D(width,height,TextureFormat.RGBA32,false);var pixels=new Color32[width*height];var original=src.GetPixels32();for(int y=0;y<height;y++)for(int x=0;x<width;x++)pixels[y*width+x]=original[Mathf.Min(src.height-1,(int)((y+.5f)*src.height/height))*src.width+Mathf.Min(src.width-1,(int)((x+.5f)*src.width/width))];dst.SetPixels32(pixels);dst.Apply();File.WriteAllBytes(destination,dst.EncodeToPNG());UnityEngine.Object.DestroyImmediate(src);UnityEngine.Object.DestroyImmediate(dst);Import(destination);}
 [MenuItem("Ian/Commercial/Build Home School Slice")]
 public static void Build(){
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode before building presentation.");
  theme=AssetDatabase.LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset");
  panel=File.Exists(Art+"UI/Panel_64.png")?Import(Art+"UI/Panel_64.png",10):theme.panelDark;
  Directory.CreateDirectory(Data);
  foreach(string name in new[]{"Home","School"}){
   Derivative(Art+"Backgrounds/"+name+"_Source.png",Art+"Backgrounds/"+name+"_640.png",640,360);
   var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/"+name+".unity");
   if(GameObject.Find("Commercial Presentation")!=null)throw new InvalidOperationException("Already built "+name+"; use targeted edits.");
   Room(name);HUD(name);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  Transitions();Atlas();AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/_Game/Scenes/Home.unity");
 }
 static void Room(string name){
  var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=1.8f;camera.transform.position=new Vector3(0,0,-10);camera.allowHDR=camera.allowMSAA=camera.allowDynamicResolution=false;camera.backgroundColor=theme.ink;
  if(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline==null||!UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().FullName.Contains("Universal"))throw new InvalidOperationException("Expected audited URP pipeline");
  var pp=Ensure<PixelPerfectCamera>(camera);pp.assetsPPU=100;pp.refResolutionX=640;pp.refResolutionY=360;pp.gridSnapping=PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;pp.cropFrame=PixelPerfectCamera.CropFrame.Windowbox;
  var cd=Ensure<UniversalAdditionalCameraData>(camera);cd.renderPostProcessing=false;cd.antialiasing=AntialiasingMode.None;
  var stage=new GameObject("Commercial Presentation");var room=stage.AddComponent<RoomAtmosphere>();room.school=name=="School";stage.AddComponent<SceneEntryPoint>();stage.AddComponent<SceneExitPoint>();
  var old=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).First(s=>s.name.Contains("Apartment")||s.name.Contains("Placeholder Background"));old.gameObject.SetActive(false);
  var back=new GameObject(name+" / environmental master",typeof(SpriteRenderer));back.transform.SetParent(stage.transform);room.background=back.GetComponent<SpriteRenderer>();room.background.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Backgrounds/"+name+"_640.png");room.background.sortingOrder=-20;
  var charView=UnityEngine.Object.FindFirstObjectByType<Character2DPresentation>();charView.enabled=false;var character=charView.spriteRenderer;character.transform.SetParent(stage.transform);character.transform.localScale=Vector3.one*(1.18f/character.sprite.bounds.size.y);character.transform.localPosition=new Vector3(0,-.55f,0);character.sortingOrder=10;room.character=character;room.characterOrigin=character.transform.localPosition;
  var dot=theme.Icon(PixelIcon.Coin);room.motes=new SpriteRenderer[10];for(int j=0;j<10;j++){var g=new GameObject(name=="School"?"Sun dust "+j:"Window rain glint "+j,typeof(SpriteRenderer));g.transform.SetParent(stage.transform);g.transform.localPosition=new Vector3(name=="School"?-2+j*.15f:-1.5f+(j%4)*.22f,.2f+j*.09f,0);g.transform.localScale=Vector3.one*.06f;var s=g.GetComponent<SpriteRenderer>();s.sprite=dot;s.sortingOrder=-10;s.color=name=="School"?new Color(1,.94f,.7f,.3f):new Color(.5f,.8f,1,.3f);room.motes[j]=s;}
  // Furniture attachments are independent; new props can be placed without repainting the master.
  new GameObject("Furniture / expansion sockets").transform.SetParent(stage.transform);
 }
 public static void ExtendHUD(string name){theme=AssetDatabase.LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset");panel=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"UI/Panel_64.png");HUD(name);}
 static void HUD(string name){
  var p=UnityEngine.Object.FindFirstObjectByType<PixelHomePresenter>();var root=p.gameObject;
  if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(root),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  foreach(var b in root.GetComponents<Behaviour>())if(b is PixelCanvasScale||b is UISafeLayout||b is UIResponsiveHUD||b is IndieHUD)b.enabled=false;
  var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
  // Preserve the existing integer UI scale; replace its old dashboard layout only in these scenes.
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=1;
  var safe=Rect(root.transform,"Commercial HUD safe area");Place(safe,root.transform,Vector2.zero,Vector2.one);var group=safe.gameObject.AddComponent<CanvasGroup>();p.navigation.stack.baseGroup=group;
  var commercial=root.AddComponent<CommercialHUD>();commercial.safeRoot=safe;
  var identity=Window(safe,"Identity / day and currency",new Vector2(.018f,.87f),new Vector2(.47f,.985f));commercial.title=Text(identity.transform,"Place",name=="Home"?"이안의 방  /  HOME":name=="School"?"교실  /  SCHOOL":name=="City"?"도시  /  CITY":"직장  /  WORKPLACE",24);Place(commercial.title.rectTransform,identity.transform,new Vector2(0,.42f),Vector2.one);commercial.title.rectTransform.offsetMin=new Vector2(18,0);
  Place(p.age.rectTransform,identity.transform,new Vector2(.04f,.08f),new Vector2(.48f,.48f));p.age.color=theme.cream;
  Place(p.cash.rectTransform,identity.transform,new Vector2(.51f,.08f),new Vector2(.96f,.48f));p.cash.color=theme.gold;
  var vital=p.vitalLabels[0].transform.parent.parent.parent.parent as RectTransform;Place(vital,safe,new Vector2(.61f,.88f),new Vector2(.982f,.985f));
  var quest=p.missionTitle.transform.parent as RectTransform;Place(quest,safe,new Vector2(.75f,.53f),new Vector2(.982f,.86f));
  var recent=p.recent.transform.parent as RectTransform;Place(recent,safe,new Vector2(.75f,.34f),new Vector2(.982f,.51f));
  var quick=p.quickButtons[0].transform.parent as RectTransform;Place(quick,safe,new Vector2(.22f,.105f),new Vector2(.73f,.18f));
  var nav=p.navigation.navigation.GetComponent<RectTransform>();Place(nav,safe,new Vector2(.22f,.02f),new Vector2(.78f,.095f));
  var more=root.GetComponentsInChildren<PixelNavigationButton>(true).First(b=>b.target==PixelNavigationButton.Target.Menu);Place(more.GetComponent<RectTransform>(),safe,new Vector2(.905f,.02f),new Vector2(.982f,.095f));more.gameObject.SetActive(true);
  var caption=Window(safe,"Location / caption",new Vector2(.018f,.20f),new Vector2(.40f,.285f));Place(p.caption.rectTransform,caption.transform,new Vector2(.04f,.05f),new Vector2(.96f,.95f));p.caption.color=theme.cream;p.caption.fontSize=16;
  // Keep all displaced controllers and modal views intact, with obsolete visual containers hidden.
  foreach(string child in new[]{"Print background","Dashboard","HUD Safe area","Location Navigation"}){var t=root.transform.Find(child);if(t!=null)t.gameObject.SetActive(false);}
  foreach(var image in new[]{vital.GetComponent<UnityEngine.UI.Image>(),quest.GetComponent<UnityEngine.UI.Image>(),recent.GetComponent<UnityEngine.UI.Image>()})if(image!=null){image.sprite=panel;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;}
  foreach(var text in vital.GetComponentsInChildren<TMP_Text>(true).Concat(quest.GetComponentsInChildren<TMP_Text>(true)))text.color=theme.cream;
  foreach(var g in root.GetComponentsInChildren<UIAdaptiveLayout>(true))if(g.transform.IsChildOf(safe))g.enabled=false;
  var receipt=Window(safe,"Pixel report / action feedback",new Vector2(.31f,.56f),new Vector2(.69f,.76f));commercial.receipt=receipt.gameObject.AddComponent<CanvasGroup>();commercial.feedback=Text(receipt.transform,"Result","",24);receipt.gameObject.SetActive(false);
  var settings=p.navigation.stack.settingsPanel;var settingsBody=settings.GetComponentsInChildren<ScrollRect>(true).FirstOrDefault();Transform target=settingsBody==null?settings.transform:settingsBody.content;
  commercial.transitionButton=Option(target,"Transition preference");commercial.effectsButton=Option(target,"Environment quality");
  // Nested overlay canvases remain rendered above the compact world HUD.
  foreach(var c in root.GetComponentsInChildren<Canvas>(true))if(c!=canvas){if(c.name.Contains("Overlay"))c.sortingOrder=200;}
  if(name=="School"){
   var lesson=Window(safe,"School activity access",new Vector2(.018f,.105f),new Vector2(.20f,.18f));lesson.raycastTarget=true;var b=lesson.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=lesson;Text(lesson.transform,"Label","학교 활동",18);var cmd=lesson.gameObject.AddComponent<UICommand>();cmd.shell=p.navigation.stack;cmd.command=UICommand.Command.Open;cmd.panel=p.navigation.locationPanel;UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick,cmd.Invoke);
  }
 }
 static UnityEngine.UI.Button Option(Transform p,string name){var r=Rect(p,name);r.sizeDelta=new Vector2(400,48);var le=r.gameObject.AddComponent<LayoutElement>();le.preferredHeight=48;var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();i.sprite=theme.button;i.type=UnityEngine.UI.Image.Type.Sliced;var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=i;var t=Text(r,"Label",name,18);t.color=theme.ink;return b;}
 public static void Transitions(){
  var path="Assets/_Game/Prefabs/GameContext.prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try{var service=root.GetComponent<SceneTransitionService>();var controller=Ensure<TransitionController>(root.transform);controller.loader=Ensure<SceneLoadingService>(root.transform);controller.audioService=Ensure<AudioTransitionService>(root.transform);controller.audioService.source=Ensure<AudioSource>(root.transform);controller.audioService.source.playOnAwake=false;
   var old=service.loadingOverlay;var host=old.transform.parent;var r=Rect(host,"Commercial Transition Overlay");Place(r,host,Vector2.zero,Vector2.one);var overlay=r.gameObject.AddComponent<TransitionOverlay>();overlay.group=r.gameObject.AddComponent<CanvasGroup>();overlay.group.alpha=0;
   // Full-size transparent hit target blocks repeated input even before the curtain covers it.
   var blocker=r.gameObject.AddComponent<UnityEngine.UI.Image>();blocker.color=Color.clear;blocker.raycastTarget=true;
   var curtain=Rect(r,"Pixel shutter");Place(curtain,r,Vector2.zero,Vector2.one);overlay.curtain=curtain;overlay.image=curtain.gameObject.AddComponent<UnityEngine.UI.Image>();overlay.image.sprite=panel;overlay.image.type=UnityEngine.UI.Image.Type.Sliced;overlay.image.raycastTarget=false;overlay.caption=Text(curtain,"Travel caption","이동 준비",30);overlay.caption.alignment=TextAlignmentOptions.Center;overlay.Block(false);
   controller.overlay=overlay;service.presentation=controller;old.SetActive(false);
   controller.profiles=new TransitionProfile[2];for(int i=0;i<2;i++){string file=Data+(i==0?"HomeToSchool":"SchoolToHome")+".asset";var p=AssetDatabase.LoadAssetAtPath<TransitionProfile>(file);if(p==null){p=ScriptableObject.CreateInstance<TransitionProfile>();AssetDatabase.CreateAsset(p,file);}p.from=i==0?SceneId.Home:SceneId.School;p.to=i==0?SceneId.School:SceneId.Home;p.departure=i==0?"등교 준비 · 오늘도 출석은 자산입니다.":"수업 종료 · 성장은 내일도 청구됩니다.";p.arrival=i==0?"학교 · 규칙적인 하루가 시작됩니다.":"집 · 현실은 문밖에 두세요.";EditorUtility.SetDirty(p);controller.profiles[i]=p;}
   PrefabUtility.SaveAsPrefabAsset(root,path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
 public static void Atlas(){CommercialAtlasPrebuild.Configure();}
}
}

