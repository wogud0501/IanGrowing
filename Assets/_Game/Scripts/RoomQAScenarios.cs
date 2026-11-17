#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using System.Collections;
using UnityEngine;

namespace IanGame
{
    public static class RoomQAScenarios
    {
        static RoomSceneCatalog Catalog => GameContext.Instance.transitions.roomCatalog;

        static IEnumerator Once(Action a)
        {
            a();
            yield break;
        }

        public static void Register(TestScenarioRegistry r)
        {
            r.Register(
                new QAScenario
                {
                    id = "scene/catalog",
                    category = "씬 아키텍처",
                    title = "독립 씬·양방향 출입구",
                    description = "빌드 씬과 포털 카탈로그",
                    success = "실제 로딩 가능한 씬과 양방향 링크",
                    run = c =>
                        Once(() =>
                        {
                            c.Require(Catalog != null, "방 카탈로그 미연결");
                            c.Check(
                                Catalog.rooms.Select(x => x.roomId).Distinct().Count() == Catalog.rooms.Length,
                                "RoomId 중복 없음"
                            );
                            foreach (var room in Catalog.rooms)
                            {
                                c.Check(
                                    Application.CanStreamedLevelBeLoaded(room.sceneName),
                                    "Build 씬 " + room.sceneName
                                );
                            }

                            foreach (var p in Catalog.portals)
                            {
                                c.Check(
                                    Catalog.Find(p.destinationRoom) != null
                                        && Catalog.portals.Any(e =>
                                            e.sourceRoom == p.destinationRoom && e.destinationRoom == p.sourceRoom
                                        ),
                                    "왕복 문 " + p.doorId
                                );
                            }
                        }),
                }
            );
            r.Register(
                new QAScenario
                {
                    id = "scene/migration",
                    category = "씬 아키텍처",
                    title = "기존 좌표 무손실 변환",
                    description = "기존 가족 집 위치를 방 좌표로 변환",
                    success = "역변환 시 원래 위치 복원",
                    run = c =>
                        Once(() =>
                        {
                            c.Require(Catalog != null, "방 카탈로그 미연결");
                            var state = new LifeWorldSnapshot { zone = FamilyLife.Home, position = new Vector2(-8, 4) };
                            Catalog.Migrate(state, 19);
                            c.Check(
                                state.zone == "family-parents-bedroom" && state.position == new Vector2(0, -1),
                                "가족 집 방 좌표 변환"
                            );
                            Catalog.ToLegacy(state);
                            c.Check(
                                state.zone == FamilyLife.Home && state.position == new Vector2(-8, 4),
                                "기존 좌표 역변환"
                            );
                            var old = c.Game.economy.Capture();
                            old.housing.owned.Add("fridge");
                            old.housing.placed.Add(
                                new FurniturePlacement
                                {
                                    id = "fridge",
                                    position = new Vector2(2.5f, .7f),
                                    installed = false,
                                }
                            );
                            c.Game.economy.Restore(old);
                            var furniture = c.Game.economy.Housing.placed.Single(p => p.id == "fridge");
                            c.Check(
                                furniture.installed
                                    && furniture.roomId == "home"
                                    && furniture.homeId == "player"
                                    && c.Game.economy.Housing.owned.Contains("fridge"),
                                "구버전 필드 누락 시 구매 가구 설치 상태 보존"
                            );
                        }),
                }
            );
            r.Register(
                new QAScenario
                {
                    id = "scene/receipts",
                    category = "씬 아키텍처",
                    title = "중단된 가족 행동의 오프스크린 복구",
                    description = "렌더링 중단 후 공통 영수증으로 정산",
                    success = "재료1회 사용 및 중복 차단",
                    run = c =>
                        Once(() =>
                        {
                            var g = c.Game;
                            g.clock.Restore(4, new TimeSnapshot { subDaySeconds = 7 * 3600 });
                            var d = g.world.rules.npcs.Single(d => d.npcId == "family-mother");
                            int index = WorldNPCScheduler.Schedule(d, 4, 420);
                            long key = ((long)4 << 16) + index;
                            var n = g.world.State.residents.Single(n => n.id == d.npcId);
                            n.zone = "family-kitchen";
                            n.minute = 3 * 1440 + 419;
                            n.completedUseKey = key;
                            var f = FamilyLife.State(g);
                            f.ingredientCarrier = "";
                            f.pantry = 12;
                            f.receipts.RemoveAll(r => r.parent == d.npcId && r.target == "family-fridge");
                            g.world.AdvanceAbsentResidents();
                            c.Check(
                                f.pantry == 11 && f.ingredientCarrier == d.npcId,
                                "애니메이션 중단된 미정산 재료를 오프스크린에서 복구"
                            );
                            g.clock.Restore(4, new TimeSnapshot { subDaySeconds = 7 * 3600 + 60 });
                            g.world.AdvanceAbsentResidents();
                            c.Check(f.pantry == 11, "다음 분 같은 일정 영수증 중복 소비 없음");
                        }),
                }
            );
            r.Register(
                new QAScenario
                {
                    id = "scene/load",
                    category = "씬 아키텍처",
                    title = "거실·주방 실제 씬 왕복",
                    description = "공유 상태를 유지하며 비동기 로딩",
                    success = "단일 플레이어 및 현금 유지",
                    run = Load,
                }
            );
        }

        static IEnumerator Load(QAScenarioContext c)
        {
            var t = GameContext.Instance.transitions;
            c.Require(Catalog != null, "방 카탈로그 미연결");
            t.PrepareRoomStateRestore();
            long cash = c.Game.economy.Cash;
            foreach (string id in new[] { "family-living", "family-kitchen", "family-living" })
            {
                c.Check(t.NavigateRoom(id, Catalog.rules.Zone(id).spawn, Vector2.down), "씬 전환 요청 " + id);
                while (t.IsTransitioning)
                {
                    yield return null;
                }

                var w = c.Game.world.Host;
                c.Check(
                    w.Room.roomId == id && w.objects.All(o => o.zone == id) && c.Game.economy.Cash == cash,
                    "공간 분리·공유 현금 유지 " + id
                );
                c.Check(
                    UnityEngine.Object.FindObjectsByType<WorldPlayerMotor>(FindObjectsSortMode.None).Length == 1,
                    "플레이어 중복 없음"
                );
            }
        }
    }
}
#endif
