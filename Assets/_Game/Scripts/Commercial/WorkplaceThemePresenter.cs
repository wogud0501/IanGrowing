using UnityEngine;
namespace IanGame.Commercial {
public sealed class WorkplaceThemePresenter:MonoBehaviour {
 public WorkplaceVisualTheme[] themes;public RoomAtmosphere room;public LayeredLocationPresentation environment;public SpriteRenderer monitor;public TMPro.TMP_Text label;
 public string AppliedJob{get;private set;}GameManager game;
 void Start(){game=GameContext.Instance==null?null:GameContext.Instance.game;if(game!=null){game.StateChanged+=State;game.ActionPerformed+=Action;}Apply();}
 void OnDestroy(){if(game!=null){game.StateChanged-=State;game.ActionPerformed-=Action;}}
 void State(GameState state){Apply();}void Action(int id){Apply();}
 public void Apply(){if(game==null||game.phase23==null||themes==null||themes.Length==0)return;string id=game.phase23.Progress.employment.currentJobId;if(AppliedJob==id)return;AppliedJob=id;var theme=System.Array.Find(themes,t=>t.jobId==id)??themes[0];room.background.sprite=theme.background;room.dayTint=theme.dayTint;room.nightTint=theme.nightTint;environment.SeatedVisible=theme.seatedCoworker;if(room.animatedDetail!=null)room.animatedDetail.enabled=theme.seatedCoworker;if(monitor!=null){monitor.color=theme.monitorTint;if(theme.monitorFrames!=null&&theme.monitorFrames.Length>0)foreach(var actor in environment.actors)if(actor.renderer==monitor)actor.frames=theme.monitorFrames;}if(label!=null)label.text=theme.displayName+"  /  WORKPLACE";}
}
}
