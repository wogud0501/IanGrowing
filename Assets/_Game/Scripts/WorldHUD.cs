using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace IanGame {
public sealed class WorldHUD:MonoBehaviour {
 public LifeWorldController world;public UIShell shell;public PolishedNavigation navigation;public HomeManagementPresenter home;
 public TMP_Text cash,needs,prompt,feedback,progress,contextTitle,inventory;public GameObject promptPanel,menuPanel,contextPanel;public RectTransform choices;public Button menuButton,contextClose;public Button[] menus;public Image fade;public Button choicePrefab;
 readonly List<Button> buttons=new List<Button>();WorldInteractable source;int notificationPriority;float until;string recent="";GameManager game;bool dirty;
 void Start(){if(GameContext.Instance==null)return;game=GameContext.Instance.game;menuButton.onClick.AddListener(()=>{if(world.CanControl)shell.Push(menuPanel);});contextClose.onClick.AddListener(CloseContext);for(int i=0;i<menus.Length;i++){int n=i;menus[i].onClick.AddListener(()=>navigation.Open(n));}game.economy.TransactionCompleted+=Money;game.world.Changed+=Summary;game.economy.Changed+=Invalidate;game.stats.Changed+=Invalidate;game.world.Restored+=Invalidate;game.clock.SimulationAdvanced+=TimeChanged;game.Logged+=Log;shell.Changed+=ShellChanged;Refresh();}
 void ShellChanged(){if(!world.Busy&&!game.BirthIntroductionPending)world.Player.Pose(shell.Depth>0?"Use Phone":"Idle");}
 void Invalidate(){dirty=true;}void TimeChanged(double seconds){dirty=true;}
 void LateUpdate(){if(dirty&&game!=null){dirty=false;Refresh();}}
 void OnDestroy(){if(game!=null){game.economy.TransactionCompleted-=Money;game.world.Changed-=Summary;game.economy.Changed-=Invalidate;game.stats.Changed-=Invalidate;game.world.Restored-=Invalidate;game.clock.SimulationAdvanced-=TimeChanged;game.Logged-=Log;shell.Changed-=ShellChanged;}}
 void Update(){if(game==null)return;menuButton.interactable=!world.Busy&&!game.BirthIntroductionPending&&!GameContext.Instance.transitions.IsTransitioning;if(world.CanControl){if(world.Player.MenuPressed)shell.Push(menuPanel);else if(world.Player.InventoryPressed){var nav=GetComponent<UINavigationService>();if(nav!=null)nav.Inventory();}}if(feedback!=null&&Time.unscaledTime>until)feedback.text="";}
 void Refresh(){cash.text=$"{game.economy.Cash:N0} G";needs.text=$"체력 {game.stats.Health}  ·  허기 {game.stats.Hunger}  ·  피로 {game.world.State.fatigue:0}";if(inventory!=null)inventory.text=$"식료품 · 쌀 {game.world.State.rice} / 빵 {game.world.State.bread}\n준비한 음식 · {(game.world.State.prepared==""?"없음":game.world.State.prepared=="rice"?"따뜻한 밥":"빵")}\n위생 {game.world.State.hygiene:0} /100 · 피로 {game.world.State.fatigue:0} /100\n{game.world.State.lastSummary}";}
 public void Prompt(string text){promptPanel.SetActive(!string.IsNullOrEmpty(text));prompt.text=text??"";if(string.IsNullOrEmpty(text))return;var rect=(RectTransform)promptPanel.transform;float available=((RectTransform)rect.parent).rect.width*.7f;float width=Mathf.Min(available,Mathf.Max(220,prompt.GetPreferredValues(text).x+40));var mobile=GetComponent<WorldMobileHUD>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,mobile!=null&&mobile.IsMobile?(mobile.Profile==WorldScreenProfile.PhonePortrait?.33f:.37f):.09f);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(width,Mathf.Max(48,prompt.GetPreferredValues(text,width-32,Mathf.Infinity).y+16));}
 public void Notify(string text,int priority=0){if(Time.unscaledTime<until&&priority<notificationPriority)return;notificationPriority=priority;feedback.text=text??"";float height=feedback.GetPreferredValues(feedback.text,Mathf.Max(1,feedback.rectTransform.rect.width-28),Mathf.Infinity).y+12;feedback.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(64,height));until=Time.unscaledTime+4;}
 void Money(int before,int after,string reason){long delta=(long)after-before;if(delta==0)return;world.Player.React(delta>0?ActorMotion.Happy:ActorMotion.Thinking);Notify($"{reason}  {delta:+#,0;-#,0;0}G",1);}
 void Log(string text){if(text==recent)return;recent=text;if(text.Contains("실패")||text.Contains("부족")||text.Contains("채용"))Notify(text,2);}
 void Summary(){if(!string.IsNullOrEmpty(game.world.State.lastSummary))Notify(game.world.State.lastSummary);Refresh();}
 public void Dim(float alpha){fade.color=new Color(0,0,0,Mathf.Clamp01(alpha));fade.raycastTarget=alpha>.01f;}
 public void Activity(string text,float amount){progress.text=string.IsNullOrEmpty(text)?"":$"{text} · {Mathf.RoundToInt(amount*100)}%   [Esc] 취소";}
 public void OpenHousing(){home.Tab(1);shell.Push(home.panel);}
 public void CloseContext(){source=null;if(shell.Top==contextPanel)shell.Close(contextPanel);}
 void Choice(string label,Action command){var b=Instantiate(choicePrefab,choices);b.gameObject.SetActive(true);b.GetComponentInChildren<TMP_Text>().text=label;b.onClick.RemoveAllListeners();b.onClick.AddListener(()=>{if(source==null||!world.IsInReach(source)||world.Busy){Notify("대상 가까이에서 다시 시도하세요.");CloseContext();return;}command();Refresh();});buttons.Add(b);}
 public void OpenContext(WorldInteractable target){source=target;foreach(var b in buttons)Destroy(b.gameObject);buttons.Clear();var s=game.world.State;contextTitle.text=target.title+"  /  "+(target.kind==WorldInteractionKind.Work?game.phase23.Jobs.Current?.DisplayName:target.kind==WorldInteractionKind.Fridge?$"쌀 {s.rice} · 빵 {s.bread}":"");
  if(target.kind==WorldInteractionKind.Fridge){Choice("밥 준비 · 쌀 1개 · 20분",()=>world.Begin(target,-1,"rice"));Choice("빵 준비 · 빵 1개 · 5분",()=>world.Begin(target,-1,"bread"));}
  else if(target.kind==WorldInteractionKind.FoodShop){Choice($"쌀 구매 · {game.world.rules.ricePrice:N0}G",()=>BuyFood("rice"));Choice($"빵 구매 · {game.world.rules.breadPrice:N0}G",()=>BuyFood("bread"));Choice($"가게 정리 돕기 · {game.world.rules.neighbourhoodHelpMinutes}분 · {game.world.rules.neighbourhoodHelpReward}G",()=>world.Begin(target,-3));Choice("바로 식사 · 빵 1개 · 30분",()=>world.Begin(target,3,"bread"));Choice("가구점 · 가격과 보유 목록",()=>{CloseContext();OpenHousing();});}
  else if(target.kind==WorldInteractionKind.Work){foreach(int a in new[]{0,20,21,22,23})ActionChoice(target,a);Choice("지원 직업 변경",()=>{var jobs=Array.FindAll(game.phase23.catalog.jobs,j=>!j.specialUnlock&&j.BaseDailyWage>0);int i=Array.FindIndex(jobs,j=>j.JobId==game.phase23.Progress.employment.selectedJobId);game.phase23.Progress.employment.selectedJobId=jobs[(i+1)%jobs.Length].JobId;Notify("지원 직업 · "+jobs[(i+1)%jobs.Length].DisplayName);});}
  else if(target.kind==WorldInteractionKind.Study){foreach(int a in new[]{7,8,10,11,12,13,14,15,16,17})ActionChoice(target,a);Choice("진로 변경 · 직업 / 진학",()=>{game.phase23.SelectedCareerId=game.phase23.SelectedCareerId=="vocational"?"academic":"vocational";Notify("선택 진로 · "+game.phase23.SelectedCareerId);});}
  else if(target.kind==WorldInteractionKind.NPC){game.phase23.SelectedNpcId=target.npcId;foreach(int a in new[]{18,9,19})ActionChoice(target,a);}
  shell.Push(contextPanel);UIShell.Focus(contextPanel);
 }
 void ActionChoice(WorldInteractable target,int action){var data=game.phase23.catalog.Find(action);string label=action==0?$"근무 · {game.world.rules.shiftHours}시간 · 예상 {game.phase23.Work.Wage:N0}G":data==null?"활동 "+action:data.displayName;Choice(label,()=>world.Begin(target,action));}
 void BuyFood(string id){if(game.world.Hour<game.world.rules.shopOpen||game.world.Hour>=game.world.rules.shopClose){Notify("지금은 영업 시간이 아닙니다.");return;}string message;Notify(game.world.PurchaseFood(id,out message)?$"식료품 구매 완료 · {(id=="rice"?game.world.rules.ricePrice:game.world.rules.breadPrice):N0}G 지출":message,1);}
}
}
