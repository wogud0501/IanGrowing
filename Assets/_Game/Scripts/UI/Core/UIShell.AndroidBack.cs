#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Scripting;

namespace IanGame
{
    public partial class UIShell
    {
        // GameActivity receives committed system gestures separately from keyboard Escape.
        sealed class AndroidBackCallback : AndroidJavaProxy
        {
            readonly UIShell owner;

            public AndroidBackCallback(UIShell shell) : base("android.window.OnBackInvokedCallback")
            {
                owner = shell;
            }

            [Preserve]
            public void onBackInvoked()
            {
                AndroidApplication.InvokeOnUnityMainThread(() =>
                {
                    if (owner && owner.isActiveAndEnabled)
                    {
                        owner.HandleAndroidBack();
                    }
                });
            }
        }

        AndroidBackCallback androidBack;

        void OnEnable()
        {
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            if (version.GetStatic<int>("SDK_INT") < 33)
            {
                return;
            }

            var callback = new AndroidBackCallback(this);
            androidBack = callback;
            AndroidApplication.InvokeOnUIThread(() =>
            {
                using var api = new AndroidJavaClass("android.window.OnBackInvokedDispatcher");
                using var dispatcher = AndroidApplication.currentActivity.Call<AndroidJavaObject>("getOnBackInvokedDispatcher");
                dispatcher.Call("registerOnBackInvokedCallback", api.GetStatic<int>("PRIORITY_OVERLAY"), callback);
            });
        }

        void HandleAndroidBack()
        {
            if (TouchScreenKeyboard.visible)
            {
                AndroidApplication.InvokeOnUIThread(() =>
                {
                    var activity = AndroidApplication.currentActivity;
                    using var focus = activity.Call<AndroidJavaObject>("getCurrentFocus");
                    if (focus != null)
                    {
                        using var ime = activity.Call<AndroidJavaObject>("getSystemService", "input_method");
                        using var token = focus.Call<AndroidJavaObject>("getWindowToken");
                        ime.Call<bool>("hideSoftInputFromWindow", token, 0);
                    }
                });
                return;
            }

            if (mainMenu && Depth == 0 && GameContext.Instance?.transitions.IsTransitioning == false)
            {
                AndroidApplication.InvokeOnUIThread(() => AndroidApplication.currentActivity.Call<bool>("moveTaskToBack", true));
                return;
            }

            RequestBack();
        }

        void ReleaseAndroidBack()
        {
            var callback = androidBack;
            if (callback == null)
            {
                return;
            }

            androidBack = null;
            AndroidApplication.InvokeOnUIThread(() =>
            {
                using var dispatcher = AndroidApplication.currentActivity.Call<AndroidJavaObject>("getOnBackInvokedDispatcher");
                dispatcher.Call("unregisterOnBackInvokedCallback", callback);
            });
        }
    }
}
#endif
