using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public sealed class IdleActivityPresenter : MonoBehaviour
    {
        public SceneId location;
        public UIShell shell;
        public GameObject panel;
        public GameObject reportPanel;
        public TMP_Text summary;
        public TMP_Text details;
        public TMP_Text reportText;
        public TMP_Text battleText;
        public Button open;
        public Button close;
        public Button startWork;
        public Button startStudy;
        public Button startBattle;
        public Button stop;
        public Button school;
        public Button workplace;
        public Button battle;
        public Button confirmReport;
        public Button offlineToggle;
        public Button statusReport;
        public Button retry;
        public Button fight;
        public SpriteRenderer enemyRenderer;
        public SpriteRenderer ianRenderer;
        public Image enemyHealth;
        GameManager game;
        string replayed;
        bool replaying;
        Coroutine replayRoutine;

        void Start()
        {
            if (GameContext.Instance == null)
            {
                return;
            }

            game = GameContext.Instance.game;
            game.idle.Changed += Refresh;
            game.idle.OfflineReportReady += MaybeReport;
            game.stats.Changed += Refresh;
            game.growth.Changed += Refresh;
            game.StateChanged += OnState;
            GameContext.Instance.transitions.SceneChanged += OnScene;
            open.onClick.AddListener(Open);
            close.onClick.AddListener(() => shell.Close(panel));
            startWork.onClick.AddListener(() => StartActivity("work"));
            startStudy.onClick.AddListener(() => StartActivity("study"));
            startBattle.onClick.AddListener(() => StartActivity("battle"));
            stop.onClick.AddListener(() => game.idle.StopActivity());
            school.onClick.AddListener(() => Travel(SceneId.School));
            workplace.onClick.AddListener(() => Travel(SceneId.Workplace));
            battle.onClick.AddListener(() => Travel(SceneId.Battle));
            confirmReport.onClick.AddListener(ConfirmReport);
            offlineToggle.onClick.AddListener(() => game.idle.SetOfflineEnabled(!game.idle.State.offlineEnabled));
            statusReport.onClick.AddListener(OpenReport);
            retry.onClick.AddListener(() => game.idle.RetryPersistence());
            if (fight != null)
            {
                fight.onClick.AddListener(() =>
                {
                    if (!replaying)
                    {
                        game.idle.FightOnce();
                    }
                });
            }

            Refresh();
            MaybeReport();
            if (enemyRenderer != null)
            {
                StartCoroutine(EnemyLoop());
            }
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.idle.Changed -= Refresh;
                game.idle.OfflineReportReady -= MaybeReport;
                game.stats.Changed -= Refresh;
                game.growth.Changed -= Refresh;
                game.StateChanged -= OnState;
            }

            if (GameContext.Instance != null)
            {
                GameContext.Instance.transitions.SceneChanged -= OnScene;
            }
        }

        void OnState(GameState state)
        {
            if (state == GameState.Starting)
            {
                if (replayRoutine != null)
                {
                    StopCoroutine(replayRoutine);
                }

                replaying = false;
                replayed = null;
                if (enemyRenderer != null)
                {
                    enemyRenderer.color = Color.white;
                }

                if (enemyHealth != null)
                {
                    enemyHealth.fillAmount = 1;
                }
            }

            Refresh();
        }

        void OnScene(SceneId id)
        {
            MaybeReport();
        }

        public void Open()
        {
            Refresh();
            shell.Push(panel);
        }

        void StartActivity(string id)
        {
            game.idle.StartActivity(id);
            Refresh();
        }

        void Travel(SceneId id)
        {
            shell.Close(panel);
            GameContext.Instance.transitions.Navigate(id);
        }

        void MaybeReport()
        {
            if (
                game == null
                || game.BirthIntroductionPending
                || GameContext.Instance.transitions.IsTransitioning
                || GameContext.Instance.transitions.CurrentScene == SceneId.MainMenu
            )
            {
                return;
            }

            if (game.idle.State.report != null && !game.idle.State.report.acknowledged)
            {
                OpenReport();
            }
        }

        public void OpenReport()
        {
            RefreshReport();
            shell.Push(reportPanel);
        }

        void ConfirmReport()
        {
            if (game.idle.State.report == null || game.idle.State.report.acknowledged || game.idle.AcknowledgeReport())
            {
                shell.Close(reportPanel);
            }
        }

        public void Refresh()
        {
            if (game == null || game.State == GameState.Starting)
            {
                return;
            }

            var idle = game.idle;
            var s = idle.State;
            var a = idle.settings == null ? null : idle.settings.Find(s.activityId);
            summary.text = s.active
                ? "자동 · " + a?.title + " / 진행 중"
                : "자동 활동 · " + (idle.IsPersistenceSuspended ? "저장 재시도 필요" : "설정");
            details.text =
                "현재 활동: "
                + (s.active ? a?.title : "중지")
                + "\n"
                + (
                    s.active
                        ? "완료 "
                            + s.sessionCycles
                            + "회 · 다음 주기 "
                            + s.cycleProgressSeconds.ToString("0")
                            + " / "
                            + idle.Period(a).ToString("0")
                            + "초"
                        : "중지 사유: " + (s.stopReason ?? "선택한 활동 없음")
                )
                + "\n\n현재 교육: "
                + (game.phase23.Progress.education.currentEducationId ?? "미입학")
                + " / 교육 EXP "
                + game.phase23.Progress.education.experience
                + "\n현재 직업: "
                + game.phase23.Jobs.Current?.DisplayName
                + " / 경력 "
                + game.phase23.Progress.employment.experience
                + "\n\n자동 노동: 현재 직업 일급 · 근무일 × "
                + game.clock.realSecondsPerGameDay.ToString("0")
                + "초\n자동 공부: 현재 입학 과정 · "
                + idle.Period(idle.settings.Find("study")).ToString("0")
                + "초마다 기존 공부 1회\n자동 전투: "
                + idle.Period(idle.settings.Find("battle")).ToString("0")
                + "초마다 실제 턴 전투 1회\n\n각 완료 주기는 기존 행동의 게임 날짜와 생존 비용을 한 번 소모합니다. 미완료 주기는 보상을 만들지 않고 진행 시간을 보관합니다.\n안전 자원이 부족하거나 교육·고용 상태가 바뀌면 중지합니다.\n\n오프라인 상한: "
                + (idle.settings.maximumOfflineSeconds / 3600).ToString("0.#")
                + "시간 · 결과 확인은 보상을 다시 지급하지 않습니다.";
            string localId =
                location == SceneId.Workplace ? "work"
                : location == SceneId.School ? "study"
                : location == SceneId.Battle ? "battle"
                : null;
            if (localId != null && !s.active)
            {
                string blockedReason;
                bool canStart = idle.CanStart(localId, out blockedReason);
                details.text += "\n\n시작 조건: " + (canStart ? "시작 가능" : blockedReason);
            }

            startWork.gameObject.SetActive(location == SceneId.Workplace);
            startStudy.gameObject.SetActive(location == SceneId.School);
            startBattle.gameObject.SetActive(location == SceneId.Battle);
            string reason;
            startWork.interactable = !s.active && idle.CanStart("work", out reason);
            startStudy.interactable = !s.active && idle.CanStart("study", out reason);
            startBattle.interactable = !s.active && idle.CanStart("battle", out reason);
            stop.interactable = s.active;
            retry.gameObject.SetActive(idle.IsPersistenceSuspended);
            offlineToggle.GetComponentInChildren<TMP_Text>().text =
                "오프라인 자동 활동 · " + (s.offlineEnabled ? "켜짐" : "꺼짐");
            RefreshReport();
            if (battleText != null && !replaying)
            {
                var last = s.battles.LastOrDefault();
                battleText.text =
                    "적: "
                    + idle.enemy.displayName
                    + "\n체력 "
                    + idle.enemy.health
                    + " / 공격 "
                    + idle.enemy.attack
                    + " / 방어 "
                    + idle.enemy.defense
                    + "\n이안 공격 "
                    + idle.Combat.StatsProvider.Attack
                    + " / 방어 "
                    + idle.Combat.StatsProvider.Defense
                    + "\n"
                    + (
                        last == null
                            ? "승리 시 " + idle.enemy.gold + "G / " + idle.enemy.experience + " EXP"
                            : (last.won ? "최근 승리" : "최근 패배")
                                + " · "
                                + last.turns.Count
                                + "턴 / +"
                                + last.gold
                                + "G"
                    );
                if (last != null && last.id != replayed)
                {
                    replayed = last.id;
                    replayRoutine = StartCoroutine(Replay(last));
                }
            }

            if (fight != null)
            {
                fight.interactable = !s.active && !replaying && idle.CanStart("battle", out reason);
            }
        }

        void RefreshReport()
        {
            if (game == null)
            {
                return;
            }

            var r = game.idle.State.report;
            if (r == null)
            {
                reportText.text = "아직 오프라인 정산 내역이 없습니다.";
                return;
            }

            var duration = TimeSpan.FromSeconds(Math.Min(r.elapsedSeconds, TimeSpan.MaxValue.TotalSeconds));
            reportText.text =
                "이안이 없는 동안...\n\n경과시간: "
                + (int)duration.TotalHours
                + "시간 "
                + duration.Minutes
                + "분\n상한 내 시간: "
                + (r.eligibleSeconds / 60).ToString("0.#")
                + "분\n실제 완료 활동: "
                + (r.activitySeconds / 60).ToString("0.#")
                + "분"
                + (r.capped ? " (상한 적용)" : "")
                + "\n자동 노동: "
                + r.workCycles
                + "회\n자동 공부: "
                + r.studyCycles
                + "회 / 교육 EXP +"
                + r.educationExperience
                + "\n지식: "
                + Signed(r.knowledge)
                + "\n전투: "
                + r.battleWins
                + "승 / "
                + r.battleLosses
                + "패\n획득 골드: "
                + Signed(r.gold)
                + "G\n성장 EXP: "
                + Signed(r.experience)
                + "\n\n허기 "
                + Signed(r.hunger)
                + " / 수분 "
                + Signed(r.thirst)
                + "\n체력 "
                + Signed(r.health)
                + " / 멘탈 "
                + Signed(r.mental)
                + "\n\n"
                + (r.clockReversed ? "기기 시각 역행 감지. 해당 시간 보상 없음.\n" : "")
                + "중지 사유: "
                + (r.stopReason ?? "설정한 활동을 계속합니다.")
                + "\n\n이 결과는 이미 저장되었습니다. 확인 버튼은 중복 보상을 지급하지 않습니다.";
        }

        static string Signed(long value) => value >= 0 ? "+" + value : value.ToString();

        IEnumerator EnemyLoop()
        {
            var wait = new WaitForSecondsRealtime(.15f);
            int index = 0;
            while (true)
            {
                if (!UIShell.ReducedMotion && game.idle.enemy.frames.Length > 0)
                {
                    enemyRenderer.sprite = game.idle.enemy.frames[index++ % game.idle.enemy.frames.Length];
                }

                yield return wait;
            }
        }

        IEnumerator Replay(BattleRecord record)
        {
            replaying = true;
            if (fight != null)
            {
                fight.interactable = false;
            }

            foreach (var turn in record.turns)
            {
                battleText.text =
                    "TURN "
                    + turn.number
                    + "\n이안의 공격: "
                    + turn.damageToEnemy
                    + "\n적의 반격: "
                    + turn.damageToPlayer
                    + "\n적 HP "
                    + turn.enemyHealth
                    + " / 이안 HP "
                    + turn.playerHealth;
                if (enemyHealth != null)
                {
                    enemyHealth.fillAmount = (float)turn.enemyHealth / Math.Max(1, game.idle.enemy.health);
                }

                if (enemyRenderer != null)
                {
                    enemyRenderer.color = new Color(1, .5f, .5f);
                }

                yield return new WaitForSecondsRealtime(UIShell.ReducedMotion ? .001f : .18f);
                if (enemyRenderer != null)
                {
                    enemyRenderer.color = Color.white;
                }
            }

            replaying = false;
            Refresh();
        }
    }
}
