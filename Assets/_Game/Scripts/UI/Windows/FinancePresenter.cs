using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public class FinancePresenter : MonoBehaviour
    {
        public bool marketScene;
        public UIShell shell;
        public GameObject marketPanel;
        public GameObject economyPanel;
        public GameObject[] pages;
        public Button entry;
        public Button economyEntry;
        public Button marketClose;
        public Button economyClose;
        public Button buy;
        public Button sell;
        public Button payTax;
        public Button repay;
        public Button[] tabs;
        public Button[] stocks;
        public TMP_Text header;
        public TMP_Text stockDetail;
        public TMP_Text portfolio;
        public TMP_Text news;
        public TMP_Text history;
        public TMP_Text economyText;
        public TMP_Text feedback;
        public TMP_Text economyFeedback;
        public TMP_InputField quantity;
        public TMP_InputField payment;
        public PixelStockChart chart;
        GameManager game;
        string selected;
        int page;
        int lastOrderFrame = -1;
        TMP_Text firstQuote;
        TMP_Text cashBalance;
        TMP_Text netBalance;
        TMP_Text debtBalance;

        void BuildStatement()
        {
            var scroll = economyText.GetComponentInParent<ScrollRect>(true);
            if (scroll == null)
            {
                return;
            }

            var frame = (RectTransform)scroll.transform.parent;
            frame.gameObject.AddComponent<RecordWindowBounds>();
            LifeRecordUI.Skin(frame.GetComponent<Image>(), RecordSurface.Window);
            var mast = LifeRecordUI.Surface(frame, "Statement header", RecordSurface.Header, .01f, .86f, .99f, .986f);
            mast.transform.SetAsFirstSibling();
            var title = frame.Find("Title").GetComponent<TMP_Text>();
            title.text = "가계 명세서";
            MissionRecordLayout.SetText(title, 30, LifeRecordUI.Heading);
            var left = LifeRecordUI.Surface(frame, "Account balance", RecordSurface.Focus, .024f, .272f, .358f, .83f).transform;
            LifeRecordUI.Label(left, title.font, "사용 가능 현금", 20, LifeRecordUI.Secondary, .07f, .82f, .96f, .96f);
            cashBalance = LifeRecordUI.Label(left, title.font, "", 35, LifeRecordUI.Body, .07f, .60f, .96f, .83f);
            LifeRecordUI.Print(left, RecordPrint.Finance, .07f, .045f, .94f, .57f);
            var debts = LifeRecordUI.Surface(frame, "Liabilities", RecordSurface.Inset, .377f, .685f, .973f, .83f).transform;
            LifeRecordUI.Label(debts, title.font, "남은 부채", 18, LifeRecordUI.Secondary, .035f, .56f, .51f, .97f);
            debtBalance = LifeRecordUI.Label(debts, title.font, "", 27, LifeRecordUI.Heading, .035f, .03f, .52f, .59f);
            LifeRecordUI.Label(debts, title.font, "순자산", 18, LifeRecordUI.Secondary, .53f, .56f, .99f, .97f);
            netBalance = LifeRecordUI.Label(debts, title.font, "", 27, LifeRecordUI.Heading, .53f, .03f, .99f, .59f);
            LifeRecordUI.Place((RectTransform)scroll.transform, .385f, .28f, .96f, .663f);
            LifeRecordUI.StyleButton(payTax);
            LifeRecordUI.StyleButton(repay, true);
            LifeRecordUI.StyleButton(economyClose);
            LifeRecordUI.Skin(payment.GetComponent<Image>(), RecordSurface.Inset);
        }

        void Start()
        {
            if (GameContext.Instance == null)
            {
                return;
            }

            game = GameContext.Instance.game;
            if (economyText != null)
            {
                BuildStatement();
            }

            game.economy.Changed += Refresh;
            game.stats.CreditChanged += CreditOrDayChanged;
            game.clock.DayChanged += CreditOrDayChanged;
            game.growth.Changed += Refresh;
            game.StateChanged += StateChanged;
            if (entry != null)
            {
                entry.onClick.AddListener(() => GameContext.Instance.transitions.Navigate(SceneId.StockMarket));
            }

            economyEntry.onClick.AddListener(OpenEconomy);
            economyClose.onClick.AddListener(() => shell.Close(economyPanel));
            payTax.onClick.AddListener(Pay);
            repay.onClick.AddListener(Repay);
            if (marketScene)
            {
                if (GrowthAccess.Adult(game))
                {
                    game.world?.City?.Learn(GuideLesson.Stocks);
                }

                chart.color = LifeRecordUI.Heading;
                firstQuote = LifeRecordUI.Label(chart.transform, header.font, "", 18, LifeRecordUI.Body,
                    .055f, .54f, .96f, .87f);
                LifeRecordUI.Skin(quantity.GetComponent<Image>(), RecordSurface.Inset);
                selected = game.economy.rules.stocks[0].StockId;
                for (int i = 0; i < tabs.Length; i++)
                {
                    int index = i;
                    tabs[i].onClick.AddListener(() => ShowTab(index));
                }

                for (int i = 0; i < stocks.Length; i++)
                {
                    string id = game.economy.rules.stocks[i].StockId;
                    stocks[i]
                        .onClick.AddListener(() =>
                        {
                            selected = id;
                            Refresh();
                        });
                }

                buy.onClick.AddListener(() => Order(true));
                sell.onClick.AddListener(() => Order(false));
                marketClose.onClick.AddListener(() =>
                {
                    shell.Close(marketPanel);
                    GameContext.Instance.transitions.Navigate(
                        game.world.State.zone == "finance" ? SceneId.LifeWorld : SceneId.City
                    );
                });
                quantity.onValueChanged.AddListener(_ => Refresh());
                ShowTab(0);
                shell.Push(marketPanel);
                shell.Changed += OnShellChanged;
            }

            Refresh();
        }

        void OnShellChanged()
        {
            if (
                marketScene
                && shell.Depth == 0
                && GameContext.Instance != null
                && !GameContext.Instance.transitions.IsTransitioning
            )
            {
                GameContext.Instance.transitions.Navigate(
                    game.world.State.zone == "finance" ? SceneId.LifeWorld : SceneId.City
                );
            }
        }

        void OnDestroy()
        {
            if (shell != null)
            {
                shell.Changed -= OnShellChanged;
            }

            if (game != null)
            {
                game.economy.Changed -= Refresh;
                game.stats.CreditChanged -= CreditOrDayChanged;
                game.clock.DayChanged -= CreditOrDayChanged;
                game.growth.Changed -= Refresh;
                game.StateChanged -= StateChanged;
            }
        }

        void CreditOrDayChanged(int value)
        {
            Refresh();
        }

        void StateChanged(GameState state)
        {
            Refresh();
        }

        public void OpenEconomy()
        {
            shell.Push(economyPanel);
            Refresh();
            var city = game.world?.City;
            if (city == null || !GrowthAccess.Adult(game))
            {
                return;
            }

            if (game.economy.Debt > 0 && economyFeedback && city.Progress(GuideLesson.Debt) == LessonState.NotStarted)
            {
                city.StartLesson(GuideLesson.Debt);
                economyFeedback.text = "남은 부채와 이자를 확인하세요. 여유가 생기면 금액을 입력해 조금씩 상환할 수 있어요.";
                city.Learn(GuideLesson.Debt);
            }

            if (city.Progress(GuideLesson.Salary) == LessonState.Active)
            {
                city.Learn(GuideLesson.Salary);
            }

            if (city.Progress(GuideLesson.Tax) == LessonState.Active)
            {
                city.Learn(GuideLesson.Tax);
            }
        }

        public void ShowTab(int value)
        {
            page = value;
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(i == page);
                LifeRecordUI.StyleButton(tabs[i], i == page);
            }

            if (page == 1 && game != null)
            {
                Durable(
                    () =>
                    {
                        game.economy.ReviewInvestment();
                        return true;
                    },
                    false
                );
            }

            Refresh();
            UIShell.Focus(marketPanel);
        }

        bool Durable(Func<bool> command, bool announce = true)
        {
            if (
                game.State != GameState.Playing
                || game.BirthIntroductionPending
                || GameContext.Instance.transitions.IsTransitioning
                || game.idle.IsPersistenceSuspended
            )
            {
                Message("현재는 거래할 수 없습니다.");
                return false;
            }

            if (announce && lastOrderFrame == Time.frameCount)
            {
                return false;
            }

            if (announce)
            {
                lastOrderFrame = Time.frameCount;
            }

            var before = game.CaptureState();
            bool ok = command();
            if (!ok)
            {
                Message("주문 실패: 잔액·수량·신용도·고지서를 확인하세요.");
                return false;
            }

            string error;
            if (!game.SaveAutomaticCheckpoint(out error))
            {
                game.RestoreState(before);
                Message("저장 실패로 거래를 취소했습니다. " + error);
                return false;
            }

            if (announce)
            {
                Message("처리 완료. 거래 내역에 보관했습니다.");
            }

            Refresh();
            return true;
        }

        void Message(string text)
        {
            if (feedback != null)
            {
                feedback.text = text;
            }

            if (economyFeedback != null)
            {
                economyFeedback.text = text;
            }
        }

        void Order(bool purchase)
        {
            if (!GrowthAccess.Adult(game))
            {
                Message("주식 거래는 청년기(19세)부터 해금됩니다.");
                return;
            }

            int n;
            if (!int.TryParse(quantity.text, out n) || n <= 0)
            {
                Message("수량은 1 이상의 정수로 입력하세요.");
                return;
            }

            Durable(() => game.economy.Trade(Guid.NewGuid().ToString("N"), selected, n, purchase));
        }

        void Pay()
        {
            if (!GrowthAccess.Adult(game))
            {
                Message("금융 활동은 19세부터 가능합니다.");
                return;
            }

            long n;
            if (!long.TryParse(payment.text, out n) || n <= 0)
            {
                Message("납부 금액을 입력하세요.");
                return;
            }

            var bill = game.economy.Finance.bills.FirstOrDefault(b => b.Remaining > 0);
            if (bill == null)
            {
                Message("미납 고지서가 없습니다.");
                return;
            }

            Durable(() => game.economy.PayTax(Guid.NewGuid().ToString("N"), bill.id, n));
        }

        void Repay()
        {
            if (!GrowthAccess.Adult(game))
            {
                Message("금융 활동은 19세부터 가능합니다.");
                return;
            }

            long n;
            if (!long.TryParse(payment.text, out n) || n <= 0)
            {
                Message("상환 금액을 입력하세요.");
                return;
            }

            Durable(() => game.economy.RepayDebt(Guid.NewGuid().ToString("N"), n));
        }

        public void Refresh()
        {
            if (game == null || game.State == GameState.Starting || (!marketScene && !economyPanel.activeInHierarchy))
            {
                return;
            }

            var e = game.economy;
            var f = e.Finance;
            if (f == null)
            {
                return;
            }

            var sb = new StringBuilder();
            if (cashBalance != null)
            {
                cashBalance.text = $"{e.Cash:N0} G";
                debtBalance.text = $"{e.Debt:N0} G";
                netBalance.text = $"{e.NetWorth:N0} G";
            }

            sb.AppendLine($"총자산   {e.TotalAssets:N0} G");
            sb.AppendLine($"누적 수입   {f.totalIncome:N0} G     소비·세금 지출   {f.totalExpenses:N0} G");
            sb.AppendLine();
            sb.AppendLine("세금 고지서");
            sb.AppendLine($"다음 고지 예정액   {f.pendingTax:N0} G");
            if (f.bills.Count == 0)
            {
                sb.AppendLine("발행된 고지서가 없습니다.");
            }

            foreach (var bill in f.bills.AsEnumerable().Reverse().Take(12))
            {
                string status = bill.Remaining == 0 ? "납부 완료" : game.clock.Day > bill.dueDay ? "체납" : "미납";
                sb.AppendLine($"[{status}]   납부 기한 {bill.dueDay}일차   ·   잔액 {bill.Remaining:N0} G");
                sb.AppendLine($"발행 {bill.issuedDay}일차   ·   세액 {bill.amount:N0} G   ·   연체료 {bill.lateFees:N0} G   ·   납부 {bill.paid:N0} G");
            }

            sb.AppendLine();
            sb.AppendLine("부채 내역");
            sb.AppendLine($"원금   {e.Principal:N0} G     미상환 이자   {f.outstandingInterest:N0} G");
            sb.AppendLine($"누적 이자   {f.cumulativeInterest:N0} G     신용도   {game.stats.Credit}");
            sb.AppendLine();
            sb.AppendLine("최근 상환·납부 기록");
            int displayed = 0;
            foreach (
                var transaction in f.transactions.Where(t =>
                    t.kind == TransactionKind.DebtRepayment
                    || t.kind == TransactionKind.Tax
                    || t.kind == TransactionKind.DebtInterest
                ).Reverse().Take(12)
            )
            {
                sb.AppendLine($"{transaction.day}일차   ·   {transaction.reason}   {transaction.amount:N0} G");
                displayed++;
            }

            if (displayed == 0)
            {
                sb.AppendLine("기록된 상환·납부 내역이 없습니다.");
            }

            sb.AppendLine();
            sb.AppendLine("납부 안내");
            sb.AppendLine($"세금은 {e.rules.taxPeriodDays}게임일마다 고지합니다. 급여는 전액 입금되며, 오래된 미납분부터 부분 납부할 수 있습니다.");
            sb.AppendLine($"이자는 {e.rules.debtPeriodDays}게임일마다 원금의 {e.rules.debtInterestBasisPoints / 100f:0.##}%입니다.");
            sb.AppendLine($"정상 상환 기준   회차당 {e.rules.minimumDebtPayment:N0} G   ·   주식 매수 신용도   {e.rules.minimumTradingCredit} 이상");
            economyText.text = sb.ToString();
            bool adult = GrowthAccess.Adult(game);
            payTax.interactable = repay.interactable = adult && game.State == GameState.Playing;
            const string restricted = "금융 활동은 청년기(19세)부터 해금됩니다.";
            if (adult && economyFeedback != null && economyFeedback.text == restricted)
            {
                economyFeedback.text = "";
            }

            if (!adult && economyFeedback != null)
            {
                economyFeedback.text = "금융 활동은 청년기(19세)부터 해금됩니다.";
            }

            if (!marketScene)
            {
                return;
            }

            header.text =
                $"이안 증권거래소  /  현금 {e.Cash:N0}G  /  평가액 {e.StockValue:N0}G\n{e.rules.marketIntervalSeconds}초 갱신 · {new DateTime(f.marketUtcTicks, DateTimeKind.Utc):MM-dd HH:mm} UTC";
            for (int i = 0; i < stocks.Length; i++)
            {
                var d = e.rules.stocks[i];
                var q = f.quotes.Find(x => x.StockId == d.StockId);
                LifeRecordUI.StyleButton(stocks[i], d.StockId == selected);
                stocks[i].GetComponentInChildren<TMP_Text>().text =
                    $"{d.CompanyName}\n<size=18>{q.CurrentPrice:N0}G  {(q.CurrentPrice / (double)q.PreviousPrice - 1) * 100:+0.00;-0.00;0.00}%</size>";
            }

            var def = Array.Find(e.rules.stocks, d => d.StockId == selected);
            var quote = e.GetQuote(selected);
            var holding = e.GetHolding(selected);
            stockDetail.text =
                $"{def.CompanyName} · {def.Sector} · {(quote.MarketStatus == MarketStatus.Open ? "거래 가능" : "거래 정지")}\n현재가 {quote.CurrentPrice:N0}G / 기준가 {def.BasePrice:N0}G\n보유 {holding?.quantity ?? 0}주 / 평균 {holding?.AveragePrice ?? 0:N2}G";
            firstQuote.gameObject.SetActive(quote.PriceHistory.Count < 2);
            if (quote.PriceHistory.Count < 2)
            {
                firstQuote.text = quote.PriceHistory.Count == 0
                    ? "아직 기록된 시세가 없습니다."
                    : $"첫 시세  {quote.PriceHistory[0]:N0} G";
            }

            chart.SetPrices(quote.PriceHistory);
            int size;
            bool valid = int.TryParse(quantity.text, out size) && size > 0;
            long total = valid ? (long)quote.CurrentPrice * size : long.MaxValue;
            buy.interactable =
                GrowthAccess.Adult(game)
                && valid
                && total <= e.Cash
                && game.stats.Credit >= e.rules.minimumTradingCredit
                && game.State == GameState.Playing;
            sell.interactable =
                GrowthAccess.Adult(game)
                && valid
                && size <= (holding?.quantity ?? 0)
                && total <= int.MaxValue - e.Cash
                && game.State == GameState.Playing;
            sb.Clear();
            sb.AppendLine(
                $"주식 평가액 {e.StockValue:N0}G\n평가 손익 {e.UnrealizedPnL:+0.00;-0.00;0.00}G\n실현 손익 {e.RealizedPnL:+0.00;-0.00;0.00}G\n총 투자 손익 {e.UnrealizedPnL + e.RealizedPnL:+0.00;-0.00;0.00}G\n"
            );
            foreach (var h in f.holdings.Where(h => h.quantity > 0))
            {
                var q = e.GetQuote(h.stockId);
                sb.AppendLine(
                    $"{Array.Find(e.rules.stocks, d => d.StockId == h.stockId)?.CompanyName}  {h.quantity:N0}주\n평균 {h.AveragePrice:N2}G / 평가 {(long)h.quantity * q.CurrentPrice:N0}G / 손익 {(long)h.quantity * q.CurrentPrice - h.costBasis:+0.00;-0.00;0.00}G\n"
                );
            }

            portfolio.text = sb.ToString();
            news.text =
                f.news.Count == 0
                    ? "아직 경제 뉴스가 없습니다.\n가상 시장의 뉴스는 가격에 일정 기간 반영됩니다."
                    : string.Join(
                        "\n\n",
                        f.news.AsEnumerable()
                            .Reverse()
                            .Take(30)
                            .Select(n =>
                                $"{n.title}\n{n.description}\n{(n.expiresTick > f.marketSequence ? "시장 반영 중" : "반영 종료")} · {n.id}"
                            )
                    );
            history.text = string.Join(
                "\n\n",
                f.transactions.AsEnumerable()
                    .Reverse()
                    .Take(60)
                    .Select(t => $"DAY {t.day} · {t.reason} {t.amount:N0}G\n{t.data}\n{t.id}")
            );
        }
    }
}
