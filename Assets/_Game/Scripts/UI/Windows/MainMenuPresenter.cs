using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public class MainMenuPresenter : MonoBehaviour
    {
        public Button continueButton;
        public Button newGameButton;

        void Start()
        {
            if (!continueButton)
            {
                return;
            }

            var canvas = continueButton.GetComponentInParent<Canvas>();
            var card = canvas.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "Main title card");
            if (!card)
            {
                return;
            }

            LifeRecordUI.Skin(card.GetComponent<Image>(), RecordSurface.Paper);
            var print = LifeRecordUI.Print(card, RecordPrint.City, .40f, .04f, .80f, .82f);
            print.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            print.transform.SetAsFirstSibling();
            foreach (var button in continueButton.transform.parent.GetComponentsInChildren<Button>(true))
            {
                LifeRecordUI.StyleButton(button, button == continueButton);
            }

            var title = card.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "Title");
            var titleColumn = title ? title.transform.parent : null;
            if (titleColumn)
            {
                // Leave room for the action row on shorter displays.
                var titleBox = title.GetComponent<LayoutElement>();
                if (titleBox)
                {
                    titleBox.preferredHeight = 185;
                }

                var footnote = titleColumn.Find("Footnote");
                if (footnote && footnote.TryGetComponent<TMP_Text>(out var note))
                {
                    note.text = "행복은 별도 구매입니다.";
                }
            }

            if (title)
            {
                title.color = LifeRecordUI.Ink;
            }
        }

        void OnEnable()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(Continue);
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(NewGame);
            }
        }

        void OnDisable()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(Continue);
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(NewGame);
            }
        }

        public void Continue()
        {
            if (GameContext.Instance != null)
            {
                GameContext.Instance.transitions.Navigate(SceneId.Home);
            }
        }

        public void NewGame()
        {
            if (GameContext.Instance == null)
            {
                return;
            }

            var shell = UIShell.For(this);
            if (shell != null)
            {
                shell.ConfirmNewLife();
                return;
            }

            GameContext.Instance.game.NewGame();
            Continue();
        }
    }
}
