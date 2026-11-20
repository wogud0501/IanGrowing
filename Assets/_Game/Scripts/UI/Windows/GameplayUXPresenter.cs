using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        TMP_Text settingsText;
        TMP_Text saveText;
        readonly List<GameObject> windows = new List<GameObject>();
        readonly Button[] hubActions = new Button[8];
        int statusTab;
        int hubTab;
        int width;
        int height;
        Vector2 viewportSize;
        bool dirty;
        int[] previousStats;
        float statUntil;
        float saveUntil;
        GameObject confirmation;
        TMP_Text confirmationText;
        Action confirmedAction;
        public GameObject Confirmation => confirmation;

        string observedTarget;
        readonly Button[] hubTabs = new Button[4];
        readonly Button[] statusTabs = new Button[5];
        TMP_Text residentAge;
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
            previousStats = Enumerable.Range(0, 8).Select(i => game.stats.Get((CoreStat)i)).ToArray();
            game.stats.Changed += Stats;
            game.StateChanged += GameStateChanged;
            game.economy.Changed += Invalidate;
            game.items.Changed += Invalidate;
            game.items.NewItemDiscovered += NewItem;
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
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("ian.audio.master", 1));
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
            LifeRecordUI.Skin(card.GetComponent<Image>(), RecordSurface.Paper);
            LifeRecordUI.Surface(card, "Mission tab", RecordSurface.Seal, 0, .18f, .015f, .84f);
            mission = ui.Text(card, "Current mission", 17, new Vector2(.04f, .71f), new Vector2(.96f, .96f));
            guide = ui.Text(card, "Optional recommendation", 17, new Vector2(.04f, .12f), new Vector2(.96f, .68f));
            mission.color = LifeRecordUI.Ink;
            guide.color = LifeRecordUI.Jade;
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
            LifeRecordUI.Label(stub.transform, hud.needs.font, "이안", 32, LifeRecordUI.Ink, .09f, .16f, .9f, .36f);
            residentAge = LifeRecordUI.Label(stub.transform, hud.needs.font, "", 20, LifeRecordUI.Jade, .09f, .025f, .96f, .185f);
            var portrait = LifeRecordUI.Rect(stub.transform, "Official Ian", .15f, .37f, .85f, .96f);
            var image = portrait.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.sprite = hud.world.Player.identity?.baseSprite;
            image.enabled = image.sprite != null;
            statusText = ui.ScrollText(Status.transform, new Vector2(.40f, .215f), new Vector2(.96f, .751f));
            ui.Button(
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
            Help = Window("도움말 · 선택하는 생활");
            helpText = ui.ScrollText(Help.transform, new Vector2(.06f, .2f), new Vector2(.94f, .86f));
            ui.Button(
                Help.transform,
                "추천 표시 / 숨기기",
                new Vector2(.05f, .06f),
                new Vector2(.49f, .16f),
                () => city.ShowSuggestions(city.Data.suggestionsHidden)
            );
            ui.Button(
                Help.transform,
                "처음 안내 다시 보기",
                new Vector2(.51f, .06f),
                new Vector2(.95f, .16f),
                () =>
                {
                    city.ResetUXGuides();
                    Tutorial("help", "안내를 다시 볼 수 있습니다. 행동 권한과 미션 진행은 유지됩니다.");
                }
            );
            History = Window("최근 기록");
            historyText = ui.ScrollText(History.transform, new Vector2(.06f, .07f), new Vector2(.94f, .86f));
            Preferences = Window("편의 설정");
            settingsText = ui.ScrollText(Preferences.transform, new Vector2(.06f, .57f), new Vector2(.94f, .86f));
            ui.Button(
                Preferences.transform,
                "모션 줄이기",
                new Vector2(.06f, .43f),
                new Vector2(.48f, .53f),
                () =>
                {
                    shell.ToggleMotion();
                    RefreshPreferences();
                }
            );
            ui.Button(
                Preferences.transform,
                "글자 크기",
                new Vector2(.52f, .43f),
                new Vector2(.94f, .53f),
                () =>
                {
                    shell.ToggleText();
                    Layout();
                }
            );
            ui.Button(
                Preferences.transform,
                "알림 표시 시간",
                new Vector2(.06f, .29f),
                new Vector2(.48f, .39f),
                () =>
                {
                    PlayerPrefs.SetFloat(
                        "ian.ui.noticeSeconds",
                        PlayerPrefs.GetFloat("ian.ui.noticeSeconds", 4) >= 8
                            ? 2
                            : PlayerPrefs.GetFloat("ian.ui.noticeSeconds", 4) + 2
                    );
                    PlayerPrefs.Save();
                    RefreshPreferences();
                }
            );
            ui.Button(
                Preferences.transform,
                "환경 설정 · 약관",
                new Vector2(.52f, .29f),
                new Vector2(.94f, .39f),
                () => shell.OpenSettings()
            );
            ui.Button(Preferences.transform, "도움말", new Vector2(.06f, .15f), new Vector2(.48f, .25f), OpenHelp);
            ui.Button(
                Preferences.transform,
                "추천 표시 / 숨기기",
                new Vector2(.52f, .15f),
                new Vector2(.94f, .25f),
                () => city.ShowSuggestions(city.Data.suggestionsHidden)
            );
            var settingsRect = (RectTransform)shell.settingsPanel.transform;
            ui.Button(
                settingsRect,
                "도움말 · 편의",
                new Vector2(.04f, .02f),
                new Vector2(.45f, .1f),
                () => Open(Preferences)
            );
            BuildSettingsCategories();
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
                hubText.text = reason + "\n이용 조건과 장소는 도움말에서 확인할 수 있습니다.";
            }
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
            RefreshGuide();
            Open(Help);
            Tutorial("help", "추천은 선택 사항입니다. 현재 미션과 도움말에서 가능한 행동과 위치를 확인하세요.");
        }

        void Open(GameObject panel)
        {
            RefreshPreferences();
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
                actions.Add(("미션 · 진행 목표", () => nav.Mission()));
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
                                "학교의 책상에서 공부합니다. 교육 조건과 과제는 학교 화면에서 확인하세요."
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
                        "인벤토리",
                        () =>
                        {
                            collection.OpenInventory();
                            Tutorial("inventory", "아이템을 선택하면 효과·수량·현재 장비 비교를 확인할 수 있습니다.");
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
                                    "도시 투자센터에서 종목·보유 자산·뉴스·거래 내역을 확인할 수 있습니다."
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
                    game.Notifications.Publish(
                        "새 기능 해금 · " + FeatureNames[i] + "\n" + FeaturePlaces[i],
                        NotificationKind.Info
                    );
                }
            }
        }

        public void Tutorial(string id, string text)
        {
            string key = "ux/tutorial/" + id;
            if (city.Seen(key))
            {
                return;
            }

            city.MarkGuide(key);
            game.Notifications.Publish(text, NotificationKind.Info);
        }

        void RefreshGuide()
        {
            var suggestion = city.Suggest();
            var active = game.missions.ActiveMission;
            mission.text =
                active == null
                    ? "진행 목표 · 미션 창"
                    : "진행 목표 · "
                        + active.title
                        + "  "
                        + game.missions.GetProgress(active).current.ToString("0")
                        + "/"
                        + game.missions.GetProgress(active).target.ToString("0");
            guide.text = "추천   " + suggestion.title;
            helpText.text =
                "지금 해볼 만한 것\n"
                + suggestion.title
                + "\n"
                + suggestion.detail
                + "\n\n이동: WASD · 방향키 · 터치 조이스틱\n상호작용: 가까이 가서 E · 터치 행동 버튼\n뒤로: ESC · 패드 취소 · 화면 뒤로\n큰 메뉴에서는 이동과 실시간 생활 시계가 멈춥니다.\n\n미션: 생활 → 미션\n아이템·장비·가챠·도감: 아이템 탭\n교육: 학교의 책상\n직업: 회사의 채용·근무\n주식: 도시 투자센터\n독립·가구: 금융 → 집 관리\n\n0세는 보호자 돌봄, 1세부터 보행, 7세부터 독립 외출입니다. 기존 해금 조건이 우선합니다.\n\n인벤토리: 선택한 아이템의 상세 정보에서 장착·사용·분해를 선택합니다. 장착 중이거나 보호된 마지막 아이템은 분해할 수 없습니다.\n가챠: 사용 재화와 확률정보를 먼저 확인하세요.\n\n이미 본 안내: "
                + string.Join(
                    " · ",
                    city.Data.guideSeen.Where(s => s.StartsWith("ux/tutorial/")).Select(s => s.Substring(12))
                );
        }

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
            residentAge.text = $"{game.growth.Age}세 · {game.clock.Day}일째";
            string text = "";
            if (statusTab == 0)
            {
                text =
                    string.Join(
                        "\n",
                        Enumerable.Range(0, 8).Select(i => StatNames[i] + "    <b>" + game.stats.Get((CoreStat)i) + "</b> / 100")
                    )
                    + "\n\n피로 "
                    + game.world.State.fatigue.ToString("0")
                    + " · 위생 "
                    + game.world.State.hygiene.ToString("0");
            }
            else if (statusTab == 1)
            {
                text =
                    "나이 "
                    + game.growth.Age
                    + "세 · "
                    + game.growth.Stage
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
                    + "\n\n학습은 학교 책상에서 E로 시작합니다. 등록·시험·과제는 기존 학교 화면에서 확인하세요.";
            }
            else if (statusTab == 2)
            {
                text =
                    "현재 직업: "
                    + (game.phase23.Jobs.Current?.DisplayName ?? "없음")
                    + "\n\n"
                    + (
                        game.growth.EmploymentUnlocked
                            ? "회사에서 채용 조건과 근무를 확인하세요."
                            : "아직 직업 활동이 해금되지 않았습니다. 성장 조건을 확인하세요."
                    )
                    + "\n일급 "
                    + (game.phase23.Jobs.Current?.BaseDailyWage ?? 0).ToString("N0")
                    + " G · 직급 "
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
                    $"현금 {game.economy.Cash:N0} G\n루비 {game.economy.Ruby.Total:N0}\n총자산 {game.economy.TotalAssets:N0} G\n부채 {game.economy.Debt:N0} G\n순자산 {game.economy.NetWorth:N0} G\n\n현재 거주: {game.economy.Capture().housing?.residenceId}\n\n세금·대출·거래 내역은 금융 → 경제 화면에서 확인하세요.\n부채는 창을 닫아도 남습니다.";
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
            previousStats = null;
            Invalidate();
        }

        void NavigationChanged()
        {
            Invalidate();
            AudioRequested?.Invoke(shell.Depth == 0 ? "Close" : "Open");
            var collection = GetComponent<CollectionUI>();
            if (collection != null && shell.Top == collection.InventoryPanel)
            {
                city.MarkGuide("ux/feature/read/items");
                Tutorial("inventory", "획득한 아이템을 선택하면 효과·수량·현재 장비 비교를 확인할 수 있습니다.");
            }

            if (collection != null && shell.Top == collection.GachaPanel)
            {
                Tutorial("gacha", "뽑기 전 비용과 확률정보를 확인하세요. 중복은 인벤토리에서 분해할 수 있습니다.");
            }
        }

        void Stats()
        {
            var current = Enumerable.Range(0, 8).Select(i => game.stats.Get((CoreStat)i)).ToArray();
            if (previousStats != null && (game.State == GameState.Playing || game.State == GameState.Resolving))
            {
                var changes = Enumerable
                    .Range(0, 8)
                    .Where(i => Math.Abs(current[i] - previousStats[i]) >= 1)
                    .Select(i => StatNames[i] + " " + (current[i] - previousStats[i]).ToString("+0;-0;0"))
                    .ToArray();
                if (changes.Length > 0)
                {
                    game.Notifications.Publish(string.Join(" · ", changes.Take(4)), NotificationKind.Info);
                    hud.needs.color = Enumerable.Range(0, 8).Any(i => current[i] < previousStats[i])
                        ? new Color32(164, 63, 58, 255)
                        : new Color32(60, 112, 85, 255);
                    statUntil = Time.unscaledTime + 2;
                }
            }

            previousStats = current;
            Invalidate();
        }

        void NewItem(ItemEvent item)
        {
            var definition = game.items.catalog.Item(item.itemId);
            if (definition != null)
            {
                game.Notifications.Publish(
                    "NEW · " + definition.displayName + "\n아이템 → 인벤토리에서 확인할 수 있습니다.",
                    NotificationKind.Item
                );
            }
        }

        void MissionCompleted(MissionData data)
        {
            game.Notifications.Publish(
                "MISSION COMPLETE\n" + data.title + "\n" + data.completionMessage + "\n보상은 미션 창에서 수령하세요.",
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
                RefreshPreferences();
            }

            Layout();
            if (!game.BirthIntroductionPending && game.State == GameState.Playing)
            {
                ObserveUnlocks();
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
            if (statUntil > 0 && Time.unscaledTime > statUntil)
            {
                statUntil = 0;
                hud.needs.color = PixelUXWidgets.Ink;
            }

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
            string key =
                target == null ? "" : target.id + "/" + game.growth.Age + "/" + game.clock.Day + "/" + game.world.Hour;
            if (key != observedTarget)
            {
                observedTarget = key;
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
            float cardWidth = Mathf.Min(350, w * (compact ? .92f : .34f));
            mission.maxVisibleLines = 2;
            mission.overflowMode = TextOverflowModes.Ellipsis;
            float missionHeight = Mathf.Min(
                52,
                mission.GetPreferredValues(mission.text, cardWidth - 24, Mathf.Infinity).y
            );
            float guideHeight = guide.GetPreferredValues(guide.text, cardWidth - 24, Mathf.Infinity).y;
            card.sizeDelta = new Vector2(cardWidth, missionHeight + guideHeight + 36);
            mission.rectTransform.anchorMin = guide.rectTransform.anchorMin = new Vector2(0, 1);
            mission.rectTransform.anchorMax = guide.rectTransform.anchorMax = Vector2.one;
            mission.rectTransform.pivot = guide.rectTransform.pivot = new Vector2(.5f, 1);
            mission.rectTransform.anchoredPosition = new Vector2(0, -12);
            mission.rectTransform.sizeDelta = new Vector2(-24, missionHeight);
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
