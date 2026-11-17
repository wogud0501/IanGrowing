using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame.Commercial
{
    public sealed class CommercialHUD : MonoBehaviour
    {
        public TMP_Text title;
        public TMP_Text subtitle;
        public TMP_Text feedback;
        public CanvasGroup receipt;
        public Button transitionButton;
        public Button effectsButton;
        public RectTransform safeRoot;
        public RectTransform[] rewardCoins = new RectTransform[0];
        GameManager game;
        Coroutine notification;

        void Start()
        {
            if (GameContext.Instance == null)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Bootstrap");
                return;
            }

            game = GameContext.Instance.game;
            game.ActionPerformed += Action;
            game.Logged += Log;
            if (transitionButton != null)
            {
                transitionButton.onClick.AddListener(CycleTransition);
            }

            if (effectsButton != null)
            {
                effectsButton.onClick.AddListener(CycleEffects);
            }

            Settings();
            ApplySafe();
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.ActionPerformed -= Action;
                game.Logged -= Log;
            }
        }

        void ApplySafe()
        {
            if (safeRoot == null || Screen.width == 0 || Screen.height == 0)
            {
                return;
            }

            Rect r = Screen.safeArea;
            safeRoot.anchorMin = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }

        void OnRectTransformDimensionsChange()
        {
            if (Application.isPlaying)
            {
                ApplySafe();
            }
        }

        void Action(int id)
        {
            string text =
                id == 5 ? "수면 기록\n현실은 로그아웃되지 않았습니다."
                : id == 3 ? "식사 완료\n오늘의 생존 비용이 정산되었습니다."
                : id == 4 ? "수분 보충\n프리미엄 수돗물은 다음에."
                : id == 0 ? "급여 명세\n노동의 가치가 통장에 반영됩니다."
                : id >= 7 ? "활동 기록\n성장이 성실하게 접수되었습니다."
                : "휴식 기록\n잠깐의 여유를 승인합니다.";
            if ((id == 11 || id == 12) && game != null)
            {
                var exams = game.phase23.Progress.education.exams;
                if (exams.Count > 0)
                {
                    var e = exams[exams.Count - 1];
                    text = "성적 통지표\n" + e.score + "점 · " + (e.passed ? "합격" : "재응시 가능");
                }
            }

            Show(text);
            if (id == 0)
            {
                Payroll();
            }
        }

        void Log(string text)
        {
            if (text.Contains("부족") || text.Contains("실패"))
            {
                Show(text);
            }
        }

        void Show(string text)
        {
            feedback.text = text;
            if (notification != null)
            {
                StopCoroutine(notification);
            }

            notification = StartCoroutine(Receipt());
        }

        public void Payroll()
        {
            if (game == null || game.phase23 == null)
            {
                return;
            }

            var record = game.phase23.Progress.employment.work.LastOrDefault();
            if (record == null || !record.paid)
            {
                return;
            }

            Show(
                "급여 명세\n지급 완료  +"
                    + record.wage.ToString("N0")
                    + " G\n잔액  "
                    + game.economy.Cash.ToString("N0")
                    + " G"
            );
            StartCoroutine(Coins());
        }

        IEnumerator Coins()
        {
            if (UIShell.ReducedMotion)
            {
                yield break;
            }

            for (int i = 0; i < rewardCoins.Length; i++)
            {
                rewardCoins[i].gameObject.SetActive(true);
            }

            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / .8f)
            {
                for (int i = 0; i < rewardCoins.Length; i++)
                {
                    rewardCoins[i].anchoredPosition = new Vector2(
                        Mathf.Round((i - (rewardCoins.Length - 1) / 2f) * 18 * t),
                        Mathf.Round(12 + Mathf.Sin(t * Mathf.PI) * 60 - i * 3)
                    );
                }

                yield return null;
            }

            foreach (var coin in rewardCoins)
            {
                coin.gameObject.SetActive(false);
            }
        }

        IEnumerator Receipt()
        {
            receipt.gameObject.SetActive(true);
            receipt.alpha = 1;
            yield return new WaitForSecondsRealtime(2.8f);
            receipt.alpha = 0;
            receipt.gameObject.SetActive(false);
        }

        void CycleTransition()
        {
            PlayerPrefs.SetInt("ian.visual.transitions", (PlayerPrefs.GetInt("ian.visual.transitions", 0) + 1) % 3);
            PlayerPrefs.Save();
            Settings();
        }

        void CycleEffects()
        {
            PlayerPrefs.SetInt("ian.visual.effects", (PlayerPrefs.GetInt("ian.visual.effects", 1) + 1) % 3);
            PlayerPrefs.Save();
            Settings();
        }

        void Settings()
        {
            if (transitionButton != null)
            {
                transitionButton.GetComponentInChildren<TMP_Text>().text =
                    "장소 전환 · "
                    + new[] { "전체", "짧게", "끄기" }[
                        Mathf.Clamp(PlayerPrefs.GetInt("ian.visual.transitions", 0), 0, 2)
                    ];
            }

            if (effectsButton != null)
            {
                effectsButton.GetComponentInChildren<TMP_Text>().text =
                    "환경 효과 · "
                    + new[] { "끄기", "낮음", "높음" }[Mathf.Clamp(PlayerPrefs.GetInt("ian.visual.effects", 1), 0, 2)];
            }
        }
    }
}
