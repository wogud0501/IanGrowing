using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using IanGame;
#endif

public sealed class IanCloudErrorReporter : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    static IanCloudErrorReporter instance;
    CloudErrorOutbox outbox;
    string configDirectory;
    float nextProbe;
    public static bool Active => instance && instance.outbox != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (!instance)
        {
            new GameObject("IanCloudErrorReporter").AddComponent<IanCloudErrorReporter>();
        }
    }

    void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        try
        {
            string root = Application.persistentDataPath;
            configDirectory = Path.Combine(root, "IanCloudDiagnostics");
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var directory = activity.Call<AndroidJavaObject>("getNoBackupFilesDir"))
            {
                configDirectory = Path.Combine(directory.Call<string>("getAbsolutePath"), "IanCloudDiagnostics");
            }
#endif
            string config = Path.Combine(configDirectory, "session.json");
            if (!File.Exists(config))
            {
                return;
            }
            string platform = Application.platform.ToString().ToLowerInvariant();
            if (platform == "iphoneplayer")
            {
                platform = "ios";
            }
            if (platform == "windowsplayer")
            {
                platform = "windows";
            }
            var ticket = CloudErrorTicket.Read(config, platform);
            if (ticket == null)
            {
                return;
            }
            string spool = Path.Combine(root, "IanCloudDiagnostics", "outbox");
#if UNITY_IOS && !UNITY_EDITOR
            Directory.CreateDirectory(Path.GetDirectoryName(spool));
            UnityEngine.iOS.Device.SetNoBackupFlag(Path.GetDirectoryName(spool));
#endif
            outbox = new CloudErrorOutbox(spool, config, platform, Application.version, Application.buildGUID);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.Full);
            Application.logMessageReceivedThreaded += OnLog;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandled;
        }
        catch
        {
            StopCapture();
        }
    }

    void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            outbox?.Record(type.ToString(), message, trace);
        }
    }

    void OnUnhandled(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception ex)
        {
            outbox?.Record("UnhandledException", ex.ToString(), ex.StackTrace ?? "");
        }
        if (args.IsTerminating)
        {
            outbox?.FlushBriefly();
        }
    }

    void Update()
    {
        if (outbox == null || Time.unscaledTime < nextProbe)
        {
            return;
        }
        nextProbe = Time.unscaledTime + 1;
        // Only an operator with access to this enrolled Development sandbox can request the probe.
        string command = Path.Combine(configDirectory, "test-once");
        try
        {
            if (!File.Exists(command))
            {
                return;
            }
            File.Delete(command);
            SendTestError();
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [ContextMenu("Send Cloud Test Error")]
    public void SendTestError()
    {
        if (outbox == null)
        {
            return;
        }
        string prefix = "IAN_CLOUD_" + outbox.Platform.ToUpperInvariant() + "_TEST_";
        Debug.LogError(prefix + "ERROR");
        try
        {
            ThrowTestException(prefix);
        }
        catch (InvalidOperationException ex)
        {
            Debug.LogException(ex);
        }
        Debug.LogAssertion(prefix + "ASSERT");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowTestException(string prefix)
    {
        throw new InvalidOperationException(prefix + "EXCEPTION\nSecond line of exception.");
    }

    void OnApplicationPause(bool paused)
    {
        if (!paused)
        {
            outbox?.Retry();
        }
    }

    void StopCapture()
    {
        Application.logMessageReceivedThreaded -= OnLog;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandled;
        Interlocked.Exchange(ref outbox, null)?.Stop();
    }

    void OnApplicationQuit() => StopCapture();

    void OnDestroy()
    {
        if (instance == this)
        {
            StopCapture();
            instance = null;
        }
    }
#else
    public static bool Active => false;
#endif
}
