#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IanGame.Editor
{
    public static class ReleaseFoundationVerification
    {
        sealed class FixtureProvider : IPurchaseProvider
        {
            public bool IsAvailable => true;
            public bool IsMock => true;

            public System.Threading.Tasks.Task Initialize() => System.Threading.Tasks.Task.CompletedTask;

            public System.Threading.Tasks.Task<StoreProduct[]> QueryProducts(RubyProductDefinition[] p) =>
                System.Threading.Tasks.Task.FromResult(Array.Empty<StoreProduct>());

            public System.Threading.Tasks.Task<PurchaseResult> Purchase(string id) =>
                System.Threading.Tasks.Task.FromResult(new PurchaseResult { status = PurchaseStatus.NotSupported });

            public System.Threading.Tasks.Task<PurchaseReceipt[]> Restore() =>
                System.Threading.Tasks.Task.FromResult(Array.Empty<PurchaseReceipt>());
        }

        sealed class FixtureValidator : IPurchaseReceiptValidator
        {
            public bool allow;

            public System.Threading.Tasks.Task<ValidatedPurchase> Validate(PurchaseReceipt p) =>
                System.Threading.Tasks.Task.FromResult(
                    new ValidatedPurchase
                    {
                        valid = allow,
                        store = p.store,
                        transactionId = p.transactionId,
                        storeProductId = p.storeProductId,
                    }
                );
        }

        static IEnumerator Shot(string name)
        {
            Directory.CreateDirectory("Docs/Release/Screenshots");
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot("Docs/Release/Screenshots/" + name + ".png");
            yield return null;
            yield return null;
        }

        static Button Named(GameObject root, string name) =>
            root.GetComponentsInChildren<Button>(true).First(b => b.name == name);

        public static IEnumerator Run(GameManager g, Action<bool, string> check, Action<GameObject, string> audit)
        {
            var config = ReleaseConfig.Current;
            check(config != null && config.documents.Length == 7, "Seven Korean legal documents imported");
            var shell = UnityEngine.Object.FindFirstObjectByType<UIShell>();
            var ui = shell.GetComponent<ReleaseUI>();
            check(ui != null, "Existing MainMenu shell owns release UI");
            PlayerPrefs.DeleteKey("ian.release.legal.acceptance");
            shell.Push(ui.ConsentPanel);
            check(LegalDocumentService.NeedsConsent, "New installation requires terms/notice acknowledgement");
            shell.Pop();
            check(shell.Top == ui.ConsentPanel, "Back cannot dismiss unacknowledged required documents");
            check(!Named(ui.ConsentPanel, "Accept required").interactable, "Unchecked required items cannot continue");
            ui.OpenDocument("terms");
            yield return Shot("First-Terms");
            shell.Pop();
            Named(ui.ConsentPanel, "Terms consent").onClick.Invoke();
            Named(ui.ConsentPanel, "Privacy acknowledgement").onClick.Invoke();
            Named(ui.ConsentPanel, "Accept required").onClick.Invoke();
            check(
                !LegalDocumentService.NeedsConsent && shell.Top != ui.ConsentPanel,
                "Actual UI acceptance persists current versions"
            );
            foreach (var d in config.documents)
            {
                ui.OpenDocument(d.documentId);
                yield return null;
                var body = ui.DocumentPanel.GetComponentsInChildren<TMP_Text>().First(t => t.name == "Legal Body");
                var scroll = ui.DocumentPanel.GetComponentInChildren<ScrollRect>();
                check(
                    body.text.Length > 100 && !body.text.Contains("LEGAL REVIEW REQUIRED"),
                    "Readable legal body without developer banner: " + d.documentId
                );
                check(
                    scroll.content.rect.height > 0 && scroll.vertical && !scroll.horizontal,
                    "Legal vertical scroll: " + d.documentId
                );
                scroll.verticalNormalizedPosition = 0;
                yield return null;
                check(
                    scroll.verticalNormalizedPosition < .1f
                        || scroll.content.rect.height <= scroll.viewport.rect.height,
                    "Legal scroll reaches bottom: " + d.documentId
                );
                shell.Pop();
                check(shell.Top != ui.DocumentPanel, "Back returns from legal: " + d.documentId);
            }

            g.NewGame();
            check(g.growth.Age == 0 && g.economy.Ruby.Total == 0, "Newborn retains original game state and zero Ruby");
            check(GameContext.Instance.transitions.Navigate(SceneId.Home), "Actual connected home scene requested");
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return null;
            yield return null;
            var birth = UnityEngine.Object.FindFirstObjectByType<BirthIntroductionPopup>();
            check(birth != null && birth.IsShowing, "Existing birth flow preserved");
            birth.begin.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.3f);
            g.clock.realtimeEnabled = false;
            var w = g.economy.Ruby;
            check(w.Credit("qa/free", 5, 0, RubySource.Event, "qa"), "Free Ruby grant");
            check(!w.Credit("qa/free", 5, 0, RubySource.Event, "qa") && w.Total == 5, "Duplicate credit ID rejected");
            check(
                w.Credit("qa/purchase", 2, 10, RubySource.Purchase, "qa-product", "google/qa-tx"),
                "Validated-path fixture records paid Ruby and free bonus separately"
            );
            check(
                !w.Credit("qa/second", 0, 10, RubySource.Purchase, "qa-product", "google/qa-tx"),
                "Duplicate Store transaction rejected"
            );
            check(
                w.Spend("qa/spend", 9, RubySource.Gacha, "qa-pool", RubySpendOrder.FreeFirst)
                    && w.Free == 0
                    && w.Paid == 8,
                "Free before paid with exact debit split"
            );
            check(
                !w.Spend("qa/too-much", 9, RubySource.Gacha, "qa-pool", RubySpendOrder.FreeFirst),
                "Insufficient Ruby cannot spend"
            );
            check(
                !w.Spend("qa/spend", 1, RubySource.Gacha, "qa-pool", RubySpendOrder.FreeFirst),
                "Duplicate spend rejected"
            );
            check(
                RubyWallet.Valid(w.Capture()) && w.PurchasedSummary().consumed == 2,
                "Ledger reconstructs balances and paid consumption"
            );
            var snapshot = w.Capture();
            snapshot.totalRuby++;
            check(!RubyWallet.Valid(snapshot), "Tampered total rejected");
            snapshot = w.Capture();
            snapshot.transactions[0].freeDelta++;
            check(!RubyWallet.Valid(snapshot), "Tampered ledger rejected");
            string error;
            check(g.SaveAutomaticCheckpoint(out error), "Ruby state saved through actual SaveManager: " + error);
            SaveData loaded;
            check(
                g.saves.TryReadAutomatic(out loaded, out error) && loaded.economy.ruby.paidRuby == 8,
                "Ruby disk read preserves split and receipts"
            );
            g.RestoreState(loaded);
            w = g.economy.Ruby;
            check(w.Total == 8 && !w.Credit("qa/free", 5, 0, RubySource.Event, "qa"), "Load does not replay a credit");
            g.NewGame();
            check(w.Total == 8, "New life preserves premium ledger; no reset-based farming");
            g.CompleteBirthIntroduction();
            var mission = g.missions.Capture();
            mission.completedMissionIds.Add("M010");
            g.missions.BeginRestore();
            g.missions.Restore(mission, 1);
            g.missions.EndRestore();
            long prior = w.Total;
            check(
                g.missions.Claim("M010") && w.Total == prior + 2,
                "Actual Mission Claim M010 awards configured 2 Ruby"
            );
            check(!g.missions.Claim("M010") && w.Total == prior + 2, "Mission duplicate claim cannot farm Ruby");
            g.growth.Restore(new ProgressSnapshot { age = 19 });
            prior = w.Total;
            g.world.City.Record("item/collection10", 10, "qa/achievement");
            check(w.Total == prior + 1, "Existing achievement service awards configured first milestone");
            g.world.City.Record("item/collection10", 10, "qa/achievement-repeat");
            check(w.Total == prior + 1, "Achievement repeated progress cannot farm Ruby");
            check(
                GachaProbabilityDisclosure.Validate(g.items.catalog) == null,
                "All normal/pity distributions sum to 100 percent"
            );
            var p = g.items.catalog.pools[0];
            var oldCurrency = p.costCurrency;
            int oldCost = p.costAmount;
            bool oldTicket = p.ticketFirst;
            try
            {
                p.costCurrency = GachaCurrency.Ruby;
                p.costAmount = 1;
                p.ticketFirst = false;
                prior = w.Total;
                GachaBatch batch;
                check(
                    g.items.Gacha.Pull("qa/ruby-gacha", p.poolId, 1, false, out batch, out error)
                        && batch.ruby == 1
                        && w.Total == prior - 1,
                    "Actual Ruby gacha spends once and grants inventory result"
                );
                check(
                    !g.items.Gacha.Pull("qa/ruby-gacha", p.poolId, 1, false, out batch, out error)
                        && w.Total == prior - 1,
                    "Repeated gacha request does not charge or grant twice"
                );
                p.costAmount = 100000;
                var quote = g.items.Gacha.Quote(p.poolId, 1);
                check(!quote.CanPull, "Ruby gacha insufficient balance blocks before random");
            }
            finally
            {
                p.costCurrency = oldCurrency;
                p.costAmount = oldCost;
                p.ticketFirst = oldTicket;
            }

            var purchase = g.purchases;
            check(
                purchase != null
                    && !purchase.IsAvailable
                    && purchase.QueryProducts().Result.Length == 0
                    && purchase.Purchase("ruby_pack_small").Result.status == PurchaseStatus.NotSupported,
                "Default provider exposes no fake product/price/success"
            );
            check(purchase.Restore().Result == 0, "Unavailable Store restore does not fabricate recovery");
            var product = config.products[0];
            bool enabled = product.enabled;
            string storeId = product.googleProductId;
            var validation = new FixtureValidator();
            try
            {
                product.enabled = true;
                product.googleProductId = "qa-only-product";
                var connected = new PurchaseService(g, config, new FixtureProvider(), validation);
                var receipt = new PurchaseReceipt
                {
                    store = "google",
                    storeProductId = product.googleProductId,
                    transactionId = "qa-reconcile",
                    payload = "QA fixture, not actual Store receipt",
                };
                prior = w.Total;
                check(
                    !connected.Reconcile(receipt).Result && w.Total == prior,
                    "Unvalidated receipt cannot grant Ruby"
                );
                validation.allow = true;
                check(
                    connected.Reconcile(receipt).Result
                        && w.Total == prior + product.rubyAmount + product.bonusRuby + product.firstPurchaseBonus,
                    "Validated test-provider reconciliation commits Ruby and save"
                );
                long after = w.Total;
                check(
                    connected.Reconcile(receipt).Result && w.Total == after,
                    "Repeated receipt reconciliation acknowledges without regrant"
                );
                check(
                    g.saves.TryReadAutomatic(out loaded, out error) && loaded.economy.ruby.totalRuby == after,
                    "Reconciled receipt and Ruby persisted in same save"
                );
                receipt.storeProductId = "wrong-product";
                check(!connected.Reconcile(receipt).Result && w.Total == after, "Unknown Store product rejected");
            }
            finally
            {
                product.enabled = enabled;
                product.googleProductId = storeId;
            }

            var legacy = g.CaptureState();
            legacy.economy.ruby = null;
            prior = w.Total;
            g.RestoreState(legacy);
            check(w.Total == prior, "Pre-Ruby legacy snapshot does not erase current premium balance");
            var current = g.world.Host;
            ui = current.hud.shell.GetComponent<ReleaseUI>();
            check(ui != null, "Release UI survives room Scene transition");
            foreach (
                var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1440, 900), new Vector2Int(904, 2316) }
            )
            {
                CommercialSliceVerification.SetSize(size.x, size.y);
                yield return new WaitForSecondsRealtime(.3f);
                ui.OpenRuby();
                yield return null;
                check(
                    !ui.ShopPanel.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("₩")),
                    "No placeholder prices: " + size
                );
                check(
                    !ui
                        .ShopPanel.GetComponentsInChildren<Button>()
                        .Any(b => b.name.Contains("Purchase") || b.name.Contains("DEV")),
                    "No cash purchase or mock buttons: " + size
                );
                audit(ui.ShopPanel, "Ruby UI " + size);
                yield return Shot("Ruby-" + size.x);
                current.hud.shell.Pop();
                ui.OpenDocument("privacy");
                yield return Shot("Privacy-" + size.x);
                var viewport = ui.DocumentPanel.GetComponentInChildren<ScrollRect>().viewport;
                check(
                    viewport.rect.width > 100 && viewport.rect.height > 100,
                    "Legal safe-area scroll usable: " + size
                );
                current.hud.shell.Pop();
            }

            CommercialSliceVerification.SetSize(1280, 720);
            var collection = current.hud.GetComponent<CollectionUI>();
            if (collection == null)
            {
                collection = current.GetComponentInChildren<CollectionUI>();
            }

            if (collection != null)
            {
                collection.OpenGacha();
                w.Credit("qa/live-ui", 1, 0, RubySource.Event, "qa");
                check(
                    collection
                        .GachaPanel.GetComponentsInChildren<TMP_Text>()
                        .Any(t =>
                            t.name == "Gacha balance" && t.text.Contains("루비 " + g.economy.Ruby.Total.ToString("N0"))
                        ),
                    "Open gacha balance refreshes immediately on Ruby ledger event"
                );
                collection.OpenProbabilities();
                yield return Shot("Gacha-Probability");
                check(
                    collection
                        .ProbabilityPanel.GetComponentsInChildren<TMP_Text>()
                        .Any(t => t.text.Contains("50") && t.text.Contains("300")),
                    "Gacha pre-pull disclosure includes actual pity rules"
                );
                current.hud.shell.Close(collection.ProbabilityPanel);
                current.hud.shell.Close(collection.GachaPanel);
            }

            check(
                g.SaveAutomaticCheckpoint(out error) && g.saves.TryReadAutomatic(out loaded, out error),
                "Final wallet/mission/achievement state written and read"
            );
            long total = w.Total;
            g.RestoreState(loaded);
            check(
                g.economy.Ruby.Total == total && g.missions.Claimed("M010"),
                "Final load retains Ruby and claimed mission together"
            );
        }
    }
}
#endif
