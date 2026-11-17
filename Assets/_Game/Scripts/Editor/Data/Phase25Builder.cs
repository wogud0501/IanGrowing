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
    public static class Phase25Builder
    {
        const string Folder = "Assets/_Game/Data/Economy/";
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

        static RectTransform Card(Transform p, string n, float x, float y, float w, float h)
        {
            var r = R(p, n, x, y, w, h);
            var image = r.gameObject.AddComponent<Image>();
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            return r;
        }

        static GameObject Window(Transform p, string n, out RectTransform card)
        {
            var root = R(p, n, 0, 0, 1, 1);
            root.gameObject.AddComponent<CanvasGroup>();
            root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .65f);
            var safe = R(root, "Safe area", 0, 0, 1, 1);
            safe.gameObject.AddComponent<BirthPopupSafeArea>().target = safe;
            card = Card(safe, "Pixel finance window", .045f, .05f, .91f, .9f);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        static TMP_Text Scroll(Transform p, string n, float x, float y, float w, float h)
        {
            var root = R(p, n, x, y, w, h);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = R(root, "Viewport", 0, 0, 1, 1);
            viewport.gameObject.AddComponent<Image>();
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = R(viewport, "Content", 0, 1, 1, 0);
            content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var text = T(content, "Body", "", 0, 0, 1, 1);
            text.gameObject.AddComponent<LayoutElement>().minHeight = 48;
            text.gameObject.AddComponent<PixelTextFlow>().minimumHeight = 48;
            scroll.viewport = viewport;
            scroll.content = content;
            return text;
        }

        static TMP_InputField Input(Transform p, string n, string initial, float x, float y, float w, float h)
        {
            var r = Card(p, n, x, y, w, h);
            var viewport = R(r, "Input viewport", .03f, .02f, .94f, .96f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var text = T(viewport, "Value", initial, 0, 0, 1, 1);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            var field = r.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = viewport;
            field.textComponent = (TextMeshProUGUI)text;
            field.contentType = TMP_InputField.ContentType.IntegerNumber;
            field.characterLimit = 10;
            field.text = initial;
            field.targetGraphic = r.GetComponent<Image>();
            return field;
        }

        public static void Build()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play Mode");
            }

            Directory.CreateDirectory(Folder);
            frame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Commercial/UI/Panel_64.png");
            var rules = AssetDatabase.LoadAssetAtPath<EconomyRules>(Folder + "EconomyRules.asset");
            if (rules == null)
            {
                rules = ScriptableObject.CreateInstance<EconomyRules>();
                AssetDatabase.CreateAsset(rules, Folder + "EconomyRules.asset");
            }

            var context = PrefabUtility.LoadPrefabContents("Assets/_Game/Prefabs/System/GameContext.prefab");
            try
            {
                var e = context.GetComponent<EconomySystem>() ?? context.AddComponent<EconomySystem>();
                e.rules = rules;
                context.GetComponent<GameManager>().economy = e;
                var controller = context.GetComponent<TransitionController>();
                var profiles = controller.profiles.ToList();
                foreach (bool incoming in new[] { true, false })
                {
                    var from = incoming ? SceneId.City : SceneId.StockMarket;
                    var to = incoming ? SceneId.StockMarket : SceneId.City;
                    if (profiles.Any(p => p.from == from && p.to == to))
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
                        profile.departure = "지갑의 미래를 확인하는 중...";
                        profile.arrival =
                            to == SceneId.StockMarket
                                ? "이안 증권거래소. 희망도 변동성이 있습니다."
                                : "시장 밖에도 생활비는 있습니다.";
                        AssetDatabase.CreateAsset(profile, path);
                    }

                    profiles.Add(profile);
                }

                controller.profiles = profiles.ToArray();
                PrefabUtility.SaveAsPrefabAsset(context, "Assets/_Game/Prefabs/System/GameContext.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(context);
            }

            if (!File.Exists("Assets/_Game/Scenes/StockMarket.unity"))
            {
                AssetDatabase.CopyAsset("Assets/_Game/Scenes/City.unity", "Assets/_Game/Scenes/StockMarket.unity");
            }

            foreach (string name in new[] { "Home", "School", "Workplace", "City", "Battle", "StockMarket" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
                var p = UnityEngine.Object.FindAnyObjectByType<PixelHomePresenter>();
                if (p.GetComponent<FinancePresenter>() == null)
                {
                    BuildUI(p, name == "StockMarket", name == "City", rules);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<SceneCatalog>(
                AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:SceneCatalog").First())
            );
            var route = catalog.Find(SceneId.StockMarket);
            if (route == null)
            {
                route = new SceneRoute { id = SceneId.StockMarket };
                catalog.routes = catalog.routes.Concat(new[] { route }).ToArray();
            }

            route.implemented = true;
            route.sceneName = "StockMarket";
            EditorUtility.SetDirty(catalog);
            if (!EditorBuildSettings.scenes.Any(s => s.path == "Assets/_Game/Scenes/StockMarket.unity"))
            {
                EditorBuildSettings.scenes = EditorBuildSettings
                    .scenes.Concat(
                        new[] { new EditorBuildSettingsScene("Assets/_Game/Scenes/StockMarket.unity", true) }
                    )
                    .ToArray();
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Home.unity");
        }

        public static void Polish()
        {
            foreach (string name in new[] { "Home", "School", "Workplace", "City", "Battle", "StockMarket" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
                var ui = UnityEngine.Object.FindAnyObjectByType<FinancePresenter>();
                if (ui.marketScene)
                {
                    if (ui.chart.GetComponent<CanvasRenderer>() == null)
                    {
                        ui.chart.gameObject.AddComponent<CanvasRenderer>();
                    }

                    ui.header.fontSize = 16;
                    foreach (var b in ui.stocks)
                    {
                        b.GetComponentInChildren<TMP_Text>().margin = new Vector4(8, 0, 8, 0);
                    }

                    var label = ui
                        .marketPanel.GetComponentsInChildren<TMP_Text>(true)
                        .First(t => t.name == "Quantity label");
                    label.text = "주";
                    label.margin = Vector4.zero;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity");
        }

        static void BuildUI(PixelHomePresenter p, bool market, bool city, EconomyRules rules)
        {
            var ui = p.gameObject.AddComponent<FinancePresenter>();
            ui.marketScene = market;
            ui.shell = p.navigation.stack;
            var safe = p.GetComponent<CommercialHUD>().safeRoot;
            if (city)
            {
                ui.entry = B(safe, "Stock market entry", "증권거래소 →", .60f, .36f, .38f, .06f);
            }

            var status = ui.shell.statusPanel.GetComponentsInChildren<ScrollRect>(true).First().content;
            ui.economyEntry = B(status, "Economy access", "경제 · 세금 · 부채 관리", 0, 0, 1, 1);
            var le = ui.economyEntry.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 52;
            RectTransform card;
            ui.economyPanel = Window(ui.shell.overlayRoot, "Economy management", out card);
            T(card, "Title", "ECONOMY / 경제·세금·부채", .03f, .89f, .94f, .09f);
            ui.economyText = Scroll(card, "Financial statement", .03f, .28f, .94f, .60f);
            ui.economyFeedback = T(
                card,
                "Payment feedback",
                "금액을 입력하고 납부 또는 상환을 선택하세요.",
                .03f,
                .19f,
                .94f,
                .08f
            );
            ui.payment = Input(card, "Payment amount", "100", .03f, .08f, .25f, .10f);
            ui.payTax = B(card, "Pay oldest bill", "세금 납부", .30f, .08f, .22f, .10f);
            ui.repay = B(card, "Repay debt", "부채 상환", .54f, .08f, .22f, .10f);
            ui.economyClose = B(card, "Close economy", "닫기", .78f, .08f, .19f, .10f);
            if (!market)
            {
                return;
            }

            p.GetComponent<CommercialHUD>().title.text = "이안 증권거래소 / MARKET";
            ui.marketPanel = Window(ui.shell.overlayRoot, "Stock market screen", out card);
            ui.header = T(card, "Market heading", "이안 증권거래소", .03f, .87f, .78f, .11f);
            ui.marketClose = B(card, "Exit to city", "도시로", .83f, .88f, .14f, .09f);
            string[] names = { "시장·주문", "보유·손익", "경제 뉴스", "거래 내역" };
            ui.tabs = new Button[4];
            ui.pages = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                ui.tabs[i] = B(card, "Tab " + i, names[i], .03f + i * .237f, .77f, .225f, .08f);
                ui.pages[i] = R(card, "Page " + i, .03f, .15f, .94f, .60f).gameObject;
            }

            var page = ui.pages[0].transform;
            ui.stocks = new Button[rules.stocks.Length];
            for (int i = 0; i < ui.stocks.Length; i++)
            {
                ui.stocks[i] = B(
                    page,
                    "Stock " + rules.stocks[i].StockId,
                    rules.stocks[i].CompanyName,
                    0,
                    .84f - i * .162f,
                    .33f,
                    .15f
                );
            }

            ui.stockDetail = T(page, "Selected stock", "", .36f, .69f, .64f, .31f);
            var chartFrame = Card(page, "Price chart", .38f, .25f, .60f, .41f);
            var plot = R(chartFrame, "Pixel line", .03f, .08f, .94f, .84f);
            ui.chart = plot.gameObject.AddComponent<PixelStockChart>();
            ui.chart.raycastTarget = false;
            ui.chart.color = new Color32(189, 224, 115, 255);
            T(page, "Quantity label", "수량", .37f, .03f, .10f, .12f);
            ui.quantity = Input(page, "Order quantity", "1", .48f, .03f, .16f, .14f);
            ui.buy = B(page, "Buy", "매수", .66f, .03f, .15f, .14f);
            ui.sell = B(page, "Sell", "매도", .83f, .03f, .15f, .14f);
            ui.portfolio = Scroll(ui.pages[1].transform, "Portfolio", 0, 0, 1, 1);
            ui.news = Scroll(ui.pages[2].transform, "Economic news", 0, 0, 1, 1);
            ui.history = Scroll(ui.pages[3].transform, "Transaction history", 0, 0, 1, 1);
            ui.feedback = T(card, "Order feedback", "가상 주식시장 · 실물 화폐와 무관합니다.", .03f, .03f, .94f, .10f);
        }
    }
}
