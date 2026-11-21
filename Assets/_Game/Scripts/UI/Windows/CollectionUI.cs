using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    // All widgets are cached. Changes update labels/cells, not the whole game UI.
    public sealed class CollectionUI : MonoBehaviour
    {
        public LifeWorldController world;
        public CollectionVisualTheme visualTheme;
        public GameObject InventoryPanel { get; private set; }
        public GameObject EquipmentPanel { get; private set; }
        public GameObject GachaPanel { get; private set; }
        public GameObject CollectionPanel { get; private set; }
        public GameObject ResultPanel { get; private set; }
        public GameObject ProbabilityPanel { get; private set; }
        public bool Revealing => revealing;
        public int VisibleResults => revealed;

        public event Action<GachaResult> RareReveal;

        sealed class Cell
        {
            public string id;
            public Button button;
            public Image icon;
            public Image mount;
            public Image equipped;
            public TMP_Text name;
            public TMP_Text quantity;
        }

        sealed class GridView
        {
            public RectTransform view;
            public RectTransform content;
            public GridLayoutGroup grid;
            public List<Cell> cells = new List<Cell>();
        }

        UIShell shell;
        WorldHUD hud;
        GameManager game;
        ItemService items;
        TMP_FontAsset font;
        Sprite frame;
        readonly List<GameObject> screens = new List<GameObject>();
        readonly List<GridView> grids = new List<GridView>();
        readonly List<Button> gachaNavigation = new List<Button>();
        GridView inventoryGrid;
        GridView collectionGrid;
        GridView resultGrid;
        TMP_Text inventorySummary;
        TMP_Text detail;
        TMP_Text collectionSummary;
        TMP_Text collectionDetail;
        TMP_Text equipmentSummary;
        readonly Image[] equippedIcons = new Image[3];
        TMP_Text feedback;
        TMP_Text balance;
        TMP_Text pity;
        TMP_Text[] guaranteeLabels;
        RectTransform[] guaranteeFills;
        TMP_Text probability;
        readonly Button[] probabilityTabs = new Button[3];
        string[] probabilitySections;
        TMP_Text guide;
        TMP_Text resultSummary;
        Button equip;
        Button unequip;
        Button use;
        Button dismantle;
        Button pull1;
        Button pull10;
        Button scrap;
        Button sortButton;
        Image inventoryTray;
        Image bagIllustration;
        Image itemPreview;
        Image specimen;
        TMP_Text emptyBag;
        readonly Button[] inventoryFilters = new Button[6];
        readonly Button[] collectionFilters = new Button[6];
        Button[] slots;
        string selected;
        string selectedCollection;
        string gachaError;
        int filter;
        int sorting;
        bool revealing;
        bool started;
        int revealed;
        Coroutine revealRoutine;
        GachaBatch activeBatch;
        string Pool => items.catalog.pools[0].poolId;

        Color Ink => visualTheme == null ? LifeRecordUI.Body : visualTheme.ink;

        RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size = default)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = min;
            r.anchorMax = max;
            r.pivot = new Vector2(.5f, .5f);
            r.sizeDelta = size;
            return r;
        }

        Image Box(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var r = Rect(name, parent, min, max);
            var image = r.gameObject.AddComponent<Image>();
            image.sprite = visualTheme != null && visualTheme.panelSprite != null ? visualTheme.panelSprite : frame;
            image.type = Image.Type.Sliced;
            image.color = visualTheme == null || visualTheme.panelSprite != null ? Color.white : visualTheme.panel;
            return image;
        }

        TMP_Text Text(
            string name,
            Transform parent,
            string value,
            float size,
            Vector2 min,
            Vector2 max,
            int padding = 8
        )
        {
            var r = Rect(name, parent, min, max, new Vector2(-padding * 2, -padding * 2));
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = value;
            t.color = Ink;
            t.fontSize = size;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            var type = t.gameObject.AddComponent<UITypeScale>();
            type.baseSize = size;
            type.Apply();
            return t;
        }

        Button Button(string name, Transform parent, string label, Vector2 min, Vector2 max, Action click)
        {
            var image = Box(name, parent, min, max);
            var b = image.gameObject.AddComponent<Button>();
            b.targetGraphic = image;
            bool primary = name == "Pull ten" || name == "Equip" || name == "Fast reveal";
            bool paper = name == "Pull one" || name == "Close";
            if (visualTheme != null)
            {
                var surface =
                    primary ? visualTheme.buttonSprite
                    : paper ? visualTheme.paperSprite
                    : visualTheme.panelSprite;
                image.color = surface != null ? Color.white : visualTheme.button;
                if (surface != null)
                {
                    image.sprite = surface;
                }
            }

            var t = Text("Label", b.transform, label, 18, Vector2.zero, Vector2.one, 4);
            t.alignment = TextAlignmentOptions.Center;
            if (visualTheme != null)
            {
                t.color = primary || paper ? visualTheme.buttonInk : visualTheme.ink;
            }

            b.gameObject.AddComponent<PixelControlFeedback>();
            b.onClick.AddListener(() => click());
            return b;
        }

        GameObject Screen(string name, string title, bool tabs = true)
        {
            var p = Box(name, shell.overlayRoot, new Vector2(.035f, .06f), new Vector2(.965f, .94f)).gameObject;
            Text("Title", p.transform, title, 24, new Vector2(.025f, .91f), new Vector2(.55f, 1));
            Button("Close", p.transform, "닫기", new Vector2(.85f, .915f), new Vector2(.975f, .99f), () => Close(p));
            p.SetActive(false);
            screens.Add(p);
            if (tabs)
            {
                Action[] actions = { OpenInventory, OpenEquipment, OpenGacha, OpenCollection };
                string[] names = { "가방", "장비", "가챠", "도감" };
                for (int i = 0; i < 4; i++)
                {
                    int n = i;
                    var b = Button(
                        "Section " + i,
                        p.transform,
                        names[i],
                        new Vector2(.025f + i * .24f, .825f),
                        new Vector2(.25f + i * .24f, .9f),
                        actions[n]
                    );
                    if (i == 2)
                    {
                        gachaNavigation.Add(b);
                    }
                }
            }

            return p;
        }

        TMP_Text ScrollText(string name, Transform parent, Vector2 min, Vector2 max, float fontSize)
        {
            var scroll = Scroll(name, parent, min, max);
            RecordPage.FitReadingColumn(scroll.content, 10);
            var t = Text("Content text", scroll.content, "", fontSize, Vector2.zero, Vector2.one, 10);
            t.rectTransform.anchorMin = new Vector2(0, 1);
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.pivot = new Vector2(.5f, 1);
            return t;
        }

        ScrollRect Scroll(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var root = Rect(name, parent, min, max);
            var s = root.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", root, Vector2.zero, Vector2.one);
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport, new Vector2(0, 1), Vector2.one);
            content.pivot = new Vector2(.5f, 1);
            s.viewport = viewport;
            s.content = content;
            s.horizontal = false;
            s.movementType = ScrollRect.MovementType.Clamped;
            s.scrollSensitivity = 24;
            return s;
        }

        GridView Grid(string name, Transform parent, Vector2 min, Vector2 max, IEnumerable<ItemDefinition> definitions)
        {
            var s = Scroll(name, parent, min, max);
            var v = new GridView { view = (RectTransform)s.transform, content = s.content };
            v.grid = s.content.gameObject.AddComponent<GridLayoutGroup>();
            v.grid.padding = new RectOffset(8, 8, 8, 8);
            v.grid.spacing = new Vector2(8, 8);
            v.grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            foreach (var d in definitions)
            {
                v.cells.Add(CreateCell(d.itemId, s.content));
            }

            var viewport = s.viewport.gameObject.AddComponent<CollectionGridViewport>();
            viewport.WidthChanged += () => Layout(v);
            grids.Add(v);
            return v;
        }

        Cell CreateCell(string id, Transform parent)
        {
            var image = Box("Item " + id, parent, Vector2.zero, Vector2.one);
            var c = new Cell { id = id, button = image.gameObject.AddComponent<Button>() };
            c.button.targetGraphic = image;
            c.mount = LifeRecordUI.Surface(image.transform, "Specimen mount", RecordSurface.Specimen,
                .03f, .20f, .24f, .86f);
            var icon = Rect("Icon", image.transform, new Vector2(.03f, .20f), new Vector2(.24f, .86f));
            c.icon = icon.gameObject.AddComponent<Image>();
            LifeRecordUI.InkSpecimen(c.icon);
            c.icon.preserveAspect = true;
            c.icon.raycastTarget = false;
            c.name = Text("Item name", image.transform, "", 18, new Vector2(.26f, .45f), new Vector2(.99f, .98f), 4);
            c.quantity = Text(
                "Item state",
                image.transform,
                "",
                16,
                new Vector2(.26f, .04f),
                new Vector2(.99f, .43f),
                4
            );
            c.equipped = LifeRecordUI.Rect(image.transform, "Equipped corner", .85f, .72f, 1, 1)
                .gameObject.AddComponent<Image>();
            c.equipped.sprite = LifeRecordUI.EquippedMark;
            c.equipped.preserveAspect = true;
            c.equipped.raycastTarget = false;
            c.equipped.enabled = false;
            c.button.onClick.AddListener(() => Select(id));
            return c;
        }

        void Start()
        {
            if (world == null || world.Service == null)
            {
                return;
            }

            game = world.Service.Game;
            items = game.items;
            hud = world.hud;
            shell = hud.shell;
            font = hud.needs.font;
            frame = hud.promptPanel.GetComponent<Image>().sprite;
            InventoryPanel = Screen("Item Inventory", "수집 보관함");
            EquipmentPanel = Screen("Item Equipment", "현재 장비");
            GachaPanel = Screen("Item Gacha", "인생의 작은 위안");
            CollectionPanel = Screen("Item Collection", "아이템 도감");
            ResultPanel = Screen("Item Results", "획득한 위안", false);
            ProbabilityPanel = Screen("Item Probabilities", "확률정보", false);
            inventorySummary = Text(
                "Inventory summary",
                InventoryPanel.transform,
                "",
                18,
                new Vector2(.03f, .76f),
                new Vector2(.97f, .825f),
                2
            );
            collectionSummary = Text(
                "Collection summary",
                CollectionPanel.transform,
                "",
                18,
                new Vector2(.03f, .70f),
                new Vector2(.97f, .82f),
                4
            );
            sortButton = Button(
                "Sort",
                InventoryPanel.transform,
                "등급순",
                new Vector2(.58f, .915f),
                new Vector2(.81f, .99f),
                () =>
                {
                    sorting = (sorting + 1) % 3;
                    Refresh();
                }
            );
            string[] filters = { "전체", "장비", "헤드폰", "음료", "코스튬", "기타" };
            for (int i = 0; i < 6; i++)
            {
                int f = i;
                inventoryFilters[i] = Button(
                    "Filter " + f,
                    InventoryPanel.transform,
                    filters[i],
                    new Vector2(.025f + i * .16f, .695f),
                    new Vector2(.177f + i * .16f, .755f),
                    () =>
                    {
                        filter = f;
                        Refresh();
                    }
                );
                collectionFilters[i] = Button(
                    "Collection filter " + f,
                    CollectionPanel.transform,
                    filters[i],
                    new Vector2(.025f + i * .16f, .635f),
                    new Vector2(.177f + i * .16f, .695f),
                    () =>
                    {
                        filter = f;
                        Refresh();
                    }
                );
            }

            inventoryGrid = Grid(
                "Inventory grid",
                InventoryPanel.transform,
                new Vector2(.025f, .16f),
                new Vector2(.60f, .685f),
                items.catalog.items
            );
            detail = ScrollText(
                "Inventory detail",
                InventoryPanel.transform,
                new Vector2(.625f, .16f),
                new Vector2(.975f, .685f),
                18
            );
            collectionGrid = Grid(
                "Collection grid",
                CollectionPanel.transform,
                new Vector2(.025f, .12f),
                new Vector2(.60f, .625f),
                Collectibles()
            );
            collectionDetail = ScrollText(
                "Collection detail",
                CollectionPanel.transform,
                new Vector2(.625f, .12f),
                new Vector2(.975f, .625f),
                18
            );
            equip = Button(
                "Equip",
                InventoryPanel.transform,
                "장착",
                new Vector2(.025f, .075f),
                new Vector2(.25f, .145f),
                EquipSelected
            );
            unequip = Button(
                "Unequip",
                InventoryPanel.transform,
                "해제",
                new Vector2(.265f, .075f),
                new Vector2(.49f, .145f),
                UnequipSelected
            );
            use = Button(
                "Use",
                InventoryPanel.transform,
                "사용",
                new Vector2(.505f, .075f),
                new Vector2(.73f, .145f),
                UseSelected
            );
            dismantle = Button(
                "Dismantle",
                InventoryPanel.transform,
                "1개 분해",
                new Vector2(.745f, .075f),
                new Vector2(.97f, .145f),
                DismantleSelected
            );
            feedback = Text(
                "Inventory feedback",
                InventoryPanel.transform,
                "",
                16,
                new Vector2(.025f, .005f),
                new Vector2(.975f, .07f),
                2
            );
            slots = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = (EquipmentSlot)i;
                float top = .79f - i * .18f;
                slots[i] = Button(
                    "Equipment slot " + slot,
                    EquipmentPanel.transform,
                    "",
                    new Vector2(.035f, top - .15f),
                    new Vector2(.48f, top),
                    () =>
                    {
                        selected = items.Equipment.Current(slot);
                        if (selected == null)
                        {
                            filter =
                                slot == EquipmentSlot.Headphone ? 2
                                : slot == EquipmentSlot.Cosmetic ? 4
                                : 1;
                            OpenInventory();
                        }
                        else
                        {
                            OpenInventory();
                            Select(selected);
                        }
                    }
                );
            }

            equipmentSummary = ScrollText(
                "Equipment effects",
                EquipmentPanel.transform,
                new Vector2(.51f, .18f),
                new Vector2(.97f, .79f),
                18
            );
            Button(
                "Choose equipment",
                EquipmentPanel.transform,
                "가방에서 장비 선택",
                new Vector2(.05f, .05f),
                new Vector2(.95f, .14f),
                OpenInventory
            );
            balance = Text(
                "Gacha balance",
                GachaPanel.transform,
                "",
                20,
                new Vector2(.04f, .70f),
                new Vector2(.96f, .815f)
            );
            pity = Text(
                "Pity progress",
                GachaPanel.transform,
                "",
                20,
                new Vector2(.04f, .36f),
                new Vector2(.59f, .695f)
            );
            guide = Text(
                "Collection guide",
                GachaPanel.transform,
                "",
                18,
                new Vector2(.04f, .245f),
                new Vector2(.96f, .36f)
            );
            if (visualTheme?.paperSprite != null)
            {
                var featured = Box(
                    "Featured collection photo",
                    GachaPanel.transform,
                    new Vector2(.62f, .39f),
                    new Vector2(.95f, .69f)
                );
                featured.sprite = visualTheme.paperSprite;
                featured.color = Color.white;
                var featuredItem = items.catalog.Item(
                    items.catalog.pools[0].pityRules.OrderByDescending(r => r.priority).First().guaranteedItem
                );
                var photo = Rect("Featured item", featured.transform, new Vector2(.12f, .22f), new Vector2(.88f, .94f))
                    .gameObject.AddComponent<Image>();
                photo.sprite = featuredItem.icon;
                LifeRecordUI.InkSpecimen(photo);
                photo.preserveAspect = true;
                photo.raycastTarget = false;
                var caption = Text(
                    "Featured caption",
                    featured.transform,
                    featuredItem.displayName,
                    18,
                    new Vector2(.05f, .01f),
                    new Vector2(.95f, .23f),
                    2
                );
                caption.color = visualTheme.buttonInk;
                caption.alignment = TextAlignmentOptions.Center;
            }

            pull1 = Button(
                "Pull one",
                GachaPanel.transform,
                "1회",
                new Vector2(.04f, .145f),
                new Vector2(.48f, .235f),
                () => Pull(1, false)
            );
            pull10 = Button(
                "Pull ten",
                GachaPanel.transform,
                "10회",
                new Vector2(.52f, .145f),
                new Vector2(.96f, .235f),
                () => Pull(10, false)
            );
            scrap = Button(
                "Scrap pull",
                GachaPanel.transform,
                "조각 뽑기",
                new Vector2(.04f, .035f),
                new Vector2(.48f, .125f),
                () => Pull(1, true)
            );
            Button(
                "Probabilities",
                GachaPanel.transform,
                "확률정보",
                new Vector2(.52f, .035f),
                new Vector2(.96f, .125f),
                OpenProbabilities
            );
            var oddsIndex = LifeRecordUI.Surface(ProbabilityPanel.transform, "Probability index", RecordSurface.Inset,
                .025f, .052f, .327f, .868f);
            LifeRecordUI.Print(oddsIndex.transform, RecordPrint.Inventory, .16f, .68f, .84f, .948f);
            string[] rateTitles = { "기본 확률", "다음 1회", "천장과 고지" };
            for (int i = 0; i < rateTitles.Length; i++)
            {
                int page = i;
                float top = .627f - i * .174f;
                probabilityTabs[i] = Button("Probability tab " + i, oddsIndex.transform, rateTitles[i],
                    new Vector2(.055f, top - .15f), new Vector2(.945f, top), () => ShowProbabilityPage(page));
            }

            probability = ScrollText(
                "Pool probabilities",
                ProbabilityPanel.transform,
                new Vector2(.35f, .055f),
                new Vector2(.968f, .862f),
                22
            );
            resultGrid = Grid(
                "Reveal grid",
                ResultPanel.transform,
                new Vector2(.025f, .20f),
                new Vector2(.975f, .88f),
                Enumerable.Repeat(items.catalog.items[0], 10)
            );
            for (int i = 0; i < resultGrid.cells.Count; i++)
            {
                int index = i;
                resultGrid.cells[i].button.onClick.RemoveAllListeners();
                resultGrid
                    .cells[i]
                    .button.onClick.AddListener(() =>
                    {
                        if (activeBatch != null && index < revealed)
                        {
                            selected = activeBatch.results[index].itemId;
                            Close(ResultPanel);
                            OpenInventory();
                            Select(selected);
                        }
                    });
            }

            resultSummary = Text(
                "Result summary",
                ResultPanel.transform,
                "",
                18,
                new Vector2(.03f, .11f),
                new Vector2(.97f, .19f)
            );
            Button(
                "Fast reveal",
                ResultPanel.transform,
                "모두 보기",
                new Vector2(.04f, .015f),
                new Vector2(.48f, .105f),
                FastReveal
            );
            Button(
                "Results inventory",
                ResultPanel.transform,
                "가방 확인",
                new Vector2(.52f, .015f),
                new Vector2(.96f, .105f),
                () =>
                {
                    FastReveal();
                    Close(ResultPanel);
                    OpenInventory();
                }
            );
            var nav = hud.GetComponent<UINavigationService>();
            if (nav != null)
            {
                var old = nav.inventoryPanel;
                nav.inventoryPanel = InventoryPanel;
                foreach (var p in hud.GetComponents<PolishedNavigation>())
                {
                    for (int i = 0; i < p.panels.Length; i++)
                    {
                        if (p.panels[i] == old)
                        {
                            p.panels[i] = InventoryPanel;
                        }
                    }
                }
            }

            items.Changed += Refresh;
            game.growth.Changed += Refresh;
            game.economy.Changed += Refresh;
            game.StateChanged += StateChanged;
            BuildInventoryLayout();
            foreach (var screen in screens)
            {
                if (screen == InventoryPanel)
                {
                    continue;
                }

                RecordPage.Apply((RectTransform)screen.transform, screen.transform.Find("Title").GetComponent<TMP_Text>());
                var section = screen == EquipmentPanel ? "Section 1" : screen == GachaPanel ? "Section 2" : "Section 3";
                var tab = screen.transform.Find(section);
                if (tab)
                {
                    LifeRecordUI.StyleButton(tab.GetComponent<Button>(), true);
                }
            }

            var featureCard = GachaPanel.transform.Find("Featured collection photo");
            if (featureCard)
            {
                LifeRecordUI.Skin(featureCard.GetComponent<Image>(), RecordSurface.Specimen);
                featureCard.Find("Featured caption").GetComponent<TMP_Text>().color = LifeRecordUI.ReverseInk;
            }

            var pityCard = LifeRecordUI.Surface(GachaPanel.transform, "Guarantee ledger", RecordSurface.Inset, .026f, .359f, .598f, .697f);
            pityCard.transform.SetAsFirstSibling();
            LifeRecordUI.Place(pity.rectTransform, .055f, .612f, .58f, .689f);
            var rules = items.catalog.Pool(Pool).pityRules;
            guaranteeLabels = new TMP_Text[rules.Length];
            guaranteeFills = new RectTransform[rules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                float row = .73f / rules.Length;
                float y = .04f + row * (rules.Length - i - 1);
                guaranteeLabels[i] = LifeRecordUI.Label(pityCard.transform, font, "", 18, LifeRecordUI.Body,
                    .05f, y + .023f, .96f, y + row);
                var track = LifeRecordUI.Rect(pityCard.transform, "Guarantee track", .05f, y, .95f, y + .014f);
                var rail = track.gameObject.AddComponent<Image>();
                rail.color = LifeRecordUI.Track;
                rail.raycastTarget = false;
                var fill = LifeRecordUI.Rect(track, "Progress", 0, 0, 0, 1).gameObject.AddComponent<Image>();
                fill.color = LifeRecordUI.Secondary;
                fill.raycastTarget = false;
                guaranteeFills[i] = fill.rectTransform;
            }

            LifeRecordUI.StyleButton(pull10, true);
            var wearer = LifeRecordUI.Surface(EquipmentPanel.transform, "Wearer card", RecordSurface.Inset,
                .53f, .439f, .96f, .80f);
            var portraitRect = LifeRecordUI.Rect(wearer.transform, "Official Ian portrait", .05f, .07f, .55f, .94f);
            var portrait = portraitRect.gameObject.AddComponent<Image>();
            portrait.sprite = world.Player.identity?.baseSprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            LifeRecordUI.Label(wearer.transform, font, "이안", 28, LifeRecordUI.Body, .57f, .40f, .95f, .76f);
            LifeRecordUI.Label(wearer.transform, font, "착용 장비", 18, LifeRecordUI.Secondary, .57f, .17f, .98f, .46f);
            for (int i = 0; i < slots.Length; i++)
            {
                var rect = LifeRecordUI.Rect(slots[i].transform, "Equipped item icon", .06f, .14f, .27f, .86f);
                equippedIcons[i] = rect.gameObject.AddComponent<Image>();
                equippedIcons[i].raycastTarget = false;
                LifeRecordUI.InkSpecimen(equippedIcons[i]);
                equippedIcons[i].preserveAspect = true;
                var text = slots[i].GetComponentInChildren<TMP_Text>();
                LifeRecordUI.Place(text.rectTransform, .32f, .05f, .97f, .95f);
                text.alignment = TextAlignmentOptions.MidlineLeft;
            }
            LifeRecordUI.Place((RectTransform)equipmentSummary.GetComponentInParent<ScrollRect>(true).transform, .53f, .16f, .96f, .42f);

            started = true;
            Resize();
            Refresh();
            hud.GetComponent<WorldMobileHUD>()?.Apply(true);
        }

        IEnumerable<ItemDefinition> Collectibles() =>
            items.catalog.items.Where(d => d.itemType != ItemType.Material && d.itemType != ItemType.Ticket);

        GameObject lastFocus;

        void LateUpdate()
        {
            var focus = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            if (focus == lastFocus)
            {
                return;
            }

            lastFocus = focus;
            if (focus == null || !screens.Any(p => p.activeInHierarchy && focus.transform.IsChildOf(p.transform)))
            {
                return;
            }

            var scroll = focus.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content.rect.height <= scroll.viewport.rect.height)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                scroll.viewport,
                (RectTransform)focus.transform
            );
            float range = scroll.content.rect.height - scroll.viewport.rect.height;
            float delta =
                bounds.min.y < scroll.viewport.rect.yMin ? (bounds.min.y - scroll.viewport.rect.yMin) / range
                : bounds.max.y > scroll.viewport.rect.yMax ? (bounds.max.y - scroll.viewport.rect.yMax) / range
                : 0;
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + delta);
        }

        void OnDestroy()
        {
            if (items != null)
            {
                items.Changed -= Refresh;
            }

            if (game != null)
            {
                game.growth.Changed -= Refresh;
                game.economy.Changed -= Refresh;
                game.StateChanged -= StateChanged;
            }
        }

        void StateChanged(GameState state)
        {
            if (state == GameState.Starting)
            {
                FastReveal();
                foreach (var s in screens)
                {
                    if (s.activeSelf)
                    {
                        shell.Close(s);
                    }
                }
            }

            Refresh();
        }

        void OnRectTransformDimensionsChange()
        {
            if (started)
            {
                Resize();
                Refresh();
            }
        }

        void Close(GameObject panel)
        {
            if (panel == ResultPanel)
            {
                FastReveal();
            }

            shell.Close(panel);
        }

        void Open(GameObject panel, string guideId)
        {
            if (!started || game.BirthIntroductionPending || game.State != GameState.Playing || world.Busy)
            {
                return;
            }

            shell.ReplaceMajor(panel);
            if (items.Unlocked && !game.world.City.Seen(guideId))
            {
                game.world.City.MarkGuide(guideId);
                if (panel == InventoryPanel)
                {
                    feedback.text = "아이템을 선택해 장착·사용하세요. 중복은 분해할 수 있어요.";
                }
            }

            Resize();
            Refresh();
            shell.InvalidateFocus();
        }

        public void OpenInventory()
        {
            Open(InventoryPanel, "items/inventory");
        }

        public void OpenEquipment()
        {
            Open(EquipmentPanel, "items/equipment");
        }

        public void OpenCollection()
        {
            Open(CollectionPanel, "items/collection");
        }

        public void OpenGacha()
        {
            if (!items.Unlocked)
            {
                return;
            }

            Open(GachaPanel, "items/gacha");
        }

        public void OpenProbabilities()
        {
            if (!items.Unlocked)
            {
                return;
            }

            probabilitySections = GachaProbabilityDisclosure.Sections(items, Pool);
            shell.Push(ProbabilityPanel);
            ShowProbabilityPage(0);
        }

        void ShowProbabilityPage(int page)
        {
            probability.text = probabilitySections[page];
            for (int i = 0; i < probabilityTabs.Length; i++)
            {
                LifeRecordUI.StyleButton(probabilityTabs[i], i == page);
            }

            Canvas.ForceUpdateCanvases();
            probability.ForceMeshUpdate(true);
            var scroll = probability.GetComponentInParent<ScrollRect>();
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1;
        }

        string Odds(Dictionary<string, double> odds) =>
            string.Join(
                "\n",
                odds.Select(p => items.catalog.Item(p.Key).displayName + "  " + (p.Value * 100).ToString("0.###") + "%")
            );

        bool Matches(ItemDefinition d) =>
            filter == 0
            || filter == 1 && d.itemType == ItemType.Equipment
            || filter == 2 && d.itemType == ItemType.Headphone
            || filter == 3 && d.itemType == ItemType.Drink
            || filter == 4 && d.itemType == ItemType.Cosmetic
            || filter == 5 && (d.itemType == ItemType.Material || d.itemType == ItemType.Ticket);

        public void Select(string id)
        {
            selected = id;
            selectedCollection = id;
            var e = items.Inventory.Entries.FirstOrDefault(e => e.itemId == id);
            if (e != null && e.isNew)
            {
                items.Inventory.MarkSeen(id, out _);
            }

            Refresh();
        }

        string Slot(EquipmentSlot s) =>
            s == EquipmentSlot.Headphone ? "헤드폰"
            : s == EquipmentSlot.Cosmetic ? "코스튬"
            : "장비";

        string Effects(ItemDefinition d) =>
            d == null ? "미등록 아이템"
            : d.statModifiers.Length == 0
                ? (
                    d.isConsumable
                        ? $"체력 {d.recovery.health:+0;-0;0} / 수분 {d.recovery.thirst:+0;-0;0}\n멘탈 {d.recovery.mental:+0;-0;0} / 행복 {d.recovery.happiness:+0;-0;0}"
                        : "능력치 변화 없음"
                )
            : string.Join(
                "\n",
                d.statModifiers.Select(m =>
                    Target(m.stat)
                    + " "
                    + m.value.ToString("+0.##;-0.##;0")
                    + (m.operation == ModifierOperation.Percent ? "%" : "")
                )
            );

        static string Target(ModifierTarget t) =>
            new[]
            {
                "허기",
                "수분",
                "지식",
                "체력",
                "멘탈",
                "사회성",
                "신용",
                "행복",
                "공부 효율",
                "노동 효율",
                "자동 활동 효율",
                "전투 공격",
                "전투 방어",
                "전투 보상 효율",
            }[(int)t];

        string Info(ItemDefinition d, bool collection = false)
        {
            if (d == null)
            {
                return "아이템을 선택하세요.";
            }

            if (collection && !items.Inventory.Discover(d.itemId))
            {
                return "???\n\n아직 발견하지 않은 아이템입니다.";
            }

            var e = items.Inventory.Entries.FirstOrDefault(e => e.itemId == d.itemId);
            string text =
                d.displayName
                + " · "
                + items.catalog.Rarity(d.rarityId).displayName
                + "\n보유 "
                + (e?.count ?? 0)
                + "개\n\n"
                + d.description
                + "\n\n"
                + Effects(d);
            if (d.Wearable)
            {
                var current = items.catalog.Item(items.Equipment.Current(d.equipmentSlot));
                text +=
                    "\n\n현재 "
                    + Slot(d.equipmentSlot)
                    + " · "
                    + (current?.displayName ?? "없음")
                    + (current == null ? "" : "\n" + Effects(current));
                if (current?.itemId != d.itemId)
                {
                    text += "\n\n교체 시 변화";
                    foreach (
                        var modifier in d
                            .statModifiers.Concat(current?.statModifiers ?? Array.Empty<StatModifier>())
                            .GroupBy(m => new { m.stat, m.operation })
                    )
                    {
                        float before =
                            current
                                ?.statModifiers.Where(m =>
                                    m.stat == modifier.Key.stat && m.operation == modifier.Key.operation
                                )
                                .Sum(m => m.value) ?? 0;
                        float after = d
                            .statModifiers.Where(m =>
                                m.stat == modifier.Key.stat && m.operation == modifier.Key.operation
                            )
                            .Sum(m => m.value);
                        float delta = after - before;
                        if (Mathf.Abs(delta) < .001f)
                        {
                            continue;
                        }

                        text +=
                            "\n"
                            + Target(modifier.Key.stat)
                            + " "
                            + (delta > 0 ? "▲ " : "▼ ")
                            + delta.ToString("+0.##;-0.##;0")
                            + (modifier.Key.operation == ModifierOperation.Percent ? "%" : "");
                    }
                }
            }

            if (collection && e != null)
            {
                text += "\n\n최초 획득 · " + (e.firstDay == 0 ? "이전 플레이 기록" : "DAY " + e.firstDay);
            }

            return text;
        }

        void BuildInventoryLayout()
        {
            var root = InventoryPanel.transform;
            root.gameObject.AddComponent<RecordWindowBounds>();
            LifeRecordUI.Skin(root.GetComponent<Image>(), RecordSurface.Window);
            var bar = LifeRecordUI.Surface(root, "Inventory masthead", RecordSurface.Header, .009f, .906f, .991f, .99f);
            bar.transform.SetAsFirstSibling();
            var title = root.Find("Title").GetComponent<TMP_Text>();
            title.text = "이안의 가방";
            MissionRecordLayout.SetText(title, 30, LifeRecordUI.Heading);
            inventorySummary.color = feedback.color = detail.color = LifeRecordUI.Body;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                LifeRecordUI.StyleButton(button, button == equip || button.name == "Section 0");
            }

            inventoryTray = LifeRecordUI.Surface(root, "Item tray", RecordSurface.Inset, .022f, .157f, .603f, .687f);
            inventoryTray.transform.SetAsFirstSibling();
            bagIllustration = LifeRecordUI.Print(inventoryGrid.view, RecordPrint.Inventory, .22f, .27f, .78f, .82f);
            emptyBag = LifeRecordUI.Label(inventoryGrid.view, font, "가방이 비어 있습니다", 22, LifeRecordUI.Body, .04f, .08f, .96f, .27f);
            emptyBag.alignment = TextAlignmentOptions.Center;
            specimen = LifeRecordUI.Surface(root, "Item specimen", RecordSurface.Specimen, .65f, .49f, .945f, .68f);
            itemPreview = LifeRecordUI.Rect(specimen.transform, "Selected item preview", .06f, .08f, .94f, .92f)
                .gameObject.AddComponent<Image>();
            LifeRecordUI.InkSpecimen(itemPreview);
            itemPreview.preserveAspect = true;
            itemPreview.raycastTarget = false;
        }

        void Refresh()
        {
            if (!started || items.catalog == null)
            {
                return;
            }

            foreach (var b in gachaNavigation)
            {
                b.gameObject.SetActive(items.Unlocked);
            }

            inventorySummary.text = items.Unlocked
                ? $"보유 {items.Inventory.Entries.Count(e => e.count > 0)}종 · 가챠권 {items.Inventory.Count("gacha-ticket")} · 조각 {items.Inventory.Count("collection-scrap")}"
                : items.catalog.unlockAge + "세부터 수집 보관함을 이용할 수 있습니다.";
            sortButton.GetComponentInChildren<TMP_Text>().text = new[] { "등급순", "이름순", "NEW순" }[sorting];
            foreach (var grid in new[] { inventoryGrid, collectionGrid })
            {
                bool collection = grid == collectionGrid;
                int order = 0;
                var cells =
                    sorting == 0
                        ? grid
                            .cells.OrderByDescending(c => items.catalog.Rarity(items.catalog.Item(c.id).rarityId).rank)
                            .ThenBy(c => c.id)
                    : sorting == 1 ? grid.cells.OrderBy(c => items.catalog.Item(c.id).displayName)
                    : grid
                        .cells.OrderByDescending(c =>
                            items.Inventory.Entries.FirstOrDefault(e => e.itemId == c.id)?.isNew == true
                        )
                        .ThenBy(c => c.id);
                foreach (var c in cells)
                {
                    var d = items.catalog.Item(c.id);
                    bool found = items.Inventory.Discover(c.id);
                    bool show = items.Unlocked && Matches(d) && (collection || items.Inventory.Count(c.id) > 0);
                    c.button.gameObject.SetActive(show);
                    if (!show)
                    {
                        continue;
                    }

                    c.button.transform.SetSiblingIndex(order++);
                    c.mount.enabled = d.icon && (!collection || found);
                    c.icon.sprite = d.icon;
                    c.icon.color = collection && !found ? new Color(0, 0, 0, .35f) : Color.white;
                    bool selectedCell = c.id == (collection ? selectedCollection : selected);
                    LifeRecordUI.StyleButton(c.button, selectedCell, GradeFrame(found ? items.catalog.Rarity(d.rarityId).rank : 0));

                    c.name.text = collection && !found ? "???" : d.displayName;
                    c.equipped.enabled = items.Equipment.Current(d.equipmentSlot) == c.id;
                    bool isNew = items.Inventory.Entries.FirstOrDefault(e => e.itemId == c.id)?.isNew == true;
                    c.quantity.text =
                        (c.equipped.enabled ? "장착 · " : "")
                        + (isNew ? "NEW · " : "")
                        + (collection ? (found ? "발견" : "미발견") : "×" + items.Inventory.Count(c.id));
                }

                Layout(grid);
            }

            var chosen = items.catalog.Item(selected);
            for (int i = 0; i < inventoryFilters.Length; i++)
            {
                LifeRecordUI.StyleButton(inventoryFilters[i], i == filter);
                LifeRecordUI.StyleButton(collectionFilters[i], i == filter);
            }

            bool empty = !inventoryGrid.cells.Any(cell => cell.button.gameObject.activeSelf);
            bagIllustration.enabled = empty;
            emptyBag.enabled = empty;
            emptyBag.text = filter == 0 ? "가방이 비어 있습니다" : "이 분류의 아이템이 없습니다";
            itemPreview.sprite = chosen?.icon;
            specimen.gameObject.SetActive(chosen != null && chosen.icon != null);
            detail.text = chosen == null ? "아이템 정보\n\n아이템을 선택하면 효과와 장착 상태를 확인할 수 있습니다." : Info(chosen);
            collectionDetail.text = Info(items.catalog.Item(selectedCollection), true);
            equip.interactable = chosen != null && items.Equipment.CanEquip(selected, out _);
            unequip.interactable =
                items.Unlocked && chosen != null && items.Equipment.Current(chosen.equipmentSlot) == selected;
            use.interactable =
                items.Unlocked && chosen != null && chosen.isConsumable && items.Inventory.Count(selected) > 0;
            dismantle.interactable =
                items.Unlocked
                && chosen != null
                && chosen.dismantlable
                && items.Inventory.Count(selected) > (chosen.protectLast ? 1 : 0)
                && !items.Equipment.Entries.Any(e => e.itemId == selected);
            collectionSummary.text = string.Join(
                " · ",
                new[] { ItemType.Headphone, ItemType.Equipment, ItemType.Drink, ItemType.Cosmetic }.Select(t =>
                {
                    var defs = Collectibles().Where(d => d.itemType == t).ToArray();
                    return (
                            t == ItemType.Headphone ? "헤드폰"
                            : t == ItemType.Equipment ? "장비"
                            : t == ItemType.Drink ? "음료"
                            : "코스튬"
                        )
                        + " "
                        + defs.Count(d => items.Inventory.Discover(d.itemId))
                        + "/"
                        + defs.Length;
                })
            );
            for (int i = 0; i < 3; i++)
            {
                var d = items.catalog.Item(items.Equipment.Current((EquipmentSlot)i));
                equippedIcons[i].sprite = d?.icon;
                equippedIcons[i].enabled = equippedIcons[i].sprite;
                slots[i].GetComponentInChildren<TMP_Text>().text =
                    Slot((EquipmentSlot)i) + "\n" + (d?.displayName ?? "미장착");
            }

            equipmentSummary.text =
                string.Join(
                    "\n\n",
                    items.Equipment.Entries.Select(e =>
                        Slot(e.slot)
                        + " · "
                        + items.catalog.Item(e.itemId)?.displayName
                        + "\n"
                        + Effects(items.catalog.Item(e.itemId))
                    )
                )
                + "\n\n현재 능력치\n"
                + string.Join(
                    " · ",
                    Enumerable.Range(0, 8).Select(i => Target((ModifierTarget)i) + " " + game.stats.Get((CoreStat)i))
                );
            var pool = items.catalog.Pool(Pool);
            var progress = items.Gacha.Progress(Pool);
            balance.text =
                $"{pool.description}\n{game.economy.Cash:N0}G · 루비 {game.economy.Ruby.Total:N0} · 가챠권 {items.Inventory.Count(pool.ticketItemId)} · 조각 {items.Inventory.Count(pool.materialItemId)}";
            pity.text = $"누적 {progress.total}회";
            for (int i = 0; i < pool.pityRules.Length; i++)
            {
                var rule = pool.pityRules[i];
                var counter = progress.pity.FirstOrDefault(entry => entry.ruleId == rule.id);
                string guaranteed = string.IsNullOrEmpty(rule.guaranteedItem)
                    ? items.catalog.Rarity(rule.minimumRarityId).displayName + " 이상"
                    : items.catalog.Item(rule.guaranteedItem).displayName;
                long current = counter?.progress ?? 0;
                guaranteeLabels[i].text = $"{guaranteed}   {current}/{rule.threshold} · 보장 {counter?.applied ?? 0}회";
                guaranteeFills[i].anchorMax = new Vector2(Mathf.Clamp01((float)current / rule.threshold), 1);
            }

            var q1 = items.Gacha.Quote(Pool, 1);
            var q10 = items.Gacha.Quote(Pool, 10);
            var qs = items.Gacha.Quote(Pool, 1, true);
            pull1.interactable = !revealing && q1.CanPull;
            pull10.interactable = !revealing && q10.CanPull;
            scrap.interactable = !revealing && qs.CanPull;
            pull1.GetComponentInChildren<TMP_Text>().text = "1회 · " + Cost(q1);
            pull10.GetComponentInChildren<TMP_Text>().text = "10회 · " + Cost(q10);
            scrap.GetComponentInChildren<TMP_Text>().text = "조각 " + pool.materialCost + "개 · 1회";
            guide.text =
                gachaError
                ?? "가챠권을 먼저 사용합니다. 중복은 가방에서 분해할 수 있어요.\n300회 대표 보상 · "
                    + items
                        .catalog.Item(pool.pityRules.OrderByDescending(r => r.priority).First().guaranteedItem)
                        .displayName;
        }

        string Cost(GachaQuote q) =>
            q.tickets > 0
                ? "권 "
                    + q.tickets
                    + (
                        q.ruby > 0 ? " + 루비 " + q.ruby
                        : q.cash > 0 ? " + " + q.cash + "G"
                        : ""
                    )
            : q.ruby > 0 ? "루비 " + q.ruby
            : q.cash + "G";

        string Receipt() => "item-ui/" + Guid.NewGuid().ToString("N");

        void EquipSelected()
        {
            Message(items.Equipment.Equip(Receipt(), selected, out var reason), reason, "장착했습니다.");
        }

        void UnequipSelected()
        {
            var d = items.catalog.Item(selected);
            if (d != null)
            {
                Message(items.Equipment.Unequip(Receipt(), d.equipmentSlot, out var reason), reason, "해제했습니다.");
            }
        }

        void UseSelected()
        {
            Message(items.Inventory.Use(Receipt(), selected, out var reason), reason, "사용했습니다.");
        }

        void DismantleSelected()
        {
            var id = selected;
            var definition = items.catalog.Item(id);
            var ux = GetComponent<GameplayUXPresenter>();
            if (definition == null)
            {
                return;
            }

            if (ux == null)
            {
                feedback.text = "분해는 생활 공간의 인벤토리에서 확인 후 이용하세요.";
                return;
            }

            ux.Confirm(
                definition.displayName
                    + "을(를) 1개 분해하시겠습니까?\n\n획득: 수집 조각 "
                    + items.Inventory.DismantleValue(id)
                    + "개\n\n분해 후에는 되돌릴 수 없습니다.",
                () =>
                {
                    Message(
                        items.Inventory.Dismantle(Receipt(), id, 1, out var reason),
                        reason,
                        "분해해 수집 조각으로 바꿨습니다."
                    );
                }
            );
        }

        string Error(string reason) =>
            reason != null && reason.StartsWith("저장 실패")
                ? "저장하지 못해 변경을 취소했습니다. 저장 공간을 확인하세요."
                : reason;

        void Message(bool ok, string reason, string success)
        {
            if (!ok && shell.Top != InventoryPanel)
            {
                OpenInventory();
            }

            feedback.text = ok ? success : Error(reason);
            Refresh();
            shell.InvalidateFocus();
        }

        public void Pull(int count, bool material)
        {
            if (revealing)
            {
                return;
            }

            if (!items.Gacha.Pull(Receipt(), Pool, count, material, out var batch, out var reason))
            {
                gachaError = Error(reason);
                if (shell.Top != GachaPanel)
                {
                    OpenGacha();
                }

                Refresh();
                return;
            }

            gachaError = null;
            activeBatch = batch;
            revealed = 0;
            revealing = true;
            foreach (var c in resultGrid.cells)
            {
                c.button.gameObject.SetActive(false);
            }

            for (int i = 0; i < batch.results.Length; i++)
            {
                var c = resultGrid.cells[i];
                c.button.gameObject.SetActive(true);
                LifeRecordUI.StyleButton(c.button);
                c.name.text = "?";
                c.quantity.text = "공개 대기";
                c.mount.enabled = false;
                c.icon.sprite = null;
                c.icon.color = Color.clear;
            }

            resultSummary.text = "새로운 위안을 확인하세요.";
            shell.Push(ResultPanel);
            Layout(resultGrid);
            Refresh();
            revealRoutine = StartCoroutine(Reveal());
        }

        static RecordSurface GradeFrame(int rank)
        {
            return rank switch
            {
                0 => RecordSurface.Button,
                1 => RecordSurface.GradeOne,
                2 => RecordSurface.GradeTwo,
                3 => RecordSurface.GradeThree,
                _ => RecordSurface.GradeFour
            };
        }

        IEnumerator Reveal()
        {
            if (UIShell.ReducedMotion)
            {
                FastReveal();
                yield break;
            }

            yield return new WaitForSecondsRealtime(.2f);
            while (activeBatch != null && revealed < activeBatch.results.Length)
            {
                var hint = activeBatch.results[revealed];
                var rarity = items.catalog.Rarity(hint.rarityId);
                var card = resultGrid.cells[revealed];
                card.name.text = rarity.displayName;
                card.quantity.text = "공개 중";
                LifeRecordUI.StyleButton(card.button, false, GradeFrame(rarity.rank));
                if (rarity.rank >= 2)
                {
                    var image = card.button.targetGraphic as Image;
                    Color rest = image.color;
                    for (float t = 0; t < 1; t += Time.unscaledDeltaTime / .16f)
                    {
                        float light = Mathf.Lerp(.5f, 1, t);
                        image.color = new Color(light, light, light);
                        yield return null;
                    }

                    image.color = rest;
                }
                else
                {
                    yield return new WaitForSecondsRealtime(.06f);
                }
                RevealOne();
                yield return new WaitForSecondsRealtime(.08f);
            }

            revealing = false;
            Refresh();
        }

        void RevealOne()
        {
            var r = activeBatch.results[revealed];
            var d = items.catalog.Item(r.itemId);
            var rarity = items.catalog.Rarity(r.rarityId);
            var c = resultGrid.cells[revealed++];
            c.button.targetGraphic.color = Color.white;
            c.name.text = d.displayName;
            c.quantity.text = rarity.displayName + "\n" + (r.isNew ? "NEW" : "DUPLICATE");
            c.mount.enabled = d.icon;
            c.icon.sprite = d.icon;
            c.icon.color = Color.white;
            LifeRecordUI.StyleButton(c.button, false, GradeFrame(rarity.rank));
            items.Reveal(r);
            if (rarity.rank >= 3)
            {
                RareReveal?.Invoke(r);
            }

            resultSummary.text = "가방에 보관했습니다. " + revealed + " / " + activeBatch.results.Length;
        }

        public void FastReveal()
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }

            if (activeBatch != null)
            {
                while (revealed < activeBatch.results.Length)
                {
                    RevealOne();
                }
            }

            revealing = false;
            if (started)
            {
                Refresh();
            }
        }

        void Layout(GridView view)
        {
            float width = view.view.rect.width;
            int count = view.cells.Count(c => c.button.gameObject.activeSelf);
            bool single = view == resultGrid && count == 1;
            int columns = single ? 1 : Math.Max(1, Mathf.FloorToInt((width - 8) / 188));
            view.grid.constraintCount = columns;
            view.grid.childAlignment = single ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;
            float height = single ? Mathf.Min(390, view.view.rect.height - 16) : 142;
            view.grid.cellSize = new Vector2(
                single ? Mathf.Min(400, width - 16) : Mathf.Max(100, (width - 16 - (columns - 1) * 8) / columns),
                height
            );
            view.content.sizeDelta = new Vector2(0, Mathf.CeilToInt((float)count / columns) * (height + 8) + 16);
            if (view == resultGrid)
            {
                foreach (var c in view.cells)
                {
                    var titleScale = c.name.GetComponent<UITypeScale>();
                    float titleSize = single ? 26 : 18;
                    if (titleScale.baseSize != titleSize)
                    {
                        titleScale.baseSize = titleSize;
                        titleScale.Apply();
                    }
                    c.icon.rectTransform.anchorMin = single ? new Vector2(.12f, .28f) : new Vector2(.03f, .20f);
                    c.icon.rectTransform.anchorMax = single ? new Vector2(.88f, .82f) : new Vector2(.24f, .86f);
                    LifeRecordUI.Place(c.mount.rectTransform, c.icon.rectTransform.anchorMin.x, c.icon.rectTransform.anchorMin.y,
                        c.icon.rectTransform.anchorMax.x, c.icon.rectTransform.anchorMax.y);
                    c.name.rectTransform.anchorMin = single ? new Vector2(.05f, .82f) : new Vector2(.26f, .45f);
                    c.name.rectTransform.anchorMax = single ? new Vector2(.95f, .98f) : new Vector2(.99f, .98f);
                    c.quantity.rectTransform.anchorMin = single ? new Vector2(.05f, .02f) : new Vector2(.26f, .04f);
                    c.quantity.rectTransform.anchorMax = single ? new Vector2(.95f, .25f) : new Vector2(.99f, .43f);
                    c.name.alignment = c.quantity.alignment = single
                        ? TextAlignmentOptions.Center
                        : TextAlignmentOptions.TopLeft;
                }
            }
        }

        public void Reflow()
        {
            Resize();
            Refresh();
        }

        public void Resize()
        {
            if (!started)
            {
                return;
            }

            bool portrait = UnityEngine.Screen.width < UnityEngine.Screen.height;
            Set(
                inventoryGrid.view,
                new Vector2(.025f, portrait ? .45f : .16f),
                new Vector2(portrait ? .975f : .60f, .685f)
            );
            LifeRecordUI.Place(inventoryTray.rectTransform, .022f, portrait ? .445f : .157f,
                portrait ? .978f : .603f, .687f);
            var id = (RectTransform)detail.transform.parent.parent.parent;
            Set(id, new Vector2(portrait ? .23f : .625f, .16f), new Vector2(.975f, portrait ? .43f : .475f));
            LifeRecordUI.Place(specimen.rectTransform, portrait ? .025f : .64f, portrait ? .18f : .48f,
                portrait ? .215f : .96f, portrait ? .425f : .68f);
            Set(
                collectionGrid.view,
                new Vector2(.025f, portrait ? .39f : .12f),
                new Vector2(portrait ? .975f : .60f, .625f)
            );
            var cd = (RectTransform)collectionDetail.transform.parent.parent.parent;
            Set(cd, new Vector2(portrait ? .025f : .625f, .12f), new Vector2(.975f, portrait ? .38f : .625f));
            foreach (var g in grids)
            {
                Layout(g);
            }
        }

        void Set(RectTransform r, Vector2 min, Vector2 max)
        {
            r.anchorMin = min;
            r.anchorMax = max;
            r.sizeDelta = Vector2.zero;
        }
    }
}
