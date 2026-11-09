using UnityEngine;
using UnityEngine.InputSystem;
using IanGame.Commercial;
namespace IanGame {
[RequireComponent(typeof(Rigidbody2D),typeof(CircleCollider2D))]
public sealed class WorldPlayerMotor:MonoBehaviour {
 public LifeWorldController world;public SpriteRenderer sprite;public SpriteMotionLibrary motions;public CharacterIdentity identity;public Transform bodyVisual;public Vector2 Facing=Vector2.down;
 public InputActionAsset controls;
 public Animator locomotion;public WorldWalkSet walkSet;public SpriteRenderer contactShadow;
 [Min(.1f)]public float visualScale=1;
 [Min(1)] public float acceleration=40, braking=64;
 Rigidbody2D body;Vector2 input,velocity;float walkPhase;string pose="Idle";float poseStart;
 InputActionAsset runtimeControls;InputActionMap gameplay;InputAction move,interact,inventory,menu;
 Vector2 observedPosition;float actualSpeed,walkDistance;bool observed;
 public bool TextInputFocused {get {var es=UnityEngine.EventSystems.EventSystem.current;if(es==null||es.currentSelectedGameObject==null)return false;var field=es.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>();return field!=null&&field.isFocused;}}
 public bool InteractPressed=>OwnsKeyboardFocus()&&interact!=null&&interact.WasPressedThisFrame();
 public bool InventoryPressed=>OwnsKeyboardFocus()&&inventory!=null&&inventory.WasPressedThisFrame();
 public bool MenuPressed=>OwnsKeyboardFocus()&&menu!=null&&menu.WasPressedThisFrame();
 public Vector2 Movement=>velocity;
 void OnEnable(){
  // Own a clone: enabling gameplay must never enable/disable EventSystem's UI map.
  if(controls==null){Debug.LogError("LifeWorld: gameplay InputActionAsset is missing.",this);return;}
  runtimeControls=Instantiate(controls);gameplay=runtimeControls.FindActionMap("World",true);
  move=gameplay.FindAction("Move",true);interact=gameplay.FindAction("Interact",true);
  inventory=gameplay.FindAction("Inventory",true);menu=gameplay.FindAction("Menu",true);gameplay.Enable();
 }
 void OnDisable(){StopMotion();gameplay?.Disable();if(runtimeControls!=null)Destroy(runtimeControls);runtimeControls=null;gameplay=null;move=interact=inventory=menu=null;}
 void OnApplicationFocus(bool focused){if(!focused)StopMotion();}
 void StopMotion(){input=velocity=Vector2.zero;actualSpeed=0;observed=false;if(body!=null)body.linearVelocity=Vector2.zero;}
 public Vector2 Position=>body==null?(Vector2)transform.position:body.position;
 void Awake(){body=GetComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;body.interpolation=RigidbodyInterpolation2D.Interpolate;}
 bool OwnsKeyboardFocus(){if(!Application.isFocused)return false;
 #if UNITY_EDITOR
  if(UnityEditor.EditorWindow.focusedWindow==null||UnityEditor.EditorWindow.focusedWindow.GetType().Name!="GameView")return false;
 #endif
  return true;
 }
 void Update(){bool controlling=world!=null&&world.CanControl&&OwnsKeyboardFocus();input=Vector2.zero;if(controlling&&move!=null){input=Vector2.ClampMagnitude(move.ReadValue<Vector2>(),1);if(input.sqrMagnitude>.01f){Facing=input;walkPhase+=Time.deltaTime*9;}}
  if(locomotion!=null&&walkSet!=null&&walkSet.Complete){
   bool walking=controlling&&actualSpeed>.08f&&pose=="Idle";
   string state=(walking?"Walk_":"Idle_")+walkSet.Direction(Facing);
   locomotion.speed=0;locomotion.Play(state,0,walking?Mathf.Repeat(walkDistance/walkSet.strideLength,1):0);locomotion.Update(0);
   float progress=Mathf.Clamp01((Time.unscaledTime-poseStart)/.35f);bool resting=pose=="Sleep",waking=pose=="Wake Up";float recline=resting?progress:waking?1-progress:0;
   bodyVisual.localPosition=Vector3.up*(recline*.55f);bodyVisual.localScale=new Vector3(visualScale,visualScale*Mathf.Lerp(1,.85f,recline),1);bodyVisual.localRotation=Quaternion.identity;
   sprite.flipX=false;sprite.sortingOrder=1000-Mathf.RoundToInt(transform.position.y*100)+(resting||waking?50:0);return;
  }
  bool walk=input.sqrMagnitude>.01f;float t=Time.unscaledTime-poseStart;float bob=pose=="Sleep"?0:walk?Mathf.Abs(Mathf.Sin(walkPhase))*.05f:Mathf.Sin(Time.unscaledTime*1.6f)*.012f;bodyVisual.localPosition=new Vector3(0,.95f+bob,0);bodyVisual.localRotation=Quaternion.Euler(0,0,pose=="Sleep"?78:pose=="Work"?Mathf.Sin(t*8)*2:pose=="Wash"||pose=="Shower"?Mathf.Sin(t*10)*3:walk?Mathf.Sin(walkPhase)*2:0);bodyVisual.localScale=new Vector3(visualScale,visualScale*(pose=="Sit"||pose=="Eat"||pose=="Work"?.88f:1),1);sprite.flipX=Facing.x<-.1f;sprite.sortingOrder=1000-Mathf.RoundToInt(transform.position.y*100);
  if(pose=="Wake Up")bodyVisual.localScale=new Vector3(visualScale,visualScale*Mathf.Lerp(.65f,1,Mathf.Clamp01(t/.35f)),1);
  if(motions!=null){SpriteMotion state=pose=="Sleep"?SpriteMotion.Sleep:pose=="Eat"?SpriteMotion.Eat:pose=="Work"?SpriteMotion.Work:walk?SpriteMotion.Walk:SpriteMotion.Idle;var clip=motions.Find(state);sprite.sprite=clip!=null&&clip.frames.Length>0?clip.frames[Mathf.FloorToInt(t*clip.framesPerSecond)%clip.frames.Length]:motions.fallback;}
  if(identity!=null&&(pose=="Idle"||walk)){string direction=Mathf.Abs(Facing.x)>Mathf.Abs(Facing.y)?Facing.x<0?"west":"east":Facing.y>0?"north":"south";var directional=identity.State((walk?"walk-":"idle-")+direction);if(directional!=null&&directional!=identity.baseSprite)sprite.sprite=directional;}
 }
 void FixedUpdate(){if(world==null||!world.CanControl||!OwnsKeyboardFocus()){StopMotion();return;}float distance=observed?Vector2.Distance(observedPosition,body.position):0;observedPosition=body.position;observed=true;actualSpeed=distance/Time.fixedDeltaTime;if(distance<world.Service.rules.moveSpeed*Time.fixedDeltaTime*2)walkDistance+=distance;float fatigue=world.Service.State.fatigue>85?.7f:1;Vector2 target=input*world.Service.rules.moveSpeed*fatigue;velocity=Vector2.MoveTowards(velocity,target,(input.sqrMagnitude>.001f?acceleration:braking)*Time.fixedDeltaTime);body.MovePosition(body.position+velocity*Time.fixedDeltaTime);}
 public void Warp(Vector2 point){if(body==null)body=GetComponent<Rigidbody2D>();StopMotion();body.position=point;transform.position=point;}
 void LateUpdate(){if(contactShadow!=null)contactShadow.sortingOrder=sprite.sortingOrder-1;}
 public void Pose(string state){pose=state;poseStart=Time.unscaledTime;}
}
}
