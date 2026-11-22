using System;
using UnityEngine;

namespace IanGame
{
    public enum GameState
    {
        Starting,
        Playing,
        Paused,
        GameOver,
        Resolving,
        Ready = Playing,
    }

    public class GameManager : MonoBehaviour
    {
        public ItemService items;
        public NotificationService Notifications { get; } = new NotificationService();
        public PurchaseService purchases { get; private set; }

        public LifeWorldService world;
        public IdleActivityService idle;
        public GameBalance balance;
        public PlayerStats stats;
        public TimeManager clock;
        public EventManager lifeEvents;
        public SaveManager saves;
        public Phase23Services phase23;
        public MissionService missions;
        public MissionCatalog missionCatalog;
        public ActivityCatalog activityCatalog;
        bool missionCheckpointPending;
        public ActionSystem actions;
        public EconomySystem economy;
        public LifeProgression growth;
        public SurvivalSystem survival;
        public StatusEffectSystem statuses;
        public bool autoLoadOnStart = true;
        public bool autoSaveAfterAction = true;
        public bool autoSaveOnLifecycle = true;
        public bool realtimeLifeEnabled = true;
        public GameState State { get; private set; } = GameState.Starting;
        public bool LifeMode => balance != null && balance.initialLife != null && balance.survival != null;

        readonly System.Collections.Generic.Queue<string> recentLogs = new System.Collections.Generic.Queue<string>();
        public string[] RecentLogs => recentLogs.ToArray();

        string runId;
        int settledDay = 1;
        bool checkpointPending;
        bool appSuspended;
        bool wasRealtime;
        long connectionStamp;
        public event Action<string> Logged;
        public event Action<int> ActionPerformed;
        public event Action<GameState> StateChanged;

        T Module<T>(T component)
            where T : Component
        {
            if (component != null)
            {
                return component;
            }

            var found = GetComponent<T>();
            return found != null ? found : gameObject.AddComponent<T>();
        }

        void Awake()
        {
            if (balance == null || stats == null || clock == null || lifeEvents == null || saves == null)
            {
                Debug.LogError("Ian: 시스템 연결을 확인하세요.", this);
                enabled = false;
                return;
            }

            missions = Module(missions);
            missions.catalog = missionCatalog;
            economy = Module(economy);
            actions = Module(actions);
            growth = Module(growth);
            survival = Module(survival);
            statuses = Module(statuses);
            economy.Configure(stats);
            statuses.Configure(stats);
            survival.Configure(stats, LifeMode ? balance.survival : null, statuses);
            actions.Configure(balance, stats, clock, economy);
            actions.ConfigureLife(growth, survival);
            lifeEvents.Configure(economy);
            saves.Configure(growth, economy, survival);
            missions.Configure(this);
            economy.ConfigureGame(this);
            actions.ConfigureEducation(
                activityCatalog,
                missions,
                () =>
                    GameContext.Instance == null
                    || (world != null && world.Active ? world.Location : GameContext.Instance.transitions.CurrentScene)
                        == SceneId.School
            );
            saves.ConfigureMissions(missions);
            phase23 = Module(phase23);
            phase23.Configure(this);
            actions.ConfigurePhase23(phase23);
            saves.ConfigurePhase23(phase23);
            missions.Completed += OnMissionCompleted;
            missions.CheckpointRequested += OnMissionCheckpoint;
            idle = Module(idle);
            idle.Configure(this);
            saves.ConfigureIdle(idle);
            world = Module(world);
            world.Configure(this);
            saves.ConfigureWorld(world);
            items = Module(items);
            items.Configure(this);
            saves.ConfigureItems(items);
            purchases = new PurchaseService(this, ReleaseConfig.Current);
            clock.SetRunning(false);
            clock.DayAdvanced += OnDay;
            clock.SimulationAdvanced += OnSimulation;
            survival.Failure += OnFailure;
        }

