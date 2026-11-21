using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public sealed class PropertyPresenter : MonoBehaviour
    {
        public UIShell shell;
        public GameObject panel;
        public GameObject confirmation;
        public Button entry;
        public Button close;
        public Button accept;
        public Button cancel;
        public Button[] tabs;
        public Button[] selections;
        public Button[] actions;
        public TMP_Text heading;
        public TMP_Text body;
        public TMP_Text feedback;
        public TMP_Text confirmationText;
        public TMP_InputField amount;
        public GameObject amountRow;
        public Image preview;
        public PixelStockChart chart;
        public ScrollRect scroll;
        public int Page { get; private set; }
        public int Selected { get; private set; }

        GameManager game;
        Func<bool> pending;
        bool processing;
        int lastFrame = -1;
        static readonly string[] tabNames = { "매물", "보유", "계약", "대출", "시세", "기록" };

        void Start()
        {
            if (GameContext.Instance == null)
            {
                return;
            }

            game = GameContext.Instance.game;
            game.economy.Changed += Refresh;
            game.StateChanged += State;
            for (int i = 0; i < tabs.Length; i++)
            {
                int n = i;
                tabs[i].onClick.AddListener(() => Tab(n));
            }

            for (int i = 0; i < selections.Length; i++)
            {
                int n = i;
                selections[i].onClick.AddListener(() => Select(n));
            }

            for (int i = 0; i < actions.Length; i++)
            {
                int n = i;
                actions[i].onClick.AddListener(() => Action(n));
            }

            if (entry != null)
            {
                entry.onClick.AddListener(Open);
            }

            close.onClick.AddListener(() => shell.Close(panel));
            accept.onClick.AddListener(Confirm);
            cancel.onClick.AddListener(() =>
            {
                pending = null;
                shell.Close(confirmation);
            });
            Refresh();
        }

        void State(GameState value) => Refresh();

        void OnDestroy()
        {
            if (game != null)
            {
                game.economy.Changed -= Refresh;
                game.StateChanged -= State;
            }
        }

        public void Open()
        {
            if (game == null || game.BirthIntroductionPending)
            {
                return;
            }

            Tab(0);
            shell.Push(panel);
        }

        public void Tab(int page)
        {
            Page = Mathf.Clamp(page, 0, 5);
            Refresh();
            scroll.verticalNormalizedPosition = 1;
            UIShell.Focus(panel);
        }

        public void Select(int n)
        {
            Selected = Mathf.Clamp(n, 0, game.economy.propertyCatalog.properties.Length - 1);
            Refresh();
            scroll.verticalNormalizedPosition = 1;
        }

        void Ask(string text, Func<bool> command)
        {
            pending = command;
            confirmationText.text = text;
            shell.Push(confirmation);
        }

        public bool Durable(Func<bool> command)
        {
            if (processing || lastFrame == Time.frameCount)
            {
                return false;
            }

            lastFrame = Time.frameCount;
            processing = true;
            var before = game.CaptureState();
            try
            {
                if (!command())
                {
                    game.RestoreState(before);
                    feedback.text = "거래 불가 · 잔액, 신용도, 소유권 또는 변경된 시세를 확인하세요.";
                    return false;
                }

                if (!game.SaveAutomaticCheckpoint(out var error))
                {
                    game.RestoreState(before);
                    feedback.text = "저장 실패 · 거래 전 상태로 복구했습니다.";
                    return false;
                }

                feedback.text = "처리 완료 · 거래와 저장에 반영했습니다.";
                return true;
            }
            catch (Exception e)
            {
                game.RestoreState(before);
                feedback.text = "처리 중단 · 거래 전 상태로 복구했습니다.";
                Debug.LogWarning(e);
                return false;
            }
            finally
            {
                processing = false;
                Refresh();
            }
        }

        public void Confirm()
        {
            if (processing || pending == null || shell.Top != confirmation)
            {
                return;
            }

            var command = pending;
            pending = null;
            shell.Close(confirmation);
            Durable(command);
        }

        public void Action(int n)
        {
            if (!GrowthAccess.Adult(game))
            {
                feedback.text = "금융·주거 계약은 청년기(19세)부터 해금됩니다.";
                return;
            }

            var e = game.economy;
            var p = e.propertyCatalog.properties[Selected];
            var q = e.GetPropertyQuote(p.PropertyId);
            string receipt = Guid.NewGuid().ToString("N");
            if (Page == 0)
            {
                if (n == 0)
                {
                    if (!e.CanBuyProperty(p.PropertyId, out var reason))
                    {
                        feedback.text = reason;
                        return;
                    }

                    int expected = q.MarketPrice;
                    Ask(
                        $"{p.PropertyName} 매수\n시세 {expected:N0}G / 잔액 {e.Cash:N0}G\n매수 후 {e.Cash - expected:N0}G\n소유권만 취득합니다. 입주는 보유 탭에서 선택하세요.\n확인 전 시세가 바뀌면 거래를 취소합니다.",
                        () => e.BuyProperty(receipt, p.PropertyId, expected)
                    );
                }

                if (n == 1 || n == 2)
                {
                    var tenure = n == 1 ? HousingTenure.Jeonse : HousingTenure.MonthlyRent;
                    if (!e.CanLease(p.PropertyId, tenure, out var reason))
                    {
                        feedback.text = reason;
                        return;
                    }

                    long deposit = tenure == HousingTenure.Jeonse ? p.JeonseDeposit : p.MonthlyDeposit;
                    int move = e.housingCatalog.Residence(p.residenceId)?.movingCost ?? 500;
                    Ask(
                        $"{p.PropertyName} · {(tenure == HousingTenure.Jeonse ? "전세" : "월세")} 계약\n반환 보증금 {deposit:N0}G + 이사 {move:N0}G\n{e.propertyCatalog.paymentPeriodDays}일마다 월세 {(tenure == HousingTenure.Jeonse ? 0 : p.MonthlyRent):N0}G + 관리비 {p.MaintenanceCost:N0}G\n계약 {e.propertyCatalog.contractPeriods}회 납부 주기 / 최초 납부는 다음 주기\n해지 시 보증금에서 미납만 상계합니다. 가구는 보관됩니다.",
                        () => e.LeaseProperty(receipt, p.PropertyId, tenure)
                    );
                }
            }
            else if (Page == 1)
            {
                if (n == 0)
                {
                    Ask(
                        $"{p.PropertyName} 입주\n이사 비용 {e.housingCatalog.Residence(p.residenceId)?.movingCost:N0}G\n보유 가구는 보관하며 새 집에서 재배치할 수 있습니다.",
                        () => e.OccupyProperty(receipt, p.PropertyId)
                    );
                }

                if (n == 1)
                {
                    int expected = q.MarketPrice;
                    Ask(
                        $"{p.PropertyName} 매각\n현재가 {expected:N0}G\n거주 중이면 기본 집으로 돌아갑니다. 부채는 계속 유지됩니다.",
                        () => e.SellProperty(receipt, p.PropertyId, expected)
                    );
                }
            }
            else if (Page == 2)
            {
                if (n == 0)
                {
                    var bill = e.Properties.bills.FirstOrDefault(b => b.Remaining > 0);
                    if (bill == null)
                    {
                        feedback.text = "미납 주거비가 없습니다.";
                        return;
                    }

                    Ask(
                        $"주거비 {bill.Remaining:N0}G 납부\n기한 DAY {bill.dueDay}\n월세와 관리비는 실제 지출로 기록됩니다.",
                        () => e.PayPropertyBill(receipt, bill.id)
                    );
                }

                if (n == 1)
                {
                    Ask(
                        "현재 계약을 종료할까요?\n미납을 보증금에서 상계하고 나머지를 반환합니다.\n보유 가구는 유지하고 기본 집으로 돌아갑니다.",
                        () => e.EndLease(receipt)
                    );
                }

                if (n == 2)
                {
                    Ask(
                        "전세보증보험 안내\n보증금은 반환받을 자산이며 미납은 상계됩니다.\n이 게임의 보험 안내는 정보 확인입니다. 실제 보험 가입이나 별도 보상 지급은 없습니다.",
                        () =>
                        {
                            e.LearnHousingInsurance();
                            return true;
                        }
                    );
                }
            }
            else if (Page == 3)
            {
                if (!long.TryParse(amount.text, out long value) || value <= 0)
                {
                    feedback.text = "0보다 큰 금액을 입력하세요.";
                    return;
                }

                if (n == 0)
                {
                    Ask(
                        $"주거자금 {value:N0}G 대출\n현금과 기존 부채가 동시에 증가합니다.\n{e.rules.debtPeriodDays}일마다 원금 기준 {e.rules.debtInterestBasisPoints / 100f:0.##}% 이자\n연체와 신용도는 기존 부채 규칙을 따릅니다.",
                        () => e.BorrowHousing(receipt, value)
                    );
                }

                if (n == 1)
                {
                    Ask(
                        $"기존 부채 {value:N0}G 상환\n미납 이자를 먼저 정산한 뒤 원금에 반영합니다.",
                        () => e.RepayDebt(receipt, value)
                    );
                }
            }
        }

        public void Refresh()
        {
            if (game == null || game.State == GameState.Starting || game.economy.propertyCatalog == null)
            {
                return;
            }

            var e = game.economy;
            var s = e.Properties;
            var p = e.propertyCatalog.properties[Selected];
            var q = e.GetPropertyQuote(p.PropertyId);
            var h = s.owned.Find(x => x.PropertyId == p.PropertyId);
            heading.text = $"이안 부동산 · {tabNames[Page]}  /  {e.Cash:N0}G";
            foreach (var a in actions)
            {
                a.gameObject.SetActive(false);
            }

            for (int i = 0; i < tabs.Length; i++)
            {
                LifeRecordUI.StyleButton(tabs[i], i == Page);
            }

            for (int i = 0; i < selections.Length; i++)
            {
                var d = e.propertyCatalog.properties[i];
                selections[i].gameObject.SetActive(Page == 0 || Page == 1 || Page == 4);
                selections[i].GetComponentInChildren<TMP_Text>().text =
                    d.PropertyName + (s.owned.Any(o => o.PropertyId == d.PropertyId) ? " · 보유" : "");
                LifeRecordUI.StyleButton(selections[i], i == Selected);
            }

            preview.gameObject.SetActive(Page == 0 || Page == 1);
            preview.sprite = p.preview;
            chart.gameObject.SetActive(Page == 4);
            if (Page == 4)
            {
                chart.SetPrices(q.PriceHistory);
            }

            amountRow.SetActive(Page == 3);
            void Button(int n, string title, bool enabled = true)
            {
                actions[n].gameObject.SetActive(true);
                actions[n].interactable = !processing && enabled && GrowthAccess.Adult(game);
                actions[n].GetComponentInChildren<TMP_Text>().text = title;
            }

            var b = new StringBuilder();
            if (Page == 0 || Page == 1 || Page == 4)
            {
                b.AppendLine(
                    $"{p.PropertyName} / {p.Location}\n시세 {q.MarketPrice:N0}G · 기준가 {p.PurchasePrice:N0}G\n소유 {(h == null ? "미보유" : "보유")} / 거주 {(e.Occupancy(p.PropertyId) == PropertyOccupancy.Vacant ? "미입주" : e.Occupancy(p.PropertyId) == PropertyOccupancy.Leased ? "임대 계약" : "자가 거주")}"
                );
                if (Page == 0)
                {
                    b.AppendLine(
                        $"전세 {p.JeonseDeposit:N0}G / 월세 보증금 {p.MonthlyDeposit:N0}G\n월세 {p.MonthlyRent:N0}G + 관리비 {p.MaintenanceCost:N0}G / {e.propertyCatalog.paymentPeriodDays}일\n입주 {p.minAge}세부터 · 수면 체력 +{p.sleepBonus.health}, 정신력 +{p.sleepBonus.mental}\n자산 전환과 별도로 이사 비용이 필요합니다. 가구는 제공하지 않습니다."
                    );
                }

                if (Page == 1)
                {
                    b.AppendLine(
                        $"취득가 {h?.costBasis ?? 0:N0}G · 평가 손익 {(h == null ? 0 : q.MarketPrice - h.costBasis):+#,0;-#,0;0}G\n보유 부동산 평가 {e.PropertyValue:N0}G / 반환 보증금 {e.HousingDeposit:N0}G\n총자산 {e.TotalAssets:N0}G · 미납 {e.HousingArrears:N0}G\n순자산 {e.NetWorth:N0}G / 누적 실현 손익 {s.realizedPnL:+#,0;-#,0;0}G"
                    );
                }

                if (Page == 4)
                {
                    b.AppendLine(
                        "가격 기록 · " + string.Join(" → ", q.PriceHistory.TakeLast(12).Select(v => v.ToString("N0")))
                    );
                    b.AppendLine(
                        $"시장 주기 {e.propertyCatalog.marketIntervalSeconds}초 / 금리 지표 {e.propertyCatalog.marketInterestBasisPoints / 100f:0.##}%\n지역·유형 변동성과 기존 경기/긴축 뉴스가 반영됩니다."
                    );
                    foreach (var n in e.Finance.news.TakeLast(8).Reverse())
                    {
                        b.AppendLine(n.title + "\n" + n.description);
                    }
                }
            }

            if (Page == 0)
            {
                Button(0, "매수", e.CanBuyProperty(p.PropertyId, out _));
                Button(1, "전세 계약", e.CanLease(p.PropertyId, HousingTenure.Jeonse, out _));
                Button(2, "월세 계약", e.CanLease(p.PropertyId, HousingTenure.MonthlyRent, out _));
            }

            if (Page == 1)
            {
                Button(0, "소유 주택 입주", h != null && s.contract == null && s.occupiedPropertyId != p.PropertyId);
                Button(1, "매각", h != null);
            }

            if (Page == 2)
            {
                var c = s.contract;
                b.AppendLine(
                    c == null
                        ? "현재 임대 계약 없음"
                        : $"{e.propertyCatalog.Find(c.PropertyId)?.PropertyName} · {(c.tenure == HousingTenure.Jeonse ? "전세" : "월세")}\n보증금 자산 {c.deposit:N0}G\nDAY {c.startDay}~{c.endDay} / 다음 납부 DAY {c.nextPaymentDay}\n월세 {c.monthlyRent:N0}G + 관리비 {c.maintenance:N0}G"
                );
                b.AppendLine(
                    $"미납 합계 {e.HousingArrears:N0}G\n납부일에 잔액이 있으면 자동 납부하며 부족하면 미납으로 보존합니다."
                );
                foreach (var bill in s.bills.TakeLast(20).Reverse())
                {
                    b.AppendLine(
                        $"DAY {bill.dueDay} · 월세 {bill.rent:N0} / 관리 {bill.maintenance:N0} / 미납 {bill.Remaining:N0}G"
                    );
                }

                Button(0, "미납 1건 납부", e.HousingArrears > 0);
                Button(1, "계약 종료", c != null);
                Button(2, "보증보험 안내");
            }

            if (Page == 3)
            {
                b.AppendLine(
                    $"대출 가능 {e.AvailableHousingLoan:N0}G / 신용 {game.stats.Credit}\n19세부터 · 최소 신용 {e.propertyCatalog.minimumLoanCredit} / 부채 포함 한도\n기존 부채 원금 {e.Principal:N0}G (주거 대출 잔여 {e.HousingLoanPrincipal:N0}G)\n미납 이자 {e.Finance.outstandingInterest:N0}G / 총 부채 {e.Debt:N0}G\n{e.rules.debtPeriodDays}일마다 원금의 {e.rules.debtInterestBasisPoints / 100f:0.##}% · 이중 이자 없음\n다음 이자 DAY {e.Finance.lastDebtDay + e.rules.debtPeriodDays}\n주거 대출은 기존 부채 계정의 용도 기록입니다. 상환은 이자 우선이며 원금 상환 시 주거 대출에도 반영됩니다."
                );
                Button(0, "대출 실행", e.AvailableHousingLoan > 0);
                Button(1, "부채 상환", e.Debt > 0);
            }

            if (Page == 5)
            {
                b.AppendLine("부동산 거래 기록 · 기존 경제 영수증과 동일한 원장");
                foreach (
                    var t in e
                        .Finance.transactions.Where(t =>
                            t.kind >= TransactionKind.PropertyBuy || t.kind == TransactionKind.DebtRepayment
                        )
                        .TakeLast(50)
                        .Reverse()
                )
                {
                    b.AppendLine($"DAY {t.day} · {t.reason}\n{t.amount:N0}G · {t.data}");
                }
            }

            body.text = b.ToString();
            Canvas.ForceUpdateCanvases();
        }
    }
}
