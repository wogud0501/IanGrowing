using System.Collections;
using UnityEngine;

namespace IanGame
{
    public class BootstrapEntry : MonoBehaviour
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        // App UI and Enhanced Touch both initialize at subsystem registration.
        // Acquire touch tracking after their reset, before native device discovery.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void InitializeMobileTouch()
        {
            if (!UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled)
            {
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
            }
        }
#endif

        IEnumerator Start()
        {
            yield return null;
            var context = GameContext.Instance;
            if (context == null)
            {
                Debug.LogError("Bootstrap GameContext missing");
                yield break;
            }

            context.transitions.Navigate(SceneId.MainMenu);
        }
    }
}
