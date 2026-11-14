using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace IanGame.Editor {
// Authoring pass only: saves independent objects into existing room scenes.
// No runtime generator, no global game state or building-door controller owner.
public static class BathroomArtReconstruction {
 const string Data="Assets/_Game/Data/InteriorReconstruction/";
 const string Atlas="Assets/_Game/Art/InteriorReconstruction/FamilyBathroomModules.png";
 static Material Ink=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/VisualRebuild/WorldAlphaCut.mat");
 static Sprite Slice(string name,Texture2D tex,Rect pixels,float worldHeight){
  string path=Data+name+".asset";var s=AssetDatabase.LoadAssetAtPath<Sprite>(path);
  var r=new Rect(Mathf.Round(pixels.x/1024*tex.width),Mathf.Round(pixels.y/1536*tex.height),Mathf.Round(pixels.width/1024*tex.width),Mathf.Round(pixels.height/1536*tex.height));
  if(s!=null&&s.rect==r&&Mathf.Abs(s.pixelsPerUnit-r.height/worldHeight)<.01f)return s;var fresh=Sprite.Create(tex,r,new Vector2(.5f,.5f),r.height/worldHeight,0,SpriteMeshType.FullRect);fresh.name=name;if(s==null){AssetDatabase.CreateAsset(fresh,path);return fresh;}EditorUtility.CopySerialized(fresh,s);UnityEngine.Object.DestroyImmediate(fresh);EditorUtility.SetDirty(s);return s;
 }
 static Sprite White(){var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Data+"ConstructionFace.asset");if(sprite!=null)return sprite;var tex=new Texture2D(2,2,TextureFormat.RGBA32,false){name="Construction face",filterMode=FilterMode.Point};tex.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});tex.Apply();AssetDatabase.CreateAsset(tex,Data+"ConstructionFaceTexture.asset");sprite=Sprite.Create(tex,new Rect(0,0,2,2),new Vector2(.5f,.5f),2);AssetDatabase.CreateAsset(sprite,Data+"ConstructionFace.asset");return sprite;}
 static Transform Child(Transform parent,string name){var t=parent.Cast<Transform>().FirstOrDefault(c=>c.name==name);if(t!=null)return t;t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
 static SpriteRenderer Image(Transform parent,string name,Sprite sprite,Vector2 center,Vector2 size,int order,Color color,bool tiled=false){var t=Child(parent,name);var r=t.GetComponent<SpriteRenderer>();if(r==null)r=t.gameObject.AddComponent<SpriteRenderer>();t.localScale=Vector3.one;t.rotation=Quaternion.identity;r.enabled=true;r.sprite=sprite;r.sharedMaterial=Ink;r.drawMode=tiled?SpriteDrawMode.Tiled:SpriteDrawMode.Sliced;r.size=size;t.position=center-new Vector2((.5f-sprite.pivot.x/sprite.rect.width)*size.x,(.5f-sprite.pivot.y/sprite.rect.height)*size.y);r.sortingOrder=order;r.color=color;return r;}
 static void Prop(Transform root,string name,Sprite sprite,Vector2 feet,float height,int order){float scale=height/sprite.bounds.size.y;Image(root,name,sprite,feet+Vector2.up*height/2,new Vector2(sprite.bounds.size.x*scale,height),order,Color.white);}
 static void Rect(Transform root,string name,Sprite s,Vector2 center,Vector2 size,int order,Color c)=>Image(root,name,s,center,size,order,c);
 static WorldDoorStyle DoorStyle(WorldDoor d){string path=Data+"FamilyBathroomDoor.asset";var style=AssetDatabase.LoadAssetAtPath<WorldDoorStyle>(path);if(style!=null){var prior=style;var fresh=BuildFittedDoor(d);EditorUtility.CopySerialized(fresh,prior);UnityEngine.Object.DestroyImmediate(fresh);EditorUtility.SetDirty(prior);return prior;}style=BuildFittedDoor(d);AssetDatabase.CreateAsset(style,path);return style;
 }
 static WorldDoorStyle BuildFittedDoor(WorldDoor d){var style=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WorldDoorStyle>("Assets/_Game/Data/Phase26/Advanced/Door0.asset"));style.name="Family bathroom fitted oak";style.hingeOffset*=.65f;
  Sprite Copy(Sprite source,string label){var r=source.rect;var s=Sprite.Create(source.texture,r,source.pivot/r.size,source.pixelsPerUnit/.65f,0,SpriteMeshType.FullRect);s.name=label;var existing=AssetDatabase.LoadAssetAtPath<Sprite>(Data+label+".asset");if(existing==null){AssetDatabase.CreateAsset(s,Data+label+".asset");return s;}EditorUtility.CopySerialized(s,existing);UnityEngine.Object.DestroyImmediate(s);EditorUtility.SetDirty(existing);return existing;}
  style.openingFrames=style.openingFrames.Select((s,i)=>Copy(s,"BathDoorFrame"+i)).ToArray();style.frame=Copy(style.frame,"BathDoorFixedFrame");return style;
 }
 [MenuItem("Ian/Room Visual/Reconstruct modular bathroom art")]
 public static void Apply(){if(Application.isPlaying)throw new Exception("Stop Play before authoring");if(!File.Exists(Atlas))return;Directory.CreateDirectory(Data);
  var importer=(TextureImporter)AssetImporter.GetAtPath(Atlas);if(importer==null){AssetDatabase.ImportAsset(Atlas);importer=(TextureImporter)AssetImporter.GetAtPath(Atlas);}if(importer.textureType!=TextureImporterType.Sprite||importer.maxTextureSize!=1024){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=1024;importer.SaveAndReimport();}
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);var floor=Slice("SageChecker",tex,new Rect(14,1034,488,488),.8f);var sage=Slice("SageWall",tex,new Rect(524,1034,488,488),1.4f);var ivory=Slice("IvoryWall",tex,new Rect(14,522,488,488),1.4f);var cabinet=Slice("LinenCabinet",tex,new Rect(640,532,250,475),1.9f);var towel=Slice("TowelRail",tex,new Rect(75,103,365,280),.6f);var shelf=Slice("ToiletryShelf",tex,new Rect(560,55,425,360),.72f);var solid=White();
  var ground=AssetDatabase.LoadAssetAtPath<Material>(Data+"ArchitecturalGround.mat");if(ground==null){ground=new Material(Shader.Find("Ian/ArchitecturalGround"));AssetDatabase.CreateAsset(ground,Data+"ArchitecturalGround.mat");}
  var catalog=AssetDatabase.LoadAssetAtPath<RoomSceneCatalog>("Assets/_Game/Data/SceneArchitecture/RoomScenes.asset");
  foreach(var room in catalog.rooms.Where(r=>!catalog.rules.Zone(r.roomId).outdoor)){
   var scene=EditorSceneManager.OpenScene("Assets/_Game/Scenes/Rooms/"+room.sceneName+".unity");var w=UnityEngine.Object.FindAnyObjectByType<LifeWorldController>();var z=catalog.rules.Zone(room.roomId);var o=z.origin;
   var surround=w.GetComponent<InteriorSurround>();surround.plaster.sharedMaterial=ground;surround.plaster.color=Color.white;surround.plaster.drawMode=SpriteDrawMode.Sliced;
   var treatment=Child(w.transform,"Architectural contact and cutaway");float rear=z.size.y/2+1.05f,front=-z.size.y/2;
   for(int i=0;i<4;i++){float spread=.09f*i;Rect(treatment,"Contact shadow "+i,solid,o+new Vector2(.09f,(rear+front)/2-.08f),new Vector2(z.size.x+.24f+spread,rear-front+.24f+spread),-31000+i,new Color(.1f,.08f,.055f,.055f));}
   if(room.roomId=="bathroom")w.Room.interior.halfHeight=4.5f;
   if(room.roomId=="family-bathroom"){
    var root=w.transform.Cast<Transform>().First(t=>t.name.EndsWith(" / family-bathroom"));
    Image(root,"Room owned floor",floor,o,z.size,-5000,Color.white,true);
    foreach(var r in root.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.EndsWith(" visible")||r.name.Contains(" cutaway")||r.name=="Room owned rear wallpaper"||r.name=="Continuous wall skirting"||r.name=="Single wall cap"))r.enabled=false;
    foreach(var c in root.GetComponentsInChildren<BoxCollider2D>())if(c.name.StartsWith("North wall"))c.transform.position=new Vector2(c.transform.position.x,o.y+z.size.y/2);
    float north=z.size.y/2,south=-z.size.y/2,top=north+1.05f,half=z.size.x/2;
    Image(treatment,"Sage ceramic wainscot",sage,o+new Vector2(0,north+.36f),new Vector2(z.size.x,.72f),-4000,Color.white,true);
    Image(treatment,"Ivory glazed upper wall",ivory,o+new Vector2(0,north+.89f),new Vector2(z.size.x,.34f),-4000,Color.white,true);
    var dark=new Color(.22f,.19f,.16f);var cream=new Color(.84f,.79f,.67f);var wood=new Color(.47f,.31f,.17f);
    Rect(treatment,"Upper construction depth",solid,o+new Vector2(0,top+.09f),new Vector2(z.size.x+.36f,.18f),-3400,dark);
    Rect(treatment,"Upper inner rim",solid,o+new Vector2(0,top+.01f),new Vector2(z.size.x+.08f,.055f),-3300,cream);
    Rect(treatment,"Wall tile seam",solid,o+new Vector2(0,north+.72f),new Vector2(z.size.x,.025f),-3300,cream);
    Rect(treatment,"Floor wall contact",solid,o+new Vector2(0,north),new Vector2(z.size.x,.055f),-3300,new Color(.24f,.3f,.24f));
    foreach(float side in new[]{-1f,1f}){
     Rect(treatment,"Fixed wall outer edge "+side,solid,o+new Vector2(side*(half+.15f),(top+south)/2),new Vector2(.16f,top-south+.22f),-3250,dark);
     Rect(treatment,"Fixed wall plaster depth "+side,solid,o+new Vector2(side*(half+.035f),(top+south)/2),new Vector2(.16f,top-south+.08f),-3200,cream);
     Rect(treatment,"Interior tile return "+side,solid,o+new Vector2(side*(half-.055f),(top+south)/2),new Vector2(.03f,top-south),-3100,new Color(.43f,.49f,.39f));
     float length=half-.43f;Rect(treatment,"Front wall depth "+side,solid,o+new Vector2(side*(half+.43f)/2,south-.09f),new Vector2(length,.25f),1350,cream);
     Rect(treatment,"Front wall wooden cap "+side,solid,o+new Vector2(side*(half+.43f)/2,south+.05f),new Vector2(length,.07f),1351,wood);
     Rect(treatment,"Front outer rim "+side,solid,o+new Vector2(side*(half+.43f)/2,south-.24f),new Vector2(length,.07f),1351,dark);
    }
    foreach(float side in new[]{-1f,1f}){var jamb=Child(treatment,"Fixed door jamb collider "+side);jamb.position=o+new Vector2(side*.615f,south);var collision=jamb.GetComponent<BoxCollider2D>();if(collision==null)collision=jamb.gameObject.AddComponent<BoxCollider2D>();collision.size=new Vector2(.37f,.18f);}
    Prop(treatment,"Family linen cupboard",cabinet,o+new Vector2(-2.15f,.03f),1.9f,1010);
    var cupboardCollider=Child(treatment,"Family linen cupboard").GetComponent<BoxCollider2D>();if(cupboardCollider==null)cupboardCollider=Child(treatment,"Family linen cupboard").gameObject.AddComponent<BoxCollider2D>();cupboardCollider.size=new Vector2(.55f,.25f);cupboardCollider.offset=new Vector2(0,-.82f);
    Prop(treatment,"Mounted toiletries",shelf,o+new Vector2(-.1f,1.65f),.65f,-3500);Prop(treatment,"Mounted towel rail",towel,o+new Vector2(.65f,1.28f),.5f,-3500);
    var laundry=root.Find("Family laundry and towels");if(laundry!=null)laundry.position=o+new Vector2(-2,-1.05f);var mat=root.Find("Non-slip bathroom mat");if(mat!=null)mat.position=o+new Vector2(1.5f,-1.05f);
    var shower=w.objects.Single(x=>x.kind==WorldInteractionKind.Shower);shower.GetComponent<ShowerBoothVisual>().ceramic=ivory;
    foreach(var fixture in w.objects.Where(x=>x.kind!=WorldInteractionKind.Door&&x.visual!=null)){
     var width=fixture.visual.bounds.size.x;Rect(treatment,"Contact / "+fixture.id,solid,(Vector2)fixture.transform.position+Vector2.down*.025f,new Vector2(width*.72f,.08f),fixture.visual.sortingOrder-2,new Color(.13f,.17f,.12f,.22f));
    }
    var d=w.objects.Single(x=>x.kind==WorldInteractionKind.Door).GetComponent<WorldDoor>();d.style=DoorStyle(d);d.frame.sprite=d.style.frame;d.leaf.sprite=d.style.openingFrames[0];d.frame.transform.position=d.Plane+Vector2.down*.2f;d.leaf.transform.position=d.Plane+Vector2.down*.2f+d.style.hingeOffset;float doorHeight=d.style.openingFrames[0].rect.height/d.style.openingFrames[0].pixelsPerUnit*.79f;float doorWidth=d.style.openingFrames[0].rect.width/d.style.openingFrames[0].pixelsPerUnit*.48f;d.apertureShadow.transform.position=d.Plane+new Vector2(.04f,doorHeight/2-.2f);d.apertureShadow.transform.localScale=new Vector3(doorWidth/d.apertureShadow.sprite.bounds.size.x,doorHeight/d.apertureShadow.sprite.bounds.size.y,1);
    Image(w.transform,"Interior threshold / "+d.portal.id,floor,d.Plane+Vector2.down*.07f,new Vector2(1.25f,.55f),-4800,Color.white,true);
    w.Room.interior.bounds=new UnityEngine.Rect(-2.85f,-1.65f,5.7f,4.25f);w.Room.interior.halfHeight=2.5f;w.Room.interior.maxScale=12;w.follow.target.orthographicSize=w.Room.interior.QuantizedSize(720);
   }
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  AssetDatabase.SaveAssets();Directory.CreateDirectory("Docs/InteriorReconstruction");File.WriteAllText("Docs/InteriorReconstruction/ART_AUTHORING.txt","Modular six-sprite atlas, fourteen preserved interiors, explicit construction faces, bathroom instance-only door style. "+DateTime.UtcNow.ToString("O"));
 }
}
}
