#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace IanGame.Editor
{
    public static class PrePhase28UIAudit
    {
        static void Close(UIShell shell)
        {
            int n = 0;
            while (shell.Depth > 0 && n++ < 20)
            {
                shell.Pop();
            }

            if (shell.Depth > 0)
            {
                throw new Exception("UI stack did not close");
            }
        }

        public static IEnumerator Run(
            LifeWorldController w,
            GameManager g,
            Action<bool, string> check,
            Action<GameObject, string> audit
        )
        {
            var qa = DeveloperQACenter.Ensure();
            qa.Open();
            qa.Commands.Fixture(30);
            qa.Minimize();
            g.clock.realtimeEnabled = false;
            System.IO.Directory.CreateDirectory("Docs/PrePhase28/Screenshots");
            try
            {
                yield return qa.Commands.Navigate(SceneId.Home);
                w = g.world.Host;
                Close(w.hud.shell);
                w.Player.Warp(w.CurrentZone.origin + new Vector2(0, -2));
                var start = w.Player.Position;
                w.Player.BeginTraversal(start + Vector2.right * .6f);
                bool walked = false;
                float until = Time.unscaledTime + .5f;
                while (Time.unscaledTime < until)
                {
                    walked |= w.Player.advanced.Motion == ActorMotion.Walk;
                    yield return null;
                }

                check(
                    Vector2.Distance(start, w.Player.Position) > .4f && walked,
                    "Adult real motor movement and Walk preserved"
                );
                w.Player.CancelTraversal();
                foreach (
                    var size in new[]
                    {
                        new[] { 1280, 720 },
                        new[] { 1920, 1080 },
                        new[] { 904, 2316 },
                        new[] { 1840, 2208 },
                    }
                )
                {
                    CommercialSliceVerification.SetSize(size[0], size[1]);
                    yield return new WaitForSecondsRealtime(.2f);
                    for (int i = 0; i < w.hud.menus.Length; i++)
                    {
                        Close(w.hud.shell);
                        w.hud.menus[i].onClick.Invoke();
                        yield return new WaitForSecondsRealtime(.1f);
                        check(w.hud.shell.Depth > 0, "Actual menu button " + i + " / " + size[0]);
                        audit(w.hud.shell.Top, "Main menu " + i + " / " + size[0]);
                        Close(w.hud.shell);
                        check(w.CanControl, "Menu close restores control " + i);
                    }

                    var property = UnityEngine.Object.FindFirstObjectByType<PropertyPresenter>();
                    property.Open();
                    for (int tab = 0; tab < property.tabs.Length; tab++)
                    {
                        property.tabs[tab].onClick.Invoke();
                        yield return null;
                        check(property.Page == tab, "Property tab button " + tab);
                        audit(property.panel, "Property tab " + tab + " / " + size[0]);
                    }

                    ScreenCapture.CaptureScreenshot("Docs/PrePhase28/Screenshots/Property-" + size[0] + ".png");
                    yield return null;
                    Close(w.hud.shell);
                }

                CommercialSliceVerification.SetSize(1280, 720);
                var p = UnityEngine.Object.FindFirstObjectByType<PropertyPresenter>();
                p.Open();
                p.selections[0].onClick.Invoke();
                long cash = g.economy.Cash;
                string id = g.economy.propertyCatalog.properties[0].PropertyId;
                int price = g.economy.GetPropertyQuote(id).MarketPrice;
                p.actions[0].onClick.Invoke();
                yield return null;
                check(p.shell.Top == p.confirmation, "Buy button opens real confirmation");
                p.accept.onClick.Invoke();
                yield return null;
                check(
                    g.economy.Cash == cash - price && g.economy.Properties.owned.Any(h => h.PropertyId == id),
                    "UI confirm buys and deducts exact cash"
                );
                p.accept.onClick.Invoke();
                check(
                    g.economy.Properties.owned.Count(h => h.PropertyId == id) == 1,
                    "Repeated confirm cannot duplicate purchase"
                );
                p.tabs[1].onClick.Invoke();
                p.actions[1].onClick.Invoke();
                yield return null;
                p.accept.onClick.Invoke();
                yield return null;
                check(
                    g.economy.Cash == cash && !g.economy.Properties.owned.Any(h => h.PropertyId == id),
                    "UI sale restores same-price cash and removes ownership"
                );
                Close(p.shell);
                var f = UnityEngine.Object.FindFirstObjectByType<FinancePresenter>();
                qa.Commands.SetDebt(10000);
                f.OpenEconomy();
                f.payment.text = "1000";
                long debt = g.economy.Debt;
                cash = g.economy.Cash;
                f.repay.onClick.Invoke();
                yield return null;
                check(
                    g.economy.Debt == debt - 1000 && g.economy.Cash == cash - 1000,
                    "Actual repayment button updates cash and debt"
                );
                Close(f.shell);
                yield return qa.Commands.Navigate(SceneId.StockMarket, true);
                var market = UnityEngine
                    .Object.FindObjectsByType<FinancePresenter>(FindObjectsSortMode.None)
                    .First(x => x.marketScene);
                market.quantity.text = "1";
                yield return null;
                cash = g.economy.Cash;
                price = g.economy.GetQuote("IAN").CurrentPrice;
                market.buy.onClick.Invoke();
                yield return null;
                check(g.economy.Cash == cash - price, "Actual stock buy button");
                market.sell.onClick.Invoke();
                yield return null;
                check(g.economy.Cash == cash, "Actual stock sell button");
                for (int i = 0; i < market.tabs.Length; i++)
                {
                    market.tabs[i].onClick.Invoke();
                    yield return null;
                    audit(market.marketPanel, "Stock tab " + i);
                }

                ScreenCapture.CaptureScreenshot("Docs/PrePhase28/Screenshots/Stock.png");
                yield return null;
                market.marketClose.onClick.Invoke();
                while (GameContext.Instance.transitions.IsTransitioning)
                {
                    yield return null;
                }

                check(
                    GameContext.Instance.transitions.CurrentScene != SceneId.StockMarket,
                    "Stock close button returns to world"
                );
            }
            finally
            {
                CommercialSliceVerification.SetSize(1280, 720);
            }

            yield return qa.CloseAndRestore();
            check(!qa.Sandbox.Active && qa.Sandbox.LastFilesUnchanged, "UI audit restores sandbox and original files");
        }
    }
}
#endif
