#if UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace IanGame.Editor {
// Sends operating-system keyboard events to the focused Game View. Does not inject
// InputSystem events or change its focus/background policies. Uses an isolated save.
public static class Phase26NativeInputProbe {
 [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public INPUTUNION data; }
 [StructLayout(LayoutKind.Explicit)] struct INPUTUNION {
  [FieldOffset(0)] public KEYBDINPUT keyboard;
  [FieldOffset(0)] public MOUSEINPUT mouse;
 }
 [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT { public ushort key,scan; public uint flags,time; public UIntPtr extra; }
 [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT { public int x,y;public uint data,flags,time;public UIntPtr extra; }
 [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint count,INPUT[] inputs,int size);
 [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")] static extern uint MapVirtualKey(uint key,uint type);
 [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
 [DllImport("user32.dll")] static extern bool EnumWindows(WindowCallback callback,IntPtr arg);
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window,StringBuilder text,int max);
 delegate bool WindowCallback(IntPtr window,IntPtr arg);
 [Serializable] public class Report {public string status="Not started",failure;public bool focused;public List<string> observations=new List<string>();}
 public static Report Result=new Report();
 static GameManager game;static SaveData original;static string slot;static bool auto,lifecycle;static IEnumerator<Action> steps;static double deadline;static byte held;static Vector2 before;static LifeWorldController world;
 static FieldInfo Slot=>typeof(SaveManager).GetField("fileName",BindingFlags.Instance|BindingFlags.NonPublic);
 public static void Begin(){
  if(!Application.isPlaying||GameContext.Instance==null)throw new InvalidOperationException("Start Bootstrap Play first.");
  if(steps!=null)throw new InvalidOperationException("Native probe already running.");
  game=GameContext.Instance.game;original=game.CaptureState();slot=(string)Slot.GetValue(game.saves);auto=game.autoSaveAfterAction;lifecycle=game.autoSaveOnLifecycle;
  Slot.SetValue(game.saves,"native-input-"+Guid.NewGuid().ToString("N")+".json");game.autoSaveAfterAction=game.autoSaveOnLifecycle=false;
  Result=new Report{status="Running"};steps=Scenario().GetEnumerator();deadline=0;EditorApplication.update+=Tick;
 }
 static void Tick(){if(EditorApplication.timeSinceStartup<deadline)return;try{if(!Application.isPlaying)throw new Exception("Play stopped during probe");if(!steps.MoveNext()){Finish();return;}steps.Current();Flush();}catch(Exception e){Result.failure=e.ToString();Finish();}}
 static void Delay(double seconds){deadline=EditorApplication.timeSinceStartup+seconds;}
 static void Key(byte key,bool down){uint extended=key>=0x25&&key<=0x28?1u:0u;var input=new INPUT{type=1,data=new INPUTUNION{keyboard=new KEYBDINPUT{scan=(ushort)MapVirtualKey(key,0),flags=8u|extended|(down?0u:2u)}}};uint sent=SendInput(1,new[]{input},Marshal.SizeOf<INPUT>());if(sent!=1)throw new Exception("OS SendInput rejected: "+Marshal.GetLastWin32Error());held=down?key:(byte)0;}
 static void Focus(){
  IntPtr handle=IntPtr.Zero;uint pid=(uint)System.Diagnostics.Process.GetCurrentProcess().Id;
  EnumWindows((IntPtr window,IntPtr arg)=>{uint owner;GetWindowThreadProcessId(window,out owner);if(owner==pid&&IsWindowVisible(window)){var title=new StringBuilder(512);GetWindowText(window,title,512);Result.observations.Add("Visible editor window: "+title);if(title.ToString().Contains("IanGrowing")){handle=window;return false;}}return true;},IntPtr.Zero);
  if(handle==IntPtr.Zero)throw new Exception("No interactive Unity window on editor desktop; native keyboard testing unavailable.");
  bool foreground=SetForegroundWindow(handle);var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Focus();Result.observations.Add("Foreground request="+foreground+", actual="+(GetForegroundWindow()==handle));if(GetForegroundWindow()!=handle)throw new NotSupportedException("Native input unavailable: Windows denied foreground activation. No physical-key result can be inferred.");Delay(.5);
 }
 static IEnumerable<Action> Scenario(){
  while(GameContext.Instance.transitions.IsTransitioning)yield return ()=>Delay(.1);
  yield return ()=>{game.NewGame();GameContext.Instance.transitions.Navigate(SceneId.Home);Delay(2);};
  double until=EditorApplication.timeSinceStartup+20;
  while(GameContext.Instance.transitions.IsTransitioning&&EditorApplication.timeSinceStartup<until)yield return ()=>Delay(.1);
  yield return ()=>{world=UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();if(world==null)throw new Exception("World not loaded");var birth=UnityEngine.Object.FindAnyObjectByType<BirthIntroductionPopup>();if(birth!=null&&birth.IsShowing)birth.begin.onClick.Invoke();Delay(.5);};
  yield return ()=>Focus();
  yield return ()=>{Result.focused=Application.isFocused;Result.observations.Add("Normal Play: focused="+Application.isFocused+", CanControl="+world.CanControl+", timeScale="+Time.timeScale+", shell="+world.hud.shell.Depth);if(!world.CanControl)throw new Exception("World input gated before keyboard test");};
  foreach(byte key in new byte[]{0x57,0x41,0x53,0x44,0x26,0x25,0x28,0x27}){
   byte current=key;
   yield return ()=>{world.Player.Warp(new Vector2(0,-2));before=world.Player.Position;Key(current,true);Delay(.35);};
   yield return ()=>{Result.observations.Add("OS held="+(GetAsyncKeyState(current)<0)+", InputSystem="+UnityEngine.InputSystem.Keyboard.current.anyKey.isPressed+", editorFocus="+(EditorWindow.focusedWindow==null?"none":EditorWindow.focusedWindow.GetType().Name)+", frame="+Time.frameCount);Key(current,false);Vector2 delta=world.Player.Position-before;Result.observations.Add("OS key "+current+" delta="+delta);if(delta.magnitude<.25f)throw new Exception("Native movement failed for key "+current);before=world.Player.Position;Delay(.25);};
   yield return ()=>{if(Vector2.Distance(before,world.Player.Position)>.08f)throw new Exception("Player drifts after key release");};
  }
  yield return ()=>{Key(0x1b,true);Delay(.12);};yield return ()=>{Key(0x1b,false);Delay(.25);};
  yield return ()=>{if(world.hud.shell.Depth==0)throw new Exception("Esc did not open menu");Key(0x1b,true);Delay(.12);};yield return ()=>{Key(0x1b,false);Delay(.3);};
  yield return ()=>{if(!world.CanControl)throw new Exception("Menu close did not restore control");var door=world.objects.First(x=>x.id=="front-door");world.Player.Warp(door.Point);Delay(.2);};
  yield return ()=>{Key(0x45,true);Delay(.12);};yield return ()=>{Key(0x45,false);Delay(1);};
  yield return ()=>{if(game.world.State.zone!="alley"||!world.CanControl)throw new Exception("E door/control after travel failed");Result.observations.Add("OS Esc open/close and E front door passed");};
 }
 static void Finish(){try{if(held!=0)Key(held,false);if(game!=null&&original!=null){game.RestoreState(original);Slot.SetValue(game.saves,slot);game.autoSaveAfterAction=auto;game.autoSaveOnLifecycle=lifecycle;}}finally{steps=null;EditorApplication.update-=Tick;Result.status=Result.failure==null?"Passed":"Failed or unavailable";Flush();}}
 static void Flush(){Directory.CreateDirectory("Docs/Phase26");File.WriteAllText("Docs/Phase26/NATIVE_INPUT_RESULTS.json",JsonUtility.ToJson(Result,true));}
}
}
#endif
