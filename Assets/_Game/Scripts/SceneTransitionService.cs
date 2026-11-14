using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using IanGame.Commercial;
namespace IanGame {
public partial class SceneTransitionService : MonoBehaviour {
 public SceneCatalog catalog;public GameObject loadingOverlay;public TMP_Text loadingText;
 public TransitionController presentation;
 public bool IsTransitioning{get;private set;}public string LastError{get;private set;}
 public SceneId CurrentScene{get;private set;}=SceneId.Bootstrap;public event Action<SceneId> SceneChanged;
 public bool Navigate(SceneId id){
  if(IsTransitioning)return false;
  if(roomCatalog!=null&&!LegacyTesting&&TryRoomNavigation(id,out bool handled))return handled;
  var accessGame=GameContext.Instance==null?null:GameContext.Instance.game;if(accessGame!=null&&!GrowthAccess.Scene(accessGame,id,out var denied)){Fail(denied);return false;}

  var world=GameContext.Instance==null?null:GameContext.Instance.game.world;if(world!=null&&world.rules!=null&&LifeWorldService.ZoneFor(id)!=null){if(CurrentScene==SceneId.LifeWorld&&world.Host!=null)return world.Host.Travel(LifeWorldService.ZoneFor(id));if(id!=SceneId.Home||CurrentScene!=SceneId.MainMenu)world.SelectDestination(id);id=SceneId.LifeWorld;}
 return NavigateRoute(id);
 }
 #if UNITY_EDITOR || DEVELOPMENT_BUILD
 // QA can reach the original automation scenes without changing normal world routing.
 public void UseLegacyTesting(){LegacyTesting=true;roomCatalog?.ToLegacy(GameContext.Instance.game.world.State);GameContext.Instance.game.world.UseLegacyRules();}
 public void PrepareRoomStateRestore(){LegacyTesting=false;GameContext.Instance.game.world.UseRoomRules(roomCatalog);}
 public bool NavigateForTesting(SceneId id){LegacyTesting=roomCatalog==null||roomCatalog.Find(id)==null;if(LegacyTesting){var world=GameContext.Instance.game.world;roomCatalog?.ToLegacy(world.State);world.UseLegacyRules();}else{var world=GameContext.Instance.game.world;return NavigateRoom(roomCatalog.Find(id).roomId,world.State.position,world.State.facing);}return NavigateRoute(id);}
 #endif
 bool NavigateRoute(SceneId id){
  if(IsTransitioning)return false;
 var accessGame=GameContext.Instance==null?null:GameContext.Instance.game;if(accessGame!=null&&!GrowthAccess.Scene(accessGame,id,out var denied)){Fail(denied);return false;}
 if(id==CurrentScene&&SceneManager.GetActiveScene().name==catalog?.Find(id)?.sceneName)return true;
  var route=catalog==null?null:catalog.Find(id);
  if(id==SceneId.Bootstrap&&GameContext.Instance!=null&&SceneManager.GetActiveScene().name!="Bootstrap"){Fail("Bootstrap은 공통 시스템의 시작 씬입니다.");return false;}
  if(route==null||!route.implemented||string.IsNullOrEmpty(route.sceneName)||!Application.CanStreamedLevelBeLoaded(route.sceneName)){Fail("아직 준비되지 않은 장소입니다. "+id);return false;}
  var context=GameContext.Instance;if(context!=null&&context.game.State==GameState.Resolving)return false;
  LastError=null;IsTransitioning=true;StartCoroutine(Load(route));return true;
 }
 IEnumerator Load(SceneRoute route){
  var game=GameContext.Instance==null?null:GameContext.Instance.game;bool running=game!=null&&game.clock.IsRunning;
  if(game!=null)game.clock.SetRunning(false);
  var previous=SceneManager.GetActiveScene();var priorId=CurrentScene;
  var profile=presentation==null?null:presentation.Find(priorId,route.id);
  if(presentation!=null)yield return presentation.Exit(profile);else if(loadingOverlay!=null)loadingOverlay.SetActive(true);
  AsyncOperation operation=null;
  try{operation=presentation==null?SceneManager.LoadSceneAsync(route.sceneName,LoadSceneMode.Additive):presentation.loader.Begin(route.sceneName);}catch(Exception e){Fail(e.Message);}
  if(operation==null){yield return Recover(game,running,profile);yield break;}
  while(operation.progress<.9f&&!operation.isDone){if(loadingText!=null)loadingText.text="이동 중 · "+Mathf.RoundToInt(operation.progress/.9f*100)+"%";yield return null;}
  // The incoming scene owns fresh input/audio/camera components. Disable outgoing
  // owners before activation so additive staging never runs two EventSystems.
  var suspended=previous.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Behaviour>(false))
   .Where(b=>b.enabled&&(b is UnityEngine.EventSystems.EventSystem||b is UnityEngine.EventSystems.BaseInputModule||b is AudioListener||b is Camera)).ToArray();
  foreach(var component in suspended)component.enabled=false;
  operation.allowSceneActivation=true;while(!operation.isDone)yield return null;
  var destination=SceneManager.GetSceneByName(route.sceneName);
  if(!destination.IsValid()||!destination.isLoaded){foreach(var component in suspended)if(component!=null)component.enabled=true;Fail("장소를 불러오지 못했습니다. 이전 화면으로 돌아갑니다.");yield return Recover(game,running,profile);yield break;}
  CurrentScene=route.id;SceneManager.SetActiveScene(destination);
  if(previous.IsValid()&&previous.isLoaded){var unload=SceneManager.UnloadSceneAsync(previous);if(unload!=null)yield return unload;}
  yield return null;if(presentation!=null)yield return presentation.Enter(profile);
  Finish(game,IsGameplay(route.id));SceneChanged?.Invoke(route.id);
 }
 IEnumerator Recover(GameManager game,bool running,TransitionProfile profile){if(presentation!=null)yield return presentation.Enter(profile);Finish(game,running);}
 void Finish(GameManager game,bool running){IsTransitioning=false;if(loadingOverlay!=null)loadingOverlay.SetActive(false);if(game!=null){game.clock.SetRunning(running&&game.State==GameState.Playing&&!game.BirthIntroductionPending);game.RefreshAvailability();}}
 static bool IsGameplay(SceneId id)=>id>=SceneId.RoomFamilyLiving||id==SceneId.Home||id==SceneId.School||id==SceneId.City||id==SceneId.Workplace||id==SceneId.Battle||id==SceneId.StockMarket||id==SceneId.LifeWorld;
 void Fail(string error){LastError=error;var game=GameContext.Instance==null?null:GameContext.Instance.game;if(game!=null)game.Log(error);if(loadingText!=null)loadingText.text=error;}
}
}
