using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace IanGame
{
    [DefaultExecutionOrder(400)]
    [RequireComponent(typeof(WorldHUD))]
    public sealed partial class GameplayUXPresenter : MonoBehaviour
    {
        public Phase23Presenter activities;
        public bool Ready { get; private set; }
        public GameObject Hub { get; private set; }
        public GameObject Status { get; private set; }
        public GameObject Help { get; private set; }
        public GameObject History { get; private set; }
        public GameObject Preferences { get; private set; }
        public string Recommendation => guide?.text;

        public event Action<string> AudioRequested;
        WorldHUD hud;
        GameManager game;
        CityLifeService city;
        UIShell shell;
        PixelUXWidgets ui;
        RectTransform card;
        TMP_Text guide;
        TMP_Text mission;
        TMP_Text statusText;
        TMP_Text helpText;
        TMP_Text historyText;
        TMP_Text hubText;
        SessionPreferences preferences;
        TMP_Text saveText;
        readonly List<GameObject> windows = new List<GameObject>();
        readonly Button[] hubActions = new Button[8];
        int statusTab;
        int hubTab;
        int width;
        int height;
        Vector2 viewportSize;
        bool dirty;
        float saveUntil;
        GameObject confirmation;
        TMP_Text confirmationText;
        Action confirmedAction;
        public GameObject Confirmation => confirmation;

        WorldInteractable observedTarget;
        readonly Button[] hubTabs = new Button[4];
        readonly Button[] statusTabs = new Button[5];
        TMP_Text residentAge;
        RectTransform vitalRecord;
        readonly TMP_Text[] vitalValues = new TMP_Text[8];
        readonly RectTransform[] vitalLevels = new RectTransform[8];
        TMP_Text conditionNote;
        TMP_Text statusAction;
        static readonly string[] StatNames = { "허기", "목마름", "지식", "체력", "멘탈", "사회성", "신용도", "행복도" };
        static readonly string[] FeatureIds = { "items", "school", "jobs", "stocks", "housing" };
        static readonly string[] FeatureNames = { "수집·가챠", "학교 생활", "직업", "주식 거래", "독립·주거" };
        static readonly string[] FeaturePlaces =
        {
            "메뉴 → 아이템",
            "학교 → 교실",
            "회사 → 채용·근무",
            "도시 → 투자센터",
            "메뉴 → 금융 → 집 관리",
        };

        void Start()
        {
            hud = GetComponent<WorldHUD>();
            game = hud.world?.Service?.Game;
            if (game == null || GetComponent<CollectionUI>() == null || GetComponent<CityLifeHUD>() == null)
            {
                return;
            }

            activities ??= gameObject
                .scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Phase23Presenter>(true))
                .FirstOrDefault();
            city = game.world.City;
            shell = hud.shell;
            ui = new PixelUXWidgets(hud);
            Build();
            game.stats.Changed += Stats;
            game.StateChanged += GameStateChanged;
            game.economy.Changed += Invalidate;
            game.items.Changed += Invalidate;
            game.items.NewItemDiscovered += NewItem;
            lastInputLabel = hud.InteractionInput;
            InputSystem.onActionChange += InputChanged;
            city.TutorialStepCompleted += Learned;
            game.missions.RewardClaimed += RewardClaimed;
            game.missions.Completed += MissionCompleted;
            game.missions.Changed += Invalidate;
            game.world.Changed += Invalidate;
            game.world.Restored += Restored;
            game.growth.Changed += Invalidate;
            game.ActionPerformed += ActionCompleted;
            game.Notifications.Changed += Invalidate;
            city.Changed += Invalidate;
            shell.Changed += NavigationChanged;
            game.saves.WriteCompleted += Saved;
            Ready = true;
            Refresh();
            Layout();
        }

        GameObject Window(string title)
        {
            var window = ui.Window(shell, title);
            windows.Add(window);
            return window;
        }

        void Build()
        {
            card = ui.Panel(
                "Next action · optional",
                hud.promptPanel.transform.parent,
                new Vector2(0, 1),
                new Vector2(0, 1)
            );
            card.pivot = new Vector2(0, 1);
            LifeRecordUI.Skin(card.GetComponent<Image>(), RecordSurface.Window);
            var guideTab = LifeRecordUI.Rect(card, "Guide tab", 0, .18f, .015f, .84f);
            guideRail = guideTab.gameObject.AddComponent<Image>();
            guideRail.raycastTarget = false;
            guideRail.color = LifeRecordUI.Track;
            mission = ui.Text(card, "Current mission", 17, new Vector2(.04f, .71f), new Vector2(.96f, .96f));
            guide = ui.Text(card, "Optional recommendation", 17, new Vector2(.04f, .12f), new Vector2(.96f, .68f));
            mission.color = LifeRecordUI.Body;
            guide.color = LifeRecordUI.Secondary;
            var helpMark = ui.Text(card, "Help shortcut", 17, Vector2.one, Vector2.one);
            helpMark.text = "?";
            helpMark.color = LifeRecordUI.Secondary;
            helpMark.alignment = TextAlignmentOptions.Center;
            helpMark.rectTransform.pivot = Vector2.one;
            helpMark.rectTransform.sizeDelta = new Vector2(22, 24);
            helpMark.rectTransform.anchoredPosition = new Vector2(-6, -8);
            helpMark.raycastTarget = false;
            var open = card.gameObject.AddComponent<Button>();
            open.onClick.AddListener(OpenHelp);
            saveText = ui.Text(
                hud.promptPanel.transform.parent,
                "Save status",
                16,
                new Vector2(.72f, .02f),
                new Vector2(.97f, .07f)
            );
            saveText.alignment = TextAlignmentOptions.Right;
            Hub = Window("생활 메뉴");
            string[] hubs = { "생활", "아이템", "금융", "시스템" };
            for (int i = 0; i < hubs.Length; i++)
            {
                int index = i;
                hubTabs[i] = ui.Button(
                    Hub.transform,
                    hubs[i],
                    new Vector2(.032f, .69f - i * .166f),
                    new Vector2(.274f, .828f - i * .166f),
                    () => HubTab(index)
                );
                LifeRecordUI.Print(hubTabs[i].transform, (RecordPrint)((int)RecordPrint.Life + i), .035f, .16f, .34f, .84f);
                var tabLabel = hubTabs[i].GetComponentInChildren<TMP_Text>();
                LifeRecordUI.Place(tabLabel.rectTransform, .33f, .05f, .98f, .95f);
            }

            hubText = ui.ScrollText(Hub.transform, new Vector2(.315f, .69f), new Vector2(.955f, .855f));
            for (int i = 0; i < hubActions.Length; i++)
            {
                int row = i / 2;
                hubActions[i] = ui.Button(
                    Hub.transform,
                    "기능",
                    new Vector2(.309f + i % 2 * .33f, .507f - row * .144f),
                    new Vector2(.62f + i % 2 * .33f, .634f - row * .144f),
                    () => { }
                );
            }

            Status = Window("이안의 상태");
            string[] categories = { "상태", "인생", "직업", "경제", "장비" };
            for (int i = 0; i < categories.Length; i++)
            {
                int index = i;
                statusTabs[i] = ui.Button(
                    Status.transform,
                    categories[i],
                    new Vector2(.04f + i * .185f, .79f),
                    new Vector2(.215f + i * .185f, .88f),
                    () =>
                    {
                        statusTab = index;
                        RefreshStatus();
                    }
                );
            }

            var stub = LifeRecordUI.Surface(Status.transform, "Resident card", RecordSurface.Inset, .034f, .21f, .366f, .752f);
            LifeRecordUI.Label(stub.transform, hud.needs.font, "이안", 32, LifeRecordUI.Body, .09f, .16f, .9f, .36f);
            residentAge = LifeRecordUI.Label(stub.transform, hud.needs.font, "", 20, LifeRecordUI.Secondary, .09f, .025f, .96f, .185f);
            var portrait = LifeRecordUI.Rect(stub.transform, "Official Ian", .15f, .37f, .85f, .96f);
            var image = portrait.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.sprite = hud.world.Player.identity?.baseSprite;
            image.enabled = image.sprite != null;
            statusText = ui.ScrollText(Status.transform, new Vector2(.40f, .215f), new Vector2(.96f, .751f));
            BuildVitalRecord();
            var openRelated = ui.Button(
                Status.transform,
                "관련 기능 열기",
                new Vector2(.06f, .07f),
                new Vector2(.94f, .18f),
                () =>
                {
                    if (statusTab == 3)
                    {
                        GetComponent<FinancePresenter>().OpenEconomy();
                    }
                    else if (statusTab == 4)
                    {
                        GetComponent<CollectionUI>().OpenEquipment();
                    }
                    else if (statusTab == 1 || statusTab == 2)
                    {
                        if (activities != null)
                        {
                            shell.Push(activities.panel);
                        }
                        else
                        {
                            statusText.text =
                                "이 장소에서는 교육·직업 창을 열 수 없습니다. 학교나 회사에서 이용하세요.";
                        }
                    }
                    else
                    {
                        OpenHelp();
                    }
                }
            );
            statusAction = openRelated.GetComponentInChildren<TMP_Text>();
            BuildGuide();
            History = Window("최근 기록");
            historyText = ui.ScrollText(History.transform, new Vector2(.06f, .07f), new Vector2(.94f, .86f));
            preferences = new SessionPreferences(shell, ui, city, OpenHelp,
                () => GetComponent<IdleActivityPresenter>().Open(), Layout);
            Preferences = preferences.Preferences;
            windows.Add(Preferences);
            shell.statusPanel = Status;
            confirmation = Window("실행 확인");
            confirmationText = ui.ScrollText(confirmation.transform, new Vector2(.06f, .3f), new Vector2(.94f, .84f));
            ui.Button(
                confirmation.transform,
                "취소",
                new Vector2(.06f, .09f),
                new Vector2(.46f, .23f),
                () =>
                {
                    confirmedAction = null;
                    shell.Close(confirmation);
                }
            );
            ui.Button(
                confirmation.transform,
                "확인 · 실행",
                new Vector2(.54f, .09f),
                new Vector2(.94f, .23f),
                ConfirmPending
            );
        }

        public void Confirm(string message, Action action)
        {
            confirmationText.text = message;
            confirmedAction = action;
            Open(confirmation);
        }

        public void ConfirmPending()
        {
            if (shell.Top != confirmation)
            {
                return;
            }

            var action = confirmedAction;
            confirmedAction = null;
            shell.Close(confirmation);
            action?.Invoke();
        }

        public void Explain(string reason)
        {
            if (shell.Top == Hub)
            {
                hubText.text = reason;
            }
        }

        public void OpenPreferences()
        {
            Open(Preferences);
        }

        public void OpenHub()
        {
            HubTab(hubTab);
            Open(Hub);
        }

        public void OpenStatus(int tab = 0)
        {
            statusTab = tab;
            if (tab == 1 && FeatureAvailable(1))
            {
                city.MarkGuide("ux/feature/read/school");
            }

            if (tab == 2 && FeatureAvailable(2))
            {
                city.MarkGuide("ux/feature/read/jobs");
            }

            RefreshStatus();
            Open(Status);
        }

        public void OpenHelp()
        {
            selectedRoute = null;
            Open(Help);
            RefreshGuide();
            city.OpenedHelp();
        }

        void Open(GameObject panel)
        {
            preferences?.RefreshPreferences();
            shell.Push(panel);
            AudioRequested?.Invoke("Open");
            Layout();
        }

        void HubTab(int tab)
        {
            hubTab = tab;
            var collection = GetComponent<CollectionUI>();
            var cityUI = GetComponent<CityLifeHUD>();
            var nav = GetComponent<UINavigationService>();
            var actions = new List<(string, Action)>();
            if (tab == 0)
            {
                actions.Add(("상세 상태", () => OpenStatus()));
                actions.Add(("미션", () => nav.Mission()));
                actions.Add(
                    (
                        (FeatureAvailable(2) && !city.Seen("ux/feature/read/jobs") ? "NEW · " : "") + "직업 · 근무",
                        () => OpenStatus(2)
                    )
                );
                actions.Add(
                    (
                        "교육 · 학교",
                        () =>
                        {
                            OpenStatus(1);
                            Tutorial(
                                "school",
                                "공부 · 학교 교실 책상"
                            );
                        }
                    )
                );
                actions.Add(("도전과제", () => cityUI.OpenAchievements()));
                actions.Add(("지도 · 장소", () => cityUI.OpenMap()));
            }
            else if (tab == 1)
            {
                actions.Add(
                    (
                        game.items.Inventory.Entries.Any(e => e.count > 0 && e.isNew) ? "NEW · 인벤토리" : "인벤토리",
                        () =>
                        {
                            collection.OpenInventory();
                        }
                    )
                );
                actions.Add(("장착 장비", () => collection.OpenEquipment()));
                actions.Add(
                    (
                        game.items.Unlocked ? "가챠" : "가챠 · 성장 후 해금",
                        () =>
                        {
                            if (game.items.Unlocked)
                            {
                                collection.OpenGacha();
                                Tutorial(
                                    "gacha",
                                    "실제 확률과 50/100/300회 보장은 뽑기 전 확률정보에서 확인하세요. 중복 아이템은 분해할 수 있습니다."
                                );
                            }
                            else
                            {
                                hud.Notify(
                                    "아직 수집 기능이 해금되지 않았습니다. 현재 성장 단계의 미션을 진행해 보세요.",
                                    2
                                );
                            }
                        }
                    )
                );
                actions.Add(("수집 도감", () => collection.OpenCollection()));
            }
            else if (tab == 2)
            {
                actions.Add(("자산 · 부채", () => OpenStatus(3)));
                actions.Add(("경제 화면", () => GetComponent<FinancePresenter>().OpenEconomy()));
                actions.Add(
                    (
                        "주식 · 이용 장소",
                        () =>
                        {
                            if (GrowthAccess.Zone(game, "finance", out var reason))
                            {
                                city.Track("finance", out _);
                                Tutorial(
                                    "stocks",
                                    "주식 거래 · 도시 투자센터"
                                );
                                OpenHelp();
                            }
                            else
                            {
                                Explain(reason);
                            }
                        }
                    )
                );
                actions.Add(("집 관리", () => hud.OpenHousing()));
            }
            else
            {
                actions.Add(("도움말", OpenHelp));
                actions.Add(
                    (
                        "최근 알림",
                        () =>
                        {
                            RefreshHistory();
                            Open(History);
                        }
                    )
                );
                actions.Add(("편의 설정", () => Open(Preferences)));
                actions.Add(("환경 설정 · 약관", () => shell.OpenSettings()));
                actions.Add(("저장", () => game.Save()));
                actions.Add(("자동 활동 · 접속 기록", () => GetComponent<IdleActivityPresenter>().Open()));
            }

            for (int i = 0; i < hubTabs.Length; i++)
            {
                LifeRecordUI.StyleButton(hubTabs[i], i == tab);
            }

            hubText.text =
                tab == 0 ? "현재 할 수 있는 일을 골라 보세요. 추천과 도전과제는 진행을 막지 않습니다."
                : tab == 1 ? "획득 → 장착 → 수집. 중복 분해는 확인 후 실행합니다."
                : tab == 2 ? "현금과 루비는 구분됩니다. 투자는 해당 시설에서 이용합니다."
                : "진행 기록과 환경 설정입니다. 구매·노동·이사는 각 원래 시스템이 처리합니다.";
            for (int i = 0; i < hubActions.Length; i++)
            {
                var button = hubActions[i];
                button.gameObject.SetActive(i < actions.Count);
                button.onClick.RemoveAllListeners();
                if (i >= actions.Count)
                {
                    continue;
                }

                var action = actions[i];
                button.GetComponentInChildren<TMP_Text>().text = action.Item1;
                button.onClick.AddListener(() =>
                {
                    AudioRequested?.Invoke("Select");
                    action.Item2();
                });
            }

            shell.InvalidateFocus();
        }

        public bool FeatureAvailable(int index)
        {
            if (index == 0)
            {
                return game.items.Unlocked;
            }

            if (index == 2)
            {
                return game.growth.EmploymentUnlocked;
            }

            return GrowthAccess.Zone(
                game,
                index == 1 ? "school"
                    : index == 3 ? "finance"
                    : "realestate",
                out _
            );
        }

        void ObserveUnlocks()
        {
            bool seed = !city.Seen("ux/unlocks/initialized");
            city.MarkGuide("ux/unlocks/initialized");
            for (int i = 0; i < FeatureIds.Length; i++)
            {
                if (!FeatureAvailable(i) || city.Seen("ux/unlock/" + FeatureIds[i]))
                {
                    continue;
                }

                city.MarkGuide("ux/unlock/" + FeatureIds[i]);
                if (!seed)
                {
                    if (i == 1)
                    {
                        city.StartLesson(GuideLesson.School);
                        city.StartLesson(GuideLesson.Social);
                    }
                    else if (i == 2 || i == 3)
                    {
                        city.StartLesson(i == 2 ? GuideLesson.Job : GuideLesson.Stocks);
                    }

                    AnnounceUnlock(i);
                }
            }
        }

        void AnnounceUnlock(int index)
        {
            game.Notifications.Publish(
                "새 기능 해금 · " + FeatureNames[index] + "\n" + FeaturePlaces[index],
                NotificationKind.Info
            );
        }

#if UNITY_EDITOR
        public void PreviewJobUnlock()
        {
            if (Ready)
            {
                AnnounceUnlock(Array.IndexOf(FeatureIds, "jobs"));
            }
        }
#endif

        public void Tutorial(string id, string text)
        {
            string key = "ux/tutorial/" + id;
            if (city.GuideSkipped || city.Seen(key))
            {
                return;
            }

            city.MarkGuide(key);
            game.Notifications.Publish(text, NotificationKind.Info);
        }

        void RefreshGuide()
        {
            var active = game.missions.ActiveMission;
            mission.text =
                active == null
                    ? "미션 보기"
                    : active.title
                        + "  "
                        + game.missions.GetProgress(active).current.ToString("0")
                        + "/"
                        + game.missions.GetProgress(active).target.ToString("0");
            RefreshLearningCard();
        }

        void BuildVitalRecord()
        {
            vitalRecord = LifeRecordUI.Rect(Status.transform, "Health record", .40f, .215f, .96f, .752f);
            for (int i = 0; i < StatNames.Length; i++)
            {
                float x = i / 4 * .515f;
                float y = .78f - i % 4 * .20f;
                var cell = LifeRecordUI.Surface(vitalRecord, StatNames[i], RecordSurface.Inset, x, y, x + .485f, y + .175f);
                LifeRecordUI.Label(cell.transform, hud.needs.font, StatNames[i], 18, LifeRecordUI.Body,
                    .06f, .25f, .67f, .94f);
                vitalValues[i] = LifeRecordUI.Label(cell.transform, hud.needs.font, "", 20, LifeRecordUI.Body,
                    .63f, .25f, .95f, .94f);
                vitalValues[i].alignment = TextAlignmentOptions.MidlineRight;
                var track = LifeRecordUI.Rect(cell.transform, "Scale", .07f, .15f, .93f, .23f);
                var rail = track.gameObject.AddComponent<Image>();
                rail.raycastTarget = false;
                rail.color = LifeRecordUI.Track;
                var fill = LifeRecordUI.Rect(track, "Level", 0, 0, 1, 1).gameObject.AddComponent<Image>();
                fill.color = LifeRecordUI.Secondary;
                fill.raycastTarget = false;
                vitalLevels[i] = fill.rectTransform;
            }

            conditionNote = LifeRecordUI.Label(vitalRecord, hud.needs.font, "", 18, LifeRecordUI.Body,
                0, 0, 1, .15f);
        }

        static string StageName(LifeStage stage) => stage switch
        {
            LifeStage.Infancy => "유아기",
            LifeStage.Childhood => "아동기",
            LifeStage.Adolescence => "청소년기",
            LifeStage.YoungAdult => "청년기",
            LifeStage.MiddleAge => "중년기",
            _ => "노년기"
        };

        void RefreshStatus()
        {
            if (statusText == null)
            {
                return;
            }

            for (int i = 0; i < statusTabs.Length; i++)
            {
                LifeRecordUI.StyleButton(statusTabs[i], i == statusTab);
            }
            statusAction.text = statusTab switch
            {
                1 => "교육·생활 활동 보기",
                2 => "채용·근무 보기",
                3 => "가계 명세서 열기",
                4 => "장비 확인",
                _ => "생활 도움말"
            };
            residentAge.text = $"{game.growth.Age}세 · {StageName(game.growth.Stage)}";
            vitalRecord.gameObject.SetActive(statusTab == 0);
            statusText.GetComponentInParent<ScrollRect>(true).gameObject.SetActive(statusTab != 0);
            string text = "";
            if (statusTab == 0)
            {
                for (int i = 0; i < vitalValues.Length; i++)
                {
                    int value = game.stats.Get((CoreStat)i);
                    vitalValues[i].text = value.ToString();
                    vitalLevels[i].anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1);
                }

                conditionNote.text = $"피로 {game.world.State.fatigue:0}  ·  위생 {game.world.State.hygiene:0}";
            }
            else if (statusTab == 1)
            {
                text =
                    "나이 "
                    + game.growth.Age
                    + "세 · "
                    + StageName(game.growth.Stage)
                    + "\n\n현재 미션: "
                    + game.missions.ActiveMission?.title
                    + "\n\n교육: "
                    + (
                        game.phase23.catalog.education.FirstOrDefault(e =>
                            e.educationId == game.phase23.Progress.education.currentEducationId
                        )?.displayName ?? "미등록"
                    )
                    + "\n독서 "
                    + game.phase23.Progress.education.books
                    + " · 공부 "
                    + game.phase23.Progress.education.studies
                    + " · 과제 "
                    + game.phase23.Progress.education.homework
                    + "\n\n공부 · 학교 책상에서 상호작용";
            }
            else if (statusTab == 2)
            {
                text =
                    "현재 직업\n<size=130%><b>"
                    + (game.phase23.Jobs.Current?.DisplayName ?? "무직")
                    + "</b></size>\n예상 일급\n<size=145%><b>"
                    + game.phase23.Work.Wage.ToString("N0")
                    + " G</b></size>\n\n"
                    + (
                        game.growth.EmploymentUnlocked
                            ? "회사에서 채용 조건과 근무를 확인하세요."
                            : "성장 후 취업 가능"
                    )
                    + "\n직급 "
                    + game.phase23.Progress.employment.rank
                    + " · 경력 "
                    + game.phase23.Progress.employment.experience
                    + "\n근무 기록 "
                    + game.phase23.Progress.employment.work.Count
                    + "회\n\n자동 노동은 생활 → 자동 활동에서 설정합니다. 퇴사와 계약은 기존 직업 화면에서 확인합니다.";
            }
            else if (statusTab == 3)
            {
                text =
                    $"현금 {game.economy.Cash:N0} G\n루비 {game.economy.Ruby.Total:N0}\n총자산 {game.economy.TotalAssets:N0} G\n부채 {game.economy.Debt:N0} G\n순자산 {game.economy.NetWorth:N0} G\n\n현재 거주: {hud.home?.catalog?.Residence(game.economy.Capture().housing?.residenceId)?.title ?? "등록된 거주지 없음"}\n\n세금·대출·거래 내역  →  금융";
            }
            else
            {
                text =
                    string.Join(
                        "\n\n",
                        game.items.Equipment.Entries.Select(e =>
                            e.slot + " · " + game.items.catalog.Item(e.itemId)?.displayName
                        )
                    ) + "\n\n장비 교체와 효과 비교: 아이템 → 인벤토리";
            }

            statusText.text = text;
        }

        void RefreshHistory()
        {
            historyText.text = string.Join("\n\n", game.Notifications.History.Reverse().Take(40).Select(n => n.text));
            if (historyText.text.Length == 0)
            {
                historyText.text = "아직 기록이 없습니다.";
            }
        }

        void Invalidate() => dirty = true;

        void GameStateChanged(GameState state) => Invalidate();

        void ActionCompleted(int action) => Invalidate();

        void Restored()
        {
            guideFeedback = null;
            lastPresented = null;
            selectedRoute = null;
            Invalidate();
        }

        void NavigationChanged()
        {
            Invalidate();
            AudioRequested?.Invoke(shell.Depth == 0 ? "Close" : "Open");
            var collection = GetComponent<CollectionUI>();
            var nav = GetComponent<UINavigationService>();
            if (shell.Top && nav && shell.Top == nav.missionPanel)
            {
                city.Learn(GuideLesson.Mission);
            }

            if (shell.Top && shell.Top == Status)
            {
                city.Learn(GuideLesson.Status);
            }

            if (collection && shell.Top && shell.Top == collection.ProbabilityPanel)
            {
                city.Learn(GuideLesson.Gacha);
            }

            if (collection != null && shell.Top == collection.InventoryPanel && game.items.Inventory.Entries.Any(e => e.count > 0))
            {
                city.Learn(GuideLesson.Inventory);
                city.MarkGuide("ux/feature/read/items");
                Tutorial("inventory", "획득한 아이템을 선택하면 효과·수량·현재 장비 비교를 확인할 수 있습니다.");
            }

            if (collection != null && shell.Top == collection.GachaPanel)
            {
                if (city.Progress(GuideLesson.Gacha) == LessonState.NotStarted)
                {
                    var odds = collection.GachaPanel.transform.Find("Probabilities");
                    if (odds && odds.TryGetComponent<Button>(out var button))
                    {
                        button.Select();
                    }
                }

                city.StartLesson(GuideLesson.Gacha);
                Tutorial("gacha", "뽑기 전 비용과 확률정보를 확인하세요. 중복은 인벤토리에서 분해할 수 있습니다.");
            }
        }

        void Stats()
        {
            Invalidate();
        }

        void NewItem(ItemEvent item)
        {
            var definition = game.items.catalog.Item(item.itemId);
            if (definition != null)
            {
                game.Notifications.Publish(
                    "NEW · " + definition.displayName + "\n아이템 → 인벤토리",
                    NotificationKind.Item
                );
            }
        }

        void MissionCompleted(MissionData data)
        {
            game.Notifications.Publish(
                data.title + " 완료\n미션 → 보상 받기",
                NotificationKind.Mission
            );
        }

        void RewardClaimed(MissionData data)
        {
            int ruby = ReleaseConfig.Current?.Reward(RubySource.Mission, data.Id) ?? 0;
            game.Notifications.Publish(
                "미션 보상 수령 · "
                    + data.title
                    + "\n+"
                    + data.rewards.gold
                    + " G · +"
                    + data.rewards.experience
                    + " EXP"
                    + (ruby > 0 ? " · 루비 +" + ruby : ""),
                NotificationKind.Mission
            );
        }

        void Saved(bool success)
        {
            saveText.text = success ? "저장 완료" : "저장 실패 · 저장 공간을 확인하세요";
            saveUntil = Time.unscaledTime + (success ? 2 : 8);
            if (!success)
            {
                game.Notifications.Publish(
                    "진행을 저장하지 못했습니다. 저장 공간을 확인하고 다시 저장하세요.",
                    NotificationKind.Critical
                );
            }
        }

        void Refresh()
        {
            dirty = false;
            RefreshGuide();
            if (Status.activeSelf)
            {
                RefreshStatus();
            }

            if (History.activeSelf)
            {
                RefreshHistory();
            }

            if (Preferences.activeSelf)
            {
                preferences?.RefreshPreferences();
            }

            Layout();
            if (!game.BirthIntroductionPending && game.State == GameState.Playing)
            {
                ObserveUnlocks();
                var catalog = game.economy.housingCatalog;
                if (new ResidenceAccess(game).IsActiveResidence && catalog && game.economy.HasIncome
                    && city.Progress(GuideLesson.Furniture) == LessonState.NotStarted
                    && catalog.furniture.Any(f => f != null && game.economy.CanBuyFurniture(f.id, out _)))
                {
                    city.StartLesson(GuideLesson.Furniture);
                }
            }
        }

        void LateUpdate()
        {
            if (!Ready)
            {
                return;
            }

            if (dirty)
            {
                Refresh();
            }

            bool clear = shell.Depth == 0 && !hud.world.Busy && !game.BirthIntroductionPending;
            card.gameObject.SetActive(
                clear
                    && !city.Data.suggestionsHidden
                    && !(viewportSize.x / Mathf.Max(1, viewportSize.y) < 1.4f && hud.NoticeVisible)
            );
            PresentLesson();
            if (saveUntil > 0 && Time.unscaledTime > saveUntil)
            {
                saveText.text = "";
                saveUntil = 0;
            }

            if (
                width != Screen.width
                || height != Screen.height
                || viewportSize != ((RectTransform)shell.overlayRoot).rect.size
            )
            {
                Layout();
            }

            var target = hud.world.Nearest;
            if (target != observedTarget)
            {
                observedTarget = target;
                Invalidate();
                if (target != null && !GrowthAccess.Interaction(game, target, -2, out var reason))
                {
                    hud.Prompt(target.title + "\n" + reason);
                }
            }
        }

        void Layout()
        {
            width = Screen.width;
            height = Screen.height;
            var safe = (RectTransform)card.parent;
            float w = safe.rect.width;
            viewportSize = ((RectTransform)shell.overlayRoot).rect.size;
            bool compact = safe.rect.width / Mathf.Max(1, safe.rect.height) < 1.4f;
            card.anchoredPosition = new Vector2(w * .018f, compact ? -160 : -68);
            var mobile = hud.GetComponent<WorldMobileHUD>();
            saveText.rectTransform.anchorMin = mobile && mobile.IsMobile ? new Vector2(.46f, .80f) : new Vector2(.72f, .02f);
            saveText.rectTransform.anchorMax = mobile && mobile.IsMobile ? new Vector2(.97f, .84f) : new Vector2(.97f, .07f);
            float cardWidth = Mathf.Min(350, w * (compact ? .92f : .34f));
            mission.maxVisibleLines = 1;
            mission.overflowMode = TextOverflowModes.Ellipsis;
            float missionHeight = Mathf.Min(
                52,
                mission.GetPreferredValues(mission.text, cardWidth - 50, Mathf.Infinity).y
            );
            float guideHeight = guide.GetPreferredValues(guide.text, cardWidth - 24, Mathf.Infinity).y;
            card.sizeDelta = new Vector2(cardWidth, missionHeight + guideHeight + 36);
            mission.rectTransform.anchorMin = guide.rectTransform.anchorMin = new Vector2(0, 1);
            mission.rectTransform.anchorMax = guide.rectTransform.anchorMax = Vector2.one;
            mission.rectTransform.pivot = guide.rectTransform.pivot = new Vector2(.5f, 1);
            mission.rectTransform.anchoredPosition = new Vector2(-12, -12);
            mission.rectTransform.sizeDelta = new Vector2(-48, missionHeight);
            guide.rectTransform.anchoredPosition = new Vector2(0, -24 - missionHeight);
            guide.rectTransform.sizeDelta = new Vector2(-24, guideHeight);
            foreach (var panel in windows)
            {
                if (panel.TryGetComponent<RecordWindowBounds>(out _))
                {
                    continue;
                }

                var r = (RectTransform)panel.transform;
                r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
                r.pivot = new Vector2(.5f, .5f);
                r.sizeDelta = new Vector2(
                    Mathf.Min(820, ((RectTransform)r.parent).rect.width * .92f),
                    ((RectTransform)r.parent).rect.height * .88f
                );
                r.anchoredPosition = Vector2.zero;
            }

            GetComponent<CityLifeHUD>()?.UseIntegratedGuide();
        }

        void OnDestroy()
        {
            if (!Ready)
            {
                return;
            }

            game.stats.Changed -= Stats;
            game.StateChanged -= GameStateChanged;
            game.economy.Changed -= Invalidate;
            game.items.Changed -= Invalidate;
            game.items.NewItemDiscovered -= NewItem;
            InputSystem.onActionChange -= InputChanged;
            city.TutorialStepCompleted -= Learned;
            if (focusObject)
            {
                focusObject.GuideFocus(false);
            }
            game.missions.RewardClaimed -= RewardClaimed;
            game.missions.Completed -= MissionCompleted;
            game.missions.Changed -= Invalidate;
            game.world.Changed -= Invalidate;
            game.world.Restored -= Restored;
            game.growth.Changed -= Invalidate;
            game.ActionPerformed -= ActionCompleted;
            game.Notifications.Changed -= Invalidate;
            city.Changed -= Invalidate;
            shell.Changed -= NavigationChanged;
            game.saves.WriteCompleted -= Saved;
        }
    }
}
