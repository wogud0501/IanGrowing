using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace IanGame
{
    public enum WorldScreenProfile
    {
        Desktop,
        PhonePortrait,
        WideTouch,
        ExpandedTouch,
    }

    // Resize changes presentation only. It never reloads the scene, resets a model,
    // warps an actor or recreates InputActionMaps. Safe-area values come from Unity.
    public sealed class WorldMobileHUD : MonoBehaviour
    {
        public WorldHUD hud;
        public CanvasScaler scaler;
        public UISafeLayout safeLayout;
        public RectTransform safeRoot;
        public RectTransform touchRoot;
        public OnScreenStick joystick;
        public OnScreenButton interact;
        public OnScreenButton inventory;
        public OnScreenButton menu;
        public OnScreenButton run;
        public Button frameRateButton;
        public Button detailButton;
        public bool simulateMobile;
        public WorldScreenProfile Profile { get; private set; }
        public int Rebuilds { get; private set; }
        public bool IsMobile => Application.isMobilePlatform || simulateMobile;

        int width;
        int height;
        Rect area;
        bool paused;
        Button cancelAction;
        static Sprite stickPlate;
        static Sprite stickThumb;
        readonly Dictionary<LayoutElement, float> originalRows = new Dictionary<LayoutElement, float>();
        readonly Dictionary<RectTransform, Vector4> originalWindows = new Dictionary<RectTransform, Vector4>();

        void Start()
        {
            frameRateButton?.onClick.AddListener(ToggleFrameRate);
            detailButton?.onClick.AddListener(ToggleDetails);
            Apply(true);
        }

        void Update()
        {
            Apply(false);
            if (touchRoot != null && touchRoot.gameObject.activeInHierarchy)
            {
                bool enabled = hud.world.CanControl;
                bool walking = enabled && hud.world.Player.MovementAllowed;
                joystick.transform.parent.gameObject.SetActive(walking);
                run.gameObject.SetActive(walking);
                cancelAction.gameObject.SetActive(hud.world.CanCancelAction);
                interact.gameObject.SetActive(!hud.world.CanCancelAction);
                joystick.enabled = enabled;
                interact.enabled = enabled;
                run.enabled = enabled;
            }
        }

        public void Apply(bool force = false)
        {
            Rect safe =
                safeLayout != null && safeLayout.simulateSafeArea ? safeLayout.simulatedSafeArea : Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0)
            {
                safe = new Rect(0, 0, Screen.width, Screen.height);
            }

            bool mobile = IsMobile;
            var profile =
                !mobile ? WorldScreenProfile.Desktop
                : Screen.width / (float)Mathf.Max(1, Screen.height) < .75f ? WorldScreenProfile.PhonePortrait
                : Screen.width > Screen.height * 1.55f ? WorldScreenProfile.WideTouch
                : WorldScreenProfile.ExpandedTouch;
            if (!force && width == Screen.width && height == Screen.height && area == safe && Profile == profile)
            {
                return;
            }

            width = Screen.width;
            height = Screen.height;
            area = safe;
            Profile = profile;
            Rebuilds++;
            bool compact = profile == WorldScreenProfile.PhonePortrait;
            scaler.referenceResolution =
                compact ? new Vector2(720, 1280)
                : profile == WorldScreenProfile.ExpandedTouch ? new Vector2(1280, 960)
                : new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (safeLayout != null)
            {
                safeLayout.Apply(true);
            }
            else
            {
                safeRoot.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
                safeRoot.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
                safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            }

            hud.GetComponent<WorldHUDLayout>().compactStack = compact;
            touchRoot.gameObject.SetActive(false);
            hud.menuButton.gameObject.SetActive(!mobile);
            foreach (var type in hud.GetComponentsInChildren<UITypeScale>(true))
            {
                type.platformScale = compact ? 1.12f : 1;
                type.Apply();
            }

            foreach (var row in hud.GetComponentsInChildren<LayoutElement>(true))
            {
                if (row.preferredHeight >= 150 && row.GetComponentInChildren<Button>(true) != null)
                {
                    if (!originalRows.ContainsKey(row))
                    {
                        originalRows[row] = row.preferredHeight;
                    }

                    row.preferredHeight = compact ? Mathf.Max(330, originalRows[row]) : originalRows[row];
                }
            }

            foreach (var root in new[] { hud.shell.overlayRoot })
            {
                foreach (RectTransform child in root)
                {
                    if (child.childCount == 0 || child.TryGetComponent<Image>(out _))
                    {
                        continue;
                    }

                    var card = child.GetChild(0) as RectTransform;
                    if (card == null || card.GetComponent<Image>() == null || card.TryGetComponent<RecordWindowBounds>(out _))
                    {
                        continue;
                    }

                    if (!originalWindows.ContainsKey(card))
                    {
                        originalWindows[card] = new Vector4(
                            card.anchorMin.x,
                            card.anchorMin.y,
                            card.anchorMax.x,
                            card.anchorMax.y
                        );
                    }

                    var old = originalWindows[card];
                    card.anchorMin = compact ? new Vector2(.025f, .05f) : new Vector2(old.x, old.y);
                    card.anchorMax = compact ? new Vector2(.975f, .95f) : new Vector2(old.z, old.w);
                    card.offsetMin = card.offsetMax = Vector2.zero;
                }
            }

            foreach (var row in hud.GetComponentsInChildren<LayoutElement>(true))
            {
                if (row.GetComponent<Button>() != null && row.preferredHeight > 0)
                {
                    if (!originalRows.ContainsKey(row))
                    {
                        originalRows[row] = row.preferredHeight;
                    }

                    row.preferredHeight = compact ? Mathf.Max(88, originalRows[row]) : originalRows[row];
                }
            }

            foreach (var back in hud.GetComponentsInChildren<Button>(true))
            {
                if (back.name == "Back" || back.name == "Close")
                {
                    var element = back.GetComponent<LayoutElement>() ?? back.gameObject.AddComponent<LayoutElement>();
                    element.minWidth = element.preferredWidth = 104;
                    var heading = back.transform.parent.GetComponent<LayoutElement>();
                    if (heading != null)
                    {
                        if (!originalRows.ContainsKey(heading))
                        {
                            originalRows[heading] = heading.preferredHeight;
                        }

                        heading.preferredHeight = compact
                            ? Mathf.Max(88, originalRows[heading])
                            : originalRows[heading];
                    }
                }
            }

            var needs = (RectTransform)hud.needs.transform.parent;
            if (mobile)
            {
                needs.anchorMin = needs.anchorMax = new Vector2(.5f, 0);
                needs.pivot = new Vector2(.5f, 0);
                needs.anchoredPosition = new Vector2(0, 12);
                needs.sizeDelta = new Vector2(244, 40);
                ConfigureControls();
            }

            hud.SetCompactVitals(mobile);
            touchRoot.gameObject.SetActive(mobile && !paused);
            hud.ReflowNotification();
            ApplyQuality();
            Canvas.ForceUpdateCanvases();
            hud.Prompt(hud.promptPanel.activeSelf ? hud.prompt.text : null);
        }

        void ConfigureControls()
        {
            var stick = (RectTransform)joystick.transform.parent;
            Position(stick, Vector2.zero, new Vector2(86, 92), new Vector2(124, 124));
            var handle = (RectTransform)joystick.transform;
            Position(handle, Vector2.one * .5f, Vector2.zero, new Vector2(132, 132));
            joystick.movementRange = 40;
            handle.GetComponent<Image>().color = Color.clear;
            var runLabel = run.GetComponentInChildren<TMP_Text>();
            runLabel.text = "뛰기";
            runLabel.textWrappingMode = TextWrappingModes.NoWrap;
            if (!stickPlate)
            {
                var atlas = Resources.Load<Texture2D>("IanLedger/TouchStick");
                stickPlate = Sprite.Create(atlas, new Rect(0, 0, 64, 64), Vector2.one * .5f, 64);
                stickThumb = Sprite.Create(atlas, new Rect(64, 0, 64, 64), Vector2.one * .5f, 64);
            }

            var plate = stick.GetComponent<Image>();
            plate.sprite = stickPlate;
            plate.type = Image.Type.Simple;
            plate.color = Color.white;
            var thumb = handle.GetChild(0).GetComponent<Image>();
            thumb.sprite = stickThumb;
            thumb.type = Image.Type.Simple;
            thumb.color = Color.white;
            thumb.rectTransform.sizeDelta = new Vector2(52, 52);

            var corner = Vector2.right;
            Position((RectTransform)interact.transform, corner, new Vector2(-64, 86), new Vector2(86, 72));
            Position((RectTransform)run.transform, corner, new Vector2(-157, 86), new Vector2(78, 64));
            Position((RectTransform)inventory.transform, corner, new Vector2(-157, 173), new Vector2(78, 56));
            Position((RectTransform)menu.transform, corner, new Vector2(-64, 173), new Vector2(78, 56));
            if (!cancelAction)
            {
                var go = new GameObject("Cancel current action", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(touchRoot, false);
                var background = go.GetComponent<Image>();
                LifeRecordUI.Skin(background, RecordSurface.Header);
                cancelAction = go.GetComponent<Button>();
                cancelAction.targetGraphic = background;
                var label = LifeRecordUI.Label(go.transform, hud.prompt.font, "취소", 21,
                    LifeRecordUI.Heading, 0, 0, 1, 1);
                label.alignment = TextAlignmentOptions.Center;
                cancelAction.onClick.AddListener(hud.world.CancelAction);
            }

            Position((RectTransform)cancelAction.transform, corner, new Vector2(-64, 86), new Vector2(86, 72));
        }

        static void Position(RectTransform rect, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        public void ToggleFrameRate()
        {
            WorldGraphicsSettings.NextFrameLimit();
            ApplyQuality();
        }

        public void ToggleDetails()
        {
            WorldGraphicsSettings.NextDetail();
            ApplyQuality();
        }

        void ApplyQuality()
        {
            if (frameRateButton)
            {
                frameRateButton.GetComponentInChildren<TMP_Text>().text = WorldGraphicsSettings.FrameLimit == 0
                    ? "수직 동기화" : "목표 FPS · " + WorldGraphicsSettings.FrameLimit;
            }

            if (detailButton)
            {
                detailButton.GetComponentInChildren<TMP_Text>().text =
                    "환경 효과 · " + WorldGraphicsSettings.Names[WorldGraphicsSettings.Detail];
            }
        }

        void OnApplicationPause(bool value)
        {
            paused = value;
            if (value)
            {
                Release();
            }
            else
            {
                Apply(true);
            }
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                Release();
            }
            else if (isActiveAndEnabled)
            {
                Apply(true);
            }
        }

        void Release()
        {
            if (touchRoot != null)
            {
                touchRoot.gameObject.SetActive(false);
            }
        }

        void OnDisable()
        {
            Release();
        }
    }
}
