#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;

namespace IanGame.Editor
{
    public class PixelUIRunner : MonoBehaviour
    {
        public static List<string> Passed = new List<string>();
        public static string Failure;
        public static string Progress;
        public static bool Finished;
        GameManager game;
        SaveData original;
        string slot;
        bool auto;
        bool lifecycle;
        bool realtime;
        float chance;
        Gamepad pad;
        Keyboard keyboard;
        int reduce;
        int large;
        InputSettings.BackgroundBehavior oldBackground;
        InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
        FieldInfo Slot => typeof(SaveManager).GetField("fileName", BindingFlags.Instance | BindingFlags.NonPublic);

        public static object Status() =>
            new
            {
                finished = Finished,
                progress = Progress,
                passed = Passed.Count,
                failure = Failure,
            };

        public void Begin()
        {
            StartCoroutine(Safe());
        }

        void Check(bool condition, string name)
        {
            if (!condition)
            {
                throw new Exception("FAIL " + name);
            }

            Passed.Add("PASS " + name);
        }

        IEnumerator Safe()
        {
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings
                .EditorInputBehaviorInPlayMode
                .AllDeviceInputAlwaysGoesToGameView;
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
            reduce = PlayerPrefs.GetInt("ian.ui.reduceMotion", 0);
            large = PlayerPrefs.GetInt("ian.ui.largeText", 0);
            Slot.SetValue(game.saves, "pixelui-suite-" + Guid.NewGuid().ToString("N") + ".json");
            game.autoSaveAfterAction = false;
            game.autoSaveOnLifecycle = false;
            game.clock.realtimeEnabled = false;
            game.realtimeLifeEnabled = false;
            game.balance.eventChance = 0;
            PlayerPrefs.SetInt("ian.ui.reduceMotion", 1);
            PlayerPrefs.SetInt("ian.ui.largeText", 0);
            var stack = new Stack<IEnumerator>();
            stack.Push(Scenario());
            while (stack.Count > 0)
            {
                bool more;
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
                if (pad != null)
                {
                    InputSystem.RemoveDevice(pad);
                }

                if (keyboard != null)
                {
                    InputSystem.RemoveDevice(keyboard);
                }

                while (Shell() != null && Shell().Depth > 0)
                {
                    Shell().Pop();
                }

                game.phase23.catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Phase23Catalog>(
                    "Assets/_Game/Data/LifeActivities/LifeCatalog.asset"
                );
                File.WriteAllText(game.saves.SavePath, JsonUtility.ToJson(original, true));
                game.Load();
                Slot.SetValue(game.saves, slot);
                game.autoSaveAfterAction = auto;
                game.autoSaveOnLifecycle = lifecycle;
                game.clock.realtimeEnabled = realtime;
                game.realtimeLifeEnabled = realtime;
                game.balance.eventChance = chance;
                PlayerPrefs.SetInt("ian.ui.reduceMotion", reduce);
                PlayerPrefs.SetInt("ian.ui.largeText", large);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Failure += "\nRESTORE " + ex;
            }

            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
            Finished = true;
            Progress = "Complete";
            Directory.CreateDirectory("Docs/PixelUI");
            File.WriteAllText(
                "Docs/PixelUI/TEST_RESULTS.json",
                JsonUtility.ToJson(new Results { passed = Passed.ToArray(), failure = Failure }, true)
            );
            Destroy(gameObject);
        }

        [Serializable]
        class Results
        {
            public string[] passed;
            public string failure;
        }

