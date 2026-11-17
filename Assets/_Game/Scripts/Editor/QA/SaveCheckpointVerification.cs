using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace IanGame.Editor
{
    public static class SaveCheckpointVerification
    {
        [MenuItem("Ian/Verify Manual Save Checkpoint (Play Mode)")]
        public static void Menu()
        {
            foreach (var item in Run())
            {
                Debug.Log(item);
            }
        }

        public static string[] Run()
        {
            var g = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            var ui = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            if (!Application.isPlaying || g == null || g.State == GameState.Starting)
            {
                throw new InvalidOperationException("Enter Play Mode first.");
            }

            var field = typeof(SaveManager).GetField("fileName", BindingFlags.NonPublic | BindingFlags.Instance);
            string slot = (string)field.GetValue(g.saves);
            bool lifecycle = g.autoSaveOnLifecycle,
                realtime = g.realtimeLifeEnabled,
                autosave = g.autoSaveAfterAction;
            float chance = g.balance.eventChance;
            field.SetValue(g.saves, "manual-checkpoint-test-" + Guid.NewGuid().ToString("N") + ".json");
            g.autoSaveOnLifecycle = false;
            g.realtimeLifeEnabled = false;
            g.autoSaveAfterAction = true;
            g.balance.eventChance = 0;
            try
            {
                g.NewGame();
                ui.drinkButton.onClick.Invoke();
                ui.saveButton.onClick.Invoke();
                string manualPath = g.saves.SavePath;
                string manual = File.ReadAllText(manualPath);
                SaveData expected = JsonUtility.FromJson<SaveData>(manual);
                ui.restButton.onClick.Invoke();
                ui.mealButton.onClick.Invoke();
                ui.drinkButton.onClick.Invoke();
                int progressedDay = g.clock.Day;
                if (progressedDay <= expected.day)
                {
                    throw new Exception("Actions did not progress");
                }

                if (File.ReadAllText(manualPath) != manual)
                {
                    throw new Exception(
                        "AUTOSAVE OVERWROTE THE MANUAL CHECKPOINT: saved day " + expected.day + ", now " + progressedDay
                    );
                }

                ui.loadButton.onClick.Invoke();
                if (
                    g.clock.Day != expected.day
                    || JsonUtility.ToJson(g.stats.Capture()) != JsonUtility.ToJson(expected.stats)
                    || JsonUtility.ToJson(g.economy.Capture()) != JsonUtility.ToJson(expected.economy)
                    || JsonUtility.ToJson(g.survival.Capture()) != JsonUtility.ToJson(expected.survival)
                    || JsonUtility.ToJson(g.growth.Capture()) != JsonUtility.ToJson(expected.progression)
                    || JsonUtility.ToJson(g.clock.Capture()) != JsonUtility.ToJson(expected.time)
                    || JsonUtility.ToJson(g.missions.Capture()) != JsonUtility.ToJson(expected.missions)
                )
                {
                    throw new Exception("Manual checkpoint did not restore exactly");
                }

                ui.restButton.onClick.Invoke();
                ui.loadButton.onClick.Invoke();
                if (g.clock.Day != expected.day || File.ReadAllText(manualPath) != manual)
                {
                    throw new Exception("Repeated load changed checkpoint");
                }

                ui.restButton.onClick.Invoke();
                ui.saveButton.onClick.Invoke();
                int next = g.clock.Day;
                ui.drinkButton.onClick.Invoke();
                ui.loadButton.onClick.Invoke();
                if (g.clock.Day != next)
                {
                    throw new Exception("New explicit save was not used");
                }

                return new[]
                {
                    "PASS manual file unchanged after three automatically saved actions",
                    "PASS Load button restores saved day and every model/time/status exactly",
                    "PASS repeated Load still restores the same checkpoint",
                    "PASS next explicit Save replaces the manual checkpoint",
                };
            }
            finally
            {
                field.SetValue(g.saves, slot);
                g.autoSaveOnLifecycle = lifecycle;
                g.realtimeLifeEnabled = realtime;
                g.autoSaveAfterAction = autosave;
                g.balance.eventChance = chance;
            }
        }
    }
}
