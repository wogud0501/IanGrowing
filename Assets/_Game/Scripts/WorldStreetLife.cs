using System;
using UnityEngine;
namespace IanGame {
[Serializable]public sealed class StreetPedestrian {
 public SpriteRenderer visual,shadow;public Sprite[] frames;public Vector2[] route;
 public float speed=1.2f;public int next;public float waiting;public bool crossing;
 [NonSerialized]public float phase;[NonSerialized]public bool crossingActive;
}
[Serializable]public sealed class StreetVehicle {
 public Rigidbody2D body;public SpriteRenderer visual,headlights,shadow;
 public float speed=3,direction=1,laneY=-3.1f;[NonSerialized]public float velocity;
}
// One controller for a small authored street. Friends/social data remain in
// WorldNPCScheduler; background citizens are scenery and do not create fake relations.
public sealed class WorldStreetLife:MonoBehaviour {
 public LifeWorldController world;public StreetPedestrian[] pedestrians;public StreetVehicle[] vehicles;
 public SpriteRenderer vehicleSignal,pedestrianSignal;float cycle;int lastDensity=-1;bool visible;
 public SpriteRenderer signal;public Sprite[] signalStates;
 public bool PedestrianGreen=>cycle%22>=14&&cycle%22<21;
 public bool VehicleGreen=>cycle%22<12;
 bool Running=>visible&&!world.Busy&&world.Service.Game.State==GameState.Playing&&world.hud.shell.Depth==0;
 Vector2 Origin=>(Vector2)transform.position;
 void Update(){
  if(world==null||world.Service==null)return;bool local=world.Service.State.zone=="city";
  int hour=world.Service.Hour,density=hour>=7&&hour<22?pedestrians.Length:Mathf.Min(3,pedestrians.Length);
  if(local!=visible||density!=lastDensity){visible=local;lastDensity=density;for(int i=0;i<pedestrians.Length;i++){pedestrians[i].visual.enabled=visible&&i<density;if(pedestrians[i].shadow!=null)pedestrians[i].shadow.enabled=visible&&i<density;}int count=hour>=7&&hour<22?vehicles.Length:Mathf.Min(2,vehicles.Length);for(int i=0;i<vehicles.Length;i++){var v=vehicles[i];bool show=visible&&i<count;if(show&&!v.body.simulated&&Vector2.Distance(v.body.position,world.Player.Position)<3)v.body.position=Origin+new Vector2(-v.direction*16,v.laneY);v.visual.enabled=show;if(v.shadow!=null)v.shadow.enabled=show;v.body.simulated=show;if(!show)v.velocity=0;}}
  foreach(var v in vehicles)if(v.headlights!=null)v.headlights.enabled=v.visual.enabled&&(hour<7||hour>=19);
  if(!Running)return;cycle+=Time.deltaTime;
  if(signal!=null&&signalStates!=null&&signalStates.Length>=3)signal.sprite=signalStates[VehicleGreen?0:PedestrianGreen?1:2];
  if(vehicleSignal!=null)vehicleSignal.color=VehicleGreen?new Color(.35f,.95f,.58f):new Color(1,.35f,.25f);
  if(pedestrianSignal!=null)pedestrianSignal.color=PedestrianGreen?new Color(.35f,.95f,.58f):new Color(1,.35f,.25f);
  Vector2 player=world.Player.Position-Origin;
  foreach(var p in pedestrians){
   if(!p.visual.enabled||p.route==null||p.route.Length<2||p.frames==null||p.frames.Length==0)continue;
   Vector2 from=p.visual.transform.localPosition,target=p.route[p.next];
   bool blocked=p.crossing&&!p.crossingActive&&(!PedestrianGreen||VehicleOnCrossing());
   if(p.waiting>0)p.waiting-=Time.deltaTime;
   Vector2 to=Vector2.MoveTowards(from,target,p.speed*Time.deltaTime);
   bool nearPlayer=Vector2.Distance(to,player)<.7f&&Vector2.Distance(to,player)<Vector2.Distance(from,player);
   bool walking=!blocked&&!nearPlayer&&p.waiting<=0&&Vector2.Distance(from,target)>.025f;
   if(walking){if(p.crossing)p.crossingActive=true;p.visual.transform.localPosition=to;p.phase+=Vector2.Distance(from,to)*5.2f;p.visual.flipX=to.x<from.x;if(Mathf.Abs(to.x-from.x)<.001f)p.visual.flipX=p.next%2==0;}
   else if(!blocked&&!nearPlayer&&p.waiting<=0){p.next=(p.next+1)%p.route.Length;p.waiting=.4f+(p.next%3)*.35f;p.crossingActive=false;}
   p.visual.sprite=p.frames[walking?Mathf.FloorToInt(p.phase)%p.frames.Length:0];p.visual.sortingOrder=1000-Mathf.RoundToInt(p.visual.transform.position.y*100);if(p.shadow!=null)p.shadow.sortingOrder=p.visual.sortingOrder-1;
  }
 }
 bool VehicleOnCrossing(){foreach(var v in vehicles)if(v.body.simulated&&Mathf.Abs(v.body.position.x-Origin.x)<3)return true;return false;}
 bool PedestrianOnCrossing(){foreach(var p in pedestrians){Vector2 at=p.visual.transform.localPosition;if(p.visual.enabled&&Mathf.Abs(at.x)<2.7f&&at.y> -6.7f&&at.y< -2.1f)return true;}var player=world.Player.Position-Origin;return Mathf.Abs(player.x)<2.7f&&player.y> -6.7f&&player.y< -2.1f;}
 void FixedUpdate(){
  if(world==null||world.Service==null)return;bool running=Running;Vector2 player=world.Player.Position-Origin;bool pedestrian=visible&&PedestrianOnCrossing();
  foreach(var v in vehicles){
   if(!v.body.simulated)continue;if(!running){v.velocity=0;v.body.linearVelocity=Vector2.zero;continue;}
   Vector2 at=v.body.position-Origin;float desired=v.speed;float stop=(-v.direction*2.2f-at.x)*v.direction;
   if((!VehicleGreen||pedestrian)&&stop>=0&&stop<3)desired=0;
   float ahead=(player.x-at.x)*v.direction;if(ahead>0&&ahead<3&&Mathf.Abs(player.y-v.laneY)<1.05f)desired=0;
   foreach(var other in vehicles)if(other!=v&&other.body.simulated&&other.direction==v.direction){float gap=(other.body.position.x-v.body.position.x)*v.direction;if(gap>0&&gap<3.6f)desired=0;}
   v.velocity=Mathf.MoveTowards(v.velocity,desired,(desired==0?10:2.8f)*Time.fixedDeltaTime);
   Vector2 to=at+Vector2.right*(v.velocity*v.direction*Time.fixedDeltaTime);
   if(to.x*v.direction>17){Vector2 spawn=new Vector2(-v.direction*16.8f,v.laneY);if(Vector2.Distance(spawn,player)>3){v.body.position=Origin+spawn;v.velocity=0;}else{v.velocity=0;v.body.linearVelocity=Vector2.zero;}}
   else v.body.MovePosition(Origin+to);
   v.visual.sortingOrder=1000-Mathf.RoundToInt(v.body.position.y*100);if(v.shadow!=null)v.shadow.sortingOrder=v.visual.sortingOrder-1;
  }
 }
}
}
