using System.Collections.Generic;
using System.Linq;

namespace IanGame
{
    public sealed partial class CityLifeService
    {
        internal bool OffersCare(WorldInteractable target)
        {
            return target
                && !game.growth.IndependentOutdoorUnlocked
                && (target.id == "family-bed" || FamilyLife.Parent(target.npcId));
        }

        internal WorldInteractable CareTarget(GuideLesson lesson)
        {
            var host = world.Host;
            if (!host || host.objects == null)
            {
                return null;
            }

            return host.Interactables.FirstOrDefault(o =>
                o
                && o.isActiveAndEnabled
                && o.zone == world.State.zone
                && (game.growth.WalkingUnlocked || host.IsInReach(o))
                && (!o.TryGetComponent<WorldLifeAction>(out var action) || action.Available)
                && (
                    OffersCare(o) && (lesson != GuideLesson.Sleep || o.kind == WorldInteractionKind.Sleep)
                    || lesson switch
                    {
                        GuideLesson.Sleep => o.kind == WorldInteractionKind.Sleep,
                        GuideLesson.Water => o.kind == WorldInteractionKind.Drink && game.growth.WalkingUnlocked,
                        GuideLesson.Meal => o.kind == WorldInteractionKind.Meal
                            && game.growth.WalkingUnlocked
                            && (
                                FamilyLife.IsFamilyRoom(o.zone)
                                    ? FamilyLife.State(game).meals > 0
                                    : !string.IsNullOrEmpty(world.State.prepared)
                            ),
                        _ => false,
                    }
                )
            );
        }

        bool HasWearable(GuideLesson lesson)
        {
            var type = lesson == GuideLesson.Headphones ? ItemType.Headphone : ItemType.Equipment;
            return game.items.Unlocked
                && game.items.Inventory.Entries.Any(entry =>
                    entry.count > 0
                    && game.items.catalog.Item(entry.itemId) is ItemDefinition item
                    && item.itemType == type
                    && game.growth.Age >= item.minAge
                );
        }

        internal WorldInteractable ExitTarget()
        {
            if ((!FamilyLife.IsFamilyRoom(world.State.zone) && world.State.zone != "alley") || !world.Host)
            {
                return null;
            }

            string next = world.rooms?.FirstDoor(world.State.zone, "city");
            return world.Host.objects.FirstOrDefault(o =>
                o
                && o.isActiveAndEnabled
                && o.zone == world.State.zone
                && o.kind == WorldInteractionKind.Door
                && (o.id == next)
                && CanVisit(o.targetZone)
            );
        }

        internal bool HasBasics =>
            Progress(GuideLesson.Meal) == LessonState.Completed
            && Progress(GuideLesson.Water) == LessonState.Completed
            && Progress(GuideLesson.Sleep) == LessonState.Completed;

        public GuideLesson? CurrentLesson()
        {
            if (!Seen(NewGuide) || GuideSkipped)
            {
                return null;
            }

            bool pending(GuideLesson lesson) => Progress(lesson) is LessonState.NotStarted or LessonState.Active;
            bool active(GuideLesson lesson) => Progress(lesson) == LessonState.Active;
            if (active(GuideLesson.Independence) && world.State.zone == "home")
            {
                return GuideLesson.Independence;
            }

            if (game.growth.WalkingUnlocked && pending(GuideLesson.Walk))
            {
                return GuideLesson.Walk;
            }

            var nearby = world.Host?.Nearest;
            if (
                game.growth.WalkingUnlocked
                && pending(GuideLesson.Interact)
                && nearby
                && GrowthAccess.Interaction(game, nearby, -2, out _)
                && (!nearby.TryGetComponent<WorldDoor>(out var door) || door.CanEnter("player", world.Hour, out _))
                && (!nearby.TryGetComponent<WorldLifeAction>(out var objectAction) || objectAction.Available)
            )
            {
                return GuideLesson.Interact;
            }

            if (game.growth.IndependentOutdoorUnlocked && pending(GuideLesson.Outside) && ExitTarget())
            {
                return GuideLesson.Outside;
            }

            if (active(GuideLesson.Visit) && world.State.zone == "city")
            {
                return GuideLesson.Visit;
            }

            if (!game.growth.IndependentOutdoorUnlocked)
            {
                if (
                    pending(GuideLesson.Sleep)
                    && game.stats.Hunger >= 100
                    && game.stats.Thirst >= 100
                    && CareTarget(GuideLesson.Sleep)
                )
                {
                    return GuideLesson.Sleep;
                }

                foreach (var lesson in Basics)
                {
                    if (
                        pending(lesson)
                        && CareTarget(lesson)
                        && (lesson != GuideLesson.Meal || game.stats.Hunger < 100)
                        && (lesson != GuideLesson.Water || game.stats.Thirst < 100)
                    )
                    {
                        return lesson;
                    }
                }
            }

            if (HasBasics && pending(GuideLesson.Mission))
            {
                return GuideLesson.Mission;
            }

            if (HasBasics && pending(GuideLesson.Status))
            {
                return GuideLesson.Status;
            }

            foreach (var lesson in Opportunities)
            {
                if (
                    active(lesson)
                    && (
                        lesson != GuideLesson.Social
                        || nearby && nearby.isActiveAndEnabled && nearby.kind == WorldInteractionKind.NPC
                    )
                    && (lesson != GuideLesson.School || CanVisit("school") && SchoolInSession())
                    && (lesson != GuideLesson.Job || CanVisit("workplace"))
                    && (lesson != GuideLesson.Headphones && lesson != GuideLesson.Equipment || HasWearable(lesson))
                )
                {
                    return lesson;
                }
            }

            return null;
        }

