using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    [Serializable]
    public sealed class CityCounter
    {
        public string id;
        public long value;
    }

    [Serializable]
    public sealed class CityLifeProgress
    {
        public int schema = 1;
        public List<string> discovered = new List<string>();
        public List<string> achievements = new List<string>();
        public List<string> guideSeen = new List<string>();
        public List<string> receipts = new List<string>();
        public List<CityCounter> counters = new List<CityCounter>();
        public string destination = "";
        public bool suggestionsHidden;
        public bool followingFirstFriend;
    }

    public sealed class CityAchievement
    {
        public string id;
        public string title;
        public string description;
        public int minAge;
        public bool hidden;
        public long target = 1;
    }

    public sealed class CitySuggestion
    {
        public string title;
        public string detail;
        public string room;
        public string screen;
        public WorldInteractable target;
        public GuideLesson? lesson;
        public bool urgent;
    }

    // Uses the original world snapshot and existing success events. No new wallet,
    // reward payment, age policy, scene loader or quest gate is introduced.
    public sealed partial class CityLifeService
    {
        readonly LifeWorldService world;
        readonly GameManager game;
        readonly SceneTransitionService transitions;
        bool dirty;
        float saveAfter;
        readonly Queue<string> notices = new Queue<string>();
        float nextNotice;
        string currentRoom;
        int friendHintMinute = -1;
        Vector2 lastPosition;
        bool observed;
        public event Action Changed;
        public event Action<string> Notice;
        public CityLifeProgress Data
        {
            get
            {
                if (world.State.cityLife == null)
                {
                    world.State.cityLife = new CityLifeProgress();
                }

                return world.State.cityLife;
            }
        }

        public static readonly CityAchievement[] Catalog = new CityAchievement[]
        {
            new CityAchievement
            {
                id = "walk",
                title = "첫 걸음",
                description = "집 안에서 실제로 걸어 보기",
                minAge = 1,
            },
            new CityAchievement
            {
                id = "outing",
                title = "첫 외출",
                description = "해금 후 직접 야외로 나가기",
                minAge = 7,
            },
            new CityAchievement
            {
                id = "visit/shop",
                title = "첫 상점 방문",
                description = "도시 상점 내부 방문",
                minAge = 7,
            },
            new CityAchievement
            {
                id = "purchase",
                title = "첫 물건 구매",
                description = "실제 재화로 물건 구매",
                minAge = 7,
            },
            new CityAchievement
            {
                id = "meal",
                title = "첫 식사",
                description = "연령에 맞는 실제 식사 또는 수유",
            },
            new CityAchievement
            {
                id = "work",
                title = "첫 근무",
                description = "조건을 충족한 직장에서 근무 완료",
                minAge = 19,
            },
            new CityAchievement
            {
                id = "salary",
                title = "첫 급여",
                description = "기존 지갑으로 급여 수령",
                minAge = 19,
            },
            new CityAchievement
            {
                id = "friend",
                title = "첫 친구",
                description = "기존 사회성 시스템에서 친구 만들기",
                minAge = 7,
            },
            new CityAchievement
            {
                id = "study",
                title = "첫 수업",
                description = "학교에서 학습 행동 완료",
                minAge = 7,
            },
            new CityAchievement
            {
                id = "furniture",
                title = "첫 가구 구매",
                description = "개인 소유 가구 구매",
                minAge = 19,
            },
            new CityAchievement
            {
                id = "placement",
                title = "첫 가구 배치",
                description = "실제 개인 집에 구매 가구 설치",
                minAge = 19,
            },
            new CityAchievement
            {
                id = "move",
                title = "첫 이사",
                description = "기존 비용·조건에 따른 주거지 이동",
                minAge = 19,
            },
            new CityAchievement
            {
                id = "investment",
                title = "첫 투자",
                description = "기존 증권 시스템으로 실제 매수",
                minAge = 19,
            },
            new CityAchievement
            {
                id = "day",
                title = "첫 하루 완주",
                description = "게임 날짜가 다음 날로 진행",
            },
            new CityAchievement
            {
                id = "discovery",
                title = "동네 알아가기",
                description = "서로 다른 시설 3곳 발견",
                minAge = 7,
                target = 3,
            },
        }
            .Concat(ItemAchievementDefinitions.All)
            .ToArray();

        public CityLifeService(LifeWorldService owner)
        {
            world = owner;
            game = owner.Game;
            transitions = GameContext.Instance == null ? null : GameContext.Instance.transitions;
            if (transitions != null)
            {
                transitions.SceneChanged += SceneEntered;
            }

            game.ActionPerformed += Action;
            game.economy.ActivityProduced += Activity;
            game.economy.TransactionCompleted += Money;
            game.economy.Changed += ObserveTax;
            game.clock.DayAdvanced += Day;
            game.stats.Changed += Invalidate;
            game.growth.Changed += Invalidate;
            world.Restored += Restored;
        }

        public void Dispose()
        {
            if (transitions != null)
            {
                transitions.SceneChanged -= SceneEntered;
            }

            game.ActionPerformed -= Action;
            game.economy.ActivityProduced -= Activity;
            game.economy.TransactionCompleted -= Money;
            game.economy.Changed -= ObserveTax;
            game.clock.DayAdvanced -= Day;
            game.stats.Changed -= Invalidate;
            game.growth.Changed -= Invalidate;
            world.Restored -= Restored;
        }

        bool Active => game.State == GameState.Playing || game.State == GameState.Resolving;

        public long Counter(string id) => Data.counters.FirstOrDefault(c => c.id == id)?.value ?? 0;

        public void Record(string id, long amount = 1, string receipt = null)
        {
            if (!Active || amount <= 0)
            {
                return;
            }

            var rule = Catalog.FirstOrDefault(c => c.id == id);
            if (rule != null && game.growth.Age < rule.minAge)
            {
                return;
            }

            if (receipt != null)
            {
                if (Data.receipts.Contains(receipt))
                {
                    return;
                }

                Data.receipts.Add(receipt);
                if (Data.receipts.Count > 2048)
                {
                    Data.receipts.RemoveAt(0);
                }
            }

            var counter = Data.counters.FirstOrDefault(c => c.id == id);
            if (counter == null)
            {
                counter = new CityCounter { id = id };
                Data.counters.Add(counter);
            }

            counter.value = Math.Min(1000000000, counter.value + Math.Min(amount, 1000000000));
            if (rule != null && counter.value >= rule.target && !Data.achievements.Contains(id))
            {
                Data.achievements.Add(id);
                var ruby = ReleaseConfig.Current;
                int award = ruby == null ? 0 : ruby.Reward(RubySource.Achievement, id);
                if (award > 0)
                {
                    game.economy.Ruby.Credit("milestone/achievement/" + id, award, 0, RubySource.Achievement, id);
                }

                notices.Enqueue("도전과제 · " + rule.title);
            }

            Touch();
        }

        void Touch()
        {
            dirty = true;
            saveAfter = Time.unscaledTime + .5f;
            Changed?.Invoke();
        }

        void Invalidate()
        {
            if (!string.IsNullOrEmpty(Data.destination) && !GrowthAccess.Zone(game, Data.destination, out _))
            {
                Data.destination = "";
                dirty = true;
            }

            Changed?.Invoke();
        }

        void Restored()
        {
            notices.Clear();
            walked = 0;
            observedHour = -1;
            friendHintMinute = -1;
            if (!string.IsNullOrEmpty(Data.destination) && !GrowthAccess.Zone(game, Data.destination, out _))
            {
                Data.destination = "";
            }

            currentRoom = null;
            observed = false;
            dirty = false;
            Changed?.Invoke();
        }

        public void Tick(LifeWorldController host)
        {
            if (!Active || host == null || host.Room == null || game.BirthIntroductionPending)
            {
                return;
            }

            MigrateLessons();
            if (currentRoom != host.Room.roomId)
            {
                currentRoom = host.Room.roomId;
                observed = false;
                EnterRoom(currentRoom);
            }

            if (Data.followingFirstFriend)
            {
                int minute = (int)(game.clock.SubDaySeconds / 60);
                if (minute != friendHintMinute)
                {
                    friendHintMinute = minute;
                    if (game.missions.ActiveMission?.Id != "M014" || game.missions.Counter(MissionCounter.FriendsMade) > 0)
                    {
                        Track("", out _);
                    }
                    else
                    {
                        game.phase23.Social.IntroductionHint(out var room);
                        if (room != null && room != Data.destination)
                        {
                            Track(room, out _, true);
                        }
                    }
                }
            }

            var p = host.Player.Position;
            if (
                observed
                && host.CanControl
                && host.Player.MovementAllowed
                && host.Player.ActualSpeed > .06f
                && Vector2.Distance(lastPosition, p) > .001f
                && Counter("walk") == 0
            )
            {
                Record("walk");
            }

            ObserveGuideMovement(host, p);
            observed = true;
            lastPosition = p;
            if (
                dirty
                && game.autoSaveAfterAction
                && game.State == GameState.Playing
                && !host.Busy
                && !GameContext.Instance.transitions.IsTransitioning
                && Time.unscaledTime >= saveAfter
            )
            {
                dirty = false;
                if (!game.SaveAutomaticCheckpoint(out _))
                {
                    dirty = true;
                    saveAfter = Time.unscaledTime + 5;
                }
            }

            if (
                (!dirty || !game.autoSaveAfterAction)
                && !host.Busy
                && !GameContext.Instance.transitions.IsTransitioning
                && notices.Count > 0
                && Time.unscaledTime >= nextNotice
            )
            {
                nextNotice = Time.unscaledTime + 2;
                Notice?.Invoke(notices.Dequeue());
            }
        }

        void SceneEntered(SceneId id)
        {
            var r = world.rooms?.Find(id);
            if (r != null && world.Host != null && world.Host.Room != null && world.Host.Room.roomId == r.roomId)
            {
                observed = false;
                currentRoom = r.roomId;
                EnterRoom(currentRoom);
            }
        }

        void EnterRoom(string id)
        {
            if (game.State != GameState.Playing)
            {
                return;
            }

            if (
                world.rules.Zone(id)?.outdoor == true
                && game.growth.IndependentOutdoorUnlocked
                && Counter("outing") == 0
            )
            {
                Record("outing");
            }

            EnteredForGuide(id);
            if (!Data.discovered.Contains(id))
            {
                Data.discovered.Add(id);
                if (new[] { "shop", "school", "workplace", "finance", "realestate" }.Contains(id))
                {
                    Record("visit/" + id);
                    Record("discovery");
                    notices.Enqueue("장소 발견 · " + world.rules.Zone(id).title + "\n" + FacilityPurpose(id));
                }

                Touch();
            }

            MarkGuide("room/" + id);
        }

        public bool Seen(string id) => Data.guideSeen.Contains(id);

        public void MarkGuide(string id)
        {
            if (!Data.guideSeen.Contains(id))
            {
                Data.guideSeen.Add(id);
                Touch();
            }
        }

        public void ResetUXGuides()
        {
            Data.guideSeen.RemoveAll(id => id.StartsWith("ux/tutorial/") || id.StartsWith(GuidePrefix));
            Data.guideSeen.Add(NewGuide);
            walked = 0;
            Touch();
        }

        public bool Track(string room, out string reason, bool followFirstFriend = false)
        {
            reason = null;
            if (string.IsNullOrEmpty(room))
            {
                Data.followingFirstFriend = false;
                Data.destination = "";
                Touch();
                return true;
            }

            if (world.rooms?.Find(room) == null || !GrowthAccess.Zone(game, room, out reason))
            {
                return false;
            }

            Data.destination = room;
            Data.followingFirstFriend = followFirstFriend;
            Touch();
            return true;
        }

        public void ShowSuggestions(bool show)
        {
            Data.suggestionsHidden = !show;
            Touch();
        }

        void Action(int action)
        {
            if (!Active)
            {
                return;
            }

            if ((action == 9 || action == 18 || action == 19) && game.growth.IndependentOutdoorUnlocked)
            {
                Learn(GuideLesson.Social);
            }

            if (action == 3)
            {
                Record("meal");
            }

            if (action == 0)
            {
                Record("work");
            }

            if (world.State.zone == "school" && new[] { 7, 8, 10, 11, 12, 13, 14, 15, 16, 17 }.Contains(action))
            {
                Record("study");
            }

            if (
                new[] { 9, 18, 19 }.Contains(action)
                && game.missions.Capture().counters.Any(c => c.counter == MissionCounter.FriendsMade && c.value > 0)
            )
            {
                Record("friend");
            }
        }

        void Activity(MissionActivityEvent e)
        {
            if (e.counter == MissionCounter.FurnitureBought)
            {
                Learn(GuideLesson.Furniture);
                Record("furniture", e.amount, e.id);
                Record("purchase", 1, e.id + "/purchase");
            }
        }

        void Money(int before, int after, string reason)
        {
            if (!Active)
            {
                return;
            }

            var last = game.economy.Capture().finance?.transactions.LastOrDefault();
            if (last == null)
            {
                return;
            }

            if (last.kind == TransactionKind.Salary)
            {
                StartLesson(GuideLesson.Salary);
                Record("salary", 1, last.id + "/city-salary");
                Record("work", 1, last.id + "/city-work");
            }

            if (last.kind == TransactionKind.StockBuy)
            {
                Record("investment", 1, last.id + "/city-stock");
            }
        }

        void Day(int day)
        {
            if (Active)
            {
                Record("day", 1, "day/" + day);
            }
        }

        public string Describe(WorldInteractable o, string input = "E")
        {
            if (o == null)
            {
                return null;
            }

            if (o.kind != WorldInteractionKind.Door)
            {
                string verb =
                    o.kind == WorldInteractionKind.Fridge ? "열기"
                    : o.kind == WorldInteractionKind.Sleep ? (game.growth.IndependentOutdoorUnlocked ? "잠자기" : "돌봄 선택")
                    : o.kind == WorldInteractionKind.Meal ? "식사"
                    : o.kind == WorldInteractionKind.Drink ? "물 마시기"
                    : o.kind == WorldInteractionKind.Study ? "공부"
                    : o.kind == WorldInteractionKind.Work ? "근무·채용"
                    : o.kind == WorldInteractionKind.NPC ? "이야기"
                    : "사용";
                return "[" + input + "] " + (verb is "사용" or "열기" ? o.title + " " : "") + verb;
            }

            if (!GrowthAccess.Interaction(game, o, -2, out var denied))
            {
                return o.title + "\n" + denied;
            }

            if (o.TryGetComponent<WorldDoor>(out var door) && !door.CanEnter("player", world.Hour, out var reason))
            {
                return o.title + "\n" + reason;
            }

            return input == "사용" || input == "터치" ? o.title : "[" + input + "] " + o.title;
        }

        public CitySuggestion Suggest()
        {
            return Suggestions()[0];
        }

        public static bool Valid(CityLifeProgress d) =>
            d == null
            || d.schema == 1
                && d.discovered != null
                && d.achievements != null
                && d.guideSeen != null
                && d.receipts != null
                && d.counters != null
                && d.discovered.Count <= 128
                && d.guideSeen.Count <= 512
                && d.receipts.Count <= 2048
                && d.achievements.Count <= 64
                && d.counters.Count <= 128
                && d.counters.Select(c => c?.id).Distinct().Count() == d.counters.Count
                && d.counters.All(c =>
                    c != null && !string.IsNullOrWhiteSpace(c.id) && c.value >= 0 && c.value <= 1000000000
                )
                && new[] { d.discovered, d.achievements, d.guideSeen, d.receipts }.All(list =>
                    list.All(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 160)
                    && list.Distinct().Count() == list.Count
                );
    }
}
