using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace IanGame.Editor {
public static class AdvancedOverhaulSetup {
 [MenuItem("Ian/Life World/Advanced/Apply Connected Advanced Overhaul")]
 public static void Apply(){
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play first");
  AdvancedLifeSetup.ApplyAnimation();AdvancedDoorSetup.Apply();AdvancedMobileSetup.Apply();AdvancedNPCSetup.Apply();
  AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
 }
}
}
