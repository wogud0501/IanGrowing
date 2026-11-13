using UnityEngine;
using UnityEngine.InputSystem;
using IanGame.Commercial;
namespace IanGame {
[RequireComponent(typeof(Rigidbody2D),typeof(CircleCollider2D))]
public sealed class WorldPlayerMotor:MonoBehaviour {
 public LifeWorldController world;public SpriteRenderer sprite;public SpriteMotionLibrary motions;public CharacterIdentity identity;public Transform bodyVisual;public Vector2 Facing=Vector2.down;
 public InputActionAsset controls;public Animator locomotion;public WorldWalkSet walkSet;public SpriteRenderer contactShadow;public WorldActorAnimation advanced;
 [Min(.1f)]public float visualScale=1;[Min(1)]public float acceleration=40,braking=64;[Min(1)]public float runMultiplier=1.65f;
 Rigidbody2D body;Vector2 input,velocity,observedPosition;float actualSpeed,pendingDistance;bool observed,wasMoving;string pose="Idle";float stopUntil,turnUntil,reactionRemaining;ActorMotion reaction;
 InputActionAsset runtimeControls;InputActionMap gameplay;InputAction move,interact,inventory,menu,run;
 bool scripted;Vector2 traversalTarget;float traversalSpeed;
 public float TraversalSeconds{get;private set;}
 public bool TraversalArrived{get;private set;}public bool IsTraversing=>scripted;public float ActualSpeed=>actualSpeed;public Vector2 Movement=>velocity;
 public bool TextInputFocused {get{var es=UnityEngine.EventSystems.EventSystem.current;var selected=es==null?null:es.currentSelectedGameObject;var field=selected==null?null:selected.GetComponent<TMPro.TMP_InputField>();return field!=null&&field.isFocused;}}
 public bool InteractPressed=>OwnsKeyboardFocus()&&interact!=null&&interact.WasPressedThisFrame();
 public bool InventoryPressed=>OwnsKeyboardFocus()&&inventory!=null&&inventory.WasPressedThisFrame();
 public bool MenuPressed=>OwnsKeyboardFocus()&&menu!=null&&menu.WasPressedThisFrame();
 void Awake(){body=GetComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;body.interpolation=RigidbodyInterpolation2D.Interpolate;}
 void OnEnable(){
  if(controls==null){Debug.LogError("LifeWorld: gameplay InputActionAsset is missing.",this);return;}
  runtimeControls=Instantiate(controls);gameplay=runtimeControls.FindActionMap("World",true);
  move=gameplay.FindAction("Move",true);interact=gameplay.FindAction("Interact",true);inventory=gameplay.FindAction("Inventory",true);menu=gameplay.FindAction("Menu",true);run=gameplay.FindAction("Run");gameplay.Enable();
 }
 void OnDisable(){CancelTraversal();StopMotion();gameplay?.Disable();if(runtimeControls!=null)Destroy(runtimeControls);runtimeControls=null;gameplay=null;move=interact=inventory=menu=run=null;}
 void OnApplicationFocus(bool focused){if(!focused)StopMotion();}void OnApplicationPause(bool paused){if(paused)StopMotion();}
 void StopMotion(){input=velocity=Vector2.zero;actualSpeed=0;pendingDistance=0;observed=false;if(body!=null)body.linearVelocity=Vector2.zero;}
 public Vector2 Position=>body==null?(Vector2)transform.position:body.position;
 bool OwnsKeyboardFocus(){if(!Application.isFocused)return false;
 #if UNITY_EDITOR
  if(UnityEditor.EditorWindow.focusedWindow==null||UnityEditor.EditorWindow.focusedWindow.GetType().Name!="GameView")return false;
 #endif
  return true;
 }
 public void BeginTraversal(Vector2 destination,float speed=2.2f){StopMotion();traversalTarget=destination;traversalSpeed=Mathf.Max(.1f,speed);TraversalArrived=false;TraversalSeconds=0;scripted=true;pose="Idle";}
 public void CancelTraversal(){scripted=false;TraversalArrived=false;StopMotion();}
 void Update(){
  bool controlling=world!=null&&world.CanControl&&OwnsKeyboardFocus();input=controlling&&move!=null?Vector2.ClampMagnitude(move.ReadValue<Vector2>(),1):Vector2.zero;
  if(scripted)input=Vector2.ClampMagnitude(traversalTarget-Position,1);
  if(actualSpeed>.03f&&velocity.sqrMagnitude>.0001f)Facing=velocity.normalized;
  bool moving=actualSpeed>.06f;bool running=controlling&&run!=null&&run.IsPressed()&&world.Service.State.fatigue<85;
  if(wasMoving&&!moving)stopUntil=Time.unscaledTime+.24f;wasMoving=moving;
  if(advanced!=null&&advanced.library!=null){
   ActorMotion motion=Parse(pose);
   if(pose=="Idle")motion=moving?(running?ActorMotion.Run:ActorMotion.Walk):Time.unscaledTime<stopUntil?ActorMotion.Stop:Time.unscaledTime<turnUntil?ActorMotion.Turn:reactionRemaining>0?reaction:ActorMotion.Idle;
   if(pose=="Idle"&&!moving&&reactionRemaining>0)reactionRemaining=Mathf.Max(0,reactionRemaining-Time.unscaledDeltaTime);
   bool walking=motion==ActorMotion.Walk||motion==ActorMotion.Run;
   advanced.Render(motion,Facing,pendingDistance,walking);pendingDistance=0;
   bodyVisual.localPosition=Vector3.zero;bodyVisual.localScale=Vector3.one*visualScale;bodyVisual.localRotation=Quaternion.identity;
  }else if(locomotion!=null&&walkSet!=null&&walkSet.Complete){
   locomotion.enabled=true;locomotion.speed=0;locomotion.Play((moving?"Walk_":"Idle_")+walkSet.Direction(Facing),0,moving?Mathf.Repeat(Time.unscaledTime*actualSpeed/walkSet.strideLength,1):0);locomotion.Update(0);
  }
  sprite.sortingOrder=1000-Mathf.RoundToInt(transform.position.y*100)+(pose=="Sleep"||pose=="Wake Up"?50:0);
 }
 void FixedUpdate(){
  if(world==null||world.Service==null||world.Service.Game.State!=GameState.Playing||!scripted&&(!world.CanControl||!OwnsKeyboardFocus())){StopMotion();return;}
  if(scripted)TraversalSeconds+=Time.fixedDeltaTime;
  float distance=observed?Vector2.Distance(observedPosition,body.position):0;observedPosition=body.position;observed=true;actualSpeed=distance/Time.fixedDeltaTime;
  float maximum=world.Service.rules.moveSpeed*runMultiplier*Time.fixedDeltaTime*2;if(distance<maximum)pendingDistance+=distance;
  if(scripted&&Vector2.Distance(body.position,traversalTarget)<.045f){scripted=false;TraversalArrived=true;StopMotion();return;}
  float fatigue=world.Service.State.fatigue>85?.7f:1;bool running=!scripted&&run!=null&&run.IsPressed()&&fatigue==1;
  Vector2 target=scripted?(traversalTarget-body.position).normalized*traversalSpeed:input*world.Service.rules.moveSpeed*fatigue*(running?runMultiplier:1);
  velocity=Vector2.MoveTowards(velocity,target,(target.sqrMagnitude>.001f?acceleration:braking)*Time.fixedDeltaTime);
  var next=body.position+velocity*Time.fixedDeltaTime;if(scripted&&(traversalTarget-body.position).sqrMagnitude<(next-body.position).sqrMagnitude)next=traversalTarget;
  body.MovePosition(next);
 }
 public void Warp(Vector2 point){if(body==null)body=GetComponent<Rigidbody2D>();scripted=false;StopMotion();body.position=point;transform.position=point;}
 void LateUpdate(){if(contactShadow!=null)contactShadow.sortingOrder=sprite.sortingOrder-1;}
 public void Face(Vector2 direction){if(direction.sqrMagnitude<.0001f)return;if(ActorAnimationSet.Direction(direction)!=ActorAnimationSet.Direction(Facing))turnUntil=Time.unscaledTime+.18f;Facing=direction.normalized;}
 public void React(ActorMotion motion,float seconds=.8f){reaction=motion;reactionRemaining=seconds;}public void ClearReaction(){reactionRemaining=0;}
 public void Pose(string state){pose=state;if(advanced!=null)advanced.Play(Parse(state),Facing,true);}
 static ActorMotion Parse(string state){switch(state){case "Wake Up":return ActorMotion.WakeUp;case "Sit":return ActorMotion.SitDown;case "Wash":return ActorMotion.WashFace;case "Stand Up":return ActorMotion.StandUp;case "Open Door":return ActorMotion.OpenDoor;case "Use Computer":return ActorMotion.UseComputer;case "Use Phone":return ActorMotion.UsePhone;default:return System.Enum.TryParse(state.Replace(" ",""),out ActorMotion motion)?motion:ActorMotion.Interact;}}
}
}
