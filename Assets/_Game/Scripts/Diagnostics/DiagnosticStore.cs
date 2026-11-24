using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace IanGame
{
    internal sealed class DiagnosticStore
    {
        internal readonly string Errors, Sessions, Exports;
        internal string Failure { get; private set; }
        const long ByteLimit = 10 * 1024 * 1024;
        static readonly Regex ReportName = new Regex(@"^ERR-[0-9]{8}-[0-9]{6}-[a-f0-9]{32}$");

        internal DiagnosticStore(string root)
        {
            Errors = Path.Combine(root, "Errors");
            Sessions = Path.Combine(root, "Sessions");
            Exports = Path.Combine(root, "Exports");
        }

        internal bool Try(Action operation)
        {
            try
            {
                operation();
                Failure = null;
                return true;
            }
            catch (Exception e)
            {
                // Logging through Unity here would recursively create another report.
                Failure = "진단 파일을 처리할 수 없습니다. 저장 공간과 접근 권한을 확인하세요. (" + e.GetType().Name + ")";
                return false;
            }
        }

        internal bool Put(DiagnosticReport report)
        {
            return Try(() =>
            {
                Directory.CreateDirectory(Errors);
                string target = Path.Combine(Errors, report.id);
                string staging = target + ".pending";
                Directory.CreateDirectory(staging);
                try
                {
                    File.WriteAllText(Path.Combine(staging, "report.json"), JsonUtility.ToJson(report, true));
                    File.WriteAllText(Path.Combine(staging, "report.txt"), report.Display());
                    Directory.Move(staging, target);
                }
                finally
                {
                    if (Directory.Exists(staging))
                    {
                        Directory.Delete(staging, true);
                    }
                }
                Prune();
            });
        }

        IEnumerable<DirectoryInfo> Directories() => new DirectoryInfo(Errors).GetDirectories("ERR-*")
            .Where(d => ReportName.IsMatch(d.Name) && (d.Attributes & FileAttributes.ReparsePoint) == 0)
            .OrderByDescending(d => d.LastWriteTimeUtc);

        internal void Prune()
        {
            Directory.CreateDirectory(Errors);
            long bytes = 0;
            int count = 0;
            var cutoff = DateTime.UtcNow.AddDays(-14);
            foreach (var dir in Directories())
            {
                var exported = new FileInfo(Path.Combine(Exports, dir.Name + ".txt"));
                long size = dir.GetFiles().Sum(f => f.Length) + (exported.Exists ? exported.Length : 0);
                if (dir.LastWriteTimeUtc < cutoff || count >= 20 || size + bytes > ByteLimit)
                {
                    Remove(dir.Name);
                    continue;
                }
                count++;
                bytes += size;
            }

            // Remove interrupted writes once they can no longer belong to a running write.
            foreach (var dir in new DirectoryInfo(Errors).GetDirectories("*.pending"))
            {
                if (dir.LastWriteTimeUtc < DateTime.UtcNow.AddHours(-1))
                {
                    dir.Delete(true);
                }
            }
        }

        internal List<DiagnosticReport> List()
        {
            var list = new List<DiagnosticReport>();
            Try(() =>
            {
                Prune();
                foreach (var dir in Directories())
                {
                    var json = new FileInfo(Path.Combine(dir.FullName, "report.json"));
                    if (!json.Exists || json.Length > 128 * 1024)
                    {
                        continue;
                    }
                    try
                    {
                        var item = JsonUtility.FromJson<DiagnosticReport>(File.ReadAllText(json.FullName));
                        if (item != null && item.id == dir.Name && item.PreparePreview())
                        {
                            list.Add(item);
                        }
                    }
                    catch (ArgumentException) { /* Damaged report, still removable by clearing the store. */ }
                }
            });
            return list;
        }

        internal void Remove(string id)
        {
            if (!ReportName.IsMatch(id))
            {
                throw new InvalidDataException();
            }
            if (Directory.Exists(Exports))
            {
                File.Delete(Path.Combine(Exports, id + ".txt"));
            }
            Directory.Delete(Path.Combine(Errors, id), true);
        }

        internal void Clear()
        {
            foreach (string path in new[] { Errors, Exports })
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
        }
    }
}