        SaveData Capture() =>
            new SaveData
            {
                day = game.clock.Day,
                stats = game.stats.Capture(),
                progression = game.growth.Capture(),
                economy = game.economy.Capture(),
                survival = game.survival.Capture(),
                time = game.clock.Capture(),
                missions = game.missions.Capture(),
                phase23 = game.phase23.Capture(),
                gameOver = game.State == GameState.GameOver,
                runId = (string)
                    typeof(GameManager)
                        .GetField("runId", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(game),
                settledDay = game.clock.Day,
                lastSeenUtcTicks = DateTime.UtcNow.Ticks,
            };

        UIShell Shell() => UnityEngine.Object.FindFirstObjectByType<UIShell>();

        UINavigationService Nav() => UnityEngine.Object.FindFirstObjectByType<UINavigationService>();

        UIManager HUD() => UnityEngine.Object.FindFirstObjectByType<UIManager>();

        MissionUI Mission() =>
            UnityEngine
                .Object.FindObjectsByType<MissionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First();

        IEnumerator Frames(int count = 3)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        IEnumerator Route(SceneId id)
        {
            Nav()?.CloseAll();
            if (GameContext.Instance.transitions.CurrentScene != id)
            {
                Check(GameContext.Instance.transitions.Navigate(id), "Navigate " + id);
                int frames = 0;
                while (GameContext.Instance.transitions.IsTransitioning)
                {
                    if (++frames > 2000)
                    {
                        throw new Exception("Scene timed out");
                    }

                    yield return null;
                }
            }

            yield return Frames();
        }

        void Size(int width, int height)
        {
            Type.GetType("IanGame.Editor.PixelUIEditorTools, Assembly-CSharp-Editor")
                .GetMethod("Size")
                .Invoke(null, new object[] { width, height });
        }

        IEnumerator CaptureAt(int width, int height, string label)
        {
            Progress = "Resolution " + label;
            Size(width, height);
            yield return Frames(6);
            Check(
                Screen.width == width && Screen.height == height,
                "Actual Game View resolution " + width + "x" + height
            );
            var pixel = UnityEngine.Object.FindFirstObjectByType<PixelHomePresenter>();
            var scale = pixel.GetComponent<PixelCanvasScale>();
            Check(
                scale.AppliedIntegerScale >= 1
                    && Mathf.Abs(scale.scaler.scaleFactor - Mathf.Round(scale.scaler.scaleFactor)) < .001f,
                "Integer UI scale " + label
            );
            var safe = pixel.GetComponent<UISafeLayout>();
            Check(Inside(safe.frame, new Rect(0, 0, width, height)), "Core HUD inside screen " + label);
            Check(
                Inside(scale.hero.GetComponent<RectTransform>(), new Rect(0, 0, width, height))
                    && Inside(scale.rail.GetComponent<RectTransform>(), new Rect(0, 0, width, height)),
                "Character and summary visible " + label
            );
            Check(
                Nav().navigation.GetComponentsInChildren<Button>().Length == 4,
                "Exactly four bottom navigation buttons " + label
            );
            Check(pixel.quickButtons.Length <= 3, "At most three contextual actions " + label);
            Check(
                pixel.missionTitle.preferredHeight <= pixel.missionTitle.rectTransform.rect.height + 3,
                "Korean mission title fits " + label
            );
            pixel.notice.SetActive(false);
            Directory.CreateDirectory("Docs/PixelUI/Captures");
            ScreenCapture.CaptureScreenshot("Docs/PixelUI/Captures/" + label + ".png");
            yield return Frames(3);
        }

        static bool Inside(RectTransform rect, Rect screen)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners.All(p =>
                p.x >= screen.xMin - 2 && p.y >= screen.yMin - 2 && p.x <= screen.xMax + 2 && p.y <= screen.yMax + 2
            );
        }

