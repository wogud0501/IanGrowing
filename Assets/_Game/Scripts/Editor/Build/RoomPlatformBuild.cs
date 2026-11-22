using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace IanGame.Editor
{
    [InitializeOnLoad]
    public static class RoomPlatformBuild
    {
        const string Key = "ian.roomBuild.phase";
        static double next;

        static RoomPlatformBuild()
        {
            EditorApplication.update += Tick;
        }

        public static void Android()
        {
            if (EditorApplication.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            if (SessionState.GetString(Key, "") != "")
            {
                throw new Exception("Build already pending");
            }

            SessionState.SetInt("ian.roomBuild.previous", (int)EditorUserBuildSettings.activeBuildTarget);
            SessionState.SetString(Key, "switch");
            if (!EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android))
            {
                SessionState.SetString(Key, "");
                throw new Exception("Android target switch rejected");
            }
        }

        static void Tick()
        {
            if (
                EditorApplication.timeSinceStartup < next
                || EditorApplication.isCompiling
                || EditorApplication.isUpdating
                || EditorApplication.isPlaying
            )
            {
                return;
            }

            next = EditorApplication.timeSinceStartup + 1;
            var phase = SessionState.GetString(Key, "");
            if (phase == "switch" && EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                SessionState.SetString(Key, "building");
                Directory.CreateDirectory("Builds/SceneArchitecture");
                try
                {
                    var result = BuildPipeline.BuildPlayer(
                        new BuildPlayerOptions
                        {
                            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                            locationPathName = "Builds/SceneArchitecture/Ian-Rooms-Development.apk",
                            target = BuildTarget.Android,
                            options = BuildOptions.Development | BuildOptions.CompressWithLz4,
                        }
                    );
                    File.WriteAllText(
                        "Docs/SceneArchitecture/ANDROID_BUILD.txt",
                        $"UTC {DateTime.UtcNow:O}\nResult {result.summary.result}\nErrors {result.summary.totalErrors}\nWarnings {result.summary.totalWarnings}\nBytes {result.summary.totalSize}\nDuration {result.summary.totalTime}\nScenes {EditorBuildSettings.scenes.Count(s => s.enabled)}\n"
                    );
                }
                catch (Exception e)
                {
                    File.WriteAllText("Docs/SceneArchitecture/ANDROID_BUILD.txt", "FAILED\n" + e);
                }

                Restore();
            }
            else if (
                phase == "restore"
                && EditorUserBuildSettings.activeBuildTarget
                    == (BuildTarget)SessionState.GetInt("ian.roomBuild.previous", (int)BuildTarget.StandaloneOSX)
            )
            {
                SessionState.SetString(Key, "");
                File.AppendAllText(
                    "Docs/SceneArchitecture/ANDROID_BUILD.txt",
                    "Editor target restored: " + EditorUserBuildSettings.activeBuildTarget + "\n"
                );
            }
        }

        static void Restore()
        {
            SessionState.SetString(Key, "restore");
            var target = (BuildTarget)SessionState.GetInt("ian.roomBuild.previous", (int)BuildTarget.StandaloneOSX);
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildPipeline.GetBuildTargetGroup(target), target);
        }
    }
}
