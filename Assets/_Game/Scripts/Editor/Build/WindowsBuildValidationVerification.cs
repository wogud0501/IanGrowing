using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace IanGame.Editor
{
    public static class WindowsBuildValidationVerification
    {
        public static void Finish()
        {
            if (
                Application.isPlaying
                || EditorApplication.isCompiling
                || SessionState.GetString("ian.release.platform.phase", "") != ""
            )
            {
                throw new Exception("Wait for build restoration");
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            File.WriteAllText(
                "Docs/WindowsBuild/EDITOR_FINAL.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    new
                    {
                        scene = scene.name,
                        dirty = scene.isDirty,
                        playing = Application.isPlaying,
                        target = EditorUserBuildSettings.activeBuildTarget.ToString(),
                        development = EditorUserBuildSettings.development,
                        debugging = EditorUserBuildSettings.allowDebugging,
                        buildPending = SessionState.GetString("ian.release.platform.phase", ""),
                    }
                )
            );
        }

        public static void Run()
        {
            var passed = new List<string>();
            void Check(bool value, string text)
            {
                if (!value)
                {
                    throw new Exception(text);
                }

                passed.Add(text);
            }

            var catalog = Resources.Load<ItemCatalog>("IanCollection");
            var config = ReleaseConfig.Current;
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var development = new BuildValidationContext
            {
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
                channel = BuildReleaseChannel.Development,
                internalBuild = true,
            };
            var issues = ReleaseBuildValidation.Evaluate(development, config, scenes, catalog);
            Check(
                !development.IsProductionRelease && issues.All(i => i.severity < BuildValidationSeverity.Error),
                "A: Windows Development permits actual legal placeholders, missing URLs and unapproved audit"
            );
            Check(
                issues.Any(i => i.severity == BuildValidationSeverity.Warning && i.message.Contains("LEGAL REVIEW")),
                "A: legal review remains an explicit warning"
            );
            Check(
                !issues.Any(i =>
                    i.message.Contains("Bundle Identifier")
                    || i.message.Contains("versionCode")
                    || i.message.Contains("CFBundleVersion")
                ),
                "B: Windows has no Android/iOS identifier or Store build-number validation"
            );
            var internalQa = new BuildValidationContext
            {
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
                channel = BuildReleaseChannel.InternalQA,
                internalBuild = true,
            };
            Check(
                !internalQa.IsProductionRelease
                    && ReleaseBuildValidation
                        .Evaluate(internalQa, config, scenes, catalog)
                        .All(i => i.severity < BuildValidationSeverity.Error),
                "Windows InternalQA permits release-style build with legal warnings"
            );
            bool Throws(BuildValidationContext ctx, string[] buildScenes, ItemCatalog data, string fragment)
            {
                try
                {
                    ReleaseBuildValidation.Enforce(
                        ctx,
                        ReleaseBuildValidation.Evaluate(ctx, config, buildScenes, data)
                    );
                    return false;
                }
                catch (BuildFailedException e)
                {
                    return e.Message.Contains(fragment);
                }
            }

            var android = new BuildValidationContext
            {
                target = BuildTarget.Android,
                channel = BuildReleaseChannel.Production,
                storeDistribution = true,
            };
            Check(
                Throws(android, scenes, catalog, "LEGAL REVIEW REQUIRED"),
                "C: Android Production fails with actual legal placeholders"
            );
            var ios = new BuildValidationContext
            {
                target = BuildTarget.iOS,
                channel = BuildReleaseChannel.Production,
                storeDistribution = true,
            };
            Check(
                Throws(ios, scenes, catalog, "HTTPS policy/support URL"),
                "D: iOS Production fails with actual missing Privacy URL"
            );
            Check(
                Throws(development, scenes.Skip(1).ToArray(), catalog, "Startup Scene"),
                "E: Windows Development fails without Startup Scene"
            );
            foreach (var target in new[] { BuildTarget.Android, BuildTarget.iOS })
            {
                var misleading = new BuildValidationContext
                {
                    target = target,
                    options = BuildOptions.None,
                    channel = BuildReleaseChannel.WindowsTest,
                    internalBuild = true,
                };
                Check(
                    misleading.IsProductionRelease,
                    "Mobile release cannot inherit WindowsTest relaxation: " + target
                );
            }

            var explicitProduction = new BuildValidationContext
            {
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
                channel = BuildReleaseChannel.Production,
                internalBuild = true,
            };
            Check(
                explicitProduction.IsProductionRelease && Throws(explicitProduction, scenes, catalog, "LEGAL REVIEW"),
                "Explicit Production overrides Windows development/internal flags"
            );
            var store = new BuildValidationContext
            {
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
                channel = BuildReleaseChannel.InternalQA,
                storeDistribution = true,
            };
            Check(store.IsProductionRelease, "Store distribution intent overrides internal flags");
            bool old = SessionState.GetBool("ian.release.exportReview", false);
            try
            {
                SessionState.SetBool("ian.release.exportReview", true);
                Check(
                    BuildValidationContext.Resolve(BuildTarget.iOS, BuildOptions.None).IsProductionRelease,
                    "Legacy review toggle cannot bypass mobile Production"
                );
            }
            finally
            {
                SessionState.SetBool("ian.release.exportReview", old);
            }

            Check(
                BuildValidationContext.Resolve(BuildTarget.StandaloneWindows64, BuildOptions.None).channel
                    == BuildReleaseChannel.WindowsTest,
                "Default non-development Windows build is local WindowsTest"
            );
            var bad = UnityEngine.Object.Instantiate(catalog);
            try
            {
                bad.pools[0].availableItems[0].weight = 0;
                Check(
                    Throws(development, scenes, bad, "추첨 Pool"),
                    "Invalid gacha data remains fatal in Windows Development"
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bad);
            }

            Directory.CreateDirectory("Docs/WindowsBuild");
            File.WriteAllText(
                "Docs/WindowsBuild/POLICY_TESTS.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    new
                    {
                        status = "PASS",
                        passed,
                        scope = "Actual shared validator execution; mobile native compilation was not repeated. A/B native Windows builds recorded separately.",
                    },
                    Newtonsoft.Json.Formatting.Indented
                )
            );
        }
    }
}
