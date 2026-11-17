using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public class Phase23Presenter : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text status;
        public TMP_Text details;
        public TMP_Text result;
        public Button[] activityButtons;
        public int[] actionIds;
        public Button[] navigationButtons;
        public Button openButton;
        public Button closeButton;
        public Button npcButton;
        public Button jobButton;
        public Button careerButton;
        GameManager game;
        bool subscribed;

        void Start()
        {
            if (GameContext.Instance == null)
            {
                return;
            }

            game = GameContext.Instance.game;
            openButton.onClick.AddListener(() =>
            {
                var shell = UIShell.For(this);
                if (shell != null)
                {
                    shell.Push(panel);
                }
                else
                {
                    panel.SetActive(true);
                }

                Refresh();
            });
            closeButton.onClick.AddListener(() =>
            {
                var shell = UIShell.For(this);
                if (shell != null)
                {
                    shell.Close(panel);
                }
                else
                {
                    panel.SetActive(false);
                }
            });
            for (int i = 0; i < activityButtons.Length; i++)
            {
                int id = actionIds[i];
                activityButtons[i]
                    .onClick.AddListener(() =>
                    {
                        game.TryAction(id);
                        Refresh();
                    });
            }

            var routes = new[] { SceneId.Home, SceneId.School, SceneId.City, SceneId.Workplace };
            for (int i = 0; i < navigationButtons.Length; i++)
            {
                var id = routes[i];
                navigationButtons[i].onClick.AddListener(() => GameContext.Instance.transitions.Navigate(id));
            }

            npcButton.onClick.AddListener(() =>
            {
                var p = game.phase23;
                var list = p
                    .catalog.npcs.Where(n => game.growth.Age >= n.minAge && game.growth.Age <= n.maxAge)
                    .ToArray();
                if (list.Length > 0)
                {
                    p.SelectedNpcId = list[
                        (Array.FindIndex(list, n => n.npcId == p.SelectedNpcId) + 1) % list.Length
                    ].npcId;
                }

                Refresh();
            });
            jobButton.onClick.AddListener(() =>
            {
                var p = game.phase23;
                var list = p.catalog.jobs.Where(j => !j.specialUnlock && j.BaseDailyWage > 0).ToArray();
                p.Progress.employment.selectedJobId = list[
                    (Array.FindIndex(list, j => j.JobId == p.Progress.employment.selectedJobId) + 1) % list.Length
                ].JobId;
                Refresh();
            });
            careerButton.onClick.AddListener(() =>
            {
                var p = game.phase23;
                p.SelectedCareerId = p.SelectedCareerId == "vocational" ? "academic" : "vocational";
                Refresh();
            });
            Attach();
            Refresh();
        }

        void OnEnable()
        {
            Attach();
        }

        void OnDisable()
        {
            Detach();
        }

        void OnDestroy()
        {
            Detach();
        }

        void Attach()
        {
            if (subscribed || game == null)
            {
                return;
            }

            subscribed = true;
            game.StateChanged += OnState;
            game.stats.Changed += Refresh;
            game.missions.Changed += Refresh;
            game.Logged += OnLog;
        }

        void Detach()
        {
            if (!subscribed)
            {
                return;
            }

            subscribed = false;
            if (game == null)
            {
                return;
            }

            game.StateChanged -= OnState;
            game.stats.Changed -= Refresh;
            game.missions.Changed -= Refresh;
            game.Logged -= OnLog;
        }

        void OnState(GameState value)
        {
            Refresh();
        }

        void OnLog(string value)
        {
            if (result != null)
            {
                result.text = value;
            }
        }

        public void Refresh()
        {
            if (
                !isActiveAndEnabled
                || game == null
                || game.phase23 == null
                || game.phase23.catalog == null
                || status == null
                || details == null
                || activityButtons.Any(b => b == null)
            )
            {
                return;
            }

            var shell = UIShell.For(this);
            if (shell != null)
            {
                shell.InvalidateFocus();
            }

            var p = game.phase23;
            var e = p.Progress.education;
            var s = p.Progress.social;
            var j = p.Progress.employment;
            status.text =
                $"{p.Location} / {game.growth.Age}세 / DAY {game.clock.Day} / {game.economy.Cash:N0}G\n지식 {game.stats.Knowledge} · 사회성 {game.stats.Social} · 체력 {game.stats.Health} · 멘탈 {game.stats.Mental}\n교육: {e.currentEducationId ?? "미입학"} / 준비도 {e.preparation} / 직업: {p.Jobs.Current?.DisplayName} · 경력 {j.experience} · 출근 {j.workCount} · 승진 {j.rank}";
            var npc = p.catalog.NPC(p.SelectedNpcId);
            var job = p.catalog.Job(j.selectedJobId);
            var r = p.Social.Relationship(p.SelectedNpcId);
            var exam = e.exams.LastOrDefault();
            details.text =
                $"NPC: {npc?.displayName} / {r?.state.ToString() ?? "첫 만남"} · 친밀도 {r?.affinity ?? 0} / 친구 {s.relationships.Count(v => v.friend)}명\n지원: {job?.DisplayName} · 일급 {job?.BaseDailyWage:N0}G · 지식 {job?.RequiredKnowledge} / 사회성 {job?.RequiredSocial} / 체력 {job?.RequiredHealth} / 경력 {job?.RequiredExperience} · {(p.Jobs.Eligible(job) ? "지원 가능" : "조건 미충족")}\n시험: {(exam == null ? "기록 없음" : exam.score + "점 / " + (exam.passed ? "합격" : "재응시 가능") + " / " + (exam.reportReceived ? "성적표 수령" : "미수령"))} · 진로: {e.careerId ?? p.SelectedCareerId}\n공부 {e.studies} / 독서 {e.books} / 숙제 {e.homework} / 졸업 {e.enrollments.Count(v => v.graduated)} · 사회활동 {s.history.Count}\n지원 가능 직업: {string.Join(", ", p.Jobs.Search().Select(v => v.DisplayName))}";
            for (int i = 0; i < activityButtons.Length; i++)
            {
                int id = actionIds[i];
                var a = p.catalog.Find(id);
                string reason;
                bool can = game.CanPerformAction(id, out reason);
                activityButtons[i].interactable = can;
                var spec = game.actions.Describe(id);
                var label = activityButtons[i].GetComponentInChildren<TMP_Text>();
                label.text =
                    (
                        a == null
                            ? (
                                id == 1 ? "휴식"
                                : id == 3 ? "식사"
                                : id == 4 ? "물"
                                : id == 5 ? "수면"
                                : "병원"
                            )
                            : a.displayName
                    )
                    + " · "
                    + spec.days
                    + "일\n"
                    + (
                        can
                            ? $"체력 {spec.core.health} / 멘탈 {spec.core.mental} / 비용 {(id == 26 ? a.cost : Math.Max(0, -spec.money))}G"
                            : reason
                    );
            }
        }
    }
}
