#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IanGame.Editor
{
    public static class GrowthVerification
    {
        static LifeWorldController W => GameContext.Instance.game.world.Host;
        static SceneTransitionService T => GameContext.Instance.transitions;

        static IEnumerator Wait()
        {
            float end = Time.unscaledTime + 30;
            while ((T.IsTransitioning || W.Busy) && Time.unscaledTime < end)
            {
                yield return null;
            }

            if (T.IsTransitioning || W.Busy)
            {
                throw new Exception("Growth scenario timeout");
            }

            yield return null;
            yield return null;
        }

        static IEnumerator Go(string id, Action<bool, string> check)
        {
            check(T.NavigateRoom(id, T.roomCatalog.rules.Zone(id).spawn, Vector2.down), "Request actual room " + id);
            yield return Wait();
            check(W.Room.roomId == id, "Actual active room " + id);
        }

        static IEnumerator Age(GameManager g, int age)
        {
            g.growth.Restore(new ProgressSnapshot { age = age });
            yield return null;
            yield return null;
            W.Player.growthVisual.RefreshAge();
        }

        static IEnumerator Shot(string name)
        {
            Directory.CreateDirectory("Docs/AgeGrowth/Screenshots");
            W.hud.Notify("", 2);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Docs/AgeGrowth/Screenshots/" + name + ".png");
            yield return null;
        }

        public static IEnumerator Run(
            GameManager g,
            Action<bool, string> check,
            Action<GameObject, string> audit,
            string mode
        )
        {
            g.clock.realtimeEnabled = false;
            g.clock.Restore(4, new TimeSnapshot { subDaySeconds = 14 * 3600 });
            check(W.Room.roomId == "family-ian-bedroom", "Actual newborn begins in separate family nursery");
            var catalog = W.Player.growthVisual?.catalog;
            check(
                catalog != null && catalog.profiles.Length == 9,
                "Nine authored visual profiles bound to real player"
            );
            if (mode == "roomVisualGrowthCare")
            {
                yield return Care(g, check);
                yield break;
            }

            if (mode == "roomVisualGrowthBed")
            {
                yield return BedReadback(g, check);
                yield break;
            }

            if (mode == "roomVisualGrowthPreview")
            {
                yield return Go("family-living", check);
                yield return Preview(catalog);
                yield break;
            }

            if (mode == "roomVisualGrowthDoors")
            {
                yield return Doors(g, check);
                yield break;
            }

            if (mode == "roomVisualGrowthMeals")
            {
                yield return Meals(g, check);
                yield break;
            }

            if (mode == "roomVisualGrowthLife")
            {
                yield return Life(g, check, audit);
                yield break;
            }

            if (mode == "roomVisualGrowthQA")
            {
                yield return QACenterVerification.Run(W, g, check, audit, false, new[] { "growth/visual-boundaries" });
                yield break;
            }

            if (mode == "roomVisualGrowthSave")
            {
                yield return Save(g, check);
                yield break;
            }

            float constantScale = W.Player.visualScale;
            var rows = new List<string>();
            var textures = new HashSet<Texture2D>();
            int[] ages = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 13, 14, 16, 17, 18, 19 };
            foreach (int age in ages)
            {
                yield return Age(g, age);
                var p = W.Player.growthVisual.Current;
                var expected = catalog.AtAge(age);
                check(
                    p == expected && W.Player.advanced.library == p.animations,
                    "Canonical age selects live profile " + age
                );
                check(
                    W.Player.MovementAllowed == (age >= 1)
                        && g.growth.IndependentOutdoorUnlocked == (age >= 7)
                        && g.growth.EmploymentUnlocked == (age >= 19),
                    "Walking/outdoor/employment permissions remain independent " + age
                );
                check(
                    W.Player.bodyVisual.localScale == Vector3.one * constantScale,
                    "No age-based Transform scaling " + age
                );
                check(
                    Mathf.Abs(W.Player.GetComponent<CircleCollider2D>().radius - p.colliderRadius) < .0001f,
                    "Live collider follows age " + age
                );
                check(
                    W.Player.sprite.enabled
                        && W.Player.sprite.sprite != null
                        && W.Player.sprite.sprite.texture == p.idlePreview.texture,
                    "Real player renders this age's own texture " + age
                );
                check(!new ResidenceAccess(g).IsActiveResidence, "No forced independence even at age " + age);
                textures.Add(p.idlePreview.texture);
                rows.Add(
                    age
                        + " "
                        + p.id
                        + " radius="
                        + p.colliderRadius
                        + " spriteHeight="
                        + W.Player.sprite.bounds.size.y
                        + " texture="
                        + p.idlePreview.texture.name
                );
                if (age == 0)
                {
                    check(
                        p.animations.Find(ActorMotion.Walk, ActorDirection.Down, false) == null
                            && !W.Player.MovementAllowed,
                        "Newborn has no authored adult walk"
                    );
                    var at = W.Player.Position;
                    W.Player.BeginTraversal(at + Vector2.right);
                    yield return new WaitForSecondsRealtime(.2f);
                    check(
                        W.Player.Position == at && !W.Player.IsTraversing,
                        "Newborn actual Rigidbody traversal rejected"
                    );
                    var needs = g.stats.Capture();
                    needs.hunger = 10;
                    g.stats.Restore(needs);
                    yield return null;
                    yield return null;
                    check(
                        W.Player.advanced.Motion == ActorMotion.InfantCry && !W.Player.advanced.UsedMotionFallback,
                        "Newborn needs select actual infant cry art"
                    );
                    yield return Shot("Newborn-Cry");
                    needs.hunger = 100;
                    g.stats.Restore(needs);
                    yield return null;
                    yield return null;
                }
                else
                {
                    W.Player.Warp(W.CurrentZone.origin + new Vector2(1, -1));
                    W.Player.Pose("Idle");
                }

                W.follow.Snap();
                yield return Shot("Age-" + age.ToString("00") + "-" + p.id);
            }

            check(textures.Count == 9, "Nine distinct texture sources, adult source retained");
            File.WriteAllLines("Docs/AgeGrowth/AGE_METRICS.txt", rows);
            foreach (int age in new[] { 1, 3, 5, 8, 11, 14, 17, 19 })
            {
                yield return Age(g, age);
                var actor = W.Player;
                var collider = actor.GetComponent<CircleCollider2D>();
                bool enabled = collider.enabled;
                collider.enabled = false;
                foreach (var direction in new[] { Vector2.down, Vector2.up, Vector2.left, Vector2.right })
                {
                    actor.Warp(W.CurrentZone.origin + new Vector2(1, -1));
                    var start = actor.Position;
                    actor.BeginTraversal(start + direction * .8f, 1.8f);
                    float end = Time.unscaledTime + 5;
                    var frames = new HashSet<Sprite>();
                    while (actor.IsTraversing && Time.unscaledTime < end)
                    {
                        if (actor.advanced.Motion == ActorMotion.Walk)
                        {
                            frames.Add(actor.sprite.sprite);
                        }

                        yield return null;
                    }

                    check(
                        actor.TraversalArrived
                            && frames.Count > 1
                            && Vector2.Distance(actor.Position, start + direction * .8f) < .07f,
                        "Actual directional walk and changing frames " + age + " " + direction
                    );
                    check(
                        actor.advanced.Direction == ActorAnimationSet.Direction(direction),
                        "Animation follows actual displacement " + age + " " + direction
                    );
                    actor.CancelTraversal();
                    yield return new WaitForSecondsRealtime(.35f);
                    check(actor.advanced.Motion == ActorMotion.Idle, "Walk returns to Idle " + age);
                }

                collider.enabled = enabled;
            }

            yield return Age(g, 0);
            check(
                g.growth.TryAdvanceAgeForMission("growth-art/first-walk", 1),
                "Existing growth-event path advances age"
            );
            yield return null;
            yield return null;
            check(
                W.Player.growthVisual.Current.stage == VisualGrowthStage.Toddler && W.Player.MovementAllowed,
                "Real growth event updates art and movement"
            );
            check(!g.growth.TryAdvanceAgeForMission("growth-art/first-walk", 1), "Growth receipt cannot repeat");
            yield return Save(g, check);
            yield return Preview(catalog);
        }

        static string FinancialContent(string json)
        {
            var e = JsonUtility.FromJson<EconomySnapshot>(json);
            if (e.finance != null)
            {
                e.finance.marketUtcTicks = 0;
            }

            if (e.property != null)
            {
                e.property.marketUtcTicks = 0;
            }

            return JsonUtility.ToJson(e);
        }

        static IEnumerator Save(GameManager g, Action<bool, string> check)
        {
            yield return Age(g, 8);
            yield return Go("family-living", check);
            long money = g.economy.Cash;
            g.Save();
            var inventory = JsonUtility.ToJson(g.economy.Capture());
            File.WriteAllText("Docs/AgeGrowth/SAVE_BEFORE.json", inventory);
            yield return Age(g, 19);
            g.Load();
            yield return Wait();
            var after = JsonUtility.ToJson(g.economy.Capture());
            File.WriteAllText("Docs/AgeGrowth/SAVE_AFTER.json", after);
            check(
                g.growth.Age == 8 && W.Player.growthVisual.Current.stage == VisualGrowthStage.SchoolChild,
                "Actual QA disk load restores authoritative age and matching art"
            );
            check(
                g.economy.Cash == money && FinancialContent(after) == FinancialContent(inventory),
                "Growth save/load preserves cash inventory and housing (UTC anchors rebase on load)"
            );
            check(
                UnityEngine.Object.FindObjectsByType<WorldPlayerMotor>(FindObjectsSortMode.None).Length == 1,
                "Scene reload has exactly one player"
            );
        }

        static IEnumerator Preview(GrowthVisualCatalog catalog)
        {
            var root = new GameObject("QA-only age comparison");
            var center = (Vector2)W.follow.target.transform.position;
            var font = W.hud.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t => t.font != null).font;
            var previews = new List<Sprite>();
            var previewMaterials = new List<Material>();
            var game = GameContext.Instance.game;
            var originalGrowth = game.growth.Capture();
            bool shadow = W.Player.contactShadow.enabled;
            try
            {
                for (int i = 0; i < 9; i++)
                {
                    var p = catalog.profiles[i];
                    yield return Age(game, p.minAge);
                    W.Player.Face(Vector2.down);
                    W.Player.Pose("Idle");
                    yield return new WaitForSecondsRealtime(.25f);
                    var original = W.Player.sprite;
                    var o = new GameObject(p.id, typeof(SpriteRenderer));
                    o.transform.SetParent(root.transform);
                    o.transform.position = center + new Vector2(-4 + i, 0);
                    var s = o.GetComponent<SpriteRenderer>();
                    s.sprite = original.sprite;
                    var material = new Material(original.sharedMaterial);
                    material.mainTexture = s.sprite.texture;
                    previewMaterials.Add(material);
                    s.sharedMaterial = material;
                    s.flipX = original.flipX;
                    s.flipY = original.flipY;
                    s.color = original.color;
                    s.sortingOrder = 3000 + i;
                    o.transform.localScale = original.transform.lossyScale;
                    var label = new GameObject("Age label", typeof(TMPro.TextMeshPro));
                    label.transform.SetParent(root.transform);
                    label.transform.position = o.transform.position + Vector3.down * .32f;
                    var text = label.GetComponent<TMPro.TextMeshPro>();
                    text.font = font;
                    text.fontSize = 2.5f;
                    text.alignment = TMPro.TextAlignmentOptions.Center;
                    text.text =
                        p.minAge
                        + (
                            i == 8 ? "+"
                            : i == 0 ? ""
                            : "–" + p.maxAge
                        );
                    text.GetComponent<MeshRenderer>().sortingOrder = 3001;
                }

                W.Player.sprite.enabled = false;
                W.Player.contactShadow.enabled = false;
                yield return Shot("Nine-stage-live-preview");
            }
            finally
            {
                W.Player.sprite.enabled = true;
                W.Player.contactShadow.enabled = shadow;
                UnityEngine.Object.Destroy(root);
                foreach (var sprite in previews)
                {
                    UnityEngine.Object.Destroy(sprite);
                }

                foreach (var material in previewMaterials)
                {
                    UnityEngine.Object.Destroy(material);
                }

                game.growth.Restore(originalGrowth);
            }
        }

        static IEnumerator Care(GameManager g, Action<bool, string> check)
        {
            foreach (int age in new[] { 0, 1 })
            {
                yield return Age(g, age);
                yield return Go("family-ian-bedroom", check);
                var bed = W.objects.Single(o => o.id == "family-bed");
                W.Player.Warp(bed.Point);
                var stats = g.stats.Capture();
                stats.hunger = stats.thirst = 40;
                g.stats.Restore(stats);
                yield return null;
                int action = age == 0 ? 3 : 4;
                check(W.Begin(bed, action), "Real feeding/drinking care starts " + age);
                var expected = age == 0 ? ActorMotion.CareFeed : ActorMotion.Drink;
                var allowed = W.Player.advanced.library.Find(expected, ActorDirection.Down).frames;
                var frames = new HashSet<Sprite>();
                float until = Time.unscaledTime + 20;
                bool own = true,
                    shot = false;
                while (W.Busy && Time.unscaledTime < until)
                {
                    if (W.Player.advanced.Motion == expected && allowed.Contains(W.Player.sprite.sprite))
                    {
                        frames.Add(W.Player.sprite.sprite);
                        own &= W.Player.sprite.enabled && !W.Player.advanced.UsedMotionFallback;
                        if (frames.Count == 2 && !shot)
                        {
                            shot = true;
                            yield return Shot("Care-" + expected + "-" + age);
                        }
                    }

                    yield return null;
                }

                File.WriteAllText(
                    "Docs/AgeGrowth/CARE-" + age + ".txt",
                    "busy="
                        + W.Busy
                        + " own="
                        + own
                        + " frames="
                        + frames.Count
                        + " hunger="
                        + g.stats.Hunger
                        + " thirst="
                        + g.stats.Thirst
                        + " control="
                        + W.CanControl
                );
                check(
                    !W.Busy
                        && own
                        && frames.Count == 2
                        && (age == 0 ? g.stats.Hunger : g.stats.Thirst) > 40
                        && W.CanControl,
                    "Correct own-age care frames, actual stat effect and input restored " + age
                );
            }
        }

        static IEnumerator BedReadback(GameManager g, Action<bool, string> check)
        {
            yield return Age(g, 14);
            yield return Go("family-ian-bedroom", check);
            var bed = W.objects.Single(o => o.id == "family-bed");
            var a = bed.GetComponent<WorldLifeAction>();
            W.Player.Warp(a.approach.position);
            yield return null;
            check(W.Begin(bed, 5), "Readonly texture bed starts");
            bool quilt = false;
            float end = Time.unscaledTime + 20;
            while (W.Busy && Time.unscaledTime < end)
            {
                quilt |= bed.GetComponentsInChildren<SpriteRenderer>()
                    .Any(r => r.name == "Age-appropriate bed blanket" && r.enabled);
                yield return null;
            }

            check(
                quilt && !W.Busy && W.CanControl,
                "GPU-only textures support actual sleep, native blanket crop and wake"
            );
            check(
                !W.Player.growthVisual.Current.idlePreview.texture.isReadable,
                "Runtime art no longer duplicates CPU pixel storage"
            );
        }

        static IEnumerator Life(GameManager g, Action<bool, string> check, Action<GameObject, string> audit)
        {
            foreach (int age in new[] { 0, 1, 3, 8, 14, 18, 19 })
            {
                yield return Age(g, age);
                yield return Go("family-ian-bedroom", check);
                var bed = W.objects.Single(o => o.id == "family-bed");
                var a = bed.GetComponent<WorldLifeAction>();
                W.Player.Warp(a.approach.position);
                yield return null;
                check(W.Begin(bed, 5), "Actual sleep starts age " + age);
                bool slept = false,
                    woke = false,
                    own = true;
                var frames = new HashSet<Sprite>();
                float end = Time.unscaledTime + 25;
                while (W.Busy && Time.unscaledTime < end)
                {
                    if (W.Player.advanced.Motion == ActorMotion.Sleep)
                    {
                        slept = true;
                        frames.Add(W.Player.sprite.sprite);
                        if (age < 19)
                        {
                            own &=
                                W.Player.sprite.enabled
                                && !a.overlay.enabled
                                && W.Player.sprite.sprite.texture == W.Player.growthVisual.Current.idlePreview.texture;
                            if (frames.Count == 2)
                            {
                                yield return Shot("Sleep-" + age);
                            }
                        }
                    }

                    woke |= W.Player.advanced.Motion == ActorMotion.WakeUp;
                    yield return null;
                }

                check(
                    !W.Busy && slept && woke && own,
                    "Own age sleep/wake animation and adult-overlay exclusion " + age
                );
                check(
                    W.CanControl
                        && W.Player.GetComponent<CircleCollider2D>().enabled
                        && a.seat.State == ActivityOccupancy.Free,
                    "Sleep restores input collision and occupancy " + age
                );
            }

            foreach (int age in new[] { 0, 1, 2 })
            {
                yield return Age(g, age);
                yield return Go("family-bathroom", check);
                var shower = W.objects.Single(o => o.kind == WorldInteractionKind.Shower);
                W.Player.Warp(shower.Point);
                yield return null;
                check(!W.Begin(shower) && !W.Busy, "Direct infant/toddler shower rejected at executor " + age);
                yield return Go("family-ian-bedroom", check);
                var bed = W.objects.Single(o => o.id == "family-bed");
                W.Player.Warp(bed.Point);
                g.world.State.hygiene = 20;
                double time = g.clock.SubDaySeconds;
                yield return null;
                check(W.Begin(bed, -4), "Guardian bath care accepted " + age);
                bool saw = false,
                    shot = false;
                float end = Time.unscaledTime + 25;
                while (W.Busy && Time.unscaledTime < end)
                {
                    saw |=
                        W.Player.advanced.Motion == ActorMotion.CareBath
                        && W.Player.sprite.enabled
                        && !W.Player.advanced.UsedMotionFallback;
                    if (
                        saw
                        && !shot
                        && W.Player.advanced.Motion == ActorMotion.CareBath
                        && W.Player.advanced.Frame == 1
                    )
                    {
                        shot = true;
                        yield return Shot("Care-Bath-" + age);
                    }

                    yield return null;
                }

                check(
                    saw
                        && !W.Busy
                        && Mathf.Abs(g.world.State.hygiene - (85 - 1200f / 86400 * g.world.rules.hygieneLossPerDay))
                            < .01f
                        && Math.Abs(g.clock.SubDaySeconds - time - 1200) < 1,
                    "Visible care-bath pose and single hygiene/time commit " + age
                );
            }

            foreach (int age in new[] { 3, 5, 8, 11, 14, 17, 19 })
            {
                yield return Age(g, age);
                yield return Go("family-bathroom", check);
                var shower = W.objects.Single(o => o.kind == WorldInteractionKind.Shower);
                var a = shower.GetComponent<WorldLifeAction>();
                W.Player.Warp(a.approach.position);
                g.world.State.hygiene = 20;
                yield return null;
                check(W.Begin(shower), "Existing permitted shower starts age " + age);
                var frames = new HashSet<Sprite>();
                bool inside = true,
                    active = false;
                float end = Time.unscaledTime + 25;
                while (W.Busy && Time.unscaledTime < end)
                {
                    if (a.booth.WaterRunning)
                    {
                        active = true;
                        inside &=
                            Vector2.Distance(W.Player.Position, W.Player.growthVisual.Inside(a)) < .06f
                            && W.Player.sprite.enabled;
                        frames.Add(W.Player.sprite.sprite);
                        if (frames.Count == 2)
                        {
                            yield return Shot("Shower-" + age);
                        }
                    }

                    yield return null;
                }

                check(
                    !W.Busy
                        && inside
                        && active
                        && frames.Count > 1
                        && Mathf.Abs(g.world.State.hygiene - (85 - 1200f / 86400 * g.world.rules.hygieneLossPerDay))
                            < .01f,
                    "Own age animated actor inside actual booth " + age
                );
                check(
                    W.CanControl && a.seat.State == ActivityOccupancy.Free && a.booth.Aperture == 0,
                    "Shower exits and restores input " + age
                );
            }

            yield return Age(g, 8);
            yield return Go("family-bathroom", check);
            var cancelShower = W.objects.Single(o => o.kind == WorldInteractionKind.Shower);
            var cancelAction = cancelShower.GetComponent<WorldLifeAction>();
            W.Player.Warp(cancelAction.approach.position);
            double cancelTime = g.clock.SubDaySeconds;
            float cancelHygiene = g.world.State.hygiene;
            yield return null;
            check(W.Begin(cancelShower), "Age-change cancellation shower starts");
            float cancelEnd = Time.unscaledTime + 15;
            while (!cancelAction.booth.WaterRunning && W.Busy && Time.unscaledTime < cancelEnd)
            {
                yield return null;
            }

            check(cancelAction.booth.WaterRunning, "Age-change fixture reaches actual shower");
            g.growth.Restore(new ProgressSnapshot { age = 14 });
            yield return null;
            check(
                W.Player.growthVisual.Deferred && W.Player.growthVisual.Current.stage == VisualGrowthStage.SchoolChild,
                "Busy action defers visual/collider swap"
            );
            W.CancelAction();
            yield return Wait();
            check(
                W.Player.growthVisual.Current.stage == VisualGrowthStage.Teen
                    && !W.Player.growthVisual.Deferred
                    && g.clock.SubDaySeconds == cancelTime
                    && g.world.State.hygiene == cancelHygiene
                    && cancelAction.seat.State == ActivityOccupancy.Free
                    && W.CanControl,
                "Cancelled age change applies new profile safely with no rewards and restores occupancy/input"
            );
            yield return Meals(g, check);
        }

        static IEnumerator Doors(GameManager g, Action<bool, string> check)
        {
            foreach (int age in new[] { 0, 1, 6, 7, 18, 19 })
            {
                yield return Age(g, age);
                yield return Go("family-living", check);
                var edge = T.roomCatalog.portals.First(p =>
                    p.sourceRoom == "family-living" && p.destinationRoom == "family-kitchen"
                );
                var o = W.objects.Single(o => o.id == edge.doorId);
                var door = o.GetComponent<WorldDoor>();
                W.Player.Warp(door.Approach(door.Plane - door.normal));
                yield return null;
                if (age == 0)
                {
                    check(!W.Use(o) && !W.Busy, "Newborn cannot independently operate or traverse family door");
                    continue;
                }

                for (int cycle = 0; cycle < 2; cycle++)
                {
                    foreach (string destination in new[] { "family-kitchen", "family-living" })
                    {
                        var route = T.roomCatalog.portals.First(p =>
                            p.sourceRoom == W.Room.roomId && p.destinationRoom == destination
                        );
                        o = W.objects.Single(o => o.id == route.doorId);
                        door = o.GetComponent<WorldDoor>();
                        door.opens = 0;
                        door.closes = 24;
                        W.Player.Warp(door.Approach(door.Plane - door.normal));
                        yield return null;
                        check(W.Use(o) && !W.Use(o), "Age door request / duplicate guard " + age + " " + destination);
                        yield return Wait();
                        check(
                            W.Room.roomId == destination
                                && W.CanControl
                                && W.Player.growthVisual.Current == W.Player.growthVisual.catalog.AtAge(age),
                            "Age-specific arrival, profile and control after actual door " + age + " " + destination
                        );
                    }
                }

                var front = T.roomCatalog.portals.First(p =>
                    p.sourceRoom == "family-living" && !FamilyLife.IsFamilyRoom(p.destinationRoom)
                );
                o = W.objects.Single(x => x.id == front.doorId);
                door = o.GetComponent<WorldDoor>();
                W.Player.Warp(door.Approach(door.Plane - door.normal));
                yield return null;
                if (age < 7)
                {
                    check(
                        !W.Use(o) && !T.NavigateRoom("city", T.roomCatalog.rules.Zone("city").spawn, Vector2.down),
                        "Both door and direct scene reject independent outdoor age " + age
                    );
                }
                else
                {
                    check(W.Use(o), "Outdoor door allowed age " + age);
                    yield return Wait();
                    check(W.Room.roomId == front.destinationRoom && W.CanControl, "Actual outdoor arrival age " + age);
                }

                check(
                    GrowthAccess.Zone(g, "workplace", out _) == (age >= 19)
                        && GrowthAccess.Zone(g, "finance", out _) == (age >= 19)
                        && GrowthAccess.Zone(g, "realestate", out _) == (age >= 19),
                    "Adult facilities stay age locked " + age
                );
            }
        }

        static IEnumerator Meals(GameManager g, Action<bool, string> check)
        {
            g.clock.Restore(8, new TimeSnapshot { subDaySeconds = 7 * 3600 + 40 * 60 });
            foreach (int age in new[] { 1, 5, 8, 14, 19 })
            {
                yield return Age(g, age);
                yield return Go("family-kitchen", check);
                FamilyLife.State(g).meals = 3;
                var o = W.objects.Single(o => o.id == "family-player-seat");
                W.Player.Warp(o.Point);
                var a = o.GetComponent<WorldLifeAction>();
                var stats = g.stats.Capture();
                stats.hunger = 40;
                g.stats.Restore(stats);
                yield return null;
                check(W.Begin(o, 3), "Actual family meal starts " + age);
                bool eat = false,
                    own = true,
                    shot = false;
                float end = Time.unscaledTime + 25;
                while (W.Busy && Time.unscaledTime < end)
                {
                    if (W.Player.advanced.Motion == ActorMotion.Eat)
                    {
                        eat = true;
                        if (age < 19)
                        {
                            own &=
                                W.Player.sprite.enabled
                                && !a.overlay.enabled
                                && W.Player.sprite.sprite.texture == W.Player.growthVisual.Current.idlePreview.texture;
                        }

                        if (!shot && W.Player.advanced.Frame == 1)
                        {
                            shot = true;
                            yield return Shot("Family-Meal-" + age);
                        }
                    }

                    yield return null;
                }

                check(
                    !W.Busy && eat && own && g.stats.Hunger > 40 && a.seat.State == ActivityOccupancy.Free,
                    "Real family food and own seated age art " + age
                );
                yield return Shot("Family-Meal-Exited-" + age);
            }
        }
    }
}
#endif
