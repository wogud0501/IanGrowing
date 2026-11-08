using System;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace IanGame.Editor {
// Code-authored pixel meshes/tiles: independent objects, never a baked room photograph.
public static class LifeWorldArtBuilder {
 static readonly System.Collections.Generic.Dictionary<string,Sprite> Cache=new System.Collections.Generic.Dictionary<string,Sprite>(); public const string Folder="Assets/_Game/Art/LifeWorld/";
 public static Sprite Solid()=>Make("White",8,8,box=>box(0,0,8,8,new Color32(255,255,255,255)),new Vector2(.5f,.5f));
 public static Sprite Tile(string kind){return Make(kind,128,128,(box)=>{Color32 baseColor=kind=="Wood"?C(158,113,76):kind=="Jade"?C(91,151,139):kind=="Paving"?C(106,114,123):C(94,116,93);box(0,0,128,128,baseColor);for(int y=0;y<128;y+=kind=="Wood"?16:32){box(0,y,128,1,kind=="Wood"?C(106,76,57):C(70,97,97));for(int x=(y/16%2)*32;x<128;x+=64){box(x,y,1,kind=="Wood"?16:32,C(103,94,78));if(kind=="Wood")box(x+4,y+8,20,1,C(177,132,88));}}},new Vector2(.5f,.5f));}
 public static Sprite Prop(string kind){return Make(kind,96,128,box=>{var dark=C(44,48,57);var wood=C(145,99,65);var top=C(196,157,105);var white=C(222,223,208);var jade=C(94,170,153);box(7,4,82,12,C(43,47,52,100));switch(kind){
  case "Bed":box(10,10,76,90,dark);box(14,14,68,82,wood);box(14,28,68,72,white);box(19,76,58,18,C(235,230,206));box(14,28,68,46,C(112,142,148));box(14,28,68,5,C(83,109,123));box(8,15,6,93,wood);break;
  case "Fridge":box(20,10,58,110,dark);box(23,14,52,102,white);box(24,80,50,3,C(111,132,131));box(62,50,5,24,dark);box(62,90,5,16,dark);box(26,110,42,4,C(241,238,217));break;
  case "Table":case "Desk":box(12,9,8,40,wood);box(76,9,8,40,wood);box(6,42,84,30,dark);box(9,45,78,24,top);if(kind=="Desk"){box(30,62,42,42,dark);box(33,67,36,32,C(85,133,154));box(46,51,10,10,dark);box(24,48,50,5,C(82,88,93));}else{box(38,51,27,14,white);box(44,56,15,6,C(218,181,115));}break;
  case "Sink":box(26,10,46,40,white);box(14,44,70,28,dark);box(18,48,62,22,white);box(29,53,40,12,C(119,171,168));box(46,68,6,22,dark);box(47,84,18,6,white);box(23,93,50,30,dark);box(26,96,44,24,C(147,187,193));break;
  case "Toilet":box(27,14,44,33,white);box(18,39,62,30,dark);box(22,43,54,22,white);box(31,47,36,11,C(115,163,157));box(25,70,49,33,dark);box(28,73,43,26,white);box(61,80,7,4,C(125,151,151));break;
  case "Shower":box(10,10,76,105,C(123,191,175,100));box(13,12,4,100,white);box(79,12,4,100,white);box(48,40,4,65,dark);box(44,100,25,7,white);box(58,78,3,15,C(172,226,231));box(68,70,3,23,C(172,226,231));box(26,17,47,4,jade);break;
  case "Door":box(17,8,63,113,dark);box(22,8,53,108,wood);box(27,17,42,89,C(169,127,83));box(63,52,6,6,C(221,195,119));box(20,8,57,6,C(85,64,50));break;
  case "Wardrobe":box(10,9,78,107,dark);box(14,14,70,98,wood);box(19,20,27,86,top);box(50,20,28,86,top);box(38,61,4,10,dark);box(54,61,4,10,dark);break;
  case "Books":box(20,12,58,31,wood);box(23,45,12,50,C(130,159,163));box(37,45,12,60,C(190,133,96));box(51,45,12,55,C(191,174,118));box(65,45,10,40,C(111,141,133));break;
  case "Bench":box(12,12,8,30,wood);box(76,12,8,30,wood);box(7,40,83,12,top);box(11,55,7,44,dark);box(78,55,7,44,dark);box(15,65,64,8,wood);box(15,80,64,9,wood);break;
  case "Building":box(3,10,90,98,dark);box(7,13,82,90,C(164,146,123));box(3,106,90,13,C(88,101,120));box(12,20,25,44,C(76,85,96));box(18,27,13,30,C(187,154,103));box(47,47,32,35,C(73,106,130));box(61,47,3,35,white);box(47,62,32,3,white);box(9,91,78,8,C(203,176,128));break;
  case "Tree":box(41,8,15,48,wood);box(16,52,65,48,C(50,87,70));box(8,70,80,28,C(74,112,80));box(23,95,51,23,C(91,130,86));break;
  case "Lamp":box(42,12,10,90,dark);box(33,10,30,7,dark);box(25,94,45,25,dark);box(29,99,37,15,C(250,218,140));break;
  default:box(20,15,60,50,jade);break;
 }},new Vector2(.5f,0));}
 static Color32 C(byte r,byte g,byte b,byte a=255)=>new Color32(r,g,b,a);
 static Sprite Make(string name,int w,int h,Action<Action<int,int,int,int,Color32>> draw,Vector2 pivot){Sprite cached;if(Cache.TryGetValue(name,out cached)&&cached!=null)return cached;Directory.CreateDirectory(Folder);string path=Folder+name+".png";if(!File.Exists(path)){var t=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=new Color32[w*h];draw((x,y,width,height,color)=>{for(int yy=Math.Max(0,y);yy<Math.Min(h,y+height);yy++)for(int xx=Math.Max(0,x);xx<Math.Min(w,x+width);xx++)pixels[yy*w+xx]=color;});t.SetPixels32(pixels);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=48;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot;settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();var result=AssetDatabase.LoadAssetAtPath<Sprite>(path);Cache[name]=result;return result;}
}
}
