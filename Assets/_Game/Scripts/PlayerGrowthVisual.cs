using UnityEngine;
namespace IanGame {
// Presentation is derived from the authoritative LifeProgression age, never saved
// as a second mutable age/stage. Existing gameplay permissions remain separate.
[DefaultExecutionOrder(-50)] public sealed class PlayerGrowthVisual:MonoBehaviour {
 public WorldPlayerMotor motor; public GrowthVisualCatalog catalog;
 public GrowthVisualProfile Current{get;private set;} public bool Young=>Current!=null&&Current.stage!=VisualGrowthStage.Adult;
 public bool Deferred{get;private set;} public Vector2 ActionOffset; public int ActionOrder=int.MinValue;
 public bool InBed{get;set;} public bool Seated{get;set;} public string ActivePose{get;private set;}="Idle";
 public Vector2 DisplayOffset=>ActionOffset+(Young&&Seated?Current.seatOffset:Vector2.zero);
 public Quaternion DisplayRotation=>Young&&InBed&&(ActivePose=="Sleep"||ActivePose=="Wake Up"&&motor.advanced.Frame==0)&&Current.sleepHorizontal?Quaternion.Euler(0,0,-90):Quaternion.identity;
 public float Speed=>Young?Current.speedFactor:1;
 GameManager game;
 void OnEnable(){RefreshBinding();}
 void Start(){RefreshBinding();}
 void Update(){RefreshBinding();if(Current?.stage==VisualGrowthStage.Newborn&&game!=null&&!motor.world.Busy&&motor.world.CanControl){string next=game.stats.Hunger<=20||game.stats.Thirst<=20?"InfantCry":"Idle";if(ActivePose!=next)motor.Pose(next);}}
 public void RefreshBinding(){
  var next=GameContext.Instance?.game;if(next==null||catalog==null||motor==null)return;
  if(game!=next){if(game!=null)game.growth.Changed-=RefreshAge;game=next;game.growth.Changed+=RefreshAge;}
  RefreshAge();
 }
 public void RefreshAge(){if(game==null||catalog==null)return;var p=catalog.AtAge(game.growth.Age);if(p==null||p==Current)return;
  if(Current!=null&&motor.world.Busy){Deferred=true;return;}Deferred=false;Current=p;
  motor.advanced.library=p.animations;motor.GetComponent<CircleCollider2D>().radius=p.colliderRadius;motor.GetComponent<CircleCollider2D>().offset=p.colliderOffset;
  motor.advanced.Play(ActorMotion.Idle,motor.Facing,true);motor.advanced.Render(ActorMotion.Idle,motor.Facing);ActionOffset=Vector2.zero;ActionOrder=int.MinValue;InBed=Seated=false;
 }
 public string CarePose(int action)=>action==-4?"CareBath":action==3?"CareFeed":action==4?(Current?.stage==VisualGrowthStage.Newborn?"CareFeed":"Drink"):"Play";
 public void Pose(string pose){ActivePose=pose;}
 public Vector2 Inside(WorldLifeAction a)=>a.anchor==null?a.GetComponent<WorldInteractable>().Point:(Vector2)a.anchor.position+(Young&&a.mode==LifeActionVisual.Shower?Current.showerOffset:Vector2.zero);
 public Vector2 SleepPoint(WorldLifeAction a){var b=a.GetComponent<WorldInteractable>().visual.bounds;return new Vector2(b.center.x,b.max.y-.22f-Current.height*.45f);}
 public void ClearAction(){ActionOffset=Vector2.zero;ActionOrder=int.MinValue;InBed=Seated=false;}
 void OnDisable(){if(game!=null)game.growth.Changed-=RefreshAge;game=null;ClearAction();}
}
}
