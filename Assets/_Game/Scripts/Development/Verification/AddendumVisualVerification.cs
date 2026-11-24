#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using IanGame.Commercial;

namespace IanGame.Editor
{
    public sealed class AddendumVisualVerification : MonoBehaviour
    {
        public static readonly List<string> Passed = new List<string>();
        public static bool Finished;
        public static string Failure;
        public static string Progress;
        GameManager game;
        SaveData original;
        string slot;
        bool auto;
        bool lifecycle;
        bool realtime;
        float chance;
        int motion;
        int effects;
        int transitions;
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo Slot => typeof(SaveManager).GetField("fileName", Flags);

        [Serializable]
        class Results
        {
            public string[] passed;
            public string failure;
        }

        public void Begin()
        {
            StartCoroutine(Run());
        }

        void Check(bool condition, string label)
        {
            if (!condition)
            {
                throw new Exception(label);
            }

            Passed.Add("PASS " + label);
        }

        SaveData Capture() =>
            new SaveData
            {
                day = game.clock.Day,
                stats = game.stats.Capture(),
                economy = game.economy.Capture(),
                progression = game.growth.Capture(),
                survival = game.survival.Capture(),
                time = game.clock.Capture(),
                missions = game.missions.Capture(),
                phase23 = game.phase23.Capture(),
                gameOver = game.State == GameState.GameOver,
                runId = (string)typeof(GameManager).GetField("runId", Flags).GetValue(game),
                settledDay = game.clock.Day,
                lastSeenUtcTicks = 1,
            };

        string State() => JsonUtility.ToJson(Capture());

        IEnumerator Frames(int count = 4)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        void Shot(string label)
        {
            ScreenCapture.CaptureScreenshot("Docs/CommercialVisual/V11/After/" + label + ".png");
        }

        IEnumerator Run()
        {
            Passed.Clear();
            Failure = null;
            Finished = false;
            game = GameContext.Instance.game;
            original = Capture();
            slot = (string)Slot.GetValue(game.saves);
            auto = game.autoSaveAfterAction;
            lifecycle = game.autoSaveOnLifecycle;
            realtime = game.clock.realtimeEnabled;
            chance = game.balance.eventChance;
            motion = PlayerPrefs.GetInt("ian.ui.reduceMotion", 0);
            effects = PlayerPrefs.GetInt("ian.visual.effects", 1);
            transitions = PlayerPrefs.GetInt("ian.visual.transitions", 0);
            Slot.SetValue(game.saves, "addendum-v11-" + Guid.NewGuid().ToString("N") + ".json");
            game.autoSaveAfterAction = game.autoSaveOnLifecycle = false;
            game.clock.realtimeEnabled = false;
            game.balance.eventChance = 0;
            PlayerPrefs.SetInt("ian.ui.reduceMotion", 0);
            PlayerPrefs.SetInt("ian.visual.effects", 2);
            PlayerPrefs.SetInt("ian.visual.transitions", 0);
            var stack = new Stack<IEnumerator>();
            stack.Push(Scenario());
            while (stack.Count > 0)
            {
                bool more = false;
                object next = null;
                try
                {
                    more = stack.Peek().MoveNext();
                    if (more)
                    {
                        next = stack.Peek().Current;
                    }
                }
                catch (Exception ex)
                {
                    Failure = ex.ToString();
                    break;
                }

                if (!more)
                {
                    stack.Pop();
                    continue;
                }

                if (next is IEnumerator)
                {
                    stack.Push((IEnumerator)next);
                    continue;
                }

                yield return next;
            }

            try
            {
                File.WriteAllText(game.saves.SavePath, JsonUtility.ToJson(original));
                game.Load();
                Slot.SetValue(game.saves, slot);
                game.autoSaveAfterAction = auto;
                game.autoSaveOnLifecycle = lifecycle;
                game.clock.realtimeEnabled = realtime;
                game.balance.eventChance = chance;
                PlayerPrefs.SetInt("ian.ui.reduceMotion", motion);
                PlayerPrefs.SetInt("ian.visual.effects", effects);
                PlayerPrefs.SetInt("ian.visual.transitions", transitions);
            }
            catch (Exception ex)
            {
                Failure += "\nRESTORE " + ex;
            }

            File.WriteAllText(
                "Docs/CommercialVisual/V11/TEST_RESULTS.json",
                JsonUtility.ToJson(new Results { passed = Passed.ToArray(), failure = Failure }, true)
            );
            Progress = "Complete";
            Finished = true;
            Destroy(gameObject);
        }

