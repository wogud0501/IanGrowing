using System;
using System.Collections.Generic;
using System.Linq;

namespace IanGame
{
    // Presentation only. Conditions and counters remain owned by MissionService.
    public static class MissionGuidance
    {
        static readonly string[] StatNames = { "허기", "목마름", "지식", "체력", "멘탈", "사회성", "신용도", "행복도" };

        public static string ActionName(int id, GameManager game = null)
        {
            if (id >= 7 && game != null && game.phase23 != null && game.phase23.catalog != null)
            {
                var a = game.phase23.catalog.Find(id);
                if (a != null)
                {
                    return a.displayName;
                }
            }

            string[] names =
            {
                "근무",
                "휴식",
                "놀기",
                "식사",
                "물 마시기",
                "수면",
                "병원 방문",
                "학교 입학",
                "책 읽기",
                "친구 만들기",
                "숙제",
                "시험 응시",
                "성적표 받기",
                "방과 후 학습",
                "공부",
                "진로 선택",
                "졸업",
                "자기계발",
                "NPC와 대화",
                "관계 유지",
                "이력서 작성",
                "면접 응시",
                "승진 신청",
                "퇴사",
                "헤드폰 장착",
                "장비 장착",
                "부채 상환",
            };
            return id >= 0 && id < names.Length ? names[id] : "조건 확인";
        }

        public static string Place(SceneId id)
        {
            switch (id)
            {
                case SceneId.Home:
                    return "Home · 집";
                case SceneId.School:
                    return "School · 학교";
                case SceneId.City:
                    return "City · 도시";
                case SceneId.Workplace:
                    return "Workplace · 직장";
                default:
                    return id.ToString();
            }
        }

        public static string CounterName(MissionCounter c)
        {
            switch (c)
            {
                case MissionCounter.Drink:
                    return "물 마시기";
                case MissionCounter.Meal:
                    return "식사";
                case MissionCounter.Sleep:
                    return "수면";
                case MissionCounter.Rest:
                    return "휴식";
                case MissionCounter.SchoolEnrollment:
                    return "교육 과정 입학";
                case MissionCounter.BooksRead:
                    return "책 읽기";
                case MissionCounter.FriendsMade:
                    return "친구 만들기 성공";
                case MissionCounter.Homework:
                    return "숙제 완료";
                case MissionCounter.Exams:
                    return "시험 응시";
                case MissionCounter.ReportCards:
                    return "성적표 수령";
                case MissionCounter.AfterSchool:
                    return "방과 후 학습";
                case MissionCounter.Study:
                    return "공부 완료";
                case MissionCounter.Work:
                    return "유효 근무 완료";
                case MissionCounter.MiddleSchool:
                    return "중등·고등교육 입학";
                case MissionCounter.HeadphonesEquipped:
                    return "스타터 헤드폰 장착";
                case MissionCounter.EquipmentEquipped:
                    return "스타터 장비 장착";
                case MissionCounter.CareerChosen:
                    return "진로 선택";
                case MissionCounter.Graduation:
                    return "교육 과정 졸업";
                case MissionCounter.SecondaryGraduation:
                    return "중등·고등교육 졸업";
                case MissionCounter.ResumeWritten:
                    return "이력서 작성";
                case MissionCounter.Interviews:
                    return "면접 응시";
                case MissionCounter.Employment:
                    return "채용 성공";
                case MissionCounter.Salary:
                    return "급여 수령";
                case MissionCounter.DebtRepaid:
                    return "부채 원금 상환";
                case MissionCounter.TaxesPaid:
                    return "세금 납부";
                case MissionCounter.StockBuys:
                    return "주식 매수";
                case MissionCounter.StockSells:
                    return "주식 매도";
                case MissionCounter.EventChoices:
                    return "인생 이벤트 선택";
                case MissionCounter.Promotions:
                    return "승진";
                case MissionCounter.GachaDraws:
                    return "가챠 추첨";
                case MissionCounter.FurnitureBought:
                    return "가구 구매";
                case MissionCounter.RentPaid:
                    return "월세 납부";
                case MissionCounter.InsuranceLearned:
                    return "보증보험 알아보기";
                case MissionCounter.PnLReviews:
                    return "투자 손익 확인";
                case MissionCounter.RetirementDecisions:
                    return "은퇴 여부 선택";
                case MissionCounter.LifeEventRecollections:
                    return "인생 이벤트 회상";
                case MissionCounter.IncomeReviews:
                    return "누적 소득 확인";
                case MissionCounter.MajorAchievements:
                    return "주요 업적 달성";
                case MissionCounter.DebtReviews:
                    return "부채 내역 확인";
                case MissionCounter.LifeReviewApplications:
                    return "인생 평가 신청";
                case MissionCounter.LifeReviewCompletions:
                    return "인생 평가 완료";
                default:
                    return c.ToString();
            }
        }

