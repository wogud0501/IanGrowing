using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace IanGame
{
    public sealed class CityLifeHUD : MonoBehaviour
    {
        public Vector2[] cityDoorPoints = Array.Empty<Vector2>();
        public LifeWorldController world;
        public Sprite playerIcon;
        public Sprite homeIcon;
        public Sprite workIcon;
        public Sprite schoolIcon;
        public Sprite shopIcon;
        public Sprite financeIcon;
        public Sprite propertyIcon;
        public Sprite parkIcon;
        public GameObject MiniMap { get; private set; }
        public GameObject MapPanel { get; private set; }
        public GameObject AchievementPanel { get; private set; }
        public GameObject GuidePanel { get; private set; }
        public TMP_Text SuggestionText { get; private set; }
        public TMP_Text Detail { get; private set; }

        sealed class Facility
        {
            public Vector2 point;
            public string room;
            public string title;
            public string actions;
            public string door;
            public Sprite icon;
            public bool essential;
        }

        sealed class View
        {
            public RectTransform rect;
            public Image player;
            public Image target;
            public List<Image> markers = new List<Image>();
        }

        readonly List<Facility> facilities = new List<Facility>();
        readonly List<View> views = new List<View>();
        CityLifeService city;
        GameManager game;
        WorldHUD hud;
        UIShell shell;
        TMP_FontAsset font;
        Sprite frame;
        Transform safe;
        TMP_Text achievementText;
        TMP_Text guideText;
        TMP_Text miniTitle;
        TMP_Text routeText;
        GameObject suggestion;
        string selected;
        float next;
        int width;
        int height;
        bool started;
        bool integratedGuide;
        int achievementCategory;

        public void UseIntegratedGuide()
        {
            integratedGuide = true;
            if (suggestion != null)
            {
                suggestion.SetActive(false);
            }
        }

        Color Ink => new Color32(54, 51, 45, 255);

        RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = min;
            r.anchorMax = max;
            r.pivot = new Vector2(.5f, .5f);
            r.sizeDelta = size;
            r.anchoredPosition = position;
            return r;
        }

        Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
        {
            var r = Rect(name, parent, min, max, size, position);
            var image = r.gameObject.AddComponent<Image>();
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            image.color = frame == null ? new Color32(246, 232, 197, 255) : Color.white;
            return image;
        }

        TMP_Text Text(string name, Transform parent, string value, float size, Vector2 min, Vector2 max, Vector2 inset)
        {
            var r = Rect(name, parent, min, max, -inset * 2, Vector2.zero);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = value;
            t.fontSize = size;
            t.color = Ink;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            var scale = t.gameObject.AddComponent<UITypeScale>();
            scale.baseSize = size;
            scale.Apply();
            return t;
        }

        Button Button(
            string name,
            Transform parent,
            string value,
            Vector2 min,
            Vector2 max,
            Vector2 size,
            Vector2 position,
            Action action
        )
        {
            var image = Panel(name, parent, min, max, size, position);
            var b = image.gameObject.AddComponent<Button>();
            b.targetGraphic = image;
            b.onClick.AddListener(() => action());
            var label = Text("Label", b.transform, value, 18, Vector2.zero, Vector2.one, new Vector2(4, 2));
            label.alignment = TextAlignmentOptions.Center;
            return b;
        }

        GameObject CreateScreen(string name, string title)
        {
            var p = Panel(
                name,
                shell.overlayRoot,
                new Vector2(.06f, .12f),
                new Vector2(.94f, .88f),
                Vector2.zero,
                Vector2.zero
            );
            Text("Title", p.transform, title, 24, new Vector2(.04f, .89f), new Vector2(.83f, .99f), Vector2.zero);
            Button(
                "Close",
                p.transform,
                "닫기",
                new Vector2(.84f, .90f),
                new Vector2(.98f, .98f),
                Vector2.zero,
                Vector2.zero,
                () => shell.Close(p.gameObject)
            );
            p.gameObject.SetActive(false);
            return p.gameObject;
        }

        void Start()
        {
            if (world == null || world.Service == null)
            {
                return;
            }

            game = world.Service.Game;
            city = world.Service.City;
            hud = world.hud;
            shell = hud.shell;
            font = hud.needs.font;
            frame = hud.promptPanel.GetComponent<Image>().sprite;
            safe = hud.promptPanel.transform.parent;
            string[] ids = { "alley", "workplace", "school", "shop", "finance", "realestate" };
            string[] titles = { "주거지", "회사", "학교", "상점", "증권사", "이안 부동산" };
            string[] actions =
            {
                "부모님 집 · 현재 주거지",
                "채용 조건 · 근무 · 급여",
                "교육 · 공부 · 친구",
                "식료품 · 식사 · 가구",
                "종목 조회 · 매수 · 매도",
                "매물 · 계약 · 이사",
            };
            Sprite[] icons = { homeIcon, workIcon, schoolIcon, shopIcon, financeIcon, propertyIcon };
            for (int i = 0; i < ids.Length; i++)
            {
                var edge = world.Room.catalog.portals.FirstOrDefault(p =>
                    p.sourceRoom == "city" && p.destinationRoom == ids[i]
                );
                if (edge == null)
                {
                    continue;
                }

                facilities.Add(
                    new Facility
                    {
                        room = ids[i],
                        title = titles[i],
                        actions = actions[i],
                        icon = icons[i],
                        point = cityDoorPoints.Length > i ? cityDoorPoints[i] : Vector2.zero,
                        door = edge.doorId,
                        essential = i == 0 || i == 2 || i == 3,
                    }
                );
            }

            var mini = Panel(
                "City minimap",
                safe,
                Vector2.one,
                Vector2.one,
                new Vector2(224, 184),
                new Vector2(-136, -180)
            );
            mini.rectTransform.pivot = Vector2.one;
            mini.rectTransform.anchoredPosition = new Vector2(-24, -76);
            MiniMap = mini.gameObject;
            miniTitle = Text(
                "District",
                mini.transform,
                "도시 중심",
                18,
                new Vector2(.05f, .82f),
                new Vector2(.76f, .99f),
                Vector2.zero
            );
            BuildView(mini.transform, new Vector2(.06f, .12f), new Vector2(.94f, .81f));
            Button(
                "ExpandMap",
                mini.transform,
                "지도",
                Vector2.one,
                Vector2.one,
                new Vector2(64, 40),
                new Vector2(-36, -22),
                OpenMap
            );
            MapPanel = CreateScreen("City map screen", "지도 · 시설과 가능한 일");
            BuildView(MapPanel.transform, new Vector2(.04f, .26f), new Vector2(.64f, .86f));
            for (int i = 0; i < facilities.Count; i++)
            {
                var f = facilities[i];
                float top = .86f - i * .09f;
                Button(
                    "Facility " + f.room,
                    MapPanel.transform,
                    f.title,
                    new Vector2(.68f, top - .075f),
                    new Vector2(.96f, top),
                    Vector2.zero,
                    Vector2.zero,
                    () => Select(f)
                );
            }

            Detail = Text(
                "Facility detail",
                MapPanel.transform,
                "시설을 선택하면 가능한 일과 해금 조건을 확인할 수 있어요.",
                18,
                new Vector2(.04f, .10f),
                new Vector2(.72f, .24f),
                Vector2.zero
            );
            Button(
                "Track destination",
                MapPanel.transform,
                "길 안내",
                new Vector2(.76f, .15f),
                new Vector2(.95f, .24f),
                Vector2.zero,
                Vector2.zero,
                () =>
                {
                    if (selected == null)
                    {
                        return;
                    }

                    if (city.Track(selected, out var why))
                    {
                        shell.Close(MapPanel);
                        hud.Notify("목적지를 표시했습니다. 문 앞에서 [E]로 이동하세요.");
                    }
                    else
                    {
                        hud.Notify(why);
                    }
                }
            );
            Button(
                "Clear destination",
                MapPanel.transform,
                "안내 지우기",
                new Vector2(.76f, .04f),
                new Vector2(.95f, .13f),
                Vector2.zero,
                Vector2.zero,
                () => city.Track(null, out _)
            );
            AchievementPanel = CreateScreen("Achievements screen", "도전과제 · 자유롭게 경험하기");
            var achievementScroll = Scroll(AchievementPanel.transform);
            achievementText = Text(
                "Achievement rows",
                achievementScroll,
                "",
                18,
                Vector2.zero,
                Vector2.one,
                new Vector2(12, 10)
            );
            achievementText.rectTransform.pivot = new Vector2(.5f, 1);
            achievementText.rectTransform.anchorMin = new Vector2(0, 1);
            achievementText.rectTransform.anchorMax = Vector2.one;
            achievementText.rectTransform.sizeDelta = new Vector2(0, 900);
            achievementText.rectTransform.anchoredPosition = Vector2.zero;
            Button(
                "Achievement previous",
                AchievementPanel.transform,
                "위",
                new Vector2(.63f, .9f),
                new Vector2(.71f, .98f),
                Vector2.zero,
                Vector2.zero,
                () =>
                    achievementScroll.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = Mathf.Clamp01(
                        achievementScroll.GetComponentInParent<ScrollRect>().verticalNormalizedPosition + .35f
                    )
            );
            Button(
                "Achievement next",
                AchievementPanel.transform,
                "아래",
                new Vector2(.73f, .9f),
                new Vector2(.82f, .98f),
                Vector2.zero,
                Vector2.zero,
                () =>
                    achievementScroll.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = Mathf.Clamp01(
                        achievementScroll.GetComponentInParent<ScrollRect>().verticalNormalizedPosition - .35f
                    )
            );
            string[] achievementTabs = { "전체", "생활·성장", "직업·경제", "수집", "사회" };
            for (int i = 0; i < achievementTabs.Length; i++)
            {
                int index = i;
                Button(
                    "Achievement category " + i,
                    AchievementPanel.transform,
                    achievementTabs[i],
                    new Vector2(.04f + i * .185f, .81f),
                    new Vector2(.215f + i * .185f, .88f),
                    Vector2.zero,
                    Vector2.zero,
                    () =>
                    {
                        achievementCategory = index;
                        Refresh();
                    }
                );
            }

            var achievementViewport = achievementScroll
                .GetComponentInParent<ScrollRect>(true)
                .GetComponent<RectTransform>();
            achievementViewport.anchorMax = new Vector2(.96f, .79f);
            GuidePanel = CreateScreen("Guide screen", "생활 안내");
            var guideContent = Scroll(GuidePanel.transform);
            ((RectTransform)guideContent.parent).anchorMin = new Vector2(.05f, .24f);
            ((RectTransform)guideContent.parent).anchorMax = new Vector2(.95f, .85f);
            guideText = Text("Guide content", guideContent, "", 20, Vector2.zero, Vector2.one, new Vector2(10, 10));
            guideText.rectTransform.anchorMin = new Vector2(0, 1);
            guideText.rectTransform.anchorMax = Vector2.one;
            guideText.rectTransform.pivot = new Vector2(.5f, 1);
            Button(
                "Toggle suggestions",
                GuidePanel.transform,
                "추천 표시 / 숨기기",
                new Vector2(.1f, .05f),
                new Vector2(.9f, .16f),
                Vector2.zero,
                Vector2.zero,
                () => city.ShowSuggestions(city.Data.suggestionsHidden)
            );
            var card = Panel(
                "Optional next action",
                safe,
                new Vector2(0, 0),
                new Vector2(0, 0),
                new Vector2(380, 84),
                new Vector2(214, 120)
            );
            suggestion = card.gameObject;
            SuggestionText = Text(
                "Suggestion",
                card.transform,
                "",
                17,
                new Vector2(.04f, .08f),
                new Vector2(.76f, .95f),
                Vector2.zero
            );
            Button(
                "Suggestion guide",
                card.transform,
                "보기",
                new Vector2(.77f, .16f),
                new Vector2(.97f, .84f),
                Vector2.zero,
                Vector2.zero,
                () =>
                {
                    var s = city.Suggest();
                    if (!string.IsNullOrEmpty(s.room) && city.Track(s.room, out var reason))
                    {
                        hud.Notify("현재 방의 다음 문을 확인하세요.");
                    }
                    else
                    {
                        OpenGuide();
                    }
                }
            );
            routeText = Text(
                "Next doorway",
                safe,
                "",
                18,
                new Vector2(.30f, .81f),
                new Vector2(.70f, .87f),
                Vector2.zero
            );
            routeText.alignment = TextAlignmentOptions.Center;
            var nav = Rect(
                "City life menu",
                hud.menuPanel.transform,
                new Vector2(.03f, .02f),
                new Vector2(.97f, .13f),
                Vector2.zero,
                Vector2.zero
            );
            Button(
                "Map menu",
                nav,
                "지도",
                new Vector2(0, 0),
                new Vector2(.31f, 1),
                Vector2.zero,
                Vector2.zero,
                OpenMap
            );
            Button(
                "Achievements menu",
                nav,
                "도전과제",
                new Vector2(.34f, 0),
                new Vector2(.65f, 1),
                Vector2.zero,
                Vector2.zero,
                OpenAchievements
            );
            Button(
                "Guide menu",
                nav,
                "가이드",
                new Vector2(.68f, 0),
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                OpenGuide
            );
            city.Changed += Refresh;
            city.Notice += Notice;
            shell.Changed += Refresh;
            started = true;
            GetComponent<WorldMobileHUD>()?.Apply(true);
            Refresh();
        }

        Transform Scroll(Transform parent)
        {
            var r = Rect(
                "Achievements scroll",
                parent,
                new Vector2(.04f, .05f),
                new Vector2(.96f, .85f),
                Vector2.zero,
                Vector2.zero
            );
            var scroll = r.gameObject.AddComponent<ScrollRect>();
            var mask = r.gameObject.AddComponent<Image>();
            mask.color = new Color(1, 1, 1, .01f);
            r.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", r, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(.5f, 1);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.sizeDelta = new Vector2(0, 920);
            scroll.content = content;
            scroll.viewport = r;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }

        void BuildView(Transform parent, Vector2 min, Vector2 max)
        {
            var image = Panel("Map plot", parent, min, max, Vector2.zero, Vector2.zero);
            image.sprite = null;
            image.color = new Color32(210, 205, 174, 255);
            var view = new View { rect = image.rectTransform };
            views.Add(view);
            var zone = world.Room.rules.Zone("city");
            var road = Panel("Road", view.rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            road.sprite = null;
            road.color = new Color32(154, 144, 123, 255);
            road.rectTransform.anchorMin = Project(new Vector2(-zone.size.x / 2, -6.7f));
            road.rectTransform.anchorMax = Project(new Vector2(zone.size.x / 2, -2.1f));
            foreach (var f in facilities)
            {
                var b = Button(
                    "POI " + f.room,
                    view.rect,
                    "",
                    Vector2.zero,
                    Vector2.zero,
                    new Vector2(28, 28),
                    Vector2.zero,
                    () => Select(f)
                );
                var marker = b.GetComponent<Image>();
                marker.sprite = f.icon;
                marker.preserveAspect = true;
                view.markers.Add(marker);
                var label = b.GetComponentInChildren<TMP_Text>();
                label.name = "Discovery";
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                label.alignment = TextAlignmentOptions.Center;
                var focus = b.gameObject.AddComponent<CityFacilityFocus>();
                focus.show = () => Select(f);
            }

            view.target = Panel(
                "Destination marker",
                view.rect,
                Vector2.zero,
                Vector2.zero,
                new Vector2(34, 34),
                Vector2.zero
            );
            view.target.sprite = null;
            view.target.color = new Color32(233, 171, 52, 150);
            view.target.raycastTarget = false;
            view.target.transform.SetAsFirstSibling();
            view.player = Panel(
                "Player marker",
                view.rect,
                Vector2.zero,
                Vector2.zero,
                new Vector2(20, 20),
                Vector2.zero
            );
            view.player.sprite = playerIcon;
            view.player.preserveAspect = true;
            view.player.color = Color.white;
            view.player.raycastTarget = false;
        }

        Vector2 Project(Vector2 local)
        {
            var z = world.Room.rules.Zone("city");
            return new Vector2(Mathf.Clamp01(local.x / z.size.x + .5f), Mathf.Clamp01(local.y / z.size.y + .5f));
        }

        Vector2 Point(Facility f) => f.point;

        void Select(Facility f)
        {
            selected = f.room;
            bool allowed = GrowthAccess.Zone(game, f.room, out var reason);
            Detail.text =
                f.title
                + " · "
                + f.actions
                + "\n"
                + (allowed ? "도전과제와 관계없이 자유롭게 이용할 수 있어요." : reason);
            if (shell.Depth == 0)
            {
                hud.Notify(f.title + " · " + f.actions + (allowed ? "" : "\n" + reason));
            }
        }

        public void OpenMap()
        {
            if (!started)
            {
                return;
            }

            shell.ReplaceMajor(MapPanel);
            city.MarkGuide("screen/map");
            Refresh();
        }

        public void OpenAchievements()
        {
            shell.ReplaceMajor(AchievementPanel);
            city.MarkGuide("screen/achievements");
            Refresh();
        }

        public void OpenGuide()
        {
            shell.ReplaceMajor(GuidePanel);
            city.MarkGuide("screen/guide");
            Refresh();
        }

        void Notice(string text)
        {
            if (world.CanControl)
            {
                hud.Notify(text, 1);
            }
        }

        public void Reflow()
        {
            Canvas.ForceUpdateCanvases();
            Refresh();
        }

        void Refresh()
        {
            if (!started || game.State == GameState.Starting || world.CurrentZone == null)
            {
                return;
            }

            var s = city.Suggest();
            SuggestionText.text = "오늘 해볼 만한 일\n" + s.title;
            var sr = (RectTransform)suggestion.transform;
            float sh =
                SuggestionText
                    .GetPreferredValues(SuggestionText.text, Mathf.Max(170, sr.rect.width * .72f), Mathf.Infinity)
                    .y + 18;
            sr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(84, sh));
            achievementText.text = string.Join(
                "\n\n",
                CityLifeService
                    .Catalog.Where(a => achievementCategory == 0 || AchievementCategory(a.id) == achievementCategory)
                    .Select(a =>
                        (city.Data.achievements.Contains(a.id) ? "[완료] " : "[ ] ")
                        + (a.hidden && !city.Data.achievements.Contains(a.id) ? "???" : a.title)
                        + "  "
                        + Math.Min(a.target, city.Counter(a.id))
                        + "/"
                        + a.target
                        + "\n"
                        + (a.hidden && !city.Data.achievements.Contains(a.id) ? "발견 후 공개됩니다." : a.description)
                        + (game.growth.Age < a.minAge ? " · 성장 후 이용" : "")
                    )
            );
            float preferred = achievementText
                .GetPreferredValues(
                    achievementText.text,
                    Mathf.Max(180, ((RectTransform)achievementText.transform.parent).rect.width - 24),
                    Mathf.Infinity
                )
                .y;
            achievementText.rectTransform.sizeDelta = new Vector2(0, preferred + 24);
            ((RectTransform)achievementText.transform.parent).sizeDelta = new Vector2(0, preferred + 24);
            guideText.text =
                "현재 장소 · "
                + world.CurrentZone.title
                + "\n\n"
                + s.title
                + "\n"
                + s.detail
                + "\n\n1세부터 집 안 보행 / 7세부터 독립 외출\n19세부터 조건에 따라 독립을 선택할 수 있어요.\n\n지도에서 시설과 가능한 행동을 확인하세요.\n추천과 도전과제는 장소를 잠그지 않습니다.";
            float gh =
                guideText
                    .GetPreferredValues(
                        guideText.text,
                        Mathf.Max(180, ((RectTransform)guideText.transform.parent).rect.width - 20),
                        Mathf.Infinity
                    )
                    .y + 24;
            guideText.rectTransform.sizeDelta = new Vector2(0, gh);
            ((RectTransform)guideText.transform.parent).sizeDelta = new Vector2(0, gh);
        }

        static int AchievementCategory(string id)
        {
            if (id.StartsWith("item/"))
            {
                return 3;
            }

            if (id == "friend")
            {
                return 4;
            }

            if (new[] { "work", "salary", "purchase", "furniture", "placement", "move", "investment" }.Contains(id))
            {
                return 2;
            }

            return 1;
        }

        bool MapPressed()
        {
            if (!Application.isFocused || world.Player.TextInputFocused)
            {
                return false;
            }

#if UNITY_EDITOR
            if (
                UnityEditor.EditorWindow.focusedWindow == null
                || UnityEditor.EditorWindow.focusedWindow.GetType().Name != "GameView"
            )
            {
                return false;
            }

#endif
            return Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
        }

        void Update()
        {
            if (!started)
            {
                return;
            }

            if (world.CanControl && MapPressed())
            {
                OpenMap();
            }

            if (Time.unscaledTime < next)
            {
                return;
            }

            next = Time.unscaledTime + .1f;
            bool cityRoom = world.Room.roomId == "city";
            bool clear = shell.Depth == 0 && !world.Busy && !game.BirthIntroductionPending;
            MiniMap.SetActive(cityRoom && clear);
            suggestion.SetActive(clear && !city.Data.suggestionsHidden && !integratedGuide);
            routeText.gameObject.SetActive(clear && !string.IsNullOrEmpty(city.Data.destination));
            if (routeText.gameObject.activeSelf)
            {
                var id = world.Room.catalog.FirstDoor(world.Room.roomId, city.Data.destination);
                var door = world.objects.FirstOrDefault(o => o.id == id);
                routeText.text =
                    world.Room.roomId == city.Data.destination ? "목적지에 도착했어요."
                    : door == null ? "지도에서 목적지를 확인하세요."
                    : "다음 문 · " + door.title;
            }

            foreach (var view in views)
            {
                for (int i = 0; i < facilities.Count; i++)
                {
                    var f = facilities[i];
                    var marker = view.markers[i];
                    var p = Project(Point(f));
                    marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = p;
                    bool known =
                        f.essential || city.Data.discovered.Contains(f.room) || city.Data.destination == f.room;
                    marker.sprite = known ? f.icon : null;
                    marker.color = known ? Color.white : new Color32(236, 225, 195, 255);
                    marker.GetComponentInChildren<TMP_Text>().text = known ? "" : "?";
                    if (
                        cityRoom
                        && Vector2.Distance(world.Player.Position - world.CurrentZone.origin, Point(f)) < 3
                        && !city.Data.discovered.Contains(f.room)
                    )
                    { /* Discovery is committed on actual facility entry; hints never fabricate visits. */
                    }
                }

                view.player.gameObject.SetActive(cityRoom);
                var p0 = Project(world.Player.Position - world.CurrentZone.origin);
                view.player.rectTransform.anchorMin = view.player.rectTransform.anchorMax = p0;
                view.player.rectTransform.localRotation = Quaternion.Euler(
                    0,
                    0,
                    Mathf.Atan2(world.Player.Facing.y, world.Player.Facing.x) * Mathf.Rad2Deg
                );
                var target = facilities.FirstOrDefault(f => f.room == city.Data.destination);
                view.target.gameObject.SetActive(target != null);
                if (target != null)
                {
                    view.target.rectTransform.anchorMin = view.target.rectTransform.anchorMax = Project(Point(target));
                }
            }

            if (width != Screen.width || height != Screen.height)
            {
                width = Screen.width;
                height = Screen.height;
                bool narrow = width < height;
                MiniMap.GetComponent<RectTransform>().sizeDelta = narrow
                    ? new Vector2(168, 142)
                    : new Vector2(224, 184);
                suggestion.GetComponent<RectTransform>().sizeDelta = narrow
                    ? new Vector2(280, 76)
                    : new Vector2(380, 84);
                suggestion.GetComponent<RectTransform>().anchoredPosition = narrow
                    ? new Vector2(156, 160)
                    : new Vector2(214, 120);
                Reflow();
            }
        }

        void OnDestroy()
        {
            if (city != null)
            {
                city.Changed -= Refresh;
                city.Notice -= Notice;
            }

            if (shell != null)
            {
                shell.Changed -= Refresh;
            }
        }
    }

    public sealed class CityMapArrow : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            vh.AddVert(new Vector3(r.center.x, r.yMax), color, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMin), color, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMin), color, Vector2.zero);
            vh.AddTriangle(0, 1, 2);
        }
    }

    public sealed class CityFacilityFocus : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public Action show;

        public void OnPointerEnter(PointerEventData e) => show?.Invoke();

        public void OnSelect(BaseEventData e) => show?.Invoke();
    }
}
