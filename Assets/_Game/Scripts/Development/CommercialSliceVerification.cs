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
using IanGame.Commercial;

namespace IanGame.Editor
{
    public sealed class CommercialSliceVerification : MonoBehaviour
    {
        public static readonly List<string> Passed = new List<string>();
        public static string Failure;
        public static string Progress;
        public static bool Finished;
        GameManager game;
        SaveData original;
        string slot;
        bool auto;
        bool lifecycle;
        bool realtime;
        int motion;
        int transitions;
        static readonly BindingFlags Flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        FieldInfo Slot => typeof(SaveManager).GetField("fileName", Flags);

        public void Begin()
        {
            StartCoroutine(Run());
        }

        [Serializable]
        class Results
        {
            public string[] passed;
            public string failure;
        }

        void Check(bool ok, string label)
        {
            if (!ok)
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

        IEnumerator Frames(int count = 3)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        IEnumerator Route(SceneId id)
        {
            Check(GameContext.Instance.transitions.Navigate(id), "Navigate " + id);
            float deadline = Time.realtimeSinceStartup + 20;
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    throw new Exception("Transition deadline " + id);
                }

                yield return null;
            }

            Check(SceneManager.GetActiveScene().name == id.ToString(), "Destination active " + id);
            yield return Frames();
        }

        void Shot(string file)
        {
            ScreenCapture.CaptureScreenshot("Docs/CommercialVisual/After/" + file + ".png");
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
            motion = PlayerPrefs.GetInt("ian.ui.reduceMotion", 0);
            transitions = PlayerPrefs.GetInt("ian.visual.transitions", 0);
            Slot.SetValue(game.saves, "commercial-verify-" + Guid.NewGuid().ToString("N") + ".json");
            game.autoSaveAfterAction = game.autoSaveOnLifecycle = false;
            game.clock.realtimeEnabled = false;
            PlayerPrefs.SetInt("ian.ui.reduceMotion", 0);
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
                catch (Exception e)
                {
                    Failure = e.ToString();
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
                PlayerPrefs.SetInt("ian.ui.reduceMotion", motion);
                PlayerPrefs.SetInt("ian.visual.transitions", transitions);
            }
            catch (Exception e)
            {
                Failure += "\nRestore " + e;
            }

            File.WriteAllText(
                "Docs/CommercialVisual/SLICE_TEST_RESULTS.json",
                JsonUtility.ToJson(new Results { passed = Passed.ToArray(), failure = Failure }, true)
            );
            Finished = true;
            Progress = "Complete";
            Destroy(gameObject);
        }

        IEnumerator Scenario()
        {
            yield return Route(SceneId.Home);
            SetSize(1920, 1080);
            yield return Frames(8);
            var room = UnityEngine.Object.FindFirstObjectByType<RoomAtmosphere>();
            int tick = room.TickCount,
                frame = Time.frameCount;
            yield return new WaitForSecondsRealtime(.4f);
            Check(
                Time.frameCount > frame && room.TickCount > tick,
                "Game frames and independent room animation advance"
            );
            Check(
                room.character.sprite == room.character.GetComponent<SpriteMotionPlayer>().library.fallback,
                "Official Ian sprite retained"
            );
            var pp = Camera.main.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            Check(
                pp.refResolutionX == 640 && pp.refResolutionY == 360 && pp.assetsPPU == 100 && pp.pixelRatio == 3,
                "1920x1080 world renders at 3x integer scale"
            );
            Shot("Home-1920x1080");
            yield return Frames();
            for (int i = 0; i < 2; i++)
            {
                string before = JsonUtility.ToJson(Capture());
                var t = GameContext.Instance.transitions;
                Check(t.Navigate(SceneId.School), "Home to School accepted");
                Check(!t.Navigate(SceneId.Home), "Duplicate transition rejected");
                Check(!game.TryAction(4), "Actions blocked while scene load owns clock");
                float end = Time.realtimeSinceStartup + 20;
                int sample = 0;
                bool uniqueInput = true;
                while (t.IsTransitioning)
                {
                    if (Time.realtimeSinceStartup > end)
                    {
                        throw new Exception("Transition stalled");
                    }

                    uniqueInput &=
                        UnityEngine
                            .Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None)
                            .Count(e => e.isActiveAndEnabled) <= 1;
                    if (i == 0 && sample < 5)
                    {
                        Shot("Transition-" + sample);
                        sample++;
                        yield return new WaitForSecondsRealtime(.12f);
                    }
                    else
                    {
                        yield return null;
                    }
                }

                Check(uniqueInput, "No simultaneous EventSystems during additive staging #" + i);
                // A denied action can log, but it cannot mutate any saved simulation data.
                Check(JsonUtility.ToJson(Capture()) == before, "Full model preserved Home -> School #" + i);
                Check(
                    UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1
                        && UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1,
                    "Single manager and input system after load"
                );
                if (i == 0)
                {
                    Shot("School-1920x1080");
                    yield return Frames();
                }

                before = JsonUtility.ToJson(Capture());
                yield return Route(SceneId.Home);
                Check(JsonUtility.ToJson(Capture()) == before, "Full model preserved School -> Home #" + i);
            }

