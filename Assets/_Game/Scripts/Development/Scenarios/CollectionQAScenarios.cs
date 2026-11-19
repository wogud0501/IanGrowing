#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using System.Collections;
using UnityEngine;

namespace IanGame
{
    public static class CollectionQAScenarios
    {
        public static void Register(TestScenarioRegistry r)
        {
            void Add(string id, string title, Func<QAScenarioContext, IEnumerator> run)
            {
                r.Register(
                    new QAScenario
                    {
                        id = id,
                        title = title,
                        category = "2.9 가챠·수집",
                        description = "QA 전용 슬롯에서 실제 서비스 실행",
                        success = "보상·원본 저장 무결성 유지",
                        run = run,
                    }
                );
            }

            Add("gacha/draw", "실제 1/10회·확률·중복", Draw);
            Add("items/pity", "50/100/300 경계·중복 방지", Pity);
            Add("items/save", "장착·분해·도감·디스크 복원", Save);
            Add("items/migration", "기존 스타터 장비 이전", Migration);
            Add("equipment/stats", "장비 해제 시 다른 슬롯과 급여 보존", Unequip);
            Add("items/effects", "장비 교체·노동 효율", Effects);
        }

        static IEnumerator Draw(QAScenarioContext c)
        {
            c.Commands.Fixture();
            var s = c.Game.items;
            c.Require(s?.catalog != null, "아이템 데이터 누락");
            yield return c.Commands.Navigate(SceneId.Home, true);
            long cash = c.Game.economy.Cash;
            c.Check(
                s.Gacha.Pull("qa/items/one", "life-collection", 1, false, out var one, out _),
                "실제 1회 비용·획득"
            );
            c.Check(
                s.Gacha.Pull("qa/items/ten", "life-collection", 10, false, out var ten, out _)
                    && ten.results.Length == 10
                    && s.Gacha.Progress("life-collection").total == 11,
                "실제 10회 및 누적11"
            );
            c.Check(c.Game.economy.Cash == cash - 1100, "기존 지갑의 정확한 비용");
            c.Check(!s.Gacha.Pull("qa/items/ten", "life-collection", 10, false, out _, out _), "영수증 재입력 거절");
            c.Check(Math.Abs(s.Gacha.Probabilities("life-collection").Values.Sum() - 1) < .0000001, "실제 확률 합1");
            c.Check(c.Commands.Sandbox.VerifyFiles(), "원본 파일 유지");
        }

        static IEnumerator Pity(QAScenarioContext c)
        {
            c.Commands.Fixture();
            yield return c.Commands.Navigate(SceneId.Home, true);
            var s = c.Game.items;
            s.Gacha.SetTestRoll(() => 0);
            try
            {
                foreach (int n in new[] { 50, 100, 300 })
                {
                    var data = s.Capture();
                    data.pools.Clear();
                    var p = new PoolProgress { poolId = "life-collection", total = n - 1 };
                    foreach (var rule in s.catalog.pools[0].pityRules)
                    {
                        p.pity.Add(
                            new PityProgress
                            {
                                ruleId = rule.id,
                                progress = (n - 1) % rule.threshold,
                                reached = (n - 1) / rule.threshold,
                                applied = (n - 1) / rule.threshold,
                            }
                        );
                    }

                    data.pools.Add(p);
                    s.Restore(data);
                    int count = s
                        .Inventory.Entries.Where(e =>
                            s.catalog.Item(e.itemId).itemType != ItemType.Material
                            && s.catalog.Item(e.itemId).itemType != ItemType.Ticket
                        )
                        .Sum(e => e.count);
                    c.Check(
                        s.Gacha.Pull("qa/items/boundary/" + n, "life-collection", 1, false, out var result, out _),
                        "경계에서 실제 1회 " + n
                    );
                    c.Check(
                        result.results.Length == 1
                            && s.Inventory.Entries.Where(e =>
                                    s.catalog.Item(e.itemId).itemType != ItemType.Material
                                    && s.catalog.Item(e.itemId).itemType != ItemType.Ticket
                                )
                                .Sum(e => e.count)
                                == count + 1,
                        "경계에서도 아이템 정확히 하나 " + n
                    );
                    c.Check(
                        n == 300
                            ? result.results[0].itemId == "headphone-silence"
                            : s.catalog.Rarity(result.results[0].rarityId).rank >= (n == 50 ? 3 : 4),
                        "보장 결과 " + n
                    );
                    c.Check(
                        s.Gacha.Progress("life-collection").pity.All(r => r.reached == r.applied),
                        "천장 완료·지급 일치 " + n
                    );
                }
            }
            finally
            {
                s.Gacha.SetTestRoll(null);
            }
        }

