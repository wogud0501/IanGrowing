using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace IanGame.Editor {
public static class AdvancedLifeSetup {
 public const string Art="Assets/_Game/Art/AdvancedLife/",Data="Assets/_Game/Data/Phase26/Advanced/";
 static Sprite[] Grid(string file,int columns,int rows,bool door=false){
  string path=Art+file;AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);string stamp="AdvancedLife/v2/"+File.GetLastWriteTimeUtc(path).Ticks;
  var cached=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();if(importer.userData==stamp&&cached.Length==columns*rows&&!importer.isReadable)return cached;
  importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=4096;importer.isReadable=true;
  importer.GetSourceTextureWidthAndHeight(out int width,out int height);importer.spritePixelsPerUnit=door?height/(float)rows/2.25f:height/(float)rows/256*120;
  importer.SaveAndReimport();var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();width=texture.width;height=texture.height;if(pixels[0].a>2||pixels.Count(p=>p.a==0)<pixels.Length/4)throw new Exception("Transparent source required: "+path);
  var parts=new SpriteMetaData[columns*rows];for(int row=0;row<rows;row++)for(int col=0;col<columns;col++){
   int x=Mathf.RoundToInt(col*width/(float)columns),x1=Mathf.RoundToInt((col+1)*width/(float)columns),y=Mathf.RoundToInt((rows-1-row)*height/(float)rows),y1=Mathf.RoundToInt((rows-row)*height/(float)rows);
   int foot=-1;for(int py=y;py<y1&&foot<0;py++)for(int px=x;px<x1;px++)if(pixels[py*width+px].a>240){foot=py;break;}
   if(foot<0)throw new Exception("Empty authored frame "+file+" "+row+"/"+col);
   parts[row*columns+col]=new SpriteMetaData{name=Path.GetFileNameWithoutExtension(file)+"_"+(row*columns+col).ToString("D3"),rect=new Rect(x,y,x1-x,y1-y),alignment=9,pivot=new Vector2(.5f,(foot+.5f-y)/(y1-y))};
  }
  importer.spritesheet=parts;importer.userData=stamp;importer.isReadable=false;importer.SaveAndReimport();return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
 }
 static ActorAnimationSet Asset(string name){var item=AssetDatabase.LoadAssetAtPath<ActorAnimationSet>(Data+name+".asset");if(item==null){item=ScriptableObject.CreateInstance<ActorAnimationSet>();AssetDatabase.CreateAsset(item,Data+name+".asset");}return item;}
 static ActorSpriteClip Clip(ActorMotion motion,ActorDirection direction,Sprite[] frames,bool loop=false,bool mirror=false,float stride=1.8f,float fps=8)=>new ActorSpriteClip{motion=motion,direction=direction,frames=frames,loop=loop,mirror=mirror,strideLength=stride,framesPerSecond=fps};
 [MenuItem("Ian/Life World/Advanced/Apply Animation Sources")]
 public static void ApplyAnimation(){
  if(Application.isPlaying)throw new Exception("Stop Play before applying animation assets");Directory.CreateDirectory(Data);AssetDatabase.Refresh();
  var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/LifeWorld.unity");var w=UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();
  var actions=Grid("IanActions.png",8,8);var run=Grid("IanRunTurnStop.png",4,8);var emotions=Grid("IanEmotions.png",4,8);var npc=Grid("CitizenDirections.png",8,8);Grid("Doors.png",6,3,true);
  var player=Asset("IanAdvanced");var clips=new List<ActorSpriteClip>();var walk=w.Player.walkSet;var dirs=new[]{ActorDirection.Down,ActorDirection.Up,ActorDirection.Left,ActorDirection.Right};
  var walks=new[]{walk.down,walk.up,walk.left,walk.right};for(int i=0;i<4;i++){clips.Add(Clip(ActorMotion.Walk,dirs[i],walks[i],true));clips.Add(Clip(ActorMotion.Idle,dirs[i],new[]{walks[i][0],walks[i][0]},true));clips.Add(Clip(ActorMotion.Run,dirs[i],run.Skip(i*4).Take(4).ToArray(),true,false,2.2f));}
  var actionNames=new[]{ActorMotion.Sleep,ActorMotion.WakeUp,ActorMotion.SitDown,ActorMotion.StandUp,ActorMotion.Eat,ActorMotion.Drink,ActorMotion.WashFace,ActorMotion.Shower,ActorMotion.Work,ActorMotion.UseComputer,ActorMotion.UsePhone,ActorMotion.Interact,ActorMotion.OpenDoor,ActorMotion.PickUpItem,ActorMotion.PutDownItem,ActorMotion.Thinking};
  for(int i=0;i<actionNames.Length;i++){
   int row=i/2,start=row*8+(i%2)*4;var frames=actions.Skip(start).Take(4).ToArray();if(actionNames[i]==ActorMotion.WakeUp)frames=frames.Concat(new[]{walk.down[0]}).ToArray();
   var direction=actionNames[i]==ActorMotion.OpenDoor?ActorDirection.Right:ActorDirection.Down;var clip=Clip(actionNames[i],direction,frames,i>=4&&i<=11);if(actionNames[i]==ActorMotion.Sleep){clip.loop=true;clip.loopFrom=3;}clips.Add(clip);
  }
  for(int i=0;i<7;i++)clips.Add(Clip((ActorMotion)((int)ActorMotion.Happy+i),ActorDirection.Down,emotions.Skip(i*4).Take(4).ToArray(),true,false,1.8f,5));
  clips.Add(Clip(ActorMotion.Turn,ActorDirection.Up,run.Skip(16).Take(4).ToArray()));clips.Add(Clip(ActorMotion.Turn,ActorDirection.Down,run.Skip(20).Take(4).ToArray()));
  clips.Add(Clip(ActorMotion.Stop,ActorDirection.Down,run.Skip(24).Take(4).ToArray()));clips.Add(Clip(ActorMotion.Stop,ActorDirection.Right,run.Skip(28).Take(4).ToArray()));clips.Add(Clip(ActorMotion.Stop,ActorDirection.Left,run.Skip(28).Take(4).ToArray(),false,true));
  clips.RemoveAll(c=>c.motion==ActorMotion.Idle&&c.direction==ActorDirection.Down);clips.Add(Clip(ActorMotion.Idle,ActorDirection.Down,emotions.Skip(28).Take(4).ToArray(),true,false,1.8f,2));
  player.clips=clips.ToArray();EditorUtility.SetDirty(player);
  var actor=w.Player.GetComponent<WorldActorAnimation>()??w.Player.gameObject.AddComponent<WorldActorAnimation>();actor.library=player;actor.visual=w.Player.sprite;actor.legacyAnimator=w.Player.locomotion;w.Player.advanced=actor;
  var citizens=new ActorAnimationSet[4];for(int person=0;person<4;person++){
   int top=person/2*4,left=person%2*4;var bank=Asset("Citizen"+person);var list=new List<ActorSpriteClip>();
   for(int direction=0;direction<4;direction++){
    // The source's two profile rows both face right. This is deliberate metadata,
    // rather than silently treating a wrong-facing row as a valid left walk.
    int source=direction==2?3:direction;var frames=Enumerable.Range(0,4).Select(c=>npc[(top+source)*8+left+c]).ToArray();
    list.Add(Clip(ActorMotion.Walk,dirs[direction],frames,true,direction==2,1.45f));list.Add(Clip(ActorMotion.Idle,dirs[direction],new[]{frames[0],frames[0]},true,direction==2));
   }
   bank.clips=list.ToArray();EditorUtility.SetDirty(bank);citizens[person]=bank;
  }
  var street=w.GetComponentInChildren<WorldStreetLife>(true);for(int i=0;i<street.pedestrians.Length;i++){var p=street.pedestrians[i];var a=p.visual.GetComponent<WorldActorAnimation>()??p.visual.gameObject.AddComponent<WorldActorAnimation>();a.visual=p.visual;a.library=citizens[i%4];p.animation=a;}
  var scheduler=w.GetComponent<WorldNPCScheduler>();for(int i=0;i<scheduler.actors.Length;i++){var o=scheduler.actors[i];var a=o.GetComponent<WorldActorAnimation>()??o.gameObject.AddComponent<WorldActorAnimation>();a.visual=o.visual;a.library=citizens[i==0?0:2];}
  var controls=w.Player.controls;var map=controls.FindActionMap("World",true);if(map.FindAction("Run")==null){map.AddAction("Run",InputActionType.Button,"<Keyboard>/leftShift").AddBinding("<Gamepad>/rightTrigger");}
  if(!map.FindAction("Inventory").bindings.Any(b=>b.path=="<Gamepad>/buttonWest"))map.FindAction("Inventory").AddBinding("<Gamepad>/buttonWest");if(!map.FindAction("Menu").bindings.Any(b=>b.path=="<Gamepad>/start"))map.FindAction("Menu").AddBinding("<Gamepad>/start");
  File.WriteAllText(AssetDatabase.GetAssetPath(controls),controls.ToJson());EditorUtility.SetDirty(w.Player);EditorUtility.SetDirty(street);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
}
}