            var transition = GameContext.Instance.transitions;
            var previous = SceneManager.GetActiveScene();
            transition.presentation.loader.FailNextLoad = true;
            yield return RouteFailure();
            Check(
                SceneManager.GetActiveScene() == previous
                    && !transition.IsTransitioning
                    && !transition.presentation.overlay.group.blocksRaycasts
                    && game.clock.IsRunning,
                "Injected load failure keeps previous scene and restores clock/input"
            );
            Check(
                !transition.Navigate(SceneId.Battle) && !transition.IsTransitioning,
                "Unimplemented route rejected without hiding screen"
            );
            foreach (var dimensions in new[] { new[] { 1280, 720 }, new[] { 2400, 1080 } })
            {
                SetSize(dimensions[0], dimensions[1]);
                yield return Frames(8);
                Check(
                    Screen.width == dimensions[0] && Screen.height == dimensions[1],
                    "Actual Game View " + dimensions[0] + "x" + dimensions[1]
                );
                var p = UnityEngine.Object.FindFirstObjectByType<PixelHomePresenter>();
                var r = p.navigation.navigation.GetComponent<RectTransform>();
                var corners = new Vector3[4];
                r.GetWorldCorners(corners);
                Check(
                    corners.All(c => c.x >= 0 && c.x <= Screen.width && c.y >= 0 && c.y <= Screen.height),
                    "Navigation inside screen " + Screen.width
                );
                Shot("Home-" + dimensions[0] + "x" + dimensions[1]);
                yield return Frames();
            }

            SetSize(1920, 1080);
            yield return Frames(8);
            var presenter = UnityEngine.Object.FindFirstObjectByType<PixelHomePresenter>();
            presenter.navigation.Life();
            yield return Frames(12);
            var drink = presenter.hud.drinkButton;
            var point = RectTransformUtility.WorldToScreenPoint(null, drink.GetComponent<RectTransform>().position);
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = point,
                button = PointerEventData.InputButton.Left,
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Any(h => h.gameObject == drink.gameObject), "Touch/mouse raycast reaches visible drink button");
            Check(EventSystem.current.currentSelectedGameObject != null, "Modal has keyboard focus");
            int day = game.clock.Day;
            ExecuteEvents.Execute(drink.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Check(
                game.clock.Day == day + game.actions.Describe(4).days,
                "Visible action button advances time exactly once"
            );
            presenter.navigation.CloseAll();
            yield return new WaitForSecondsRealtime(.22f);
            var hud = UnityEngine.Object.FindFirstObjectByType<CommercialHUD>();
            Check(
                hud.receipt.gameObject.activeSelf && hud.feedback.text.Contains("수분"),
                "Action produces visible pixel report"
            );
            Shot("Home-ActionFeedback");
            yield return Frames();
            game.Pause();
            yield return Route(SceneId.School);
            Check(
                game.State == GameState.Paused && !game.clock.IsRunning,
                "Paused state stays paused after scene transition"
            );
            game.Resume();
            PlayerPrefs.SetInt("ian.visual.transitions", 2);
            yield return Route(SceneId.Home);
            Check(
                !transition.presentation.overlay.gameObject.activeSelf,
                "Disabled transition still loads safely and removes overlay"
            );
            PlayerPrefs.SetInt("ian.visual.transitions", 0);
            Check(game.missionCatalog.missions.Length == 60, "60 mission definitions retained");
        }

        IEnumerator RouteFailure()
        {
            Check(GameContext.Instance.transitions.Navigate(SceneId.School), "Injected failing route starts");
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            Check(!string.IsNullOrEmpty(GameContext.Instance.transitions.LastError), "Load failure is reported");
        }

        public static void SetSize(int width, int height)
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var type = asm.GetType("UnityEditor.GameViewSizes");
            var sizes = typeof(UnityEditor.ScriptableSingleton<>)
                .MakeGenericType(type)
                .GetProperty("instance", Flags)
                .GetValue(null);
            var kind = type.GetProperty("currentGroupType", Flags).GetValue(sizes);
            var group = type.GetMethod("GetGroup", Flags).Invoke(sizes, new[] { kind });
            int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null),
                index = -1;
            for (int i = 0; i < count; i++)
            {
                var item = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if (
                    (int)item.GetType().GetProperty("width", Flags).GetValue(item) == width
                    && (int)item.GetType().GetProperty("height", Flags).GetValue(item) == height
                )
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                var item = Activator.CreateInstance(
                    asm.GetType("UnityEditor.GameViewSize"),
                    Flags,
                    null,
                    new object[]
                    {
                        Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"),
                        width,
                        height,
                        "Commercial " + width + "x" + height,
                    },
                    null
                );
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { item });
                index = count;
            }

            var view = UnityEditor.EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));
            view.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(view, index);
            view.Focus();
            view.Repaint();
        }
    }
}
#endif
