using System;
using System.Collections;
using UnityEngine;
namespace IanGame.Commercial {
[Serializable]public sealed class EnvironmentAnimation {
 public SpriteRenderer renderer;public Sprite[] frames;public float fps=6,speed;public Vector3 origin;public float span=7.4f,phase,idleSeconds=1.2f;public bool seated;
 public bool IsIdle{get;internal set;}
}
// One 12Hz scheduler owns all props, weather and parallax in each new location.
public sealed class LayeredLocationPresentation:MonoBehaviour {
 public SceneId location;public RoomAtmosphere room;public Camera sceneCamera;
 public SpriteRenderer[] layers;public float[] depths;public Vector3[] origins;
 public EnvironmentAnimation[] actors;public LineRenderer[] rain;
 public SpriteRenderer[] flicker;public TMPro.TMP_Text clockLabel;
 public int TickCount{get;private set;}public int LiveRain{get;private set;}
 public bool SeatedVisible=true;GameManager game;Vector3 cameraOrigin;int displayedMinute=-1;
 void Start(){game=GameContext.Instance==null?null:GameContext.Instance.game;cameraOrigin=sceneCamera.transform.position;StartCoroutine(Animate());}
 IEnumerator Animate(){var wait=new WaitForSecondsRealtime(1f/12);while(true){
  if(game==null||game.State!=GameState.Paused){TickCount++;float time=Time.unscaledTime;int quality=PlayerPrefs.GetInt("ian.visual.effects",1);bool motion=!UIShell.ReducedMotion&&quality>0;
   float travel=motion?Mathf.Round(Mathf.Sin(time*.17f)*2)/100f:0;sceneCamera.transform.position=cameraOrigin+Vector3.right*travel;
   for(int i=0;i<layers.Length;i++){layers[i].transform.localPosition=origins[i]+Vector3.right*(Mathf.Round(travel*depths[i]*100)/100);layers[i].color=room.background.color;}
   foreach(var actor in actors){if(actor.renderer==null)continue;actor.renderer.enabled=!actor.seated||SeatedVisible;float x=actor.origin.x;actor.IsIdle=false;if(motion&&actor.speed!=0){float duration=actor.span/Mathf.Abs(actor.speed),half=duration/2;float phase=Mathf.Repeat(time+actor.phase,duration+actor.idleSeconds);actor.IsIdle=phase>=half&&phase<half+actor.idleSeconds;float walkingTime=phase<half?phase:actor.IsIdle?half:phase-actor.idleSeconds;x=-Mathf.Sign(actor.speed)*actor.span/2+walkingTime*actor.speed;}if(actor.frames!=null&&actor.frames.Length>0)actor.renderer.sprite=actor.frames[motion&&!actor.IsIdle?Mathf.FloorToInt((time+actor.phase)*actor.fps)%actor.frames.Length:0];float idleBob=motion&&actor.IsIdle?Mathf.Round(Mathf.Sin(time*1.6f+actor.phase))/100f:0;actor.renderer.transform.localPosition=new Vector3(Mathf.Round(x*100)/100,actor.origin.y+idleBob,actor.origin.z);}
   LiveRain=0;for(int i=0;i<rain.Length;i++){bool show=motion&&(quality==2||i<8);rain[i].enabled=show;if(!show)continue;LiveRain++;float x=-3.3f+Mathf.Repeat(i*.47f+time*.035f,6.6f),y=1.9f-Mathf.Repeat(time*1.8f+i*.31f,3.8f);rain[i].SetPosition(0,new Vector3(Mathf.Round(x*100)/100,Mathf.Round(y*100)/100,0));rain[i].SetPosition(1,new Vector3(Mathf.Round((x-.025f)*100)/100,Mathf.Round((y-.07f)*100)/100,0));}
   for(int i=0;i<flicker.Length;i++)if(flicker[i]!=null)flicker[i].color=Color.Lerp(new Color(.5f,.7f,.85f),Color.white,!motion?1:Mathf.Sin(time*.8f+i)*Mathf.Sin(time*1.3f+i)>.72f?.35f:1);
   int minute=game==null?480:((int)(game.clock.SubDaySeconds/60)+480)%1440;if(clockLabel!=null&&minute!=displayedMinute){displayedMinute=minute;clockLabel.text=(location==SceneId.Workplace?"업무 시각  ":"도시 시각  ")+(minute/60).ToString("00")+":"+(minute%60).ToString("00")+(minute>=1140?"  ·  야근은 자율입니다.":"  ·  오늘도 정상 영업");}
  }yield return wait;
 }}
}
}

