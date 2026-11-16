using System;using System.Linq;using UnityEngine;
namespace IanGame {
public enum RubySource {Mission,Achievement,Event,Compensation,Purchase,Admin,Migration,Gacha}
public enum RubySpendOrder {FreeFirst,PaidFirst}
public enum GachaCurrency {Gold,Ruby}
[Serializable]public sealed class RubyRewardRule {public string id;public int amount;}
[Serializable]public sealed class RubyProductDefinition {
 public string internalId,appleProductId,googleProductId,localizedName,localizedDescription;public int rubyAmount,bonusRuby,displayOrder,firstPurchaseBonus;public bool enabled;
 public string StoreProductId(string store)=>store=="apple"?appleProductId:store=="google"?googleProductId:null;
}
[Serializable]public sealed class LegalDocumentDefinition {
 public string documentId,version="draft-1",effectiveDate="[시행일]",title,locale="ko",url;public TextAsset body;public bool required,requireConsent;
}
[CreateAssetMenu(menuName="Ian/Release config")]
public sealed class ReleaseConfig:ScriptableObject {
 public string companyName="[회사명]",representative="[대표자명]",businessNumber="[사업자등록번호]",commerceNumber="[통신판매업 신고번호]",address="[사업장 주소]",supportEmail="[고객센터 이메일]",supportPhone="[고객센터 전화번호]",privacyOfficer="[개인정보 보호책임자]",privacyEmail="[개인정보 문의 이메일]";
 public CollectionVisualTheme uiTheme;public string TermsUrl,PrivacyPolicyUrl,SupportUrl,RefundHelpUrl;public bool legalReviewed,privacyAuditApproved;public RubySpendOrder spendOrder;
 public RubyRewardRule[] missionRuby=Array.Empty<RubyRewardRule>(),achievementRuby=Array.Empty<RubyRewardRule>();
 public RubyProductDefinition[] products=Array.Empty<RubyProductDefinition>();public LegalDocumentDefinition[] documents=Array.Empty<LegalDocumentDefinition>();
 public static ReleaseConfig Current=>Resources.Load<ReleaseConfig>("IanRelease");
 public int Reward(RubySource source,string id)=>(source==RubySource.Mission?missionRuby:achievementRuby).FirstOrDefault(r=>r.id==id)?.amount??0;
 public LegalDocumentDefinition Document(string id,string locale="ko")=>documents.FirstOrDefault(d=>d.documentId==id&&d.locale==locale);
}
}
