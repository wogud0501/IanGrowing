using System;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    public enum RubySource
    {
        Mission,
        Achievement,
        Event,
        Compensation,
        Purchase,
        Admin,
        Migration,
        Gacha,
    }

    public enum RubySpendOrder
    {
        FreeFirst,
        PaidFirst,
    }

    public enum GachaCurrency
    {
        Gold,
        Ruby,
    }

    [Serializable]
    public sealed class RubyRewardRule
    {
        public string id;
        public int amount;
    }

    [Serializable]
    public sealed class RubyProductDefinition
    {
        public string internalId;
        public string appleProductId;
        public string googleProductId;
        public string localizedName;
        public string localizedDescription;
        public int rubyAmount;
        public int bonusRuby;
        public int displayOrder;
        public int firstPurchaseBonus;
        public bool enabled;

        public string StoreProductId(string store) =>
            store == "apple" ? appleProductId
            : store == "google" ? googleProductId
            : null;
    }

    [Serializable]
    public sealed class LegalDocumentDefinition
    {
        public string documentId;
        public string version = "draft-1";
        public string effectiveDate = "[시행일]";
        public string title;
        public string locale = "ko";
        public string url;
        public TextAsset body;
        public bool required;
        public bool requireConsent;
    }

    [CreateAssetMenu(menuName = "Ian/Release config")]
    public sealed class ReleaseConfig : ScriptableObject
    {
        public string companyName = "[회사명]";
        public string representative = "[대표자명]";
        public string businessNumber = "[사업자등록번호]";
        public string commerceNumber = "[통신판매업 신고번호]";
        public string address = "[사업장 주소]";
        public string supportEmail = "[고객센터 이메일]";
        public string supportPhone = "[고객센터 전화번호]";
        public string privacyOfficer = "[개인정보 보호책임자]";
        public string privacyEmail = "[개인정보 문의 이메일]";
        public CollectionVisualTheme uiTheme;
        public string TermsUrl;
        public string PrivacyPolicyUrl;
        public string SupportUrl;
        public string RefundHelpUrl;
        public bool legalReviewed;
        public bool privacyAuditApproved;
        public RubySpendOrder spendOrder;
        public RubyRewardRule[] missionRuby = Array.Empty<RubyRewardRule>();
        public RubyRewardRule[] achievementRuby = Array.Empty<RubyRewardRule>();
        public RubyProductDefinition[] products = Array.Empty<RubyProductDefinition>();
        public LegalDocumentDefinition[] documents = Array.Empty<LegalDocumentDefinition>();
        public static ReleaseConfig Current => Resources.Load<ReleaseConfig>("IanRelease");

        public int Reward(RubySource source, string id) =>
            (source == RubySource.Mission ? missionRuby : achievementRuby).FirstOrDefault(r => r.id == id)?.amount ?? 0;

        public LegalDocumentDefinition Document(string id, string locale = "ko") =>
            documents.FirstOrDefault(d => d.documentId == id && d.locale == locale);
    }
}
