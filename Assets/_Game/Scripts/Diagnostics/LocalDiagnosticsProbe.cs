#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace IanGame
{
    // Opt-in console probe for Development players. No forced native crash or save mutation.
    internal static class LocalDiagnosticsProbe
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-ian-test-local-report") < 0)
            {
                return;
            }
            try
            {
                ReadLength(null);
            }
            catch (NullReferenceException exception)
            {
                for (int i = 0; i < 5; i++)
                {
                    Debug.LogException(exception);
                }
            }
            Debug.LogError("QA private payload: Authorization: Bearer FAKE-QA-123 qa@example.invalid /Users/qa/private");
            Debug.LogAssertion("QA assertion");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ReadLength(string value)
        {
            return value.Length;
        }
    }
}
#endif
