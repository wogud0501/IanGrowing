using UnityEngine;
using UnityEngine.SceneManagement;
namespace IanGame.Commercial {
public sealed class SceneLoadingService : MonoBehaviour {
 public AsyncOperation Begin(string sceneName) {
  // Additive staging leaves the previous scene available until destination is ready.
  var operation=SceneManager.LoadSceneAsync(sceneName,LoadSceneMode.Additive);
  if(operation!=null)operation.allowSceneActivation=false;
  return operation;
 }
}
}
