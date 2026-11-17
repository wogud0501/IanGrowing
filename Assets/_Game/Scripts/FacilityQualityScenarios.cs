#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    public static class FacilityQualityScenarios
    {
        static IEnumerator Once(Action run)
        {
            run();
            yield break;
        }

        static IEnumerator Layout(QAScenarioContext c, Action run)
        {
            yield return c.Commands.Navigate(SceneId.Home);
            run();
        }

        public static void Register(TestScenarioRegistry registry)
        {
            registry.Register(
                new QAScenario
                {
                    id = "world/facility-seats",
                    title = "시설 좌석·방향·독점 예약",
                    category = "월드·시설",
                    description = "실제 씬의 좌석 앵커·원래 업무석·공식 시설 명칭 검사",
                    success = "예약과 앵커/역할 연결",
                    run = c =>
                        Layout(
                            c,
                            () =>
                            {
                                var w = c.Game.world.Host;
                                c.Require(w != null, "생활 월드를 먼저 여세요");
                                var slots = w.GetComponentsInChildren<WorldActivitySlot>()
                                    .Where(s => s.useSeatAnchors)
                                    .ToArray();
                                c.Check(
                                    slots.Length >= 13
                                        && slots.All(s =>
                                            s.seatAnchor != null && s.approachAnchor != null && s.standUpAnchor != null
                                        ),
                                    "시설마다 실제 접근/착석/기립 앵커"
                                );
                                c.Check(
                                    w.GetComponentsInChildren<WorldWorkstationPresentation>().Length == 9,
                                    "원래 회사·금융 업무석9개 유지"
                                );
                                c.Check(c.Game.world.rules.Zone("school").title == "학교", "학교 ID와 공식 이름");
                                var seat = w
                                    .objects.First(o => o.id == "school-seat-2")
                                    .GetComponent<WorldActivitySlot>();
                                c.Require(string.IsNullOrEmpty(seat.Occupant), "미사용 좌석 필요");
                                c.Check(
                                    seat.TryReserve("qa/a") && !seat.TryReserve("qa/b"),
                                    "동일 좌석 중복 예약 거부"
                                );
                                seat.MarkOccupied("qa/a");
                                c.Check(seat.State == ActivityOccupancy.Occupied, "실제 점유 전환");
                                seat.Release("qa/a");
                                c.Check(seat.State == ActivityOccupancy.Free, "실제 예약 해제");
                            }
                        ),
                }
            );
            registry.Register(
                new QAScenario
                {
                    id = "world/facility-age",
                    title = "학교·상점 연령 실행 조건",
                    category = "월드·시설",
                    description = "기존 성장 정책 실행 검사",
                    success = "0세 거부·7세 허용·회사19세 유지",
                    run = c =>
                        Once(() =>
                        {
                            foreach (int age in new[] { 0, 1, 6, 7, 18, 19 })
                            {
                                c.Commands.SetAge(age);
                                c.Check(GrowthAccess.Zone(c.Game, "school", out _) == (age >= 7), "학교 경계 " + age);
                                c.Check(GrowthAccess.Zone(c.Game, "shop", out _) == (age >= 7), "상점 경계 " + age);
                                c.Check(
                                    GrowthAccess.Zone(c.Game, "workplace", out _) == (age >= 19),
                                    "회사 경계 " + age
                                );
                            }
                        }),
                }
            );
        }
    }
}
#endif
