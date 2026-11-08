using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace IanGame.Editor {
// Records only gameplay keys; never records composed text or arbitrary key input.
// Backend events alone do not prove physical input: the integration harness also
// injects events. The report explicitly labels that harness when it is present.
[InitializeOnLoad]public static class Phase26InputTrace {
 [Serializable]class Entry {public int frame;public string keys,layout,device,focus,eventKind;public bool ime,canControl,injectedHarness;public Vector2 position;}
 [Serializable]class Trace {public List<Entry> entries=new List<Entry>();}
 static Trace trace=new Trace();static string previous="";static bool dirty;
 static readonly Dictionary<Keyboard,Action<char>> textHandlers=new Dictionary<Keyboard,Action<char>>();
 static Phase26InputTrace(){InputSystem.onEvent+=Record;InputSystem.onDeviceChange+=DeviceChanged;foreach(var device in InputSystem.devices)if(device is Keyboard k)HookText(k);EditorApplication.update+=Flush;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode){trace=new Trace();previous="";}};AssemblyReloadEvents.beforeAssemblyReload+=Unhook;}
 static void HookText(Keyboard k){if(textHandlers.ContainsKey(k))return;Action<char> handler=character=>{if(!Application.isPlaying||"wasdWASDeEiImMㅈㅁㄴㅇㄷㅑㅡ".IndexOf(character)<0)return;var world=UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();if(world!=null)Add(world,k,"TextOnly:"+character,"TextEvent");};textHandlers.Add(k,handler);k.onTextInput+=handler;}
 static void DeviceChanged(InputDevice device,InputDeviceChange change){if(!(device is Keyboard k))return;if(change==InputDeviceChange.Added)HookText(k);else if(change==InputDeviceChange.Removed&&textHandlers.TryGetValue(k,out var handler)){k.onTextInput-=handler;textHandlers.Remove(k);}}
 static void Unhook(){InputSystem.onEvent-=Record;InputSystem.onDeviceChange-=DeviceChanged;EditorApplication.update-=Flush;foreach(var pair in textHandlers)pair.Key.onTextInput-=pair.Value;textHandlers.Clear();}
 static void Record(InputEventPtr input,InputDevice device){
  if(!Application.isPlaying||!(device is Keyboard k))return;
  var world=UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();if(world==null)return;
  if(!input.IsA<StateEvent>()&&!input.IsA<DeltaStateEvent>())return;
  var keys=new List<string>();foreach(var key in new[]{Key.W,Key.A,Key.S,Key.D,Key.UpArrow,Key.LeftArrow,Key.DownArrow,Key.RightArrow,Key.E,Key.I,Key.Escape})if(k[key].ReadValueFromEvent(input)>.5f)keys.Add(key.ToString());
  string state=string.Join(",",keys);if(state==previous)return;previous=state;
  Add(world,k,state,"KeyState");
 }
 static void Add(LifeWorldController world,Keyboard k,string state,string kind){
  trace.entries.Add(new Entry{frame=Time.frameCount,keys=state,eventKind=kind,layout=k.keyboardLayout,device=k.description.interfaceName,focus=EditorWindow.focusedWindow==null?"none":EditorWindow.focusedWindow.GetType().Name,ime=k.imeSelected.isPressed,canControl=world.CanControl,position=world.Player.Position,injectedHarness=UnityEngine.Object.FindAnyObjectByType<Phase26Verification>()!=null});
  if(trace.entries.Count>80)trace.entries.RemoveAt(0);dirty=true;
 }
 static void Flush(){if(!dirty)return;dirty=false;Directory.CreateDirectory("Docs/Phase26");File.WriteAllText("Docs/Phase26/LIVE_KEY_TRACE.json",JsonUtility.ToJson(trace,true));}
}
}
