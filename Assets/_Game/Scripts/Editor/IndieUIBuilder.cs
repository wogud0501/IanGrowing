using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IanGame.Editor
{
    public static class IndieUIBuilder
    {
        const string ThemePath = "Assets/_Game/UI/IndieTheme.asset";
        static IndieUITheme theme;

        static T C<T>(Component host)
            where T : Component
        {
            var value = host.GetComponent<T>();
            return value != null ? value : host.gameObject.AddComponent<T>();
        }

        static T C<T>(GameObject host)
            where T : Component
        {
            var value = host.GetComponent<T>();
            return value != null ? value : host.AddComponent<T>();
        }

        static RectTransform R(Transform parent, string name)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            return r;
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.pivot = new Vector2(.5f, .5f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            r.localScale = Vector3.one;
            r.localRotation = Quaternion.identity;
        }

        static LayoutElement Size(Component c, float height = -1, float width = -1, float flexible = 0)
        {
            var l = C<LayoutElement>(c);
            l.minHeight = height > 0 ? height : 0;
            l.preferredHeight = height;
            l.preferredWidth = width;
            l.flexibleHeight = flexible;
            l.flexibleWidth = width < 0 ? 1 : 0;
            return l;
        }

        static VerticalLayoutGroup Col(Transform t, int pad = 0, float gap = 8)
        {
            var l = C<VerticalLayoutGroup>(t);
            l.padding = new RectOffset(pad, pad, pad, pad);
            l.spacing = gap;
            l.childControlWidth = l.childControlHeight = true;
            l.childForceExpandWidth = true;
            l.childForceExpandHeight = false;
            return l;
        }

        static HorizontalLayoutGroup Row(Transform t, int pad = 0, float gap = 12)
        {
            var l = C<HorizontalLayoutGroup>(t);
            l.padding = new RectOffset(pad, pad, pad, pad);
            l.spacing = gap;
            l.childControlWidth = l.childControlHeight = true;
            l.childForceExpandWidth = false;
            l.childForceExpandHeight = true;
            return l;
        }

        static Image Card(Transform parent, string name, Color color, int padding = 20)
        {
            var r = R(parent, name);
            var image = r.gameObject.AddComponent<Image>();
            image.sprite = theme.rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            Col(r, padding, 8);
            Size(image, -1, -1, 1);
            return image;
        }

        static TMP_Text T(Transform parent, string name, string value, float size, Color color, float height = -1)
        {
            var r = R(parent, name);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            StyleText(t, size, color);
            t.text = value;
            Size(t, height);
            return t;
        }

        static void StyleText(TMP_Text t, float size, Color color)
        {
            t.font = theme.font;
            t.fontSize = size;
            t.fontStyle = size >= 26 ? FontStyles.Bold : FontStyles.Normal;
            t.color = color;
            t.alignment = TextAlignmentOptions.TopLeft;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.enableAutoSizing = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.margin = Vector4.zero;
            var scale = C<UITypeScale>(t);
            scale.baseSize = size;
        }

        static void Move(Component c, Transform parent, float height = -1)
        {
            if (c == null)
            {
                return;
            }

            c.transform.SetParent(parent, false);
            c.transform.SetAsLastSibling();
            c.transform.localScale = Vector3.one;
            c.transform.localRotation = Quaternion.identity;
            var r = c.GetComponent<RectTransform>();
            if (r != null)
            {
                r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
                r.pivot = new Vector2(.5f, .5f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = Vector2.zero;
            }

            Size(c, height);
            c.gameObject.SetActive(true);
        }

        static void ButtonStyle(Button b, Color color)
        {
            var image = b.GetComponent<Image>();
            if (image == null)
            {
                image = b.gameObject.AddComponent<Image>();
            }

            image.sprite = theme.rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            b.targetGraphic = image;
            b.transition = Selectable.Transition.ColorTint;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            cb.pressedColor = new Color(.82f, .82f, .82f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(.68f, .68f, .68f, .65f);
            cb.fadeDuration = .08f;
            b.colors = cb;
            var text = b.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                StyleText(text, 22, theme.ink);
                text.alignment = TextAlignmentOptions.Center;
                text.enableAutoSizing = true;
                text.fontSizeMin = 18;
                text.fontSizeMax = 22;
                Stretch(text.rectTransform);
                text.margin = new Vector4(14, 6, 14, 6);
            }

            var feel = C<UIButtonFeel>(b);
            if (feel.focusMark == null)
            {
                var mark = R(b.transform, "Focus / non-color cue");
                Stretch(mark);
                mark.offsetMin = new Vector2(-3, -3);
                mark.offsetMax = new Vector2(3, 3);
                var outline = mark.gameObject.AddComponent<Outline>();
                outline.effectColor = theme.ink;
                outline.effectDistance = new Vector2(3, -3);
                var img = mark.gameObject.AddComponent<Image>();
                img.sprite = theme.rounded;
                img.type = Image.Type.Sliced;
                img.color = new Color(1, 1, 1, .07f);
                img.raycastTarget = false;
                feel.focusMark = mark.gameObject;
                mark.gameObject.SetActive(false);
            }
        }

        static Button B(
            Transform parent,
            string name,
            string label,
            Color color,
            UIShell shell = null,
            UICommand.Command command = UICommand.Command.Open,
            GameObject panel = null
        )
        {
            var r = R(parent, name);
            var image = r.gameObject.AddComponent<Image>();
            var b = r.gameObject.AddComponent<Button>();
            T(r, "Label", label, 22, theme.ink);
            ButtonStyle(b, color);
            Size(b, 54);
            if (shell != null)
            {
                var action = r.gameObject.AddComponent<UICommand>();
                action.shell = shell;
                action.command = command;
                action.panel = panel;
                UnityEventTools.AddPersistentListener(b.onClick, action.Invoke);
            }

            return b;
        }

        static void Deco(Transform parent, UIAccentGraphic.Shape shape, Color color, Vector2 a, Vector2 b)
        {
            var r = R(parent, "Print / " + shape);
            Stretch(r);
            r.anchorMin = a;
            r.anchorMax = b;
            var g = r.gameObject.AddComponent<UIAccentGraphic>();
            g.shape = shape;
            g.color = color;
            g.raycastTarget = false;
            Size(g).ignoreLayout = true;
            r.SetAsFirstSibling();
        }

        static RectTransform Scroll(Transform parent, string name, out ScrollRect scroll)
        {
            var viewport = R(parent, name);
            Size(viewport, -1, -1, 1);
            var bg = viewport.gameObject.AddComponent<Image>();
            bg.color = new Color(1, 1, 1, .01f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = R(viewport, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;
            Col(content, 2, 10);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36;
            return content;
        }

        static GameObject Modal(UIShell shell, string name, string title, Color color, out Transform body)
        {
            var root = R(shell.overlayRoot, name);
            Stretch(root);
            root.gameObject.AddComponent<CanvasGroup>();
            var veil = root.gameObject.AddComponent<Image>();
            veil.color = new Color(.06f, .08f, .14f, .76f);
            var window = Card(root, "Window / " + name, color, 24);
            var wr = window.rectTransform;
            Stretch(wr);
            wr.anchorMin = new Vector2(.04f, .04f);
            wr.anchorMax = new Vector2(.96f, .96f);
            var top = R(wr, "Modal heading");
            Size(top, 52);
            Row(top);
            T(top, "Title", title, 32, theme.ink, 44);
            var close = B(top, "Back", "닫기 / ESC", theme.white, shell, UICommand.Command.Close, root.gameObject);
            Size(close, 48, 180);
            ScrollRect scroll;
            body = Scroll(wr, "Scrollable body", out scroll);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        static UIShell Shell(Transform canvas, CanvasGroup baseGroup, out RectTransform overlay)
        {
            var shell = C<UIShell>(canvas);
            shell.baseGroup = baseGroup;
            overlay = R(canvas, "Overlay Stack");
            Stretch(overlay);
            var c = overlay.gameObject.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 100;
            overlay.gameObject.AddComponent<GraphicRaycaster>();
            var safe = R(overlay, "Safe area");
            Stretch(safe);
            var frame = R(safe, "Constrained overlays");
            Stretch(frame);
            var layout = overlay.gameObject.AddComponent<UISafeLayout>();
            layout.safe = safe;
            layout.frame = frame;
            layout.maxWidth = 1660;
            shell.overlayRoot = frame;
            return shell;
        }

        static void Settings(UIShell shell, UIManager hud)
        {
            Transform body;
            shell.settingsPanel = Modal(shell, "Settings", "환경 설정 / 내 인생의 표시 옵션", theme.lavender, out body);
            T(body, "Intro", "화면 표시만 변경합니다. 게임 속도와 능력치에는 영향을 주지 않습니다.", 24, theme.ink);
            shell.motionButton = B(body, "Reduce motion", "모션 줄이기", theme.white, shell, UICommand.Command.Motion);
            shell.textSizeButton = B(body, "Text size", "글자 크기", theme.white, shell, UICommand.Command.TextSize);
            T(
                body,
                "Input",
                "조작 안내\n마우스·터치: 버튼 선택 / 방향키·게임패드: 포커스 이동\nEnter·패드 A: 실행 / ESC·패드 B: 이전 화면 / Tab: 다음 항목",
                24,
                theme.ink
            );
            shell.confirmPanel = Modal(
                shell,
                "New life confirmation",
                "새 인생 / 기존 진행 초기화",
                theme.coral,
                out body
            );
            T(
                body,
                "Warning",
                "새 인생을 시작하면 현재 진행을 초기화하고 저장합니다.\n계속 진행하려면 아래 버튼을 선택하세요.",
                28,
                theme.ink
            );
            B(body, "Confirm new life", "확인 · 새 인생 시작", theme.ink, shell, UICommand.Command.AcceptLife)
                .GetComponentInChildren<TMP_Text>()
                .color = theme.white;
            shell.statusPanel = Modal(shell, "Detailed status", "생존 명세서 / 8개 핵심 능력치", theme.mint, out body);
            T(body, "Status details", "", 28, theme.ink);
            shell.pausePanel = Modal(shell, "Pause", "업무 일시 중단 / 일시정지", theme.paper, out body);
            T(body, "Pause caption", "잠깐 쉬어도 됩니다. 부채는 그대로 기다립니다.", 28, theme.ink);
            B(body, "Resume", "계속하기", theme.lime, shell, UICommand.Command.Close, shell.pausePanel);
            B(body, "Settings", "환경 설정", theme.lavender, shell, UICommand.Command.Settings);
            if (hud != null)
            {
                Move(hud.saveButton, body, 58);
                Move(hud.loadButton, body, 58);
                Move(hud.newGameButton, body, 58);
                ButtonStyle(hud.saveButton, theme.white);
                ButtonStyle(hud.loadButton, theme.white);
                ButtonStyle(hud.newGameButton, theme.coral);
            }
            else
            {
                B(body, "New life", "새 인생 시작", theme.coral, shell, UICommand.Command.ConfirmLife);
            }
        }

        static void PrepareTheme()
        {
            Directory.CreateDirectory("Assets/_Game/UI/Indie");
            theme = AssetDatabase.LoadAssetAtPath<IndieUITheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<IndieUITheme>();
                theme.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/UI/IanKoreanSDF.asset");
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            if (theme.rounded == null)
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                var pixels = new Color[4096];
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        float dx = Mathf.Max(0, 10 - Mathf.Min(x, 63 - x)),
                            dy = Mathf.Max(0, 10 - Mathf.Min(y, 63 - y));
                        pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01(10.5f - Mathf.Sqrt(dx * dx + dy * dy)));
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();
                string path = "Assets/_Game/UI/Indie/SoftCorner.png";
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(12, 12, 12, 12);
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                theme.rounded = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                EditorUtility.SetDirty(theme);
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem("Ian/UI UX 1.0/Apply Indie Design")]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                throw new Exception("Stop Play Mode first.");
            }

            PrepareTheme();
            StyleRows();
            string path = "Assets/_Game/Prefabs/GameHUD.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<IndieHUD>() == null)
                {
                    BuildHUD(root);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            foreach (var name in new[] { "Home", "School", "City", "Workplace" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/" + name + ".unity");
                var hud = UnityEngine.Object.FindFirstObjectByType<UIManager>();
                if (hud.GetComponent<IndieHUD>() == null)
                {
                    throw new Exception("HUD prefab did not propagate to " + name);
                }

                var bindings = UnityEngine.Object.FindFirstObjectByType<GameplaySceneBindings>();
                bindings.hud = hud;
                bindings.missionUI = hud.GetComponentInChildren<MissionUI>(true);
                EditorUtility.SetDirty(bindings);
                var phase = UnityEngine.Object.FindFirstObjectByType<Phase23Presenter>();
                if (phase != null && phase.panel.transform.parent != hud.GetComponent<UIShell>().overlayRoot)
                {
                    StyleLocation(phase, hud.GetComponent<UIShell>());
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            BuildMenu();
            BuildVariants();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity");
        }

        static void BuildHUD(GameObject root)
        {
            var hud = root.GetComponent<UIManager>();
            var canvas = root.GetComponent<Canvas>();
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 1000);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1;
            var old = root.transform.Find("Dashboard");
            var mission = root.GetComponentInChildren<MissionUI>(true);
            if (PrefabUtility.IsPartOfPrefabInstance(mission.gameObject))
            {
                PrefabUtility.UnpackPrefabInstance(
                    mission.gameObject,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction
                );
            }

            var back = R(root.transform, "Print background");
            Stretch(back);
            var bg = back.gameObject.AddComponent<Image>();
            bg.color = theme.paper;
            Deco(back, UIAccentGraphic.Shape.Grid, new Color(.11f, .15f, .22f, .055f), Vector2.zero, Vector2.one);
            back.SetAsFirstSibling();
            var safe = R(root.transform, "HUD Safe area");
            Stretch(safe);
            var frame = R(safe, "HUD Content / max 1660");
            Stretch(frame);
            var group = frame.gameObject.AddComponent<CanvasGroup>();
            var safeLayout = root.AddComponent<UISafeLayout>();
            safeLayout.safe = safe;
            safeLayout.frame = frame;
            safeLayout.maxWidth = 1660;
            Col(frame, 20, 12);
            RectTransform overlay;
            var shell = Shell(root.transform, group, out overlay);
            Settings(shell, hud);
            var visual = root.AddComponent<IndieHUD>();
            visual.hud = hud;
            visual.missions = mission;
            visual.theme = theme;
            visual.shell = shell;
            visual.statusDetails = shell
                .statusPanel.GetComponentsInChildren<TMP_Text>(true)
                .First(t => t.name == "Status details");
            var header = R(frame, "Top / identity and economy");
            Size(header, 88);
            var headerFlow = header.gameObject.AddComponent<UIAdaptiveLayout>();
            headerFlow.spacing = 12;
            var brand = R(header, "Brand");
            Col(brand, 0, 0);
            Size(brand, -1, 390);
            T(brand, "Edition", "LIFE MANAGEMENT BUREAU  /  인생 관리국", 18, theme.ink, 26);
            T(brand, "Game title", "이안 키우기", 42, theme.ink, 52);
            var chips = R(header, "Financial summary");
            Row(chips, 0, 10);
            Size(chips);
            visual.ageText = T(Card(chips, "Age and day", theme.ink, 12).transform, "Age", "", 26, theme.white);
            visual.cashText = T(Card(chips, "Cash", theme.lime, 12).transform, "Cash", "", 24, theme.ink);
            visual.debtText = T(Card(chips, "Debt", theme.coral, 12).transform, "Debt", "", 24, theme.ink);
            var middle = R(frame, "Core / character stats mission");
            var middleElement = Size(middle, -1, -1, 1);
            var middleFlow = middle.gameObject.AddComponent<UIAdaptiveLayout>();
            middleFlow.spacing = 16;
            var stats = Card(middle, "Vitals / 8 stat strips", theme.lavender, 16);
            Size(stats, -1, 290, 1);
            T(stats.transform, "Vitals heading", "생존 명세서", 30, theme.ink, 42);
            var statRows = R(stats.transform, "Stat rows");
            Size(statRows, -1, -1, 1);
            var statsFlow = statRows.gameObject.AddComponent<UIAdaptiveLayout>();
            statsFlow.vertical = true;
            statsFlow.spacing = 8;
            visual.statValues = new TMP_Text[8];
            visual.statFills = hud.coreFills;
            Color[] statColors =
            {
                theme.coral,
                new Color32(56, 127, 171, 255),
                new Color32(114, 82, 171, 255),
                new Color32(46, 119, 92, 255),
                new Color32(167, 77, 88, 255),
                new Color32(82, 90, 154, 255),
                new Color32(80, 107, 64, 255),
                new Color32(156, 93, 40, 255),
            };
            for (int i = 0; i < 8; i++)
            {
                var strip = R(statRows, "Stat " + i);
                Col(strip, 0, 5);
                Size(strip, 42, -1, 1);
                var line = R(strip, "Name and value");
                Size(line, 28);
                Row(line, 0, 4);
                Move(hud.coreLabels[i], line, 28);
                StyleText(hud.coreLabels[i], 21, theme.ink);
                visual.statValues[i] = T(line, "Value", "0 /100", 24, theme.ink, 28);
                Size(visual.statValues[i], 28, 96);
                visual.statValues[i].alignment = TextAlignmentOptions.Right;
                var track = R(strip, "Track");
                Size(track, 6);
                var trackImage = track.gameObject.AddComponent<Image>();
                trackImage.color = new Color(.11f, .15f, .22f, .12f);
                Move(hud.coreFills[i], track);
                var fill = hud.coreFills[i];
                var le = fill.GetComponent<LayoutElement>();
                le.ignoreLayout = true;
                Stretch(fill.rectTransform);
                fill.color = statColors[i];
                fill.raycastTarget = false;
            }

            var hero = Card(middle, "Ian / official identity", theme.coral, 20);
            var heroElement = Size(hero, -1, -1, 1);
            Deco(
                hero.transform,
                UIAccentGraphic.Shape.Sunburst,
                new Color(1, .93f, .55f, .72f),
                new Vector2(.12f, .1f),
                new Vector2(.88f, .88f)
            );
            var heroTop = R(hero.transform, "Subject header");
            Row(heroTop);
            Size(heroTop, 36);
            visual.locationText = T(heroTop, "Location", "HOME", 22, theme.ink, 32);
            T(heroTop, "Case number", "CASE  /  IAN-001", 18, theme.ink, 28).alignment = TextAlignmentOptions.Right;
            var portrait = R(hero.transform, "Official Ian image / unchanged sprite");
            Size(portrait, -1, -1, 1);
            var image = portrait.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase
                .LoadAssetAtPath<CharacterIdentity>("Assets/_Game/Data/IanOfficialIdentity.asset")
                .baseSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            visual.heroTitle = T(hero.transform, "Hero headline", "미래의 노동자,\n현재는 이안.", 36, theme.ink, 88);
            visual.heroCaption = T(hero.transform, "Hero caption", "행복은 별도 구매입니다.", 21, theme.ink, 52);
            var slot = R(middle, "Mission slot");
            var missionElement = Size(slot, -1, 445, 1);
            Move(mission, slot);
            Size(mission).ignoreLayout = true;
            Stretch(mission.GetComponent<RectTransform>());
            var current = mission.missionTitle.transform.parent;
            Move(current, mission.transform);
            Stretch(current.GetComponent<RectTransform>());
            var currentImage = current.GetComponent<Image>();
            currentImage.sprite = theme.rounded;
            currentImage.type = Image.Type.Sliced;
            currentImage.color = theme.lime;
            Col(current, 20, 8);
            foreach (Transform c in current)
            {
                if (
                    c.GetComponent<TMP_Text>() != null
                    && c.GetComponent<TMP_Text>() != mission.missionTitle
                    && c.GetComponent<TMP_Text>() != mission.chapterText
                    && c.GetComponent<TMP_Text>() != mission.conditionText
                    && c.GetComponent<TMP_Text>() != mission.progressText
                    && c.GetComponent<TMP_Text>() != mission.rewardText
                    && c.GetComponent<TMP_Text>() != mission.nextText
                )
                {
                    c.gameObject.SetActive(false);
                }
            }

            Move(mission.chapterText, current, 24);
            StyleText(mission.chapterText, 18, theme.ink);
            Move(mission.missionTitle, current, 68);
            StyleText(mission.missionTitle, 30, theme.ink);
            var prog = R(current, "Mission progress rail");
            Size(prog, 8);
            prog.gameObject.AddComponent<Image>().color = new Color(.1f, .15f, .2f, .12f);
            var pfill = R(prog, "Progress fill");
            Stretch(pfill);
            visual.missionFill = pfill.gameObject.AddComponent<Image>();
            visual.missionFill.color = theme.ink;
            visual.missionFill.type = Image.Type.Filled;
            visual.missionFill.fillMethod = Image.FillMethod.Horizontal;
            Move(mission.progressText, current, 26);
            StyleText(mission.progressText, 20, theme.ink);
            ScrollRect conditionScroll;
            var conditions = Scroll(current, "Exact conditions and action guide", out conditionScroll);
            Move(mission.conditionText, conditions);
            StyleText(mission.conditionText, 22, theme.ink);
            Move(mission.rewardText, current, 44);
            StyleText(mission.rewardText, 21, theme.ink);
            var missionActions = R(current, "Mission actions");
            Row(missionActions, 0, 10);
            Size(missionActions, 60);
            Move(mission.actionButton, missionActions, 60);
            Move(mission.claimButton, missionActions, 60);
            ButtonStyle(mission.actionButton, theme.white);
            ButtonStyle(mission.claimButton, theme.ink);
            mission.claimButton.GetComponentInChildren<TMP_Text>().color = theme.white;
            var missionLinks = R(current, "Mission links");
            Row(missionLinks, 0, 10);
            Size(missionLinks, 48);
            Move(mission.listButton, missionLinks, 48);
            Move(mission.educationButton, missionLinks, 48);
            ButtonStyle(mission.listButton, theme.white);
            ButtonStyle(mission.educationButton, theme.white);
            Move(mission.nextText, current, 24);
            StyleText(mission.nextText, 17, theme.ink);
            StyleMissionOverlays(mission, shell);
            var actions = Card(frame, "Life actions / choose one", theme.white, 12);
            var actionsElement = Size(actions, 132);
            T(
                actions.transform,
                "Action heading",
                "오늘도 생존에 투자하세요.  /  행동 한 번은 게임 속 하루를 사용합니다.",
                20,
                theme.ink,
                28
            );
            var actionGridRect = R(actions.transform, "Action buttons");
            Size(actionGridRect, -1, -1, 1);
            var actionGrid = actionGridRect.gameObject.AddComponent<GridLayoutGroup>();
            actionGrid.spacing = new Vector2(10, 8);
            actionGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            actionGrid.constraintCount = 6;
            var actionFluid = actionGridRect.gameObject.AddComponent<UIResponsiveGrid>();
            actionFluid.grid = actionGrid;
            actionFluid.rowHeight = 78;
            actionFluid.minCellWidth = 220;
            actionFluid.maxColumns = 6;
            var buttons = new[]
            {
                hud.mealButton,
                hud.drinkButton,
                hud.sleepButton,
                hud.restButton,
                hud.playButton,
                hud.hospitalButton,
            };
            visual.lifeButtons = buttons;
            visual.actionIds = new[] { 3, 4, 5, 1, 2, 6 };
            visual.actionReasons = new TMP_Text[6];
            for (int i = 0; i < 6; i++)
            {
                var holder = R(actionGridRect, "Action tile " + i);
                Col(holder, 0, 3);
                Move(buttons[i], holder, 46);
                ButtonStyle(
                    buttons[i],
                    i < 2 ? theme.mint
                        : i == 2 ? theme.lavender
                        : theme.paper
                );
                visual.actionReasons[i] = T(holder, "Cost or lock reason", "", 17, theme.ink, 24);
                visual.actionReasons[i].alignment = TextAlignmentOptions.Center;
            }

            var navigation = R(frame, "Location navigation");
            var navElement = Size(navigation, 66);
            var navGrid = navigation.gameObject.AddComponent<GridLayoutGroup>();
            navGrid.spacing = new Vector2(10, 8);
            navGrid.constraintCount = 6;
            var navFluid = navigation.gameObject.AddComponent<UIResponsiveGrid>();
            navFluid.grid = navGrid;
            navFluid.rowHeight = 48;
            navFluid.minCellWidth = 220;
            navFluid.maxColumns = 6;
            var loc = root.GetComponentInChildren<LocationPresenter>(true);
            if (loc != null)
            {
                loc.transform.SetParent(root.transform, false);
                loc.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                Move(loc.homeButton, navigation, 48);
                Move(loc.schoolButton, navigation, 48);
                ButtonStyle(loc.homeButton, theme.ink);
                ButtonStyle(loc.schoolButton, theme.paper);
                loc.homeButton.GetComponentInChildren<TMP_Text>().color = theme.white;
                loc.menuButton.transform.SetParent(
                    shell.pausePanel.GetComponentInChildren<ScrollRect>(true).content,
                    false
                );
                ButtonStyle(loc.menuButton, theme.paper);
                Size(loc.menuButton, 54);
            }

            foreach (var route in new[] { SceneId.City, SceneId.Workplace })
            {
                var b = B(
                    navigation,
                    route.ToString(),
                    MissionGuidance.Place(route),
                    theme.paper,
                    shell,
                    UICommand.Command.Route
                );
                b.GetComponent<UICommand>().route = route;
            }

            B(navigation, "Status", "상태 명세서", theme.lavender, shell, UICommand.Command.Status);
            Move(hud.pauseButton, navigation, 48);
            ButtonStyle(hud.pauseButton, theme.ink);
            hud.pauseButton.GetComponentInChildren<TMP_Text>().color = theme.white;
            var feed = Card(frame, "Event ticker / system messages", theme.ink, 12);
            Size(feed, 66);
            Move(hud.logText, feed.transform, -1);
            StyleText(hud.logText, 19, theme.white);
            hud.logText.maxVisibleLines = 2;
            hud.logText.overflowMode = TextOverflowModes.Ellipsis;
            var notice = Card(overlay, "System notification / approval stamp", theme.coral, 20);
            var nr = notice.rectTransform;
            nr.anchorMin = new Vector2(.1f, .74f);
            nr.anchorMax = new Vector2(.9f, .98f);
            nr.offsetMin = nr.offsetMax = Vector2.zero;
            visual.notice = notice.gameObject;
            visual.noticeText = T(nr, "Notice", "", 28, theme.ink);
            var nc = notice.gameObject.AddComponent<CanvasGroup>();
            nc.blocksRaycasts = nc.interactable = false;
            Deco(nr, UIAccentGraphic.Shape.Stripes, new Color(.1f, .15f, .2f, .12f), new Vector2(.75f, 0), Vector2.one);
            notice.gameObject.SetActive(false);
            var responsive = root.AddComponent<UIResponsiveHUD>();
            responsive.scaler = scaler;
            responsive.frame = frame;
            responsive.middle = middleElement;
            responsive.middleFlow = middleFlow;
            responsive.headerFlow = headerFlow;
            responsive.stats = stats.GetComponent<LayoutElement>();
            responsive.hero = heroElement;
            responsive.mission = missionElement;
            responsive.header = header.GetComponent<LayoutElement>();
            responsive.actions = actionsElement;
            responsive.navigation = navElement;
            responsive.statsFlow = statsFlow;
            responsive.statRows = statRows;
            responsive.actionGrid = actionGrid;
            responsive.navGrid = navGrid;
            responsive.Apply();
            if (old != null)
            {
                old.gameObject.SetActive(false);
            }

            hud.game = null;
        }

        static void StyleMissionOverlays(MissionUI m, UIShell shell)
        {
            m.listPanel.transform.SetParent(shell.overlayRoot, false);
            Stretch(m.listPanel.GetComponent<RectTransform>());
            var window = m.listPanel.transform.GetChild(0);
            var wr = window.GetComponent<RectTransform>();
            Stretch(wr);
            wr.anchorMin = new Vector2(.04f, .04f);
            wr.anchorMax = new Vector2(.96f, .96f);
            window.GetComponent<Image>().color = theme.paper;
            window.GetComponent<Image>().sprite = theme.rounded;
            window.GetComponent<Image>().type = Image.Type.Sliced;
            Col(window, 20, 12);
            var heading = window.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "Heading");
            var bar = R(window, "Mission archive header");
            Size(bar, 48);
            Row(bar);
            if (heading != null)
            {
                Move(heading, bar, 44);
                StyleText(heading, 28, theme.ink);
                heading.text = "인생 업무 목록 / 정확한 달성 조건";
            }

            Move(m.closeListButton, bar, 48);
            Size(m.closeListButton, 48, 150);
            ButtonStyle(m.closeListButton, theme.coral);
            var viewport = m.rowContainer.parent.GetComponent<RectTransform>();
            Move(viewport, window);
            Size(viewport, -1, -1, 1);
            viewport.GetComponent<Image>().color = theme.paper;
            var oldScroll = window.GetComponent<ScrollRect>();
            if (oldScroll != null)
            {
                oldScroll.verticalScrollbar = null;
            }

            m.listPanel.SetActive(false);
            m.educationPanel.transform.SetParent(shell.overlayRoot, false);
            Stretch(m.educationPanel.GetComponent<RectTransform>());
            var edu = m.educationPanel.transform.GetChild(0);
            Stretch(edu.GetComponent<RectTransform>());
            edu.GetComponent<RectTransform>().anchorMin = new Vector2(.04f, .04f);
            edu.GetComponent<RectTransform>().anchorMax = new Vector2(.96f, .96f);
            edu.GetComponent<Image>().sprite = theme.rounded;
            edu.GetComponent<Image>().type = Image.Type.Sliced;
            edu.GetComponent<Image>().color = theme.lavender;
            Col(edu, 20, 12);
            var title = edu.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "Title");
            var ebar = R(edu, "Education heading");
            Size(ebar, 48);
            Row(ebar);
            if (title != null)
            {
                Move(title, ebar, 44);
                StyleText(title, 30, theme.ink);
            }

            Move(m.closeEducationButton, ebar, 48);
            Size(m.closeEducationButton, 48, 150);
            ButtonStyle(m.closeEducationButton, theme.white);
            Move(m.educationHint, edu, 60);
            StyleText(m.educationHint, 24, theme.ink);
            ScrollRect scr;
            var body = Scroll(edu, "School actions", out scr);
            var grid = R(body, "Actions grid");
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.spacing = new Vector2(12, 12);
            var fitter = grid.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var responsive = grid.gameObject.AddComponent<UIResponsiveGrid>();
            responsive.grid = gl;
            responsive.rowHeight = 92;
            foreach (var button in m.activityButtons)
            {
                Move(button, grid, 92);
                ButtonStyle(button, theme.white);
            }

            m.educationPanel.SetActive(false);
            if (m.toastPanel != null)
            {
                m.toastPanel.transform.SetParent(shell.overlayRoot, false);
                m.toastPanel.SetActive(false);
                var cg = C<CanvasGroup>(m.toastPanel);
                cg.blocksRaycasts = false;
                cg.alpha = 0;
            }
        }

        static void StyleRows()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/_Game/Prefabs/MissionRow.prefab");
            try
            {
                var view = root.GetComponent<MissionRowView>();
                root.GetComponent<Image>().sprite = theme.rounded;
                root.GetComponent<Image>().type = Image.Type.Sliced;
                root.GetComponent<Image>().color = theme.white;
                Col(root.transform, 18, 8);
                Size(root.GetComponent<RectTransform>(), -1);
                var fit = C<ContentSizeFitter>(root);
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Move(view.titleText, root.transform);
                StyleText(view.titleText, 27, theme.ink);
                Move(view.stateText, root.transform);
                StyleText(view.stateText, 22, theme.ink);
                Move(view.progressText, root.transform);
                StyleText(view.progressText, 21, theme.ink);
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Prefabs/MissionRow.prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void StyleLocation(Phase23Presenter p, UIShell shell)
        {
            p.panel.transform.SetParent(shell.overlayRoot, false);
            var pr = p.panel.GetComponent<RectTransform>();
            Stretch(pr);
            pr.anchorMin = new Vector2(.04f, .04f);
            pr.anchorMax = new Vector2(.96f, .96f);
            p.panel.GetComponent<Image>().sprite = theme.rounded;
            p.panel.GetComponent<Image>().type = Image.Type.Sliced;
            p.panel.GetComponent<Image>().color = theme.paper;
            Col(pr, 20, 12);
            var top = R(pr, "Location heading");
            Row(top);
            Size(top, 48);
            var title = pr.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "Title");
            if (title != null)
            {
                Move(title, top, 44);
                StyleText(title, 30, theme.ink);
            }

            Move(p.closeButton, top, 48);
            Size(p.closeButton, 48, 150);
            ButtonStyle(p.closeButton, theme.coral);
            ScrollRect scroll;
            var body = Scroll(pr, "Location content", out scroll);
            Move(p.status, body);
            StyleText(p.status, 24, theme.ink);
            Move(p.details, body);
            StyleText(p.details, 22, theme.ink);
            var grid = R(body, "Location actions");
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.spacing = new Vector2(12, 12);
            grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var response = grid.gameObject.AddComponent<UIResponsiveGrid>();
            response.grid = gl;
            response.rowHeight = 100;
            foreach (var b in new[] { p.npcButton, p.jobButton, p.careerButton }.Concat(p.activityButtons))
            {
                Move(b, grid, 100);
                ButtonStyle(b, theme.lavender);
            }

            foreach (var b in p.navigationButtons)
            {
                b.gameObject.SetActive(false);
            }

            Move(p.result, body);
            StyleText(p.result, 24, theme.ink);
            var hud = shell.GetComponent<UIManager>();
            var mission = hud.GetComponentInChildren<MissionUI>(true);
            Move(p.openButton, mission.educationButton.transform.parent, 48);
            ButtonStyle(p.openButton, theme.white);
            p.openButton.GetComponentInChildren<TMP_Text>().text = "장소별 활동";
            p.openButton.GetComponent<LayoutElement>().preferredWidth = -1;
            p.panel.SetActive(false);
        }

        static void BuildMenu()
        {
            var s = EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity");
            var presenter = UnityEngine.Object.FindFirstObjectByType<MainMenuPresenter>();
            if (UnityEngine.Object.FindFirstObjectByType<UIShell>() != null)
            {
                return;
            }

            var canvas = presenter.GetComponentInParent<Canvas>();
            var old = presenter.transform;
            old.SetParent(canvas.transform, false);
            var safe = R(canvas.transform, "Menu safe area");
            Stretch(safe);
            var frame = R(safe, "Menu content");
            Stretch(frame);
            var cg = frame.gameObject.AddComponent<CanvasGroup>();
            Col(frame, 28, 18);
            var safeLayout = canvas.gameObject.AddComponent<UISafeLayout>();
            safeLayout.safe = safe;
            safeLayout.frame = frame;
            RectTransform overlay;
            var shell = Shell(canvas.transform, cg, out overlay);
            shell.mainMenu = true;
            Settings(shell, null);
            var bg = R(canvas.transform, "Menu paper");
            Stretch(bg);
            bg.gameObject.AddComponent<Image>().color = theme.paper;
            bg.SetAsFirstSibling();
            Deco(bg, UIAccentGraphic.Shape.Grid, new Color(.1f, .15f, .2f, .08f), Vector2.zero, Vector2.one);
            T(frame, "Imprint", "LIFE MANAGEMENT BUREAU   /   이안 인생 관리국", 24, theme.ink, 44);
            var hero = Card(frame, "Main title card", theme.coral, 30);
            Size(hero, -1, -1, 1);
            T(hero.transform, "Edition", "BLACK COMEDY EDITION  /  업무 개시 안내", 24, theme.ink, 42);
            var row = R(hero.transform, "Title and official Ian");
            Row(row, 0, 18);
            Size(row, -1, -1, 1);
            var text = Col(R(row, "Title column"), 0, 12);
            Size(text, -1, -1, 1);
            T(text.transform, "Title", "이안\n키우기.", 92, theme.ink, 250);
            T(text.transform, "Tagline", "태어난 김에,\n흑자 인생을 만들어 보세요.", 32, theme.ink, 110);
            T(text.transform, "Footnote", "성장 · 생존 · 노동\n행복은 별도 구매입니다.", 24, theme.ink, 80);
            var image = R(row, "Official Ian").gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase
                .LoadAssetAtPath<CharacterIdentity>("Assets/_Game/Data/IanOfficialIdentity.asset")
                .baseSprite;
            image.preserveAspect = true;
            Size(image, -1, 300, 1);
            Deco(
                hero.transform,
                UIAccentGraphic.Shape.Sunburst,
                new Color(1, 1, .6f, .7f),
                new Vector2(.5f, .03f),
                new Vector2(.98f, .95f)
            );
            var buttons = R(frame, "Menu actions");
            Size(buttons, 76);
            Row(buttons, 0, 16);
            Move(presenter.continueButton, buttons, 68);
            Move(presenter.newGameButton, buttons, 68);
            ButtonStyle(presenter.continueButton, theme.lime);
            ButtonStyle(presenter.newGameButton, theme.paper);
            B(buttons, "Settings", "환경 설정", theme.lavender, shell, UICommand.Command.Settings);
            T(
                frame,
                "Footer",
                "마우스 · 터치 · 키보드 · 게임패드 지원 / 미션으로 나이를 먹는 인생 시뮬레이션",
                20,
                theme.ink,
                38
            );
            foreach (Transform c in old)
            {
                if (c != presenter.continueButton.transform && c != presenter.newGameButton.transform)
                {
                    c.gameObject.SetActive(false);
                }
            }

            var oldRect = old.GetComponent<RectTransform>();
            oldRect.sizeDelta = Vector2.zero;
            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s);
        }

        static void BuildVariants()
        {
            string dir = "Assets/_Game/Prefabs/UI";
            Directory.CreateDirectory(dir);
            var button = B(null, "Button Base", "행동 선택", theme.paper);
            PrefabUtility.SaveAsPrefabAsset(button.gameObject, dir + "/ButtonBase.prefab");
            UnityEngine.Object.DestroyImmediate(button.gameObject);
            foreach (var name in new[] { "Primary", "Secondary", "Danger" })
            {
                var instance = (GameObject)
                    PrefabUtility.InstantiatePrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(dir + "/ButtonBase.prefab")
                    );
                ButtonStyle(
                    instance.GetComponent<Button>(),
                    name == "Primary" ? theme.lime
                        : name == "Danger" ? theme.coral
                        : theme.lavender
                );
                PrefabUtility.SaveAsPrefabAsset(instance, dir + "/Button" + name + ".prefab");
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
