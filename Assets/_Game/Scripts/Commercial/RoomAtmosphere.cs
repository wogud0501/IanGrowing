using System.Collections;
using UnityEngine;
namespace IanGame.Commercial {
// One low-frequency scheduler per room. Props have no Update or runtime allocations.
public sealed class RoomAtmosphere : MonoBehaviour {
 public bool school;public SpriteRenderer background,character,animatedDetail;public SpriteRenderer[] motes;
 public Transform clockHand;public Vector3 characterOrigin;
 public int TickCount{get;private set;}public string CurrentState{get;private set;}="Idle";
 GameManager game;Vector3[] origins;int action=-1;float actionUntil;
 void Start(){game=GameContext.Instance==null?null:GameContext.Instance.game;if(game!=null)game.ActionPerformed+=React;origins=new Vector3[motes.Length];for(int i=0;i<motes.Length;i++)origins[i]=motes[i].transform.localPosition;StartCoroutine(Tick());}
 void OnDestroy(){if(game!=null)game.ActionPerformed-=React;}
 public void Enter(){CurrentState="Interaction";}
 void React(int id){action=id;actionUntil=Time.unscaledTime+2.4f;CurrentState=id==5?"Sleep":id==3?"Eat":id==4?"Drink":id==0?"Work":id>=7?"Study":"Interaction";}
 IEnumerator Tick(){var delay=new WaitForSecondsRealtime(.125f);while(true){
  if(game==null||game.State!=GameState.Paused){TickCount++;float t=Time.unscaledTime;
   if(action>=0&&t>actionUntil){action=-1;CurrentState="Idle";}
   bool reduced=UIShell.ReducedMotion;int quality=PlayerPrefs.GetInt("ian.visual.effects",1);
   if(character!=null)character.transform.localPosition=characterOrigin+Vector3.up*(reduced?0:Mathf.Round(Mathf.Sin(t*1.6f))/100f);
   float hour=game==null?12:(float)(game.clock.SubDaySeconds/3600+8)%24;
   if(background!=null)background.color=Color.Lerp(Color.white,new Color(.49f,.57f,.79f),hour>=19||hour<6?.48f:0);
   for(int i=0;i<motes.Length;i++){bool active=!reduced&&quality>0&&(quality>1||i<4);motes[i].enabled=active;if(!active)continue;float phase=(t*.12f+i*.173f)%1;motes[i].transform.localPosition=origins[i]+new Vector3(Mathf.Round(Mathf.Sin(t*.3f+i)*4)/100f,Mathf.Round(phase*25)/100f,0);var c=motes[i].color;c.a=.15f+.25f*Mathf.Abs(Mathf.Sin(t*.7f+i));motes[i].color=c;}
   if(animatedDetail!=null)animatedDetail.color=Color.Lerp(new Color(.72f,.85f,1),Color.white,reduced?1:.75f+.25f*Mathf.Sin(t*(school?.4f:1.3f)));
   if(clockHand!=null)clockHand.localRotation=Quaternion.Euler(0,0,-Mathf.Floor(t/2)*6);
  }yield return delay;
 }}
}
}
