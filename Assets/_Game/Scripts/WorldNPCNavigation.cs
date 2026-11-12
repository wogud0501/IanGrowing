using System.Collections.Generic;
using UnityEngine;
namespace IanGame {
// Static occupancy is cached per zone. Dynamic people/traffic are handled by the
// scheduler's local avoidance, so distant actors do not run physics/path queries.
public sealed class WorldNPCNavigation {
 sealed class Grid {public Vector2 origin;public int width,height;public bool[] blocked;public Vector2 Point(int i)=>origin+new Vector2(i%width,i/width)*.5f;}
 readonly Dictionary<string,Grid> cache=new Dictionary<string,Grid>();int furnishings=-1;
 public int Bakes{get;private set;}
 public void Clear(){cache.Clear();}
 public static bool StaticFree(Vector2 p){foreach(var c in Physics2D.OverlapCircleAll(p,.22f)){if(c.isTrigger||c.attachedRigidbody!=null)continue;var door=c.GetComponentInParent<WorldDoor>();if(door!=null&&c==door.closedCollider)continue;return false;}return true;}
 Grid Get(LifeWorldController world,WorldZoneDefinition zone){
  int count=world.furnishingRoot.childCount;if(count!=furnishings){furnishings=count;cache.Clear();}
  if(cache.TryGetValue(zone.id,out var found))return found;
  float top=Mathf.Min(zone.size.y/2-.4f,zone.walkableTop-.4f),bottom=-zone.size.y/2+.4f;
  var grid=new Grid{origin=zone.origin+new Vector2(-zone.size.x/2+.4f,bottom),width=Mathf.FloorToInt((zone.size.x-.8f)/.5f)+1,height=Mathf.FloorToInt((top-bottom)/.5f)+1};grid.blocked=new bool[grid.width*grid.height];
  for(int i=0;i<grid.blocked.Length;i++){var p=grid.Point(i);var local=p-zone.origin;grid.blocked[i]=!StaticFree(p)||(zone.id=="city"&&local.y> -6.7f&&local.y< -2.1f&&Mathf.Abs(local.x)>.65f);}
  cache[zone.id]=grid;Bakes++;return grid;
 }
 static int Nearest(Grid g,Vector2 p){int cx=Mathf.Clamp(Mathf.RoundToInt((p.x-g.origin.x)/.5f),0,g.width-1),cy=Mathf.Clamp(Mathf.RoundToInt((p.y-g.origin.y)/.5f),0,g.height-1);int best=-1;float distance=float.PositiveInfinity;for(int y=Mathf.Max(0,cy-3);y<=Mathf.Min(g.height-1,cy+3);y++)for(int x=Mathf.Max(0,cx-3);x<=Mathf.Min(g.width-1,cx+3);x++){int i=y*g.width+x;float d=(g.Point(i)-p).sqrMagnitude;if(!g.blocked[i]&&d<distance){distance=d;best=i;}}return best;}
 public List<Vector2> Find(LifeWorldController world,string zoneId,Vector2 start,Vector2 goal){
  var zone=world.Service.rules.Zone(zoneId);var g=Get(world,zone);int first=Nearest(g,start),last=Nearest(g,goal);if(first<0||last<0)return new List<Vector2>();
  var parent=new int[g.blocked.Length];System.Array.Fill(parent,-1);var queue=new Queue<int>();queue.Enqueue(first);parent[first]=first;int[] dx={0,1,0,-1,1,1,-1,-1},dy={1,0,-1,0,1,-1,1,-1};
  while(queue.Count>0&&parent[last]<0){int current=queue.Dequeue(),x=current%g.width,y=current/g.width;for(int s=0;s<8;s++){int nx=x+dx[s],ny=y+dy[s];if(nx<0||ny<0||nx>=g.width||ny>=g.height)continue;int next=ny*g.width+nx;if(parent[next]>=0||g.blocked[next])continue;if(s>=4&&(g.blocked[y*g.width+nx]||g.blocked[ny*g.width+x]))continue;parent[next]=current;queue.Enqueue(next);}}
  var result=new List<Vector2>();if(parent[last]<0)return result;for(int i=last;i!=first;i=parent[i])result.Add(g.Point(i));result.Reverse();
  // Grid rounding can put the final node beyond a door's interaction point,
  // inside its closed collider. Connect to the exact goal from the last safe
  // waypoint instead. Unlike the cached grid, this sweep includes closed doors.
  while(result.Count>0){var previous=result.Count>1?result[result.Count-2]:start;if(!ClearApproach(previous,goal))break;result.RemoveAt(result.Count-1);}
  result.Add(goal);return result;
 }
 static bool ClearApproach(Vector2 from,Vector2 to){var delta=to-from;foreach(var hit in Physics2D.CircleCastAll(from,.22f,delta.normalized,delta.magnitude)){var c=hit.collider;if(c==null||c.isTrigger||c.attachedRigidbody!=null)continue;return false;}return true;}
}
}
