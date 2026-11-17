using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class Phase27Builder
    {
        const string Data = "Assets/_Game/Data/Phase27/";
        static Sprite frame;

        static RectTransform R(Transform p, string n, float x = 0, float y = 0, float w = 1, float h = 1) =>
            ReadabilityAndBirthBuilder.Rect(p, n, new Vector2(x, y), new Vector2(x + w, y + h));

        static TMP_Text T(Transform p, string n, string value) =>
            ReadabilityAndBirthBuilder.Label(p, n, value, Vector2.zero, Vector2.one, 18);

        static Button B(Transform p, string n, string title)
        {
            var b = ReadabilityAndBirthBuilder.Button(p, n, title, Vector2.zero, Vector2.one);
            b.GetComponent<Image>().sprite = frame;
            return b;
        }

        static void Height(Component c, float height)
        {
            var e = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            e.minHeight = e.preferredHeight = height;
        }

        static GameObject Window(UIShell shell, string name, out RectTransform card)
        {
            var root = R(shell.overlayRoot, name);
            root.gameObject.AddComponent<CanvasGroup>();
            root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .65f);
            var safe = R(root, "Safe area");
            safe.gameObject.AddComponent<BirthPopupSafeArea>().target = safe;
            card = R(safe, "Pixel property panel", .035f, .035f, .93f, .93f);
            var image = card.gameObject.AddComponent<Image>();
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        static ScrollRect Scroll(Transform p, string n, float x, float y, float w, float h)
        {
            var root = R(p, n, x, y, w, h);
            var s = root.gameObject.AddComponent<ScrollRect>();
            s.horizontal = false;
            s.movementType = ScrollRect.MovementType.Clamped;
            s.scrollSensitivity = 36;
            var v = R(root, "Viewport");
            v.gameObject.AddComponent<Image>();
            v.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var c = R(v, "Content", 0, 1, 1, 0);
            c.pivot = new Vector2(.5f, 1);
            var l = c.gameObject.AddComponent<VerticalLayoutGroup>();
            l.padding = new RectOffset(8, 8, 8, 12);
            l.spacing = 12;
            l.childControlWidth = l.childControlHeight = true;
            l.childForceExpandHeight = false;
            c.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            s.viewport = v;
            s.content = c;
            return s;
        }

        static GridLayoutGroup Grid(Transform p, string n)
        {
            var root = R(p, n);
            var g = root.gameObject.AddComponent<GridLayoutGroup>();
            g.spacing = new Vector2(10, 10);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            root.gameObject.AddComponent<LayoutElement>();
            return g;
        }

        static TMP_Text Flow(Transform p, string n)
        {
            var t = T(p, n, "");
            t.gameObject.AddComponent<LayoutElement>();
            t.gameObject.AddComponent<PixelTextFlow>();
            return t;
        }

        public static PropertyPresenter UI(UIShell shell, PropertyCatalog catalog)
        {
            var ui = shell.gameObject.AddComponent<PropertyPresenter>();
            ui.shell = shell;
            ui.panel = Window(shell, "Real estate agency", out var card);
            var heading = R(card, "Heading", .02f, .88f, .76f, .1f);
            ui.heading = T(heading, "Agency title", "이안 부동산");
            ui.close = B(card, "Close property", "닫기");
            var close = (RectTransform)ui.close.transform;
            close.anchorMin = new Vector2(.8f, .89f);
            close.anchorMax = new Vector2(.98f, .98f);
            close.offsetMin = close.offsetMax = Vector2.zero;
            var tabs = R(card, "Property tabs", .02f, .77f, .96f, .095f);
            var row = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 6;
            row.childControlHeight = row.childControlWidth = true;
            row.childForceExpandWidth = true;
            string[] titles = { "매물", "보유", "계약", "대출", "시세", "기록" };
            ui.tabs = titles
                .Select(
                    (t, i) =>
                    {
                        var b = B(tabs, "Property tab " + i, t);
                        Height(b, 52);
                        b.gameObject.AddComponent<LayoutElement>().minWidth = 52;
                        return b;
                    }
                )
                .ToArray();
            ui.scroll = Scroll(card, "Property scroll", .02f, .14f, .96f, .61f);
            var content = ui.scroll.content;
            var selectors = Grid(content, "Property selection");
            ui.selections = catalog
                .properties.Select(p => B(selectors.transform, p.PropertyId, p.PropertyName))
                .ToArray();
            ui.body = Flow(content, "Property details");
            var picture = R(content, "Property preview");
            ui.preview = picture.gameObject.AddComponent<Image>();
            ui.preview.preserveAspect = true;
            ui.preview.raycastTarget = false;
            Height(ui.preview, 180);
            var plot = R(content, "Property price history");
            ui.chart = plot.gameObject.AddComponent<PixelStockChart>();
            ui.chart.raycastTarget = false;
            ui.chart.color = new Color32(189, 224, 115, 255);
            Height(ui.chart, 150);
            var amount = R(content, "Loan amount");
            ui.amountRow = amount.gameObject;
            Height(amount, 64);
            var image = amount.gameObject.AddComponent<Image>();
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            var viewport = R(amount, "Number viewport", .02f, .02f, .96f, .96f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var value = T(viewport, "Amount", "1000");
            value.alignment = TextAlignmentOptions.MidlineLeft;
            ui.amount = amount.gameObject.AddComponent<TMP_InputField>();
            ui.amount.textViewport = viewport;
            ui.amount.textComponent = (TextMeshProUGUI)value;
            ui.amount.contentType = TMP_InputField.ContentType.IntegerNumber;
            ui.amount.characterLimit = 10;
            ui.amount.text = "1000";
            ui.amount.targetGraphic = image;
            var actions = Grid(content, "Property actions");
            ui.actions = Enumerable
                .Range(0, 3)
                .Select(i => B(actions.transform, "Property action " + i, "거래"))
                .ToArray();
            var flow = card.gameObject.AddComponent<PropertyUIFlow>();
            flow.grids = new[] { selectors, actions };
            var feedback = R(card, "Property feedback", .02f, .015f, .96f, .11f);
            ui.feedback = T(feedback, "Feedback", "지붕은 고정되어 있지만 가격은 아닙니다. / 스크롤로 자세히 보기");
            ui.confirmation = Window(shell, "Property confirmation", out var confirm);
            var cs = Scroll(confirm, "Confirmation scroll", .04f, .22f, .92f, .72f);
            ui.confirmationText = Flow(cs.content, "Transaction terms");
            ui.accept = B(confirm, "Confirm property transaction", "확인 · 실행");
            var ar = (RectTransform)ui.accept.transform;
            ar.anchorMin = new Vector2(.04f, .06f);
            ar.anchorMax = new Vector2(.49f, .18f);
            ar.offsetMin = ar.offsetMax = Vector2.zero;
            ui.cancel = B(confirm, "Cancel property transaction", "취소");
            var cr = (RectTransform)ui.cancel.transform;
            cr.anchorMin = new Vector2(.51f, .06f);
            cr.anchorMax = new Vector2(.96f, .18f);
            cr.offsetMin = cr.offsetMax = Vector2.zero;
            return ui;
        }

        [MenuItem("Ian/Phase 2.7/Apply Property System")]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play first");
            }

            Directory.CreateDirectory(Data);
            frame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Commercial/UI/Panel_64.png");
            var housing = AssetDatabase.LoadAssetAtPath<HousingCatalog>(
                "Assets/_Game/Data/Phase256/HousingCatalog.asset"
            );
            var catalog = AssetDatabase.LoadAssetAtPath<PropertyCatalog>(Data + "PropertyCatalog.asset");
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PropertyCatalog>();
                AssetDatabase.CreateAsset(catalog, Data + "PropertyCatalog.asset");
                string[] names =
                {
                    "퇴근 고시원",
                    "골목 원룸",
                    "햇살 빌라",
                    "야근 오피스텔",
                    "평온 아파트",
                    "구름 고급 아파트",
                };
                int[] prices = { 8000, 30000, 80000, 120000, 250000, 750000 };
                catalog.properties = Enumerable
                    .Range(0, 6)
                    .Select(i => new PropertyDefinition
                    {
                        PropertyId = "property-" + i,
                        PropertyName = names[i],
                        PropertyType = (PropertyType)i,
                        Location =
                            i < 2 ? "서쪽 골목"
                            : i < 4 ? "중앙 업무지구"
                            : "동쪽 주거지",
                        PurchasePrice = prices[i],
                        JeonseDeposit = prices[i] * 7 / 10,
                        MonthlyDeposit = prices[i] / 10,
                        MonthlyRent = prices[i] / 100,
                        MaintenanceCost = prices[i] / 500,
                        residenceId = i < 2 ? "starter" : "improved",
                        minAge =
                            i < 2 ? 19
                            : i < 4 ? 30
                            : i == 4 ? 35
                            : 40,
                        volatility = .009f + i * .003f,
                        regionalTrend = i < 2 ? -.0005f : .0005f,
                        sleepBonus = new StatDelta
                        {
                            health = 1 + i,
                            mental = 1 + i,
                            happiness = i / 2,
                        },
                        restBonus = new StatDelta { health = i / 2, mental = 1 + i / 2 },
                        preview = housing.residences[i < 2 ? 0 : 1].preview,
                    })
                    .ToArray();
                EditorUtility.SetDirty(catalog);
            }

            var prefab = PrefabUtility.LoadPrefabContents("Assets/_Game/Prefabs/GameContext.prefab");
            try
            {
                prefab.GetComponent<EconomySystem>().propertyCatalog = catalog;
                PrefabUtility.SaveAsPrefabAsset(prefab, "Assets/_Game/Prefabs/GameContext.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");
            var world = UnityEngine.Object.FindFirstObjectByType<LifeWorldController>();
            if (world.hud.GetComponent<PropertyPresenter>() == null)
            {
                var ui = UI(world.hud.shell, catalog);
                var stock = world.objects.First(o => o.kind == WorldInteractionKind.StockMarket);
                var agent = UnityEngine.Object.Instantiate(stock, stock.transform.parent);
                agent.name = "City real estate agency";
                agent.id = "property-agency";
                agent.title = "이안 부동산 · 매매/전세/월세";
                agent.kind = WorldInteractionKind.RealEstate;
                agent.transform.position = stock.transform.position + Vector3.left * 3.5f;
                agent.UsePoint = Vector2.down * .8f;
                agent.radius = 1.8f;
                foreach (var text in agent.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.text = "부동산";
                }

                world.objects = world.objects.Concat(new[] { agent }).ToArray();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/City.unity");
            var shell = UnityEngine.Object.FindFirstObjectByType<UIShell>();
            if (shell.GetComponent<PropertyPresenter>() == null)
            {
                var ui = UI(shell, catalog);
                var stock = UnityEngine.Object.FindFirstObjectByType<FinancePresenter>().entry;
                ui.entry = UnityEngine.Object.Instantiate(stock, stock.transform.parent);
                ui.entry.name = "City property agency entry";
                ui.entry.onClick = new Button.ButtonClickedEvent();
                ui.entry.GetComponentInChildren<TMP_Text>().text = "부동산 중개소 →";
                var rect = (RectTransform)ui.entry.transform;
                rect.anchorMin = new Vector2(.60f, .27f);
                rect.anchorMax = new Vector2(.98f, .33f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity");
        }
    }
}
