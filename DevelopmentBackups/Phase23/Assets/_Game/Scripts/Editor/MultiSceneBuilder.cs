using System;using System.Linq;using System.IO;using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;using UnityEngine.InputSystem.UI;using TMPro;using UnityEditor;using UnityEditor.SceneManagement;using UnityEditor.Build.Profile;
namespace IanGame.Editor {
public static class MultiSceneBuilder {
 static readonly string folder="Assets/_Game/Scenes/";
 static Color ink=new Color32(19,24,28,255),cream=new Color32(236,232,215,255),lime=new Color32(210,230,116,255);
 static void Camera(){var go=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));go.tag="MainCamera";go.transform.position=new Vector3(0,0,-10);var c=go.GetComponent<Camera>();c.orthographic=true;c.orthographicSize=5;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=ink;}
 static Transform Canvas(string name){var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.GetComponent<UnityEngine.Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var s=go.GetComponent<CanvasScaler>();s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=new Vector2(1440,900);s.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;var frame=MissionUIBuilder.Rect(go.transform,"Frame",0,0,1440,900);frame.anchorMin=frame.anchorMax=new Vector2(.5f,.5f);frame.pivot=new Vector2(.5f,.5f);frame.anchoredPosition=Vector2.zero;return frame;}
 static void Input(){var go=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
 [MenuItem("Ian/Phase 2.2/Build Bootstrap Home School")]
 public static void Build(){
 foreach(string name in new[]{"Bootstrap","MainMenu","Home","School"})if(File.Exists(folder+name+".unity"))throw new Exception(name+" already exists; do not overwrite existing scenes.");
 var source=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(source.path!=folder+"MainGame2D.unity")throw new Exception("Open preserved MainGame2D first.");
 if(!EditorSceneManager.SaveScene(source,folder+"Home.unity",true))throw new Exception("Home copy failed");
 var home=EditorSceneManager.OpenScene(folder+"Home.unity",OpenSceneMode.Single);
 var game=UnityEngine.Object.FindAnyObjectByType<GameManager>();var systems=game.gameObject;
 var route=ScriptableObject.CreateInstance<SceneCatalog>();route.routes=Enum.GetValues(typeof(SceneId)).Cast<SceneId>().Select(id=>new SceneRoute{id=id,sceneName=id.ToString(),implemented=(int)id<=3}).ToArray();AssetDatabase.CreateAsset(route,"Assets/_Game/Data/SceneRoutes.asset");
 var context=systems.AddComponent<GameContext>();context.game=game;var transitions=systems.AddComponent<SceneTransitionService>();context.transitions=transitions;transitions.catalog=route;
 var loadingCanvas=new GameObject("Persistent Loading UI",typeof(RectTransform),typeof(UnityEngine.Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));loadingCanvas.transform.SetParent(systems.transform,false);var c=loadingCanvas.GetComponent<UnityEngine.Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=1000;
 var bg=MissionUIBuilder.Panel(loadingCanvas.transform,"Loading Overlay",0,0,1440,900,new Color(.04f,.06f,.07f,.97f));var br=bg.rectTransform;br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;transitions.loadingOverlay=bg.gameObject;
 var text=MissionUIBuilder.Text(bg.transform,"Loading Text","이동 준비 중...",0,0,700,90,32,cream);text.alignment=TextAlignmentOptions.Center;text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,.5f);text.rectTransform.pivot=new Vector2(.5f,.5f);text.rectTransform.anchoredPosition=Vector2.zero;transitions.loadingText=text;bg.gameObject.SetActive(false);
 PrefabUtility.SaveAsPrefabAsset(systems,"Assets/_Game/Prefabs/GameContext.prefab");
 var bindings=UnityEngine.Object.FindAnyObjectByType<GameplaySceneBindings>();bindings.game=null;UnityEngine.Object.DestroyImmediate(systems);
 var edition=GameObject.Find("Game Canvas").transform.Find("Dashboard/Edition").GetComponent<TMP_Text>();edition.text="HOME / BLACK COMEDY EDITION";
 EditorSceneManager.MarkSceneDirty(home);EditorSceneManager.SaveScene(home);
 EditorSceneManager.SaveScene(home,folder+"School.unity",true);var school=EditorSceneManager.OpenScene(folder+"School.unity",OpenSceneMode.Single);
 GameObject.Find("Game Canvas").transform.Find("Dashboard/Edition").GetComponent<TMP_Text>().text="SCHOOL / 기초 교육 · 관계 활동";
 var room=GameObject.Find("Apartment Sprite");if(room!=null){room.name="School Placeholder Background";room.GetComponent<SpriteRenderer>().color=new Color(.8f,.9f,1);}
 EditorSceneManager.MarkSceneDirty(school);EditorSceneManager.SaveScene(school);
 var boot=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Camera();PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/GameContext.prefab"));new GameObject("Bootstrap Entry",typeof(BootstrapEntry));EditorSceneManager.SaveScene(boot,folder+"Bootstrap.unity");
 var menu=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Camera();Input();var frame=Canvas("MainMenu Canvas");
 MissionUIBuilder.Panel(frame,"Background",0,0,1440,900,ink);MissionUIBuilder.Text(frame,"Edition","BLACK COMEDY EDITION",180,160,1080,40,25,lime);
 MissionUIBuilder.Text(frame,"Title","이안 키우기",180,230,1080,100,64,cream);
 MissionUIBuilder.Text(frame,"Slogan","이안을 행복하게 만들어 주세요. 행복은 별도 구매입니다.",180,345,1080,70,28,cream);
 MissionUIBuilder.Text(frame,"Scope","미션으로 성장하는 인생 / 공식 캐릭터 유지 / 기능 검증용 화면",180,435,1080,50,20,lime);
 var presenter=frame.gameObject.AddComponent<MainMenuPresenter>();presenter.continueButton=MissionUIBuilder.Button(frame,"Continue","계속하기 / Home",180,540,500,85);presenter.newGameButton=MissionUIBuilder.Button(frame,"New Life","새 인생",720,540,500,85);
 EditorSceneManager.SaveScene(menu,folder+"MainMenu.unity");
 var required=new[]{"Bootstrap","MainMenu","Home","School"}.Select(name=>new EditorBuildSettingsScene(folder+name+".unity",true)).ToArray();
 EditorBuildSettings.scenes=required.Concat(EditorBuildSettings.scenes.Where(s=>!required.Any(r=>r.path==s.path))).ToArray();
 var profile=BuildProfile.GetActiveBuildProfile();if(profile!=null&&profile.overrideGlobalScenes){profile.scenes=required.Concat(profile.scenes.Where(s=>!required.Any(r=>r.path==s.path))).ToArray();EditorUtility.SetDirty(profile);}
 AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(folder+"Bootstrap.unity",OpenSceneMode.Single);
 }
}
}
