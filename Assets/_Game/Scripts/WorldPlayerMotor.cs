using UnityEngine;
using UnityEngine.InputSystem;
using IanGame.Commercial;
namespace IanGame {
[RequireComponent(typeof(Rigidbody2D),typeof(CircleCollider2D))]
public sealed class WorldPlayerMotor:MonoBehaviour {
 public LifeWorldController world;public SpriteRenderer sprite;public SpriteMotionLibrary motions;public CharacterIdentity identity;public Transform bodyVisual;public Vector2 Facing=Vector2.down;
 Rigidbody2D body;Vector2 input;float walkPhase;string pose="Idle";float poseStart;
 public Vector2 Position=>body==null?(Vector2)transform.position:body.position;
 void Awake(){body=GetComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;body.interpolation=RigidbodyInterpolation2D.Interpolate;}
 void Update(){input=Vector2.zero;if(world!=null&&world.CanControl){var k=Keyboard.current;if(k!=null)input=new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0));if(Gamepad.current!=null&&Gamepad.current.leftStick.ReadValue().sqrMagnitude>.1f)input=Gamepad.current.leftStick.ReadValue();input=Vector2.ClampMagnitude(input,1);if(input.sqrMagnitude>.01f){Facing=input;walkPhase+=Time.deltaTime*9;}}
  bool walk=input.sqrMagnitude>.01f;float t=Time.unscaledTime-poseStart;float bob=pose=="Sleep"?0:walk?Mathf.Abs(Mathf.Sin(walkPhase))*.05f:Mathf.Sin(Time.unscaledTime*1.6f)*.012f;bodyVisual.localPosition=new Vector3(0,.95f+bob,0);bodyVisual.localRotation=Quaternion.Euler(0,0,pose=="Sleep"?78:pose=="Work"?Mathf.Sin(t*8)*2:pose=="Wash"||pose=="Shower"?Mathf.Sin(t*10)*3:walk?Mathf.Sin(walkPhase)*2:0);bodyVisual.localScale=new Vector3(1,pose=="Sit"||pose=="Eat"||pose=="Work"?.88f:1,1);sprite.flipX=Facing.x<-.1f;sprite.sortingOrder=1000-Mathf.RoundToInt(transform.position.y*100);
  if(pose=="Wake Up")bodyVisual.localScale=new Vector3(1,Mathf.Lerp(.65f,1,Mathf.Clamp01(t/.35f)),1);
  if(motions!=null){SpriteMotion state=pose=="Sleep"?SpriteMotion.Sleep:pose=="Eat"?SpriteMotion.Eat:pose=="Work"?SpriteMotion.Work:walk?SpriteMotion.Walk:SpriteMotion.Idle;var clip=motions.Find(state);sprite.sprite=clip!=null&&clip.frames.Length>0?clip.frames[Mathf.FloorToInt(t*clip.framesPerSecond)%clip.frames.Length]:motions.fallback;}
  if(identity!=null&&(pose=="Idle"||walk)){string direction=Mathf.Abs(Facing.x)>Mathf.Abs(Facing.y)?Facing.x<0?"west":"east":Facing.y>0?"north":"south";var directional=identity.State((walk?"walk-":"idle-")+direction);if(directional!=null&&directional!=identity.baseSprite)sprite.sprite=directional;}
 }
 void FixedUpdate(){if(world==null||!world.CanControl||input.sqrMagnitude<.001f)return;float fatigue=world.Service.State.fatigue>85?.7f:1;body.MovePosition(body.position+new Vector2(input.x,input.y*.78f)*world.Service.rules.moveSpeed*fatigue*Time.fixedDeltaTime);}
 public void Warp(Vector2 point){if(body==null)body=GetComponent<Rigidbody2D>();body.position=point;transform.position=point;body.linearVelocity=Vector2.zero;input=Vector2.zero;}
 public void Pose(string state){pose=state;poseStart=Time.unscaledTime;}
}
}
