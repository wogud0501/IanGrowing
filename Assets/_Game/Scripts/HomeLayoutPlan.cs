using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace IanGame {
[Flags]public enum HomeArea {Bedroom=1,Living=2,Study=4,Kitchen=8,Dining=16}
[Serializable]public class HomeAreaRegion {public string title;public HomeArea area;public Rect bounds;}
[Serializable]public class HomeFixedFootprint {public string id;public Rect bounds;public bool improvedOnly,optionalFurnishing;}
[Serializable]public class HomeFurnitureRule {
 public string id;public HomeArea areas;public Vector2 footprint=new Vector2(1,.5f),approach=new Vector2(0,-.75f);public float height=1.5f;public Vector2[] slots;
 public Rect At(Vector2 p)=>new Rect(p.x-footprint.x/2,p.y,footprint.x,footprint.y);
}
[CreateAssetMenu(menuName="Ian/Housing/Placement plan")]
public sealed class HomeLayoutPlan:ScriptableObject {
 public int version=2;public float grid=.25f,playerRadius=.3f;public Rect floor;
 public Vector2 entrance;public Vector2[] accessPoints;public Vector2[] starterAccessPoints;public HomeAreaRegion[] regions;
 public Material furnitureMaterial;
 public HomeFixedFootprint[] fixedFootprints;public Rect[] reserved;public HomeFurnitureRule[] furniture;
 public HomeFurnitureRule Rule(string id)=>furniture?.FirstOrDefault(r=>r.id==id);
 public bool ValidateAccess(HousingSnapshot home,out string reason){bool ok=Connected(Obstacles(home,null),new List<Vector2>(home.starterRevision>0&&starterAccessPoints!=null&&starterAccessPoints.Length>0?starterAccessPoints:accessPoints));reason=ok?null:"필수 가구와 출입문의 실제 접근 동선이 막힙니다.";return ok;}
 public Vector2 Snap(Vector2 world)=>new Vector2(Mathf.Round(world.x/grid)*grid,Mathf.Round(world.y/grid)*grid);
 static Rect Expand(Rect r,float amount)=>new Rect(r.xMin-amount,r.yMin-amount,r.width+2*amount,r.height+2*amount);
 static bool Contains(Rect outer,Rect inner)=>inner.xMin>=outer.xMin&&inner.yMin>=outer.yMin&&inner.xMax<=outer.xMax&&inner.yMax<=outer.yMax;
 List<Rect> Obstacles(HousingSnapshot home,string except){var list=fixedFootprints.Where(f=>(!f.improvedOnly||home.residenceId=="improved")&&(!f.optionalFurnishing||home.starterRevision==0)).Select(f=>f.bounds).ToList();foreach(var p in home.placed){if(p.id==except)continue;var rule=Rule(p.id);if(rule!=null)list.Add(rule.At(p.position*2));}return list;}
 public bool Validate(string id,Vector2 world,HousingSnapshot home,out string reason){
  reason=null;var rule=Rule(id);if(rule==null){reason="이 가구의 배치 구역이 없습니다.";return false;}
  var footprint=rule.At(world);if(!Contains(floor,footprint)){reason="벽과 바닥 경계 안에 배치하세요.";return false;}
  if(!regions.Any(r=>(r.area&rule.areas)!=0&&Contains(r.bounds,footprint))){reason="이 가구에 맞는 공간이 아닙니다.";return false;}
  if(reserved.Any(r=>r.Overlaps(footprint))){reason="문 앞과 주요 통로를 비워 두세요.";return false;}
  var obstacles=Obstacles(home,id);if(obstacles.Any(r=>Expand(r,.04f).Overlaps(footprint))){reason="다른 가구와 겹치는 위치입니다.";return false;}
  Vector2 approach=world+rule.approach;if(!floor.Contains(approach)||obstacles.Any(r=>Expand(r,playerRadius).Contains(approach))){reason="가구 앞에 접근할 공간이 필요합니다.";return false;}
  obstacles.Add(footprint);var targets=new List<Vector2>(home.starterRevision>0&&starterAccessPoints!=null&&starterAccessPoints.Length>0?starterAccessPoints:accessPoints);targets.Add(approach);if(!Connected(obstacles,targets)){reason="침대·주방·출입문의 이동 동선이 막힙니다.";return false;}return true;
 }
 bool Connected(List<Rect> obstacles,List<Vector2> targets){
  // The actual use point must be clear; a reachable neighbour cannot stand in
  // for a bed/door approach occupied by furniture.
  if(targets.Any(p=>!floor.Contains(p)||obstacles.Any(r=>Expand(r,playerRadius).Contains(p))))return false;
  var area=Expand(floor,-playerRadius);int width=Mathf.CeilToInt(area.width/grid)+1,height=Mathf.CeilToInt(area.height/grid)+1;var blocked=new bool[width,height];var visited=new bool[width,height];
  for(int y=0;y<height;y++)for(int x=0;x<width;x++){var point=new Vector2(area.xMin+x*grid,area.yMin+y*grid);blocked[x,y]=!area.Contains(point)||obstacles.Any(r=>Expand(r,playerRadius).Contains(point));}
  int sx=Mathf.Clamp(Mathf.RoundToInt((entrance.x-area.xMin)/grid),0,width-1),sy=Mathf.Clamp(Mathf.RoundToInt((entrance.y-area.yMin)/grid),0,height-1);if(blocked[sx,sy])return false;
  var queue=new Queue<Vector2Int>();queue.Enqueue(new Vector2Int(sx,sy));visited[sx,sy]=true;var steps=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
  while(queue.Count>0){var point=queue.Dequeue();foreach(var step in steps){var next=point+step;if(next.x<0||next.y<0||next.x>=width||next.y>=height||blocked[next.x,next.y]||visited[next.x,next.y])continue;visited[next.x,next.y]=true;queue.Enqueue(next);}}
  foreach(var target in targets){bool reachable=false;int tx=Mathf.RoundToInt((target.x-area.xMin)/grid),ty=Mathf.RoundToInt((target.y-area.yMin)/grid);for(int y=Mathf.Max(0,ty-2);y<=Mathf.Min(height-1,ty+2);y++)for(int x=Mathf.Max(0,tx-2);x<=Mathf.Min(width-1,tx+2);x++)if(visited[x,y]&&Vector2.Distance(target,new Vector2(area.xMin+x*grid,area.yMin+y*grid))<.65f)reachable=true;if(!reachable)return false;}return true;
 }
 public bool Find(string id,HousingSnapshot home,Vector2? skip,out Vector2 model,out string reason){model=Vector2.zero;reason="배치할 수 있는 위치가 없습니다.";var rule=Rule(id);if(rule?.slots==null){reason="배치 위치가 설정되지 않았습니다.";return false;}foreach(var slot in rule.slots){var p=Snap(slot);if(skip.HasValue&&Vector2.Distance(p,skip.Value)<.02f)continue;if(Validate(id,p,home,out reason)){model=p/2;return true;}}reason="문과 가구의 동선을 확보할 수 있는 다른 위치가 없습니다.";return false;}
 public void Migrate(HousingSnapshot home){
  if(home.layoutVersion>=version&&home.layoutResidenceId==home.residenceId)return;var previous=home.placed.ToArray();var draft=JsonUtility.FromJson<HousingSnapshot>(JsonUtility.ToJson(home));draft.placed.Clear();int adjusted=0;
  foreach(var item in previous){if(Rule(item.id)==null){draft.placed.Add(item);continue;}if(Validate(item.id,item.position*2,draft,out var why)){draft.placed.Add(item);continue;}if(Find(item.id,draft,null,out var position,out why))draft.placed.Add(new FurniturePlacement{id=item.id,position=position,homeId=item.homeId,roomId=item.roomId,ownership=item.ownership,installed=item.installed,direction=item.direction});adjusted++;}
  home.placed=draft.placed;home.layoutVersion=version;home.layoutResidenceId=home.residenceId;home.layoutAdjusted+=adjusted;
 }
}
}
