using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public sealed class BirthIntroductionPopup : MonoBehaviour
    {
        public GameObject view;
        public CanvasGroup group;
        public RectTransform frame;
        public Button begin;
        public UIShell shell;
        GameManager game;
        bool showing;
        bool closing;
        Coroutine animation;
        public bool IsShowing => showing;

        void Start()
        {
            if (GameContext.Instance == null)
            {
                return;
            }

            if (frame && frame.Find("System heading") is RectTransform heading &&
                heading.TryGetComponent<TMPro.TMP_Text>(out var title))
            {
                RecordPage.Apply(frame, title, false);
                title.text = "출생 신고서";
                if (frame.Find("Birth greeting") is RectTransform greeting)
                {
                    greeting.GetComponent<TMPro.TMP_Text>().text = "이안의 인생이 시작되었습니다.";
                    LifeRecordUI.Place(greeting, .045f, .595f, .94f, .785f);
                }
                LifeRecordUI.Print(frame, RecordPrint.Life, .69f, .36f, .92f, .67f);
                var facts = frame.Find("Birth facts") as RectTransform;
                if (facts)
                {
                    facts.GetComponent<TMPro.TMP_Text>().text = "아직 혼자 걸을 수 없어요. 부모님의 도움을 받아 먹고, 마시고, 자는 것부터 시작해요.";
                    LifeRecordUI.Place(facts, .045f, .32f, .68f, .61f);
                }

                if (frame.Find("Satire") is RectTransform hint)
                {
                    hint.GetComponent<TMPro.TMP_Text>().text = "무엇을 할지 모르겠다면 현재 미션과 ‘지금 해볼 만한 것’을 확인하세요.";
                }

                begin.GetComponentInChildren<TMPro.TMP_Text>().text = "시작하기";
                LifeRecordUI.StyleButton(begin, true);
            }

            game = GameContext.Instance.game;
            game.StateChanged += Changed;
            GameContext.Instance.transitions.SceneChanged += SceneChanged;
            begin.onClick.AddListener(Confirm);
            Check();
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.StateChanged -= Changed;
            }

            if (GameContext.Instance != null)
            {
                GameContext.Instance.transitions.SceneChanged -= SceneChanged;
            }
        }

        void Changed(GameState state)
        {
            Check();
        }

        void SceneChanged(SceneId id)
        {
            Check();
        }

        void Check()
        {
            if (game == null || closing)
            {
                return;
            }

            if (
                game.BirthIntroductionPending
                && (
                    GameContext.Instance.transitions.CurrentScene == SceneId.Home
                    || game.world != null && game.world.Active
                )
                && !GameContext.Instance.transitions.IsTransitioning
            )
            {
                if (showing)
                {
                    return;
                }

                showing = true;
                game.clock.SetRunning(false);

                if (animation != null)
                {
                    StopCoroutine(animation);
                }

                animation = StartCoroutine(Reveal());
            }
            else if (showing && !game.BirthIntroductionPending)
            {
                showing = false;
                shell.Close(view);
            }
        }

        IEnumerator Reveal()
        {
            yield return new WaitForSecondsRealtime(.8f);
            if (!showing || closing)
            {
                yield break;
            }

            shell.Push(view);
            float duration = UIShell.ReducedMotion ? .001f : .16f;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / duration)
            {
                group.alpha = Mathf.SmoothStep(0, 1, t);
                float scale = Mathf.Round(Mathf.Lerp(.98f, 1, t) * 100) / 100;
                frame.localScale = Vector3.one * scale;
                yield return null;
            }

            frame.localScale = Vector3.one;
            group.alpha = 1;
            UIShell.Focus(view);
        }

        public void Confirm()
        {
            if (!showing || closing)
            {
                return;
            }

            closing = true;
            if (!game.CompleteBirthIntroduction())
            {
                closing = false;
                return;
            }

            showing = false;
            shell.Close(view);
            closing = false;
        }
    }
}
