using System;
using System.IO;
using System.Linq;
using IanGame.Commercial;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class Phase256Builder
    {
        const string Data = "Assets/_Game/Data/Housing/";
        const string Art = "Assets/_Game/Art/Housing/";
        static Sprite frame;

        static RectTransform R(Transform p, string n, float x, float y, float w, float h) =>
            ReadabilityAndBirthBuilder.Rect(p, n, new Vector2(x, y), new Vector2(x + w, y + h));

        static TMP_Text T(Transform p, string n, string text, float x, float y, float w, float h) =>
            ReadabilityAndBirthBuilder.Label(p, n, text, new Vector2(x, y), new Vector2(x + w, y + h));

        static Button B(Transform p, string n, string text, float x, float y, float w, float h)
        {
            var b = ReadabilityAndBirthBuilder.Button(p, n, text, new Vector2(x, y), new Vector2(x + w, y + h));
            b.GetComponent<Image>().sprite = frame;
            return b;
        }

        static Image Preview(Transform p, Sprite sprite, float x, float y, float w, float h)
        {
            var i = R(p, "Pixel preview", x, y, w, h).gameObject.AddComponent<Image>();
            i.sprite = sprite;
            i.preserveAspect = true;
            i.raycastTarget = false;
            return i;
        }

        static GameObject Window(UIShell shell, string name, out RectTransform card)
        {
            var root = R(shell.overlayRoot, name, 0, 0, 1, 1);
            root.gameObject.AddComponent<CanvasGroup>();
            root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .7f);
            card = R(root, "Pixel window", .025f, .135f, .95f, .84f);
            var image = card.gameObject.AddComponent<Image>();
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            var close = B(card, "Close", "닫기", .86f, .88f, .12f, .10f);
            var c = close.gameObject.AddComponent<UICommand>();
            c.shell = shell;
            c.command = UICommand.Command.Close;
            c.panel = root.gameObject;
            UnityEventTools.AddPersistentListener(close.onClick, c.Invoke);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        static RectTransform List(Transform p)
        {
            var root = R(p, "Scroll list", 0, 0, 1, 1);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            var viewport = R(root, "Viewport", 0, 0, 1, 1);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, .001f);
            var content = R(viewport, "Content", 0, 1, 1, 0);
            content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = viewport;
            return content;
        }

        static RectTransform Row(Transform p, string name, int height)
        {
            var r = R(p, name, 0, 0, 1, 1);
            var le = r.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = height;
            var i = r.gameObject.AddComponent<Image>();
            i.sprite = frame;
            i.type = Image.Type.Sliced;
            return r;
        }

        static void Place(RectTransform r, Transform p, float x, float y, float w, float h)
        {
            r.SetParent(p, false);
            r.anchorMin = new Vector2(x, y);
            r.anchorMax = new Vector2(x + w, y + h);
            r.offsetMin = r.offsetMax = Vector2.zero;
            var le = r.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.ignoreLayout = true;
            }
        }

        static Sprite Prop(string id, int index)
        {
            string path = Art + id + ".png";
            if (!File.Exists(path))
            {
                var t = new Texture2D(48, 64, TextureFormat.RGBA32, false);
                var pixels = new Color32[48 * 64];
                Action<int, int, int, int, Color32> box = (x, y, w, h, c) =>
                {
                    for (int yy = y; yy < y + h; yy++)
                    {
                        for (int xx = x; xx < x + w; xx++)
                        {
                            pixels[yy * 48 + xx] = c;
                        }
                    }
                };
                Color32 outline = new Color32(49, 44, 43, 255),
                    wood = new Color32(148, 99, 62, 255),
                    light = new Color32(194, 150, 96, 255);
                if (index == 0)
                {
                    box(7, 5, 34, 51, outline);
                    box(10, 8, 28, 45, wood);
                    for (int y = 12; y < 50; y += 16)
                    {
                        box(11, y, 26, 3, light);
                        box(14, y + 3, 4, 9, new Color32(99, 139, 143, 255));
                        box(20, y + 3, 5, 10, new Color32(174, 109, 86, 255));
                    }
                }
                else if (index == 1)
                {
                    box(12, 4, 24, 5, outline);
                    box(22, 9, 4, 27, wood);
                    box(10, 36, 28, 20, outline);
                    box(12, 38, 24, 16, new Color32(230, 201, 127, 255));
                }
                else
                {
                    box(8, 10, 32, 23, outline);
                    box(11, 13, 26, 17, new Color32(131, 151, 115, 255));
                    box(10, 3, 4, 7, wood);
                    box(34, 3, 4, 7, wood);
                    box(8, 33, 7, 20, wood);
                    box(33, 33, 7, 20, wood);
                    box(15, 46, 18, 7, wood);
                }

                t.SetPixels32(pixels);
                t.Apply();
                File.WriteAllBytes(path, t.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(t);
            }

            return CommercialSliceBuilder.Import(path);
        }

        public static void Build()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            Directory.CreateDirectory(Data);
            Directory.CreateDirectory(Art);
            frame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Commercial/UI/Panel_64.png");
            var catalog = AssetDatabase.LoadAssetAtPath<HousingCatalog>(Data + "HousingCatalog.asset");
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<HousingCatalog>();
                AssetDatabase.CreateAsset(catalog, Data + "HousingCatalog.asset");
            }

            var starter = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "StarterHome_640.png");
            var improved = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Game/Art/Commercial/Backgrounds/Home_640.png"
            );
            catalog.bathroom = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "JadeBathroom_640.png");
            if (starter == null || catalog.bathroom == null)
            {
                throw new Exception("Housing backgrounds required");
            }

            catalog.furniture = new[]
            {
                new FurnitureDefinition
                {
                    id = "basic-bed",
                    title = "기본 침대",
                    description = "시작 시 제공 · 방의 기본 설비",
                    starter = true,
                    preview = starter,
                },
                new FurnitureDefinition
                {
                    id = "basic-table",
                    title = "기본 식탁",
                    description = "시작 시 제공 · 방의 기본 설비",
                    starter = true,
                    preview = starter,
                },
                new FurnitureDefinition
                {
                    id = "bookshelf",
                    title = "작은 책장",
                    description = "생활 도구를 정리하는 나무 책장",
                    price = 600,
                    preview = Prop("Bookshelf", 0),
                    position = new Vector2(-1.5f, -.7f),
                },
                new FurnitureDefinition
                {
                    id = "lamp",
                    title = "스탠드 조명",
                    description = "따뜻한 빛의 소박한 조명",
                    price = 250,
                    preview = Prop("Lamp", 1),
                    position = new Vector2(1.6f, -.7f),
                },
                new FurnitureDefinition
                {
                    id = "chair",
                    title = "편안한 의자",
                    description = "하루를 마치고 쉬는 작은 의자",
                    price = 450,
                    preview = Prop("Chair", 2),
                    position = new Vector2(1, -1),
                },
            };
            catalog.residences = new[]
            {
                new ResidenceDefinition
                {
                    id = "starter",
                    title = "기본 집",
                    description = "작고 소박한 방 · 나무 장판과 흰 벽지\n첫 입주는 무료, 돌아올 때만 이사 비용",
                    movingCost = 500,
                    minAge = 0,
                    preview = starter,
                },
                new ResidenceDefinition
                {
                    id = "improved",
                    title = "개선된 소형 주거지",
                    description = "생활 설비와 수납을 갖춘 방 · 작업 책상과 넉넉한 수납\n기존 30세 주거 해금 조건 적용",
                    movingCost = 10000,
                    minAge = 30,
                    preview = improved,
                },
            };
            EditorUtility.SetDirty(catalog);
            var context = PrefabUtility.LoadPrefabContents("Assets/_Game/Prefabs/System/GameContext.prefab");
            try
            {
                context.GetComponent<EconomySystem>().housingCatalog = catalog;
                PrefabUtility.SaveAsPrefabAsset(context, "Assets/_Game/Prefabs/System/GameContext.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(context);
            }

            foreach (var name in new[] { "Home", "School", "Workplace", "City", "Battle", "StockMarket" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
                var p = UnityEngine.Object.FindAnyObjectByType<PixelHomePresenter>();
                if (p.GetComponent<PolishedNavigation>() == null)
                {
                    HUD(p, catalog, name);
                }

                RefineHUD(p);
                foreach (var root in scene.GetRootGameObjects())
                {
                    Polish(root);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            var menu = EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity");
            foreach (var root in menu.GetRootGameObjects())
            {
                Polish(root);
            }

            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity");
        }

        static void HUD(PixelHomePresenter p, HousingCatalog catalog, string scene)
        {
            var shell = p.navigation.stack;
            var commercial = p.GetComponent<CommercialHUD>();
            var safe = commercial.safeRoot;
            p.missionTitle.transform.parent.gameObject.SetActive(false);
            p.recent.transform.parent.gameObject.SetActive(false);
            p.caption.transform.parent.gameObject.SetActive(false);
            p.navigation.navigation.gameObject.SetActive(false);
            p.navigation.navigation = null;
            foreach (
                var b in p.GetComponentsInChildren<PixelNavigationButton>(true)
                    .Where(b => b.target == PixelNavigationButton.Target.Menu)
            )
            {
                b.gameObject.SetActive(false);
            }

            Place(p.quickButtons[0].transform.parent as RectTransform, safe, .22f, .14f, .56f, .075f);
            // Duplicate floating location shortcuts are accessible in the city/work panels.
            foreach (
                string n in new[]
                {
                    "School activity access",
                    "City activity access",
                    "Workplace activity access",
                    "Stock market entry",
                }
            )
            {
                var t = safe.Find(n);
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                }
            }

            var nav = p.gameObject.AddComponent<PolishedNavigation>();
            nav.shell = shell;
            var bar = R(safe, "Primary navigation", .018f, .025f, .964f, .09f);
            var nc = bar.gameObject.AddComponent<Canvas>();
            nc.overrideSorting = true;
            nc.sortingOrder = 250;
            bar.gameObject.AddComponent<GraphicRaycaster>();
            nav.group = bar.gameObject.AddComponent<CanvasGroup>();
            nav.group.ignoreParentGroups = true;
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            RectTransform cityCard;
            var city = Window(shell, "City destinations", out cityCard);
            T(cityCard, "Title", "도시 · 장소 이동", .02f, .88f, .80f, .10f);
            var list = List(R(cityCard, "Destinations", .02f, .03f, .96f, .82f));
            foreach (
                var id in new[]
                {
                    SceneId.Home,
                    SceneId.City,
                    SceneId.School,
                    SceneId.Workplace,
                    SceneId.Battle,
                    SceneId.StockMarket,
                }
            )
            {
                string label =
                    id == SceneId.Home ? "집으로"
                    : id == SceneId.City ? "도시 · 대화와 구직"
                    : id == SceneId.School ? "학교 · 교육과 학습"
                    : id == SceneId.Workplace ? "직장 · 취업과 근무"
                    : id == SceneId.Battle ? "전투"
                    : "증권거래소";
                var row = Row(list, "Destination " + id, 64);
                var b = B(row, "Go", label, 0, 0, 1, 1);
                var cmd = b.gameObject.AddComponent<PixelNavigationButton>();
                cmd.service = p.navigation;
                cmd.target = PixelNavigationButton.Target.Location;
                cmd.location = id;
                UnityEventTools.AddPersistentListener(b.onClick, cmd.Invoke);
            }

            var home = HousingUI(p, catalog);
            nav.panels = new[]
            {
                p.navigation.lifePanel,
                p.navigation.growthPanel,
                city,
                p.navigation.activityPanel,
                p.navigation.missionPanel,
                home.panel,
                p.navigation.menuPanel,
            };
            string[] labels = { "생활", "상태", "도시", "직업·교육", "미션", "집 관리", "기타" };
            nav.buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                nav.buttons[i] = B(bar, "Menu " + i, labels[i], 0, 0, 1, 1);
                var le = nav.buttons[i].gameObject.AddComponent<LayoutElement>();
                le.minWidth = 0;
                le.flexibleWidth = 1;
            }

            if (scene == "Home")
            {
                var env = p.gameObject.AddComponent<HomeEnvironmentPresenter>();
                env.catalog = catalog;
                env.room = UnityEngine.Object.FindAnyObjectByType<RoomAtmosphere>();
                env.room.background.sprite = catalog.residences[0].preview;
                var b = B(safe, "Bathroom entry", "화장실 / 방", .018f, .14f, .18f, .075f);
                UnityEventTools.AddPersistentListener(b.onClick, env.ToggleBathroom);
            }

            // Move log information into the existing status scroll rather than keeping a second HUD card.
            var body = shell.statusPanel.GetComponentsInChildren<ScrollRect>(true).FirstOrDefault();
            if (body != null)
            {
                var text = p.recent;
                Place(text.rectTransform, body.content, 0, 0, 1, 1);
                text.gameObject.SetActive(true);
                var le = text.GetComponent<LayoutElement>() ?? text.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = false;
                le.minHeight = le.preferredHeight = 72;
            }
        }

        static HomeManagementPresenter HousingUI(PixelHomePresenter p, HousingCatalog c)
        {
            var ui = p.gameObject.AddComponent<HomeManagementPresenter>();
            ui.shell = p.navigation.stack;
            ui.catalog = c;
            RectTransform card;
            ui.panel = Window(ui.shell, "Home management", out card);
            ui.balance = T(card, "Housing balance", "집 관리", .02f, .88f, .81f, .10f);
            ui.tabs = new Button[3];
            ui.pages = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                ui.tabs[i] = B(
                    card,
                    "Housing tab " + i,
                    new[] { "인테리어", "가구", "이사" }[i],
                    .02f + i * .32f,
                    .77f,
                    .31f,
                    .09f
                );
                ui.pages[i] = R(card, "Housing page " + i, .02f, .15f, .96f, .59f).gameObject;
            }

            ui.feedback = T(
                card,
                "Housing feedback",
                "새 가구만 유료 · 배치와 회수는 무료입니다.",
                .02f,
                .02f,
                .96f,
                .11f
            );
            var content = List(ui.pages[0].transform);
            var row = Row(content, "Current home", 360);
            ui.interiorPreview = Preview(row, c.residences[0].preview, .02f, .40f, .47f, .57f);
            ui.interior = T(row, "Interior information", "", .50f, .04f, .48f, .92f);
            content = List(ui.pages[1].transform);
            int n = c.furniture.Length;
            ui.buy = new Button[n];
            ui.place = new Button[n];
            ui.reposition = new Button[n];
            ui.furnitureInfo = new TMP_Text[n];
            for (int i = 0; i < n; i++)
            {
                var f = c.furniture[i];
                row = Row(content, "Furniture " + f.id, 210);
                Preview(row, f.preview, .02f, .28f, .20f, .68f);
                ui.furnitureInfo[i] = T(row, "Furniture information", "", .24f, .31f, .74f, .65f);
                ui.buy[i] = B(row, "Buy", "구매", .24f, .04f, .22f, .25f);
                ui.place[i] = B(row, "Place", "배치 · 무료", .48f, .04f, .24f, .25f);
                ui.reposition[i] = B(row, "Reposition", "자리 변경", .74f, .04f, .24f, .25f);
            }

            content = List(ui.pages[2].transform);
            n = c.residences.Length;
            ui.move = new Button[n];
            ui.residenceInfo = new TMP_Text[n];
            for (int i = 0; i < n; i++)
            {
                row = Row(content, "Residence " + c.residences[i].id, 260);
                Preview(row, c.residences[i].preview, .02f, .08f, .34f, .84f);
                ui.residenceInfo[i] = T(row, "Residence information", "", .38f, .30f, .60f, .66f);
                ui.move[i] = B(row, "Move", "이사 확인", .65f, .04f, .33f, .22f);
            }

            ui.confirmation = Window(ui.shell, "Confirm housing payment", out card);
            var canvas = ui.confirmation.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 300;
            ui.confirmation.AddComponent<GraphicRaycaster>();
            ui.confirmationText = T(card, "Confirm details", "", .06f, .30f, .88f, .52f);
            ui.accept = B(card, "Accept payment", "결제 확정", .08f, .10f, .40f, .13f);
            ui.cancel = B(card, "Cancel payment", "취소", .52f, .10f, .40f, .13f);
            return ui;
        }

        static void RefineHUD(PixelHomePresenter p)
        {
            var safe = p.GetComponent<CommercialHUD>().safeRoot;
            var recent = safe.Find("Recent event");
            if (recent != null)
            {
                recent.gameObject.SetActive(false);
            }

            var housing = p.GetComponent<HomeManagementPresenter>();
            if (housing != null)
            {
                for (int i = 0; i < housing.catalog.furniture.Length; i++)
                {
                    if (!housing.catalog.furniture[i].starter)
                    {
                        housing.furnitureInfo[i].transform.parent.SetSiblingIndex(i - 2);
                    }
                }
            }

            var idle = p.GetComponent<IdleActivityPresenter>();
            if (idle != null)
            {
                var target = p.navigation.activityPanel.GetComponentsInChildren<ScrollRect>(true).FirstOrDefault();
                if (target != null)
                {
                    Place(idle.open.GetComponent<RectTransform>(), target.content, 0, 0, 1, 1);
                    var le =
                        idle.open.GetComponent<LayoutElement>() ?? idle.open.gameObject.AddComponent<LayoutElement>();
                    le.ignoreLayout = false;
                    le.minHeight = le.preferredHeight = 56;
                }
            }
        }

        public static void Polish(GameObject root)
        {
            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler != null && root.GetComponent<CommercialHUD>() != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }

            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                t.enableAutoSizing = false;
                t.maxVisibleLines = 99999;
                t.textWrappingMode = TextWrappingModes.Normal;
                t.overflowMode = TextOverflowModes.Masking;
                var b = t.GetComponentInParent<Button>(true);
                if (b != null)
                {
                    t.fontSize = 18;
                    t.margin = new Vector4(14, 4, 14, 4);
                    var le = b.GetComponent<LayoutElement>();
                    if (le != null)
                    {
                        le.minHeight = Mathf.Max(52, le.minHeight);
                        if (le.preferredHeight > 0)
                        {
                            le.preferredHeight = Mathf.Max(52, le.preferredHeight);
                        }
                    }
                }
                else if (t.fontSize < 18)
                {
                    t.fontSize = 18;
                }

                float baseSize = t.fontSize;
                var scale = t.GetComponent<UITypeScale>();
                if (scale == null)
                {
                    scale = t.gameObject.AddComponent<UITypeScale>();
                }

                scale.baseSize = baseSize;
                scale.Apply();
                if (b == null)
                {
                    var le = t.GetComponent<LayoutElement>();
                    if (le != null && le.preferredHeight > 0 && le.preferredHeight < 32)
                    {
                        le.minHeight = le.preferredHeight = 32;
                    }
                }

                if (t.name == "Footnote" || t.name == "Footer")
                {
                    var le = t.GetComponent<LayoutElement>();
                    if (le != null)
                    {
                        le.minHeight = le.preferredHeight = 64;
                    }
                }

                if (
                    t.GetComponent<LayoutElement>() != null
                    && t.GetComponentInParent<ScrollRect>(true) != null
                    && b == null
                )
                {
                    var flow = t.GetComponent<PixelTextFlow>() ?? t.gameObject.AddComponent<PixelTextFlow>();
                    flow.minimumHeight = Mathf.Max(48, t.GetComponent<LayoutElement>().preferredHeight);
                }

                if (
                    b != null
                    && b.GetComponent<LayoutElement>() != null
                    && b.GetComponentInParent<ScrollRect>(true) != null
                    && b.GetComponent<PixelButtonTextFlow>() == null
                )
                {
                    b.gameObject.AddComponent<PixelButtonTextFlow>();
                }
            }

            foreach (var row in root.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                if (row.transform.Cast<Transform>().Any(t => t.GetComponent<Button>() != null))
                {
                    var size = row.GetComponent<LayoutElement>();
                    if (size != null && size.preferredHeight > 0)
                    {
                        size.minHeight = Mathf.Max(64, size.minHeight);
                        size.preferredHeight = Mathf.Max(64, size.preferredHeight);
                    }
                }
            }
        }
    }
}
