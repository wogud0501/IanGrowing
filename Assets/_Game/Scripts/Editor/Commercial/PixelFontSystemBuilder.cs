using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
namespace IanGame.Editor {
public static class PixelFontSystemBuilder {
 public const string Folder="Assets/_Game/UI/Pixel/Fonts/Galmuri/";
 public static TMP_FontAsset Body,Heading,Title,Numbers,Fallback;
 static TMP_FontAsset Create(string file,string assetName,int point,int size,string chars,bool dynamic=false){
  string path=Folder+assetName+".asset";var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(existing!=null)return existing;
  var source=AssetDatabase.LoadAssetAtPath<Font>(file);if(source==null)throw new InvalidOperationException("Font source missing "+file);
  var font=TMP_FontAsset.CreateFontAsset(source,point,1,GlyphRenderMode.RASTER_HINTED,size,size,AtlasPopulationMode.Dynamic,true);font.name=assetName;
  AssetDatabase.CreateAsset(font,path);AssetDatabase.AddObjectToAsset(font.material,font);
  if(!dynamic){string missing;font.TryAddCharacters(chars,out missing);font.atlasPopulationMode=AtlasPopulationMode.Static;}
  foreach(var t in font.atlasTextures){t.filterMode=FilterMode.Point;if(!AssetDatabase.Contains(t))AssetDatabase.AddObjectToAsset(t,font);}
  if(dynamic){var so=new SerializedObject(font);var clear=so.FindProperty("m_ClearDynamicDataOnBuild");if(clear!=null){clear.boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();}}
  var info=font.faceInfo;info.scale=1;font.faceInfo=info;EditorUtility.SetDirty(font);return font;
 }
 public static void Load(){Body=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder+"IanBodyPixel.asset");Heading=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder+"IanHeadingPixel.asset");Title=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder+"IanTitlePixel.asset");Numbers=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder+"IanNumbersPixel.asset");Fallback=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder+"IanSymbolsFallback.asset");}
 public static void Complete(){
  Load();Numbers=Create(Folder+"Galmuri11.ttf","IanNumbersPixel",12,512,"0123456789.,+-/%G: ");
  foreach(var f in new[]{Body,Heading,Title,Numbers}){float advance=Enumerable.Range('0',10).Max(c=>f.characterLookupTable[(uint)c].glyph.metrics.horizontalAdvance);foreach(int c in Enumerable.Range('0',10)){var glyph=f.characterLookupTable[(uint)c].glyph;var metrics=glyph.metrics;metrics.horizontalBearingX+=(advance-metrics.horizontalAdvance)/2;metrics.horizontalAdvance=advance;glyph.metrics=metrics;}EditorUtility.SetDirty(f);}
  Numbers.fallbackFontAssetTable=new List<TMP_FontAsset>{Body};EditorUtility.SetDirty(Numbers);
  string license=File.ReadAllText(Folder+"OFL.txt")+"\n\n--- Nanum Gothic fallback ---\n\n"+File.ReadAllText("Assets/_Game/UI/Pixel/Fonts/OFL.txt");File.WriteAllText(Folder+"Combined-Licenses.txt",license);AssetDatabase.ImportAsset(Folder+"Combined-Licenses.txt");var theme=AssetDatabase.LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset");theme.fontLicense=AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"Combined-Licenses.txt");EditorUtility.SetDirty(theme);
  foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Game/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath)){var root=PrefabUtility.LoadPrefabContents(path);try{foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))Style(text);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
  foreach(string name in new[]{"MainMenu","Home","School","City","Workplace"}){var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/"+name+".unity");foreach(var text in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true)))Style(text);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/_Game/Scenes/Home.unity");
 }
 public static string Corpus(){var text=new System.Text.StringBuilder();foreach(var file in Directory.GetFiles("Assets/_Game/Scripts","*.cs",SearchOption.AllDirectories))foreach(Match m in Regex.Matches(File.ReadAllText(file),"\"([^\"\\\\]|\\\\.)*\""))text.Append(m.Value);foreach(var file in Directory.GetFiles("Assets/_Game/Data","*.asset",SearchOption.AllDirectories))text.Append(File.ReadAllText(file));return new string(text.ToString().Where(c=>!char.IsControl(c)&&!char.IsSurrogate(c)).Distinct().ToArray());}
 public static void Build(){
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode");
  AssetDatabase.Refresh();if(!File.ReadAllText(Folder+"OFL.txt").Contains("SIL OPEN FONT LICENSE"))throw new InvalidOperationException("Verified OFL notice required");
  string corpus=Corpus();string hangul=new string(Enumerable.Range(0xAC00,11172).Concat(Enumerable.Range(0x3131,94)).Concat(Enumerable.Range(32,95)).Select(i=>(char)i).ToArray());
  Body=Create(Folder+"Galmuri11.ttf","IanBodyPixel",12,2048,hangul+corpus);
  Heading=Create(Folder+"Galmuri11-Bold.ttf","IanHeadingPixel",12,1024,corpus);
  Title=Create(Folder+"Galmuri14.ttf","IanTitlePixel",16,1024,corpus);
  Fallback=Create("Assets/_Game/UI/Pixel/Fonts/NanumGothic-Regular.ttf","IanSymbolsFallback",12,1024,"",true);
  Body.fallbackFontAssetTable=new List<TMP_FontAsset>{Fallback};Heading.fallbackFontAssetTable=new List<TMP_FontAsset>{Body};Title.fallbackFontAssetTable=new List<TMP_FontAsset>{Body};
  foreach(var f in new[]{Body,Heading,Title,Fallback})EditorUtility.SetDirty(f);
  var theme=AssetDatabase.LoadAssetAtPath<PixelUITheme>("Assets/_Game/UI/Pixel/PixelTheme.asset");theme.font=Body;theme.fontLicense=AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"OFL.txt");EditorUtility.SetDirty(theme);
  foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Game/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath)){
   var root=PrefabUtility.LoadPrefabContents(path);try{foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))Style(t);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  foreach(string name in new[]{"MainMenu","Home","School","City","Workplace"}){
   var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/"+name+".unity");foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))Style(t);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  string missingHangul=new string(hangul.Where(c=>!Body.HasCharacter(c)).ToArray());string missingCorpus=new string(corpus.Where(c=>!Body.HasCharacter(c,true,true)).ToArray());
  foreach(var t in Fallback.atlasTextures)if(t!=null){t.filterMode=FilterMode.Point;if(!AssetDatabase.Contains(t))AssetDatabase.AddObjectToAsset(t,Fallback);}
  AssetDatabase.SaveAssets();Directory.CreateDirectory("Docs/CommercialVisual/V11");File.WriteAllText("Docs/CommercialVisual/V11/FONT_VALIDATION.txt","Source: quiple/galmuri commit 71e1cacf1437a11220307120e63e30bc275312d4\nLicense: bundled SIL OFL 1.1\nMain: RASTER_HINTED, static, Point\nHangul syllables tested: 11172\nMissing Hangul/Jamo/ASCII: "+missingHangul+"\nCorpus unique characters: "+corpus.Length+"\nMissing corpus: "+missingCorpus+"\nBody atlas count: "+Body.atlasTextureCount+"\nHeading atlas count: "+Heading.atlasTextureCount+"\nTitle atlas count: "+Title.atlasTextureCount);
  EditorSceneManager.OpenScene("Assets/_Game/Scenes/Home.unity");
 }
 public static void Style(TMP_Text t){
  if(Body==null)Load();bool title=t.name=="Title"&&t.fontSize>=30;bool heading=(t.fontStyle&FontStyles.Bold)!=0||t.name=="Place"||t.name.Contains("Heading");
  bool numeric=Numbers!=null&&(t.name.Contains("Value")||t.name.Contains("value")||t.name.Contains("Cash")||t.name.Contains("Currency")||t.name.Contains("Clock")||t.name=="Progress");
  t.font=title?Title:heading?Heading:numeric?Numbers:Body;t.fontSharedMaterial=t.font.material;t.fontStyle&=~FontStyles.Bold;
  if(t.name.Contains("License")&&t.text.Contains("SIL"))t.text=File.Exists(Folder+"Combined-Licenses.txt")?File.ReadAllText(Folder+"Combined-Licenses.txt"):File.ReadAllText(Folder+"OFL.txt");
  // Keep readable sizes and existing responsive geometry; no SDF softening or fake bold.
  if(t.fontSize<14)t.fontSize=14;
  EditorUtility.SetDirty(t);
 }
}
}
