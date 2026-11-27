using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IanGame;
using Newtonsoft.Json.Linq;

internal sealed class DiagnosticService : HttpMessageHandler
{
    internal volatile int Mode = 503;
    internal int Attempts;
    internal byte[] Stored;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Interlocked.Increment(ref Attempts);
        if (Mode == 503)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
        var data = await request.Content.ReadAsByteArrayAsync(token);
        var id = request.Headers.GetValues("X-Event-Id").Single();
        var date = request.Headers.GetValues("X-Report-Date").Single();
        var digest = request.Headers.GetValues("X-Content-Sha256").Single();
        var ack = new JObject
        {
            ["stored"] = true,
            ["key"] = "android/" + date + "/" + id + ".json",
            ["eventId"] = id,
            ["sha256"] = Mode == 299 ? "invalid" : digest
        };
        if (Mode == 201)
        {
            Stored = data;
        }
        return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(ack.ToString()) };
    }
}

internal static class JournalHarness
{
    static void Require(bool condition, string reason)
    {
        if (!condition)
        {
            throw new Exception(reason);
        }
    }

    static void Until(Func<bool> condition)
    {
        Require(SpinWait.SpinUntil(condition, 20000), "Timeout");
    }

    static void ReleaseAssembly(string path)
    {
        using var file = File.OpenRead(path);
        using var pe = new PEReader(file);
        var metadata = pe.GetMetadataReader();
        var types = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition).ToArray();
        var reporter = types.Single(type => metadata.GetString(type.Name) == "IanCloudErrorReporter");
        var methods = reporter.GetMethods().Select(metadata.GetMethodDefinition).ToArray();
        var names = methods.Select(method => metadata.GetString(method.Name)).OrderBy(name => name).ToArray();
        var active = methods.Single(method => metadata.GetString(method.Name) == "get_Active");
        var bytecode = pe.GetMethodBody(active.RelativeVirtualAddress).GetILBytes();
        Require(names.SequenceEqual(new[] { ".ctor", "get_Active" }), "Release probe methods present");
        Require(bytecode.SequenceEqual(new byte[] { 0x16, 0x2a }), "Release Active must return false");
        var capture = new[] { "CloudErrorOutbox", "CloudErrorTransport", "CloudErrorTicket", "CloudErrorPrivacy" };
        Require(!types.Any(type => capture.Contains(metadata.GetString(type.Name))), "Release transport present");
        Console.WriteLine(JsonSerializer.Serialize(new { reporterMethods = names, captureTypes = 0, activeReturnsFalse = true }));
    }

    public static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            ReleaseAssembly(args[0]);
            return;
        }
        string confidential = "Authorization: Bearer fixture-secret\r\npassword=fixture-pass\n"
            + "email@example.com /Users/Test Person/Assets/Test.cs:42 192.0.2.1 2001:db8::1 ::1 "
            + "4111111111111111 010-1234-5678 00:1A:2B:3C:4D:5E\n사용자입력=sample private text";
        string cleaned = CloudErrorPrivacy.Clean(confidential, "");
        foreach (string raw in new[] {"fixture-secret", "fixture-pass", "example.com", "Test Person", "192.0.2.1",
            "2001:db8::1", "::1", "4111111111111111", "010-1234-5678", "00:1A:2B:3C:4D:5E", "sample private text"})
        {
            Require(!cleaned.Contains(raw), "Unmasked fixture");
        }
        Require(cleaned.Contains("/Assets/Test.cs:42") && cleaned.Contains("\r\n"), "Lost frame or newline");
        string trace = string.Concat(Enumerable.Repeat("  at Ian.Example (at Assets/Test.cs:19)\r\n", 160000));
        Require(CloudErrorPrivacy.Clean(trace, "") == trace, "Long trace changed");
        string root = Directory.CreateTempSubdirectory("ian-cloud-journal-").FullName;
        string spool = Path.Combine(root, "outbox");
        string config = Path.Combine(root, "session.json");
        string enrollment = new JObject
        {
            ["ticket"] = "ian2.android.synthetic-fixture",
            ["platform"] = "android",
            ["expires"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        }.ToString();
        File.WriteAllText(config, enrollment);
        var fake = new DiagnosticService();
        var box = new CloudErrorOutbox(spool, config, "android", "qa", "fixture", fake);
        try
        {
            var timer = Stopwatch.StartNew();
            box.Record("Error", "diagnostic service failure probe", trace.Substring(0, 1200));
            Require(timer.ElapsedMilliseconds < 200, "Callback blocked on network");
            string failure = Path.Combine(spool, "last-upload-failure.txt");
            Until(() => File.Exists(failure) && File.ReadAllText(failure).Contains("HTTP 503"));
            string file = Directory.GetFiles(spool, "*.json").Single();
            var before = File.ReadAllBytes(file);
            fake.Mode = 299;
            int attempts = fake.Attempts;
            Until(() => fake.Attempts > attempts);
            Require(File.ReadAllBytes(file).SequenceEqual(before), "Corrupt acknowledgement deleted event");
            fake.Mode = 201;
            Until(() => !File.Exists(file));
            Require(fake.Stored.SequenceEqual(before), "Retry changed bytes");
            Console.WriteLine("PASS privacy fixtures, >5 MiB unmodified trace, HTTP 503, invalid 201, retry and callback isolation.");
        }
        finally
        {
            box.Stop();
        }
        string offline = Path.Combine(root, "transport-offline");
        File.WriteAllText(offline,"");
        var burst = new CloudErrorOutbox(spool, config,"android","qa","fixture",new DiagnosticService());
        try
        {
            Parallel.For(0,3000,i => burst.Record("Exception","burst "+i,"synthetic frame"));
            Until(() => File.Exists(Path.Combine(spool,"capacity-loss.txt")));
            burst.FlushBriefly();
            Require(Directory.GetFiles(spool,"*.json").Length <= 128, "Unbounded disk journal");
            Console.WriteLine("PASS concurrent burst, bounded queue and recorded capacity loss.");
        }
        finally
        {
            burst.Stop();
        }
    }
}
