using UnityEditor;
using UnityEngine;

namespace IanGame.Editor
{
    public static class DeveloperQAMenu
    {
        [MenuItem("Ian/Developer QA Center/Open (F10)")]
        public static void Open()
        {
            if (EditorApplication.isPlaying)
            {
                DeveloperQACenter.Ensure().Open();
                return;
            }

            SessionState.SetBool("ian.qa.open", true);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Ian/Developer QA Center/Restore And Close")]
        static void Close()
        {
            if (EditorApplication.isPlaying && DeveloperQACenter.Instance != null)
            {
                DeveloperQACenter.Instance.StartCoroutine(DeveloperQACenter.Instance.CloseAndRestore());
            }
        }

        static CityLifeService Guide => EditorApplication.isPlaying ? GameContext.Instance?.game.world?.City : null;

        [MenuItem("Ian/Developer QA Center/Onboarding/Reset guide")]
        static void ResetGuide()
        {
            Guide?.ResetUXGuides();
        }

        [MenuItem("Ian/Developer QA Center/Onboarding/Start current step")]
        static void StartStep()
        {
            var city = Guide;
            if (city?.CurrentLesson() is GuideLesson lesson)
            {
                city.StartLesson(lesson);
            }
        }

        [MenuItem("Ian/Developer QA Center/Onboarding/Complete current step")]
        static void CompleteStep()
        {
            var city = Guide;
            if (city?.CurrentLesson() is GuideLesson lesson)
            {
                city.Learn(lesson);
            }
        }

        [MenuItem("Ian/Developer QA Center/Onboarding/Preview job unlock")]
        static void PreviewJob()
        {
            var host = GameContext.Instance?.game.world?.Host;
            if (EditorApplication.isPlaying && host)
            {
                host.hud.GetComponent<GameplayUXPresenter>()?.PreviewJobUnlock();
            }
        }

        [MenuItem("Ian/Developer QA Center/Run All Registered Tests")]
        static void All()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("먼저 Play에서 QA Center를 여세요.");
                return;
            }

            var qa = DeveloperQACenter.Ensure();
            qa.Open();
            qa.RunAll();
        }
    }
}
