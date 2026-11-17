using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IanGame.Editor
{
    public static class ReleaseFoundationSetup
    {
        public static void PrepareQA()
        {
            SessionState.SetBool("ian.release.oldFast", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt("ian.release.oldFlags", (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = false;
        }

        public static void Finish()
        {
            var native = (BuildReport)
                typeof(BuildReport)
                    .GetMethod(
                        "GetLatestReport",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                    )
                    .Invoke(null, null);
            if (native != null && native.summary.platform == BuildTarget.iOS)
            {
                File.WriteAllText(
                    "Docs/Release/IOS_NATIVE_REPORT.json",
                    Newtonsoft.Json.JsonConvert.SerializeObject(
                        new
                        {
                            result = native.summary.result.ToString(),
                            errors = native.summary.totalErrors,
                            warnings = native.summary.totalWarnings,
                            output = native.summary.outputPath,
                            started = native.summary.buildStartedAt,
                            ended = native.summary.buildEndedAt,
                            bytes = native.summary.totalSize,
                            duration = native.summary.totalTime.ToString(),
                        },
                        Newtonsoft.Json.Formatting.Indented
                    )
                );
                File.WriteAllLines(
                    "Docs/Release/IOS_WARNINGS.txt",
                    native
                        .steps.SelectMany(s => s.messages)
                        .Where(m => m.type == LogType.Warning || m.type == LogType.Error)
                        .Select(m => m.type + ": " + m.content)
                );
            }

            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool("ian.release.oldFast", true);
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt("ian.release.oldFlags", 1);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/_Game/Scenes/Rooms/FamilyHome_IanBedroom.unity"
            );
            AssetDatabase.SaveAssets();
            File.WriteAllText(
                "Docs/Release/EDITOR_FINAL.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    new
                    {
                        scene = scene.name,
                        dirty = scene.isDirty,
                        playing = Application.isPlaying,
                        target = EditorUserBuildSettings.activeBuildTarget.ToString(),
                        fastPlay = EditorSettings.enterPlayModeOptionsEnabled,
                        buildPhase = SessionState.GetString("ian.release.platform.phase", ""),
                    }
                )
            );
        }

        [MenuItem("Ian/Release/Prepare Foundation Data")]
        public static void Setup()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            const string path = "Assets/Resources/IanRelease.asset";
            var config = AssetDatabase.LoadAssetAtPath<ReleaseConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ReleaseConfig>();
                AssetDatabase.CreateAsset(config, path);
                config.missionRuby = new[]
                {
                    new RubyRewardRule { id = "M010", amount = 2 },
                    new RubyRewardRule { id = "M020", amount = 3 },
                    new RubyRewardRule { id = "M030", amount = 3 },
                    new RubyRewardRule { id = "M040", amount = 3 },
                    new RubyRewardRule { id = "M050", amount = 3 },
                    new RubyRewardRule { id = "M060", amount = 5 },
                };
                config.achievementRuby = new[]
                {
                    new RubyRewardRule { id = "move", amount = 1 },
                    new RubyRewardRule { id = "item/collection10", amount = 1 },
                };
                config.products = new[]
                {
                    new RubyProductDefinition
                    {
                        internalId = "ruby_pack_small",
                        localizedName = "소형 루비 상품",
                        localizedDescription = "Store 연결 후 제공",
                        rubyAmount = 100,
                    },
                    new RubyProductDefinition
                    {
                        internalId = "ruby_pack_medium",
                        localizedName = "중형 루비 상품",
                        localizedDescription = "Store 연결 후 제공",
                        rubyAmount = 300,
                        displayOrder = 1,
                    },
                    new RubyProductDefinition
                    {
                        internalId = "ruby_pack_large",
                        localizedName = "대형 루비 상품",
                        localizedDescription = "Store 연결 후 제공",
                        rubyAmount = 1000,
                        displayOrder = 2,
                    },
                };
                config.documents = new[]
                {
                    ("terms", "이용약관"),
                    ("privacy", "개인정보처리방침"),
                    ("paid", "유료서비스 약관"),
                    ("refund", "청약철회 및 환불정책"),
                    ("probability", "확률형 아이템 안내"),
                    ("operation", "운영정책"),
                    ("licenses", "오픈소스 라이선스"),
                }
                    .Select(x => new LegalDocumentDefinition
                    {
                        documentId = x.Item1,
                        title = x.Item2,
                        locale = "ko",
                        version = "0.1.0-draft",
                        body = AssetDatabase.LoadAssetAtPath<TextAsset>(
                            "Assets/Resources/Legal/ko/" + x.Item1 + ".txt"
                        ),
                        required = x.Item1 == "terms" || x.Item1 == "privacy",
                        requireConsent = x.Item1 == "terms",
                    })
                    .ToArray();
            }

            config.uiTheme = AssetDatabase.LoadAssetAtPath<CollectionVisualTheme>(
                "Assets/_Game/Data/Phase30/CollectionTheme.asset"
            );
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Validate();
        }

        [MenuItem("Ian/Release/Validate Distribution")]
        public static void Validate()
        {
            Directory.CreateDirectory("Docs/Release");
            var config = ReleaseConfig.Current;
            var errors = ReleaseBuildValidation.FindBlockers(config);
            File.WriteAllText(
                "Docs/Release/VALIDATION.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    new
                    {
                        utc = DateTime.UtcNow,
                        status = errors.Length == 0 ? "READY" : "NOT READY",
                        blockers = errors,
                        probability = GachaProbabilityDisclosure.Validate(Resources.Load<ItemCatalog>("IanCollection"))
                            ?? "PASS",
                        development = EditorUserBuildSettings.development,
                        identifierAndroid = PlayerSettings.GetApplicationIdentifier(
                            UnityEditor.Build.NamedBuildTarget.Android
                        ),
                        identifierIOS = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS),
                        version = PlayerSettings.bundleVersion,
                        androidCode = PlayerSettings.Android.bundleVersionCode,
                        iosBuild = PlayerSettings.iOS.buildNumber,
                        scenes = EditorBuildSettings.scenes.Count(s => s.enabled),
                    },
                    Newtonsoft.Json.Formatting.Indented
                )
            );
        }
    }

    public sealed class ReleaseBuildValidation : IPreprocessBuildWithReport, IProcessSceneWithReport
    {
        public int callbackOrder => -100;

        static bool firstScene;

        public static bool IsProductionRelease(BuildReport report) =>
            BuildValidationContext.IsProductionReleaseBuild(report);

        public static string[] FindBlockers(ReleaseConfig c) =>
            new[] { BuildTarget.Android, BuildTarget.iOS }.SelectMany(t => FindBlockers(c, t)).Distinct().ToArray();

        // Target-scoped: Windows never reads Android/iOS identifiers or Store build numbers.
        public static string[] FindBlockers(ReleaseConfig c, BuildTarget target)
        {
            var errors = new System.Collections.Generic.List<string>();
            if (c == null)
            {
                return new[] { "Release config missing" };
            }

            if (!c.legalReviewed)
            {
                errors.Add("LEGAL REVIEW REQUIRED BEFORE RELEASE");
            }

            if (!c.privacyAuditApproved)
            {
                errors.Add("Final SDK/privacy audit approval missing");
            }

            foreach (var f in typeof(ReleaseConfig).GetFields().Where(f => f.FieldType == typeof(string)))
            {
                var value = (string)f.GetValue(c);
                if (string.IsNullOrWhiteSpace(value) || value.Contains("["))
                {
                    errors.Add("Config placeholder: " + f.Name);
                }
            }

            if (c.documents == null || c.documents.Length == 0)
            {
                errors.Add("Legal documents missing");
            }
            else
            {
                foreach (var d in c.documents)
                {
                    if (d == null)
                    {
                        errors.Add("Legal document definition missing");
                        continue;
                    }

                    if (d.body == null)
                    {
                        errors.Add("Missing legal body: " + d.documentId);
                    }
                    else if (
                        System.Text.RegularExpressions.Regex.IsMatch(
                            d.body.text,
                            @"\[(회사명|대표자명|사업자등록번호|통신판매업 신고번호|사업장 주소|주소|고객센터 이메일|고객센터 전화번호|전화번호|개인정보 보호책임자|개인정보 문의 이메일|시행일|문의 보유기간)\]"
                        )
                    )
                    {
                        errors.Add("Legal placeholder: " + d.documentId);
                    }

                    if (string.IsNullOrWhiteSpace(d.effectiveDate) || d.effectiveDate.Contains("["))
                    {
                        errors.Add("Effective date: " + d.documentId);
                    }
                }
            }

            if (target == BuildTarget.Android || target == BuildTarget.iOS)
            {
                var platform = target == BuildTarget.Android ? NamedBuildTarget.Android : NamedBuildTarget.iOS;
                string id = PlayerSettings.GetApplicationIdentifier(platform);
                if (
                    string.IsNullOrWhiteSpace(id)
                    || id.Contains("UnityTechnologies")
                    || id.Contains("Unity-Technologies")
                    || id.Contains("template")
                )
                {
                    errors.Add("Production Bundle Identifier required: " + platform.TargetName);
                }
            }

            if (PlayerSettings.companyName == "DefaultCompany" || string.IsNullOrWhiteSpace(PlayerSettings.companyName))
            {
                errors.Add("Production Company Name required");
            }

            if (string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion))
            {
                errors.Add("Version missing");
            }

            if (target == BuildTarget.Android && PlayerSettings.Android.bundleVersionCode < 1)
            {
                errors.Add("Android versionCode missing");
            }

            if (
                target == BuildTarget.iOS
                && (!int.TryParse(PlayerSettings.iOS.buildNumber, out var iosNumber) || iosNumber < 1)
            )
            {
                errors.Add("iOS CFBundleVersion missing");
            }

            foreach (string url in new[] { c.TermsUrl, c.PrivacyPolicyUrl, c.SupportUrl, c.RefundHelpUrl })
            {
                if (
                    !Uri.TryCreate(url, UriKind.Absolute, out var parsed)
                    || parsed.Scheme != "https"
                    || parsed.Host.Equals("example.com", StringComparison.OrdinalIgnoreCase)
                    || parsed.Host.EndsWith(".example.com", StringComparison.OrdinalIgnoreCase)
                )
                {
                    errors.Add("Production HTTPS policy/support URL required");
                }
            }

            if (
                c.products != null
                && target == BuildTarget.Android
                && c.products.Any(p => p != null && p.enabled && string.IsNullOrWhiteSpace(p.googleProductId))
            )
            {
                errors.Add("Enabled Google product has placeholder ID");
            }

            if (
                c.products != null
                && target == BuildTarget.iOS
                && c.products.Any(p => p != null && p.enabled && string.IsNullOrWhiteSpace(p.appleProductId))
            )
            {
                errors.Add("Enabled Apple product has placeholder ID");
            }

            return errors.Distinct().ToArray();
        }

        public static string[] CommonIntegrity(string[] scenes, ItemCatalog catalog, ReleaseConfig config)
        {
            var errors = new System.Collections.Generic.List<string>();
            if (scenes == null || scenes.Length == 0 || scenes[0] != "Assets/_Game/Scenes/Bootstrap.unity")
            {
                errors.Add("Required Startup Scene Bootstrap must be first");
            }

            if (scenes != null)
            {
                foreach (string path in scenes)
                {
                    if (!File.Exists(path))
                    {
                        errors.Add("Missing required Scene: " + path);
                    }
                }
            }

            try
            {
                var probability = GachaProbabilityDisclosure.Validate(catalog);
                if (probability != null)
                {
                    errors.Add(probability);
                }
            }
            catch (Exception e)
            {
                errors.Add("Invalid gacha probability data: " + e.Message);
            }

            if (
                config != null
                && (
                    config.missionRuby == null
                    || config.achievementRuby == null
                    || config.missionRuby.Any(r => r == null || r.amount < 0 || r.amount > 5)
                    || config.achievementRuby.Any(r => r == null || r.amount < 0 || r.amount > 3)
                    || config.missionRuby.Select(r => r.id).Distinct().Count() != config.missionRuby.Length
                    || config.achievementRuby.Select(r => r.id).Distinct().Count() != config.achievementRuby.Length
                )
            )
            {
                errors.Add("Ruby reward configuration invalid");
            }

            return errors.ToArray();
        }

        public static BuildValidationIssue[] Evaluate(
            BuildValidationContext context,
            ReleaseConfig config,
            string[] scenes,
            ItemCatalog catalog
        )
        {
            var result = new System.Collections.Generic.List<BuildValidationIssue>();
            result.AddRange(
                CommonIntegrity(scenes, catalog, config)
                    .Select(e => new BuildValidationIssue(BuildValidationSeverity.Fatal, e))
            );
            var level = context.IsProductionRelease ? BuildValidationSeverity.Fatal : BuildValidationSeverity.Warning;
            result.AddRange(FindBlockers(config, context.target).Select(e => new BuildValidationIssue(level, e)));
            if (context.IsProductionRelease && context.Development)
            {
                result.Add(
                    new BuildValidationIssue(BuildValidationSeverity.Fatal, "Production Development Build forbidden")
                );
            }

            if (context.IsProductionRelease && (context.options & BuildOptions.AllowDebugging) != 0)
            {
                result.Add(
                    new BuildValidationIssue(BuildValidationSeverity.Fatal, "Production script debugging forbidden")
                );
            }

            if (
                context.IsProductionRelease
                && context.target == BuildTarget.Android
                && (
                    !PlayerSettings.Android.useCustomKeystore
                    || string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName)
                    || !File.Exists(PlayerSettings.Android.keystoreName)
                    || string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName)
                )
            )
            {
                result.Add(
                    new BuildValidationIssue(
                        BuildValidationSeverity.Fatal,
                        "ANDROID RELEASE SIGNING CREDENTIALS REQUIRED"
                    )
                );
            }

            return result.ToArray();
        }

        public static void Enforce(BuildValidationContext context, BuildValidationIssue[] issues)
        {
            var fatal = issues.Where(e => e.severity >= BuildValidationSeverity.Error).Select(e => e.message).ToArray();
            if (fatal.Length > 0)
            {
                throw new BuildFailedException(string.Join("\n", fatal));
            }

            var warnings = issues
                .Where(e => e.severity == BuildValidationSeverity.Warning)
                .Select(e => e.message)
                .Distinct()
                .ToArray();
            if (warnings.Length > 0)
            {
                Debug.LogWarning(
                    "[ReleaseCompliance] IAN GROWING — INTERNAL DEVELOPMENT BUILD\nTarget: "
                        + context.target
                        + " / Channel: "
                        + context.channel
                        + "\n[LEGAL WARNING] Production legal/privacy validation is relaxed for this non-production build. Do not distribute publicly.\n"
                        + string.Join("\n", warnings)
                );
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            firstScene = true;
            var context = BuildValidationContext.ForReport(report);
            var issues = Evaluate(
                context,
                ReleaseConfig.Current,
                EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                Resources.Load<ItemCatalog>("IanCollection")
            );
            Directory.CreateDirectory("Docs/WindowsBuild");
            File.WriteAllText(
                "Docs/WindowsBuild/LAST_VALIDATION.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    new
                    {
                        target = context.target.ToString(),
                        channel = context.channel.ToString(),
                        production = context.IsProductionRelease,
                        development = context.Development,
                        context.internalBuild,
                        context.storeDistribution,
                        issues = issues.Select(i => new { severity = i.severity.ToString(), i.message }),
                    },
                    Newtonsoft.Json.Formatting.Indented
                )
            );
            Enforce(context, issues);
        }

        // Inspect actual processed scenes as well as the configured list: custom CLI scene lists cannot bypass startup checks.
        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
        {
            if (report == null)
            {
                return;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                    {
                        throw new BuildFailedException("Missing script in build Scene: " + scene.path + " / " + t.name);
                    }
                }
            }

            if (!firstScene)
            {
                return;
            }

            firstScene = false;
            if (scene.path != "Assets/_Game/Scenes/Bootstrap.unity")
            {
                throw new BuildFailedException("Required Startup Scene Bootstrap must be first in actual build");
            }

            var contexts = scene
                .GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<GameContext>(true))
                .ToArray();
            if (contexts.Length != 1 || contexts[0].game == null || contexts[0].transitions == null)
            {
                throw new BuildFailedException(
                    "Required GameContext/GameManager/SceneTransitionService missing from Bootstrap"
                );
            }

            var g = contexts[0].game;
            if (g.balance == null || g.stats == null || g.clock == null || g.lifeEvents == null || g.saves == null)
            {
                throw new BuildFailedException("Broken Bootstrap reference / Save initialization dependency missing");
            }
        }
    }

    [InitializeOnLoad]
    public static class ReleasePlatformBuild
    {
        const string Key = "ian.release.platform";
        static double next;

        static ReleasePlatformBuild()
        {
            EditorApplication.update += Tick;
        }

        [MenuItem("Ian/Build/Windows Development")]
        public static void WindowsDevelopment() => Windows(BuildReleaseChannel.Development);

        [MenuItem("Ian/Build/Windows Internal QA")]
        public static void WindowsInternal() => Windows(BuildReleaseChannel.InternalQA);

        public static void Windows(BuildReleaseChannel channel)
        {
            SessionState.SetInt(Key + ".channel", (int)channel);
            Begin(BuildTarget.StandaloneWindows64);
        }

        [MenuItem("Ian/Release/Build Signed Android AAB")]
        public static void Android() => Begin(BuildTarget.Android);

        [MenuItem("Ian/Release/Export iOS Review Xcode Project")]
        public static void IOS() => Begin(BuildTarget.iOS);

        public static void Begin(BuildTarget target)
        {
            if (
                Application.isPlaying
                || BuildPipeline.isBuildingPlayer
                || SessionState.GetString(Key + ".phase", "") != ""
            )
            {
                throw new Exception("Stop Play and wait for the current build");
            }

            Directory.CreateDirectory("Docs/Release");
            if (!BuildValidationContext.IsWindows(target))
            {
                ReleaseFoundationSetup.Validate();
            }

            if (target == BuildTarget.Android)
            {
                string path = Environment.GetEnvironmentVariable("IAN_ANDROID_KEYSTORE");
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = PlayerSettings.Android.keystoreName;
                }

                string alias = Environment.GetEnvironmentVariable("IAN_ANDROID_KEY_ALIAS");
                if (string.IsNullOrWhiteSpace(alias))
                {
                    alias = PlayerSettings.Android.keyaliasName;
                }

                if (
                    string.IsNullOrWhiteSpace(path)
                    || !File.Exists(path)
                    || string.IsNullOrWhiteSpace(alias)
                    || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IAN_ANDROID_STORE_PASSWORD"))
                        && string.IsNullOrEmpty(PlayerSettings.Android.keystorePass)
                    || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IAN_ANDROID_KEY_PASSWORD"))
                        && string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass)
                )
                {
                    File.WriteAllText(
                        "Docs/Release/ANDROID_RESULT.json",
                        "{\"status\":\"BLOCKED\",\"reason\":\"ANDROID RELEASE SIGNING CREDENTIALS REQUIRED\",\"aabGenerated\":false,\"newKeyCreated\":false}"
                    );
                    return;
                }

                if (ReleaseBuildValidation.FindBlockers(ReleaseConfig.Current, target).Length > 0)
                {
                    File.WriteAllText(
                        "Docs/Release/ANDROID_RESULT.json",
                        "{\"status\":\"BLOCKED\",\"reason\":\"Release validation failed; see VALIDATION.json\",\"aabGenerated\":false}"
                    );
                    return;
                }
            }

            SessionState.SetInt(Key + ".previous", (int)EditorUserBuildSettings.activeBuildTarget);
            SessionState.SetString(Key, target.ToString());
            SessionState.SetString(Key + ".phase", "switch");
            if (
                !EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildPipeline.GetBuildTargetGroup(target), target)
            )
            {
                SessionState.SetString(Key + ".phase", "");
                File.WriteAllText(
                    "Docs/Release/IOS_RESULT.json",
                    "{\"status\":\"BLOCKED\",\"reason\":\"Unity target switch rejected\",\"xcodeProjectGenerated\":false}"
                );
            }
        }

        static void Tick()
        {
            if (
                EditorApplication.timeSinceStartup < next
                || EditorApplication.isCompiling
                || EditorApplication.isUpdating
                || Application.isPlaying
            )
            {
                return;
            }

            next = EditorApplication.timeSinceStartup + 1;
            string phase = SessionState.GetString(Key + ".phase", "");
            if (phase == "restore")
            {
                if (EditorUserBuildSettings.activeBuildTarget == (BuildTarget)SessionState.GetInt(Key + ".previous", 0))
                {
                    SessionState.SetString(Key + ".phase", "");
                    SessionState.SetString(Key, "");
                }

                return;
            }

            if (phase != "switch")
            {
                return;
            }

            var target = (BuildTarget)Enum.Parse(typeof(BuildTarget), SessionState.GetString(Key, ""));
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                return;
            }

            SessionState.SetString(Key + ".phase", "building");
            bool windows = BuildValidationContext.IsWindows(target);
            var channel =
                windows
                    ? (BuildReleaseChannel)SessionState.GetInt(Key + ".channel", (int)BuildReleaseChannel.WindowsTest)
                : target == BuildTarget.iOS ? BuildReleaseChannel.InternalQA
                : BuildReleaseChannel.Production;
            string output =
                windows
                    ? (
                        channel == BuildReleaseChannel.Development
                            ? "Builds/Windows/Development/IanGrowing.exe"
                            : "Builds/Windows/IanGrowing.exe"
                    )
                : target == BuildTarget.Android
                    ? "Builds/Android/IanGrowing-" + PlayerSettings.bundleVersion + "-release.aab"
                : "Builds/iOS/Xcode";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            bool oldBundle = EditorUserBuildSettings.buildAppBundle,
                oldDev = EditorUserBuildSettings.development,
                oldDebug = EditorUserBuildSettings.allowDebugging;
            bool oldKey = PlayerSettings.Android.useCustomKeystore;
            string oldPath = PlayerSettings.Android.keystoreName,
                oldAlias = PlayerSettings.Android.keyaliasName,
                oldStorePass = PlayerSettings.Android.keystorePass,
                oldKeyPass = PlayerSettings.Android.keyaliasPass;
            try
            {
                EditorUserBuildSettings.development = windows && channel == BuildReleaseChannel.Development;
                EditorUserBuildSettings.allowDebugging = false;
                EditorUserBuildSettings.buildAppBundle = target == BuildTarget.Android;
                if (target == BuildTarget.Android)
                {
                    PlayerSettings.Android.useCustomKeystore = true;
                    PlayerSettings.Android.keystoreName =
                        Environment.GetEnvironmentVariable("IAN_ANDROID_KEYSTORE") ?? oldPath;
                    PlayerSettings.Android.keyaliasName =
                        Environment.GetEnvironmentVariable("IAN_ANDROID_KEY_ALIAS") ?? oldAlias;
                    PlayerSettings.Android.keystorePass =
                        Environment.GetEnvironmentVariable("IAN_ANDROID_STORE_PASSWORD") ?? oldStorePass;
                    PlayerSettings.Android.keyaliasPass =
                        Environment.GetEnvironmentVariable("IAN_ANDROID_KEY_PASSWORD") ?? oldKeyPass;
                }

                var options =
                    windows && channel == BuildReleaseChannel.Development
                        ? BuildOptions.Development
                        : BuildOptions.None;
                using (
                    var contextScope = new BuildValidationContext.Scope(
                        new BuildValidationContext
                        {
                            target = target,
                            channel = channel,
                            internalBuild = channel != BuildReleaseChannel.Production,
                            storeDistribution = target == BuildTarget.Android,
                            reviewExport = target == BuildTarget.iOS,
                        }
                    )
                )
                {
                    var report = BuildPipeline.BuildPlayer(
                        new BuildPlayerOptions
                        {
                            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                            locationPathName = output,
                            target = target,
                            options = options,
                        }
                    );
                    Directory.CreateDirectory(windows ? "Docs/WindowsBuild" : "Docs/Release");
                    File.WriteAllText(
                        windows
                            ? "Docs/WindowsBuild/" + channel + "_RESULT.json"
                            : "Docs/Release/" + (target == BuildTarget.Android ? "ANDROID" : "IOS") + "_RESULT.json",
                        Newtonsoft.Json.JsonConvert.SerializeObject(
                            new
                            {
                                status = report.summary.result.ToString(),
                                errors = report.summary.totalErrors,
                                warnings = report.summary.totalWarnings,
                                bytes = report.summary.totalSize,
                                output,
                                target = target.ToString(),
                                channel = channel.ToString(),
                                development = (options & BuildOptions.Development) != 0,
                                scriptDebugging = false,
                                started = report.summary.buildStartedAt,
                                ended = report.summary.buildEndedAt,
                                exeExists = windows && File.Exists(output),
                                distributionApproved = target == BuildTarget.Android,
                                projectExists = File.Exists(output + "/Unity-iPhone.xcodeproj/project.pbxproj"),
                                messages = report
                                    .steps.SelectMany(s => s.messages)
                                    .Where(m => m.type == LogType.Error)
                                    .Select(m => m.content)
                                    .Take(12)
                                    .ToArray(),
                            },
                            Newtonsoft.Json.Formatting.Indented
                        )
                    );
                    if (windows)
                    {
                        File.WriteAllLines(
                            "Docs/WindowsBuild/" + channel + "_WARNINGS.txt",
                            report
                                .steps.SelectMany(step => step.messages)
                                .Where(m => m.type == LogType.Warning || m.type == LogType.Error)
                                .Select(m => m.type + ": " + m.content)
                        );
                    }
                }
            }
            catch (Exception e)
            {
                File.WriteAllText(
                    windows
                        ? "Docs/WindowsBuild/" + channel + "_RESULT.json"
                        : "Docs/Release/" + (target == BuildTarget.Android ? "ANDROID" : "IOS") + "_RESULT.json",
                    Newtonsoft.Json.JsonConvert.SerializeObject(
                        new
                        {
                            status = "BLOCKED",
                            error = e.Message,
                            output,
                        },
                        Newtonsoft.Json.Formatting.Indented
                    )
                );
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = oldBundle;
                EditorUserBuildSettings.development = oldDev;
                EditorUserBuildSettings.allowDebugging = oldDebug;
                if (target == BuildTarget.Android)
                {
                    PlayerSettings.Android.useCustomKeystore = oldKey;
                    PlayerSettings.Android.keystoreName = oldPath;
                    PlayerSettings.Android.keyaliasName = oldAlias;
                    PlayerSettings.Android.keystorePass = oldStorePass;
                    PlayerSettings.Android.keyaliasPass = oldKeyPass;
                }

                SessionState.SetString(Key + ".phase", "restore");
                var previous = (BuildTarget)SessionState.GetInt(Key + ".previous", (int)BuildTarget.StandaloneOSX);
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(
                    BuildPipeline.GetBuildTargetGroup(previous),
                    previous
                );
            }
        }
    }
}
