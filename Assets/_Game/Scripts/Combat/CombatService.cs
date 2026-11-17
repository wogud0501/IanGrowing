using System;
using UnityEngine;

namespace IanGame
{
    public interface ICombatStatsProvider
    {
        int Attack { get; }

        int Defense { get; }
    }

    public sealed class CoreCombatStats : ICombatStatsProvider
    {
        readonly GameManager game;

        public CoreCombatStats(GameManager value)
        {
            game = value;
        }

        public int Attack =>
            Mathf.RoundToInt(
                game.items?.Apply(
                    ModifierTarget.CombatAttack,
                    6 + game.stats.Knowledge / 10 + Math.Min(100, game.growth.Level)
                )
                    ?? 6 + game.stats.Knowledge / 10 + Math.Min(100, game.growth.Level)
            );
        public int Defense =>
            Mathf.RoundToInt(
                game.items?.Apply(ModifierTarget.CombatDefense, 1 + game.stats.Social / 20)
                    ?? 1 + game.stats.Social / 20
            );
    }

    public sealed class CombatService
    {
        readonly GameManager game;
        public ICombatStatsProvider StatsProvider { get; set; }

        public CombatService(GameManager value)
        {
            game = value;
            StatsProvider = new CoreCombatStats(value);
        }

        // The same bounded turn resolver is used for interactive and offline battles.
        public BattleRecord Resolve(EnemyData enemy, string id)
        {
            var r = new BattleRecord
            {
                id = id,
                enemyId = enemy.id,
                healthBefore = game.stats.Health,
            };
            int hp = Math.Max(1, enemy.health);
            for (int i = 0; i < Mathf.Clamp(enemy.maxTurns, 1, 128) && hp > 0 && game.stats.Health > 0; i++)
            {
                int hit = (int)
                    Math.Max(1, Math.Min(int.MaxValue, (long)StatsProvider.Attack - Math.Max(0, enemy.defense)));
                hp = (int)Math.Max(0, (long)hp - hit);
                int damage =
                    hp > 0
                        ? (int)Math.Max(1, Math.Min(100, (long)enemy.attack - Math.Max(0, StatsProvider.Defense)))
                        : 0;
                game.stats.ApplyCore(new StatDelta { health = -damage });
                r.turns.Add(
                    new BattleTurn
                    {
                        number = i + 1,
                        damageToEnemy = hit,
                        damageToPlayer = damage,
                        enemyHealth = hp,
                        playerHealth = game.stats.Health,
                    }
                );
            }

            r.won = hp == 0 && game.stats.Health > 0;
            r.enemyRemaining = hp;
            r.healthAfter = game.stats.Health;
            int reward = (int)
                Math.Min(
                    int.MaxValue,
                    Math.Max(0, enemy.gold) * (double)(game.items?.Multiplier(ModifierTarget.RewardEfficiency) ?? 1)
                );
            if (
                r.won
                && game.economy.TryTransaction(id + "/battle-gold", TransactionKind.Income, reward, "실제 전투 승리")
            )
            {
                r.gold = reward;
                r.experience = Math.Max(0, enemy.experience);
                game.growth.GrantMissionExperience(
                    id + "/battle-xp",
                    r.experience,
                    game.missionCatalog.experiencePerLevel
                );
                game.idle.RecordVictory(id);
            }

            return r;
        }
    }
}
