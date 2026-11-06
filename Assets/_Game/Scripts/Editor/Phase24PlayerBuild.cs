using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.Build.Reporting;using UnityEngine;
namespace IanGame.Editor {
public static class Phase24PlayerBuild {
 [Serializable]class Result {public string platform,status,output,error;public int errors,warnings;public double seconds;public ulong bytes;}
 public static void Begin(BuildTarget target){if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode");string report="Docs/Phase24/BUILD_"+target+".json";File.WriteAllText(report,"{\"status\":\"Running\"}");EditorApplication.delayCall+=()=>{
  string folder="E:/Projects/Build/Phase24/"+target+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(folder);var result=new Result{platform=target.ToString(),output=folder};try{var options=new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),target=target,locationPathName=folder+(target==BuildTarget.Android?"/IanGrowing.apk":"/IanGrowing.exe"),options=BuildOptions.Development};var built=BuildPipeline.BuildPlayer(options);result.status=built.summary.result.ToString();result.errors=(int)built.summary.totalErrors;result.warnings=(int)built.summary.totalWarnings;result.seconds=built.summary.totalTime.TotalSeconds;result.bytes=built.summary.totalSize;}catch(Exception e){result.status="Exception";result.error=e.ToString();}File.WriteAllText(report,JsonUtility.ToJson(result,true));
 };}
}
}
