using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public partial class UIShell
    {
        enum SaveDestination
        {
            Stay,
            Title,
            Quit
        }

#if UNITY_ANDROID || UNITY_IOS
        public static bool DesktopExit => false;
#else
        public static bool DesktopExit =>
            Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer
            || Application.isEditor;
#endif

        SessionPreferences titlePreferences;
        GameObject sessionDialog;
        TMP_Text dialogHeading;
        TMP_Text dialogBody;
        TMP_Text dialogAction;
        TMP_Text saveStatus;
        float savedNoticeUntil;
        Action confirmed;
        bool saving;
        bool quitting;
        bool pauseBuilt;
        PixelUXWidgets widgets;
        TMP_FontAsset SessionFont => !mainMenu && game?.world?.Host?.hud?.needs != null
            ? game.world.Host.hud.needs.font
            : baseGroup.GetComponentInChildren<TMP_Text>(true).font;
        PixelUXWidgets Widgets => widgets ??= new PixelUXWidgets(SessionFont, LifeRecordUI.Frame(RecordSurface.Window));

        RectTransform MenuSurface(string name, float height, out GameObject root)
        {
            var layer = LifeRecordUI.Rect(overlayRoot, name, 0, 0, 1, 1);
            var dimmer = layer.gameObject.AddComponent<Image>();
            dimmer.color = new Color(0, 0, 0, .62f);
            root = layer.gameObject;
            var card = LifeRecordUI.Surface(layer, "Session card", RecordSurface.Window, .5f, .5f, .5f, .5f);
            card.raycastTarget = true;
            var fit = card.gameObject.AddComponent<SessionCardBounds>();
            fit.SetHeight(height);
            root.SetActive(false);
            return card.rectTransform;
        }

        void BuildPause()
        {
            if (pauseBuilt)
            {
                return;
            }

            pauseBuilt = true;
            var card = MenuSurface("Pause", 555, out pausePanel);
            LifeRecordUI.Label(card, SessionFont, "일시정지", 32, LifeRecordUI.Heading, .07f, .855f, .94f, .97f);
            float step = DesktopExit ? .123f : .163f;
            float rowHeight = DesktopExit ? .105f : .145f;
            void Choice(string label, int row, float left, float right, Action action)
            {
                float top = .84f - step * row;
                var button = Widgets.Button(card, label, new Vector2(left, top - rowHeight), new Vector2(right, top), action);
                if (!DesktopExit)
                {
                    MissionRecordLayout.SetText(button.GetComponentInChildren<TMP_Text>(), 22, LifeRecordUI.Body);
                }
            }

            Choice("계속하기", 0, .07f, .93f, Pop);
            Choice("저장하기", 1, .07f, .93f, () => BeginSave(SaveDestination.Stay));
            var hud = game?.world?.Host?.hud;
            var ux = hud ? hud.GetComponent<GameplayUXPresenter>() : null;
            if (hud)
            {
                Choice("생활 메뉴", 2, .07f, .495f, () =>
                {
                    if (ux && ux.Ready)
                    {
                        ux.OpenHub();
                    }
                    else
                    {
                        Push(hud.menuPanel);
                    }
                });
                Choice("설정", 2, .51f, .93f, OpenSettings);
            }
            else
            {
                Choice("설정", 2, .07f, .93f, OpenSettings);
            }

            Choice("메인 메뉴로", 3, .07f, .93f, RequestTitle);
            if (DesktopExit)
            {
                Choice("저장 후 종료", 4, .07f, .93f, RequestQuit);
            }

            saveStatus = LifeRecordUI.Label(card, SessionFont, "", 17, LifeRecordUI.Secondary, .07f, .025f, .93f, .19f);
        }

        public void ConfirmSession(string heading, string body, string action, Action accept)
        {
            if (!sessionDialog)
            {
                var card = MenuSurface("Session confirmation", 320, out sessionDialog);
                dialogHeading = LifeRecordUI.Label(card, SessionFont, "", 29, LifeRecordUI.Heading, .07f, .69f, .93f, .94f);
                dialogBody = LifeRecordUI.Label(card, SessionFont, "", 19, LifeRecordUI.Body, .07f, .34f, .93f, .69f);
                Widgets.Button(card, "취소", new Vector2(.07f, .08f), new Vector2(.47f, .29f), Pop);
                var submit = Widgets.Button(card, "확인", new Vector2(.50f, .08f), new Vector2(.93f, .29f), () =>
                {
                    if (saving || Top != sessionDialog)
                    {
                        return;
                    }

                    var actionToRun = confirmed;
                    confirmed = null;
                    Pop();
                    actionToRun?.Invoke();
                });
                dialogAction = submit.GetComponentInChildren<TMP_Text>(true);
            }

            confirmed = accept;
            dialogHeading.text = heading;
            dialogBody.textWrappingMode = TextWrappingModes.Normal;
            dialogBody.text = body;
            dialogAction.text = action;
            Push(sessionDialog);
        }

        public void RequestTitle()
        {
            if (saving || mainMenu || GameContext.Instance.transitions.IsTransitioning)
            {
                return;
            }

            EnsurePauseForExit();
            ConfirmSession("메인 메뉴로 돌아갈까요?", "현재 진행을 저장하고 타이틀로 이동합니다.",
                "저장 후 이동", () => BeginSave(SaveDestination.Title));
        }

        public void RequestQuit()
        {
            if (!DesktopExit || saving || GameContext.Instance.transitions.IsTransitioning)
            {
                return;
            }

            if (mainMenu)
            {
                ConfirmSession("게임을 종료할까요?", "", "종료", QuitProcess);
            }
            else
            {
                EnsurePauseForExit();
                ConfirmSession("게임을 종료할까요?", "현재 진행을 저장한 뒤 종료합니다.",
                    "저장 후 종료", () => BeginSave(SaveDestination.Quit));
            }
        }

        void EnsurePauseForExit()
        {
            BuildPause();
            if (Depth == 0)
            {
                OpenPause();
            }
        }

        bool WantsToQuit()
        {
            if (quitting || mainMenu || !DesktopExit)
            {
                return true;
            }

            if (!saving && !GameContext.Instance.transitions.IsTransitioning)
            {
                if (Depth == 0)
                {
                    OpenPause();
                }

                RequestQuit();
            }

            return false;
        }

        void BeginSave(SaveDestination destination)
        {
            if (saving || GameContext.Instance.transitions.IsTransitioning)
            {
                return;
            }

            saving = true;
            if (Top != null)
            {
                Group(Top).interactable = false;
            }

            saveStatus.text = "저장 중…";
            StartCoroutine(Commit(destination));
        }

        IEnumerator Commit(SaveDestination destination)
        {
            yield return null;
            bool success = game.SaveForMenu(out string reason);
            saving = false;
            Sync();
            if (!success)
            {
                Debug.LogWarning("[Save] " + reason);
                saveStatus.text = "저장 실패";
                string detail = reason.StartsWith("저장 실패", StringComparison.Ordinal)
                    ? "저장 파일을 쓸 수 없습니다. 저장 공간과 쓰기 권한을 확인하세요."
                    : reason;
                ConfirmSession("저장 실패", detail, "다시 시도", () => BeginSave(destination));
                yield break;
            }

            saveStatus.text = "저장 완료";
            savedNoticeUntil = Time.unscaledTime + 2.0f;
            if (destination == SaveDestination.Quit)
            {
                QuitProcess();
            }
            else if (destination == SaveDestination.Title)
            {
                var transitions = GameContext.Instance.transitions;
                if (transitions.Navigate(SceneId.MainMenu))
                {
                    while (transitions.IsTransitioning)
                    {
                        yield return null;
                    }
                }

                if (this && pausePanel && !string.IsNullOrEmpty(transitions.LastError))
                {
                    saveStatus.text = transitions.LastError;
                }
            }
        }

        void QuitProcess()
        {
            quitting = true;
            game?.EnterTitle();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
