#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    public static class CityLifeQAScenarios
    {
        public static void Register(TestScenarioRegistry r)
        {
            r.Register(
                new QAScenario
                {
                    id = "city/first-outing",
                    category = "초반 생활 안내",
                    title = "아동의 방에서 도심까지 첫 외출",
                    success = "문 상호작용 반경 안쪽 가장자리에서 세 문 통과",
                    run = FirstOuting,
                }
            );
            r.Register(
                new QAScenario
                {
                    id = "city/onboarding-save",
                    category = "초반 생활 안내",
                    title = "튜토리얼 건너뛰기·다시 보기·저장",
                    success = "행동 진행 복원, 미션 및 재화 보존",
                    run = GuideSave,
                }
            );
            r.Register(
                new QAScenario
                {
                    id = "city/age-guidance",
                    category = "2.8 도시 UX",
                    title = "연령별 추천·목적지 제한",
                    description = "기존 성장 권한을 사용하며 추천은 강제 퀘스트가 아닙니다.",
                    success = "0~6세 외출 추천 없음, 7세 외출/19세 금융 유지",
                    run = Age,
                }
            );
            r.Register(
                new QAScenario
                {
                    id = "city/progress-save",
                    category = "2.8 도시 UX",
                    title = "도전과제 중복·저장·실패 복구",
                    description = "전용 QA 슬롯에서 신규 선택 저장 블록 검사",
                    success = "원본 파일 보존, 실제 디스크 복원 및 영수증 중복 차단",
                    run = Save,
                }
            );
            r.Register(
                new QAScenario
                {
                    id = "city/economy-hooks",
                    category = "2.8 도시 UX",
                    title = "실제 계약·소유 주택 입주 도전과제",
                    run = Housing,
                }
            );
        }

        static IEnumerator FirstOuting(QAScenarioContext c)
        {
            var panel = DeveloperQACenter.Instance;
            bool returnToPanel = panel != null && panel.Visible;
            if (returnToPanel)
            {
                panel.Minimize();
            }

            try
            {
                yield return WalkToCity(c);
            }
            finally
            {
                if (returnToPanel)
                {
                    panel.Open();
                }
            }
        }

        static IEnumerator WalkToCity(QAScenarioContext c)
        {
            c.Commands.Fixture(7, false, false);
            var world = c.Game.world;
            var transitions = GameContext.Instance.transitions;
            // A fixture may be restored over any active scene. Load the room through its real entry point.
            yield return c.Commands.Navigate(SceneId.City);
            yield return c.Commands.Navigate(world.rooms.Find("family-ian-bedroom").sceneId);
            yield return null;
            world.City.ResetUXGuides();

            foreach (string destination in new[] { "family-living", "alley", "city" })
            {
                var host = world.Host;
                string id = world.rooms.FirstDoor(world.State.zone, destination);
                var portal = host.objects.FirstOrDefault(o => o && o.id == id);
                c.Check(portal, "실제 출입구 " + destination);
                Vector2 approach = Vector2.MoveTowards(portal.Point, host.Player.Position, portal.radius - .06f);
                host.Player.Warp(approach);
                yield return new WaitForFixedUpdate();
                c.Check(host.Use(portal), "문 사용 " + destination);

                float deadline = Time.realtimeSinceStartup + 15;
                while (
                    Time.realtimeSinceStartup < deadline
                    && (transitions.IsTransitioning || !world.Host || world.Host.Room.roomId != destination)
                )
                {
                    yield return null;
                }
                c.Check(
                    !transitions.IsTransitioning && world.Host && world.Host.Room.roomId == destination,
                    "통과 " + destination
                );
            }

            c.Check(world.City.Progress(GuideLesson.Outside) == LessonState.Completed, "도심 입장으로 외출 안내 완료");
            var origin = world.Host.CurrentZone.origin;
            var north = origin + Vector2.down * 1.25f;
            var south = origin + Vector2.down * 7.4f;
            var planner = new WorldNPCNavigation();
            float inbound = CrossingX(planner.Find(world.Host, "city", south, north), south, origin.y - 4);
            float outbound = CrossingX(planner.Find(world.Host, "city", north, south), north, origin.y - 4);
            c.Check(Mathf.Abs(inbound - outbound) >= NPCCrowdSpace.Clearance, "횡단보도 양방향 보행 간격");

            var clock = c.Game.clock;
            clock.Restore(clock.Day, new TimeSnapshot { subDaySeconds = 4 * 3600 });
            yield return null;
            c.Check(!world.City.NearbyFacility(), "새벽에 이용할 수 없는 건물 제외");
            clock.Restore(clock.Day, new TimeSnapshot { subDaySeconds = 10 * 3600 });
            yield return null;
            c.Check(world.City.NearbyFacility(), "낮에는 실제로 열린 건물 입구 추천");
        }

        static float CrossingX(System.Collections.Generic.List<Vector2> path, Vector2 previous, float y)
        {
            foreach (var point in path)
            {
                if (previous.y != point.y && (previous.y - y) * (point.y - y) <= 0)
                {
                    return Mathf.Lerp(previous.x, point.x, Mathf.InverseLerp(previous.y, point.y, y));
                }
                previous = point;
            }
            return float.NaN;
        }

        static IEnumerator GuideSave(QAScenarioContext c)
        {
            c.Commands.Fixture(0, false, false);
            var city = c.Game.world.City;
            string missions = JsonUtility.ToJson(c.Game.missions.Capture());
            long cash = c.Game.economy.Cash;
            city.ResetUXGuides();
            city.StartLesson(GuideLesson.Sleep);
            c.Check(city.Progress(GuideLesson.Sleep) == LessonState.Active, "수면 안내 시작");
            city.SkipLessons();
            c.Check(
                city.Progress(GuideLesson.Sleep) == LessonState.Skipped && city.CurrentLesson() == null,
                "확정 후 안내 종료"
            );
            c.Check(c.Commands.Save() && c.Commands.Load() && city.GuideSkipped, "전용 저장 슬롯에서 건너뛰기 복원");
            c.Check(city.Suggestions().Count is >= 1 and <= 3, "건너뛴 뒤에도 추천 사용 가능");
            city.ResetUXGuides();
            c.Check(!city.GuideSkipped && city.Progress(GuideLesson.Sleep) == LessonState.NotStarted, "안내만 재시작");
            c.Check(
                c.Game.economy.Cash == cash && JsonUtility.ToJson(c.Game.missions.Capture()) == missions,
                "미션·재화 보존"
            );
            c.Check(c.Commands.Sandbox.VerifyFiles(), "기존 저장 파일 보존");
            yield break;
        }

        static IEnumerator Housing(QAScenarioContext c)
        {
            for (int mode = 0; mode < 2; mode++)
            {
                c.Commands.Fixture(30, false, false);
                c.Game.world.State.cityLife = new CityLifeProgress();
                yield return c.Commands.Navigate(SceneId.City, true);
                var e = c.Game.economy;
                if (mode == 0)
                {
                    c.Check(
                        e.LeaseProperty("qa/city-lease", "property-0", HousingTenure.MonthlyRent),
                        "실제 월세 입주"
                    );
                    c.Check(
                        !e.LeaseProperty("qa/city-lease-again", "property-0", HousingTenure.MonthlyRent),
                        "중복 계약 차단"
                    );
                }
                else
                {
                    c.Check(
                        e.BuyProperty("qa/city-buy", "property-0") && e.OccupyProperty("qa/city-occupy", "property-0"),
                        "실제 매수 후 입주"
                    );
                    c.Check(!e.OccupyProperty("qa/city-occupy-again", "property-0"), "중복 입주 차단");
                }

                c.Check(
                    c.Game.world.City.Counter("move") == 1
                        && c.Game.world.City.Data.achievements.Count(id => id == "move") == 1,
                    "성공한 입주만 한 번 기록"
                );
            }

            c.Check(c.Commands.Sandbox.VerifyFiles(), "경제 경로 테스트 원본 세이브 보존");
        }

        static IEnumerator Age(QAScenarioContext c)
        {
            c.Commands.Fixture(0, false, false);
            var city = c.Game.world.City;
            foreach (int age in new[] { 0, 1, 6, 7, 18, 19 })
            {
                c.Commands.SetAge(age);
                var s = city.Suggest();
                c.Check(
                    age >= 7 || s.room == null || FamilyLife.IsFamilyRoom(s.room),
                    "독립 외출을 권하지 않는 추천 " + age
                );
                c.Check(city.Track("city", out _) == (age >= 7), "외출 목적지 " + age);
                c.Check(city.Track("finance", out _) == (age >= 19), "금융 목적지 " + age);
            }

            yield break;
        }

        static IEnumerator Save(QAScenarioContext c)
        {
            c.Commands.Fixture(7, false, false);
            var city = c.Game.world.City;
            long money = c.Game.economy.Cash;
            city.Record("walk", 1, "qa/city-walk");
            city.Record("walk", 1, "qa/city-walk");
            c.Check(
                city.Counter("walk") == 1 && c.Game.economy.Cash == money,
                "동일 영수증 재입력과 무료 재화 보상 없음"
            );
            city.MarkGuide("qa/city-guide");
            city.Track("school", out _);
            c.Check(c.Commands.Save(), "QA 전용 슬롯 저장");
            string before = JsonUtility.ToJson(city.Data);
            city.Data.guideSeen.Clear();
            city.Track(null, out _);
            c.Check(
                c.Commands.Load() && JsonUtility.ToJson(city.Data) == before,
                "실제 디스크의 가이드·목적지·진행률 복원"
            );
            long day = city.Counter("day");
            c.Game.clock.SetDay(c.Game.clock.Day + 1);
            c.Check(city.Counter("day") == day, "날짜 설정은 하루 완주 아님");
            c.Check(c.Commands.Sandbox.VerifyFiles(), "원본 세이브 해시 보존");
            yield break;
        }
    }
}
#endif