        IEnumerator Route(SceneId id)
        {
            var nav = UnityEngine.Object.FindAnyObjectByType<UINavigationService>();
            if (nav != null)
            {
                nav.CloseAll();
            }

            string before = State();
            var service = GameContext.Instance.transitions;
            SceneId from = service.CurrentScene;
            Check(service.Navigate(id), "Navigate " + from + " -> " + id);
            bool exclusive = true;
            float deadline = Time.realtimeSinceStartup + 20;
            if (from != id)
            {
                Check(!service.Navigate(from), "Concurrent input rejected " + from + " -> " + id);
            }

            while (service.IsTransitioning)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    throw new Exception("Transition deadline");
                }

                exclusive &= UnityEngine.Object.FindObjectsByType<EventSystem>().Count(e => e.isActiveAndEnabled) <= 1;
                yield return null;
            }

            yield return Frames();
            Check(SceneManager.GetActiveScene().name == id.ToString(), "Actual destination " + id);
            Check(before == State(), "All stats economy missions survival time employment preserved " + id);
            Check(exclusive, "Exclusive input owners during transition " + id);
            Check(
                GameContext.Instance.game == game && UnityEngine.Object.FindObjectsByType<GameManager>().Length == 1,
                "Persistent manager retained " + id
            );
        }

        void Fonts(string label)
        {
            string[] allowed = { "PFStardust_Main" };
            var texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
            Check(
                texts.All(t => t.font != null && allowed.Contains(t.font.name)),
                "Pixel font roles on all TMP fields " + label
            );
            var missing = new HashSet<char>();
            foreach (var t in texts)
            {
                foreach (char c in t.text ?? "")
                {
                    if (!char.IsControl(c) && !char.IsSurrogate(c) && !t.font.HasCharacter(c, true, true))
                    {
                        missing.Add(c);
                    }
                }
            }

            Check(missing.Count == 0, "No missing current UI glyphs " + label + " / " + new string(missing.ToArray()));
        }

        IEnumerator Scenario()
        {
            Progress = "Fonts";
            yield return Route(SceneId.MainMenu);
            Fonts("MainMenu");
            var body = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/_Game/UI/Pixel/Fonts/PFStardust/PFStardust_Main.asset"
            );
            Check(
                "이안키우기0123456789금요일₩".All(c => body.HasCharacter(c, true, true))
                    && body.fallbackFontAssetTable.Any(a => a.sourceFontFile && a.faceInfo.familyName.Contains("Stardust")),
                "Game glyphs and extended Stardust source"
            );
            Check(
                body.atlasPopulationMode == AtlasPopulationMode.Static
                    && body.atlasTexture.filterMode == FilterMode.Point,
                "Static point-filtered bitmap primary"
            );
            Check(body.material.shader.name.Contains("Bitmap"), "Bitmap shader avoids SDF edge blur");
            Check(
                Enumerable
                    .Range('0', 10)
                    .Select(c => body.characterLookupTable[(uint)c].glyph.metrics.horizontalAdvance)
                    .Distinct()
                    .Count() == 1,
                "Numerals have equal advances"
            );
            CommercialSliceVerification.SetSize(1920, 1080);
            yield return Frames(8);
            foreach (
                var id in new[]
                {
                    SceneId.Home,
                    SceneId.City,
                    SceneId.Workplace,
                    SceneId.Home,
                    SceneId.School,
                    SceneId.City,
                }
            )
            {
                Progress = "Scene " + id;
                yield return Route(id);
                Fonts(id.ToString());
                var room = UnityEngine.Object.FindAnyObjectByType<RoomAtmosphere>();
                Check(
                    room != null
                        && room.character.sprite == room.character.GetComponent<SpriteMotionPlayer>().library.fallback,
                    "Official Ian retained " + id
                );
                var pp = Camera.main.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
                Check(
                    pp.assetsPPU == 100 && pp.refResolutionX == 640 && pp.refResolutionY == 360,
                    "Shared PPU/reference policy " + id
                );
                Check(
                    UnityEngine
                        .Object.FindObjectsByType<UnityEngine.UI.Button>()
                        .Count(b => b.gameObject.activeInHierarchy) <= 12,
                    "Compact main HUD button count " + id
                );
                Shot(id + "-1920x1080");
                yield return Frames();
            }

            var controller = GameContext.Instance.transitions.presentation;
            Check(
                controller.profiles.Length == 12
                    && new[]
                    {
                        (SceneId.Home, SceneId.City),
                        (SceneId.City, SceneId.Workplace),
                        (SceneId.Workplace, SceneId.Home),
                        (SceneId.Home, SceneId.School),
                        (SceneId.School, SceneId.City),
                    }.All(r => controller.Find(r.Item1, r.Item2) != null),
                "All requested routes have authored profiles"
            );
            var fx = UnityEngine.Object.FindAnyObjectByType<LayeredLocationPresentation>();
            Check(
                fx.layers.Length == 3 && fx.depths.Distinct().Count() == 3,
                "City has three separately rendered depth bands"
            );
            int tick = fx.TickCount;
            var first = fx.actors[0].renderer.sprite;
            var position = fx.actors[0].renderer.transform.localPosition;
            bool moved = false,
                frameChanged = false;
            for (int i = 0; i < 12; i++)
            {
                yield return new WaitForSecondsRealtime(.15f);
                moved |= fx.actors[0].renderer.transform.localPosition != position;
                frameChanged |= fx.actors[0].renderer.sprite != first;
            }

            Check(fx.TickCount > tick && moved, "Pedestrian animation scheduler and movement advance");
            Check(frameChanged, "Walk frame changes");
            Check(fx.actors.All(a => a.frames.Length == 4), "Four actual frames per environment actor");
            Check(fx.LiveRain == 24, "High weather option reuses 24 rain objects");
            PlayerPrefs.SetInt("ian.visual.effects", 0);
            yield return new WaitForSecondsRealtime(.2f);
            Check(fx.LiveRain == 0, "Effects off disables rain");
            PlayerPrefs.SetInt("ian.visual.effects", 1);
            yield return new WaitForSecondsRealtime(.2f);
            Check(fx.LiveRain == 8, "Low weather option uses eight rain objects");
            PlayerPrefs.SetInt("ian.visual.effects", 2);
            var roomCity = fx.room;
            var clock = game.clock.Capture();
            game.clock.Restore(
                game.clock.Day,
                new TimeSnapshot { subDaySeconds = 3600, elapsedSeconds = clock.elapsedSeconds }
            );
            yield return new WaitForSecondsRealtime(.2f);
            Color day = roomCity.background.color;
            game.clock.Restore(
                game.clock.Day,
                new TimeSnapshot { subDaySeconds = 10 * 3600, elapsedSeconds = clock.elapsedSeconds }
            );
            yield return new WaitForSecondsRealtime(.2f);
            Color evening = roomCity.background.color;
            game.clock.Restore(
                game.clock.Day,
                new TimeSnapshot { subDaySeconds = 14 * 3600, elapsedSeconds = clock.elapsedSeconds }
            );
            yield return new WaitForSecondsRealtime(.2f);
            Check(day != evening && day != roomCity.background.color, "Day evening and night lighting differ");
            Shot("City-Night-Fixture");
            yield return Frames();
            game.clock.Restore(game.clock.Day, clock);
            foreach (var id in new[] { SceneId.City, SceneId.Workplace })
            {
                yield return Route(id);
                var p = UnityEngine.Object.FindAnyObjectByType<PixelHomePresenter>();
                var location = UnityEngine.Object.FindAnyObjectByType<Phase23Presenter>();
                p.navigation.stack.Push(p.navigation.locationPanel);
                yield return new WaitForSecondsRealtime(.2f);
                var groups = location
                    .panel.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == "Categorized location activities")
                    .ToArray();
                Check(groups.Length == 1, "No duplicate activity groups " + id);
                var tabs = groups[0].GetComponent<PixelTabGroup>();
                foreach (
                    var tab in groups[0]
                        .GetComponentsInChildren<PixelTabGroup>(true)
                        .Where(t => t.tabs != null && t.tabs.Length > 0)
                )
                {
                    for (int i = 0; i < tab.tabs.Length; i++)
                    {
                        tab.Select(i);
                        yield return Frames();
                        Check(
                            tab.pages.Count(page => page.activeSelf) == 1,
                            "Single visible activity tab " + id + " #" + i
                        );
                        Check(
                            tab.tabs.All(b => b.GetComponentInChildren<TMP_Text>().color.a > 0),
                            "Tab labels have visible colors " + id
                        );
                    }
                }

                Shot(id + "-Activities");
                yield return Frames();
                var close = location.closeButton;
                var pointer = new PointerEventData(EventSystem.current)
                {
                    position = RectTransformUtility.WorldToScreenPoint(
                        null,
                        close.GetComponent<RectTransform>().position
                    ),
                    button = PointerEventData.InputButton.Left,
                };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                Check(
                    hits.Any(h => h.gameObject == close.gameObject),
                    "Touch raycast reaches actual location close " + id
                );
                ExecuteEvents.Execute(close.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                yield return new WaitForSecondsRealtime(.2f);
                Check(p.navigation.stack.Depth == 0, "Actual location close restores HUD " + id);
            }

            Progress = "Job themes and payroll fixture";
            var workplace = UnityEngine.Object.FindAnyObjectByType<WorkplaceThemePresenter>();
            foreach (string job in new[] { "office", "developer", "analyst", "convenience" })
            {
                game.phase23.Progress.employment.currentJobId = job;
                workplace.Apply();
                yield return new WaitForSecondsRealtime(.2f);
                Check(workplace.AppliedJob == job, "Existing job selects theme " + job);
                if (job == "convenience")
                {
                    Check(
                        workplace.room.background.sprite.name.Contains("Convenience"),
                        "Convenience uses separate authored interior"
                    );
                }

                Shot("Workplace-" + job + "-Fixture");
                yield return Frames();
            }

            game.growth.Restore(new ProgressSnapshot { age = 19 });
            game.stats.ApplyCore(
                new StatDelta
                {
                    health = 100,
                    mental = 100,
                    hunger = 100,
                    thirst = 100,
                    knowledge = 100,
                }
            );
            long cash = game.economy.Cash;
            Check(game.TryAction(0), "Actual work action succeeds in employed fixture");
            var record = game.phase23.Progress.employment.work.Last();
            Check(record.paid && game.economy.Cash - cash == record.wage, "Actual payroll credited once");
            Check(!game.phase23.Work.Pay(record.id), "Salary deduplication preserved");
            var hud = UnityEngine.Object.FindAnyObjectByType<CommercialHUD>();
            Check(
                hud.receipt.gameObject.activeSelf && hud.feedback.text.Contains(record.wage.ToString("N0")),
                "Pixel payslip displays recorded wage"
            );
            Check(
                hud.rewardCoins.Length == 8 && hud.rewardCoins.Any(c => c.gameObject.activeSelf),
                "Pooled coin feedback plays"
            );
            Shot("Workplace-Payroll-Fixture");
            yield return Frames();
            var workRoom = UnityEngine.Object.FindAnyObjectByType<RoomAtmosphere>();
            Color healthyLight = workRoom.background.color;
            int health = game.stats.Health;
            game.stats.ApplyCore(new StatDelta { health = 20 - health });
            yield return new WaitForSecondsRealtime(.2f);
            Check(workRoom.background.color != healthyLight, "Overwork health threshold changes office lighting");
            game.stats.ApplyCore(new StatDelta { health = health - game.stats.Health });
            game.Save();
            var saved = Capture();
            string checkpoint = JsonUtility.ToJson(saved);
            game.TryAction(4);
            game.Load();
            var loaded = Capture();
            File.WriteAllText("Docs/CommercialVisual/V11/Save-Expected.json", checkpoint);
            File.WriteAllText("Docs/CommercialVisual/V11/Save-Actual.json", JsonUtility.ToJson(loaded));
            bool fractions =
                Math.Abs(saved.survival.hungerRemainder - loaded.survival.hungerRemainder) <= 1e-12
                && Math.Abs(saved.survival.thirstRemainder - loaded.survival.thirstRemainder) <= 1e-12;
            saved.survival.hungerRemainder = loaded.survival.hungerRemainder;
            saved.survival.thirstRemainder = loaded.survival.thirstRemainder;
            Check(
                fractions && JsonUtility.ToJson(saved) == JsonUtility.ToJson(loaded),
                "Save/load exact discrete state and payroll; survival fraction tolerance 1e-12"
            );
            foreach (var size in new[] { new[] { 1280, 720 }, new[] { 2400, 1080 } })
            {
                CommercialSliceVerification.SetSize(size[0], size[1]);
                yield return Frames(10);
                Check(
                    Screen.width == size[0] && Screen.height == size[1],
                    "Actual Game View " + size[0] + "x" + size[1]
                );
                foreach (var id in new[] { SceneId.Home, SceneId.School, SceneId.City, SceneId.Workplace })
                {
                    yield return Route(id);
                    Fonts(id + " " + size[0]);
                    var p = UnityEngine.Object.FindAnyObjectByType<PixelHomePresenter>();
                    var corners = new Vector3[4];
                    p.navigation.navigation.GetComponent<RectTransform>().GetWorldCorners(corners);
                    Check(
                        corners.All(c => c.x >= 0 && c.x <= Screen.width && c.y >= 0 && c.y <= Screen.height),
                        "Navigation stays inside screen " + id + " " + size[0]
                    );
                    Shot(id + "-" + size[0] + "x" + size[1]);
                    yield return Frames();
                }
            }

            CommercialSliceVerification.SetSize(1920, 1080);
            yield return Route(SceneId.Home);
            Check(game.missionCatalog.missions.Length == 60, "60 mission definitions retained");
        }
    }
}
#endif
