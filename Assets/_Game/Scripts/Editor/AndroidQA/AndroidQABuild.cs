using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;

namespace IanGame.Editor
{
    public static class AndroidQABuild
    {
        public const string Apk = "Builds/AndroidQA/IanGrowing.apk";
        public const string Status = "Library/AndroidQA-build.txt";
        const string Pending = "IanGrowing.AndroidQA.Pending";

        [InitializeOnLoadMethod]
        static void ResumeQueue()
        {
            if (SessionState.GetBool(Pending, false))
            {
                EditorApplication.update -= BuildWhenReady;
                EditorApplication.update += BuildWhenReady;
            }
        }

        [MenuItem("Tools/IanGrowing/Build Android QA APK")]
        public static void Queue()
        {
            if (SessionState.GetBool(Pending, false) || BuildPipeline.isBuildingPlayer || EditorApplication.isPlayingOrWillChangePlaymode
                || EditorUtility.scriptCompilationFailed || EditorApplication.isCompiling || EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                throw new InvalidOperationException("Select Android and wait for an idle Editor.");
            }
            SessionState.SetBool(Pending, true);
            File.WriteAllText(Status, "QUEUED");
            ResumeQueue();
        }

        static void BuildWhenReady()
        {
            if (!SessionState.GetBool(Pending, false))
            {
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }
            EditorApplication.update -= BuildWhenReady;
            SessionState.SetBool(Pending, false);
            try
            {
                if (EditorUserBuildSettings.buildAppBundle || EditorUserBuildSettings.exportAsGoogleAndroidProject)
                {
                    throw new InvalidOperationException("Choose APK in the Android build profile.");
                }
                Directory.CreateDirectory(Path.GetDirectoryName(Apk));
                File.WriteAllText(Status, "BUILDING");
                var result = RoomPlatformBuild.BuildDevelopmentApk(Apk, BuildOptions.ConnectWithProfiler);
                File.WriteAllText(Status, result.summary.result + " · " + result.summary.totalErrors + " errors · "
                    + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            }
            catch (Exception error)
            {
                File.WriteAllText(Status, "FAILED · " + error.GetType().Name + " · " + error.Message);
                throw;
            }
        }
    }
}
