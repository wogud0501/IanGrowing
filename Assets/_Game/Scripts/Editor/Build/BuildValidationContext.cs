using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace IanGame.Editor
{
    public enum BuildReleaseChannel
    {
        Development,
        InternalQA,
        WindowsTest,
        Production,
    }

    public enum BuildValidationSeverity
    {
        Info,
        Warning,
        Error,
        Fatal,
    }

    public sealed class BuildValidationContext
    {
        public BuildTarget target;
        public BuildOptions options;
        public BuildReleaseChannel channel;
        public bool internalBuild;
        public bool storeDistribution;
        public bool reviewExport;
        public bool Development => (options & BuildOptions.Development) != 0;
        public bool Windows => IsWindows(target);
        public bool IsProductionRelease =>
            channel == BuildReleaseChannel.Production
            || storeDistribution
            || IsMobile(target)
                && !Development
                && !(
                    target == BuildTarget.iOS
                    && reviewExport
                    && internalBuild
                    && channel == BuildReleaseChannel.InternalQA
                );

        public static bool IsWindows(BuildTarget t) =>
            t == BuildTarget.StandaloneWindows || t == BuildTarget.StandaloneWindows64;

        public static bool IsMobile(BuildTarget t) => t == BuildTarget.Android || t == BuildTarget.iOS;

        static BuildValidationContext scoped;

        public sealed class Scope : IDisposable
        {
            readonly BuildValidationContext previous;

            public Scope(BuildValidationContext context)
            {
                previous = scoped;
                scoped = context;
            }

            public void Dispose()
            {
                scoped = previous;
            }
        }

        public static BuildValidationContext ForReport(BuildReport report) =>
            Resolve(report.summary.platform, report.summary.options);

        public static bool IsProductionReleaseBuild(BuildReport report) => ForReport(report).IsProductionRelease;

        public static BuildValidationContext Resolve(BuildTarget target, BuildOptions options)
        {
            string raw = Environment.GetEnvironmentVariable("IAN_BUILD_CHANNEL");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-releaseChannel")
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new BuildFailedException("-releaseChannel requires a value");
                    }

                    raw = args[i + 1];
                }
            }

            BuildReleaseChannel? requested = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                if (
                    !Enum.TryParse(raw, true, out BuildReleaseChannel parsed)
                    || !Enum.IsDefined(typeof(BuildReleaseChannel), parsed)
                )
                {
                    throw new BuildFailedException("Unknown IAN_BUILD_CHANNEL/-releaseChannel: " + raw);
                }

                requested = parsed;
            }

            var active = scoped != null && scoped.target == target ? scoped : null;
            bool dev = (options & BuildOptions.Development) != 0;
            var channel =
                requested
                ?? active?.channel
                ?? (
                    dev ? BuildReleaseChannel.Development
                    : IsWindows(target) ? BuildReleaseChannel.WindowsTest
                    : BuildReleaseChannel.Production
                );
            // Explicit Production always wins, even over a development preset or internal scope.
            if (active?.channel == BuildReleaseChannel.Production)
            {
                channel = BuildReleaseChannel.Production;
            }

            return new BuildValidationContext
            {
                target = target,
                options = options,
                channel = channel,
                internalBuild = active?.internalBuild ?? channel != BuildReleaseChannel.Production,
                storeDistribution = active?.storeDistribution ?? IsMobile(target) && !dev,
                reviewExport = active?.reviewExport ?? false,
            };
        }
    }

    public sealed class BuildValidationIssue
    {
        public BuildValidationSeverity severity;
        public string message;

        public BuildValidationIssue(BuildValidationSeverity level, string value)
        {
            severity = level;
            message = value;
        }
    }
}
