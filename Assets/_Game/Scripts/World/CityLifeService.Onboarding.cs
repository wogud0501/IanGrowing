using System;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    public enum GuideLesson
    {
        Meal,
        Water,
        Sleep,
        Mission,
        Status,
        Walk,
        Interact,
        Outside,
        Visit,
        School,
        Social,
        Inventory,
        Equipment,
        Headphones,
        Independence,
        Job,
        Salary,
        Tax,
        Debt,
        Stocks,
        Gacha,
        Furniture,
    }

    public enum LessonState
    {
        NotStarted,
        Active,
        Completed,
        Skipped,
    }

    public sealed partial class CityLifeService
    {
        const string GuidePrefix = "ux/learning/";
        internal const string NewGuide = GuidePrefix + "v1";
        const string SkipKey = GuidePrefix + "skip";
        static readonly string[] ActiveKeys = LessonKeys("active");
        static readonly string[] CompletedKeys = LessonKeys("done");
        static readonly string[] MigratedKeys = LessonKeys("past");
        float walked;
        int observedHour = -1;

        public event Action<GuideLesson> TutorialStarted;
        public event Action<GuideLesson> TutorialStepCompleted;
        public event Action TutorialSkipped;
        public event Action TutorialHelpOpened;

        static string[] LessonKeys(string state)
        {
            return Enum.GetNames(typeof(GuideLesson)).Select(name => GuidePrefix + state + "/" + name).ToArray();
        }

        public bool GuideSkipped => Seen(SkipKey);

        public LessonState Progress(GuideLesson lesson)
        {
            if (Seen(CompletedKeys[(int)lesson]))
            {
                return LessonState.Completed;
            }

            if (GuideSkipped || Seen(MigratedKeys[(int)lesson]))
            {
                return LessonState.Skipped;
            }

            return Seen(ActiveKeys[(int)lesson]) ? LessonState.Active : LessonState.NotStarted;
        }

        public void StartLesson(GuideLesson lesson)
        {
            if (!Seen(NewGuide) || Progress(lesson) != LessonState.NotStarted)
            {
                return;
            }

            MarkGuide(ActiveKeys[(int)lesson]);
            TutorialStarted?.Invoke(lesson);
        }

        public void Learn(GuideLesson lesson)
        {
            var status = Progress(lesson);
            if (!Seen(NewGuide) || status == LessonState.Skipped || status == LessonState.Completed)
            {
                return;
            }

            Data.guideSeen.Remove(ActiveKeys[(int)lesson]);
            MarkGuide(CompletedKeys[(int)lesson]);
            TutorialStepCompleted?.Invoke(lesson);
        }

        public void SkipLessons()
        {
            if (!GuideSkipped)
            {
                MarkGuide(SkipKey);
                TutorialSkipped?.Invoke();
            }
        }

        public void OpenedHelp()
        {
            TutorialHelpOpened?.Invoke();
        }

        void MigrateLessons()
        {
            if (Seen(NewGuide))
            {
                return;
            }

            // Old saves already in play can read the basics in Help. Future unlocks still teach normally.
            foreach (GuideLesson lesson in Enum.GetValues(typeof(GuideLesson)))
            {
                bool past = lesson switch
                {
                    GuideLesson.Walk or GuideLesson.Interact => game.growth.WalkingUnlocked,
                    GuideLesson.Outside or GuideLesson.Visit or GuideLesson.School or GuideLesson.Social =>
                        game.growth.IndependentOutdoorUnlocked,
                    GuideLesson.Inventory => game.items.Inventory.Entries.Any(e => e.count > 0),
                    GuideLesson.Equipment => game.items.Equipment.Current(EquipmentSlot.Equipment) != null,
                    GuideLesson.Headphones => game.items.Equipment.Current(EquipmentSlot.Headphone) != null,
                    GuideLesson.Gacha => Seen("items/gacha"),
                    GuideLesson.Independence or GuideLesson.Furniture => new ResidenceAccess(game).IsActiveResidence,
                    GuideLesson.Job or GuideLesson.Stocks => GrowthAccess.Adult(game),
                    GuideLesson.Salary => Counter("salary") > 0,
                    GuideLesson.Tax => game.economy.HasTaxBill,
                    GuideLesson.Debt => Seen("ux/tutorial/debt"),
                    _ => !game.BirthIntroductionPending,
                };
                if (past)
                {
                    Data.guideSeen.Add(MigratedKeys[(int)lesson]);
                }
            }

            MarkGuide(NewGuide);
        }

        void ObserveGuideMovement(LifeWorldController host, Vector2 position)
        {
            if (
                game.growth.WalkingUnlocked
                && Progress(GuideLesson.Walk) == LessonState.Active
                && observed
                && host.CanControl
                && host.Player.ActualSpeed > .06f
            )
            {
                float delta = Vector2.Distance(lastPosition, position);
                if (delta < .75f)
                {
                    walked += delta;
                    if (walked >= .65f)
                    {
                        Learn(GuideLesson.Walk);
                    }
                }
            }

            if (world.Hour != observedHour)
            {
                observedHour = world.Hour;
                Changed?.Invoke();
            }
        }

        void ObserveTax()
        {
            if (Active && GrowthAccess.Adult(game) && game.economy.HasTaxBill)
            {
                StartLesson(GuideLesson.Tax);
            }
        }

        internal void ItemFound(ItemEvent item)
        {
            StartLesson(GuideLesson.Inventory);
            var definition = game.items.catalog.Item(item.itemId);
            if (definition?.itemType == ItemType.Headphone || definition?.itemType == ItemType.Equipment)
            {
                StartLesson(definition.itemType == ItemType.Headphone ? GuideLesson.Headphones : GuideLesson.Equipment);
            }
        }

        internal void Equipped(ItemEvent item)
        {
            if (item.slot == EquipmentSlot.Headphone || item.slot == EquipmentSlot.Equipment)
            {
                Learn(item.slot == EquipmentSlot.Headphone ? GuideLesson.Headphones : GuideLesson.Equipment);
            }
        }

        internal void ActionFinishedForGuide(int action, int hungerBefore, int thirstBefore)
        {
            if (action == 3 && game.stats.Hunger > hungerBefore)
            {
                Learn(GuideLesson.Meal);
            }
            else if (action == 4 && game.stats.Thirst > thirstBefore)
            {
                Learn(GuideLesson.Water);
            }
            else if (action == 5)
            {
                Learn(GuideLesson.Sleep);
                if (world.State.zone == "home" && Progress(GuideLesson.Independence) == LessonState.Active)
                {
                    Learn(GuideLesson.Independence);
                }
            }
        }

        internal void UsedObject()
        {
            if (game.growth.WalkingUnlocked && Progress(GuideLesson.Walk) == LessonState.Completed)
            {
                Learn(GuideLesson.Interact);
            }
        }

        internal void EnteredForGuide(string room)
        {
            if (world.rules.Zone(room)?.outdoor == true && game.growth.IndependentOutdoorUnlocked)
            {
                if (Progress(GuideLesson.Independence) == LessonState.Active)
                {
                    Learn(GuideLesson.Independence);
                }

                if (room == "city")
                {
                    Learn(GuideLesson.Outside);
                    StartLesson(GuideLesson.Visit);
                }
            }

            if (GrowthAccess.Independent(game) && FacilityPurpose(room) != null)
            {
                Learn(GuideLesson.Visit);
            }

            if (room == "school")
            {
                Learn(GuideLesson.School);
            }

            if (room == "workplace")
            {
                Learn(GuideLesson.Job);
            }

            if (room == "home" && new ResidenceAccess(game).IsActiveResidence)
            {
                StartLesson(GuideLesson.Independence);
            }
        }

        internal static string FacilityPurpose(string room)
        {
            return room switch
            {
                "shop" => "계산대에서 쌀과 빵을 살 수 있어요.",
                "school" => "교실의 책상에서 교육과 수업을 확인하세요.",
                "workplace" => "채용·근무 창에서 지원 조건을 확인하세요.",
                "finance" => "투자 단말기에서 시세와 보유 종목을 확인하세요.",
                "realestate" => "매물과 계약 조건을 살펴볼 수 있습니다.",
                _ => null,
            };
        }

        internal bool CanVisit(string room)
        {
            if (world.rules?.Zone(room) == null || !GrowthAccess.Zone(game, room, out _))
            {
                return false;
            }

            if (room == "shop" && (world.Hour < world.rules.shopOpen || world.Hour >= world.rules.shopClose))
            {
                return false;
            }

            if (
                world.Host
                && world.Host.Room
                && room != world.State.zone
                && world.rooms.FirstDoor(world.State.zone, room) == null
            )
            {
                return false;
            }

            var entrance = world.Host?.objects.FirstOrDefault(o =>
                o && o.kind == WorldInteractionKind.Door && o.zone == world.State.zone && o.targetZone == room
            );
            return !entrance
                || !entrance.TryGetComponent<WorldDoor>(out var door)
                || door.CanEnter("player", world.Hour, out _);
        }
    }
}
