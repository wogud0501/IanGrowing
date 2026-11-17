namespace IanGame
{
    // Release players contain only this false gate; developer commands and UI are compiled out.
    public static class DeveloperInputLock
    {
        public static bool Active
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return DeveloperQACenter.Instance != null && DeveloperQACenter.Instance.Visible;
#else
                return false;
#endif
            }
        }
    }
}
