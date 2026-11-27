#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace IanGame
{
    internal static class CloudErrorPrivacy
    {
        static Regex Rule(string pattern)
        {
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(3));
        }

        static readonly Regex PrivateKey = Rule(
            @"-----BEGIN [A-Z0-9 ]*PRIVATE KEY-----[\s\S]*?(?:-----END [A-Z0-9 ]*PRIVATE KEY-----|\z)"
        );
        static readonly Regex Header = Rule(
            @"(\b(?:authorization|proxy-authorization|set-cookie|cookie)[ \t]*[:=][ \t]*)[^\r\n]+"
        );
        static readonly Regex Field = Rule(
            @"([""']?(?:access[_-]?token|refresh[_-]?token|api[_-]?key|secret|password|passwd|token|e-?mail|phone|device[_-]?id|user[_-]?(?:id|name|input)|account[_-]?id|chat|payment|card[_-]?number|address|gps|latitude|longitude|비밀번호|전화번호|이메일|사용자입력)[""']?[ \t]*[:=][ \t]*)(?:""(?:\\.|[^""\\\r\n])*""|'(?:\\.|[^'\\\r\n])*'|[^\r\n,;&}]+)"
        );
        static readonly Regex Credential = Rule(
            @"\b(?:Bearer[ \t]+[^\s,;]+|ian2\.[^\s""',;]+|(?:sk[-_]|gh[pousr]_|AKIA|eyJ)[A-Za-z0-9_./+=-]{12,})"
        );
        static readonly Regex Email = Rule(@"(?<![\w.+%-])[\w.+%-]{1,128}@[\w.-]{1,189}\.[a-z]{2,24}");
        static readonly Regex Home = Rule(@"((?:/Users/|/home/|[A-Z]:[\\/]+Users[\\/]+))[^/\\:\r\n""')]+");
        static readonly Regex Network = Rule(
            @"(?<![\w:.])(?:[\da-f]{0,4}:){2,7}[\da-f.]{0,15}(?![\w:.])|(?<![\w.])(?:\d{1,3}\.){3}\d{1,3}(?![\w.])"
        );
        static readonly Regex MacAddress = Rule(@"(?<!\w)(?:[\da-f]{2}[:-]){5}[\da-f]{2}\b");
        static readonly Regex Phone = Rule(
            @"(?<!\w)(?:(?:\+82[- .]?|0)1[016789][- .]?\d{3,4}[- .]?\d{4}|\+1[- .]?\(?\d{3}\)?[- .]?\d{3}[- .]?\d{4})(?!\d)"
        );
        static readonly Regex Card = Rule(@"(?<![\w.-])\d(?:[ -]?\d){12,18}(?![\w.-])");
        static readonly Regex Url = Rule(@"(https?://)[^/\s@]+:[^/\s@]+@|([?&][^=\s&]{1,80}=)[^&\s""']+");

        static string MaskCard(Match match)
        {
            int sum = 0;
            bool doubleDigit = false;
            for (int index = match.Length - 1; index >= 0; index--)
            {
                int digit = match.Value[index] - '0';
                if (digit < 0 || digit > 9)
                {
                    continue;
                }
                if (doubleDigit)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }
                sum += digit;
                doubleDigit = !doubleDigit;
            }
            return sum % 10 == 0 ? "[redacted]" : match.Value;
        }

        internal static string Clean(string text, string ticket)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text ?? "";
            }
            try
            {
                if (!string.IsNullOrEmpty(ticket))
                {
                    text = text.Replace(ticket, "[redacted]");
                }
                if (text.IndexOf("PRIVATE KEY-----", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    text = PrivateKey.Replace(text, "[redacted private key]");
                }
                // Bound regex work per physical line so that a long trace is not one time-limited match.
                // Split only on LF. CR and the presence or absence of a final newline survive the round trip.
                var lines = text.Split('\n');
                for (int index = 0; index < lines.Length; index++)
                {
                    string line = lines[index];
                    try
                    {
                        line = Header.Replace(line, "$1[redacted]");
                        line = Field.Replace(line, "$1[redacted]");
                        line = Credential.Replace(line, "[redacted]");
                        line = Home.Replace(line, "$1[redacted]");
                        line = Url.Replace(line, "$1$2[redacted]");
                        if (line.Contains("@"))
                        {
                            line = Email.Replace(line, "[redacted]");
                        }
                        line = MacAddress.Replace(line, "[redacted]");
                        line = Network.Replace(line, m => IPAddress.TryParse(m.Value, out _) ? "[redacted]" : m.Value);
                        lines[index] = Card.Replace(Phone.Replace(line, "[redacted]"), MaskCard);
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        lines[index] = "[withheld: privacy inspection timed out]";
                    }
                }
                return string.Join("\n", lines);
            }
            catch (RegexMatchTimeoutException)
            {
                return "[withheld: privacy inspection timed out]";
            }
        }
    }
}
#endif
