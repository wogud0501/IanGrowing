using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace IanGame.Editor {
public static class DoorSpriteGeometry {
 static Sprite Make(Sprite source,string name,Vector2 pivot,params Rect[] quads){
  var sprite=Sprite.Create(source.texture,source.rect,pivot,source.pixelsPerUnit,0,SpriteMeshType.FullRect);sprite.name=name;
  var vertices=new List<Vector2>();var triangles=new List<ushort>();
  foreach(var q in quads){ushort b=(ushort)vertices.Count;foreach(var p in new[]{new Vector2(q.xMin,q.yMin),new Vector2(q.xMax,q.yMin),new Vector2(q.xMax,q.yMax),new Vector2(q.xMin,q.yMax)})vertices.Add(Vector2.Scale(p-pivot,source.rect.size)/source.pixelsPerUnit);triangles.AddRange(new ushort[]{b,(ushort)(b+1),(ushort)(b+2),b,(ushort)(b+2),(ushort)(b+3)});}
  sprite.OverrideGeometry(vertices.ToArray(),triangles.ToArray());return sprite;
 }
 public static DoorSpriteGeometryData Get(int row){
  string path=AdvancedLifeSetup.Data+"DoorGeometry"+row+".asset";var existing=AssetDatabase.LoadAssetAtPath<DoorSpriteGeometryData>(path);if(existing!=null&&existing.leaves!=null&&existing.leaves.Length==6)return existing;
  var source=AssetDatabase.LoadAllAssetsAtPath(AdvancedLifeSetup.Art+"Doors.png").OfType<Sprite>().OrderBy(s=>s.name).Skip(row*6).Take(6).ToArray();var data=ScriptableObject.CreateInstance<DoorSpriteGeometryData>();AssetDatabase.CreateAsset(data,path);
  float left=row==2?.13f:row==1?.318f:.304f,right=row==2?.87f:row==1?.770f:.784f,bottom=row==2?.055f:.06f,top=.85f;
  data.frame=Make(source[0],"Fixed frame",new Vector2(.5f,.04f),new Rect(0,0,left,1),new Rect(right,0,1-right,1),new Rect(left,top,right-left,1-top),new Rect(left,0,right-left,bottom));AssetDatabase.AddObjectToAsset(data.frame,data);
  float[] oakLeft={.304f,.288f,.270f,.257f,.237f,.223f},oakRight={.784f,.715f,.611f,.497f,.380f,.301f};float[] jadeLeft={.318f,.386f,.429f,.493f,.576f,.611f},jadeRight={.770f,.756f,.736f,.723f,.708f,.693f};
  data.hinge=Vector2.Scale(new Vector2((row==0?left:row==1?right:.5f)-.5f,bottom-.04f),source[0].rect.size)/source[0].pixelsPerUnit;data.leaves=new Sprite[6];
  for(int i=0;i<6;i++){if(row==2){float opening=i*.065f;data.leaves[i]=Make(source[i],"Sliding panels "+i,new Vector2(.5f,.04f),new Rect(left,bottom,.5f-left-opening,top-bottom),new Rect(.5f+opening,bottom,right-.5f-opening,top-bottom));}
   else {float x=row==0?oakLeft[i]:jadeLeft[i],end=row==0?oakRight[i]:jadeRight[i];var pivot=new Vector2(row==0?x:end,bottom);data.leaves[i]=Make(source[i],"Hinged leaf "+i,pivot,new Rect(x,bottom,end-x,top-bottom));}AssetDatabase.AddObjectToAsset(data.leaves[i],data);}
  if(row==2)data.hinge=Vector2.zero;EditorUtility.SetDirty(data);return data;
 }
}
}
