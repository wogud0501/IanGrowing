#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;

namespace IanGame.Editor
{
    public static class Phase22Verification
    {
        internal static string OriginalSlot;
        internal static string SandboxSlot;
        internal static string OriginalStateJson;
        internal static bool OriginalPaused;
        public static readonly List<string> Passed = new List<string>();
        public static string Failure;
        public static string Progress;
        public static bool Running;
        public static bool Finished;

        [MenuItem("Ian/Verify Phase 2.2 Multi Scene (Play Mode)")]
        public static void Begin()
        {
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException("Start Bootstrap Play Mode.");
            }

            if (Running)
            {
                throw new InvalidOperationException("Test already running");
            }

            var g = GameContext.Instance == null ? null : GameContext.Instance.game;
            if (g == null)
            {
                throw new InvalidOperationException("Wait for Bootstrap context");
            }

            var slot = typeof(SaveManager).GetField("fileName", BindingFlags.Instance | BindingFlags.NonPublic);
            OriginalSlot = (string)slot.GetValue(g.saves);
            OriginalPaused = g.State == GameState.Paused;
            OriginalStateJson = JsonUtility.ToJson(
                new SaveData
                {
                    day = g.clock.Day,
                    stats = g.stats.Capture(),
                    progression = g.growth.Capture(),
                    economy = g.economy.Capture(),
                    survival = g.survival.Capture(),
                    time = g.clock.Capture(),
                    missions = g.missions.Capture(),
                    gameOver = g.State == GameState.GameOver,
                    runId = (string)
                        typeof(GameManager)
                            .GetField("runId", BindingFlags.Instance | BindingFlags.NonPublic)
                            .GetValue(g),
                    settledDay = g.clock.Day,
                    lastSeenUtcTicks = DateTime.UtcNow.Ticks,
                }
            );
            SandboxSlot = "phase22-suite-" + Guid.NewGuid().ToString("N") + ".json";
            slot.SetValue(g.saves, SandboxSlot);
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
            {
                GameContext.Instance.transitions.Navigate(SceneId.MainMenu);
            }

            Passed.Clear();
            Failure = null;
            Progress = "Starting";
            Running = true;
            Finished = false;
            var go = new GameObject("Editor-only Phase22 Verification");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Phase22Runner>().StartChecks();
        }

        public static object Status() =>
            new
            {
                running = Running,
                finished = Finished,
                progress = Progress,
                passed = Passed.ToArray(),
                failure = Failure,
            };
    }

    class FakeMissionProvider : IMissionStateProvider
    {
        readonly MissionService service;
        readonly GameManager game;
        readonly Dictionary<CoreStat, int> stats = new Dictionary<CoreStat, int>();
        readonly HashSet<MissionItemKey> items = new HashSet<MissionItemKey>();
        long? cash;

        public FakeMissionProvider(MissionService s, GameManager g)
        {
            service = s;
            game = g;
        }

        public int Stat(CoreStat stat) => stats.ContainsKey(stat) ? stats[stat] : game.stats.Get(stat);

        public long Counter(MissionCounter c) => service.Counter(c);

        public int Age => game.growth.Age;
        public LifeStage Stage => game.growth.Stage;
        public int DaysSurvived => 100;
        public long Cash => cash ?? game.economy.Cash;
        public bool Alive => true;

        public bool HasItem(MissionItemKey item) => items.Contains(item);

        public bool ChoiceOutcome(MissionChoiceOutcome outcome) => service.HasChoice(outcome);

        public bool Claimed(string id) => service.Claimed(id);

        public bool Unlocked(ContentUnlock value) => service.HasUnlock(value);

        public bool Supports(MissionFeature features) => true;

        public void SetStat(CoreStat stat, int value)
        {
            stats[stat] = value;
        }

        public void Satisfy(MissionCondition condition, string id)
        {
            switch (condition.kind)
            {
                case MissionConditionKind.All:
                    foreach (var child in condition.children)
                    {
                        Satisfy(child, id);
                    }

                    break;
                case MissionConditionKind.Any:
                    Satisfy(condition.children[0], id);
                    break;
                case MissionConditionKind.StatAtLeast:
                    stats[condition.stat] = (int)condition.target;
                    break;
                case MissionConditionKind.CounterAtLeast:
                    long needed = condition.target - Counter(condition.counter);
                    if (needed > 0)
                    {
                        service.RecordSignal(
                            new MissionActivityEvent
                            {
                                id = id + "/" + condition.counter,
                                counter = condition.counter,
                                amount = needed,
                            }
                        );
                    }

                    break;
                case MissionConditionKind.ItemOwned:
                    items.Add(condition.item);
                    break;
                case MissionConditionKind.EventChoiceOutcome:
                    service.RecordChoice(id + "/choice", condition.choice);
                    break;
                case MissionConditionKind.CashAtLeast:
                    cash = condition.target;
                    break;
            }

            service.Refresh();
        }
    }

    class FakePendingGateway : IPendingMissionRewardGateway
    {
        public int calls;
        public MissionService reenter;
        public bool reentryAccepted;

        public bool TryDeliver(PendingMissionReward value)
        {
            calls++;
            if (reenter != null)
            {
                reentryAccepted = reenter.DeliverPending(value.id, this);
            }

            return true;
        }
    }
}
#endif