        public static string Requirements(MissionCondition c, MissionService service)
        {
            return Describe(c, service, 0);
        }

        static string Describe(MissionCondition c, MissionService s, int depth)
        {
            if (c == null || depth > 20)
            {
                return "조건 데이터를 확인할 수 없습니다.";
            }

            var g = s == null ? null : s.GetComponent<GameManager>();
            switch (c.kind)
            {
                case MissionConditionKind.All:
                case MissionConditionKind.Any:
                    var list = new List<string>();
                    var children = c.children ?? new MissionCondition[0];
                    var claims = children.Where(v => v.kind == MissionConditionKind.MissionClaimed).ToArray();
                    if (claims.Length > 2)
                    {
                        int done = s == null ? 0 : claims.Count(v => s.Claimed(v.missionId));
                        list.Add(
                            "선행 미션 보상 수령 "
                                + done
                                + "/"
                                + claims.Length
                                + "개 ("
                                + claims.First().missionId
                                + "~"
                                + claims.Last().missionId
                                + ")"
                        );
                    }

                    foreach (var child in children)
                    {
                        if (claims.Length <= 2 || child.kind != MissionConditionKind.MissionClaimed)
                        {
                            list.Add(Describe(child, s, depth + 1));
                        }
                    }

                    return (c.kind == MissionConditionKind.Any ? "아래 중 하나 충족\n" : "")
                        + string.Join("\n", list.ToArray());
                case MissionConditionKind.StatAtLeast:
                    int value = g == null ? 0 : g.stats.Get(c.stat);
                    return StatNames[(int)c.stat] + " " + c.target + " 이상 · 현재 " + value + "/100";
                case MissionConditionKind.CounterAtLeast:
                    return CounterName(c.counter)
                        + " "
                        + (s == null ? 0 : s.Counter(c.counter))
                        + "/"
                        + c.target
                        + (c.counter == MissionCounter.DebtRepaid ? "G" : "회");
                case MissionConditionKind.AgeAtLeast:
                    return "나이 "
                        + c.target
                        + "세 이상 · 현재 "
                        + (g == null ? 0 : g.growth.Age)
                        + "세 (미션 완료로 성장)";
                case MissionConditionKind.StageAtLeast:
                    return "성장 단계 " + ((LifeStage)c.target) + " 이상";
                case MissionConditionKind.DaysSurvived:
                    return "생존 "
                        + (g == null || s == null ? 0 : Math.Max(0, g.clock.Day - s.State.startDay))
                        + "/"
                        + c.target
                        + "일 · 생존 상태 유지";
                case MissionConditionKind.Alive:
                    return "생존 상태 유지";
                case MissionConditionKind.CashAtLeast:
                    return "보유 현금 "
                        + c.target.ToString("N0")
                        + "G 이상 · 현재 "
                        + (g == null ? 0 : g.economy.Cash).ToString("N0")
                        + "G";
                case MissionConditionKind.MissionClaimed:
                    return c.missionId + " 보상 수령 · " + (s != null && s.Claimed(c.missionId) ? "완료" : "미완료");
                case MissionConditionKind.ItemOwned:
                    return "아이템 보유: " + c.item;
                case MissionConditionKind.ContentUnlocked:
                    return "콘텐츠 해금: " + c.unlock;
                case MissionConditionKind.EventChoiceOutcome:
                    return "이벤트 선택 결과: " + c.choice;
                default:
                    return "미션 상세 조건 확인";
            }
        }