        IEnumerator Scenario()
        {
            yield return Route(SceneId.Home);
            game.NewGame();
            game.clock.realtimeEnabled = false;
            Progress = "Core UI";
            var navigation = Nav();
            var shell = Shell();
            var ui = HUD();
            var mission = Mission();
            Check(game.missions.catalog.missions.Length == 60, "All sixty missions retained");
            Check(
                MissionGuidance.Summary(game.missions.ActiveMission, game.missions).Contains("물 마시기 0/1회"),
                "Exact action and count shown for first mission"
            );
            navigation.Life();
            yield return Frames();
            Check(shell.Top == navigation.lifePanel, "Life is a dedicated screen");
            ui.drinkButton.onClick.Invoke();
            yield return Frames();
            Check(
                game.missions.Counter(MissionCounter.Drink) == 1,
                "Existing drink button reaches game and mission service"
            );
            shell.Close(navigation.lifePanel);
            navigation.Mission();
            yield return Frames();
            Check(mission.conditionText.text.Contains("보상 수령"), "Completion guidance tells player to claim reward");
            mission.claimButton.onClick.Invoke();
            long money = game.economy.Cash;
            mission.claimButton.onClick.Invoke();
            Check(
                game.economy.Cash == money && game.missions.Claimed("M001") && !game.missions.Claimed("M002"),
                "Repeated reward click cannot duplicate gold"
            );
            navigation.CloseAll();
            navigation.Growth();
            yield return Frames();
            Check(
                shell.Top == navigation.growthPanel && navigation.growthTabs.pages.Length == 5,
                "Growth has five purpose tabs"
            );
            for (int tab = 0; tab < 5; tab++)
            {
                navigation.growthTabs.Select(tab);
                yield return Frames(1);
                Check(
                    navigation.growthTabs.pages.Count(p => p.activeSelf) == 1,
                    "Exactly one growth tab visible " + tab
                );
            }

            navigation.CloseAll();
            navigation.Activity();
            yield return Frames();
            Check(
                navigation
                    .activityPanel.GetComponentsInChildren<PixelNavigationButton>()
                    .Count(b => b.target == PixelNavigationButton.Target.Location) == 4,
                "Activity offers four actual locations"
            );
            navigation.CloseAll();
            navigation.Menu();
            yield return Frames();
            ui.saveButton.onClick.Invoke();
            SaveData saved;
            string message;
            Check(game.saves.TryRead(out saved, out message), "Save button writes isolated slot");
            navigation.CloseAll();
            navigation.Life();
            ui.mealButton.onClick.Invoke();
            navigation.CloseAll();
            navigation.Menu();
            ui.loadButton.onClick.Invoke();
            Check(
                game.clock.Day == saved.day && game.economy.Cash == saved.stats.money && game.missions.Claimed("M001"),
                "Load restores cash day and mission progress"
            );
            navigation.CloseAll();
            shell.OpenPause();
            shell.OpenSettings();
            yield return Frames();
            Check(shell.Depth == 2 && game.State == GameState.Paused, "Settings stacks above pause");
            var selected = EventSystem.current.currentSelectedGameObject;
            Check(
                selected != null && selected.transform.IsChildOf(shell.settingsPanel.transform),
                "Initial focus trapped in top modal"
            );
            pad = InputSystem.AddDevice<Gamepad>();
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.DpadDown));
            yield return Frames(2);
            Check(
                EventSystem.current.currentSelectedGameObject != null
                    && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(shell.settingsPanel.transform),
                "Gamepad navigation stays inside modal"
            );
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return Frames(2);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return Frames(2);
            Check(
                shell.Depth == 1 && game.State == GameState.Paused,
                "ESC closes only top settings modal; depth="
                    + shell.Depth
                    + " state="
                    + game.State
                    + " key="
                    + keyboard.escapeKey.wasPressedThisFrame
            );
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Frames(2);
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East));
            yield return Frames(2);
            Check(shell.Depth == 0 && game.State == GameState.Playing, "Gamepad back returns from pause and resumes");
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return LicenseInput();
            int priorDay = game.clock.Day;
            long priorCash = game.economy.Cash;
            shell.OpenPause();
            ui.newGameButton.onClick.Invoke();
            yield return Frames();
            Check(shell.Top == shell.confirmPanel, "New life requires confirmation dialog");
            shell.Pop();
            shell.Pop();
            Check(
                game.clock.Day == priorDay && game.economy.Cash == priorCash && game.missions.Claimed("M001"),
                "Cancelling new life preserves progress and cash"
            );
            navigation.Life();
            shell.Push(navigation.lifePanel);
            shell.Push(navigation.lifePanel);
            Check(shell.Depth == 1, "Duplicate screen pushes do not stack");
            navigation.CloseAll();
            foreach (
                var size in new[]
                {
                    new[] { 1920, 1080 },
                    new[] { 2560, 1440 },
                    new[] { 2560, 1080 },
                    new[] { 1080, 2400 },
                    new[] { 2400, 1080 },
                }
            )
            {
                yield return CaptureAt(size[0], size[1], "Home-" + size[0] + "x" + size[1]);
            }

            Progress = "Safe area";
            Size(1080, 2400);
            yield return Frames(5);
            foreach (var safe in UnityEngine.Object.FindObjectsByType<UISafeLayout>(FindObjectsSortMode.None))
            {
                safe.simulateSafeArea = true;
                safe.simulatedSafeArea = new Rect(64, 72, 952, 2232);
                safe.Apply(true);
            }

            yield return Frames(4);
            var home = UnityEngine.Object.FindFirstObjectByType<PixelHomePresenter>();
            Check(
                Inside(home.GetComponent<UISafeLayout>().frame, new Rect(64, 72, 952, 2232)),
                "Simulated notch and safe-area bounds respected"
            );
            ScreenCapture.CaptureScreenshot("Docs/PixelUI/Captures/Home-Mobile-SafeArea.png");
            yield return Frames(3);
            foreach (var safe in UnityEngine.Object.FindObjectsByType<UISafeLayout>(FindObjectsSortMode.None))
            {
                safe.simulateSafeArea = false;
                safe.Apply(true);
            }

            Size(1920, 1080);
            yield return Frames(4);
            foreach (var id in new[] { SceneId.School, SceneId.City, SceneId.Workplace, SceneId.Home })
            {
                var before = Capture();
                yield return Route(id);
                Check(
                    GameContext.Instance.game == game
                        && game.stats.Knowledge == before.stats.knowledge
                        && game.economy.Cash == before.stats.money
                        && game.growth.Age == before.progression.age
                        && JsonUtility.ToJson(game.phase23.Capture()) == JsonUtility.ToJson(before.phase23),
                    "Data preserved through " + id
                );
                Check(
                    UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1
                        && UnityEngine.Object.FindObjectsByType<SaveManager>(FindObjectsSortMode.None).Length == 1,
                    "Unique services in " + id
                );
            }

            Progress = "Touch and focus";
            navigation = Nav();
            navigation.Life();
            yield return Frames();
            var target = HUD().mealButton;
            var point = RectTransformUtility.WorldToScreenPoint(null, target.GetComponent<RectTransform>().position);
            var pointer = new PointerEventData(EventSystem.current) { position = point };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Check(
                hits.Any(h => h.gameObject == target.gameObject || h.gameObject.transform.IsChildOf(target.transform)),
                "Touch position raycast reaches life action"
            );
            navigation.CloseAll();
            yield return NavigationGuard();
            Progress = "Original Phase2.1 regression";
            var catalog = game.phase23.catalog;
            game.phase23.catalog = null;
            try
            {
                var results = (string[])
                    Type.GetType("IanGame.Editor.Phase2Verification, Assembly-CSharp-Editor")
                        .GetMethod("Run")
                        .Invoke(null, null);
                Passed.AddRange(results.Select(r => "REGRESSION " + r));
            }
            finally
            {
                game.phase23.catalog = catalog;
            }

            Progress = "Original Phase2.2 regression";
            catalog = game.phase23.catalog;
            game.phase23.catalog = null;
            try
            {
                Phase22Verification.Begin();
                int frames = 0;
                while (!Phase22Verification.Finished)
                {
                    if (++frames > 20000)
                    {
                        throw new Exception("Phase22 regression timeout");
                    }

                    yield return null;
                }

                Check(
                    string.IsNullOrEmpty(Phase22Verification.Failure),
                    "Phase22 regression: " + Phase22Verification.Failure
                );
                Passed.AddRange(Phase22Verification.Passed.Select(r => "REGRESSION " + r));
            }
            finally
            {
                game.phase23.catalog = catalog;
            }
        }

        // Existing regression scenarios ask to use a control directly; reveal its new owning screen first.
        IEnumerator LicenseInput()
        {
            var shell = Shell();
            shell.OpenSettings();
            yield return Frames();
            var button = shell.settingsPanel.GetComponentsInChildren<Button>(true).First(b => b.name == "Font license");
            button.onClick.Invoke();
            yield return Frames();
            Check(
                shell.Top.name == "FontLicensePanel" && shell.Depth == 2,
                "Font license is accessible in settings stack"
            );
            var scroll = shell.Top.GetComponentInChildren<ScrollRect>();
            float before = scroll.verticalNormalizedPosition;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.PageDown));
            yield return Frames(2);
            Check(scroll.verticalNormalizedPosition < before, "Keyboard PageDown scrolls long license text");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Frames(2);
            shell.Pop();
            shell.Pop();
            var theme = UnityEngine.Object.FindFirstObjectByType<PixelHomePresenter>().theme;
            Check(
                theme.fontLicense != null && theme.font.sourceFontFile != null,
                "OFL notice and source font are included in UI theme"
            );
        }

        IEnumerator NavigationGuard()
        {
            Nav().Location(SceneId.School);
            Nav().Location(SceneId.City);
            int frames = 0;
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                if (++frames > 2000)
                {
                    throw new Exception("Navigation guard timeout");
                }

                yield return null;
            }

            yield return Frames();
            Check(
                GameContext.Instance.transitions.CurrentScene == SceneId.School,
                "Repeated UI destination request ignored while loading"
            );
            Nav().CloseAll();
            yield return Route(SceneId.Home);
        }

        public static Button LocationForRegression(SceneId id)
        {
            var nav = UnityEngine.Object.FindFirstObjectByType<UINavigationService>();
            if (nav == null)
            {
                return null;
            }

            nav.Activity();
            return nav
                .activityPanel.GetComponentsInChildren<PixelNavigationButton>(true)
                .First(b => b.target == PixelNavigationButton.Target.Location && b.location == id)
                .GetComponent<Button>();
        }

        public static GameObject RevealForRegression(Button button)
        {
            var shell = UIShell.For(button);
            if (shell == null)
            {
                return null;
            }

            Transform panel = button.transform;
            while (panel.parent != null && panel.parent != shell.overlayRoot)
            {
                panel = panel.parent;
            }

            GameObject revealed = null;
            if (panel.parent == shell.overlayRoot && shell.Top != panel.gameObject)
            {
                shell.Push(panel.gameObject);
                revealed = panel.gameObject;
            }

            foreach (var tabs in panel.GetComponentsInChildren<PixelTabGroup>(true))
            {
                if (tabs.pages == null)
                {
                    continue;
                }

                for (int i = 0; i < tabs.pages.Length; i++)
                {
                    if (tabs.pages[i] != null && button.transform.IsChildOf(tabs.pages[i].transform))
                    {
                        tabs.Select(i);
                    }
                }
            }

            return revealed;
        }
    }
}
#endif
