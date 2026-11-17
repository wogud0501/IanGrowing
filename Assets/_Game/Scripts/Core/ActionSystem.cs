using System;
using UnityEngine;

namespace IanGame
{
    public struct ActionSpec
    {
        public int happiness;
        public int stress;
        public int energy;
        public int money;
        public int days;
        public StatDelta core;
        public string message;
    }

    public class ActionSystem : MonoBehaviour
    {
        [SerializeField]
        GameBalance balance;

        [SerializeField]
        PlayerStats stats;

        [SerializeField]
        TimeManager clock;

        [SerializeField]
        EconomySystem economy;
        Phase23Services phase23;

        public void ConfigurePhase23(Phase23Services value)
        {
            phase23 = value;
        }

        LifeProgression growth;
        SurvivalSystem survival;
        ActivityCatalog activities;
        MissionService missions;
        Func<bool> inSchool;

        public void ConfigureEducation(ActivityCatalog data, MissionService service, Func<bool> school)
        {
            activities = data;
            missions = service;
            inSchool = school;
        }

        public void Configure(GameBalance b, PlayerStats s, TimeManager t, EconomySystem e)
        {
            balance = b;
            stats = s;
            clock = t;
            economy = e;
        }

        public void ConfigureLife(LifeProgression p, SurvivalSystem s)
        {
            growth = p;
            survival = s;
        }

        public ActionSpec Describe(int action)
        {
            if (phase23 != null && phase23.Handles(action))
            {
                return phase23.Describe(action);
            }

            if (balance == null)
            {
                return default(ActionSpec);
            }

            if (action == 0)
            {
                return new ActionSpec
                {
                    money = balance.workMoney,
                    stress = balance.workStress,
                    energy = balance.workEnergy,
                    days = Mathf.Max(1, balance.workDays),
                    message = "일했습니다. 통장은 웃고, 이안은 아닙니다.",
                };
            }

            if (action == 1)
            {
                return new ActionSpec
                {
                    money = balance.restMoney,
                    stress = balance.restStress,
                    energy = balance.restEnergy,
                    days = Mathf.Max(1, balance.restDays),
                    message = "쉬었습니다. 충전 요금은 인생에 청구됩니다.",
                };
            }

            if (action == 2)
            {
                return new ActionSpec
                {
                    money = balance.playMoney,
                    happiness = balance.playHappiness,
                    stress = balance.playStress,
                    energy = balance.playEnergy,
                    days = Mathf.Max(1, balance.playDays),
                    message = "놀았습니다. 행복 영수증은 보관하세요.",
                };
            }

            if (action >= 7)
            {
                var a = activities == null ? null : activities.Find(action);
                return a == null
                    ? default(ActionSpec)
                    : new ActionSpec
                    {
                        core = a.delta,
                        money = -a.cost,
                        days = Mathf.Max(1, a.days),
                        message = a.message,
                    };
            }

            var cfg = balance.survival;
            if (cfg == null)
            {
                return default(ActionSpec);
            }

            var d = new ActionSpec { days = Mathf.Max(1, cfg.careActionDays) };
            if (action == 3)
            {
                d.core = cfg.meal;
                d.money = -cfg.mealCost;
                d.message = "식사했습니다. 출생 비용에 식비는 포함되지 않았습니다.";
            }
            else if (action == 4)
            {
                d.core = cfg.drink;
                d.money = -cfg.drinkCost;
                d.message = "물을 마셨습니다. 프리미엄 수돗물은 다음에.";
            }
            else if (action == 5)
            {
                d.core = cfg.sleep;
                d.money = -cfg.sleepCost;
                d.message = "잠을 잤습니다. 현실은 로그아웃되지 않았습니다.";
            }
            else if (action == 6)
            {
                d.core = cfg.hospital;
                d.money = -cfg.hospitalCost;
                d.message = "무료 재화 병원에서 가상 피로를 회복했습니다. 유료권은 필요 없습니다.";
            }

            return d;
        }