        static IEnumerator Save(QAScenarioContext c)
        {
            c.Commands.Fixture();
            yield return c.Commands.Navigate(SceneId.Home, true);
            var s = c.Game.items;
            c.Check(s.DebugGive("headphone-focus", 2) && s.DebugGive("starter-equipment", 1), "QA 명시적 중복 fixture");
            c.Check(s.Equipment.Equip("qa/items/equip", "headphone-focus", out _), "실제 헤드폰 장착");
            string saved = JsonUtility.ToJson(s.Capture());
            c.Check(
                c.Commands.Save() && c.Commands.Load() && JsonUtility.ToJson(s.Capture()) == saved,
                "실제 디스크 상태 복원"
            );
            c.Check(!s.Inventory.Dismantle("qa/items/no-equipped", "headphone-focus", 1, out _), "장착 중 분해 차단");
            c.Check(
                s.Equipment.Unequip("qa/items/off", EquipmentSlot.Headphone, out _)
                    && s.Inventory.Dismantle("qa/items/salvage", "headphone-focus", 1, out _)
                    && s.Inventory.Count("headphone-focus") == 1
                    && s.Inventory.Count("collection-scrap") == 12,
                "해제 후 중복 한 개만 분해"
            );
            c.Check(s.Inventory.Discover("headphone-focus"), "도감 이력 유지");
        }

        static IEnumerator Migration(QAScenarioContext c)
        {
            c.Commands.Fixture();
            var d = c.Game.CaptureState();
            d.items = null;
            d.phase23.owned.Add(MissionItemKey.StarterHeadphones);
            d.phase23.owned.Add(MissionItemKey.StarterEquipment);
            d.phase23.equipped.Add(MissionItemKey.StarterHeadphones);
            var missions = d.missions.claimedMissionIds.ToArray();
            c.Game.RestoreState(d);
            c.Check(
                c.Game.items.Inventory.Count("starter-headphones") == 1
                    && c.Game.items.Equipment.Current(EquipmentSlot.Headphone) == "starter-headphones",
                "구형 소유·장착 이전"
            );
            c.Check(c.Game.missions.State.claimedMissionIds.SequenceEqual(missions), "완료 미션 목록 보존");
            yield break;
        }

        static IEnumerator Unequip(QAScenarioContext c)
        {
            c.Commands.Fixture();
            yield return c.Commands.Navigate(SceneId.Home, true);

            var game = c.Game;
            game.phase23.Progress.employment.currentJobId = "convenience";
            var items = game.items;
            var equipment = items.Equipment;
            int baseWage = game.phase23.Work.Wage;
            c.Require(items.DebugGive("work-badge", 1) && items.DebugGive("headphone-wired", 1), "장비 두 슬롯 준비");
            c.Check(equipment.Equip("qa/slots/work", "work-badge", out _), "업무 배지 장착");
            c.Check(equipment.Equip("qa/slots/phones", "headphone-wired", out _), "별도 헤드폰 슬롯 장착");
            c.Check(game.phase23.Work.Wage > baseWage, "배지 급여 효과 적용");
            c.Check(equipment.Unequip("qa/slots/remove", EquipmentSlot.Equipment, out _), "업무 장비만 해제");
            c.Check(game.phase23.Work.Wage == baseWage, "해제한 배지의 급여 효과 제거");
            c.Check(equipment.Current(EquipmentSlot.Headphone) == "headphone-wired", "다른 장착 슬롯 보존");
        }

        static IEnumerator Effects(QAScenarioContext c)
        {
            c.Commands.Fixture();
            yield return c.Commands.Navigate(SceneId.Home, true);
            var g = c.Game;
            g.phase23.Progress.employment.currentJobId = "convenience";
            long wage = g.phase23.Work.Wage;
            var raw = JsonUtility.ToJson(g.stats.Capture());
            c.Check(
                wage > 0 && g.items.DebugGive("work-badge", 1) && g.items.DebugGive("starter-equipment", 1),
                "기존 직업·장비 fixture"
            );
            c.Check(
                g.items.Equipment.Equip("qa/items/wage", "work-badge", out _)
                    && g.phase23.Work.Wage == Math.Floor(wage * 1.05),
                "실제 급여 getter의 데이터 Modifier"
            );
            c.Check(
                g.items.Equipment.Equip("qa/items/replace", "starter-equipment", out _) && g.phase23.Work.Wage == wage,
                "같은 슬롯 교체가 이전 효율만 제거"
            );
            c.Check(
                g.items.Equipment.Unequip("qa/items/clear", EquipmentSlot.Equipment, out _)
                    && JsonUtility.ToJson(g.stats.Capture()) == raw,
                "기본 능력치 원본 불변"
            );
        }
    }
}
#endif
