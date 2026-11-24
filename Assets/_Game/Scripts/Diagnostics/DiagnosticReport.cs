using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace IanGame
{
    // Never copy a raw console line, frame argument, user path or save object to disk.
    [Serializable]
    internal sealed class DiagnosticReport
    {
        internal const string Missing = "Unavailable";
        public string id, utc, type, message, scene, version, build, platform, activity;
        public int age, day;
        public string sourceClass = Missing, method = Missing, file = Missing, line = Missing;
        public string[] stack, events;

        static readonly Regex Frame = new Regex(
            @"^\s*(?:at )?(?<symbol>[\w.$+/<>`-]+[:.][\w<>.$`-]+)\s*\([^\r\n)]*\)(?<location>.*)$",
            RegexOptions.CultureInvariant);
        static readonly Regex Location = new Regex(
            @"(?:\(at | in )[^\r\n]*?(?<file>[A-Za-z_][\w.-]*\.cs):(?:line )?(?<line>[1-9][0-9]{0,6})\)?\s*$");
        static readonly string[] KnownMessages =
        {
            "Object reference not set to an instance of an object",
            "Index was outside the bounds of the array",
            "Attempted to divide by zero",
            "LifeWorld: gameplay InputActionAsset is missing.",
            "Ian: 시스템 연결을 확인하세요.",
            "Bootstrap GameContext missing"
        };

        internal static string SafeMessage(string value)
        {
            foreach (string known in KnownMessages)
            {
                if (value.IndexOf(known, StringComparison.Ordinal) >= 0)
                {
                    return known;
                }
            }

            // Keep the exception category, never its potentially user-controlled arguments.
            var exception = Regex.Match(value, @"^(?:System\.)?([A-Za-z]{1,60}Exception)\b");
            return exception.Success ? exception.Groups[1].Value : "Message withheld (unreviewed text)";
        }

        internal void Decode(string raw, string trace)
        {
            message = SafeMessage(raw);
            var exception = Regex.Match(raw, @"^(?:System\.)?([A-Za-z]{1,60}Exception)\b");
            if (type == "Exception" && exception.Success)
            {
                type = exception.Groups[1].Value;
            }

            var frames = new List<string>();
            bool gameSource = false;
            foreach (string row in trace.Split('\n'))
            {
                if (row.Length > 2048 || frames.Count == 32)
                {
                    continue;
                }

                var match = Frame.Match(row);
                if (!match.Success)
                {
                    continue;
                }

                string symbol = match.Groups["symbol"].Value;
                int split = Math.Max(symbol.LastIndexOf(':'), symbol.LastIndexOf('.'));
                var source = Location.Match(match.Groups["location"].Value);
                string name = source.Success ? source.Groups["file"].Value : Missing;
                string number = source.Success ? source.Groups["line"].Value : Missing;
                frames.Add(symbol + (source.Success ? " (" + name + ":" + number + ")" : ""));
                bool project = symbol.StartsWith("IanGame.", StringComparison.Ordinal);
                if (frames.Count == 1 || (project && !gameSource))
                {
                    sourceClass = symbol.Substring(0, split);
                    method = symbol.Substring(split + 1);
                    file = name;
                    line = number;
                    gameSource = project;
                }
            }

            stack = frames.ToArray();
        }

        // Rebuild the preview from known fields even when a local report was edited or damaged.
        internal bool PreparePreview()
        {
            if (!DateTimeOffset.TryParseExact(utc, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return false;
            }
            string Symbol(string value) => value != null && Regex.IsMatch(value, @"^[A-Za-z0-9_.$+/<>`-]{1,180}$")
                ? value : Missing;
            scene = Symbol(scene);
            version = Symbol(version);
            platform = Symbol(platform);
            build = Symbol(build);
            activity = Symbol(activity);
            type = Symbol(type);
            message = type == "Manual" ? "Player requested a diagnostic snapshot."
                : type == "UnexpectedTermination" ? "Previous foreground session ended without a clean marker."
                : SafeMessage(message ?? "");
            sourceClass = Symbol(sourceClass);
            method = Symbol(method);
            file = file != null && Regex.IsMatch(file, @"^[A-Za-z_][\w.-]{0,120}\.cs$") ? file : Missing;
            line = int.TryParse(line, out int value) && value > 0 ? value.ToString() : Missing;
            stack = Array.FindAll(stack ?? Array.Empty<string>(), frame => frame != null && frame.Length <= 400
                && Regex.IsMatch(frame, @"^[\w.$+/<>`-]+[:.][\w<>.$`-]+(?: \([A-Za-z_][\w.-]*\.cs:[0-9]{1,7}\))?$"));
            events = Array.FindAll(events ?? Array.Empty<string>(), IsKnownEvent);
            return true;
        }

        static bool IsKnownEvent(string value)
        {
            if (value == null || value.Length > 70)
            {
                return false;
            }
            var words = value.Split(' ');
            return (words.Length == 2 || words.Length == 3)
                && Regex.IsMatch(words[0], @"^[0-9]{2}:[0-9]{2}:[0-9]{2}$")
                && Enum.IsDefined(typeof(DiagnosticEvent), words[1])
                && (words.Length == 2 || Enum.IsDefined(typeof(WorldInteractionKind), words[2]));
        }

        internal string Display()
        {
            return $"IAN GROWING — LOCAL ERROR REPORT\n\nReport ID: {id}\nUTC: {utc}\n"
                + $"Game version: {version}\nBuild ID: {build}\nPlatform: {platform}\n\n"
                + $"Type: {type}\nMessage: {message}\nScene: {scene}\n"
                + $"Age: {Number(age)}   Day: {Number(day)}   Activity: {activity}\n\n"
                + $"Class: {sourceClass}\nMethod: {method}\nFile: {file}\nLine: {line}\n\n"
                + "STACK TRACE\n" + Lines(stack) + "\n\nRECENT EVENTS\n" + Lines(events) + "\n";
        }

        static string Number(int value) => value < 0 ? Missing : value.ToString(CultureInfo.InvariantCulture);
        static string Lines(string[] values) => values == null || values.Length == 0 ? Missing : string.Join("\n", values);
    }
}
