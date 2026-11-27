using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace IanGame.Editor
{
    public sealed class AndroidQADashboard : EditorWindow
    {
        static readonly string[] Filters = { "Live", "Debug", "Warning", "Error", "Crash", "Quest", "Save", "UI", "Network", "Performance", "Legal" };
        string[] ids = { "" }, labels = { "Auto (one phone)" };
        string output = "", lines = "", search = "", counts = "";
        string session;
        int selected, filter;
        bool busy, polling, paused, autoScroll = true;
        double nextPoll, clearTime;
        Vector2 scroll, page;
        Vector2 touch = new Vector2(.5f, .5f);
        string Device => ids[Math.Min(selected, ids.Length - 1)];

        [MenuItem("Tools/IanGrowing/Android QA Dashboard")]
        public static void Open() => GetWindow<AndroidQADashboard>("Android QA");

        async void RefreshDevices()
        {
            try
            {
                var list = JArray.Parse(await AndroidQATool.Run("devices")).Where(d => (string)d["state"] == "device" && !(bool)d["emulator"]).ToList();
                string previous = Device;
                ids = new[] { "" }.Concat(list.Select(d => (string)d["target"])).ToArray();
                labels = new[] { "Auto (one phone)" }.Concat(list.Select(d => d["model"] + " · " + d["connection"] + " · " + d["target"])).ToArray();
                selected = Math.Max(0, Array.IndexOf(ids, previous));
            }
            catch (Exception e) { output = e.Message; }
            Repaint();
        }

        async void InvokeQA(string command, params string[] values)
        {
            busy = true;
            try
            {
                if (command == "export")
                {
                    output = await AndroidQATool.Run(command, Device, values, Filters[filter], session);
                }
                else
                {
                    output = await AndroidQATool.Run(command, Device, values);
                }
                if (command == "start")
                {
                    session = Path.GetFileName(output);
                    paused = false;
                    clearTime = 0;
                }
                if (command == "end")
                {
                    paused = true;
                }
            }
            catch (Exception e) { output = e.Message; }
            finally { busy = false; }
            Repaint();
        }

        void Button(string label, string command)
        {
            if (GUILayout.Button(label)) InvokeQA(command);
        }

        void OnEnable() => RefreshDevices();

        async void Update()
        {
            if (SessionState.GetBool("ian.androidQA.installAfterBuild", false) && File.Exists(AndroidQABuild.Status))
            {
                string state = File.ReadAllText(AndroidQABuild.Status);
                if (state != "BUILDING" && state != "QUEUED")
                {
                    SessionState.SetBool("ian.androidQA.installAfterBuild", false);
                    if (state.StartsWith("Succeeded"))
                    {
                        busy = true;
                        try
                        {
                            string target = SessionState.GetString("ian.androidQA.buildTarget", "");
                            await AndroidQATool.Run("install", target);
                            session = Path.GetFileName(await AndroidQATool.Run("start", target));
                            output = await AndroidQATool.Run("launch", target);
                        }
                        catch (Exception e) { output = e.Message; }
                        finally { busy = false; }
                    }
                    else output = state;
                }
            }
            if (paused || polling || string.IsNullOrEmpty(session) || EditorApplication.timeSinceStartup < nextPoll)
                return;
            nextPoll = EditorApplication.timeSinceStartup + 1.3;
            polling = true;
            try
            {
                var result = await AndroidQATool.Run("tail", null,
                    string.IsNullOrEmpty(search) ? null : new[] { search }, Filters[filter], session);
                lines = string.Join("\n", result.Split('\n').Where(line =>
                    double.TryParse(line.Split(' ')[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double t) && t > clearTime));
                var path = Path.Combine("QAArtifacts/Android", session, "counts.json");
                if (File.Exists(path)) counts = File.ReadAllText(path);
                if (autoScroll) scroll.y = float.MaxValue;
            }
            catch (Exception e) { output = e.Message; }
            finally { polling = false; }
            Repaint();
        }

        void OnGUI()
        {
            page = EditorGUILayout.BeginScrollView(page);
            EditorGUILayout.LabelField("Android physical device", EditorStyles.boldLabel);
            int choice = EditorGUILayout.Popup("Target", selected, labels);
            if (choice != selected)
            {
                selected = choice;
                session = null;
                lines = "";
                counts = "";
            }
            using (new EditorGUI.DisabledScope(busy))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh devices")) RefreshDevices();
                Button("Device info", "info");
                Button("Status", "status");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Development APK")) AndroidQABuild.Queue();
                if (GUILayout.Button("Build & run"))
                {
                    AndroidQABuild.Queue();
                    SessionState.SetString("ian.androidQA.buildTarget", Device);
                    SessionState.SetBool("ian.androidQA.installAfterBuild", true);
                }
                if (GUILayout.Button("Patch via Build Profiles…"))
                {
                    EditorApplication.ExecuteMenuItem("File/Build Profiles");
                }
                Button("Install update", "install");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                Button("Launch", "launch");
                Button("Stop", "stop");
                Button("Restart", "restart");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Start Logcat")) { paused = false; InvokeQA("start"); }
                Button("End session", "end");
                if (GUILayout.Button("Unity Logcat")) EditorApplication.ExecuteMenuItem("Window/Analysis/Android Logcat");
                if (GUILayout.Button("Profiler")) EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                Button("PNG", "screenshot");
                Button("Record 10s", "record");
                Button("Mirror", "mirror");
                Button("Screen info", "screen");
                EditorGUILayout.EndHorizontal();
                touch = EditorGUILayout.Vector2Field("Touch (0–1)", touch);
                if (GUILayout.Button("Tap game"))
                    InvokeQA("tap", touch.x.ToString(CultureInfo.InvariantCulture), touch.y.ToString(CultureInfo.InvariantCulture));
                EditorGUILayout.BeginHorizontal();
                Button("Smoke", "smoke");
                Button("Regression", "regression");
                Button("Performance", "performance");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                Button("Crash", "crash");
                Button("Stack trace", "stack");
                Button("QA report", "report");
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.HelpBox(output, MessageType.None);
            filter = EditorGUILayout.Popup("IanGrowing filter", filter, Filters);
            search = EditorGUILayout.TextField("Search", search);
            EditorGUILayout.BeginHorizontal();
            paused = GUILayout.Toggle(paused, "Pause view");
            autoScroll = GUILayout.Toggle(autoScroll, "Auto-scroll");
            if (GUILayout.Button("Clear view"))
            {
                clearTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
                lines = "";
            }
            EditorGUILayout.EndHorizontal();
            using (new EditorGUI.DisabledScope(busy || string.IsNullOrEmpty(session)))
            {
                if (GUILayout.Button("Export filtered log"))
                {
                    InvokeQA("export", search);
                }
            }
            EditorGUILayout.LabelField(counts);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(160));
            EditorGUILayout.TextArea(lines);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndScrollView();
        }
    }
}
