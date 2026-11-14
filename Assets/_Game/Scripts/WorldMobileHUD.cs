using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;
namespace IanGame {
public enum WorldScreenProfile {Desktop,PhonePortrait,WideTouch,ExpandedTouch}
// Resize changes presentation only. It never reloads the scene, resets a model,
// warps an actor or recreates InputActionMaps. Safe-area values come from Unity.
public sealed class WorldMobileHUD:MonoBehaviour {
 public WorldHUD hud;public CanvasScaler scaler;public UISafeLayout safeLayout;public RectTransform safeRoot,touchRoot;
 public OnScreenStick joystick;public OnScreenButton interact,inventory,menu,run;
 public Button frameRateButton,detailButton;public bool simulateMobile;
 public WorldScreenProfile Profile{get;private set;}public int Rebuilds{get;private set;}
 public bool IsMobile=>Application.isMobilePlatform||simulateMobile;
 int width,height;Rect area;bool paused;int previousFps,previousVsync;bool lowDetails;
 readonly Dictionary<LayoutElement,float> originalRows=new Dictionary<LayoutElement,float>();
 readonly Dictionary<RectTransform,Vector4> originalWindows=new Dictionary<RectTransform,Vector4>();
 void Start(){previousFps=Application.targetFrameRate;previousVsync=QualitySettings.vSyncCount;frameRateButton?.onClick.AddListener(ToggleFrameRate);detailButton?.onClick.AddListener(ToggleDetails);Apply(true);}
 void Update(){Apply(false);if(touchRoot!=null&&touchRoot.gameObject.activeInHierarchy){bool enabled=hud.world.CanControl;joystick.enabled=enabled;interact.enabled=enabled;run.enabled=enabled;}}
 public void Apply(bool force=false){
  Rect safe=safeLayout!=null&&safeLayout.simulateSafeArea?safeLayout.simulatedSafeArea:Screen.safeArea;
  if(safe.width<=0||safe.height<=0)safe=new Rect(0,0,Screen.width,Screen.height);
  bool mobile=IsMobile;var profile=!mobile?WorldScreenProfile.Desktop:Screen.width/(float)Mathf.Max(1,Screen.height)<.75f?WorldScreenProfile.PhonePortrait:Screen.width>Screen.height*1.55f?WorldScreenProfile.WideTouch:WorldScreenProfile.ExpandedTouch;
  if(!force&&width==Screen.width&&height==Screen.height&&area==safe&&Profile==profile)return;width=Screen.width;height=Screen.height;area=safe;Profile=profile;Rebuilds++;
  bool compact=profile==WorldScreenProfile.PhonePortrait;
  scaler.referenceResolution=compact?new Vector2(720,1280):profile==WorldScreenProfile.ExpandedTouch?new Vector2(1280,960):new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
  if(safeLayout!=null)safeLayout.Apply(true);else{safeRoot.anchorMin=new Vector2(safe.xMin/width,safe.yMin/height);safeRoot.anchorMax=new Vector2(safe.xMax/width,safe.yMax/height);safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;}
  hud.GetComponent<WorldHUDLayout>().compactStack=compact;
  touchRoot.gameObject.SetActive(false);hud.menuButton.gameObject.SetActive(!mobile);
  foreach(var type in hud.GetComponentsInChildren<UITypeScale>(true)){type.platformScale=compact?1.12f:1;type.Apply();}
  foreach(var row in hud.GetComponentsInChildren<LayoutElement>(true))if(row.preferredHeight>=150&&row.GetComponentInChildren<Button>(true)!=null){if(!originalRows.ContainsKey(row))originalRows[row]=row.preferredHeight;row.preferredHeight=compact?Mathf.Max(330,originalRows[row]):originalRows[row];}
  foreach(var root in new[]{hud.shell.overlayRoot})foreach(RectTransform child in root){if(child.childCount==0)continue;var card=child.GetChild(0) as RectTransform;if(card==null||card.GetComponent<Image>()==null)continue;if(!originalWindows.ContainsKey(card))originalWindows[card]=new Vector4(card.anchorMin.x,card.anchorMin.y,card.anchorMax.x,card.anchorMax.y);var old=originalWindows[card];card.anchorMin=compact?new Vector2(.025f,.05f):new Vector2(old.x,old.y);card.anchorMax=compact?new Vector2(.975f,.95f):new Vector2(old.z,old.w);card.offsetMin=card.offsetMax=Vector2.zero;}
  foreach(var row in hud.GetComponentsInChildren<LayoutElement>(true))if(row.GetComponent<Button>()!=null&&row.preferredHeight>0){if(!originalRows.ContainsKey(row))originalRows[row]=row.preferredHeight;row.preferredHeight=compact?Mathf.Max(88,originalRows[row]):originalRows[row];}
  foreach(var back in hud.GetComponentsInChildren<Button>(true))if(back.name=="Back"||back.name=="Close"){
   var element=back.GetComponent<LayoutElement>()??back.gameObject.AddComponent<LayoutElement>();element.minWidth=element.preferredWidth=104;
   var heading=back.transform.parent.GetComponent<LayoutElement>();if(heading!=null){if(!originalRows.ContainsKey(heading))originalRows[heading]=heading.preferredHeight;heading.preferredHeight=compact?Mathf.Max(88,originalRows[heading]):originalRows[heading];}
  }
  var needs=(RectTransform)hud.needs.transform.parent;needs.anchorMin=mobile?new Vector2(.018f,compact?.25f:.02f):new Vector2(.018f,.02f);needs.anchorMax=mobile?new Vector2(compact?.982f:.48f,compact?.30f:.085f):new Vector2(.46f,.085f);needs.offsetMin=needs.offsetMax=Vector2.zero;
  Position(joystick.transform.parent as RectTransform,compact?new Vector2(.2f,.115f):new Vector2(.12f,.19f),new Vector2(200,200));
  var handle=(RectTransform)joystick.transform;handle.anchorMin=handle.anchorMax=new Vector2(.5f,.5f);handle.anchoredPosition=Vector2.zero;handle.sizeDelta=new Vector2(200,200);
  Position(interact.transform as RectTransform,compact?new Vector2(.84f,.12f):new Vector2(.9f,.18f),new Vector2(112,112));
  Position(inventory.transform as RectTransform,compact?new Vector2(.72f,.215f):new Vector2(.9f,.43f),new Vector2(92,92));
  Position(menu.transform as RectTransform,compact?new Vector2(.9f,.215f):new Vector2(.78f,.43f),new Vector2(92,92));
  Position(run.transform as RectTransform,compact?new Vector2(.61f,.10f):new Vector2(.78f,.18f),new Vector2(92,92));
  touchRoot.gameObject.SetActive(mobile&&!paused);hud.ReflowNotification();ApplyQuality();Canvas.ForceUpdateCanvases();hud.Prompt(hud.promptPanel.activeSelf?hud.prompt.text:null);
 }
 static void Position(RectTransform r,Vector2 anchor,Vector2 size){r.anchorMin=r.anchorMax=anchor;r.pivot=Vector2.one*.5f;r.anchoredPosition=Vector2.zero;r.sizeDelta=size;}
 public void ToggleFrameRate(){PlayerPrefs.SetInt("ian.mobile.fps",PlayerPrefs.GetInt("ian.mobile.fps",60)==60?30:60);PlayerPrefs.Save();ApplyQuality();}
 public void ToggleDetails(){PlayerPrefs.SetInt("ian.mobile.lowDetail",PlayerPrefs.GetInt("ian.mobile.lowDetail",0)==0?1:0);PlayerPrefs.Save();ApplyQuality();}
 void ApplyQuality(){lowDetails=PlayerPrefs.GetInt("ian.mobile.lowDetail",0)==1;hud.world.lowDetail=IsMobile&&lowDetails;if(IsMobile){QualitySettings.vSyncCount=0;Application.targetFrameRate=PlayerPrefs.GetInt("ian.mobile.fps",60)==30?30:60;}if(frameRateButton!=null)frameRateButton.GetComponentInChildren<TMP_Text>().text="모바일 목표 FPS · "+PlayerPrefs.GetInt("ian.mobile.fps",60);if(detailButton!=null)detailButton.GetComponentInChildren<TMP_Text>().text="모바일 환경 효과 · "+(lowDetails?"절전":"기본");}
 void OnApplicationPause(bool value){paused=value;if(value)Release();else Apply(true);}
 void OnApplicationFocus(bool focused){if(!focused)Release();else if(isActiveAndEnabled)Apply(true);}
 void Release(){if(touchRoot!=null)touchRoot.gameObject.SetActive(false);}
 void OnDisable(){Release();if(IsMobile){Application.targetFrameRate=previousFps;QualitySettings.vSyncCount=previousVsync;}}
}
}
