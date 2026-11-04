using UnityEngine;using UnityEngine.UI;
namespace IanGame {
public class MainMenuPresenter:MonoBehaviour {
 public Button continueButton,newGameButton;
 void OnEnable(){if(continueButton!=null)continueButton.onClick.AddListener(Continue);if(newGameButton!=null)newGameButton.onClick.AddListener(NewGame);}
 void OnDisable(){if(continueButton!=null)continueButton.onClick.RemoveListener(Continue);if(newGameButton!=null)newGameButton.onClick.RemoveListener(NewGame);}
 public void Continue(){if(GameContext.Instance!=null)GameContext.Instance.transitions.Navigate(SceneId.Home);}
 public void NewGame(){if(GameContext.Instance==null)return;var shell=UIShell.For(this);if(shell!=null){shell.ConfirmNewLife();return;}GameContext.Instance.game.NewGame();Continue();}
}
}