#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IanGame.Editor
{
    public class Phase256Verification : MonoBehaviour
    {
        [Serializable]
        class Results
        {
            public List<string> passed = new List<string>();
            public List<string> uiIssues = new List<string>();
            public string failure;
        }

        public static bool Finished;
        public static string Progress;
        Results result = new Results();
        GameManager g;
        SaveData original;
        string slot;
        bool auto;
        bool life;
        bool realtime;
        bool realLife;
        FieldInfo Slot => typeof(SaveManager).GetField("fileName", BindingFlags.Instance | BindingFlags.NonPublic);

        public void Begin()
        {
            DontDestroyOnLoad(gameObject);
            StartCoroutine(Run());
        }

        void Check(bool ok, string text)
        {
            if (!ok)
            {
                throw new Exception(text);
            }

            result.passed.Add(text);
        }

        IEnumerator Frames(int n = 8)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
        }

        IEnumerator Route(SceneId id)
        {
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            var s = UnityEngine.Object.FindAnyObjectByType<UIShell>();
            if (s != null)
            {
                while (s.Depth > 0)
                {
                    s.Pop();
                }
            }

            Check(GameContext.Instance.transitions.Navigate(id), "Navigate " + id);
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return Frames();
        }

        IEnumerator Run()
        {
            Finished = false;
            yield return Frames(15);
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            g = GameContext.Instance.game;
            original = g.CaptureState();
            slot = (string)Slot.GetValue(g.saves);
            auto = g.autoSaveAfterAction;
            life = g.autoSaveOnLifecycle;
            realtime = g.clock.realtimeEnabled;
            realLife = g.realtimeLifeEnabled;
            Slot.SetValue(g.saves, "phase256-" + Guid.NewGuid().ToString("N") + ".json");
            g.autoSaveAfterAction = g.autoSaveOnLifecycle = g.clock.realtimeEnabled = g.realtimeLifeEnabled = false;
            var stack = new Stack<IEnumerator>();
            stack.Push(Scenario());
            while (stack.Count > 0)
            {
                object next = null;
                bool more = false;
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
                    result.failure = ex.ToString();
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
                g.RestoreState(original);
                Slot.SetValue(g.saves, slot);
                g.autoSaveAfterAction = auto;
                g.autoSaveOnLifecycle = life;
                g.clock.realtimeEnabled = realtime;
                g.realtimeLifeEnabled = realLife;
            }
            catch (Exception ex)
            {
                result.failure += " RESTORE " + ex;
            }

            Directory.CreateDirectory("Docs/Phase256");
            File.WriteAllText("Docs/Phase256/PLAY_TEST_RESULTS.json", JsonUtility.ToJson(result, true));
            Finished = true;
            Progress = result.failure ?? "Complete";
            Destroy(gameObject);
        }

        IEnumerator Scenario()
        {
            Progress = "New game and starter home";
            g.NewGame();
            Check(
                g.economy.Cash == 0 && g.economy.Housing.residenceId == "starter",
                "Newborn receives free starter home without wallet charge"
            );
            Check(
                g.economy.Housing.owned.Count == 2 && g.economy.Housing.placed.Count == 0,
                "Only two basic facilities supplied"
            );
            Check(g.CompleteBirthIntroduction(), "Actual birth confirmation checkpoint");
            g.clock.realtimeEnabled = false;
            yield return Route(SceneId.Home);
            var ui = UnityEngine.Object.FindAnyObjectByType<HomeManagementPresenter>();
            var env = UnityEngine.Object.FindAnyObjectByType<HomeEnvironmentPresenter>();
            Check(
                env.room.background.sprite == ui.catalog.residences[0].preview,
                "Actual Home renderer uses starter wood/white room"
            );
            env.ToggleBathroom();
            Check(
                env.room.background.sprite == ui.catalog.bathroom,
                "Actual bathroom toggle selects jade tile background"
            );
            Shot("Bathroom");
            yield return Frames();
            env.ToggleBathroom();
            Shot("Home");
            yield return Frames();
            string why;
            Check(
                !g.economy.BuyFurniture("poor", "bookshelf")
                    && !g.economy.Housing.owned.Contains("bookshelf")
                    && g.economy.Cash == 0,
                "Insufficient furniture purchase has no mutation"
            );
            ui.Refresh();
            Check(
                !ui.buy[2].interactable && ui.furnitureInfo[2].text.Contains("부족"),
                "Insufficient balance disables real furniture button with explanation"
            );
            Check(!g.economy.PlaceFurniture("bookshelf", new Vector2(0, -1)), "Unowned furniture placement rejected");
            Check(!g.economy.MoveResidence("poor-move", "improved"), "Locked/poor move refused");
            var data = g.CaptureState();
            data.stats.money = 20000;
            data.progression.age = 30;
            g.RestoreState(data);
            g.clock.realtimeEnabled = false;
            yield return Frames();
            var nav = ui.GetComponent<PolishedNavigation>();
            nav.Open(5);
            ui.Tab(1);
            yield return Frames();
            long cash = g.economy.Cash;
            ui.buy[2].onClick.Invoke();
            yield return Frames();
            Check(ui.shell.Top == ui.confirmation && g.economy.Cash == cash, "Purchase confirmation precedes charge");
            ui.accept.onClick.Invoke();
            ui.accept.onClick.Invoke();
            yield return Frames();
            Check(
                g.economy.Cash == cash - 600 && g.economy.Housing.owned.Count(id => id == "bookshelf") == 1,
                "Actual UI confirm charges exactly once and grants furniture"
            );
            Check(!g.economy.BuyFurniture("duplicate-owned", "bookshelf"), "Owned furniture cannot be bought twice");
            cash = g.economy.Cash;
            ui.place[2].onClick.Invoke();
            yield return Frames();
            Check(g.economy.Housing.placed.Count == 1 && g.economy.Cash == cash, "Actual placement is free");
            Check(GameObject.Find("Owned furniture / bookshelf") != null, "Purchased furniture appears in world");
            ui.reposition[2].onClick.Invoke();
            yield return Frames();
            Check(g.economy.Cash == cash, "Reposition is free");
            ui.Tab(2);
            ui.move[1].onClick.Invoke();
            yield return Frames();
            Check(ui.shell.Top == ui.confirmation && g.economy.Cash == cash, "Move confirmation precedes charge");
            ui.accept.onClick.Invoke();
            ui.accept.onClick.Invoke();
            yield return Frames();
            Check(
                g.economy.Cash == cash - 10000 && g.economy.Housing.residenceId == "improved",
                "Actual move charges configured cost once"
            );
            Check(!g.economy.MoveResidence("repeat-move", "improved"), "Current residence rejects repeat move");
            Check(g.economy.Housing.placed.Count == 1, "Move preserves owned/placed furniture");
            SaveData saved;
            string message;
            Check(
                g.saves.TryReadAutomatic(out saved, out message)
                    && saved.economy.housing.residenceId == "improved"
                    && saved.economy.housing.owned.Contains("bookshelf"),
                "Real autosave IO retains house and owned furniture"
            );
            g.economy.Restore(new EconomySnapshot());
            g.RestoreState(saved);
            g.clock.realtimeEnabled = false;
            yield return Frames();
            Check(
                g.economy.Housing.residenceId == "improved" && g.economy.Housing.placed.Count == 1,
                "Save/load restores house and placement"
            );
            // Real IO failure rolls back the complete game snapshot.
            string good = (string)Slot.GetValue(g.saves),
                bad = "phase256-failure-" + Guid.NewGuid().ToString("N") + ".json";
            Directory.CreateDirectory(
                Path.Combine(Application.persistentDataPath, Path.GetFileNameWithoutExtension(bad) + ".autosave.json")
            );
            Slot.SetValue(g.saves, bad);
            cash = g.economy.Cash;
            ui.Tab(1);
            ui.buy[3].onClick.Invoke();
            yield return Frames();
            ui.accept.onClick.Invoke();
            yield return Frames();
            Check(
                g.economy.Cash == cash
                    && !g.economy.Housing.owned.Contains("lamp")
                    && ui.feedback.text.Contains("저장 실패"),
                "Failed save rolls back wallet, ownership and receipt"
            );
            Slot.SetValue(g.saves, good);
            var legacy = g.CaptureState();
            legacy.economy.housing = null;
            g.RestoreState(legacy);
            Check(
                g.economy.Housing.floor == "wood-linoleum" && g.economy.Housing.owned.Count == 2,
                "Missing optional housing receives safe starter defaults"
            );
            saved.economy.housing.wallpaper = "legacy-custom";
            g.RestoreState(saved);
            Check(
                g.economy.Housing.wallpaper == "legacy-custom" && g.economy.Housing.owned.Contains("bookshelf"),
                "Existing nondefault interior and purchases preserved"
            );
            saved.economy.housing.wallpaper = "warm-white";
            g.RestoreState(saved);
            g.clock.realtimeEnabled = false;
            Progress = "All major windows and aspect ratios";
            foreach (var id in new[] { SceneId.Home, SceneId.School, SceneId.Workplace, SceneId.City, SceneId.Battle })
            {
                yield return Route(id);
                ui = UnityEngine.Object.FindAnyObjectByType<HomeManagementPresenter>();
                nav = ui.GetComponent<PolishedNavigation>();
                if (id == SceneId.StockMarket)
                {
                    ui.shell.Close(UnityEngine.Object.FindAnyObjectByType<FinancePresenter>().marketPanel);
                }

                foreach (
                    var size in new[]
                    {
                        new[] { 1280, 720 },
                        new[] { 1920, 1080 },
                        new[] { 2400, 1080 },
                        new[] { 1080, 1920 },
                    }
                )
                {
                    CommercialSliceVerification.SetSize(size[0], size[1]);
                    yield return Frames();
                    foreach (int n in Enumerable.Range(0, 7))
                    {
                        nav.Open(n);
                        yield return Frames();
                        Audit(nav.panels[n], id + "/" + n + "/" + size[0]);
                        if (n == 5)
                        {
                            for (int tab = 0; tab < 3; tab++)
                            {
                                ui.Tab(tab);
                                yield return Frames();
                                Audit(ui.panel, id + "/Housing" + tab + "/" + size[0]);
                            }
                        }

                        if (size[0] == 1280 && id == SceneId.Home)
                        {
                            yield return new WaitForSecondsRealtime(.2f);
                            Shot("Menu-" + n);
                        }

                        yield return Frames(2);
                    }

                    foreach (
                        var extra in new[]
                        {
                            ui.shell.settingsPanel,
                            ui.shell.statusPanel,
                            ui.shell.pausePanel,
                            ui.shell.confirmPanel,
                            ui.GetComponent<PixelHomePresenter>().navigation.inventoryPanel,
                            ui.GetComponent<PixelHomePresenter>().mission.educationPanel,
                            ui.GetComponent<PixelHomePresenter>().navigation.locationPanel,
                            UnityEngine.Object.FindAnyObjectByType<FinancePresenter>().economyPanel,
                            UnityEngine.Object.FindAnyObjectByType<IdleActivityPresenter>().panel,
                            UnityEngine.Object.FindAnyObjectByType<IdleActivityPresenter>().reportPanel,
                        }
                    )
                    {
                        if (extra == null)
                        {
                            continue;
                        }

                        ui.shell.Push(extra);
                        yield return Frames();
                        Audit(extra, id + "/" + extra.name + "/" + size[0]);
                        ui.shell.Close(extra);
                        yield return Frames(2);
                    }

                    while (ui.shell.Depth > 0)
                    {
                        ui.shell.Pop();
                    }

                    yield return Frames();
                    Check(
                        ui.shell.baseGroup.interactable && ui.shell.baseGroup.blocksRaycasts,
                        "Closing all restores input " + id + " " + size[0]
                    );
                }
            }

            CommercialSliceVerification.SetSize(1280, 720);
            yield return Route(SceneId.Home);
            ui = UnityEngine.Object.FindAnyObjectByType<HomeManagementPresenter>();
            nav = ui.GetComponent<PolishedNavigation>();
            foreach (
                var root in nav
                    .panels.Concat(
                        new[]
                        {
                            ui.GetComponent<PixelHomePresenter>().navigation.locationPanel,
                            ui.GetComponent<PixelHomePresenter>().mission.educationPanel,
                        }
                    )
                    .Where(r => r != null)
            )
            {
                ui.shell.ReplaceMajor(root);
                foreach (
                    var tabs in root.GetComponentsInChildren<PixelTabGroup>(true)
                        .Where(t => t.enabled && t.pages != null)
                )
                {
                    for (int i = 0; i < tabs.pages.Length; i++)
                    {
                        tabs.Select(i);
                        yield return Frames();
                        Audit(root, "Nested tabs / " + root.name + "/" + i);
                    }
                }
            }

            Check(
                g.missions.Counter(MissionCounter.FurnitureBought) == 1,
                "Furniture purchase records existing mission counter once"
            );
            Check(
                !g.missions.SupportedFeatures.HasFlag(MissionFeature.Housing),
                "Unimplemented rent and insurance remain planned"
            );
            // Actual old v4 file without the optional housing field.
            var legacyFile = g.CaptureState();
            string housing = JsonUtility.ToJson(legacyFile.economy.housing);
            string json = JsonUtility.ToJson(legacyFile).Replace(",\"housing\":" + housing, "");
            Check(!json.Contains("\"housing\":"), "Legacy fixture really omits housing field");
            File.WriteAllText(g.saves.BasePath, json);
            SaveData migrated;
            Check(
                g.saves.TryRead(out migrated, out message) && migrated.economy.housing == null,
                "Real v4 file missing housing loads without corrupting existing finance"
            );
            yield return Route(SceneId.StockMarket);
            var finance = UnityEngine.Object.FindAnyObjectByType<FinancePresenter>();
            foreach (
                var size in new[]
                {
                    new[] { 1280, 720 },
                    new[] { 1920, 1080 },
                    new[] { 2400, 1080 },
                    new[] { 1080, 1920 },
                }
            )
            {
                CommercialSliceVerification.SetSize(size[0], size[1]);
                yield return Frames();
                for (int tab = 0; tab < 4; tab++)
                {
                    finance.ShowTab(tab);
                    yield return Frames();
                    Audit(finance.marketPanel, "StockMarket/" + tab + "/" + size[0]);
                }

                if (size[0] == 1280)
                {
                    yield return new WaitForSecondsRealtime(.2f);
                    Shot("StockMarket");
                }
            }

            // Closing the market returns to City through its existing navigation behavior.
            finance.marketClose.onClick.Invoke();
            while (GameContext.Instance.transitions.IsTransitioning)
            {
                yield return null;
            }

            yield return Frames();
            yield return Route(SceneId.MainMenu);
            foreach (var size in new[] { new[] { 1280, 720 }, new[] { 1080, 1920 } })
            {
                CommercialSliceVerification.SetSize(size[0], size[1]);
                yield return Frames();
                foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    Audit(root, "MainMenu/" + size[0]);
                }
            }

            CommercialSliceVerification.SetSize(1280, 720);
            yield return Route(SceneId.Home);
            Progress = "Done";
        }

        void Shot(string name)
        {
            ScreenCapture.CaptureScreenshot("Docs/Phase256/Screenshots/" + name + ".png");
        }

        void Audit(GameObject root, string context)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(t.text))
                {
                    continue;
                }

                t.ForceMeshUpdate();
                var preferred = t.GetPreferredValues(t.text, t.rectTransform.rect.width, Mathf.Infinity);
                if (t.isTextOverflowing || preferred.y > t.rectTransform.rect.height + 3)
                {
                    string item =
                        context
                        + " | "
                        + t.name
                        + " | "
                        + preferred
                        + " > "
                        + t.rectTransform.rect.size
                        + " | "
                        + t.text.Replace("\n", " ").Substring(0, Math.Min(70, t.text.Length));
                    if (!result.uiIssues.Contains(item))
                    {
                        result.uiIssues.Add(item);
                    }
                }

                if (t.text.Any(c => !char.IsControl(c) && !t.font.HasCharacter(c, true, true)))
                {
                    result.uiIssues.Add(context + " | missing glyph " + t.name);
                }
            }
        }
    }
}
#endif
