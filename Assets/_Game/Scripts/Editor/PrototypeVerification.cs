using UnityEditor;

namespace IanGame.Editor
{
    public static class PrototypeVerification
    {
        [MenuItem("Ian/Verify Active 2D Play Mode")]
        public static void RunMenu()
        {
            Stage1Verification.RunMenu();
        }

        public static string[] Run()
        {
            return Stage1Verification.Run();
        }
    }
}
