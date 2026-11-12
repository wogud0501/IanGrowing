using System;using System.Linq;using System.Text;using TMPro;using UnityEngine;using UnityEngine.UI;
namespace IanGame {
public class FinancePresenter:MonoBehaviour {
 public bool marketScene;public UIShell shell;public GameObject marketPanel,economyPanel;public GameObject[] pages;
 public Button entry,economyEntry,marketClose,economyClose,buy,sell,payTax,repay;public Button[] tabs,stocks;
 public TMP_Text header,stockDetail,portfolio,news,history,economyText,feedback,economyFeedback;public TMP_InputField quantity,payment;
 public PixelStockChart chart;GameManager game;string selected;int page,lastOrderFrame=-1;float nextRefresh;
 void Start(){if(GameContext.Instance==null)return;game=GameContext.Instance.game;game.economy.Changed+=Refresh;if(entry!=null)entry.onClick.AddListener(()=>GameContext.Instance.transitions.Navigate(SceneId.StockMarket));economyEntry.onClick.AddListener(OpenEconomy);economyClose.onClick.AddListener(()=>shell.Close(economyPanel));payTax.onClick.AddListener(Pay);repay.onClick.AddListener(Repay);
  if(marketScene){selected=game.economy.rules.stocks[0].StockId;for(int i=0;i<tabs.Length;i++){int index=i;tabs[i].onClick.AddListener(()=>ShowTab(index));}for(int i=0;i<stocks.Length;i++){string id=game.economy.rules.stocks[i].StockId;stocks[i].onClick.AddListener(()=>{selected=id;Refresh();});}buy.onClick.AddListener(()=>Order(true));sell.onClick.AddListener(()=>Order(false));marketClose.onClick.AddListener(()=>{shell.Close(marketPanel);GameContext.Instance.transitions.Navigate(game.world.State.zone=="finance"?SceneId.LifeWorld:SceneId.City);});quantity.onValueChanged.AddListener(_=>Refresh());ShowTab(0);shell.Push(marketPanel);shell.Changed+=OnShellChanged;}
  Refresh();
 }
 void OnShellChanged(){if(marketScene&&shell.Depth==0&&GameContext.Instance!=null&&!GameContext.Instance.transitions.IsTransitioning)GameContext.Instance.transitions.Navigate(game.world.State.zone=="finance"?SceneId.LifeWorld:SceneId.City);}
 void OnDestroy(){if(shell!=null)shell.Changed-=OnShellChanged;if(game!=null)game.economy.Changed-=Refresh;}
 void Update(){if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+1;if(game!=null&&(marketScene||economyPanel.activeSelf))Refresh();}
 public void OpenEconomy(){Refresh();shell.Push(economyPanel);}
 public void ShowTab(int value){page=value;for(int i=0;i<pages.Length;i++)pages[i].SetActive(i==page);if(page==1&&game!=null)Durable(()=>{game.economy.ReviewInvestment();return true;},false);Refresh();UIShell.Focus(marketPanel);}
 bool Durable(Func<bool> command,bool announce=true){
  if(game.State!=GameState.Playing||game.BirthIntroductionPending||GameContext.Instance.transitions.IsTransitioning||game.idle.IsPersistenceSuspended){Message("현재는 거래할 수 없습니다.");return false;}
  if(announce&&lastOrderFrame==Time.frameCount)return false;if(announce)lastOrderFrame=Time.frameCount;
  var before=game.CaptureState();bool ok=command();if(!ok){Message("주문 실패: 잔액·수량·신용도·고지서를 확인하세요.");return false;}
  string error;if(!game.SaveAutomaticCheckpoint(out error)){game.RestoreState(before);Message("저장 실패로 거래를 취소했습니다. "+error);return false;}
  if(announce)Message("처리 완료. 거래 내역에 보관했습니다.");Refresh();return true;
 }
 void Message(string text){if(feedback!=null)feedback.text=text;if(economyFeedback!=null)economyFeedback.text=text;}
 void Order(bool purchase){int n;if(!int.TryParse(quantity.text,out n)||n<=0){Message("수량은 1 이상의 정수로 입력하세요.");return;}Durable(()=>game.economy.Trade(Guid.NewGuid().ToString("N"),selected,n,purchase));}
 void Pay(){long n;if(!long.TryParse(payment.text,out n)||n<=0){Message("납부 금액을 입력하세요.");return;}var bill=game.economy.Finance.bills.FirstOrDefault(b=>b.Remaining>0);if(bill==null){Message("미납 고지서가 없습니다.");return;}Durable(()=>game.economy.PayTax(Guid.NewGuid().ToString("N"),bill.id,n));}
 void Repay(){long n;if(!long.TryParse(payment.text,out n)||n<=0){Message("상환 금액을 입력하세요.");return;}Durable(()=>game.economy.RepayDebt(Guid.NewGuid().ToString("N"),n));}
 public void Refresh(){
  if(game==null||game.State==GameState.Starting)return;var e=game.economy;var f=e.Finance;if(f==null)return;
  var sb=new StringBuilder();sb.AppendLine($"현금 {e.Cash:N0}G    총자산 {e.TotalAssets:N0}G");sb.AppendLine($"부채 {e.Debt:N0}G    순자산 {e.NetWorth:N0}G");sb.AppendLine($"원금 {e.Principal:N0}G / 미상환 이자 {f.outstandingInterest:N0}G");sb.AppendLine($"누적 이자 {f.cumulativeInterest:N0}G / 신용도 {game.stats.Credit}");sb.AppendLine($"누적 수입 {f.totalIncome:N0}G / 소비·세금 지출 {f.totalExpenses:N0}G");sb.AppendLine($"다음 고지 예정 세금 {f.pendingTax:N0}G / 급여는 전액 수령 후 고지서로 납부");sb.AppendLine($"세금 {e.rules.taxPeriodDays}게임일 / 이자 {e.rules.debtPeriodDays}게임일마다, 회차당 원금의 {e.rules.debtInterestBasisPoints/100f:0.##}%");sb.AppendLine($"정상 상환 기준 {e.rules.minimumDebtPayment:N0}G/회차 · 매수 신용도 {e.rules.minimumTradingCredit} 이상");sb.AppendLine("\n세금 고지서 — 오래된 미납분부터 부분 납부 가능");
  foreach(var b in f.bills.AsEnumerable().Reverse().Take(12))sb.AppendLine($"{b.id}  기한 DAY {b.dueDay} {(b.Remaining==0?"납부 완료":game.clock.Day>b.dueDay?"체납":"미납")}\n세액 {b.amount:N0} / 연체료 {b.lateFees:N0} / 납부 {b.paid:N0} / 잔액 {b.Remaining:N0}G");
  sb.AppendLine("\n최근 상환·납부 기록");foreach(var t in f.transactions.Where(t=>t.kind==TransactionKind.DebtRepayment||t.kind==TransactionKind.Tax||t.kind==TransactionKind.DebtInterest).Reverse().Take(12))sb.AppendLine($"DAY {t.day} · {t.reason} {t.amount:N0}G");economyText.text=sb.ToString();
  if(!marketScene)return;
  header.text=$"이안 증권거래소  /  현금 {e.Cash:N0}G  /  평가액 {e.StockValue:N0}G\n{e.rules.marketIntervalSeconds}초 갱신 · {new DateTime(f.marketUtcTicks,DateTimeKind.Utc):MM-dd HH:mm} UTC";
  for(int i=0;i<stocks.Length;i++){var d=e.rules.stocks[i];var q=f.quotes.Find(x=>x.StockId==d.StockId);stocks[i].GetComponentInChildren<TMP_Text>().text=$"{d.CompanyName}\n<size=14>{q.CurrentPrice:N0}G  {(q.CurrentPrice/(double)q.PreviousPrice-1)*100:+0.00;-0.00;0.00}%</size>";}
  var def=Array.Find(e.rules.stocks,d=>d.StockId==selected);var quote=e.GetQuote(selected);var holding=e.GetHolding(selected);stockDetail.text=$"{def.CompanyName} · {def.Sector} · {quote.MarketStatus}\n현재가 {quote.CurrentPrice:N0}G / 기준가 {def.BasePrice:N0}G\n보유 {holding?.quantity??0}주 / 평균 {holding?.AveragePrice??0:N2}G";chart.SetPrices(quote.PriceHistory);
  int size;bool valid=int.TryParse(quantity.text,out size)&&size>0;long total=valid?(long)quote.CurrentPrice*size:long.MaxValue;buy.interactable=valid&&total<=e.Cash&&game.stats.Credit>=e.rules.minimumTradingCredit&&game.State==GameState.Playing;sell.interactable=valid&&size<=(holding?.quantity??0)&&total<=int.MaxValue-e.Cash&&game.State==GameState.Playing;
  sb.Clear();sb.AppendLine($"주식 평가액 {e.StockValue:N0}G\n평가 손익 {e.UnrealizedPnL:+0.00;-0.00;0.00}G\n실현 손익 {e.RealizedPnL:+0.00;-0.00;0.00}G\n총 투자 손익 {e.UnrealizedPnL+e.RealizedPnL:+0.00;-0.00;0.00}G\n");foreach(var h in f.holdings.Where(h=>h.quantity>0)){var q=e.GetQuote(h.stockId);sb.AppendLine($"{Array.Find(e.rules.stocks,d=>d.StockId==h.stockId)?.CompanyName}  {h.quantity:N0}주\n평균 {h.AveragePrice:N2}G / 평가 {(long)h.quantity*q.CurrentPrice:N0}G / 손익 {(long)h.quantity*q.CurrentPrice-h.costBasis:+0.00;-0.00;0.00}G\n");}portfolio.text=sb.ToString();
  news.text=f.news.Count==0?"아직 경제 뉴스가 없습니다.\n가상 시장의 뉴스는 가격에 일정 기간 반영됩니다.":string.Join("\n\n",f.news.AsEnumerable().Reverse().Take(30).Select(n=>$"{n.title}\n{n.description}\n{(n.expiresTick>f.marketSequence?"시장 반영 중":"반영 종료")} · {n.id}"));
  history.text=string.Join("\n\n",f.transactions.AsEnumerable().Reverse().Take(60).Select(t=>$"DAY {t.day} · {t.reason} {t.amount:N0}G\n{t.data}\n{t.id}"));
 }
}
}


