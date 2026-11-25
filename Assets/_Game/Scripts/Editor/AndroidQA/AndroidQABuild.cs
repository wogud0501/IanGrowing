using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace IanGame.Editor
{
    public static class AndroidQABuild
    {
        public const string Apk = "Builds/AndroidQA/IanGrowing.apk";
        public const string Status = "Library/AndroidQA-build.txt";
        static bool queued;

        [MenuItem("Tools/IanGrowing/Build Android QA APK")]
        public static void Queue()
        {
            if (queued || BuildPipeline.isBuildingPlayer || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling || EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                throw new InvalidOperationException("Select Android and wait for an idle Editor.");
            }
            queued = true;
            File.WriteAllText(Status, "QUEUED");
            EditorApplication.delayCall += Build;
        }

        static void Build()
        {
            try
            {
                if (EditorUserBuildSettings.buildAppBundle || EditorUserBuildSettings.exportAsGoogleAndroidProject)
                {
                    throw new InvalidOperationException("Choose APK in the Android build profile.");
                }
                Directory.CreateDirectory(Path.GetDirectoryName(Apk));
                File.WriteAllText(Status, "BUILDING");
                // Use the same enabled scene list and Development options as RoomPlatformBuild.
                var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    target = BuildTarget.Android,
                    scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                    locationPathName = Apk,
                    options = BuildOptions.Development | BuildOptions.CompressWithLz4 | BuildOptions.ConnectWithProfiler
                });
                File.WriteAllText(Status, result.summary.result + " · " + result.summary.totalErrors + " errors · "
                    + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            }
            catch (Exception error)
            {
                File.WriteAllText(Status, "FAILED · " + error.GetType().Name + " · " + error.Message);
                throw;
            }
            finally
            {
                queued = false;
            }
        }
    }
}
