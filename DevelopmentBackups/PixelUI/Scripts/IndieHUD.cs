using System;using System.Linq;using System.Collections;using TMPro;using UnityEngine;using UnityEngine.UI;
namespace IanGame {
public class IndieHUD:MonoBehaviour {
 public UIManager hud;public MissionUI missions;public IndieUITheme theme;public TMP_Text ageText,cashText,debtText,heroTitle,heroCaption,locationText,statusDetails,noticeText;public Image missionFill;public GameObject notice;
 public TMP_Text[] statValues;public Image[] statFills;public Button[] lifeButtons;public int[] actionIds;public TMP_Text[] actionReasons;public UIShell shell;
 GameManager game;bool attached;int[] previous=new int[8];Coroutine[] statTweens=new Coroutine[8];long lastCash=-1;Coroutine cashTween,noticeTween;
 void Start(){Attach();Refresh();}void OnEnable(){Attach();}void OnDisable(){Detach();StopAllCoroutines();for(int i=0;i<8;i++)statTweens[i]=null;cashTween=noticeTween=null;}
 void Attach(){if(attached||hud==null||hud.game==null)return;game=hud.game;attached=true;hud.Refreshed+=Refresh;game.missions.Changed+=Refresh;game.missions.Completed+=OnComplete;game.missions.RewardClaimed+=OnReward;game.Logged+=OnLog;for(int i=0;i<8;i++)previous[i]=game.stats.Get((CoreStat)i);}
 void Detach(){if(!attached)return;attached=false;hud.Refreshed-=Refresh;if(game!=null){game.missions.Changed-=Refresh;game.missions.Completed-=OnComplete;game.missions.RewardClaimed-=OnReward;game.Logged-=OnLog;}}
 public void Refresh(){if(!attached){Attach();if(!attached)return;}var g=game;
 ageText.text=g.growth.Age+"세  /  DAY "+g.clock.Day.ToString("000");debtText.text="미상환 부채\n"+g.economy.Debt.ToString("N0")+" G";
 if(lastCash!=g.economy.Cash){if(cashTween!=null)StopCoroutine(cashTween);long from=lastCash<0?g.economy.Cash:lastCash;lastCash=g.economy.Cash;if(isActiveAndEnabled)cashTween=StartCoroutine(Cash(from,lastCash));else cashText.text="가용 자금\n"+lastCash.ToString("N0")+" G";}
 heroTitle.text="이안은\n오늘도 출근 중.";if(g.growth.Age<19)heroTitle.text="미래의 노동자,\n현재는 이안.";
 heroCaption.text=g.State==GameState.Paused?"일시정지 / 부채는 잠들지 않습니다.":g.State==GameState.GameOver?"생존 종료 / 저장된 인생은 보관됩니다.":g.stats.Mental<30?"주의: 멘탈 잔액이 부족합니다.":"인생은 진행 중. 행복은 별도 구매입니다.";
 locationText.text=GameContext.Instance==null?"HOME":GameContext.Instance.transitions.CurrentScene.ToString().ToUpperInvariant();
 string[] labels={"허기","목마름","지식","체력","멘탈","사회성","신용도","행복도"};
 for(int i=0;i<8;i++){int value=g.stats.Get((CoreStat)i);statValues[i].text=value+"<size=65%> /100</size>";if(hud.coreLabels!=null&&i<hud.coreLabels.Length)hud.coreLabels[i].text="<size=70%>"+(i+1).ToString("00")+"</size>  "+labels[i];if(previous[i]!=value){if(statTweens[i]!=null)StopCoroutine(statTweens[i]);if(isActiveAndEnabled)statTweens[i]=StartCoroutine(Stat(i,previous[i],value));}else statFills[i].rectTransform.anchorMax=new Vector2(value/100f,1);previous[i]=value;}
 if(hud.logText!=null&&g.RecentLogs.Length>0)hud.logText.text=g.RecentLogs.Last();var m=g.missions.ActiveMission;if(m!=null){var p=g.missions.GetProgress(m);missionFill.fillAmount=(float)(p.current/Math.Max(1,p.target));}
 for(int i=0;i<lifeButtons.Length;i++){string reason;bool can=g.CanPerformAction(actionIds[i],out reason);lifeButtons[i].interactable=can;var spec=g.actions.Describe(actionIds[i]);var label=lifeButtons[i].GetComponentInChildren<TMP_Text>();label.text=MissionGuidance.ActionName(actionIds[i],g);if(actionReasons!=null&&i<actionReasons.Length)actionReasons[i].text=can?spec.days+"일 · "+(spec.money<0?(-(long)spec.money)+"G":"무료"):reason;}
 if(statusDetails!=null)statusDetails.text=string.Join("\n",Enumerable.Range(0,8).Select(i=>labels[i]+"   "+g.stats.Get((CoreStat)i)+" / 100"))+"\n\n"+g.growth.Age+"세 / Lv."+g.growth.Level+" / EXP "+g.growth.Experience+"\n現 순자산 "+g.economy.NetWorth.ToString("N0")+"G\n"+(g.survival.InDanger?"생존 위험: 돌봄 행동이 필요합니다.":"생존 상태: 안정");
 }
 IEnumerator Cash(long a,long b){if(UIShell.ReducedMotion){cashText.text="가용 자금\n"+b.ToString("N0")+" G";yield break;}for(float t=0;t<1;t+=Time.unscaledDeltaTime/.22f){cashText.text="가용 자금\n"+((long)Math.Round(a+(b-a)*(1-Math.Pow(1-t,3)))).ToString("N0")+" G";yield return null;}cashText.text="가용 자금\n"+b.ToString("N0")+" G";}
 IEnumerator Stat(int index,int a,int b){var image=statFills[index];Color original=image.color;float duration=UIShell.ReducedMotion?.001f:.2f;for(float t=0;t<1;t+=Time.unscaledDeltaTime/duration){image.rectTransform.anchorMax=new Vector2(Mathf.Lerp(a,b,t)/100f,1);image.color=Color.Lerp(theme.white,original,t);yield return null;}image.rectTransform.anchorMax=new Vector2(b/100f,1);image.color=original;}
 void OnComplete(MissionData m){Show("MISSION APPROVED / 미션 승인\n"+m.Id+"  "+m.title+"\n보상을 수령해 다음 단계로 진행하세요.");}void OnReward(MissionData m){Show("입금 확인  +"+m.rewards.gold.ToString("N0")+" G\n"+m.title+" · EXP +"+m.rewards.experience);}
 void OnLog(string text){if(text.Contains("실패")||text.Contains("부족")||text.Contains("거절")||text.Contains("위험"))Show("시스템 통보\n"+text);}
 void Show(string text){if(notice==null)return;noticeText.text=text;if(noticeTween!=null)StopCoroutine(noticeTween);notice.SetActive(true);noticeTween=StartCoroutine(Hide());}
 IEnumerator Hide(){yield return new WaitForSecondsRealtime(3.5f);notice.SetActive(false);}
}
}
