#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;

namespace IanGame
{
    [Serializable]
    public sealed class TestSaveSandbox
    {
        [Serializable]
        public class Fingerprint
        {
            public string path;
            public string sha256;
        }

        [SerializeField]
        bool active;

        [SerializeField]
        GameManager game;

        [SerializeField]
        SaveData original;

        [SerializeField]
        SaveManager.TestingChannel channel;

        [SerializeField]
        SceneId scene;

        [SerializeField]
        bool auto;

        [SerializeField]
        bool lifecycle;

        [SerializeField]
        bool realtime;

        [SerializeField]
        bool life;

        [SerializeField]
        bool running;

        [SerializeField]
        bool wasPaused;

        [SerializeField]
        bool missionPending;

        [SerializeField]
        bool checkpointPending;

        [SerializeField]
        UnityEngine.Random.State randomState;

        [SerializeField]
        float eventChance;

        [SerializeField]
        string npc;

        [SerializeField]
        string career;

        [SerializeField]
        List<Fingerprint> files = new List<Fingerprint>();
        public string OriginalCopySource => files.Find(f => File.Exists(f.path))?.path;
        public bool Active => active;
        public string Slot => active ? game.saves.BasePath : "No QA session";
        public bool LastFilesUnchanged { get; private set; } = true;

        static FieldInfo Field(string name) =>
            typeof(GameManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        static string Hash(string path)
        {
            if (!File.Exists(path))
            {
                return "<missing>";
            }

            using (var h = SHA256.Create())
            {
                return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
            }
        }

        public void Enter(GameManager owner)
        {
            if (active)
            {
                return;
            }

            if (
                owner == null
                || GameContext.Instance.transitions.IsTransitioning
                || owner.State == GameState.Resolving
                || owner.world.Busy
            )
            {
                throw new InvalidOperationException("이동/행동이 끝난 뒤 QA를 여세요.");
            }

            game = owner;
            original = game.CaptureState();
            scene = GameContext.Instance.transitions.CurrentScene;
            auto = game.autoSaveAfterAction;
            lifecycle = game.autoSaveOnLifecycle;
            realtime = game.clock.realtimeEnabled;
            life = game.realtimeLifeEnabled;
            running = game.clock.IsRunning;
            wasPaused = game.State == GameState.Paused;
            randomState = UnityEngine.Random.state;
            eventChance = game.balance.eventChance;
            npc = game.phase23.SelectedNpcId;
            career = game.phase23.SelectedCareerId;
            missionPending = (bool)Field("missionCheckpointPending").GetValue(game);
            checkpointPending = (bool)Field("checkpointPending").GetValue(game);
            files.Clear();
            foreach (
                var p in new[]
                {
                    game.saves.BasePath,
                    game.saves.SavePath,
                    game.saves.AutomaticSavePath,
                    game.saves.BasePath + ".bak",
                    game.saves.AutomaticSavePath + ".bak",
                }
            )
            {
                if (!files.Exists(f => f.path == p))
                {
                    files.Add(new Fingerprint { path = p, sha256 = Hash(p) });
                }
            }

            channel = game.saves.EnterTestingChannel("qa-" + Guid.NewGuid().ToString("N") + ".json");
            active = true;
            game.autoSaveAfterAction =
                game.autoSaveOnLifecycle =
                game.realtimeLifeEnabled =
                game.clock.realtimeEnabled =
                    false;
            game.balance.eventChance = 0;
            game.clock.SetRunning(false);
            if (wasPaused)
            {
                game.Resume();
            }

            game.clock.SetRunning(false);
        }

        public void ResetData()
        {
            if (!active)
            {
                throw new InvalidOperationException("테스트 슬롯이 필요합니다.");
            }

            game.saves.AllowExplicitNewGame();
            if (GameContext.Instance.transitions.roomCatalog?.Find(scene) != null)
            {
                GameContext.Instance.transitions.PrepareRoomStateRestore();
            }

            game.RestoreState(original);
            game.autoSaveAfterAction =
                game.autoSaveOnLifecycle =
                game.clock.realtimeEnabled =
                game.realtimeLifeEnabled =
                    false;
            game.clock.SetRunning(false);
            Field("missionCheckpointPending").SetValue(game, false);
            Field("checkpointPending").SetValue(game, false);
        }

        public IEnumerator Exit()
        {
            if (!active)
            {
                yield break;
            }

            var transitions = GameContext.Instance.transitions;
            float until = Time.realtimeSinceStartup + 20;
            while (transitions.IsTransitioning && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            if (transitions.IsTransitioning)
            {
                throw new InvalidOperationException("전환이 끝나지 않아 테스트 슬롯을 유지합니다.");
            }

            ResetData();
            if (transitions.CurrentScene != scene)
            {
                if (!transitions.NavigateForTesting(scene))
                {
                    throw new InvalidOperationException("원래 씬 복귀 실패. 테스트 슬롯을 유지합니다.");
                }

                while (transitions.IsTransitioning)
                {
                    yield return null;
                }
            }

            game.RestoreState(original);
            game.phase23.SelectedNpcId = npc;
            game.phase23.SelectedCareerId = career;
            if (wasPaused)
            {
                game.Pause();
            }

            game.balance.eventChance = eventChance;
            UnityEngine.Random.state = randomState;
            game.clock.SetRunning(running);
            game.clock.realtimeEnabled = realtime;
            game.realtimeLifeEnabled = life;
            Field("missionCheckpointPending").SetValue(game, missionPending);
            Field("checkpointPending").SetValue(game, checkpointPending);
            game.saves.RestoreTestingChannel(channel);
            game.autoSaveAfterAction = auto;
            game.autoSaveOnLifecycle = lifecycle;
            LastFilesUnchanged = files.TrueForAll(f => Hash(f.path) == f.sha256);
            active = false;
        }

        public bool VerifyFiles() => files.TrueForAll(f => Hash(f.path) == f.sha256);

        public void Shutdown()
        {
            if (!active || game == null)
            {
                return;
            }

            game.autoSaveAfterAction = game.autoSaveOnLifecycle = false;
            game.balance.eventChance = eventChance; /* Keep QA channel on quit: lifecycle order must never save into the real slot. */
        }
    }
}
#endif
