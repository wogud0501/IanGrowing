using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IanGame.Editor
{
    [InitializeOnLoad]
    public static class WorldRecoveryBuildCheck
    {
        const string Key = "ian.worldRecovery.build";
        static bool running;

        static WorldRecoveryBuildCheck()
        {
            EditorApplication.update += Tick;
        }

        public static void Begin()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            SessionState.SetInt("ian.worldRecovery.previousTarget", (int)EditorUserBuildSettings.activeBuildTarget);
            SessionState.SetInt(Key, 1);
            Directory.CreateDirectory("Docs/WorldRecovery");
            File.WriteAllText("Docs/WorldRecovery/BUILD_STATUS.txt", "Queued Android release + iOS release export");
        }

        static void Tick()
        {
            int phase = SessionState.GetInt(Key, 0);
            if (
                phase == 0
                || running
                || EditorApplication.isCompiling
                || EditorApplication.isUpdating
                || Application.isPlaying
                || BuildPipeline.isBuildingPlayer
            )
            {
                return;
            }

            running = true;
            try
            {
                if (phase == 3)
                {
                    var target = (BuildTarget)
                        SessionState.GetInt("ian.worldRecovery.previousTarget", (int)BuildTarget.StandaloneOSX);
                    SessionState.SetInt(Key, 0);
                    File.WriteAllText(
                        "Docs/WorldRecovery/BUILD_STATUS.txt",
                        "Completed build attempts; restoring editor target " + target
                    );
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(target), target);
                    return;
                }

                var targetBuild = phase == 1 ? BuildTarget.Android : BuildTarget.iOS;
                var name = phase == 1 ? "ANDROID" : "IOS_EXPORT";
                SessionState.SetInt(Key, phase + 1);
                var folder = Path.GetFullPath("Builds/WorldRecovery/" + (phase == 1 ? "Android" : "iOS"));
                Directory.CreateDirectory(folder);
                File.WriteAllText(
                    "Docs/WorldRecovery/BUILD_STATUS.txt",
                    "Building " + name + " (release, QA excluded)"
                );
                try
                {
                    var report = BuildPipeline.BuildPlayer(
                        new BuildPlayerOptions
                        {
                            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                            target = targetBuild,
                            locationPathName = phase == 1 ? Path.Combine(folder, "IanGrowing.apk") : folder,
                            options = targetBuild == BuildTarget.Android ? BuildOptions.CompressWithLz4 : BuildOptions.None,
                        }
                    );
                    File.WriteAllText(
                        "Docs/WorldRecovery/" + name + "_BUILD.txt",
                        report.summary.result
                            + "\nErrors "
                            + report.summary.totalErrors
                            + "\nWarnings "
                            + report.summary.totalWarnings
                            + "\nDuration "
                            + report.summary.totalTime
                            + "\nOutput "
                            + report.summary.outputPath
                            + "\nActual device run: NOT TESTED\n"
                            + (phase == 2 ? "Native Xcode build: BLOCKED (only Command Line Tools installed)" : "")
                    );
                    var messages = report
                        .steps.SelectMany(s => s.messages)
                        .Where(m => m.type == LogType.Error || m.type == LogType.Warning)
                        .Select(m => m.type + ": " + m.content)
                        .Distinct();
                    File.WriteAllLines("Docs/WorldRecovery/" + name + "_MESSAGES.txt", messages);
                }
                catch (Exception e)
                {
                    File.WriteAllText("Docs/WorldRecovery/" + name + "_BUILD.txt", "Exception\n" + e);
                }
            }
            finally
            {
                running = false;
            }
        }
    }
}
