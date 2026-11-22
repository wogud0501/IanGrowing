using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IanGame.Editor
{
    public static class Phase24PlayerBuild
    {
        [Serializable]
        class Result
        {
            public string platform;
            public string status;
            public string output;
            public string error;
            public int errors;
            public int warnings;
            public double seconds;
            public ulong bytes;
        }

        public static void Begin(BuildTarget target)
        {
            if (EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer)
            {
                throw new Exception("Stop Play Mode and wait for current build");
            }

            string report = "Docs/Phase24/BUILD_" + target + ".json";
            File.WriteAllText(report, "{\"status\":\"Running\"}");
            EditorApplication.CallbackFunction run = null;
            run = () =>
            {
                EditorApplication.update -= run;
                string folder =
                    "E:/Projects/Build/Phase24/" + target + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                Directory.CreateDirectory(folder);
                var result = new Result { platform = target.ToString(), output = folder };
                try
                {
                    var options = new BuildPlayerOptions
                    {
                        scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                        target = target,
                        locationPathName =
                            folder + (target == BuildTarget.Android ? "/IanGrowing.apk" : "/IanGrowing.exe"),
                        options = target == BuildTarget.Android
                            ? BuildOptions.Development | BuildOptions.CompressWithLz4
                            : BuildOptions.Development,
                    };
                    var built = BuildPipeline.BuildPlayer(options);
                    result.status = built.summary.result.ToString();
                    result.errors = (int)built.summary.totalErrors;
                    result.warnings = (int)built.summary.totalWarnings;
                    result.seconds = built.summary.totalTime.TotalSeconds;
                    result.bytes = built.summary.totalSize;
                }
                catch (Exception e)
                {
                    result.status = "Exception";
                    result.error = e.ToString();
                }

                File.WriteAllText(report, JsonUtility.ToJson(result, true));
            };
            EditorApplication.update += run;
        }
    }
}
