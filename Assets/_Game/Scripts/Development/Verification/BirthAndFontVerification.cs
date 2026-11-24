#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace IanGame.Editor
{
    public sealed class BirthAndFontVerification : MonoBehaviour
    {
        public static List<string> Passed = new List<string>();
        public static bool Finished;
        public static string Failure;
        GameManager g;
        SaveData original;
        string slot;
        bool auto;
        bool lifecycle;
        bool realtime;
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo Slot => typeof(SaveManager).GetField("fileName", Flags);

        [Serializable]
        class Results
        {
            public string[] passed;
            public string failure;
        }

        SaveData Capture() => g.CaptureState();

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

        IEnumerator Frames(int n = 5)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
        }

        IEnumerator Route(SceneId id)
        {
            GameContext.Instance.transitions.Navigate(id);
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return Frames();
        }

        IEnumerator Run()
        {
            g = GameContext.Instance.game;
            original = Capture();
            slot = (string)Slot.GetValue(g.saves);
            auto = g.autoSaveAfterAction;
            lifecycle = g.autoSaveOnLifecycle;
            realtime = g.clock.realtimeEnabled;
            Slot.SetValue(g.saves, "birth-suite-" + Guid.NewGuid().ToString("N") + ".json");
            g.autoSaveAfterAction = g.autoSaveOnLifecycle = g.clock.realtimeEnabled = false;
            Passed.Clear();
            Failure = null;
            Finished = false;
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
                g.saves.AllowExplicitNewGame();
                File.WriteAllText(g.saves.SavePath, JsonUtility.ToJson(original));
                g.Load();
                Slot.SetValue(g.saves, slot);
                g.autoSaveAfterAction = auto;
                g.autoSaveOnLifecycle = lifecycle;
                g.clock.realtimeEnabled = realtime;
            }
            catch (Exception e)
            {
                Failure += " RESTORE " + e;
            }

            File.WriteAllText(
                "Docs/Phase24/BIRTH_FONT_TESTS.json",
                JsonUtility.ToJson(new Results { passed = Passed.ToArray(), failure = Failure }, true)
            );
            Finished = true;
            Destroy(gameObject);
        }

        IEnumerator Scenario()
        {
            yield return Route(SceneId.MainMenu);
            var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuPresenter>();
            menu.newGameButton.onClick.Invoke();
            var shell = UIShell.For(menu);
            shell.AcceptNewLife();
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return Frames(8);
            var p = UnityEngine.Object.FindAnyObjectByType<BirthIntroductionPopup>();
            Check(p != null && p.IsShowing, "Actual new game menu -> Home -> birth popup");
            Check(g.growth.Age == 0 && g.economy.Cash == 0 && g.economy.Debt == 1000000, "Newborn data unchanged");
            Check(!g.clock.IsRunning, "Intro suspends simulation");
            string reason;
            Check(!g.CanPerformAction(4, out reason), "Intro gates actions");
            shell = p.shell;
            shell.Pop();
            Check(shell.Top == p.view, "Escape cannot skip birth acknowledgement");
            Check(!g.RecentLogs.Any(l => l.Contains("태어났습니다")), "Long birth text absent from general log");
            g.Save();
            IanGame.SaveData pending;
            string message;
            Check(
                g.saves.TryRead(out pending, out message) && !pending.birthIntroCompleted,
                "Unconfirmed intro persists for interrupted launch"
            );
            g.Load();
            yield return Frames();
            Check(p.IsShowing && g.BirthIntroductionPending, "Reload before confirmation remains pending");
            foreach (var size in new[] { new[] { 1280, 720 }, new[] { 1920, 1080 }, new[] { 2400, 1080 } })
            {
                CommercialSliceVerification.SetSize(size[0], size[1]);
                yield return Frames(10);
                yield return new WaitForSecondsRealtime(.25f);
                Canvas.ForceUpdateCanvases();
                foreach (var t in p.view.GetComponentsInChildren<TMP_Text>())
                {
                    t.ForceMeshUpdate();
                    Check(
                        t.preferredHeight <= t.rectTransform.rect.height + 1,
                        "Popup height fits " + t.name + " " + size[0]
                    );
                    Check(
                        !t.text.Any(c => !char.IsControl(c) && !t.font.HasCharacter(c, true, true)),
                        "Popup glyphs complete " + t.name + " " + size[0]
                    );
                }

                ScreenCapture.CaptureScreenshot("Docs/Phase24/Screenshots/Birth-" + size[0] + "x" + size[1] + ".png");
                yield return Frames();
            }

            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, p.begin.transform.position),
                button = PointerEventData.InputButton.Left,
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Any(h => h.gameObject == p.begin.gameObject), "Actual begin button receives touch raycast");
            ExecuteEvents.Execute(p.begin.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.25f);
            Check(
                !g.BirthIntroductionPending && g.clock.IsRunning && !p.view.activeSelf,
                "Acknowledgement closes popup and resumes gameplay"
            );
            Check(
                g.saves.TryReadAutomatic(out pending, out message) && pending.birthIntroCompleted,
                "Acknowledgement saved permanently"
            );
            g.Load();
            yield return Frames();
            Check(!p.IsShowing, "Confirmed checkpoint does not reopen");
            yield return Route(SceneId.School);
            yield return Route(SceneId.Home);
            Check(
                !UnityEngine.Object.FindAnyObjectByType<BirthIntroductionPopup>().IsShowing,
                "Home re-entry does not reopen"
            );
            string json = JsonUtility.ToJson(Capture());
            json = System.Text.RegularExpressions.Regex.Replace(json, "\"birthIntroVersion\":1,", "");
            json = System.Text.RegularExpressions.Regex.Replace(json, "\"birthIntroCompleted\":true,", "");
            File.WriteAllText(g.saves.SavePath, json);
            g.Load();
            Check(!g.BirthIntroductionPending, "Existing save missing birth extension migrates to completed");
            g.Log("검증용 미션 완료 알림");
            Check(g.RecentLogs.Last().Contains("검증용 미션 완료"), "Other system messages retained");
            g.NewGame();
            Check(g.BirthIntroductionPending, "Explicit new life resets introduction");
            g.CompleteBirthIntroduction();
            var f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/_Game/UI/Pixel/Fonts/PFStardust/PFStardust_Main.asset"
            );
            Check(
                "이안키우기0123456789금요일₩".All(c => f.HasCharacter(c, true, true))
                    && f.fallbackFontAssetTable.Any(a => a.sourceFontFile && a.faceInfo.familyName.Contains("Stardust")),
                "Game glyphs and extended Stardust source"
            );
            Check(f.atlasTextures.All(t => t.filterMode == FilterMode.Point), "Readable bitmap atlases use Point");
            CommercialSliceVerification.SetSize(1920, 1080);
        }
    }
}
#endif
