using UnityEngine;
namespace IanGame {
public class GameplaySceneBindings:MonoBehaviour {
 public GameManager game;public UIManager hud;public Character2DPresentation character;public MissionUI missionUI;bool bound;
 void Awake(){Resolve();}
 void Start(){Resolve();}
 void Resolve(){if(bound)return;if(game==null&&GameContext.Instance!=null)game=GameContext.Instance.game;if(game==null)return;bound=true;if(hud!=null)hud.Bind(game);if(character!=null)character.Bind(game);if(missionUI!=null)missionUI.Bind(game.missions);}
}
}
