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
        int preferenceTab = 4;

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

            string[] tabs = { "게임", "오디오", "그래픽", "입력", "편의", "약관" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                ui.Button(
                    Preferences.transform,
                    tabs[i],
                    new Vector2(.035f + i * .157f, .8f),
                    new Vector2(.18f + i * .157f, .88f),
                    () =>
                    {
                        preferenceTab = index;
                        RefreshPreferences();
                    }
                );
            }

            settingsText.rectTransform.parent.GetComponent<RectTransform>().anchorMax = new Vector2(.94f, .77f);
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
                    + "%\n설정은 다음 실행에도 유지됩니다. 없는 사운드는 생성하지 않습니다.";
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
                settingsText.text =
                    "그래픽\n환경 효과 "
                    + PlayerPrefs.GetInt("ian.visual.effects", 1)
                    + " / 2\n전환 "
                    + PlayerPrefs.GetInt("ian.visual.transitions", 0)
                    + " / 2\n픽셀 렌더링과 정수 배율은 유지합니다.";
                actions.Add(("환경 효과", () => Setting("ian.visual.effects", 3)));
                actions.Add(("장소 전환", () => Setting("ian.visual.transitions", 3)));
            }
            else if (preferenceTab == 3)
            {
                settingsText.text =
                    "입력\n이동: WASD · 방향키 · 터치 조이스틱\n상호작용: E · 행동 버튼\n뒤로: ESC · 패드 취소 · UI 뒤로\n선택 이동: Tab · 방향키 · 패드\n장문 스크롤: Page Up/Down · 패드 오른쪽 스틱\n\n키 재지정은 이 단계에서 추가하지 않습니다.";
                actions.Add(("도움말", OpenHelp));
            }
            else if (preferenceTab == 4)
            {
                settingsText.text =
                    "편의\n모션 줄이기 "
                    + (UIShell.ReducedMotion ? "켜짐" : "꺼짐")
                    + " · 글자 "
                    + (PlayerPrefs.GetInt("ian.ui.largeText", 0) == 1 ? "115%" : "100%")
                    + "\n알림 "
                    + PlayerPrefs.GetFloat("ian.ui.noticeSeconds", 4)
                    + "초\nUI 배율 "
                    + (
                        PlayerPrefs.GetInt("ian.ui.integerScale", 0) == 0
                            ? "자동"
                            : PlayerPrefs.GetInt("ian.ui.integerScale", 0) + "x · 화면에 맞춰 제한"
                    );
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
                button.GetComponentInChildren<TMP_Text>().text = action.Item1;
                button.onClick.AddListener(() => action.Item2());
            }

            shell.InvalidateFocus();
        }
    }
}
