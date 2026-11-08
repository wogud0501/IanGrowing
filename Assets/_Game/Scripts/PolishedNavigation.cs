using UnityEngine;
using UnityEngine.UI;
namespace IanGame {
public sealed class PolishedNavigation:MonoBehaviour {
 public UIShell shell;public GameObject[] panels;public Button[] buttons;public CanvasGroup group;
 void Start(){for(int i=0;i<buttons.Length;i++){int n=i;buttons[i].onClick.AddListener(()=>Open(n));}shell.Changed+=Refresh;Refresh();}
 void OnDestroy(){if(shell!=null)shell.Changed-=Refresh;}
 public void Open(int n){if(GameContext.Instance==null||GameContext.Instance.transitions.IsTransitioning||GameContext.Instance.game.BirthIntroductionPending)return;if(shell.Top==panels[n]){shell.Close(panels[n]);return;}while(shell.Depth>0){int count=shell.Depth;shell.Pop();if(count==shell.Depth)return;}shell.Push(panels[n]);}
 void Refresh(){bool major=shell.Top==null||System.Array.IndexOf(panels,shell.Top)>=0;group.interactable=group.blocksRaycasts=major;for(int i=0;i<buttons.Length;i++)buttons[i].GetComponent<Image>().color=shell.Top==panels[i]?new Color32(192,216,150,255):Color.white;}
}
}
