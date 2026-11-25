using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IanGame
{
    internal enum DiagnosticEvent
    {
        SessionStarted, SceneEntered, SceneTransitionRequested, InteractionStarted, InteractionEnded,
        DialogueStarted, DialogueEnded, SleepStarted, MissionCompleted, SaveStarted, SaveCompleted, SaveFailed
    }

    [DefaultExecutionOrder(-1500)]
    internal sealed class LocalDiagnostics : MonoBehaviour
    {
        internal static LocalDiagnostics Current { get; private set; }
        internal DiagnosticStore Store { get; private set; }
        internal bool PreviousInterrupted { get; private set; }
        readonly object gate = new object();
        readonly Queue<string> history = new Queue<string>();
        readonly Queue<DiagnosticReport> pending = new Queue<DiagnosticReport>();
        readonly Dictionary<string, DateTime> recent = new Dictionary<string, DateTime>();
        string scene, version, platform, build;
        string activity = DiagnosticReport.Missing;
        int age = -1, day = -1;
        DateTime nextCleanup;
        FileStream marker;
        string markerPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (!Current)
            {
                new GameObject("Local diagnostics").AddComponent<LocalDiagnostics>();
            }
        }

        void Awake()
        {
            Current = this;
            DontDestroyOnLoad(gameObject);
            string root = Path.Combine(Application.persistentDataPath, "Diagnostics");
            Store = new DiagnosticStore(root);
            version = Application.version;
            platform = Application.platform.ToString();
            build = string.IsNullOrEmpty(Application.buildGUID) ? DiagnosticReport.Missing : Application.buildGUID;
            scene = DiagnosticReport.Missing;
            Store.Try(() =>
            {
                Directory.CreateDirectory(Store.Sessions);
#if UNITY_IOS && !UNITY_EDITOR
                UnityEngine.iOS.Device.SetNoBackupFlag(root);
#endif
                foreach (var path in Directory.GetFiles(Store.Sessions, "*.session"))
                {
                    try
                    {
                        using (var old = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        {
                            PreviousInterrupted |= old.ReadByte() == 1;
                        }
                        File.Delete(path);
                    }
                    catch (IOException) { /* Another process may own this session. */ }
                }
                markerPath = Path.Combine(Store.Sessions, Guid.NewGuid().ToString("N") + ".session");
                marker = new FileStream(markerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                WriteMarker(1);
            });
            Record(DiagnosticEvent.SessionStarted);
            Application.logMessageReceivedThreaded += OnLog;
            SceneManager.activeSceneChanged += SceneChanged;
            if (PreviousInterrupted)
            {
                Write("UnexpectedTermination", "Previous foreground session ended without a clean marker.");
            }
        }

        public bool ConsumeNotice()
        {
            bool show = PreviousInterrupted;
            PreviousInterrupted = false;
            return show;
        }

        static string Stamp() => DateTime.UtcNow.ToString("HH:mm:ss");
        internal static void Record(DiagnosticEvent code, WorldInteractionKind? kind = null)
        {
            AndroidQAEvent.Breadcrumb(code);
            var recorder = Current;
            if (recorder == null)
            {
                return;
            }
            lock (recorder.gate)
            {
                if (recorder.history.Count == 40)
                {
                    recorder.history.Dequeue();
                }
                recorder.history.Enqueue(Stamp() + " " + code + (kind.HasValue ? " " + kind.Value : ""));
                if (code == DiagnosticEvent.InteractionStarted)
                {
                    recorder.activity = kind.ToString();
                }
                else if (code == DiagnosticEvent.InteractionEnded)
                {
                    recorder.activity = "Idle";
                }
            }
        }

        void SceneChanged(Scene previous, Scene current)
        {
            lock (gate)
            {
                scene = current.name;
                activity = "Idle";
            }
            Record(DiagnosticEvent.SceneEntered);
        }

        DiagnosticReport Snapshot(string kind, DateTime now)
        {
            return new DiagnosticReport
            {
                id = "ERR-" + now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"),
                utc = now.ToString("O"), type = kind, version = version, platform = platform, build = build,
                scene = scene, day = day, age = age, activity = activity, events = history.ToArray()
            };
        }

        void OnLog(string condition, string trace, LogType kind)
        {
            if (kind != LogType.Error && kind != LogType.Exception && kind != LogType.Assert)
            {
                return;
            }

            lock (gate)
            {
                if (pending.Count >= 16)
                {
                    return;
                }
                DateTime now = DateTime.UtcNow;
                string raw = Cut(condition, 1024);
                string stack = Cut(trace, 16384);
                // Keep only a bounded, transient fingerprint of potentially sensitive input.
                string key = kind + "/" + raw.GetHashCode() + "/" + stack.GetHashCode();
                if (recent.TryGetValue(key, out var last) && now - last < TimeSpan.FromMinutes(1))
                {
                    return;
                }
                if (recent.Count >= 64)
                {
                    foreach (var stale in recent.Where(p => now - p.Value >= TimeSpan.FromMinutes(1)).Select(p => p.Key).ToArray())
                    {
                        recent.Remove(stale);
                    }
                    if (recent.Count >= 64)
                    {
                        return;
                    }
                }
                recent[key] = now;
                var report = Snapshot(kind.ToString(), now);
                report.Decode(raw, stack);
                pending.Enqueue(report);
            }
        }

        static string Cut(string text, int limit) =>
            string.IsNullOrEmpty(text) ? "" : text.Length <= limit ? text : text.Substring(0, limit);

        internal static void HandledSaveError(Exception exception)
        {
            var recorder = Current;
            if (recorder != null)
            {
                // Use only the exception category. IO error messages often contain a personal filesystem path.
                recorder.OnLog(exception.GetType().Name, exception.StackTrace, LogType.Exception);
            }
        }

        bool Write(string type, string message)
        {
            DiagnosticReport report;
            lock (gate)
            {
                report = Snapshot(type, DateTime.UtcNow);
            }
            report.message = message;
            return Store.Put(report);
        }

        internal bool Manual() => Write("Manual", "Player requested a diagnostic snapshot.");

        void Update()
        {
            var game = GameContext.Instance?.game;
            lock (gate)
            {
                age = game && game.growth ? game.growth.Age : -1;
                day = game && game.clock ? game.clock.Day : -1;
            }
            Drain(2);
            if (DateTime.UtcNow >= nextCleanup)
            {
                nextCleanup = DateTime.UtcNow.AddHours(1);
                Store.Try(Store.Prune);
            }
        }

        internal void Drain(int limit)
        {
            for (int i = 0; i < limit; i++)
            {
                DiagnosticReport next;
                lock (gate)
                {
                    if (pending.Count == 0)
                    {
                        return;
                    }
                    next = pending.Dequeue();
                }
                Store.Put(next);
            }
        }

        internal void DeleteAll()
        {
            lock (gate)
            {
                pending.Clear();
            }
            Store.Try(Store.Clear);
        }

        void WriteMarker(byte state)
        {
            if (marker == null)
            {
                return;
            }
            marker.Position = 0;
            marker.WriteByte(state);
            marker.Flush(true);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Drain(16);
            }
            Store.Try(() => WriteMarker(paused ? (byte)0 : (byte)1));
        }

        void OnApplicationQuit()
        {
            Drain(16);
            Store.Try(() => WriteMarker(0));
        }

        void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            SceneManager.activeSceneChanged -= SceneChanged;
            Drain(16);
            Store.Try(() =>
            {
                marker?.Dispose();
                if (markerPath != null)
                {
                    File.Delete(markerPath);
                }
            });
            Current = null;
        }
    }
}
