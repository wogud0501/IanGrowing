using System.Collections.Generic;using TMPro;using UnityEngine;using UnityEngine.UI;
namespace IanGame {
public class UIManager:MonoBehaviour {
 public GameManager game;public TMP_Text header,happinessLabel,stressLabel,energyLabel,logText,moodText;
 public Image happinessFill,stressFill,energyFill;public Button workButton,restButton,playButton,saveButton,loadButton;
 readonly Queue<string> logs=new Queue<string>();bool subscribed;
 public void Bind(GameManager model){Detach();game=model;if(isActiveAndEnabled)Attach();Refresh();}
 void OnEnable(){Attach();Wire(true);Refresh();}
 void Start(){Append("DAY 1  이안이 입주했습니다. 행복은 별도 구매입니다.");Refresh();}
 void OnDisable(){Detach();Wire(false);}
 void Attach(){if(subscribed||game==null||game.stats==null||game.clock==null)return;game.stats.Changed+=Refresh;game.clock.DayChanged+=OnDay;game.Logged+=Append;subscribed=true;}
 void Detach(){if(!subscribed)return;if(game!=null){if(game.stats!=null)game.stats.Changed-=Refresh;if(game.clock!=null)game.clock.DayChanged-=OnDay;game.Logged-=Append;}subscribed=false;}
 void Wire(bool add){Hook(workButton,Work,add);Hook(restButton,Rest,add);Hook(playButton,Play,add);Hook(saveButton,Save,add);Hook(loadButton,Load,add);}
 void Hook(Button b,UnityEngine.Events.UnityAction action,bool add){if(b==null)return;b.onClick.RemoveListener(action);if(add&&b.onClick.GetPersistentEventCount()==0)b.onClick.AddListener(action);}
 public void Work(){if(game!=null)game.Work();}public void Rest(){if(game!=null)game.Rest();}public void Play(){if(game!=null)game.Play();}public void Save(){if(game!=null)game.Save();}public void Load(){if(game!=null)game.Load();}
 void OnDay(int day){Refresh();}
 public void Refresh(){
 if(game==null||game.stats==null||game.clock==null||game.balance==null)return;var s=game.stats;
 if(header!=null)header.text="DAY "+game.clock.Day.ToString("000")+"     /     잔액  ₩ "+s.Money.ToString("00");
 Gauge(happinessLabel,happinessFill,"행복",s.Happiness);Gauge(stressLabel,stressFill,"스트레스",s.Stress);Gauge(energyLabel,energyFill,"체력",s.Energy);
 if(moodText!=null)moodText.text=s.Stress>=75?"웃고 있습니다. 아마도.":s.Energy<20?"절전 모드의 이안":s.Happiness>=70?"오늘은 제법 사람답습니다.":"이안은 적응 중입니다.";
 if(workButton!=null)workButton.interactable=s.Energy>=Mathf.Max(0,-game.balance.workEnergy)&&s.Money>=Mathf.Max(0,-game.balance.workMoney);
 if(restButton!=null)restButton.interactable=true;
 if(playButton!=null)playButton.interactable=s.Money>=Mathf.Max(0,-game.balance.playMoney);
 }
 void Gauge(TMP_Text label,Image fill,string name,int value){if(label!=null)label.text=name+"   "+value+" / 100";if(fill!=null)fill.rectTransform.anchorMax=new Vector2(value/100f,1);}
 public void Append(string value){logs.Enqueue(value);while(logs.Count>4)logs.Dequeue();if(logText!=null)logText.text=string.Join("\n\n",logs.ToArray());Refresh();}
}
}