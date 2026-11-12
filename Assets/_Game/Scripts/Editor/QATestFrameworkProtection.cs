using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
namespace IanGame.Editor {
// Test Runner can enter Play before its first test method. Protect Bootstrap before that entry,
// including runs launched directly from Unity's Test Runner rather than the MCP bridge.
[InitializeOnLoad]public static class QATestFrameworkProtection {
 static QATestFrameworkProtection(){EditorApplication.playModeStateChanged+=Changed;}
 static void Changed(PlayModeStateChange state){
  if(state==PlayModeStateChange.EnteredEditMode){if(SessionState.GetString("ian.phase26.pendingTest","")=="qaFramework")SessionState.EraseString("ian.phase26.pendingTest");return;}
  if(state!=PlayModeStateChange.ExitingEditMode)return;
  var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi")).FirstOrDefault(t=>t!=null);
  var method=type?.GetMethod("IsRunActive",BindingFlags.Static|BindingFlags.NonPublic);if(method!=null&&(bool)method.Invoke(null,null))SessionState.SetString("ian.phase26.pendingTest","qaFramework");
 }
}
}
