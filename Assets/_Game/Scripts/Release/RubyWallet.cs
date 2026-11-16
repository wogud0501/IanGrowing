using System;using System.Linq;using System.Collections.Generic;using UnityEngine;
namespace IanGame {
[Serializable]public sealed class RubyTransaction {
 public string transactionId,type,relatedId;public RubySource source;public long timestamp;public long freeDelta,paidDelta,amount;public bool paid;
}
[Serializable]public sealed class RubySnapshot {
 public int walletVersion=1;public long freeRuby,paidRuby,totalRuby;public List<RubyTransaction> transactions=new List<RubyTransaction>();public List<string> processedPurchaseTransactionIds=new List<string>();
}
// Economy owns this wallet. No expiry and no eviction of deduplication receipts.
public sealed class RubyWallet {
 RubySnapshot state=new RubySnapshot();public event Action Changed;
 public long Free=>state.freeRuby;public long Paid=>state.paidRuby;public long Total=>Free+Paid;public const long Limit=1000000000;
 public RubySnapshot Capture()=>JsonUtility.FromJson<RubySnapshot>(JsonUtility.ToJson(state));
 public void Restore(RubySnapshot value){if(!Valid(value))throw new ArgumentException("Invalid Ruby ledger");state=value==null?new RubySnapshot():JsonUtility.FromJson<RubySnapshot>(JsonUtility.ToJson(value));Changed?.Invoke();}
 public bool Contains(string id)=>state.transactions.Any(t=>t.transactionId==id);
 static bool Id(string id)=>!string.IsNullOrWhiteSpace(id)&&id.Length<=200;
 internal bool Credit(string id,long free,long paid,RubySource source,string related,string purchaseId=null){
  if(!Id(id)||!Id(related)||!Enum.IsDefined(typeof(RubySource),source)||free<0||paid<0||free>Limit||paid>Limit||free+paid<=0||Total>Limit-free-paid||Contains(id)||state.transactions.Count>=100000||paid>0&&source!=RubySource.Purchase||source==RubySource.Purchase&&(!Id(purchaseId)||state.processedPurchaseTransactionIds.Contains(purchaseId)))return false;
  if(source==RubySource.Purchase)state.processedPurchaseTransactionIds.Add(purchaseId);
  return Append(new RubyTransaction{transactionId=id,type="credit",source=source,relatedId=related,freeDelta=free,paidDelta=paid,amount=free+paid,paid=paid>0,timestamp=DateTime.UtcNow.Ticks});
 }
 internal bool Spend(string id,long amount,RubySource source,string related,RubySpendOrder order){
  if(!Id(id)||!Id(related)||amount<=0||amount>Total||Contains(id)||state.transactions.Count>=100000)return false;
  long f=order==RubySpendOrder.FreeFirst?Math.Min(Free,amount):Math.Max(0,amount-Paid);long p=amount-f;
  return Append(new RubyTransaction{transactionId=id,type="debit",source=source,relatedId=related,amount=amount,freeDelta=-f,paidDelta=-p,paid=p>0,timestamp=DateTime.UtcNow.Ticks});
 }
 bool Append(RubyTransaction t){state.freeRuby+=t.freeDelta;state.paidRuby+=t.paidDelta;state.totalRuby=Total;state.transactions.Add(t);Changed?.Invoke();return true;}
 public static bool Valid(RubySnapshot s){
  if(s==null)return true;if(s.walletVersion!=1||s.freeRuby<0||s.paidRuby<0||s.freeRuby>Limit||s.paidRuby>Limit||s.freeRuby+s.paidRuby>Limit||s.totalRuby!=s.freeRuby+s.paidRuby||s.transactions==null||s.processedPurchaseTransactionIds==null||s.transactions.Count>100000||s.transactions.Select(t=>t?.transactionId).Distinct().Count()!=s.transactions.Count||s.processedPurchaseTransactionIds.Any(id=>!Id(id))||s.processedPurchaseTransactionIds.Distinct().Count()!=s.processedPurchaseTransactionIds.Count)return false;
  long f=0,p=0;foreach(var t in s.transactions){if(t==null||!Id(t.transactionId)||!Id(t.relatedId)||t.timestamp<0||t.timestamp>DateTime.MaxValue.Ticks||!Enum.IsDefined(typeof(RubySource),t.source)||t.amount<=0||t.amount>Limit||t.paid!=(t.paidDelta!=0)||t.freeDelta < -Limit||t.freeDelta>Limit||t.paidDelta < -Limit||t.paidDelta>Limit)return false;
   if(t.type=="credit"){if(t.freeDelta<0||t.paidDelta<0||t.amount!=t.freeDelta+t.paidDelta||t.paidDelta>0&&t.source!=RubySource.Purchase)return false;}
   else if(t.type=="debit"){if(t.freeDelta>0||t.paidDelta>0||t.amount!=-t.freeDelta-t.paidDelta)return false;}else return false;
   f+=t.freeDelta;p+=t.paidDelta;if(f<0||p<0||f+p>Limit)return false;
  }return f==s.freeRuby&&p==s.paidRuby;
 }
 public (long purchased,long consumed,long remaining) PurchasedSummary(){long purchased=state.transactions.Where(t=>t.type=="credit").Sum(t=>t.paidDelta);return (purchased,purchased-Paid,Paid);}
}
}