        public static string NextStep(MissionData m, MissionService service)
        {
            if (m == null)
            {
                return "등록된 미션을 모두 확인했습니다.";
            }

            var g = service == null ? null : service.GetComponent<GameManager>();
            var progress = service == null ? null : service.GetProgress(m);
            if (progress != null && progress.state == MissionState.RewardClaimed)
            {
                return "완료한 미션입니다. 보상은 이미 지급되었습니다.";
            }

            if (progress != null && progress.state == MissionState.Planned)
            {
                return m.futurePhase + " 구현 예정입니다. 현재는 달성하거나 보상을 받을 수 없습니다.";
            }

            if (progress != null && progress.state == MissionState.Locked)
            {
                return "먼저 "
                    + string.Join(
                        ", ",
                        (m.prerequisiteIds ?? new string[0]).Where(id => !service.Claimed(id)).ToArray()
                    )
                    + "의 보상을 수령하세요.";
            }

            if (progress != null && progress.state == MissionState.Completed)
            {
                return "달성했습니다! ‘보상 수령’을 눌러 다음 미션으로 진행하세요.";
            }

            if (m.Id == "M035")
            {
                return "상태 → 경제·세금·부채 관리에서 고지서를 납부하세요. 급여 소득은 게임 날짜에 따라 고지됩니다. 부분 납부는 잔액이 0이 될 때 완료됩니다.";
            }

            if (m.Id == "M038")
            {
                return "도시 → 증권거래소 → 시장·주문에서 종목과 수량을 선택하고 매수하세요. 현금과 신용도가 필요합니다.";
            }

            if (m.Id == "M047")
            {
                return "도시 → 증권거래소 → 시장·주문에서 보유한 주식을 매도하세요.";
            }

            if (m.Id == "M048")
            {
                return "실제 주식 매수 후 증권거래소의 보유·손익 탭을 열어 투자 결과를 확인하세요.";
            }

            if (m.Id == "M043")
            {
                return "상태 → 경제·세금·부채 관리에서 정상 상환으로 신용도를 높이세요. 연체하면 신용도가 내려갑니다.";
            }

            if (m.suggestedAction < 0)
            {
                return "선행 미션의 보상을 모두 수령하고 위 조건을 충족하세요. 시간 경과만으로 나이가 오르지 않습니다.";
            }

            string name = ActionName(m.suggestedAction, g);
            string place =
                m.suggestedAction < 7 ? "생활 행동"
                : g != null && g.phase23 != null && g.phase23.catalog != null
                    ? Place(g.phase23.Destination(m.suggestedAction))
                : "School · 학교";
            string text = place + " → ‘" + name + "’ 선택 후 실행하세요.";
            if (m.suggestedAction == 9)
            {
                text += " 친구 만들기에 성공해야 집계됩니다. 대화만으로는 친구가 생기지 않습니다.";
            }

            if (m.suggestedAction == 12)
            {
                text += " 먼저 시험을 보고, 아직 받지 않은 성적표를 수령하세요.";
            }

            if (m.suggestedAction == 16 && g != null && g.phase23 != null && g.phase23.catalog != null)
            {
                var e = g.phase23.Education.Available;
                if (e != null)
                {
                    text +=
                        $" 공부 {e.graduationStudies}회, 숙제 {e.graduationHomework}회, {e.graduationScore}점 이상 성적표가 필요합니다.";
                }
            }

            if (m.suggestedAction == 0)
            {
                text += " 먼저 채용되어야 하며 유효 근무가 끝나야 급여가 지급됩니다.";
            }

            string reason;
            if (g != null && !g.CanPerformAction(m.suggestedAction, out reason) && g.State == GameState.Playing)
            {
                text += "\n현재: " + reason;
            }

            return text;
        }

        public static string Summary(MissionData m, MissionService s) =>
            Requirements(m.condition, s) + "\n\n다음 행동  " + NextStep(m, s);
    }
}
