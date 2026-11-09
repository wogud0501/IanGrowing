#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace IanGame.Editor {
// Tests the scene's real body/colliders without claiming keyboard verification.
public static class HomeTraversalVerification {
 static bool Free(Vector2 p,Rigidbody2D body)=>!Physics2D.OverlapCircleAll(p,.32f).Any(c=>!c.isTrigger&&c.attachedRigidbody!=body);
 static bool Segment(Vector2 a,Vector2 b,Rigidbody2D body){var d=b-a;return !Physics2D.CircleCastAll(a,.31f,d.normalized,d.magnitude).Any(h=>h.collider!=null&&!h.collider.isTrigger&&h.rigidbody!=body);}
 static List<Vector2> Path(Vector2 start,Vector2 goal,Rect floor,Rigidbody2D body){
  const float step=.25f;Vector2 origin=floor.min+Vector2.one*.35f;int width=Mathf.FloorToInt((floor.width-.7f)/step)+1,height=Mathf.FloorToInt((floor.height-.7f)/step)+1;
  Func<Vector2Int,Vector2> Point=c=>origin+(Vector2)c*step;
  var nearest=new Vector2Int(Mathf.RoundToInt((start.x-origin.x)/step),Mathf.RoundToInt((start.y-origin.y)/step));
  // A rounded cell may be inside a nearby cabinet even when the real start is
  // clear. Connect the start to a clear cell instead of seeding inside a solid.
  var candidates=new List<Vector2Int>();for(int y=-3;y<=3;y++)for(int x=-3;x<=3;x++)candidates.Add(nearest+new Vector2Int(x,y));
  var starts=candidates.Where(c=>c.x>=0&&c.y>=0&&c.x<width&&c.y<height&&Free(Point(c),body)&&Segment(start,Point(c),body)).OrderBy(c=>Vector2.SqrMagnitude(Point(c)-start)).ToArray();
  if(starts.Length==0)throw new Exception("No clear connection from the player's actual position");var first=starts[0];
  var queue=new Queue<Vector2Int>();var parents=new Dictionary<Vector2Int,Vector2Int>();queue.Enqueue(first);parents[first]=first;var directions=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};Vector2Int? found=null;
  while(queue.Count>0){var current=queue.Dequeue();if(Vector2.Distance(Point(current),goal)<.22f&&Segment(Point(current),goal,body)){found=current;break;}foreach(var dir in directions){var next=current+dir;if(next.x<0||next.y<0||next.x>=width||next.y>=height||parents.ContainsKey(next)||!Free(Point(next),body)||!Segment(Point(current),Point(next),body))continue;parents[next]=current;queue.Enqueue(next);}}
  if(!found.HasValue)throw new Exception("No physical home route to "+goal);var path=new List<Vector2>{goal};var node=found.Value;while(node!=first){path.Add(Point(node));node=parents[node];}path.Add(Point(first));path.Reverse();return path;
 }
 public static IEnumerator Run(LifeWorldController world,Action<bool,string> check){
  var motor=world.Player;var body=motor.GetComponent<Rigidbody2D>();bool enabled=motor.enabled;var plan=world.Service.Game.economy.housingCatalog.placementPlan;
  var solids=world.objects.Where(o=>o!=null&&o.zone=="home"&&o.gameObject.activeInHierarchy).Select(o=>o.GetComponent<BoxCollider2D>()).Where(c=>c!=null&&c.enabled&&!c.isTrigger).ToArray();
  for(int i=0;i<solids.Length;i++)for(int j=i+1;j<solids.Length;j++){var a=solids[i].bounds;var b=solids[j].bounds;check(!(Mathf.Min(a.max.x,b.max.x)-Mathf.Max(a.min.x,b.min.x)>.01f&&Mathf.Min(a.max.y,b.max.y)-Mathf.Max(a.min.y,b.min.y)>.01f),"No static footprint overlap: "+solids[i].name+" / "+solids[j].name);}
  try{
   motor.enabled=false;motor.Warp(plan.entrance);yield return new WaitForFixedUpdate();
   foreach(var id in new[]{"closet","bed","starter-desk","bath-door","kitchen-prep","tap-home","fridge","meal-table","home-chair","front-door"}){
    var target=world.objects.First(o=>o.id==id);var route=Path(body.position,target.Point,plan.floor,body);float distance=0;foreach(var point in route){float timeout=Time.realtimeSinceStartup+3;while(Vector2.Distance(body.position,point)>.025f){if(Time.realtimeSinceStartup>timeout)throw new Exception("Physical traversal stalled at "+id);var before=body.position;body.MovePosition(Vector2.MoveTowards(before,point,3.8f*Time.fixedDeltaTime));yield return new WaitForFixedUpdate();distance+=Vector2.Distance(before,body.position);}}
    check(world.IsInReach(target)&&Free(body.position,body),"Actual player Rigidbody traversed clear home route to "+id+" ("+distance.ToString("F1")+" units)");
   }
   // Push against the actual front wall. No teleport is used during traversal.
   for(int i=0;i<25;i++){body.MovePosition(body.position+Vector2.down*.1f);yield return new WaitForFixedUpdate();}
   check(body.position.y>plan.floor.yMin-.05f,"Actual front boundary collision stops the player");
  }finally{body.linearVelocity=Vector2.zero;motor.enabled=enabled;}
 }
}
}
#endif
