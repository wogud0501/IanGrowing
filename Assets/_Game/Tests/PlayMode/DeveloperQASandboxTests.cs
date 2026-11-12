#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace IanQA.Tests {
public class DeveloperQASandboxTests {
 static Type Type(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("IanGame."+name)).First(t=>t!=null);
 static object Context=>Type("GameContext").GetProperty("Instance").GetValue(null);
 static string Hash(string p){if(!File.Exists(p))return "<missing>";using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p)));}
 static void IsolateBootstrap(Scene scene,LoadSceneMode mode){if(scene.name!="Bootstrap")return;var g=Context.GetType().GetField("game").GetValue(Context);var s=g.GetType().GetField("saves").GetValue(g);s.GetType().GetMethod("EnterTestingChannel").Invoke(s,new object[]{"qa-framework-"+Guid.NewGuid().ToString("N")+".json"});foreach(var name in new[]{"autoLoadOnStart","autoSaveAfterAction","autoSaveOnLifecycle","realtimeLifeEnabled"})g.GetType().GetField(name).SetValue(g,false);}
 [UnityTest]public IEnumerator SandboxCommandsSaveAndRestorePreserveSourceFiles(){
  var paths=Directory.Exists(Application.persistentDataPath)?Directory.GetFiles(Application.persistentDataPath,"ian-black*"):Array.Empty<string>();var hashes=paths.ToDictionary(p=>p,Hash);
  if(Context==null){SceneManager.sceneLoaded+=IsolateBootstrap;yield return SceneManager.LoadSceneAsync("Bootstrap");SceneManager.sceneLoaded-=IsolateBootstrap;}
  float until=Time.realtimeSinceStartup+20;while(Context==null&&Time.realtimeSinceStartup<until)yield return null;Assert.NotNull(Context);
  var transitions=Context.GetType().GetField("transitions").GetValue(Context);while((bool)transitions.GetType().GetProperty("IsTransitioning").GetValue(transitions))yield return null;yield return null;
  var qa=Type("DeveloperQACenter").GetMethod("Ensure").Invoke(null,null);qa.GetType().GetMethod("Open").Invoke(qa,null);var sandbox=qa.GetType().GetProperty("Sandbox").GetValue(qa);Assert.IsTrue((bool)sandbox.GetType().GetProperty("Active").GetValue(sandbox));
  var commands=qa.GetType().GetProperty("Commands").GetValue(qa);commands.GetType().GetMethod("SetCash").Invoke(commands,new object[]{76543L});commands.GetType().GetMethod("SetAge").Invoke(commands,new object[]{20});Assert.IsTrue((bool)commands.GetType().GetMethod("Save").Invoke(commands,null));Assert.IsTrue((bool)sandbox.GetType().GetMethod("VerifyFiles").Invoke(sandbox,null));
  yield return (IEnumerator)qa.GetType().GetMethod("CloseAndRestore").Invoke(qa,null);Assert.IsFalse((bool)sandbox.GetType().GetProperty("Active").GetValue(sandbox));Assert.IsTrue((bool)sandbox.GetType().GetProperty("LastFilesUnchanged").GetValue(sandbox));foreach(var p in paths)Assert.AreEqual(hashes[p],Hash(p),"User save changed: "+p);
 }
}
}
#endif
