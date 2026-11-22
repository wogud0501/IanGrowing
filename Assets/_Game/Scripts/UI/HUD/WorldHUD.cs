using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace IanGame
{
    public sealed class WorldHUD : MonoBehaviour
    {
        public LifeWorldController world;
        public UIShell shell;
        public PolishedNavigation navigation;
        public HomeManagementPresenter home;
        public TMP_Text cash;
        public TMP_Text needs;
        public TMP_Text prompt;
        public TMP_Text feedback;
        public TMP_Text progress;
        public TMP_Text contextTitle;
        public TMP_Text inventory;
        public GameObject promptPanel;
        public GameObject menuPanel;
        public GameObject contextPanel;
        public RectTransform choices;
        public Button menuButton;
        public Button contextClose;
        public Button[] menus;
        public Image fade;
        public bool NoticeVisible =>
            feedbackCard != null && feedbackCard.gameObject.activeSelf && Time.unscaledTime < until;

        public Button choicePrefab;
        readonly List<Button> buttons = new List<Button>();
        WorldInteractable source;
        float until;
        string recent = "";
        GameManager game;
        bool dirty;
        SurvivalMeters survival;

        public void SetCompactVitals(bool compact)
        {
            survival?.SetCompact(compact);
        }

        public void FocusVital(int index)
        {
            survival?.Focus(index);
        }
        CanvasGroup[] worldReadouts;
        Image contextStamp;
        TMP_Text contextCaption;
        Image contextObject;
        string purchaseResult;
        RectTransform feedbackCard;
        RectTransform activityCard;
        string activityLabel;
        int activityPercent;
        Vector2 activityViewport;
        bool activityTouch;
        bool activityGamepad;
        float feedbackWidth;
        CanvasGroup noticeGroup;
        RectTransform noticeRule;
        Coroutine noticeMotion;

        void Start()
        {
            if (GameContext.Instance == null)
            {
                return;
            }

            game = GameContext.Instance.game;
            menuButton.onClick.AddListener(() =>
            {
                if (world.CanControl)
                {
                    OpenMenu();
                }
            });
            contextClose.onClick.AddListener(CloseContext);
            for (int i = 0; i < menus.Length; i++)
            {
                int n = i;
                menus[i].onClick.AddListener(() => navigation.Open(n));
            }

            game.economy.TransactionCompleted += Money;
            game.world.Changed += Summary;
            game.economy.Changed += Invalidate;
            game.stats.Changed += Invalidate;
            game.world.Restored += Invalidate;
            game.Logged += Log;
            shell.Changed += ShellChanged;
            game.growth.AgeChanged += WalkingAgeChanged;
            game.StateChanged += WalkingStateChanged;
            var bar = GetComponentInChildren<CompactWorldStatusBar>(true);
            if (bar != null)
            {
                LifeRecordUI.Skin(bar.GetComponent<Image>(), RecordSurface.Header);
                bar.place.color = LifeRecordUI.Heading;
                bar.time.color = LifeRecordUI.Secondary;
            }

            LifeRecordUI.Skin(cash.transform.parent.GetComponent<Image>(), RecordSurface.Window);
            cash.color = LifeRecordUI.Heading;
            LifeRecordUI.StyleButton(menuButton);
            LifeRecordUI.Skin(promptPanel.GetComponent<Image>(), RecordSurface.Context);
            prompt.color = LifeRecordUI.Heading;
            survival = new SurvivalMeters(needs, this);
            worldReadouts = new[] { bar ? bar.transform : null, cash.transform.parent, needs.transform.parent }
                .Where(readout => readout != null)
                .Select(readout => readout.TryGetComponent<CanvasGroup>(out var group)
                    ? group : readout.gameObject.AddComponent<CanvasGroup>())
                .ToArray();
            ShellChanged();
            Refresh();
            MaybeWalkingNotice();
        }

        void WalkingAgeChanged(int age, LifeStage stage) => MaybeWalkingNotice();

        void WalkingStateChanged(GameState state) => MaybeWalkingNotice();

        void MaybeWalkingNotice()
        {
            if (
                game == null
                || game.BirthIntroductionPending
                || game.State == GameState.Starting
                || !game.growth.TryMarkWalkingIntroduction()
            )
            {
                return;
            }

            if (!GetComponent<GameplayUXPresenter>() && !game.world.City.GuideSkipped)
            {
                Notify("첫 걸음! 집 안을 걸어 보세요.", 2);
            }
            if (game.State == GameState.Playing)
            {
                game.SaveAutomaticCheckpoint(out _);
            }
        }

        void ShellChanged()
        {
            if (feedbackCard && shell.Depth > 0)
            {
                feedbackCard.gameObject.SetActive(false);
            }

            if (worldReadouts != null)
            {
                foreach (var readout in worldReadouts)
                {
                    readout.alpha = shell.Depth == 0 ? 1 : 0;
                    readout.blocksRaycasts = shell.Depth == 0;
                }
            }

            if (!world.Busy && !game.BirthIntroductionPending)
            {
                world.Player.Pose(shell.Depth > 0 && GrowthAccess.Independent(game) ? "Use Phone" : "Idle");
            }
        }

        void Invalidate()
        {
            dirty = true;
        }

        void TimeChanged(double seconds)
        {
            dirty = true;
        }

        void LateUpdate()
        {
            if (dirty && game != null)
            {
                dirty = false;
                Refresh();
            }

            if (
                feedbackCard != null
                && feedbackCard.gameObject.activeSelf
                && Mathf.Abs(feedbackCard.rect.width - feedbackWidth) > 1
            )
            {
                SizeFeedback();
            }
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.growth.AgeChanged -= WalkingAgeChanged;
                game.StateChanged -= WalkingStateChanged;
                game.economy.TransactionCompleted -= Money;
                game.world.Changed -= Summary;
                game.economy.Changed -= Invalidate;
                game.stats.Changed -= Invalidate;
                game.world.Restored -= Invalidate;
                game.Logged -= Log;
                shell.Changed -= ShellChanged;
            }
        }

        void Update()
        {
            if (game == null)
            {
                return;
            }

            menuButton.interactable =
                !world.Busy && !game.BirthIntroductionPending && !GameContext.Instance.transitions.IsTransitioning;
            if (world.CanControl)
            {
                if (world.Player.MenuPressed)
                {
                    OpenMenu();
                }
                else if (world.Player.InventoryPressed)
                {
                    var nav = GetComponent<UINavigationService>();
                    if (nav != null)
                    {
                        nav.Inventory();
                    }
                }
            }

            ShowNextNotice();
            if (feedback != null && Time.unscaledTime > until && !string.IsNullOrEmpty(feedback.text))
            {
                feedback.text = "";
                if (feedbackCard != null)
                {
                    feedbackCard.gameObject.SetActive(false);
                }
            }
        }

        void UpdateContextCaption()
        {
            if (!contextCaption || !source)
            {
                return;
            }

            contextCaption.text = source.kind is WorldInteractionKind.FoodShop or WorldInteractionKind.FurnitureShop
                ? $"잔액  {game.economy.Cash:N0} G{(string.IsNullOrEmpty(purchaseResult) ? "" : "\n" + purchaseResult)}"
                : world.CurrentZone.title;
        }

        void Refresh()
        {
            UpdateContextCaption();
            cash.text = $"{game.economy.Cash:N0} G";
            survival?.Set(0, game.stats.Hunger);
            survival?.Set(1, game.stats.Thirst);
            survival?.Set(2, game.stats.Health);
            survival?.Set(3, game.stats.Mental);
            needs.text =
                $"허기 {game.stats.Hunger} · 수분 {game.stats.Thirst} · 체력 {game.stats.Health} · 멘탈 {game.stats.Mental}";
            if (inventory != null)
            {
                inventory.text =
                    $"식료품 · 쌀 {game.world.State.rice} / 빵 {game.world.State.bread}\n준비한 음식 · {(game.world.State.prepared == "" ? "없음" : game.world.State.prepared == "rice" ? "따뜻한 밥" : "빵")}\n위생 {game.world.State.hygiene:0} /100 · 피로 {game.world.State.fatigue:0} /100\n{game.world.State.lastSummary}";
            }
        }

        internal bool HasHardwareInput { get; set; }
        internal bool TouchHints => !HasHardwareInput && GetComponent<WorldMobileHUD>()?.IsMobile == true;
        internal bool UsingGamepad { get; set; }

        public string InteractionInput
        {
            get
            {
                if (TouchHints)
                {
                    return "사용";
                }

                return UsingGamepad ? "패드 아래쪽 버튼" : "E";
            }
        }

        public void Prompt(string text)
        {
            promptPanel.SetActive(!string.IsNullOrEmpty(text));
            prompt.text = text ?? "";
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var rect = (RectTransform)promptPanel.transform;
            float available = ((RectTransform)rect.parent).rect.width * .7f;
            float width = Mathf.Min(available, Mathf.Max(220, prompt.GetPreferredValues(text).x + 40));
            var mobile = GetComponent<WorldMobileHUD>();
            rect.anchorMin = rect.anchorMax = new Vector2(
                .5f,
                mobile != null && mobile.IsMobile
                    ? (mobile.Profile == WorldScreenProfile.PhonePortrait ? .18f : .27f)
                    : .09f
            );
            rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(
                width,
                Mathf.Max(48, prompt.GetPreferredValues(text, width - 32, Mathf.Infinity).y + 16)
            );
        }

        void EnsureFeedbackCard()
        {
            if (feedbackCard != null)
            {
                return;
            }

            var r = feedback.rectTransform;
            feedbackCard = new GameObject(
                "World notification",
                typeof(RectTransform),
                typeof(Image)
            ).GetComponent<RectTransform>();
            feedbackCard.SetParent(r.parent, false);
            feedbackCard.SetSiblingIndex(r.GetSiblingIndex());
            feedbackCard.anchorMin = r.anchorMin;
            feedbackCard.anchorMax = r.anchorMax;
            feedbackCard.pivot = r.pivot;
            feedbackCard.anchoredPosition = r.anchoredPosition;
            feedbackCard.sizeDelta = r.sizeDelta;
            var image = feedbackCard.GetComponent<Image>();
            LifeRecordUI.Skin(image, RecordSurface.Window);
            image.raycastTarget = false;
            r.SetParent(feedbackCard, false);
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(16, 8);
            r.offsetMax = new Vector2(-16, -8);
            feedback.color = LifeRecordUI.Body;
            feedback.raycastTarget = false;
            noticeGroup = feedbackCard.gameObject.AddComponent<CanvasGroup>();
            noticeGroup.blocksRaycasts = false;
            noticeRule = LifeRecordUI.Rect(feedbackCard, "Completion line", .025f, .06f, .975f, .06f);
            noticeRule.offsetMax = new Vector2(0, 2);
            var ruleImage = noticeRule.gameObject.AddComponent<Image>();
            ruleImage.color = LifeRecordUI.Heading;
            ruleImage.raycastTarget = false;
            noticeRule.gameObject.SetActive(false);
        }

        public void ReflowNotification()
        {
            if (feedbackCard == null)
            {
                return;
            }

            var mobile = GetComponent<WorldMobileHUD>();
            bool touch = mobile != null && mobile.IsMobile;
            var parentRect = (RectTransform)feedbackCard.parent;
            bool narrow = parentRect.rect.width / Mathf.Max(1, parentRect.rect.height) < 1.4f;
            feedbackCard.anchorMin = feedbackCard.anchorMax = new Vector2(narrow ? .5f : .68f, touch ? .72f : .89f);
            feedbackCard.pivot = new Vector2(.5f, 1);
            feedbackCard.anchoredPosition = Vector2.zero;
            float canvasWidth = ((RectTransform)feedbackCard.parent).rect.width;
            feedbackCard.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                Mathf.Min(touch ? canvasWidth * .9f : canvasWidth * .5f, 560)
            );
            SizeFeedback();
        }

        void SizeFeedback()
        {
            feedbackWidth = feedbackCard.rect.width;
            float height =
                feedback.GetPreferredValues(feedback.text, Mathf.Max(1, feedbackWidth - 32), Mathf.Infinity).y + 16;
            feedbackCard.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(64, height));
        }

        public void Notify(string text, int priority = 0)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (feedback != null)
                {
                    feedback.text = "";
                }

                if (feedbackCard != null)
                {
                    feedbackCard.gameObject.SetActive(false);
                }

                until = 0;
                return;
            }

            game ??= GameContext.Instance?.game;
            if (game == null)
            {
                return;
            }

            var kind =
                text != null && text.StartsWith("도전과제") ? NotificationKind.Achievement
                : text != null && text.Contains("MISSION COMPLETE") ? NotificationKind.Mission
                : priority >= 2 ? NotificationKind.Warning
                : priority == 1 ? NotificationKind.Economy
                : NotificationKind.Info;
            if (priority >= 2 && shell.Depth > 0)
            {
                var ux = GetComponent<GameplayUXPresenter>();
                if (ux != null && shell.Top == ux.Hub)
                {
                    ux.Explain(text);
                    return;
                }

                if (shell.Top == contextPanel && source != null)
                {
                    contextTitle.text = source.title + "\n" + text;
                    return;
                }
            }

            game.Notifications.Publish(text, kind);
        }

        public void OpenMenu()
        {
            shell.OpenPause();
        }

        System.Collections.IEnumerator AnimateNotice()
        {
            float start = Time.unscaledTime;
            var text = feedback.rectTransform;
            var rest = text.anchoredPosition;
            while (feedbackCard.gameObject.activeSelf && Time.unscaledTime < until)
            {
                float enter = Mathf.Clamp01((Time.unscaledTime - start) / .14f);
                if (noticeRule.gameObject.activeSelf && noticeRule.anchorMax.x < .975f)
                {
                    noticeRule.anchorMax = new Vector2(Mathf.Lerp(.025f, .975f, enter), .06f);
                }

                noticeGroup.alpha = Mathf.Min(enter, Mathf.Clamp01((until - Time.unscaledTime) / .16f));
                text.anchoredPosition = rest + Vector2.right * Mathf.Round((1 - enter) * 12);
                yield return null;
            }
            text.anchoredPosition = rest;
            noticeMotion = null;
        }

        void ShowNextNotice()
        {
            if (Time.unscaledTime < until || shell.Depth > 0 || world.Busy || game.BirthIntroductionPending)
            {
                return;
            }

            var notice = game.Notifications.Take();
            if (notice == null)
            {
                return;
            }

            EnsureFeedbackCard();
            var surface = notice.kind switch
            {
                NotificationKind.Mission or NotificationKind.Achievement => RecordSurface.Reward,
                NotificationKind.Critical or NotificationKind.Warning => RecordSurface.Warning,
                _ => RecordSurface.Window
            };
            LifeRecordUI.Skin(feedbackCard.GetComponent<Image>(), surface);
            feedback.color = surface == RecordSurface.Warning ? LifeRecordUI.Heading : LifeRecordUI.Body;
            bool milestone = notice.kind == NotificationKind.Mission || notice.kind == NotificationKind.Achievement;
            noticeRule.gameObject.SetActive(milestone);
            noticeRule.anchorMax = new Vector2(UIShell.ReducedMotion ? .975f : .025f, .06f);
            feedback.text = notice.text;
            feedbackCard.gameObject.SetActive(true);
            ReflowNotification();
            float duration = Mathf.Clamp(PlayerPrefs.GetFloat("ian.ui.noticeSeconds", 4), 2, 8);
            until = Time.unscaledTime + (notice.kind == NotificationKind.Mission ? duration * .35f : duration);
            if (noticeMotion != null)
            {
                StopCoroutine(noticeMotion);
            }
            noticeGroup.alpha = 1;
            if (!UIShell.ReducedMotion)
            {
                noticeMotion = StartCoroutine(AnimateNotice());
            }
            GetComponent<PresentationFeedback>()?.Cue(notice.kind.ToString());
        }

        void Money(int before, int after, string reason)
        {
            long delta = (long)after - before;
            if (delta == 0)
            {
                return;
            }

            world.Player.React(delta > 0 ? ActorMotion.Happy : ActorMotion.Thinking);
            string direction = delta > 0 ? "▲" : "▼";
            Notify($"{reason}  {direction} {delta:+#,0;-#,0;0}G", 1);
        }

        void Log(string text)
        {
            if (text == recent)
            {
                return;
            }

            recent = text;
            if (text.Contains("실패") || text.Contains("부족") || text.Contains("채용"))
            {
                Notify(
                    text.StartsWith("저장 실패")
                        ? "진행을 저장하지 못했습니다. 저장 공간을 확인하고 다시 시도하세요."
                        : text,
                    2
                );
            }
        }

        void Summary()
        {
            if (!string.IsNullOrEmpty(game.world.State.lastSummary))
            {
                Notify(game.world.State.lastSummary);
            }

            Refresh();
        }

        public void Dim(float alpha)
        {
            fade.color = new Color(0, 0, 0, Mathf.Clamp01(alpha));
            fade.raycastTarget = alpha > .01f;
        }

        public void Activity(string text, float amount)
        {
            bool touch = TouchHints;
            int percent = Mathf.Clamp(Mathf.RoundToInt(amount * 100), 0, 100);
            var viewport = new Vector2(Screen.width, Screen.height);
            if (
                activityCard
                && activityLabel == text
                && activityPercent == percent
                && activityTouch == touch
                && activityGamepad == UsingGamepad
                && activityViewport == viewport
            )
            {
                return;
            }

            activityLabel = text;
            activityPercent = percent;
            activityTouch = touch;
            activityGamepad = UsingGamepad;
            activityViewport = viewport;
            var cancelHint = TouchHints ? "" : UsingGamepad ? "   [패드 오른쪽] 취소" : "   [Esc] 취소";
            progress.text = string.IsNullOrEmpty(text) ? "" : $"{text} · {percent}%{cancelHint}";
            if (activityCard == null)
            {
                var r = progress.rectTransform;
                activityCard = new GameObject(
                    "World action progress",
                    typeof(RectTransform),
                    typeof(Image)
                ).GetComponent<RectTransform>();
                activityCard.SetParent(r.parent, false);
                activityCard.SetSiblingIndex(r.GetSiblingIndex());
                var image = activityCard.GetComponent<Image>();
                image.sprite = promptPanel.GetComponent<Image>().sprite;
                image.type = Image.Type.Sliced;
                image.raycastTarget = false;
                r.SetParent(activityCard, false);
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(16, 8);
                r.offsetMax = new Vector2(-16, -8);
                progress.color = LifeRecordUI.Body;
                progress.raycastTarget = false;
            }

            activityCard.gameObject.SetActive(!string.IsNullOrEmpty(text));
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            activityCard.anchorMin = activityCard.anchorMax = new Vector2(.5f, touch ? .27f : .16f);
            activityCard.pivot = new Vector2(.5f, 0);
            activityCard.anchoredPosition = Vector2.zero;
            float width = Mathf.Min(
                ((RectTransform)activityCard.parent).rect.width * (touch ? .76f : .65f),
                Mathf.Max(240, progress.GetPreferredValues(progress.text).x + 40)
            );
            activityCard.sizeDelta = new Vector2(
                width,
                Mathf.Max(48, progress.GetPreferredValues(progress.text, width - 32, Mathf.Infinity).y + 16)
            );
        }

        public void OpenHousing()
        {
            home.Tab(1);
            shell.Push(home.panel);
        }

        public void CloseContext()
        {
            source = null;
            if (shell.Top == contextPanel)
            {
                shell.Close(contextPanel);
            }
        }

        void Choice(string label, Action command)
        {
            var b = Instantiate(choicePrefab, choices);
            b.gameObject.SetActive(true);
            b.GetComponentInChildren<TMP_Text>().text = label;
            b.onClick.RemoveAllListeners();
            LifeRecordUI.StyleButton(b);
            b.onClick.AddListener(() =>
            {
                if (source == null || !world.IsInReach(source) || world.Busy)
                {
                    Notify("대상 가까이에서 다시 시도하세요.");
                    CloseContext();
                    return;
                }

                command();
                Refresh();
            });
            buttons.Add(b);
        }

        public void OpenContext(WorldInteractable target)
        {
            if (target.kind == WorldInteractionKind.NPC && target.npcId == "shop-clerk")
            {
                target = Array.Find(world.objects, o => o.id == "counter") ?? target;
            }

            source = target;
            purchaseResult = null;
            var choicesFrame = choices.GetComponentInParent<ScrollRect>(true);
            if (choicesFrame && contextStamp == null)
            {
                var panel = contextTitle.transform.parent;
                contextStamp = LifeRecordUI.Surface(panel, "Location card", RecordSurface.Inset, .034f, .075f, .325f, .771f);
                var art = LifeRecordUI.Rect(contextStamp.transform, "Interaction object", .12f, .36f, .88f, .90f);
                contextObject = art.gameObject.AddComponent<Image>();
                contextObject.preserveAspect = true;
                contextObject.raycastTarget = false;
                contextCaption = LifeRecordUI.Label(contextStamp.transform, contextTitle.font, "", 21, LifeRecordUI.Body,
                    .09f, .04f, .95f, .32f);
                LifeRecordUI.Place((RectTransform)choicesFrame.transform, .35f, .066f, .97f, .776f);
            }
            if (contextObject)
            {
                var renderer = target.visual ? target.visual : target.GetComponentInChildren<SpriteRenderer>();
                contextObject.sprite = renderer ? renderer.sprite : null;
                if (target.kind == WorldInteractionKind.NPC)
                {
                    var actor = target.GetComponentInChildren<WorldActorAnimation>(true);
                    var portrait = actor?.library?.Find(ActorMotion.Idle, ActorDirection.Down, false);
                    if (portrait != null && portrait.Available)
                    {
                        contextObject.sprite = portrait.frames[0];
                    }
                }

                contextObject.enabled = contextObject.sprite;
                contextObject.color = Color.white;
            }

            UpdateContextCaption();
            foreach (var b in buttons)
            {
                Destroy(b.gameObject);
            }

            buttons.Clear();
            var s = game.world.State;
            string detail = target.kind switch
            {
                WorldInteractionKind.Work => game.phase23.Jobs.Current?.DisplayName,
                WorldInteractionKind.Fridge => $"쌀 {s.rice} · 빵 {s.bread}",
                _ => null
            };
            contextTitle.text = string.IsNullOrEmpty(detail) ? target.title : $"{target.title} · {detail}";
            var ambient = Array.Find(game.world.rules.npcs, n => n.ambient && n.npcId == target.npcId);
            if (target.id == "npc-residence-front" && world.Room)
            {
                contextTitle.text = "주민 아파트 · 방문할 집";
                foreach (var unit in world.Room.catalog.rooms.Where(r => r.sceneName == "Resident_Housing"))
                {
                    Choice(unit.title, () => world.VisitApartment(target, unit));
                }
            }
            else if (ambient != null)
            {
                contextTitle.text = target.title + " · 주변 주민";
                Choice(
                    "잠깐 말 걸기",
                    () =>
                    {
                        CloseContext();
                        var scheduler = world.GetComponent<WorldNPCScheduler>();
                        Notify(
                            scheduler != null && scheduler.GreetAmbient(target.npcId, world.Player.Position)
                                ? ambient.ambientLine
                                : "주민이 이동 중입니다. 조금 뒤에 다시 말을 걸어보세요."
                        );
                    }
                );
            }
            else if (FamilyLife.Parent(target.npcId))
            {
                contextTitle.text = target.title + " · 가족";
                Choice("가족과 이야기하기", () => world.Begin(target, -1));
                if (!game.growth.IndependentOutdoorUnlocked)
                {
                    foreach (int a in new[] { 3, 4, 5, 2 })
                    {
                        int action = a;
                        Choice(
                            a == 3 ? "식사 돌봄"
                                : a == 4 ? "수분 돌봄"
                                : a == 5 ? "재우기"
                                : "함께 놀기",
                            () =>
                            {
                                CloseContext();
                                if (action == 5)
                                {
                                    var bed = game.world.City.CareTarget(GuideLesson.Sleep);
                                    if (bed && world.IsInReach(bed))
                                    {
                                        world.Begin(bed, action);
                                    }
                                    else
                                    {
                                        game.world.City.Track("family-ian-bedroom", out _);
                                        Notify("잠자리는 이안의 방에 있어요. 침대에서 잠자기를 선택하세요.");
                                    }
                                }
                                else
                                {
                                    world.Begin(target, action);
                                }
                            }
                        );
                    }
                }

                Choice(
                    "집 관리 · 독립과 가구",
                    () =>
                    {
                        CloseContext();
                        OpenHousing();
                    }
                );
            }
            else if (target.kind == WorldInteractionKind.Sleep && !GrowthAccess.Independent(game))
            {
                contextTitle.text = "돌봄 · 기초 생활";
                if (target.id == "family-bed" && game.growth.Age <= 2)
                {
                    Choice("보호자 목욕 돌봄 · 20분", () => world.Begin(target, -4));
                }

                foreach (int a in new[] { 5, 3, 4, 1, 2, 6, 17 })
                {
                    int action = a;
                    string label =
                        a == 5 ? "잠자기"
                        : a == 3 ? "식사 돌봄"
                        : a == 4 ? "수분 돌봄"
                        : a == 1 ? "쉬기"
                        : a == 2 ? "놀기"
                        : a == 6 ? "건강 돌봄"
                        : "기초 생활 학습";
                    Choice(
                        label,
                        () =>
                        {
                            var ux = GetComponent<GameplayUXPresenter>();
                            if (action == 23 && ux != null && game.CanPerformAction(action, out _))
                            {
                                ux.Confirm(
                                    "현재 직장을 퇴사하시겠습니까?\n다시 취업하려면 채용 조건을 충족해야 합니다.",
                                    () => world.Begin(target, action)
                                );
                            }
                            else
                            {
                                world.Begin(target, action);
                            }
                        }
                    );
                }
            }
            else if (target.kind == WorldInteractionKind.Fridge)
            {
                Choice("밥 준비 · 쌀 1개 · 20분", () => world.Begin(target, -1, "rice"));
                Choice("빵 준비 · 빵 1개 · 5분", () => world.Begin(target, -1, "bread"));
            }
            else if (
                (
                    target.kind == WorldInteractionKind.FoodShop
                    || target.kind == WorldInteractionKind.NPC && target.npcId == "shop-clerk"
                )
            )
            {
                Choice($"쌀 구매 · {game.world.rules.ricePrice:N0}G", () => BuyFood("rice"));
                Choice($"빵 구매 · {game.world.rules.breadPrice:N0}G", () => BuyFood("bread"));
                Choice(
                    $"가게 정리 돕기 · {game.world.rules.neighbourhoodHelpMinutes}분 · {game.world.rules.neighbourhoodHelpReward}G",
                    () => world.Begin(target, -3)
                );
                Choice("바로 식사 · 빵 1개 · 30분", () => world.Begin(target, 3, "bread"));
                Choice(
                    "가구점 · 가격과 보유 목록",
                    () =>
                    {
                        CloseContext();
                        OpenHousing();
                    }
                );
            }
            else if (target.kind == WorldInteractionKind.Work)
            {
                foreach (int a in new[] { 0, 20, 21, 22, 23 })
                {
                    ActionChoice(target, a);
                }

                Choice(
                    "지원 직업 변경",
                    () =>
                    {
                        var jobs = Array.FindAll(
                            game.phase23.catalog.jobs,
                            j => !j.specialUnlock && j.BaseDailyWage > 0
                        );
                        int i = Array.FindIndex(jobs, j => j.JobId == game.phase23.Progress.employment.selectedJobId);
                        game.phase23.Progress.employment.selectedJobId = jobs[(i + 1) % jobs.Length].JobId;
                        Notify("지원 직업 · " + jobs[(i + 1) % jobs.Length].DisplayName);
                    }
                );
            }
            else if (
                (
                    target.kind == WorldInteractionKind.Study
                    || target.kind == WorldInteractionKind.NPC && target.npcId == "school-teacher"
                )
            )
            {
                foreach (int a in new[] { 7, 8, 10, 11, 12, 13, 14, 15, 16, 17 })
                {
                    ActionChoice(target, a);
                }

                Choice(
                    "진로 변경 · 직업 / 진학",
                    () =>
                    {
                        game.phase23.SelectedCareerId =
                            game.phase23.SelectedCareerId == "vocational" ? "academic" : "vocational";
                        Notify("선택 진로 · " + game.phase23.SelectedCareerId);
                    }
                );
            }
            else if (
                target.kind == WorldInteractionKind.NPC
                && (target.zone == "finance" || target.zone == "realestate")
            )
            {
                Choice(
                    target.zone == "finance" ? "투자 상담 · 시장 보기" : "주거 상담 · 매물 보기",
                    () =>
                    {
                        if (!GrowthAccess.Adult(game))
                        {
                            Notify("금융·주거 계약은 19세부터 해금됩니다.");
                            return;
                        }

                        CloseContext();
                        if (target.zone == "finance")
                        {
                            GameContext.Instance.transitions.Navigate(SceneId.StockMarket);
                        }
                        else
                        {
                            GetComponent<PropertyPresenter>().Open();
                        }
                    }
                );
            }
            else if (target.kind == WorldInteractionKind.NPC)
            {
                game.phase23.SelectedNpcId = target.npcId;
                var profile = game.phase23.catalog.NPC(target.npcId);
                if (profile == null || game.growth.Age < profile.minAge || game.growth.Age > profile.maxAge)
                {
                    Choice("인사하기", () => world.Begin(target, -1));
                }
                else
                {
                    ActionChoice(target, 18);
                    var relationship = game.phase23.Social.Relationship(target.npcId);
                    if (relationship?.friend == true)
                    {
                        ActionChoice(target, 19);
                    }
                    else if (game.missions.ActiveMission?.Id != "M014")
                    {
                        ActionChoice(target, 9);
                    }
                }
            }

            var frame = contextTitle.transform.parent;
            if (!frame.TryGetComponent<RecordWindowBounds>(out var bounds))
            {
                bounds = frame.gameObject.AddComponent<RecordWindowBounds>();
            }

            bounds.FitChoices(buttons.Count);
            shell.Push(contextPanel);
            UIShell.Focus(contextPanel);
        }

        void ActionChoice(WorldInteractable target, int action)
        {
            var data = game.phase23.catalog.Find(action);
            string label =
                action == 0 ? $"근무 · {game.world.rules.shiftHours}시간 · 예상 {game.phase23.Work.Wage:N0}G"
                : data == null ? "활동 " + action
                : data.displayName;
            Choice(
                label,
                () =>
                {
                    var ux = GetComponent<GameplayUXPresenter>();
                    if (action == 23 && ux != null && game.CanPerformAction(action, out _))
                    {
                        ux.Confirm(
                            "현재 직장을 퇴사하시겠습니까?\n다시 취업하려면 채용 조건을 충족해야 합니다.",
                            () => world.Begin(target, action)
                        );
                    }
                    else
                    {
                        world.Begin(target, action);
                    }
                }
            );
        }

        void BuyFood(string id)
        {
            if (game.world.Hour < game.world.rules.shopOpen || game.world.Hour >= game.world.rules.shopClose)
            {
                Notify("지금은 영업 시간이 아닙니다.");
                return;
            }

            bool purchased = game.world.PurchaseFood(id, out string message);
            int cost = id == "rice" ? game.world.rules.ricePrice : game.world.rules.breadPrice;
            string itemName = id == "rice" ? "쌀" : "빵";
            purchaseResult = purchased ? $"{itemName} +1   ·   −{cost:N0} G" : message;
            UpdateContextCaption();
            Notify(purchased ? $"{itemName} 구매 완료 · {cost:N0} G 지출" : message, purchased ? 1 : 2);
        }
    }
}
