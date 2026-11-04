using System;using System.Linq;using System.Reflection;using UnityEngine;using UnityEditor;
namespace IanGame.Editor {
public static class PixelUIEditorTools {
 const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 public static void Size(int width,int height){
 var assembly=typeof(UnityEditor.Editor).Assembly;var viewType=assembly.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(viewType);var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=singleton.GetProperty("instance",Flags).GetValue(null);var groupEnum=assembly.GetType("UnityEditor.GameViewSizeGroupType");var groupType=Enum.Parse(groupEnum,"Standalone");var group=sizesType.GetMethod("GetGroup",Flags).Invoke(sizes,new[]{groupType});int built=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null),custom=(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null);int index=-1;
 for(int i=0;i<built+custom;i++){var item=group.GetType().GetMethod("GetGameViewSize").Invoke(group,new object[]{i});int w=(int)item.GetType().GetProperty("width",Flags).GetValue(item),h=(int)item.GetType().GetProperty("height",Flags).GetValue(item);if(w==width&&h==height){index=i;break;}}
 if(index<0){var sizeType=assembly.GetType("UnityEditor.GameViewSize");var kind=Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution");var item=Activator.CreateInstance(sizeType,Flags,null,new object[]{kind,width,height,"PixelUI "+width+"x"+height},null);group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{item});index=built+custom;}
 viewType.GetProperty("selectedSizeIndex",Flags).SetValue(view,index);view.Focus();view.Repaint();EditorApplication.QueuePlayerLoopUpdate();
 }
 [MenuItem("Ian/Pixel UI/Verify in Play Mode")]
 public static void Run(){if(!Application.isPlaying||GameContext.Instance==null)throw new InvalidOperationException("Start Bootstrap Play Mode.");var go=new GameObject("Pixel UI isolated verification");UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent<PixelUIRunner>().Begin();}
}
}
