#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace IanGame
{
    internal sealed class CloudErrorRecord
    {
        public string timestamp;
        public string eventId;
        public string platform;
        public string gameVersion;
        public string buildId;
        public string sessionId;
        public string level;
        public string message;
        public string stackTrace;
    }

    internal sealed class CloudErrorTicket
    {
        public string ticket;
        public string platform;
        public long expires;

        internal static CloudErrorTicket Read(string path, string source)
        {
            if (!File.Exists(path) || new FileInfo(path).Length > 2048)
            {
                return null;
            }
            var config = JsonConvert.DeserializeObject<CloudErrorTicket>(File.ReadAllText(path));
            return
                config?.platform == source
                && config.ticket != null
                && config.ticket.StartsWith("ian2." + source + ".", StringComparison.Ordinal)
                ? config
                : null;
        }
    }

    internal sealed class CloudErrorOutbox
    {
        const int PendingLimit = 128;
        const long DiskLimit = 96L * 1024 * 1024;
        const long MessageLimit = 64L * 1024 * 1024;
        readonly Queue<CloudErrorRecord> input = new Queue<CloudErrorRecord>();
        readonly AutoResetEvent changed = new AutoResetEvent(false);
        readonly AutoResetEvent retry = new AutoResetEvent(false);
        readonly CancellationTokenSource end = new CancellationTokenSource();
        readonly string directory;
        readonly string config;
        readonly string version;
        readonly string build;
        readonly string session = Guid.NewGuid().ToString("N");
        readonly string initialTicket;
        readonly HttpMessageHandler handler;
        long enrolledUntil;
        long queuedChars;
        long lost;
        bool writing;
        volatile bool stopping;
        internal string Platform { get; }

        internal CloudErrorOutbox(
            string directory,
            string config,
            string platform,
            string version,
            string build,
            HttpMessageHandler handler = null
        )
        {
            this.handler = handler;
            this.directory = directory;
            this.config = config;
            this.version = version;
            this.build = string.IsNullOrWhiteSpace(build) || build.Trim('0', '-') == "" ? null : build;
            Platform = platform;
            var enrollment = CloudErrorTicket.Read(config, platform);
            initialTicket = enrollment?.ticket;
            enrolledUntil = enrollment?.expires ?? 0;
            new Thread(WriteLoop) { IsBackground = true, Name = "Ian error journal" }.Start();
            new Thread(SendLoop) { IsBackground = true, Name = "Ian diagnostic upload" }.Start();
        }

        internal void Record(string level, string message, string trace)
        {
            long size = (long)(message?.Length ?? 0) + (trace?.Length ?? 0);
            lock (input)
            {
                if (stopping || DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= Interlocked.Read(ref enrolledUntil))
                {
                    return;
                }
                if (input.Count >= PendingLimit || size > MessageLimit || queuedChars + size > MessageLimit)
                {
                    Interlocked.Increment(ref lost);
                }
                else
                {
                    var now = DateTimeOffset.UtcNow;
                    input.Enqueue(
                        new CloudErrorRecord
                        {
                            timestamp = now.ToString("O"),
                            eventId =
                                now.ToUnixTimeMilliseconds().ToString("x12")
                                + Guid.NewGuid().ToString("N").Substring(12),
                            platform = Platform,
                            gameVersion = version,
                            buildId = build,
                            sessionId = session,
                            level = level,
                            message = message ?? "",
                            stackTrace = trace ?? "",
                        }
                    );
                    queuedChars += size;
                }
            }
            changed.Set();
        }

        internal void FlushBriefly()
        {
            changed.Set();
            lock (input)
            {
                if (input.Count != 0 || writing)
                {
                    Monitor.Wait(input, 200);
                }
            }
        }

        internal void Stop()
        {
            stopping = true;
            end.Cancel();
            changed.Set();
            retry.Set();
            FlushBriefly();
        }

        internal void Retry() => retry.Set();

        static void AtomicWrite(string path, string text)
        {
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(text);
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(temp, path);
            }
            finally
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
        }

        void Persist(CloudErrorRecord record)
        {
            Directory.CreateDirectory(directory);
            record.message = CloudErrorPrivacy.Clean(record.message, initialTicket);
            record.stackTrace = CloudErrorPrivacy.Clean(record.stackTrace, initialTicket);
            var body = JsonConvert.SerializeObject(record);
            long bytes = Encoding.UTF8.GetByteCount(body);
            if (bytes > MessageLimit)
            {
                Interlocked.Increment(ref lost);
                return;
            }
            var files = new DirectoryInfo(directory)
                .GetFiles("*.json")
                .OrderBy(i => i.Name, StringComparer.Ordinal)
                .ToArray();
            long total = files.Sum(i => i.Exists ? i.Length : 0);
            int count = files.Length;
            foreach (var old in files)
            {
                if (count < PendingLimit && total + bytes <= DiskLimit)
                {
                    break;
                }
                if (old.Exists)
                {
                    total -= old.Length;
                    old.Delete();
                    Interlocked.Increment(ref lost);
                }
                count--;
            }
            AtomicWrite(Path.Combine(directory, record.eventId + ".json"), body);
            retry.Set();
        }

        void WriteLoop()
        {
            for (; ; )
            {
                CloudErrorRecord record = null;
                lock (input)
                {
                    if (input.Count > 0)
                    {
                        record = input.Dequeue();
                        queuedChars -= record.message.Length + record.stackTrace.Length;
                        writing = true;
                    }
                }
                try
                {
                    if (record != null)
                    {
                        Persist(record);
                    }
                    long count = Interlocked.Exchange(ref lost, 0);
                    if (count > 0)
                    {
                        try
                        {
                            Directory.CreateDirectory(directory);
                            string path = Path.Combine(directory, "capacity-loss.txt");
                            long previous =
                                File.Exists(path) && long.TryParse(File.ReadAllText(path), out var n) ? n : 0;
                            File.WriteAllText(path, (previous + count).ToString());
                        }
                        catch
                        {
                            Interlocked.Add(ref lost, count);
                        }
                    }
                }
                catch
                {
                    Interlocked.Increment(ref lost);
                }
                finally
                {
                    lock (input)
                    {
                        writing = false;
                        if (input.Count == 0)
                        {
                            Monitor.PulseAll(input);
                        }
                    }
                }
                if (record == null)
                {
                    if (stopping)
                    {
                        return;
                    }
                    changed.WaitOne(1000);
                }
            }
        }

        void NoteFailure(Exception failure)
        {
            if (stopping)
            {
                return;
            }
            try
            {
                Directory.CreateDirectory(directory);
                string category = failure is CloudIngestException rejected
                    ? "HTTP " + rejected.Status
                    : failure.GetType().Name;
                File.WriteAllText(
                    Path.Combine(directory, "last-upload-failure.txt"),
                    DateTime.UtcNow.ToString("O") + " " + category
                );
            }
            catch (Exception)
            {
                // Disk and network failure must never re-enter Unity's logging callback.
            }
        }

        void SendLoop()
        {
            int seconds = 2;
            try
            {
                using (var transport = new CloudErrorTransport(handler))
                {
                    while (!stopping)
                    {
                        try
                        {
                            var auth = CloudErrorTicket.Read(config, Platform);
                            Interlocked.Exchange(ref enrolledUntil, auth?.expires ?? 0);
                            if (
                                auth == null
                                || auth.expires <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                                || File.Exists(Path.Combine(Path.GetDirectoryName(config), "transport-offline"))
                            )
                            {
                                retry.WaitOne(3000);
                                continue;
                            }
                            var files = Directory.Exists(directory)
                                ? Directory.GetFiles(directory, "*.json")
                                : Array.Empty<string>();
                            Array.Sort(files, StringComparer.Ordinal);
                            foreach (string file in files)
                            {
                                if (stopping)
                                {
                                    return;
                                }
                                if (!File.Exists(file))
                                {
                                    continue;
                                }
                                transport.Send(file, Platform, auth.ticket, end.Token).GetAwaiter().GetResult();
                                File.Delete(file);
                            }
                            seconds = 2;
                            retry.WaitOne(3000);
                        }
                        catch (Exception failure)
                        {
                            NoteFailure(failure);
                            // Do not send exceptions from this uploader back through Unity logging.
                            end.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(seconds));
                            seconds = Math.Min(seconds * 2, 60);
                        }
                    }
                }
            }
            catch (Exception failure)
            {
                NoteFailure(failure);
            }
        }
    }
}
#endif
