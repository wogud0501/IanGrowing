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
