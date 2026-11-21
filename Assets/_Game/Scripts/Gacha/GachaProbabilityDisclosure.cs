using System;
using System.Collections.Generic;
using System.Linq;

namespace IanGame
{
    public static class GachaProbabilityDisclosure
    {
        public static string Describe(ItemService items, string poolId)
        {
            return string.Join("\n\n", Sections(items, poolId));
        }

        public static string[] Sections(ItemService items, string poolId)
        {
            var p = items.catalog.Pool(poolId);
            if (p == null)
            {
                return new[] { "추첨 정보를 불러올 수 없습니다.", "", "" };
            }

            string List(Dictionary<string, double> odds) =>
                string.Join(
                    "\n",
                    odds.Select(e =>
                        items.catalog.Item(e.Key).displayName
                        + " · "
                        + items.catalog.Rarity(items.catalog.Item(e.Key).rarityId).displayName
                        + " · "
                        + (e.Value * 100).ToString("0.######")
                        + "%"
                    )
                );
            var rules = p.pityRules.Select(rule =>
            {
                string reward = !string.IsNullOrEmpty(rule.guaranteedItem)
                    ? items.catalog.Item(rule.guaranteedItem).displayName
                    : items.catalog.Rarity(rule.minimumRarityId).displayName + " 이상";
                string reset = rule.reset switch
                {
                    PityReset.Threshold => "보장 발동 시 초기화",
                    PityReset.MatchingDrop => "대상 아이템 획득 시 초기화",
                    _ => "누적 횟수 유지"
                };
                return $"{rule.threshold}회 · {reward}\n{reset}";
            });

            return new[]
            {
                p.displayName + "\n현재 연령의 실제 추첨 데이터 · 개별 확률\n\n" + List(items.Gacha.Probabilities(poolId)),
                "다음 1회 (현재 천장 반영)\n\n" + List(items.Gacha.Probabilities(poolId, true)),
                "천장 규칙\n\n" + string.Join("\n\n", rules)
                    + "\n\n동일 아이템 중복 획득이 가능합니다. 겹치는 천장은 결과 1개에 함께 적용되며 가장 높은 우선순위의 특정 아이템 보장이 우선합니다.\n10회는 각 회차의 천장 상태를 순서대로 반영합니다. 표시 반올림으로 합계에 미세한 차이가 날 수 있습니다. 확률 변경은 업데이트 고지 후 해당 데이터가 적용된 추첨부터 효력이 있습니다."
            };
        }

        public static string Validate(ItemCatalog catalog)
        {
            var error = catalog?.Validate();
            if (catalog == null || error != null)
            {
                return error ?? "Catalog missing";
            }

            foreach (var pool in catalog.pools)
            {
                foreach (int age in new[] { pool.minAge, 19, 30, 60 }.Where(a => a >= pool.minAge))
                {
                    foreach (
                        var rules in new[] { Array.Empty<PityRule>(), pool.pityRules }.Concat(
                            pool.pityRules.Select(r => new[] { r })
                        )
                    )
                    {
                        var odds = GachaService.Compute(catalog, age, pool, rules);
                        if (
                            odds.Count == 0
                            || odds.Any(o => double.IsNaN(o.Value) || double.IsInfinity(o.Value) || o.Value < 0)
                            || Math.Abs(odds.Values.Sum() - 1) > 1e-10
                        )
                        {
                            return "Probability sum invalid: " + pool.poolId + " age " + age;
                        }
                    }
                }
            }

            return null;
        }
    }
}
