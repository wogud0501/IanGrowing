using System;using System.Collections;using TMPro;using UnityEngine;using UnityEngine.SceneManagement;
namespace IanGame {
public class SceneTransitionService:MonoBehaviour {
 public SceneCatalog catalog;public GameObject loadingOverlay;public TMP_Text loadingText;
 public bool IsTransitioning{get;private set;}public string LastError{get;private set;}
 public SceneId CurrentScene{get;private set;}=SceneId.Bootstrap;public event Action<SceneId> SceneChanged;
 public bool Navigate(SceneId id){
 if(IsTransitioning)return false;var route=catalog==null?null:catalog.Find(id);
 if(id==SceneId.Bootstrap&&GameContext.Instance!=null&&SceneManager.GetActiveScene().name!="Bootstrap"){Fail("Bootstrap은 공통 시스템의 시작 씬입니다.");return false;}
 if(route==null||!route.implemented||string.IsNullOrEmpty(route.sceneName)||!Application.CanStreamedLevelBeLoaded(route.sceneName)){Fail("아직 준비되지 않은 장소입니다: "+id);return false;}
 var context=GameContext.Instance;if(context!=null&&context.game.State==GameState.Resolving)return false;
 LastError=null;StartCoroutine(Load(route));return true;
 }
 IEnumerator Load(SceneRoute route){
 IsTransitioning=true;if(loadingOverlay!=null)loadingOverlay.SetActive(true);
 var game=GameContext.Instance==null?null:GameContext.Instance.game;if(game!=null)game.clock.SetRunning(false);
 AsyncOperation operation=null;try{operation=SceneManager.LoadSceneAsync(route.sceneName,LoadSceneMode.Single);}catch(Exception e){Fail(e.Message);}
 if(operation==null){Finish(game,route.id,false);yield break;}
 while(!operation.isDone){if(loadingText!=null)loadingText.text="이동 중... "+Mathf.RoundToInt(operation.progress*100)+"%";yield return null;}
 CurrentScene=route.id;Finish(game,route.id,true);SceneChanged?.Invoke(route.id);
 }
 void Finish(GameManager game,SceneId target,bool success){IsTransitioning=false;if(loadingOverlay!=null)loadingOverlay.SetActive(false);if(game!=null)game.clock.SetRunning((success?target==SceneId.Home||target==SceneId.School||target==SceneId.City||target==SceneId.Workplace:CurrentScene==SceneId.Home||CurrentScene==SceneId.School||CurrentScene==SceneId.City||CurrentScene==SceneId.Workplace)&&game.State==GameState.Playing);if(game!=null)game.RefreshAvailability();}
 void Fail(string error){LastError=error;var game=GameContext.Instance==null?null:GameContext.Instance.game;if(game!=null)game.Log(error);if(loadingText!=null)loadingText.text=error;}
}
}