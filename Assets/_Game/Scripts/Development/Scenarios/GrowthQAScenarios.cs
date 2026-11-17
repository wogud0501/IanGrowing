#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using System.Collections;
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
                    title = "9단계 프로필·이동 권한 경계",
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

        static IEnumerator Boundaries(QAScenarioContext c)
        {
            var w = c.Game.world.Host;
            c.Require(w != null && w.Player.growthVisual != null, "분리된 실제 방에서 실행하세요.");
            var visual = w.Player.growthVisual;
            var catalog = visual.catalog;
            c.Require(catalog != null, "성장 외형 카탈로그 미연결");
            c.Check(
                c.Commands.LifecyclePresets.Count(p => p.id.StartsWith("visual/")) == 9,
                "카탈로그에서 생성한 9개 외형 프리셋"
            );
            foreach (int age in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 13, 14, 16, 17, 18, 19 })
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
                catalog.profiles.Select(p => p.idlePreview.texture).Distinct().Count() == 9,
                "성인 원본 포함 서로 다른 9개 외형 텍스처"
            );
        }
    }
}
#endif
