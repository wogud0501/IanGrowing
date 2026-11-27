using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public partial class UIShell
    {
        GameObject diagnosticList, diagnosticDetail, diagnosticHelp;
        TMP_Text diagnosticStatus, diagnosticPreview;
        readonly Button[] diagnosticRows = new Button[3];
        Button previousReports, nextReports;
        int diagnosticPage;
        List<DiagnosticReport> reports;
        DiagnosticReport selection;
        LocalDiagnostics Recorder => LocalDiagnostics.Current;

        Button DiagnosticButton(GameObject window, string name, float x, float y, float width, Action click)
        {
            return Widgets.Button(window.transform, name, new Vector2(x, y), new Vector2(x + width, y + .092f), click);
        }

        public void OpenDiagnostics()
        {
            if (!diagnosticList)
            {
                BuildDiagnostics();
            }
            diagnosticPage = 0;
            RefreshReports();
            Push(diagnosticList);
        }

        void BuildDiagnostics()
        {
            diagnosticList = Widgets.Window(this, "진단 및 오류 기록");
            diagnosticStatus = Widgets.ScrollText(diagnosticList.transform, new Vector2(.04f,.72f), new Vector2(.96f,.875f));
            for (int i = 0; i < diagnosticRows.Length; i++)
            {
                int row = i;
                diagnosticRows[i] = DiagnosticButton(diagnosticList, "보고서", .04f, .61f - i * .108f, .92f,
                    () => OpenReport(reports[diagnosticPage * diagnosticRows.Length + row]));
                var label = diagnosticRows[i].GetComponentInChildren<TMP_Text>();
                label.richText = false;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
            previousReports = DiagnosticButton(diagnosticList, "이전", .04f, .28f, .17f, () => ChangePage(-1));
            nextReports = DiagnosticButton(diagnosticList, "다음", .22f, .28f, .17f, () => ChangePage(1));
            DiagnosticButton(diagnosticList, "보고서 만들기", .40f, .28f, .30f, () =>
            {
                bool ok = Recorder.Manual();
                string status = Recorder.Store.Failure;
                diagnosticPage = 0;
                RefreshReports(ok ? "수동 보고서를 저장했습니다." : status);
            });
            DiagnosticButton(diagnosticList, "전체 삭제", .71f, .28f, .25f, () =>
                ConfirmSession("진단 기록을 모두 지울까요?", "기기 안의 보고서와 앱이 보관한 내보내기 사본을 삭제합니다.",
                    "삭제", () =>
                    {
                        Recorder.DeleteAll();
                        RefreshReports(Recorder.Store.Failure ?? "기기 안의 진단 기록을 삭제했습니다.");
                    }));
            DiagnosticButton(diagnosticList, "개인정보처리방침", .04f, .16f, .45f, () => GetComponent<ReleaseUI>().OpenDocument("privacy"));
            DiagnosticButton(diagnosticList, "이용약관", .51f, .16f, .45f, () => GetComponent<ReleaseUI>().OpenDocument("terms"));
            DiagnosticButton(diagnosticList, "기록 안내", .04f, .045f, DesktopExit ? .26f : .92f, OpenDiagnosticHelp);
            if (DesktopExit)
            {
                DiagnosticButton(diagnosticList, "기록 폴더 열기", .31f, .045f, .32f,
                    () => Folder(Recorder.Store.Errors));
            }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            DiagnosticButton(diagnosticList, "크래시 폴더", .64f, .045f, .32f, () =>
                ConfirmSession("시스템 덤프 폴더", "덤프에는 메모리 내용이 포함될 수 있습니다. 파일을 직접 검토하세요.",
                    "폴더 열기", () => Folder(UnityEngine.Windows.CrashReporting.crashReportFolder)));
#endif
            diagnosticDetail = Widgets.Window(this, "보고서 상세 · 내보내기 미리보기");
            diagnosticPreview = Widgets.ScrollText(diagnosticDetail.transform, new Vector2(.04f,.22f), new Vector2(.96f,.85f));
            diagnosticPreview.richText = false;
            DiagnosticButton(diagnosticDetail, "파일 내보내기", .04f, .07f, .29f, () => ConfirmExport(false));
#if UNITY_ANDROID && !UNITY_EDITOR
            DiagnosticButton(diagnosticDetail, "파일 공유", .35f, .07f, .29f, () => ConfirmExport(true));
#endif
            DiagnosticButton(diagnosticDetail, "이 기록 삭제", .66f, .07f, .30f, () =>
                ConfirmSession("이 보고서를 지울까요?", "다른 앱에 저장한 복사본은 따로 관리됩니다.", "삭제", () =>
                {
                    bool ok = Recorder.Store.Try(() => Recorder.Store.Remove(selection.id));
                    string status = Recorder.Store.Failure;
                    Close(diagnosticDetail);
                    RefreshReports(ok ? "삭제했습니다." : status);
                }));
        }

        void OpenDiagnosticHelp()
        {
            if (!diagnosticHelp)
            {
                diagnosticHelp = Widgets.Window(this, "오류 기록 안내");
                var help = Widgets.ScrollText(diagnosticHelp.transform, new Vector2(.05f, .10f), new Vector2(.95f, .84f));
                help.text = "문제를 겪은 직후 ‘보고서 만들기’를 누르세요. 최근 행동과 게임 상태를 담습니다.\n\n"
                    + "목록에서 한 건을 골라 내용을 확인한 뒤 내보낼 수 있습니다. 개발사에 전달하는 것은 선택 사항입니다.\n\n"
                    + "게임 기록은 최대 14일, 20건, 총 10MB까지 관리합니다. 기기에서 삭제한 뒤에도 다른 곳에 내보낸 복사본은 남습니다.\n\n"
                    + "강제 종료나 메모리 부족은 기록이 남지 않을 수 있습니다. 시스템에서 생성한 덤프는 별도로 관리되며 메모리 내용이 포함될 수 있습니다.";
#if UNITY_IOS && !UNITY_EDITOR
                help.text += "\n\niOS 자체 진단 파일은 기기 설정의 개인정보 보호 및 보안 → 분석 및 향상 → 분석 데이터에서 확인할 수 있습니다.";
#endif
            }
            Push(diagnosticHelp);
        }

        void ChangePage(int step)
        {
            diagnosticPage += step;
            RefreshReports();
        }

        void Folder(string path)
        {
            if (!Recorder.Store.Try(() => DiagnosticExport.OpenFolder(path)))
            {
                diagnosticStatus.text = "열 수 있는 기록 폴더가 없습니다.";
            }
        }

        void RefreshReports(string status = null)
        {
            reports = Recorder.Store.List();
            diagnosticStatus.text = status ?? Recorder.Store.Failure ??
                ((IanCloudErrorReporter.Active ? "등록된 내부 개발 기기입니다. 실행 오류는 개발자 비공개 저장소에 전송됩니다." : "오류 기록은 이 기기에 저장되며 개발사로 자동 전송되지 않습니다.") + "\n최근 보고서 " + reports.Count + "개");
            diagnosticPage = Mathf.Clamp(diagnosticPage, 0, Mathf.Max(0, (reports.Count - 1) / diagnosticRows.Length));
            previousReports.interactable = diagnosticPage > 0;
            nextReports.interactable = (diagnosticPage + 1) * diagnosticRows.Length < reports.Count;
            for (int i = 0; i < diagnosticRows.Length; i++)
            {
                int index = diagnosticPage * diagnosticRows.Length + i;
                diagnosticRows[i].gameObject.SetActive(index < reports.Count);
                if (index < reports.Count)
                {
                    var report = reports[index];
                    string time = report.utc.Substring(5, 11).Replace('T', ' ');
                    string shortId = report.id.Substring(report.id.Length - 8);
                    diagnosticRows[i].GetComponentInChildren<TMP_Text>().text =
                        time + " UTC  ·  #" + shortId + "\n" + Summary(report);
                }
            }
        }

        static string Summary(DiagnosticReport report)
        {
            return report.type switch
            {
                "Manual" => "수동 진단 보고서",
                "UnexpectedTermination" => "이전 실행 종료 상태 확인",
                "Assert" => "조건 검증 실패 · " + report.method,
                "Error" => "실행 오류 · " + report.message,
                _ => report.type + " · " + report.method
            };
        }

        void OpenReport(DiagnosticReport report)
        {
            selection = report;
            diagnosticPreview.text = report.Display();
            Push(diagnosticDetail);
            Canvas.ForceUpdateCanvases();
            diagnosticPreview.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1;
        }

        void ConfirmExport(bool share)
        {
            string id = selection.id;
            if (!selection.PreparePreview())
            {
                Close(diagnosticDetail);
                RefreshReports("보고서 형식을 확인할 수 없습니다.");
                return;
            }
            string preview = selection.Display();
            if (preview != diagnosticPreview.text)
            {
                OpenReport(selection);
                return;
            }
            string destination = DesktopExit
                ? "미리 본 내용을 TXT 파일로 저장하고 내보내기 폴더를 엽니다."
                : "미리 본 내용만 TXT 파일에 담습니다. 저장 위치나 공유 대상을 직접 선택하세요.";
            ConfirmSession("이 보고서를 내보낼까요?", destination,
                "계속", () =>
                {
                    bool ok = Recorder.Store.Try(() => DiagnosticExport.Confirm(id, preview, share));
                    string status = Recorder.Store.Failure;
                    Close(diagnosticDetail);
                    RefreshReports(ok ? "내보내기를 열었습니다." : status);
                });
        }
    }
}
