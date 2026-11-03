using UnityEngine;
namespace IanGame {
public class GameplaySceneBindings:MonoBehaviour{
 public GameManager game;public UIManager hud;public Character2DPresentation character;
 void Awake(){if(game==null){Debug.LogError("Ian: GameManager is missing.",this);enabled=false;return;}if(hud!=null)hud.Bind(game);if(character!=null)character.Bind(game);}
}
}