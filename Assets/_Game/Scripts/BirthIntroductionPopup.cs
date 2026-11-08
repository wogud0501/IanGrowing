using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace IanGame {
public sealed class BirthIntroductionPopup:MonoBehaviour {
 public GameObject view;public CanvasGroup group;public RectTransform frame;public Button begin;public UIShell shell;
 GameManager game;bool showing,closing;Coroutine animation;
 public bool IsShowing=>showing;
 void Start(){if(GameContext.Instance==null)return;game=GameContext.Instance.game;game.StateChanged+=Changed;GameContext.Instance.transitions.SceneChanged+=SceneChanged;begin.onClick.AddListener(Confirm);Check();}
 void OnDestroy(){if(game!=null)game.StateChanged-=Changed;if(GameContext.Instance!=null)GameContext.Instance.transitions.SceneChanged-=SceneChanged;}
 void Changed(GameState state){Check();}void SceneChanged(SceneId id){Check();}
 void Check(){if(game==null||closing)return;if(game.BirthIntroductionPending&&(GameContext.Instance.transitions.CurrentScene==SceneId.Home||game.world!=null&&game.world.Active)&&!GameContext.Instance.transitions.IsTransitioning){if(showing)return;showing=true;game.clock.SetRunning(false);shell.Push(view);if(animation!=null)StopCoroutine(animation);animation=StartCoroutine(Reveal());}else if(showing&&!game.BirthIntroductionPending){showing=false;shell.Close(view);}}
 IEnumerator Reveal(){float duration=UIShell.ReducedMotion?.001f:.16f;for(float t=0;t<1;t+=Time.unscaledDeltaTime/duration){group.alpha=Mathf.SmoothStep(0,1,t);float scale=Mathf.Round(Mathf.Lerp(.98f,1,t)*100)/100;frame.localScale=Vector3.one*scale;yield return null;}frame.localScale=Vector3.one;group.alpha=1;UIShell.Focus(view);}
 public void Confirm(){if(!showing||closing)return;closing=true;if(!game.CompleteBirthIntroduction()){closing=false;return;}showing=false;shell.Close(view);closing=false;}
}
}
