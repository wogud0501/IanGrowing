using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    [Serializable]
    public sealed class LegalAcceptance
    {
        public string documentId;
        public string locale;
        public string version;
        public long acknowledgedUtc;
    }

    [Serializable]
    public sealed class LegalAcceptanceSnapshot
    {
        public List<LegalAcceptance> accepted = new List<LegalAcceptance>();
    }

    public static class LegalDocumentService
    {
        const string Key = "ian.release.legal.acceptance";

        public static LegalAcceptanceSnapshot Capture()
        {
            try
            {
                return JsonUtility.FromJson<LegalAcceptanceSnapshot>(PlayerPrefs.GetString(Key, "{}"))
                    ?? new LegalAcceptanceSnapshot();
            }
            catch
            {
                return new LegalAcceptanceSnapshot();
            }
        }

        public static bool NeedsConsent
        {
            get
            {
                var c = ReleaseConfig.Current;
                var a = Capture().accepted ?? new List<LegalAcceptance>();
                return c != null
                    && c.documents.Any(d =>
                        d.required
                        && !a.Any(v => v.documentId == d.documentId && v.locale == d.locale && v.version == d.version)
                    );
            }
        }

        public static void AcceptRequired()
        {
            var c = ReleaseConfig.Current;
            if (c == null)
            {
                return;
            }

            var a = Capture();
            if (a.accepted == null)
            {
                a.accepted = new List<LegalAcceptance>();
            }

            foreach (var d in c.documents.Where(d => d.required))
            {
                a.accepted.RemoveAll(v => v.documentId == d.documentId && v.locale == d.locale);
                a.accepted.Add(
                    new LegalAcceptance
                    {
                        documentId = d.documentId,
                        locale = d.locale,
                        version = d.version,
                        acknowledgedUtc = DateTime.UtcNow.Ticks,
                    }
                );
            }

            PlayerPrefs.SetString(Key, JsonUtility.ToJson(a));
            PlayerPrefs.Save();
        }

        public static void Restore(LegalAcceptanceSnapshot value)
        {
            if (value?.accepted == null)
            {
                return;
            }

            var c = ReleaseConfig.Current;
            var a = Capture();
            if (a.accepted == null)
            {
                a.accepted = new List<LegalAcceptance>();
            }

            foreach (var v in value.accepted)
            {
                var d = c?.Document(v.documentId, v.locale);
                if (
                    d == null
                    || d.version != v.version
                    || v.acknowledgedUtc <= 0
                    || a.accepted.Any(x =>
                        x.documentId == v.documentId && x.locale == v.locale && x.version == v.version
                    )
                )
                {
                    continue;
                }

                a.accepted.Add(v);
            }

            PlayerPrefs.SetString(Key, JsonUtility.ToJson(a));
        }

        public static string Body(LegalDocumentDefinition d) =>
            d == null
                ? "해당 언어의 문서가 없습니다."
                : (d.body?.text ?? "문서 준비 중").Replace("<!-- LEGAL REVIEW REQUIRED BEFORE RELEASE -->", "").Trim();
    }
}
