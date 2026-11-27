using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IanGame
{
    public class MissionUI : MonoBehaviour
    {
        [NonSerialized]
        public MissionService service;
        public TMP_Text missionTitle;
        public TMP_Text chapterText;
        public TMP_Text conditionText;
        public TMP_Text progressText;
        public TMP_Text rewardText;
        public TMP_Text nextText;
        public TMP_Text toastText;
        public TMP_Text educationHint;
        public Button claimButton;
        public Button actionButton;
        public Button listButton;
        public Button closeListButton;
        public Button educationButton;
        public Button closeEducationButton;
        public Button[] activityButtons;
        public GameObject listPanel;
        public GameObject educationPanel;
        public GameObject toastPanel;
        public RectTransform rowContainer;
        public MissionRowView rowPrefab;
        readonly List<MissionRowView> rows = new List<MissionRowView>();
        UnityAction[] activityListeners;
        RectTransform chapterRail;
        readonly List<Image> chapterMarks = new List<Image>();

        internal void BindChapterRail(RectTransform rail)
        {
            chapterRail = rail;
            Refresh();
        }

        void DrawChapter(int completed, int total)
        {
            if (!chapterRail)
            {
                return;
            }

            for (int i = 0; i < total; i++)
            {
                if (i == chapterMarks.Count)
                {
                    var mark = LifeRecordUI.Rect(chapterRail, "Chapter step " + (i + 1), 0, 0, 1, 1)
                        .gameObject.AddComponent<Image>();
                    mark.raycastTarget = false;
                    chapterMarks.Add(mark);
                }
            }

            for (int i = 0; i < chapterMarks.Count; i++)
            {
                var mark = chapterMarks[i];
                mark.gameObject.SetActive(i < total);
                if (i < total)
                {
                    LifeRecordUI.Place(mark.rectTransform, (float)i / total, 0, (i + .82f) / total, 1);
                    mark.color = i < completed ? LifeRecordUI.Heading : LifeRecordUI.Track;
                }
            }
        }

        public void Bind(MissionService value)
        {
            Detach();
            service = value;
            if (isActiveAndEnabled)
            {
                Attach();
            }

            Refresh();
        }

        void OnEnable()
        {
            Attach();
            Wire(true);
            Refresh();
        }

        void OnDisable()
        {
            Detach();
            Wire(false);
            StopAllCoroutines();
        }

        void Attach()
        {
            if (service == null)
            {
                return;
            }

            service.Changed -= Refresh;
            service.Changed += Refresh;
            service.Completed -= OnComplete;
            service.Completed += OnComplete;
            if (Game != null)
            {
                Game.StateChanged -= OnGameState;
                Game.StateChanged += OnGameState;
                Game.clock.SimulationAdvanced -= UpdateIntroduction;
                Game.clock.SimulationAdvanced += UpdateIntroduction;
            }
        }

        int hintMinute = -1;
        bool showingIntroduction;

        void UpdateIntroduction(double elapsed)
        {
            if (!showingIntroduction)
            {
                return;
            }

            int minute = (int)(Game.clock.SubDaySeconds / 60);
            if (minute != hintMinute && service.ActiveMission && service.GetProgress(service.ActiveMission).state == MissionState.InProgress)
            {
                hintMinute = minute;
                conditionText.text = Game.phase23.Social.IntroductionHint();
            }
        }

        void OnGameState(GameState value)
        {
            Refresh();
        }

        void Detach()
        {
            if (service == null)
            {
                return;
            }

            service.Changed -= Refresh;
            service.Completed -= OnComplete;
            if (Game != null)
            {
                Game.StateChanged -= OnGameState;
                Game.clock.SimulationAdvanced -= UpdateIntroduction;
            }
        }

        void Hook(Button b, UnityAction action, bool add)
        {
            if (b == null)
            {
                return;
            }

            b.onClick.RemoveListener(action);
            if (add)
            {
                b.onClick.AddListener(action);
            }
        }

        void Wire(bool add)
        {
            Hook(claimButton, Claim, add);
            Hook(actionButton, DoSuggested, add);
            Hook(listButton, OpenList, add);
            Hook(closeListButton, CloseList, add);
            Hook(educationButton, OpenEducation, add);
            Hook(closeEducationButton, CloseEducation, add);
            if (activityButtons == null)
            {
                return;
            }

            if (activityListeners == null || activityListeners.Length != activityButtons.Length)
            {
                activityListeners = new UnityAction[activityButtons.Length];
                for (int i = 0; i < activityButtons.Length; i++)
                {
                    int action = i + 7;
                    activityListeners[i] = () => DoActivity(action);
                }
            }

            for (int i = 0; i < activityButtons.Length; i++)
            {
                Hook(activityButtons[i], activityListeners[i], add);
            }
        }

        SceneId CurrentLocation =>
            Game != null && Game.phase23 != null ? Game.phase23.Location
            : GameContext.Instance == null ? SceneId.Home
            : GameContext.Instance.transitions.CurrentScene;

        GameManager Game => service == null ? null : service.GetComponent<GameManager>();

        public void Refresh()
        {
            if (service == null || !service.Ready)
            {
                if (missionTitle != null)
                {
                    missionTitle.text = "미션 준비 중";
                }

                return;
            }

            if (listPanel && listPanel.activeInHierarchy)
            {
                RefreshRows();
            }
            if (listButton)
            {
                listButton.GetComponentInChildren<TMP_Text>().text = "성장 기록";
            }
            var data = service.ActiveMission ?? service.UnclaimedReward;
            if (data == null)
            {
                showingIntroduction = false;
                missionTitle.text = "자유 생활";
                chapterText.text = "성장 기록";
                conditionText.text = "성장 미션을 모두 마쳤습니다.";
                progressText.text = rewardText.text = nextText.text = "";
                DrawChapter(chapterMarks.Count, chapterMarks.Count);
                claimButton.interactable = false;
                actionButton.interactable = false;
                return;
            }

            var p = service.GetProgress(data);
            missionTitle.text = data.Id + " · " + data.title;
            int claimed = service.catalog.missions.Count(m => m.chapter == data.chapter && service.Claimed(m.Id));
            int chapterTotal = service.catalog.missions.Count(m => m.chapter == data.chapter);
            DrawChapter(claimed, chapterTotal);
            string chapterName =
                data.chapter > 0 && data.chapter <= service.catalog.chapterTitles.Length
                    ? service.catalog.chapterTitles[data.chapter - 1]
                    : "CHAPTER " + data.chapter;
            chapterText.text = chapterName + " · " + claimed + " / " + chapterTotal;
            string guidance = MissionGuidance.Summary(data, service);
            conditionText.text = string.IsNullOrWhiteSpace(data.description)
                || data.description == data.title + "의 명시된 조건을 달성하세요."
                ? guidance
                : data.description + "\n\n" + guidance;
            progressText.text =
                MissionRowView.StateName(p.state) + "   " + p.current.ToString("0") + " / " + p.target.ToString("0");
            rewardText.text =
                data.rewards.gold
                + "G · EXP "
                + data.rewards.experience
                + " · 성장 "
                + data.rewards.growthPoints
                + (data.rewards.gachaTickets > 0 ? "\n가챠권 " + data.rewards.gachaTickets + "" : "");
            int ruby = ReleaseConfig.Current?.Reward(RubySource.Mission, data.Id) ?? 0;
            if (ruby > 0)
            {
                rewardText.text += " · 루비 " + ruby;
            }

            var next = service.catalog.missions.FirstOrDefault(m => m.number == data.number + 1);
            nextText.text = next == null || data.number >= 30 ? "자유 생활" : "다음: " + next.Id + " " + next.title;
            claimButton.interactable =
                p.state == MissionState.Completed && Game != null && Game.State == GameState.Playing && !service.IsBusy;
            bool canAction =
                data.suggestedAction >= 0 && p.state != MissionState.Planned && p.state != MissionState.Locked;
            string reason = null;
            SceneId destination =
                Game != null && Game.phase23 != null && Game.phase23.catalog != null
                    ? Game.phase23.Destination(data.suggestedAction)
                    : SceneId.School;
            bool travel = GameContext.Instance != null && data.suggestedAction >= 7 && CurrentLocation != destination;
            actionButton.interactable =
                !service.FreeLifeMode && canAction
                && Game != null
                && Game.State == GameState.Playing
                && (travel || Game.CanPerformAction(data.suggestedAction, out reason));
            var actionLabel = actionButton.GetComponentInChildren<TMP_Text>();
            if (actionLabel != null)
            {
                actionLabel.text = travel
                    ? MissionGuidance.Place(destination) + "로 이동"
                    : MissionGuidance.ActionName(data.suggestedAction, Game);
            }

            showingIntroduction = data.Id == "M014" && Game?.world?.Active == true && p.state == MissionState.InProgress;
            if (showingIntroduction)
            {
                conditionText.text = Game.phase23.Social.IntroductionHint();
                actionButton.interactable = true;
                actionLabel.text = "친구 위치 확인";
            }

            if (activityButtons != null)
            {
                for (int i = 0; i < activityButtons.Length; i++)
                {
                    if (activityButtons[i] != null)
                    {
                        activityButtons[i].interactable = Game != null && Game.CanPerformAction(i + 7, out reason);
                    }
                }
            }

            if (educationHint != null)
            {
                educationHint.text =
                    GameContext.Instance != null && CurrentLocation != SceneId.School
                        ? "수업·과제·시험은 학교에서 진행할 수 있습니다."
                        : "할 일을 선택해 준비 조건과 보상을 확인하세요.";
            }

        }

        void RefreshRows()
        {
            if (service == null || !service.Ready)
            {
                return;
            }

            var visible = service.catalog.missions.Where(m => m && (m.number <= 30 || service.Claimed(m.Id)
                || service.GetProgress(m).state == MissionState.Completed)).ToArray();
            if (rows.Count != visible.Length && rowPrefab != null && rowContainer != null)
            {
                foreach (var r in rows)
                {
                    if (r != null)
                    {
                        Destroy(r.gameObject);
                    }
                }

                rows.Clear();
                foreach (var mission in visible)
                {
                    var r = Instantiate(rowPrefab, rowContainer);
                    r.gameObject.SetActive(true);
                    rows.Add(r);
                }
            }

            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].Render(visible[i], service.GetProgress(visible[i]), service);
            }
        }

        public void Claim()
        {
            if (service != null)
            {
                var reward = service.ActiveMission ?? service.UnclaimedReward;
                if (reward)
                {
                    service.Claim(reward.Id);
                }
            }
        }

        public void DoSuggested()
        {
            var data = service == null ? null : service.ActiveMission;
            if (data == null || data.suggestedAction < 0)
            {
                return;
            }

            if (data.Id == "M014" && Game.world.Active)
            {
                string hint = Game.phase23.Social.IntroductionHint(out string room);
                if (room != null)
                {
                    Game.world.City.Track(room, out _, true);
                }

                Game.world.Host.hud.GetComponent<UINavigationService>()?.CloseAll();
                Game.world.Host.hud.Notify(hint);
                return;
            }

            var destination =
                Game.phase23 != null && Game.phase23.catalog != null
                    ? Game.phase23.Destination(data.suggestedAction)
                    : SceneId.School;
            if (GameContext.Instance != null && data.suggestedAction >= 7 && CurrentLocation != destination)
            {
                GameContext.Instance.transitions.Navigate(destination);
                return;
            }

            DoActivity(data.suggestedAction);
        }

        void DoActivity(int action)
        {
            if (Game != null)
            {
                Game.TryAction(action);
            }

            Refresh();
        }

        void OnComplete(MissionData data)
        {
            if (data.Id == MissionService.FinalGrowthMission)
            {
                Game.Notifications.Publish("성장 미션을 모두 마쳤습니다. 이제 원하는 대로 살아가세요.", NotificationKind.Info);
                return;
            }
            if (Game != null && Game.world != null && Game.world.Active)
            {
                Game.Notifications.Publish(
                    "MISSION COMPLETE\n"
                        + data.title
                        + "\n"
                        + data.completionMessage
                        + "\n보상은 미션 창에서 수령하세요.",
                    NotificationKind.Mission
                );
                return;
            }

            if (toastText != null)
            {
                toastText.text = data.Id + " 완료 · " + data.completionMessage;
            }

            if (toastPanel != null)
            {
                StopAllCoroutines();
                toastPanel.SetActive(true);
                StartCoroutine(HideToast());
            }
        }

        IEnumerator HideToast()
        {
            yield return new WaitForSecondsRealtime(1.25f);
            if (toastPanel != null)
            {
                toastPanel.SetActive(false);
            }
        }

        public void OpenList()
        {
            RefreshRows();
            var shell = UIShell.For(this);
            if (shell != null)
            {
                shell.Push(listPanel);
            }
            else if (listPanel != null)
            {
                listPanel.SetActive(true);
            }
        }

        public void CloseList()
        {
            var shell = UIShell.For(this);
            if (shell != null)
            {
                shell.Close(listPanel);
            }
            else if (listPanel != null)
            {
                listPanel.SetActive(false);
            }
        }

        public void OpenEducation()
        {
            Refresh();
            var shell = UIShell.For(this);
            if (shell != null)
            {
                shell.Push(educationPanel);
            }
            else if (educationPanel != null)
            {
                educationPanel.SetActive(true);
            }
        }

        public void CloseEducation()
        {
            var shell = UIShell.For(this);
            if (shell != null)
            {
                shell.Close(educationPanel);
            }
            else if (educationPanel != null)
            {
                educationPanel.SetActive(false);
            }
        }
    }
}
