using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IanGame
{
    [DefaultExecutionOrder(-50)]
    public sealed class LifeWorldController : MonoBehaviour
    {
        public WorldPlayerMotor Player;
        public WorldCameraController follow;
        public WorldHUD hud;
        public WorldInteractable[] objects;
        public WorldTransitionDirector transitions;
        public GameObject upgradedHome;
        public GameObject starterWritingDesk;
        public Transform furnishingRoot;
        public WorldLifeAudio audio;
        public LifeWorldService Service { get; private set; }
        public WorldZoneDefinition CurrentZone { get; private set; }

        public bool lowDetail;
        public bool CanCancelAction => acting && !cancel;
        public bool Busy => acting || travelling;
        public bool CanControl =>
            !DeveloperInputLock.Active
            && !(Player.controlLocks?.Locked ?? false)
            && Service != null
            && (Room == null || Room.roomId == Service.State.zone)
            && !Busy
            && Service.Game.State == GameState.Playing
            && !Service.Game.BirthIntroductionPending
            && hud.shell.Depth == 0
            && !Player.TextInputFocused
            && !GameContext.Instance.transitions.IsTransitioning;
        public WorldInteractable Nearest { get; private set; }

        readonly List<WorldInteractable> furniture = new List<WorldInteractable>();
        internal IEnumerable<WorldInteractable> Interactables => objects.Concat(furniture);
        string furnitureKey;
        public int FurnitureRevision { get; private set; }
        bool acting;
        bool travelling;
        bool cancel;
        int restoreGeneration;
        Vector2 activityEntry;
        Vector2 activityCheckpoint;
        public WorldInteractable ConversationPartner { get; private set; }
        Vector2 transitionEntry;
        bool beforePortalCommit;
        bool started;
        bool subscribed;
        public RoomSceneIdentity Room => GetComponent<RoomSceneIdentity>();

        float nextLight;
        string restoreZone;
        Vector2 restoredPosition;
        readonly Dictionary<SpriteRenderer, Color> colors = new Dictionary<SpriteRenderer, Color>();
        public SpriteRenderer[] lamps;
        public WorldRoomBackdrop[] roomBackdrops = new WorldRoomBackdrop[0];

        void Awake()
        {
            if (GameContext.Instance == null)
            {
                return;
            }

            Service = GameContext.Instance.game.world;
            if (TryGetComponent<ResidentApartment>(out var apartment))
            {
                var unit = Room.catalog.Find(Service.State.zone);
                if (unit != null && unit.sceneName == gameObject.scene.name)
                {
                    apartment.Furnish(this, unit);
                }
            }

            FurnitureSeatPose.BindRoom(this);
            Service.Attach(this);
            if (Room != null && GameContext.Instance.transitions.IsTransitioning)
            {
                Player.GetComponent<Rigidbody2D>().simulated = false;
            }
        }

        void OnEnable()
        {
            if (started && Service != null)
            {
                Service.Attach(this);
                BindSignals();
                ApplySavedState();
            }
        }

        void BindSignals()
        {
            if (subscribed)
            {
                return;
            }

            Service.Restored += ApplySavedState;
            Service.Game.economy.Changed += RefreshFurniture;
            Service.Game.StateChanged += StateChanged;
            subscribed = true;
        }

        void Start()
        {
            if (Service == null)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Bootstrap");
                return;
            }

            started = true;
            BindSignals();
            foreach (var s in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (s != Player.sprite)
                {
                    colors[s] = s.color;
                }
            }

            ApplySavedState();
            RefreshFurniture();
        }

        void OnDisable()
        {
            if (Service == null)
            {
                return;
            }

            bool owner = Service.Host == this;
            if (owner && (Room == null || Room.roomId == Service.State.zone))
            {
                GetComponent<WorldNPCScheduler>()?.Capture();
            }

            subscribed = false;
            Service.Restored -= ApplySavedState;
            Service.Game.economy.Changed -= RefreshFurniture;
            Service.Game.StateChanged -= StateChanged;
            StopAllCoroutines();
            ConversationPartner = null;
            acting = travelling = false;
            if (Player != null)
            {
                Player.CancelTraversal();
            }

            transitions?.End();
            if (owner)
            {
                Service.State.pendingAction = "";
                Service.Game.clock.RealtimeSuppressed = Service.Game.idle.State.active;
            }

            if (Player != null)
            {
                Player.GetComponent<CircleCollider2D>().enabled = true;
                Player.Pose("Idle");
            }

            Service.Detach(this);
        }

        void StateChanged(GameState state)
        {
            if (state == GameState.Starting)
            {
                cancel = true;
            }
            else if (
                state == GameState.Playing
                && Room != null
                && Service.Game.BirthIntroductionPending
                && Service.Game.growth.Age == 0
            )
            {
                ApplySavedState();
            }
        }

        void Update()
        {
            if (Service == null || CurrentZone == null)
            {
                return;
            }

            Service.City?.Tick(this);
            Service.Game.clock.RealtimeSuppressed = Busy || Service.Game.idle.State.active || hud.shell.Depth > 0;
            Detect();
            if (CanControl && Player.InteractPressed && Nearest != null)
            {
                Use(Nearest);
            }

            if (Time.unscaledTime >= nextLight)
            {
                nextLight = Time.unscaledTime + .25f;
                Light();
            }
        }

        void Detect()
        {
            WorldInteractable best = null;
            float distance = float.MaxValue;
            if (CanControl)
            {
                foreach (var o in Interactables)
                {
                    if (o == null || !o.isActiveAndEnabled || o.zone != Service.State.zone)
                    {
                        continue;
                    }

                    float d = (Player.Position - o.Point).sqrMagnitude;
                    var toward = o.Point - Player.Position;
                    float facing = toward.sqrMagnitude > .001f ? Vector2.Dot(Player.Facing, toward.normalized) : 1;
                    float score = d + (1 - facing) * .18f - (o == Nearest ? .025f : 0);
                    if (d <= o.radius * o.radius && score < distance)
                    {
                        best = o;
                        distance = score;
                    }
                }
            }

            if (best == Nearest)
            {
                return;
            }

            if (Nearest != null)
            {
                Nearest.Select(false);
            }

            Nearest = best;
            if (best != null)
            {
                best.Select(true);
            }

            hud.Prompt(best == null ? null : Service.City.Describe(best, hud.InteractionInput));
        }

        public bool NearbyKind(WorldInteractionKind kind) => objects.Any(o => o.kind == kind && IsInReach(o));

        public bool NearbyNPC(string id) =>
            objects.Any(o => o.kind == WorldInteractionKind.NPC && o.npcId == id && IsInReach(o));

        public bool IsInReach(WorldInteractable o) =>
            o != null
            && o.isActiveAndEnabled
            && o.zone == Service.State.zone
            && Vector2.Distance(Player.Position, o.Point) <= o.radius + .05f;

        public bool Use(WorldInteractable o)
        {
            bool used = UseTarget(o);
            if (used && (hud.shell.Depth > 0 || o.kind == WorldInteractionKind.Wardrobe))
            {
                Service.City.UsedObject();
            }

            return used;
        }

        bool UseTarget(WorldInteractable o)
        {
            if (!CanControl || !IsInReach(o))
            {
                return false;
            }

            if (!GrowthAccess.Interaction(Service.Game, o, -2, out var denied))
            {
                hud.Notify(denied, 2);
                return false;
            }

            if (!GrowthAccess.Independent(Service.Game) && o.kind == WorldInteractionKind.Sleep)
            {
                hud.OpenContext(o);
                return true;
            }

            switch (o.kind)
            {
                case WorldInteractionKind.Door:
                    if (Busy)
                    {
                        return false;
                    }

                    var d = o.GetComponent<WorldDoor>();
                    if (d != null && !d.CanEnter("player", Service.Hour, out var lockedReason))
                    {
                        hud.Notify(lockedReason, 2);
                        return false;
                    }

                    if (Room && o.id == "npc-residence-front")
                    {
                        hud.OpenContext(o);
                        return true;
                    }

                    if (Room && o.id is "city-path" or "alley-path" or "platform" or "bus-stop" or "taxi-rank")
                    {
                        var travel = hud.GetComponent<GameplayUXPresenter>();
                        if (travel && travel.OfferTransport(o))
                        {
                            return true;
                        }
                    }

                    StartCoroutine(DoorTravel(o));
                    return true;
                case WorldInteractionKind.Fridge:
                case WorldInteractionKind.Work:
                case WorldInteractionKind.Study:
                case WorldInteractionKind.NPC:
                case WorldInteractionKind.FoodShop:
                    hud.OpenContext(o);
                    return true;
                case WorldInteractionKind.FurnitureShop:
                    hud.OpenHousing();
                    return true;
                case WorldInteractionKind.RealEstate:
                    var broker = FindFirstObjectByType<PropertyPresenter>();
                    if (broker == null)
                    {
                        return false;
                    }

                    broker.Open();
                    return true;
                case WorldInteractionKind.StockMarket:
                    return GameContext.Instance.transitions.Navigate(SceneId.StockMarket);
                case WorldInteractionKind.Wardrobe:
                    hud.Notify("검은 반팔 · 카고 바지 · 헤드폰\n공식 기본 복장을 착용 중입니다.");
                    return true;
                default:
                    return Begin(o);
            }
        }

        public int ActionFor(WorldInteractionKind kind) =>
            kind == WorldInteractionKind.Sleep ? 5
            : kind == WorldInteractionKind.Meal ? 3
            : kind == WorldInteractionKind.Drink ? 4
            : kind == WorldInteractionKind.Rest ? 1
            : -1;

        public bool TryLegacyAction(int action)
        {
            if (!GrowthAccess.Independent(Service.Game) && GrowthAccess.Care(action))
            {
                var bed = objects.FirstOrDefault(x =>
                    x.id == (FamilyLife.AtFamily(Service.Game) ? "family-bed" : "bed")
                );
                if (bed != null && IsInReach(bed))
                {
                    return Begin(bed, action);
                }

                hud.Notify("침대 가까이에서 돌봄 행동을 선택하세요.");
                return false;
            }

            WorldInteractionKind kind =
                action == 0 || action >= 20 ? WorldInteractionKind.Work
                : action >= 7 && action != 9 && action != 18 && action != 19 ? WorldInteractionKind.Study
                : action == 9 || action == 18 || action == 19 ? WorldInteractionKind.NPC
                : action == 5 ? WorldInteractionKind.Sleep
                : action == 3 ? WorldInteractionKind.Meal
                : action == 4 ? WorldInteractionKind.Drink
                : WorldInteractionKind.Rest;
            var o = Interactables.FirstOrDefault(x => x.kind == kind && IsInReach(x)
                && (kind != WorldInteractionKind.NPC || Service.Game.phase23.catalog.NPC(x.npcId) != null));
            if (o == null)
            {
                hud.Notify("해당 사물이나 사람에게 가까이 가서 [" + hud.InteractionInput + "]로 행동하세요.");
                return false;
            }

            return Begin(o, action);
        }

        public bool CanBegin(WorldInteractable o, int action, string food, out string reason)
        {
            reason = null;
            if (
                Service.Game.State != GameState.Playing
                || Service.Game.BirthIntroductionPending
                || Busy
                || (Player.controlLocks?.Locked ?? false)
                || !IsInReach(o)
            )
            {
                reason = "사물 가까이에서 정상 플레이 중에 사용하세요.";
                return false;
            }

            if (!GrowthAccess.Interaction(Service.Game, o, action, out reason))
            {
                return false;
            }

            if (
                o.kind == WorldInteractionKind.FoodShop
                && (Service.Hour < Service.rules.shopOpen || Service.Hour >= Service.rules.shopClose)
            )
            {
                reason = $"영업 시간 {Service.rules.shopOpen:00}:00–{Service.rules.shopClose:00}:00";
                return false;
            }

            if (o.kind == WorldInteractionKind.FoodShop && action == -3 && Service.State.fatigue > 90)
            {
                reason = "먼저 침대에서 충분히 쉬세요.";
                return false;
            }

            if (o.kind == WorldInteractionKind.FoodShop && action == 3 && Service.State.bread < 1)
            {
                reason = "먹을 빵이 없습니다. 빵을 구매하세요.";
                return false;
            }

            if (o.GetComponent<WorldLifeAction>() is WorldLifeAction quality && !quality.Available)
            {
                reason = "지금은 빈자리가 없습니다.";
                return false;
            }

            if (
                o.kind == WorldInteractionKind.Meal
                && FamilyLife.IsFamilyRoom(o.zone)
                && FamilyLife.State(Service.Game).meals < 1
            )
            {
                reason = "부모님이 식사를 준비하는 동안 기다리거나 돌봄을 선택하세요.";
                return false;
            }

            if (
                o.kind == WorldInteractionKind.Meal
                && !FamilyLife.IsFamilyRoom(o.zone)
                && string.IsNullOrEmpty(Service.State.prepared)
                && !(o.zone == "shop" && Service.State.bread > 0)
            )
            {
                reason = "먼저 냉장고에서 음식을 골라 준비하세요.";
                return false;
            }

            if (
                o.kind == WorldInteractionKind.Fridge
                && !FamilyLife.IsFamilyRoom(o.zone)
                && (
                    food != "rice" && food != "bread"
                    || food == "rice" && Service.State.rice < 1
                    || food == "bread" && Service.State.bread < 1
                    || !string.IsNullOrEmpty(Service.State.prepared)
                )
            )
            {
                reason = "음식이 없거나 이미 준비한 식사가 있습니다.";
                return false;
            }

            if (o.kind == WorldInteractionKind.NPC && !FamilyLife.Parent(o.npcId))
            {
                Service.Game.phase23.SelectedNpcId = o.npcId;
            }

            if (action >= 0 && !Service.Game.CanPerformAction(action, out reason))
            {
                return false;
            }

            return true;
        }

        public bool Begin(WorldInteractable o, int action = -2, string food = "")
        {
            if (action == -2)
            {
                action = ActionFor(o.kind);
            }

            string reason;
            if (!CanBegin(o, action, food, out reason))
            {
                hud.Notify(reason, 2);
                return false;
            }

            hud.CloseContext();
            while (hud.shell.Depth > 0)
            {
                int depth = hud.shell.Depth;
                hud.shell.Pop();
                if (hud.shell.Depth == depth)
                {
                    return false;
                }
            }

            Player.Facing =
                o.kind == WorldInteractionKind.Sleep
                    ? Vector2.down
                    : ((Vector2)o.transform.position - Player.Position).normalized;
            StartCoroutine(Activity(o, action, food));
            return true;
        }

        IEnumerator Activity(WorldInteractable o, int action, string food)
        {
            var interactionKind = o.kind;
            LocalDiagnostics.Record(DiagnosticEvent.InteractionStarted, interactionKind);
            if (interactionKind == WorldInteractionKind.NPC)
            {
                LocalDiagnostics.Record(DiagnosticEvent.DialogueStarted);
            }
            else if (interactionKind == WorldInteractionKind.Sleep)
            {
                LocalDiagnostics.Record(DiagnosticEvent.SleepStarted);
            }
            Player.ClearReaction();
            acting = true;
            string label = action == 3 ? "식사" : action == 4 ? "마시기" : action == 5 ? "잠자기" : o.title;
            hud.Prompt(null);
            hud.Activity(label, 0);
            cancel = false;
            int generation = restoreGeneration;
            activityEntry = Player.Position;
            activityCheckpoint = activityEntry;
            string sourceRoom = CurrentZone.id;
            string positionPolicy = "stay";
            Service.State.pendingAction = o.kind.ToString();
            var collider = Player.GetComponent<CircleCollider2D>();
            bool sleep = o.kind == WorldInteractionKind.Sleep && action == 5;
            string pose =
                sleep ? "Sleep"
                : o.kind == WorldInteractionKind.Drink ? "Drink"
                : o.kind == WorldInteractionKind.Study ? (action == 8 || action == 14 ? "Study" : "Use Computer")
                : o.kind == WorldInteractionKind.Meal || o.kind == WorldInteractionKind.FoodShop && action == 3 ? "Eat"
                : o.kind == WorldInteractionKind.Shower ? "Shower"
                : o.kind == WorldInteractionKind.Wash ? "Wash"
                : o.kind == WorldInteractionKind.Work ? "Work"
                : o.kind == WorldInteractionKind.Rest || o.kind == WorldInteractionKind.Toilet ? "Sit"
                : o.kind == WorldInteractionKind.NPC ? "Talk"
                : "Interact";
            var detail = o.GetComponent<WorldLifeAction>();
            var controlLease = Player.controlLocks?.Acquire("life-action");
            var conversation = o.kind == WorldInteractionKind.NPC ? o.GetComponent<WorldActorAnimation>() : null;
            Vector2 talkFacing = (Vector2)o.transform.position - Player.Position;
            try
            {
                if (conversation != null)
                {
                    ConversationPartner = o;
                    Player.Face(talkFacing);
                }

                if (detail != null)
                {
                    yield return detail.Enter(this, action, () => cancel || generation != restoreGeneration);
                    if (!detail.Ready)
                    {
                        yield break;
                    }

                    if (detail.standUp && Player.MovementAllowed && detail.UsesExitAnchor)
                    {
                        activityCheckpoint = detail.standUp.position;
                        positionPolicy = "object exit";
                    }

                    if (!string.IsNullOrEmpty(detail.activePose))
                    {
                        pose = detail.activePose;
                    }
                }

                if (detail == null && (o.kind == WorldInteractionKind.Work || o.kind == WorldInteractionKind.Study))
                {
                    Player.Pose("Sit");
                    yield return new WaitForSecondsRealtime(.45f);
                    if (generation != restoreGeneration)
                    {
                        acting = false;
                        yield break;
                    }
                }

                if (
                    Player.growthVisual?.Young == true
                    && !sleep
                    && (o.kind == WorldInteractionKind.Sleep || FamilyLife.Parent(o.npcId) && action is 3 or 4)
                )
                {
                    pose = Player.growthVisual.CarePose(action);
                }

                Player.Pose(pose);
                if (o.activityEffect != null)
                {
                    o.activityEffect.SetActive(true);
                }

                float duration = Mathf.Max(
                    .1f,
                    Service.rules.activityAnimationScale
                        * (
                            sleep ? 3.6f
                            : action == 0 ? 5
                            : o.kind == WorldInteractionKind.Shower ? 3.2f
                            : 1.7f
                        )
                );
                bool newbornCare =
                    Service.Game.growth.Age == 0
                    && (o.id == "family-bed" || FamilyLife.Parent(o.npcId))
                    && action is 3 or 4 or 5;
                if (newbornCare)
                {
                    duration = Mathf.Min(duration, sleep ? 1.2f : .7f);
                }

                for (float t = 0; t < duration; )
                {
                    if (cancel || !o.gameObject.activeInHierarchy)
                    {
                        yield break;
                    }

                    if (Service.Game.State == GameState.Paused)
                    {
                        yield return null;
                        continue;
                    }

                    t += Time.unscaledDeltaTime;
                    detail?.Animate(t);
                    if (o.activityEffect != null)
                    {
                        for (int i = 0; i < o.activityEffect.transform.childCount; i++)
                        {
                            o.activityEffect.transform.GetChild(i).localPosition = new Vector3(
                                -.3f + i * .15f,
                                1.6f - Mathf.Repeat(t * 2 + i * .2f, .8f),
                                -.01f
                            );
                        }
                    }

                    hud.Activity(label, t / duration);
                    if (sleep)
                    {
                        hud.Dim(Mathf.Sin(t / duration * Mathf.PI) * .16f);
                    }

                    yield return null;
                }

                Service.State.pendingAction = "";
                string message;
                bool ok = Service.Durable(
                    () =>
                    {
                        if (!GrowthAccess.Interaction(Service.Game, o, action, out _))
                        {
                            return false;
                        }

                        if (o.kind == WorldInteractionKind.FoodShop && action == -3)
                        {
                            int reward = Service.rules.neighbourhoodHelpReward;
                            if (!Service.Game.economy.Earn(reward, "동네 가게 정리"))
                            {
                                return false;
                            }

                            Service.Game.clock.AdvanceSimulation(Service.rules.neighbourhoodHelpMinutes * 60);
                            Service.State.dailyIncome += reward;
                            Service.State.fatigue = Mathf.Min(100, Service.State.fatigue + 4);
                            return true;
                        }

                        if (o.kind == WorldInteractionKind.FoodShop && action == 3)
                        {
                            if (Service.State.bread < 1)
                            {
                                return false;
                            }

                            Service.State.bread--;
                        }

                        if (o.kind == WorldInteractionKind.Meal)
                        {
                            if (FamilyLife.IsFamilyRoom(o.zone))
                            {
                                var f = FamilyLife.State(Service.Game);
                                if (f.meals < 1)
                                {
                                    return false;
                                }

                                f.meals--;
                            }
                            else if (!string.IsNullOrEmpty(Service.State.prepared))
                            {
                                Service.State.prepared = "";
                            }
                            else if (o.zone == "shop" && Service.State.bread > 0)
                            {
                                Service.State.bread--;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        if (o.kind == WorldInteractionKind.Fridge)
                        {
                            if (FamilyLife.IsFamilyRoom(o.zone))
                            {
                                var f = FamilyLife.State(Service.Game);
                                if (f.pantry < 1)
                                {
                                    return false;
                                }

                                f.pantry--;
                                Service.State.prepared = food == "rice" ? "rice" : "bread";
                                Service.Game.clock.AdvanceSimulation(5 * 60);
                                return true;
                            }

                            if (food == "rice")
                            {
                                if (Service.State.rice < 1)
                                {
                                    return false;
                                }

                                Service.State.rice--;
                            }
                            else
                            {
                                if (Service.State.bread < 1)
                                {
                                    return false;
                                }

                                Service.State.bread--;
                            }

                            Service.State.prepared = food;
                            Service.Game.clock.AdvanceSimulation(food == "rice" ? 20 * 60 : 5 * 60);
                            return true;
                        }

                        if (FamilyLife.Parent(o.npcId) && action < 0)
                        {
                            var f = FamilyLife.State(Service.Game);
                            if (o.npcId == "family-mother")
                            {
                                f.motherRelationship = Mathf.Min(100, f.motherRelationship + 1);
                            }
                            else
                            {
                                f.fatherRelationship = Mathf.Min(100, f.fatherRelationship + 1);
                            }

                            Service.Game.clock.AdvanceSimulation(5 * 60);
                            return true;
                        }

                        if (action == -4)
                        {
                            Service.Game.clock.AdvanceSimulation(20 * 60);
                            Service.State.hygiene = Mathf.Min(100, Service.State.hygiene + 65);
                            Service.Game.stats.ApplyCore(new StatDelta { mental = 2 });
                            return true;
                        }

                        if (action >= 0)
                        {
                            return Service.CommitAction(action);
                        }

                        double seconds =
                            o.kind == WorldInteractionKind.Shower ? 20 * 60
                            : o.kind == WorldInteractionKind.Toilet ? 8 * 60
                            : 5 * 60;
                        Service.Game.clock.AdvanceSimulation(seconds);
                        Service.State.hygiene = Mathf.Clamp(
                            Service.State.hygiene
                                + (
                                    o.kind == WorldInteractionKind.Shower ? 65
                                    : o.kind == WorldInteractionKind.Wash ? 15
                                    : 5
                                ),
                            0,
                            100
                        );
                        Service.Game.stats.ApplyCore(new StatDelta { mental = 2 });
                        return true;
                    },
                    out message
                );
                if (ok)
                {
                    if (action == -1 && FamilyLife.Parent(o.npcId) && Service.Game.growth.IndependentOutdoorUnlocked)
                    {
                        Service.City.Learn(GuideLesson.Social);
                    }

                    Service.City.UsedObject();
                }

                hud.Notify(ok ? (sleep ? "기상했습니다. " + Service.State.lastSummary : o.title + " 완료") : message);
                if (!sleep || detail == null)
                {
                    Player.Pose(sleep ? "Wake Up" : "Idle");
                }
                if (detail != null)
                {
                    yield return detail.Exit(this, () => cancel || generation != restoreGeneration);
                }

                if (
                    detail == null
                    && (
                        o.kind == WorldInteractionKind.Work
                        || o.kind == WorldInteractionKind.Study
                        || o.kind == WorldInteractionKind.Rest
                    )
                )
                {
                    Player.Pose("Stand Up");
                    yield return new WaitForSecondsRealtime(.45f);
                }
            }
            finally
            {
                ConversationPartner = null;
                detail?.ResetVisual();
                if (o != null && o.activityEffect != null)
                {
                    o.activityEffect.SetActive(false);
                }

                collider.enabled = true;
                if (generation != restoreGeneration)
                {
                    positionPolicy = "save restore";
                }
                else if (detail && !detail.Completed && !Clear(Player.Position) && Player.Position != activityEntry)
                {
                    Player.Warp(LocalExit(activityEntry, Player.Position));
                    positionPolicy = "local return";
                }

                Player.Pose("Idle");
                controlLease?.Dispose();
                Service.State.pendingAction = "";
                acting = false;
                CapturePosition();
                if (interactionKind == WorldInteractionKind.NPC)
                {
                    LocalDiagnostics.Record(DiagnosticEvent.DialogueEnded);
                }
                LocalDiagnostics.Record(DiagnosticEvent.InteractionEnded, interactionKind);
                TraceActivity(o, sourceRoom, activityEntry, positionPolicy);
                hud.Dim(0);
                hud.Activity(null, 0);
                Service.Game.clock.RealtimeSuppressed = Service.Game.idle.State.active || hud.shell.Depth > 0;
            }
        }

        Vector2 LocalExit(Vector2 preferred, Vector2 current)
        {
            if (Clear(preferred))
            {
                return preferred;
            }

            if (TryNearbyFloor(CurrentZone, preferred, out var nearby))
            {
                return nearby;
            }

            return current;
        }

        bool TryNearbyFloor(WorldZoneDefinition zone, Vector2 center, out Vector2 floor)
        {
            const int directions = 16;
            for (int ring = 1; ring <= 6; ring++)
            {
                for (int i = 0; i < directions; i++)
                {
                    float angle = i * Mathf.PI * 2 / directions;
                    floor = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (ring * .16f);
                    Vector2 local = floor - zone.origin;
                    if (Mathf.Abs(local.x) <= zone.size.x / 2 - .6f && local.y >= -zone.size.y / 2 + .6f
                        && local.y <= Mathf.Min(zone.size.y / 2 - .6f, zone.walkableTop - .65f) && Clear(floor))
                    {
                        return true;
                    }
                }
            }

            floor = center;
            return false;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        void TraceActivity(WorldInteractable source, string room, Vector2 entry, string policy)
        {
            bool changedRoom = room != CurrentZone.id;
            string trace = $"Interaction {source?.id} in {room}, {entry} -> {Player.Position}, "
                + $"position policy={policy}, scene transition={changedRoom}";
            if (policy == "stay" && !changedRoom && Vector2.Distance(entry, Player.Position) > .5f)
            {
                Debug.LogWarning(trace, this);
            }
            else
            {
                Debug.Log(trace, this);
            }
        }

        public void CancelAction()
        {
            if (acting)
            {
                cancel = true;
                hud.Notify("행동 취소 · 시간과 보상은 처리하지 않았습니다.", 2);
            }
        }

        public bool PlacementClear(string id, Vector2 local, out string reason)
        {
            reason = null;
            var rule = Service.Game.economy.housingCatalog.placementPlan?.Rule(id);
            if (rule == null)
            {
                return true;
            }

            var home = Service.rules.Zone("home");
            Physics2D.SyncTransforms();
            Vector2 p = home.origin + local;
            foreach (var col in Physics2D.OverlapBoxAll(p + Vector2.up * (rule.footprint.y / 2), rule.footprint, 0))
            {
                if (col.isTrigger)
                {
                    continue;
                }

                var owner = col.GetComponentInParent<WorldInteractable>();
                if (owner != null && owner.id == id && owner.name.StartsWith("Owned furniture"))
                {
                    continue;
                }

                reason = "이안이나 다른 가구와 겹치는 위치입니다.";
                return false;
            }

            foreach (var col in Physics2D.OverlapCircleAll(p + rule.approach, .3f))
            {
                if (col.isTrigger || col.attachedRigidbody == Player.GetComponent<Rigidbody2D>())
                {
                    continue;
                }

                var owner = col.GetComponentInParent<WorldInteractable>();
                if (owner != null && owner.id == id && owner.name.StartsWith("Owned furniture"))
                {
                    continue;
                }

                reason = "가구 앞에 접근할 수 있는 공간이 필요합니다.";
                return false;
            }

            return true;
        }

        IEnumerator StepTo(Vector2 point, float limit = .8f)
        {
            const float speed = 2.5f;
            float approachSpeed = speed * (Player.growthVisual ? Player.growthVisual.Speed : 1);
            float distance = Vector2.Distance(Player.Position, point);
            // A child entering at the edge of the interaction radius needs time to accelerate to the threshold.
            limit = Mathf.Max(limit, distance / Mathf.Max(.1f, approachSpeed) + .35f);
            Player.BeginTraversal(point, speed);
            while (Player.IsTraversing && Player.TraversalSeconds < limit)
            {
                yield return null;
            }

            if (Player.IsTraversing)
            {
                Player.CancelTraversal();
            }
        }

        public void VisitApartment(WorldInteractable entrance, RoomSceneDefinition apartment)
        {
            hud.CloseContext();
            if (CanControl && entrance.id == "npc-residence-front" && IsInReach(entrance)
                && apartment.sceneName == "Resident_Housing" && GrowthAccess.Zone(Service.Game, apartment.roomId, out _))
            {
                StartCoroutine(DoorTravel(entrance, apartment.roomId));
            }
        }

        public IEnumerator WalkToTransit(WorldInteractable stop)
        {
            if (!CanControl || !stop || stop.zone != Room.roomId)
            {
                yield break;
            }
            int generation = restoreGeneration;
            travelling = true;
            try
            {
                var path = new WorldNPCNavigation().Find(this, Room.roomId, Player.Position, stop.Point);
                foreach (var waypoint in path)
                {
                    if (restoreGeneration != generation || Service.Game.State != GameState.Playing)
                    {
                        yield break;
                    }
                    yield return StepTo(waypoint);
                    if (Vector2.Distance(Player.Position, waypoint) > .25f)
                    {
                        yield break;
                    }
                }
            }
            finally
            {
                if (generation == restoreGeneration)
                {
                    Player.CancelTraversal();
                    travelling = false;
                }
            }
        }

        public void WalkThrough(WorldInteractable entrance)
        {
            if (entrance && CanControl && IsInReach(entrance) && !Busy
                && (entrance.id == "city-path" || entrance.id == "alley-path"))
            {
                StartCoroutine(DoorTravel(entrance));
            }
        }

        IEnumerator DoorTravel(WorldInteractable portal, string apartment = null)
        {
            travelling = true;
            int generation = restoreGeneration;
            beforePortalCommit = true;
            transitionEntry = Player.Position;
            var door = portal.GetComponent<WorldDoor>();
            WorldDoor arrivalDoor = null;
            bool committed = false;
            try
            {
                // A room portal always opens from the playable side of its facade.
                // A lateral approach near the jamb can otherwise select the far side of the closed leaf.
                bool roomDoor = door != null && portal.zone != portal.targetZone;
                Vector2 approachSide = roomDoor ? portal.Point : Player.Position;
                var approach = door == null ? portal.Point : door.Approach(approachSide);
                var crossing = door == null ? portal.Point : door.Passage(approachSide);
                if (door != null && !roomDoor)
                {
                    crossing = door.Plane - door.normal * door.SideOf(approachSide) * .85f;
                }

                hud.CloseContext();
                if (roomDoor)
                {
                    Vector2 side = door.normal * door.SideOf(approachSide);
                    float depth = Vector2.Dot(Player.Position - door.Plane, side);
                    float lateral = Mathf.Abs(Vector2.Dot(Player.Position - door.Plane, Vector2.Perpendicular(side)));
                    if (lateral > .55f && depth < .65f)
                    {
                        // Clear the near corner before moving across the width of the doorway.
                        yield return StepTo(Player.Position + side * (.65f - depth));
                    }
                }

                yield return StepTo(approach);
                if (Vector2.Distance(Player.Position, approach) > .25f)
                {
                    hud.Notify("문 앞의 통로를 비워 주세요.");
                    yield break;
                }

                Player.Face((Vector2)portal.transform.position - Player.Position);
                Player.Pose("Open Door");
                if (door != null)
                {
                    if (!door.Acquire("player", Service.Hour, out var reason))
                    {
                        hud.Notify(reason, 2);
                        yield break;
                    }

                    float openedAt =
                        Time.unscaledTime
                        + Mathf.Max(
                            .32f,
                            Player.advanced == null ? 0 : Player.advanced.Duration(ActorMotion.OpenDoor, Player.Facing)
                        );
                    while (door.State != DoorState.Open || Time.unscaledTime < openedAt)
                    {
                        if (generation != restoreGeneration)
                        {
                            yield break;
                        }

                        yield return null;
                    }

                    Player.Pose("Idle");
                    yield return StepTo(crossing, .65f);
                    if (!Player.TraversalArrived)
                    {
                        hud.Notify("문을 통과할 공간이 필요합니다.");
                        yield break;
                    }
                }

                if (generation != restoreGeneration)
                {
                    yield break;
                }

                if (Room != null && portal.zone != portal.targetZone)
                {
                    beforePortalCommit = false;
                    committed = true;
                    GetComponent<WorldNPCScheduler>()?.Capture();
                    bool accepted = GameContext.Instance.transitions.NavigateRoom(
                        apartment ?? portal.targetZone,
                        portal.arrival,
                        Player.Facing,
                        Room.catalog.portals.FirstOrDefault(p => p.sourceRoom == portal.zone && p.doorId == portal.id)?.destinationEntryId ?? ""
                    );
                    if (accepted)
                    {
                        Service.City.UsedObject();
                    }

                    yield break;
                }

                if (portal.zone == portal.targetZone)
                {
                    beforePortalCommit = false;
                    committed = true;
                    Service.City.UsedObject();
                    CapturePosition();
                    Service.Game.SaveAutomaticCheckpoint(out _);
                    yield break;
                }

                string from = Service.State.zone;
                transitions?.Begin(from, portal.targetZone);
                for (float t = 0; t < .16f; t += Time.unscaledDeltaTime)
                {
                    if (generation != restoreGeneration)
                    {
                        yield break;
                    }

                    if (transitions != null)
                    {
                        transitions.Cover(t / .16f);
                    }
                    else
                    {
                        hud.Dim(t / .16f);
                    }

                    yield return null;
                }

                arrivalDoor = objects
                    .Where(o =>
                        o.zone == portal.targetZone && o.targetZone == from && o.kind == WorldInteractionKind.Door
                    )
                    .Select(o => o.GetComponent<WorldDoor>())
                    .FirstOrDefault(d => d != null);
                if (arrivalDoor != null)
                {
                    if (!arrivalDoor.Acquire("player", Service.Hour, out var reason))
                    {
                        hud.Notify(reason, 2);
                        yield break;
                    }

                    while (arrivalDoor.State != DoorState.Open)
                    {
                        if (generation != restoreGeneration)
                        {
                            yield break;
                        }

                        yield return null;
                    }
                }

                if (transitions != null)
                {
                    transitions.Cover(1);
                }
                else
                {
                    hud.Dim(1);
                }

                yield return null;
                CommitZone(portal.targetZone, portal.arrival);
                Service.City.UsedObject();
                beforePortalCommit = false;
                committed = true;
                door?.Release("player");
                Player.Pose("Idle");
                for (float t = 0; t < .2f; t += Time.unscaledDeltaTime)
                {
                    if (generation != restoreGeneration)
                    {
                        yield break;
                    }

                    if (transitions != null)
                    {
                        transitions.Cover(1 - t / .2f);
                    }
                    else
                    {
                        hud.Dim(1 - t / .2f);
                    }

                    yield return null;
                }

                CapturePosition();
                string error;
                if (!Service.Game.SaveAutomaticCheckpoint(out error))
                {
                    hud.Notify("이동 상태 저장 실패 · 저장 메뉴에서 다시 시도하세요.");
                }
            }
            finally
            {
                door?.Release("player");
                arrivalDoor?.Release("player");
                Player.CancelTraversal();
                Player.Pose("Idle");
                transitions?.End();
                hud.Dim(0);
                beforePortalCommit = false;
                travelling = false;
                if (!committed && generation == restoreGeneration)
                {
                    Player.Warp(Safe(CurrentZone, transitionEntry - CurrentZone.origin));
                }
            }
        }

        public bool Travel(string zone)
        {
            if (Room != null)
            {
                var room = Room.catalog.Resolve(zone, Vector2.zero, Service.Game.growth.Age);
                if (room != null)
                {
                    return Travel(room.roomId, Service.rules.Zone(room.roomId).spawn);
                }
            }

            var z = Service.rules.Zone(zone);
            return z != null && Travel(zone, z.spawn);
        }

        public bool Travel(string zone, Vector2 arrival)
        {
            if (Room != null)
            {
                var room = Room.catalog.Resolve(zone, arrival, Service.Game.growth.Age);
                if (room == null)
                {
                    return false;
                }

                if (zone != room.roomId)
                {
                    arrival -= room.legacyOffset;
                }

                if (Busy)
                {
                    return false;
                }

                LocalDiagnostics.Record(DiagnosticEvent.SceneTransitionRequested);
                return GameContext.Instance.transitions.NavigateRoom(room.roomId, arrival, Player.Facing);
            }

            if (zone == FamilyLife.Bath)
            {
                zone = FamilyLife.Home;
                arrival = new Vector2(8, 2.3f);
            }

            if (!GrowthAccess.Zone(Service.Game, zone, out var denied))
            {
                hud.Notify(denied, 2);
                return false;
            }

            if (Busy || Service.Game.BirthIntroductionPending || Service.rules.Zone(zone) == null)
            {
                return false;
            }

            LocalDiagnostics.Record(DiagnosticEvent.SceneTransitionRequested);
            StartCoroutine(ChangeZone(zone, arrival));
            return true;
        }

        void CommitZone(string zone, Vector2 arrival)
        {
            Service.State.zone = zone;
            CurrentZone = Service.rules.Zone(zone);
            Player.Warp(Safe(CurrentZone, arrival));
            Service.State.position = Player.Position - CurrentZone.origin;
            follow.Snap();
            RefreshFurniture();
        }

        IEnumerator ChangeZone(string zone, Vector2 arrival)
        {
            travelling = true;
            int generation = restoreGeneration;
            transitionEntry = Player.Position;
            beforePortalCommit = true;
            hud.CloseContext();
            transitions?.Begin(Service.State.zone, zone);
            try
            {
                for (float t = 0; t < .16f; t += Time.unscaledDeltaTime)
                {
                    if (generation != restoreGeneration)
                    {
                        yield break;
                    }

                    if (transitions != null)
                    {
                        transitions.Cover(t / .16f);
                    }
                    else
                    {
                        hud.Dim(t / .16f);
                    }

                    yield return null;
                }

                if (transitions != null)
                {
                    transitions.Cover(1);
                }
                else
                {
                    hud.Dim(1);
                }

                yield return null;
                CommitZone(zone, arrival);
                beforePortalCommit = false;
                for (float t = 0; t < .16f; t += Time.unscaledDeltaTime)
                {
                    if (generation != restoreGeneration)
                    {
                        yield break;
                    }

                    if (transitions != null)
                    {
                        transitions.Cover(1 - t / .16f);
                    }
                    else
                    {
                        hud.Dim(1 - t / .16f);
                    }

                    yield return null;
                }

                CapturePosition();
                string error;
                if (!Service.Game.SaveAutomaticCheckpoint(out error))
                {
                    hud.Notify("이동 상태 저장 실패 · 저장 메뉴에서 다시 시도하세요.");
                }
            }
            finally
            {
                transitions?.End();
                hud.Dim(0);
                beforePortalCommit = false;
                travelling = false;
            }
        }

        public void CapturePosition()
        {
            if (CurrentZone == null || Service == null || Room != null && Room.roomId != Service.State.zone)
            {
                return;
            }

            Service.State.position =
                (
                    acting ? activityCheckpoint
                    : beforePortalCommit ? transitionEntry
                    : Player.Position
                ) - CurrentZone.origin;
            Service.State.facing = Player.Facing;
        }

        public void ApplySavedState()
        {
            if (Service == null)
            {
                return;
            }

            if (Room != null && Room.roomId != Service.State.zone)
            {
                StartCoroutine(ResumeSavedRoom());
                return;
            }

            restoreGeneration++;
            cancel = true;
            Player.ClearReaction();
            Player.Pose("Idle");
            Player.CancelTraversal();
            foreach (var door in GetComponentsInChildren<WorldDoor>(true))
            {
                door.ResetClosed();
            }

            transitions?.End();
            CurrentZone = Service.rules.Zone(Service.State.zone) ?? Service.rules.Zone("home");
            Service.State.zone = CurrentZone.id;
            RefreshFurniture();
            if (Service.Game.growth.Age == 0 && (CurrentZone.id == "home" || FamilyLife.IsNursery(CurrentZone.id)))
            {
                var bed = objects.FirstOrDefault(o =>
                    o.id == (FamilyLife.IsNursery(CurrentZone.id) ? "family-bed" : "bed")
                );
                if (bed != null)
                {
                    Service.State.position = bed.Point - CurrentZone.origin;
                }
            }

            Player.Warp(Safe(CurrentZone, Service.State.position));
            Service.State.position = Player.Position - CurrentZone.origin;
            Player.Facing = Service.State.facing;
            follow.Snap();
        }

        IEnumerator ResumeSavedRoom()
        {
            yield return null;
            var t = GameContext.Instance.transitions;
            if (!t.IsTransitioning && Service.Host == this)
            {
                t.NavigateRoom(Service.State.zone, Service.State.position, Service.State.facing);
            }
        }

        public Vector2 Safe(WorldZoneDefinition zone, Vector2 local)
        {
            local.x = Mathf.Clamp(local.x, -zone.size.x / 2 + .6f, zone.size.x / 2 - .6f);
            local.y = Mathf.Clamp(
                local.y,
                -zone.size.y / 2 + .6f,
                Mathf.Min(zone.size.y / 2 - .6f, zone.walkableTop - .65f)
            );
            Vector2 p = zone.origin + local;
            if (Clear(p))
            {
                return p;
            }

            if (TryNearbyFloor(zone, p, out var floor))
            {
                return floor;
            }

            p = zone.origin + zone.spawn;
            if (Clear(p))
            {
                return p;
            }

            for (int y = -4; y <= 4; y++)
            {
                for (int x = -6; x <= 6; x++)
                {
                    p = zone.origin + new Vector2(x, y);
                    if (Clear(p))
                    {
                        return p;
                    }
                }
            }

            return zone.origin;
        }

        bool Clear(Vector2 p)
        {
            var feet = Player.GetComponent<CircleCollider2D>();
            Vector2 offset = feet.transform.TransformVector(feet.offset);
            Vector3 scale = feet.transform.lossyScale;
            float radius = feet.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            foreach (var c in Physics2D.OverlapCircleAll(p + offset, radius))
            {
                if (!c.isTrigger && c.attachedRigidbody != feet.attachedRigidbody)
                {
                    return false;
                }
            }

            return true;
        }

        void RefreshFurniture()
        {
            if (Service == null || furnishingRoot == null || Room != null && Room.roomId != "home")
            {
                return;
            }

            var housing = Service.Game.economy.Housing;
            bool improved = housing.residenceId == "improved";
            bool legacy = housing.starterRevision == 0;
            if (upgradedHome != null)
            {
                upgradedHome.SetActive(legacy && improved);
            }

            if (starterWritingDesk != null)
            {
                starterWritingDesk.SetActive(legacy && !improved);
            }

            foreach (var o in objects)
            {
                if (o != null && o.zone == "home" && StarterHomePresentation.Optional(o.id))
                {
                    o.gameObject.SetActive(
                        legacy && (o != starterWritingDesk?.GetComponent<WorldInteractable>() || !improved)
                    );
                }
            }

            GetComponent<StarterHomePresentation>()?.Apply(legacy);
            string key = JsonUtility.ToJson(housing);
            if (key == furnitureKey)
            {
                return;
            }

            if (
                furnitureKey != null
                && Service.Game.State == GameState.Playing
                && furniture.Count != housing.placed.Count
            )
            {
                Player.React(furniture.Count > housing.placed.Count ? ActorMotion.PickUpItem : ActorMotion.PutDownItem);
            }

            furnitureKey = key;
            foreach (var previous in furniture)
            {
                if (previous != null)
                {
                    colors.Remove(previous.visual);
                    previous.gameObject.SetActive(false);
                    Destroy(previous.gameObject);
                }
            }

            furniture.Clear();
            var home = Service.rules.Zone("home");
            var catalog = Service.Game.economy.housingCatalog;
            foreach (
                var placement in housing.placed.Where(p =>
                    p.installed
                    && (string.IsNullOrEmpty(p.roomId) || p.roomId == "home")
                    && (string.IsNullOrEmpty(p.homeId) || p.homeId == "player")
                )
            )
            {
                var definition = catalog.Furniture(placement.id);
                if (definition == null || definition.preview == null)
                {
                    continue;
                }

                var rule = catalog.placementPlan?.Rule(placement.id);
                var go = new GameObject("Owned furniture / " + placement.id);
                go.transform.SetParent(furnishingRoot, false);
                go.transform.position = home.origin + placement.position * 2;
                var visual = new GameObject("Owned furniture sprite", typeof(SpriteRenderer));
                visual.transform.SetParent(go.transform, false);
                var renderer = visual.GetComponent<SpriteRenderer>();
                renderer.sprite = definition.preview;
                if (catalog.placementPlan != null && catalog.placementPlan.furnitureMaterial != null)
                {
                    renderer.sharedMaterial = catalog.placementPlan.furnitureMaterial;
                }

                float height = rule == null ? Mathf.Min(2.4f, definition.preview.bounds.size.y * 1.7f) : rule.height;
                float scale = height / definition.preview.bounds.size.y;
                visual.transform.localScale = Vector3.one * scale;
                visual.transform.localPosition = Vector3.up * (-definition.preview.bounds.min.y * scale);
                renderer.sortingOrder = 1000 - Mathf.RoundToInt(go.transform.position.y * 100);
                var collider = go.AddComponent<BoxCollider2D>();
                collider.size =
                    rule == null ? new Vector2(definition.preview.bounds.size.x * scale * .72f, .3f) : rule.footprint;
                collider.offset = Vector2.up * (collider.size.y / 2);
                var target = go.AddComponent<WorldInteractable>();
                target.id = placement.id;
                target.zone = "home";
                target.title = definition.title + " 사용하기";
                target.kind = definition.interaction;
                target.visual = renderer;
                target.UsePoint = rule == null ? Vector2.down * .9f : rule.approach;
                target.radius = Service.rules.interactionRadius;
                var highlight = new GameObject("Pixel highlight", typeof(SpriteRenderer));
                highlight.transform.SetParent(visual.transform, false);
                highlight.transform.localScale = Vector3.one * 1.035f;
                var glow = highlight.GetComponent<SpriteRenderer>();
                glow.sprite = definition.preview;
                glow.sharedMaterial = renderer.sharedMaterial;
                glow.color = new Color(1, .85f, .3f, .35f);
                glow.sortingOrder = renderer.sortingOrder - 1;
                glow.enabled = false;
                target.highlight = glow;
                if (Player.contactShadow != null)
                {
                    var shadow = new GameObject("Contact shadow", typeof(SpriteRenderer));
                    shadow.transform.SetParent(go.transform, false);
                    var image = shadow.GetComponent<SpriteRenderer>();
                    image.sprite = Player.contactShadow.sprite;
                    image.sharedMaterial = Player.contactShadow.sharedMaterial;
                    image.color = new Color(0, 0, 0, .22f);
                    image.sortingOrder = renderer.sortingOrder - 2;
                    shadow.transform.localScale = new Vector3(
                        collider.size.x / image.sprite.bounds.size.x,
                        .22f / image.sprite.bounds.size.y,
                        1
                    );
                    target.shadow = image;
                }

                var qualitySource = objects.FirstOrDefault(o =>
                    o.id
                        == (
                            definition.interaction == WorldInteractionKind.Fridge ? "fridge"
                            : definition.interaction == WorldInteractionKind.Meal ? "meal-table"
                            : definition.interaction == WorldInteractionKind.Study ? "starter-desk"
                            : definition.interaction == WorldInteractionKind.Rest
                            && (definition.id == "chair" || definition.id == "sofa")
                                ? "home-chair"
                            : ""
                        )
                    && o.GetComponent<WorldLifeAction>() != null
                );
                if (qualitySource != null)
                {
                    WorldLifeAction.CopyTo(target, qualitySource.GetComponent<WorldLifeAction>());
                }

                furniture.Add(target);
                colors[renderer] = Color.white;
            }

            Physics2D.SyncTransforms();
            FurnitureRevision++;
            if (!Busy && CurrentZone != null && !Clear(Player.Position))
            {
                Player.Warp(Safe(CurrentZone, Player.Position - CurrentZone.origin));
            }
        }

        void Light()
        {
            int hour = Service.Hour;
            foreach (var backdrop in roomBackdrops)
            {
                if (backdrop != null)
                {
                    backdrop.Apply(hour < 6 || hour >= 19);
                }
            }

            var presentation = GetComponent<SceneVisualPresentation>();
            if (presentation != null && presentation.profile != null)
            {
                presentation.ApplyLighting(hour, colors, lamps, lowDetail);
                return;
            }

            float daylight =
                hour < 5 ? .32f
                : hour < 8 ? Mathf.Lerp(.32f, 1, (hour - 5) / 3f)
                : hour < 18 ? 1
                : hour < 21 ? Mathf.Lerp(1, .32f, (hour - 18) / 3f)
                : .32f;
            float ambient = CurrentZone.outdoor ? daylight : Mathf.Max(.68f, daylight);
            foreach (var pair in colors)
            {
                if (pair.Key != null && !lamps.Contains(pair.Key))
                {
                    var c = pair.Value;
                    pair.Key.color = new Color(c.r * ambient, c.g * ambient, c.b * Mathf.Lerp(ambient, 1, .1f), c.a);
                }
            }

            foreach (var lamp in lamps)
            {
                if (lamp != null)
                {
                    lamp.enabled = daylight < .7f && !lowDetail;
                }
            }

            Player.sprite.color = Color.Lerp(new Color(.65f, .72f, .87f), Color.white, ambient);
        }
    }
}
