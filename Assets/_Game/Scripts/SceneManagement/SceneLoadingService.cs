using UnityEngine;
using UnityEngine.SceneManagement;

namespace IanGame.Commercial
{
    public sealed class SceneLoadingService : MonoBehaviour
    {
#if UNITY_EDITOR
        public bool FailNextLoad;
#endif

        public AsyncOperation Begin(string sceneName)
        {
#if UNITY_EDITOR
            if (FailNextLoad)
            {
                FailNextLoad = false;
                throw new System.InvalidOperationException("검증용 로딩 실패 · 이전 장소를 유지합니다.");
            }

#endif
            // Additive staging leaves the previous scene available until destination is ready.
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation != null)
            {
                operation.allowSceneActivation = false;
            }

            return operation;
        }
    }
}
