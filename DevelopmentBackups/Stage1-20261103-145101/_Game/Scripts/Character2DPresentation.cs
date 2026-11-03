using UnityEngine;
namespace IanGame {
[DisallowMultipleComponent]
public class Character2DPresentation:MonoBehaviour {
 public SpriteRenderer spriteRenderer;public Character2DVisuals visuals;
 [SerializeField,Min(.1f)]float reactionDuration=1.6f;
 GameManager game;int action=-1;float elapsed;Vector3 origin;
 public string CurrentAction=>action<0?"Idle":action==0?"Work":action==1?"Rest":"Play";
 void Awake(){origin=transform.localPosition;}
 public void Bind(GameManager model){if(game!=null)game.ActionPerformed-=React;game=model;if(isActiveAndEnabled&&game!=null)game.ActionPerformed+=React;}
 void OnEnable(){if(game!=null){game.ActionPerformed-=React;game.ActionPerformed+=React;}}
 void OnDisable(){if(game!=null)game.ActionPerformed-=React;}
 void React(int value){action=value;elapsed=0;RenderFrame();}
 void Update(){elapsed+=Time.deltaTime;if(action>=0&&elapsed>=reactionDuration){action=-1;elapsed=0;}RenderFrame();float bob=action==2?.065f:action==1?.008f:.018f;transform.localPosition=origin+Vector3.up*(Mathf.Sin(Time.time*3)*bob);}
 void RenderFrame(){if(spriteRenderer==null||visuals==null)return;var frames=visuals.Frames(action);if(frames==null||frames.Length==0)frames=visuals.idle;if(frames==null||frames.Length==0)return;var frame=frames[Mathf.FloorToInt(elapsed*Mathf.Max(1,visuals.framesPerSecond))%frames.Length];if(frame!=null)spriteRenderer.sprite=frame;}
}
}