        public bool CanPerform(int action, out string reason)
        {
            reason = null;
            if (balance == null || stats == null || clock == null || economy == null)
            {
                reason = "행동 시스템 연결 누락";
                return false;
            }

            if (phase23 != null && phase23.Handles(action))
            {
                if (!phase23.Can(action, out reason))
                {
                    return false;
                }

                return CanSpend(Describe(action), action, out reason);
            }

            if (action < 0 || action > 14 || action > 2 && action < 7 && balance.survival == null)
            {
                reason = "알 수 없는 행동입니다.";
                return false;
            }

            if (action == 0 && balance.initialLife != null && growth != null && !growth.EmploymentUnlocked)
            {
                reason = "취업은 청년기(19세)부터 해금됩니다. 나이는 미션으로 성장합니다.";
                return false;
            }

            if (action >= 7)
            {
                var a = activities == null ? null : activities.Find(action);
                if (a == null || missions == null || growth == null)
                {
                    reason = "아직 준비되지 않은 활동입니다.";
                    return false;
                }

                if (growth.Age < a.minAge || growth.Age > a.maxAge)
                {
                    reason = "현재 나이에서 할 수 없는 활동입니다.";
                    return false;
                }

                if (a.schoolOnly && inSchool != null && !inSchool())
                {
                    reason = "School에서 수행하는 활동입니다.";
                    return false;
                }

                if (a.requiredCounter != MissionCounter.None && missions.Counter(a.requiredCounter) < a.requiredCount)
                {
                    reason = "먼저 필요한 학교 활동을 완료하세요.";
                    return false;
                }

                if (a.onceOnly && missions.Counter(a.producedCounter) > 0)
                {
                    reason = "이미 완료한 활동입니다.";
                    return false;
                }
            }

            return CanSpend(Describe(action), action, out reason);
        }

        bool CanSpend(ActionSpec d, int action, out string reason)
        {
            reason = null;
            if (!clock.CanAdvance(d.days))
            {
                reason = "날짜를 진행할 수 없습니다.";
                return false;
            }

            long cost = d.money < 0 ? -(long)d.money : 0;
            if (phase23 != null && phase23.Handles(action) && action == 26)
            {
                cost = System.Math.Min(phase23.catalog.Find(action).cost, economy.Debt);
            }

            if (
                phase23 != null
                && phase23.Handles(action)
                && action == 0
                && phase23.Work.Wage > int.MaxValue - economy.Cash
            )
            {
                reason = "현금 보관 한도를 초과합니다.";
                return false;
            }

            if (cost > int.MaxValue || !economy.CanAfford((int)cost))
            {
                reason = "잔액 부족. 부채로 자동 결제하지 않습니다.";
                return false;
            }

            long healthCost = Math.Max(0, -(long)d.energy) + Math.Max(0, -(long)d.core.health);
            if (stats.Health < healthCost)
            {
                reason = "체력이 부족합니다. 수면이나 병원으로 회복하세요.";
                return false;
            }

            return true;
        }

        internal bool Execute(int action, out string message, double? duration = null)
        {
            if (!CanPerform(action, out message))
            {
                return false;
            }

            var d = Describe(action);
            float efficiency = balance.initialLife == null || survival == null ? 1 : survival.Efficiency;
            if (d.money < 0)
            {
                if (!economy.TrySpend((int)(-(long)d.money), "행동 비용"))
                {
                    return false;
                }
            }
            else
            {
                economy.Earn(Mathf.FloorToInt(d.money * efficiency), "행동 수입");
            }

            // Recovery stays fully effective at low health, avoiding a paid recovery dependency.
            stats.Apply(
                d.happiness > 0 ? Mathf.FloorToInt(d.happiness * efficiency) : d.happiness,
                d.stress,
                d.energy,
                0
            );
            var delta = d.core;
            if (action >= 7)
            {
                if (delta.knowledge > 0)
                {
                    delta.knowledge = Mathf.FloorToInt(
                        delta.knowledge
                            * efficiency
                            * (phase23?.Game.items?.Multiplier(ModifierTarget.StudyEfficiency) ?? 1)
                    );
                }

                if (delta.social > 0)
                {
                    delta.social = Mathf.FloorToInt(delta.social * efficiency);
                }
            }

            stats.ApplyCore(delta);
            economy.ApplyHousingRecovery(action);
            double started = clock.ElapsedSeconds;
            double requested = duration ?? d.days * TimeManager.SecondsPerDay;
            clock.AdvanceSimulation(requested);
            // Survival failure can interrupt a multi-day action before its promised duration.
            if (phase23 != null && phase23.Handles(action))
            {
                if (clock.ElapsedSeconds - started + .001 < requested)
                {
                    message = "활동이 중단되었습니다. 완료 기록과 급여를 지급하지 않습니다.";
                    return false;
                }

                phase23.Complete(action, Guid.NewGuid().ToString("N"));
            }

            message = d.message;
            return true;
        }
    }
}
