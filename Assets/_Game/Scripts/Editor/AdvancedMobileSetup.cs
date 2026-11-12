using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.OnScreen;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace IanGame.Editor {
public static class AdvancedMobileSetup {
 static RectTransform Rect(Transform parent,string name){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go.GetComponent<RectTransform>();}
 [MenuItem("Ian/Life World/Advanced/Apply Mobile Controls And Layout")]
 public static void Apply(){
  if(Application.isPlaying)throw new Exception("Stop Play first");var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");var w=UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();var hud=w.hud;
  var mobile=hud.GetComponent<WorldMobileHUD>()??hud.gameObject.AddComponent<WorldMobileHUD>();mobile.hud=hud;mobile.scaler=hud.GetComponent<CanvasScaler>();mobile.safeLayout=hud.GetComponentInChildren<UISafeLayout>(true);mobile.safeRoot=(RectTransform)hud.GetComponentInChildren<CompactWorldStatusBar>(true).transform.parent;
  if(mobile.touchRoot==null){
   mobile.touchRoot=Rect(hud.shell.baseGroup.transform,"Touch controls / same World actions");mobile.touchRoot.anchorMin=Vector2.zero;mobile.touchRoot.anchorMax=Vector2.one;mobile.touchRoot.offsetMin=mobile.touchRoot.offsetMax=Vector2.zero;
   var baseplate=Rect(mobile.touchRoot,"Joystick base");var image=baseplate.gameObject.AddComponent<Image>();image.sprite=hud.menuButton.GetComponent<Image>().sprite;image.type=Image.Type.Sliced;image.color=new Color(.6f,.7f,.8f,.45f);image.raycastTarget=false;
   var handle=Rect(baseplate,"Joystick touch area");handle.sizeDelta=new Vector2(200,200);var target=handle.gameObject.AddComponent<Image>();target.color=new Color(1,1,1,.01f);target.raycastTarget=true;
   var thumb=Rect(handle,"Joystick thumb");thumb.sizeDelta=new Vector2(80,80);var dot=thumb.gameObject.AddComponent<Image>();dot.sprite=image.sprite;dot.type=Image.Type.Sliced;dot.raycastTarget=false;
   mobile.joystick=handle.gameObject.AddComponent<OnScreenStick>();mobile.joystick.controlPath="<Gamepad>/leftStick";mobile.joystick.movementRange=64;mobile.joystick.useIsolatedInputActions=true;
   // Layout positions the input area. Its base plate follows the same anchor.
   baseplate.anchorMin=baseplate.anchorMax=Vector2.zero;baseplate.sizeDelta=Vector2.zero;
   OnScreenButton Button(string name,string path){var b=UnityEngine.Object.Instantiate(hud.menuButton,mobile.touchRoot);b.name=name;b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();b.GetComponentInChildren<TMP_Text>().text=name;var control=b.gameObject.AddComponent<OnScreenButton>();control.controlPath=path;return control;}
   mobile.interact=Button("사용","<Gamepad>/buttonSouth");mobile.inventory=Button("가방","<Gamepad>/buttonWest");mobile.menu=Button("메뉴","<Gamepad>/start");mobile.run=Button("달리기","<Gamepad>/rightTrigger");
   foreach(var b in new[]{mobile.interact,mobile.inventory,mobile.menu,mobile.run}){var rect=b.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(100,100);}
  }
  var content=hud.shell.settingsPanel.GetComponentInChildren<ScrollRect>(true).content;
  Button Setting(string name){var existing=content.Find(name);if(existing!=null)return existing.GetComponent<Button>();var b=UnityEngine.Object.Instantiate(hud.shell.motionButton,content);b.name=name;b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();var le=b.GetComponent<LayoutElement>()??b.gameObject.AddComponent<LayoutElement>();le.preferredHeight=le.minHeight=72;return b;}
  mobile.frameRateButton=Setting("Mobile frame rate");mobile.detailButton=Setting("Mobile environment detail");mobile.touchRoot.gameObject.SetActive(false);mobile.simulateMobile=false;
  PlayerSettings.Android.resizeableActivity=true;PlayerSettings.Android.resizeableActivity=true;PlayerSettings.resizableWindow=true;
  PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;PlayerSettings.allowedAutorotateToPortrait=true;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;PlayerSettings.allowedAutorotateToLandscapeLeft=PlayerSettings.allowedAutorotateToLandscapeRight=true;
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);PlayerSettings.iOS.targetOSVersionString="15.0";
  EditorUtility.SetDirty(mobile);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 }
}
}
