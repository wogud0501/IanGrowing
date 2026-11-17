using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    public enum CoreStat
    {
        Hunger,
        Thirst,
        Knowledge,
        Health,
        Mental,
        Social,
        Credit,
        Happiness,
    }

    [Serializable]
    public struct StatDelta
    {
        public int hunger;
        public int thirst;
        public int knowledge;
        public int health;
        public int mental;
        public int social;
        public int credit;
        public int happiness;
    }

    [Serializable]
    public class StatSnapshot
    {
        public int happiness;
        public int stress;
        public int energy;
        public int money;
        public int hunger = 100;
        public int thirst = 100;
        public int knowledge;
        public int mental = -1;
        public int social;
        public int credit = 50;
    }

    public class PlayerStats : MonoBehaviour
    {
        [SerializeField, Range(0, 100)]
        int happiness = 50;

        [SerializeField, Range(0, 100)]
        int stress = 20;

        [SerializeField, Range(0, 100)]
        int energy = 70;

        [SerializeField, Range(0, 100)]
        int hunger = 100;

        [SerializeField, Range(0, 100)]
        int thirst = 100;

        [SerializeField, Range(0, 100)]
        int knowledge;

        [SerializeField, Range(0, 100)]
        int mental = 80;

        [SerializeField, Range(0, 100)]
        int social;

        [SerializeField, Range(0, 100)]
        int credit = 50;

        [SerializeField, Min(0)]
        int money = 50;
        public int Hunger => Effective(CoreStat.Hunger, hunger);
        public int Thirst => Effective(CoreStat.Thirst, thirst);
        public int Knowledge => Effective(CoreStat.Knowledge, knowledge);
        public int Health => Effective(CoreStat.Health, energy);
        public int Energy => Health;
        public int Mental => Effective(CoreStat.Mental, mental);
        public int Stress => 100 - Mental;
        public int Social => Effective(CoreStat.Social, social);
        public int Credit => Effective(CoreStat.Credit, credit);
        public int Happiness => Effective(CoreStat.Happiness, happiness);
        public int Money => money;

        readonly List<StatModifier> itemModifiers = new List<StatModifier>();

        int Effective(CoreStat stat, int value)
        {
            float add = 0,
                percent = 0;
            foreach (var m in itemModifiers)
            {
                if ((int)m.stat == (int)stat)
                {
                    if (m.operation == ModifierOperation.Add)
                    {
                        add += m.value;
                    }
                    else
                    {
                        percent += m.value;
                    }
                }
            }

            return Mathf.Clamp(Mathf.RoundToInt(value * (1 + percent / 100) + add), 0, 100);
        }

        public void SetItemModifiers(IEnumerable<StatModifier> modifiers)
        {
            var old = Enumerable.Range(0, 8).Select(i => Get((CoreStat)i)).ToArray();
            itemModifiers.Clear();
            if (modifiers != null)
            {
                itemModifiers.AddRange(modifiers.Where(m => (int)m.stat < 8));
            }

            Action<int>[] handlers =
            {
                HungerChanged,
                ThirstChanged,
                KnowledgeChanged,
                HealthChanged,
                MentalChanged,
                SocialChanged,
                CreditChanged,
                HappinessChanged,
            };
            bool changed = false;
            for (int i = 0; i < 8; i++)
            {
                int now = Get((CoreStat)i);
                if (now == old[i])
                {
                    continue;
                }

                changed = true;
                handlers[i]?.Invoke(now);
                StatChanged?.Invoke((CoreStat)i, now);
                if (i == 3)
                {
                    EnergyChanged?.Invoke(now);
                }

                if (i == 4)
                {
                    StressChanged?.Invoke(100 - now);
                }
            }

            if (changed)
            {
                Changed?.Invoke();
            }
        }

        public event Action Changed;
        public event Action<CoreStat, int> StatChanged;
        public event Action<int> HungerChanged;
        public event Action<int> ThirstChanged;
        public event Action<int> KnowledgeChanged;
        public event Action<int> HealthChanged;
        public event Action<int> MentalChanged;
        public event Action<int> SocialChanged;
        public event Action<int> CreditChanged;
        public event Action<int> HappinessChanged;
        public event Action<int> StressChanged;
        public event Action<int> EnergyChanged;
        public event Action<int> MoneyChanged;

        public int Get(CoreStat stat)
        {
            switch (stat)
            {
                case CoreStat.Hunger:
                    return Hunger;
                case CoreStat.Thirst:
                    return Thirst;
                case CoreStat.Knowledge:
                    return Knowledge;
                case CoreStat.Health:
                    return Health;
                case CoreStat.Mental:
                    return Mental;
                case CoreStat.Social:
                    return Social;
                case CoreStat.Credit:
                    return Credit;
                default:
                    return Happiness;
            }
        }

        public StatSnapshot Capture() =>
            new StatSnapshot
            {
                happiness = happiness,
                stress = 100 - mental,
                energy = energy,
                money = money,
                hunger = hunger,
                thirst = thirst,
                knowledge = knowledge,
                mental = mental,
                social = social,
                credit = credit,
            };

        static int Limit(long value, int max = 100) => (int)Math.Max(0, Math.Min(max, value));

        public void Restore(StatSnapshot v)
        {
            if (v == null)
            {
                return;
            }

            var old = Capture();
            hunger = Limit(v.hunger);
            thirst = Limit(v.thirst);
            knowledge = Limit(v.knowledge);
            energy = Limit(v.energy);
            mental = Limit(v.mental < 0 ? 100L - v.stress : v.mental);
            stress = 100 - mental;
            social = Limit(v.social);
            credit = Limit(v.credit);
            happiness = Limit(v.happiness);
            money = Limit(v.money, int.MaxValue);
            bool changed = false;
            changed |= Raise(CoreStat.Hunger, old.hunger, hunger, HungerChanged);
            changed |= Raise(CoreStat.Thirst, old.thirst, thirst, ThirstChanged);
            changed |= Raise(CoreStat.Knowledge, old.knowledge, knowledge, KnowledgeChanged);
            changed |= Raise(CoreStat.Health, old.energy, energy, HealthChanged);
            changed |= Raise(CoreStat.Mental, old.mental, mental, MentalChanged);
            changed |= Raise(CoreStat.Social, old.social, social, SocialChanged);
            changed |= Raise(CoreStat.Credit, old.credit, credit, CreditChanged);
            changed |= Raise(CoreStat.Happiness, old.happiness, happiness, HappinessChanged);
            if (old.energy != energy)
            {
                EnergyChanged?.Invoke(energy);
            }

            if (old.stress != Stress)
            {
                StressChanged?.Invoke(Stress);
            }

            if (old.money != money)
            {
                MoneyChanged?.Invoke(money);
                changed = true;
            }

            if (changed)
            {
                Changed?.Invoke();
            }
        }

        bool Raise(CoreStat stat, int old, int value, Action<int> handler)
        {
            old = Effective(stat, old);
            value = Effective(stat, value);
            if (old == value)
            {
                return false;
            }

            handler?.Invoke(value);
            StatChanged?.Invoke(stat, value);
            return true;
        }

        public void Apply(int h, int s, int e, int m)
        {
            var v = Capture();
            v.happiness = Limit((long)happiness + h);
            v.mental = Limit((long)mental - s);
            v.energy = Limit((long)energy + e);
            v.money = Limit((long)money + m, int.MaxValue);
            Restore(v);
        }

        public void ApplyCore(StatDelta d)
        {
            var v = Capture();
            v.hunger = Limit((long)hunger + d.hunger);
            v.thirst = Limit((long)thirst + d.thirst);
            v.knowledge = Limit((long)knowledge + d.knowledge);
            v.energy = Limit((long)energy + d.health);
            v.mental = Limit((long)mental + d.mental);
            v.social = Limit((long)social + d.social);
            v.credit = Limit((long)credit + d.credit);
            v.happiness = Limit((long)happiness + d.happiness);
            Restore(v);
        }

        public void ResetStats(GameBalance b)
        {
            if (b != null)
            {
                Restore(
                    new StatSnapshot
                    {
                        happiness = b.initialHappiness,
                        stress = b.initialStress,
                        energy = b.initialEnergy,
                        money = b.initialMoney,
                    }
                );
            }
        }

        public void ResetLife(InitialLifeSettings settings)
        {
            if (settings != null)
            {
                Restore(settings.NewStats());
            }
        }
    }
}
