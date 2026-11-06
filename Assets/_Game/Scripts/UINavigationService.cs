using System;using UnityEngine;using UnityEngine.UI;
namespace IanGame {
// UI navigation only; location changes are delegated to the existing scene service.
public class UINavigationService:MonoBehaviour {
 public UIShell stack;public GameObject lifePanel,growthPanel,activityPanel,inventoryPanel,menuPanel,missionPanel,locationPanel;public CanvasGroup navigation;public PixelTabGroup growthTabs;
 static bool openLocationAfterTransition;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetPending(){openLocationAfterTransition=false;}
 void OnEnable(){if(stack!=null)stack.Changed+=RefreshNavigation;}void OnDisable(){if(stack!=null)stack.Changed-=RefreshNavigation;}
 void Start(){if(openLocationAfterTransition){openLocationAfterTransition=false;if(locationPanel!=null)stack.Push(locationPanel);}RefreshNavigation();}
 void RefreshNavigation(){if(navigation==null)return;bool major=stack.Top==null||stack.Top==lifePanel||stack.Top==growthPanel||stack.Top==activityPanel;navigation.interactable=major;navigation.blocksRaycasts=major;}
 public void Home(){if(GameContext.Instance!=null&&GameContext.Instance.transitions.IsTransitioning)return;CloseAll();if(GameContext.Instance!=null&&GameContext.Instance.transitions.CurrentScene!=SceneId.Home)GameContext.Instance.transitions.Navigate(SceneId.Home);}
 public void Life(){Major(lifePanel);}public void Growth(){Major(growthPanel);}public void Activity(){Major(activityPanel);}public void Menu(){stack.Push(menuPanel);}public void Inventory(){stack.Push(inventoryPanel);}public void Mission(){stack.Push(missionPanel);}
 public void GrowthTab(int tab){Major(growthPanel);growthTabs.Select(tab);}
 void Major(GameObject panel){if(GameContext.Instance!=null&&GameContext.Instance.transitions.IsTransitioning)return;if(stack.Top==panel)return;CloseAll();stack.Push(panel);RefreshNavigation();}
 public void CloseAll(){while(stack.Depth>0){int before=stack.Depth;stack.Pop();if(stack.Depth==before)break;}}
 public void Location(SceneId id){if(GameContext.Instance!=null&&GameContext.Instance.transitions.IsTransitioning)return;CloseAll();if(GameContext.Instance==null)return;if(GameContext.Instance.transitions.CurrentScene==id){if(locationPanel!=null)stack.Push(locationPanel);}else{openLocationAfterTransition=id!=SceneId.Home;if(!GameContext.Instance.transitions.Navigate(id))openLocationAfterTransition=false;}}
}
}
