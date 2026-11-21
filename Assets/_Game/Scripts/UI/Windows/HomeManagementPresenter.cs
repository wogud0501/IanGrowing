using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public sealed class HomeManagementPresenter : MonoBehaviour
    {
        public UIShell shell;
        public HousingCatalog catalog;
        public GameObject panel;
        public GameObject confirmation;
        public GameObject[] pages;
        public Button[] tabs;
        public Button[] buy;
        public Button[] place;
        public Button[] reposition;
        public Button[] move;
        public TMP_Text balance;
        public TMP_Text interior;
        public TMP_Text feedback;
        public TMP_Text confirmationText;
        public TMP_Text[] furnitureInfo;
        public TMP_Text[] residenceInfo;
        public Button accept;
        public Button cancel;
        public Image interiorPreview;
        GameManager game;
        Func<bool> pending;
        bool processing;
        int lastFrame = -1;

        void Start()
        {
            if (GameContext.Instance == null)
            {
                return;
            }

            game = GameContext.Instance.game;
            if (confirmationText)
            {
                var frame = (RectTransform)confirmationText.transform.parent;
                var title = LifeRecordUI.Label(frame, confirmationText.font, "주거 계약 확인", 29, LifeRecordUI.Body,
                    .045f, .865f, .79f, .975f);
                RecordPage.Apply(frame, title);
                LifeRecordUI.StyleButton(accept, true);
            }

            game.economy.Changed += Refresh;
            game.StateChanged += State;
            for (int i = 0; i < tabs.Length; i++)
            {
                int n = i;
                tabs[i].onClick.AddListener(() => Tab(n));
            }

            for (int i = 0; i < buy.Length; i++)
            {
                int n = i;
                buy[i].onClick.AddListener(() => RequestFurniture(n));
                place[i].onClick.AddListener(() => Placement(n, false));
                reposition[i].onClick.AddListener(() => Placement(n, true));
            }

            for (int i = 0; i < move.Length; i++)
            {
                int n = i;
                move[i].onClick.AddListener(() => RequestMove(n));
            }

            accept.onClick.AddListener(Confirm);
            cancel.onClick.AddListener(() =>
            {
                pending = null;
                shell.Close(confirmation);
            });
            Tab(0);
            Refresh();
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.economy.Changed -= Refresh;
                game.StateChanged -= State;
            }
        }

        void State(GameState value)
        {
            Refresh();
        }

        public void Tab(int n)
        {
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(i == n);
                LifeRecordUI.StyleButton(tabs[i], i == n);
            }

            if (shell.Top == panel)
            {
                UIShell.Focus(panel);
            }
        }

        void RequestFurniture(int n)
        {
            var f = catalog.furniture[n];
            string reason;
            if (!game.economy.CanBuyFurniture(f.id, out reason))
            {
                feedback.text = reason;
                return;
            }

            Ask(
                $"{f.title}\n가격 {f.price:N0}G / 소지금 {game.economy.Cash:N0}G\n구매 후 잔액 {game.economy.Cash - f.price:N0}G\n보관함에 추가합니다. 배치는 별도로 무료입니다.",
                () => game.economy.BuyFurniture(Guid.NewGuid().ToString("N"), f.id)
            );
        }

        void RequestMove(int n)
        {
            var r = catalog.residences[n];
            string reason;
            if (!game.economy.CanMoveResidence(r.id, out reason))
            {
                feedback.text = reason;
                return;
            }

            Ask(
                $"{r.title}로 이사할까요?\n이사 비용 {r.movingCost:N0}G / 소지금 {game.economy.Cash:N0}G\n결제 후 {game.economy.Cash - r.movingCost:N0}G\n보유 가구는 보관함으로 옮겨집니다. 새 집에서 무료로 배치하세요.",
                () =>
                {
                    bool moved = game.economy.MoveResidence(Guid.NewGuid().ToString("N"), r.id);
                    if (moved && game.world.Active)
                    {
                        while (shell.Depth > 0)
                        {
                            shell.Pop();
                        }

                        game.world.Host.Travel("home");
                    }

                    return moved;
                }
            );
        }

        void Ask(string text, Func<bool> command)
        {
            pending = command;
            confirmationText.text = text;
            shell.Push(confirmation);
        }

        public void Confirm()
        {
            if (pending == null || processing || shell.Top != confirmation)
            {
                return;
            }

            var command = pending;
            pending = null;
            shell.Close(confirmation);
            Durable(command, "결제 완료. 잔액과 집 정보를 저장했습니다.");
        }

        void Placement(int n, bool change)
        {
            var f = catalog.furniture[n];
            var h = game.economy.Housing;
            var existing = h.placed.Find(p => p.id == f.id);
            if (!change && existing != null)
            {
                Durable(() => game.economy.PlaceFurniture(f.id, existing.position, false), "가구 회수 · 무료");
                return;
            }

            var editor = GetComponent<WorldFurniturePlacement>();
            if (editor != null && game.world.Active)
            {
                editor.Begin(f.id);
                return;
            }

            Vector2 position;
            string reason;
            if (!game.economy.FindFurniturePlacement(f.id, change, out position, out reason))
            {
                feedback.text = reason;
                return;
            }

            Durable(
                () => game.economy.PlaceFurniture(f.id, position, true),
                change ? "안전한 위치로 재배치 · 무료" : "가구 배치 · 무료"
            );
        }

        public bool Durable(Func<bool> command, string success)
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
                    feedback.text = "처리할 수 없습니다. 잔액과 소유 상태를 확인하세요.";
                    return false;
                }

                string error;
                if (!game.SaveAutomaticCheckpoint(out error))
                {
                    game.RestoreState(before);
                    feedback.text =
                        "저장 실패 · 변경을 취소했습니다.\n저장 공간과 파일 접근 상태를 확인하고 다시 시도하세요.";
                    Debug.LogWarning(error);
                    return false;
                }

                feedback.text = success;
                return true;
            }
            finally
            {
                processing = false;
                Refresh();
            }
        }

        public void Refresh()
        {
            if (game == null)
            {
                return;
            }

            var e = game.economy;
            var h = e.Housing;
            var home = catalog.Residence(h.residenceId);
            balance.text = $"집 관리  /  소지금 {e.Cash:N0}G";
            interior.text =
                $"현재 집 · {home?.title ?? h.residenceId}\n{Surface(h.floor)} · {Surface(h.wallpaper)}\n{Surface(h.bathroom)} · 고정 위생 설비\n\n보유 가구를 직접 배치하세요.\n추가 가구는 구매 · 재배치는 무료";
            interiorPreview.sprite = home?.preview;
            interiorPreview.enabled = !FamilyLife.AtFamily(game);
            if (FamilyLife.AtFamily(game))
            {
                interior.text =
                    "부모님 집 · 가족 거주\n개인 주거지 · 미보유\n가족 가구는 공동 소유입니다.\n\n19세 이후 비용을 확인하고\n독립을 선택할 수 있습니다.";
            }

            for (int i = 0; i < buy.Length; i++)
            {
                var f = catalog.furniture[i];
                bool owned = h.owned.Contains(f.id),
                    placed = h.placed.Exists(p => p.id == f.id);
                string reason;
                e.CanBuyFurniture(f.id, out reason);
                furnitureInfo[i].text =
                    $"{f.title} · {(f.starter ? "기본 제공" : f.price.ToString("N0") + "G")}\n{f.description}\n{(owned ? f.starter ? "기본 설비 · 설치됨" : placed ? "보유 · 배치 중" : "보유 · 보관 중" : reason ?? "구매 가능")}";
                buy[i].interactable = !processing && e.CanBuyFurniture(f.id, out reason);
                buy[i].GetComponentInChildren<TMP_Text>().text = owned ? "보유 중" : "구매";
                place[i].interactable = reposition[i].interactable = !processing && owned && !f.starter;
                place[i].GetComponentInChildren<TMP_Text>().text =
                    f.starter ? "기본 설비"
                    : placed ? "회수 · 무료"
                    : "배치 · 무료";
            }

            for (int i = 0; i < move.Length; i++)
            {
                var r = catalog.residences[i];
                string reason;
                bool can = e.CanMoveResidence(r.id, out reason);
                residenceInfo[i].text =
                    $"{r.title} · 이사 {r.movingCost:N0}G\n{r.description}\n입주 조건 · {Math.Max(19, r.minAge)}세 이상\n{(can ? "이사 가능" : reason)}";
                move[i].interactable = !processing && can;
                move[i].GetComponentInChildren<TMP_Text>().text = FamilyLife.AtFamily(game) ? "독립 선택" : "이사";
            }
        }

        static string Surface(string id) =>
            id == "wood-linoleum" ? "따뜻한 나무 장판"
            : id == "warm-white" ? "흰색 벽지"
            : id == "jade-tile" ? "옥색 타일"
            : id;
    }
}
