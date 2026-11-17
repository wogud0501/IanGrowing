using System;
using System.IO;
using System.Linq;
using IanGame.Commercial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class Phase24Builder
    {
        const string Folder = "Assets/_Game/Data/Phase24/";
        static Sprite frame;
        static PixelUITheme theme;

        static RectTransform R(Transform p, string n, Vector2 min, Vector2 max) =>
            ReadabilityAndBirthBuilder.Rect(p, n, min, max);

        static TMP_Text T(Transform p, string n, string value, Vector2 min, Vector2 max) =>
            ReadabilityAndBirthBuilder.Label(p, n, value, min, max);

        static UnityEngine.UI.Button B(Transform p, string n, string value, Vector2 min, Vector2 max)
        {
            var b = ReadabilityAndBirthBuilder.Button(p, n, value, min, max);
            b.GetComponent<UnityEngine.UI.Image>().sprite = frame;
            return b;
        }

        static GameObject Window(Transform parent, string name, out RectTransform card)
        {
            var root = R(parent, name, Vector2.zero, Vector2.one);
            root.gameObject.AddComponent<CanvasGroup>();
            root.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, .45f);
            var safe = R(root, "Safe area", Vector2.zero, Vector2.one);
            safe.gameObject.AddComponent<BirthPopupSafeArea>().target = safe;
            card = R(safe, "Pixel window", new Vector2(.12f, .08f), new Vector2(.88f, .92f));
            var image = card.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = frame;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        static TMP_Text Scroll(Transform card, string name)
        {
            var rect = R(card, "Scrollable text", new Vector2(.04f, .26f), new Vector2(.96f, .85f));
            var scroll = rect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = R(rect, "Viewport", Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = R(viewport, "Content", new Vector2(0, 1), Vector2.one);
            content.pivot = new Vector2(.5f, 1);
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childControlHeight = v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            v.padding = new RectOffset(4, 4, 4, 4);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var text = T(content, name, "", Vector2.zero, Vector2.one);
            text.gameObject.AddComponent<LayoutElement>().minHeight = 64;
            text.gameObject.AddComponent<PixelTextFlow>().minimumHeight = 64;
            scroll.viewport = viewport;
            scroll.content = content;
            return text;
        }

        public static void Build()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play Mode");
            }

            Directory.CreateDirectory(Folder);
            theme = AssetDatabase.LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset");
            frame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Commercial/UI/Panel_64.png");
            var settings = AssetDatabase.LoadAssetAtPath<IdleActivitySettings>(Folder + "IdleSettings.asset");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<IdleActivitySettings>();
                settings.activities = new[]
                {
                    new IdleActivityDefinition
                    {
                        id = "work",
                        title = "자동 노동",
                        kind = IdleActivityKind.Work,
                        actionId = 0,
                        minAge = 19,
                        startLocation = SceneId.Workplace,
                    },
                    new IdleActivityDefinition
                    {
                        id = "study",
                        title = "자동 공부",
                        kind = IdleActivityKind.Study,
                        actionId = 14,
                        minAge = 7,
                        startLocation = SceneId.School,
                        periodSeconds = 60,
                    },
                    new IdleActivityDefinition
                    {
                        id = "battle",
                        title = "자동 전투",
                        kind = IdleActivityKind.Battle,
                        minAge = 7,
                        startLocation = SceneId.Battle,
                        periodSeconds = 30,
                    },
                };
                AssetDatabase.CreateAsset(settings, Folder + "IdleSettings.asset");
            }

            var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(Folder + "Deadline.asset");
            if (enemy == null)
            {
                enemy = ScriptableObject.CreateInstance<EnemyData>();
                enemy.frames = AssetDatabase
                    .LoadAssetAtPath<EnvironmentSpriteLibrary>("Assets/_Game/Data/Commercial/CityOfficeSprites.asset")
                    .billboard;
                AssetDatabase.CreateAsset(enemy, Folder + "Deadline.asset");
            }

            var prefab = PrefabUtility.LoadPrefabContents("Assets/_Game/Prefabs/GameContext.prefab");
            try
            {
                var idle = prefab.GetComponent<IdleActivityService>() ?? prefab.AddComponent<IdleActivityService>();
                idle.settings = settings;
                idle.enemy = enemy;
                prefab.GetComponent<GameManager>().idle = idle;
                PrefabUtility.SaveAsPrefabAsset(prefab, "Assets/_Game/Prefabs/GameContext.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            if (!File.Exists("Assets/_Game/Scenes/Battle.unity"))
            {
                AssetDatabase.CopyAsset("Assets/_Game/Scenes/City.unity", "Assets/_Game/Scenes/Battle.unity");
            }

            foreach (string name in new[] { "Home", "School", "City", "Workplace", "Battle" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
                var p = UnityEngine.Object.FindAnyObjectByType<PixelHomePresenter>();
                if (p.GetComponent<IdleActivityPresenter>() == null)
                {
                    UI(p, (SceneId)Enum.Parse(typeof(SceneId), name));
                }

                if (name == "Battle")
                {
                    BattleScene(p, enemy);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<SceneCatalog>("Assets/_Game/Data/SceneRoutes.asset");
            if (catalog == null)
            {
                var guid = AssetDatabase.FindAssets("t:SceneCatalog").First();
                catalog = AssetDatabase.LoadAssetAtPath<SceneCatalog>(AssetDatabase.GUIDToAssetPath(guid));
            }

            var route = catalog.Find(SceneId.Battle);
            if (route == null)
            {
                route = new SceneRoute { id = SceneId.Battle };
                catalog.routes = catalog.routes.Concat(new[] { route }).ToArray();
            }

            route.implemented = true;
            route.sceneName = "Battle";
            EditorUtility.SetDirty(catalog);
            if (!EditorBuildSettings.scenes.Any(s => s.path == "Assets/_Game/Scenes/Battle.unity"))
            {
                EditorBuildSettings.scenes = EditorBuildSettings
                    .scenes.Concat(new[] { new EditorBuildSettingsScene("Assets/_Game/Scenes/Battle.unity", true) })
                    .ToArray();
            }

            Transitions();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Home.unity");
        }

        static void UI(PixelHomePresenter p, SceneId id)
        {
            var ui = p.gameObject.AddComponent<IdleActivityPresenter>();
            ui.location = id;
            ui.shell = p.navigation.stack;
            var safe = p.GetComponent<CommercialHUD>().safeRoot;
            ui.open = B(
                safe,
                "Idle activity summary",
                "자동 활동 · 설정",
                new Vector2(.018f, .36f),
                new Vector2(.39f, .42f)
            );
            ui.summary = ui.open.GetComponentInChildren<TMP_Text>();
            RectTransform card;
            ui.panel = Window(ui.shell.overlayRoot, "Automatic activity settings", out card);
            T(card, "Title", "자동 활동 관리", new Vector2(.04f, .87f), new Vector2(.96f, .98f));
            ui.details = Scroll(card, "Automatic activity details");
            ui.startWork = B(card, "Start work", "자동 노동 시작", new Vector2(.04f, .16f), new Vector2(.48f, .25f));
            ui.startStudy = B(card, "Start study", "자동 공부 시작", new Vector2(.04f, .16f), new Vector2(.48f, .25f));
            ui.startBattle = B(
                card,
                "Start battle",
                "자동 전투 시작",
                new Vector2(.04f, .16f),
                new Vector2(.48f, .25f)
            );
            ui.stop = B(card, "Stop", "활동 중지", new Vector2(.51f, .16f), new Vector2(.96f, .25f));
            ui.school = B(card, "School", "학교", new Vector2(.04f, .06f), new Vector2(.25f, .15f));
            ui.workplace = B(card, "Workplace", "직장", new Vector2(.27f, .06f), new Vector2(.48f, .15f));
            ui.battle = B(card, "Battle", "전투", new Vector2(.50f, .06f), new Vector2(.71f, .15f));
            ui.close = B(card, "Close", "닫기", new Vector2(.73f, .06f), new Vector2(.96f, .15f));
            ui.retry = B(card, "Retry persistence", "저장 재시도", new Vector2(.04f, .16f), new Vector2(.48f, .25f));
            ui.retry.gameObject.SetActive(false);
            ui.reportPanel = Window(ui.shell.overlayRoot, "Offline progress report", out card);
            T(card, "Title", "OFFLINE — 인생 정산서", new Vector2(.04f, .87f), new Vector2(.96f, .98f));
            ui.reportText = Scroll(card, "Offline report body");
            ui.confirmReport = B(card, "Acknowledge", "결과 확인", new Vector2(.15f, .06f), new Vector2(.85f, .20f));
            var settingsContent = ui.shell.settingsPanel.GetComponentsInChildren<ScrollRect>(true).First().content;
            ui.offlineToggle = B(settingsContent, "Offline setting", "오프라인 자동 활동", Vector2.zero, Vector2.one);
            var le = ui.offlineToggle.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 52;
            var statusContent = ui.shell.statusPanel.GetComponentsInChildren<ScrollRect>(true).First().content;
            ui.statusReport = B(statusContent, "Offline summary", "오프라인 정산 내역", Vector2.zero, Vector2.one);
            le = ui.statusReport.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 52;
        }

        static void BattleScene(PixelHomePresenter p, EnemyData enemy)
        {
            var ui = p.GetComponent<IdleActivityPresenter>();
            p.GetComponent<CommercialHUD>().title.text = "마감 골목 / BATTLE";
            foreach (string name in new[] { "City activity access", "Place entry hotspot" })
            {
                var t = p.GetComponent<CommercialHUD>().safeRoot.Find(name);
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                }
            }

            if (ui.enemyRenderer != null)
            {
                return;
            }

            var room = UnityEngine.Object.FindAnyObjectByType<RoomAtmosphere>();
            room.character.transform.localPosition = new Vector3(-.8f, -.55f, 0);
            room.characterOrigin = room.character.transform.localPosition;
            ui.ianRenderer = room.character;
            var enemyObject = new GameObject("Deadline / reused animated billboard", typeof(SpriteRenderer));
            enemyObject.transform.SetParent(room.transform);
            enemyObject.transform.localPosition = new Vector3(.9f, -.2f, 0);
            ui.enemyRenderer = enemyObject.GetComponent<SpriteRenderer>();
            ui.enemyRenderer.sprite = enemy.frames[0];
            ui.enemyRenderer.sortingOrder = 9;
            enemyObject.transform.localScale = Vector3.one * (1.0f / ui.enemyRenderer.sprite.bounds.size.y);
            var safe = p.GetComponent<CommercialHUD>().safeRoot;
            var card = R(safe, "Battle status", new Vector2(.018f, .44f), new Vector2(.39f, .72f));
            var image = card.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = frame;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            ui.battleText = T(
                card,
                "Battle outcome",
                "적: 마감 알림",
                new Vector2(.03f, .18f),
                new Vector2(.97f, .97f)
            );
            var bar = R(card, "Enemy HP", new Vector2(.06f, .06f), new Vector2(.94f, .13f));
            ui.enemyHealth = bar.gameObject.AddComponent<UnityEngine.UI.Image>();
            ui.enemyHealth.sprite = theme.fill;
            ui.enemyHealth.type = UnityEngine.UI.Image.Type.Filled;
            ui.enemyHealth.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            ui.enemyHealth.color = theme.coral;
            ui.fight = B(safe, "Fight once", "1회 전투", new Vector2(.018f, .105f), new Vector2(.20f, .18f));
        }

        static void Transitions()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/_Game/Prefabs/GameContext.prefab");
            try
            {
                var c = root.GetComponent<TransitionController>();
                var list = c.profiles.ToList();
                foreach (var id in new[] { SceneId.Home, SceneId.School, SceneId.City, SceneId.Workplace })
                {
                    foreach (bool incoming in new[] { true, false })
                    {
                        var from = incoming ? id : SceneId.Battle;
                        var to = incoming ? SceneId.Battle : id;
                        if (list.Any(p => p.from == from && p.to == to))
                        {
                            continue;
                        }

                        string path = Folder + "Transition-" + from + "-" + to + ".asset";
                        var profile = AssetDatabase.LoadAssetAtPath<TransitionProfile>(path);
                        if (profile == null)
                        {
                            profile = ScriptableObject.CreateInstance<TransitionProfile>();
                            profile.from = from;
                            profile.to = to;
                            profile.departure = "퇴근 전에 마지막으로 하나만...";
                            profile.arrival =
                                to == SceneId.Battle ? "새로운 마감이 도착했습니다." : "오늘의 전투를 정산했습니다.";
                            profile.curtain = Color.white;
                            AssetDatabase.CreateAsset(profile, path);
                        }

                        list.Add(profile);
                    }
                }

                c.profiles = list.ToArray();
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Prefabs/GameContext.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void PolishReadablePanels()
        {
            foreach (string name in new[] { "Home", "School", "City", "Workplace", "Battle" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
                var p = UnityEngine.Object.FindAnyObjectByType<PixelHomePresenter>();
                if (p.recent.GetComponentInParent<ScrollRect>() == null)
                {
                    var parent = p.recent.transform.parent;
                    var r = R(parent, "Recent message scroll", Vector2.zero, Vector2.one);
                    r.offsetMin = new Vector2(8, 8);
                    r.offsetMax = new Vector2(-8, -8);
                    var fill = r.gameObject.AddComponent<LayoutElement>();
                    fill.minHeight = 16;
                    fill.flexibleHeight = 1;
                    var scroll = r.gameObject.AddComponent<ScrollRect>();
                    scroll.horizontal = false;
                    scroll.movementType = ScrollRect.MovementType.Clamped;
                    var viewport = R(r, "Viewport", Vector2.zero, Vector2.one);
                    viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
                    viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                    var content = R(viewport, "Content", new Vector2(0, 1), Vector2.one);
                    content.pivot = new Vector2(.5f, 1);
                    var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                    layout.childControlWidth = layout.childControlHeight = true;
                    layout.childForceExpandHeight = false;
                    content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter
                        .FitMode
                        .PreferredSize;
                    p.recent.transform.SetParent(content, false);
                    p.recent.enableAutoSizing = false;
                    p.recent.fontSize = 18;
                    p.recent.maxVisibleLines = 99999;
                    p.recent.overflowMode = TextOverflowModes.Overflow;
                    var le =
                        p.recent.GetComponent<LayoutElement>() ?? p.recent.gameObject.AddComponent<LayoutElement>();
                    le.minHeight = 32;
                    var flow =
                        p.recent.GetComponent<PixelTextFlow>() ?? p.recent.gameObject.AddComponent<PixelTextFlow>();
                    flow.minimumHeight = 32;
                    scroll.viewport = viewport;
                    scroll.content = content;
                }

                var activities = UnityEngine.Object.FindAnyObjectByType<Phase23Presenter>();
                if (activities != null)
                {
                    for (int i = 0; i < activities.actionIds.Length; i++)
                    {
                        if (activities.actionIds[i] == 24 || activities.actionIds[i] == 25)
                        {
                            var row = activities.activityButtons[i].GetComponent<LayoutElement>();
                            if (row != null)
                            {
                                row.minHeight = row.preferredHeight = 72;
                            }
                        }
                    }
                }

                var presenter = p.GetComponent<IdleActivityPresenter>();
                if (presenter != null)
                {
                    var overlay = presenter.reportPanel.GetComponent<Canvas>();
                    if (overlay == null)
                    {
                        overlay = presenter.reportPanel.AddComponent<Canvas>();
                    }

                    overlay.overrideSorting = true;
                    overlay.sortingOrder = 350;
                    if (presenter.reportPanel.GetComponent<GraphicRaycaster>() == null)
                    {
                        presenter.reportPanel.AddComponent<GraphicRaycaster>();
                    }

                    if (name == "Battle")
                    {
                        var room = UnityEngine.Object.FindAnyObjectByType<RoomAtmosphere>();
                        room.characterOrigin = new Vector3(0, -.55f, 0);
                        room.character.transform.localPosition = room.characterOrigin;
                        presenter.enemyRenderer.transform.localPosition = new Vector3(.9f, .35f, 0);
                    }
                }

                var birth = p.GetComponent<BirthIntroductionPopup>();
                if (birth != null && birth.frame.Find("Birth seal") == null)
                {
                    var rect = R(birth.frame, "Birth seal", new Vector2(.83f, .83f), new Vector2(.95f, .95f));
                    var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
                    image.sprite = AssetDatabase
                        .LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset")
                        .Icon(PixelIcon.Life);
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Home.unity");
        }
    }
}
