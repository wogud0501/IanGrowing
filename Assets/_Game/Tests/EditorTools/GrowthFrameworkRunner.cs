using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace IanQA.EditorTools
{
    [InitializeOnLoad]
    public sealed class GrowthFrameworkRunner : ICallbacks
    {
        static readonly TestRunnerApi api;

        static GrowthFrameworkRunner()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new GrowthFrameworkRunner());
        }

        [MenuItem("Ian/Growth/Run registered Unity Framework tests")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new Exception("Stop Play first");
            }

            Directory.CreateDirectory("Docs/AgeGrowth");
            SessionState.SetString("ian.phase26.pendingTest", "qaFramework");
            SessionState.SetBool("ian.phase26.protectWrites", true);
            SessionState.SetBool("ian.phase26.automationHold", true);
            SessionState.SetBool("ian.growth.framework", true);
            File.WriteAllText("Docs/AgeGrowth/FRAMEWORK_STATUS.txt", "Running " + DateTime.UtcNow.ToString("O"));
            api.Execute(
                new ExecutionSettings(
                    new Filter
                    {
                        testMode = TestMode.PlayMode,
                        testNames = new[]
                        {
                            "IanQA.Tests.GrowthVisualCatalogTests.EveryAgeHasExactlyOneVisualProfileAndOriginalAdultRemainsDistinct",
                            "IanQA.Tests.GrowthVisualCatalogTests.InfantNeverContainsLocomotionAndAllAgeBanksHaveRealFrames",
                        },
                    }
                )
            );
        }

        public void RunStarted(ITestAdaptor tests) { }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            if (!SessionState.GetBool("ian.growth.framework", false))
            {
                return;
            }

            SessionState.SetBool("ian.growth.framework", false);
            TestRunnerApi.SaveResultToFile(result, Path.GetFullPath("Docs/AgeGrowth/UNITY_FRAMEWORK_RESULTS.xml"));
            File.WriteAllText(
                "Docs/AgeGrowth/FRAMEWORK_STATUS.txt",
                result.ResultState
                    + " PASS="
                    + result.PassCount
                    + " FAIL="
                    + result.FailCount
                    + " "
                    + DateTime.UtcNow.ToString("O")
            );
        }
    }
}