        void Start()
        {
            if (enabled && GameContext.Instance?.game == this
                && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Bootstrap")
            {
                ResetModel();
                EnterTitle();
            }
            else if (enabled)
            {
                StartGame();
            }
        }

        void OnMissionCompleted(MissionData data)
        {
            Log(data.completionMessage);
        }

        void OnMissionCheckpoint()
        {
            missionCheckpointPending = true;
        }

        void OnDestroy()
        {
            if (missions != null)
            {
                missions.Completed -= OnMissionCompleted;
                missions.CheckpointRequested -= OnMissionCheckpoint;
            }

            if (clock != null)
            {
                clock.DayAdvanced -= OnDay;
                clock.SimulationAdvanced -= OnSimulation;
            }

            if (survival != null)
            {
                survival.Failure -= OnFailure;
            }
        }

        void ConfigureMode()
        {
            clock.realtimeEnabled = LifeMode && realtimeLifeEnabled;
            if (LifeMode)
            {
                clock.realSecondsPerGameDay = Math.Max(
                    1,
                    world != null && world.Active && world.rules != null
                        ? world.rules.realSecondsPerGameDay
                        : balance.survival.realSecondsPerGameDay
                );
            }

            survival.Configure(stats, LifeMode ? balance.survival : null, statuses);
        }

        void ResetModel()
        {
            world?.ResetLife();
            idle?.ResetLife();
            saves.BirthIntroCompleted = true;
            recentLogs.Clear();
            phase23.ResetLife();
            items?.ResetLife();
            missions.BeginRestore();
            ConfigureMode();
            if (LifeMode)
            {
                stats.ResetLife(balance.initialLife);
            }
            else
            {
                stats.ResetStats(balance);
            }

            economy.ResetLife(LifeMode);
            growth.Restore(new ProgressSnapshot { age = LifeMode ? 0 : 19 });
            survival.ResetLife();
            clock.Restore(1, new TimeSnapshot { subDaySeconds = world != null && world.rules != null ? 8 * 3600 : 0 });
            runId = Guid.NewGuid().ToString("N");
            settledDay = 1;
            connectionStamp = DateTime.UtcNow.Ticks;
            missions.ResetLife(1);
            missions.EndRestore();
            missionCheckpointPending = false;
        }

        public void StartGame()
        {
            if (!enabled || State == GameState.Resolving)
            {
                return;
            }

            SetState(GameState.Starting);
            ResetModel();
            if (autoLoadOnStart)
            {
                RestoreSave(false);
            }
            else
            {
                SetState(GameState.Playing);
                BirthMessage();
            }
        }

        void BirthMessage()
        {
            if (!LifeMode)
            {
                Log("이안이 입주했습니다. 행복은 별도 구매입니다.");
            }
        }

        public bool BirthIntroductionPending => LifeMode && !saves.BirthIntroCompleted;

        public bool CompleteBirthIntroduction()
        {
            if (!BirthIntroductionPending)
            {
                return true;
            }

            saves.BirthIntroCompleted = true;
            string message;
            if (!saves.SaveAutomatic(stats, clock, State == GameState.GameOver, runId, settledDay, out message))
            {
                saves.BirthIntroCompleted = false;
                Log(message);
                return false;
            }

            connectionStamp = saves.LastSavedUtcTicks;
            clock.SetRunning(
                State == GameState.Playing
                    && (GameContext.Instance == null || !GameContext.Instance.transitions.IsTransitioning)
            );
            RefreshAvailability();
            return true;
        }

        public void NewGame()
        {
            TryStartNewLife(out _, false);
        }

        public bool TryStartNewLife(out string message, bool titleSlot = true)
        {
            Notifications.Clear();
            if (LegalDocumentService.NeedsConsent)
            {
                message = "이용약관과 개인정보처리방침을 먼저 확인하세요.";
                Log(message);
                return false;
            }

            if (!enabled || State == GameState.Resolving)
            {
                message = "현재 행동이 끝난 뒤 다시 시도하세요.";
                return false;
            }

            SetState(GameState.Starting);
            ResetModel();
            saves.AllowExplicitNewGame();
            saves.BirthIntroCompleted = !LifeMode;
            SetState(GameState.Playing);
            clock.SetRunning(!BirthIntroductionPending);
            BirthMessage();
            return titleSlot ? SaveForMenu(out message) : SaveAutomaticCheckpoint(out message);
        }

        void SetState(GameState state)
        {
            if (clock != null)
            {
                clock.SetRunning(
                    (state == GameState.Playing || state == GameState.Resolving) && !BirthIntroductionPending
                );
            }

            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
            if (missions != null)
            {
                missions.Refresh();
            }
        }

        public void RefreshAvailability()
        {
            StateChanged?.Invoke(State);
        }

        public void Pause()
        {
            if (State == GameState.Playing)
            {
                SetState(GameState.Paused);
                Log("일시정지. 부채는 그대로 기다리고 있습니다.");
            }
        }

        public void Resume()
        {
            if (State == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }

        public void TogglePause()
        {
            if (State == GameState.Paused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void EndGame()
        {
            if (State == GameState.GameOver)
            {
                return;
            }

            SetState(GameState.GameOver);
            Log(
                LifeMode
                    ? "생존 위험이 유예시간 동안 지속되어 생존에 실패했습니다."
                    : "번아웃입니다. 새 인생에서 다시 시작할 수 있습니다."
            );
        }

        void OnFailure()
        {
            EndGame();
            checkpointPending = true;
        }

        void LateUpdate()
        {
            if (
                missionCheckpointPending
                && missions != null
                && !missions.IsBusy
                && State != GameState.Starting
                && State != GameState.Resolving
            )
            {
                missionCheckpointPending = false;
                if (autoSaveAfterAction)
                {
                    SaveQuietly();
                }
            }

            if (checkpointPending && State == GameState.GameOver)
            {
                checkpointPending = false;
                if (autoSaveAfterAction)
                {
                    SaveQuietly();
                }
            }
        }

        public bool CanPerformAction(int action, out string reason)
        {
            if (items != null && (action == 24 || action == 25))
            {
                return items.Equipment.CanEquip(
                    items.LegacyId(action == 24 ? MissionItemKey.StarterHeadphones : MissionItemKey.StarterEquipment),
                    out reason
                );
            }

            if (BirthIntroductionPending)
            {
                reason = "출생 신고를 확인하고 인생을 시작하세요.";
                return false;
            }

            reason = null;
            if (world != null && !world.CanAction(action, out reason))
            {
                return false;
            }

            if (!enabled || State != GameState.Playing)
            {
                reason =
                    State == GameState.Paused ? "일시정지 중입니다."
                    : State == GameState.GameOver ? "생존 종료 상태입니다."
                    : "행동 처리 또는 준비 중입니다.";
                return false;
            }

            if (actions == null)
            {
                reason = "행동 시스템 연결 누락";
                return false;
            }

            return actions.CanPerform(action, out reason);
        }

        public void Work()
        {
            Act(0);
        }

        public void Rest()
        {
            Act(1);
        }

        public void Play()
        {
            Act(2);
        }

        public void Eat()
        {
            Act(3);
        }

        public void Drink()
        {
            Act(4);
        }

        public void Sleep()
        {
            Act(5);
        }

        public void VisitHospital()
        {
            Act(6);
        }

        public bool TryAction(int action) => Act(action);

        public bool TryTimedAction(int action, double seconds) => Act(action, seconds);

        internal bool CanAutomaticAction(int action, out string reason)
        {
            if (action != 0 && action != 14)
            {
                reason = "허용되지 않은 자동 행동";
                return false;
            }

            phase23.AutomaticExecution = true;
            try
            {
                return CanPerformAction(action, out reason);
            }
            finally
            {
                phase23.AutomaticExecution = false;
            }
        }

        internal bool TryAutomaticAction(int action)
        {
            if (action != 0 && action != 14)
            {
                return false;
            }

            phase23.AutomaticExecution = true;
            try
            {
                return Act(action);
            }
            finally
            {
                phase23.AutomaticExecution = false;
            }
        }

        internal BattleRecord ResolveIdleBattle(EnemyData enemy, string receipt)
        {
            if (
                State != GameState.Playing
                || BirthIntroductionPending
                || enemy == null
                || !clock.CanAdvance(1)
                || economy.HasTransaction(receipt + "/battle-gold")
            )
            {
                return null;
            }

            SetState(GameState.Resolving);
            try
            {
                stats.ApplyCore(new StatDelta { health = -2, mental = -1 });
                clock.AdvanceDays(1);
                if (State == GameState.GameOver)
                {
                    return new BattleRecord
                    {
                        id = receipt,
                        enemyId = enemy.id,
                        healthBefore = stats.Health,
                        healthAfter = stats.Health,
                    };
                }

                var record = idle.Combat.Resolve(enemy, receipt);
                Log(record.won ? "전투 승리. 마감은 내일 다시 옵니다." : "전투 패배. 수면과 식사 후 다시 도전하세요.");
                return record;
            }
            finally
            {
                if (State != GameState.GameOver)
                {
                    SetState(GameState.Playing);
                }
            }
        }

        public SaveData CaptureState() =>
            SaveManager.StampOptionalBlocks(
                new SaveData
                {
                    legal = LegalDocumentService.Capture(),
                    items = items?.Capture(),
                    world = world?.Capture(),
                    resumeScene = SceneTransitionService.ResumeTarget(GameContext.Instance?.transitions?.CurrentScene ?? SceneId.LifeWorld),
                    birthIntroCompleted = saves.BirthIntroCompleted,
                    idle = idle == null ? new IdleProgressSnapshot() : idle.Capture(),
                    phase23 = phase23.Capture(),
                    day = clock.Day,
                    stats = stats.Capture(),
                    economy = economy.Capture(),
                    progression = growth.Capture(),
                    survival = survival.Capture(),
                    time = clock.Capture(),
                    missions = missions.Capture(),
                    gameOver = State == GameState.GameOver,
                    runId = runId,
                    settledDay = settledDay,
                    lastSeenUtcTicks = connectionStamp,
                }
            );

        public void RestoreState(SaveData data)
        {
            SetState(GameState.Starting);
            saves.ApplyToModels(data, stats, clock);
            runId = data.runId;
            settledDay = data.settledDay;
            connectionStamp = data.lastSeenUtcTicks;
            SetState(data.gameOver || survival.Failed ? GameState.GameOver : GameState.Playing);
            RefreshAvailability();
        }

        public bool SaveAutomaticCheckpoint(out string message)
        {
            missionCheckpointPending = false;
            if (State == GameState.Starting || State == GameState.Resolving)
            {
                message = "활동 처리 중";
                return false;
            }

            bool saved = saves.SaveAutomatic(stats, clock, State == GameState.GameOver, runId, settledDay, out message);
            if (saved)
            {
                connectionStamp = saves.LastSavedUtcTicks;
            }

            return saved;
        }

        bool Act(int action, double? seconds = null)
        {
            if (items != null && (action == 24 || action == 25))
            {
                bool equipped = items.Equipment.Equip(
                    Guid.NewGuid().ToString("N"),
                    items.LegacyId(action == 24 ? MissionItemKey.StarterHeadphones : MissionItemKey.StarterEquipment),
                    out var denied
                );
                if (!equipped)
                {
                    Log(denied);
                }

                return equipped;
            }

            if (world != null && world.Active && !world.Executing)
            {
                return world.Host.TryLegacyAction(action);
            }

            string message;
            if (!CanPerformAction(action, out message))
            {
                Log(message);
                return false;
            }

            SetState(GameState.Resolving);
            bool completed = false;
            try
            {
                int hungerBefore = stats.Hunger;
                int thirstBefore = stats.Thirst;
                completed = actions.Execute(action, out message, seconds);
                if (completed)
                {
                    Log(message);
                    if (missions != null && (phase23 == null || !phase23.Handles(action)))
                    {
                        missions.RecordAction(missions.NextEventId(runId), action);
                    }

                    world?.City?.ActionFinishedForGuide(action, hungerBefore, thirstBefore);
                    ActionPerformed?.Invoke(action);
                }
                else
                {
                    Log(message);
                }
            }
            finally
            {
                if (State != GameState.GameOver)
                {
                    if (LifeMode ? survival.Failed : balance.IsGameOver(stats))
                    {
                        EndGame();
                    }
                    else
                    {
                        SetState(GameState.Playing);
                    }
                }
            }

            if (completed && autoSaveAfterAction)
            {
                checkpointPending = false;
                SaveQuietly();
            }

            return completed;
        }

        void OnSimulation(double seconds)
        {
            if (LifeMode && (State == GameState.Playing || State == GameState.Resolving))
            {
                survival.Tick(seconds);
            }
        }

        void OnDay(int day)
        {
            if (day <= settledDay)
            {
                return;
            }

            settledDay = day;
            if (State != GameState.Playing && State != GameState.Resolving)
            {
                return;
            }

            economy.SettleDay(day);
            float chance = balance.eventChance * (LifeMode ? survival.EventChanceMultiplier : 1);
            var result = lifeEvents.TryEvent(stats, chance);
            if (result != null)
            {
                Log(result);
            }
        }

        void SaveQuietly()
        {
            if (saves == null || State == GameState.Starting || missions != null && missions.IsBusy)
            {
                return;
            }

            missionCheckpointPending = false;
            string message;
            if (!saves.SaveAutomatic(stats, clock, State == GameState.GameOver, runId, settledDay, out message))
            {
                Log(message);
            }
            else
            {
                connectionStamp = saves.LastSavedUtcTicks;
            }
        }

        public void Save()
        {
            if (
                !enabled
                || State == GameState.Starting
                || State == GameState.Resolving
                || missions != null && missions.IsBusy
            )
            {
                return;
            }

            string message;
            if (saves.Save(stats, clock, State == GameState.GameOver, runId, settledDay, out message))
            {
                connectionStamp = saves.LastSavedUtcTicks;
                SaveQuietly();
            }

            Log(message);
        }

        public void EnterTitle()
        {
            missionCheckpointPending = false;
            checkpointPending = false;
            Notifications.Clear();
            SetState(GameState.Starting);
            clock.SetRunning(false);
        }

        public bool SaveForMenu(out string message)
        {
            if (!enabled || State == GameState.Starting || State == GameState.Resolving || missions.IsBusy
                || world.Busy || GameContext.Instance?.transitions.IsTransitioning == true)
            {
                message = "현재 행동을 마친 뒤 다시 시도하세요.";
                return false;
            }

            bool saved = saves.Save(stats, clock, State == GameState.GameOver, runId, settledDay, out message);
            if (saved)
            {
                connectionStamp = saves.LastSavedUtcTicks;
                missionCheckpointPending = false;
            }
            else
            {
                Log(message);
            }

            return saved;
        }

        public void ContinueSnapshot(SaveData data)
        {
            var routes = GameContext.Instance?.transitions;
            if (routes != null && routes.IsRoomArchitecture)
            {
                world.UseRoomRules(routes.roomCatalog);
            }

            RestoreLoaded(data, false);
            clock.SetRunning(false);
        }

        public void Load()
        {
            if (enabled && State != GameState.Resolving)
            {
                RestoreSave(true);
            }
        }

        void RestoreSave(bool preserve)
        {
            SaveData data = null;
            string message = null;
            bool loaded = !preserve && saves.TryReadAutomatic(out data, out message);
            if (!loaded)
            {
                loaded = saves.TryRead(out data, out message);
            }

            if (loaded)
            {
                RestoreLoaded(data, preserve);
                Log(message);
                return;
            }

            if (!preserve)
            {
                SetState(GameState.Playing);
                if (saves.LastLoadStatus == SaveLoadStatus.Missing)
                {
                    BirthMessage();
                }
                else
                {
                    Log(message + " / 저장을 보호하는 임시 세션입니다.");
                }
            }
            else
            {
                Log(message);
            }
        }

        void RestoreLoaded(SaveData data, bool preserve)
        {
            long previousIdleWatermark = idle == null ? 0 : idle.State.accountedUtcTicks;
            long previousMarketWatermark = economy.Finance?.marketUtcTicks ?? 0;
            if (data.runId == runId && saves.BirthIntroCompleted)
            {
                data.birthIntroCompleted = true;
            }

            SetState(GameState.Starting);
            saves.ApplyToModels(data, stats, clock);
            runId = data.runId;
            settledDay = data.settledDay;
            connectionStamp = data.lastSeenUtcTicks;
            clock.RecordConnection(
                data.lastSeenUtcTicks,
                LifeMode ? balance.survival.maxOfflineInspectionSeconds : 28800
            );
            SetState(data.gameOver || survival.Failed ? GameState.GameOver : GameState.Playing);
            economy.EnsureFinance(clock.Day, DateTime.UtcNow.Ticks);
            if (preserve)
            {
                economy.ReanchorMarket(Math.Max(previousMarketWatermark, DateTime.UtcNow.Ticks));
            }

            idle?.Loaded(preserve, previousIdleWatermark);
            economy.SettleMarket(DateTime.UtcNow.Ticks);
            if (preserve)
            {
                SaveQuietly();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (State == GameState.Starting || appSuspended == paused)
            {
                return;
            }

            appSuspended = paused;
            if (paused)
            {
                idle?.PrepareBackground();
                wasRealtime = clock != null && clock.realtimeEnabled;
                if (clock != null)
                {
                    clock.realtimeEnabled = false;
                }

                if (autoSaveOnLifecycle && enabled && State != GameState.Starting && State != GameState.Resolving)
                {
                    SaveQuietly();
                }
            }
            else if (clock != null)
            {
                clock.RecordConnection(
                    connectionStamp,
                    LifeMode ? balance.survival.maxOfflineInspectionSeconds : 28800
                );
                clock.SkipNextRealtimeTick();
                clock.realtimeEnabled = wasRealtime;
                idle?.ResumeBackground();
            }
        }

        void OnApplicationQuit()
        {
            if (autoSaveOnLifecycle && enabled && State != GameState.Starting && State != GameState.Resolving)
            {
                SaveQuietly();
            }
        }

        public void Log(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string entry = "DAY " + (clock == null ? 1 : clock.Day) + "  " + text;
            recentLogs.Enqueue(entry);
            while (recentLogs.Count > 20)
            {
                recentLogs.Dequeue();
            }

            Logged?.Invoke(entry);
        }
    }
}
