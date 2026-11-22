using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    // Uses the existing safe-area overlay and screen stack. No independent input owner.
    public sealed class ReleaseUI : MonoBehaviour
    {
        UIShell shell;
        GameManager game;
        ReleaseConfig config;
        CollectionVisualTheme theme;
        TMP_FontAsset font;
        TMP_Text documentText;
        TMP_Text rubyText;
        ScrollRect documentScroll;
        GameObject menu;
        GameObject document;
        GameObject shop;
        GameObject consent;
        Action afterConsent;
        bool agreed;
        bool privacyRead;
        public GameObject DocumentPanel => document;
        public GameObject ShopPanel => shop;
        public GameObject ConsentPanel => consent;

        public static void Attach(UIShell s)
        {
            if (s.GetComponent<ReleaseUI>() != null || ReleaseConfig.Current == null)
            {
                return;
            }

            var ui = s.gameObject.AddComponent<ReleaseUI>();
            ui.Build(s);
        }

        RectTransform Rect(string name, Transform parent, Vector2 a, Vector2 b)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = a;
            r.anchorMax = b;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        TMP_Text Text(string name, Transform parent, string value, Vector2 a, Vector2 b, float size = 22)
        {
            var r = Rect(name, parent, a, b);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.fontSize = size;
            t.color = LifeRecordUI.Body;
            t.text = value;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Truncate;
            return t;
        }

        Button Button(string name, Transform parent, string label, Vector2 a, Vector2 b, Action click)
        {
            var r = Rect(name, parent, a, b);
            var image = r.gameObject.AddComponent<Image>();
            if (theme != null)
            {
                image.sprite = theme.panelSprite;
                image.type = Image.Type.Sliced;
            }

            image.color = theme?.panelSprite != null ? Color.white : LifeRecordUI.Track;
            var button = r.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text("Label", r, label, new Vector2(.03f, .05f), new Vector2(.97f, .95f), 20);
            text.alignment = TextAlignmentOptions.Center;
            button.gameObject.AddComponent<PixelControlFeedback>();
            LifeRecordUI.StyleButton(button);
            button.onClick.AddListener(() => click());
            return button;
        }

        GameObject Window(string name, string title, bool closable = true)
        {
            var r = Rect(name, shell.overlayRoot, new Vector2(.04f, .05f), new Vector2(.96f, .95f));
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = theme?.panelSprite;
            img.type = Image.Type.Sliced;
            img.color = img.sprite != null ? Color.white : LifeRecordUI.ReverseInk;
            Text("Title", r, title, new Vector2(.035f, .88f), new Vector2(.79f, .98f), 25);
            if (closable)
            {
                Button(
                    "Close",
                    r,
                    "뒤로",
                    new Vector2(.82f, .90f),
                    new Vector2(.97f, .98f),
                    () => shell.Close(r.gameObject)
                );
            }

            RecordPage.Apply(r, r.Find("Title").GetComponent<TMP_Text>());
            r.gameObject.SetActive(false);
            return r.gameObject;
        }

        void Build(UIShell owner)
        {
            shell = owner;
            game = GameContext.Instance?.game;
            config = ReleaseConfig.Current;
            theme = config.uiTheme;
            if (theme == null)
            {
                theme = game?.world?.Host?.GetComponentInChildren<CollectionUI>()?.visualTheme;
            }

            font = shell.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.font != null)?.font;
            menu = Window("Release Legal Menu", "약관 및 정책");
            document = Window("Release Legal Document", "문서 읽기");
            shop = Window("Release Ruby Shop", "루비 안내");
            consent = Window("Release Consent", "시작 전 확인", false);
            for (int i = 0; i < config.documents.Length; i++)
            {
                var d = config.documents[i];
                float top = .85f - i * .105f;
                Button(
                    "Legal " + d.documentId,
                    menu.transform,
                    d.title,
                    new Vector2(.05f, top - .085f),
                    new Vector2(.95f, top),
                    () => OpenDocument(d.documentId)
                );
            }

            var scroll = Rect("Legal Scroll", document.transform, new Vector2(.045f, .08f), new Vector2(.955f, .86f));
            documentScroll = scroll.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", scroll, Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .02f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport, new Vector2(0, 1), Vector2.one);
            content.pivot = new Vector2(.5f, 1);
            documentText = Text("Legal Body", content, "", Vector2.zero, Vector2.one, 21);
            documentText.overflowMode = TextOverflowModes.Overflow;
            documentText.richText = false;
            RecordPage.FitReadingColumn(content, 0);
            documentScroll.viewport = viewport;
            documentScroll.content = content;
            documentScroll.horizontal = false;
            documentScroll.movementType = ScrollRect.MovementType.Clamped;
            documentScroll.scrollSensitivity = 35;
            var parent =
                shell.motionButton != null ? shell.motionButton.transform.parent : shell.settingsPanel.transform;
            foreach (var pair in new[] { ("약관 및 정책", (Action)OpenLegal), ("루비 안내", (Action)OpenRuby) })
            {
                var b = Button("Release " + pair.Item1, parent, pair.Item1, Vector2.zero, Vector2.one, pair.Item2);
                var layout = b.gameObject.AddComponent<LayoutElement>();
                layout.minHeight = 42;
                layout.preferredHeight = 48;
                layout.flexibleWidth = 1;
            }

            rubyText = Text("Ruby Balance", shop.transform, "", new Vector2(.05f, .72f), new Vector2(.95f, .86f), 23);
            Text(
                "Ruby Explanation",
                shop.transform,
                "루비는 특별한 마일스톤에서 소량 획득하는 재화입니다.\n무료·유료 루비는 합산하여 사용할 수 있으며 만료되지 않습니다.\n기본 성장·생존·학교·직업·주거는 루비 구매 없이 이용할 수 있습니다.",
                new Vector2(.05f, .48f),
                new Vector2(.95f, .72f),
                20
            );
            Text(
                "Purchase Availability",
                shop.transform,
                "구매 기능 준비 중\n현재 실제 결제와 구매 복구는 제공하지 않습니다.\n상품·가격은 Store 연동 후 표시됩니다.\n미성년자의 향후 유료 구매에는 법정대리인 관련 고지가 적용됩니다.",
                new Vector2(.05f, .23f),
                new Vector2(.95f, .48f),
                20
            );
            Button(
                "Paid terms",
                shop.transform,
                "유료서비스 약관",
                new Vector2(.05f, .12f),
                new Vector2(.49f, .21f),
                () => OpenDocument("paid")
            );
            Button(
                "Refund policy",
                shop.transform,
                "환불·고객지원",
                new Vector2(.51f, .12f),
                new Vector2(.95f, .21f),
                () => OpenDocument("refund")
            );
            Text(
                "Consent explanation",
                consent.transform,
                "이용약관에 동의하고 개인정보처리방침을 확인해 주세요.\n개인정보 수집에 대한 별도 포괄 동의를 요구하지 않습니다.\n문서를 읽은 뒤 선택할 수 있습니다.",
                new Vector2(.05f, .66f),
                new Vector2(.95f, .85f),
                22
            );
            Button terms = null,
                privacy = null,
                accept = null;
            terms = Button(
                "Terms consent",
                consent.transform,
                "이용약관 동의 [ ]",
                new Vector2(.05f, .48f),
                new Vector2(.70f, .61f),
                () =>
                {
                    agreed = !agreed;
                    terms.GetComponentInChildren<TMP_Text>().text = "이용약관 동의 " + (agreed ? "[V]" : "[ ]");
                    accept.interactable = agreed && privacyRead;
                }
            );
            Button(
                "Read terms",
                consent.transform,
                "읽기",
                new Vector2(.73f, .48f),
                new Vector2(.95f, .61f),
                () => OpenDocument("terms")
            );
            privacy = Button(
                "Privacy acknowledgement",
                consent.transform,
                "개인정보처리방침 확인 [ ]",
                new Vector2(.05f, .31f),
                new Vector2(.70f, .44f),
                () =>
                {
                    privacyRead = !privacyRead;
                    privacy.GetComponentInChildren<TMP_Text>().text =
                        "개인정보처리방침 확인 " + (privacyRead ? "[V]" : "[ ]");
                    accept.interactable = agreed && privacyRead;
                }
            );
            Button(
                "Read privacy",
                consent.transform,
                "읽기",
                new Vector2(.73f, .31f),
                new Vector2(.95f, .44f),
                () => OpenDocument("privacy")
            );
            accept = Button(
                "Accept required",
                consent.transform,
                "확인하고 시작",
                new Vector2(.15f, .10f),
                new Vector2(.85f, .23f),
                () =>
                {
                    if (!agreed || !privacyRead)
                    {
                        return;
                    }

                    LegalDocumentService.AcceptRequired();
                    shell.Close(consent);
                    var proceed = afterConsent;
                    afterConsent = null;
                    proceed?.Invoke();
                }
            );
            accept.interactable = false;
            if (game != null)
            {
                game.economy.Ruby.Changed += RefreshRuby;
            }
        }

        public void BeforePlay(Action proceed)
        {
            if (LegalDocumentService.NeedsConsent)
            {
                afterConsent = proceed;
                shell.Push(consent);
                return;
            }

            proceed();
        }

        void OnDestroy()
        {
            if (game != null)
            {
                game.economy.Ruby.Changed -= RefreshRuby;
            }
        }

        public void OpenLegal()
        {
            shell.Push(menu);
        }

        public void OpenRuby()
        {
            RefreshRuby();
            shell.Push(shop);
        }

        public void OpenDocument(string id)
        {
            var d = config.Document(id, "ko");
            documentText.text =
                d == null
                    ? "한국어 문서를 찾을 수 없습니다."
                    : d.title
                        + "\n버전 "
                        + d.version
                        + " · 시행일 "
                        + d.effectiveDate
                        + "\n\n"
                        + LegalDocumentService.Body(d);
            shell.Push(document);
            Canvas.ForceUpdateCanvases();
            documentScroll.verticalNormalizedPosition = 1;
        }

        void RefreshRuby()
        {
            if (rubyText != null && game != null)
            {
                rubyText.text =
                    "루비 "
                    + game.economy.Ruby.Total.ToString("N0")
                    + "\n무료 "
                    + game.economy.Ruby.Free
                    + " / 유료 "
                    + game.economy.Ruby.Paid;
            }
        }
    }
}
