namespace IanGame
{
    internal static class AndroidQAEvent
    {
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        internal static void Log(string category, string code)
        {
#if UNITY_ANDROID
            UnityEngine.Debug.Log("[IAN][" + category + "] " + code);
#endif
        }

        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        internal static void Breadcrumb(DiagnosticEvent code)
        {
            string category = code switch
            {
                DiagnosticEvent.SaveStarted or DiagnosticEvent.SaveFailed or DiagnosticEvent.SaveCompleted => "SAVE",
                DiagnosticEvent.MissionCompleted => "QUEST",
                DiagnosticEvent.SceneEntered or DiagnosticEvent.SceneTransitionRequested => "SCENE",
                _ => "CORE"
            };
            Log(category, code.ToString());
        }
    }
}
