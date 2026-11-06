using System.Collections;
using UnityEngine;
namespace IanGame.Commercial {
public sealed class TransitionController : MonoBehaviour {
 public TransitionProfile[] profiles;
 public TransitionOverlay overlay;public SceneLoadingService loader;public AudioTransitionService audioService;
 public TransitionProfile Find(SceneId from,SceneId to){return profiles==null?null:System.Array.Find(profiles,p=>p!=null&&p.from==from&&p.to==to);}
 public IEnumerator Exit(TransitionProfile profile){
  foreach(var point in Object.FindObjectsByType<SceneExitPoint>(FindObjectsSortMode.None))point.Exit();
  if(profile!=null){overlay.image.color=profile.curtain;overlay.caption.text=profile.departure;audioService.Play(profile.exitSound);}
  yield return overlay.Wipe(true,Duration(profile==null?.25f:profile.exitSeconds));
 }
 public IEnumerator Enter(TransitionProfile profile){
  foreach(var point in Object.FindObjectsByType<SceneEntryPoint>(FindObjectsSortMode.None))point.Enter();
  if(profile!=null){overlay.caption.text=profile.arrival;audioService.Play(profile.entrySound);}
  // Wait for layout, binding and a rendered destination before uncovering it.
  yield return null;yield return new WaitForEndOfFrame();
  if(GameContext.Instance!=null)GameContext.Instance.game.RefreshAvailability();
  Canvas.ForceUpdateCanvases();
  foreach(var text in Object.FindObjectsByType<TMPro.TMP_Text>())text.ForceMeshUpdate(false,true);
  if(profile!=null&&Duration(profile.coveredSeconds)>0)yield return new WaitForSecondsRealtime(Duration(profile.coveredSeconds));
  yield return overlay.Wipe(false,Duration(profile==null?.3f:profile.entrySeconds));
 }
 public static float Duration(float seconds){int mode=PlayerPrefs.GetInt("ian.visual.transitions",0);return mode==2?0:UIShell.ReducedMotion||mode==1?Mathf.Min(.08f,seconds):seconds;}
}
}
