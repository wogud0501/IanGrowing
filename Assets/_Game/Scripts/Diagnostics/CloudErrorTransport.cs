#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IanGame
{
    internal sealed class CloudIngestException : IOException
    {
        internal readonly int Status;

        internal CloudIngestException(int status)
        {
            Status = status;
        }
    }

    internal sealed class CloudErrorTransport : IDisposable
    {
        internal const string Endpoint = "https://iangrowing-error-api.iangrowing-error-api.workers.dev/v1/errors";
        const int PartBytes = 1024 * 1024;
        readonly HttpClient client;

        internal CloudErrorTransport(HttpMessageHandler handler = null)
        {
            client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = TimeSpan.FromSeconds(20),
            };
        }

        internal async Task Send(string file, string platform, string ticket, CancellationToken token)
        {
            string id = Path.GetFileNameWithoutExtension(file);
            string day = DateTimeOffset
                .FromUnixTimeMilliseconds(long.Parse(id.Substring(0, 12), NumberStyles.HexNumber))
                .UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            using (var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
            {
                string build = null;
                string version = null;
                using (var reader = new StreamReader(input, System.Text.Encoding.UTF8, true, 4096, true))
                using (var json = new JsonTextReader(reader))
                {
                    while (json.Read())
                    {
                        if (json.TokenType != JsonToken.PropertyName)
                        {
                            continue;
                        }
                        var key = (string)json.Value;
                        if (key == "message")
                        {
                            break;
                        }
                        if (key == "buildId" || key == "gameVersion")
                        {
                            string value = json.ReadAsString();
                            if (key == "buildId")
                            {
                                build = value;
                            }
                            else
                            {
                                version = value;
                            }
                        }
                    }
                }
                input.Position = 0;
                string digest;
                using (var sha = SHA256.Create())
                {
                    digest = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
                }
                input.Position = 0;
                long size = input.Length;
                string expectedKey = platform + "/" + day + "/" + id + ".json";

                async Task<bool> Post(string suffix, byte[] body, int index)
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, Endpoint + suffix))
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + ticket);
                        request.Headers.TryAddWithoutValidation("User-Agent", "IanGrowingDiagnostics/2.0");
                        request.Headers.Add("X-Log-Source", platform);
                        request.Headers.Add("X-Event-Id", id);
                        request.Headers.Add("X-Report-Date", day);
                        request.Headers.Add("X-Content-Sha256", digest);
                        request.Headers.Add("X-Total-Bytes", size.ToString(CultureInfo.InvariantCulture));
                        if (!string.IsNullOrEmpty(build))
                        {
                            request.Headers.Add("X-Build-Id", build);
                        }
                        if (!string.IsNullOrEmpty(version))
                        {
                            request.Headers.Add("X-Game-Version", version);
                        }
                        request.Content = new ByteArrayContent(body);
                        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                            suffix.Length == 0 ? "application/json" : "application/octet-stream"
                        );
                        using (var response = await client.SendAsync(request, token).ConfigureAwait(false))
                        {
                            if ((int)response.StatusCode != 200 && (int)response.StatusCode != 201)
                            {
                                throw new CloudIngestException((int)response.StatusCode);
                            }
                            var ack = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            if (
                                (bool?)ack["stored"] == true
                                && (string)ack["key"] == expectedKey
                                && (string)ack["sha256"] == digest
                                && (string)ack["eventId"] == id
                            )
                            {
                                return true;
                            }
                            if (index >= 0 && (bool?)ack["partStored"] == true && (int?)ack["index"] == index)
                            {
                                return false;
                            }
                            throw new IOException("Invalid ingest receipt.");
                        }
                    }
                }

                int part = 0;
                bool large = size > 5L * PartBytes;
                while (input.Position < size)
                {
                    int count = (int)Math.Min(large ? PartBytes : size, size - input.Position);
                    byte[] data = new byte[count];
                    int offset = 0;
                    while (offset < count)
                    {
                        int read = input.Read(data, offset, count - offset);
                        if (read == 0)
                        {
                            throw new EndOfStreamException();
                        }
                        offset += read;
                    }
                    if (
                        await Post(large ? "?phase=part&index=" + part : "", data, large ? part : -1)
                            .ConfigureAwait(false)
                    )
                    {
                        return;
                    }
                    part++;
                }
                if (!large || !await Post("?phase=commit", Array.Empty<byte>(), -1).ConfigureAwait(false))
                {
                    throw new IOException("Missing final receipt.");
                }
            }
        }

        public void Dispose() => client.Dispose();
    }
}
#endif
