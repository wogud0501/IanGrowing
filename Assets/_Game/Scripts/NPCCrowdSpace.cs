using System.Collections.Generic;
using UnityEngine;
namespace IanGame {
// Updated once per scheduler frame and incrementally after accepted movement.
// No pairwise all-agent scan or positional repulsion is performed in Move.
public sealed class NPCCrowdSpace {
 public const float Clearance=.48f;
 public sealed class Entry {public string id;public Vector2 position;public Vector2Int cell;}
 readonly Dictionary<Vector2Int,List<Entry>> cells=new Dictionary<Vector2Int,List<Entry>>();readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();
 Vector2Int Cell(Vector2 p)=>new Vector2Int(Mathf.FloorToInt(p.x/.8f),Mathf.FloorToInt(p.y/.8f));
 public void Clear(){cells.Clear();entries.Clear();}
 public void Set(string id,Vector2 p){var key=Cell(p);if(entries.TryGetValue(id,out var e)){if(e.cell!=key){cells[e.cell].Remove(e);if(!cells.TryGetValue(key,out var bucket))cells[key]=bucket=new List<Entry>();bucket.Add(e);}e.cell=key;e.position=p;return;}e=new Entry{id=id,position=p,cell=key};entries[id]=e;if(!cells.TryGetValue(key,out var list))cells[key]=list=new List<Entry>();list.Add(e);}
 public void Remove(string id){if(!entries.TryGetValue(id,out var e))return;cells[e.cell].Remove(e);entries.Remove(id);}
 public IEnumerable<Entry> Nearby(Vector2 p){var c=Cell(p);for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)if(cells.TryGetValue(c+new Vector2Int(x,y),out var list))foreach(var e in list)yield return e;}
 public bool Free(string id,Vector2 p,float radius=Clearance){foreach(var e in Nearby(p))if(e.id!=id&&(e.position-p).sqrMagnitude<radius*radius)return false;return true;}
 public bool Blocks(string id,Vector2 from,Vector2 next){foreach(var e in Nearby(next)){if(e.id==id)continue;float before=(e.position-from).sqrMagnitude,after=(e.position-next).sqrMagnitude;if(after<Clearance*Clearance&&after<before-.0000001f)return true;}return false;}
}
}
