using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
namespace IanGame.Editor {
// Keep new body frames and handheld props in one Point-filtered delivery atlas.
public sealed class Phase31Atlas:IPreprocessBuildWithReport {
 public int callbackOrder=>1;
 public void OnPreprocessBuild(BuildReport report){Configure();}
 public static void Configure(){
  var sprites=AssetDatabase.FindAssets("t:Sprite",new[]{"Assets/_Game/Data/Phase31"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.EndsWith("Ian_RestingBody.asset")).Select(AssetDatabase.LoadAssetAtPath<Sprite>).Where(s=>s!=null).Cast<UnityEngine.Object>().ToArray();
  if(sprites.Length==0)return;const string path="Assets/_Game/Art/Phase31/PlayerLife.spriteatlasv2";
  var atlas=new SpriteAtlasAsset();atlas.Add(sprites);SpriteAtlasAsset.Save(atlas,path);AssetDatabase.ImportAsset(path);
  var importer=(SpriteAtlasImporter)AssetImporter.GetAtPath(path);importer.includeInBuild=true;
  var packing=importer.packingSettings;packing.padding=4;packing.enableRotation=false;packing.enableTightPacking=false;importer.packingSettings=packing;
  var texture=importer.textureSettings;texture.filterMode=FilterMode.Point;texture.generateMipMaps=false;texture.readable=false;texture.sRGB=true;importer.textureSettings=texture;
  foreach(string target in new[]{"DefaultTexturePlatform","Standalone","Android","iPhone"}){var p=importer.GetPlatformSettings(target);p.name=target;p.maxTextureSize=2048;p.format=TextureImporterFormat.RGBA32;p.textureCompression=TextureImporterCompression.Uncompressed;p.overridden=target!="DefaultTexturePlatform";importer.SetPlatformSettings(p);}importer.SaveAndReimport();
 }
}
}
