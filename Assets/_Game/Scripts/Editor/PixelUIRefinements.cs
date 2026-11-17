using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static partial class PixelUIBuilder
    {
        static void LegacyInsets(UIShell shell, MissionUI mission)
        {
            var panels = new[]
            {
                shell.pausePanel,
                shell.settingsPanel,
                shell.statusPanel,
                shell.confirmPanel,
                mission.listPanel,
                mission.educationPanel,
            };
            foreach (var panel in panels)
            {
                if (panel == null || panel.transform.childCount == 0)
                {
                    continue;
                }

                var r = panel.transform.GetChild(0) as RectTransform;
                if (r == null)
                {
                    continue;
                }

                r.anchorMin = new Vector2(.025f, .135f);
                r.anchorMax = new Vector2(.975f, .98f);
                r.offsetMin = r.offsetMax = Vector2.zero;
            }
        }

        static void LicensePanel(UIShell shell)
        {
            if (shell.overlayRoot.GetComponentsInChildren<RectTransform>(true).Any(r => r.name == "FontLicensePanel"))
            {
                return;
            }

            Transform body;
            var panel = Modal(shell, "FontLicensePanel", "서체 라이선스 / OFL 1.1", out body);
            T(body, "License", theme.fontLicense.text, 14);
            Command(
                ExistingBody(shell.settingsPanel),
                "Font license",
                "서체 라이선스 보기",
                shell,
                UICommand.Command.Open,
                panel
            );
        }

        public static void FontLicenseUI()
        {
            theme = AssetDatabase.LoadAssetAtPath<PixelUITheme>(PixelAssetBuilder.Folder + "PixelTheme.asset");
            if (theme.fontLicense == null)
            {
                throw new InvalidOperationException("Font license is required");
            }

            var path = "Assets/_Game/Prefabs/GameHUD.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                LicensePanel(root.GetComponent<UIShell>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity");
            LicensePanel(UnityEngine.Object.FindFirstObjectByType<UIShell>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity");
        }

        static void MissionController(GameObject root, PixelHomePresenter pixel)
        {
            var m = pixel.mission;
            if (m.transform.parent == root.transform)
            {
                return;
            }

            var slot = m.transform.parent;
            var visible = m.missionTitle.transform.parent;
            visible.SetParent(slot, false);
            Fill(visible.GetComponent<RectTransform>());
            m.transform.SetParent(root.transform, false);
            var r = m.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
            r.sizeDelta = Vector2.zero;
            r.anchoredPosition = Vector2.zero;
            m.gameObject.SetActive(true);
        }

        static void CurrencyRow(PixelHomePresenter pixel)
        {
            if (pixel.cash.transform.parent.name == "CurrencyWidget")
            {
                return;
            }

            var card = pixel.cash.transform.parent;
            var row = R(card, "CurrencyWidget");
            H(row, 0, 6);
            S(row, 24);
            Icon(row, PixelIcon.Coin, 16);
            Move(pixel.cash, row, 24);
        }

        public static void FinishWidgets()
        {
            theme = AssetDatabase.LoadAssetAtPath<PixelUITheme>(PixelAssetBuilder.Folder + "PixelTheme.asset");
            var path = "Assets/_Game/Prefabs/GameHUD.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var pixel = root.GetComponent<PixelHomePresenter>();
                MissionController(root, pixel);
                LegacyInsets(root.GetComponent<UIShell>(), pixel.mission);
                pixel.cash.enableAutoSizing = true;
                pixel.cash.fontSizeMin = 14;
                pixel.cash.fontSizeMax = 20;
                C<PixelNotificationLayout>(pixel.notice.transform);
                TextStyle(pixel.noticeText, 16);
                pixel.noticeText.maxVisibleLines = 3;
                pixel.noticeText.overflowMode = TextOverflowModes.Ellipsis;
                V(pixel.notice.transform, 8, 0);
                CurrencyRow(pixel);
                var folder = "Assets/_Game/Prefabs/PixelUI/";
                PrefabUtility.SaveAsPrefabAsset(
                    pixel.vitalLabels[0].transform.parent.parent.parent.parent.gameObject,
                    folder + "StatSummaryWidget.prefab"
                );
                PrefabUtility.SaveAsPrefabAsset(
                    pixel.missionTitle.transform.parent.gameObject,
                    folder + "MissionWidget.prefab"
                );
                PrefabUtility.SaveAsPrefabAsset(
                    pixel.cash.transform.parent.parent.gameObject,
                    folder + "CurrencyWidget.prefab"
                );
                PrefabUtility.SaveAsPrefabAsset(pixel.notice, folder + "PixelNotification.prefab");
                PrefabUtility.SaveAsPrefabAsset(
                    pixel.navigation.navigation.gameObject,
                    folder + "NavigationBar.prefab"
                );
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
        }

        public static void Refine()
        {
            theme = AssetDatabase.LoadAssetAtPath<PixelUITheme>(PixelAssetBuilder.Folder + "PixelTheme.asset");
            var path = "Assets/_Game/Prefabs/GameHUD.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var p = root.GetComponent<PixelHomePresenter>();
                var m = p.mission;
                S(m.missionTitle.transform.parent.parent, 520);
                var archiveBody = m.rowContainer.parent.GetComponent<Image>();
                if (archiveBody != null)
                {
                    archiveBody.color = theme.cream;
                }

                if (m.educationPanel.GetComponentInChildren<PixelTabGroup>(true) == null)
                {
                    var oldGrid = m.activityButtons[0].transform.parent;
                    var parent = oldGrid.parent;
                    var newTabs = R(parent, "Education categories");
                    V(newTabs, 0, 8);
                    S(newTabs, 350);
                    Transform[] pages;
                    Tabs(newTabs, new[] { "학습", "과제·시험", "친구" }, out pages);
                    for (int i = 0; i < m.activityButtons.Length; i++)
                    {
                        int action = i + 7;
                        int tab =
                            action == 9 ? 2
                            : action == 10 || action == 11 || action == 12 ? 1
                            : 0;
                        Move(m.activityButtons[i], pages[tab], 44);
                        StyleButton(m.activityButtons[i]);
                    }

                    oldGrid.gameObject.SetActive(false);
                }

                foreach (var element in root.GetComponentsInChildren<LayoutElement>(true))
                {
                    element.maxWidth = float.MaxValue;
                    element.maxHeight = float.MaxValue;
                }

                foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    StyleButton(button);
                    C<PixelFocusScroll>(button);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var row = PrefabUtility.LoadPrefabContents("Assets/_Game/Prefabs/MissionRow.prefab");
            try
            {
                var view = row.GetComponent<MissionRowView>();
                if (view.titleText.name != "Mission title")
                {
                    foreach (var old in new[] { view.titleText, view.stateText, view.progressText })
                    {
                        old.gameObject.SetActive(false);
                    }

                    view.titleText = T(row.transform, "Mission title", "", 20);
                    view.stateText = T(row.transform, "Exact mission conditions", "", 16);
                    view.progressText = T(row.transform, "Mission progress", "", 16);
                }

                var image = row.GetComponent<Image>();
                image.sprite = theme.panel;
                image.color = Color.white;
                foreach (var e in row.GetComponentsInChildren<LayoutElement>(true))
                {
                    e.maxWidth = e.maxHeight = float.MaxValue;
                }

                PrefabUtility.SaveAsPrefabAsset(row, "Assets/_Game/Prefabs/MissionRow.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(row);
            }

            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity");
            var presenter = UnityEngine.Object.FindFirstObjectByType<MainMenuPresenter>();
            var canvas = presenter.GetComponentInParent<Canvas>();
            if (canvas.GetComponent<PixelCanvasScale>() == null)
            {
                var scale = canvas.gameObject.AddComponent<PixelCanvasScale>();
                scale.scaler = canvas.GetComponent<CanvasScaler>();
                scale.safe = canvas.GetComponent<UISafeLayout>();
            }

            var character = canvas.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.name == "Official Ian");
            var menuLayout = C<PixelMenuLayout>(canvas);
            if (character != null)
            {
                menuLayout.character = character.GetComponent<LayoutElement>();
            }

            var all = canvas.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in all)
            {
                if (t.name == "Imprint")
                {
                    TextStyle(t, 14, true);
                    S(t, 24);
                }
                else if (t.name == "Title" && t.text.Contains("키우기"))
                {
                    TextStyle(t, 42, false, true);
                    S(t, 142);
                }
                else if (t.name == "Tagline")
                {
                    TextStyle(t, 18);
                    S(t, 62);
                }
                else if (t.name == "Footnote")
                {
                    TextStyle(t, 16);
                    S(t, 44);
                }
                else if (t.name == "Edition")
                {
                    TextStyle(t, 14);
                    S(t, 24);
                }
                else if (t.name == "Footer")
                {
                    TextStyle(t, 14, true);
                    S(t, 24);
                }
            }

            var bg = canvas.transform.Find("Menu paper");
            if (bg != null)
            {
                bg.GetComponent<Image>().color = theme.ink;
            }

            var frame = canvas.GetComponent<UISafeLayout>().frame;
            V(frame, 12, 8);
            var actionParent = presenter.continueButton.transform.parent;
            S(actionParent, 48);
            S(presenter.continueButton, 44);
            S(presenter.newGameButton, 44);
            foreach (var button in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                StyleButton(button);
                C<PixelFocusScroll>(button);
            }

            foreach (var e in canvas.GetComponentsInChildren<LayoutElement>(true))
            {
                e.maxWidth = e.maxHeight = float.MaxValue;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity");
        }
    }
}
