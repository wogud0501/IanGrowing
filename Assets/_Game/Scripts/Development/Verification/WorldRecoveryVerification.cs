#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class WorldRecoveryVerification
    {
        const string Shots = "Docs/WorldRecovery/Screenshots/";

        static IEnumerator Settle(float seconds = .35f)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        static void Shot(string name)
        {
            System.IO.Directory.CreateDirectory(Shots);
            ScreenCapture.CaptureScreenshot(Shots + name + ".png");
        }

        static IEnumerator Go(LifeWorldController w, string zone)
        {
            if (w.Service.State.zone != zone)
            {
                if (!w.Travel(zone))
                {
                    throw new Exception("Cannot travel to " + zone);
                }

                float timeout = Time.unscaledTime + 5;
                while (w.Busy && Time.unscaledTime < timeout)
                {
                    yield return null;
                }

                if (w.Busy)
                {
                    throw new Exception("Travel timeout " + zone);
                }
            }

            yield return Settle();
        }

        static IEnumerator Use(LifeWorldController w, string id, Action<bool, string> check)
        {
            var o = w.objects.First(x => x.id == id);
            w.Player.Warp(o.Point);
            w.follow.Snap();
            yield return Settle(.15f);
            check(w.Use(o), "Existing interaction accepts " + id);
            check(!w.Use(o), "Duplicate interaction blocked " + id);
            float end = Time.unscaledTime + 6;
            while (w.Busy && Time.unscaledTime < end)
            {
                yield return null;
            }

            check(!w.Busy && w.Service.State.zone == o.targetZone, "Door reaches correct destination " + id);
            check(w.CanControl, "Control restored " + id);
            yield return Settle();
        }

        public static IEnumerator Safety(LifeWorldController w, GameManager g, Action<bool, string> check)
        {
            check(
                UnityEditor.SessionState.GetBool("ian.phase26.protectWrites", false),
                "Editor test write guard is active before scenario execution"
            );
            check(
                !g.autoLoadOnStart && g.saves.BasePath.Contains("phase26-"),
                "Startup is isolated before original load/lifecycle writes"
            );
            var channel = g.saves.EnterTestingChannel("qa-guard-probe.json");
            try
            {
                g.saves.RestoreTestingChannel(new SaveManager.TestingChannel { slot = SaveManager.CurrentSlot });
                check(
                    !g.SaveAutomaticCheckpoint(out var reason) && reason.Contains("자동 검증"),
                    "Final SaveManager write boundary rejects production automatic saves during tests"
                );
            }
            finally
            {
                g.saves.RestoreTestingChannel(channel);
            }

            var holdChannel = g.saves.EnterTestingChannel("qa-guard-latch-probe.json");
            bool wasProtected = UnityEditor.SessionState.GetBool("ian.phase26.protectWrites", false);
            try
            {
                UnityEditor.SessionState.SetBool("ian.phase26.protectWrites", false);
                g.saves.RestoreTestingChannel(new SaveManager.TestingChannel { slot = SaveManager.CurrentSlot });
                check(
                    !g.SaveAutomaticCheckpoint(out var heldReason) && heldReason.Contains("자동 검증"),
                    "Automation latch blocks real production write even when transient guard is cleared"
                );
            }
            finally
            {
                g.saves.RestoreTestingChannel(holdChannel);
                UnityEditor.SessionState.SetBool("ian.phase26.protectWrites", wasProtected);
            }

            check(g.SaveAutomaticCheckpoint(out _), "Isolated automatic save still succeeds");
            check(System.IO.File.Exists(g.saves.AutomaticSavePath), "Test checkpoint uses its separate disk slot");
            yield return null;
        }

        public static IEnumerator Polish(
            LifeWorldController w,
            GameManager g,
            Action<bool, string> check,
            Action<GameObject, string> audit
        )
        {
            g.clock.Restore(
                g.clock.Day,
                new TimeSnapshot { subDaySeconds = 10 * 3600 + 20 * 60, elapsedSeconds = g.clock.ElapsedSeconds }
            );
            yield return Settle();
            yield return Go(w, "workplace");
            w.Player.Warp(new Vector2(223, -4.5f));
            w.follow.Snap();
            yield return Settle(2.4f);
            WorldRecoveryChecks.Layout(w, check);
            WorldRecoveryChecks.DoorLease(w, check);
            var floor = w
                .transform.Cast<Transform>()
                .First(t => t.name.EndsWith(" / workplace"))
                .GetComponentsInChildren<SpriteRenderer>(true)
                .First(r => r.name == "Walkable floor");
            check(
                floor.bounds.size.x > 17.9f && floor.bounds.size.y > 11.9f,
                "Office floor covers the entire room at consistent pixel scale"
            );
            Shot("01-Workspace-Working-Final");
            yield return Settle();
            audit(w.hud.gameObject, "Final Workspace HUD");
            var scheduler = w.GetComponent<WorldNPCScheduler>();
            var colleague = scheduler.actors.First(a => a.npcId == "colleague");
            g.clock.Restore(
                g.clock.Day,
                new TimeSnapshot { subDaySeconds = 10 * 3600 + 60, elapsedSeconds = g.clock.ElapsedSeconds }
            );
            yield return Settle(.35f);
            Vector2 breakFrom = colleague.transform.position;
            float end = Time.unscaledTime + 25;
            var breakSlot = w.objects.First(o => o.id == "office-break-0").GetComponent<WorldActivitySlot>();
            bool walked = false;
            var sprites = new HashSet<Sprite>();
            while (Time.unscaledTime < end)
            {
                var motion = colleague.GetComponent<WorldActorAnimation>();
                walked |= motion.Motion == ActorMotion.Walk;
                sprites.Add(colleague.visual.sprite);
                if (
                    Vector2.Distance(colleague.transform.position, breakSlot.Point) < .2f
                    && motion.Motion == ActorMotion.SitDown
                )
                {
                    break;
                }

                yield return null;
            }

            check(
                walked && sprites.Count >= 2 && Vector2.Distance(breakFrom, colleague.transform.position) > 2,
                "Scheduled staff break uses real visible walking"
            );
            check(
                Vector2.Distance(colleague.transform.position, breakSlot.Point) < .2f
                    && breakSlot.Occupant == "colleague",
                "Employee arrives at a reserved indoor lounge seat"
            );
            yield return Settle(.8f);
            check(
                colleague.GetComponent<WorldActorAnimation>().Motion == ActorMotion.SitDown,
                "Rest displays the authored seated pose"
            );
            Shot("02-Workspace-Staff-Break-Final");
            yield return Settle();
            g.clock.Restore(
                g.clock.Day,
                new TimeSnapshot { subDaySeconds = 10 * 3600 + 16 * 60, elapsedSeconds = g.clock.ElapsedSeconds }
            );
            float standDeadline = Time.unscaledTime + 1;
            bool stood = false;
            while (Time.unscaledTime < standDeadline)
            {
                if (colleague.GetComponent<WorldActorAnimation>().Motion == ActorMotion.StandUp)
                {
                    stood = true;
                    break;
                }

                yield return null;
            }

            check(stood, "Leaving rest plays the authored standing transition");
            yield return Settle(.6f);
            check(breakSlot.Occupant != "colleague", "Work resumption releases the lounge seat");
            foreach (var size in new[] { new[] { 1280, 720 }, new[] { 1920, 1080 } })
            {
                CommercialSliceVerification.SetSize(size[0], size[1]);
                yield return Settle(.4f);
                audit(w.hud.gameObject, "Final HUD " + size[0]);
                Shot("HUD-Final-" + size[0]);
                yield return Settle();
                for (int n = 0; n < 7; n++)
                {
                    w.hud.navigation.Open(n);
                    yield return Settle(.2f);
                    audit(w.hud.navigation.panels[n], "Final menu " + n + " / " + size[0]);
                    if (size[0] == 1280)
                    {
                        Shot("Menu-" + n);
                    }

                    yield return null;
                    while (w.hud.shell.Depth > 0)
                    {
                        w.hud.shell.Pop();
                    }
                }
            }

            CommercialSliceVerification.SetSize(1280, 720);
            yield return Use(w, "work-exit", check);
            yield return Go(w, "workplace");
            var reversePortal = w.objects.First(o => o.id == "work-exit");
            var reverse = reversePortal.GetComponent<WorldDoor>();
            reverse.ResetClosed();
            var fixedRoot = reverse.transform.position;
            var fixedFrame = reverse.frame.transform.position;
            w.Player.Warp(reverse.Plane + reverse.normal * .35f);
            yield return Settle(.2f);
            check(w.Use(reversePortal), "Reverse-side door interaction accepted");
            var apertureFrames = new HashSet<Sprite>();
            float reverseEnd = Time.unscaledTime + 6;
            while (w.Busy && Time.unscaledTime < reverseEnd)
            {
                apertureFrames.Add(reverse.leaf.sprite);
                yield return null;
            }

            check(
                !w.Busy && g.world.State.zone == "city" && apertureFrames.Count >= 3,
                "Reverse-side passage plays actual aperture frames and commits destination"
            );
            check(
                reverse.transform.position == fixedRoot && reverse.frame.transform.position == fixedFrame,
                "Reverse-side passage leaves root and frame fixed"
            );
            yield return Go(w, "city");
            w.Player.Warp(g.world.rules.Zone("city").origin + new Vector2(0, -.7f));
            w.follow.Snap();
            yield return Settle(1);
            Shot("03-City-Facilities-Final");
            yield return Settle();
            yield return Use(w, "finance-front", check);
            Shot("04-Finance-Interior-Final");
            yield return Settle();
            var terminal = w.objects.First(o => o.id == "stock-terminal");
            w.Player.Warp(terminal.Point);
            yield return Settle(.15f);
            check(w.Use(terminal), "Final terminal opens market");
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return Settle(.4f);
            var finance = UnityEngine.Object.FindFirstObjectByType<FinancePresenter>();
            audit(finance.gameObject, "Final investment UI");
            Shot("05-Investment-UI-Final");
            yield return Settle();
            finance.marketClose.onClick.Invoke();
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return Settle(.4f);
            w = UnityEngine.Object.FindFirstObjectByType<LifeWorldController>();
            yield return Use(w, "finance-exit", check);
            yield return Use(w, "realestate-front", check);
            Shot("06-Agency-Interior-Final");
            yield return Settle();
            var agency = w.objects.First(o => o.id == "property-agency");
            w.Player.Warp(agency.Point);
            yield return Settle(.2f);
            check(w.Use(agency), "Final agency opens property UI");
            yield return Settle(.4f);
            var property = UnityEngine.Object.FindFirstObjectByType<PropertyPresenter>();
            for (int page = 0; page < 6; page++)
            {
                property.Tab(page);
                yield return Settle(.2f);
                audit(property.panel, "Final property page " + page);
                Shot("Property-Page-" + page);
                yield return null;
            }

            while (w.hud.shell.Depth > 0)
            {
                w.hud.shell.Pop();
            }

            yield return AdvancedWorldVerification.Mobile(w, g, check, audit);
            var mobile = w.hud.GetComponent<WorldMobileHUD>();
            mobile.simulateMobile = true;
            try
            {
                foreach (var size in new[] { new[] { 904, 2316 }, new[] { 1840, 2208 }, new[] { 1920, 1080 } })
                {
                    CommercialSliceVerification.SetSize(size[0], size[1]);
                    yield return Settle(.25f);
                    mobile.Apply(true);
                    property.Open();
                    yield return Settle(.4f);
                    audit(property.panel, "Final property responsive " + size[0]);
                    var dock = property.actions[0].transform.parent as RectTransform;
                    var bounds = new Vector3[4];
                    dock.GetWorldCorners(bounds);
                    foreach (var button in property.actions.Where(b => b.gameObject.activeSelf))
                    {
                        var corners = new Vector3[4];
                        ((RectTransform)button.transform).GetWorldCorners(corners);
                        check(
                            corners.All(p =>
                                p.x >= bounds[0].x - 1
                                && p.x <= bounds[2].x + 1
                                && p.y >= bounds[0].y - 1
                                && p.y <= bounds[2].y + 1
                            ),
                            "Complete transaction button inside adaptive dock: " + button.name + " / " + size[0]
                        );
                    }

                    Shot("Property-Responsive-" + size[0]);
                    yield return Settle();
                    while (w.hud.shell.Depth > 0)
                    {
                        w.hud.shell.Pop();
                    }
                }
            }
            finally
            {
                mobile.simulateMobile = false;
                CommercialSliceVerification.SetSize(1280, 720);
                mobile.Apply(true);
            }
        }

        public static IEnumerator Run(
            LifeWorldController w,
            GameManager g,
            Action<bool, string> check,
            Action<GameObject, string> audit
        )
        {
            check(g.saves.BasePath.Contains("phase26-"), "Test startup isolated from original player saves");
            check(
                w.objects.Where(o => o.zone == "home" && StarterHomePresentation.Optional(o.id))
                    .All(o => !o.gameObject.activeSelf),
                "New home has no free optional furniture"
            );
            var defs = g
                .world.rules.npcs.Where(d =>
                    d.schedule.Any(e => e.zone == "workplace" && e.action == NPCDailyAction.Work)
                )
                .ToArray();
            check(defs.Length == 9, "All nine employee records retained");
            var slots = w.GetComponentsInChildren<WorldActivitySlot>()
                .Where(s => !string.IsNullOrEmpty(s.staffId))
                .ToArray();
            check(
                slots.Length == 9 && slots.Select(s => s.staffId).Distinct().Count() == 9,
                "Nine unique assigned workstations"
            );
            foreach (var d in defs)
            {
                var work = d.schedule.Where(e => e.zone == "workplace" && e.action == NPCDailyAction.Work);
                check(work.All(e => e.objectId == "staff-desk-" + d.npcId), "Schedule owns a real desk: " + d.npcId);
            }

            foreach (var s in slots)
            {
                check(
                    s.TryReserve(s.staffId) && !s.TryReserve("intruder"),
                    "Seat rejects simultaneous occupancy " + s.staffId
                );
                s.Release(s.staffId);
            }

            check(
                w.objects.First(o => o.id == "work-rest").visual.sprite.name == "Settee",
                "Office uses indoor sofa instead of park bench"
            );
            check(
                w.objects.First(o => o.id == "work-water").visual.sprite.name == "KitchenSink",
                "Pantry has kitchen equipment instead of exposed bathroom basin"
            );
            g.clock.Restore(
                g.clock.Day,
                new TimeSnapshot { subDaySeconds = 10 * 3600 + 20 * 60, elapsedSeconds = g.clock.ElapsedSeconds }
            );
            yield return Settle();
            yield return Go(w, "workplace");
            w.Player.Warp(new Vector2(223, -4.5f));
            w.follow.Snap();
            yield return Settle(.6f);
            var scheduler = w.GetComponent<WorldNPCScheduler>();
            scheduler.Capture();
            var active = scheduler.actors.Where(a => a.zone == "workplace" && a.visual.enabled).ToArray();
            check(active.Length >= 5, "Workday presents scheduled employees");
            foreach (var a in active)
            {
                foreach (var b in active.Where(b => string.CompareOrdinal(a.npcId, b.npcId) < 0))
                {
                    check(
                        Vector2.Distance(a.transform.position, b.transform.position) > .65f,
                        "Employees do not overlap: " + a.npcId + " / " + b.npcId
                    );
                }
            }

            check(
                active.All(a =>
                    g.world.State.residents.First(s => s.id == a.npcId).objectId == "staff-desk-" + a.npcId
                    || g.world.State.residents.First(s => s.id == a.npcId).objectId.StartsWith("office-break-")
                ),
                "Visible staff use their assigned destination"
            );
            Shot("01-Workspace-Working");
            yield return Settle();
            audit(w.hud.gameObject, "Workspace HUD 1280x720");
            // Seed one saved walking state, then observe the real navigation/motion system.
            var actor = active.First(a => a.npcId == "colleague");
            scheduler.Capture();
            var saved = g.world.State.residents.First(s => s.id == actor.npcId);
            saved.action = NPCDailyAction.Walk;
            saved.position = new Vector2(-4.7f, -.1f);
            saved.minute = scheduler.Minute;
            scheduler.Restore();
            yield return Settle(.15f);
            Vector2 before = actor.transform.position;
            var frames = new HashSet<Sprite>();
            float until = Time.unscaledTime + 7;
            var animation = actor.GetComponent<WorldActorAnimation>();
            bool walked = false;
            while (Time.unscaledTime < until)
            {
                frames.Add(actor.visual.sprite);
                if (animation.Motion == ActorMotion.Walk)
                {
                    walked = true;
                }

                if (animation.Motion == ActorMotion.UseComputer)
                {
                    break;
                }

                yield return null;
            }

            check(
                Vector2.Distance(before, actor.transform.position) > .4f && walked && frames.Count >= 2,
                "Employee physically navigates and advances walking frames"
            );
            check(animation.Motion == ActorMotion.UseComputer, "Arrival transitions through sitting to computer work");
            Shot("02-Workspace-Lounge");
            yield return Settle();
            var guard = w.objects.First(o => o.id == "bath-door").GetComponent<WorldDoor>();
            guard.enabled = false;
            var root = guard.transform.position;
            var framePosition = guard.frame.transform.position;
            var hinge = guard.leaf.transform.position;
            check(
                guard.SideOf(guard.Plane + guard.normal) != guard.SideOf(guard.Plane - guard.normal),
                "Front and back approach resolve opposite sides in door coordinates"
            );
            foreach (float dt in new[] { 1f / 30, 1f / 60, .11f })
            {
                for (int i = 0; i < 20; i++)
                {
                    guard.ResetClosed();
                    check(guard.Acquire("qa-owner", 12, out _), "Acquire real door cycle " + i + " dt=" + dt);
                    guard.AdvanceAnimation(dt);
                    guard.locked = true;
                    check(
                        !guard.Acquire("denied", 12, out _) && guard.State == DoorState.Opening,
                        "Denied request does not freeze active opening"
                    );
                    guard.locked = false;
                    for (int n = 0; n < 100 && guard.State != DoorState.Open; n++)
                    {
                        guard.AdvanceAnimation(dt);
                    }

                    check(
                        guard.State == DoorState.Open && !guard.closedCollider.enabled,
                        "Opened leaf permits passage"
                    );
                    guard.Release("qa-owner");
                    yield return Settle(.14f);
                    guard.AdvanceAnimation(dt);
                    check(
                        guard.State == DoorState.Closing && !guard.closedCollider.enabled,
                        "Closing does not activate pushing collider"
                    );
                    for (int n = 0; n < 100 && guard.State != DoorState.Closed; n++)
                    {
                        guard.AdvanceAnimation(dt);
                    }

                    check(
                        guard.State == DoorState.Closed && guard.closedCollider.enabled,
                        "Closed latch exactly restores collision"
                    );
                    check(
                        guard.transform.position == root
                            && guard.frame.transform.position == framePosition
                            && guard.leaf.transform.position == hinge,
                        "Root, frame and hinge remain fixed throughout cycle"
                    );
                }
            }

            guard.enabled = true;
            yield return Use(w, "work-exit", check);
            Shot("03-City-Facilities");
            yield return Settle();
            check(
                w.objects.First(o => o.id == "stock-terminal").zone == "finance"
                    && w.objects.First(o => o.id == "property-agency").zone == "realestate",
                "Both financial terminals are indoors with existing functional IDs"
            );
            yield return Use(w, "finance-front", check);
            Shot("04-Finance-Interior");
            yield return Settle();
            var terminal = w.objects.First(o => o.id == "stock-terminal");
            w.Player.Warp(terminal.Point);
            yield return Settle(.15f);
            check(w.Use(terminal), "Indoor terminal opens existing stock market");
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return Settle(.4f);
            var finance = UnityEngine.Object.FindFirstObjectByType<FinancePresenter>();
            check(finance != null && finance.marketScene, "Existing investment UI remains reachable");
            audit(finance.gameObject, "Investment UI");
            Shot("05-Investment-UI");
            yield return Settle();
            finance.marketClose.onClick.Invoke();
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return Settle(.4f);
            w = UnityEngine.Object.FindFirstObjectByType<LifeWorldController>();
            check(w != null && g.world.State.zone == "finance", "Closing investment returns to facility interior");
            yield return Use(w, "finance-exit", check);
            yield return Use(w, "realestate-front", check);
            Shot("06-Agency-Interior");
            yield return Settle();
            var agency = w.objects.First(o => o.id == "property-agency");
            w.Player.Warp(agency.Point);
            yield return Settle(.15f);
            check(w.Use(agency), "Indoor agency opens existing property UI");
            yield return Settle(.4f);
            var property = UnityEngine.Object.FindFirstObjectByType<PropertyPresenter>();
            audit(property.gameObject, "Property UI");
            Shot("07-Property-UI");
            yield return Settle();
            while (w.hud.shell.Depth > 0)
            {
                w.hud.shell.Pop();
            }

            yield return Use(w, "realestate-exit", check);
            yield return Use(w, "office-front", check);
            yield return Use(w, "office-bath-front", check);
            Shot("08-Office-Bathroom");
            yield return Settle();
            yield return Use(w, "office-bath-exit", check);
            yield return Use(w, "work-exit", check);
            yield return Use(w, "school-front", check);
            yield return Use(w, "school-exit", check);
            yield return Use(w, "shop-front", check);
            yield return Use(w, "shop-exit", check);
            foreach (var size in new[] { new[] { 1920, 1080 }, new[] { 1280, 720 } })
            {
                CommercialSliceVerification.SetSize(size[0], size[1]);
                yield return Settle(.3f);
                audit(w.hud.gameObject, "Recovery HUD " + size[0]);
                Shot("09-HUD-" + size[0]);
                yield return Settle();
            }

            yield return AdvancedWorldVerification.Mobile(w, g, check, audit);
            // Save/load uses the isolated slot and the existing complete SaveManager.
            var snapshot = g.CaptureState();
            string json = JsonUtility.ToJson(snapshot);
            g.RestoreState(JsonUtility.FromJson<SaveData>(json));
            check(
                g.economy.Cash == snapshot.stats.money && g.world.State.zone == snapshot.world.zone,
                "Full save snapshot restores cash and facility zone"
            );
        }
    }
}
#endif
