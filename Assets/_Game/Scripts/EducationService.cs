using System;
using System.Linq;
using UnityEngine;

namespace IanGame
{
    public class ExamService
    {
        public int Score(PlayerStats stats, int preparation, float roll) =>
            Mathf.Clamp(
                Mathf.RoundToInt(stats.Knowledge * .55f + preparation * .3f + stats.Mental * .15f + (roll - .5f) * 20),
                0,
                100
            );
    }

    public class EducationService
    {
        readonly Phase23Services owner;

        public EducationService(Phase23Services value)
        {
            owner = value;
        }

        public EducationProgress Progress => owner.Progress.education;
        public EducationData Available =>
            owner.catalog.education.FirstOrDefault(e =>
                owner.Game.growth.Age >= e.minAge && owner.Game.growth.Age <= e.maxAge
            );
        public EducationEnrollment Current =>
            Progress.enrollments.FirstOrDefault(e => e.educationId == Progress.currentEducationId);

        public bool Can(int action, out string reason)
        {
            reason = null;
            var data = Available;
            if (data == null)
            {
                reason = "현재 교육 과정이 없습니다.";
                return false;
            }

            if (action == 7)
            {
                if (
                    owner.Game.growth.Stage < data.requiredStage
                    || !string.IsNullOrEmpty(data.prerequisiteMission)
                        && !owner.Game.missions.Claimed(data.prerequisiteMission)
                )
                {
                    reason = "선행 성장 미션을 완료하세요: " + data.prerequisiteMission;
                    return false;
                }

                if (Progress.enrollments.Any(e => e.educationId == data.educationId))
                {
                    reason = "이미 입학했습니다.";
                    return false;
                }

                return true;
            }

            if (action == 17)
            {
                return true;
            }

            if (Current == null || Current.educationId != data.educationId || Current.graduated)
            {
                reason = "현재 나이에 맞는 과정에 먼저 입학하세요.";
                return false;
            }

            if (action == 12 && !Progress.exams.Any(e => e.educationId == Current.educationId && !e.reportReceived))
            {
                reason = "수령할 성적표가 없습니다. 시험부터 응시하세요.";
                return false;
            }

            if (action == 15 && (!string.IsNullOrEmpty(Progress.careerId) || owner.Game.growth.Age < 13))
            {
                reason = "진로는 청소년기부터 한 번 선택합니다.";
                return false;
            }

            if (
                action == 16
                && (
                    Current.studies < data.graduationStudies
                    || Current.homework < data.graduationHomework
                    || !Progress.exams.Any(e =>
                        e.educationId == data.educationId && e.reportReceived && e.score >= data.graduationScore
                    )
                )
            )
            {
                reason =
                    $"졸업 조건: 공부 {data.graduationStudies}회 / 숙제 {data.graduationHomework}회 / 성적표 {data.graduationScore}점 이상";
                return false;
            }

            return true;
        }

        public void Complete(int action, string id, LifeActivityData activity)
        {
            var p = Progress;
            var current = Current;
            switch (action)
            {
                case 7:
                    var data = Available;
                    p.currentEducationId = data.educationId;
                    p.enrollments.Add(new EducationEnrollment { educationId = data.educationId });
                    p.preparation = 0;
                    owner.Signal(id, data.enrollmentEvent);
                    break;
                case 8:
                    p.books++;
                    break;
                case 10:
                    p.homework++;
                    current.homework++;
                    break;
                case 14:
                case 13:
                    p.studies++;
                    current.studies++;
                    break;
                case 11:
                    var score = new ExamService().Score(owner.Game.stats, p.preparation, owner.Roll());
                    p.exams.Add(
                        new ExamRecord
                        {
                            id = id,
                            educationId = p.currentEducationId,
                            day = owner.Game.clock.Day,
                            score = score,
                            passed = score >= Available.graduationScore,
                        }
                    );
                    p.preparation = Math.Max(0, p.preparation - 20);
                    owner.Game.Log(
                        $"시험 {score}점. "
                            + (
                                score >= Available.graduationScore
                                    ? "합격. 다음 고통이 해금됩니다."
                                    : "재응시 가능. 공부와 휴식으로 회복하세요."
                            )
                    );
                    break;
                case 12:
                    var exam = p.exams.First(e => e.educationId == p.currentEducationId && !e.reportReceived);
                    exam.reportReceived = true;
                    owner.Game.Log($"성적표: {exam.score}점");
                    break;
                case 15:
                    p.careerId = owner.SelectedCareerId;
                    break;
                case 16:
                    current.graduated = true;
                    if (current.educationId == "secondary")
                    {
                        owner.Signal(id + "/secondary", MissionCounter.SecondaryGraduation);
                    }

                    owner.Signal(id, MissionCounter.Graduation);
                    break;
            }

            p.experience += activity.experience;
            if (action == 8 || action == 10 || action == 13 || action == 14 || action == 17)
            {
                p.preparation = Mathf.Clamp(p.preparation + activity.preparation, 0, 100);
            }

            if (action != 7)
            {
                owner.Signal(id, activity.missionEvent);
            }
        }
    }
}
