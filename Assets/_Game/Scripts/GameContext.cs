using UnityEngine;
namespace IanGame {
[DefaultExecutionOrder(-1000)]
public class GameContext:MonoBehaviour {
 public static GameContext Instance{get;private set;}public GameManager game;public SceneTransitionService transitions;public PlayerStats Stats=>game.stats;public EconomySystem Economy=>game.economy;public SaveManager Saves=>game.saves;public MissionService Missions=>game.missions;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatic(){Instance=null;}
 void Awake(){if(Instance!=null&&Instance!=this){gameObject.SetActive(false);Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);}
 // Script recompilation in Play reloads static fields without invoking Awake again.
 void OnEnable(){if(Instance==null)Instance=this;}
 void OnDestroy(){if(Instance==this)Instance=null;}
}
}
