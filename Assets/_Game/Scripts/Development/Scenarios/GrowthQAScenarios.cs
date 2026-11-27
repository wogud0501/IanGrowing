#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IanGame
{
    public static class GrowthQAScenarios
    {
        public static void Register(TestScenarioRegistry registry)
        {
            registry.Register(
                new QAScenario
                {
                    id = "growth/visual-boundaries",
                    category = "성장 외형",
                    title = "성장 프로필·이동 권한 경계",
                    description =
                        "현재 방의 실제 캐릭터에서 나이별 라이브러리와 Collider를 확인합니다. 아트 품질 판정은 수동입니다.",
                    success = "나이·외형·충돌 일치, 보행1/외출7/성인19 분리",
                    run = Boundaries,
                }
            );
            registry.Register(
                new QAScenario
                {
                    id = "growth/art-review",
                    category = "성장 외형",
                    title = "외형·생활 동작 육안 검수",
                    initial = QAStatus.MANUAL,
                    description =
                        "각 프로필의 표정, 프레임 이음새, 손·가구 정렬과 상호작용 방향별 아트를 Game View에서 확인하세요.",
                    success = "사람의 최종 아트 검수 필요",
                }
            );
        }

        public static SortedSet<int> SampleAges(GrowthVisualCatalog catalog)
        {
            var starts = new SortedSet<int>(catalog.profiles.Select(p => p.minAge))
            {
                LifeProgression.WalkingAge, LifeProgression.IndependentOutdoorAge, MissionService.FreeLifeAge
            };
            foreach (LifeStage stage in Enum.GetValues(typeof(LifeStage)))
            {
                int low = 0, high = int.MaxValue;
                while (low < high)
                {
                    int middle = low + (high - low) / 2;
                    if (LifeProgression.StageAtAge(middle) < stage)
                    {
                        low = middle + 1;
                    }
                    else
                    {
                        high = middle;
                    }
                }
                starts.Add(low);
            }
            var samples = new SortedSet<int>(starts);
            foreach (var profile in catalog.profiles)
            {
                samples.Add(profile.maxAge);
                if (profile.maxAge != int.MaxValue)
                {
                    samples.Add(profile.minAge + (profile.maxAge - profile.minAge) / 2);
                }
            }
            foreach (int start in starts.Where(a => a > 0))
            {
                samples.Add(start - 1);
                samples.Add(start + 1);
            }
            return samples;
        }

        static IEnumerator Boundaries(QAScenarioContext c)
        {
            var w = c.Game.world.Host;
            c.Require(w != null && w.Player.growthVisual != null, "분리된 실제 방에서 실행하세요.");
            var visual = w.Player.growthVisual;
            var catalog = visual.catalog;
            c.Require(catalog != null, "성장 외형 카탈로그 미연결");
            c.Check(
                catalog.profiles.All(p =>
                    c.Commands.LifecyclePresets.Any(preset => preset.id == "visual/" + p.id && preset.age == p.minAge)
                ),
                "모든 카탈로그 프로필에 해당하는 외형 프리셋"
            );
            foreach (int age in SampleAges(catalog))
            {
                c.Commands.SetAge(age);
                yield return null;
                visual.RefreshAge();
                var p = catalog.AtAge(age);
                c.Check(
                    p != null && visual.Current == p && w.Player.advanced.library == p.animations,
                    "실제 프로필과 나이 " + age
                );
                c.Check(
                    Mathf.Abs(w.Player.GetComponent<CircleCollider2D>().radius - p.colliderRadius) < .0001f,
                    "실제 Collider " + age
                );
                c.Check(
                    c.Game.growth.WalkingUnlocked == (age >= 1)
                        && c.Game.growth.IndependentOutdoorUnlocked == (age >= 7)
                        && c.Game.growth.EmploymentUnlocked == (age >= 19),
                    "독립된 행동 권한 " + age
                );
            }

            c.Check(
                catalog.growthSheet && catalog.profiles.Select(p => p.idlePreview).Distinct().Count() == 20
                    && catalog.profiles.Select(p => p.animations).Distinct().Count() == 20,
                "공식 성장 20단계 연결"
            );
        }
    }
}
#endif