        internal static readonly GuideLesson[] Basics = { GuideLesson.Meal, GuideLesson.Water, GuideLesson.Sleep };
        static readonly GuideLesson[] Opportunities =
        {
            GuideLesson.Inventory,
            GuideLesson.Headphones,
            GuideLesson.Equipment,
            GuideLesson.Salary,
            GuideLesson.Tax,
            GuideLesson.Job,
            GuideLesson.School,
            GuideLesson.Social,
            GuideLesson.Stocks,
            GuideLesson.Furniture,
        };

        bool SuggestCare(List<CitySuggestion> choices, GuideLesson lesson, string title, bool urgent = false)
        {
            var target = CareTarget(lesson);
            string room = target
                ? target.zone
                : lesson switch
                {
                    GuideLesson.Sleep => FamilyLife.AtFamily(game) ? "family-ian-bedroom" : "home",
                    GuideLesson.Water => "family-living",
                    _ => "family-kitchen",
                };
            if (!target && (room == world.State.zone || !game.growth.WalkingUnlocked || !CanVisit(room)))
            {
                return false;
            }

            if (!target && lesson == GuideLesson.Meal && FamilyLife.State(game).meals == 0)
            {
                return false;
            }

            var verb =
                lesson == GuideLesson.Meal ? "식사"
                : lesson == GuideLesson.Water ? "물 마시기"
                : "잠자기";
            string instruction = OffersCare(target)
                ? (
                    lesson == GuideLesson.Meal ? "식사 돌봄"
                    : lesson == GuideLesson.Water ? "수분 돌봄"
                    : "잠자기"
                )
                : verb;
            choices.Add(
                new CitySuggestion
                {
                    title = title,
                    detail = (target ? target.title : world.rules.Zone(room).title) + " → " + instruction,
                    room = room,
                    target = target,
                    lesson = lesson,
                    urgent = urgent,
                }
            );
            return true;
        }

        bool SchoolInSession()
        {
            var profile = world.Host ? world.Host.GetComponent<WorldNPCScheduler>()?.lifeProfile : null;
            return profile ? profile.AmbientLimit("school", world.Hour) > 0 : world.Hour >= 8 && world.Hour < 17;
        }

        internal WorldInteractable NearbyFacility()
        {
            var host = world.Host;
            if (!host || !host.Player || world.rules.Zone(world.State.zone)?.outdoor != true)
            {
                return null;
            }

            WorldInteractable nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var entry in host.objects)
            {
                if (
                    !entry
                    || !entry.isActiveAndEnabled
                    || entry.zone != world.State.zone
                    || entry.kind != WorldInteractionKind.Door
                    || FacilityPurpose(entry.targetZone) == null
                    || !CanVisit(entry.targetZone)
                    || entry.targetZone == "school" && !SchoolInSession()
                )
                {
                    continue;
                }

                float candidate = (entry.Point - host.Player.Position).sqrMagnitude;
                if (candidate < distance)
                {
                    distance = candidate;
                    nearest = entry;
                }
            }

            return nearest;
        }

