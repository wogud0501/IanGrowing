using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public class MainMenuPresenter : MonoBehaviour
    {
        public Button continueButton;
        public Button newGameButton;
        UIShell shell;
        RawImage street;
        RectTransform plate;
        TMP_Text notice;
        Vector2 lastSize;
        bool entering;

        void Start()
        {
            if (GameContext.Instance == null)
            {
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Bootstrap");
                return;
            }

            shell = UIShell.For(this);
            var content = (RectTransform)shell.baseGroup.transform;
            var font = newGameButton.GetComponentInChildren<TMP_Text>(true).font;
            var ui = new PixelUXWidgets(font, LifeRecordUI.Frame(RecordSurface.Window));
            foreach (Transform child in content)
            {
                child.gameObject.SetActive(false);
            }

            if (content.TryGetComponent<LayoutGroup>(out var layout))
            {
                layout.enabled = false;
            }

            var canvas = shell.GetComponentInParent<Canvas>().transform;
            street = LifeRecordUI.Rect(canvas, "Residential street at night", 0, 0, 1, 1).gameObject.AddComponent<RawImage>();
            street.transform.SetSiblingIndex(content.parent.GetSiblingIndex());
            street.texture = Resources.Load<Texture2D>("IanLedger/TitleStreet");
            street.raycastTarget = false;

            plate = LifeRecordUI.Rect(content, "Title and navigation", 0, 0, .4f, 1);
            var backing = plate.gameObject.AddComponent<Image>();
            backing.color = new Color32(15, 15, 15, 242);
            backing.raycastTarget = false;
            var rule = LifeRecordUI.Rect(plate, "Title rule", .11f, .695f, .89f, .698f).gameObject.AddComponent<Image>();
            rule.color = LifeRecordUI.Inactive;
            rule.raycastTarget = false;
            var imprint = LifeRecordUI.Label(plate, font, "IAN GROWING", 18, LifeRecordUI.Secondary, .11f, .825f, .90f, .91f);
            imprint.characterSpacing = 12;
            var logo = LifeRecordUI.Label(plate, font, "이안 키우기", 56, LifeRecordUI.Heading, .10f, .71f, .95f, .846f);
            logo.enableAutoSizing = true;
            logo.fontSizeMin = 30;
            logo.fontSizeMax = 56;
            logo.textWrappingMode = TextWrappingModes.NoWrap;
            Button Choice(string label, float y, Action action)
            {
                var button = ui.Button(plate, label, new Vector2(.11f, y), new Vector2(.89f, y + .085f), action);
                var states = button.spriteState;
                states.selectedSprite = LifeRecordUI.Frame(RecordSurface.InvertedFocus);
                button.spriteState = states;
                return button;
            }

            continueButton = Choice("이어하기", .55f, Continue);
            newGameButton = Choice("새 게임", .445f, NewGame);
            Choice("설정", .34f, shell.OpenSettings);
            if (UIShell.DesktopExit)
            {
                Choice("게임 종료", .235f, shell.RequestQuit);
            }

            notice = LifeRecordUI.Label(plate, font, "", 16, LifeRecordUI.Body, .11f, .08f, .90f, .225f);
            LifeRecordUI.Label(plate, font, "v" + Application.version, 16, LifeRecordUI.Inactive, .11f, .012f, .89f, .075f);
            Reflow();
            var game = GameContext.Instance.game;
            bool valid = game.saves.ReadContinue(out _, out bool exists, out bool recovery, out string reason);
            continueButton.interactable = valid;
            if (exists && (!valid || recovery))
            {
                notice.text = valid ? "저장 사본으로 복구할 수 있습니다." : reason;
            }

            if (shell.Depth == 0)
            {
                UIShell.Focus(content.gameObject);
            }
        }

        void LateUpdate()
        {
            if (!plate)
            {
                return;
            }

            Reflow();
            if (!entering && shell.Depth == 0 && LocalDiagnostics.Current && LocalDiagnostics.Current.ConsumeNotice())
            {
                shell.ConfirmSession("지난 실행 확인", "이전 게임 실행이 정상적으로 종료되지 않았을 수 있습니다.",
                    "오류 기록 보기", shell.OpenDiagnostics);
            }
            var transitions = GameContext.Instance.transitions;
            if (entering && !transitions.IsTransitioning && !string.IsNullOrEmpty(transitions.LastError))
            {
                entering = false;
                GameContext.Instance.game.EnterTitle();
                notice.text = transitions.LastError;
            }
        }

        void Reflow()
        {
            Vector2 size = ((RectTransform)plate.parent).rect.size;
            if (size == lastSize || size.x <= 0 || size.y <= 0)
            {
                return;
            }

            lastSize = size;
            bool portrait = size.x < size.y;
            LifeRecordUI.Place(plate, portrait ? .04f : .035f, portrait ? .025f : .055f,
                portrait ? .96f : Mathf.Clamp(500 / size.x, .31f, .53f), portrait ? .61f : .945f);
            if (street.texture)
            {
                float factor = Screen.width * (float)street.texture.height / (Screen.height * (float)street.texture.width);
                float w = Mathf.Min(1, factor);
                float h = Mathf.Min(1, 1 / factor);
                street.uvRect = new Rect((1 - w) * .55f, (1 - h) * .5f, w, h);
            }
        }

        bool Available => !entering
            && !GameContext.Instance.transitions.IsTransitioning;

        public void Continue()
        {
            if (!Available || shell.Depth != 0)
            {
                return;
            }

            BeforePlay(RestoreSavedLife);
        }

        internal void BeforePlay(Action action)
        {
            if (!LegalDocumentService.NeedsConsent)
            {
                action();
            }
            else if (shell.TryGetComponent<ReleaseUI>(out var release))
            {
                release.BeforePlay(action);
            }
            else
            {
                notice.text = "약관을 불러올 수 없습니다. 게임을 다시 실행하세요.";
            }
        }

        void RestoreSavedLife()
        {
            var saves = GameContext.Instance.game.saves;
            if (!saves.ReadContinue(out var data, out _, out bool recovery, out string reason))
            {
                notice.text = reason;
                continueButton.interactable = false;
                return;
            }

            void Restore()
            {
                try
                {
                    GameContext.Instance.game.ContinueSnapshot(data);
                    EnterWorld(SceneTransitionService.ResumeTarget(data.resumeScene));
                }
                catch (Exception error)
                {
                    GameContext.Instance.game.EnterTitle();
                    entering = false;
                    Debug.LogWarning("[Continue] " + error);
                    notice.text = "저장 데이터를 복원할 수 없습니다. 원본 파일은 보존됩니다.";
                }
            }

            if (recovery)
            {
                shell.ConfirmSession("저장 복구", reason, "복구 후 이어하기", Restore);
            }
            else
            {
                Restore();
            }
        }

        public void NewGame()
        {
            if (!Available || shell.Depth != 0)
            {
                return;
            }

            BeforePlay(ChooseNewLife);
        }

        void ChooseNewLife()
        {
            GameContext.Instance.game.saves.ReadContinue(out _, out bool exists, out _, out _);
            if (exists)
            {
                shell.ConfirmSession("새 게임을 시작할까요?", "현재 저장된 진행을 새 인생으로 교체합니다.", "새 게임 시작", StartLife);
            }
            else
            {
                StartLife();
            }
        }

        internal void StartLife()
        {
            var game = GameContext.Instance.game;
            if (!game.TryStartNewLife(out string reason))
            {
                game.EnterTitle();
                Debug.LogWarning("[New Game] " + reason);
                notice.text = "새 게임을 저장할 수 없습니다. 저장 공간을 확인한 뒤 다시 시도하세요.";
                return;
            }

            EnterWorld();
        }

        void EnterWorld(SceneId destination = SceneId.LifeWorld)
        {
            var context = GameContext.Instance;
            entering = context.transitions.Navigate(destination);
            if (!entering)
            {
                context.game.EnterTitle();
                notice.text = context.transitions.LastError ?? "입장할 수 없습니다. 다시 시도하세요.";
            }
        }
    }
}
