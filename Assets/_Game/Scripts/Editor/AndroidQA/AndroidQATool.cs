using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace IanGame.Editor
{
    [McpForUnityTool("ian_android_qa", Description = "Local physical Android QA. Use devices, info, build, build-status, install, launch, stop, restart, start/end (log session), screenshot, record, tap/swipe/long-press (normalized coordinates), key, screen, tail/search/errors/crash/stack/export, diagnostics, performance, smoke, regression or report. PNG paths can be viewed locally.", Group = "testing")]
    public static class AndroidQATool
    {
        public class Parameters
        {
            [ToolParameter("Operation name")] public string action { get; set; }
            [ToolParameter("ADB transport ID; required with multiple phones", Required = false)] public string device { get; set; }
            [ToolParameter("Action arguments, e.g. [0.5,0.4] for tap", Required = false)] public string[] values { get; set; }
            [ToolParameter("Live, Debug, Warning, Error, Crash, Quest, Save, UI, Network, Performance or Legal", Required = false)]
            public string preset { get; set; }
        }

        static readonly HashSet<string> Commands = new HashSet<string>(
            "devices info status reconnect install launch stop restart screenshot record tap swipe long-press key screen performance start end tail search errors crash stack export report smoke regression diagnostics reward-check background-resume mirror".Split(' '));

        public static async Task<object> HandleCommand(JObject parameters)
        {
            try
            {
                var p = parameters.ToObject<Parameters>();
                if (p.action == "preflight")
                {
                    if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorUtility.scriptCompilationFailed)
                    {
                        throw new InvalidOperationException("Wait for successful script compilation before deploying.");
                    }
                    return new { success = true, result = "Editor scripts compiled" };
                }
                if (p.action == "build")
                {
                    AndroidQABuild.Queue();
                    return new { success = true, result = "QUEUED; poll build-status" };
                }
                if (p.action == "build-status")
                {
                    return new { success = true, result = File.Exists(AndroidQABuild.Status) ? File.ReadAllText(AndroidQABuild.Status) : "No build" };
                }
                if (p.action == "mirror")
                {
                    throw new ArgumentException("Open the interactive mirror from the dashboard or CLI.");
                }
                return new { success = true, result = await Run(p.action, p.device, p.values, p.preset) };
            }
            catch (Exception e)
            {
                return new { success = false, error = e.Message };
            }
        }

        internal static Task<string> Run(string action, string device = null, string[] values = null, string preset = null, string session = null)
        {
            if (!Commands.Contains(action))
            {
                throw new ArgumentException("Unknown QA action.");
            }
            string root = Path.GetDirectoryName(Application.dataPath);
            var args = new List<string> { Path.Combine(root, "Tools/AndroidQA/qa.py"), action };
            if (!string.IsNullOrWhiteSpace(device))
            {
                args.Add("--device");
                args.Add(device);
            }
            if (!string.IsNullOrWhiteSpace(preset))
            {
                args.Add("--preset");
                args.Add(preset);
            }
            if (!string.IsNullOrEmpty(session))
            {
                args.Add("--session");
                args.Add(session);
            }
            if (values != null)
            {
                if (values.Any(v => v == null || v.StartsWith("--") || v.IndexOf('\0') >= 0))
                {
                    throw new ArgumentException("Invalid argument.");
                }
                args.AddRange(values);
            }

            // No shell or interpolated shell commands. Read both pipes concurrently.
            return Task.Run(async () =>
            {
                string python = File.Exists("/opt/homebrew/bin/python3") ? "/opt/homebrew/bin/python3" : "/usr/bin/python3";
                string Quote(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo(python, string.Join(" ", args.Select(Quote)))
                    {
                        WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true,
                        RedirectStandardError = true, CreateNoWindow = true
                    }
                };
                process.Start();
                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                await Task.WhenAll(stdout, stderr);
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException((await stderr).Trim());
                }
                return (await stdout).Trim();
            });
        }
    }
}
