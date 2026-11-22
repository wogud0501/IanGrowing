using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public sealed partial class GameplayUXPresenter
    {
        Button[] preferenceActions;
        Image[] settingFields;
        TMP_Text[] settingValues;
        int preferenceTab = 4;
        readonly Button[] preferenceTabs = new Button[6];

        void BuildSettingsCategories()
        {
            string[] names =
            {
                "모션 줄이기",
                "글자 크기",
                "알림 표시 시간",
                "환경 설정 · 약관",
                "도움말",
                "추천 표시 / 숨기기",
            };
            preferenceActions = new Button[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                preferenceActions[i] = Preferences.GetComponentsInChildren<Button>(true).First(b => b.name == names[i]);
            }

            var root = Preferences.transform;
            root.gameObject.AddComponent<RecordWindowBounds>();
            LifeRecordUI.Skin(root.GetComponent<Image>(), RecordSurface.Window);
            var header = LifeRecordUI.Surface(root, "Settings masthead", RecordSurface.Header, .012f, .88f, .988f, .984f);
            header.transform.SetAsFirstSibling();
            var title = root.Find("Title").GetComponent<TMP_Text>();
            title.text = "환경 설정";
            MissionRecordLayout.SetText(title, 30, LifeRecordUI.Heading);
            LifeRecordUI.StyleButton(root.Find("뒤로").GetComponent<Button>());
            LifeRecordUI.Surface(root, "Category index", RecordSurface.Inset, .022f, .04f, .283f, .855f).transform.SetAsFirstSibling();
            LifeRecordUI.Print(root, RecordPrint.Settings, .07f, .047f, .235f, .19f);
            string[] tabs = { "게임", "오디오", "그래픽", "입력", "편의", "약관" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                preferenceTabs[i] = ui.Button(
                    Preferences.transform,
                    tabs[i],
                    new Vector2(.039f, .738f - i * .104f),
                    new Vector2(.266f, .827f - i * .104f),
                    () =>
                    {
                        preferenceTab = index;
                        RefreshPreferences();
                    }
                );
            }

            LifeRecordUI.Place((RectTransform)settingsText.rectTransform.parent, .323f, .71f, .948f, .834f);
            settingFields = new Image[preferenceActions.Length];
            settingValues = new TMP_Text[preferenceActions.Length];
            for (int i = 0; i < preferenceActions.Length; i++)
            {
                float y = .575f - i * .101f;
                LifeRecordUI.Place((RectTransform)preferenceActions[i].transform, .32f, y, .955f, y + .091f);
                LifeRecordUI.StyleButton(preferenceActions[i]);
                var label = preferenceActions[i].GetComponentInChildren<TMP_Text>(true);
                LifeRecordUI.Place(label.rectTransform, .035f, 0, .79f, 1);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                settingFields[i] = LifeRecordUI.Surface(preferenceActions[i].transform, "Current value",
                    RecordSurface.Inset, .78f, .18f, .97f, .82f);
                settingValues[i] = LifeRecordUI.Label(settingFields[i].transform, title.font, "", 18,
                    LifeRecordUI.Body, .02f, 0, .98f, 1);
                settingValues[i].alignment = TextAlignmentOptions.Center;
            }

            RefreshPreferences();
        }

        void Setting(string key, int count)
        {
            PlayerPrefs.SetInt(key, (PlayerPrefs.GetInt(key, 0) + 1) % count);
            PlayerPrefs.Save();
            RefreshPreferences();
        }

        void RefreshPreferences()
        {
            if (settingsText == null || preferenceActions == null)
            {
                return;
            }

            for (int i = 0; i < preferenceTabs.Length; i++)
            {
                LifeRecordUI.StyleButton(preferenceTabs[i], i == preferenceTab);
            }

            var actions = new List<(string, Action)>();
            if (preferenceTab == 0)
            {
                settingsText.text =
                    "게임\n추천은 자유롭게 숨길 수 있습니다. 큰 메뉴는 이동과 실시간 생활 시계를 멈춥니다. 직접 시작한 행동·자동 활동의 시간 처리는 기존 규칙을 따릅니다.";
                actions.Add(("추천 표시 / 숨기기", () => city.ShowSuggestions(city.Data.suggestionsHidden)));
                actions.Add(
                    (
                        "안내 다시 보기",
                        () =>
                        {
                            city.ResetUXGuides();
                            OpenHelp();
                        }
                    )
                );
                actions.Add(("자동 활동 설정", () => GetComponent<IdleActivityPresenter>().Open()));
            }
            else if (preferenceTab == 1)
            {
                settingsText.text =
                    "오디오\n전체 음량 "
                    + Mathf.RoundToInt(AudioListener.volume * 100)
                    + "%\n설정한 음량은 다음 실행에도 유지됩니다.";
                actions.Add(
                    (
                        "전체 음량 변경",
                        () =>
                        {
                            AudioListener.volume =
                                AudioListener.volume <= .01f ? 1 : Mathf.Max(0, AudioListener.volume - .25f);
                            PlayerPrefs.SetFloat("ian.audio.master", AudioListener.volume);
                            PlayerPrefs.Save();
                            RefreshPreferences();
                        }
                    )
                );
            }
            else if (preferenceTab == 2)
            {
                settingsText.text = "그래픽  /  " + WorldGraphicsSettings.Names[WorldGraphicsSettings.Preset];
                void Adjust(string label, Action change)
                {
                    actions.Add((label, () =>
                    {
                        change();
                        RefreshPreferences();
                    }));
                }

                Adjust("품질   " + WorldGraphicsSettings.Names[WorldGraphicsSettings.Preset],
                    () => WorldGraphicsSettings.Select((WorldGraphicsSettings.Preset + 1) % 5));
                Adjust("조명 · 날씨   " + WorldGraphicsSettings.Names[WorldGraphicsSettings.Detail],
                    WorldGraphicsSettings.NextDetail);
                Adjust("배경 인구   " + WorldGraphicsSettings.Names[WorldGraphicsSettings.Crowd],
                    WorldGraphicsSettings.NextCrowd);
                Adjust(WorldGraphicsSettings.FrameLimit == 0 ? "수직 동기화" : WorldGraphicsSettings.FrameLimit + " FPS",
                    WorldGraphicsSettings.NextFrameLimit);
                if (WorldGraphicsSettings.HasPipeline)
                {
                    Adjust("월드 해상도   " + WorldGraphicsSettings.RenderScale.ToString("P0"),
                        WorldGraphicsSettings.NextScale);
                    Adjust("윤곽선   " + (WorldGraphicsSettings.Smooth ? "FXAA" : "픽셀"), WorldGraphicsSettings.ToggleAA);
                }
            }
            else if (preferenceTab == 3)
            {
                settingsText.text =
                    "입력\n이동: WASD · 방향키 · 터치 조이스틱\n상호작용: E · 행동 버튼\n뒤로: ESC · 패드 취소 · UI 뒤로\n선택 이동: Tab · 방향키 · 패드\n장문 스크롤: Page Up/Down · 패드 오른쪽 스틱\n\n키 재지정은 이 단계에서 추가하지 않습니다.";
                actions.Add(("도움말", OpenHelp));
            }
            else if (preferenceTab == 4)
            {
                settingsText.text = "<b>편의 기능</b>\n글자와 움직임을 편하게 조정하세요.";
                actions.Add(
                    (
                        "모션 줄이기",
                        () =>
                        {
                            shell.ToggleMotion();
                            RefreshPreferences();
                        }
                    )
                );
                actions.Add(
                    (
                        "글자 크기",
                        () =>
                        {
                            shell.ToggleText();
                            RefreshPreferences();
                            Layout();
                        }
                    )
                );
                actions.Add(
                    (
                        "알림 표시 시간",
                        () =>
                        {
                            PlayerPrefs.SetFloat(
                                "ian.ui.noticeSeconds",
                                PlayerPrefs.GetFloat("ian.ui.noticeSeconds", 4) >= 8
                                    ? 2
                                    : PlayerPrefs.GetFloat("ian.ui.noticeSeconds", 4) + 2
                            );
                            PlayerPrefs.Save();
                            RefreshPreferences();
                        }
                    )
                );
                actions.Add(
                    (
                        "UI 정수 배율",
                        () =>
                        {
                            Setting("ian.ui.integerScale", 3);
                            GetComponentInParent<Canvas>()?.GetComponent<PixelCanvasScale>()?.Apply();
                            Layout();
                        }
                    )
                );
                actions.Add(
                    (
                        "안내 다시 보기",
                        () =>
                        {
                            city.ResetUXGuides();
                            OpenHelp();
                        }
                    )
                );
            }
            else
            {
                settingsText.text =
                    "약관\n이용약관 · 개인정보 · 유료재화 · 환불 · 확률정보 · 운영정책 · 라이선스는 기존 법적 고지에서 확인합니다. 실제 결제 연결과 출시 승인은 기존 정책을 유지합니다.";
                actions.Add(("약관 및 정책", () => shell.GetComponent<ReleaseUI>().OpenLegal()));
                actions.Add(("루비 안내", () => shell.GetComponent<ReleaseUI>().OpenRuby()));
            }

            for (int i = 0; i < preferenceActions.Length; i++)
            {
                var button = preferenceActions[i];
                button.gameObject.SetActive(i < actions.Count);
                button.onClick.RemoveAllListeners();
                if (i >= actions.Count)
                {
                    continue;
                }

                var action = actions[i];
                string value = action.Item1 switch
                {
                    "모션 줄이기" => UIShell.ReducedMotion ? "켜짐" : "꺼짐",
                    "글자 크기" => PlayerPrefs.GetInt("ian.ui.largeText", 0) == 1 ? "115%" : "100%",
                    "알림 표시 시간" => PlayerPrefs.GetFloat("ian.ui.noticeSeconds", 4) + "초",
                    "UI 정수 배율" => PlayerPrefs.GetInt("ian.ui.integerScale", 0) == 0
                        ? "자동"
                        : PlayerPrefs.GetInt("ian.ui.integerScale", 0) + "×",
                    _ => ""
                };
                button.GetComponentInChildren<TMP_Text>().text = action.Item1;
                var field = settingFields[i];
                field.gameObject.SetActive(value.Length > 0);
                bool on = action.Item1 == "모션 줄이기" && UIShell.ReducedMotion;
                LifeRecordUI.Skin(field, on ? RecordSurface.Inverted : RecordSurface.Inset);
                settingValues[i].color = on ? LifeRecordUI.ReverseInk : LifeRecordUI.Heading;
                settingValues[i].text = value;
                button.onClick.AddListener(() => action.Item2());
            }

            shell.InvalidateFocus();
        }
    }
}
