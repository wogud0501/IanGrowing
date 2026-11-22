using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace IanGame
{
    public sealed partial class GameplayUXPresenter
    {
        readonly Button[] recommendationButtons = new Button[3];
        readonly List<CitySuggestion> visibleChoices = new List<CitySuggestion>();
        Image guideRail;
        float railUntil;
        Button skipButton;
        GameObject manual;
        TMP_Text manualText;
        GuideLesson? displayedLesson;
        GuideLesson? lastPresented;
        string selectedRoute;
        WorldInteractable focusObject;
        float focusUntil;
        string guideFeedback;
        float guideFeedbackUntil;

        static readonly string[] LessonNames =
        {
            "식사 돌봄",
            "수분 돌봄",
            "잠자기",
            "미션",
            "상세 상태",
            "첫걸음",
            "주변 상호작용",
            "첫 외출",
            "도시와 건물",
            "학교",
            "사람들과 대화하기",
            "인벤토리",
            "장비",
            "헤드폰",
            "독립 첫날",
            "일자리",
            "첫 급여",
            "세금 고지",
            "부채",
            "주식",
            "가챠",
            "가구",
        };

        string lastInputLabel;

        void InputChanged(object input, InputActionChange change)
        {
            if (
                change != InputActionChange.ActionPerformed
                || input is not InputAction action
                || action.activeControl == null
            )
            {
                return;
            }

            var device = action.activeControl.device;
            if (device is not (Keyboard or Mouse or Gamepad))
            {
                return;
            }

            hud.UsingGamepad = device is Gamepad;
            string label = hud.InteractionInput;
            if (lastInputLabel == label)
            {
                return;
            }

            lastInputLabel = label;
            if (hud.world.CanControl && hud.world.Nearest)
            {
                hud.Prompt(city.Describe(hud.world.Nearest, label));
            }

            Invalidate();
        }

        string LessonText(GuideLesson lesson)
        {
            string interact = hud.InteractionInput;
            var target = Array.IndexOf(CityLifeService.Basics, lesson) >= 0 ? city.CareTarget(lesson) : null;
            string place = target ? target.title : "잠자리";
            switch (lesson)
            {
                case GuideLesson.Meal:
                    return !game.growth.WalkingUnlocked
                            ? "아직 혼자 걸을 수 없어요. "
                                + place
                                + "에서 "
                                + interact
                                + " → 식사 돌봄. 먹으면 허기가 차요."
                        : city.OffersCare(target) ? place + "에서 " + interact + " → 식사 돌봄."
                        : "식사하면 허기가 차요. 식탁에서 준비된 식사를 먹어 보세요.";
                case GuideLesson.Water:
                    return
                        !game.growth.IndependentOutdoorUnlocked
                        && (!game.growth.WalkingUnlocked || city.OffersCare(target))
                        ? place + "에서 " + interact + " → 수분 돌봄. 마시면 수분이 차요."
                        : (target ? place : "부모님 집 거실의 식수") + "에서 마시면 수분이 차요.";
                case GuideLesson.Sleep:
                    if (!game.growth.WalkingUnlocked && game.stats.Hunger >= 100 && game.stats.Thirst >= 100)
                    {
                        return "아직 혼자 걸을 수 없어요. 허기·수분은 가득해요. "
                            + place
                            + "에서 "
                            + interact
                            + " → 잠자기. 깨어나면 돌봄을 받아요.";
                    }

                    return place + "에서 " + interact + " → 잠자기. 시간이 흐르고 체력이 회복돼요.";
                case GuideLesson.Mission:
                    return "기본 생활을 배웠어요. 생활 → 미션에서 보상을 받으면 성장해요. 다른 일을 먼저 해도 괜찮아요.";
                case GuideLesson.Status:
                    return "메뉴 → 생활 → 상세 상태에서 허기·수분·체력·멘탈을 확인하세요.";
                case GuideLesson.Walk:
                    return GetComponent<WorldMobileHUD>()?.IsMobile == true
                            ? "이제 걸을 수 있어요. 화면의 이동 스틱으로 조금 걸어 보세요."
                        : hud.InteractionInput == "E" ? "이제 걸을 수 있어요. WASD 또는 방향키로 조금 걸어 보세요."
                        : "이제 걸을 수 있어요. 왼쪽 스틱으로 조금 걸어 보세요.";
                case GuideLesson.Interact:
                    return (hud.world.Nearest ? hud.world.Nearest.title : "가까운 사람이나 물건")
                        + " 곁에서 "
                        + interact
                        + "를 눌러 보세요.";
                case GuideLesson.Outside:
                    var exit = city.ExitTarget();
                    if (game.world.State.zone == "alley" && exit)
                    {
                        return "골목으로 나왔어요. ‘" + exit.title + "’ 쪽으로 가면 도시의 건물을 방문할 수 있어요.";
                    }

                    return "이제 혼자 외출할 수 있어요. "
                        + (exit ? "‘" + exit.title + "’ 쪽으로 가서 " : "")
                        + "부모님 집 현관을 통해 도시로 나가 보세요.";
                case GuideLesson.Visit:
                    var entrance = city.NearbyFacility();
                    if (entrance)
                    {
                        var host = game.world.Host;
                        bool crossStreet = !WorldNPCNavigation.OnPedestrianRoute(
                            host.CurrentZone,
                            host.Player.Position,
                            entrance.Point
                        );
                        return (crossStreet ? "도시에 왔어요. 횡단보도를 건너 ‘" : "도시에 왔어요. 가까운 ‘")
                            + game.world.rules.Zone(entrance.targetZone).title
                            + "’ 입구에서 상호작용해 보세요. 다른 곳을 둘러봐도 좋아요.";
                    }

                    return "지금은 방문할 시설이 없어요. 동네 상점은 "
                        + game.world.rules.shopOpen
                        + "시에 열어요. 골목으로 돌아가 집에서 쉬어도 좋아요.";
                case GuideLesson.School:
                    return "도시 → 학교의 교실 책상에서 공부하면 지식이 늘어요.";
                case GuideLesson.Social:
                    return "주변 사람에게 말을 걸어 보세요. 대화하면 관계와 사회성이 변할 수 있어요.";
                case GuideLesson.Inventory:
                    return "새 물건이 생겼어요. 메뉴 → 아이템 → 인벤토리에서 효과와 수량을 확인하세요.";
                case GuideLesson.Equipment:
                    return "가방에서 장비를 선택해 비교하고 장착하세요. 착용 효과가 상태에 반영돼요.";
                case GuideLesson.Headphones:
                    return "헤드폰은 이안의 대표 장비예요. 가방에서 장착하면 외형과 효과가 바뀌어요.";
                case GuideLesson.Independence:
                    var furnishings = game.economy.Housing;
                    bool bare =
                        furnishings.starterRevision > 0
                        && furnishings.owned.Count == 1
                        && furnishings.owned[0] == "basic-bed";
                    return "독립했습니다. "
                        + (bare ? "이 집의 가구는 아직 침대 하나예요. " : "")
                        + (game.economy.Cash == 0 ? "현금은 0 G예요. " : "")
                        + "침대에서 쉬거나 현관으로 나가세요. "
                        + (
                            game.phase23.Jobs.Current == null
                                ? "생활 → 직업에서 일자리를 확인하세요."
                                : "생활 → 직업에서 다음 근무를 확인하세요."
                        );
                case GuideLesson.Job:
                    return "도시 → 회사에서 일자리를 알아보세요. 근무하면 시간과 체력을 쓰고 급여를 받아요.";
                case GuideLesson.Salary:
                    return "첫 급여를 받았어요. 금융 → 경제 화면에서 잔액을 확인하세요. 생활비나 가구에 쓸 수 있어요.";
                case GuideLesson.Tax:
                    return "세금 고지서가 발행됐어요. 금융 → 경제 화면에서 납부 기한과 금액을 확인하세요.";
                case GuideLesson.Debt:
                    return "가계 명세서에서 남은 부채와 이자를 확인하세요. 금액을 입력해 조금씩 상환할 수 있어요.";
                case GuideLesson.Stocks:
                    return "도시 투자센터에서 주식을 거래할 수 있어요. 가격은 내려갈 수도 있어요.";
                case GuideLesson.Gacha:
                    return "뽑기 전에 보유 재화와 비용을 확인하세요. 아래쪽 ‘확률정보’에서 등급별 확률을 볼 수 있어요.";
                default:
                    return "독립 후에는 집 관리에서 가구를 살 수 있어요. 가진 돈과 가격을 비교하세요.";
            }
        }

        void BuildGuide()
        {
            Help = Window("지금 해볼 만한 것");
            helpText = ui.ScrollText(Help.transform, new Vector2(.05f, .54f), new Vector2(.95f, .86f));
            for (int i = 0; i < recommendationButtons.Length; i++)
            {
                int index = i;
                float top = .54f - i * .125f;
                recommendationButtons[i] = ui.Button(
                    Help.transform,
                    "추천 행동",
                    new Vector2(.05f, top - .105f),
                    new Vector2(.95f, top),
                    () => FollowSuggestion(index)
                );
            }

            ui.Button(Help.transform, "생활 도움말", new Vector2(.05f, .045f), new Vector2(.48f, .145f), OpenManual);
            skipButton = ui.Button(
                Help.transform,
                "안내 건너뛰기",
                new Vector2(.52f, .045f),
                new Vector2(.95f, .145f),
                () =>
                    Confirm(
                        "기본 조작 안내를 건너뛸까요?\n나중에 생활 도움말에서 다시 읽을 수 있어요.",
                        () =>
                        {
                            city.SkipLessons();
                            RefreshGuide();
                        }
                    )
            );
            manual = Window("생활 도움말");
            ui.Button(
                manual.transform,
                "기본 안내 다시 시작",
                new Vector2(.51f, .045f),
                new Vector2(.95f, .145f),
                () => Confirm("생활 안내를 처음부터 다시 볼까요? 게임 진행은 유지됩니다.", city.ResetUXGuides)
            );
            ui.Button(
                manual.transform,
                "HUD 추천 표시 전환",
                new Vector2(.05f, .045f),
                new Vector2(.48f, .145f),
                () => city.ShowSuggestions(city.Data.suggestionsHidden)
            );
            manualText = ui.ScrollText(manual.transform, new Vector2(.05f, .18f), new Vector2(.95f, .86f));
        }

        void OpenManual()
        {
            var entries = new List<string>();
            foreach (GuideLesson lesson in Enum.GetValues(typeof(GuideLesson)))
            {
                if (CanRead(lesson))
                {
                    entries.Add(LessonNames[(int)lesson] + "\n" + LessonText(lesson));
                }
            }

            manualText.text = string.Join("\n\n", entries);
            Open(manual);
        }

        bool CanRead(GuideLesson lesson)
        {
            return lesson switch
            {
                GuideLesson.Walk or GuideLesson.Interact => game.growth.WalkingUnlocked,
                GuideLesson.Visit or GuideLesson.Outside or GuideLesson.Social => GrowthAccess.Independent(game),
                GuideLesson.School => GrowthAccess.Zone(game, "school", out _),
                GuideLesson.Inventory => game.items.Inventory.Entries.Any(e => e.count > 0) || game.items.Unlocked,
                GuideLesson.Equipment or GuideLesson.Headphones or GuideLesson.Gacha => game.items.Unlocked,
                GuideLesson.Job or GuideLesson.Stocks or GuideLesson.Debt or GuideLesson.Tax or GuideLesson.Salary =>
                    GrowthAccess.Adult(game),
                GuideLesson.Independence or GuideLesson.Furniture => new ResidenceAccess(game).IsActiveResidence,
                _ => true,
            };
        }

        void FollowSuggestion(int index)
        {
            if (index >= visibleChoices.Count)
            {
                return;
            }

            var choice = visibleChoices[index];
            switch (choice.screen)
            {
                case "mission":
                    GetComponent<UINavigationService>().Mission();
                    return;
                case "inventory":
                    GetComponent<CollectionUI>().OpenInventory();
                    return;
                case "job":
                    OpenStatus(2);
                    return;
                case "finance":
                    GetComponent<FinancePresenter>().OpenEconomy();
                    return;
                case "housing":
                    hud.OpenHousing();
                    return;
                case "status":
                    OpenStatus();
                    return;
            }

            if (choice.target && hud.world.IsInReach(choice.target))
            {
                GetComponent<UINavigationService>().CloseAll();
                if (hud.world.Use(choice.target))
                {
                    return;
                }
            }

            if (!string.IsNullOrEmpty(choice.room) && !city.Track(choice.room, out var reason))
            {
                selectedRoute = reason ?? "현재 위치에서 갈 수 있는 경로를 찾지 못했어요.";
                helpText.text = selectedRoute;
                return;
            }

            selectedRoute = choice.title + "\n" + choice.detail;
            if (!string.IsNullOrEmpty(choice.room) && choice.room != game.world.State.zone)
            {
                string doorId = game.world.rooms?.FirstDoor(game.world.State.zone, choice.room);
                var door = hud.world.objects.FirstOrDefault(o => o && o.id == doorId);
                if (door)
                {
                    selectedRoute += "\n먼저 ‘" + door.title + "’ 방향으로 가세요.";
                }
            }

            helpText.text = selectedRoute;
        }

        string LessonHint(GuideLesson lesson)
        {
            if (Array.IndexOf(CityLifeService.Basics, lesson) >= 0)
            {
                var target = city.CareTarget(lesson);
                if (target)
                {
                    string verb = lesson == GuideLesson.Sleep ? "잠자기"
                        : lesson == GuideLesson.Meal ? (city.OffersCare(target) ? "식사 돌봄" : "식사")
                        : city.OffersCare(target) ? "수분 돌봄" : "물 마시기";
                    return target.title + " · " + hud.InteractionInput + " → " + verb;
                }
            }

            switch (lesson)
            {
                case GuideLesson.Walk:
                    return GetComponent<WorldMobileHUD>()?.IsMobile == true ? "이동 스틱으로 걷기"
                        : hud.UsingGamepad ? "왼쪽 스틱으로 걷기" : "WASD · 방향키로 걷기";
                case GuideLesson.Interact:
                    return hud.InteractionInput + " · 주변과 상호작용";
                case GuideLesson.Mission:
                    return "생활 → 미션에서 보상 받기";
                case GuideLesson.Outside:
                    return (city.ExitTarget()?.title ?? "현관") + " → 도시";
                case GuideLesson.Visit:
                    var entrance = city.NearbyFacility();
                    return entrance ? game.world.rules.Zone(entrance.targetZone).title + " 입구로 가기"
                        : "상점 " + game.world.rules.shopOpen + "시 개점 · 집으로";
                case GuideLesson.Independence:
                    return "독립 첫날 · 생활 → 직업";
                default:
                    return LessonNames[(int)lesson] + " · 도움말";
            }
        }

        void RefreshLearningCard()
        {
            var options = city.Suggestions();
            displayedLesson = city.CurrentLesson();
            bool urgent = options[0].urgent;
            if (urgent && displayedLesson != GuideLesson.Independence && options[0].lesson != displayedLesson)
            {
                displayedLesson = null;
            }
            guide.text = guideFeedback ?? (displayedLesson.HasValue ? LessonHint(displayedLesson.Value) : options[0].title);
            visibleChoices.Clear();
            visibleChoices.AddRange(options);
            skipButton.gameObject.SetActive(!city.GuideSkipped);
            if (!Help.activeInHierarchy)
            {
                return;
            }

            helpText.text =
                selectedRoute
                ?? (
                    displayedLesson.HasValue
                        ? LessonText(displayedLesson.Value)
                        : "상태에 맞는 행동을 골라 보세요. 다른 일을 먼저 해도 괜찮아요."
                );
            if (selectedRoute == null && game.world.Host?.CurrentZone != null)
            {
                helpText.text += "\n\n현재 위치   " + game.world.Host.CurrentZone.title;
            }
            for (int i = 0; i < recommendationButtons.Length; i++)
            {
                var button = recommendationButtons[i];
                button.gameObject.SetActive(i < options.Count);
                if (i < options.Count)
                {
                    button.GetComponentInChildren<TMP_Text>().text = options[i].title;
                }
            }
        }

        void PresentLesson()
        {
            if (railUntil > 0 && Time.unscaledTime >= railUntil)
            {
                guideRail.color = LifeRecordUI.Track;
                railUntil = 0;
            }

            if (guideFeedback != null && Time.unscaledTime >= guideFeedbackUntil)
            {
                guideFeedback = null;
                Invalidate();
            }

            if (focusObject && Time.unscaledTime >= focusUntil)
            {
                focusObject.GuideFocus(false);
                focusObject = null;
            }

            if (!card.gameObject.activeInHierarchy || !displayedLesson.HasValue || displayedLesson == lastPresented)
            {
                return;
            }

            var step = displayedLesson.Value;
            if (focusObject)
            {
                focusObject.GuideFocus(false);
                focusObject = null;
            }

            lastPresented = step;
            if (
                step == GuideLesson.Mission
                || city.Progress(step) == LessonState.NotStarted && !game.growth.WalkingUnlocked
            )
            {
                guideRail.color = LifeRecordUI.Heading;
                railUntil = Time.unscaledTime + 2;
            }

            city.StartLesson(step);
            if (
                step == GuideLesson.Sleep
                && !game.growth.WalkingUnlocked
                && game.stats.Hunger >= 100
                && game.stats.Thirst >= 100
            )
            {
                hud.FocusVital(0);
                hud.FocusVital(1);
            }
            else if (step == GuideLesson.Meal || step == GuideLesson.Water)
            {
                hud.FocusVital(step == GuideLesson.Meal ? 0 : 1);
            }

            if (step == GuideLesson.Sleep || step == GuideLesson.Outside || step == GuideLesson.Interact)
            {
                focusObject =
                    step == GuideLesson.Sleep ? city.CareTarget(step)
                    : step == GuideLesson.Outside ? city.ExitTarget()
                    : hud.world.Nearest;
                if (focusObject)
                {
                    focusObject.GuideFocus(true);
                    focusUntil = Time.unscaledTime + 2;
                }
            }
        }

        void Learned(GuideLesson lesson)
        {
            if (!displayedLesson.HasValue || displayedLesson.Value != lesson)
            {
                return;
            }

            guideFeedback = lesson switch
            {
                GuideLesson.Meal => "식사했어요. 지금 허기 " + game.stats.Hunger,
                GuideLesson.Water => "물을 마셨어요. 지금 수분 " + game.stats.Thirst,
                GuideLesson.Sleep => "잠에서 깼어요.",
                _ => null,
            };
            lastPresented = null;
            guideFeedbackUntil = Time.unscaledTime + 3;
            Invalidate();
        }
    }
}
