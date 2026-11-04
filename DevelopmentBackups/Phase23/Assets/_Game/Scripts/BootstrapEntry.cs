using System.Collections;using UnityEngine;
namespace IanGame {
public class BootstrapEntry:MonoBehaviour {
 IEnumerator Start(){yield return null;var context=GameContext.Instance;if(context==null){Debug.LogError("Bootstrap GameContext missing");yield break;}context.transitions.Navigate(SceneId.MainMenu);}
}
}