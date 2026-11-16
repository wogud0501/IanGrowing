using System;using System.Linq;using System.Threading.Tasks;
namespace IanGame {
public enum PurchaseStatus {Success,Cancelled,Pending,Failed,NotSupported}
public sealed class StoreProduct {public string internalId,storeProductId,localizedPrice,localizedName;}
public sealed class PurchaseReceipt {public string store,transactionId,storeProductId,payload;}
public sealed class PurchaseResult {public PurchaseStatus status;public string message;public PurchaseReceipt receipt;}
public interface IPurchaseProvider {
 bool IsAvailable{get;}bool IsMock{get;}Task Initialize();Task<StoreProduct[]> QueryProducts(RubyProductDefinition[] products);Task<PurchaseResult> Purchase(string storeProductId);Task<PurchaseReceipt[]> Restore();
}
public sealed class ValidatedPurchase {public bool valid;public string store,transactionId,storeProductId;}
public interface IPurchaseReceiptValidator {Task<ValidatedPurchase> Validate(PurchaseReceipt receipt);}
public sealed class UnavailablePurchaseProvider:IPurchaseProvider {
 public bool IsAvailable=>false;public bool IsMock=>false;public Task Initialize()=>Task.CompletedTask;public Task<StoreProduct[]> QueryProducts(RubyProductDefinition[] p)=>Task.FromResult(Array.Empty<StoreProduct>());public Task<PurchaseResult> Purchase(string id)=>Task.FromResult(new PurchaseResult{status=PurchaseStatus.NotSupported,message="구매 기능 준비 중"});public Task<PurchaseReceipt[]> Restore()=>Task.FromResult(Array.Empty<PurchaseReceipt>());
}
public sealed class PurchaseService {
 readonly GameManager game;readonly ReleaseConfig config;readonly IPurchaseProvider provider;readonly IPurchaseReceiptValidator validator;bool busy;
 public bool IsAvailable=>provider.IsAvailable&&validator!=null&&(!provider.IsMock||DevelopmentAllowed);
 static bool DevelopmentAllowed {
 get {
 #if UNITY_EDITOR || DEVELOPMENT_BUILD
 return true;
 #else
 return false;
 #endif
 }}
 public PurchaseService(GameManager g,ReleaseConfig c,IPurchaseProvider p=null,IPurchaseReceiptValidator v=null){game=g;config=c;provider=p??new UnavailablePurchaseProvider();validator=v;}
 public Task Initialize()=>provider.Initialize();public Task<StoreProduct[]> QueryProducts()=>IsAvailable?provider.QueryProducts(config.products.Where(p=>p.enabled).ToArray()):Task.FromResult(Array.Empty<StoreProduct>());
 public async Task<PurchaseResult> Purchase(string id){
  if(!IsAvailable)return new PurchaseResult{status=PurchaseStatus.NotSupported,message="구매 기능 준비 중"};if(busy)return new PurchaseResult{status=PurchaseStatus.Pending};var p=config.products.FirstOrDefault(p=>p.enabled&&p.internalId==id);if(p==null)return new PurchaseResult{status=PurchaseStatus.Failed};busy=true;
  try{var products=await QueryProducts();var store=products.FirstOrDefault(x=>x.internalId==id);if(store==null)return new PurchaseResult{status=PurchaseStatus.NotSupported};var result=await provider.Purchase(store.storeProductId);if(result.status==PurchaseStatus.Success&&!await Reconcile(result.receipt))return new PurchaseResult{status=PurchaseStatus.Pending,message="구매 검증 또는 저장을 기다리는 중"};return result;}catch{return new PurchaseResult{status=PurchaseStatus.Failed,message="구매 상태를 확인하지 못했습니다."};}finally{busy=false;}
 }
 public async Task<bool> Reconcile(PurchaseReceipt receipt){
  if(!IsAvailable||receipt==null)return false;var verified=await validator.Validate(receipt);if(verified==null||!verified.valid||verified.store!=receipt.store||verified.transactionId!=receipt.transactionId||verified.storeProductId!=receipt.storeProductId)return false;
  var product=config.products.FirstOrDefault(p=>p.enabled&&p.StoreProductId(verified.store)==verified.storeProductId);if(product==null||product.rubyAmount<=0||product.bonusRuby<0||product.firstPurchaseBonus<0)return false;
  string purchaseId=verified.store+"/"+verified.transactionId;var wallet=game.economy.Ruby;if(wallet.Capture().processedPurchaseTransactionIds.Contains(purchaseId))return true;
  if(game.State!=GameState.Playing||game.missions.IsBusy)return false;var before=game.CaptureState();long bonus=product.bonusRuby;if(!wallet.Capture().transactions.Any(t=>t.source==RubySource.Purchase&&t.relatedId==product.internalId))bonus+=product.firstPurchaseBonus;
  if(!wallet.Credit("purchase/"+purchaseId,bonus,product.rubyAmount,RubySource.Purchase,product.internalId,purchaseId))return false;
  if(!game.SaveAutomaticCheckpoint(out _)){game.RestoreState(before);return false;}return true;
 }
 // Consumables require validated server reconciliation; this hook is not a claim that Store restore works.
 public async Task<int> Restore(){if(!IsAvailable||busy)return 0;busy=true;try{int count=0;foreach(var r in await provider.Restore())if(await Reconcile(r))count++;return count;}finally{busy=false;}}
}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
public sealed class MockPurchaseProvider:IPurchaseProvider {
 public bool IsAvailable=>true;public bool IsMock=>true;public Task Initialize()=>Task.CompletedTask;
 public Task<StoreProduct[]> QueryProducts(RubyProductDefinition[] p)=>Task.FromResult(p.Select(x=>new StoreProduct{internalId=x.internalId,storeProductId=x.googleProductId,localizedName="DEV · "+x.localizedName,localizedPrice="실제 결제 아님"}).ToArray());
 public Task<PurchaseResult> Purchase(string id)=>Task.FromResult(new PurchaseResult{status=PurchaseStatus.Success,message="DEV: 실제 결제 아님",receipt=new PurchaseReceipt{store="google",storeProductId=id,transactionId="mock-"+Guid.NewGuid().ToString("N"),payload="development-only"}});
 public Task<PurchaseReceipt[]> Restore()=>Task.FromResult(Array.Empty<PurchaseReceipt>());
}
#endif
}
