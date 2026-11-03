using System.Collections.Generic;using TMPro;using UnityEngine;using UnityEngine.UI;
namespace IanGame {
public class UIManager:MonoBehaviour {
 public GameManager game;public TMP_Text header,happinessLabel,stressLabel,energyLabel,logText,moodText,stateLabel;
 public Image happinessFill,stressFill,energyFill;public Button workButton,restButton,playButton,saveButton,loadButton,pauseButton,newGameButton;
 readonly Queue<string> logs=new Queue<string>();bool subscribed;
 public void Bind(GameManager model){Detach();game=model;if(isActiveAndEnabled)Attach();Refresh();}
 void OnEnable(){Attach();Wire(true);Refresh();}
 void Start(){Refresh();}
 void OnDisable(){Detach();Wire(false);}
 void Attach(){if(subscribed||game==null||game.stats==null||game.clock==null)return;game.stats.Changed+=Refresh;game.clock.DayChanged+=OnDay;game.Logged+=Append;game.StateChanged+=OnState;subscribed=true;}
 void Detach(){if(!subscribed)return;if(game!=null){if(game.stats!=null)game.stats.Changed-=Refresh;if(game.clock!=null)game.clock.DayChanged-=OnDay;game.Logged-=Append;game.StateChanged-=OnState;}subscribed=false;}
 void Wire(bool add){Hook(workButton,Work,add);Hook(restButton,Rest,add);Hook(playButton,Play,add);Hook(saveButton,Save,add);Hook(loadButton,Load,add);Hook(pauseButton,Pause,add);Hook(newGameButton,NewGame,add);}
 void Hook(Button b,UnityEngine.Events.UnityAction action,bool add){if(b==null)return;b.onClick.RemoveListener(action);if(add&&b.onClick.GetPersistentEventCount()==0)b.onClick.AddListener(action);}
 public void Work(){if(game!=null)game.Work();}public void Rest(){if(game!=null)game.Rest();}public void Play(){if(game!=null)game.Play();}
 public void Save(){if(game!=null)game.Save();}public void Load(){if(game!=null)game.Load();}public void Pause(){if(game!=null)game.TogglePause();}public void NewGame(){if(game!=null)game.NewGame();}
 void OnDay(int day){Refresh();}void OnState(GameState state){Refresh();}
 public void Refresh(){
 if(game==null||game.stats==null||game.clock==null||game.balance==null)return;var s=game.stats;
 if(header!=null)header.text="DAY "+game.clock.Day.ToString("000")+"     /     잔액  ₩ "+s.Money.ToString("N0");
 Gauge(happinessLabel,happinessFill,"행복",s.Happiness);Gauge(stressLabel,stressFill,"스트레스",s.Stress);Gauge(energyLabel,energyFill,"체력",s.Energy);
 if(moodText!=null)moodText.text=game.State==GameState.GameOver?"번아웃. 새 인생을 시작해 주세요.":game.State==GameState.Paused?"잠시 멈췄습니다. 이안도 숨을 쉽니다.":s.Stress>=75?"웃고 있습니다. 아마도.":s.Energy<20?"절전 모드의 이안":s.Happiness>=70?"오늘은 제법 사람답습니다.":"이안은 적응 중입니다.";
 if(stateLabel!=null)stateLabel.text=game.State==GameState.Paused?"●  일시정지":game.State==GameState.GameOver?"●  게임오버":game.State==GameState.Starting?"●  준비 중":"●  진행 중";
 string reason;
 if(workButton!=null)workButton.interactable=game.CanPerformAction(0,out reason);
 if(restButton!=null)restButton.interactable=game.CanPerformAction(1,out reason);
 if(playButton!=null)playButton.interactable=game.CanPerformAction(2,out reason);
 if(pauseButton!=null){pauseButton.interactable=game.State==GameState.Playing||game.State==GameState.Paused;Label(pauseButton,game.State==GameState.Paused?"계속":"일시정지");}
 bool stable=game.State!=GameState.Starting&&game.State!=GameState.Resolving;
 if(saveButton!=null)saveButton.interactable=stable;if(loadButton!=null)loadButton.interactable=stable;if(newGameButton!=null)newGameButton.interactable=stable;
 ActionLabel(workButton,"일하기",0);ActionLabel(restButton,"쉬기",1);ActionLabel(playButton,"놀기",2);
 }
 void ActionLabel(Button b,string title,int action){if(b==null||game.actions==null)return;var d=game.actions.Describe(action);string money=d.money==0?"무료":d.money>0?"+₩"+d.money:"-₩"+(-(long)d.money);Label(b,title+"    "+money+" / "+d.days+"일");}
 void Label(Button b,string text){var label=b.GetComponentInChildren<TMP_Text>();if(label!=null)label.text=text;}
 void Gauge(TMP_Text label,Image fill,string name,int value){if(label!=null)label.text=name+"   "+value+" / 100";if(fill!=null)fill.rectTransform.anchorMax=new Vector2(value/100f,1);}
 public void Append(string value){logs.Enqueue(value);while(logs.Count>4)logs.Dequeue();if(logText!=null)logText.text=string.Join("\n\n",logs.ToArray());Refresh();}
}
}