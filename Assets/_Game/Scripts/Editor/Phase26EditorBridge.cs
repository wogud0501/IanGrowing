using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace IanGame.Editor {
// Local, project-scoped fallback when the session exposes no Unity MCP tools.
// Only explicit editor commands are accepted; no arbitrary code execution or save deletion.
[InitializeOnLoad]public static class Phase26EditorBridge {
 [Serializable]class Request {public string id,command;}
 [Serializable]class Reply {public string id,status,scene,message;public bool playing,compiling;public string utc;}
 const string DirectoryPath="Docs/Phase26",RequestPath=DirectoryPath+"/EditorCommand.json";
 static double next;static string last;
 static Phase26EditorBridge(){Directory.CreateDirectory(DirectoryPath);last=SessionState.GetString("ian.phase26.lastCommand","");EditorApplication.update+=Tick;Write("ready","Editor bridge loaded");}
 static void Write(string status,string message,string id=""){File.WriteAllText(DirectoryPath+"/EDITOR_STATE.json",JsonUtility.ToJson(new Reply{id=id,status=status,message=message,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,playing=Application.isPlaying,compiling=EditorApplication.isCompiling,utc=DateTime.UtcNow.ToString("O")},true));}
 static void Tick(){if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating)return;next=EditorApplication.timeSinceStartup+.25;
  string pending=SessionState.GetString("ian.phase26.pendingTest","");if(pending!=""&&Application.isPlaying&&GameContext.Instance!=null&&!GameContext.Instance.transitions.IsTransitioning&&GameContext.Instance.transitions.CurrentScene==SceneId.MainMenu){SessionState.SetString("ian.phase26.pendingTest","");try{var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("IanGame.Editor.Phase26Verification")).FirstOrDefault(t=>t!=null);if(type==null)throw new Exception("Phase26Verification not compiled");var go=new GameObject("Phase26 isolated verification");go.AddComponent(type).SendMessage("Begin",pending);Write("testing",pending);}catch(Exception e){Write("error",e.ToString());}}
  if(!File.Exists(RequestPath))return;Request r;try{r=JsonUtility.FromJson<Request>(File.ReadAllText(RequestPath));}catch{return;}if(r==null||string.IsNullOrEmpty(r.id)||r.id==last)return;last=r.id;SessionState.SetString("ian.phase26.lastCommand",last);File.Delete(RequestPath);
  try{switch(r.command){case "info":Write("ready","Unity "+Application.unityVersion,r.id);break;case "build":LifeWorldBuilder.Build();Write("built","LifeWorld scene, catalog, GameContext and UI saved",r.id);break;case "stageA":case "cycle":case "ui":case "edge":case "integration":case "capture":if(Application.isPlaying)throw new Exception("Stop the previous Play session first");EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity");SessionState.SetString("ian.phase26.pendingTest",r.command);Application.runInBackground=true;EditorApplication.isPlaying=true;Write("starting",r.command,r.id);break;case "stop":EditorApplication.isPlaying=false;Write("stopping","Play stopped",r.id);break;case "refresh":AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();Write("refresh","Compilation requested",r.id);break;default:throw new Exception("Unknown editor command "+r.command);}}catch(Exception e){Write("error",e.ToString(),r.id);}
 }
}
}