        public List<CitySuggestion> Suggestions()
        {
            var choices = new List<CitySuggestion>(3);
            if (game.stats.Hunger < 45)
            {
                SuggestCare(choices, GuideLesson.Meal, "허기가 낮아요 · 식사하기", true);
            }

            if (game.stats.Thirst < 35)
            {
                SuggestCare(choices, GuideLesson.Water, "수분이 낮아요 · 물 마시기", true);
            }

            if (game.stats.Health < 25 || world.State.fatigue > 75)
            {
                SuggestCare(choices, GuideLesson.Sleep, "피곤해요 · 잠자리에서 쉬기", true);
            }

            void add(string title, string detail, string room = null, string screen = null)
            {
                if (choices.Count < 3 && (room == null || CanVisit(room)))
                {
                    choices.Add(
                        new CitySuggestion
                        {
                            title = title,
                            detail = detail,
                            room = room,
                            screen = screen,
                        }
                    );
                }
            }

            var lesson = CurrentLesson();
            var entrance = NearbyFacility();
            void suggestFacility()
            {
                string room = entrance.targetZone;
                add(world.rules.Zone(room).title + " 방문", FacilityPurpose(room), room);
            }

            if (
                choices.Count < 3
                && lesson.HasValue
                && System.Array.IndexOf(Basics, lesson.Value) >= 0
                && !(lesson == GuideLesson.Meal && game.stats.Hunger < 45)
                && !(lesson == GuideLesson.Water && game.stats.Thirst < 35)
                && !(lesson == GuideLesson.Sleep && (game.stats.Health < 25 || world.State.fatigue > 75))
            )
            {
                SuggestCare(
                    choices,
                    lesson.Value,
                    lesson == GuideLesson.Meal ? "먹어 보기"
                        : lesson == GuideLesson.Water ? "마셔 보기"
                        : "잠자기"
                );
            }

            if (game.missions.ActiveMission is MissionData mission)
            {
                bool claimable = game.missions.GetProgress(mission).state == MissionState.Completed;
                string direction =
                    claimable ? "미션 창 → 보상 수령"
                    : mission.Id == "M014" ? game.phase23.Social.IntroductionHint()
                    : mission.Id == "M009" ? "날짜가 바뀌면 생존 일수가 늘어요"
                    : "생활 메뉴 → 미션에서 조건과 보상 확인";
                add("현재 미션 · " + mission.title, direction, screen: "mission");
                if (
                    mission.Id == "M009"
                    && !claimable
                    && choices.Count < 3
                    && CareTarget(GuideLesson.Sleep)
                    && !choices.Exists(c => c.lesson == GuideLesson.Sleep)
                )
                {
                    SuggestCare(choices, GuideLesson.Sleep, "침대에서 8시간 보내기");
                }
            }

            if (game.items.Inventory.Entries.Any(e => e.isNew && e.count > 0))
            {
                add("새 물건 확인", "아이템 → 가방", screen: "inventory");
            }

            if (lesson == GuideLesson.Salary || lesson == GuideLesson.Tax)
            {
                add("가계 명세서 확인", "금융 → 경제 화면", screen: "finance");
            }
            else if (lesson == GuideLesson.Furniture)
            {
                add("살 수 있는 가구 보기", "집 관리 → 가구", screen: "housing");
            }
            else if (lesson == GuideLesson.Equipment || lesson == GuideLesson.Headphones)
            {
                add("가방에서 장비 비교하기", "아이템 → 인벤토리", screen: "inventory");
            }
            else if (lesson == GuideLesson.Outside)
            {
                var exit = ExitTarget();
                add("첫 외출", exit ? exit.title + " → 도시" : "부모님 집 현관 → 도시", "city");
            }
            else if (lesson == GuideLesson.Visit)
            {
                if (entrance)
                {
                    suggestFacility();
                }
                else
                {
                    string room = FamilyLife.AtFamily(game) ? "family-ian-bedroom" : "home";
                    add("집으로 돌아가 쉬기", "주거 골목 → 집 → 침대", room);
                }
            }
            else if (lesson == GuideLesson.Stocks)
            {
                add("투자센터 방문", "도시 → 투자센터", "finance");
            }

            if (!game.growth.WalkingUnlocked && choices.Count == 1 && CareTarget(GuideLesson.Sleep))
            {
                SuggestCare(choices, GuideLesson.Sleep, "침대에서 보호자 돌봄 살펴보기");
            }
            else if (game.growth.EmploymentUnlocked && game.economy.Cash < 1000)
            {
                add(
                    game.phase23.Jobs.Current == null ? "채용 조건 알아보기" : "다음 근무 시간 확인",
                    "생활 메뉴 → 직업 · 근무",
                    screen: "job"
                );
            }
            else if (
                game.growth.IndependentOutdoorUnlocked
                && !game.growth.EmploymentUnlocked
                && SchoolInSession()
                && (game.phase23.Education.Can(7, out _) || game.phase23.Education.Can(8, out _))
            )
            {
                add("학교에서 수업 알아보기", "도시 → 학교 → 교실 책상", "school");
            }

            if (entrance && !choices.Exists(choice => choice.room == entrance.targetZone))
            {
                suggestFacility();
            }

            add("이안의 상태 살펴보기", "생활 메뉴 → 상세 상태", screen: "status");
            return choices;
        }
    }
}
