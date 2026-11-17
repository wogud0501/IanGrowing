#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace IanGame.Editor
{
    public static class FacilityTerminalVerification
    {
        public static IEnumerator CompanyShift(LifeWorldController w, GameManager g, Action<bool, string> check)
        {
            g.growth.Restore(new ProgressSnapshot { age = 19 });
            g.phase23.Progress.employment.currentJobId = "convenience";
            g.clock.realtimeEnabled = false;
            g.clock.Restore(1, new TimeSnapshot { subDaySeconds = 9 * 3600 + 30 * 60 });
            check(w.Travel("workplace"), "Qualified employee enters preserved office");
            yield return FacilityQualityVerification.Wait(w);
            var desk = w.objects.First(o => o.id == "workdesk");
            w.Player.Warp(desk.Point);
            yield return null;
            long cash = g.economy.Cash;
            int count = g.phase23.Progress.employment.workCount;
            g.world.rules.activityAnimationScale = .03f;
            check(w.Begin(desk, 0), "Original company desk starts real shift");
            yield return FacilityQualityVerification.Wait(w);
            check(
                g.economy.Cash > cash && g.phase23.Progress.employment.workCount == count + 1,
                "Real shift records salary and one completed work day"
            );
            cash = g.economy.Cash;
            check(!w.Begin(desk, 0) && g.economy.Cash == cash, "Duplicate same-day company shift cannot pay again");
            check(w.CanControl, "Company shift restores control");
        }

        public static IEnumerator Run(
            LifeWorldController w,
            GameManager g,
            Action<bool, string> check,
            Action<GameObject, string> audit
        )
        {
            g.growth.Restore(new ProgressSnapshot { age = 19 });
            g.clock.realtimeEnabled = false;
            check(w.Travel("finance"), "Reach existing finance interior");
            yield return FacilityQualityVerification.Wait(w);
            var terminal = w.objects.First(o => o.id == "stock-terminal");
            w.Player.Warp(terminal.Point);
            yield return null;
            check(w.Use(terminal), "Indoor terminal opens original market scene");
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(.4f);
            var finance = UnityEngine
                .Object.FindObjectsByType<FinancePresenter>(FindObjectsSortMode.None)
                .First(f => f.marketScene);
            check(finance.marketPanel.activeInHierarchy, "Original stock UI visible from new interior");
            audit(finance.marketPanel, "Facility stock terminal");
            ScreenCapture.CaptureScreenshot("Docs/FacilityStandardization/Screenshots/Stock-UI.png");
            yield return null;
            finance.marketClose.onClick.Invoke();
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(.4f);
            w = g.world.Host;
            check(
                w != null && w.CurrentZone.id == "finance" && w.CanControl,
                "Stock close returns to finance interior and control"
            );
            check(w.Travel("realestate"), "Reach existing agency interior");
            yield return FacilityQualityVerification.Wait(w);
            var agency = w.objects.First(o => o.id == "property-agency");
            w.Player.Warp(agency.Point);
            yield return null;
            check(w.Use(agency), "Indoor agency opens real property UI");
            yield return new WaitForSecondsRealtime(.3f);
            var property = UnityEngine.Object.FindAnyObjectByType<PropertyPresenter>();
            check(property.panel.activeInHierarchy, "Original property listings visible");
            for (int i = 0; i < property.tabs.Length; i++)
            {
                property.tabs[i].onClick.Invoke();
                yield return null;
                audit(property.panel, "Agency tab " + i);
            }

            ScreenCapture.CaptureScreenshot("Docs/FacilityStandardization/Screenshots/Property-UI.png");
            yield return null;
            while (w.hud.shell.Depth > 0)
            {
                w.hud.shell.Pop();
            }

            check(w.CanControl, "Agency menu close restores movement");
        }
    }
}
#endif
