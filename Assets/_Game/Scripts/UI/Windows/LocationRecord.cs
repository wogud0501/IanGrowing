using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    // This sheet presents the actions allowed by the live room, including rooms hosted by the shared Home scene.
    internal sealed class LocationRecord
    {
        readonly GameManager game;
        readonly SceneId location;
        readonly TMP_Text heading;
        readonly TMP_Text summary;
        readonly TMP_Text result;
        readonly List<Button> commands = new List<Button>();
        readonly List<LifeActivityData> actions = new List<LifeActivityData>();
        readonly string[] restrictions;
        public readonly RectTransform Frame;

        public LocationRecord(Phase23Presenter owner, GameManager game, WorldHUD hud, SceneId location, Action<int> choose)
        {
            this.game = game;
            this.location = location;
            bool school = location == SceneId.School;
            var ui = new PixelUXWidgets(hud);
            var font = hud.needs.font;
            Frame = LifeRecordUI.Surface(owner.panel.transform, "Location record", RecordSurface.Window, 0, 0, 1, 1).rectTransform;
            Frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Frame.gameObject.AddComponent<RecordWindowBounds>();
            var header = LifeRecordUI.Surface(Frame, "Heading", RecordSurface.Header, .012f, .865f, .988f, .987f);
            LifeRecordUI.Label(header.transform, font, school ? "학교 생활 기록" : "근무 기록", 29, LifeRecordUI.Heading,
                .024f, .05f, .72f, .94f);
            ui.Button(header.transform, "뒤로", new Vector2(.82f, .14f), new Vector2(.975f, .86f),
                owner.closeButton.onClick.Invoke);

            var card = LifeRecordUI.Surface(Frame, "Record stub", RecordSurface.Inset, .025f, .165f, .358f, .83f);
            LifeRecordUI.Print(card.transform, school ? RecordPrint.Education : RecordPrint.Life, .14f, .45f, .88f, .92f);
            heading = LifeRecordUI.Label(card.transform, font, "", 22, LifeRecordUI.Secondary, .07f, .235f, .96f, .43f);
            var selection = school ? owner.careerButton : owner.jobButton;
            ui.Button(card.transform, school ? "진로 변경" : "지원 직업 변경", new Vector2(.05f, .055f), new Vector2(.95f, .22f),
                selection.onClick.Invoke);
            summary = ui.ScrollText(Frame, new Vector2(.385f, .565f), new Vector2(.959f, .84f));

            var rows = LifeRecordUI.Rect(Frame, "Available actions", .38f, .173f, .966f, .54f);
            var scroll = rows.gameObject.AddComponent<ScrollRect>();
            var viewport = LifeRecordUI.Rect(rows, "Viewport", 0, 0, 1, 1);
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = LifeRecordUI.Rect(viewport, "Action list", 0, 1, 1, 1);
            content.pivot = new Vector2(.5f, 1);
            var group = content.gameObject.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.spacing = 5;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36;
            foreach (var action in game.phase23.catalog.activities.Where(a => a && a.locations.Contains(location)))
            {
                int id = (int)action.action;
                var b = ui.Button(content, action.displayName, Vector2.zero, Vector2.one, () => choose(id));
                b.gameObject.AddComponent<LayoutElement>().preferredHeight = 55;
                b.gameObject.AddComponent<PixelFocusScroll>();
                actions.Add(action);
                commands.Add(b);
            }

            restrictions = new string[commands.Count];
            ui.Button(Frame, "대화 상대 변경", new Vector2(.026f, .042f), new Vector2(.357f, .136f),
                owner.npcButton.onClick.Invoke);
            result = ui.ScrollText(Frame, new Vector2(.385f, .033f), new Vector2(.966f, .154f));
        }

        public void Refresh(string feedback)
        {
            var p = game.phase23;
            var target = p.catalog.NPC(p.SelectedNpcId);
            if (location == SceneId.School)
            {
                var education = p.Progress.education;
                var enrolled = p.catalog.education.FirstOrDefault(d => d.educationId == education.currentEducationId);
                var exam = education.exams.LastOrDefault();
                heading.text = enrolled?.displayName ?? "미입학";
                string grade = exam == null ? "아직 시험 기록이 없습니다." : $"최근 시험 {exam.score}점";
                summary.text = $"지식 {game.stats.Knowledge}  ·  준비도 {education.preparation}\n공부 {education.studies}회   독서 {education.books}권   과제 {education.homework}회\n{grade}\n희망 진로  {(p.SelectedCareerId == "academic" ? "진학" : "직업 교육")}";
            }
            else
            {
                var employed = p.Progress.employment;
                var job = p.Jobs.Current;
                var selected = p.catalog.Job(employed.selectedJobId);
                heading.text = job?.DisplayName ?? "미취업";
                summary.text = $"예상 일급\n<size=135%><b>{p.Work.Wage:N0} G</b></size>  ·  직급 {employed.rank}\n근무 {employed.workCount}회   경력 {employed.experience}\n지원 직업  {selected?.DisplayName ?? "미선택"}";
            }

            string sharedReason = null;
            int lockedCount = 0;
            bool sameReason = true;
            for (int i = 0; i < commands.Count; i++)
            {
                bool allowed = game.CanPerformAction((int)actions[i].action, out var reason);
                commands[i].interactable = allowed;
                restrictions[i] = allowed ? null : reason;
                if (!allowed)
                {
                    lockedCount++;
                    sharedReason ??= reason;
                    sameReason &= sharedReason == reason;
                }
            }

            bool showOnce = sameReason && lockedCount > 1 && !string.IsNullOrEmpty(sharedReason);
            if (showOnce)
            {
                summary.text += "\n\n잠긴 활동 · " + sharedReason;
            }

            for (int i = 0; i < commands.Count; i++)
            {
                var action = actions[i];
                var button = commands[i];
                string reason = showOnce ? "잠김" : restrictions[i];
                button.GetComponentInChildren<TMP_Text>().text = button.interactable
                    ? $"{action.displayName}   ·   {action.days}일"
                    : $"{action.displayName}   ·   {reason}";
            }

            result.text = string.IsNullOrEmpty(feedback) ? $"대화 상대   {target?.displayName ?? "없음"}" : feedback;
        }
    }
}
